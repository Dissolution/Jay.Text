// ReSharper disable InvokeAsExtensionMethod
// ^ I want to be sure I'm calling the very specific version of a method

using Jay.Text.Building;
using Jay.Text.Comparision;

namespace Jay.Text.Utilities;

public static partial class TextHelper
{
    public static FastTextComparers Comparers { get; } = new FastTextComparers();

    public static string Interpolate(this ref InterpolatedTextBuilder text)
    {
        return text.ToStringAndDispose();
    }
    
    public static void CopyTo(ReadOnlySpan<char> source, Span<char> dest)
    {
        if (!TryCopyTo(source, dest))
        {
            throw new InvalidOperationException(
#if NET6_0_OR_GREATER
                $"Cannot copy source '{source}' (char[{source.Length}] to dest char[{dest.Length}]");
#else
                $"Cannot copy source '{source.ToString()}' (char[{source.Length}] to dest char[{dest.Length}]");
#endif
        }
    }

    public static void CopyTo(string? source, Span<char> dest)
    {
        if (!TryCopyTo(source, dest))
        {
            throw new InvalidOperationException(
                $"Cannot copy source '{source}' (char[{source?.Length}] to dest char[{dest.Length}]");
        }
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
}