// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snap.Hutao.SourceGeneration.Extension;
using Snap.Hutao.SourceGeneration.Primitive;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.FastSyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.SyntaxKeywords;

namespace Snap.Hutao.SourceGeneration.Automation;

[Generator(LanguageNames.CSharp)]
internal sealed class UnsafePropertyBackingFieldAccessorGenerator : IIncrementalGenerator
{
    private static readonly NameSyntax NameOfUnsafeAccessor = ParseName("global::System.Runtime.CompilerServices.UnsafeAccessor");
    private static readonly NameSyntax NameOfUnsafeAccessorKind = ParseName("global::System.Runtime.CompilerServices.UnsafeAccessorKind");

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<ImmutableArray<GeneratorAttributeSyntaxContext>> provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                WellKnownMetadataNames.FieldAccessAttribute,
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
            production.AddSource($"Error-{Guid.NewGuid().ToString()}.g.cs", e.ToString());
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

        ImmutableDictionary<IPropertySymbol, string> propertyBackingFieldNames = GetPropertyBackingFieldNames(containingTypeSymbol, contexts, production.CancellationToken);
        ImmutableArray<MethodDeclarationSyntax> accessMethods = GenerateAccessMethods(contexts, propertyBackingFieldNames, containingTypeSymbol, production.CancellationToken);

        if (accessMethods.Length <= 0)
        {
            return;
        }

        CompilationUnitSyntax syntax = CompilationUnit()
            .WithMembers(SingletonList<MemberDeclarationSyntax>(FileScopedNamespaceDeclaration(containingTypeSymbol.ContainingNamespace)
                .WithLeadingTrivia(NullableEnableTriviaList)
                .WithMembers(SingletonList<MemberDeclarationSyntax>(
                    PartialTypeDeclaration(containingTypeSymbol)
                        .WithMembers(List<MemberDeclarationSyntax>(accessMethods))))))
            .NormalizeWhitespace();

        production.AddSource($"{containingTypeSymbol.NormalizedFullyQualifiedName()}.g.cs", syntax.ToFullString());
    }

    private static ImmutableDictionary<IPropertySymbol, string> GetPropertyBackingFieldNames(INamedTypeSymbol containingTypeSymbol, ImmutableArray<GeneratorAttributeSyntaxContext> contexts, CancellationToken token)
    {
        ImmutableHashSet<IPropertySymbol> properties = contexts
            .Select(context => context.TargetSymbol)
            .OfType<IPropertySymbol>()
            .ToImmutableHashSet<IPropertySymbol>(SymbolEqualityComparer.Default);

        token.ThrowIfCancellationRequested();
        ImmutableDictionary<IPropertySymbol, string>.Builder builder = ImmutableDictionary.CreateBuilder<IPropertySymbol, string>(SymbolEqualityComparer.Default);
        foreach (IFieldSymbol fieldSymbol in containingTypeSymbol.GetMembers().OfType<IFieldSymbol>())
        {
            token.ThrowIfCancellationRequested();
            if (fieldSymbol.AssociatedSymbol is IPropertySymbol associatedProperty && properties.Contains(associatedProperty))
            {
                builder[associatedProperty] = fieldSymbol.Name;
            }
        }

        token.ThrowIfCancellationRequested();
        return builder.ToImmutable();
    }

    private static ImmutableArray<MethodDeclarationSyntax> GenerateAccessMethods(
        ImmutableArray<GeneratorAttributeSyntaxContext> contexts,
        ImmutableDictionary<IPropertySymbol, string> propertyBackingFieldNames,
        INamedTypeSymbol containingTypeSymbol,
        CancellationToken token)
    {
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

            TypeSyntax propertyType = ParseTypeName(propertySymbol.Type.GetFullyQualifiedName());
            RefTypeSyntax refPropertyType = RefType(propertyType);
            if (readOnly)
            {
                refPropertyType = refPropertyType.WithReadOnlyKeyword(ReadOnlyKeyword);
            }

            MethodDeclarationSyntax method = MethodDeclaration(refPropertyType, Identifier($"FieldRefOf{propertySymbol.Name}"))
                .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(
                    GenerateUnsafeAccessorAttribute(fieldName)))))
                .WithModifiers(PrivateStaticExternTokenList)
                .WithParameterList(ParameterList(SingletonSeparatedList(
                    Parameter(ParseTypeName(containingTypeSymbol.GetFullyQualifiedNameWithNullabilityAnnotations()), Identifier("self")))))
                .WithSemicolonToken(SemicolonToken);

            token.ThrowIfCancellationRequested();
            accessMethodsBuilder.Add(method);
        }

        token.ThrowIfCancellationRequested();
        return accessMethodsBuilder.ToImmutable();
    }

    private static AttributeSyntax GenerateUnsafeAccessorAttribute(string fieldName)
    {
        return Attribute(NameOfUnsafeAccessor)
            .WithArgumentList(AttributeArgumentList(SeparatedList<AttributeArgumentSyntax>(
                [
                    AttributeArgument(SimpleMemberAccessExpression(NameOfUnsafeAccessorKind, IdentifierName("Field"))),
                    AttributeArgument(StringLiteralExpression(fieldName)).WithNameEquals(NameEquals("Name")),
                ])));
    }
}