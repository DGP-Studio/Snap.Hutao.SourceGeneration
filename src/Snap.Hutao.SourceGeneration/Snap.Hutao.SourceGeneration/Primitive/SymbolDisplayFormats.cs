// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;

namespace Snap.Hutao.SourceGeneration.Primitive;

internal static class SymbolDisplayFormats
{
    public static SymbolDisplayFormat NullableFullyQualifiedFormat { get; } = SymbolDisplayFormat.FullyQualifiedFormat
        .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);
}