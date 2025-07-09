using CnpjScanner.Api.Analyzers;
using CnpjScanner.Api.Models;

namespace CnpjScanner.Api.Services
{
    public class MultiLanguageAnalyzerService(
        CSharpAnalyzer csharp,
        VBNetAnalyzer vbnet,
        TypeScriptAnalyzerService typescript)
    {
        private readonly CSharpAnalyzer _csharp = csharp;
        private readonly VBNetAnalyzer _vbnet = vbnet;
        private readonly TypeScriptAnalyzerService _typescript = typescript;

        public async Task<List<VariableMatch>> AnalyzeDirectoryAsync(string directoryPath, string[] extensions)
        {
            if (extensions.Count() == 0)
                extensions = ["cs", "vb", "ts"];
            var allMatches = new List<VariableMatch>();
            var tasks = new List<Task<List<VariableMatch>>>();

            if (extensions.Contains("cs"))
                tasks.Add(_csharp.AnalyzeCSharpFilesAsync(directoryPath));

            if (extensions.Contains("ts"))
                tasks.Add(AnalyzeAsVariableMatchesAsync(directoryPath));

            if (extensions.Contains("vb"))
                tasks.Add(_vbnet.AnalyzeDirectoryAsync(directoryPath));

            var results = await Task.WhenAll(tasks);
            foreach (var result in results)
                allMatches.AddRange(result);

            return allMatches.OrderByDescending(x => x.LooksLikeCnpj).ToList();
        }
        public async Task<List<VariableMatch>> AnalyzeAsVariableMatchesAsync(string codePath)
        {
            var tsResults = await _typescript.AnalyzeAsync(codePath);
            return tsResults.Select(match => new VariableMatch
            {
                FilePath = match.FilePath,
                LineNumber = match.LineNumber,
                Declaration = match.Declaration,
                LooksLikeCnpj = match.LooksLikeCnpj,
                Language = "TypeScript",
                Type = "number"
            }).ToList();
        }
    }
}