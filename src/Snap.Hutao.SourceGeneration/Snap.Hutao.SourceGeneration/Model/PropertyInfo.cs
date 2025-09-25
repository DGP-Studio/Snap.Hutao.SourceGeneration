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
        Accessibility? getMethodAccessibility,
        Accessibility? setMethodAccessibility,
        string name,
        string fullyQualifiedTypeName,
        string fullyQualifiedTypeNameWithNullabilityAnnotation)
    {
        Attributes = attributes;
        DeclaredAccessibility = declaredAccessibility;
        GetMethodAccessibility = getMethodAccessibility;
        SetMethodAccessibility = setMethodAccessibility;
        Name = name;
        FullyQualifiedTypeName = fullyQualifiedTypeName;
        FullyQualifiedTypeNameWithNullabilityAnnotation = fullyQualifiedTypeNameWithNullabilityAnnotation;
    }

    public string Name { get; }

    public string FullyQualifiedTypeName { get; }

    public string FullyQualifiedTypeNameWithNullabilityAnnotation { get; }

    public EquatableArray<AttributeInfo> Attributes { get; set; }

    public Accessibility DeclaredAccessibility { get; init; }

    public Accessibility? GetMethodAccessibility { get; init; }

    public Accessibility? SetMethodAccessibility { get; init; }

    public bool IsIndexer { get; init; }

    public string? FullyQualifiedIndexerParameterTypeName { get; init; }

    public bool IsStatic { get; init; }

    public static PropertyInfo Create(IPropertySymbol propertySymbol)
    {
        return new(
            ImmutableArray.CreateRange(propertySymbol.GetAttributes(), AttributeInfo.Create),
            propertySymbol.DeclaredAccessibility,
            propertySymbol.GetMethod?.DeclaredAccessibility,
            propertySymbol.SetMethod?.DeclaredAccessibility,
            propertySymbol.Name,
            propertySymbol.Type.GetFullyQualifiedName(),
            propertySymbol.Type.GetFullyQualifiedNameWithNullabilityAnnotations())
        {
            IsIndexer = propertySymbol.IsIndexer,
            FullyQualifiedIndexerParameterTypeName = propertySymbol.IsIndexer ? propertySymbol.Parameters[0].Type.GetFullyQualifiedNameWithNullabilityAnnotations() : null,
            IsStatic = propertySymbol.IsStatic,
        };
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