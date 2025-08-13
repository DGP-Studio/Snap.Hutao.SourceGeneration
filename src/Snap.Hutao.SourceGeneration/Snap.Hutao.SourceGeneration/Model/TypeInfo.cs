// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snap.Hutao.SourceGeneration.Extension;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.SyntaxKeywords;

namespace Snap.Hutao.SourceGeneration.Model;

internal sealed record TypeInfo
{
    public TypeInfo(INamedTypeSymbol symbol)
    {
        FullyQualifiedName = symbol.GetFullyQualifiedName();
        MinimallyQualifiedName = symbol.GetMinimallyQualifiedName();
        Kind = symbol.TypeKind;
        IsRecord = symbol.IsRecord;
    }

    public string FullyQualifiedName { get; }

    public string MinimallyQualifiedName { get; }

    public TypeKind Kind { get; }

    public bool IsRecord { get; }

    public TypeDeclarationSyntax GetSyntax()
    {
        // Create the partial type declaration with the kind.
        // This code produces a class declaration as follows:
        //
        // <TYPE_KIND> <TYPE_NAME>
        // {
        // }
        //
        // Note that specifically for record declarations, we also need to explicitly add the open
        // and close brace tokens, otherwise member declarations will not be formatted correctly.
        return (Kind, IsRecord) switch
        {
            (TypeKind.Struct, false) => StructDeclaration(MinimallyQualifiedName),
            (TypeKind.Struct, true) => RecordDeclaration(RecordKeyword, MinimallyQualifiedName)
                .WithClassOrStructKeyword(StructKeyword)
                .WithOpenBraceToken(Token(SyntaxKind.OpenBraceToken))
                .WithCloseBraceToken(Token(SyntaxKind.CloseBraceToken)),
            (TypeKind.Interface, _) => InterfaceDeclaration(MinimallyQualifiedName),
            (TypeKind.Class, true) => RecordDeclaration(RecordKeyword, MinimallyQualifiedName)
                .WithOpenBraceToken(Token(SyntaxKind.OpenBraceToken))
                .WithCloseBraceToken(Token(SyntaxKind.CloseBraceToken)),
            _ => ClassDeclaration(MinimallyQualifiedName)
        };
    }

    public TypeSyntax GetTypeSyntax()
    {
        return ParseTypeName(FullyQualifiedName);
    }
}