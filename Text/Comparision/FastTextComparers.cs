namespace Jay.Text.Comparision;

internal sealed class FastTextComparers : TextComparers
{
    public override int Compare(char x, char y)
    {
        if (x < y) return -1;
        if (x == y) return 0;
        return 1;
    }

    public override int Compare(ReadOnlySpan<char> x, ReadOnlySpan<char> y)
    {
        return MemoryExtensions.SequenceCompareTo<char>(x, y);
    }

    public override bool Equals(char x, char y)
    {
        return x == y;
    }

    public override bool Equals(string? x, string? y)
    {
        return TextHelper.Equals(x, y);
    }

    public override bool Equals(char[]? x, char[]? y)
    {
        return TextHelper.Equals(x, y);
    }

    public override bool Equals(ReadOnlySpan<char> x, ReadOnlySpan<char> y)
    {
        return TextHelper.Equals(x, y);
    }

    public override int GetHashCode(char ch)
    {
        return (int)ch;
    }

#if NET6_0_OR_GREATER
    public override int GetHashCode(string? text)
    {
        return string.GetHashCode(text);
    }

    public override int GetHashCode(ReadOnlySpan<char> text)
    {
        return string.GetHashCode(text);
    }
#else
    public override int GetHashCode(string? text)
    {
        if (text is null) return 0;
        return StringComparer.Ordinal.GetHashCode(text);
    }

    public override int GetHashCode(ReadOnlySpan<char> text)
    {
        return StringComparer.Ordinal.GetHashCode(text.AsString());
    }
#endif
}