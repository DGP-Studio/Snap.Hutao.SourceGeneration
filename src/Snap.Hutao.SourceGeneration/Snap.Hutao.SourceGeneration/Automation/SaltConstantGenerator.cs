// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using System;
using System.Net.Http;
using System.Text.Json;

namespace Snap.Hutao.SourceGeneration.Automation;

[Generator(LanguageNames.CSharp)]
internal sealed class SaltConstantGenerator : IIncrementalGenerator
{
    private static readonly Lazy<Response<SaltLatest>?> LazySaltInfo;

    static SaltConstantGenerator()
    {
        LazySaltInfo = new(() =>
        {
            try
            {
                string body = new HttpClient().GetStringAsync("https://internal.snapgenshin.com/Archive/Salt/Latest").GetAwaiter().GetResult();
                return JsonSerializer.Deserialize<Response<SaltLatest>>(body);
            }
            catch
            {
                return default!;
            }
        });
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(GenerateSaltConstants);
    }

    private static void GenerateSaltConstants(IncrementalGeneratorPostInitializationContext context)
    {
        Response<SaltLatest>? saltInfo = LazySaltInfo.Value;

        string code;
        if (saltInfo is null)
        {
            code = """
                namespace Snap.Hutao.Web.Hoyolab;

                [global::System.Obsolete(Info, true)]
                internal sealed class SaltConstants
                {
                    private const string Info = "Failed to get the latest salt information. You may need to restart the IDE and compiler";
                
                    [global::System.Obsolete(Info, true)] public const string CNVersion = "";
                    [global::System.Obsolete(Info, true)] public const string CNK2 = "";
                    [global::System.Obsolete(Info, true)] public const string CNLK2 = "";
                
                    [global::System.Obsolete(Info, true)] public const string OSVersion = "";
                    [global::System.Obsolete(Info, true)] public const string OSK2 = "";
                    [global::System.Obsolete(Info, true)] public const string OSLK2 = "";
                }
                """;
        }
        else
        {
            code = $$"""
                namespace Snap.Hutao.Web.Hoyolab;

                internal sealed class SaltConstants
                {
                    public const string CNVersion = "{{saltInfo.Data.CNVersion}}";
                    public const string CNK2 = "{{saltInfo.Data.CNK2}}";
                    public const string CNLK2 = "{{saltInfo.Data.CNLK2}}";
                
                    public const string OSVersion = "{{saltInfo.Data.OSVersion}}";
                    public const string OSK2 = "{{saltInfo.Data.OSK2}}";
                    public const string OSLK2 = "{{saltInfo.Data.OSLK2}}";
                }
                """;
        }

        context.AddSource("SaltConstants.g.cs", code);
    }

    private sealed class Response<T>
    {
        public required T Data { get; init; }
    }

    // ReSharper disable InconsistentNaming
    internal sealed class SaltLatest
    {
        public required string CNVersion { get; init; }


        public required string CNK2 { get; init; }


        public required string CNLK2 { get; init; }

        public required string OSVersion { get; init; }

        public required string OSK2 { get; init; }

        public required string OSLK2 { get; init; }
    }
    // ReSharper restore InconsistentNaming
}