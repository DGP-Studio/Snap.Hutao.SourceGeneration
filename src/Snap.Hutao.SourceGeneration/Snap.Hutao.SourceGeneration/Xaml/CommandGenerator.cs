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
using System.Threading;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.FastSyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.SyntaxKeywords;

namespace Snap.Hutao.SourceGeneration.Xaml;

[Generator(LanguageNames.CSharp)]
internal sealed class CommandGenerator : IIncrementalGenerator
{
    private static readonly NameSyntax NameOfCommunityToolkitMvvmInput = ParseName("global::CommunityToolkit.Mvvm.Input");
    private static readonly NameSyntax NameOfCommunityToolkitMvvmInputAsyncRelayCommandOptions = ParseName("global::CommunityToolkit.Mvvm.Input.AsyncRelayCommandOptions");
    private static readonly NameSyntax NameOfSystemDiagnosticsCodeAnalysisMaybeNull = ParseName("global::System.Diagnostics.CodeAnalysis.MaybeNull");

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<CommandGeneratorContext> provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                WellKnownMetadataNames.CommandAttribute,
                SyntaxNodeHelper.Is<MethodDeclarationSyntax>,
                Transform)
            .GroupBy(t => t.Left, t => AttributedMethodInfo.Create(t.Right))
            .Select(CommandGeneratorContext.Create);

        context.RegisterSourceOutput(provider, GenerateWrapper);
    }

    private static (HierarchyInfo Hierarchy, (EquatableArray<AttributeInfo> Attribute, MethodInfo Method)) Transform(GeneratorAttributeSyntaxContext context, CancellationToken token)
    {
        if (context.TargetSymbol is IMethodSymbol { ContainingType: { } typeSymbol } methodSymbol)
        {
            return (HierarchyInfo.Create(typeSymbol), (ImmutableArray.CreateRange(context.Attributes, AttributeInfo.Create),  MethodInfo.Create(methodSymbol)));
        }

        return default;
    }

    private static void GenerateWrapper(SourceProductionContext production, CommandGeneratorContext context)
    {
        try
        {
            Generate(production, context);
        }
        catch (Exception e)
        {
            production.AddSource($"Error-{Guid.NewGuid().ToString()}.g.cs", e.ToString());
        }
    }

    private static void Generate(SourceProductionContext production, CommandGeneratorContext context)
    {
        CompilationUnitSyntax syntax = context.Hierarchy.GetCompilationUnit([.. GenerateCommandProperties(context.Methods)])
            .NormalizeWhitespace();

        production.AddSource(context.Hierarchy.FileNameHint, syntax.ToFullString());
    }

    private static IEnumerable<MemberDeclarationSyntax> GenerateCommandProperties(EquatableArray<AttributedMethodInfo> methods)
    {
        foreach (AttributedMethodInfo attributedMethod in methods)
        {
            bool isAsync = attributedMethod.Method.FullyQualifiedReturnTypeMetadataName.StartsWith("System.Threading.Tasks.Task");
            SyntaxToken identifier = Identifier(isAsync ? "AsyncRelayCommand" : "RelayCommand");

            TypeSyntax propertyType;
            ImmutableArray<ParameterInfo> parameters = attributedMethod.Method.Parameters;
            if (parameters.Length >= 1)
            {
                TypeSyntax type = ParseTypeName(parameters[0].FullyQualifiedTypeName);
                propertyType = QualifiedName(NameOfCommunityToolkitMvvmInput, GenericName(identifier).WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList(type))));
            }
            else
            {
                propertyType = QualifiedName(NameOfCommunityToolkitMvvmInput, IdentifierName(identifier));
            }

            foreach (AttributeInfo attribute in attributedMethod.Attributes)
            {
                if (!attribute.TryGetConstructorArgument(0, out string? commandName))
                {
                    continue;
                }

                SeparatedSyntaxList<ArgumentSyntax> arguments = SingletonSeparatedList(
                    Argument(IdentifierName(attributedMethod.Method.MinimallyQualifiedName)));

                if (attribute.HasNamedArgument("AllowConcurrentExecutions", true))
                {
                    arguments = arguments.Add(Argument(SimpleMemberAccessExpression(
                        NameOfCommunityToolkitMvvmInputAsyncRelayCommandOptions,
                        IdentifierName("AllowConcurrentExecutions"))));
                }

                yield return PropertyDeclaration(propertyType, commandName)
                    .WithAttributeLists(SingletonList(
                        AttributeList(SingletonSeparatedList(
                            Attribute(NameOfSystemDiagnosticsCodeAnalysisMaybeNull)))
                            .WithTarget(AttributeTargetSpecifier(FieldKeyword))))
                    .WithModifiers(PublicTokenList)
                    .WithAccessorList(AccessorList(SingletonList(
                        GetAccessorDeclaration()
                            .WithExpressionBody(ArrowExpressionClause(CoalesceAssignmentExpression(
                                FieldExpression(),
                                ImplicitObjectCreationExpression()
                                    .WithArgumentList(ArgumentList(arguments))))))));
            }
        }
    }

    private sealed record CommandGeneratorContext
    {
        private CommandGeneratorContext(HierarchyInfo hierarchy, EquatableArray<AttributedMethodInfo> methods)
        {
            Hierarchy = hierarchy;
            Methods = methods;
        }

        public HierarchyInfo Hierarchy { get; }

        public EquatableArray<AttributedMethodInfo> Methods { get; }

        public static CommandGeneratorContext Create((HierarchyInfo Hierarchy, EquatableArray<AttributedMethodInfo> Methods) tuple, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return new(tuple.Hierarchy, tuple.Methods);
        }
    }

    private sealed record AttributedMethodInfo
    {
        private AttributedMethodInfo(EquatableArray<AttributeInfo> attributes, MethodInfo method)
        {
            Attributes = attributes;
            Method = method;
        }

        public EquatableArray<AttributeInfo> Attributes { get; }

        public MethodInfo Method { get; }

        public static AttributedMethodInfo Create((EquatableArray<AttributeInfo> Attributes, MethodInfo Method) tuple)
        {
            return new AttributedMethodInfo(tuple.Attributes, tuple.Method);
        }
    }
}