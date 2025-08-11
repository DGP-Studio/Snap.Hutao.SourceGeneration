// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Snap.Hutao.SourceGeneration.Model;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Snap.Hutao.SourceGeneration.Extension;

internal static class IncrementalValuesProviderExtension
{
    public static IncrementalValuesProvider<(TKey Key, EquatableArray<TElement> Right)> GroupBy<TLeft, TRight, TKey, TElement>(
        this IncrementalValuesProvider<(TLeft Left, TRight Right)> source,
        Func<(TLeft Left, TRight Right), TKey> keySelector,
        Func<(TLeft Left, TRight Right), TElement> elementSelector)
        where TLeft : IEquatable<TLeft>
        where TRight : IEquatable<TRight>
        where TKey : IEquatable<TKey>
        where TElement : IEquatable<TElement>
    {
        return source.Collect().SelectMany((item, token) =>
        {
            Dictionary<TKey, ImmutableArray<TElement>.Builder> map = new();

            foreach ((TLeft, TRight) pair in item)
            {
                TKey key = keySelector(pair);
                TElement element = elementSelector(pair);

                if (!map.TryGetValue(key, out ImmutableArray<TElement>.Builder builder))
                {
                    builder = ImmutableArray.CreateBuilder<TElement>();

                    map.Add(key, builder);
                }

                builder.Add(element);
            }

            token.ThrowIfCancellationRequested();

            ImmutableArray<(TKey Key, EquatableArray<TElement> Elements)>.Builder result =
                ImmutableArray.CreateBuilder<(TKey, EquatableArray<TElement>)>();

            foreach (KeyValuePair<TKey, ImmutableArray<TElement>.Builder> entry in map)
            {
                result.Add((entry.Key, entry.Value.ToImmutable()));
            }

            return result;
        });
    }
}