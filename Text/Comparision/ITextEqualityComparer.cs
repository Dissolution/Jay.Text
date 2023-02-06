using System.Collections;

namespace Jay.Text.Comparision;

public interface ITextEqualityComparer : 
    IEqualityComparer<string?>,
    IEqualityComparer<char[]>,
    IEqualityComparer<char>,
    IEqualityComparer
{
    bool Equals(ReadOnlySpan<char> x, ReadOnlySpan<char> y);
    int GetHashCode(ReadOnlySpan<char> span);
}