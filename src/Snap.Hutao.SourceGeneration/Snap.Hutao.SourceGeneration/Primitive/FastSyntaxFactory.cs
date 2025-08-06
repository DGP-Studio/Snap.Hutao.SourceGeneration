// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;

namespace Snap.Hutao.SourceGeneration.Primitive;

// Properties and methods are ordered by return type
internal static class FastSyntaxFactory
{
    public static ArgumentListSyntax EmptyArgumentList { get; } = SyntaxFactory.ArgumentList();

    public static LiteralExpressionSyntax FalseLiteralExpression { get; } = SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);

    public static LiteralExpressionSyntax TrueLiteralExpression { get; } = SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);

    public static PredefinedTypeSyntax BoolType { get; } = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword));

    public static PredefinedTypeSyntax IntType { get; } = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword));

    public static PredefinedTypeSyntax ObjectType { get; } = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword));

    public static PredefinedTypeSyntax StringType { get; } = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword));

    public static PredefinedTypeSyntax VoidType { get; } = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));

    public static SyntaxToken AbstractKeyword { get; } = SyntaxFactory.Token(SyntaxKind.AbstractKeyword);

    public static SyntaxToken ClassKeyword { get; } = SyntaxFactory.Token(SyntaxKind.ClassKeyword);

    public static SyntaxToken InternalKeyword { get; } = SyntaxFactory.Token(SyntaxKind.InternalKeyword);

    public static SyntaxToken PartialKeyword { get; } = SyntaxFactory.Token(SyntaxKind.PartialKeyword);

    public static SyntaxToken PrivateKeyword { get; } = SyntaxFactory.Token(SyntaxKind.PrivateKeyword);

    public static SyntaxToken PublicKeyword { get; } = SyntaxFactory.Token(SyntaxKind.PublicKeyword);

    public static SyntaxToken ReadOnlyKeyword { get; } = SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword);

    public static SyntaxToken RecordKeyword { get; } = SyntaxFactory.Token(SyntaxKind.RecordKeyword);

    public static SyntaxToken SealedKeyword { get; } = SyntaxFactory.Token(SyntaxKind.SealedKeyword);

    public static SyntaxToken SemicolonToken { get; } = SyntaxFactory.Token(SyntaxKind.SemicolonToken);

    public static SyntaxToken StructKeyword { get; } = SyntaxFactory.Token(SyntaxKind.StructKeyword);

    public static SyntaxTokenList InternalTokenList { get; } = SyntaxFactory.TokenList(
        SyntaxFactory.Token(SyntaxKind.InternalKeyword));

    public static SyntaxTokenList InternalAbstractTokenList { get; } = SyntaxFactory.TokenList(
        SyntaxFactory.Token(SyntaxKind.InternalKeyword),
        SyntaxFactory.Token(SyntaxKind.AbstractKeyword));

    public static SyntaxTokenList InternalPartialTokenList { get; } = SyntaxFactory.TokenList(
        SyntaxFactory.Token(SyntaxKind.InternalKeyword),
        SyntaxFactory.Token(SyntaxKind.PartialKeyword));

    public static SyntaxTokenList PrivateTokenList { get; } = SyntaxFactory.TokenList(
        SyntaxFactory.Token(SyntaxKind.PrivateKeyword));

    public static SyntaxTokenList PrivateProtectedTokenList { get; } = SyntaxFactory.TokenList(
        SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
        SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));

    public static SyntaxTokenList PrivateStaticExternTokenList { get; } = SyntaxFactory.TokenList(
        SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
        SyntaxFactory.Token(SyntaxKind.StaticKeyword),
        SyntaxFactory.Token(SyntaxKind.ExternKeyword));

    public static SyntaxTokenList ProtectedTokenList { get; } = SyntaxFactory.TokenList(SyntaxFactory.Token(
        SyntaxKind.ProtectedKeyword));

    public static SyntaxTokenList ProtectedInternalTokenList { get; } = SyntaxFactory.TokenList(
        SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
        SyntaxFactory.Token(SyntaxKind.InternalKeyword));

    public static SyntaxTokenList PublicTokenList { get; } = SyntaxFactory.TokenList(
        SyntaxFactory.Token(SyntaxKind.PublicKeyword));

    public static AccessorListSyntax GetAndSetAccessorList()
    {
        return SyntaxFactory.AccessorList(SyntaxFactory.List(
        [
            SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SemicolonToken),
            SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SemicolonToken)
        ]));
    }

    public static AssignmentExpressionSyntax SimpleAssignmentExpression(ExpressionSyntax left, ExpressionSyntax right)
    {
        return SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, left, right);
    }

    public static ConstructorDeclarationSyntax WithEmptyBlockBody(this ConstructorDeclarationSyntax constructor)
    {
        return constructor.WithBody(SyntaxFactory.Block());
    }

    public static ConstructorInitializerSyntax BaseConstructorInitializer(ArgumentListSyntax? argumentList = null)
    {
        return SyntaxFactory.ConstructorInitializer(SyntaxKind.BaseConstructorInitializer, argumentList);
    }

    public static FileScopedNamespaceDeclarationSyntax FileScopedNamespaceDeclaration(string name)
    {
        return SyntaxFactory.FileScopedNamespaceDeclaration(SyntaxFactory.ParseName(name));
    }

    public static FileScopedNamespaceDeclarationSyntax FileScopedNamespaceDeclaration(INamespaceSymbol symbol)
    {
        return SyntaxFactory.FileScopedNamespaceDeclaration(SyntaxFactory.ParseName(symbol.ToDisplayString()));
    }

    public static InvocationExpressionSyntax NameOf(ExpressionSyntax argument)
    {
        return SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("nameof"), SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(argument))));
    }

    public static LiteralExpressionSyntax StringLiteralExpression(string value)
    {
        return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(value));
    }

    public static MemberAccessExpressionSyntax SimpleMemberAccessExpression(ExpressionSyntax expression, SimpleNameSyntax name)
    {
        return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, expression, name);
    }

    public static ObjectCreationExpressionSyntax WithArgumentList(this ObjectCreationExpressionSyntax expression)
    {
        return expression.WithArgumentList(EmptyArgumentList);
    }

    public static TypeDeclarationSyntax PartialTypeDeclaration(INamedTypeSymbol typeSymbol)
    {
        string typeName = typeSymbol.Name;

        TypeDeclarationSyntax typeDeclaration = (typeSymbol.TypeKind, typeSymbol.IsRecord) switch
        {
            (TypeKind.Class, false) => SyntaxFactory.ClassDeclaration(typeName),
            (TypeKind.Class, true) => SyntaxFactory.RecordDeclaration(SyntaxKind.RecordDeclaration, RecordKeyword, typeName).WithClassOrStructKeyword(ClassKeyword),
            (TypeKind.Struct, false) => SyntaxFactory.StructDeclaration(typeName),
            (TypeKind.Struct, true) => SyntaxFactory.RecordDeclaration(SyntaxKind.RecordStructDeclaration, RecordKeyword, typeName).WithClassOrStructKeyword(StructKeyword),
            (TypeKind.Interface, _) => SyntaxFactory.InterfaceDeclaration(typeName),
            _ => throw new InvalidOperationException("Unsupported type kind for partial declaration: " + typeSymbol.TypeKind)
        };

        if (typeSymbol.IsGenericType)
        {
            typeDeclaration = typeDeclaration.WithTypeParameterList(SyntaxFactory.TypeParameterList(SyntaxFactory.SeparatedList(
                ImmutableArray.CreateRange(typeSymbol.TypeParameters, static tParam => SyntaxFactory.TypeParameter(tParam.Name)))));
        }

        return typeDeclaration.WithModifiers(SyntaxFactory.TokenList(PartialKeyword));
    }

    public static UsingDirectiveSyntax UsingDirective(string name)
    {
        return SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(name));
    }
}