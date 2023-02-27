using Jay.Text.Utilities;

namespace Jay.Text.Extensions;

public static class SplitExtensions
{
    public static TextSplitEnumerable TextSplit(
        this ReadOnlySpan<char> text,
        ReadOnlySpan<char> separator,
        StringSplitOptions splitOptions = StringSplitOptions.None,
        StringComparison stringComparison = StringComparison.Ordinal)
    {
        return new TextSplitEnumerable(text, separator, splitOptions, stringComparison);
    }

    public static TextSplitEnumerable TextSplit(
        this ReadOnlySpan<char> text,
        string? separator,
        StringSplitOptions splitOptions = StringSplitOptions.None,
        StringComparison stringComparison = StringComparison.Ordinal)
    {
        return TextSplit(text, separator.AsSpan(), splitOptions, stringComparison);
    }

    public static TextSplitEnumerable TextSplit(
        this Span<char> text,
        ReadOnlySpan<char> separator,
        StringSplitOptions splitOptions = StringSplitOptions.None,
        StringComparison stringComparison = StringComparison.Ordinal)
    {
        return TextSplit((ReadOnlySpan<char>)text, separator, splitOptions, stringComparison);
    }

    public static TextSplitEnumerable TextSplit(
        this Span<char> text,
        string? separator,
        StringSplitOptions splitOptions = StringSplitOptions.None,
        StringComparison stringComparison = StringComparison.Ordinal)
    {
        return TextSplit((ReadOnlySpan<char>)text, separator.AsSpan(), splitOptions, stringComparison);
    }

    public static TextSplitEnumerable TextSplit(
        this string? text,
        string? separator,
        StringSplitOptions splitOptions = StringSplitOptions.None,
        StringComparison stringComparison = StringComparison.Ordinal)
    {
        return TextSplit(text.AsSpan(), separator.AsSpan(), splitOptions, stringComparison);
    }
    
    public static TextSplitEnumerable TextSplit(
        this string? text,
        ReadOnlySpan<char> separator,
        StringSplitOptions splitOptions = StringSplitOptions.None,
        StringComparison stringComparison = StringComparison.Ordinal)
    {
        return TextSplit(text.AsSpan(), separator, splitOptions, stringComparison);
    }



    public static RangeSplitEnumerable RangeSplit(
        this ReadOnlySpan<char> text,
        ReadOnlySpan<char> separator,
        StringSplitOptions splitOptions = StringSplitOptions.None,
        StringComparison stringComparison = StringComparison.Ordinal)
    {
        return new RangeSplitEnumerable(text, separator, splitOptions, stringComparison);
    }

    public static RangeSplitEnumerable RangeSplit(
        this ReadOnlySpan<char> text,
        string? separator,
        StringSplitOptions splitOptions = StringSplitOptions.None,
        StringComparison stringComparison = StringComparison.Ordinal)
    {
        return RangeSplit(text, separator.AsSpan(), splitOptions, stringComparison);
    }

    public static RangeSplitEnumerable RangeSplit(
        this Span<char> text,
        ReadOnlySpan<char> separator,
        StringSplitOptions splitOptions = StringSplitOptions.None,
        StringComparison stringComparison = StringComparison.Ordinal)
    {
        return RangeSplit((ReadOnlySpan<char>)text, separator, splitOptions, stringComparison);
    }

    public static RangeSplitEnumerable RangeSplit(
        this Span<char> text,
        string? separator,
        StringSplitOptions splitOptions = StringSplitOptions.None,
        StringComparison stringComparison = StringComparison.Ordinal)
    {
        return RangeSplit((ReadOnlySpan<char>)text, separator.AsSpan(), splitOptions, stringComparison);
    }

    public static RangeSplitEnumerable RangeSplit(
        this string? text,
        string? separator,
        StringSplitOptions splitOptions = StringSplitOptions.None,
        StringComparison stringComparison = StringComparison.Ordinal)
    {
        return RangeSplit(text.AsSpan(), separator.AsSpan(), splitOptions, stringComparison);
    }
    public static RangeSplitEnumerable RangeSplit(
        this string? text,
        ReadOnlySpan<char> separator,
        StringSplitOptions splitOptions = StringSplitOptions.None,
        StringComparison stringComparison = StringComparison.Ordinal)
    {
        return RangeSplit(text.AsSpan(), separator, splitOptions, stringComparison);
    }
}