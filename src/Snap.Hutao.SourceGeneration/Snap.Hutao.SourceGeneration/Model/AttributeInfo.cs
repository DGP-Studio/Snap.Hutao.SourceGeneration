﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snap.Hutao.SourceGeneration.Extension;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Snap.Hutao.SourceGeneration.Model;

internal sealed record AttributeInfo
{
    private AttributeInfo(
        string fullyQualifiedTypeName,
        EquatableArray<TypeArgumentInfo> typeArguments,
        EquatableArray<TypedConstantInfo> constructorArguments,
        EquatableArray<(string Name, TypedConstantInfo Value)> namedArguments)
    {
        FullyQualifiedTypeName = fullyQualifiedTypeName;
        TypeArguments = typeArguments;
        ConstructorArguments = constructorArguments;
        NamedArguments = namedArguments;
    }

    public string FullyQualifiedTypeName { get; }

    public EquatableArray<TypeArgumentInfo> TypeArguments { get; }

    public EquatableArray<TypedConstantInfo> ConstructorArguments { get; }

    public EquatableArray<(string Name, TypedConstantInfo Value)> NamedArguments { get; }

    public static AttributeInfo Create(AttributeData attributeData)
    {
        string typeName = attributeData.AttributeClass!.GetFullyQualifiedName();

        return new(attributeData.AttributeClass!.GetFullyQualifiedName(),
            ImmutableArray.CreateRange(attributeData.AttributeClass!.TypeArguments, TypeArgumentInfo.Create),
            ImmutableArray.CreateRange(attributeData.ConstructorArguments, TypedConstantInfo.Create),
            ImmutableArray.CreateRange(attributeData.NamedArguments, static kvp => (kvp.Key, TypedConstantInfo.Create(kvp.Value))));
    }

    public AttributeSyntax GetSyntax()
    {
        // Gather the constructor arguments
        IEnumerable<AttributeArgumentSyntax> arguments =
            ConstructorArguments
                .Select(static arg => AttributeArgument(arg.GetSyntax()));

        // Gather the named arguments
        IEnumerable<AttributeArgumentSyntax> namedArguments =
            NamedArguments.Select(static arg =>
                AttributeArgument(arg.Value.GetSyntax())
                    .WithNameEquals(NameEquals(IdentifierName(arg.Name))));

        return Attribute(IdentifierName(FullyQualifiedTypeName), AttributeArgumentList(SeparatedList([.. arguments, .. namedArguments])));
    }

    public bool TryGetTypeArgument(int index, [NotNullWhen(true)] out TypeArgumentInfo? result)
    {
        if (TypeArguments.AsImmutableArray().Length > index)
        {
            result = TypeArguments[index];
            return true;
        }

        result = default;
        return false;
    }

    public bool TryGetConstructorArgument(int index, [NotNullWhen(true)] out string? result)
    {
        if (ConstructorArguments.AsImmutableArray().Length > index &&
            ConstructorArguments[index] is TypedConstantInfo.Primitive.String argument)
        {
            result = argument.Value;
            return true;
        }

        result = default;
        return false;
    }

    public bool TryGetConstructorArgument(int index, [NotNullWhen(true)] out TypedConstantInfo? result)
    {
        if (ConstructorArguments.AsImmutableArray().Length > index)
        {
            result = ConstructorArguments[index];
            return true;
        }

        result = default;
        return false;
    }

    public bool TryGetNamedArgument(string name, [NotNullWhen(true)] out string? result)
    {
        foreach ((string propertyName, TypedConstantInfo constant) in NamedArguments)
        {
            if (propertyName == name && constant is TypedConstantInfo.Primitive.String argument)
            {
                result = argument.Value;
                return true;
            }
        }

        result = default;
        return false;
    }

    public bool TryGetNamedArgument(string name, [NotNullWhen(true)] out TypedConstantInfo? result)
    {
        foreach ((string propertyName, TypedConstantInfo constant) in NamedArguments)
        {
            if (propertyName == name)
            {
                result = constant;
                return true;
            }
        }

        result = default;
        return false;
    }

    public bool HasNamedArgument(string name, bool value)
    {
        foreach ((string propertyName, TypedConstantInfo constant) in NamedArguments)
        {
            if (propertyName == name)
            {
                return constant is TypedConstantInfo.Primitive.Boolean argument && argument.Value == value;
            }
        }

        return false;
    }
}