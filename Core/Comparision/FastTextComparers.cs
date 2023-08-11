using System.Collections;
// ReSharper disable InvokeAsExtensionMethod

namespace Jay.Text.Comparision;

public sealed class FastTextComparers : ITextComparers
{
#region Compare
    int IComparer<string?>.Compare(string? x, string? y) => Compare(x, y);
    int IComparer<char[]?>.Compare(char[]? x, char[]? y) => Compare(x, y);
    int IComparer<char>.Compare(char x, char y) => x.CompareTo(y);
    int IComparer.Compare(object? x, object? y)
    {
        ReadOnlySpan<char> xSpan = x switch
        {
            string str => str.AsSpan(),
            char[] chars => chars.AsSpan(),
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
        return Compare(xSpan, ySpan);
    }
    int ITextComparer.Compare(ReadOnlySpan<char> x, ReadOnlySpan<char> y) => Compare(x, y);

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(ReadOnlySpan<char> x, ReadOnlySpan<char> y)
    {
        return MemoryExtensions.SequenceCompareTo(x, y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(ReadOnlySpan<char> x, char[]? y)
    {
        return MemoryExtensions.SequenceCompareTo(x, y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(ReadOnlySpan<char> x, string? y)
    {
        return MemoryExtensions.SequenceCompareTo(x, MemoryExtensions.AsSpan(y));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(char[]? x, ReadOnlySpan<char> y)
    {
        return MemoryExtensions.SequenceCompareTo(x, y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(char[]? x, char[]? y)
    {
        return MemoryExtensions.SequenceCompareTo<char>(x, y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(char[]? x, string? y)
    {
        return MemoryExtensions.SequenceCompareTo(x, MemoryExtensions.AsSpan(y));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(string? x, ReadOnlySpan<char> y)
    {
        return MemoryExtensions.SequenceCompareTo(MemoryExtensions.AsSpan(x), y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(string? x, char[]? y)
    {
        return MemoryExtensions.SequenceCompareTo(MemoryExtensions.AsSpan(x), y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(string? x, string? y)
    {
        return string.CompareOrdinal(x, y);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(ReadOnlySpan<char> x, ReadOnlySpan<char> y, StringComparison comparison)
    {
        return MemoryExtensions.CompareTo(x, y, comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(ReadOnlySpan<char> x, char[]? y, StringComparison comparison)
    {
        return MemoryExtensions.CompareTo(x, y, comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(ReadOnlySpan<char> x, string? y, StringComparison comparison)
    {
        return MemoryExtensions.CompareTo(x, MemoryExtensions.AsSpan(y), comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(char[]? x, ReadOnlySpan<char> y, StringComparison comparison)
    {
        return MemoryExtensions.CompareTo(x, y, comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(char[]? x, char[]? y, StringComparison comparison)
    {
        return MemoryExtensions.CompareTo(x, y, comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(char[]? x, string? y, StringComparison comparison)
    {
        return MemoryExtensions.CompareTo(x, MemoryExtensions.AsSpan(y), comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(string? x, ReadOnlySpan<char> y, StringComparison comparison)
    {
        return MemoryExtensions.CompareTo(MemoryExtensions.AsSpan(x), y, comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(string? x, char[]? y, StringComparison comparison)
    {
        return MemoryExtensions.CompareTo(MemoryExtensions.AsSpan(x), y, comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(string? x, string? y, StringComparison comparison)
    {
        return string.Compare(x, y, comparison);
    }
#endregion
    
#region Equals
    bool IEqualityComparer<string?>.Equals(string? x, string? y) => Equals(x, y);
    bool IEqualityComparer<char[]>.Equals(char[]? x, char[]? y) => Equals(x, y);
    bool IEqualityComparer<char>.Equals(char x, char y) => x == y;
    bool IEqualityComparer.Equals(object? x, object? y)
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
    bool ITextEqualityComparer.Equals(ReadOnlySpan<char> x, ReadOnlySpan<char> y) => Equals(x, y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(ReadOnlySpan<char> x, ReadOnlySpan<char> y)
    {
        return MemoryExtensions.SequenceEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(ReadOnlySpan<char> x, char[]? y)
    {
        return MemoryExtensions.SequenceEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(ReadOnlySpan<char> x, string? y)
    {
        return MemoryExtensions.SequenceEqual(x, MemoryExtensions.AsSpan(y));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(char[]? x, ReadOnlySpan<char> y)
    {
        return MemoryExtensions.SequenceEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(char[]? x, char[]? y)
    {
        return MemoryExtensions.SequenceEqual<char>(x, y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(char[]? x, string? y)
    {
        return MemoryExtensions.SequenceEqual(x, MemoryExtensions.AsSpan(y));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(string? x, ReadOnlySpan<char> y)
    {
        return MemoryExtensions.SequenceEqual(MemoryExtensions.AsSpan(x), y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(string? x, char[]? y)
    {
        return MemoryExtensions.SequenceEqual(MemoryExtensions.AsSpan(x), y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(string? x, string? y)
    {
        return string.Equals(x, y);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(ReadOnlySpan<char> x, ReadOnlySpan<char> y, StringComparison comparison)
    {
        return MemoryExtensions.Equals(x, y, comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(ReadOnlySpan<char> x, char[]? y, StringComparison comparison)
    {
        return MemoryExtensions.Equals(x, y, comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(ReadOnlySpan<char> x, string? y, StringComparison comparison)
    {
        return MemoryExtensions.Equals(x, MemoryExtensions.AsSpan(y), comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(char[]? x, ReadOnlySpan<char> y, StringComparison comparison)
    {
        return MemoryExtensions.Equals(x, y, comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(char[]? x, char[]? y, StringComparison comparison)
    {
        return MemoryExtensions.Equals(x, y, comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(char[]? x, string? y, StringComparison comparison)
    {
        return MemoryExtensions.Equals(x, MemoryExtensions.AsSpan(y), comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(string? x, ReadOnlySpan<char> y, StringComparison comparison)
    {
        return MemoryExtensions.Equals(MemoryExtensions.AsSpan(x), y, comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(string? x, char[]? y, StringComparison comparison)
    {
        return MemoryExtensions.Equals(MemoryExtensions.AsSpan(x), y, comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(string? x, string? y, StringComparison comparison)
    {
        return string.Equals(x, y, comparison);
    }

#endregion

#region GetHashCode
    int IEqualityComparer<string?>.GetHashCode(string? str) => GetHashCode(str);
    int IEqualityComparer<char[]>.GetHashCode(char[]? chars) => GetHashCode(chars);
    int IEqualityComparer<char>.GetHashCode(char ch) => (int)ch;
    int IEqualityComparer.GetHashCode(object? obj)
    {
        return obj switch
        {
            char ch => (int)ch,
            char[] chars => GetHashCode(chars.AsSpan()),
            string str => GetHashCode(str.AsSpan()),
            _ => 0,
        };
    }
    int ITextEqualityComparer.GetHashCode(ReadOnlySpan<char> span) => GetHashCode(span);

#if NETCOREAPP3_1_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetHashCode(string? str)
    {
        return string.GetHashCode(str);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetHashCode(char[]? chars)
    {
        return string.GetHashCode(chars);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetHashCode(ReadOnlySpan<char> text)
    {
        return string.GetHashCode(text);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetHashCode(string? str, StringComparison comparison)
    {
        return string.GetHashCode(str, comparison);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetHashCode(char[]? chars, StringComparison comparison)
    {
        return string.GetHashCode(chars, comparison);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetHashCode(ReadOnlySpan<char> text, StringComparison comparison)
    {
        return string.GetHashCode(text, comparison);
    }
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetHashCode(string? str)
    {
        if (str is null) return 0;
        return str.GetHashCode();
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetHashCode(char[]? chars)
    {
        return new string(chars).GetHashCode();
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetHashCode(ReadOnlySpan<char> text)
    {
        return text.ToString().GetHashCode();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetHashCode(string? str, StringComparison comparison)
    {
        if (str is null) return 0;
        var comparer = new StringComparisonTextComparers(comparison);
        return comparer.GetHashCode(str);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetHashCode(char[]? chars, StringComparison comparison)
    {
        if (chars is null) return 0;
        var comparer = new StringComparisonTextComparers(comparison);
        return comparer.GetHashCode(chars);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetHashCode(ReadOnlySpan<char> text, StringComparison comparison)
    {
        var comparer = new StringComparisonTextComparers(comparison);
        return comparer.GetHashCode(text);
    }
#endif
#endregion
}