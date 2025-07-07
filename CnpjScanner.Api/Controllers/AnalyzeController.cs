using CnpjScanner.Api.Models;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/analyze")]
public class AnalyzeController : ControllerBase
{
    private readonly MultiLanguageAnalyzerService _analyzer;

    public AnalyzeController(MultiLanguageAnalyzerService analyzer)
    {
        _analyzer = analyzer;
    }

    [HttpPost("all")]
    public async Task<IActionResult> AnalyzeAll(string[] extensions, string path, int pageNumber = 1, int pageSize = 10)
    {
        var results = await _analyzer.AnalyzeDirectoryAsync(path, extensions);
        var pagedResults = results
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        
        var paginated = new PaginatedResult<VariableMatch>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = results.Count,
            Items = pagedResults
        };

        return Ok(paginated);
    }
}
