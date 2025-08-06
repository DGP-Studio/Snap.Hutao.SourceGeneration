// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snap.Hutao.SourceGeneration.Primitive;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.FastSyntaxFactory;

namespace Snap.Hutao.SourceGeneration.Automation;

[Generator(LanguageNames.CSharp)]
internal sealed class UnsafePropertyBackingFieldAccessorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<ImmutableArray<GeneratorAttributeSyntaxContext>> provider = context.SyntaxProvider.ForAttributeWithMetadataName(
            WellKnownAttributeNames.FieldAccessAttribute,
            SyntaxNodeHelper.Is<PropertyDeclarationSyntax>,
            SyntaxContext.Transform)
            .Collect();

        context.RegisterSourceOutput(provider, GenerateWrapper);
    }

    private static void GenerateWrapper(SourceProductionContext production, ImmutableArray<GeneratorAttributeSyntaxContext> contexts)
    {
        try
        {
            GenerateAll(production, contexts);
        }
        catch (Exception e)
        {
            production.AddSource("Error.g.cs", e.ToString());
        }
    }

    private static void GenerateAll(SourceProductionContext production, ImmutableArray<GeneratorAttributeSyntaxContext> contextArray)
    {
        IEnumerable<IGrouping<ISymbol, GeneratorAttributeSyntaxContext>> groups = contextArray
            .GroupBy(context => context.TargetSymbol.ContainingSymbol, SymbolEqualityComparer.Default);

        foreach ((ISymbol containingSymbol, IEnumerable<GeneratorAttributeSyntaxContext> contexts) in groups)
        {
            GenerateForContainingType(production, containingSymbol, [.. contexts]);
        }
    }

    private static void GenerateForContainingType(SourceProductionContext production, ISymbol containingSymbol, ImmutableArray<GeneratorAttributeSyntaxContext> contexts)
    {
        if (containingSymbol is not INamedTypeSymbol containingTypeSymbol)
        {
            return;
        }

        ImmutableDictionary<IPropertySymbol, string> propertyBackingFieldNames = GetPropertyBackingFieldNames(containingTypeSymbol, contexts);
        ImmutableArray<MethodDeclarationSyntax>.Builder accessMethodsBuilder = ImmutableArray.CreateBuilder<MethodDeclarationSyntax>(contexts.Length);
        foreach (GeneratorAttributeSyntaxContext context in contexts)
        {
            if (context.TargetSymbol is not IPropertySymbol propertySymbol)
            {
                continue;
            }

            if (propertySymbol.RefCustomModifiers.Length > 0 || (propertySymbol.GetMethod is null && propertySymbol.SetMethod is null))
            {
                continue;
            }

            if (!propertyBackingFieldNames.TryGetValue(propertySymbol, out string? fieldName))
            {
                continue;
            }

            // { get; } or { get; init; } or { init; } => ref readonly
            // { get; set; } or { set; } => ref
            bool readOnly = propertySymbol.SetMethod is null || propertySymbol.SetMethod.IsInitOnly;

            TypeSyntax type = ParseTypeName(propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            RefTypeSyntax refType = RefType(type);
            if (readOnly)
            {
                refType = refType.WithReadOnlyKeyword(ReadOnlyKeyword);
            }

            MethodDeclarationSyntax method = MethodDeclaration(refType, Identifier($"FieldRefOf{propertySymbol.Name}"))
                .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(
                    GenerateUnsafeAccessorAttribute(fieldName)))))
                .WithModifiers(PrivateStaticExternTokenList)
                .WithParameterList(ParameterList(SingletonSeparatedList(
                    Parameter(Identifier("self")).WithType(type))))
                .WithSemicolonToken(SemicolonToken);

            accessMethodsBuilder.Add(method);
        }

        if (accessMethodsBuilder.Count <= 0)
        {
            return;
        }

        CompilationUnitSyntax syntax = CompilationUnit()
            .WithMembers(SingletonList<MemberDeclarationSyntax>(FileScopedNamespaceDeclaration(containingTypeSymbol.ContainingNamespace)
                .WithMembers(SingletonList<MemberDeclarationSyntax>(
                    PartialTypeDeclaration(containingTypeSymbol)
                        .WithMembers(List<MemberDeclarationSyntax>(accessMethodsBuilder.ToImmutable()))))));

        production.AddSource($"{containingTypeSymbol.ToDisplayString().NormalizeSymbolName()}.g.cs", syntax.NormalizeWhitespace().ToFullString());
    }

    private static ImmutableDictionary<IPropertySymbol, string> GetPropertyBackingFieldNames(INamedTypeSymbol containingTypeSymbol, ImmutableArray<GeneratorAttributeSyntaxContext> contexts)
    {
        ImmutableHashSet<IPropertySymbol> properties = contexts
            .Select(context => context.TargetSymbol)
            .OfType<IPropertySymbol>()
            .ToImmutableHashSet<IPropertySymbol>(SymbolEqualityComparer.Default);

        ImmutableDictionary<IPropertySymbol, string>.Builder builder = ImmutableDictionary.CreateBuilder<IPropertySymbol, string>(SymbolEqualityComparer.Default);
        foreach (IFieldSymbol fieldSymbol in containingTypeSymbol.GetMembers().OfType<IFieldSymbol>())
        {
            if (fieldSymbol.AssociatedSymbol is IPropertySymbol associatedProperty && properties.Contains(associatedProperty))
            {
                builder[associatedProperty] = fieldSymbol.Name;
            }
        }

        return builder.ToImmutable();
    }

    private static AttributeSyntax GenerateUnsafeAccessorAttribute(string fieldName)
    {
        return Attribute(IdentifierName("UnsafeAccessor"))
            .WithArgumentList(AttributeArgumentList(SeparatedList<AttributeArgumentSyntax>(
                [
                    AttributeArgument(SimpleMemberAccessExpression(IdentifierName("UnsafeAccessorKind"), IdentifierName("Field"))),
                    AttributeArgument(StringLiteralExpression(fieldName)).WithNameEquals(NameEquals("Name")),
                ])));
    }
}