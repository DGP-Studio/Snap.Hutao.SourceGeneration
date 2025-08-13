// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;

namespace Snap.Hutao.SourceGeneration.Model;

internal interface ISyntaxInfo<out TSyntaxNode>
    where TSyntaxNode : SyntaxNode
{
    TSyntaxNode GetSyntax();
}