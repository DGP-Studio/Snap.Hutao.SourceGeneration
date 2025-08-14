// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System;

namespace Snap.Hutao.SourceGeneration.Extension;

internal static class Functions<T>
{
    public static readonly Func<T, T> Identity = static t => t;
    public static readonly Func<T, bool> True = static t => true;
}