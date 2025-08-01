// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.FastSyntaxFactory;

namespace Snap.Hutao.SourceGeneration;

[Generator(LanguageNames.CSharp)]
internal sealed class AttributeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(GenerateAllAttributes);
    }

    public static void GenerateAllAttributes(IncrementalGeneratorPostInitializationContext context)
    {
        CompilationUnitSyntax coreAnnotation = CompilationUnit()
            .WithUsings(SingletonList(UsingDirective("System", "Diagnostics")))
            .WithMembers(SingletonList<MemberDeclarationSyntax>(FileScopedNamespaceDeclaration("Snap", "Hutao", "Core", "Annotation")
                .WithMembers(List<MemberDeclarationSyntax>(
                [
                    ClassDeclaration("CommandAttribute")
                        .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(Attribute(IdentifierName("AttributeUsage"))
                            .WithArgumentList(AttributeArgumentList(SeparatedList(
                            [
                                AttributeArgument(SimpleMemberAccessExpression(IdentifierName("AttributeTargets"), IdentifierName("Method"))),
                                AttributeArgument(FalseLiteralExpression).WithNameEquals(NameEquals(IdentifierName("Inherited")))
                            ])))))))
                        .WithModifiers(TokenList(InternalKeyword, SealedKeyword))
                        .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(IdentifierName("Attribute")))))
                        .WithMembers(List<MemberDeclarationSyntax>(
                        [
                            ConstructorDeclaration(Identifier("CommandAttribute"))
                                .WithModifiers(TokenList(PublicKeyword))
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(Identifier("commandName")).WithType(StringType))))
                                .WithEmptyBlockBody(),
                            ConstructorDeclaration(Identifier("CommandAttribute"))
                                .WithModifiers(TokenList(PublicKeyword))
                                .WithParameterList(ParameterList(SeparatedList(
                                [
                                    Parameter(Identifier("commandName")).WithType(StringType),
                                    Parameter(Identifier("canExecuteName")).WithType(StringType)
                                ])))
                                .WithEmptyBlockBody(),
                            PropertyDeclaration(BoolType, Identifier("AllowConcurrentExecutions"))
                                .WithModifiers(TokenList(PublicKeyword))
                                .WithAccessorList(GetAndSetAccessorList()),
                        ])),
                    ClassDeclaration("ConstructorGeneratedAttribute")
                        .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(Attribute(IdentifierName("AttributeUsage"))
                            .WithArgumentList(AttributeArgumentList(SeparatedList(
                            [
                                AttributeArgument(SimpleMemberAccessExpression(IdentifierName("AttributeTargets"), IdentifierName("Class"))),
                                AttributeArgument(FalseLiteralExpression).WithNameEquals(NameEquals(IdentifierName("Inherited")))
                            ])))))))
                        .WithModifiers(TokenList(InternalKeyword, SealedKeyword))
                        .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(IdentifierName("Attribute")))))
                        .WithMembers(List<MemberDeclarationSyntax>(
                        [
                            ConstructorDeclaration(Identifier("ConstructorGeneratedAttribute"))
                                .WithModifiers(TokenList(PublicKeyword))
                                .WithEmptyBlockBody(),
                            PropertyDeclaration(BoolType, Identifier("CallBaseConstructor"))
                                .WithModifiers(TokenList(PublicKeyword))
                                .WithAccessorList(GetAndSetAccessorList()),
                            PropertyDeclaration(BoolType, Identifier("ResolveHttpClient"))
                                .WithModifiers(TokenList(PublicKeyword))
                                .WithAccessorList(GetAndSetAccessorList()),
                            PropertyDeclaration(BoolType, Identifier("InitializeComponent"))
                                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                                .WithAccessorList(GetAndSetAccessorList())
                        ])),
                    ClassDeclaration("DependencyPropertyAttribute")
                        .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(Attribute(IdentifierName("AttributeUsage"))
                            .WithArgumentList(AttributeArgumentList(SeparatedList(
                            [
                                AttributeArgument(SimpleMemberAccessExpression(IdentifierName("AttributeTargets"), IdentifierName("Class"))),
                                AttributeArgument(TrueLiteralExpression).WithNameEquals(NameEquals(IdentifierName("AllowMultiple"))),
                                AttributeArgument(FalseLiteralExpression).WithNameEquals(NameEquals(IdentifierName("Inherited")))
                            ])))))))
                        .WithModifiers(TokenList(InternalKeyword, SealedKeyword))
                        .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(IdentifierName("Attribute")))))
                        .WithMembers(List<MemberDeclarationSyntax>(
                        [
                            ConstructorDeclaration(Identifier("DependencyPropertyAttribute"))
                                .WithModifiers(TokenList(PublicKeyword))
                                .WithParameterList(ParameterList(SeparatedList(
                                [
                                    Parameter(Identifier("name")).WithType(StringType),
                                    Parameter(Identifier("type")).WithType(IdentifierName("Type"))
                                ])))
                                .WithEmptyBlockBody(),
                            ConstructorDeclaration(Identifier("DependencyPropertyAttribute"))
                                .WithModifiers(TokenList(PublicKeyword))
                                .WithParameterList(ParameterList(SeparatedList(
                                [
                                    Parameter(Identifier("name")).WithType(StringType),
                                    Parameter(Identifier("type")).WithType(IdentifierName("Type")),
                                    Parameter(Identifier("defaultValue")).WithType(ObjectType)
                                ])))
                                .WithEmptyBlockBody(),
                            ConstructorDeclaration(Identifier("DependencyPropertyAttribute"))
                                .WithModifiers(TokenList(PublicKeyword))
                                .WithParameterList(ParameterList(SeparatedList(
                                [
                                    Parameter(Identifier("name")).WithType(StringType),
                                    Parameter(Identifier("type")).WithType(IdentifierName("Type")),
                                    Parameter(Identifier("defaultValue")).WithType(ObjectType),
                                    Parameter(Identifier("valueChangedCallbackName")).WithType(StringType)
                                ])))
                                .WithEmptyBlockBody(),
                            PropertyDeclaration(BoolType, Identifier("IsAttached"))
                                .WithModifiers(TokenList(PublicKeyword))
                                .WithAccessorList(GetAndSetAccessorList()),
                            PropertyDeclaration(IdentifierName("Type"), Identifier("AttachedType"))
                                .WithModifiers(TokenList(PublicKeyword))
                                .WithAccessorList(GetAndSetAccessorList()),
                            PropertyDeclaration(StringType, Identifier("RawDefaultValue"))
                                .WithModifiers(TokenList(PublicKeyword))
                                .WithAccessorList(GetAndSetAccessorList())
                        ]))
                ]))))
            .NormalizeWhitespace();

        context.AddSource("Snap.Hutao.Core.Annotation.Attributes.g.cs", coreAnnotation.ToFullString());

        CompilationUnitSyntax coreDependencyInjectionAnnotationHttpClient = CompilationUnit()
            .WithUsings(SingletonList(UsingDirective("JetBrains", "Annotations")))
            .WithMembers(SingletonList<MemberDeclarationSyntax>(FileScopedNamespaceDeclaration("Snap", "Hutao", "Core", "DependencyInjection", "Annotation", "HttpClient")
                .WithMembers(List<MemberDeclarationSyntax>(
                [
                    ClassDeclaration("HttpClientAttribute")
                        .WithAttributeLists(List(
                        [
                            AttributeList(SingletonSeparatedList(Attribute(IdentifierName("MeansImplicitUse")))),
                            AttributeList(SingletonSeparatedList(Attribute(IdentifierName("AttributeUsage"))
                                .WithArgumentList(AttributeArgumentList(SeparatedList(
                                [
                                    AttributeArgument(SimpleMemberAccessExpression(IdentifierName("AttributeTargets"), IdentifierName("Class"))),
                                    AttributeArgument(FalseLiteralExpression).WithNameEquals(NameEquals(IdentifierName("Inherited")))
                                ])))))
                        ]))
                        .WithModifiers(TokenList(InternalKeyword, SealedKeyword))
                        .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(IdentifierName("Attribute")))))
                        .WithMembers(List<MemberDeclarationSyntax>(
                        [
                            ConstructorDeclaration(Identifier("HttpClientAttribute"))
                                .WithModifiers(TokenList(PublicKeyword))
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(Identifier("configuration")).WithType(IdentifierName("HttpClientConfiguration")))))
                                .WithEmptyBlockBody(),
                            ConstructorDeclaration(Identifier("HttpClientAttribute"))
                                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                                .WithParameterList(ParameterList(SeparatedList(
                                [
                                    Parameter(Identifier("configuration")).WithType(IdentifierName("HttpClientConfiguration")),
                                    Parameter(Identifier("interfaceType")).WithType(IdentifierName("Type"))
                                ])))
                                .WithEmptyBlockBody()
                        ])),
                    ClassDeclaration("PrimaryHttpMessageHandlerAttribute")
                        .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(Attribute(IdentifierName("AttributeUsage"))
                            .WithArgumentList(AttributeArgumentList(SeparatedList(
                            [
                                AttributeArgument(SimpleMemberAccessExpression(IdentifierName("AttributeTargets"), IdentifierName("Class"))),
                                AttributeArgument(FalseLiteralExpression).WithNameEquals(NameEquals(IdentifierName("Inherited")))
                            ])))))))
                        .WithModifiers(TokenList(InternalKeyword, SealedKeyword))
                        .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(IdentifierName("Attribute")))))
                        .WithMembers(List<MemberDeclarationSyntax>(
                        [
                            PropertyDeclaration(IntType, Identifier("MaxConnectionsPerServer"))
                                .WithModifiers(TokenList(PublicKeyword))
                                .WithAccessorList(GetAndSetAccessorList()),
                            PropertyDeclaration(BoolType, Identifier("UseCookies"))
                                .WithModifiers(TokenList(PublicKeyword))
                                .WithAccessorList(GetAndSetAccessorList())
                        ]))
                ]))))
            .NormalizeWhitespace();

        context.AddSource("Snap.Hutao.Core.DependencyInjection.Annotation.HttpClient.Attributes.g.cs", coreDependencyInjectionAnnotationHttpClient.ToFullString());

        CompilationUnitSyntax coreDependencyInjectionAnnotation = CompilationUnit()
            .WithUsings(SingletonList(UsingDirective("JetBrains", "Annotations")))
            .WithMembers(SingletonList<MemberDeclarationSyntax>(FileScopedNamespaceDeclaration("Snap", "Hutao", "Core", "DependencyInjection", "Annotation")
                .WithMembers(List<MemberDeclarationSyntax>(
                [
                    EnumDeclaration("InjectAs")
                        .WithModifiers(TokenList(InternalKeyword))
                        .WithMembers(SeparatedList(
                        [
                            EnumMemberDeclaration(Identifier("Singleton")),
                            EnumMemberDeclaration(Identifier("Transient")),
                            EnumMemberDeclaration(Identifier("Scoped")),
                            EnumMemberDeclaration(Identifier("HostedService"))
                        ])),
                    ClassDeclaration("InjectionAttribute")
                        .WithAttributeLists(List(
                        [
                            AttributeList(SingletonSeparatedList(Attribute(IdentifierName("MeansImplicitUse")))),
                            AttributeList(SingletonSeparatedList(Attribute(IdentifierName("AttributeUsage"))
                                .WithArgumentList(AttributeArgumentList(SeparatedList(
                                [
                                    AttributeArgument(SimpleMemberAccessExpression(IdentifierName("AttributeTargets"), IdentifierName("Class"))),
                                    AttributeArgument(TrueLiteralExpression).WithNameEquals(NameEquals(IdentifierName("AllowMultiple"))),
                                    AttributeArgument(FalseLiteralExpression).WithNameEquals(NameEquals(IdentifierName("Inherited")))
                                ])))))
                        ]))
                        .WithModifiers(TokenList(InternalKeyword, SealedKeyword))
                        .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(IdentifierName("Attribute")))))
                        .WithMembers(List<MemberDeclarationSyntax>(
                        [
                            ConstructorDeclaration(Identifier("InjectionAttribute"))
                                .WithModifiers(TokenList(PublicKeyword))
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(Identifier("injectAs")).WithType(IdentifierName("InjectAs")))))
                                .WithEmptyBlockBody(),
                            ConstructorDeclaration(Identifier("InjectionAttribute"))
                                .WithModifiers(TokenList(PublicKeyword))
                                .WithParameterList(ParameterList(SeparatedList(
                                [
                                    Parameter(Identifier("injectAs")).WithType(IdentifierName("InjectAs")),
                                    Parameter(Identifier("interfaceType")).WithType(IdentifierName("Type"))
                                ])))
                                .WithEmptyBlockBody(),
                            PropertyDeclaration(ObjectType, Identifier("Key"))
                                .WithModifiers(TokenList(PublicKeyword))
                                .WithAccessorList(GetAndSetAccessorList())
                        ])),
                    ClassDeclaration("FromKeyedServicesAttribute")
                        .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(Attribute(IdentifierName("AttributeUsage"))
                            .WithArgumentList(AttributeArgumentList(SingletonSeparatedList(
                                AttributeArgument(SimpleMemberAccessExpression(IdentifierName("AttributeTargets"), IdentifierName("Field"))))))))))
                        .WithModifiers(TokenList(InternalKeyword, SealedKeyword))
                        .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(IdentifierName("Attribute")))))
                        .WithMembers(SingletonList<MemberDeclarationSyntax>(
                            ConstructorDeclaration(Identifier("FromKeyedServicesAttribute"))
                                .WithModifiers(TokenList(PublicKeyword))
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(Identifier("key")).WithType(ObjectType))))
                                .WithEmptyBlockBody()))
                ]))))
            .NormalizeWhitespace();

        context.AddSource("Snap.Hutao.Core.DependencyInjection.Annotation.Attributes.g.cs", coreDependencyInjectionAnnotation.ToFullString());

        CompilationUnitSyntax resourceLocalization = CompilationUnit()
            .WithMembers(SingletonList<MemberDeclarationSyntax>(FileScopedNamespaceDeclaration("Snap", "Hutao", "Resource", "Localization")
                .WithMembers(List<MemberDeclarationSyntax>(
                [
                    ClassDeclaration("LocalizationAttribute")
                        .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(
                            Attribute(IdentifierName("AttributeUsage"))
                                .WithArgumentList(AttributeArgumentList(SingletonSeparatedList(
                                    AttributeArgument(SimpleMemberAccessExpression(IdentifierName("AttributeTargets"), IdentifierName("Enum"))))))))))
                        .WithModifiers(TokenList(InternalKeyword, SealedKeyword))
                        .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(IdentifierName("Attribute"))))),
                    ClassDeclaration("LocalizationKeyAttribute")
                        .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(
                            Attribute(IdentifierName("AttributeUsage"))
                                .WithArgumentList(AttributeArgumentList(SingletonSeparatedList(
                                    AttributeArgument(SimpleMemberAccessExpression(IdentifierName("AttributeTargets"), IdentifierName("Field"))))))))))
                        .WithModifiers(TokenList(InternalKeyword, SealedKeyword))
                        .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(IdentifierName("Attribute")))))
                        .WithMembers(SingletonList<MemberDeclarationSyntax>(
                            ConstructorDeclaration(Identifier("LocalizationKeyAttribute"))
                                .WithModifiers(TokenList(PublicKeyword))
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(Identifier("key")).WithType(StringType))))
                                .WithEmptyBlockBody()))
                ]))))
            .NormalizeWhitespace();

        context.AddSource("Snap.Hutao.Resource.Localization.Attributes.g.cs", resourceLocalization.ToFullString());

        CompilationUnitSyntax interceptsLocation = CompilationUnit()
            .WithMembers(SingletonList<MemberDeclarationSyntax>(FileScopedNamespaceDeclaration("System", "Runtime", "CompilerServices")
                .WithMembers(SingletonList<MemberDeclarationSyntax>(
                    ClassDeclaration("InterceptsLocationAttribute")
                        .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(
                            Attribute(IdentifierName("AttributeUsage"))
                                .WithArgumentList(AttributeArgumentList(SeparatedList(
                                [
                                    AttributeArgument(SimpleMemberAccessExpression(IdentifierName("AttributeTargets"), IdentifierName("Method"))),
                                    AttributeArgument(TrueLiteralExpression).WithNameEquals(NameEquals(IdentifierName("AllowMultiple")))
                                ])))))))
                        .WithModifiers(TokenList(InternalKeyword, SealedKeyword))
                        .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(IdentifierName("Attribute")))))
                        .WithMembers(SingletonList<MemberDeclarationSyntax>(
                            ConstructorDeclaration(Identifier("InterceptsLocationAttribute"))
                                .WithModifiers(TokenList(PublicKeyword))
                                .WithParameterList(ParameterList(SeparatedList(
                                [
                                    Parameter(Identifier("version")).WithType(PredefinedType(Token(SyntaxKind.IntKeyword))),
                                    Parameter(Identifier("data")).WithType(PredefinedType(Token(SyntaxKind.StringKeyword)))
                                ])))
                                .WithEmptyBlockBody()))))))
            .NormalizeWhitespace();

        context.AddSource("System.Runtime.CompilerServices.InterceptsLocationAttribute.g.cs", interceptsLocation.ToFullString());
    }
}