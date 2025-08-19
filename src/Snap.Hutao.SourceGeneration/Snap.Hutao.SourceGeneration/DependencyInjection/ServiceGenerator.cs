﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snap.Hutao.SourceGeneration.Extension;
using Snap.Hutao.SourceGeneration.Model;
using Snap.Hutao.SourceGeneration.Primitive;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.FastSyntaxFactory;
using static Snap.Hutao.SourceGeneration.WellKnownSyntax;
using TypeInfo = Snap.Hutao.SourceGeneration.Model.TypeInfo;

namespace Snap.Hutao.SourceGeneration.DependencyInjection;

[Generator(LanguageNames.CSharp)]
internal sealed class ServiceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<ServiceGeneratorContext> provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                WellKnownMetadataNames.ServiceAttribute,
                SyntaxNodeHelper.Is<ClassDeclarationSyntax>,
                ServiceEntry.Create)
            .Where(static entry => entry is not null)
            .Collect()
            .Select(ServiceGeneratorContext.Create);

        context.RegisterImplementationSourceOutput(provider, GenerateWrapper);
    }

    private static void GenerateWrapper(SourceProductionContext production, ServiceGeneratorContext context)
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

    private static void Generate(SourceProductionContext production, ServiceGeneratorContext context)
    {
        CompilationUnitSyntax syntax = CompilationUnit()
            .WithUsings(SingletonList(UsingDirective("Microsoft.Extensions.DependencyInjection")))
            .WithMembers(SingletonList<MemberDeclarationSyntax>(FileScopedNamespaceDeclaration("Snap.Hutao.Core.DependencyInjection")
                .WithLeadingTrivia(NullableEnableTriviaList)
                .WithMembers(SingletonList<MemberDeclarationSyntax>(
                    ClassDeclaration("ServiceCollectionExtension")
                        .WithModifiers(InternalStaticPartialTokenList)
                        .WithMembers(SingletonList<MemberDeclarationSyntax>(
                            MethodDeclaration(TypeOfMicrosoftExtensionsDependencyInjectionIServiceCollection, "AddServices")
                                .WithModifiers(PublicStaticPartialTokenList)
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(TypeOfMicrosoftExtensionsDependencyInjectionIServiceCollection, Identifier("services"))
                                        .WithModifiers(ThisTokenList))))
                                .WithBody(Block(List(GenerateAddServices(context))))))))))
            .NormalizeWhitespace();

        production.AddSource("ServiceCollectionExtension.g.cs", syntax.ToFullStringWithHeader());
    }

    private static IEnumerable<StatementSyntax> GenerateAddServices(ServiceGeneratorContext context)
    {
        foreach (ServiceEntry entry in context.Services)
        {
            TypeSyntax targetType = entry.Type.GetTypeSyntax();
            SeparatedSyntaxList<TypeSyntax> typeArguments = SingletonSeparatedList(targetType);
            if (entry.Attribute.TryGetConstructorArgument(1, out TypedConstantInfo? info) &&
                info is TypedConstantInfo.Type infoType) // [Service(serviceLifetime, typeof(T))]
            {
                typeArguments = typeArguments.Insert(0, ParseTypeName(infoType.FullyQualifiedTypeName));
            }

            bool hasKey = entry.Attribute.TryGetNamedArgument("Key", out TypedConstantInfo? key);

            ArgumentListSyntax argumentList = hasKey
                ? ArgumentList(SingletonSeparatedList(
                    Argument(key!.GetSyntax())))
                : EmptyArgumentList;

            InvocationExpressionSyntax invocation = InvocationExpression(
                    SimpleMemberAccessExpression(
                        IdentifierName("services"),
                        GenericName($"Add{(hasKey ? "Keyed" : string.Empty)}{entry.ServiceLifetime}")
                            .WithTypeArgumentList(TypeArgumentList(typeArguments))))
                .WithArgumentList(argumentList);

            yield return ExpressionStatement(invocation);
        }

        yield return ReturnStatement(IdentifierName("services"));
    }

    private sealed record ServiceEntry : IComparable<ServiceEntry?>
    {
        public required AttributeInfo Attribute { get; init; }

        public required TypeInfo Type { get; init; }

        public required string ServiceLifetime { get; init; }

        public static ServiceEntry Create(GeneratorAttributeSyntaxContext context, CancellationToken token)
        {
            if (context.TargetSymbol is not INamedTypeSymbol typeSymbol || context.Attributes.SingleOrDefault() is not { } service)
            {
                return default!;
            }

            AttributeInfo serviceInfo = AttributeInfo.Create(service);

            string? serviceLifetime = default;
            if (serviceInfo.TryGetConstructorArgument(0, out TypedConstantInfo? lifetime) &&
                lifetime is TypedConstantInfo.Enum lifetimeEnum)
            {
                serviceLifetime = (int)lifetimeEnum.Value switch
                {
                    0 => "Singleton",
                    1 => "Scoped",
                    2 => "Transient",
                    _ => default
                };
            }

            if (string.IsNullOrEmpty(serviceLifetime))
            {
                return default!;
            }

            return new()
            {
                Attribute = serviceInfo,
                Type = TypeInfo.Create(typeSymbol),
                ServiceLifetime = serviceLifetime!,
            };
        }

        public int CompareTo(ServiceEntry? other)
        {
            if (ReferenceEquals(this, other))
            {
                return 0;
            }

            if (other is null)
            {
                return 1;
            }

            int result = string.Compare(ServiceLifetime, other.ServiceLifetime, StringComparison.Ordinal);

            if (result == 0)
            {
                result = string.Compare(Type.FullyQualifiedName, other.Type.FullyQualifiedName, StringComparison.Ordinal);
            }

            return result;
        }
    }

    private sealed record ServiceGeneratorContext
    {
        public required EquatableArray<ServiceEntry> Services { get; init; }

        public static ServiceGeneratorContext Create(ImmutableArray<ServiceEntry> services, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            return new()
            {
                Services = services.Sort(),
            };
        }
    }
}