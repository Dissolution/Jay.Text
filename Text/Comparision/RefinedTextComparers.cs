#if NET6_0_OR_GREATER
using System.Runtime.CompilerServices;

namespace Jay.Text.Comparision;

/// <summary>
/// A utility for simplifying <see cref="string"/>s by stripping all non-ASCII characters, non-digits, and non-letters, then uppercasing.
/// </summary>
public sealed class RefinedTextComparers : TextComparers
{
    public static RefinedTextComparers Instance { get; } = new RefinedTextComparers();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryRefine(ref char ch)
    {
        if (char.IsDigit(ch) || char.IsAsciiLetterUpper(ch))
            return true;
        if (char.IsAsciiLetterLower(ch))
        {
            ch = (char)(ch - TextHelper.UppercaseOffset);
            return true;
        }

        return false;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryFindNextRefinedChar(ReadOnlySpan<char> text, ref int index, out char refinedChar)
    {
        while (index < text.Length)
        {
            refinedChar = text[index++];

            if (char.IsDigit(refinedChar) || char.IsAsciiLetterUpper(refinedChar))
                return true;

            if (char.IsAsciiLetterLower(refinedChar))
            {
                refinedChar = (char)(refinedChar - TextHelper.UppercaseOffset);
                return true;
            }
        }

        refinedChar = default;
        return false;
    }

    public override bool Equals(ReadOnlySpan<char> xText, ReadOnlySpan<char> yText)
    {
        int x = 0;
        int y = 0;
        while (TryFindNextRefinedChar(xText, ref x, out var xCh) &&
            TryFindNextRefinedChar(yText, ref y, out var yCh))
        {
            if (xCh != yCh) return false;
        }

        //Both ended at the same time, so they're equal
        return true;
    }

    public int GetHashCode(char ch)
    {
        char c = ch;
        if (TryRefine(ref c))
            return (int)c;
        return 0;
    }

    public override int GetHashCode(ReadOnlySpan<char> text)
    {
        var hasher = new HashCode();
        for (var i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (TryRefine(ref c))
            {
                hasher.Add(c);
            }
        }

        return hasher.ToHashCode();
    }

    public override int Compare(ReadOnlySpan<char> xText, ReadOnlySpan<char> yText)
    {
        int x = 0;
        int y = 0;
        while (TryFindNextRefinedChar(xText, ref x, out var xCh) &&
            TryFindNextRefinedChar(yText, ref y, out var yCh))
        {
            var c = xCh.CompareTo(yCh);
            if (c != 0)
                return c;
        }

        //Both ended at the same time, so they're equal
        return 0;
    }
}
#endif