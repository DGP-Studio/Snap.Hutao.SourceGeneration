// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snap.Hutao.SourceGeneration.Primitive;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.FastSyntaxFactory;

namespace Snap.Hutao.SourceGeneration.DependencyInjection;

[Generator(LanguageNames.CSharp)]
internal sealed class ServiceGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor InvalidServiceLifetimeDescriptor = new("SH101", "不支持的 ServiceLifetime 枚举值", "不支持生成 {0} 服务", "Quality", DiagnosticSeverity.Error, true);

    private static readonly TypeSyntax TypeOfMicrosoftExtensionsDependencyInjectionIServiceCollection = ParseTypeName("global::Microsoft.Extensions.DependencyInjection.IServiceCollection");

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<ImmutableArray<GeneratorAttributeSyntaxContext>> provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                WellKnownMetadataNames.ServiceAttribute,
                SyntaxNodeHelper.Is<ClassDeclarationSyntax>,
                SyntaxContext.Transform)
            .Collect();

        context.RegisterImplementationSourceOutput(provider, GenerateWrapper);
    }

    private static void GenerateWrapper(SourceProductionContext production, ImmutableArray<GeneratorAttributeSyntaxContext> contexts)
    {
        try
        {
            Generate(production, contexts);
        }
        catch (Exception e)
        {
            production.AddSource($"Error-{Guid.NewGuid().ToString()}.g.cs", e.ToString());
        }
    }

    private static void Generate(SourceProductionContext production, ImmutableArray<GeneratorAttributeSyntaxContext> contexts)
    {
        if (contexts.IsDefaultOrEmpty)
        {
            return;
        }

        CompilationUnitSyntax syntax = CompilationUnit()
            .WithUsings(SingletonList(UsingDirective("Microsoft.Extensions.DependencyInjection")))
            .WithMembers(SingletonList<MemberDeclarationSyntax>(FileScopedNamespaceDeclaration("Snap.Hutao.Core.DependencyInjection")
                .WithLeadingTrivia(NullableEnableList)
                .WithMembers(SingletonList<MemberDeclarationSyntax>(
                    ClassDeclaration("ServiceCollectionExtension")
                        .WithModifiers(InternalStaticPartialList)
                        .WithMembers(SingletonList<MemberDeclarationSyntax>(
                            MethodDeclaration(TypeOfMicrosoftExtensionsDependencyInjectionIServiceCollection, "AddServices")
                                .WithModifiers(PublicStaticPartialList)
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(TypeOfMicrosoftExtensionsDependencyInjectionIServiceCollection, Identifier("services"))
                                        .WithModifiers(ThisList))))
                                .WithBody(Block(List(GenerateAddServices(production, contexts))))))))))
            .NormalizeWhitespace();

        production.AddSource("ServiceCollectionExtension.g.cs", syntax.ToFullString());
    }

    private static IEnumerable<StatementSyntax> GenerateAddServices(SourceProductionContext production, ImmutableArray<GeneratorAttributeSyntaxContext> contexts)
    {
        foreach (GeneratorAttributeSyntaxContext context in contexts)
        {
            if (context.TargetSymbol is not INamedTypeSymbol targetSymbol)
            {
                continue;
            }

            if (context.Attributes.Single() is not { } service)
            {
                continue;
            }

            TypeSyntax targetType = ParseTypeName(targetSymbol.GetFullyQualifiedName());
            SeparatedSyntaxList<TypeSyntax> typeArguments = SingletonSeparatedList(targetType);
            if (service.TryGetConstructorArgument(1, out ITypeSymbol? type)) // [Service(serviceLifetime, typeof(T))]
            {
                typeArguments = typeArguments.Insert(0, ParseTypeName(type.GetFullyQualifiedName()));
            }

            string? serviceLifetime = service.ConstructorArguments[0].ToCSharpString() switch
            {
                WellKnownMetadataNames.ServiceLifetimeSingleton => "Singleton",
                WellKnownMetadataNames.ServiceLifetimeScoped => "Scoped",
                WellKnownMetadataNames.ServiceLifetimeTransient => "Transient",
                _ => default
            };

            if (string.IsNullOrEmpty(serviceLifetime))
            {
                production.ReportDiagnostic(Diagnostic.Create(InvalidServiceLifetimeDescriptor, context.TargetNode.GetLocation(), service.ConstructorArguments[0].ToCSharpString()));
                continue;
            }

            bool hasKey = service.TryGetNamedArgument("Key", out TypedConstant key);

            ArgumentListSyntax argumentList = hasKey
                ? ArgumentList(SingletonSeparatedList(
                    Argument(TypedConstantInfo.Create(key).GetSyntax())))
                : EmptyArgumentList;

            InvocationExpressionSyntax invocation = InvocationExpression(
                    SimpleMemberAccessExpression(
                        IdentifierName("services"),
                        GenericName($"Add{(hasKey ? "Keyed" : string.Empty)}{serviceLifetime}")
                            .WithTypeArgumentList(TypeArgumentList(typeArguments))))
                .WithArgumentList(argumentList);

            yield return ExpressionStatement(invocation);
        }

        yield return ReturnStatement(IdentifierName("services"));
    }
}