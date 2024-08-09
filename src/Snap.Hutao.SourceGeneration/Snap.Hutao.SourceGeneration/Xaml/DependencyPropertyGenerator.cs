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
using System.Threading;

namespace Snap.Hutao.SourceGeneration.Xaml;

[Generator(LanguageNames.CSharp)]
internal sealed class DependencyPropertyGenerator : IIncrementalGenerator
{
    private const string AttributeName = "Snap.Hutao.Core.Annotation.DependencyPropertyAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<ImmutableArray<AttributedGeneratorSymbolContext>> commands =
            context.SyntaxProvider.CreateSyntaxProvider(FilterAttributedClasses, CommandMethod)
            .Where(AttributedGeneratorSymbolContext.NotNull)
            .Collect();

        context.RegisterImplementationSourceOutput(commands, GenerateDependencyPropertyImplementations);
    }

    private static bool FilterAttributedClasses(SyntaxNode node, CancellationToken token)
    {
        return node is ClassDeclarationSyntax classDeclarationSyntax
            && classDeclarationSyntax.Modifiers.Count > 1
            && classDeclarationSyntax.HasAttributeLists();
    }

    private static AttributedGeneratorSymbolContext CommandMethod(GeneratorSyntaxContext context, CancellationToken token)
    {
        if (context.TryGetDeclaredSymbol(token, out INamedTypeSymbol? methodSymbol))
        {
            ImmutableArray<AttributeData> attributes = methodSymbol.GetAttributes();
            if (attributes.Any(data => data.AttributeClass!.ToDisplayString() == AttributeName))
            {
                return new(context, methodSymbol, attributes);
            }
        }

        return default;
    }

    private static void GenerateDependencyPropertyImplementations(SourceProductionContext production, ImmutableArray<AttributedGeneratorSymbolContext> contexts)
    {
        foreach (AttributedGeneratorSymbolContext context2 in contexts.DistinctBy(c => c.Symbol.ToDisplayString()))
        {
            GenerateDependencyPropertyImplementation(production, context2);
        }
    }

    private static void GenerateDependencyPropertyImplementation(SourceProductionContext production, AttributedGeneratorSymbolContext context)
    {
        foreach (AttributeData propertyInfo in context.Attributes.Where(attr => attr.AttributeClass!.ToDisplayString() is AttributeName))
        {
            string owner = context.Symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            Dictionary<string, TypedConstant> namedArguments = propertyInfo.NamedArguments.ToDictionary();
            bool isAttached = namedArguments.TryGetValue("IsAttached", out TypedConstant constant) && (bool)constant.Value!;
            string register = isAttached ? "RegisterAttached" : "Register";

            ImmutableArray<TypedConstant> constructorArguments = propertyInfo.ConstructorArguments;

            string propertyName = (string)constructorArguments[0].Value!;
            string propertyType = constructorArguments[1].Value!.ToString();
            string defaultValue = constructorArguments.ElementAtOrDefault(2).ToCSharpString() ?? "default";

            if (defaultValue is "null")
            {
                defaultValue = "default";
            }

            if (namedArguments.TryGetValue("RawDefaultValue", out TypedConstant rawDefaultValue))
            {
                defaultValue = (string)rawDefaultValue.Value!;
            }

            string propertyChangedCallback = constructorArguments.ElementAtOrDefault(3) is { IsNull: false } callbackName ? $", {callbackName.Value}" : string.Empty;

            string code;
            if (isAttached)
            {
                string objType = namedArguments.TryGetValue("AttachedType", out TypedConstant attachedType)
                    ? attachedType.Value!.ToString()
                    : "object";

                code = $$"""
                    using Microsoft.UI.Xaml;

                    namespace {{context.Symbol.ContainingNamespace}};

                    partial class {{owner}}
                    {
                        private static readonly DependencyProperty {{propertyName}}Property =
                            DependencyProperty.RegisterAttached(
                                "{{propertyName}}",
                                typeof({{propertyType}}),
                                typeof({{owner}}),
                                new PropertyMetadata({{defaultValue}}{{propertyChangedCallback}}));

                        public static {{propertyType}} Get{{propertyName}}({{objType}} obj)
                        {
                            return ({{propertyType}})obj?.GetValue({{propertyName}}Property);
                        }

                        public static void Set{{propertyName}}({{objType}} obj, {{propertyType}} value)
                        {
                            obj.SetValue({{propertyName}}Property, value);
                        }
                    }
                    """;
            }
            else
            {
                code = $$"""
                    using Microsoft.UI.Xaml;

                    namespace {{context.Symbol.ContainingNamespace}};

                    partial class {{owner}}
                    {
                        private static readonly DependencyProperty {{propertyName}}Property =
                            DependencyProperty.Register(
                                nameof({{propertyName}}),
                                typeof({{propertyType}}),
                                typeof({{owner}}),
                                new PropertyMetadata({{defaultValue}}{{propertyChangedCallback}}));

                        public {{propertyType}} {{propertyName}}
                        {
                            get => ({{propertyType}})GetValue({{propertyName}}Property);
                            set => SetValue({{propertyName}}Property, value);
                        }
                    }
                    """;
            }

            production.AddSource($"{context.Symbol.ToDisplayString().NormalizeSymbolName()}.{propertyName}.g.cs", code);
        }
    }
}