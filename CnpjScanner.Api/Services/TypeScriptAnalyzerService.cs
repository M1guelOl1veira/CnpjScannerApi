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
            Arguments = $"--max-old-space-size=512 ../TypescriptAnalyzer/dist/typescriptAnalyzer.js \"{codePath}\" --quiet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        Task<string> stdOutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stdErrTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        string output = await stdOutTask;
        string error = await stdErrTask;

        // ✅ Only throw if output is invalid (not if there are debug logs in stderr)
        if (string.IsNullOrWhiteSpace(output) || output.Trim().StartsWith("A")) // <- could be adjusted
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
