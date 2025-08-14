// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snap.Hutao.SourceGeneration.Extension;
using Snap.Hutao.SourceGeneration.Primitive;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Snap.Hutao.SourceGeneration.Model;

internal sealed record AttributeInfo
{
    private AttributeInfo(
        string fullyQualifiedTypeName,
        EquatableArray<TypedConstantInfo> constructorArgumentInfo,
        EquatableArray<(string Name, TypedConstantInfo Value)> namedArgumentInfo)
    {
        FullyQualifiedTypeName = fullyQualifiedTypeName;
        ConstructorArgumentInfo = constructorArgumentInfo;
        NamedArgumentInfo = namedArgumentInfo;
    }

    public string FullyQualifiedTypeName { get; }

    public EquatableArray<TypedConstantInfo> ConstructorArgumentInfo { get; }

    public EquatableArray<(string Name, TypedConstantInfo Value)> NamedArgumentInfo { get; }

    public static AttributeInfo Create(AttributeData attributeData)
    {
        string typeName = attributeData.AttributeClass!.GetFullyQualifiedName();

        using ImmutableArrayBuilder<TypedConstantInfo> constructorArguments = ImmutableArrayBuilder<TypedConstantInfo>.Rent();
        using ImmutableArrayBuilder<(string, TypedConstantInfo)> namedArguments = ImmutableArrayBuilder<(string, TypedConstantInfo)>.Rent();

        // Get the constructor arguments
        foreach (TypedConstant typedConstant in attributeData.ConstructorArguments)
        {
            constructorArguments.Add(TypedConstantInfo.Create(typedConstant));
        }

        // Get the named arguments
        foreach ((string name, TypedConstant constant) in attributeData.NamedArguments)
        {
            namedArguments.Add((name, TypedConstantInfo.Create(constant)));
        }

        return new(typeName, constructorArguments.ToImmutable(), namedArguments.ToImmutable());
    }

    public AttributeSyntax GetSyntax()
    {
        // Gather the constructor arguments
        IEnumerable<AttributeArgumentSyntax> arguments =
            ConstructorArgumentInfo
                .Select(static arg => AttributeArgument(arg.GetSyntax()));

        // Gather the named arguments
        IEnumerable<AttributeArgumentSyntax> namedArguments =
            NamedArgumentInfo.Select(static arg =>
                AttributeArgument(arg.Value.GetSyntax())
                    .WithNameEquals(NameEquals(IdentifierName(arg.Name))));

        return Attribute(IdentifierName(FullyQualifiedTypeName), AttributeArgumentList(SeparatedList([.. arguments, .. namedArguments])));
    }

    public bool TryGetConstructorArgument(int index, [NotNullWhen(true)] out string? result)
    {
        if (ConstructorArgumentInfo.AsImmutableArray().Length > index &&
            ConstructorArgumentInfo[index] is TypedConstantInfo.Primitive.String argument)
        {
            result = argument.Value;
            return true;
        }

        result = default;
        return false;
    }

    public bool HasNamedArgument(string name, bool value)
    {
        foreach ((string propertyName, TypedConstantInfo constant) in NamedArgumentInfo)
        {
            if (propertyName == name)
            {
                return constant is TypedConstantInfo.Primitive.Boolean argument && argument.Value == value;
            }
        }

        return false;
    }
}