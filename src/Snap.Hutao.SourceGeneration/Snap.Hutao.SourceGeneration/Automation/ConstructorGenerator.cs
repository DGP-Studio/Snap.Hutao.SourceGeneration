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
using System.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.FastSyntaxFactory;

namespace Snap.Hutao.SourceGeneration.Automation;

[Generator(LanguageNames.CSharp)]
internal sealed class ConstructorGenerator : IIncrementalGenerator
{
    private static readonly TypeSyntax SystemIServiceProviderType = ParseName("System.IServiceProvider");

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
            production.AddSource("Error.g.cs", ex.ToString());
        }
    }

    private static void Generate(SourceProductionContext production, GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            return;
        }

        CompilationUnitSyntax syntax = CompilationUnit()
            .WithMembers(SingletonList<MemberDeclarationSyntax>(FileScopedNamespaceDeclaration(context.TargetSymbol.ContainingNamespace)
                .WithMembers(SingletonList<MemberDeclarationSyntax>(
                    ClassDeclaration(classSymbol)
                        .WithModifiers(TokenList(PartialKeyword))
                        .WithMembers(List<MemberDeclarationSyntax>(
                        [
                            ConstructorDeclaration(Identifier(classSymbol.Name))
                                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(Identifier("serviceProvider")).WithType(SystemIServiceProviderType))))
                                .WithBody(Block(List<StatementSyntax>(
                                [
                                    // Call PreConstruct
                                    ExpressionStatement(InvocationExpression(IdentifierName("PreConstruct"))
                                        .WithArgumentList(ArgumentList(SingletonSeparatedList(
                                            Argument(IdentifierName("serviceProvider")))))),

                                    .. GenerateStatements(),
                                    EmptyStatement().WithTrailingTrivia(Comment("// Skipped field with initializer: ConsoleBanner")),
                                    ExpressionStatement(SimpleAssignmentExpression(
                                        SimpleMemberAccessExpression(ThisExpression(), IdentifierName("serviceProvider")),
                                        IdentifierName("serviceProvider"))),
                                    ExpressionStatement(SimpleAssignmentExpression(
                                        SimpleMemberAccessExpression(ThisExpression(), IdentifierName("activation")),
                                        InvocationExpression(SimpleMemberAccessExpression(IdentifierName("serviceProvider"), GenericName(Identifier("GetRequiredService"))
                                            .WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList<TypeSyntax>(
                                                Name("Snap", "Hutao", "Core", "LifeCycle", "IAppActivation")))))))),
                                    ExpressionStatement(SimpleAssignmentExpression(
                                        SimpleMemberAccessExpression(
                                            ThisExpression(),
                                            IdentifierName("logger")),
                                        InvocationExpression(SimpleMemberAccessExpression(IdentifierName("serviceProvider"), GenericName(Identifier("GetRequiredService"))
                                            .WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList<TypeSyntax>(
                                                QualifiedName(
                                                    Name("Microsoft", "Extensions", "Logging"),
                                                    GenericName(Identifier("ILogger")).WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList<TypeSyntax>(Name("Snap", "Hutao", "App")))))))))))),
                                    ExpressionStatement(SimpleAssignmentExpression(
                                        IdentifierName("Options"),
                                        InvocationExpression(SimpleMemberAccessExpression(IdentifierName("serviceProvider"), GenericName(Identifier("GetRequiredService"))
                                            .WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList<TypeSyntax>(
                                                Name("Snap", "Hutao", "Service", "AppOptions")))))))),

                                    // Call InitializeComponent
                                    ExpressionStatement(InvocationExpression(IdentifierName("InitializeComponent"))),

                                    // Call PostConstruct
                                    ExpressionStatement(InvocationExpression(IdentifierName("PostConstruct"))
                                        .WithArgumentList(ArgumentList(SingletonSeparatedList(
                                            Argument(IdentifierName("serviceProvider"))))))
                                ]))),

                            // Property declarations
                            PropertyDeclaration(Name("Snap", "Hutao", "Service", "AppOptions"), Identifier("Options"))
                                .WithModifiers(TokenList(InternalKeyword, PartialKeyword))
                                .WithAccessorList(AccessorList(SingletonList(
                                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                        .WithExpressionBody(
                                            ArrowExpressionClause(FieldExpression())).WithSemicolonToken(SemicolonToken)))),

                            // PreConstruct & PostConstruct Method declarations
                            MethodDeclaration(VoidType, Identifier("PreConstruct"))
                                .WithModifiers(TokenList(PartialKeyword))
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(Identifier("serviceProvider")).WithType(Name("System", "IServiceProvider")))))
                                .WithSemicolonToken(SemicolonToken),
                            MethodDeclaration(VoidType, Identifier("PostConstruct"))
                                .WithModifiers(TokenList(Token(SyntaxKind.PartialKeyword)))
                                .WithParameterList(
                                    ParameterList(SingletonSeparatedList(
                                        Parameter(Identifier("serviceProvider")).WithType(Name("System", "IServiceProvider")))))
                                .WithSemicolonToken(SemicolonToken)
                        ]))))))
            .NormalizeWhitespace();

        production.AddSource(context.TargetSymbol.ToDisplayString().NormalizeSymbolName(), syntax.ToFullString());
    }

    private static IEnumerable<StatementSyntax> GenerateStatements()
    {

    }

    private static void GenerateConstructorImplementations(SourceProductionContext production, ImmutableArray<AttributedGeneratorSymbolContext> contexts)
    {
        foreach (AttributedGeneratorSymbolContext context in contexts.DistinctBy(c => c.Symbol.ToDisplayString()))
        {
            GenerateConstructorImplementation(production, context);
        }
    }

    private static void GenerateConstructorImplementation(SourceProductionContext production, AttributedGeneratorSymbolContext context)
    {
        AttributeData constructorInfo = context.SingleAttribute(WellKnownAttributeNames.ConstructorGeneratedAttribute);

        bool resolveHttpClient = constructorInfo.HasNamedArgumentWith<bool>("ResolveHttpClient", value => value);
        bool callBaseConstructor = constructorInfo.HasNamedArgumentWith<bool>("CallBaseConstructor", value => value);
        bool initializeComponent = constructorInfo.HasNamedArgumentWith<bool>("InitializeComponent", value => value);
        string httpclient = resolveHttpClient ? ", System.Net.Http.HttpClient httpClient" : string.Empty;

        ConstructorOptions options = new(resolveHttpClient, callBaseConstructor, initializeComponent);

        StringBuilder sourceBuilder = new StringBuilder().Append($$"""
            namespace {{context.Symbol.ContainingNamespace}};

            partial class {{context.Symbol.ToDisplayString(SymbolDisplayFormats.QualifiedNonNullableFormat)}}
            {
                public {{context.Symbol.Name}}(System.IServiceProvider serviceProvider{{httpclient}}){{(options.CallBaseConstructor ? " : base(serviceProvider)" : string.Empty)}}
                {
                    PreConstruct(serviceProvider);

            """);

        FillUpWithFieldValueAssignment(sourceBuilder, context, options);
        StringBuilder propBuilder = FillUpWithPropertyAssignment(sourceBuilder, context, options);

        foreach (INamedTypeSymbol interfaceSymbol in context.Symbol.Interfaces)
        {
            if (interfaceSymbol.Name == "IRecipient")
            {
                sourceBuilder
                    .Append("        CommunityToolkit.Mvvm.Messaging.IMessengerExtensions.Register<")
                    .Append(interfaceSymbol.TypeArguments[0])
                    .AppendLine(">(serviceProvider.GetRequiredService<CommunityToolkit.Mvvm.Messaging.IMessenger>(), this);");
            }
        }

        if (options.InitializeComponent)
        {
            sourceBuilder.AppendLine("        InitializeComponent();");
        }

        sourceBuilder.AppendLine("""
                    PostConstruct(serviceProvider);
                }
            """);
        sourceBuilder.Append(propBuilder);
        sourceBuilder.Append("""
            
                partial void PreConstruct(System.IServiceProvider serviceProvider);
            
                partial void PostConstruct(System.IServiceProvider serviceProvider);

            """);
        sourceBuilder.Append("}");

        production.AddSource($"{context.Symbol.ToDisplayString().NormalizeSymbolName()}.ctor.g.cs", sourceBuilder.ToString());
    }

    private static void FillUpWithFieldValueAssignment(StringBuilder builder, AttributedGeneratorSymbolContext context2, ConstructorOptions options)
    {
        IEnumerable<IFieldSymbol> fields = context2.Symbol.GetMembers().Where(m => m.Kind is SymbolKind.Field).OfType<IFieldSymbol>();

        foreach (IFieldSymbol fieldSymbol in fields)
        {
            if (fieldSymbol.IsImplicitlyDeclared || fieldSymbol.Name.AsSpan()[0] is '<')
            {
                continue;
            }

            bool shouldSkip = false;
            foreach (SyntaxReference syntaxReference in fieldSymbol.DeclaringSyntaxReferences)
            {
                if (syntaxReference.GetSyntax() is VariableDeclaratorSyntax declarator)
                {
                    if (declarator.Initializer is not null)
                    {
                        // Skip field with initializer
                        builder.Append("        // Skipped field with initializer: ").AppendLine(fieldSymbol.Name);
                        shouldSkip = true;
                        break;
                    }
                }
            }

            if (shouldSkip)
            {
                continue;
            }

            if (!fieldSymbol.IsReadOnly || fieldSymbol.IsStatic)
            {
                continue;
            }

            switch (fieldSymbol.Type.ToDisplayString())
            {
                case "System.IServiceProvider":
                    builder
                        .Append("        this.")
                        .Append(fieldSymbol.Name)
                        .AppendLine(" = serviceProvider;");
                    break;

                case "System.Net.Http.HttpClient":
                    if (options.ResolveHttpClient)
                    {
                        builder
                            .Append("        this.")
                            .Append(fieldSymbol.Name)
                            .AppendLine(" = httpClient;");
                    }
                    else
                    {
                        builder
                            .Append("        this.")
                            .Append(fieldSymbol.Name)
                            .Append(" = serviceProvider.GetRequiredService<System.Net.Http.IHttpClientFactory>().CreateClient(nameof(")
                            .Append(context2.Symbol.Name)
                            .AppendLine("));");
                    }
                    break;

                default:
                    if (fieldSymbol.GetAttributes().SingleOrDefault(data => data.AttributeClass!.ToDisplayString() is WellKnownAttributeNames.FromKeyedServicesAttribute) is { } keyInfo)
                    {
                        string key = keyInfo.ConstructorArguments[0].ToCSharpString();
                        builder
                            .Append("        this.")
                            .Append(fieldSymbol.Name)
                            .Append(" = serviceProvider.GetRequiredKeyedService<")
                            .Append(fieldSymbol.Type)
                            .Append(">(")
                            .Append(key)
                            .AppendLine(");");
                    }
                    else
                    {
                        builder
                            .Append("        this.")
                            .Append(fieldSymbol.Name)
                            .Append(" = serviceProvider.GetRequiredService<")
                            .Append(fieldSymbol.Type)
                            .AppendLine(">();");
                    }
                    break;
            }
        }
    }

    private static StringBuilder FillUpWithPropertyAssignment(StringBuilder builder, AttributedGeneratorSymbolContext context2, ConstructorOptions options)
    {
        IEnumerable<IPropertySymbol> fields = context2.Symbol.GetMembers().Where(m => m.Kind is SymbolKind.Property).OfType<IPropertySymbol>();
        StringBuilder propsBuilder = new();

        foreach (IPropertySymbol propertySymbol in fields)
        {
            if (propertySymbol.IsImplicitlyDeclared)
            {
                continue;
            }

            if (!propertySymbol.IsPartialDefinition)
            {
                continue;
            }

            if (!propertySymbol.IsReadOnly || propertySymbol.IsStatic)
            {
                continue;
            }

            switch (propertySymbol.Type.ToDisplayString())
            {
                case "System.IServiceProvider":
                    builder
                        .Append("        ")
                        .Append(propertySymbol.Name)
                        .AppendLine(" = serviceProvider;");
                    break;

                case "System.Net.Http.HttpClient":
                    if (options.ResolveHttpClient)
                    {
                        builder
                            .Append("        ")
                            .Append(propertySymbol.Name)
                            .AppendLine(" = httpClient;");
                    }
                    else
                    {
                        builder
                            .Append("        ")
                            .Append(propertySymbol.Name)
                            .Append(" = serviceProvider.GetRequiredService<System.Net.Http.IHttpClientFactory>().CreateClient(nameof(")
                            .Append(context2.Symbol.Name)
                            .AppendLine("));");
                    }
                    break;

                default:
                    if (propertySymbol.GetAttributes().SingleOrDefault(data => data.AttributeClass!.ToDisplayString() is WellKnownAttributeNames.FromKeyedServicesAttribute) is { } keyInfo)
                    {
                        string key = keyInfo.ConstructorArguments[0].ToCSharpString();
                        builder
                            .Append("        ")
                            .Append(propertySymbol.Name)
                            .Append(" = serviceProvider.GetRequiredKeyedService<")
                            .Append(propertySymbol.Type)
                            .Append(">(")
                            .Append(key)
                            .AppendLine(");");
                    }
                    else
                    {
                        builder
                            .Append("        ")
                            .Append(propertySymbol.Name)
                            .Append(" = serviceProvider.GetRequiredService<")
                            .Append(propertySymbol.Type)
                            .AppendLine(">();");
                    }
                    break;
            }

            propsBuilder
                .AppendLine()
                .Append("    ")
                .Append(propertySymbol.DeclaredAccessibility.ToCSharpString())
                .Append(" partial ")
                .Append(propertySymbol.Type)
                .Append(" ")
                .Append(propertySymbol.Name)
                .AppendLine(" { get => field; }");
        }

        return propsBuilder;
    }

    private readonly struct ConstructorOptions
    {
        public readonly bool ResolveHttpClient;
        public readonly bool CallBaseConstructor;
        public readonly bool InitializeComponent;

        public ConstructorOptions(bool resolveHttpClient, bool callBaseConstructor, bool initializeComponent)
        {
            ResolveHttpClient = resolveHttpClient;
            CallBaseConstructor = callBaseConstructor;
            InitializeComponent = initializeComponent;
        }
    }
}