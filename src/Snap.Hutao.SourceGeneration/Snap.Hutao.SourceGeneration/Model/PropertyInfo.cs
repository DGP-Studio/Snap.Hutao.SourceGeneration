// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Snap.Hutao.SourceGeneration.Extension;

namespace Snap.Hutao.SourceGeneration.Model;

internal sealed record PropertyInfo
{
    public PropertyInfo(string minimallyQualifiedName, string fullyQualifiedTypeName)
    {
        MinimallyQualifiedName = minimallyQualifiedName;
        FullyQualifiedTypeName = fullyQualifiedTypeName;
    }

    public string MinimallyQualifiedName { get; }

    public string FullyQualifiedTypeName { get; }

    public static PropertyInfo Create(IPropertySymbol propertySymbol)
    {
        return new(propertySymbol.Name, propertySymbol.Type.GetFullyQualifiedNameWithNullabilityAnnotations());
    }
}