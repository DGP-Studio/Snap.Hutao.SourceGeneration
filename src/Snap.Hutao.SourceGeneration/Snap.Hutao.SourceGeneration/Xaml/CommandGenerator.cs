// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snap.Hutao.SourceGeneration.Extension;
using Snap.Hutao.SourceGeneration.Primitive;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Snap.Hutao.SourceGeneration.Xaml;

[Generator(LanguageNames.CSharp)]
internal sealed class CommandGenerator : IIncrementalGenerator
{
    public const string AttributeName = "Snap.Hutao.Core.Annotation.CommandAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<ImmutableArray<AttributedGeneratorSymbolContext<IMethodSymbol>>> commands = context.SyntaxProvider
            .CreateSyntaxProvider(FilterAttributedMethods, CommandMethod)
            .Where(AttributedGeneratorSymbolContext<IMethodSymbol>.NotNull)
            .Collect();

        context.RegisterImplementationSourceOutput(commands, GenerateCommandImplementations);
    }

    private static bool FilterAttributedMethods(SyntaxNode node, CancellationToken token)
    {
        return node is MethodDeclarationSyntax methodDeclarationSyntax
            && methodDeclarationSyntax.Parent is ClassDeclarationSyntax classDeclarationSyntax
            && classDeclarationSyntax.Modifiers.Count > 1
            && methodDeclarationSyntax.HasAttributeLists();
    }

    private static AttributedGeneratorSymbolContext<IMethodSymbol> CommandMethod(GeneratorSyntaxContext context, CancellationToken token)
    {
        if (context.TryGetDeclaredSymbol(token, out IMethodSymbol? methodSymbol))
        {
            ImmutableArray<AttributeData> attributes = methodSymbol.GetAttributes();
            if (attributes.Any(data => data.AttributeClass!.ToDisplayString() == AttributeName))
            {
                return new(context, methodSymbol, attributes);
            }
        }

        return default;
    }

    private static void GenerateCommandImplementations(SourceProductionContext production, ImmutableArray<AttributedGeneratorSymbolContext<IMethodSymbol>> context2s)
    {
        foreach (AttributedGeneratorSymbolContext<IMethodSymbol> context2 in context2s.DistinctBy(c => c.Symbol.ToDisplayString()))
        {
            GenerateCommandImplementation(production, context2);
        }
    }

    private static void GenerateCommandImplementation(SourceProductionContext production, AttributedGeneratorSymbolContext<IMethodSymbol> context2)
    {
        INamedTypeSymbol classSymbol = context2.Symbol.ContainingType;

        AttributeData commandInfo = context2.SingleAttribute(AttributeName);
        string commandName = (string)commandInfo.ConstructorArguments[0].Value!;

        string? canExecute = commandInfo.ConstructorArguments.ElementAtOrDefault(1).Value as string;
        string canExecuteParameter = canExecute is not null
            ? $", {canExecute}"
            : string.Empty;

        string commandType = context2.Symbol.ReturnType.HasOrInheritsFromFullyQualifiedMetadataName("System.Threading.Tasks.Task")
            ? "AsyncRelayCommand"
            : "RelayCommand";

        string genericParameter = context2.Symbol.Parameters.ElementAtOrDefault(0) is IParameterSymbol parameter
            ? $"<{parameter.Type.ToDisplayString(SymbolDisplayFormats.NonNullableQualifiedFormat)}>"
            : string.Empty;

        string commandFullType = $"{commandType}{genericParameter}";

        string concurrentExecution = commandInfo.HasNamedArgument<bool>("AllowConcurrentExecutions", value => value)
            ? ", AsyncRelayCommandOptions.AllowConcurrentExecutions"
            : string.Empty;

        string className = classSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        string code = $$"""
            using CommunityToolkit.Mvvm.Input;

            namespace {{classSymbol.ContainingNamespace}};

            partial class {{className}}
            {
                [field: MaybeNull]
                public {{commandFullType}} {{commandName}}
                {
                    get => field ??= new {{commandFullType}}({{context2.Symbol.Name}}{{canExecuteParameter}}{{concurrentExecution}});
                }
            }
            """;

        production.AddSource($"{classSymbol.NormalizedFullyQualifiedName()}.{commandName}.g.cs", code);
    }
}