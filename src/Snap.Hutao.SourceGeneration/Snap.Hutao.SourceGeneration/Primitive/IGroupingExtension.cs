// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace Snap.Hutao.SourceGeneration.Primitive;

internal static class IGroupingExtension
{
    public static void Deconstruct<TKey, TElement>(this IGrouping<TKey, TElement> grouping, out TKey key, out IEnumerable<TElement> elements)
    {
        key = grouping.Key;
        elements = grouping;
    }
}