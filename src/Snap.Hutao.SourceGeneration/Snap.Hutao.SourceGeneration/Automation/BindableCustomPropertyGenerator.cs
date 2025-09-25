// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snap.Hutao.SourceGeneration.Extension;
using Snap.Hutao.SourceGeneration.Model;
using Snap.Hutao.SourceGeneration.Primitive;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.FastSyntaxFactory;
using static Snap.Hutao.SourceGeneration.WellKnownSyntax;

namespace Snap.Hutao.SourceGeneration.Automation;

[Generator(LanguageNames.CSharp)]
internal sealed class BindableCustomPropertyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<BindableCustomPropertyGeneratorContext> provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                WellKnownMetadataNames.ConstructorGeneratedAttribute,
                SyntaxNodeHelper.Is<ClassDeclarationSyntax>,
                BindableCustomPropertyGeneratorContext.Create)
            .Where(context => context is not null);

        context.RegisterSourceOutput(provider, GenerateWrapper);
    }

    private static void GenerateWrapper(SourceProductionContext production, BindableCustomPropertyGeneratorContext context)
    {
        try
        {
            Generate(production, context);
        }
        catch (Exception ex)
        {
            production.AddSource($"Error-{Guid.NewGuid().ToString()}.g.cs", ex.ToString());
        }
    }

    private static void Generate(SourceProductionContext production, BindableCustomPropertyGeneratorContext context)
    {
        TypeSyntax baseType = ParseTypeName("global::Microsoft.UI.Xaml.Data.IBindableCustomPropertyImplementation");
        TypeSyntax bindableCustomPropertyType = ParseTypeName("global::Microsoft.UI.Xaml.Data.BindableCustomProperty");

        CompilationUnitSyntax syntax = context.Hierarchy.GetCompilationUnit(
                [
                    // GetProperty(string)
                    MethodDeclaration(bindableCustomPropertyType, Identifier("GetProperty"))
                        .WithModifiers(PublicTokenList)
                        .WithParameterList(ParameterList(SingletonSeparatedList(
                            Parameter(StringType, Identifier("name")))))
                        .WithBody(Block(SingletonList(
                            ReturnStatement(SwitchExpression(IdentifierName("name"))
                                .WithArms(SeparatedList(GenerateGetPropertySwitchExpressionArms(context.Hierarchy.Hierarchy[0].FullyQualifiedName, context.Properties))))))),
                    MethodDeclaration(bindableCustomPropertyType, Identifier("GetProperty"))
                        .WithModifiers(PublicTokenList)
                        .WithParameterList(ParameterList(SingletonSeparatedList(
                            Parameter(TypeOfSystemType, Identifier("indexParameterType")))))
                ],
                BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(baseType))))
            .NormalizeWhitespace();

        production.AddSource(context.Hierarchy.FileNameHint, syntax.ToFullStringWithHeader());
    }

    private static IEnumerable<SwitchExpressionArmSyntax> GenerateGetPropertySwitchExpressionArms(string typeName, EquatableArray<PropertyInfo> properties)
    {
        foreach (PropertyInfo property in properties)
        {
            if (property.IsIndexer)
            {
                continue;
            }

            bool canRead = property.GetMethodAccessibility is Accessibility.Public;
            bool canWrite = property.SetMethodAccessibility is Accessibility.Public;

            TypeSyntax ownerType = ParseTypeName(typeName);
            TypeSyntax propertyType = ParseTypeName(property.FullyQualifiedTypeName);

            ExpressionSyntax getValue = canRead && !property.IsIndexer
                ? SimpleLambdaExpression(Parameter(Identifier("instance")))
                    .WithModifiers(StaticTokenList)
                    .WithExpressionBody(SimpleMemberAccessExpression(property.IsStatic
                            ? ownerType
                            : ParenthesizedExpression(CastExpression(ownerType, IdentifierName("instance"))),
                        IdentifierName(property.Name)))
                : NullLiteralExpression;

            ExpressionSyntax setValue = canWrite && !property.IsIndexer
                ? ParenthesizedLambdaExpression()
                    .WithModifiers(StaticTokenList)
                    .WithParameterList(ParameterList(SeparatedList(
                    [
                        Parameter(Identifier("instance")),
                        Parameter(Identifier("value"))
                    ])))
                    .WithExpressionBody(SimpleAssignmentExpression(
                        SimpleMemberAccessExpression(property.IsStatic
                                ? ownerType
                                : ParenthesizedExpression(CastExpression(ownerType, IdentifierName("instance"))),
                            IdentifierName(property.Name)),
                        CastExpression(propertyType, IdentifierName("value"))))
                : NullLiteralExpression;

            yield return SwitchExpressionArm(
                ConstantPattern(NameOfExpression(IdentifierName(property.Name))),
                ImplicitObjectCreationExpression()
                    .WithArgumentList(ArgumentList(SeparatedList(
                    [
                        Argument(LiteralExpression(canRead)),                      // canRead
                        Argument(LiteralExpression(canWrite)),                     // canWrite
                        Argument(NameOfExpression(IdentifierName(property.Name))), // name
                        Argument(TypeOfExpression(propertyType)),                  // type
                        Argument(getValue),                                        // getValue
                        Argument(setValue),                                        // setValue
                        Argument(NullLiteralExpression),                           // getIndexedValue
                        Argument(NullLiteralExpression)                            // setIndexedValue
                    ]))));
        }
    }

    private sealed record BindableCustomPropertyGeneratorContext
    {
        public required AttributeInfo Attribute { get; init; }

        public required HierarchyInfo Hierarchy { get; init; }

        public required EquatableArray<PropertyInfo> Properties { get; init; }

        public static BindableCustomPropertyGeneratorContext Create(GeneratorAttributeSyntaxContext context, CancellationToken token)
        {
            if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
            {
                return default!;
            }

            ImmutableArray<PropertyInfo>.Builder propertiesBuilder = ImmutableArray.CreateBuilder<PropertyInfo>();

            for (INamedTypeSymbol? currentSymbol = typeSymbol; currentSymbol is not null; currentSymbol = currentSymbol.BaseType)
            {
                propertiesBuilder.AddRange(currentSymbol
                    .GetMembers()
                    .Where(member => member.Kind is SymbolKind.Property && member.DeclaredAccessibility is Accessibility.Public)
                    .Cast<IPropertySymbol>()
                    .Select(PropertyInfo.Create));
            }


            return new()
            {
                Attribute = AttributeInfo.Create(context.Attributes.Single()),
                Hierarchy = HierarchyInfo.Create(typeSymbol),
                Properties = propertiesBuilder.ToImmutable(),
            };
        }
    }
}