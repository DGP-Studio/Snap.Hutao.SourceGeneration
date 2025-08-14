// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Snap.Hutao.SourceGeneration.Extension;

namespace Snap.Hutao.SourceGeneration.Model;

internal sealed record PropertyInfo
{
    private PropertyInfo(string minimallyQualifiedName, string fullyQualifiedTypeName, string fullyQualifiedTypeNameWithNullabilityAnnotation)
    {
        MinimallyQualifiedName = minimallyQualifiedName;
        FullyQualifiedTypeName = fullyQualifiedTypeName;
        FullyQualifiedTypeNameWithNullabilityAnnotation = fullyQualifiedTypeNameWithNullabilityAnnotation;
    }

    public string MinimallyQualifiedName { get; }

    public string FullyQualifiedTypeName { get; }

    public string FullyQualifiedTypeNameWithNullabilityAnnotation { get; }

    public static PropertyInfo Create(IPropertySymbol propertySymbol)
    {
        return new(
            propertySymbol.Name,
            propertySymbol.Type.GetFullyQualifiedName(),
            propertySymbol.Type.GetFullyQualifiedNameWithNullabilityAnnotations());
    }
}