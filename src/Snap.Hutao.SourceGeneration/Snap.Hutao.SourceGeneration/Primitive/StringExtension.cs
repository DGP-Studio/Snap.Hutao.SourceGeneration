using System.Text;

namespace Snap.Hutao.SourceGeneration.Primitive;

internal static class StringExtension
{
    public static string NormalizeSymbolName(this string symbol)
    {
        return new StringBuilder(symbol).Replace('<', '{').Replace('>', '}').ToString();
    }
}