#if NET6_0_OR_GREATER
using System.Collections;
using Jay.Text.Extensions;

namespace Jay.Text.Comparision;

public interface ITextEqualityComparer : 
    IEqualityComparer<string?>,
    IEqualityComparer<char[]>,
    IEqualityComparer<char>,
    IEqualityComparer
{

    bool IEqualityComparer<string?>.Equals(string? x, string? y) => Equals(x.AsSpan(), y.AsSpan());

    bool IEqualityComparer<char[]>.Equals(char[]? x, char[]? y) => Equals(x.AsSpan(), y.AsSpan());

    bool IEqualityComparer<char>.Equals(char x, char y) => Equals(x.AsSpan(), y.AsSpan());

    bool IEqualityComparer.Equals(object? x, object? y)
    {
        ReadOnlySpan<char> xSpan = x switch
        {
            string str => str.AsSpan(),
            char[] chars => chars,
            char ch => ch.AsSpan(),
            _ => default
        };
        ReadOnlySpan<char> ySpan = y switch
        {
            string str => str.AsSpan(),
            char[] chars => chars,
            char ch => ch.AsSpan(),
            _ => default
        };
        return Equals(xSpan, ySpan);
    }

    bool Equals(ReadOnlySpan<char> x, ReadOnlySpan<char> y);

    int IEqualityComparer<string?>.GetHashCode(string? text) => GetHashCode(text.AsSpan());

    int IEqualityComparer<char[]>.GetHashCode(char[]? chars) => GetHashCode(chars.AsSpan());

    int IEqualityComparer<char>.GetHashCode(char ch) => GetHashCode(ch.AsSpan());

    int IEqualityComparer.GetHashCode(object? obj)
    {
        ReadOnlySpan<char> span = obj switch
        {
            string str => str.AsSpan(),
            char[] chars => chars,
            char ch => ch.AsSpan(),
            _ => default
        };
        return GetHashCode(span);
    }

    int GetHashCode(ReadOnlySpan<char> span);
}
#endif