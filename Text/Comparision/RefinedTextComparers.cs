namespace Jay.Text.Comparision;

/// <summary>
/// A utility for simplifying <see cref="string"/>s by stripping all non-ASCII characters, non-digits, non-letters, then uppercasing.
/// </summary>
public sealed class RefinedTextComparers : TextComparers
{
    public static RefinedTextComparers Instance { get; } = new RefinedTextComparers();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryRefine(ref char ch)
    {
        switch (ch)
        {
            case >= '0' and <= '9':
            case >= 'A' and <= 'Z':
                return true;
            case >= 'a' and <= 'z':
                ch = (char)(ch - TextHelper.UppercaseOffset);
                return true;
            default:
                return false;
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryFindNextRefinedChar(ReadOnlySpan<char> text, ref int index, out char refinedChar)
    {
        while (index < text.Length)
        {
            refinedChar = text[index++];
            switch (refinedChar)
            {
                case >= '0' and <= '9':
                case >= 'A' and <= 'Z':
                    return true;
                case >= 'a' and <= 'z':
                    refinedChar = (char)(refinedChar - TextHelper.UppercaseOffset);
                    return true;
            }
        }

        refinedChar = default;
        return false;
    }

    public override bool Equals(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
    {
        int leftIndex = 0;
        int rightIndex = 0;
        while(true)
        {
            bool foundLeft = TryFindNextRefinedChar(left, ref leftIndex, out var leftChar);
            bool foundRight = TryFindNextRefinedChar(right, ref rightIndex, out var rightChar);

            if (!foundLeft)
            {
                if (!foundRight) return true;
                return false;
            }
            if (!foundRight) return false;
            
            // Either ended before the other
            if (foundLeft != foundRight)
            {
                return false;
            }
            
            // Both ended?
            if (!foundLeft && !foundRight)
            {
                return true;
            }
            
            // Are the chars different?
            if (leftChar != rightChar)
            {
                return false;
            }
        } 
    }

    public override int GetHashCode(ReadOnlySpan<char> text)
    {
        var hasher = new HashCode();
        for (var i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (TryRefine(ref c))
            {
                hasher.Add<char>(c);
            }
        }
        return hasher.ToHashCode();
    }

    public override int Compare(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
    {
        int leftIndex = 0;
        int rightIndex = 0;
        int compare;
        do
        {
            bool foundLeft = TryFindNextRefinedChar(left, ref leftIndex, out var leftChar);
            bool foundRight = TryFindNextRefinedChar(right, ref rightIndex, out var rightChar);
            
            if (!foundLeft)
            {
                // Left is shorter, sorts before right
                if (foundRight) return -1;
                // Ended at the same time
                return 0;
            }

            // Right is shorter, sorts before left
            if (!foundRight) return 1;

            // Compare them
            compare = leftChar.CompareTo(rightChar);
            // Only if they are different do we exit
        } while (compare == 0);
        // They are different
        return compare;
    }
}