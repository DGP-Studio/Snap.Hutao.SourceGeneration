// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Text;

namespace Snap.Hutao.SourceGeneration.Primitive;

internal sealed class SyntaxNormalizer : CSharpSyntaxVisitor
{
    private readonly StringBuilder stringBuilder = new();

    public override void DefaultVisit(SyntaxNode node)
    {
        stringBuilder.Append(node.ToString());
    }

    public static string NormalizeAndGetString(SyntaxNode node)
    {
        SyntaxNormalizer normalizer = new();
        normalizer.Visit(node);
        return normalizer.stringBuilder.ToString();
    }
}