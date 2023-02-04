#if NET6_0_OR_GREATER
using System.Collections;
using Jay.Text.Extensions;

namespace Jay.Text.Comparision;

public interface ITextComparer : 
    IComparer<string?>,
    IComparer<char[]?>,
    IComparer<char>,
    IComparer
{

    int IComparer<string?>.Compare(string? x, string? y) => Compare(x.AsSpan(), y.AsSpan());

    int IComparer<char[]?>.Compare(char[]? x, char[]? y) => Compare(x.AsSpan(), y.AsSpan());

    int IComparer<char>.Compare(char x, char y) => Compare(x, y);

    int IComparer.Compare(object? x, object? y)
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
        return Compare(xSpan, ySpan);
    }

    int Compare(ReadOnlySpan<char> x, ReadOnlySpan<char> y);
}
#endif