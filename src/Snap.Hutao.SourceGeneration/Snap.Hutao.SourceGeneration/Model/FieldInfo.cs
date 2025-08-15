// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Snap.Hutao.SourceGeneration.Extension;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Snap.Hutao.SourceGeneration.Model;

internal sealed record FieldInfo
{
    private FieldInfo(EquatableArray<AttributeInfo> attributes, string minimallyQualifiedName, string fullyQualifiedTypeName, string fullyQualifiedTypeNameWithNullabilityAnnotation)
    {
        Attributes = attributes;
        MinimallyQualifiedName = minimallyQualifiedName;
        FullyQualifiedTypeName = fullyQualifiedTypeName;
        FullyQualifiedTypeNameWithNullabilityAnnotation = fullyQualifiedTypeNameWithNullabilityAnnotation;
    }

    public string MinimallyQualifiedName { get; }

    public string FullyQualifiedTypeName { get; }

    public string FullyQualifiedTypeNameWithNullabilityAnnotation { get; }

    public EquatableArray<AttributeInfo> Attributes { get; set; }

    public static FieldInfo Create(IFieldSymbol fieldSymbol)
    {
        return new(
            ImmutableArray.CreateRange(fieldSymbol.GetAttributes(), AttributeInfo.Create),
            fieldSymbol.Name,
            fieldSymbol.Type.GetFullyQualifiedName(),
            fieldSymbol.Type.GetFullyQualifiedNameWithNullabilityAnnotations());
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