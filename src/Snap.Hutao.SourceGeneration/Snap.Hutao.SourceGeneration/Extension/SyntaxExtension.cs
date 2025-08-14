// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Snap.Hutao.SourceGeneration.Extension;

internal static class SyntaxExtension
{
    [Obsolete]
    public static bool HasAttributeLists<TSyntax>(this TSyntax declaration)
        where TSyntax : MemberDeclarationSyntax
    {
        return declaration.AttributeLists.Count > 0;
    }
}