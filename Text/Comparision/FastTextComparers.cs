namespace Jay.Text.Comparision;

internal sealed class FastTextComparers : TextComparers
{
    public int Compare(char x, char y)
    {
        if (x < y) return -1;
        if (x == y) return 0;
        return 1;
    }

    public int Compare(string? x, string? y)
    {
        return string.Compare(x, y, StringComparison.Ordinal);
    }

    public int Compare(char[]? x, char[]? y)
    {
        return MemoryExtensions.SequenceCompareTo<char>(x, y);
    }

    public override int Compare(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
    {
        return MemoryExtensions.SequenceCompareTo<char>(left, right);
    }

    public bool Equals(char x, char y)
    {
        return x == y;
    }

    public bool Equals(string? x, string? y)
    {
        return TextHelper.Equals(x, y);
    }

    public bool Equals(char[]? x, char[]? y)
    {
        return TextHelper.Equals(x, y);
    }

    public override bool Equals(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
    {
        return TextHelper.Equals(left, right);
    }

    public int GetHashCode(char ch)
    {
        return ch.GetHashCode();
    }
    
    public int GetHashCode(string? text)
    {
        return string.GetHashCode(text);
    }

    public override int GetHashCode(ReadOnlySpan<char> text)
    {
        return string.GetHashCode(text);
    }
}