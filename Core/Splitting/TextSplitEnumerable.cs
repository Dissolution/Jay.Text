namespace Jay.Text.Splitting;

public readonly ref struct TextSplitEnumerable 
    // : IEnumerable<ReadOnlySpan<char>>, IEnumerable
{
    public readonly ReadOnlySpan<char> InputText;
    public readonly ReadOnlySpan<char> Separator;
    public readonly TextSplitOptions SplitOptions;
    public readonly StringComparison StringComparison;

    public TextSplitEnumerable(
        ReadOnlySpan<char> inputText,
        ReadOnlySpan<char> separator,
        TextSplitOptions splitOptions = TextSplitOptions.None,
        StringComparison stringComparison = StringComparison.Ordinal
    )
    {
        InputText = inputText;
        Separator = separator;
        SplitOptions = splitOptions;
        StringComparison = stringComparison;
    }

    public IReadOnlyList<string> ToListOfStrings()
    {
        var e = GetEnumerator();
        var strings = new List<string>();
        while (e.MoveNext())
        {
            strings.Add(e.String);
        }
        return strings;
    }

    public TextSplitList ToList()
    {
        List<Range> ranges = new();
        var e = GetEnumerator();
        while (e.MoveNext())
        {
            ranges.Add(e.Range);
        }
        return new TextSplitList(InputText, ranges);
    }


    /// <inheritdoc cref="IEnumerable{T}"/>
    public TextSplitEnumerator GetEnumerator()
    {
        return new TextSplitEnumerator(this);
    }
}