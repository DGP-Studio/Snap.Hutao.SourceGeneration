// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snap.Hutao.SourceGeneration.Extension;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.FastSyntaxFactory;

namespace Snap.Hutao.SourceGeneration.Automation;

[Generator(LanguageNames.CSharp)]
internal sealed class SaltConstantGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(Generate);
    }

    private static void Generate(IncrementalGeneratorPostInitializationContext context)
    {
        Response<SaltLatest>? saltInfo;
        try
        {
            const string Url = "https://internal.snapgenshin.com/Archive/Salt/Latest";
            string body = new HttpClient().GetStringAsync(Url).GetAwaiter().GetResult();
            saltInfo = JsonSerializer.Deserialize<Response<SaltLatest>>(body);
            if (saltInfo is null)
            {
                throw new ArgumentNullException(nameof(saltInfo));
            }
        }
        catch (Exception ex)
        {
            context.AddSource("Error.g.cs", ex.ToString());
            return;
        }

        CompilationUnitSyntax syntax = CompilationUnit()
            .WithMembers(SingletonList<MemberDeclarationSyntax>(FileScopedNamespaceDeclaration("Snap.Hutao.Web.Hoyolab")
                .WithLeadingTrivia(NullableEnableTriviaList)
                .WithMembers(SingletonList<MemberDeclarationSyntax>(
                    ClassDeclaration("SaltConstants")
                        .WithModifiers(InternalStaticTokenList)
                        .WithMembers(List<MemberDeclarationSyntax>(
                        [
                            ConstStringFieldDeclaration("CNVersion", saltInfo.Data.CNVersion),
                            ConstStringFieldDeclaration("CNK2", saltInfo.Data.CNK2),
                            ConstStringFieldDeclaration("CNLK2", saltInfo.Data.CNLK2),
                            ConstStringFieldDeclaration("OSVersion", saltInfo.Data.OSVersion),
                            ConstStringFieldDeclaration("OSK2", saltInfo.Data.OSK2),
                            ConstStringFieldDeclaration("OSLK2", saltInfo.Data.OSLK2)
                        ]))))))
            .NormalizeWhitespace();

        context.AddSource("SaltConstants.g.cs", syntax.ToFullStringWithHeader());
    }

    private static FieldDeclarationSyntax ConstStringFieldDeclaration(string fieldName, string fieldValue)
    {
        return FieldDeclaration(VariableDeclaration(StringType)
            .WithVariables(SingletonSeparatedList(
                VariableDeclarator(fieldName)
                    .WithInitializer(EqualsValueClause(StringLiteralExpression(fieldValue))))))
            .WithModifiers(PublicConstTokenList);
    }

    private sealed class Response<[MeansImplicitUse] T>
    {
        [JsonPropertyName("data")]
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