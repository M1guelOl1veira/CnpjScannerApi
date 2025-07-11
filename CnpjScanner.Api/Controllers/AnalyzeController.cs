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
        private static readonly Dictionary<string, List<VariableMatch>> _repoCache = new();

        [HttpGet("repo")]
        public async Task<IActionResult> AnalyzeAll([FromQuery] RepoRequest request)
        {
            string repoPath = "";
            if (!_repoCache.ContainsKey(repoPath))
            {
                repoPath = await _gitService.CloneRepoAsync(request);
                var results = await _analyzer.AnalyzeDirectoryAsync(repoPath, request.Extensions!);
                _repoCache[repoPath] = results;
            }

            return Ok(_repoCache[repoPath]);
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