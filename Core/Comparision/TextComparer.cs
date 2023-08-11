namespace Jay.Text.Comparision;

public abstract class TextComparer : ITextComparer
{
    public int Compare(string? x, string? y) => Compare(x.AsSpan(), y.AsSpan());

    public int Compare(char[]? x, char[]? y) => Compare(x.AsSpan(), y.AsSpan());

    public int Compare(char x, char y) => Compare(x.AsSpan(), y.AsSpan());

    public int Compare(object? x, object? y)
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
            char[] chars => chars,
            char ch => ch.AsSpan(),
            _ => default,
        };
        return Compare(xSpan, ySpan);
    }

    public abstract int Compare(ReadOnlySpan<char> x, ReadOnlySpan<char> y);
}