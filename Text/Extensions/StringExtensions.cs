namespace Jay.Text.Extensions;

public static class StringExtensions
{
    public static bool IsNullOrEmpty([NotNullWhen(false)] this string? text)
    {
        return text == null || (uint)text.Length <= 0u;
    }
    
    public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string? text)
    {
        if (text == null) return true;
        for (int i = text.Length - 1; i >= 0; i--)
        {
            if (!char.IsWhiteSpace(text[i])) return false;
        }
        return true;
    }
    
    public static bool IsNonWhiteSpace([NotNullWhen(true)] this string? text)
    {
        if (text == null) return false;
        for (int i = text.Length - 1; i >= 0; i--)
        {
            if (!char.IsWhiteSpace(text[i])) return true;
        }
        return false;
    }
    
    public static bool TryGetChar(this string? text, int index, out char ch)
    {
        if (text is not null && (uint)index < (uint)text.Length)
        {
            ch = text[index];
            return true;
        }
        ch = default;
        return false;
    }
    
    [return: NotNullIfNotNull("ifInvalid")]
    public static string? IfNull(this string? str, string? ifInvalid)
    {
        return str ?? ifInvalid;
    }

    [return: NotNullIfNotNull("ifInvalid")]
    public static string? IfNullOrEmpty(this string? str, string? ifInvalid)
    {
        if (string.IsNullOrEmpty(str))
            return ifInvalid;
        return str;
    }

    [return: NotNullIfNotNull("ifInvalid")]
    public static string? IfNullOrWhiteSpace(this string? str, string? ifInvalid)
    {
        if (string.IsNullOrWhiteSpace(str))
            return ifInvalid;
        return str;
    }
}