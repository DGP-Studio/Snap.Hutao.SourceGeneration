using Microsoft.CodeAnalysis;
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

        string endpointsText = csvFile.GetText(context.CancellationToken)!.ToString();

        List<ApiEndpointsMetadata> apis = [];
        using (StringReader reader = new(csvFile.GetText(context.CancellationToken)!.ToString()))
        {
            while (reader.ReadLine() is { Length: > 0 } line)
            {
                if (line is "Name,CN,OS")
                {
                    continue;
                }

                string[] parts = line.Split(',');
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
            namespace Snap.Hutao.Web.Endpoint.Hoyolab;

            internal interface IApiEndpoints
            {
            {{FillWithInterfaceMethods(apis)}}
            }

            internal abstract class ApiEndpointsImplmentationForChinese
            {
            {{FillWithChineseMethods(apis)}}
            }

            internal abstract class ApiEndpointsImplmentationForOversea
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
            resultBuilder.AppendLine($@"    string {metadata.MethodName};");
        }

        return resultBuilder.ToString();
    }

    private static string FillWithChineseMethods(List<ApiEndpointsMetadata> apis)
    {
        StringBuilder resultBuilder = new();

        foreach (ApiEndpointsMetadata api in apis)
        {
            if (string.IsNullOrEmpty(api.Chinese))
            {
                resultBuilder.AppendLine($"""    public string {api.MethodName} => throw new NotSuppportedException();""");
            }
            else
            {
                resultBuilder.AppendLine($"""    public string {api.MethodName} => {api.Chinese};""");
            }
        }

        return resultBuilder.ToString();
    }

    private static string FillWithOverseaMethods(List<ApiEndpointsMetadata> apis)
    {
        StringBuilder resultBuilder = new();

        foreach (ApiEndpointsMetadata api in apis)
        {
            if (string.IsNullOrEmpty(api.Chinese))
            {
                resultBuilder.AppendLine($"""    public string {api.MethodName} => throw new NotSuppportedException();""");
            }
            else
            {
                resultBuilder.AppendLine($"""    public string {api.MethodName} => {api.Oversea};""");
            }
        }

        return resultBuilder.ToString();
    }

    private sealed class ApiEndpointsMetadata
    {
        public string MethodName { get; set; } = default!;

        public string? Chinese { get; set; }

        public string? Oversea { get; set; }
    }
}