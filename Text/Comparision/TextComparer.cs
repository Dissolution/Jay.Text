namespace Jay.Text.Comparision;

public abstract class TextComparers : ITextComparer, ITextEqualityComparer
{
    public static implicit operator TextComparers(StringComparison stringComparison)
    {
        switch (stringComparison)
        {
            case StringComparison.CurrentCulture:
                return CurrentCulture;
            case StringComparison.CurrentCultureIgnoreCase:
                return CurrentCultureIgnoreCase;
            case StringComparison.InvariantCulture:
                return Invariant;
            case StringComparison.InvariantCultureIgnoreCase:
                return InvariantIgnoreCase;
            case StringComparison.Ordinal:
                return Ordinal;
            case StringComparison.OrdinalIgnoreCase:
                return OrdinalIgnoreCase;
            default:
                return Default;
        }
    }

    public static TextComparers CurrentCulture { get; } = new TextComparison(StringComparison.CurrentCulture);
    public static TextComparers CurrentCultureIgnoreCase { get; } = new TextComparison(StringComparison.CurrentCultureIgnoreCase);
    public static TextComparers Ordinal { get; } = new TextComparison(StringComparison.Ordinal);
    public static TextComparers OrdinalIgnoreCase { get; } = new TextComparison(StringComparison.OrdinalIgnoreCase);
    public static TextComparers Invariant { get; } = new TextComparison(StringComparison.InvariantCulture);
    public static TextComparers InvariantIgnoreCase { get; } = new TextComparison(StringComparison.InvariantCultureIgnoreCase);
                  
    public static TextComparers Default { get; } = new FastTextComparers();
    
    public abstract int Compare(ReadOnlySpan<char> left, ReadOnlySpan<char> right);
    public abstract bool Equals(ReadOnlySpan<char> left, ReadOnlySpan<char> right);
    public abstract int GetHashCode(ReadOnlySpan<char> span);
}