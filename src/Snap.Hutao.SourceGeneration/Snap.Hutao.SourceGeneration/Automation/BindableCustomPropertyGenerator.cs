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
                WellKnownMetadataNames.BindableCustomPropertyProviderAttribute,
                SyntaxNodeHelper.Is<ClassDeclarationSyntax>,
                BindableCustomPropertyGeneratorContext.Create)
            .Where(context => context is not null);

        context.RegisterImplementationSourceOutput(provider, GenerateWrapper);
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

        TypeSyntax type = ParseTypeName(context.Hierarchy.Hierarchy[0].FullyQualifiedName);

        CompilationUnitSyntax syntax = context.Hierarchy.GetCompilationUnit(
                [
                    // GetProperty(string)
                    MethodDeclaration(NullableType(bindableCustomPropertyType), Identifier("GetProperty"))
                        .WithModifiers(PublicTokenList)
                        .WithParameterList(ParameterList(SingletonSeparatedList(
                            Parameter(StringType, Identifier("name")))))
                        .WithBody(Block(SingletonList(
                            ReturnStatement(SwitchExpression(IdentifierName("name"))
                                .WithArms(SeparatedList(GenerateGetPropertySwitchExpressionArms(type, context.Properties))))))),

                    // GetProperty(Type)
                    MethodDeclaration(NullableType(bindableCustomPropertyType), Identifier("GetProperty"))
                        .WithModifiers(PublicTokenList)
                        .WithParameterList(ParameterList(SingletonSeparatedList(
                            Parameter(TypeOfSystemType, Identifier("indexParameterType")))))
                        .WithBody(Block(List(GenerateGetIndexerStatements(type, context.Properties))))
                ],
                BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(baseType))))
            .NormalizeWhitespace();

        production.AddSource(context.Hierarchy.FileNameHint, syntax.ToFullStringWithHeader());
    }

    private static IEnumerable<SwitchExpressionArmSyntax> GenerateGetPropertySwitchExpressionArms(TypeSyntax ownerType, EquatableArray<PropertyInfo> properties)
    {
        foreach (PropertyInfo property in properties)
        {
            if (property.IsIndexer)
            {
                continue;
            }

            bool canRead = property.GetMethodAccessibility is Accessibility.Public;
            bool canWrite = property.SetMethodAccessibility is Accessibility.Public;

            TypeSyntax propertyType = ParseTypeName(property.FullyQualifiedTypeName);

            ExpressionSyntax getValue = canRead
                ? SimpleLambdaExpression(Parameter(Identifier("instance")))
                    .WithModifiers(StaticTokenList)
                    .WithExpressionBody(SimpleMemberAccessExpression(property.IsStatic
                            ? ownerType
                            : ParenthesizedExpression(CastExpression(ownerType, IdentifierName("instance"))),
                        IdentifierName(property.Name)))
                : DefaultLiteralExpression;

            ExpressionSyntax setValue = canWrite
                ? ParenthesizedLambdaExpression()
                    .WithModifiers(StaticTokenList)
                    .WithParameterList(ParameterList(SeparatedList(
                    [
                        Parameter(Identifier("instance")),
                        Parameter(Identifier("value"))
                    ])))
                    .WithBody(Block(List<StatementSyntax>(
                    [
                        LocalDeclarationStatement(VariableDeclaration(ownerType, SingletonSeparatedList(VariableDeclarator("typedInstance")
                            .WithInitializer(EqualsValueClause(CastExpression(ownerType, IdentifierName("instance"))))))),
                        IfStatement(
                            SimpleMemberAccessExpression(SimpleMemberAccessExpression(IdentifierName("typedInstance"), IdentifierName("IsViewUnloaded")), IdentifierName("Value")),
                            Block(ReturnStatement())),
                        ExpressionStatement(SimpleAssignmentExpression(
                            SimpleMemberAccessExpression(
                                property.IsStatic ? ownerType : IdentifierName("typedInstance"),
                                IdentifierName(property.Name)),
                            CastExpression(propertyType, IdentifierName("value"))))
                    ])))
                : DefaultLiteralExpression;

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
                        Argument(DefaultLiteralExpression),                        // getIndexedValue
                        Argument(DefaultLiteralExpression)                         // setIndexedValue
                    ]))));
        }

        yield return SwitchExpressionArm(
            DiscardPattern(),
            DefaultLiteralExpression);
    }

    private static IEnumerable<StatementSyntax> GenerateGetIndexerStatements(TypeSyntax ownerType, EquatableArray<PropertyInfo> properties)
    {
        foreach (PropertyInfo property in properties)
        {
            if (!property.IsIndexer)
            {
                continue;
            }

            bool canRead = property.GetMethodAccessibility is Accessibility.Public;
            bool canWrite = property.SetMethodAccessibility is Accessibility.Public;

            TypeSyntax propertyType = ParseTypeName(property.FullyQualifiedTypeName);
            TypeSyntax indexerType = ParseTypeName(property.FullyQualifiedIndexerParameterTypeName!);

            ExpressionSyntax getIndexedValue = canRead
                ? ParenthesizedLambdaExpression()
                    .WithModifiers(StaticTokenList)
                    .WithParameterList(ParameterList(SeparatedList(
                    [
                        Parameter(Identifier("instance")),
                        Parameter(Identifier("index"))
                    ])))
                    .WithExpressionBody(ElementAccessExpression(
                        ParenthesizedExpression(CastExpression(ownerType, IdentifierName("instance"))),
                        BracketedArgumentList(SingletonSeparatedList(
                            Argument(CastExpression(indexerType, IdentifierName("index")))))))
                : NullLiteralExpression;

            ExpressionSyntax setIndexedValue = canWrite
                ? ParenthesizedLambdaExpression()
                    .WithModifiers(StaticTokenList)
                    .WithParameterList(ParameterList(SeparatedList(
                    [
                        Parameter(Identifier("instance")),
                        Parameter(Identifier("value")),
                        Parameter(Identifier("index"))
                    ])))
                    .WithBody(Block(List<StatementSyntax>(
                    [
                        LocalDeclarationStatement(VariableDeclaration(ownerType, SingletonSeparatedList(VariableDeclarator("typedInstance")
                            .WithInitializer(EqualsValueClause(CastExpression(ownerType, IdentifierName("instance"))))))),
                        IfStatement(
                            SimpleMemberAccessExpression(SimpleMemberAccessExpression(IdentifierName("typedInstance"), IdentifierName("IsViewUnloaded")), IdentifierName("Value")),
                            Block(ReturnStatement())),
                        ExpressionStatement(SimpleAssignmentExpression(ElementAccessExpression(
                                IdentifierName("typedInstance"),
                                BracketedArgumentList(SingletonSeparatedList(
                                    Argument(CastExpression(indexerType, IdentifierName("index")))))),
                            CastExpression(propertyType, IdentifierName("value"))))
                    ])))
                : NullLiteralExpression;

            yield return IfStatement(
                EqualsExpression(
                    IdentifierName("indexParameterType"),
                    TypeOfExpression(indexerType)),
                Block(SingletonList(
                    ReturnStatement(ImplicitObjectCreationExpression()
                        .WithArgumentList(ArgumentList(SeparatedList(
                        [
                            Argument(LiteralExpression(canRead)),      // canRead
                            Argument(LiteralExpression(canWrite)),     // canWrite
                            Argument(StringLiteralExpression("Item")), // name
                            Argument(TypeOfExpression(propertyType)),  // type
                            Argument(DefaultLiteralExpression),        // getValue
                            Argument(DefaultLiteralExpression),        // setValue
                            Argument(getIndexedValue),                 // getIndexedValue
                            Argument(setIndexedValue)                  // setIndexedValue
                        ])))))));
        }

        yield return ReturnStatement(DefaultLiteralExpression);
    }

    private sealed record BindableCustomPropertyGeneratorContext
    {
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
                Hierarchy = HierarchyInfo.Create(typeSymbol),
                Properties = propertiesBuilder.ToImmutable(),
            };
        }
    }
}