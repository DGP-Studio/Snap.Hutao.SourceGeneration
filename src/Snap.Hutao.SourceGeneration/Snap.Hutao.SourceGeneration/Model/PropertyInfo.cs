// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Snap.Hutao.SourceGeneration.Extension;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Snap.Hutao.SourceGeneration.Model;

internal sealed record PropertyInfo
{
    private PropertyInfo(
        EquatableArray<AttributeInfo> attributes,
        Accessibility declaredAccessibility,
        string minimallyQualifiedName,
        string fullyQualifiedTypeName,
        string fullyQualifiedTypeNameWithNullabilityAnnotation)
    {
        Attributes = attributes;
        DeclaredAccessibility = declaredAccessibility;
        MinimallyQualifiedName = minimallyQualifiedName;
        FullyQualifiedTypeName = fullyQualifiedTypeName;
        FullyQualifiedTypeNameWithNullabilityAnnotation = fullyQualifiedTypeNameWithNullabilityAnnotation;
    }

    public string MinimallyQualifiedName { get; }

    public string FullyQualifiedTypeName { get; }

    public string FullyQualifiedTypeNameWithNullabilityAnnotation { get; }

    public EquatableArray<AttributeInfo> Attributes { get; set; }

    public Accessibility DeclaredAccessibility { get; init; }

    public static PropertyInfo Create(IPropertySymbol propertySymbol)
    {
        return new(
            ImmutableArray.CreateRange(propertySymbol.GetAttributes(), AttributeInfo.Create),
            propertySymbol.DeclaredAccessibility,
            propertySymbol.Name,
            propertySymbol.Type.GetFullyQualifiedName(),
            propertySymbol.Type.GetFullyQualifiedNameWithNullabilityAnnotations());
    }

    public bool TryGetAttributeWithFullyQualifiedMetadataName(string name, [NotNullWhen(true)] out AttributeInfo? attributeInfo)
    {
        foreach (AttributeInfo attribute in Attributes)
        {
            if (string.Equals(attribute.FullyQualifiedMetadataName, name, StringComparison.Ordinal))
            {
                attributeInfo = attribute;
                return true;
            }
        }

        attributeInfo = null;
        return false;
    }
}