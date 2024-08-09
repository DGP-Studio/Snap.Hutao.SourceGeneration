// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snap.Hutao.SourceGeneration.Primitive;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

namespace Snap.Hutao.SourceGeneration.Xaml;

[Generator(LanguageNames.CSharp)]
internal sealed class AdvancedCollectionViewItemGenerator : IIncrementalGenerator
{
    public const string InterfaceName = "Snap.Hutao.UI.Xaml.Data.IAdvancedCollectionViewItem";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<ImmutableArray<GeneratorSymbolContext>> commands = context.SyntaxProvider
            .CreateSyntaxProvider(FilterClassWithBaseList, ItemClass)
            .Where(GeneratorSymbolContext.NotNull)
            .Collect();

        context.RegisterImplementationSourceOutput(commands, GenerateInterfaceImplementations);
    }

    private static bool FilterClassWithBaseList(SyntaxNode node, CancellationToken token)
    {
        return node is ClassDeclarationSyntax { BaseList: not null };
    }

    private static GeneratorSymbolContext ItemClass(GeneratorSyntaxContext context, CancellationToken token)
    {
        if (context.TryGetDeclaredSymbol(token, out INamedTypeSymbol? classSymbol))
        {
            ImmutableArray<INamedTypeSymbol> interfaces = classSymbol.Interfaces;
            if (interfaces.Any(symbol => symbol.ToDisplayString() == InterfaceName))
            {
                return new(context, classSymbol);
            }
        }

        return default;
    }

    private static void GenerateInterfaceImplementations(SourceProductionContext production, ImmutableArray<GeneratorSymbolContext> context3s)
    {
        foreach (GeneratorSymbolContext context3 in context3s.DistinctBy(c => c.Symbol.ToDisplayString()))
        {
            GenerateInterfaceImplementation(production, context3);
        }
    }

    private static void GenerateInterfaceImplementation(SourceProductionContext production, GeneratorSymbolContext context3)
    {
        context3.Symbol.GetMembers().OfType<IPropertySymbol>();
        StringBuilder sourceBuilder = new StringBuilder().Append($$"""
            // Copyright (c) DGP Studio. All rights reserved.
            // Licensed under the MIT license.

            using Microsoft.UI.Xaml;
            
            namespace {{context3.Symbol.ContainingNamespace}};
            
            #nullable enable
            partial class {{context3.Symbol.ToDisplayString(SymbolDisplayFormats.QualifiedNonNullableFormat)}}
            {
                [global::System.CodeDom.Compiler.GeneratedCodeAttribute("{{nameof(AdvancedCollectionViewItemGenerator)}}", "1.0.0.0")]
                public object? GetPropertyValue(string propertyName)
                {
                    return propertyName switch
                    {
            {{FillProperties(context3.Symbol)}}        _ => default,
                    };
                }
            }
            """);


        production.AddSource($"{context3.Symbol.ToDisplayString().NormalizeSymbolName()}.g.cs", sourceBuilder.ToString());
    }

    private static string FillProperties(INamedTypeSymbol classSymbol)
    {
        StringBuilder stringBuilder = new();
        foreach (IPropertySymbol property in classSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            stringBuilder.AppendLine($"""
                        nameof({property.Name}) => {property.Name},
            """);
        }

        return stringBuilder.ToString();
    }
}