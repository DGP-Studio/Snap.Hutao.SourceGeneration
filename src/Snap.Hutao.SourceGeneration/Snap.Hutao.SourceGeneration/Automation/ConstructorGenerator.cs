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

namespace Snap.Hutao.SourceGeneration.Automation;

[Generator(LanguageNames.CSharp)]
internal sealed class ConstructorGenerator : IIncrementalGenerator
{
    private static readonly TypeSyntax CommunityToolkitMvvmMessagingIMessengerType = ParseTypeName("CommunityToolkit.Mvvm.Messaging.IMessenger");
    private static readonly TypeSyntax SystemNetHttpIHttpClientFactoryType = ParseTypeName("System.Net.Http.IHttpClientFactory");
    private static readonly TypeSyntax SystemNetHttpHttpClientType = ParseTypeName("System.Net.Http.HttpClient");
    private static readonly TypeSyntax SystemIServiceProviderType = ParseTypeName("System.IServiceProvider");

    private static readonly IdentifierNameSyntax IdentifierNameOfInitializeComponent = IdentifierName("InitializeComponent");
    private static readonly IdentifierNameSyntax IdentifierNameOfServiceProvider = IdentifierName("serviceProvider");
    private static readonly IdentifierNameSyntax IdentifierNameOfHttpClient = IdentifierName("httpClient");

    private static readonly GenericNameSyntax GenericNameOfGetRequiredService = GenericName("GetRequiredService");
    private static readonly GenericNameSyntax GenericNameOfGetRequiredKeyedService = GenericName("GetRequiredKeyedService");

    private static readonly SyntaxToken IdentifierOfServiceProvider = Identifier("serviceProvider");
    private static readonly SyntaxToken IdentifierOfHttpClient = Identifier("httpClient");
    private static readonly SyntaxToken IdentifierOfPreConstruct = Identifier("PreConstruct");
    private static readonly SyntaxToken IdentifierOfPostConstruct = Identifier("PostConstruct");

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<GeneratorAttributeSyntaxContext> provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                WellKnownAttributeNames.ConstructorGeneratedAttribute,
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
                            MethodDeclaration(VoidType, IdentifierOfPreConstruct)
                                .WithModifiers(TokenList(PartialKeyword))
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(IdentifierOfServiceProvider).WithType(SystemIServiceProviderType))))
                                .WithSemicolonToken(SemicolonToken),
                            MethodDeclaration(VoidType, IdentifierOfPostConstruct)
                                .WithModifiers(TokenList(Token(SyntaxKind.PartialKeyword)))
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(IdentifierOfServiceProvider).WithType(SystemIServiceProviderType))))
                                .WithSemicolonToken(SemicolonToken)
                        ]))))))
            .NormalizeWhitespace();

        production.AddSource(context.TargetSymbol.ToDisplayString().NormalizeSymbolName(), syntax.ToFullString());
    }

    private static ConstructorDeclarationSyntax GenerateConstructorDeclaration(INamedTypeSymbol classSymbol, AttributeData attributeData)
    {
        ConstructorDeclarationSyntax constructorDeclaration = ConstructorDeclaration(Identifier(classSymbol.Name))
            .WithModifiers(PublicTokenList);

        if (attributeData.HasNamedArgumentWith("CallBaseConstructor", true))
        {
            constructorDeclaration = constructorDeclaration.WithInitializer(
                BaseConstructorInitializer(ArgumentList(SingletonSeparatedList(
                    Argument(IdentifierNameOfServiceProvider)))));
        }

        return constructorDeclaration;
    }

    private static ParameterListSyntax GenerateConstructorParameterList(AttributeData attributeData)
    {
        ImmutableArray<ParameterSyntax>.Builder parameters = ImmutableArray.CreateBuilder<ParameterSyntax>();
        parameters.Add(Parameter(IdentifierOfServiceProvider).WithType(SystemIServiceProviderType));

        if (attributeData.HasNamedArgumentWith("ResolveHttpClient", true))
        {
            parameters.Add(Parameter(IdentifierOfHttpClient).WithType(SystemNetHttpHttpClientType));
        }

        return ParameterList(SeparatedList(parameters.ToImmutable()));
    }

    private static IEnumerable<StatementSyntax> GenerateConstructorBodyStatements(INamedTypeSymbol classSymbol, AttributeData attributeData, CancellationToken token)
    {
        // Call PreConstruct
        yield return ExpressionStatement(InvocationExpression(IdentifierName("PreConstruct"))
            .WithArgumentList(ArgumentList(SingletonSeparatedList(
                Argument(IdentifierNameOfServiceProvider)))));

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
            if (interfaceSymbol.IsOrInheritsFrom("CommunityToolkit.Mvvm.Messaging.IRecipient"))
            {
                TypeSyntax messageType = ParseTypeName(interfaceSymbol.TypeArguments.Single().ToDisplayString());

                yield return ExpressionStatement(InvocationExpression(SimpleMemberAccessExpression(
                        IdentifierName("CommunityToolkit.Mvvm.Messaging.IMessengerExtensions"),
                        GenericName(Identifier("Register"))
                            .WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList(messageType)))))
                    .WithArgumentList(ArgumentList(SeparatedList(
                    [
                        Argument(ServiceProviderGetRequiredService(IdentifierNameOfServiceProvider, CommunityToolkitMvvmMessagingIMessengerType)),
                        Argument(ThisExpression())
                    ]))));
            }
        }

        // Call InitializeComponent if specified
        if (attributeData.HasNamedArgumentWith("InitializeComponent", true))
        {
            yield return ExpressionStatement(InvocationExpression(IdentifierNameOfInitializeComponent));
        }

        // Call PostConstruct
        yield return ExpressionStatement(InvocationExpression(IdentifierName("PostConstruct"))
            .WithArgumentList(ArgumentList(SingletonSeparatedList(
                Argument(IdentifierNameOfServiceProvider)))));
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

            string fieldTypeString = fieldSymbol.Type.ToDisplayString();
            TypeSyntax fieldType = ParseTypeName(fieldTypeString);
            IdentifierNameSyntax fieldIdentifier = IdentifierName(fieldSymbol.Name);
            MemberAccessExpressionSyntax fieldAccess = SimpleMemberAccessExpression(ThisExpression(), fieldIdentifier);

            switch (fieldTypeString)
            {
                // this.${fieldName} = serviceProvider;
                case "System.IServiceProvider":
                    yield return ExpressionStatement(SimpleAssignmentExpression(fieldAccess, IdentifierNameOfServiceProvider));
                    break;

                // this.${fieldName} = httpClient;
                // this.${fieldName} = serviceProvider.GetRequiredService<System.Net.Http.IHttpClientFactory>().CreateClient(nameof(${className}));
                case "System.Net.Http.HttpClient":
                    yield return ExpressionStatement(SimpleAssignmentExpression(
                        fieldAccess,
                        attributeData.HasNamedArgumentWith("ResolveHttpClient", true)
                            ? IdentifierNameOfHttpClient
                            : InvocationExpression(
                                    SimpleMemberAccessExpression(
                                        ServiceProviderGetRequiredService(IdentifierNameOfServiceProvider, SystemNetHttpIHttpClientFactoryType),
                                        IdentifierName("CreateClient")))
                                .WithArgumentList(ArgumentList(SingletonSeparatedList(
                                    Argument(NameOf(IdentifierName(classSymbol.Name))))))));
                    break;

                // this.${fieldName} = serviceProvider.GetRequiredKeyedService<${fieldType}>(key);
                // this.${fieldName} = serviceProvider.GetRequiredService<${fieldType}>();
                default:
                    if (fieldSymbol.GetAttributes().SingleOrDefault(data => data.AttributeClass?.IsOrInheritsFrom(WellKnownAttributeNames.FromKeyedServicesAttribute) ?? false) is { } fromKeyed)
                    {
                        yield return ExpressionStatement(SimpleAssignmentExpression(
                            fieldAccess,
                            ServiceProviderGetRequiredKeyedService(IdentifierNameOfServiceProvider, fieldType, ParseExpression(fromKeyed.ConstructorArguments.Single().ToCSharpString()))));
                    }
                    else
                    {
                        yield return ExpressionStatement(SimpleAssignmentExpression(
                            fieldAccess,
                            ServiceProviderGetRequiredService(IdentifierNameOfServiceProvider, fieldType)));
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

            string propertyTypeString = propertySymbol.Type.ToDisplayString();
            TypeSyntax propertyType = ParseTypeName(propertyTypeString);
            IdentifierNameSyntax propertyIdentifier = IdentifierName(propertySymbol.Name);
            MemberAccessExpressionSyntax propertyAccess = SimpleMemberAccessExpression(ThisExpression(), propertyIdentifier);

            switch (propertyTypeString)
            {
                // this.${fieldName} = serviceProvider;
                case "System.IServiceProvider":
                    yield return ExpressionStatement(SimpleAssignmentExpression(propertyAccess, IdentifierNameOfServiceProvider));
                    break;

                // this.${fieldName} = httpClient;
                // this.${fieldName} = serviceProvider.GetRequiredService<System.Net.Http.IHttpClientFactory>().CreateClient(nameof(${className}));
                case "System.Net.Http.HttpClient":
                    yield return ExpressionStatement(SimpleAssignmentExpression(
                        propertyAccess,
                        attributeData.HasNamedArgumentWith("ResolveHttpClient", true)
                            ? IdentifierNameOfHttpClient
                            : InvocationExpression(
                                    SimpleMemberAccessExpression(
                                        ServiceProviderGetRequiredService(IdentifierNameOfServiceProvider, SystemNetHttpIHttpClientFactoryType),
                                        IdentifierName("CreateClient")))
                                .WithArgumentList(ArgumentList(SingletonSeparatedList(
                                    Argument(NameOf(IdentifierName(classSymbol.Name))))))));
                    break;

                // this.${fieldName} = serviceProvider.GetRequiredKeyedService<${fieldType}>(key);
                // this.${fieldName} = serviceProvider.GetRequiredService<${fieldType}>();
                default:
                    if (propertySymbol.GetAttributes().SingleOrDefault(data => data.AttributeClass?.IsOrInheritsFrom(WellKnownAttributeNames.FromKeyedServicesAttribute) ?? false) is { } fromKeyed)
                    {
                        ExpressionSyntax? argumentExpression = default;
                        if (fromKeyed.ApplicationSyntaxReference is { } syntaxRef && syntaxRef.GetSyntax(token) is AttributeSyntax syntax)
                        {
                            argumentExpression = syntax.ArgumentList?.Arguments.Single().Expression;
                        }

                        if (argumentExpression is null)
                        {
                            TypedConstant key = fromKeyed.ConstructorArguments.Single();
                            argumentExpression = ParseExpression(key.ToCSharpString());
                        }

                        yield return ExpressionStatement(SimpleAssignmentExpression(
                            propertyAccess,
                            ServiceProviderGetRequiredKeyedService(IdentifierNameOfServiceProvider, propertyType, argumentExpression)));
                    }
                    else
                    {
                        yield return ExpressionStatement(SimpleAssignmentExpression(
                            propertyAccess,
                            ServiceProviderGetRequiredService(IdentifierNameOfServiceProvider, propertyType)));
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

            yield return PropertyDeclaration(ParseTypeName(propertySymbol.Type.ToDisplayString()), Identifier(propertySymbol.Name))
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
            GenericNameOfGetRequiredService.WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList(type)))));
    }

    private static InvocationExpressionSyntax ServiceProviderGetRequiredKeyedService(ExpressionSyntax serviceProvider, TypeSyntax type, ExpressionSyntax argument)
    {
        return InvocationExpression(SimpleMemberAccessExpression(
            serviceProvider,
            GenericNameOfGetRequiredKeyedService.WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList(type)))))
            .WithArgumentList(ArgumentList(SingletonSeparatedList(Argument(argument))));
    }
}