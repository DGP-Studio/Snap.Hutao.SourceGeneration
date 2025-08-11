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
using System.Threading;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.FastSyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.SyntaxKeywords;

namespace Snap.Hutao.SourceGeneration.Automation;

[Generator(LanguageNames.CSharp)]
internal sealed class ConstructorGenerator : IIncrementalGenerator
{
    private static readonly TypeSyntax TypeOfCommunityToolkitMvvmMessagingIMessenger =
        ParseTypeName("global::CommunityToolkit.Mvvm.Messaging.IMessenger");
    private static readonly TypeSyntax TypeOfCommunityToolkitMvvmMessagingIMessengerExtensions =
        ParseTypeName("global::CommunityToolkit.Mvvm.Messaging.IMessengerExtensions");
    private static readonly TypeSyntax TypeOfSystemNetHttpIHttpClientFactory =
        ParseTypeName("global::System.Net.Http.IHttpClientFactory");
    private static readonly TypeSyntax TypeOfSystemNetHttpHttpClient =
        ParseTypeName("global::System.Net.Http.HttpClient");
    private static readonly TypeSyntax TypeOfSystemIServiceProvider =
        ParseTypeName("global::System.IServiceProvider");

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<GeneratorAttributeSyntaxContext> provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                WellKnownMetadataNames.ConstructorGeneratedAttribute,
                SyntaxNodeHelper.Is<ClassDeclarationSyntax>,
                SyntaxContext.Transform);

        context.RegisterSourceOutput(provider, GenerateWrapper);
    }

    private static void GenerateWrapper(SourceProductionContext production, GeneratorAttributeSyntaxContext contexts)
    {
        try
        {
            Generate(production, contexts);
        }
        catch (Exception ex)
        {
            production.AddSource($"Error-{Guid.NewGuid().ToString()}.g.cs", ex.ToString());
        }
    }

    private static void Generate(SourceProductionContext production, GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            return;
        }

        AttributeData attributeData = context.Attributes.Single();

        CompilationUnitSyntax syntax = CompilationUnit()
            .WithMembers(SingletonList<MemberDeclarationSyntax>(FileScopedNamespaceDeclaration(context.TargetSymbol.ContainingNamespace)
                .WithLeadingTrivia(NullableEnableList)
                .WithMembers(SingletonList<MemberDeclarationSyntax>(
                    PartialTypeDeclaration(classSymbol)
                        .WithMembers(List<MemberDeclarationSyntax>(
                        [
                            GenerateConstructorDeclaration(classSymbol, attributeData)
                                .WithParameterList(GenerateConstructorParameterList(attributeData))
                                .WithBody(Block(List(GenerateConstructorBodyStatements(classSymbol, attributeData, production.CancellationToken)))),

                            // Property declarations
                            .. GeneratePropertyDeclarations(classSymbol, production.CancellationToken),

                            // PreConstruct & PostConstruct Method declarations
                            MethodDeclaration(VoidType, Identifier("PreConstruct"))
                                .WithModifiers(TokenList(PartialKeyword))
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(TypeOfSystemIServiceProvider, Identifier("serviceProvider")))))
                                .WithSemicolonToken(SemicolonToken),
                            MethodDeclaration(VoidType, Identifier("PostConstruct"))
                                .WithModifiers(TokenList(Token(SyntaxKind.PartialKeyword)))
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(TypeOfSystemIServiceProvider, Identifier("serviceProvider")))))
                                .WithSemicolonToken(SemicolonToken)
                        ]))))))
            .NormalizeWhitespace();

        production.AddSource(context.TargetSymbol.ToDisplayString().NormalizeSymbolName(), syntax.ToFullString());
    }

    private static ConstructorDeclarationSyntax GenerateConstructorDeclaration(INamedTypeSymbol classSymbol, AttributeData attributeData)
    {
        ConstructorDeclarationSyntax constructorDeclaration = ConstructorDeclaration(Identifier(classSymbol.Name))
            .WithModifiers(PublicTokenList);

        if (attributeData.HasNamedArgument("CallBaseConstructor", true))
        {
            constructorDeclaration = constructorDeclaration.WithInitializer(
                BaseConstructorInitializer(ArgumentList(SingletonSeparatedList(
                    Argument(IdentifierName("serviceProvider"))))));
        }

        return constructorDeclaration;
    }

    private static ParameterListSyntax GenerateConstructorParameterList(AttributeData attributeData)
    {
        ImmutableArray<ParameterSyntax>.Builder parameters = ImmutableArray.CreateBuilder<ParameterSyntax>();
        parameters.Add(Parameter(TypeOfSystemIServiceProvider, Identifier("serviceProvider")));

        if (attributeData.HasNamedArgument("ResolveHttpClient", true))
        {
            parameters.Add(Parameter(TypeOfSystemNetHttpHttpClient, Identifier("httpClient")));
        }

        return ParameterList(SeparatedList(parameters.ToImmutable()));
    }

    private static IEnumerable<StatementSyntax> GenerateConstructorBodyStatements(INamedTypeSymbol classSymbol, AttributeData attributeData, CancellationToken token)
    {
        // Call PreConstruct
        yield return ExpressionStatement(InvocationExpression(IdentifierName("PreConstruct"))
            .WithArgumentList(ArgumentList(SingletonSeparatedList(
                Argument(IdentifierName("serviceProvider"))))));

        // Assign fields
        foreach (StatementSyntax? statementSyntax in GenerateConstructorBodyFieldAssignments(classSymbol, attributeData, token))
        {
            yield return statementSyntax;
        }

        // Assign properties
        foreach (StatementSyntax? statementSyntax in GenerateConstructorBodyPropertyAssignments(classSymbol, attributeData, token))
        {
            yield return statementSyntax;
        }

        // Call Register for IRecipient interfaces
        foreach (INamedTypeSymbol interfaceSymbol in classSymbol.Interfaces)
        {
            if (interfaceSymbol.HasFullyQualifiedMetadataName("CommunityToolkit.Mvvm.Messaging.IRecipient`1"))
            {
                string messageTypeString = interfaceSymbol.TypeArguments.Single().GetFullyQualifiedNameWithNullabilityAnnotations();
                TypeSyntax messageType = ParseTypeName(messageTypeString);

                yield return ExpressionStatement(InvocationExpression(SimpleMemberAccessExpression(
                        TypeOfCommunityToolkitMvvmMessagingIMessengerExtensions,
                        GenericName(Identifier("Register"))
                            .WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList(messageType)))))
                    .WithArgumentList(ArgumentList(SeparatedList(
                    [
                        Argument(ServiceProviderGetRequiredService(IdentifierName("serviceProvider"), TypeOfCommunityToolkitMvvmMessagingIMessenger)),
                        Argument(ThisExpression())
                    ]))));
            }
        }

        // Call InitializeComponent if specified
        if (attributeData.HasNamedArgument("InitializeComponent", true))
        {
            yield return ExpressionStatement(InvocationExpression(IdentifierName("InitializeComponent")));
        }

        // Call PostConstruct
        yield return ExpressionStatement(InvocationExpression(IdentifierName("PostConstruct"))
            .WithArgumentList(ArgumentList(SingletonSeparatedList(
                Argument(IdentifierName("serviceProvider"))))));
    }

    private static IEnumerable<StatementSyntax> GenerateConstructorBodyFieldAssignments(INamedTypeSymbol classSymbol, AttributeData attributeData, CancellationToken token)
    {
        foreach (IFieldSymbol fieldSymbol in classSymbol.GetMembers().OfType<IFieldSymbol>())
        {
            if (fieldSymbol.IsImplicitlyDeclared || fieldSymbol.HasConstantValue || fieldSymbol.IsStatic || !fieldSymbol.IsReadOnly)
            {
                continue;
            }

            bool shouldSkip = false;
            foreach (SyntaxReference syntaxReference in fieldSymbol.DeclaringSyntaxReferences)
            {
                if (syntaxReference.GetSyntax() is VariableDeclaratorSyntax { Initializer: not null } declarator)
                {
                    // Skip field with initializer
                    yield return EmptyStatement().WithTrailingTrivia(Comment($"// Skipped field with initializer: {fieldSymbol.Name}"));
                    shouldSkip = true;
                    break;
                }
            }

            if (shouldSkip)
            {
                continue;
            }

            string fieldTypeString = fieldSymbol.Type.GetFullyQualifiedNameWithNullabilityAnnotations();
            TypeSyntax fieldType = ParseTypeName(fieldTypeString);
            IdentifierNameSyntax fieldIdentifier = IdentifierName(fieldSymbol.Name);
            MemberAccessExpressionSyntax fieldAccess = SimpleMemberAccessExpression(ThisExpression(), fieldIdentifier);

            switch (fieldTypeString)
            {
                // this.${fieldName} = serviceProvider;
                case "global::System.IServiceProvider":
                    yield return ExpressionStatement(SimpleAssignmentExpression(fieldAccess, IdentifierName("serviceProvider")));
                    break;

                // this.${fieldName} = httpClient;
                // this.${fieldName} = serviceProvider.GetRequiredService<System.Net.Http.IHttpClientFactory>().CreateClient(nameof(${className}));
                case "global::System.Net.Http.HttpClient":
                    yield return ExpressionStatement(SimpleAssignmentExpression(
                        fieldAccess,
                        attributeData.HasNamedArgument("ResolveHttpClient", true)
                            ? IdentifierName("httpClient")
                            : InvocationExpression(
                                    SimpleMemberAccessExpression(
                                        ServiceProviderGetRequiredService(IdentifierName("serviceProvider"), TypeOfSystemNetHttpIHttpClientFactory),
                                        IdentifierName("CreateClient")))
                                .WithArgumentList(ArgumentList(SingletonSeparatedList(
                                    Argument(NameOf(IdentifierName(classSymbol.Name))))))));
                    break;

                // this.${fieldName} = serviceProvider.GetRequiredKeyedService<${fieldType}>(key);
                // this.${fieldName} = serviceProvider.GetRequiredService<${fieldType}>();
                default:
                    if (fieldSymbol.GetAttributes().SingleOrDefault(data => data.AttributeClass?.HasFullyQualifiedMetadataName(WellKnownMetadataNames.FromKeyedServicesAttribute) ?? false) is { } fromKeyed)
                    {
                        yield return ExpressionStatement(SimpleAssignmentExpression(
                            fieldAccess,
                            ServiceProviderGetRequiredKeyedService(IdentifierName("serviceProvider"), fieldType, TypedConstantInfo.Create(fromKeyed.ConstructorArguments.Single()).GetSyntax())));
                    }
                    else
                    {
                        yield return ExpressionStatement(SimpleAssignmentExpression(
                            fieldAccess,
                            ServiceProviderGetRequiredService(IdentifierName("serviceProvider"), fieldType)));
                    }
                    break;
            }
        }
    }

    private static IEnumerable<StatementSyntax> GenerateConstructorBodyPropertyAssignments(INamedTypeSymbol classSymbol, AttributeData attributeData, CancellationToken token)
    {
        foreach (IPropertySymbol propertySymbol in classSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            if (ShouldSkipProperty(propertySymbol))
            {
                continue;
            }

            string propertyTypeString = propertySymbol.Type.GetFullyQualifiedNameWithNullabilityAnnotations();
            TypeSyntax propertyType = ParseTypeName(propertyTypeString);
            IdentifierNameSyntax propertyIdentifier = IdentifierName(propertySymbol.Name);
            MemberAccessExpressionSyntax propertyAccess = SimpleMemberAccessExpression(ThisExpression(), propertyIdentifier);

            switch (propertyTypeString)
            {
                // this.${propertyName} = serviceProvider;
                case "global::System.IServiceProvider":
                    yield return ExpressionStatement(SimpleAssignmentExpression(propertyAccess, IdentifierName("serviceProvider")));
                    break;

                // this.${propertyName} = httpClient;
                // this.${propertyName} = serviceProvider.GetRequiredService<System.Net.Http.IHttpClientFactory>().CreateClient(nameof(${className}));
                case "global::System.Net.Http.HttpClient":
                    yield return ExpressionStatement(SimpleAssignmentExpression(
                        propertyAccess,
                        attributeData.HasNamedArgument("ResolveHttpClient", true)
                            ? IdentifierName("httpClient")
                            : InvocationExpression(
                                    SimpleMemberAccessExpression(
                                        ServiceProviderGetRequiredService(IdentifierName("serviceProvider"), TypeOfSystemNetHttpIHttpClientFactory),
                                        IdentifierName("CreateClient")))
                                .WithArgumentList(ArgumentList(SingletonSeparatedList(
                                    Argument(NameOf(IdentifierName(classSymbol.Name))))))));
                    break;

                // this.${propertyName} = serviceProvider.GetRequiredKeyedService<${fieldType}>(key);
                // this.${propertyName} = serviceProvider.GetRequiredService<${fieldType}>();
                default:
                    if (propertySymbol.GetAttributes().SingleOrDefault(data => data.AttributeClass?.HasFullyQualifiedMetadataName(WellKnownMetadataNames.FromKeyedServicesAttribute) ?? false) is { } fromKeyed)
                    {
                        yield return ExpressionStatement(SimpleAssignmentExpression(
                            propertyAccess,
                            ServiceProviderGetRequiredKeyedService(IdentifierName("serviceProvider"), propertyType, TypedConstantInfo.Create(fromKeyed.ConstructorArguments.Single()).GetSyntax())));
                    }
                    else
                    {
                        yield return ExpressionStatement(SimpleAssignmentExpression(
                            propertyAccess,
                            ServiceProviderGetRequiredService(IdentifierName("serviceProvider"), propertyType)));
                    }
                    break;
            }
        }
    }

    private static IEnumerable<PropertyDeclarationSyntax> GeneratePropertyDeclarations(INamedTypeSymbol classSymbol, CancellationToken token)
    {
        foreach (IPropertySymbol propertySymbol in classSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            if (ShouldSkipProperty(propertySymbol))
            {
                continue;
            }

            TypeSyntax propertyType = ParseTypeName(propertySymbol.Type.GetFullyQualifiedNameWithNullabilityAnnotations());
            yield return PropertyDeclaration(propertyType, Identifier(propertySymbol.Name))
                .WithModifiers(propertySymbol.DeclaredAccessibility.ToSyntaxTokenList(PartialKeyword))
                .WithAccessorList(AccessorList(SingletonList(
                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithExpressionBody(ArrowExpressionClause(FieldExpression()))
                        .WithSemicolonToken(SemicolonToken))));
        }
    }

    private static bool ShouldSkipProperty(IPropertySymbol propertySymbol)
    {
        return propertySymbol.IsStatic || !propertySymbol.IsPartialDefinition || !propertySymbol.IsReadOnly;
    }

    private static InvocationExpressionSyntax ServiceProviderGetRequiredService(ExpressionSyntax serviceProvider, TypeSyntax type)
    {
        return InvocationExpression(SimpleMemberAccessExpression(
            serviceProvider,
            GenericName("GetRequiredService").WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList(type)))));
    }

    private static InvocationExpressionSyntax ServiceProviderGetRequiredKeyedService(ExpressionSyntax serviceProvider, TypeSyntax type, ExpressionSyntax argument)
    {
        return InvocationExpression(SimpleMemberAccessExpression(
                serviceProvider,
                GenericName("GetRequiredKeyedService").WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList(type)))))
            .WithArgumentList(ArgumentList(SingletonSeparatedList(Argument(argument))));
    }
}