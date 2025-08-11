// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.FastSyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.SyntaxKeywords;

namespace Snap.Hutao.SourceGeneration.Automation;

[Generator(LanguageNames.CSharp)]
internal sealed class ApiEndpointsGenerator : IIncrementalGenerator
{
    private const string FileName = "Endpoints.csv";
    private static readonly ExpressionSyntax ThrowNotSupportedException = ThrowExpression(ObjectCreationExpression(IdentifierName("NotSupportedException")).WithArgumentList());

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<ImmutableArray<AdditionalText>> provider = context.AdditionalTextsProvider.Where(Match).Collect();
        context.RegisterImplementationSourceOutput(provider, GenerateWrapper);
    }

    private static bool Match(AdditionalText text)
    {
        // Match '*Endpoints.csv' files
        return Path.GetFileName(text.Path).EndsWith(FileName, StringComparison.OrdinalIgnoreCase);
    }

    private static void GenerateWrapper(SourceProductionContext production, ImmutableArray<AdditionalText> texts)
    {
        try
        {
            GenerateAll(production, texts);
        }
        catch (Exception ex)
        {
            production.AddSource($"Error-{Guid.NewGuid().ToString()}.g.cs", ex.ToString());
        }
    }

    private static void GenerateAll(SourceProductionContext production, ImmutableArray<AdditionalText> texts)
    {
        foreach (AdditionalText csvFile in texts)
        {
            string fileName = Path.GetFileNameWithoutExtension(csvFile.Path);

            EndpointsExtraInfo? extraInfo = default;
            ImmutableArray<EndpointsMetadata>.Builder endpointsBuilder = ImmutableArray.CreateBuilder<EndpointsMetadata>();
            using (StringReader reader = new(csvFile.GetText(production.CancellationToken)!.ToString()))
            {
                while (reader.ReadLine() is { Length: > 0 } line)
                {
                    if (line is "Name,CN,OS")
                    {
                        continue;
                    }

                    if (line.StartsWith("Extra:", StringComparison.OrdinalIgnoreCase))
                    {
                        extraInfo = JsonSerializer.Deserialize<EndpointsExtraInfo>(line[6..]);
                        continue;
                    }

                    IReadOnlyList<string> columns = ParseCsvLine(line);
                    string? methodDeclarationString = columns.ElementAtOrDefault(0);
                    string? chinese = columns.ElementAtOrDefault(1);
                    string? oversea = columns.ElementAtOrDefault(2);
                    EndpointsMetadata metadata = new()
                    {
                        MethodDeclaration = string.IsNullOrEmpty(methodDeclarationString) ? default : ParseMemberDeclaration(methodDeclarationString),
                        Chinese = chinese,
                        ChineseExpression = string.IsNullOrEmpty(chinese) ? ThrowNotSupportedException : ParseExpression($"$\"{chinese}\""),
                        Oversea = oversea,
                        OverseaExpression = string.IsNullOrEmpty(oversea) ? ThrowNotSupportedException : ParseExpression($"$\"{oversea}\""),
                    };

                    endpointsBuilder.Add(metadata);
                }
            }

            if (endpointsBuilder.Count <= 0)
            {
                return;
            }

            ImmutableArray<EndpointsMetadata> endpoints = endpointsBuilder.ToImmutable();

            string interfaceName = $"I{fileName}";
            IdentifierNameSyntax interfaceIdentifier = IdentifierName(interfaceName);
            string chineseImplName = $"{fileName}ImplementationForChinese";
            string overseaImplName = $"{fileName}ImplementationForOversea";

            CompilationUnitSyntax compilation = CompilationUnit()
                .WithMembers(SingletonList<MemberDeclarationSyntax>(FileScopedNamespaceDeclaration(extraInfo?.Namespace ?? "Snap.Hutao.Web")
                    .WithLeadingTrivia(NullableEnableList)
                    .WithMembers(
                        List<MemberDeclarationSyntax>(
                        [
                            InterfaceDeclaration(interfaceName)
                                .WithModifiers(InternalPartialTokenList)
                                .WithMembers(List(GenerateInterfaceMethods(endpoints))),
                            ClassDeclaration(chineseImplName)
                                .WithModifiers(InternalAbstractTokenList)
                                .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(interfaceIdentifier))))
                                .WithMembers(List(GenerateClassMethods(endpoints, true))),
                            ClassDeclaration(overseaImplName)
                                .WithModifiers(InternalAbstractTokenList)
                                .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(interfaceIdentifier))))
                                .WithMembers(List(GenerateClassMethods(endpoints, false)))
                        ]))))
                .NormalizeWhitespace();

            production.AddSource($"{fileName}.g.cs", compilation.ToFullString());
        }
    }

    private static IEnumerable<MemberDeclarationSyntax> GenerateInterfaceMethods(ImmutableArray<EndpointsMetadata> metadataArray)
    {
        foreach (EndpointsMetadata metadata in metadataArray)
        {
            if (metadata.MethodDeclaration is not MethodDeclarationSyntax methodDeclaration)
            {
                continue;
            }

            string lead = $"""
                /// <summary>
                /// <code>CN: {metadata.Chinese?.Replace("&", "&amp;")}</code>
                /// <code>OS: {metadata.Oversea?.Replace("&", "&amp;")}</code>
                /// </summary>

                """;

            yield return methodDeclaration
                .WithLeadingTrivia(ParseLeadingTrivia(lead))
                .WithSemicolonToken(SemicolonToken);
        }
    }

    private static IEnumerable<MemberDeclarationSyntax> GenerateClassMethods(ImmutableArray<EndpointsMetadata> metadataArray, bool isChinese)
    {
        foreach (EndpointsMetadata metadata in metadataArray)
        {
            if (metadata.MethodDeclaration is not MethodDeclarationSyntax methodDeclaration)
            {
                continue;
            }

            yield return methodDeclaration
                .WithModifiers(PublicTokenList)
                .WithExpressionBody(ArrowExpressionClause(isChinese ? metadata.ChineseExpression : metadata.OverseaExpression))
                .WithSemicolonToken(SemicolonToken);
        }
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

    private sealed class EndpointsMetadata
    {
        public required MemberDeclarationSyntax? MethodDeclaration { get; init; }

        public required string? Chinese { get; init; }

        public required ExpressionSyntax ChineseExpression { get; init; }

        public required string? Oversea { get; init; }

        public required ExpressionSyntax OverseaExpression { get; init; }
    }

    private sealed class EndpointsExtraInfo
    {
        public string? Namespace { get; init; }
    }
}