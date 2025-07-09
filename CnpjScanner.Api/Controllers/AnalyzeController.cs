using CnpjScanner.Api.Interfaces;
using CnpjScanner.Api.Models;
using CnpjScanner.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CnpjScanner.Api.Controllers
{
    [ApiController]
    [Route("api/analyze")]
    public class AnalyzeController(MultiLanguageAnalyzerService analyzer, IGitService gitService) : ControllerBase
    {
        private readonly MultiLanguageAnalyzerService _analyzer = analyzer;
        private readonly IGitService _gitService = gitService;

        [HttpGet("repo")]
        public async Task<IActionResult> AnalyzeAll([FromQuery]RepoRequest request)
        {
            string repoPath = "";
            try
            {
                repoPath = await _gitService.CloneRepoAsync(request);
                var results = await _analyzer.AnalyzeDirectoryAsync(repoPath, request.Extensions!);
                var pagedResults = results
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                var paginated = new PaginatedResult<VariableMatch>
                {
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalCount = results.Count,
                    Items = pagedResults
                };

                return Ok(paginated);
            }
            finally
            {
                TryForceDelete(repoPath);
            }
        }

        private static void TryForceDelete(string path)
        {
            try
            {
                if (!Directory.Exists(path)) return;

                foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                {
                    try { System.IO.File.SetAttributes(file, FileAttributes.Normal); }
                    catch { /* ignore locked file */ }
                }

                Directory.Delete(path, true);
            }
            catch (Exception ex)
            {
                // Optionally log error
                Console.WriteLine($"Failed to delete temp repo: {ex.Message}");
            }
        }
    }
}