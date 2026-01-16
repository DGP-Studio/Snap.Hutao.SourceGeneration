// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snap.Hutao.SourceGeneration.Extension;
using Snap.Hutao.SourceGeneration.Model;
using System;
using System.Linq;
using System.Threading;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.FastSyntaxFactory;

namespace Snap.Hutao.SourceGeneration.Automation;

[Generator(LanguageNames.CSharp)]
internal sealed class UnmanagedFunctionPointerGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<UnmanagedFunctionPointerContext> provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                WellKnownMetadataNames.GeneratedUnmanagedFunctionPointerAttribute,
                static (a, b) => true, // SyntaxNodeHelper.Is<FieldDeclarationSyntax>,
                UnmanagedFunctionPointerContext.Create);

        context.RegisterSourceOutput(provider, GenerateWrapper);
    }

    private static void GenerateWrapper(SourceProductionContext production, UnmanagedFunctionPointerContext context)
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

    private static void Generate(SourceProductionContext production, UnmanagedFunctionPointerContext context)
    {
        SyntaxToken name = Identifier(context.FieldInfo.Name);
        IdentifierNameSyntax nameSyntax = IdentifierName(context.FieldInfo.Name);
        TypeSyntax type = ParseTypeName(context.FieldInfo.FullyQualifiedTypeNameWithNullabilityAnnotation);

        CompilationUnitSyntax syntax = context.Hierarchy.GetCompilationUnit(
        [
            ConstructorDeclaration(Identifier(context.Hierarchy.Hierarchy[0].Name))
                .WithModifiers(PublicUnsafeTokenList)
                .WithParameterList(ParameterList(SingletonSeparatedList(
                    Parameter(type, name))))
                .WithBody(Block(List(
                    [
                        // this.${fieldName} = value;
                        ExpressionStatement(SimpleAssignmentExpression(
                            SimpleMemberAccessExpression(
                                ThisExpression(),
                                IdentifierName(context.FieldInfo.Name)),
                            nameSyntax))
                    ]))),

            MethodDeclaration(context.Hierarchy.Hierarchy[0].GetTypeSyntax(), Identifier("Create"))
                .WithModifiers(PublicStaticUnsafeTokenList)
                .WithParameterList(ParameterList(SingletonSeparatedList(
                    Parameter(type, name))))
                .WithBody(Block(List(
                    [
                        // return new(${fieldName});
                        ReturnStatement(
                            ImplicitObjectCreationExpression(
                                ArgumentList(SingletonSeparatedList(
                                    Argument(nameSyntax))),
                                default))
                    ])))
        ]).NormalizeWhitespace();

        production.AddSource(context.Hierarchy.FileNameHint, syntax.ToFullStringWithHeader());
    }

    private sealed record UnmanagedFunctionPointerContext
    {
        public required AttributeInfo Attribute { get; init; }

        public required HierarchyInfo Hierarchy { get; init; }

        public required FieldInfo FieldInfo { get; init; }

        public static UnmanagedFunctionPointerContext Create(GeneratorAttributeSyntaxContext context, CancellationToken token)
        {
            if (context.TargetSymbol is not IFieldSymbol fieldSymbol)
            {
                return default!;
            }

            return new()
            {
                Attribute = AttributeInfo.Create(context.Attributes.Single()),
                Hierarchy = HierarchyInfo.Create(fieldSymbol.ContainingType),
                FieldInfo = FieldInfo.Create(fieldSymbol),
            };
        }
    }
}