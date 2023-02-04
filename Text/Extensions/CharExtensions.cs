using System.Runtime.CompilerServices;

namespace Jay.Text.Extensions;

public static class CharExtensions
{
#if NET48 || NETSTANDARD2_0 || NETSTANDARD2_1
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> AsSpan(this in char ch)
    {
        unsafe
        {
            fixed (char* chPtr = &ch)
            {
                return new ReadOnlySpan<char>(chPtr, 1);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiDigit(this char ch) => ch is >= '0' and <= '9';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiLetterLower(this char ch) => ch is >= 'a' and <= 'z';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiLetterUpper(this char ch) => ch is >= 'A' and <= 'Z';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAscii(this char ch) => (ushort)ch < 128;
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> AsSpan(this in char ch)
    {
        return new ReadOnlySpan<char>(in ch);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiDigit(this char ch) => char.IsAsciiDigit(ch);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiLetterLower(this char ch) => char.IsAsciiLetterLower(ch);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiLetterUpper(this char ch) => char.IsAsciiLetterUpper(ch);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAscii(this char ch) => char.IsAscii(ch);
#endif
}