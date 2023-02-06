using Jay.Text.Extensions;

namespace Jay.Text.Comparision;

public abstract class TextEqualityComparer : ITextEqualityComparer
{
    public bool Equals(string? x, string? y) => Equals(x.AsSpan(), y.AsSpan());

    public bool Equals(char[]? x, char[]? y) => Equals(x.AsSpan(), y.AsSpan());

    public bool Equals(char x, char y) => Equals(x.AsSpan(), y.AsSpan());

    public new bool Equals(object? x, object? y)
    {
        ReadOnlySpan<char> xSpan = x switch
        {
            string str => str.AsSpan(),
            char[] chars => chars,
            char ch => ch.AsSpan(),
            _ => default,
        };
        ReadOnlySpan<char> ySpan = y switch
        {
            string str => str.AsSpan(),
            char[] chars => chars.AsSpan(),
            char ch => ch.AsSpan(),
            _ => default,
        };
        return Equals(xSpan, ySpan);
    }

    public abstract bool Equals(ReadOnlySpan<char> x, ReadOnlySpan<char> y);

    public int GetHashCode(string? text) => GetHashCode(text.AsSpan());

    public int GetHashCode(char[]? chars) => GetHashCode(chars.AsSpan());

    public int GetHashCode(char ch) => GetHashCode(ch.AsSpan());

    public int GetHashCode(object? obj)
    {
        ReadOnlySpan<char> span = obj switch
        {
            string str => str.AsSpan(),
            char[] chars => chars.AsSpan(),
            char ch => ch.AsSpan(),
            _ => default,
        };
        return GetHashCode(span);
    }
    
    public abstract int GetHashCode(ReadOnlySpan<char> span);
}