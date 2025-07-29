using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.FastSyntaxFactory;

namespace Snap.Hutao.SourceGeneration.Automation;

[Generator(LanguageNames.CSharp)]
internal sealed class ApiEndpointsGenerator : IIncrementalGenerator
{
    private const string FileName = "Endpoints.csv";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<ImmutableArray<AdditionalText>> provider = context.AdditionalTextsProvider.Where(Match).Collect();
        context.RegisterImplementationSourceOutput(provider, GenerateAll);
    }

    private static bool Match(AdditionalText text)
    {
        // Match '*Endpoints.csv' files
        return Path.GetFileName(text.Path).EndsWith(FileName, StringComparison.OrdinalIgnoreCase);
    }

    private static void GenerateAll(SourceProductionContext context, ImmutableArray<AdditionalText> texts)
    {
        foreach (AdditionalText csvFile in texts)
        {
            string fileName = Path.GetFileName(csvFile.Path);

            ImmutableArray<ApiEndpointsMetadata>.Builder endpointsBuilder = ImmutableArray.CreateBuilder<ApiEndpointsMetadata>();
            using (StringReader reader = new(csvFile.GetText(context.CancellationToken)!.ToString()))
            {
                while (reader.ReadLine() is { Length: > 0 } line)
                {
                    if (line is "Name,CN,OS")
                    {
                        continue;
                    }

                    IReadOnlyList<string> columns = ParseCsvLine(line);
                    ApiEndpointsMetadata metadata = new()
                    {
                        MethodSignature = columns.ElementAtOrDefault(0),
                        Chinese = columns.ElementAtOrDefault(1),
                        Oversea = columns.ElementAtOrDefault(2)
                    };

                    endpointsBuilder.Add(metadata);
                }
            }

            if (endpointsBuilder.Count <= 0)
            {
                return;
            }

            ImmutableArray<ApiEndpointsMetadata> endpoints = endpointsBuilder.ToImmutable();

            string interfaceName = $"I{fileName}";
            string chineseImplName = $"{fileName}ImplementationForChinese";
            string overseaImplName = $"{fileName}ImplementationForOversea";

            _ = CompilationUnit()
                .WithUsings(SingletonList(UsingDirective("Snap", "Hutao", "Web", "Hoyolab")))
                .WithMembers(SingletonList<MemberDeclarationSyntax>(FileScopedNamespaceDeclaration("Snap", "Hutao", "Web", "Endpoint", "Hoyolab")
                    .WithMembers(
                        List<MemberDeclarationSyntax>(
                        [
                            InterfaceDeclaration(interfaceName)
                                .WithModifiers(TokenList(InternalKeyword, PartialKeyWord))
                                .WithMembers(List<MemberDeclarationSyntax>()),
                            ClassDeclaration(chineseImplName)
                                .WithModifiers(TokenList(InternalKeyword, AbstractKeyword))
                                .WithMembers(List<MemberDeclarationSyntax>()),
                            ClassDeclaration(overseaImplName)
                                .WithModifiers(TokenList(InternalKeyword, AbstractKeyword))
                                .WithMembers(List<MemberDeclarationSyntax>())
                        ]))))
                .NormalizeWhitespace();

            string source = $$"""
                using Snap.Hutao.Web.Hoyolab;

                namespace Snap.Hutao.Web.Endpoint.Hoyolab;

                internal partial interface IApiEndpoints
                {
                {{FillWithInterfaceMethods(endpoints)}}
                }

                internal abstract class ApiEndpointsImplementationForChinese : IApiEndpoints
                {
                {{FillWithChineseMethods(endpoints)}}
                }

                internal abstract class ApiEndpointsImplementationForOversea : IApiEndpoints
                {
                {{FillWithOverseaMethods(endpoints)}}
                }
                """;

            context.AddSource($"{fileName}.g.cs", source);
        }
    }

    private static string FillWithInterfaceMethods(ImmutableArray<ApiEndpointsMetadata> apis)
    {
        StringBuilder resultBuilder = new();

        foreach (ApiEndpointsMetadata metadata in apis)
        {
            resultBuilder.AppendLine($@"    /// <summary>");
            resultBuilder.AppendLine($@"    /// <code>CN: {metadata.Chinese?.Replace("&", "&amp;")}</code>");
            resultBuilder.AppendLine($@"    /// <code>OS: {metadata.Oversea?.Replace("&", "&amp;")}</code>");
            resultBuilder.AppendLine($@"    /// </summary>");
            resultBuilder.AppendLine($@"    string {metadata.MethodSignature};");
            resultBuilder.AppendLine();
        }

        return resultBuilder.ToString();
    }

    private static string FillWithChineseMethods(ImmutableArray<ApiEndpointsMetadata> apis)
    {
        StringBuilder resultBuilder = new();

        foreach (ApiEndpointsMetadata api in apis)
        {
            if (string.IsNullOrWhiteSpace(api.Chinese))
            {
                resultBuilder.AppendLine($"""    public string {api.MethodSignature} => throw new NotSupportedException();""");
            }
            else
            {
                resultBuilder.AppendLine($"""    public string {api.MethodSignature} => $"{api.Chinese}";""");
            }
        }

        return resultBuilder.ToString();
    }

    private static string FillWithOverseaMethods(ImmutableArray<ApiEndpointsMetadata> apis)
    {
        StringBuilder resultBuilder = new();

        foreach (ApiEndpointsMetadata api in apis)
        {
            if (string.IsNullOrWhiteSpace(api.Oversea))
            {
                resultBuilder.AppendLine($"""    public string {api.MethodSignature} => throw new NotSupportedException();""");
            }
            else
            {
                resultBuilder.AppendLine($"""    public string {api.MethodSignature} => $"{api.Oversea}";""");
            }
        }

        return resultBuilder.ToString();
    }

    private static IReadOnlyList<string> ParseCsvLine(string line)
    {
        List<string> fields = [];
        StringBuilder currentField = new();
        bool insideQuotes = false;

        ReadOnlySpan<char> lineSpan = line.AsSpan();
        for (int i = 0; i < lineSpan.Length; i++)
        {
            ref readonly char currentChar = ref lineSpan[i];

            if (currentChar is '"')
            {
                if (insideQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // 处理双引号转义
                    currentField.Append('"');
                    i++;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }
            }
            else if (currentChar == ',' && !insideQuotes)
            {
                fields.Add(currentField.ToString());
                currentField.Clear();
            }
            else
            {
                currentField.Append(currentChar);
            }
        }

        // 添加最后一个字段
        fields.Add(currentField.ToString());

        return fields;
    }

    private sealed class ApiEndpointsMetadata
    {
        public required string? MethodSignature { get; init; }

        public required string? Chinese { get; init; }

        public required string? Oversea { get; init; }
    }
}