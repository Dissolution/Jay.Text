namespace Jay.Text.Comparision;

/// <inheritdoc />
/// <summary>
/// Compares two strings by using logical numeric and character order.
/// </summary>
public class AlphanumericTextComparer : ITextComparer
{
    private readonly StringComparison _stringComparison;
    private readonly TextOrder _textOrder;

    public AlphanumericTextComparer(StringComparison stringComparison = StringComparison.CurrentCulture,
                                    TextOrder textOrder = TextOrder.FrontToBack)
    {
        _stringComparison = stringComparison;
        _textOrder = textOrder;
    }

    private static int FrontToBlackCompare(ReadOnlySpan<char> left,
                                           ReadOnlySpan<char> right,
                                           StringComparison comparison)
    {
        int l = 0;
        int leftLength = left.Length; 

        int r = 0;
        int rightLength = right.Length;

        int result = 0;

        while (l < leftLength || r < rightLength)
        {
            if (l >= leftLength) return -1; // Left ran out, right is longer
            if (r >= rightLength) return 1;  // Right ran out, left is longer

            // Get left chunk (grouped based on Digits / NonDigits
            bool leftChunkDigit = char.IsDigit(left[l]);
            int leftChunkStart = l;
            do
            {
                l++;
            } while (l < leftLength && char.IsDigit(left[l]) == leftChunkDigit);
            ReadOnlySpan<char> leftChunk = left.Slice(leftChunkStart, l - leftChunkStart);

            // Get right chunk (grouped based on Digits / NonDigits
            bool rightChunkDigit = char.IsDigit(right[r]);
            int rightChunkStart = r;
            do
            {
                r++;
            } while (r < rightLength && char.IsDigit(right[r]) == rightChunkDigit);
            ReadOnlySpan<char> rightChunk = right.Slice(rightChunkStart, r - rightChunkStart);

            // If they are both digit chunks, compare them
            if (leftChunkDigit && rightChunkDigit &&
                int.TryParse(leftChunk, out int leftChunkValue) &&
                int.TryParse(rightChunk, out int rightChunkValue))
            {
                if (leftChunkValue < rightChunkValue)
                    result = -1;
                else if (leftChunkValue > rightChunkValue)
                    result = 1;
            }
            // Otherwise, use our string comparer
            else
            {
                result = leftChunk.CompareTo(rightChunk, comparison);
            }

            // Did we find a comparison?
            if (result != 0)
                return result;
        }

        // They are the same
        return 0;
    }

    private static int BackToFrontCompare(ReadOnlySpan<char> left,
                                          ReadOnlySpan<char> right,
                                          StringComparison comparison)
    {
        int l = left.Length - 1;

        int r = right.Length - 1;

        int result = 0;

        while (l >= 0 || r >= 0)
        {
            if (l < 0) return -1; // Left ran out, right is longer
            if (r < 0) return 1; // Right ran out, left is longer

            // Get left chunk (grouped based on Digits / NonDigits
            bool leftChunkDigit = char.IsDigit(left[l]);
            int leftChunkEnd = l + 1;
            do
            {
                l--;
            } while (l >= 0 && char.IsDigit(left[l]) == leftChunkDigit);
            ReadOnlySpan<char> leftChunk = left[(l + 1)..leftChunkEnd];

            // Get right chunk (grouped based on Digits / NonDigits
            bool rightChunkDigit = char.IsDigit(right[r]);
            int rightChunkEnd = r + 1;
            do
            {
                r--;
            } while (r >= 0 && char.IsDigit(right[r]) == rightChunkDigit);
            ReadOnlySpan<char> rightChunk = right[(r + 1)..rightChunkEnd];

            // If they are both digit chunks, compare them
            if (leftChunkDigit && rightChunkDigit &&
                int.TryParse(leftChunk, out int leftChunkValue) &&
                int.TryParse(rightChunk, out int rightChunkValue))
            {
                if (leftChunkValue < rightChunkValue)
                    result = -1;
                else if (leftChunkValue > rightChunkValue)
                    result = 1;
            }
            // Otherwise, use our string comparer
            else
            {
                result = leftChunk.CompareTo(rightChunk, comparison);
            }

            // Did we find a comparison?
            if (result != 0)
                return result;
        }

        // They are the same
        return 0;
    }

    public int Compare(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
    {
        if (_textOrder == TextOrder.FrontToBack)
            return FrontToBlackCompare(left, right, _stringComparison);
        return BackToFrontCompare(left, right, _stringComparison);
    }

    public int Compare(string? x, string? y)
    {
        if (_textOrder == TextOrder.FrontToBack)
            return FrontToBlackCompare(x, y, _stringComparison);
        return BackToFrontCompare(x, y, _stringComparison);
    }

    public int Compare(char[]? x, char[]? y)
    {
        if (_textOrder == TextOrder.FrontToBack)
            return FrontToBlackCompare(x, y, _stringComparison);
        return BackToFrontCompare(x, y, _stringComparison);
    }

    int IComparer.Compare(object? x, object? y)
    {
        ReadOnlySpan<char> left = x switch
        {
            string str => str,
            char[] chars => chars,
            _ => x?.ToString()
        };
        ReadOnlySpan<char> right = y switch
        {
            string str => str,
            char[] chars => chars,
            _ => x?.ToString()
        };
        return Compare(left, right);
    }
}