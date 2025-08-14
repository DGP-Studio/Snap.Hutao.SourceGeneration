// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snap.Hutao.SourceGeneration.Extension;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Snap.Hutao.SourceGeneration.Model;

internal sealed record AttributeInfo
{
    public AttributeInfo(string fullyQualifiedTypeName, EquatableArray<TypedConstantInfo> constructorArgumentInfo, EquatableArray<(string Name, TypedConstantInfo Value)> namedArgumentInfo)
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
        foreach (KeyValuePair<string, TypedConstant> namedConstant in attributeData.NamedArguments)
        {
            namedArguments.Add((namedConstant.Key, TypedConstantInfo.Create(namedConstant.Value)));
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