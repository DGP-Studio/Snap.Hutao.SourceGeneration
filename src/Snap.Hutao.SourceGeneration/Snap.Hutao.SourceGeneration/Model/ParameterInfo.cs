// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Snap.Hutao.SourceGeneration.Extension;

namespace Snap.Hutao.SourceGeneration.Model;

internal sealed record ParameterInfo
{
    public required string MinimallyQualifiedName { get; init; }

    public required string FullyQualifiedTypeName { get; init; }

    public required string FullyQualifiedTypeNameWithNullabilityAnnotations { get; init; }

    public static ParameterInfo Create(IParameterSymbol parameterSymbol)
    {
        return new()
        {
            MinimallyQualifiedName = parameterSymbol.Name,
            FullyQualifiedTypeName = parameterSymbol.Type.GetFullyQualifiedName(),
            FullyQualifiedTypeNameWithNullabilityAnnotations = parameterSymbol.Type.GetFullyQualifiedNameWithNullabilityAnnotations(),
        };
    }
}