using Jay.Text.Utilities;

namespace Jay.Text.Scratch.WriteExtensions;

public static class WriteExtensions
{
    public static void Write(this ref StackTextBuilder textBuilder, char ch)
        => textBuilder.Allocate(1)[0] = ch;
    public static void Write(this ref StackTextBuilder textBuilder, string? str)
        => Write(ref textBuilder, str.AsSpan());
    public static void Write(this ref StackTextBuilder textBuilder, scoped ReadOnlySpan<char> text)
        => TextHelper.CopyTo(text, textBuilder.Allocate(text.Length));
    
    public static void Write<T>(this ref StackTextBuilder textBuilder, T? value)
    {
        string? str;
        if (value is IFormattable)
        {
#if NET6_0_OR_GREATER
            if (value is ISpanFormattable)
            {
                int charsWritten;
                while (!((ISpanFormattable)value).TryFormat(
                           textBuilder.Available,
                           out charsWritten, 
                           default, default))
                {
                    // Grow!
                    textBuilder.GrowBy(1);
                }
                // Add length
                textBuilder.Length += charsWritten;
                // Done
                return;
            }
#endif
            str = ((IFormattable)value).ToString(default, default);
        }
        else
        {
            str = value?.ToString();
        }
        if (str is not null)
        {
            TextHelper.CopyTo(str, textBuilder.Allocate(str.Length));
        }
    }
    
    public static void Write<T>(this ref StackTextBuilder textBuilder,
        T? value,
        string? format,
        IFormatProvider? provider = default)
    {
        string? str;
        if (value is IFormattable)
        {
#if NET6_0_OR_GREATER
            if (value is ISpanFormattable)
            {
                int charsWritten;
                while (!((ISpanFormattable)value).TryFormat(
                           textBuilder.Available,
                           out charsWritten,
                           format.AsSpan(),
                           provider))
                {
                    // Grow!
                    textBuilder.GrowBy(1);
                }
                // Add length
                textBuilder.Length += charsWritten;
                // Done
                return;
            }
#endif
            str = ((IFormattable)value).ToString(format, provider);
        }
        else
        {
            str = value?.ToString();
        }
        if (str is not null)
        {
            TextHelper.CopyTo(str, textBuilder.Allocate(str.Length));
        }
    }
    
    public static void Write<T>(this ref StackTextBuilder textBuilder,
        T? value,
        scoped ReadOnlySpan<char> format,
        IFormatProvider? provider = default)
    {
        string? str;
        if (value is IFormattable)
        {
#if NET6_0_OR_GREATER
            if (value is ISpanFormattable)
            {
                int charsWritten;
                while (!((ISpanFormattable)value).TryFormat(
                           textBuilder.Available,
                           out charsWritten,
                           format,
                           provider))
                {
                    // Grow!
                    textBuilder.GrowBy(1);
                }
                // Add length
                textBuilder.Length += charsWritten;
                // Done
                return;
            }
#endif
            str = ((IFormattable)value).ToString(format.ToString(), provider);
        }
        else
        {
            str = value?.ToString();
        }
        if (str is not null)
        {
            TextHelper.CopyTo(str, textBuilder.Allocate(str.Length));
        }
    }

    public static void WriteLine(this ref StackTextBuilder textBuilder)
    {
        Write(ref textBuilder, TextHelper.NewLineSpan);
    }
}