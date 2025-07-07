// Services/TypeScriptAnalyzerService.cs
using System.Diagnostics;
using System.Text.Json;

public class TypeScriptAnalyzerService
{
    public async Task<List<VariableMatch>> AnalyzeAsync(string codePath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "node",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add("--max-old-space-size=512");
        psi.ArgumentList.Add("../TypescriptAnalyzer/dist/typescriptAnalyzer.js");
        psi.ArgumentList.Add(codePath);
        psi.ArgumentList.Add("--quiet");
        using var process = Process.Start(psi);
        Task<string> stdOutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stdErrTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        string output = await stdOutTask;
        string error = await stdErrTask;

        if (string.IsNullOrWhiteSpace(output) || output.Trim().StartsWith("A"))
        {
            throw new Exception($"TypeScript analyzer error: {error}");
        }

        try
        {
            var result = JsonSerializer.Deserialize<List<VariableMatch>>(output, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<VariableMatch>();

            return result;
        }
        catch (JsonException ex)
        {
            throw new Exception($"Failed to parse analyzer output. Output: {output}", ex);
        }
    }
}
