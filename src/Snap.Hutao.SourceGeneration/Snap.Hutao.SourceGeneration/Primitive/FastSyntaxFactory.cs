// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using static Snap.Hutao.SourceGeneration.Primitive.SyntaxKeywords;

namespace Snap.Hutao.SourceGeneration.Primitive;

// Properties and methods are ordered by return type
internal static class FastSyntaxFactory
{
    public static ArgumentListSyntax EmptyArgumentList { get; } = SyntaxFactory.ArgumentList();

    public static BlockSyntax EmptyBlock { get; } = SyntaxFactory.Block();

    public static IdentifierNameSyntax NameOfIdentifier { get; } = SyntaxFactory.IdentifierName("nameof");

    public static LiteralExpressionSyntax FalseLiteralExpression { get; } = SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);

    public static LiteralExpressionSyntax TrueLiteralExpression { get; } = SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);

    public static NullableTypeSyntax NullableStringType { get; } = SyntaxFactory.NullableType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)));

    public static PredefinedTypeSyntax BoolType { get; } = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword));

    public static PredefinedTypeSyntax IntType { get; } = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword));

    public static PredefinedTypeSyntax ObjectType { get; } = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword));

    public static PredefinedTypeSyntax StringType { get; } = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword));

    public static PredefinedTypeSyntax VoidType { get; } = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));

    public static SyntaxTokenList InternalTokenList { get; } = SyntaxFactory.TokenList(InternalKeyword);

    public static SyntaxTokenList InternalAbstractTokenList { get; } = SyntaxFactory.TokenList(InternalKeyword, AbstractKeyword);

    public static SyntaxTokenList InternalSealedTokenList { get; } = SyntaxFactory.TokenList(InternalKeyword, SealedKeyword);

    public static SyntaxTokenList InternalStaticTokenList { get; } = SyntaxFactory.TokenList(InternalKeyword, StaticKeyword);

    public static SyntaxTokenList InternalStaticPartialList { get; } = SyntaxFactory.TokenList(InternalKeyword, StaticKeyword, PartialKeyword);

    public static SyntaxTokenList InternalPartialTokenList { get; } = SyntaxFactory.TokenList(InternalKeyword, PartialKeyword);

    public static SyntaxTokenList PartialTokenList { get; } = SyntaxFactory.TokenList(PartialKeyword);

    public static SyntaxTokenList PrivateTokenList { get; } = SyntaxFactory.TokenList(PrivateKeyword);

    public static SyntaxTokenList PrivateProtectedTokenList { get; } = SyntaxFactory.TokenList(PrivateKeyword, ProtectedKeyword);

    public static SyntaxTokenList PrivateStaticExternTokenList { get; } = SyntaxFactory.TokenList(PrivateKeyword, StaticKeyword, ExternKeyword);

    public static SyntaxTokenList ProtectedTokenList { get; } = SyntaxFactory.TokenList(ProtectedKeyword);

    public static SyntaxTokenList ProtectedInternalTokenList { get; } = SyntaxFactory.TokenList(ProtectedKeyword, InternalKeyword);

    public static SyntaxTokenList PublicTokenList { get; } = SyntaxFactory.TokenList(PublicKeyword);

    public static SyntaxTokenList PublicConstList { get; } = SyntaxFactory.TokenList(PublicKeyword, ConstKeyword);

    public static SyntaxTokenList PublicStaticList { get; } = SyntaxFactory.TokenList(PublicKeyword, StaticKeyword);

    public static SyntaxTokenList PublicStaticPartialList { get; } = SyntaxFactory.TokenList(PublicKeyword, StaticKeyword, PartialKeyword);

    public static SyntaxTokenList ThisList { get; } = SyntaxFactory.TokenList(ThisKeyword);

    public static SyntaxTriviaList NullableEnableList { get; } = SyntaxFactory.TriviaList(SyntaxFactory.Trivia(SyntaxFactory.NullableDirectiveTrivia(SyntaxFactory.Token(SyntaxKind.EnableKeyword), true)));

    public static AccessorListSyntax GetAndSetAccessorList { get; } = SyntaxFactory.AccessorList(SyntaxFactory.List(
    [
        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SemicolonToken),
        SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SemicolonToken)
    ]));

    public static AssignmentExpressionSyntax SimpleAssignmentExpression(ExpressionSyntax left, ExpressionSyntax right)
    {
        return SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, left, right);
    }

    public static BinaryExpressionSyntax BitwiseOrExpression(ExpressionSyntax left, ExpressionSyntax right)
    {
        return SyntaxFactory.BinaryExpression(SyntaxKind.BitwiseOrExpression, left, right);
    }

    public static BinaryExpressionSyntax CoalesceExpression(ExpressionSyntax left, ExpressionSyntax right)
    {
        return SyntaxFactory.BinaryExpression(SyntaxKind.CoalesceExpression, left, right);
    }

    public static ConstructorDeclarationSyntax WithEmptyBlockBody(this ConstructorDeclarationSyntax constructor)
    {
        return constructor.WithBody(EmptyBlock);
    }

    public static ConstructorInitializerSyntax BaseConstructorInitializer(ArgumentListSyntax? argumentList = null)
    {
        return SyntaxFactory.ConstructorInitializer(SyntaxKind.BaseConstructorInitializer, argumentList);
    }

    public static FileScopedNamespaceDeclarationSyntax FileScopedNamespaceDeclaration(string qualifiedName)
    {
        return SyntaxFactory.FileScopedNamespaceDeclaration(SyntaxFactory.ParseName(qualifiedName));
    }

    public static FileScopedNamespaceDeclarationSyntax FileScopedNamespaceDeclaration(INamespaceSymbol symbol)
    {
        return SyntaxFactory.FileScopedNamespaceDeclaration(SyntaxFactory.ParseName(symbol.ToDisplayString()));
    }

    public static InvocationExpressionSyntax NameOf(ExpressionSyntax argument)
    {
        return SyntaxFactory.InvocationExpression(NameOfIdentifier, SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(argument))));
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

    public static ParameterSyntax Parameter(TypeSyntax type, SyntaxToken name)
    {
        return SyntaxFactory.Parameter(name).WithType(type);
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

        return typeDeclaration.WithModifiers(PartialTokenList);
    }

    public static UsingDirectiveSyntax UsingDirective(string qualifiedName)
    {
        return SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(qualifiedName));
    }
}