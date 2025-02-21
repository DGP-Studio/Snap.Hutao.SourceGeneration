// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

namespace Snap.Hutao.SourceGeneration;

[Generator(LanguageNames.CSharp)]
internal sealed class InterpolatedGenerator : IIncrementalGenerator
{
    private const string ParseMethodName = "Snap.Hutao.Core.Text.Interpolated.Parse(string, Snap.Hutao.Core.Text.Interpolated.InterpolatedParseStringHandler)";

    private static readonly HashSet<string> BuiltInTypes =
    [
        "string",
        "char",
        "bool",
        "byte",
        "sbyte",
        "ushort",
        "short",
        "int",
        "uint",
        "long",
        "ulong",
        "float",
        "double",
        "decimal"
    ];

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(InitializationOutput);

        IncrementalValueProvider<ImmutableArray<ParserCall>> interpolationCalls = context.SyntaxProvider
            .CreateSyntaxProvider(FindParseCalls, FindParseCallsTransform)!
            .Where<ParserCall>(x => x is not null)
            .Collect();

        context.RegisterSourceOutput(interpolationCalls, EmitParseCallsCode);

        IncrementalValueProvider<ImmutableArray<TypeData>> types = interpolationCalls.Select(FindTypes);
        context.RegisterSourceOutput(types, EmitTypesCode);
    }

    private static void InitializationOutput(IncrementalGeneratorPostInitializationContext context)
    {
        /* lang=c# */
        const string Source = """"
            #nullable enable

            using System.Collections.Immutable;
            using System.Globalization;
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;
            
            namespace Snap.Hutao.Core.Text;

            internal static partial class Interpolated
            {
                public static void Parse(string input, [InterpolatedStringHandlerArgument("input")] InterpolatedParseStringHandler template)
                {
                    // Do nothing
                }
            
                [InterpolatedStringHandler]
                public partial struct InterpolatedParseStringHandler
                {
                    private static readonly Dictionary<(string File, int Line), InterpolatedStringSource> Sources = GetSourcesLineExpanded();
            
                    private readonly string inputString;
                    private readonly InterpolatedStringSource source;
            
                    private int currentIndex;
                    private int currentStringPosition;
            
                    public InterpolatedParseStringHandler(
                        int literalLength,
                        int formattedCount,
                        string input,
                        out bool shouldFormat,
                        [CallerFilePath] string sourceFilePath = "",
                        [CallerLineNumber] int sourceLineNumber = 0)
                    {
                        shouldFormat = true;
            
                        currentIndex = 0;
                        currentStringPosition = 0;
                        inputString = input;
                        if (!Sources.TryGetValue((sourceFilePath, sourceLineNumber), out source!))
                        {
                            throw new InvalidOperationException($"""
                                Unable to find source string for {sourceFilePath}:{sourceLineNumber}.
                                This can occur if the source generator resolves the line that calls Parse incorrectly.
                                Make sure there's only one Interpolated.Parse call on its line.
                                """);
                        }
                    }
            
                    private static Dictionary<(string File, int Line), InterpolatedStringSource> GetSourcesLineExpanded()
                    {
                        Dictionary<(string File, int Line), InterpolatedStringSource> dict = [];
                        Dictionary<(string File, LineRange Lines), InterpolatedStringSource> sources = GetInterpolatedStringSources();
                        foreach (((string file, LineRange lines), InterpolatedStringSource source) in sources)
                        {
                            for (int i = lines.Start; i <= lines.End; i++)
                            {
                                dict.Add((file, i), source);
                            }
                        }
            
                        return dict;
                    }
            
                    private static List<T> GetListFromCharsParsable<T>(ReadOnlySpan<char> chars, ReadOnlySpan<char> separator)
                        where T : IParsable<T>
                    {
                        if (separator[0] is '\'' && separator[^1] is '\'')
                        {
                            separator = separator[1..^1];
                        }
            
                        List<T> list = [];
            
                        while (true)
                        {
                            int index = chars.IndexOf(separator);
                            if (index is -1)
                            {
                                list.Add(T.Parse(new(chars), CultureInfo.InvariantCulture));
                                break;
                            }
            
                            list.Add(T.Parse(new(chars[..index]), CultureInfo.InvariantCulture));
                            chars = chars[(index + separator.Length)..];
                        }
            
                        return list;
                    }
            
                    private static List<T> GetListFromCharsSpanParsable<T>(ReadOnlySpan<char> chars, ReadOnlySpan<char> separator)
                        where T : ISpanParsable<T>
                    {
                        if (separator[0] is '\'' && separator[^1] is '\'')
                        {
                            separator = separator[1..^1];
                        }
            
                        List<T> list = [];
            
                        while (true)
                        {
                            int index = chars.IndexOf(separator);
                            if (index is -1)
                            {
                                list.Add(T.Parse(chars, CultureInfo.InvariantCulture));
                                break;
                            }
            
                            list.Add(T.Parse(chars[..index], CultureInfo.InvariantCulture));
                            chars = chars[(index + separator.Length)..];
                        }
            
                        return list;
                    }
            
                    private ReadOnlySpan<char> GetNextPart()
                    {
                        // Final bit
                        if (currentIndex >= source.Components.Length)
                        {
                            if (currentStringPosition > inputString.Length)
                            {
                                throw new InvalidOperationException("""
                                    Tried to read beyond the size of the input string.
                                    This usually means the provided string is missing parts of the template.
                                    """);
                            }
            
                            return inputString.AsSpan(currentStringPosition, inputString.Length - currentStringPosition);
                        }
            
                        string nextPart = source.Components[currentIndex++];
                        int index = inputString.IndexOf(nextPart, currentStringPosition, StringComparison.Ordinal);
            
                        if (index <= -1)
                        {
                            throw new InvalidOperationException($"""
                                Failed to find the next part of the template: \"{nextPart}\" in the remainder of the input string.
                                Make sure the template matches the input string, and that any previous parsed part does not contain the substring: "{nextPart}"
                                """);
                        }
            
                        int startPos = currentStringPosition;
                        currentStringPosition = index;
                        return inputString.AsSpan(startPos, index - startPos);
                    }
            
                    private sealed class InterpolatedStringSource
                    {
                        public InterpolatedStringSource(string[] components)
                        {
                            Components = ImmutableCollectionsMarshal.AsImmutableArray(components);
                        }
            
                        public ImmutableArray<string> Components { get; }
                    }
            
                    private sealed class LineRange
                    {
                        public LineRange(int start, int end)
                        {
                            Start = start;
                            End = end;
                        }
            
                        public int Start { get; }
            
                        public int End { get; }
                    }
            
                }
            }

            partial class Interpolated
            {
                partial struct InterpolatedParseStringHandler
                {
                    public void AppendLiteral(string literal)
                    {
                        currentStringPosition += literal.Length;
                    }
            
                    public void AppendFormatted(in string value)
                    {
                        ReadOnlySpan<char> nextPart = GetNextPart();
            
                        // Errors here, but is implemented everywhere interpolated string handling is.
                        Unsafe.AsRef(in value) = new(nextPart);
                    }
            
                    public void AppendFormatted(in char value)
                    {
                        ReadOnlySpan<char> nextPart = GetNextPart();
                        Unsafe.AsRef(in value) = nextPart[0];
                    }
            
                    public void AppendFormatted(in bool value)
                    {
                        ReadOnlySpan<char> nextPart = GetNextPart();
                        Unsafe.AsRef(in value) = bool.Parse(nextPart);
                    }
            
                    public void AppendFormatted(in byte value)
                    {
                        AppendFormatted(in value, 0, default);
                    }
            
                    public void AppendFormatted(in byte value, int alignment = 0, string? format = default)
                    {
                        ReadOnlySpan<char> nextPart = GetNextPart();
                        Unsafe.AsRef(in value) = byte.Parse(nextPart, GetNumberStylesFromString(format), CultureInfo.InvariantCulture);
                    }
            
                    public void AppendFormatted(in sbyte value)
                    {
                        AppendFormatted(in value, 0, default);
                    }
            
                    public void AppendFormatted(in sbyte value, int alignment = 0, string? format = default)
                    {
                        ReadOnlySpan<char> nextPart = GetNextPart();
                        Unsafe.AsRef(in value) = sbyte.Parse(nextPart, GetNumberStylesFromString(format), CultureInfo.InvariantCulture);
                    }
            
                    public void AppendFormatted(in short value)
                    {
                        AppendFormatted(in value, 0, default);
                    }
            
                    public void AppendFormatted(in short value, int alignment = 0, string? format = default)
                    {
                        ReadOnlySpan<char> nextPart = GetNextPart();
                        Unsafe.AsRef(in value) = short.Parse(nextPart, GetNumberStylesFromString(format), CultureInfo.InvariantCulture);
                    }
            
                    public void AppendFormatted(in ushort value)
                    {
                        AppendFormatted(in value, 0, default);
                    }
            
                    public void AppendFormatted(in ushort value, int alignment = 0, string? format = default)
                    {
                        ReadOnlySpan<char> nextPart = GetNextPart();
                        Unsafe.AsRef(in value) = ushort.Parse(nextPart, GetNumberStylesFromString(format), CultureInfo.InvariantCulture);
                    }
            
                    public void AppendFormatted(in int value)
                    {
                        AppendFormatted(value, 0, default);
                    }
            
                    public void AppendFormatted(in int value, int alignment = 0, string? format = default)
                    {
                        ReadOnlySpan<char> nextPart = GetNextPart();
                        Unsafe.AsRef(in value) = int.Parse(nextPart, GetNumberStylesFromString(format), CultureInfo.InvariantCulture);
                    }
            
                    public void AppendFormatted(in uint value)
                    {
                        AppendFormatted(in value, 0, default);
                    }
            
                    public void AppendFormatted(in uint value, int alignment = 0, string? format = default)
                    {
                        ReadOnlySpan<char> nextPart = GetNextPart();
                        Unsafe.AsRef(in value) = uint.Parse(nextPart, GetNumberStylesFromString(format), CultureInfo.InvariantCulture);
                    }
            
                    public void AppendFormatted(in long value)
                    {
                        AppendFormatted(in value, 0, default);
                    }
            
                    public void AppendFormatted(in long value, int alignment = 0, string? format = default)
                    {
                        ReadOnlySpan<char> nextPart = GetNextPart();
                        Unsafe.AsRef(in value) = long.Parse(nextPart, GetNumberStylesFromString(format), CultureInfo.InvariantCulture);
                    }
            
                    public void AppendFormatted(in ulong value)
                    {
                        AppendFormatted(in value, 0, default);
                    }
            
                    public void AppendFormatted(in ulong value, int alignment = 0, string? format = default)
                    {
                        ReadOnlySpan<char> nextPart = GetNextPart();
                        Unsafe.AsRef(in value) = ulong.Parse(nextPart, GetNumberStylesFromString(format), CultureInfo.InvariantCulture);
                    }
            
                    public void AppendFormatted(in float value)
                    {
                        AppendFormatted(in value, 0, default);
                    }
            
                    public void AppendFormatted(in float value, int alignment = 0, string? format = default)
                    {
                        ReadOnlySpan<char> nextPart = GetNextPart();
                        Unsafe.AsRef(in value) = float.Parse(nextPart, GetNumberStylesFromString(format), CultureInfo.InvariantCulture);
                    }
            
                    public void AppendFormatted(in double value)
                    {
                        AppendFormatted(in value, 0, default);
                    }
            
                    public void AppendFormatted(in double value, int alignment = 0, string? format = default)
                    {
                        ReadOnlySpan<char> nextPart = GetNextPart();
                        Unsafe.AsRef(in value) = double.Parse(nextPart, GetNumberStylesFromString(format), CultureInfo.InvariantCulture);
                    }
            
                    public void AppendFormatted(in decimal value)
                    {
                        AppendFormatted(in value, 0, default);
                    }
            
                    public void AppendFormatted(in decimal value, int alignment = 0, string? format = default)
                    {
                        ReadOnlySpan<char> nextPart = GetNextPart();
                        Unsafe.AsRef(in value) = decimal.Parse(nextPart, GetNumberStylesFromString(format), CultureInfo.InvariantCulture);
                    }
            
                    private static NumberStyles GetNumberStylesFromString(string? style)
                    {
                        if (style is null)
                        {
                            return NumberStyles.Integer;
                        }
            
                        NumberStyles styles = NumberStyles.Integer;
            
                        if (style.StartsWith("x", StringComparison.CurrentCultureIgnoreCase))
                        {
                            styles = NumberStyles.HexNumber;
                        }
            
                        return styles;
                    }
                }
            }
            """";
        context.AddSource("Interpolated.Core.g.cs", Source);
    }

    private static bool FindParseCalls(SyntaxNode node, CancellationToken token)
    {
        if (node is not InvocationExpressionSyntax invocation)
        {
            return false;
        }

        return invocation.ArgumentList.Arguments.Count > 0;
    }

    private static ParserCall? FindParseCallsTransform(GeneratorSyntaxContext context, CancellationToken token)
    {
        if (context.SemanticModel.GetOperation(context.Node, token) is not IInvocationOperation operation)
        {
            return null;
        }

        SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(context.Node, token);

        if (symbolInfo.Symbol?.ToString() != ParseMethodName)
        {
            return null;
        }

        if (operation.Arguments[1].Value is not IInterpolatedStringHandlerCreationOperation interpolatedString)
        {
            return null;
        }

        if (interpolatedString.ChildOperations.Last() is not IInterpolatedStringOperation stringOperation)
        {
            return null;
        }

        List<TypeData> types = [];
        List<string> components = [];
        bool isFirst = true;

        foreach (IOperation childOperation in stringOperation.ChildOperations)
        {
            if (childOperation.Syntax is InterpolatedStringTextSyntax interpolatedStringText)
            {
                if (!isFirst)
                {
                    components.Add(interpolatedStringText.TextToken.ValueText);
                }

                continue;
            }

            isFirst = false;

            if (childOperation.Syntax is not InterpolationSyntax interpolationSyntax)
            {
                continue;
            }

            if (interpolationSyntax.Expression is not IdentifierNameSyntax idNameSyntax)
            {
                continue;
            }

            TypeData? type = GetTypeData(context.SemanticModel.GetTypeInfo(idNameSyntax).Type, false);
            if (type is not null)
            {
                types.Add(type);
            }
        }

        FileLinePositionSpan lineSpan = context.Node.GetLocation().GetLineSpan();

        return new(types.ToArray(), context.Node.SyntaxTree.FilePath, lineSpan.StartLinePosition.Line + 1, lineSpan.EndLinePosition.Line + 1, components.ToArray());
    }

    private static TypeData? GetTypeData(ITypeSymbol? type, bool ignoreBuiltinCheck)
    {
        if (type is null)
        {
            return null;
        }

        string typeName = type.ToString();

        if ((!ignoreBuiltinCheck) && BuiltInTypes.Contains(typeName))
        {
            return null;
        }

        TypeData? innerType = null;

        TypeDataKind kind;
        if (type is IArrayTypeSymbol array)
        {
            kind = TypeDataKind.Array;
            innerType = GetTypeData(array.ElementType, true);
        }
        else if (type.OriginalDefinition?.ToString() == "System.Collections.Generic.List<T>")
        {
            if (type is not INamedTypeSymbol namedType)
            {
                return null;
            }

            kind = TypeDataKind.List;
            innerType = GetTypeData(namedType.TypeArguments[0], true);
        }
        else
        {
            string spanParsableString = $"System.ISpanParsable<{typeName}>";
            bool isSpanParsable = type.AllInterfaces.Any(i => i.ToString() == spanParsableString);

            if (!isSpanParsable)
            {
                // span parsable has priority, so only check for normal parsable as fallback
                string parsableString = $"System.IParsable<{typeName}>";
                if (type.AllInterfaces.Any(i => i.ToString() == parsableString) == false)
                {
                    // If neither type of parsable, skip the type
                    return null;
                }
            }

            kind = isSpanParsable ? TypeDataKind.SpanParsable : TypeDataKind.Parsable;
        }

        return new(typeName, kind, innerType);
    }

    private static void EmitParseCallsCode(SourceProductionContext context, ImmutableArray<ParserCall> parserCalls)
    {
        CodeBuilder code = new();
        code.AddLine("namespace Snap.Hutao.Core.Text;");
        code.AddLine();
        code.StartBlock("partial class Interpolated");
        code.StartBlock("partial struct InterpolatedParseStringHandler");
        code.StartBlock("private static Dictionary<(string File, LineRange Line), InterpolatedStringSource> GetInterpolatedStringSources()");
        code.StartBlock("return new()");

        foreach (ParserCall call in parserCalls)
        {
            code.AddLine($"{{({EscapeString(call.FileLocation)}, new({call.LineStart}, {call.LineEnd})), new([{string.Join(", ", call.Components.Select(EscapeString))}])}},");
        }

        code.EndBlock(";");
        code.EndBlock();
        code.EndBlock();
        code.EndBlock();

        context.AddSource("Interpolated.Sources.g.cs", code.ToString());
    }

    private static ImmutableArray<TypeData> FindTypes(ImmutableArray<ParserCall> calls, CancellationToken token)
    {
        Dictionary<string, TypeData> dict = [];

        foreach (TypeData type in calls.SelectMany(x => x.Types))
        {
            if (dict.ContainsKey(type.FullName))
            {
                continue;
            }

            dict.Add(type.FullName, type);
        }

        return [.. dict.Values];
    }

    private static void EmitTypesCode(SourceProductionContext context, ImmutableArray<TypeData> types)
    {
        CodeBuilder code = new();
        code.AddLine("#nullable enable");
        code.AddLine("namespace Snap.Hutao.Core.Text;");
        code.AddLine();
        code.StartBlock("partial class Interpolated");
        code.StartBlock("partial struct InterpolatedParseStringHandler");

        foreach (TypeData type in types)
        {
            switch (type.Kind)
            {
                case (TypeDataKind.Parsable or TypeDataKind.SpanParsable):
                    {
                        code.StartBlock($"public void AppendFormatted(in {type.FullName} value)");
                        code.AddLine("ReadOnlySpan<char> nextPart = GetNextPart();");
                        code.AddLine("Unsafe.AsRef(in value) = ");
                        string variableCall = type.Kind == TypeDataKind.SpanParsable ? "nextPart" : "new string(nextPart)";
                        code.AddLine($" {type.FullName}.Parse({variableCall}, System.Globalization.CultureInfo.InvariantCulture);");
                        code.EndBlock();
                        break;
                    }
                case TypeDataKind.Array or TypeDataKind.List:
                    {
                        code.StartBlock($"public void AppendFormatted(in {type.FullName} value, int alignment = 0, string? format = null)");

                        string returnedValue = type.Kind == TypeDataKind.List ? "list" : "list.ToArray()";
                        string function = type.InnerType!.Kind == TypeDataKind.Parsable ? "GetListFromCharsParsable" : "GetListFromCharsSpanParsable";

                        code.AddLine($$"""
                            if (format is null || format.Length is 0)
                            {
                                throw new System.InvalidOperationException("An array needs a separator provided in the format string provided.", nameof(format));
                            }

                            ReadOnlySpan<char> nextPart = GetNextPart();

                            List<{{type.InnerType.FullName}}> list = {{function}}<{{type.InnerType.FullName}}>(nextPart, format);
                            Unsafe.AsRef(in value) = {{returnedValue}};
                            """);

                        code.EndBlock();
                        break;
                    }
            }
        }

        code.EndBlock();
        code.EndBlock();

        context.AddSource("Interpolated.Types.g.cs", code.ToString());
    }

    private static string EscapeString(string str)
    {
        return new StringBuilder($"\"{str}\"")
            .Replace("\\", """\\""")
            .Replace("\r", """\r""")
            .Replace("\n", """\n""")
            .ToString();
    }

    internal record ParserCall(TypeData[] Types, string FileLocation, int LineStart, int LineEnd, string[] Components);

    public enum TypeDataKind
    {
        Parsable,
        SpanParsable,
        Array,
        List,
    }

    public record TypeData(string FullName, TypeDataKind Kind, TypeData? InnerType);

    public class CodeBuilder
    {
        private readonly StringBuilder builder;
        private int indent;

        public CodeBuilder()
        {
            builder = new();
            indent = 0;
        }

        public void AddLine()
        {
            builder.AppendLine();
        }

        public void AddLine(string line)
        {
            if (line.Contains("\n"))
            {
                foreach (string str in line.Split('\n'))
                {
                    AddLine(str);
                }

                return;
            }

            builder.AppendLine(new string(' ', indent * 4) + line);
        }

        public void AddLines(params string[] lines)
        {
            foreach (string line in lines)
            {
                builder.AppendLine(new string('\t', indent) + line);
            }
        }

        public void StartBlock()
        {
            AddLine("{");
            Indent();
        }

        public void StartBlock(string blockStart)
        {
            AddLine(blockStart);
            AddLine("{");
            Indent();
        }

        public void EndBlock()
        {
            Unindent();
            AddLine("}");
        }

        public void EndBlock(string blockEnd)
        {
            Unindent();
            AddLine($"}}{blockEnd}");
        }

        public void Indent()
        {
            indent++;
        }

        public void Unindent()
        {
            indent--;
        }

        public override string ToString()
        {
            return builder.ToString();
        }
    }
}