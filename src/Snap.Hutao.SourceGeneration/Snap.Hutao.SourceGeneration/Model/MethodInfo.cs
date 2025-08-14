// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Snap.Hutao.SourceGeneration.Extension;
using System.Collections.Immutable;

namespace Snap.Hutao.SourceGeneration.Model;

internal sealed record MethodInfo
{
    private MethodInfo(
        string minimallyQualifiedName,
        string fullyQualifiedReturnTypeName,
        string fullyQualifiedReturnTypeMetadataName,
        EquatableArray<ParameterInfo> parameters)
    {
        MinimallyQualifiedName = minimallyQualifiedName;
        FullyQualifiedReturnTypeName = fullyQualifiedReturnTypeName;
        FullyQualifiedReturnTypeMetadataName = fullyQualifiedReturnTypeMetadataName;
        Parameters = parameters;
    }

    public string MinimallyQualifiedName { get; }

    public string FullyQualifiedReturnTypeName { get; }

    public string FullyQualifiedReturnTypeMetadataName { get; }

    public EquatableArray<ParameterInfo> Parameters { get; init; }

    public static MethodInfo Create(IMethodSymbol methodSymbol)
    {
        return new(
            methodSymbol.Name,
            methodSymbol.ReturnType.GetFullyQualifiedNameWithNullabilityAnnotations(),
            methodSymbol.ReturnType.GetFullyQualifiedMetadataName(),
            ImmutableArray.CreateRange(methodSymbol.Parameters, ParameterInfo.Create));
    }
}