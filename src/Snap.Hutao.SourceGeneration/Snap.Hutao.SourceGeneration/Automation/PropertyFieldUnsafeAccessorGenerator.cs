// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snap.Hutao.SourceGeneration.Primitive;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Snap.Hutao.SourceGeneration.Automation;

[Generator(LanguageNames.CSharp)]
internal sealed class PropertyFieldUnsafeAccessorGenerator : IIncrementalGenerator
{
    private const string AttributeName = "Snap.Hutao.Core.Annotation.FieldAccessAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<ImmutableArray<AttributedGeneratorSymbolContext<IPropertySymbol>>> fieldAccessProperties =
            context.SyntaxProvider.CreateSyntaxProvider(FilterAttributedProperties, FieldAccessProperty)
                .Where(AttributedGeneratorSymbolContext<IPropertySymbol>.NotNull)
                .Collect();

        context.RegisterSourceOutput(fieldAccessProperties, FieldAccessPropertyImplementations);
    }

    private static bool FilterAttributedProperties(SyntaxNode node, CancellationToken cancellationToken)
    {
        return node is PropertyDeclarationSyntax propertyDeclaration && propertyDeclaration.HasAttributeLists();
    }

    private static AttributedGeneratorSymbolContext<IPropertySymbol> FieldAccessProperty(GeneratorSyntaxContext context, CancellationToken token)
    {
        if (context.TryGetDeclaredSymbol(token, out IPropertySymbol? propertySymbol))
        {
            ImmutableArray<AttributeData> attributes = propertySymbol.GetAttributes();
            if (attributes.Any(data => data.AttributeClass!.ToDisplayString() == AttributeName))
            {
                return new(context, propertySymbol, attributes);
            }
        }

        return default;
    }

    private static void FieldAccessPropertyImplementations(SourceProductionContext production, ImmutableArray<AttributedGeneratorSymbolContext<IPropertySymbol>> context2s)
    {
        foreach (AttributedGeneratorSymbolContext<IPropertySymbol> context2 in context2s.DistinctBy(c => c.Symbol.ToDisplayString()))
        {
            FieldAccessPropertyImplementation(production, context2);
        }
    }

    private static void FieldAccessPropertyImplementation(SourceProductionContext production, AttributedGeneratorSymbolContext<IPropertySymbol> context)
    {
        INamedTypeSymbol typeSymbol = context.Symbol.ContainingType;
        string propertyName = context.Symbol.Name;
        string fieldName = $"<{propertyName}>k__BackingField";

        string typeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        string propertyTypeName = context.Symbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        string code = $$"""
            using System.Runtime.CompilerServices;
            
            namespace {{typeSymbol.ContainingNamespace.ToDisplayString()}}
            {
                partial class {{typeName}}
                {
                    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "{{fieldName}}")]
                    private static extern ref {{propertyTypeName}} FieldRefOf{{propertyName}}({{typeSymbol}} self);
                }
            }
            """;

        production.AddSource($"{typeSymbol.ToDisplayString().NormalizeSymbolName()}.{propertyName}.g.cs", code);
    }
}