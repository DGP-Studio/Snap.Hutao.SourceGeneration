﻿// Copyright (c) DGP Studio. All rights reserved.
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
using System.Threading;

namespace Snap.Hutao.SourceGeneration.Automation;

[Generator(LanguageNames.CSharp)]
internal sealed class ConstructorGenerator : IIncrementalGenerator
{
    private const string AttributeName = "Snap.Hutao.Core.Annotation.ConstructorGeneratedAttribute";
    private const string FromKeyedServices = "Snap.Hutao.Core.DependencyInjection.Annotation.FromKeyedServicesAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<ImmutableArray<AttributedGeneratorSymbolContext>> injectionClasses =
            context.SyntaxProvider.CreateSyntaxProvider(FilterAttributedClasses, ConstructorGeneratedClass)
            .Where(AttributedGeneratorSymbolContext.NotNull)
            .Collect();

        context.RegisterSourceOutput(injectionClasses, GenerateConstructorImplementations);
    }

    private static bool FilterAttributedClasses(SyntaxNode node, CancellationToken token)
    {
        return node is ClassDeclarationSyntax classDeclarationSyntax
            && classDeclarationSyntax.Modifiers.Count > 1
            && classDeclarationSyntax.HasAttributeLists();
    }

    private static AttributedGeneratorSymbolContext ConstructorGeneratedClass(GeneratorSyntaxContext context, CancellationToken token)
    {
        if (context.TryGetDeclaredSymbol(token, out INamedTypeSymbol? classSymbol))
        {
            ImmutableArray<AttributeData> attributes = classSymbol.GetAttributes();
            if (attributes.Any(data => data.AttributeClass!.ToDisplayString() is AttributeName))
            {
                return new(context, classSymbol, attributes);
            }
        }

        return default;
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
        AttributeData constructorInfo = context.SingleAttribute(AttributeName);

        bool injectToPropertiesInstead = constructorInfo.HasNamedArgumentWith<bool>("InjectToPropertiesInstead", value => value);
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

            """);

        if (injectToPropertiesInstead)
        {
            FillUpWithPropertyAssignment(sourceBuilder, context, options);
        }
        else
        {
            FillUpWithFieldValueAssignment(sourceBuilder, context, options);
        }

        sourceBuilder.Append("""
                }
            }
            """);

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

            bool shoudSkip = false;
            foreach (SyntaxReference syntaxReference in fieldSymbol.DeclaringSyntaxReferences)
            {
                if (syntaxReference.GetSyntax() is VariableDeclaratorSyntax declarator)
                {
                    if (declarator.Initializer is not null)
                    {
                        // Skip field with initializer
                        builder.Append("        // Skipped field with initializer: ").AppendLine(fieldSymbol.Name);
                        shoudSkip = true;
                        break;
                    }
                }
            }

            if (shoudSkip)
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
                    if (fieldSymbol.GetAttributes().SingleOrDefault(data => data.AttributeClass!.ToDisplayString() is FromKeyedServices) is { } keyInfo)
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

        foreach (INamedTypeSymbol interfaceSymbol in context2.Symbol.Interfaces)
        {
            if (interfaceSymbol.Name == "IRecipient")
            {
                builder
                    .Append("        CommunityToolkit.Mvvm.Messaging.IMessengerExtensions.Register<")
                    .Append(interfaceSymbol.TypeArguments[0])
                    .AppendLine(">(serviceProvider.GetRequiredService<CommunityToolkit.Mvvm.Messaging.IMessenger>(), this);");
            }
        }

        if (options.InitializeComponent)
        {
            builder.AppendLine("        InitializeComponent();");
        }
    }

    private static void FillUpWithPropertyAssignment(StringBuilder builder, AttributedGeneratorSymbolContext context2, ConstructorOptions options)
    {
        IEnumerable<IPropertySymbol> fields = context2.Symbol.GetMembers().Where(m => m.Kind is SymbolKind.Property).OfType<IPropertySymbol>();

        foreach (IPropertySymbol propertySymbol in fields)
        {
            if (propertySymbol.IsImplicitlyDeclared)
            {
                continue;
            }

            bool shoudSkip = false;
            foreach (SyntaxReference syntaxReference in propertySymbol.DeclaringSyntaxReferences)
            {
                if (syntaxReference.GetSyntax() is PropertyDeclarationSyntax declarationSyntax)
                {
                    if (declarationSyntax.Initializer is not null)
                    {
                        // Skip property with initializer
                        builder.Append("        // Skipped property with initializer: ").AppendLine(propertySymbol.Name);
                        shoudSkip = true;
                        break;
                    }

                    if (declarationSyntax.AccessorList is not null)
                    {
                        foreach (AccessorDeclarationSyntax accessorDeclaration in declarationSyntax.AccessorList.Accessors)
                        {
                            if (accessorDeclaration.Kind() is SyntaxKind.GetAccessorDeclaration)
                            {
                                if (accessorDeclaration.ExpressionBody is not null || accessorDeclaration.Body is not null)
                                {
                                    // Skip property with body
                                    builder.Append("        // Skipped property with expression body: ").AppendLine(propertySymbol.Name);
                                    shoudSkip = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            if (shoudSkip)
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
                    if (propertySymbol.GetAttributes().SingleOrDefault(data => data.AttributeClass!.ToDisplayString() is FromKeyedServices) is { } keyInfo)
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
        }

        foreach (INamedTypeSymbol interfaceSymbol in context2.Symbol.Interfaces)
        {
            if (interfaceSymbol.Name == "IRecipient")
            {
                builder
                    .Append("        CommunityToolkit.Mvvm.Messaging.IMessengerExtensions.Register<")
                    .Append(interfaceSymbol.TypeArguments[0])
                    .AppendLine(">(serviceProvider.GetRequiredService<CommunityToolkit.Mvvm.Messaging.IMessenger>(), this);");
            }
        }

        if (options.InitializeComponent)
        {
            builder.AppendLine("        InitializeComponent();");
        }
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