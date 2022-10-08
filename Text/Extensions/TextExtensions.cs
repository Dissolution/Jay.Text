namespace Jay.Text.Extensions;

public static class TextExtensions
{
    public static void ConvertToLower(this Span<char> text)
    {
        for (var i = text.Length - 1; i >= 0; i--)
        {
            text[i] = char.ToLower(text[i]);
        }
    }

    public static void ConvertToUpper(this Span<char> text)
    {
        for (var i = text.Length - 1; i >= 0; i--)
        {
            text[i] = char.ToUpper(text[i]);
        }
    }

    public static void Convert(this Span<char> text, Func<char, char> transform)
    {
        for (var i = 0; i < text.Length; i++)
        {
            text[i] = transform(text[i]);
        }
    }

    public static void Convert(this Span<char> text, Func<char, int, char> transform)
    {
        for (var i = 0; i < text.Length; i++)
        {
            text[i] = transform(text[i], i);
        }
    }
}