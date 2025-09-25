// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
using static Snap.Hutao.SourceGeneration.Primitive.SyntaxKeywords;
using static Snap.Hutao.SourceGeneration.WellKnownSyntax;
using TypeInfo = Snap.Hutao.SourceGeneration.Model.TypeInfo;

namespace Snap.Hutao.SourceGeneration.Automation;

[Generator(LanguageNames.CSharp)]
internal sealed class ConstructorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<ConstructorGeneratorContext> provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                WellKnownMetadataNames.ConstructorGeneratedAttribute,
                SyntaxNodeHelper.Is<ClassDeclarationSyntax>,
                ConstructorGeneratorContext.Create);

        context.RegisterSourceOutput(provider, GenerateWrapper);
    }

    private static void GenerateWrapper(SourceProductionContext production, ConstructorGeneratorContext context)
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

    private static void Generate(SourceProductionContext production, ConstructorGeneratorContext context)
    {
        CompilationUnitSyntax syntax = context.Hierarchy.GetCompilationUnit(
        [
            GenerateConstructorDeclaration(context)
                .WithParameterList(GenerateConstructorParameterList(context.Attribute))
                .WithBody(Block(List(GenerateConstructorBodyStatements(context, production.CancellationToken)))),

            // Property declarations
            .. GeneratePropertyDeclarations(context, production.CancellationToken),

            // PreConstruct & PostConstruct Method declarations
            MethodDeclaration(VoidType, Identifier("PreConstruct"))
                .WithModifiers(PartialTokenList)
                .WithParameterList(ParameterList(SingletonSeparatedList(
                    Parameter(TypeOfSystemIServiceProvider, Identifier("serviceProvider")))))
                .WithSemicolonToken(SemicolonToken),
            MethodDeclaration(VoidType, Identifier("PostConstruct"))
                .WithModifiers(PartialTokenList)
                .WithParameterList(ParameterList(SingletonSeparatedList(
                    Parameter(TypeOfSystemIServiceProvider, Identifier("serviceProvider")))))
                .WithSemicolonToken(SemicolonToken)
        ]).NormalizeWhitespace();

        production.AddSource(context.Hierarchy.FileNameHint, syntax.ToFullStringWithHeader());
    }

    private static ConstructorDeclarationSyntax GenerateConstructorDeclaration(ConstructorGeneratorContext context)
    {
        SyntaxTokenList modifiers = context.Attribute.HasNamedArgument("Private", true)
            ? PrivateTokenList
            : PublicTokenList;

        ConstructorDeclarationSyntax constructorDeclaration = ConstructorDeclaration(Identifier(context.Hierarchy.Hierarchy[0].Name))
            .WithModifiers(modifiers);

        if (context.Attribute.HasNamedArgument("CallBaseConstructor", true))
        {
            constructorDeclaration = constructorDeclaration.WithInitializer(
                BaseConstructorInitializer(ArgumentList(SingletonSeparatedList(
                    Argument(IdentifierName("serviceProvider"))))));
        }

        return constructorDeclaration;
    }

    private static ParameterListSyntax GenerateConstructorParameterList(AttributeInfo attributeInfo)
    {
        ImmutableArray<ParameterSyntax>.Builder parameters = ImmutableArray.CreateBuilder<ParameterSyntax>();
        parameters.Add(Parameter(TypeOfSystemIServiceProvider, Identifier("serviceProvider")));

        if (attributeInfo.HasNamedArgument("ResolveHttpClient", true))
        {
            parameters.Add(Parameter(TypeOfSystemNetHttpHttpClient, Identifier("httpClient")));
        }

        return ParameterList(SeparatedList(parameters.ToImmutable()));
    }

    private static IEnumerable<StatementSyntax> GenerateConstructorBodyStatements(ConstructorGeneratorContext context, CancellationToken token)
    {
        // Call PreConstruct
        token.ThrowIfCancellationRequested();
        yield return ExpressionStatement(InvocationExpression(IdentifierName("PreConstruct"))
            .WithArgumentList(ArgumentList(SingletonSeparatedList(
                Argument(IdentifierName("serviceProvider"))))));

        // Assign fields
        foreach (StatementSyntax? statementSyntax in GenerateConstructorBodyFieldAssignments(context, token))
        {
            token.ThrowIfCancellationRequested();
            yield return statementSyntax;
        }

        // Assign properties
        foreach (StatementSyntax? statementSyntax in GenerateConstructorBodyPropertyAssignments(context, token))
        {
            token.ThrowIfCancellationRequested();
            yield return statementSyntax;
        }

        token.ThrowIfCancellationRequested();

        // Call Register for IRecipient interfaces
        foreach (TypeInfo recipientInterface in context.Interfaces)
        {
            string messageTypeString = recipientInterface.TypeArguments.Single().FullyQualifiedTypeNameWithNullabilityAnnotations;
            TypeSyntax messageType = ParseTypeName(messageTypeString);

            token.ThrowIfCancellationRequested();
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

        // Call InitializeComponent if specified
        if (context.Attribute.HasNamedArgument("InitializeComponent", true))
        {
            token.ThrowIfCancellationRequested();
            yield return ExpressionStatement(InvocationExpression(IdentifierName("InitializeComponent")));
        }

        // Call PostConstruct
        token.ThrowIfCancellationRequested();
        yield return ExpressionStatement(InvocationExpression(IdentifierName("PostConstruct"))
            .WithArgumentList(ArgumentList(SingletonSeparatedList(
                Argument(IdentifierName("serviceProvider"))))));
    }

    private static IEnumerable<StatementSyntax> GenerateConstructorBodyFieldAssignments(ConstructorGeneratorContext context, CancellationToken token)
    {
        foreach ((bool shouldSkip, FieldInfo fieldInfo) in context.Fields)
        {
            if (shouldSkip)
            {
                yield return EmptyStatement().WithTrailingTrivia(Comment($"// Skipped field with initializer: {fieldInfo.MinimallyQualifiedName}"));
                continue;
            }

            string fullyQualifiedFieldTypeName = fieldInfo.FullyQualifiedTypeNameWithNullabilityAnnotation;
            TypeSyntax fieldType = ParseTypeName(fullyQualifiedFieldTypeName);
            MemberAccessExpressionSyntax fieldAccess = SimpleMemberAccessExpression(ThisExpression(), IdentifierName(fieldInfo.MinimallyQualifiedName));
            token.ThrowIfCancellationRequested();
            switch (fullyQualifiedFieldTypeName)
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
                        context.Attribute.HasNamedArgument("ResolveHttpClient", true)
                            ? IdentifierName("httpClient")
                            : InvocationExpression(
                                    SimpleMemberAccessExpression(
                                        ServiceProviderGetRequiredService(IdentifierName("serviceProvider"), TypeOfSystemNetHttpIHttpClientFactory),
                                        IdentifierName("CreateClient")))
                                .WithArgumentList(ArgumentList(SingletonSeparatedList(
                                    Argument(NameOfExpression(IdentifierName(context.Hierarchy.Hierarchy[0].Name))))))));
                    break;

                // this.${fieldName} = serviceProvider.GetRequiredKeyedService<${fieldType}>(key);
                // this.${fieldName} = serviceProvider.GetRequiredService<${fieldType}>();
                default:
                    if (fieldInfo.TryGetAttributeWithFullyQualifiedMetadataName(WellKnownMetadataNames.FromKeyedServicesAttribute, out AttributeInfo? fromKeyed))
                    {
                        yield return ExpressionStatement(SimpleAssignmentExpression(
                            fieldAccess,
                            ServiceProviderGetRequiredKeyedService(IdentifierName("serviceProvider"), fieldType, fromKeyed.ConstructorArguments.Single().GetSyntax())));
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

    private static IEnumerable<StatementSyntax> GenerateConstructorBodyPropertyAssignments(ConstructorGeneratorContext context, CancellationToken token)
    {
        foreach (PropertyInfo propertyInfo in context.Properties)
        {
            string fullyQualifiedPropertyTypeName = propertyInfo.FullyQualifiedTypeNameWithNullabilityAnnotation;
            TypeSyntax propertyType = ParseTypeName(fullyQualifiedPropertyTypeName);
            MemberAccessExpressionSyntax propertyAccess = SimpleMemberAccessExpression(ThisExpression(), IdentifierName(propertyInfo.Name));
            token.ThrowIfCancellationRequested();
            switch (fullyQualifiedPropertyTypeName)
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
                        context.Attribute.HasNamedArgument("ResolveHttpClient", true)
                            ? IdentifierName("httpClient")
                            : InvocationExpression(
                                    SimpleMemberAccessExpression(
                                        ServiceProviderGetRequiredService(IdentifierName("serviceProvider"), TypeOfSystemNetHttpIHttpClientFactory),
                                        IdentifierName("CreateClient")))
                                .WithArgumentList(ArgumentList(SingletonSeparatedList(
                                    Argument(NameOfExpression(IdentifierName(context.Hierarchy.Hierarchy[0].Name))))))));
                    break;

                // this.${propertyName} = serviceProvider.GetRequiredKeyedService<${fieldType}>(key);
                // this.${propertyName} = serviceProvider.GetRequiredService<${fieldType}>();
                default:
                    if (propertyInfo.TryGetAttributeWithFullyQualifiedMetadataName(WellKnownMetadataNames.FromKeyedServicesAttribute, out AttributeInfo? fromKeyed))
                    {
                        yield return ExpressionStatement(SimpleAssignmentExpression(
                            propertyAccess,
                            ServiceProviderGetRequiredKeyedService(IdentifierName("serviceProvider"), propertyType, fromKeyed.ConstructorArguments.Single().GetSyntax())));
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

    private static IEnumerable<PropertyDeclarationSyntax> GeneratePropertyDeclarations(ConstructorGeneratorContext context, CancellationToken token)
    {
        foreach (PropertyInfo propertyInfo in context.Properties)
        {
            TypeSyntax propertyType = ParseTypeName(propertyInfo.FullyQualifiedTypeNameWithNullabilityAnnotation);
            token.ThrowIfCancellationRequested();
            yield return PropertyDeclaration(propertyType, Identifier(propertyInfo.Name))
                .WithModifiers(propertyInfo.DeclaredAccessibility.ToSyntaxTokenList(PartialKeyword))
                .WithAccessorList(AccessorList(SingletonList(
                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithExpressionBody(ArrowExpressionClause(FieldExpression()))
                        .WithSemicolonToken(SemicolonToken))));
        }
    }

    private sealed record ConstructorGeneratorContext
    {
        public required AttributeInfo Attribute { get; init; }

        public required HierarchyInfo Hierarchy { get; init; }

        public required EquatableArray<(bool ShouldSkip, FieldInfo Field)> Fields { get; init; }

        public required EquatableArray<PropertyInfo> Properties { get; init; }

        public required EquatableArray<TypeInfo> Interfaces { get; init; }

        public static ConstructorGeneratorContext Create(GeneratorAttributeSyntaxContext context, CancellationToken token)
        {
            if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
            {
                return default!;
            }

            ImmutableArray<(bool ShouldSkip, FieldInfo Field)>.Builder fieldsBuilder = ImmutableArray.CreateBuilder<(bool ShouldSkip, FieldInfo Field)>();
            ImmutableArray<PropertyInfo>.Builder propertiesBuilder = ImmutableArray.CreateBuilder<PropertyInfo>();

            foreach (ISymbol member in typeSymbol.GetMembers())
            {
                switch (member)
                {
                    case IFieldSymbol fieldSymbol:
                        {
                            if (fieldSymbol.IsImplicitlyDeclared || fieldSymbol.HasConstantValue || fieldSymbol.IsStatic || !fieldSymbol.IsReadOnly)
                            {
                                continue;
                            }

                            bool shouldSkip = false;
                            foreach (SyntaxReference syntaxReference in fieldSymbol.DeclaringSyntaxReferences)
                            {
                                if (syntaxReference.GetSyntax() is VariableDeclaratorSyntax { Initializer: not null })
                                {
                                    // Skip field with initializer
                                    shouldSkip = true;
                                    break;
                                }
                            }

                            fieldsBuilder.Add((shouldSkip, FieldInfo.Create(fieldSymbol)));
                            break;
                        }
                    case IPropertySymbol propertySymbol:
                        {
                            if (propertySymbol.IsStatic || propertySymbol.IsImplicitlyDeclared || !propertySymbol.IsPartialDefinition || !propertySymbol.IsReadOnly)
                            {
                                continue;
                            }

                            propertiesBuilder.Add(PropertyInfo.Create(propertySymbol));
                            break;
                        }
                }
            }

            ImmutableArray<TypeInfo>.Builder interfacesBuilder = ImmutableArray.CreateBuilder<TypeInfo>();
            foreach (INamedTypeSymbol interfaceSymbol in typeSymbol.Interfaces)
            {
                if (!interfaceSymbol.HasFullyQualifiedMetadataName("CommunityToolkit.Mvvm.Messaging.IRecipient`1"))
                {
                    continue;
                }

                interfacesBuilder.Add(TypeInfo.Create(interfaceSymbol));
            }

            return new()
            {
                Attribute = AttributeInfo.Create(context.Attributes.Single()),
                Hierarchy = HierarchyInfo.Create(typeSymbol),
                Fields = fieldsBuilder.ToImmutable(),
                Properties = propertiesBuilder.ToImmutable(),
                Interfaces = interfacesBuilder.ToImmutable(),
            };
        }
    }
}