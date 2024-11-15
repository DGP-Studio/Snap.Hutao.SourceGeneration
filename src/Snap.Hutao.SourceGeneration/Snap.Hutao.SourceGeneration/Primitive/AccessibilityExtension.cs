// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Snap.Hutao.SourceGeneration.Primitive;

internal static class AccessibilityExtension
{
    public static string ToCSharpString(this Accessibility accessibility)
    {
        return accessibility switch
        {
            Accessibility.NotApplicable => string.Empty,
            Accessibility.Private => "private",
            Accessibility.ProtectedAndInternal => "protected internal",
            Accessibility.Protected => "protected",
            Accessibility.Internal => "internal",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.Public => "public",
            _ => throw new ArgumentOutOfRangeException(nameof(accessibility), accessibility, null)
        };
    }
}