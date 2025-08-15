// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Snap.Hutao.SourceGeneration.Extension;
using System.Collections.Immutable;

namespace Snap.Hutao.SourceGeneration;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class UniversalAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor TypeInternalDescriptor = new("SH001", "Type should be internal", "Type [{0}] should be internal or private", "Quality", DiagnosticSeverity.Info, true);
    // SH002: ReadOnly struct should be passed with ref-like key word
    private static readonly DiagnosticDescriptor UseValueTaskIfPossibleDescriptor = new("SH003", "Use ValueTask instead of Task whenever possible", "Use ValueTask instead of Task", "Quality", DiagnosticSeverity.Info, true);
    // SH004: Use "is not null" instead of "!= null" whenever possible
    // SH005: Use "is null" instead of "== null" whenever possible
    // SH006: Use "is { } obj" whenever possible
    private static readonly DiagnosticDescriptor UseArgumentNullExceptionThrowIfNullDescriptor = new("SH007", "Use \"ArgumentNullException.ThrowIfNull()\" instead of \"!\"", "Use \"ArgumentNullException.ThrowIfNull()\"", "Quality", DiagnosticSeverity.Info, true);
    private static readonly DiagnosticDescriptor EquatableInterfaceTypeShouldBeNullableDescriptor = new("SH008", "The type parameter of IEquatable<T> interface should be nullable", "The type parameter of IEquatable<T> interface should be nullable", "Quality", DiagnosticSeverity.Info, true);
    // SH020: File header mismatch
    // SH100: Use "IContentDialogFactory.EnqueueAndShowAsync" instead

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get =>
        [
            TypeInternalDescriptor,
            UseValueTaskIfPossibleDescriptor,
            UseArgumentNullExceptionThrowIfNullDescriptor,
        ];
    }

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(CompilationStart);
    }

    private static void CompilationStart(CompilationStartAnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(HandleTypeShouldBeInternal, SyntaxKind.ClassDeclaration, SyntaxKind.InterfaceDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.EnumDeclaration);
        context.RegisterSyntaxNodeAction(HandleMethodReturnTypeShouldUseValueTaskInsteadOfTask, SyntaxKind.MethodDeclaration);
        context.RegisterSyntaxNodeAction(HandleArgumentNullExceptionThrowIfNull, SyntaxKind.SuppressNullableWarningExpression);
    }

    // SH001
    private static void HandleTypeShouldBeInternal(SyntaxNodeAnalysisContext context)
    {
        BaseTypeDeclarationSyntax syntax = (BaseTypeDeclarationSyntax)context.Node;

        bool privateExists = false;
        bool internalExists = false;
        bool fileExists = false;

        foreach (SyntaxToken token in syntax.Modifiers)
        {
            if (token.IsKind(SyntaxKind.PrivateKeyword))
            {
                privateExists = true;
                break;
            }

            if (token.IsKind(SyntaxKind.InternalKeyword))
            {
                internalExists = true;
                break;
            }

            if (token.IsKind(SyntaxKind.FileKeyword))
            {
                fileExists = true;
                break;
            }
        }

        if (!privateExists && !internalExists && !fileExists)
        {
            Location location = syntax.Identifier.GetLocation();
            Diagnostic diagnostic = Diagnostic.Create(TypeInternalDescriptor, location, syntax.Identifier);
            context.ReportDiagnostic(diagnostic);
        }
    }

    // SH003
    private static void HandleMethodReturnTypeShouldUseValueTaskInsteadOfTask(SyntaxNodeAnalysisContext context)
    {
        MethodDeclarationSyntax methodSyntax = (MethodDeclarationSyntax)context.Node;
        IMethodSymbol methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodSyntax)!;

        // 跳过重载方法
        if (methodSymbol.IsOverride)
        {
            return;
        }

        // AsyncRelayCommand backing method can only use Task or Task<T>
        if (methodSymbol.HasAttributeWithFullyQualifiedMetadataName(WellKnownMetadataNames.CommandAttribute))
        {
            return;
        }

        if (methodSymbol.ReturnType.HasOrInheritsFromFullyQualifiedMetadataName("System.Threading.Tasks.Task"))
        {
            Location location = methodSyntax.ReturnType.GetLocation();
            Diagnostic diagnostic = Diagnostic.Create(UseValueTaskIfPossibleDescriptor, location);
            context.ReportDiagnostic(diagnostic);
        }
    }

    // SH007
    private static void HandleArgumentNullExceptionThrowIfNull(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not PostfixUnaryExpressionSyntax syntax || syntax.Kind() is not SyntaxKind.SuppressNullableWarningExpression)
        {
            return;
        }

        // default!
        if (syntax.Operand is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.DefaultLiteralExpression))
        {
            return;
        }

        // default(?)!
        if (syntax.Operand is DefaultExpressionSyntax)
        {
            return;
        }

        Location location = syntax.GetLocation();
        Diagnostic diagnostic = Diagnostic.Create(UseArgumentNullExceptionThrowIfNullDescriptor, location);
        context.ReportDiagnostic(diagnostic);
    }
}