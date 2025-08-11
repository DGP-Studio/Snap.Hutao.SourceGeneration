// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using System;

namespace Snap.Hutao.SourceGeneration.Primitive;

[Obsolete]
internal readonly struct GeneratorSymbolContext
{
    public readonly GeneratorSyntaxContext SyntaxContext;
    public readonly INamedTypeSymbol Symbol;
    public readonly bool HasValue = false;

    public GeneratorSymbolContext(GeneratorSyntaxContext context, INamedTypeSymbol symbol)
    {
        SyntaxContext = context;
        Symbol = symbol;
        HasValue = true;
    }

    public static bool NotNull(GeneratorSymbolContext context)
    {
        return context.HasValue;
    }
}