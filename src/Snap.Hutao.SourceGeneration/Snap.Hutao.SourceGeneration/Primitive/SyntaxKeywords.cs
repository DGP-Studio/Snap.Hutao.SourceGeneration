// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Snap.Hutao.SourceGeneration.Primitive;

internal static class SyntaxKeywords
{
    public static SyntaxToken AbstractKeyword { get; } = SyntaxFactory.Token(SyntaxKind.AbstractKeyword);

    public static SyntaxToken ClassKeyword { get; } = SyntaxFactory.Token(SyntaxKind.ClassKeyword);

    public static SyntaxToken ConstKeyword { get; } = SyntaxFactory.Token(SyntaxKind.ConstKeyword);

    public static SyntaxToken ExternKeyword { get; } = SyntaxFactory.Token(SyntaxKind.ExternKeyword);

    public static SyntaxToken InternalKeyword { get; } = SyntaxFactory.Token(SyntaxKind.InternalKeyword);

    public static SyntaxToken PartialKeyword { get; } = SyntaxFactory.Token(SyntaxKind.PartialKeyword);

    public static SyntaxToken PrivateKeyword { get; } = SyntaxFactory.Token(SyntaxKind.PrivateKeyword);

    public static SyntaxToken ProtectedKeyword { get; } = SyntaxFactory.Token(SyntaxKind.ProtectedKeyword);

    public static SyntaxToken PublicKeyword { get; } = SyntaxFactory.Token(SyntaxKind.PublicKeyword);

    public static SyntaxToken ReadOnlyKeyword { get; } = SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword);

    public static SyntaxToken RecordKeyword { get; } = SyntaxFactory.Token(SyntaxKind.RecordKeyword);

    public static SyntaxToken SealedKeyword { get; } = SyntaxFactory.Token(SyntaxKind.SealedKeyword);

    public static SyntaxToken SemicolonToken { get; } = SyntaxFactory.Token(SyntaxKind.SemicolonToken);

    public static SyntaxToken StaticKeyword { get; } = SyntaxFactory.Token(SyntaxKind.StaticKeyword);

    public static SyntaxToken StructKeyword { get; } = SyntaxFactory.Token(SyntaxKind.StructKeyword);

    public static SyntaxToken ThisKeyword { get; } = SyntaxFactory.Token(SyntaxKind.ThisKeyword);
}