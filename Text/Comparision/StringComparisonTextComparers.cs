namespace Jay.Text.Comparision;

internal sealed class StringComparisonTextComparers : TextComparers
{
    private readonly StringComparison _stringComparison;

#if !NET6_0_OR_GREATER
    private StringComparer GetStringComparer()
    {
        return _stringComparison switch
        {
            StringComparison.CurrentCulture => StringComparer.CurrentCulture,
            StringComparison.CurrentCultureIgnoreCase => StringComparer.CurrentCultureIgnoreCase,
            StringComparison.InvariantCulture => StringComparer.InvariantCulture,
            StringComparison.InvariantCultureIgnoreCase => StringComparer.InvariantCultureIgnoreCase,
            StringComparison.Ordinal => StringComparer.Ordinal,
            StringComparison.OrdinalIgnoreCase => StringComparer.OrdinalIgnoreCase,
            _ => StringComparer.CurrentCulture
        };
    }
#endif

    public StringComparisonTextComparers(StringComparison stringComparison)
    {
        _stringComparison = stringComparison;
    }

    public override int Compare(string? x, string? y)
    {
        return string.Compare(x, y, _stringComparison);
    }

    public override int Compare(ReadOnlySpan<char> x, ReadOnlySpan<char> y)
    {
        return MemoryExtensions.CompareTo(x, y, _stringComparison);
    }

    public override bool Equals(string? x, string? y)
    {
        return string.Equals(x, y, _stringComparison);
    }

    public override bool Equals(ReadOnlySpan<char> x, ReadOnlySpan<char> y)
    {
        return MemoryExtensions.Equals(x, y, _stringComparison);
    }

#if NETSTANDARD2_1 || NET6_0_OR_GREATER
    public override int GetHashCode(string? str)
    {
        if (str is null) return 0;
        return str.GetHashCode(_stringComparison);
    }
#else
    public override int GetHashCode(string? str)
    {
        if (str is null) return 0;
        return GetStringComparer().GetHashCode(str);
    }
#endif

#if NET6_0_OR_GREATER
    public override int GetHashCode(ReadOnlySpan<char> span)
    {
        return string.GetHashCode(span, _stringComparison);
    }
#else
    public override int GetHashCode(ReadOnlySpan<char> span)
    {
        return GetStringComparer().GetHashCode(span.AsString());
    }
#endif
}