public class VariableMatch
{
    public string FilePath { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public bool LooksLikeCnpj { get; set; }
    public string? Declaration { get; set; }
    public string? Type { get; set; }
    public string? Language { get; set; }
}
