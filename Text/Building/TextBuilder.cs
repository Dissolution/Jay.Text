using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Jay.Text;

public sealed class TextBuilder : TextBuilder<TextBuilder>
{
    public TextBuilder() : base() { }
}

public abstract class TextBuilder<TBuilder> : CharArrayBuilderBase
    where TBuilder : TextBuilder<TBuilder>
{
    protected readonly TBuilder _this;

    protected TextBuilder()
        : base()
    {
        _this = (TBuilder)this;
    }

    public virtual TBuilder NewLine()
    {
        AppendNonNullString(DefaultNewLine);
        return _this;
    }

    public TBuilder NewLines(int count)
    {
        for (var i = 0; i < count; i++)
        {
            NewLine();
        }
        return _this;
    }

    public virtual TBuilder Write(char ch)
    {
        AppendChar(ch);
        return _this;
    }

    public virtual TBuilder Write(string? str)
    {
        AppendString(str);
        return _this;
    }

    public virtual TBuilder Write(ReadOnlySpan<char> text)
    {
        AppendCharSpan(text);
        return _this;
    }

    public virtual TBuilder Write<T>([AllowNull] T value)
    {
        AppendValue<T>(value);
        return _this;
    }

    public virtual TBuilder Write<T>([AllowNull] T value, string? format)
    {
        AppendFormat<T>(value, format);
        return _this;
    }

    protected void FormatHelper(ReadOnlySpan<char> format, object?[] args)
    {
        // Undocumented exclusive limits on the range for Argument Hole Index
        const int IndexLimit = 1_000_000; // Note:            0 <= ArgIndex < IndexLimit

        // Repeatedly find the next hole and process it.
        int pos = 0;
        char ch;
        while (true)
        {
            // Skip until either the end of the input or the first unescaped opening brace, whichever comes first.
            // Along the way we need to also unescape escaped closing braces.
            while (true)
            {
                // Find the next brace.  If there isn't one, the remainder of the input is text to be appended, and we're done.
                if (pos >= format.Length)
                {
                    return;
                }

                ReadOnlySpan<char> remainder = format.Slice(pos);
                int countUntilNextBrace = remainder.IndexOfAny('{', '}');
                if (countUntilNextBrace < 0)
                {
                    AppendCharSpan(remainder);
                    return;
                }

                // Append the text until the brace.
                AppendCharSpan(remainder.Slice(0, countUntilNextBrace));
                pos += countUntilNextBrace;

                // Get the brace.
                // It must be followed by another character, either a copy of itself in the case of being escaped,
                // or an arbitrary character that's part of the hole in the case of an opening brace.
                char brace = format[pos];
                ch = moveNext(format, ref pos);
                if (brace == ch)
                {
                    AppendChar(ch);
                    pos++;
                    continue;
                }

                // This wasn't an escape, so it must be an opening brace.
                if (brace != '{')
                {
                    throwFormatException(format, pos, "Missing opening brace");
                }

                // Proceed to parse the hole.
                break;
            }

            // We're now positioned just after the opening brace of an argument hole, which consists of
            // an opening brace, an index, and an optional format
            // preceded by a colon, with arbitrary amounts of spaces throughout.
            ReadOnlySpan<char> itemFormatSpan = default; // used if itemFormat is null

            // First up is the index parameter, which is of the form:
            //     at least on digit
            //     optional any number of spaces
            // We've already read the first digit into ch.
            Debug.Assert(format[pos - 1] == '{');
            Debug.Assert(ch != '{');
            int index = ch - '0';
            // Has to be between 0 and 9
            if ((uint)index >= 10u)
            {
                throwFormatException(format, pos, "Invalid character in index");
            }

            // Common case is a single digit index followed by a closing brace.  If it's not a closing brace,
            // proceed to finish parsing the full hole format.
            ch = moveNext(format, ref pos);
            if (ch != '}')
            {
                // Continue consuming optional additional digits.
                while (ch.IsAsciiDigit() && index < IndexLimit)
                {
                    // Shift by power of 10
                    index = index * 10 + (ch - '0');
                    ch = moveNext(format, ref pos);
                }

                // Consume optional whitespace.
                while (ch == ' ')
                {
                    ch = moveNext(format, ref pos);
                }

                // We do not support alignment
                if (ch == ',')
                {
                    throwFormatException(format, pos, "Alignment is not supported");
                }

                // The next character needs to either be a closing brace for the end of the hole,
                // or a colon indicating the start of the format.
                if (ch != '}')
                {
                    if (ch != ':')
                    {
                        // Unexpected character
                        throwFormatException(format, pos, "Unexpected character");
                    }

                    // Search for the closing brace; everything in between is the format,
                    // but opening braces aren't allowed.
                    int startingPos = pos;
                    while (true)
                    {
                        ch = moveNext(format, ref pos);

                        if (ch == '}')
                        {
                            // Argument hole closed
                            break;
                        }

                        if (ch == '{')
                        {
                            // Braces inside the argument hole are not supported
                            throwFormatException(format, pos, "Braces inside the argument hole are not supported");
                        }
                    }

                    startingPos++;
                    itemFormatSpan = format.Slice(startingPos, pos - startingPos);
                }
            }

            // Construct the output for this arg hole.
            Debug.Assert(format[pos] == '}');
            pos++;

            if ((uint)index >= (uint)args.Length)
            {
                throw new FormatException($"Invalid Format: Argument '{index}' does not exist");
            }

            string? itemFormat = null;
            if (itemFormatSpan.Length > 0)
                itemFormat = itemFormatSpan.ToString();

            object? arg = args[index];

            // Write this arg
            Write<object?>(arg, itemFormat);

            // Continue parsing the rest of the format string.
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static char moveNext(ReadOnlySpan<char> format, ref int pos)
        {
            pos++;
            if (pos >= format.Length)
            {
                throwFormatException(format, pos, "Ran out of room");
            }

            return format[pos];
        }

        [DoesNotReturn]
        static void throwFormatException(ReadOnlySpan<char> format, int pos, string? details = null)
        {
            using var message = new CharSpanBuilder();
            message.Write("Invalid Format at position ");
            message.Write(pos);
            message.Write(Environment.NewLine);
            int start = pos - 16;
            if (start < 0)
                start = 0;
            int end = pos + 16;
            if (end > format.Length)
                end = format.Length - 1;
            message.Write(format[new Range(start, end)]);
            if (details is not null)
            {
                message.Write(Environment.NewLine);
                message.Write("Details: ");
                message.Write(details);
            }
            throw new FormatException(message.ToString());
        }
    }

#if NET6_0_OR_GREATER
    public TBuilder Format(string format, params object?[] args)
    {
        FormatHelper(format.AsSpan(), args);
        return _this;
    }
    
    public TBuilder Format(
        [InterpolatedStringHandlerArgument("")]
        ref InterpolatedTextBuilder<TBuilder> interpolatedString)
    {
        // The writing has already happened by the time we get into this method!
        return _this;
    }
    
     public TBuilder FormatLine(
        [InterpolatedStringHandlerArgument("")]
        ref InterpolatedTextBuilder<TBuilder> interpolatedString)
    {
        return NewLine();
    }
    #else
    public TBuilder Format(NonFormattableString format, params object?[] args)
    {
        FormatHelper(format.CharSpan, args);
        return _this;
    }
    public virtual TBuilder Format(FormattableString formattableString)
    {
        FormatHelper(formattableString.Format.AsSpan(), formattableString.GetArguments());
        return _this;
    }

    public TBuilder FormatLine(FormattableString formattableString) => Format(formattableString).NewLine();
#endif
    
    public TBuilder WriteLine(char ch) => Write(ch).NewLine();
    public TBuilder WriteLine(string? str) => Write(str).NewLine();
    public TBuilder WriteLine(ReadOnlySpan<char> text) => Write(text).NewLine();
    public TBuilder WriteLine<T>(T? value) => Write<T>(value).NewLine();

    
    
    public TBuilder Enumerate<T>(IEnumerable<T> values, TextBuilderValueAction<TBuilder, T> perValue)
    {
        foreach (var value in values)
        {
            perValue(_this, value);
        }
        return _this;
    }

    public TBuilder Enumerate<T>(IEnumerable<T> values, TextBuilderValueIndexAction<TBuilder, T> perValueIndex)
    {
        if (values is IList<T> list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                perValueIndex(_this, list[i], i);
            }
        }
        else
        {
            using var e = values.GetEnumerator();
            if (!e.MoveNext()) return _this;
            int i = 0;
            perValueIndex(_this, e.Current, i);
            while (e.MoveNext())
            {
                i++;
                perValueIndex(_this, e.Current, i);
            }
        }
        return _this;
    }

    public TBuilder EnumerateWrite<T>(IEnumerable<T> enumerable) =>
        Enumerate(enumerable, static (tb, v) => tb.Write<T>(v));

    public TBuilder EnumerateWriteLines<T>(IEnumerable<T> enumerable) =>
        Enumerate(enumerable, static (tb, v) => tb.Write<T>(v).NewLine());

    public TBuilder EnumerateLines<T>(IEnumerable<T> enumerable, TextBuilderValueAction<TBuilder, T> perValue) =>
        Enumerate(enumerable,
            (tb, v) =>
            {
                perValue(tb, v);
                tb.NewLine();
            });

    public TBuilder Delimit<T>(TextBuilderAction<TBuilder> delimit, IEnumerable<T> values, TextBuilderValueAction<TBuilder, T> perValue)
    {
        if (values is IList<T> list)
        {
            var count = list.Count;
            if (count == 0) return _this;
            perValue(_this, list[0]);
            for (var i = 1; i < count; i++)
            {
                delimit(_this);
                perValue(_this, list[i]);
            }
        }
        else
        {
            using var e = values.GetEnumerator();
            if (!e.MoveNext()) return _this;
            perValue(_this, e.Current);
            while (e.MoveNext())
            {
                delimit(_this);
                perValue(_this, e.Current);
            }
        }

        return _this;
    }

    public TBuilder Delimit<T>(TextBuilderAction<TBuilder> delimit, IEnumerable<T> values, TextBuilderValueIndexAction<TBuilder, T> perValueIndex)
    {
        if (values is IList<T> list)
        {
            var count = list.Count;
            if (count == 0) return _this;
            perValueIndex(_this, list[0], 0);
            for (var i = 1; i < count; i++)
            {
                delimit(_this);
                perValueIndex(_this, list[i], i);
            }
        }
        else
        {
            using var e = values.GetEnumerator();
            if (!e.MoveNext()) return _this;
            int i = 0;
            perValueIndex(_this, e.Current, i);
            while (e.MoveNext())
            {
                i++;
                delimit(_this);
                perValueIndex(_this, e.Current, i);
            }
        }

        return _this;
    }

    public TBuilder Delimit<T>(string delimiter, IEnumerable<T> values, TextBuilderValueAction<TBuilder, T> perValue)
    {
        return Delimit(w => w.Write(delimiter), values, perValue);
    }

    public TBuilder Delimit<T>(string delimiter, IEnumerable<T> values, TextBuilderValueIndexAction<TBuilder, T> perValueIndex)
    {
        return Delimit(w => w.Write(delimiter), values, perValueIndex);
    }

    public TBuilder DelimitLines<T>(IEnumerable<T> values, TextBuilderValueAction<TBuilder, T> perValue)
    {
        return Delimit(static w => w.NewLine(), values, perValue);
    }

    public TBuilder DelimitLines<T>(IEnumerable<T> values, TextBuilderValueIndexAction<TBuilder, T> perValueIndex)
    {
        return Delimit(static w => w.NewLine(), values, perValueIndex);
    }
}