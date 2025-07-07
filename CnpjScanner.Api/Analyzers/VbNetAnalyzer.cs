using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

public class VBNetAnalyzer
{
    private static readonly Regex CnpjRegex = new(@"\d{2}\.??\d{3}\.??\d{3}/??\d{4}-??\d{2}", RegexOptions.Compiled);
    private static readonly string[] Keywords = ["cnpj", "tax"];

    public List<VariableMatch> AnalyzeDirectory(string rootPath)
    {
        var matches = new List<VariableMatch>();
        var excludedDirs = new[] { "bin", "obj", ".git", ".vs", "packages" };
        var vbFiles = Directory.GetFiles(rootPath, "*.vb", SearchOption.AllDirectories)
            .Where(file => !excludedDirs.Any(ex => file.Split(Path.DirectorySeparatorChar).Contains(ex)))
            .ToList();

        foreach (var file in vbFiles)
        {
            var code = File.ReadAllText(file);
            var tree = VisualBasicSyntaxTree.ParseText(code);
            var root = tree.GetCompilationUnitRoot();

            var compilation = VisualBasicCompilation.Create("VBAnalysis")
                .AddSyntaxTrees(tree)
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

            var model = compilation.GetSemanticModel(tree);

            var visitor = new VBNetVariableVisitor(file, model, matches);
            visitor.Visit(root);
        }

        return matches;
    }

    private class VBNetVariableVisitor : VisualBasicSyntaxWalker
    {
        private readonly string _filePath;
        private readonly SemanticModel _model;
        private readonly List<VariableMatch> _matches;

        private readonly List<string> _types;
        public VBNetVariableVisitor(string filePath, SemanticModel model, List<VariableMatch> matches)
        {
            _filePath = filePath;
            _model = model;
            _matches = matches;
            _types = ["Int32", "Int64", "Object"];
        }

        public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            var name = node.Names.FirstOrDefault()?.Identifier.Text;
            if (string.IsNullOrEmpty(name)) return;

            var symbol = _model.GetDeclaredSymbol(node.Names.First());
            if ((symbol is ILocalSymbol local && _types.Contains(local.Type.Name)) ||
                (symbol is IFieldSymbol field && _types.Contains(field.Type.Name)))
            {
                AddMatch(symbol, node.GetLocation(), node.ToFullString());
            }
        }

        public override void VisitPropertyStatement(PropertyStatementSyntax node)
        {
            var symbol = _model.GetDeclaredSymbol(node);
            if (symbol is IPropertySymbol prop &&  _types.Contains(prop.Type.Name))
            {
                AddMatch(symbol, node.GetLocation(), node.ToFullString());
            }
        }

        public override void VisitParameter(ParameterSyntax node)
        {
            var symbol = _model.GetDeclaredSymbol(node);
            if (symbol is IParameterSymbol param && _types.Contains(param.Type.Name))
            {
                AddMatch(symbol, node.GetLocation(), node.ToFullString());
            }
        }

        private void AddMatch(ISymbol symbol, Location location, string declaration)
        {
            var name = symbol.Name.ToLower();
            var lineNumber = location.GetLineSpan().StartLinePosition.Line + 1;
            var looksLikeCnpj = Keywords.Any(k => name.Contains(k)) || CnpjRegex.IsMatch(declaration);

            var typeName = symbol switch
            {
                ILocalSymbol local => local.Type.Name,
                IFieldSymbol field => field.Type.Name,
                IPropertySymbol prop => prop.Type.Name,
                IParameterSymbol param => param.Type.Name,
                _ => "Unknown"
            };

            _matches.Add(new VariableMatch
            {
                FilePath = _filePath,
                LineNumber = lineNumber,
                Declaration = declaration.Trim(),
                LooksLikeCnpj = looksLikeCnpj,
                Type = typeName
            });
        }
    }
}