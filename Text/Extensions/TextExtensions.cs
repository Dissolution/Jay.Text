using System.Globalization;

namespace Jay.Text.Extensions;

public enum Casing
{
    Lower,
    Upper,
    Camel,
    Pascal,
    Title,
    Field,
}

public static class TextExtensions
{
    public static string ToCasedString(this string? text, Casing casing)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        if (casing == Casing.Title)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text);
        }
        return ToCasedString(text.AsSpan(), casing);
    }

    public static string ToCasedString(this ReadOnlySpan<char> text, Casing casing)
    {
        int textLen = text.Length;
        if (textLen == 0) return string.Empty;
        switch (casing)
        {
            case Casing.Lower:
            {
                Span<char> buffer = stackalloc char[textLen];
                for (var i = textLen - 1; i >= 0; i--)
                {
                    buffer[i] = char.ToLower(text[i]);
                }

                return buffer.AsString();
            }
            case Casing.Upper:
            {
                Span<char> buffer = stackalloc char[textLen];
                for (var i = textLen - 1; i >= 0; i--)
                {
                    buffer[i] = char.ToUpper(text[i]);
                }

                return buffer.AsString();
            }
            case Casing.Camel:
            {
                Span<char> buffer = stackalloc char[textLen];
                buffer[0] = char.ToLower(text[0]);
#if net48
                text.Slice(1).CopyTo(buffer.Slice(1));
#else
                TextHelper.Unsafe.CopyTo(text.Slice(1), buffer.Slice(1));
#endif
                return buffer.AsString();
            }
            case Casing.Pascal:
            {
                Span<char> buffer = stackalloc char[textLen];
                buffer[0] = char.ToUpper(text[0]);
#if net48
                text.Slice(1).CopyTo(buffer.Slice(1));
#else
                TextHelper.Unsafe.CopyTo(text.Slice(1), buffer.Slice(1));
#endif
                return buffer.AsString();
            }
            case Casing.Title:
            {
                // Have to allocate
                return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.AsString());
            }
            case Casing.Field:
            {
                Span<char> buffer = stackalloc char[textLen + 1];
                buffer[0] = '_';
                buffer[1] = char.ToLower(text[0]);
#if net48
                text.Slice(1).CopyTo(buffer.Slice(2));
#else
                TextHelper.Unsafe.CopyTo(text.Slice(1), buffer.Slice(2));
#endif
                return buffer.AsString();
            }
            default:
                return text.AsString();
        }
    }

    public static string ToCasedString(this string? text, Casing casing, CultureInfo? culture)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        if (casing == Casing.Title)
        {
            return (culture ?? CultureInfo.CurrentCulture).TextInfo.ToTitleCase(text);
        }
        return ToCasedString(text.AsSpan(), casing, culture);
    }

    public static string ToCasedString(this ReadOnlySpan<char> text, Casing casing, CultureInfo? culture)
    {
        int textLen = text.Length;
        if (textLen == 0) return string.Empty;
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

                return buffer.AsString();
            }
            case Casing.Upper:
            {
                Span<char> buffer = stackalloc char[textLen];
                for (var i = textLen - 1; i >= 0; i--)
                {
                    buffer[i] = textInfo.ToUpper(text[i]);
                }

                return buffer.AsString();
            }
            case Casing.Camel:
            {
                Span<char> buffer = stackalloc char[textLen];
                buffer[0] = textInfo.ToLower(text[0]);
#if net48
                text.Slice(1).CopyTo(buffer.Slice(1));
#else
                TextHelper.Unsafe.CopyTo(text.Slice(1), buffer.Slice(1));
#endif
                return buffer.AsString();
            }
            case Casing.Pascal:
            {
                Span<char> buffer = stackalloc char[textLen];
                buffer[0] = textInfo.ToUpper(text[0]);
#if net48
                text.Slice(1).CopyTo(buffer.Slice(1));
#else
                TextHelper.Unsafe.CopyTo(text.Slice(1), buffer.Slice(1));
#endif
                return buffer.AsString();
            }
            case Casing.Title:
            {
                // Have to allocate
                return textInfo.ToTitleCase(text.AsString());
            }
            case Casing.Field:
            {
                Span<char> buffer = stackalloc char[textLen + 1];
                buffer[0] = '_';
                buffer[1] = textInfo.ToLower(text[0]);
#if net48
                text.Slice(1).CopyTo(buffer.Slice(2));
#else
                TextHelper.Unsafe.CopyTo(text.Slice(1), buffer.Slice(2));
#endif
                return buffer.AsString();
            }
            default:
                return text.AsString();
        }
    }
}