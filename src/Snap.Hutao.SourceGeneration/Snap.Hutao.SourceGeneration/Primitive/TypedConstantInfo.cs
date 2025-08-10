// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Snap.Hutao.SourceGeneration.Primitive;

internal abstract record TypedConstantInfo
{
    public static TypedConstantInfo Create(TypedConstant arg)
    {
        if (arg.IsNull)
        {
            return new Null();
        }

        if (arg.Kind == TypedConstantKind.Array)
        {
            string elementTypeName = ((IArrayTypeSymbol)arg.Type!).ElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            ImmutableArray<TypedConstantInfo> items = [.. arg.Values.Select(Create)];

            return new Array(elementTypeName, items);
        }

        return (arg.Kind, arg.Value) switch
        {
            (TypedConstantKind.Primitive, string text) => new Primitive.String(text),
            (TypedConstantKind.Primitive, bool flag) => new Primitive.Boolean(flag),
            (TypedConstantKind.Primitive, object value) => value switch
            {
                byte b => new Primitive.Of<byte>(b),
                char c => new Primitive.Of<char>(c),
                double d => new Primitive.Of<double>(d),
                float f => new Primitive.Of<float>(f),
                int i => new Primitive.Of<int>(i),
                long l => new Primitive.Of<long>(l),
                sbyte sb => new Primitive.Of<sbyte>(sb),
                short sh => new Primitive.Of<short>(sh),
                uint ui => new Primitive.Of<uint>(ui),
                ulong ul => new Primitive.Of<ulong>(ul),
                ushort ush => new Primitive.Of<ushort>(ush),
                _ => throw new ArgumentException("Invalid primitive type")
            },
            (TypedConstantKind.Type, ITypeSymbol type) => new Type(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
            (TypedConstantKind.Enum, object value) => new Enum(arg.Type!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), value),
            _ => throw new ArgumentException("Invalid typed constant type"),
        };
    }

    public static bool TryCreate(
        IOperation operation,
        SemanticModel semanticModel,
        ExpressionSyntax expression,
        CancellationToken token,
        [NotNullWhen(true)] out TypedConstantInfo? info)
    {
        if (operation.ConstantValue.HasValue)
        {
            // Enum values are constant but need to be checked explicitly in this case
            if (operation.Type?.TypeKind is TypeKind.Enum)
            {
                info = new Enum(operation.Type!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), operation.ConstantValue.Value!);

                return true;
            }

            // Handle all other constant literals normally
            info = operation.ConstantValue.Value switch
            {
                null => new Null(),
                string text => new Primitive.String(text),
                bool flag => new Primitive.Boolean(flag),
                byte b => new Primitive.Of<byte>(b),
                char c => new Primitive.Of<char>(c),
                double d => new Primitive.Of<double>(d),
                float f => new Primitive.Of<float>(f),
                int i => new Primitive.Of<int>(i),
                long l => new Primitive.Of<long>(l),
                sbyte sb => new Primitive.Of<sbyte>(sb),
                short sh => new Primitive.Of<short>(sh),
                uint ui => new Primitive.Of<uint>(ui),
                ulong ul => new Primitive.Of<ulong>(ul),
                ushort ush => new Primitive.Of<ushort>(ush),
                _ => throw new ArgumentException("Invalid primitive type")
            };

            return true;
        }

        if (operation is ITypeOfOperation typeOfOperation)
        {
            info = new Type(typeOfOperation.TypeOperand.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

            return true;
        }

        if (operation is IArrayCreationOperation)
        {
            string? elementTypeName = ((IArrayTypeSymbol?)operation.Type)?.ElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            // If the element type is not available (since the attribute wasn't checked), just default to object
            elementTypeName ??= "object";

            InitializerExpressionSyntax? initializerExpression =
                (expression as ImplicitArrayCreationExpressionSyntax)?.Initializer
                ?? (expression as ArrayCreationExpressionSyntax)?.Initializer;

            // No initializer found, just return an empty array
            if (initializerExpression is null)
            {
                info = new Array(elementTypeName, ImmutableArray<TypedConstantInfo>.Empty);

                return true;
            }

            using ImmutableArrayBuilder<TypedConstantInfo> items = ImmutableArrayBuilder<TypedConstantInfo>.Rent();

            // Enumerate all array elements and extract serialized info for them
            foreach (ExpressionSyntax initializationExpression in initializerExpression.Expressions)
            {
                if (semanticModel.GetOperation(initializationExpression, token) is not IOperation initializationOperation)
                {
                    goto Failure;
                }

                if (!TryCreate(initializationOperation, semanticModel, initializationExpression, token, out TypedConstantInfo? elementInfo))
                {
                    goto Failure;
                }

                items.Add(elementInfo);
            }

            info = new Array(elementTypeName, items.ToImmutable());

            return true;
        }

        Failure:
        info = null;

        return false;
    }

    public abstract ExpressionSyntax GetSyntax();

    public sealed record Array(string ElementTypeName, EquatableArray<TypedConstantInfo> Items) : TypedConstantInfo
    {
        public override ExpressionSyntax GetSyntax()
        {
            return
                ArrayCreationExpression(
                        ArrayType(IdentifierName(ElementTypeName))
                            .AddRankSpecifiers(ArrayRankSpecifier(SingletonSeparatedList<ExpressionSyntax>(OmittedArraySizeExpression()))))
                    .WithInitializer(InitializerExpression(SyntaxKind.ArrayInitializerExpression)
                        .AddExpressions(Items.Select(static c => c.GetSyntax()).ToArray()));
        }
    }

    public abstract record Primitive : TypedConstantInfo
    {
        public sealed record String(string Value) : TypedConstantInfo
        {
            /// <inheritdoc/>
            public override ExpressionSyntax GetSyntax()
            {
                return LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(Value));
            }
        }

        public sealed record Boolean(bool Value) : TypedConstantInfo
        {
            /// <inheritdoc/>
            public override ExpressionSyntax GetSyntax()
            {
                return LiteralExpression(Value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);
            }
        }

        public sealed record Of<T>(T Value) : TypedConstantInfo
            where T : unmanaged, IEquatable<T>
        {
            /// <inheritdoc/>
            public override ExpressionSyntax GetSyntax()
            {
                return LiteralExpression(SyntaxKind.NumericLiteralExpression, Value switch
                {
                    byte b => Literal(b),
                    char c => Literal(c),

                    // For doubles, we need to manually format it and always add the trailing "D" suffix.
                    // This ensures that the correct type is produced if the expression was assigned to
                    // an object (eg. the literal was used in an attribute object parameter/property).
                    double d => Literal(d.ToString("R", CultureInfo.InvariantCulture) + "D", d),

                    // For floats, Roslyn will automatically add the "F" suffix, so no extra work is needed
                    float f => Literal(f),
                    int i => Literal(i),
                    long l => Literal(l),
                    sbyte sb => Literal(sb),
                    short sh => Literal(sh),
                    uint ui => Literal(ui),
                    ulong ul => Literal(ul),
                    ushort ush => Literal(ush),
                    _ => throw new ArgumentException("Invalid primitive type")
                });
            }
        }
    }

    public sealed record Type(string TypeName) : TypedConstantInfo
    {
        public override ExpressionSyntax GetSyntax()
        {
            return TypeOfExpression(IdentifierName(TypeName));
        }
    }

    public sealed record Enum(string TypeName, object Value) : TypedConstantInfo
    {
        public override ExpressionSyntax GetSyntax()
        {
            // We let Roslyn parse the value expression, so that it can automatically handle both positive and negative values. This
            // is needed because negative values have a different syntax tree (UnaryMinusExpression holding the numeric expression).
            ExpressionSyntax valueExpression = ParseExpression(Value.ToString());

            // If the value is negative, we have to put parentheses around them (to avoid CS0075 errors)
            if (valueExpression is PrefixUnaryExpressionSyntax unaryExpression && unaryExpression.IsKind(SyntaxKind.UnaryMinusExpression))
            {
                valueExpression = ParenthesizedExpression(valueExpression);
            }

            // Now we can safely return the cast expression for the target enum type (with optional parentheses if needed)
            return CastExpression(IdentifierName(TypeName), valueExpression);
        }
    }

    public sealed record Null : TypedConstantInfo
    {
        /// <inheritdoc/>
        public override ExpressionSyntax GetSyntax()
        {
            return LiteralExpression(SyntaxKind.NullLiteralExpression);
        }
    }
}