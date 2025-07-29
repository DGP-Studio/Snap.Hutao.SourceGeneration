// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.SourceGeneration.Primitive;

internal static class SpanHelpers
{
    internal static int IndexOfAnyExcept<T>(ref T searchSpace, T value0, int length)
    {
        for (int elementOffset = 0; elementOffset < length; ++elementOffset)
        {
            if (!EqualityComparer<T>.Default.Equals(Unsafe.Add(ref searchSpace, elementOffset), value0))
            {
                return elementOffset;
            }
        }

        return -1;
    }
}