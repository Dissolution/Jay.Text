namespace Jay.Text.Comparision;

internal sealed class TextComparison : TextComparers
{
    private readonly StringComparison _stringComparison;

    public TextComparison(StringComparison stringComparison)
    {
        _stringComparison = stringComparison;
    }

    public int Compare(string? x, string? y)
    {
        return string.Compare(x, y, _stringComparison);
    }
    
    

    public override int Compare(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
    {
        return MemoryExtensions.CompareTo(left, right, _stringComparison);
    }

    public bool Equals(string? x, string? y)
    {
        return string.Equals(x, y, _stringComparison);
    }

    public override bool Equals(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
    {
        return MemoryExtensions.Equals(left, right, _stringComparison);
    }

    public int GetHashCode(string? str)
    {
        if (str is null) return 0;
        return str.GetHashCode(_stringComparison);
    }

    public override int GetHashCode(ReadOnlySpan<char> span)
    {
        return string.GetHashCode(span, _stringComparison);
    }
}