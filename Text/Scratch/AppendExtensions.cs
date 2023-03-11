using Jay.Text.Utilities;

namespace Jay.Text.Scratch.AppendExtensions;

public static class AppendExtensions
{
    public static ref StackTextBuilder Append(this ref StackTextBuilder textBuilder, char ch)
    {
        textBuilder.Allocate(1)[0] = ch;
        return ref textBuilder;
    }
    public static ref StackTextBuilder Append(this ref StackTextBuilder textBuilder, string? str)
    {
        if (str is not null)
        {
            TextHelper.CopyTo(str, textBuilder.Allocate(str.Length));
        }
        return ref textBuilder;
    }
    public static ref StackTextBuilder Append(this ref StackTextBuilder textBuilder, scoped ReadOnlySpan<char> text)
    {
        TextHelper.CopyTo(text, textBuilder.Allocate(text.Length));
        return ref textBuilder;
    }

    public static ref StackTextBuilder Append<T>(this ref StackTextBuilder textBuilder, T? value)
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
                           default,
                           default))
                {
                    // Grow!
                    textBuilder.GrowBy(1);
                }
                // Add length
                textBuilder.Length += charsWritten;
                // Done
                return ref textBuilder;
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

        return ref textBuilder;
    }

    public static ref StackTextBuilder Append<T>(this ref StackTextBuilder textBuilder,
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
                return ref textBuilder;
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

        return ref textBuilder;
    }

    public static ref StackTextBuilder Append<T>(this ref StackTextBuilder textBuilder,
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
                return ref textBuilder;
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

        return ref textBuilder;
    }

    public static ref StackTextBuilder AppendLine(this ref StackTextBuilder textBuilder)
    {
        return ref Append(ref textBuilder, TextHelper.NewLineSpan);
    }

    public static ref StackTextBuilder AppendLine(this ref StackTextBuilder textBuilder, char ch)
    {
        Append(ref textBuilder, ch);
        AppendLine(ref textBuilder);
        return ref textBuilder;
    }

    public static ref StackTextBuilder AppendLine(this ref StackTextBuilder textBuilder, string? str)
    {
        Append(ref textBuilder, str);
        AppendLine(ref textBuilder);
        return ref textBuilder;
    }

    public static ref StackTextBuilder AppendLine(this ref StackTextBuilder textBuilder, scoped ReadOnlySpan<char> text)
    {
        Append(ref textBuilder, text);
        AppendLine(ref textBuilder);
        return ref textBuilder;
    }

    public static ref StackTextBuilder AppendLine<T>(this ref StackTextBuilder textBuilder, T? value)
    {
        Append<T>(ref textBuilder, value);
        AppendLine(ref textBuilder);
        return ref textBuilder;
    }

    public static ref StackTextBuilder AppendLine<T>(this ref StackTextBuilder textBuilder, T? value,
        string? format, IFormatProvider? provider = default)
    {
        Append<T>(ref textBuilder, value, format, provider);
        AppendLine(ref textBuilder);
        return ref textBuilder;
    }

    public static ref StackTextBuilder AppendLine<T>(this ref StackTextBuilder textBuilder, T? value,
        ReadOnlySpan<char> format, IFormatProvider? provider = default)
    {
        Append<T>(ref textBuilder, value, format, provider);
        AppendLine(ref textBuilder);
        return ref textBuilder;
    }
}