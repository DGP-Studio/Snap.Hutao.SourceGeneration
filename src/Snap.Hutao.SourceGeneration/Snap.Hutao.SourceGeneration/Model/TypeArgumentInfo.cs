// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snap.Hutao.SourceGeneration.Extension;

namespace Snap.Hutao.SourceGeneration.Model;

internal sealed record TypeArgumentInfo
{
    public TypeArgumentInfo(string minimallyQualifiedName, string fullyQualifiedTypeName, string fullyQualifiedTypeNameWithNullabilityAnnotations)
    {
        MinimallyQualifiedName = minimallyQualifiedName;
        FullyQualifiedTypeName = fullyQualifiedTypeName;
        FullyQualifiedTypeNameWithNullabilityAnnotations = fullyQualifiedTypeNameWithNullabilityAnnotations;
    }

    public string MinimallyQualifiedName { get; }

    public string FullyQualifiedTypeName { get; }

    public string FullyQualifiedTypeNameWithNullabilityAnnotations { get; }

    public static TypeArgumentInfo Create(ITypeSymbol typeSymbol)
    {
        return new(
            typeSymbol.Name,
            typeSymbol.GetFullyQualifiedName(),
            typeSymbol.GetFullyQualifiedNameWithNullabilityAnnotations());
    }

    public TypeSyntax GetSyntax()
    {
        return SyntaxFactory.ParseTypeName(FullyQualifiedTypeNameWithNullabilityAnnotations);
    }
}