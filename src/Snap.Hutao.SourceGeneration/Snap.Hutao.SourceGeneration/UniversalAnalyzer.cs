using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Snap.Hutao.SourceGeneration.Primitive;
using Snap.Hutao.SourceGeneration.Xaml;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Snap.Hutao.SourceGeneration;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class UniversalAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor typeInternalDescriptor = new("SH001", "Type should be internal", "Type [{0}] should be internal", "Quality", DiagnosticSeverity.Info, true);
    private static readonly DiagnosticDescriptor useValueTaskIfPossibleDescriptor = new("SH003", "Use ValueTask instead of Task whenever possible", "Use ValueTask instead of Task", "Quality", DiagnosticSeverity.Info, true);
    private static readonly DiagnosticDescriptor useIsNotNullPatternMatchingDescriptor = new("SH004", "Use \"is not null\" instead of \"!= null\" whenever possible", "Use \"is not null\" instead of \"!= null\"", "Quality", DiagnosticSeverity.Info, true);
    private static readonly DiagnosticDescriptor useIsNullPatternMatchingDescriptor = new("SH005", "Use \"is null\" instead of \"== null\" whenever possible", "Use \"is null\" instead of \"== null\"", "Quality", DiagnosticSeverity.Info, true);
    private static readonly DiagnosticDescriptor useIsPatternRecursiveMatchingDescriptor = new("SH006", "Use \"is { } obj\" whenever possible", "Use \"is {{ }} {0}\"", "Quality", DiagnosticSeverity.Info, true);
    private static readonly DiagnosticDescriptor useArgumentNullExceptionThrowIfNullDescriptor = new("SH007", "Use \"ArgumentNullException.ThrowIfNull()\" instead of \"!\"", "Use \"ArgumentNullException.ThrowIfNull()\"", "Quality", DiagnosticSeverity.Info, true);

    private static readonly DiagnosticDescriptor avoidUsingContentDialogShowAsyncDescriptor = new("SH100", "Use \"IContentDialogFactory.EnqueueAndShowAsync\" instead", "Use \"IContentDialogFactory.EnqueueAndShowAsync\" instead", "Quality", DiagnosticSeverity.Warning, true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get
        {
            return new DiagnosticDescriptor[]
            {
                typeInternalDescriptor,
                useValueTaskIfPossibleDescriptor,
                useIsNotNullPatternMatchingDescriptor,
                useIsNullPatternMatchingDescriptor,
                useIsPatternRecursiveMatchingDescriptor,
                useArgumentNullExceptionThrowIfNullDescriptor,
                avoidUsingContentDialogShowAsyncDescriptor
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
        context.RegisterSyntaxNodeAction(HandleTypeShouldBeInternal, SyntaxKind.ClassDeclaration, SyntaxKind.InterfaceDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.EnumDeclaration);
        context.RegisterSyntaxNodeAction(HandleMethodReturnTypeShouldUseValueTaskInsteadOfTask, SyntaxKind.MethodDeclaration);
        context.RegisterSyntaxNodeAction(HandleEqualsAndNotEqualsExpressionShouldUsePatternMatching, SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression);
        context.RegisterSyntaxNodeAction(HandleIsPatternShouldUseRecursivePattern, SyntaxKind.IsPatternExpression);
        context.RegisterSyntaxNodeAction(HandleArgumentNullExceptionThrowIfNull, SyntaxKind.SuppressNullableWarningExpression);
        context.RegisterSyntaxNodeAction(HandleContentDialogShowAsyncAccess, SyntaxKind.SimpleMemberAccessExpression, SyntaxKind.InvocationExpression);
    }

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
            }

            if (token.IsKind(SyntaxKind.InternalKeyword))
            {
                internalExists = true;
            }

            if (token.IsKind(SyntaxKind.FileKeyword))
            {
                fileExists = true;
            }
        }

        if (!privateExists && !internalExists && !fileExists)
        {
            Location location = syntax.Identifier.GetLocation();
            Diagnostic diagnostic = Diagnostic.Create(typeInternalDescriptor, location, syntax.Identifier);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void HandleMethodReturnTypeShouldUseValueTaskInsteadOfTask(SyntaxNodeAnalysisContext context)
    {
        MethodDeclarationSyntax methodSyntax = (MethodDeclarationSyntax)context.Node;
        IMethodSymbol methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodSyntax)!;

        // 跳过重载方法
        if (methodSyntax.Modifiers.Any(token => token.IsKind(SyntaxKind.OverrideKeyword)))
        {
            return;
        }

        // ICommand can only use Task or Task<T>
        if (methodSymbol.GetAttributes().Any(attr => attr.AttributeClass!.ToDisplayString() == CommandGenerator.AttributeName))
        {
            return;
        }

        if (methodSymbol.ReturnType.IsOrInheritsFrom("System.Threading.Tasks.Task"))
        {
            Location location = methodSyntax.ReturnType.GetLocation();
            Diagnostic diagnostic = Diagnostic.Create(useValueTaskIfPossibleDescriptor, location);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void HandleEqualsAndNotEqualsExpressionShouldUsePatternMatching(SyntaxNodeAnalysisContext context)
    {
        BinaryExpressionSyntax syntax = (BinaryExpressionSyntax)context.Node;
        if (syntax.IsKind(SyntaxKind.NotEqualsExpression) && syntax.Right.IsKind(SyntaxKind.NullLiteralExpression))
        {
            Location location = syntax.OperatorToken.GetLocation();
            Diagnostic diagnostic = Diagnostic.Create(useIsNotNullPatternMatchingDescriptor, location);
            context.ReportDiagnostic(diagnostic);
        }
        else if (syntax.IsKind(SyntaxKind.EqualsExpression) && syntax.Right.IsKind(SyntaxKind.NullLiteralExpression))
        {
            Location location = syntax.OperatorToken.GetLocation();
            Diagnostic diagnostic = Diagnostic.Create(useIsNullPatternMatchingDescriptor, location);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void HandleIsPatternShouldUseRecursivePattern(SyntaxNodeAnalysisContext context)
    {
        IsPatternExpressionSyntax syntax = (IsPatternExpressionSyntax)context.Node;
        if (syntax.Pattern is DeclarationPatternSyntax declaration)
        {
            ITypeSymbol? leftType = context.SemanticModel.GetTypeInfo(syntax.Expression).ConvertedType;
            ITypeSymbol? rightType = context.SemanticModel.GetTypeInfo(declaration).ConvertedType;
            if (SymbolEqualityComparer.Default.Equals(leftType, rightType))
            {
                Location location = declaration.GetLocation();
                Diagnostic diagnostic = Diagnostic.Create(useIsPatternRecursiveMatchingDescriptor, location, declaration.Designation);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static void HandleArgumentNullExceptionThrowIfNull(SyntaxNodeAnalysisContext context)
    {
        PostfixUnaryExpressionSyntax syntax = (PostfixUnaryExpressionSyntax)context.Node;

        if (syntax.Kind() is not SyntaxKind.SuppressNullableWarningExpression)
        {
            return;
        }

        // default!
        if (syntax.Operand is LiteralExpressionSyntax literal)
        {
            if (literal.IsKind(SyntaxKind.DefaultLiteralExpression))
            {
                return;
            }
        }

        // default(?)!
        if (syntax.Operand is DefaultExpressionSyntax)
        {
            return;
        }

        Location location = syntax.GetLocation();
        Diagnostic diagnostic = Diagnostic.Create(useArgumentNullExceptionThrowIfNullDescriptor, location);
        context.ReportDiagnostic(diagnostic);
    }

    private static void HandleContentDialogShowAsyncAccess(SyntaxNodeAnalysisContext context)
    {
        switch (context.Node)
        {
            case MemberAccessExpressionSyntax memberAccess:
                {
                    if (memberAccess.Name.Identifier.Text is "ShowAsync")
                    {
                        ISymbol? containingSymbol = context.SemanticModel.GetSymbolInfo(memberAccess.Name).Symbol?.ContainingSymbol;

                        if (containingSymbol?.ToDisplayString() is "Microsoft.UI.Xaml.Controls.ContentDialog")
                        {
                            Location location = memberAccess.GetLocation();
                            Diagnostic diagnostic = Diagnostic.Create(avoidUsingContentDialogShowAsyncDescriptor, location);
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }

                break;
            case InvocationExpressionSyntax invocation:
                {
                    if (invocation.Expression is IdentifierNameSyntax identifierName)
                    {
                        if (identifierName.Identifier.Text is "ShowAsync")
                        {
                            ISymbol? containingSymbol = context.SemanticModel.GetSymbolInfo(identifierName).Symbol?.ContainingSymbol;

                            if (containingSymbol?.ToDisplayString() is "Microsoft.UI.Xaml.Controls.ContentDialog")
                            {
                                Location location = invocation.GetLocation();
                                Diagnostic diagnostic = Diagnostic.Create(avoidUsingContentDialogShowAsyncDescriptor, location);
                                context.ReportDiagnostic(diagnostic);
                            }
                        }
                    }
                }
                break;
        }
    }
}