// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Snap.Hutao.SourceGeneration.Primitive;

internal static class FastSyntaxFactory
{
    public static ArgumentListSyntax EmptyArgumentList { get; } = SyntaxFactory.ArgumentList();

    public static LiteralExpressionSyntax FalseLiteralExpression { get; } = SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);

    public static LiteralExpressionSyntax TrueLiteralExpression { get; } = SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);

    public static PredefinedTypeSyntax BoolType { get; } = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword));

    public static PredefinedTypeSyntax IntType { get; } = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword));

    public static PredefinedTypeSyntax ObjectType { get; } = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword));

    public static PredefinedTypeSyntax StringType { get; } = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword));

    public static SyntaxToken AbstractKeyword { get; } = SyntaxFactory.Token(SyntaxKind.AbstractKeyword);

    public static SyntaxToken InternalKeyword { get; } = SyntaxFactory.Token(SyntaxKind.InternalKeyword);

    public static SyntaxToken PartialKeyWord { get; } = SyntaxFactory.Token(SyntaxKind.PartialKeyword);

    public static SyntaxToken PublicKeyword { get; } = SyntaxFactory.Token(SyntaxKind.PublicKeyword);

    public static SyntaxToken SealedKeyword { get; } = SyntaxFactory.Token(SyntaxKind.SealedKeyword);

    public static SyntaxToken SemicolonToken { get; } = SyntaxFactory.Token(SyntaxKind.SemicolonToken);

    public static FileScopedNamespaceDeclarationSyntax FileScopedNamespaceDeclaration(params ReadOnlySpan<string> names)
    {
        return SyntaxFactory.FileScopedNamespaceDeclaration(Name(names));
    }

    public static AccessorListSyntax GetAndSetAccessorList()
    {
        return SyntaxFactory.AccessorList(SyntaxFactory.List(
        [
            SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SemicolonToken),
            SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SemicolonToken)
        ]));
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

    public static MemberAccessExpressionSyntax SimpleMemberAccessExpression(ExpressionSyntax expression, SimpleNameSyntax name)
    {
        return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, expression, name);
    }

    public static UsingDirectiveSyntax UsingDirective(params ReadOnlySpan<string> names)
    {
        return SyntaxFactory.UsingDirective(Name(names));
    }

    public static ObjectCreationExpressionSyntax WithArgumentList(this ObjectCreationExpressionSyntax expression)
    {
        return expression.WithArgumentList(EmptyArgumentList);
    }

    public static ConstructorDeclarationSyntax WithEmptyBlockBody(this ConstructorDeclarationSyntax constructor)
    {
        return constructor.WithBody(SyntaxFactory.Block());
    }
}