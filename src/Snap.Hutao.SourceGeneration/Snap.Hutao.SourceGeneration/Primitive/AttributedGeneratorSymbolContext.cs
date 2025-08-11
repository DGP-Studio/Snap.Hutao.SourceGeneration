// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Snap.Hutao.SourceGeneration.Primitive;

[Obsolete]
internal readonly struct AttributedGeneratorSymbolContext
{
    public readonly GeneratorSyntaxContext SyntaxContext;
    public readonly INamedTypeSymbol Symbol;
    public readonly ImmutableArray<AttributeData> Attributes;
    public readonly bool HasValue = false;

    public AttributedGeneratorSymbolContext(GeneratorSyntaxContext context, INamedTypeSymbol symbol, ImmutableArray<AttributeData> attributes)
    {
        SyntaxContext = context;
        Symbol = symbol;
        Attributes = attributes;
        HasValue = true;
    }

    public static bool NotNull(AttributedGeneratorSymbolContext context)
    {
        return context.HasValue;
    }

    public AttributeData SingleAttribute(string name)
    {
        return Attributes.Single(attribute => attribute.AttributeClass!.ToDisplayString() == name);
    }

    public AttributeData? SingleOrDefaultAttribute(string name)
    {
        return Attributes.SingleOrDefault(attribute => attribute.AttributeClass!.ToDisplayString() == name);
    }
}

[Obsolete]
internal readonly struct AttributedGeneratorSymbolContext<TSymbol>
    where TSymbol : ISymbol
{
    public readonly GeneratorSyntaxContext SyntaxContext;
    public readonly TSymbol Symbol;
    public readonly ImmutableArray<AttributeData> Attributes;
    public readonly bool HasValue = false;

    public AttributedGeneratorSymbolContext(GeneratorSyntaxContext context, TSymbol symbol, ImmutableArray<AttributeData> attributes)
    {
        SyntaxContext = context;
        Symbol = symbol;
        Attributes = attributes;
        HasValue = true;
    }

    public static bool NotNull(AttributedGeneratorSymbolContext<TSymbol> context)
    {
        return context.HasValue;
    }

    public AttributeData SingleAttribute(string name)
    {
        return Attributes.Single(attribute => attribute.AttributeClass!.ToDisplayString() == name);
    }
}