// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Snap.Hutao.SourceGeneration.Extension;

internal static class GeneratorSyntaxContextExtension
{
    public static bool TryGetDeclaredSymbol<TSymbol>(this GeneratorSyntaxContext context, CancellationToken token, [NotNullWhen(true)] out TSymbol? symbol)
        where TSymbol : class, ISymbol
    {
        symbol = context.SemanticModel.GetDeclaredSymbol(context.Node, token) as TSymbol;
        return symbol is not null;
    }
}