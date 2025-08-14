// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading;

namespace Snap.Hutao.SourceGeneration.Primitive;

internal static class SyntaxNodeHelper
{
    public static bool Is<T>(SyntaxNode node, CancellationToken token)
        where T : SyntaxNode
    {
        token.ThrowIfCancellationRequested();
        return node is T;
    }

    public static bool TypeHasBaseType(SyntaxNode node, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return node is TypeDeclarationSyntax { BaseList: not null };
    }
}