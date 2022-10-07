using Jay.Text.Extensions;

namespace Jay.Text.Comparision;

public interface ITextEqualityComparer : IEqualityComparer<char>,
                                         IEqualityComparer<char[]>,
                                         //IEqualityComparer<ReadOnlySpan<char>>,
                                         IEqualityComparer<string?>,
                                         IEqualityComparer
{
    bool IEqualityComparer<string?>.Equals(string? x, string? y) => Equals(x.AsSpan(), y.AsSpan());
    
    bool IEqualityComparer<char[]>.Equals(char[]? x, char[]? y) => Equals(new Span<char>(x), new Span<char>(y));

    bool IEqualityComparer<char>.Equals(char x, char y) => Equals(x.AsReadOnlySpan(), y.AsReadOnlySpan());

    bool IEqualityComparer.Equals(object? x, object? y)
    {
        if (x is char xChar)
        {
            if (y is char yChar)
            {
                return Equals(xChar, yChar);
            }
            
            // Different types
            return false;
        }
        if (x is char[] xChars)
        {
            if (y is char[] yChars)
            {
                return Equals(xChars, yChars);
            }
            // Different types
            return false;
        }
        // Different types
        return false;
    }

    bool Equals(ReadOnlySpan<char> left, ReadOnlySpan<char> right);

    int IEqualityComparer<string?>.GetHashCode(string? str) => GetHashCode(str.AsSpan());

    int IEqualityComparer<char[]>.GetHashCode(char[]? charArray) => GetHashCode(new Span<char>(charArray));

    int IEqualityComparer<char>.GetHashCode(char ch) => GetHashCode(ch.AsReadOnlySpan());

    int IEqualityComparer.GetHashCode(object? obj)
    {
        if (obj is char ch)
            return GetHashCode(ch);
        if (obj is char[] chars)
            return GetHashCode(chars);
        if (obj is string str)
            return GetHashCode(str);
        return 0;
    }

    int GetHashCode(ReadOnlySpan<char> span);
}