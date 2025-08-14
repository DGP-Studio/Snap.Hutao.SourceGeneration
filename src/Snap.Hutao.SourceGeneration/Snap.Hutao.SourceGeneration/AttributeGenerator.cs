// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Runtime.CompilerServices;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.FastSyntaxFactory;

[assembly:InternalsVisibleTo("Snap.Hutao.SourceGeneration.Test")]

namespace Snap.Hutao.SourceGeneration;

[Generator(LanguageNames.CSharp)]
internal sealed class AttributeGenerator : IIncrementalGenerator
{
    private static readonly TypeSyntax TypeOfSystemType = ParseTypeName("global::System.Type");
    private static readonly TypeSyntax TypeOfSystemAttributeTargets = ParseTypeName("global::System.AttributeTargets");
    private static readonly TypeSyntax TypeOfMicrosoftExtensionsDependencyInjectionServiceLifetime = ParseTypeName("global::Microsoft.Extensions.DependencyInjection.ServiceLifetime");

    private static readonly BaseListSyntax SystemAttributeBaseList = BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(ParseTypeName("global::System.Attribute"))));
    private static readonly AttributeListSyntax JetBrainsAnnotationsMeansImplicitUseAttributeList = AttributeList(SingletonSeparatedList(Attribute(ParseName("global::JetBrains.Annotations.MeansImplicitUse"))));

    private static readonly IdentifierNameSyntax IdentifierNameOfField = IdentifierName("Field");
    private static readonly IdentifierNameSyntax IdentifierNameOfProperty = IdentifierName("Property");

    private static readonly AttributeArgumentSyntax AttributeTargetsClass = AttributeArgument(SimpleMemberAccessExpression(TypeOfSystemAttributeTargets, IdentifierName("Class")));
    private static readonly AttributeArgumentSyntax AttributeTargetsEnum = AttributeArgument(SimpleMemberAccessExpression(TypeOfSystemAttributeTargets, IdentifierName("Enum")));
    private static readonly AttributeArgumentSyntax AttributeTargetsField = AttributeArgument(SimpleMemberAccessExpression(TypeOfSystemAttributeTargets, IdentifierNameOfField));
    private static readonly AttributeArgumentSyntax AttributeTargetsFieldAndProperty = AttributeArgument(BitwiseOrExpression(
        SimpleMemberAccessExpression(TypeOfSystemAttributeTargets, IdentifierNameOfField),
        SimpleMemberAccessExpression(TypeOfSystemAttributeTargets, IdentifierNameOfProperty)));
    private static readonly AttributeArgumentSyntax AttributeTargetsMethod = AttributeArgument(SimpleMemberAccessExpression(TypeOfSystemAttributeTargets, IdentifierName("Method")));
    private static readonly AttributeArgumentSyntax AttributeTargetsProperty = AttributeArgument(SimpleMemberAccessExpression(TypeOfSystemAttributeTargets, IdentifierNameOfProperty));

    private static readonly AttributeArgumentSyntax AllowMultipleTrue = AttributeArgument(TrueLiteralExpression).WithNameEquals(NameEquals(IdentifierName("AllowMultiple")));
    private static readonly AttributeArgumentSyntax InheritedFalse = AttributeArgument(FalseLiteralExpression).WithNameEquals(NameEquals(IdentifierName("Inherited")));

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(GenerateAllAttributes);
    }

    public static void GenerateAllAttributes(IncrementalGeneratorPostInitializationContext context)
    {
        SyntaxToken identifierOfCommandAttribute = Identifier("CommandAttribute");
        SyntaxToken identifierOfCommandName = Identifier("commandName");
        SyntaxToken identifierOfCanExecuteName = Identifier("canExecuteName");
        SyntaxToken identifierOfAllowConcurrentExecutions = Identifier("AllowConcurrentExecutions");

        SyntaxToken identifierOfConstructorGeneratedAttribute = Identifier("ConstructorGeneratedAttribute");
        SyntaxToken identifierOfCallBaseConstructor = Identifier("CallBaseConstructor");
        SyntaxToken identifierOfResolveHttpClient = Identifier("ResolveHttpClient");
        SyntaxToken identifierOfInitializeComponent = Identifier("InitializeComponent");

        SyntaxToken identifierOfDependencyPropertyAttribute = Identifier("DependencyPropertyAttribute");
        SyntaxToken identifierOfName = Identifier("name");
        SyntaxToken identifierOfIsAttached = Identifier("IsAttached");
        SyntaxToken identifierOfDefaultValue = Identifier("DefaultValue");
        SyntaxToken identifierOfCreateDefaultValueCallbackName = Identifier("CreateDefaultValueCallbackName");
        SyntaxToken identifierOfPropertyChangedCallbackName = Identifier("PropertyChangedCallbackName");

        SyntaxToken identifierOfFieldAccessorAttribute = Identifier("FieldAccessorAttribute");

        CompilationUnitSyntax coreAnnotation = CompilationUnit()
            .WithMembers(SingletonList<MemberDeclarationSyntax>(FileScopedNamespaceDeclaration("Snap.Hutao.Core.Annotation")
                .WithLeadingTrivia(NullableEnableTriviaList)
                .WithMembers(List<MemberDeclarationSyntax>(
                [
                    ClassDeclaration(identifierOfCommandAttribute)
                        .WithAttributeLists(SingletonList(SystemAttributeUsageList(AttributeTargetsMethod, inherited: false)))
                        .WithModifiers(InternalSealedTokenList)
                        .WithBaseList(SystemAttributeBaseList)
                        .WithMembers(List<MemberDeclarationSyntax>(
                        [
                            ConstructorDeclaration(identifierOfCommandAttribute)
                                .WithModifiers(PublicTokenList)
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(StringType, identifierOfCommandName))))
                                .WithEmptyBlockBody(),
                            ConstructorDeclaration(identifierOfCommandAttribute)
                                .WithModifiers(PublicTokenList)
                                .WithParameterList(ParameterList(SeparatedList(
                                [
                                    Parameter(StringType, identifierOfCommandName),
                                    Parameter(StringType, identifierOfCanExecuteName)
                                ])))
                                .WithEmptyBlockBody(),
                            PropertyDeclaration(BoolType, identifierOfAllowConcurrentExecutions)
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                        ])),
                    ClassDeclaration(identifierOfConstructorGeneratedAttribute)
                        .WithAttributeLists(SingletonList(SystemAttributeUsageList(AttributeTargetsClass, inherited: false)))
                        .WithModifiers(InternalSealedTokenList)
                        .WithBaseList(SystemAttributeBaseList)
                        .WithMembers(List<MemberDeclarationSyntax>(
                        [
                            ConstructorDeclaration(identifierOfConstructorGeneratedAttribute)
                                .WithModifiers(PublicTokenList)
                                .WithEmptyBlockBody(),
                            PropertyDeclaration(BoolType, identifierOfCallBaseConstructor)
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            PropertyDeclaration(BoolType, identifierOfResolveHttpClient)
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            PropertyDeclaration(BoolType, identifierOfInitializeComponent)
                                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                                .WithAccessorList(GetAndSetAccessorList)
                        ])),
                    ClassDeclaration(identifierOfDependencyPropertyAttribute)
                        .WithAttributeLists(SingletonList(SystemAttributeUsageList(AttributeTargetsClass, allowMultiple: true, inherited: false)))
                        .WithModifiers(InternalSealedTokenList)
                        .WithTypeParameterList(TypeParameterList(SingletonSeparatedList(
                            TypeParameter(Identifier("T")))))
                        .WithBaseList(SystemAttributeBaseList)
                        .WithMembers(List<MemberDeclarationSyntax>(
                        [
                            ConstructorDeclaration(identifierOfDependencyPropertyAttribute)
                                .WithModifiers(PublicTokenList)
                                .WithParameterList(ParameterList(SeparatedList(
                                [
                                    Parameter(StringType, identifierOfName)
                                ])))
                                .WithEmptyBlockBody(),
                            PropertyDeclaration(BoolType, identifierOfIsAttached)
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            PropertyDeclaration(StringType, identifierOfDefaultValue)
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            PropertyDeclaration(StringType, identifierOfCreateDefaultValueCallbackName)
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            PropertyDeclaration(StringType, identifierOfPropertyChangedCallbackName)
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList)
                        ])),
                    ClassDeclaration(identifierOfFieldAccessorAttribute)
                        .WithAttributeLists(SingletonList(SystemAttributeUsageList(AttributeTargetsProperty, inherited: false)))
                        .WithModifiers(InternalSealedTokenList)
                        .WithBaseList(SystemAttributeBaseList)
                ]))))
            .NormalizeWhitespace();

        context.AddSource("Snap.Hutao.Core.Annotation.Attributes.g.cs", coreAnnotation.ToFullString());

        SyntaxToken identifierOfHttpClientAttribute = Identifier("HttpClientAttribute");
        TypeSyntax typeOfHttpClientConfiguration = ParseTypeName("global::Snap.Hutao.Core.DependencyInjection.Annotation.HttpClient.HttpClientConfiguration");
        SyntaxToken identifierOfConfiguration = Identifier("configuration");
        SyntaxToken identifierOfServiceType = Identifier("serviceType");

        SyntaxToken identifierOfPrimaryHttpMessageHandlerAttribute = Identifier("PrimaryHttpMessageHandlerAttribute");

        CompilationUnitSyntax coreDependencyInjectionAnnotationHttpClient = CompilationUnit()
            .WithMembers(SingletonList<MemberDeclarationSyntax>(FileScopedNamespaceDeclaration("Snap.Hutao.Core.DependencyInjection.Annotation.HttpClient")
                .WithLeadingTrivia(NullableEnableTriviaList)
                .WithMembers(List<MemberDeclarationSyntax>(
                [
                    ClassDeclaration(identifierOfHttpClientAttribute)
                        .WithAttributeLists(List(
                        [
                            JetBrainsAnnotationsMeansImplicitUseAttributeList,
                            SystemAttributeUsageList(AttributeTargetsClass, inherited: false)
                        ]))
                        .WithModifiers(InternalSealedTokenList)
                        .WithBaseList(SystemAttributeBaseList)
                        .WithMembers(List<MemberDeclarationSyntax>(
                        [
                            ConstructorDeclaration(identifierOfHttpClientAttribute)
                                .WithModifiers(PublicTokenList)
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(typeOfHttpClientConfiguration, identifierOfConfiguration))))
                                .WithEmptyBlockBody(),
                            ConstructorDeclaration(identifierOfHttpClientAttribute)
                                .WithModifiers(PublicTokenList)
                                .WithParameterList(ParameterList(SeparatedList(
                                [
                                    Parameter(typeOfHttpClientConfiguration, identifierOfConfiguration),
                                    Parameter(TypeOfSystemType, identifierOfServiceType)
                                ])))
                                .WithEmptyBlockBody()
                        ])),
                    ClassDeclaration(identifierOfPrimaryHttpMessageHandlerAttribute)
                        .WithAttributeLists(SingletonList(SystemAttributeUsageList(AttributeTargetsClass, inherited: false)))
                        .WithModifiers(InternalSealedTokenList)
                        .WithBaseList(SystemAttributeBaseList)
                        .WithMembers(List<MemberDeclarationSyntax>(
                        [
                            PropertyDeclaration(IntType, "MaxAutomaticRedirections")
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            PropertyDeclaration(IntType, "MaxConnectionsPerServer")
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            PropertyDeclaration(IntType, "MaxResponseDrainSize")
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            PropertyDeclaration(IntType, "MaxResponseHeadersLength")
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            // Unsupported: MeterFactory, PlaintextStreamFilter, PooledConnectionIdleTimeout, PooledConnectionLifetime
                            // Unsupported: Proxy, RequestHeaderEncodingSelector, ResponseDrainTimeout, ResponseHeaderEncodingSelector
                            // Unsupported: SslOptions
                            PropertyDeclaration(BoolType, "PreAuthenticate")
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            // Unsupported: KeepAlivePingTimeout
                            PropertyDeclaration(ParseTypeName("global::System.Net.Http.HttpKeepAlivePingPolicy"), "KeepAlivePingPolicy")
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            // Unsupported: KeepAlivePingDelay, ActivityHeadersPropagator
                            PropertyDeclaration(BoolType, "AllowAutoRedirect")
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            PropertyDeclaration(ParseTypeName("global::System.Net.DecompressionMethods"), "AutomaticDecompression")
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            // Unsupported: ConnectCallback
                            PropertyDeclaration(BoolType, "UseCookies")
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            // Unsupported: CookieContainer, ConnectTimeout, DefaultProxyCredentials
                            PropertyDeclaration(BoolType, "EnableMultipleHttp2Connections")
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            PropertyDeclaration(BoolType, "EnableMultipleHttp3Connections")
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            // Unsupported: Expect100ContinueTimeout
                            PropertyDeclaration(IntType, "InitialHttp2StreamWindowSize")
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            // Unsupported: Credentials
                            PropertyDeclaration(BoolType, "UseProxy")
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList)
                        ]))
                ]))))
            .NormalizeWhitespace();

        context.AddSource("Snap.Hutao.Core.DependencyInjection.Annotation.HttpClient.Attributes.g.cs", coreDependencyInjectionAnnotationHttpClient.ToFullString());

        SyntaxToken identifierOfServiceLifetime = Identifier("serviceLifetime");
        SyntaxToken identifierOfNamedArgKey = Identifier("Key");

        SyntaxToken identifierOfServiceAttribute = Identifier("ServiceAttribute");

        SyntaxToken identifierOfFromKeyedServicesAttribute = Identifier("FromKeyedServicesAttribute");
        SyntaxToken identifierOfKey = Identifier("key");

        CompilationUnitSyntax coreDependencyInjectionAnnotation = CompilationUnit()
            .WithMembers(SingletonList<MemberDeclarationSyntax>(FileScopedNamespaceDeclaration("Snap.Hutao.Core.DependencyInjection.Annotation")
                .WithLeadingTrivia(NullableEnableTriviaList)
                .WithMembers(List<MemberDeclarationSyntax>(
                [
                    ClassDeclaration(identifierOfServiceAttribute)
                        .WithAttributeLists(SingletonList(SystemAttributeUsageList(AttributeTargetsClass, inherited: false)))
                        .WithModifiers(InternalSealedTokenList)
                        .WithBaseList(SystemAttributeBaseList).WithMembers(List<MemberDeclarationSyntax>(
                        [
                            ConstructorDeclaration(identifierOfServiceAttribute)
                                .WithModifiers(PublicTokenList)
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(TypeOfMicrosoftExtensionsDependencyInjectionServiceLifetime, identifierOfServiceLifetime))))
                                .WithEmptyBlockBody(),
                            ConstructorDeclaration(identifierOfServiceAttribute)
                                .WithModifiers(PublicTokenList)
                                .WithParameterList(ParameterList(SeparatedList(
                                [
                                    Parameter(TypeOfMicrosoftExtensionsDependencyInjectionServiceLifetime, identifierOfServiceLifetime),
                                    Parameter(TypeOfSystemType, identifierOfServiceType)
                                ])))
                                .WithEmptyBlockBody(),
                            PropertyDeclaration(ObjectType, identifierOfNamedArgKey)
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList)
                        ])),
                    ClassDeclaration(identifierOfFromKeyedServicesAttribute)
                        .WithAttributeLists(SingletonList(SystemAttributeUsageList(AttributeTargetsFieldAndProperty)))
                        .WithModifiers(InternalSealedTokenList)
                        .WithBaseList(SystemAttributeBaseList)
                        .WithMembers(SingletonList<MemberDeclarationSyntax>(
                            ConstructorDeclaration(identifierOfFromKeyedServicesAttribute)
                                .WithModifiers(PublicTokenList)
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(ObjectType, identifierOfKey))))
                                .WithEmptyBlockBody()))
                ]))))
            .NormalizeWhitespace();

        context.AddSource("Snap.Hutao.Core.DependencyInjection.Annotation.Attributes.g.cs", coreDependencyInjectionAnnotation.ToFullString());

        SyntaxToken identifierOfExtendedEnumAttribute = Identifier("ExtendedEnumAttribute");
        SyntaxToken identifierOfLocalizationKeyAttribute = Identifier("LocalizationKeyAttribute");

        CompilationUnitSyntax resourceLocalization = CompilationUnit()
            .WithMembers(SingletonList<MemberDeclarationSyntax>(FileScopedNamespaceDeclaration("Snap.Hutao.Resource.Localization")
                .WithLeadingTrivia(NullableEnableTriviaList)
                .WithMembers(List<MemberDeclarationSyntax>(
                [
                    ClassDeclaration(identifierOfExtendedEnumAttribute)
                        .WithAttributeLists(SingletonList(SystemAttributeUsageList(AttributeTargetsEnum)))
                        .WithModifiers(InternalSealedTokenList)
                        .WithBaseList(SystemAttributeBaseList),
                    ClassDeclaration(identifierOfLocalizationKeyAttribute)
                        .WithAttributeLists(SingletonList(SystemAttributeUsageList(AttributeTargetsField)))
                        .WithModifiers(InternalSealedTokenList)
                        .WithBaseList(SystemAttributeBaseList)
                        .WithMembers(SingletonList<MemberDeclarationSyntax>(
                            ConstructorDeclaration(identifierOfLocalizationKeyAttribute)
                                .WithModifiers(PublicTokenList)
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(StringType, identifierOfKey))))
                                .WithEmptyBlockBody()))
                ]))))
            .NormalizeWhitespace();

        context.AddSource("Snap.Hutao.Resource.Localization.Attributes.g.cs", resourceLocalization.ToFullString());

        SyntaxToken identifierOfInterceptsLocationAttribute = Identifier("InterceptsLocationAttribute");
        SyntaxToken identifierOfVersion = Identifier("version");
        SyntaxToken identifierOfData = Identifier("data");

        CompilationUnitSyntax interceptsLocation = CompilationUnit()
            .WithMembers(SingletonList<MemberDeclarationSyntax>(FileScopedNamespaceDeclaration("System.Runtime.CompilerServices")
                .WithLeadingTrivia(NullableEnableTriviaList)
                .WithMembers(SingletonList<MemberDeclarationSyntax>(
                    ClassDeclaration(identifierOfInterceptsLocationAttribute)
                        .WithAttributeLists(SingletonList(SystemAttributeUsageList(AttributeTargetsMethod, allowMultiple: true)))
                        .WithModifiers(InternalSealedTokenList)
                        .WithBaseList(SystemAttributeBaseList)
                        .WithMembers(SingletonList<MemberDeclarationSyntax>(
                            ConstructorDeclaration(identifierOfInterceptsLocationAttribute)
                                .WithModifiers(PublicTokenList)
                                .WithParameterList(ParameterList(SeparatedList(
                                [
                                    Parameter(IntType, identifierOfVersion),
                                    Parameter(StringType, identifierOfData)
                                ])))
                                .WithEmptyBlockBody()))))))
            .NormalizeWhitespace();

        context.AddSource("System.Runtime.CompilerServices.InterceptsLocationAttribute.g.cs", interceptsLocation.ToFullString());
    }

    private static AttributeListSyntax SystemAttributeUsageList(AttributeArgumentSyntax attributeTargets, bool allowMultiple = false, bool inherited = true)
    {
        SeparatedSyntaxList<AttributeArgumentSyntax> arguments = SingletonSeparatedList(attributeTargets);
        if (allowMultiple)
        {
            arguments = arguments.Add(AllowMultipleTrue);
        }

        if (!inherited)
        {
            arguments = arguments.Add(InheritedFalse);
        }

        return AttributeList(SingletonSeparatedList(
            Attribute(ParseName("global::System.AttributeUsage"))
                .WithArgumentList(AttributeArgumentList(arguments))));
    }
}