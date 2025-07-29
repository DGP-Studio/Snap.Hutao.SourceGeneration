// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Snap.Hutao.SourceGeneration.Primitive;

internal static class SpanExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfAnyExcept<T>(this ReadOnlySpan<T> span, T value) where T : IEquatable<T>?
    {
        return SpanHelpers.IndexOfAnyExcept(ref MemoryMarshal.GetReference(span), value, span.Length);
    }
}