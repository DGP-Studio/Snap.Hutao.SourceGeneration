using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;
using Snap.Hutao.SourceGeneration.Primitive;
using System.Collections.Immutable;

namespace Snap.Hutao.SourceGeneration.Xaml;

[Generator]
internal class XamlUnloadObjectOverrideGenerator : IIncrementalGenerator
{
    private const string ClassName = "Snap.Hutao.UI.Xaml.Control.ScopedPage";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<ImmutableArray<GeneratorSymbolContext>> inheritClasses = context.SyntaxProvider
            .CreateSyntaxProvider(FilterClassWithBaseList, ScopedPageInheritClass)
            .Where(GeneratorSymbolContext.NotNull)
            .Collect();

        context.RegisterSourceOutput(inheritClasses, GenerateUnloadObjectOverrideImplementations);
    }

    private static bool FilterClassWithBaseList(SyntaxNode node, CancellationToken token)
    {
        return node is ClassDeclarationSyntax { BaseList: not null };
    }

    private static GeneratorSymbolContext ScopedPageInheritClass(GeneratorSyntaxContext context, CancellationToken token)
    {
        if (context.TryGetDeclaredSymbol(token, out INamedTypeSymbol? classSymbol))
        {
            if (classSymbol.BaseType?.ToDisplayString() is ClassName)
            {
                return new(context, classSymbol);
            }
        }

        return default;
    }

    private static void GenerateUnloadObjectOverrideImplementations(SourceProductionContext production, ImmutableArray<GeneratorSymbolContext> context3s)
    {
        foreach (GeneratorSymbolContext context3 in context3s.DistinctBy(c => c.Symbol.ToDisplayString()))
        {
            GenerateUnloadObjectOverrideImplementation(production, context3);
        }
    }

    private static void GenerateUnloadObjectOverrideImplementation(SourceProductionContext production, GeneratorSymbolContext context3)
    {
        StringBuilder sourceBuilder = new StringBuilder().Append($$"""
            // Copyright (c) DGP Studio. All rights reserved.
            // Licensed under the MIT license.

            using Microsoft.UI.Xaml;
            
            namespace {{context3.Symbol.ContainingNamespace}};
            
            partial class {{context3.Symbol.ToDisplayString(SymbolDisplayFormats.NonNullableFormat)}}
            {
                [global::System.CodeDom.Compiler.GeneratedCodeAttribute("{{nameof(XamlUnloadObjectOverrideGenerator)}}", "1.0.0.0")]
                public override void UnloadObjectOverride(DependencyObject unloadableObject)
                {
                    UnloadObject(unloadableObject);
                }
            }
            """);

        production.AddSource($"{context3.Symbol.ToDisplayString().NormalizeSymbolName()}.ctor.g.cs", sourceBuilder.ToString());
    }
}