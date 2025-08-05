// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace Snap.Hutao.SourceGeneration.Primitive;

internal static class AttributeDataExtension
{
    public static bool HasNamedArgumentWith<TValue>(this AttributeData data, string key, Func<TValue, bool> predicate)
    {
        foreach ((string name, TypedConstant constant) in data.NamedArguments)
        {
            if (name == key && constant.Value is TValue typedValue && predicate(typedValue))
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasNamedArgumentWith(this AttributeData data, string key, bool value)
    {
        foreach ((string name, TypedConstant constant) in data.NamedArguments)
        {
            if (name == key && constant.Value is bool typedValue && typedValue == value)
            {
                return true;
            }
        }

        return false;
    }

    public static bool TryGetNamedArgumentValue(this AttributeData data, string key, out TypedConstant value)
    {
        foreach (KeyValuePair<string, TypedConstant> pair in data.NamedArguments)
        {
            if (pair.Key == key)
            {
                value = pair.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}