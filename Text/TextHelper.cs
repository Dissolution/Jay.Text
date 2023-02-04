using System.Runtime.CompilerServices;
using InlineIL;
using Jay.Text.Extensions;

// ReSharper disable InvokeAsExtensionMethod
// ^ I want to be sure I'm calling the very specific version of a method

// ReSharper disable EntityNameCapturedOnly.Global

namespace Jay.Text;

public static class TextHelper
{
    public const string Digits = "0123456789";
    public const string UppercaseLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public const string LowercaseLetters = "abcdefghijklmnopqrstuvwxyz";

    /// <summary>
    /// The offset between an uppercase ascii letter and its lowercase equivalent
    /// </summary>
    internal const int UppercaseOffset = 'a' - 'A';

    /// <summary>
    /// Unsafe / Unchecked Methods -- Nothing here has bounds checks!
    /// </summary>
    internal static class Unsafe
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void CopyBlock(char* sourcePtr, ref char destPtr, int charCount)
        {
            IL.Emit.Ldarg(nameof(destPtr));
            IL.Emit.Ldarg(nameof(sourcePtr));
            IL.Emit.Ldarg(nameof(charCount));
            IL.Emit.Sizeof<char>();
            IL.Emit.Mul();
            IL.Emit.Cpblk();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CopyBlock(in char sourcePtr, ref char destPtr, int charCount)
        {
            IL.Emit.Ldarg(nameof(destPtr));
            IL.Emit.Ldarg(nameof(sourcePtr));
            IL.Emit.Ldarg(nameof(charCount));
            IL.Emit.Sizeof<char>();
            IL.Emit.Mul();
            IL.Emit.Cpblk();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CopyTo(ReadOnlySpan<char> source, Span<char> dest)
        {
            CopyBlock(
                in source.GetPinnableReference(),
                ref dest.GetPinnableReference(),
                source.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CopyTo(ReadOnlySpan<char> source, char[] dest)
        {
            CopyBlock(
                in source.GetPinnableReference(),
                ref dest.GetPinnableReference(),
                source.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CopyTo(char[] source, Span<char> dest)
        {
            CopyBlock(
                in source.GetPinnableReference(),
                ref dest.GetPinnableReference(),
                source.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CopyTo(char[] source, char[] dest)
        {
            CopyBlock(in source.GetPinnableReference(),
                ref dest.GetPinnableReference(),
                source.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CopyTo(string source, Span<char> dest)
        {
#if NET48 || NETSTANDARD2_0 || NETSTANDARD2_1
            unsafe
            {
                fixed (char* ptr = source)
                {
                    CopyBlock(
                        ptr,
                        ref dest.GetPinnableReference(),
                        source.Length);
                }
            }
#else
            CopyBlock(
                in source.GetPinnableReference(),
                ref dest.GetPinnableReference(),
                source.Length);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CopyTo(string source, char[] dest)
        {
#if NET48 || NETSTANDARD2_0 || NETSTANDARD2_1
            unsafe
            {
                fixed (char* ptr = source)
                {
                    CopyBlock(
                        ptr,
                        ref dest.GetPinnableReference(),
                        source.Length);
                }
            }
#else
            CopyBlock(
                in source.GetPinnableReference(),
                ref dest.GetPinnableReference(),
                source.Length);
#endif
        }
    }

    public static void CopyTo(ReadOnlySpan<char> source, Span<char> dest)
    {
        var len = source.Length;
        if (len == 0) return;
        if (len > dest.Length)
            throw new ArgumentException("Destination cannot contain Source", nameof(dest));
        Unsafe.CopyBlock(
            in source.GetPinnableReference(),
            ref dest.GetPinnableReference(),
            len);
    }

    public static void CopyTo(string? source, Span<char> dest)
    {
        if (source is null) return;
        var len = source.Length;
        if (len == 0) return;
        if (len > dest.Length)
            throw new ArgumentException("Destination cannot contain Source", nameof(dest));
        unsafe
        {
            fixed (char* sourcePtr = source)
            {
                Unsafe.CopyBlock(
                    sourcePtr,
                    ref dest.GetPinnableReference(),
                    len);
            }
        }
    }

    public static bool TryCopyTo(ReadOnlySpan<char> source, Span<char> dest)
    {
        var len = source.Length;
        if (len == 0) return true;
        if (len > dest.Length) return false;
        Unsafe.CopyBlock(
            in source.GetPinnableReference(),
            ref dest.GetPinnableReference(),
            len);
        return true;
    }

    public static bool TryCopyTo(string? source, Span<char> dest)
    {
        if (source is null) return true;
        var len = source.Length;
        if (len == 0) return true;
        if (len > dest.Length) return false;
        unsafe
        {
            fixed (char* sourcePtr = source)
            {
                Unsafe.CopyBlock(
                    sourcePtr,
                    ref dest.GetPinnableReference(),
                    len);
            }
        }

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
        return MemoryExtensions.SequenceEqual<char>(x, y.AsSpan());
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
        return MemoryExtensions.SequenceEqual<char>(x, y.AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(string? x, ReadOnlySpan<char> y)
    {
        return MemoryExtensions.SequenceEqual<char>(x.AsSpan(), y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(string? x, char[]? y)
    {
        return MemoryExtensions.SequenceEqual<char>(x.AsSpan(), y);
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
        return MemoryExtensions.Equals(x, y.AsSpan(), comparison);
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
        return MemoryExtensions.Equals(x, y.AsSpan(), comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(string? x, ReadOnlySpan<char> y, StringComparison comparison)
    {
        return MemoryExtensions.Equals(x.AsSpan(), y, comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(string? x, char[]? y, StringComparison comparison)
    {
        return MemoryExtensions.Equals(x.AsSpan(), y, comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(string? x, string? y, StringComparison comparison)
    {
        return string.Equals(x, y, comparison);
    }

    #endregion


    public static string AsString(this ReadOnlySpan<char> text)
    {
#if NET48 || NETSTANDARD2_0
        unsafe
        {
            fixed (char* textPtr = text)
            {
                return new string(textPtr, 0, text.Length);
            }
        }
#else
        return new string(text);
#endif
    }

    public static string AsString(this Span<char> text)
    {
#if NET48 || NETSTANDARD2_0
        unsafe
        {
            fixed (char* textPtr = text)
            {
                return new string(textPtr, 0, text.Length);
            }
        }
#else
        return new string(text);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string[] Split(string? text, string separator, StringSplitOptions options = StringSplitOptions.None)
    {
        if (text is null) return Array.Empty<string>();
        return text.Split(new string[1] { separator }, options);
    }

    public static List<(int start, int length)> SplitLines(this ReadOnlySpan<char> text)
    {
        var ranges = new List<(int, int)>();
        ReadOnlySpan<char> sep = Environment.NewLine.AsSpan();
        int start = 0;
        int index = 0;
        int len = text.Length;
        while (index < len)
        {
            if (text.StartsWith(sep))
            {
                int end = index;
                if (end - start > 0)
                {
                    ranges.Add((start, end - start));
                }

                start = index + sep.Length;
                index = start;
            }
            else
            {
                index++;
            }
        }

        if (index - start > 0)
        {
            ranges.Add((start, index - start));
        }

        return ranges;
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
#if NET48 || NETSTANDARD2_0 || NETSTANDARD2_1
            if ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z'))
            {
                buffer[b++] = ch;
            }
            else if (ch >= 'a' && ch <= 'z')
            {
                buffer[b++] = (char)(ch - UppercaseOffset);
            }
#else
            if (char.IsAsciiDigit(ch) || char.IsAsciiLetterUpper(ch))
            {
                buffer[b++] = ch;
            }
            else if (char.IsAsciiLetterLower(ch))
            {
                buffer[b++] = (char)(ch - UppercaseOffset);
            }
#endif
        }

#if NET48 || NETSTANDARD2_0 || NETSTANDARD2_1
        unsafe
        {
            fixed (char* bufferPtr = buffer)
            {
                return new string(bufferPtr, 0, b);
            }
        }

#else
        return new string(buffer.Slice(0, b));
#endif
    }
}