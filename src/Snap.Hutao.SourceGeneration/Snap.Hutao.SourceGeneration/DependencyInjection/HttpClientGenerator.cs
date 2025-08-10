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
internal sealed class HttpClientGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor InjectionShouldOmitDescriptor = new("SH201", "多余的 Injection/Service 特性", "HttpClient 特性已将 {0} 注册为 Transient 服务", "Quality", DiagnosticSeverity.Warning, true);

    private static readonly TypeSyntax TypeOfSystemNetHttpSocketsHttpHandler = ParseTypeName("global::System.Net.Http.SocketsHttpHandler");
    private static readonly TypeSyntax TypeOfMicrosoftExtensionsDependencyInjectionIServiceCollection = ParseTypeName("global::Microsoft.Extensions.DependencyInjection.IServiceCollection");

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<ImmutableArray<GeneratorAttributeSyntaxContext>> provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                WellKnownMetadataNames.HttpClientAttribute,
                SyntaxNodeHelper.Is<ClassDeclarationSyntax>,
                SyntaxContext.Transform)
            .Collect();

        context.RegisterSourceOutput(provider, GenerateWrapper);
    }

    private static void GenerateWrapper(SourceProductionContext production, ImmutableArray<GeneratorAttributeSyntaxContext> contexts)
    {
        try
        {
            Generate(production, contexts);
        }
        catch (Exception e)
        {
            production.AddSource("Error.g.cs", e.ToString());
        }
    }

    private static void Generate(SourceProductionContext production, ImmutableArray<GeneratorAttributeSyntaxContext> contexts)
    {
        if (contexts.IsDefaultOrEmpty)
        {
            return;
        }

        CompilationUnitSyntax syntax = CompilationUnit()
            .WithUsings(SingletonList(UsingDirective("System.Net.Http")))
            .WithMembers(SingletonList<MemberDeclarationSyntax>(FileScopedNamespaceDeclaration("Snap.Hutao.Core.DependencyInjection")
                .WithLeadingTrivia(NullableEnableList())
                .WithMembers(SingletonList<MemberDeclarationSyntax>(
                    ClassDeclaration("ServiceCollectionExtension")
                        .WithModifiers(InternalStaticPartialList)
                        .WithMembers(SingletonList<MemberDeclarationSyntax>(
                            MethodDeclaration(TypeOfMicrosoftExtensionsDependencyInjectionIServiceCollection,"AddHttpClients")
                                .WithModifiers(PublicStaticPartialList)
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(TypeOfMicrosoftExtensionsDependencyInjectionIServiceCollection, Identifier("services"))
                                        .WithModifiers(ThisList))))
                                .WithBody(Block(List(
                                    GenerateAddHttpClients(production, contexts))))))))))
            .NormalizeWhitespace();

        production.AddSource("Snap.Hutao.Core.DependencyInjection.ServiceCollectionExtension.g.cs", syntax.ToFullString());
    }

    private static IEnumerable<StatementSyntax> GenerateAddHttpClients(SourceProductionContext production, ImmutableArray<GeneratorAttributeSyntaxContext> contexts)
    {
        foreach (GeneratorAttributeSyntaxContext context in contexts)
        {
            if (context.TargetSymbol is not INamedTypeSymbol targetSymbol)
            {
                continue;
            }

            if (context.Attributes.Single() is not { } httpClient)
            {
                continue;
            }

            AttributeData? primaryHttpMessageHandler = default;

            // SH201
            foreach (AttributeData attribute in targetSymbol.GetAttributes())
            {
                if (attribute.AttributeClass?.HasFullyQualifiedMetadataName(WellKnownMetadataNames.InjectionAttribute) is true &&
                    attribute.ConstructorArguments.Length < 2 &&
                    attribute.ConstructorArguments[0].ToCSharpString() is WellKnownMetadataNames.InjectAsTransientName &&
                    context.TargetNode is BaseTypeDeclarationSyntax syntaxNode1)
                {
                    production.ReportDiagnostic(Diagnostic.Create(InjectionShouldOmitDescriptor, syntaxNode1.Identifier.GetLocation(), context.TargetNode));
                }

                if (attribute.AttributeClass?.HasFullyQualifiedMetadataName(WellKnownMetadataNames.ServiceAttribute) is true &&
                    attribute.ConstructorArguments.Length < 2 &&
                    attribute.ConstructorArguments[0].ToCSharpString() is WellKnownMetadataNames.ServiceLifetimeTransient &&
                    context.TargetNode is BaseTypeDeclarationSyntax syntaxNode2)
                {
                    production.ReportDiagnostic(Diagnostic.Create(InjectionShouldOmitDescriptor, syntaxNode2.Identifier.GetLocation(), context.TargetNode));
                }

                if (attribute.AttributeClass?.HasFullyQualifiedMetadataName(WellKnownMetadataNames.PrimaryHttpMessageHandlerAttribute) is true)
                {
                    primaryHttpMessageHandler = attribute;
                }
            }

            TypeSyntax targetType = ParseTypeName(targetSymbol.GetFullyQualifiedName());
            SeparatedSyntaxList<TypeSyntax> typeArguments = SingletonSeparatedList(targetType);
            if (httpClient.ConstructorArguments is [_, { Value: ITypeSymbol type } _]) // [HttpClient(config, typeof(T))]
            {
                typeArguments = typeArguments.Insert(0, ParseTypeName(type.GetFullyQualifiedName()));
            }

            string configurationName = $"{httpClient.ConstructorArguments[0].ToCSharpString()[WellKnownMetadataNames.HttpClientConfiguration.Length..]}Configuration";

            InvocationExpressionSyntax invocation = InvocationExpression(
                    SimpleMemberAccessExpression(
                        IdentifierName("services"),
                        GenericName(Identifier("AddHttpClient"))
                            .WithTypeArgumentList(TypeArgumentList(typeArguments))))
                .WithArgumentList(ArgumentList(SingletonSeparatedList(
                    Argument(IdentifierName(configurationName)))));

            if (primaryHttpMessageHandler is not null && !primaryHttpMessageHandler.NamedArguments.IsEmpty)
            {
                invocation = InvocationExpression(SimpleMemberAccessExpression(
                        invocation,
                        IdentifierName("ConfigurePrimaryHttpMessageHandler")))
                    .WithArgumentList(ArgumentList(SingletonSeparatedList(
                        Argument(ParenthesizedLambdaExpression()
                            .WithParameterList(ParameterList(SeparatedList(
                            [
                                Parameter(Identifier("handler")),
                                Parameter(Identifier("serviceProvider"))
                            ])))
                            .WithBlock(Block(List(
                                GenerateConfigurePrimaryHttpMessageHandlerStatements(primaryHttpMessageHandler.NamedArguments))))))));
            }

            yield return ExpressionStatement(invocation);
        }

        yield return ReturnStatement(IdentifierName("services"));
    }

    private static IEnumerable<StatementSyntax> GenerateConfigurePrimaryHttpMessageHandlerStatements(ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments)
    {
        yield return LocalDeclarationStatement(VariableDeclaration(TypeOfSystemNetHttpSocketsHttpHandler)
            .WithVariables(SingletonSeparatedList(
                VariableDeclarator(Identifier("typedHandler"))
                    .WithInitializer(EqualsValueClause(CastExpression(TypeOfSystemNetHttpSocketsHttpHandler, IdentifierName("handler")))))));

        foreach ((string name, TypedConstant typedConstant) in namedArguments)
        {
            yield return ExpressionStatement(SimpleAssignmentExpression(
                SimpleMemberAccessExpression(IdentifierName("typedHandler"), IdentifierName(name)),
                TypedConstantInfo.Create(typedConstant).GetSyntax()));
        }
    }
}