using CnpjScanner.Api.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System.Text.RegularExpressions;

namespace CnpjScanner.Api.Analyzers
{
    public class VBNetAnalyzer
    {
        private static readonly Regex CnpjRegex = new(@"\d{2}\.??\d{3}\.??\d{3}/??\d{4}-??\d{2}", RegexOptions.Compiled);
        private static readonly string[] Keywords = ["cnpj"];
        private static readonly string[] Types = ["Int32", "Int64", "Object", "String"];
        private static readonly string[] ExcludedDirs = ["bin", "obj", ".git", ".vs", "packages"];

        public async Task<List<VariableMatch>> AnalyzeDirectoryAsync(string rootPath)
        {
            var matches = new List<VariableMatch>();
            var vbFiles = Directory.GetFiles(rootPath, "*.vb", SearchOption.AllDirectories)
                .Where(file => !ExcludedDirs.Any(ex => file.Split(Path.DirectorySeparatorChar).Contains(ex)))
                .ToList();

            foreach (var file in vbFiles)
            {
                try
                {
                    var code = await File.ReadAllTextAsync(file);
                    var tree = VisualBasicSyntaxTree.ParseText(code);
                    var root = await tree.GetRootAsync();

                    var compilation = VisualBasicCompilation.Create("VBAnalysis")
                        .AddSyntaxTrees(tree)
                        .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

                    var model = compilation.GetSemanticModel(tree);

                    var visitor = new VBNetVariableVisitor(file, model);
                    visitor.Visit(root);

                    matches.AddRange(visitor.Matches);

                    // Help GC reclaim memory
                    compilation = null;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error analyzing {file}: {ex.Message}");
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            return matches;
        }

        private class VBNetVariableVisitor : VisualBasicSyntaxWalker
        {
            private readonly string _filePath;
            private readonly SemanticModel _model;
            public List<VariableMatch> Matches { get; } = new();

            public VBNetVariableVisitor(string filePath, SemanticModel model)
            {
                _filePath = filePath;
                _model = model;
            }

            public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
            {
                var nameSyntax = node.Names.FirstOrDefault();
                if (nameSyntax is null) return;

                var symbol = _model.GetDeclaredSymbol(nameSyntax);
                if (symbol is not ILocalSymbol local) return;

                if (ShouldMatch(local.Type.Name))
                {
                    AddMatch(symbol, node.GetLocation(), node.ToFullString());
                    return;
                }

                if (local.Type.Name == "Object" && node.Initializer?.Value is ExpressionSyntax expr)
                {
                    var exprTypeInfo = _model.GetTypeInfo(expr);
                    if (ShouldMatch(exprTypeInfo.Type?.Name))
                    {
                        AddMatch(symbol, node.GetLocation(), node.ToFullString());
                    }
                }
            }

            public override void VisitPropertyStatement(PropertyStatementSyntax node)
            {
                var symbol = _model.GetDeclaredSymbol(node);
                if (symbol is IPropertySymbol prop && ShouldMatch(prop.Type.Name))
                {
                    AddMatch(symbol, node.GetLocation(), node.ToFullString());
                }
            }

            public override void VisitParameter(ParameterSyntax node)
            {
                var symbol = _model.GetDeclaredSymbol(node);
                if (symbol is IParameterSymbol param && ShouldMatch(param.Type.Name))
                {
                    AddMatch(symbol, node.GetLocation(), node.ToFullString());
                }
            }

            private void AddMatch(ISymbol symbol, Location location, string declaration)
            {
                var name = symbol.Name.ToLower();
                if (name == "cp") return;

                var looksLikeCnpj = Keywords.Any(k => name.Contains(k)) || CnpjRegex.IsMatch(declaration);

                Matches.Add(new VariableMatch
                {
                    FilePath = _filePath,
                    LineNumber = location.GetLineSpan().StartLinePosition.Line + 1,
                    Declaration = declaration.Trim(),
                    LooksLikeCnpj = looksLikeCnpj,
                    Type = GetTypeName(symbol),
                    Language = "VB.NET"
                });
            }

            private static string GetTypeName(ISymbol symbol) => symbol switch
            {
                ILocalSymbol local => local.Type.Name,
                IFieldSymbol field => field.Type.Name,
                IPropertySymbol prop => prop.Type.Name,
                IParameterSymbol param => param.Type.Name,
                _ => "Unknown"
            };

            private static bool ShouldMatch(string? typeName) => typeName != null && Types.Contains(typeName);
        }
    }
}
