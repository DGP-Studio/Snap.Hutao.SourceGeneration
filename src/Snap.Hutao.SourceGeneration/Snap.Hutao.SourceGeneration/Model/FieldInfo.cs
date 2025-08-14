// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.SourceGeneration.Model;

internal sealed record FieldInfo
{
    private FieldInfo(string minimallyQualifiedName, string fullyQualifiedTypeName, string fullyQualifiedTypeNameWithNullabilityAnnotation)
    {
        MinimallyQualifiedName = minimallyQualifiedName;
        FullyQualifiedTypeName = fullyQualifiedTypeName;
        FullyQualifiedTypeNameWithNullabilityAnnotation = fullyQualifiedTypeNameWithNullabilityAnnotation;
    }

    public string MinimallyQualifiedName { get; }

    public string FullyQualifiedTypeName { get; }

    public string FullyQualifiedTypeNameWithNullabilityAnnotation { get; }
}