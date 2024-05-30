// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;

namespace Snap.Hutao.SourceGeneration.Primitive;

internal readonly struct GeneratorSyntaxContext3
{
    public readonly GeneratorSyntaxContext Context;
    public readonly INamedTypeSymbol Symbol;
    public readonly bool HasValue = false;

    public GeneratorSyntaxContext3(GeneratorSyntaxContext context, INamedTypeSymbol symbol)
    {
        Context = context;
        Symbol = symbol;
        HasValue = true;
    }

    public static bool NotNull(GeneratorSyntaxContext3 context)
    {
        return context.HasValue;
    }

    public TSyntaxNode Node<TSyntaxNode>()
        where TSyntaxNode : SyntaxNode
    {
        return (TSyntaxNode)Context.Node;
    }
}