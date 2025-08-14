// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Snap.Hutao.SourceGeneration.Extension;

namespace Snap.Hutao.SourceGeneration.Model;

internal sealed record ParameterInfo
{
    public ParameterInfo(string minimallyQualifiedName, string fullyQualifiedTypeName, string fullyQualifiedTypeNameWithNullabilityAnnotations)
    {
        MinimallyQualifiedName = minimallyQualifiedName;
        FullyQualifiedTypeName = fullyQualifiedTypeName;
        FullyQualifiedTypeNameWithNullabilityAnnotations = fullyQualifiedTypeNameWithNullabilityAnnotations;
    }

    public string MinimallyQualifiedName { get; }

    public string FullyQualifiedTypeName { get; }

    public string FullyQualifiedTypeNameWithNullabilityAnnotations { get; }

    public static ParameterInfo Create(IParameterSymbol parameterSymbol)
    {
        return new(
            parameterSymbol.Name,
            parameterSymbol.Type.GetFullyQualifiedName(),
            parameterSymbol.Type.GetFullyQualifiedNameWithNullabilityAnnotations());
    }
}