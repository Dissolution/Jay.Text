namespace Jay.Text;

/// <summary>
/// A utility for Damerau-Levenshtein distance calculation
/// </summary>
public static class Levenshtein
{
    /// <summary>
    /// Computes the distance between two integer arrays, representing character code points.
    /// </summary>
    /// <param name="spanA"></param>
    /// <param name="spanB"></param>
    /// <param name="maximumDistance"></param>
    /// <returns></returns>
    private static int CalculateImpl(ReadOnlySpan<char> spanA, ReadOnlySpan<char> spanB, int maximumDistance)
    {
        int maxA = spanA.Length;
        int maxB = spanB.Length;

        // Return trivial case - difference in string lengths exceeds threshold
        if (Math.Abs(maxA - maxB) > maximumDistance)
            return int.MaxValue;

        int[] dCurrent = new int[maxA + 1];
        int[] dMinus1 = new int[maxA + 1];
        int[] dMinus2 = new int[maxA + 1];

        //Initialize dCurrent
        for (var i = 0; i <= maxA; i++)
        {
            dCurrent[i] = i;
        }

        var jm1 = 0;

        for (var j = 1; j <= maxB; j++)
        {
            // Rotate
            var temp = dMinus2;
            dMinus2 = dMinus1;
            dMinus1 = dCurrent;
            dCurrent = temp;

            // Initialize
            var minDistance = int.MaxValue;
            dCurrent[0] = j;
            var im1 = 0;
            var im2 = -1;

            for (var i = 1; i <= maxA; i++)
            {
                var cost = spanA[im1] == spanB[jm1] ? 0 : 1;

                var del = dCurrent[im1] + 1;
                var ins = dMinus1[i] + 1;
                var sub = dMinus1[im1] + cost;

                //Fastest execution for min value of 3 integers
                var min = (del > ins) ? (ins > sub ? sub : ins) : (del > sub ? sub : del);

                if (i > 1 && j > 1 && spanA[im2] == spanB[jm1] && spanA[im1] == spanB[j - 2])
                    min = Math.Min(min, dMinus2[im2] + cost);

                dCurrent[i] = min;
                if (min < minDistance)
                    minDistance = min;
                im1++;
                im2++;
            }
            jm1++;
            if (minDistance > maximumDistance)
            {
                return int.MaxValue;
            }
        }

        var result = dCurrent[maxA];
        return (result > maximumDistance) ? int.MaxValue : result;
    }

    /// <summary>
    /// Calculate the distance between the two strings.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="maximumDistance"></param>
    /// <returns>Int32.MaxValue if threshold exceeded; otherwise, the distance between the strings.</returns>
    public static int Calculate(string? a, string? b, int maximumDistance = int.MaxValue)
    {
        if (a is null && b is null)
            return 0;
        if (a is null) return b!.Length;
        if (b is null) return a!.Length;

        if (a.Length <= b.Length)
            return CalculateImpl(a.AsSpan(), b.AsSpan(), maximumDistance);
        return CalculateImpl(b.AsSpan(), a.AsSpan(), maximumDistance);
    }

    public static int Calculate(ReadOnlySpan<char> a, ReadOnlySpan<char> b, int maximumDistance = int.MaxValue)
    {
        if (a.Length <= b.Length)
            return CalculateImpl(a, b, maximumDistance);
        return CalculateImpl(b, a, maximumDistance);
    }
}