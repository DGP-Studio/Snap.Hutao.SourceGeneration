// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Snap.Hutao.SourceGeneration.Primitive;

internal static class SymbolHelper
{
    public static NameSyntax NamespaceSymbolToNameSyntax(INamespaceSymbol namespaceSymbol)
    {
        return SyntaxFactory.ParseName(namespaceSymbol.ToDisplayString());
    }
}