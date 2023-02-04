#if NET6_0_OR_GREATER
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

    public abstract int Compare(ReadOnlySpan<char> x, ReadOnlySpan<char> y);
    public abstract bool Equals(ReadOnlySpan<char> x, ReadOnlySpan<char> y);
    public abstract int GetHashCode(ReadOnlySpan<char> span);
}

internal sealed class FastTextComparers : TextComparers
{
    public int Compare(char x, char y)
    {
        if (x < y) return -1;
        if (x == y) return 0;
        return 1;
    }

    public override int Compare(ReadOnlySpan<char> x, ReadOnlySpan<char> y)
    {
        return MemoryExtensions.SequenceCompareTo<char>(x, y);
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

    public override bool Equals(ReadOnlySpan<char> x, ReadOnlySpan<char> y)
    {
        return TextHelper.Equals(x, y);
    }

    public int GetHashCode(char ch)
    {
        return (int)ch;
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
#endif