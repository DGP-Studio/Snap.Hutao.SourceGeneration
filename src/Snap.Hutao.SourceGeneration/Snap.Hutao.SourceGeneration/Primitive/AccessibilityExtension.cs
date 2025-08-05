// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.FastSyntaxFactory;

namespace Snap.Hutao.SourceGeneration.Primitive;

internal static class AccessibilityExtension
{
    public static string ToCSharpString(this Accessibility accessibility)
    {
        return accessibility switch
        {
            Accessibility.NotApplicable => string.Empty,
            Accessibility.Private => "private",
            Accessibility.ProtectedAndInternal => "private protected",
            Accessibility.Protected => "protected",
            Accessibility.Internal => "internal",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.Public => "public",
            _ => throw new ArgumentOutOfRangeException(nameof(accessibility), accessibility, "Unknown accessibility")
        };
    }

    public static SyntaxTokenList ToSyntaxTokenList(this Accessibility accessibility, SyntaxToken additionalToken)
    {
        SyntaxTokenList list = accessibility switch
        {
            Accessibility.NotApplicable => TokenList(),
            Accessibility.Private => PrivateTokenList,
            Accessibility.ProtectedAndInternal => PrivateProtectedTokenList,
            Accessibility.Protected => ProtectedTokenList,
            Accessibility.Internal => InternalTokenList,
            Accessibility.ProtectedOrInternal => ProtectedInternalTokenList,
            Accessibility.Public => PublicTokenList,
            _ => throw new ArgumentOutOfRangeException(nameof(accessibility), accessibility, "Unknown accessibility")
        };

        return list.Add(additionalToken);
    }
}