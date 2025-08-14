// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snap.Hutao.SourceGeneration.Extension;
using System.Collections.Immutable;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.FastSyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.SyntaxKeywords;

namespace Snap.Hutao.SourceGeneration.Model;

internal sealed record HierarchyInfo
{
    public HierarchyInfo(string fileNameHint, string metadataName, string @namespace, EquatableArray<TypeInfo> hierarchy)
    {
        FileNameHint = fileNameHint;
        MetadataName = metadataName;
        Namespace = @namespace;
        Hierarchy = hierarchy;
    }

    public string FileNameHint { get; }

    public string MetadataName { get; }

    public string Namespace { get; }

    public EquatableArray<TypeInfo> Hierarchy { get; }

    public static HierarchyInfo From(INamedTypeSymbol typeSymbol)
    {
        using ImmutableArrayBuilder<TypeInfo> hierarchy = ImmutableArrayBuilder<TypeInfo>.Rent();

        for (INamedTypeSymbol? parent = typeSymbol; parent is not null; parent = parent.ContainingType)
        {
            hierarchy.Add(new(parent));
        }

        return new(
            typeSymbol.GetFullyQualifiedMetadataName(),
            typeSymbol.MetadataName,
            typeSymbol.ContainingNamespace.ToDisplayString(new(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces)),
            hierarchy.ToImmutable());
    }

    public CompilationUnitSyntax GetCompilationUnit(ImmutableArray<MemberDeclarationSyntax> memberDeclarations, BaseListSyntax? baseList = null)
    {
        // Create the partial type declaration with the given member declarations.
        // This code produces a class declaration as follows:
        //
        // partial <TYPE_KIND> <TYPE_NAME>
        // {
        //     <MEMBERS>
        // }
        TypeDeclarationSyntax typeDeclarationSyntax =
            Hierarchy[0].GetSyntax()
            .AddModifiers(PartialKeyword)
            .AddMembers(memberDeclarations.ToArray());

        // Add the base list, if present
        if (baseList is not null)
        {
            typeDeclarationSyntax = typeDeclarationSyntax.WithBaseList(baseList);
        }

        // Add all parent types in ascending order, if any
        foreach (TypeInfo parentType in Hierarchy.AsSpan()[1..])
        {
            typeDeclarationSyntax =
                parentType.GetSyntax()
                .AddModifiers(PartialKeyword)
                .AddMembers(typeDeclarationSyntax);
        }

        if (Namespace is "")
        {
            // If there is no namespace, attach the pragma directly to the declared type,
            // and skip the namespace declaration. This will produce code as follows:
            //
            // <SYNTAX_TRIVIA>
            // <TYPE_HIERARCHY>
            return CompilationUnit()
                .WithMembers(SingletonList<MemberDeclarationSyntax>(
                    typeDeclarationSyntax
                        .WithLeadingTrivia(NullableEnableTriviaList)));
        }

        // Create the compilation unit with disabled warnings, target namespace and generated type.
        // This will produce code as follows:
        //
        // namespace <NAMESPACE>;
        // <TYPE_HIERARCHY>
        return CompilationUnit()
            .WithMembers(SingletonList<MemberDeclarationSyntax>(FileScopedNamespaceDeclaration(Namespace)
                .WithMembers(SingletonList<MemberDeclarationSyntax>(typeDeclarationSyntax
                    .WithLeadingTrivia(NullableEnableTriviaList)))));
    }
}