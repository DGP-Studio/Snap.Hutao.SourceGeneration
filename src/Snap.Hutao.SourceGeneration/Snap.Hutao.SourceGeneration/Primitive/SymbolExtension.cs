﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;

namespace Snap.Hutao.SourceGeneration.Primitive;

internal static class SymbolExtension
{
    public static string GetFullyQualifiedName(this ISymbol symbol)
    {
        return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    public static string GetFullyQualifiedNameWithNullabilityAnnotations(this ISymbol symbol)
    {
        return symbol.ToDisplayString(SymbolDisplayFormats.NullableFullyQualifiedFormat);
    }

    public static bool HasFullyQualifiedName(this ISymbol symbol, string name)
    {
        return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == name;
    }

    public static bool HasAttributeWithFullyQualifiedMetadataName(this ISymbol symbol, string name)
    {
        foreach (AttributeData attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass?.HasFullyQualifiedMetadataName(name) is true)
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasAttributeWithType(this ISymbol symbol, ITypeSymbol typeSymbol)
    {
        return TryGetAttributeWithType(symbol, typeSymbol, out _);
    }

    public static bool TryGetAttributeWithType(this ISymbol symbol, ITypeSymbol typeSymbol, [NotNullWhen(true)] out AttributeData? attributeData)
    {
        foreach (AttributeData attribute in symbol.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, typeSymbol))
            {
                attributeData = attribute;

                return true;
            }
        }

        attributeData = null;

        return false;
    }

    public static Accessibility GetEffectiveAccessibility(this ISymbol symbol)
    {
        // Start by assuming it's visible
        Accessibility visibility = Accessibility.Public;

        // Handle special cases
        switch (symbol.Kind)
        {
            case SymbolKind.Alias: return Accessibility.Private;
            case SymbolKind.Parameter: return GetEffectiveAccessibility(symbol.ContainingSymbol);
            case SymbolKind.TypeParameter: return Accessibility.Private;
        }

        // Traverse the symbol hierarchy to determine the effective accessibility
        while (symbol is not null && symbol.Kind != SymbolKind.Namespace)
        {
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.NotApplicable:
                case Accessibility.Private:
                    return Accessibility.Private;
                case Accessibility.Internal:
                case Accessibility.ProtectedAndInternal:
                    visibility = Accessibility.Internal;
                    break;
            }

            symbol = symbol.ContainingSymbol;
        }

        return visibility;
    }

    public static bool CanBeAccessedFrom(this ISymbol symbol, IAssemblySymbol assembly)
    {
        Accessibility accessibility = symbol.GetEffectiveAccessibility();

        return
            accessibility == Accessibility.Public ||
            accessibility == Accessibility.Internal && symbol.ContainingAssembly.GivesAccessTo(assembly);
    }

    public static Location? GetLocationFromAttributeDataOrDefault(this ISymbol symbol, AttributeData attributeData)
    {
        Location? firstLocation = null;

        // Get the syntax tree where the attribute application is located. We use
        // it to try to find the symbol location that belongs to the same file.
        SyntaxTree? attributeTree = attributeData.ApplicationSyntaxReference?.SyntaxTree;

        foreach (Location location in symbol.Locations)
        {
            if (location.SourceTree == attributeTree)
            {
                return location;
            }

            firstLocation ??= location;
        }

        return firstLocation;
    }
}