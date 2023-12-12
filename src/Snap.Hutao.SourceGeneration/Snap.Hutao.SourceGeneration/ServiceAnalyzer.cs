using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Snap.Hutao.SourceGeneration;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class ServiceAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor singletonCaptureDescriptor = new("SH051", "Dangerous service capture", "Singleton service should not capture Scpoed or Transient service: {0}", "Quality", DiagnosticSeverity.Warning, true);
    private static readonly DiagnosticDescriptor unknownServiceLifeTimeDescriptor = new("SH052", "Service LifeTime unknown", "Service {0} has a unknown lifetime", "Quality", DiagnosticSeverity.Warning, true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get
        {
            return new DiagnosticDescriptor[]
            {
                singletonCaptureDescriptor,
                unknownServiceLifeTimeDescriptor,
            }.ToImmutableArray();
        }
    }

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(CompilationStart);
    }

    private static void CompilationStart(CompilationStartAnalysisContext context)
    {
        // TODO add analyzer for unnecessary IServiceProvider registration
        // TODO add analyzer for Singlton service use Scoped or Transient services
        context.RegisterSyntaxNodeAction(HandleSingletonServiceShouldNotCaptureScoedOrTransientService, SyntaxKind.ClassDeclaration);
    }

    private static void HandleSingletonServiceShouldNotCaptureScoedOrTransientService(SyntaxNodeAnalysisContext context)
    {
        ClassDeclarationSyntax syntax = (ClassDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(syntax) is not { } typeSymbol)
        {
            return;
        }

        if (!TypeIsSingletonService(typeSymbol))
        {
            return;
        }

        foreach (IFieldSymbol fieldSymbol in typeSymbol.GetMembers().Where(m => m.Kind == SymbolKind.Field).OfType<IFieldSymbol>())
        {
            if (fieldSymbol.Name.AsSpan()[0] is '<')
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
                        shoudSkip = true;
                        break;
                    }
                }
            }

            if (shoudSkip)
            {
                continue;
            }

            if (fieldSymbol.IsReadOnly && !fieldSymbol.IsStatic)
            {
                string fieldTypeString = fieldSymbol.Type.ToDisplayString();
                switch (fieldTypeString)
                {
                    case "System.IServiceProvider":
                        break;
                    case "System.Net.Http.HttpClient":
                        context.ReportDiagnostic(Diagnostic.Create(singletonCaptureDescriptor, syntax.GetLocation(), fieldTypeString));
                        break;
                    default:
                        {
                            if (TypeIsScopedOrTransientService(fieldSymbol.Type))
                            {
                                context.ReportDiagnostic(Diagnostic.Create(singletonCaptureDescriptor, syntax.GetLocation(), fieldTypeString));
                            }
                            else
                            {
                                context.ReportDiagnostic(Diagnostic.Create(unknownServiceLifeTimeDescriptor, syntax.GetLocation(), fieldTypeString));
                            }
                        }
                        break;
                }
            }
        }
    }

    private static bool TypeIsSingletonService(ITypeSymbol typeSymbol)
    {
        bool typeIsSingleton = false;
        foreach (AttributeData attributeData in typeSymbol.GetAttributes())
        {
            if (attributeData.AttributeClass is not { } attrTypeSymbol)
            {
                continue;
            }

            if (attrTypeSymbol.ToDisplayString() is not "Snap.Hutao.Core.DependencyInjection.Annotation.InjectionAttribute")
            {
                continue;
            }

            if (attributeData.ConstructorArguments.Length <= 0)
            {
                continue;
            }

            TypedConstant first = attributeData.ConstructorArguments[0];
            if (first.ToCSharpString() is "Snap.Hutao.Core.DependencyInjection.Annotation.InjectAs.Singleton")
            {
                typeIsSingleton = true;
            }
        }

        return typeIsSingleton;
    }

    private static bool TypeIsScopedOrTransientService(ITypeSymbol typeSymbol)
    {
        bool typeIsScopedOrTransient = false;
        foreach (AttributeData attributeData in typeSymbol.GetAttributes())
        {
            if (attributeData.AttributeClass is not { } attrTypeSymbol)
            {
                continue;
            }

            if (attrTypeSymbol.ToDisplayString() is not "Snap.Hutao.Core.DependencyInjection.Annotation.InjectionAttribute")
            {
                continue;
            }

            if (attributeData.ConstructorArguments.Length <= 0)
            {
                continue;
            }

            TypedConstant first = attributeData.ConstructorArguments[0];
            if (first.ToCSharpString()
                is "Snap.Hutao.Core.DependencyInjection.Annotation.InjectAs.Scoped"
                or "Snap.Hutao.Core.DependencyInjection.Annotation.InjectAs.Transient")
            {
                typeIsScopedOrTransient = true;
            }
        }

        return typeIsScopedOrTransient;
    }
}
