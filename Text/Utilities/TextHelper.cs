using static InlineIL.IL;

// ReSharper disable InvokeAsExtensionMethod
// ^ I want to be sure I'm calling the very specific version of a method

// ReSharper disable EntityNameCapturedOnly.Global

namespace Jay.Text.Utilities;

public static class TextHelper
{
    public const string AsciiDigits = "0123456789";
    public const string AsciiLetterUppers = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public const string AsciiLetterLowers = "abcdefghijklmnopqrstuvwxyz";

    public static readonly string NewLine = Environment.NewLine;
    public static ReadOnlySpan<char> NewLineSpan => NewLine.AsSpan();

    /// <summary>
    /// The offset between an uppercase ascii letter and its lowercase equivalent
    /// </summary>
    internal const int UppercaseOffset = 'a' - 'A';

    /// <summary>
    /// Unsafe / Unchecked Methods -- Nothing here has bounds checks!
    /// </summary>
    internal static class Unsafe
    {
#if NET48 || NETSTANDARD2_0 || NETSTANDARD2_1
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void CopyBlock(char* sourcePtr, ref char destPtr, int charCount)
        {
            Emit.Ldarg(nameof(destPtr));
            Emit.Ldarg(nameof(sourcePtr));
            Emit.Ldarg(nameof(charCount));
            Emit.Sizeof<char>();
            Emit.Mul();
            Emit.Cpblk();
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CopyBlock(in char sourcePtr, ref char destPtr, int charCount)
        {
            Emit.Ldarg(nameof(destPtr));
            Emit.Ldarg(nameof(sourcePtr));
            Emit.Ldarg(nameof(charCount));
            Emit.Sizeof<char>();
            Emit.Mul();
            Emit.Cpblk();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CopyTo(ReadOnlySpan<char> source, Span<char> dest, int sourceLen)
        {
            CopyBlock(
                in source.GetPinnableReference(),
                ref dest.GetPinnableReference(),
                sourceLen);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CopyTo(string source, Span<char> dest, int sourceLen)
        {
#if NET48 || NETSTANDARD2_0 || NETSTANDARD2_1
            unsafe
            {
                fixed (char* ptr = source)
                {
                    CopyBlock(
                        ptr,
                        ref dest.GetPinnableReference(),
                        sourceLen);
                }
            }
#else
            CopyBlock(
                in source.GetPinnableReference(),
                ref dest.GetPinnableReference(),
                sourceLen);
#endif
        }
    }

    public static void CopyTo(ReadOnlySpan<char> source, Span<char> dest)
    {
        if (!TryCopyTo(source, dest))
            throw new ArgumentException($"Destination 'char[{dest.Length}]' cannot contain Source 'char[{source.Length}]'", nameof(dest));
    }

    public static void CopyTo(string? source, Span<char> dest)
    {
        if (!TryCopyTo(source, dest))
            throw new ArgumentException($"Destination 'char[{dest.Length}]' cannot contain Source 'char[{source!.Length}]'", nameof(dest));
    }

    public static bool TryCopyTo(ReadOnlySpan<char> source, Span<char> dest)
    {
        var sourceLen = source.Length;
        if (sourceLen == 0) return true;
        if (sourceLen > dest.Length) return false;
        Unsafe.CopyTo(source, dest, sourceLen);
        return true;
    }

    public static bool TryCopyTo(string? source, Span<char> dest)
    {
        if (source is null) return true;
        var sourceLen = source.Length;
        if (sourceLen == 0) return true;
        if (sourceLen > dest.Length) return false;
        Unsafe.CopyTo(source, dest, sourceLen);
        return true;
    }

    #region Equals

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(ReadOnlySpan<char> x, ReadOnlySpan<char> y)
    {
        return x.SequenceEqual(y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(ReadOnlySpan<char> x, char[]? y)
    {
        return x.SequenceEqual(y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(ReadOnlySpan<char> x, string? y)
    {
        return x.SequenceEqual(y.AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(char[]? x, ReadOnlySpan<char> y)
    {
        return MemoryExtensions.SequenceEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(char[]? x, char[]? y)
    {
        return MemoryExtensions.SequenceEqual<char>(x, y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(char[]? x, string? y)
    {
        return MemoryExtensions.SequenceEqual(x, y.AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(string? x, ReadOnlySpan<char> y)
    {
        return MemoryExtensions.SequenceEqual(x.AsSpan(), y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(string? x, char[]? y)
    {
        return MemoryExtensions.SequenceEqual(x.AsSpan(), y);
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
            if (ch.IsAsciiDigit() || ch.IsAsciiLetterUpper())
            {
                buffer[b++] = ch;
            }
            else if (ch.IsAsciiLetterLower())
            {
                buffer[b++] = (char)(ch - UppercaseOffset);
            }
        }

        return buffer.Slice(0, b).ToString();
    }
}