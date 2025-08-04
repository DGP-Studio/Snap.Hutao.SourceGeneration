// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using System.Threading;

namespace Snap.Hutao.SourceGeneration.Primitive;

internal static class SyntaxContext
{
    public static TSyntaxContext Transform<TSyntaxContext>(TSyntaxContext context, CancellationToken token)
        where TSyntaxContext : struct
    {
        token.ThrowIfCancellationRequested();
        return context;
    }
}