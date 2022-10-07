using System.Globalization;

// ReSharper disable EntityNameCapturedOnly.Global

namespace Jay.Text;

public static class TextHelper
{
    /// <summary>
    /// The offset between a lowercase ASCII a-z letter and its uppercase A-Z partner
    /// </summary>
    internal const int UppercaseOffset = 'a' - 'A';

    /// <summary>
    /// The numbers 0 through 9
    /// </summary>
    public const string Digits = "0123456789";
    /// <summary>
    /// The uppercase letters, A through Z
    /// </summary>
    public const string UppercaseLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    /// <summary>
    /// The lowercase letters, a through z
    /// </summary>
    public const string LowercaseLetters = "abcdefghijklmnopqrstuvwxyz";

    /// <summary>
    /// Unsafe methods that operate on text with no validation
    /// </summary>
    public unsafe static class Unsafe
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(char* sourcePtr, char* destPtr, int charCount)
        {
            Emit.Ldarg(nameof(destPtr));
            Emit.Ldarg(nameof(sourcePtr));
            Emit.Ldarg(nameof(charCount));
            Emit.Sizeof<char>();
            Emit.Mul();
            Emit.Cpblk();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(in char firstChar, ref char destChar, int charCount)
        {
            Emit.Ldarg(nameof(destChar));
            Emit.Ldarg(nameof(firstChar));
            Emit.Ldarg(nameof(charCount));
            Emit.Sizeof<char>();
            Emit.Mul();
            Emit.Cpblk();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(ReadOnlySpan<char> source, Span<char> dest)
        {
            Copy(in source.GetPinnableReference(),
                ref dest.GetPinnableReference(),
                source.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(ReadOnlySpan<char> source, char[] dest)
        {
            Copy(in source.GetPinnableReference(),
                ref MemoryMarshal.GetArrayDataReference<char>(dest),
                source.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(char[] source, Span<char> dest)
        {
            Copy(in MemoryMarshal.GetArrayDataReference<char>(source),
                ref dest.GetPinnableReference(),
                source.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(char[] source, char[] dest)
        {
            Copy(in MemoryMarshal.GetArrayDataReference<char>(source),
                ref MemoryMarshal.GetArrayDataReference<char>(dest),
                source.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(string source, Span<char> dest)
        {
            Copy(in source.GetPinnableReference(),
                ref dest.GetPinnableReference(),
                source.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(string source, char[] dest)
        {
            Copy(in source.GetPinnableReference(),
                ref MemoryMarshal.GetArrayDataReference<char>(dest),
                source.Length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryCopyTo(ReadOnlySpan<char> source, Span<char> dest)
    {
        if (dest.Length < source.Length) return false;
        Unsafe.Copy(in source.GetPinnableReference(),
            ref dest.GetPinnableReference(),
            source.Length);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryCopyTo(ReadOnlySpan<char> source, char[]? dest)
    {
        if (dest is null) return source.Length == 0;
        if (dest.Length < source.Length) return false;
        Unsafe.Copy(in source.GetPinnableReference(),
            ref MemoryMarshal.GetArrayDataReference<char>(dest),
            source.Length);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryCopyTo(char[]? source, Span<char> dest)
    {
        if (source is null || source.Length == 0) return true;
        if (dest.Length < source.Length) return false;
        Unsafe.Copy(in MemoryMarshal.GetArrayDataReference<char>(source),
            ref dest.GetPinnableReference(),
            source.Length);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryCopyTo(char[]? source, char[]? dest)
    {
        if (source is null || source.Length == 0) return true;
        if (dest is null) return false;
        if (dest.Length < source.Length) return false;
        Unsafe.Copy(in MemoryMarshal.GetArrayDataReference<char>(source),
            ref MemoryMarshal.GetArrayDataReference<char>(dest),
            source.Length);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryCopyTo(string? source, Span<char> dest)
    {
        if (string.IsNullOrEmpty(source)) return true;
        if (dest.Length < source.Length) return false;
        Unsafe.Copy(in source.GetPinnableReference(),
            ref dest.GetPinnableReference(),
            source.Length);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryCopyTo(string? source, char[]? dest)
    {
        if (string.IsNullOrEmpty(source)) return true;
        if (dest is null) return false;
        if (dest.Length < source.Length) return false;
        Unsafe.Copy(in source.GetPinnableReference(),
            ref MemoryMarshal.GetArrayDataReference<char>(dest),
            source.Length);
        return true;
    }
    
    #region Equals
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(ReadOnlySpan<char> x, ReadOnlySpan<char> y)
    {
        return MemoryExtensions.SequenceEqual<char>(x, y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(ReadOnlySpan<char> x, char[]? y)
    {
        return MemoryExtensions.SequenceEqual<char>(x, y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(ReadOnlySpan<char> x, string? y)
    {
        return MemoryExtensions.SequenceEqual<char>(x, y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(char[]? x, ReadOnlySpan<char> y)
    {
        return MemoryExtensions.SequenceEqual<char>(x, y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(char[]? x, char[]? y)
    {
        return MemoryExtensions.SequenceEqual<char>(x, y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(char[]? x, string? y)
    {
        return MemoryExtensions.SequenceEqual<char>(x, y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(string? x, ReadOnlySpan<char> y)
    {
        return MemoryExtensions.SequenceEqual<char>(x, y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(string? x, char[]? y)
    {
        return MemoryExtensions.SequenceEqual<char>(x, y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(string? x, string? y)
    {
        return string.Equals(x, y);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(ReadOnlySpan<char> x, ReadOnlySpan<char> y, StringComparison comparison)
    {
        return MemoryExtensions.Equals(x, y, comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(ReadOnlySpan<char> x, char[]? y, StringComparison comparison)
    {
        return MemoryExtensions.Equals(x, y, comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(ReadOnlySpan<char> x, string? y, StringComparison comparison)
    {
        return MemoryExtensions.Equals(x, y, comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(char[]? x, ReadOnlySpan<char> y, StringComparison comparison)
    {
        return MemoryExtensions.Equals(x, y, comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(char[]? x, char[]? y, StringComparison comparison)
    {
        return MemoryExtensions.Equals(x, y, comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(char[]? x, string? y, StringComparison comparison)
    {
        return MemoryExtensions.Equals(x, y, comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(string? x, ReadOnlySpan<char> y, StringComparison comparison)
    {
        return MemoryExtensions.Equals(x, y, comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(string? x, char[]? y, StringComparison comparison)
    {
        return MemoryExtensions.Equals(x, y, comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(string? x, string? y, StringComparison comparison)
    {
        return string.Equals(x, y, comparison);
    }
    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiDigit(char c) => c is <= '9' and >= '0';
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiLower(char c) => c is <= 'z' and >= 'a';
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiUpper(char c) => c is <= 'Z' and >= 'A';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAscii(char c) => c <= 127;
    
    /// <summary>
    /// Transforms the specified characters into UppercaseLetters, using char.ToUpper(c)
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static string ToUppercaseString(ReadOnlySpan<char> text)
    {
        Span<char> buffer = stackalloc char[text.Length];
        for (var i = text.Length - 1; i >= 0; i--)
        {
            buffer[i] = char.ToUpper(text[i]);
        }
        return new string(buffer);
    }

    /// <summary>
    /// Transforms the specified characters into UppercaseLetters, using char.ToLower(c)
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static string ToLowercaseString(ReadOnlySpan<char> text)
    {
        Span<char> buffer = stackalloc char[text.Length];
        for (var i = text.Length - 1; i >= 0; i--)
        {
            buffer[i] = char.ToLower(text[i]);
        }
        return new string(buffer);
    }
    
    public static string ToTitleCaseString(ReadOnlySpan<char> text)
    {
        var str = new string(text);
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str);
    }
    
    [return: NotNullIfNotNull("text")]
    public static string? ToTitleCaseString(string? text)
    {
        if (text is null) return null;
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text);
    }

    /// <summary>
    /// Reverses the order of the specified characters.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static string ToReverseString(ReadOnlySpan<char> text)
    {
        int end = text.Length - 1;
        Span<char> buffer = stackalloc char[text.Length];
        for (var i = 0; i <= end; i++)
        {
            buffer[i] = text[end - i];
        }
        return new string(buffer);
    }

    public static string Refine(string? text) => Refine(text.AsSpan());

    public static string Refine(params char[]? chars) => Refine(chars.AsSpan());

    public static string Refine(ReadOnlySpan<char> text)
    {
        Span<char> buffer = stackalloc char[text.Length];
        int b = 0;
        char ch;
        for (var i = 0; i < text.Length; i++)
        {
            ch = text[i];
            if (ch is >= '0' and <= '9' || ch is >= 'A' and <= 'Z')
            {
                buffer[b++] = ch;
            }
            else if (ch is >= 'a' and <= 'z')
            {
                buffer[b++] = (char)(ch - UppercaseOffset);
            }
        }
        return new string(buffer[..b]);
    }
    
    // #region Split
    // public static IEnumerable<string> Split(ReadOnlySpan<char> text, char delimiter)
    // {
    //     int start = 0;
    //     for (var i = 0; i < text.Length; i++)
    //     {
    //         if (text[i] == delimiter)
    //         {
    //             yield return new string(text[start..i]);
    //             start = i + 1;
    //         }
    //     }
    //     // Remaining
    //     yield return new string(text[start..]);
    // }
    // #endregion
}