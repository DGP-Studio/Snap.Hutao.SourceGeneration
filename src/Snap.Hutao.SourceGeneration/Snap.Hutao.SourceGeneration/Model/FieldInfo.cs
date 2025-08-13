// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.SourceGeneration.Model;

internal sealed record FieldInfo
{
    public FieldInfo(string minimallyQualifiedName, TypeInfo typeInfo)
    {
        MinimallyQualifiedName = minimallyQualifiedName;
        TypeInfo = typeInfo;
    }

    public string MinimallyQualifiedName { get; }

    public TypeInfo TypeInfo { get; }
}