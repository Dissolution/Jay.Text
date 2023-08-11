namespace Jay.Text.Extensions;

public static class CharExtensions
{
#if !NET7_0_OR_GREATER
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
    public static bool IsAsciiDigit(this char ch) => (uint)(ch - '0') <= '9' - '0';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiLetterLower(this char ch) => ch is >= 'a' and <= 'z';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiLetterUpper(this char ch) => ch is >= 'A' and <= 'Z';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAscii(this char ch) => (ushort)ch < 128;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiLetter(this char ch) => (ch is >= 'a' and <= 'z') || (ch is >= 'A' and <= 'Z');

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

     [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiLetter(this char ch) => char.IsAsciiLetter(ch);

#endif
}