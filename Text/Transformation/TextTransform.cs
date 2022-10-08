namespace Jay.Text.Transformation;

public enum TextTransform
{
    None,
    Uppercase,
    Lowercase,
    TitleCase,
    CamelCase,
    SnakeCase,
    KebabCase,
    PascalCase,
}

public class TextTransformer
{
    public CultureInfo CultureInfo { get; set; } = CultureInfo.CurrentCulture;
    
    public void TransformSpan(Span<char> charSpan, TextTransform transform)
    {
        switch (transform)
        {
            case TextTransform.None:
                return;
            case TextTransform.Uppercase:
            {
                foreach (ref char ch in charSpan)
                {
                    ch = char.ToUpper(ch);
                }
                return;
            }
            case TextTransform.Lowercase:
            {
                foreach (ref char ch in charSpan)
                {
                    ch = char.ToLower(ch);
                }
                return;
            }
            case TextTransform.TitleCase:
            {
                //return this.CultureInfo.TextInfo.ToTitleCase();
                break;
            }
            case TextTransform.CamelCase:
                break;
            case TextTransform.SnakeCase:
                break;
            case TextTransform.KebabCase:
                break;
            case TextTransform.PascalCase:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(transform), transform, null);
        }
        throw new NotImplementedException();
    }

    public string Transform(string? text, TextTransform transform)
    {
        if (string.IsNullOrEmpty(text)) return "";
        switch (transform)
        {
            case TextTransform.None:
                return text;
            case TextTransform.Uppercase:
                return text.ToUpper(CultureInfo);
            case TextTransform.Lowercase:
                return text.ToLower(CultureInfo);
            case TextTransform.TitleCase:
                return this.CultureInfo.TextInfo.ToTitleCase(text);
            case TextTransform.CamelCase:
                return string.Create(text.Length,
                    text,
                    (span, txt) =>
                    {
                        if (txt.Length == 0) return;
                        span[0] = char.ToLower(txt[0]);
                        for (int i = 1; i < txt.Length; i++)
                        {
                            span[i] = txt[i];
                        }
                    });
            case TextTransform.SnakeCase:
            {
                using var textBuilder = TextBuilder.Borrow();
                CharSpanReader e = text;
                e.SkipWhiteSpace();
                e.TakeWhile(ch => !new char[] { ' ', '_' }.Contains(ch) && !char.IsUpper(ch));
                break;
            }
            case TextTransform.KebabCase:
                break;
            case TextTransform.PascalCase:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(transform), transform, null);
        }
        throw new NotImplementedException();
    }
}