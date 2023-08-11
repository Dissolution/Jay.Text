using System.Globalization;

namespace Jay.Text.Extensions;

public enum Casing
{
    Lower,
    Upper,
    Camel,
    Pascal,
    Title,
}

public static class CasingExtensions
{
    public static string ToCasedString(this string? text, Casing casing, CultureInfo? culture = null)
    {
        if (text is null) return string.Empty;
        int textLen = text.Length;
        if (textLen == 0) return string.Empty;
        TextInfo textInfo = (culture ?? CultureInfo.CurrentCulture).TextInfo;
        switch (casing)
        {
            case Casing.Lower:
            {
                return textInfo.ToLower(text);
            }
            case Casing.Upper:
            {
                return textInfo.ToUpper(text);
            }
            case Casing.Camel:
            {
                Span<char> buffer = stackalloc char[textLen];
                buffer[0] = textInfo.ToLower(text[0]);
                TextHelper.Unsafe.CopyTo(text.AsSpan(1), buffer.Slice(1), textLen - 1);
                return buffer.ToString();
            }
            case Casing.Pascal:
            {
                Span<char> buffer = stackalloc char[textLen];
                buffer[0] = textInfo.ToUpper(text[0]);
                TextHelper.Unsafe.CopyTo(text.AsSpan(1), buffer.Slice(1), textLen - 1);
                return buffer.ToString();
            }
            case Casing.Title:
            {
                return textInfo.ToTitleCase(text);
            }
            default:
                return text;
        }
    }

    public static string ToCasedString(
        this ReadOnlySpan<char> text,
        Casing casing,
        CultureInfo? culture = null
    )
    {
        int textLen = text.Length;
        if (textLen == 0)
            return string.Empty;
        TextInfo textInfo = (culture ?? CultureInfo.CurrentCulture).TextInfo;
        switch (casing)
        {
            case Casing.Lower:
            {
                Span<char> buffer = stackalloc char[textLen];
                for (var i = textLen - 1; i >= 0; i--)
                {
                    buffer[i] = textInfo.ToLower(text[i]);
                }
                return buffer.ToString();
            }
            case Casing.Upper:
            {
                Span<char> buffer = stackalloc char[textLen];
                for (var i = textLen - 1; i >= 0; i--)
                {
                    buffer[i] = textInfo.ToUpper(text[i]);
                }
                return buffer.ToString();
            }
            case Casing.Camel:
            {
                Span<char> buffer = stackalloc char[textLen];
                buffer[0] = textInfo.ToLower(text[0]);
                TextHelper.Unsafe.CopyTo(text.Slice(1), buffer.Slice(1), textLen - 1);
                return buffer.ToString();
            }
            case Casing.Pascal:
            {
                Span<char> buffer = stackalloc char[textLen];
                buffer[0] = textInfo.ToUpper(text[0]);
                TextHelper.Unsafe.CopyTo(text.Slice(1), buffer.Slice(1), textLen - 1);
                return buffer.ToString();
            }
            case Casing.Title:
            {
                // Have to allocate a string
                return textInfo.ToTitleCase(text.ToString());
            }
            default:
                return text.ToString();
        }
    }
}