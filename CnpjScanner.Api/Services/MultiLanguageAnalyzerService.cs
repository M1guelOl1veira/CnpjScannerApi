public class MultiLanguageAnalyzerService
{
    private readonly CSharpAnalyzer _csharp;
    private readonly VBNetAnalyzer _vbnet;
    private readonly TypeScriptAnalyzerService _typescript;

    public MultiLanguageAnalyzerService(
        CSharpAnalyzer csharp,
        VBNetAnalyzer vbnet,
        TypeScriptAnalyzerService typescript)
    {
        _csharp = csharp;
        _vbnet = vbnet;
        _typescript = typescript;
    }

    public async Task<List<VariableMatch>> AnalyzeDirectoryAsync(string directoryPath)
    {
        var result = new List<VariableMatch>();

        if (!Directory.Exists(directoryPath))
            return result;

        var csharpResults = _csharp.AnalyzeCSharpFiles(directoryPath)
            .Select(r => { r.Language = "C#"; return r; });
        result.AddRange(csharpResults);

        var vbnetResults = _vbnet.AnalyzeDirectory(directoryPath)
            .Select(r => { r.Language = "VB.NET"; return r; });
        result.AddRange(vbnetResults);

        var tsResults = await _typescript.AnalyzeAsync(directoryPath);
        foreach (var tsMatch in tsResults)
        {
            result.Add(new VariableMatch
            {
                FilePath = tsMatch.FilePath,
                LineNumber = tsMatch.LineNumber,
                Declaration = tsMatch.Declaration,
                LooksLikeCnpj = tsMatch.LooksLikeCnpj,
                Language = "TypeScript",
                Type = "number"
            });
        }

        return result.OrderByDescending(x => x.LooksLikeCnpj).ToList();
    }
}
