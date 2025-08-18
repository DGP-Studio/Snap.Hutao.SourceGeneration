﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Snap.Hutao.SourceGeneration.Extension;
using Snap.Hutao.SourceGeneration.Model;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Xml.Linq;
using System.Xml.XPath;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.FastSyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.SyntaxKeywords;
using static Snap.Hutao.SourceGeneration.WellKnownSyntax;

namespace Snap.Hutao.SourceGeneration.Resx;

[Generator]
public sealed class ResxGenerator2 : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor InvalidResx = new("SH401", "Couldn't parse Resx file", "Couldn't parse Resx file '{0}'", "ResxGenerator", DiagnosticSeverity.Warning, true);
    private static readonly DiagnosticDescriptor InvalidPropertiesForNamespace = new("SH402", "Couldn't compute namespace", "Couldn't compute namespace for file '{0}'", "ResxGenerator", DiagnosticSeverity.Warning, true);
    private static readonly DiagnosticDescriptor InvalidPropertiesForResourceName = new("SH403", "Couldn't compute resource name", "Couldn't compute resource name for file '{0}'", "ResxGenerator", DiagnosticSeverity.Warning, true);
    private static readonly DiagnosticDescriptor InconsistentProperties = new("SH404", "Inconsistent properties", "Property '{0}' values for '{1}' are inconsistent", "ResxGenerator", DiagnosticSeverity.Warning, true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<string?> assemblyNameProvider = context.CompilationProvider
            .Select(static (compilation, token) => compilation.AssemblyName);

        IncrementalValuesProvider<ResxGeneratorContext> resxProvider = context.AdditionalTextsProvider
            .Where(static text => text.Path.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
            .Combine(context.AnalyzerConfigOptionsProvider.Combine(assemblyNameProvider))
            .Select(static (tuple, token) => ResxFile.Create(tuple.Left, tuple.Right.Left, tuple.Right.Right, token))
            .Where(static file => file is { ResourceName: not null, Namespace: not null, ClassName: not null })
            .GroupBy(static file => (file.Namespace!, file.ClassName!, file.ResourceName!))
            .Select(ResxGeneratorContext.Create);

        context.RegisterSourceOutput(resxProvider, GenerateWrapper);
    }

    private static void GenerateWrapper(SourceProductionContext production, ResxGeneratorContext context)
    {
        try
        {
            Generate(production, context);
        }
        catch (Exception e)
        {
            production.AddSource($"Error-{Guid.NewGuid().ToString()}.g.cs", e.ToString());
        }
    }

    private static void Generate(SourceProductionContext production, ResxGeneratorContext context)
    {
        if (context.Entries.IsEmpty)
        {
            return;
        }

        if (context.Namespace is null)
        {
            production.ReportDiagnostic(Diagnostic.Create(InvalidPropertiesForNamespace, Location.None, context.ResourceName));
            return;
        }

        if (context.ResourceName is null)
        {
            production.ReportDiagnostic(Diagnostic.Create(InvalidPropertiesForResourceName, Location.None, context.ResourceName));
            return;
        }

        CompilationUnitSyntax syntax = GenerateCompilationUnit(context);
        production.AddSource($"{context.ResourceName}.cs", syntax.ToFullString());
    }

    private static CompilationUnitSyntax GenerateCompilationUnit(ResxGeneratorContext context)
    {
        return CompilationUnit()
            .WithMembers(SingletonList<MemberDeclarationSyntax>(FileScopedNamespaceDeclaration(context.Namespace!)
                .WithMembers(SingletonList<MemberDeclarationSyntax>(
                    ClassDeclaration(context.ClassName!)
                        .WithModifiers(InternalAbstractPartialTokenList)
                        .WithMembers(List<MemberDeclarationSyntax>(
                        [
                            .. GenerateSharedMemberDeclarations(context),
                            .. GenerateEntryMemberDeclarations(context)
                        ]))))))
            .NormalizeWhitespace();
    }

    private static IEnumerable<MemberDeclarationSyntax> GenerateSharedMemberDeclarations(ResxGeneratorContext context)
    {
        // [field: global::System.Diagnostics.CodeAnalysis.MaybeNull]
        // [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        // public static global::System.Resources.ResourceManager ResourceManager
        // {
        //     get => field ??= new("${namespace}", typeof(${className}).Assembly);
        // }
        yield return PropertyDeclaration(TypeOfSystemResourcesResourceManager, Identifier("ResourceManager"))
            .WithAttributeLists(List<AttributeListSyntax>(
            [
                AttributeList(SingletonSeparatedList(Attribute(NameOfSystemDiagnosticsCodeAnalysisMaybeNull)))
                    .WithTarget(AttributeTargetSpecifier(FieldKeyword)),
                AttributeList(SingletonSeparatedList(Attribute(NameOfSystemComponentModelEditorBrowsable)
                    .WithArgumentList(AttributeArgumentList(SingletonSeparatedList(
                        AttributeArgument(SimpleMemberAccessExpression(
                            NameOfSystemComponentModelEditorBrowsableState,
                            IdentifierName("Advanced"))))))))
            ]))
            .WithModifiers(PublicStaticTokenList)
            .WithAccessorList(AccessorList(SingletonList(
                GetAccessorDeclaration()
                    .WithExpressionBody(ArrowExpressionClause(CoalesceAssignmentExpression(
                        FieldExpression(),
                        ImplicitObjectCreationExpression()
                            .WithArgumentList(ArgumentList(SeparatedList(
                            [
                                Argument(StringLiteralExpression(context.ResourceName!)),
                                Argument(SimpleMemberAccessExpression(
                                    TypeOfExpression(IdentifierName(context.ClassName!)),
                                    IdentifierName("Assembly"))),
                            ])))))))));

        // [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        // public static global::System.Globalization.CultureInfo? Culture { get; set; }
        yield return PropertyDeclaration(NullableType(TypeOfSystemGlobalizationCultureInfo), Identifier("Culture"))
            .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(
                Attribute(NameOfSystemComponentModelEditorBrowsable)
                    .WithArgumentList(AttributeArgumentList(SingletonSeparatedList(
                        AttributeArgument(SimpleMemberAccessExpression(
                            NameOfSystemComponentModelEditorBrowsableState,
                            IdentifierName("Advanced"))))))))))
            .WithModifiers(PublicStaticTokenList)
            .WithAccessorList(GetAndSetAccessorList);

        // [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(defaultValue))]
        // public static object? GetObject(global::System.Globalization.CultureInfo? culture, string name, object? defaultValue)
        // {
        //     return ResourceManager.GetObject(name, culture ?? Culture) ?? defaultValue;
        // }
        yield return MethodDeclaration(NullableObjectType, Identifier("GetObject"))
            .WithAttributeLists(SingletonList(
                ReturnNotNullIfNotNullAttributeList("defaultValue")))
            .WithModifiers(PublicStaticTokenList)
            .WithParameterList(ParameterList(SeparatedList(
            [
                Parameter(NullableType(TypeOfSystemGlobalizationCultureInfo), Identifier("culture")),
                Parameter(StringType, Identifier("name")),
                Parameter(NullableObjectType, Identifier("defaultValue"))
            ])))
            .WithBody(Block(SingletonList(
                ReturnStatement(CoalesceExpression(
                    InvocationExpression(SimpleMemberAccessExpression(
                            IdentifierName("ResourceManager"),
                            IdentifierName("GetObject")))
                        .WithArgumentList(ArgumentList(SeparatedList(
                        [
                            Argument(IdentifierName("name")),
                            Argument(CoalesceExpression(IdentifierName("culture"), IdentifierName("Culture"))),
                        ]))),
                    IdentifierName("defaultValue"))))));

        // public static object? GetObject(global::System.Globalization.CultureInfo? culture, string name)
        // {
        //     return GetObject(culture, name, default);
        // }
        yield return MethodDeclaration(NullableObjectType, Identifier("GetObject"))
            .WithModifiers(PublicStaticTokenList)
            .WithParameterList(ParameterList(SeparatedList(
            [
                Parameter(NullableType(TypeOfSystemGlobalizationCultureInfo), Identifier("culture")),
                Parameter(StringType, Identifier("name"))
            ])))
            .WithBody(Block(SingletonList(
                ReturnStatement(InvocationExpression(IdentifierName("GetObject"))
                    .WithArgumentList(ArgumentList(SeparatedList(
                    [
                        Argument(IdentifierName("culture")),
                        Argument(IdentifierName("name")),
                        Argument(DefaultLiteralExpression),
                    ])))))));

        // public static object? GetObject(string name)
        // {
        //     return GetObject(default, name, default);
        // }
        yield return MethodDeclaration(NullableObjectType, Identifier("GetObject"))
            .WithModifiers(PublicStaticTokenList)
            .WithParameterList(ParameterList(SingletonSeparatedList(
                Parameter(StringType, Identifier("name")))))
            .WithBody(Block(SingletonList(
                ReturnStatement(InvocationExpression(IdentifierName("GetObject"))
                    .WithArgumentList(ArgumentList(SeparatedList(
                    [
                        Argument(DefaultLiteralExpression),
                        Argument(IdentifierName("name")),
                        Argument(DefaultLiteralExpression),
                    ])))))));

        // [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(defaultValue))]
        // public static object? GetObject(string name, object? defaultValue)
        // {
        //     return GetObject(default, name, defaultValue);
        // }
        yield return MethodDeclaration(NullableObjectType, Identifier("GetObject"))
            .WithAttributeLists(SingletonList(
                ReturnNotNullIfNotNullAttributeList("defaultValue")))
            .WithModifiers(PublicStaticTokenList)
            .WithParameterList(ParameterList(SeparatedList(
            [
                Parameter(StringType, Identifier("name")),
                Parameter(NullableObjectType, Identifier("defaultValue"))
            ])))
            .WithBody(Block(SingletonList(
                ReturnStatement(InvocationExpression(IdentifierName("GetObject"))
                    .WithArgumentList(ArgumentList(SeparatedList(
                    [
                        Argument(DefaultLiteralExpression),
                        Argument(IdentifierName("name")),
                        Argument(IdentifierName("defaultValue")),
                    ])))))));

        // public static global::System.IO.Stream? GetStream(global::System.Globalization.CultureInfo? culture, string name)
        // {
        //     return ResourceManager.GetStream(name, culture ?? Culture);
        // }
        yield return MethodDeclaration(NullableType(TypeOfSystemIOStream), Identifier("GetStream"))
            .WithModifiers(PublicStaticTokenList)
            .WithParameterList(ParameterList(SeparatedList(
            [
                Parameter(NullableType(TypeOfSystemGlobalizationCultureInfo), Identifier("culture")),
                Parameter(StringType, Identifier("name"))
            ])))
            .WithBody(Block(SingletonList(
                ReturnStatement(InvocationExpression(SimpleMemberAccessExpression(
                        IdentifierName("ResourceManager"),
                        IdentifierName("GetStream")))
                    .WithArgumentList(ArgumentList(SeparatedList(
                    [
                        Argument(IdentifierName("name")),
                        Argument(CoalesceExpression(IdentifierName("culture"), IdentifierName("Culture"))),
                    ])))))));

        // public static global::System.IO.Stream? GetStream(string name)
        // {
        //     return GetStream(default, name);
        // }
        yield return MethodDeclaration(NullableType(TypeOfSystemIOStream), Identifier("GetStream"))
            .WithModifiers(PublicStaticTokenList)
            .WithParameterList(ParameterList(SingletonSeparatedList(
                Parameter(StringType, Identifier("name")))))
            .WithBody(Block(SingletonList(
                ReturnStatement(InvocationExpression(IdentifierName("GetStream"))
                    .WithArgumentList(ArgumentList(SeparatedList(
                    [
                        Argument(DefaultLiteralExpression),
                        Argument(IdentifierName("name")),
                    ])))))));

        // public static string? GetString(global::System.Globalization.CultureInfo? culture, string name, params object?[]? args)
        // {
        //     culture ??= Culture;
        //     string? str = ResourceManager.GetString(name, culture);
        //     if (str is null)
        //     {
        //         return default;
        //     }
        //
        //     if (args is null)
        //     {
        //         return str;
        //     }
        //
        //     return string.Format(culture, str, args);
        // }
        yield return MethodDeclaration(NullableStringType, Identifier("GetString"))
            .WithModifiers(PublicStaticTokenList)
            .WithParameterList(ParameterList(SeparatedList(
            [
                Parameter(NullableType(TypeOfSystemGlobalizationCultureInfo), Identifier("culture")),
                Parameter(StringType, Identifier("name")),
                NullableArrayOfNullableObjectTypeParameter("args")
            ])))
            .WithBody(Block(List<StatementSyntax>(
            [
                ExpressionStatement(CoalesceAssignmentExpression(
                    IdentifierName("culture"),
                    IdentifierName("Culture"))),
                LocalDeclarationStatement(VariableDeclaration(NullableStringType)
                    .WithVariables(SingletonSeparatedList(
                        VariableDeclarator(Identifier("str"))
                            .WithInitializer(EqualsValueClause(
                                InvocationExpression(SimpleMemberAccessExpression(
                                        IdentifierName("ResourceManager"),
                                        IdentifierName("GetString")))
                                    .WithArgumentList(ArgumentList(SeparatedList(
                                    [
                                        Argument(IdentifierName("name")),
                                        Argument(IdentifierName("culture")),
                                    ])))))))),
                IfStatement(
                    IsPatternExpression(IdentifierName("str"), ConstantPattern(NullLiteralExpression)),
                    Block(SingletonList(ReturnStatement(DefaultLiteralExpression)))),
                IfStatement(
                    IsPatternExpression(IdentifierName("args"), ConstantPattern(NullLiteralExpression)),
                    Block(SingletonList(ReturnStatement(IdentifierName("str"))))),
                ReturnStatement(
                    InvocationExpression(SimpleMemberAccessExpression(
                            StringType,
                            IdentifierName("Format")))
                        .WithArgumentList(ArgumentList(SeparatedList(
                        [
                            Argument(IdentifierName("culture")),
                            Argument(IdentifierName("str")),
                            Argument(IdentifierName("args"))
                        ]))))
            ])));

        // public static string? GetString(global::System.Globalization.CultureInfo? culture, string name)
        // {
        //     return GetString(culture, name, default);
        // }
        yield return MethodDeclaration(NullableStringType, Identifier("GetString"))
            .WithModifiers(PublicStaticTokenList)
            .WithParameterList(ParameterList(SeparatedList(
            [
                Parameter(NullableType(TypeOfSystemGlobalizationCultureInfo), Identifier("culture")),
                Parameter(StringType, Identifier("name"))
            ])))
            .WithBody(Block(SingletonList(
                ReturnStatement(InvocationExpression(IdentifierName("GetString"))
                    .WithArgumentList(ArgumentList(SeparatedList(
                    [
                        Argument(IdentifierName("culture")),
                        Argument(IdentifierName("name")),
                        Argument(DefaultLiteralExpression),
                    ])))))));

        // public static string? GetString(string name, params object?[]? args)
        // {
        //     return GetString(null, name, args);
        // }
        yield return MethodDeclaration(NullableStringType, Identifier("GetString"))
            .WithModifiers(PublicStaticTokenList)
            .WithParameterList(ParameterList(SeparatedList(
            [
                Parameter(StringType, Identifier("name")),
                NullableArrayOfNullableObjectTypeParameter("args")
            ])))
            .WithBody(Block(SingletonList(
                ReturnStatement(InvocationExpression(IdentifierName("GetString"))
                    .WithArgumentList(ArgumentList(SeparatedList(
                    [
                        Argument(DefaultLiteralExpression),
                        Argument(IdentifierName("name")),
                        Argument(IdentifierName("args"))
                    ])))))));

        // [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(defaultValue))]
        // public static string? GetStringOrDefault(global::System.Globalization.CultureInfo? culture, string name, string? defaultValue, params object?[]? args)
        // {
        //     culture ??= Culture;
        //     string? str = ResourceManager.GetString(name, culture) ?? defaultValue;
        //     if (str is null)
        //     {
        //         return null;
        //     }
        //
        //     if (args is null)
        //     {
        //         return str;
        //     }
        //
        //     return string.Format(culture, str, args);
        // }
        yield return MethodDeclaration(NullableStringType, Identifier("GetStringOrDefault"))
            .WithAttributeLists(SingletonList(
                ReturnNotNullIfNotNullAttributeList("defaultValue")))
            .WithModifiers(PublicStaticTokenList)
            .WithParameterList(ParameterList(SeparatedList(
            [
                Parameter(NullableType(TypeOfSystemGlobalizationCultureInfo), Identifier("culture")),
                Parameter(StringType, Identifier("name")),
                Parameter(NullableStringType, Identifier("defaultValue")),
                NullableArrayOfNullableObjectTypeParameter("args")
            ])))
            .WithBody(Block(List<StatementSyntax>(
            [
                ExpressionStatement(CoalesceAssignmentExpression(
                    IdentifierName("culture"),
                    IdentifierName("Culture"))),
                LocalDeclarationStatement(VariableDeclaration(NullableStringType)
                    .WithVariables(SingletonSeparatedList(
                        VariableDeclarator(Identifier("str"))
                            .WithInitializer(EqualsValueClause(CoalesceExpression(
                                InvocationExpression(SimpleMemberAccessExpression(
                                        IdentifierName("ResourceManager"),
                                        IdentifierName("GetString")))
                                    .WithArgumentList(ArgumentList(SeparatedList(
                                    [
                                        Argument(IdentifierName("name")),
                                        Argument(IdentifierName("culture")),
                                    ]))),
                                IdentifierName("defaultValue"))))))),
                IfStatement(
                    IsPatternExpression(IdentifierName("str"), ConstantPattern(NullLiteralExpression)),
                    Block(SingletonList(ReturnStatement(DefaultLiteralExpression)))),
                IfStatement(
                    IsPatternExpression(IdentifierName("args"), ConstantPattern(NullLiteralExpression)),
                    Block(SingletonList(ReturnStatement(IdentifierName("str"))))),
                ReturnStatement(
                    InvocationExpression(SimpleMemberAccessExpression(
                            StringType,
                            IdentifierName("Format")))
                        .WithArgumentList(ArgumentList(SeparatedList(
                        [
                            Argument(IdentifierName("culture")),
                            Argument(IdentifierName("str")),
                            Argument(IdentifierName("args"))
                        ]))))
            ])));

        // [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(defaultValue))]
        // public static string? GetStringOrDefault(global::System.Globalization.CultureInfo? culture, string name, string? defaultValue)
        // {
        //     return GetStringOrDefault(culture, name, defaultValue, default);
        // }
        yield return MethodDeclaration(NullableStringType, Identifier("GetStringOrDefault"))
            .WithAttributeLists(SingletonList(
                ReturnNotNullIfNotNullAttributeList("defaultValue")))
            .WithModifiers(PublicStaticTokenList)
            .WithParameterList(ParameterList(SeparatedList(
            [
                Parameter(NullableType(TypeOfSystemGlobalizationCultureInfo), Identifier("culture")),
                Parameter(StringType, Identifier("name")),
                Parameter(NullableStringType, Identifier("defaultValue"))
            ])))
            .WithBody(Block(SingletonList(
                ReturnStatement(InvocationExpression(IdentifierName("GetStringOrDefault"))
                    .WithArgumentList(ArgumentList(SeparatedList(
                    [
                        Argument(IdentifierName("culture")),
                        Argument(IdentifierName("name")),
                        Argument(IdentifierName("defaultValue")),
                        Argument(DefaultLiteralExpression)
                    ])))))));

        // [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(defaultValue))]
        // public static string? GetStringOrDefault(string name, string? defaultValue, params object?[]? args)
        // {
        //     return GetStringOrDefault(null, name, defaultValue, args);
        // }
        yield return MethodDeclaration(NullableStringType, Identifier("GetStringOrDefault"))
            .WithAttributeLists(SingletonList(
                ReturnNotNullIfNotNullAttributeList("defaultValue")))
            .WithModifiers(PublicStaticTokenList)
            .WithParameterList(ParameterList(SeparatedList(
            [
                Parameter(StringType, Identifier("name")),
                Parameter(NullableStringType, Identifier("defaultValue")),
                NullableArrayOfNullableObjectTypeParameter("args")
            ])))
            .WithBody(Block(SingletonList(
                ReturnStatement(InvocationExpression(IdentifierName("GetStringOrDefault"))
                    .WithArgumentList(ArgumentList(SeparatedList(
                    [
                        Argument(DefaultLiteralExpression),
                        Argument(IdentifierName("name")),
                        Argument(IdentifierName("defaultValue")),
                        Argument(IdentifierName("args"))
                    ])))))));

        // [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(defaultValue))]
        // public static string? GetStringOrDefault(string name, string? defaultValue)
        // {
        //     return GetStringOrDefault(null, name, defaultValue, null);
        // }
        yield return MethodDeclaration(NullableStringType, Identifier("GetStringOrDefault"))
            .WithAttributeLists(SingletonList(
                ReturnNotNullIfNotNullAttributeList("defaultValue")))
            .WithModifiers(PublicStaticTokenList)
            .WithParameterList(ParameterList(SeparatedList(
            [
                Parameter(StringType, Identifier("name")),
                Parameter(NullableStringType, Identifier("defaultValue"))
            ])))
            .WithBody(Block(SingletonList(
                ReturnStatement(InvocationExpression(IdentifierName("GetStringOrDefault"))
                    .WithArgumentList(ArgumentList(SeparatedList(
                    [
                        Argument(DefaultLiteralExpression),
                        Argument(IdentifierName("name")),
                        Argument(IdentifierName("defaultValue")),
                        Argument(DefaultLiteralExpression)
                    ])))))));
    }

    private static IEnumerable<MemberDeclarationSyntax> GenerateEntryMemberDeclarations(ResxGeneratorContext context)
    {
        foreach (ResxEntry entry in context.Entries)
        {
            XElement summary = new("summary", new XElement("para", $"Looks up a localized string for \"{entry.Name}\"."));
            if (!string.IsNullOrWhiteSpace(entry.Comment))
            {
                summary.Add(new XElement("para", entry.Comment));
            }

            foreach((string locale, string? each) in entry.Values)
            {
                summary.Add(new XElement("code", $"{locale,-8} Value: \"{each}\""));
            }

            SyntaxTriviaList comment = ParseLeadingTrivia(new StringBuilder(summary.ToString()).Replace("\r\n", "\r\n    /// ").ToString());

            yield return PropertyDeclaration(StringType, entry.Name)
                .WithLeadingTrivia(comment)
                .WithModifiers(PublicStaticTokenList)
                .WithExpressionBody(ArrowExpressionClause(InvocationExpression(IdentifierName("GetString"))
                    .WithArgumentList(ArgumentList(SingletonSeparatedList(
                        Argument(StringLiteralExpression(entry.Name)))))));
        }
    }

    private static AttributeListSyntax ReturnNotNullIfNotNullAttributeList(string parameterName)
    {
        return AttributeList(SingletonSeparatedList(
            Attribute(NameOfSystemDiagnosticsCodeAnalysisNotNullIfNotNull)
                .WithArgumentList(AttributeArgumentList(SingletonSeparatedList(
                    AttributeArgument(NameOfExpression(IdentifierName(parameterName))))))))
            .WithTarget(AttributeTargetSpecifier(Token(SyntaxKind.ReturnKeyword)));
    }

    private static ParameterSyntax NullableArrayOfNullableObjectTypeParameter(string name)
    {
        return Parameter(Identifier(name))
            .WithType(NullableType(ArrayType(NullableObjectType)
                .WithRankSpecifiers(SingletonList(
                    ArrayRankSpecifier(SingletonSeparatedList<ExpressionSyntax>(
                        OmittedArraySizeExpression()))))));
    }

    private sealed record ResxGeneratorContext
    {
        public required string? Namespace { get; init; }

        public required string? ClassName { get; init; }

        public required string? ResourceName { get; init; }

        public required EquatableArray<ResxEntry> Entries { get; init; }

        public static ResxGeneratorContext Create(((string Namespace, string ClassName, string ResourceName) Key, EquatableArray<ResxFile> Entries) source, CancellationToken token)
        {
            List<ResxEntry.Builder> entryBuilders = [];
            foreach (ResxFile? file in source.Entries)
            {
                foreach (ResxData? data in file.DataArray)
                {
                    if (entryBuilders.Find(entry => entry.Name == data.Name) is not { } existingEntry)
                    {
                        existingEntry = ResxEntry.CreateBuilder(data.Name, data.Type, data.Comment);
                        entryBuilders.Add(existingEntry);
                    }

                    existingEntry.Add(file.Locale, data.Value);
                }
            }

            entryBuilders.Sort(static (x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));

            return new()
            {
                Namespace = source.Key.Namespace,
                ClassName = source.Key.ClassName,
                ResourceName = source.Key.ResourceName,
                Entries = entryBuilders.Select(builder => builder.ToEntry()).ToImmutableArray(),
            };
        }
    }

    private sealed record ResxEntry
    {
        public required string Name { get; init; }

        public string? Type { get; init; }

        public string? Comment { get; init; }

        [JsonConverter(typeof(TupleArrayConverter))]
        public required EquatableArray<(string Locale, string? Value)> Values { get; init; }

        public static Builder CreateBuilder(string name, string? type = null, string? comment = null)
        {
            return new()
            {
                Name = name,
                Type = type,
                Comment = comment,
            };
        }

        public sealed class Builder
        {
            private readonly List<(string Locale, string? Value)> values = [];

            public required string Name { get; init; }

            public string? Type { get; init; }

            public string? Comment { get; init; }

            public void Add(string locale, string? value)
            {
                values.Add((locale, value));
            }

            public ResxEntry ToEntry()
            {
                values.Sort(static (x, y) =>
                {
                    if (string.Equals(x.Locale, y.Locale, StringComparison.Ordinal))
                    {
                        return 0;
                    }

                    if (x.Locale == "Neutral")
                    {
                        return -1;
                    }

                    if (y.Locale == "Neutral")
                    {
                        return 1;
                    }

                    return string.Compare(x.Locale, y.Locale, StringComparison.Ordinal);
                });

                return new()
                {
                    Name = Name,
                    Type = Type,
                    Comment = Comment,
                    Values = values.ToImmutableArray(),
                };
            }
        }
    }

    private sealed record ResxFile
    {
        public required string ResourcePath { get; init; }

        public required string Locale { get; init; }

        public required EquatableArray<ResxData> DataArray { get; init; }

        public required string? Namespace { get; init; }

        public required string? ClassName { get; init; }

        public required string? ResourceName { get; init; }

        public static ResxFile Create(AdditionalText text, AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider, string? assemblyName, CancellationToken token)
        {
            SourceText? content = text.GetText(token);
            if (content is null)
            {
                return default!;
            }

            ImmutableArray<ResxData>.Builder resxDataBuilder = ImmutableArray.CreateBuilder<ResxData>();

            try
            {
                XDocument document = XDocument.Parse(content.ToString());
                foreach (XElement? element in document.XPathSelectElements("/root/data"))
                {
                    string? name = element.Attribute("name")?.Value;
                    string? type = element.Attribute("type")?.Value;
                    string? comment = element.Element("comment")?.Value;
                    string? value = element.Element("value")?.Value;

                    if (name is not null)
                    {
                        ResxData resxData = new()
                        {
                            Name = name,
                            Type = type,
                            Comment = comment,
                            Value = value,
                        };

                        resxDataBuilder.Add(resxData);
                    }
                }
            }
            catch
            {
                return default!;
            }

            string resourcePath = GetResourcePath(text.Path);

            string? metadataRootNamespace = GetMetadataValue(text, analyzerConfigOptionsProvider, "RootNamespace", "RootNamespace");
            string? metadataProjectDir = GetMetadataValue(text, analyzerConfigOptionsProvider, "ProjectDir", "ProjectDir");
            string? metadataNamespace = GetMetadataValue(text, analyzerConfigOptionsProvider, "Namespace", "DefaultResourcesNamespace");
            string? metadataResourceName = GetMetadataValue(text, analyzerConfigOptionsProvider, "ResourceName", null);
            string? metadataClassName = GetMetadataValue(text, analyzerConfigOptionsProvider, "ClassName", null);

            string? rootNamespace = metadataRootNamespace ?? assemblyName ?? string.Empty;
            string? projectDir = metadataProjectDir ?? assemblyName ?? string.Empty;
            string? defaultResourceName = ComputeResourceName(rootNamespace, projectDir, resourcePath);
            string? defaultNamespace = ComputeNamespace(rootNamespace, projectDir, resourcePath);

            return new()
            {
                ResourcePath = resourcePath,
                Locale = GetLocaleName(text.Path),
                DataArray = resxDataBuilder.ToImmutable(),
                Namespace = metadataNamespace ?? defaultNamespace,
                ResourceName = metadataResourceName ?? defaultResourceName,
                ClassName = metadataClassName ?? ToCSharpNameIdentifier(Path.GetFileName(resourcePath))
            };
        }

        private static string GetResourcePath(string path)
        {
            string pathWithoutExtension = Path.Combine(Path.GetDirectoryName(path)!, Path.GetFileNameWithoutExtension(path));
            int indexOf = pathWithoutExtension.LastIndexOf('.');
            if (indexOf < 0)
            {
                return pathWithoutExtension;
            }

            try
            {
                _ = CultureInfo.GetCultureInfo(pathWithoutExtension[(indexOf + 1)..]);
                return pathWithoutExtension[..indexOf];
            }
            catch
            {
                return pathWithoutExtension;
            }
        }

        private static string GetLocaleName(string path)
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            int indexOf = fileName.LastIndexOf('.');
            return indexOf < 0 ? "Neutral" : fileName[(indexOf + 1)..];
        }

        private static string? GetMetadataValue(AdditionalText file, AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider, string name, string? globalName)
        {
            string? result = null;
            if (analyzerConfigOptionsProvider.GetOptions(file).TryGetValue($"build_metadata.AdditionalFiles.{name}", out string? value))
            {
                if (value != result)
                {
                    return default;
                }

                result = value;
            }

            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }

            if (globalName is not null && analyzerConfigOptionsProvider.GlobalOptions.TryGetValue($"build_property.{globalName}", out string? globalValue) && !string.IsNullOrEmpty(globalValue))
            {
                return globalValue;
            }

            return default;
        }

        private static string? ComputeResourceName(string rootNamespace, string projectDir, string resourcePath)
        {
            string fullProjectDir = EnsureEndSeparator(Path.GetFullPath(projectDir));
            string fullResourcePath = Path.GetFullPath(resourcePath);

            if (fullProjectDir == fullResourcePath)
            {
                return rootNamespace;
            }

            if (fullResourcePath.StartsWith(fullProjectDir, StringComparison.Ordinal))
            {
                string relativePath = fullResourcePath[fullProjectDir.Length..];
                return rootNamespace + '.' + relativePath.Replace('/', '.').Replace('\\', '.');
            }

            return null;
        }

        private static string? ComputeNamespace(string rootNamespace, string projectDir, string resourcePath)
        {
            string fullProjectDir = EnsureEndSeparator(Path.GetFullPath(projectDir));
            string fullResourcePath = EnsureEndSeparator(Path.GetDirectoryName(Path.GetFullPath(resourcePath))!);

            if (fullProjectDir == fullResourcePath)
            {
                return rootNamespace;
            }

            if (fullResourcePath.StartsWith(fullProjectDir, StringComparison.Ordinal))
            {
                string relativePath = fullResourcePath.Substring(fullProjectDir.Length);
                return rootNamespace + '.' + relativePath.Replace('/', '.').Replace('\\', '.').TrimEnd('.');
            }

            return null;
        }

        private static string ToCSharpNameIdentifier(string name)
        {
            // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/lexical-structure#identifiers
            // https://docs.microsoft.com/en-us/dotnet/api/system.globalization.unicodecategory?view=net-5.0
            StringBuilder sb = new();
            foreach (char c in name)
            {
                UnicodeCategory category = char.GetUnicodeCategory(c);
                switch (category)
                {
                    case UnicodeCategory.UppercaseLetter:
                    case UnicodeCategory.LowercaseLetter:
                    case UnicodeCategory.TitlecaseLetter:
                    case UnicodeCategory.ModifierLetter:
                    case UnicodeCategory.OtherLetter:
                    case UnicodeCategory.LetterNumber:
                        sb.Append(c);
                        break;

                    case UnicodeCategory.DecimalDigitNumber:
                    case UnicodeCategory.ConnectorPunctuation:
                    case UnicodeCategory.Format:
                        if (sb.Length == 0)
                        {
                            sb.Append('_');
                        }
                        sb.Append(c);
                        break;

                    default:
                        sb.Append('_');
                        break;
                }
            }

            return sb.ToString();
        }

        private static string EnsureEndSeparator(string path)
        {
            if (path[^1] == Path.DirectorySeparatorChar)
            {
                return path;
            }

            return path + Path.DirectorySeparatorChar;
        }
    }

    private sealed record ResxData
    {
        public required string Name { get; init; }

        public string? Type { get; init; }

        public string? Value { get; init; }

        public string? Comment { get; init; }
    }

    private sealed class TupleArrayConverter : JsonConverter<EquatableArray<(string Locale, string? Value)>>
    {
        public override EquatableArray<(string Locale, string? Value)> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, EquatableArray<(string Locale, string? Value)> values, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach ((string locale, string? value) in values)
            {
                writer.WritePropertyName(locale);
                if (value is not null)
                {
                    writer.WriteStringValue(value);
                }
                else
                {
                    writer.WriteNullValue();
                }
            }
            writer.WriteEndObject();
        }
    }
}