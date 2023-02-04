

using System.Runtime.CompilerServices;

namespace Jay.Text.Compat;

internal static class CompatExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Clamp(this int value, int min, int max)
    {
#if NET48 || NETSTANDARD2_0
        if (min > max)
        {
            throw new ArgumentException("Max must not be greater than min", nameof(max));
        }

        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
#else
        return Math.Clamp(value, min, max);
#endif
    }

#if NET48 || NETSTANDARD2_0
    public static Span<char> AsSpan(this char[]? array, Range range)
    {
        (int offset, int length) = range.GetOffsetAndLength(array?.Length ?? 0);
        return array.AsSpan(offset, length);
    }
#endif
}



