#if NET6_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

namespace Jay.Text.Extensions;

internal static class CompatExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Clamp(this int value, int min)
    {
        if (value >= min)
        {
            return value;
        }
        return min;
    }

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

#if NET48 || NETSTANDARD2_0 || NETSTANDARD2_1
    public static ref readonly char GetPinnableReference(this string str)
    {
        unsafe
        {
            fixed (char* strPtr = str)
            {
                return ref Unsafe.AsRef<char>(strPtr);
            }
        }
    }
#endif

    public static ref T NullRef<T>()
    {
#if !NET6_0_OR_GREATER
        unsafe
        {
            return ref Unsafe.AsRef<T>(UIntPtr.Zero.ToPointer());
        }
#else
        return ref Unsafe.NullRef<T>();
#endif
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(this char[] charArray, char ch)
    {
#if NET6_0_OR_GREATER
        return MemoryExtensions.Contains<char>(charArray, ch);
#else
        for (var i = 0; i < charArray.Length; i++)
        {
            if (charArray[i] == ch) return true;
        }

        return false;
#endif
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref char GetPinnableReference(this char[] charArray)
    {
#if NET6_0_OR_GREATER
        return ref MemoryMarshal.GetArrayDataReference<char>(charArray);
#else
        return ref charArray[0];
#endif
    }
}