using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snap.Hutao.SourceGeneration.Primitive;
using System;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.FastSyntaxFactory;

namespace Snap.Hutao.SourceGeneration.Enum;

[Generator(LanguageNames.CSharp)]
internal class ExtendedEnumGenerator : IIncrementalGenerator
{
    private static readonly TypeSyntax TypeOfSystemEnum = ParseTypeName("global::System.Enum");
    private static readonly TypeSyntax TypeOfSystemResourcesResourceManager = ParseTypeName("global::System.Resources.ResourceManager");
    private static readonly TypeSyntax TypeOfSystemGlobalizationCultureInfo = ParseTypeName("global::System.Globalization.CultureInfo");

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<GeneratorAttributeSyntaxContext> provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                WellKnownMetadataNames.ExtendedEnumAttribute,
                SyntaxNodeHelper.Is<EnumDeclarationSyntax>,
                SyntaxContext.Transform);

        context.RegisterSourceOutput(provider, GenerateWrapper);
    }

    private static void GenerateWrapper(SourceProductionContext production, GeneratorAttributeSyntaxContext context)
    {
        try
        {
            Generate(production, context);
        }
        catch (Exception e)
        {
            production.AddSource($"Error-{Guid.NewGuid().ToString()}.g.cs", e.ToString());
        }
    }

    private static void Generate(SourceProductionContext production, GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol enumSymbol)
        {
            return;
        }

        TypeSyntax enumType = ParseTypeName(enumSymbol.GetFullyQualifiedNameWithNullabilityAnnotations());
        ExpressionSyntax typeExpression = ParseTypeName(enumSymbol.GetFullyQualifiedNameWithNullabilityAnnotations());

        CompilationUnitSyntax syntax = CompilationUnit()
            .WithUsings(SingletonList(UsingDirective("System.Globalization")))
            .WithMembers(SingletonList<MemberDeclarationSyntax>(FileScopedNamespaceDeclaration("Snap.Hutao.Resource.Localization")
                .WithLeadingTrivia(NullableEnableList)
                .WithMembers(SingletonList<MemberDeclarationSyntax>(
                    ClassDeclaration($"{enumSymbol.Name}Extension")
                        .WithModifiers(InternalStaticPartialList)
                        .WithMembers(List<MemberDeclarationSyntax>(
                        [
                            // public static string? GetName(this T value)
                            MethodDeclaration(NullableStringType, "GetName")
                                .WithModifiers(PublicStaticList)
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(enumType, Identifier("value")).WithModifiers(ThisList))))
                                .WithBody(Block(SingletonList<StatementSyntax>(
                                    ReturnStatement(SwitchExpression(IdentifierName("value"))
                                        .WithArms(SeparatedList(GenerateGetNameSwitchArms(enumSymbol, typeExpression))))))),

                            // public static string? GetLocalizedDescriptionOrDefault(this T value, ResourceManager resourceManager, CultureInfo cultureInfo)
                            MethodDeclaration(NullableStringType, "GetLocalizedDescriptionOrDefault")
                                .WithModifiers(PublicStaticList)
                                .WithParameterList(ParameterList(SeparatedList(
                                [
                                    Parameter(enumType, Identifier("value")).WithModifiers(ThisList),
                                    Parameter(TypeOfSystemResourcesResourceManager, Identifier("resourceManager")),
                                    Parameter(TypeOfSystemGlobalizationCultureInfo, Identifier("cultureInfo"))
                                ])))
                                .WithBody(Block(List<StatementSyntax>(
                                [
                                    LocalDeclarationStatement(VariableDeclaration(StringType)
                                        .WithVariables(SingletonSeparatedList(
                                            VariableDeclarator(Identifier("key"))
                                                .WithInitializer(EqualsValueClause(SwitchExpression(IdentifierName("value"))
                                                    .WithArms(SeparatedList(GenerateGetLocalizedDescriptionOrDefaultSwitchArms(enumSymbol, typeExpression)))))))),
                                    ReturnStatement(InvocationExpression(SimpleMemberAccessExpression(
                                            IdentifierName("resourceManager"),
                                            IdentifierName("GetString")))
                                        .WithArgumentList(ArgumentList(SeparatedList(
                                        [
                                            Argument(IdentifierName("key")),
                                            Argument(IdentifierName("cultureInfo"))
                                        ]))))
                                ]))),

                            // public static string? GetLocalizedDescriptionOrDefault(this T value, ResourceManager resourceManager)
                            MethodDeclaration(NullableStringType, "GetLocalizedDescriptionOrDefault")
                                .WithModifiers(PublicStaticList)
                                .WithParameterList(ParameterList(SeparatedList(
                                [
                                    Parameter(enumType, Identifier("value")).WithModifiers(ThisList),
                                    Parameter(TypeOfSystemResourcesResourceManager, Identifier("resourceManager"))
                                ])))
                                .WithBody(Block(SingletonList<StatementSyntax>(
                                    ReturnStatement(InvocationExpression(IdentifierName("GetLocalizedDescriptionOrDefault"))
                                        .WithArgumentList(ArgumentList(SeparatedList(
                                        [
                                            Argument(IdentifierName("value")),
                                            Argument(IdentifierName("resourceManager")),
                                            Argument(SimpleMemberAccessExpression(TypeOfSystemGlobalizationCultureInfo, IdentifierName("CurrentCulture")))
                                        ]))))))),

                            // public static string GetLocalizedDescription(this T value, ResourceManager resourceManager, CultureInfo cultureInfo)
                            MethodDeclaration(NullableStringType, "GetLocalizedDescription")
                                .WithModifiers(PublicStaticList)
                                .WithParameterList(ParameterList(SeparatedList(
                                [
                                    Parameter(enumType, Identifier("value")).WithModifiers(ThisList),
                                    Parameter(TypeOfSystemResourcesResourceManager, Identifier("resourceManager")),
                                    Parameter(TypeOfSystemGlobalizationCultureInfo, Identifier("cultureInfo"))
                                ])))
                                .WithBody(Block(SingletonList<StatementSyntax>(
                                    ReturnStatement(CoalesceExpression(
                                        InvocationExpression(IdentifierName("GetLocalizedDescriptionOrDefault"))
                                            .WithArgumentList(ArgumentList(SeparatedList(
                                            [
                                                Argument(IdentifierName("value")),
                                                Argument(IdentifierName("resourceManager")),
                                                Argument(IdentifierName("cultureInfo"))
                                            ]))),
                                        CoalesceExpression(
                                            InvocationExpression(IdentifierName("GetName"))
                                                .WithArgumentList(ArgumentList(SingletonSeparatedList(
                                                    Argument(IdentifierName("value"))))),
                                            SimpleMemberAccessExpression(StringType, IdentifierName("Empty")))))))),

                            // public static string GetLocalizedDescription(this T value, ResourceManager resourceManager)
                            MethodDeclaration(NullableStringType, "GetLocalizedDescription")
                                .WithModifiers(PublicStaticList)
                                .WithParameterList(ParameterList(SeparatedList(
                                [
                                    Parameter(enumType, Identifier("value")).WithModifiers(ThisList),
                                    Parameter(TypeOfSystemResourcesResourceManager, Identifier("resourceManager"))
                                ])))
                                .WithBody(Block(SingletonList<StatementSyntax>(
                                    ReturnStatement(InvocationExpression(IdentifierName("GetLocalizedDescription"))
                                        .WithArgumentList(ArgumentList(SeparatedList(
                                        [
                                            Argument(IdentifierName("value")),
                                            Argument(IdentifierName("resourceManager")),
                                            Argument(SimpleMemberAccessExpression(TypeOfSystemGlobalizationCultureInfo, IdentifierName("CurrentCulture")))
                                        ])))))))
                        ]))))))
            .NormalizeWhitespace();

        production.AddSource($"{enumSymbol.ToDisplayString().NormalizeSymbolName()}Extension.g.cs", syntax.ToFullString());
    }

    private static IEnumerable<SwitchExpressionArmSyntax> GenerateGetNameSwitchArms(INamedTypeSymbol enumSymbol, ExpressionSyntax typeExpression)
    {
        foreach (IFieldSymbol fieldSymbol in enumSymbol.GetMembers().OfType<IFieldSymbol>())
        {
            yield return SwitchExpressionArm(
                ConstantPattern(SimpleMemberAccessExpression(typeExpression, IdentifierName(fieldSymbol.Name))),
                StringLiteralExpression(fieldSymbol.Name));
        }

        yield return SwitchExpressionArm(
            DiscardPattern(),
            InvocationExpression(SimpleMemberAccessExpression(
                    TypeOfSystemEnum,
                    IdentifierName("GetName")))
                .WithArgumentList(ArgumentList(SingletonSeparatedList(
                    Argument(IdentifierName("value"))))));
    }

    private static IEnumerable<SwitchExpressionArmSyntax> GenerateGetLocalizedDescriptionOrDefaultSwitchArms(INamedTypeSymbol enumSymbol, ExpressionSyntax typeExpression)
    {
        foreach (IFieldSymbol fieldSymbol in enumSymbol.GetMembers().OfType<IFieldSymbol>())
        {
            AttributeData? localizationKeyInfo = fieldSymbol.GetAttributes()
                .SingleOrDefault(static data => data.AttributeClass?.HasFullyQualifiedMetadataName(WellKnownMetadataNames.LocalizationKeyAttribute) is true);
            if (localizationKeyInfo is not null && localizationKeyInfo.TryGetConstructorArgument(0, out string? localizationKey))
            {
                yield return SwitchExpressionArm(
                    ConstantPattern(SimpleMemberAccessExpression(typeExpression, IdentifierName(fieldSymbol.Name))),
                    StringLiteralExpression(localizationKey));
            }
        }

        yield return SwitchExpressionArm(
            DiscardPattern(),
            SimpleMemberAccessExpression(StringType, IdentifierName("Empty")));
    }
}