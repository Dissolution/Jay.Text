using System.Collections;

namespace Jay.Text.Comparision;

public interface ITextComparer :
    IComparer<string?>,
    IComparer<char[]?>,
    IComparer<char>,
    IComparer
{
    int Compare(ReadOnlySpan<char> x, ReadOnlySpan<char> y);
}
