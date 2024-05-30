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
    private const string ClassName = "Snap.Hutao.Control.ScopedPage";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<GeneratorSyntaxContext3> inheritClasses = context.SyntaxProvider
            .CreateSyntaxProvider(FilterClassWithBaseList, ScopedPageInheritClass)
            .Where(GeneratorSyntaxContext3.NotNull);

        context.RegisterSourceOutput(inheritClasses, GenerateUnloadObjectOverrideImplementation);
    }

    private static bool FilterClassWithBaseList(SyntaxNode node, CancellationToken token)
    {
        return node is ClassDeclarationSyntax { BaseList: not null };
    }

    private static GeneratorSyntaxContext3 ScopedPageInheritClass(GeneratorSyntaxContext context, CancellationToken token)
    {
        if (context.TryGetDeclaredSymbol(token, out INamedTypeSymbol? classSymbol))
        {
            if (classSymbol.BaseType?.ToDisplayString() == ClassName)
            {
                return new(context, classSymbol);
            }
        }

        return default;
    }

    private static void GenerateUnloadObjectOverrideImplementation(SourceProductionContext production, GeneratorSyntaxContext3 context3)
    {
        StringBuilder sourceBuilder = new StringBuilder().Append($$"""
            // Copyright (c) DGP Studio. All rights reserved.
            // Licensed under the MIT license.

            using Microsoft.UI.Xaml;
            
            namespace {{context3.Symbol.ContainingNamespace}};
            
            partial class {{context3.Symbol.ToDisplayString(SymbolDisplayFormats.QualifiedNonNullableFormat)}}
            {
                [global::System.CodeDom.Compiler.GeneratedCodeAttribute("{{nameof(XamlUnloadObjectOverrideGenerator)}}", "1.0.0.0")]
                public override void UnloadObjectOverride(DependencyObject unloadableObject)
                {
                    UnloadObject(unloadableObject);
                }
            }
            """);

        string normalizedClassName = new StringBuilder(context3.Symbol.ToDisplayString()).Replace('<', '{').Replace('>', '}').ToString();
        production.AddSource($"{normalizedClassName}.ctor.g.cs", sourceBuilder.ToString());
    }
}