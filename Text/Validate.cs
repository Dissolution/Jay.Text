namespace Jay.Text;

internal static class Validate
{
    /// <summary>
    /// Validates an existing index
    /// </summary>
    public static void Index(int available,
        int index, [CallerArgumentExpression(nameof(index))] string indexName = "")
    {
        if ((uint)index >= available)
            throw new ArgumentOutOfRangeException(indexName, index, $"{indexName} must be between 0 and {available - 1}");
    }

    /// <summary>
    /// Validates an insertion index
    /// </summary>
    public static void Insert(int available,
        int index, [CallerArgumentExpression(nameof(index))] string indexName = "")
    {
        if ((uint)index > available)
            throw new ArgumentOutOfRangeException(indexName, index, $"{indexName} must be between 0 and {available}");
    }

    /// <summary>
    /// Validates a <see cref="Range"/>
    /// </summary>
    public static void Range(int available,
        int index, int length,
        [CallerArgumentExpression(nameof(index))] string indexName = "",
        [CallerArgumentExpression(nameof(length))] string lengthName = "")
    {
        if (index < 0 || index > available)
            throw new ArgumentOutOfRangeException(indexName, index, $"{indexName} must be between 0 and {available}");
        if (index + length > available)
            throw new ArgumentOutOfRangeException(lengthName, length, $"{lengthName} must be between 0 and {available - index}");
    }

    public static void Range(int available,
        Range range, [CallerArgumentExpression(nameof(range))] string rangeName = "")
    {
        (int offset, int length) = range.GetOffsetAndLength(available);
        if (offset < 0 || offset > available || offset + length > available)
            throw new ArgumentOutOfRangeException(rangeName, range, $"Range must exist within [0..{available})");
    }
}