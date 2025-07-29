// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;

namespace Snap.Hutao.SourceGeneration.Primitive;

internal static class FastSyntaxFactory
{
    public static SyntaxToken AbstractKeyword { get; } = SyntaxFactory.Token(SyntaxKind.AbstractKeyword);

    public static SyntaxToken InternalKeyword { get; } = SyntaxFactory.Token(SyntaxKind.InternalKeyword);

    public static SyntaxToken PartialKeyWord { get; } = SyntaxFactory.Token(SyntaxKind.PartialKeyword);

    public static UsingDirectiveSyntax UsingDirective(params ReadOnlySpan<string> names)
    {
        return SyntaxFactory.UsingDirective(Name(names));
    }

    public static FileScopedNamespaceDeclarationSyntax FileScopedNamespaceDeclaration(params ReadOnlySpan<string> names)
    {
        return SyntaxFactory.FileScopedNamespaceDeclaration(Name(names));
    }

    public static NameSyntax Name(params ReadOnlySpan<string> names)
    {
        if (names.Length <= 0)
        {
            throw new ArgumentException("At least one name must be provided.", nameof(names));
        }

        NameSyntax name = SyntaxFactory.IdentifierName(names[0]);

        for (int i = 1; i < names.Length; i++)
        {
            name = SyntaxFactory.QualifiedName(name, SyntaxFactory.IdentifierName(names[i]));
        }

        return name;
    }
}