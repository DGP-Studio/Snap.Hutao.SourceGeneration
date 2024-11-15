using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

namespace Snap.Hutao.SourceGeneration.Automation;

[Generator(LanguageNames.CSharp)]
internal sealed class ApiEndpointsGenerator : IIncrementalGenerator
{
    private const string FileName = "ApiEndpoints.csv";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<ImmutableArray<AdditionalText>> provider = context.AdditionalTextsProvider.Where(MatchFileName).Collect();

        context.RegisterImplementationSourceOutput(provider, GenerateApiEndpoints);
    }

    private static bool MatchFileName(AdditionalText text)
    {
        return string.Equals(Path.GetFileName(text.Path), FileName, StringComparison.OrdinalIgnoreCase);
    }

    private static void GenerateApiEndpoints(SourceProductionContext context, ImmutableArray<AdditionalText> texts)
    {
        AdditionalText csvFile = texts.Single();

        List<ApiEndpointsMetadata> apis = [];
        using (StringReader reader = new(csvFile.GetText(context.CancellationToken)!.ToString()))
        {
            while (reader.ReadLine() is { Length: > 0 } line)
            {
                if (line is "Name,CN,OS")
                {
                    continue;
                }

                string[] parts = ParseCsvLine(line);
                ApiEndpointsMetadata metadata = new()
                {
                    MethodName = parts.ElementAtOrDefault(0),
                    Chinese = parts.ElementAtOrDefault(1),
                    Oversea = parts.ElementAtOrDefault(2)
                };

                apis.Add(metadata);
            }
        }

        if (apis.Count <= 0)
        {
            return;
        }

        string source = $$"""
            using Snap.Hutao.Web.Hoyolab;

            namespace Snap.Hutao.Web.Endpoint.Hoyolab;

            internal partial interface IApiEndpoints
            {
            {{FillWithInterfaceMethods(apis)}}
            }

            internal abstract class ApiEndpointsImplmentationForChinese : IApiEndpoints
            {
            {{FillWithChineseMethods(apis)}}
            }

            internal abstract class ApiEndpointsImplmentationForOversea : IApiEndpoints
            {
            {{FillWithOverseaMethods(apis)}}
            }
            """;

        context.AddSource("ApiEndpoints.g.cs", source);
    }

    private static string FillWithInterfaceMethods(List<ApiEndpointsMetadata> apis)
    {
        StringBuilder resultBuilder = new();

        foreach (ApiEndpointsMetadata metadata in apis)
        {
            resultBuilder.AppendLine($@"    /// <summary>");
            resultBuilder.AppendLine($@"    /// <code>CN: {metadata.Chinese?.Replace("&", "&amp;")}</code>");
            resultBuilder.AppendLine($@"    /// <code>OS: {metadata.Oversea?.Replace("&", "&amp;")}</code>");
            resultBuilder.AppendLine($@"    /// </summary>");
            resultBuilder.AppendLine($@"    string {metadata.MethodName};");
            resultBuilder.AppendLine();
        }

        return resultBuilder.ToString();
    }

    private static string FillWithChineseMethods(List<ApiEndpointsMetadata> apis)
    {
        StringBuilder resultBuilder = new();

        foreach (ApiEndpointsMetadata api in apis)
        {
            if (string.IsNullOrWhiteSpace(api.Chinese))
            {
                resultBuilder.AppendLine($"""    public string {api.MethodName} => throw new NotSupportedException();""");
            }
            else
            {
                resultBuilder.AppendLine($"""    public string {api.MethodName} => $"{api.Chinese}";""");
            }
        }

        return resultBuilder.ToString();
    }

    private static string FillWithOverseaMethods(List<ApiEndpointsMetadata> apis)
    {
        StringBuilder resultBuilder = new();

        foreach (ApiEndpointsMetadata api in apis)
        {
            if (string.IsNullOrWhiteSpace(api.Oversea))
            {
                resultBuilder.AppendLine($"""    public string {api.MethodName} => throw new NotSupportedException();""");
            }
            else
            {
                resultBuilder.AppendLine($"""    public string {api.MethodName} => $"{api.Oversea}";""");
            }
        }

        return resultBuilder.ToString();
    }

    static string[] ParseCsvLine(string line)
    {
        List<string> fields = [];
        StringBuilder currentField = new();
        bool insideQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char currentChar = line[i];

            if (currentChar == '"')
            {
                if (insideQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
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

        return [.. fields];
    }

    private sealed class ApiEndpointsMetadata
    {
        public string MethodName { get; set; } = default!;

        public string? Chinese { get; set; }

        public string? Oversea { get; set; }
    }
}