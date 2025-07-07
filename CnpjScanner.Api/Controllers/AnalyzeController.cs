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

    [HttpGet("all")]
    public async Task<IActionResult> AnalyzeAll([FromQuery] string path)
    {
        var results = await _analyzer.AnalyzeDirectoryAsync(path);
        return Ok(results);
    }
}
