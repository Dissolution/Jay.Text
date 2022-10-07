using Jay.Text.Extensions;

namespace Jay.Text.Comparision;

public interface ITextComparer : IComparer<string?>,
                                 IComparer<char[]?>,
                                 IComparer<char>,
                                 IComparer
{
    int IComparer<string?>.Compare(string? x, string? y)
    {
        return Compare(x.AsSpan(), y.AsSpan());
    }

    int IComparer<char[]?>.Compare(char[]? x, char[]? y)
    {
        return Compare(new Span<char>(x), new Span<char>(y));
    }

    int IComparer<char>.Compare(char x, char y)
    {
        return Compare(x.AsReadOnlySpan(), y.AsReadOnlySpan());
    }
    
    int IComparer.Compare(object? x, object? y)
    {
        if (x is char xChar)
        {
            if (y is char yChar)
            {
                return Compare(xChar, yChar);
            }
    
            return 0;
        }
        else if (x is char[] xChars)
        {
            if (y is char[] yChars)
            {
                return Compare(xChars, yChars);
            }

            return 0;
        }
        else
        {
            return 0;
        }
    }

    int Compare(ReadOnlySpan<char> left, ReadOnlySpan<char> right);
}