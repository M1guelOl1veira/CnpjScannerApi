using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;
using System.Text.RegularExpressions;

public class CSharpAnalyzer
{
    private static readonly Regex CnpjRegex = new(@"\d{2}\.??\d{3}\.??\d{3}/??\d{4}-??\d{2}", RegexOptions.Compiled);
    private static readonly string[] CnpjKeywords = new[] { "cnpj", "tax" };

    public async Task<List<VariableMatch>> AnalyzeCSharpFilesAsync(string rootPath)
    {
        var matches = new List<VariableMatch>();
        var files = Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories);
        var language = "C#";

        var syntaxTrees = new List<SyntaxTree>();
        foreach (var file in files)
        {
            var code = await File.ReadAllTextAsync(file);
            syntaxTrees.Add(CSharpSyntaxTree.ParseText(code, path: file));
        }

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location)
        };

        var compilation = CSharpCompilation.Create("Analysis")
            .AddReferences(references)
            .AddSyntaxTrees(syntaxTrees);

        foreach (var tree in syntaxTrees)
        {
            var root = await tree.GetRootAsync();
            var model = compilation.GetSemanticModel(tree);

            var declarations = root.DescendantNodes().OfType<VariableDeclarationSyntax>();

            foreach (var declaration in declarations)
            {
                foreach (var variable in declaration.Variables)
                {
                    var declarationText = declaration.ToFullString().Trim();
                    var valueLiteral = variable.Initializer?.Value as LiteralExpressionSyntax;
                    var rawValue = valueLiteral?.Token.Value?.ToString() ?? "";
                    var line = variable.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    var variableName = variable.Identifier.Text.ToLower();

                    var initializerType = variable.Initializer?.Value != null ? model.GetTypeInfo(variable.Initializer?.Value).Type : null;
                    var declaredType = model.GetTypeInfo(declaration.Type).Type;
                    var inferredType = initializerType?.ToDisplayString() ?? declaredType?.ToDisplayString();
                    bool looksLikeCnpj = CnpjRegex.IsMatch(rawValue ?? "") || CnpjKeywords.Any(k => variableName.Contains(k));

                    if (inferredType is "System.Int32" or "System.Int64" or "System.Int16" or "int" or "long")
                    {
                        matches.Add(new VariableMatch
                        {
                            FilePath = tree.FilePath,
                            LineNumber = line,
                            LooksLikeCnpj = looksLikeCnpj,
                            Declaration = declarationText,
                            Type = inferredType,
                            Language = language
                        });
                    }
                }
            }

            var forStatements = root.DescendantNodes().OfType<ForStatementSyntax>();
            foreach (var forStatement in forStatements)
            {
                if (forStatement.Declaration != null)
                {
                    var declaration = forStatement.Declaration;

                    foreach (var variable in declaration.Variables)
                    {
                        var declarationText = declaration.ToFullString().Trim();
                        var value = variable.Initializer?.Value.ToString() ?? "";
                        var valueLiteral = variable.Initializer?.Value as LiteralExpressionSyntax;
                        var rawValue = valueLiteral?.Token.Value?.ToString() ?? value;
                        var line = variable.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        var variableName = variable.Identifier.Text.ToLower();

                        var initializerType = model.GetTypeInfo(variable.Initializer?.Value).Type;
                        var declaredType = model.GetTypeInfo(declaration.Type).Type;
                        var inferredType = initializerType?.ToDisplayString() ?? declaredType?.ToDisplayString();
                        bool looksLikeCnpj = CnpjRegex.IsMatch(rawValue ?? "") || CnpjKeywords.Any(k => variableName.Contains(k));

                        if (inferredType is "System.Int32" or "System.Int64" or "System.Int16")
                        {
                            matches.Add(new VariableMatch
                            {
                                FilePath = tree.FilePath,
                                LineNumber = line,
                                LooksLikeCnpj = looksLikeCnpj,
                                Declaration = declarationText,
                                Type = "<for loop>",
                                Language = language
                            });
                        }
                    }
                }
            }

            var foreachStatements = root.DescendantNodes().OfType<ForEachStatementSyntax>();
            foreach (var foreachStatement in foreachStatements)
            {
                var type = model.GetTypeInfo(foreachStatement.Type).Type;
                var variableName = foreachStatement.Identifier.Text.ToLower();
                bool looksLikeCnpj = CnpjKeywords.Any(k => variableName.Contains(k));
                if (type != null && (type.ToString().StartsWith("int") || type.ToString().StartsWith("long")))
                {
                    matches.Add(new VariableMatch
                    {
                        FilePath = tree.FilePath,
                        LineNumber = foreachStatement.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        LooksLikeCnpj = looksLikeCnpj,
                        Declaration = foreachStatement.ToFullString().Trim(),
                        Type = "<for each>",
                        Language = language
                    });
                }
            }

            var propertyDeclarations = root.DescendantNodes().OfType<PropertyDeclarationSyntax>();
            foreach (var prop in propertyDeclarations)
            {
                var typeInfo = model.GetTypeInfo(prop.Type);
                var inferredType = typeInfo.Type?.ToDisplayString();
                var line = prop.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                var propertyCode = prop.ToFullString().Trim();
                var propName = prop.Identifier.Text.ToLower();
                bool looksLikeCnpj = CnpjKeywords.Any(k => propName.Contains(k));

                if (inferredType is "System.Int32" or "System.Int64" or "System.Int16" or "int" or "long")
                {
                    matches.Add(new VariableMatch
                    {
                        FilePath = tree.FilePath,
                        LineNumber = line,
                        LooksLikeCnpj = looksLikeCnpj,
                        Declaration = propertyCode,
                        Type = inferredType,
                        Language = language
                    });
                }
            }
        }

        return matches;
    }
}
