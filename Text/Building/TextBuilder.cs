using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Jay.Text;

public sealed class TextBuilder : TextBuilder<TextBuilder>
{
    public TextBuilder() : base()
    {
    }
}

public abstract class TextBuilder<TBuilder> : IDisposable
    where TBuilder : TextBuilder<TBuilder>
{
    protected readonly CharArrayBuilder _charArrayBuilder;
    protected readonly TBuilder _this;

    public int Length
    {
        get => _charArrayBuilder.Length;
        set => _charArrayBuilder.Length = value;
    }

    public Span<char> Written => _charArrayBuilder.Written;
    public Span<char> Available => _charArrayBuilder.Available;

    protected TextBuilder()
        : base()
    {
        _charArrayBuilder = new CharArrayBuilder();
        _this = (TBuilder)this;
    }

    public Span<char> GetWrittenSpan(TextBuilderAction<TBuilder> textBuilderAction)
    {
        int start = Length;
        textBuilderAction(_this);
        int end = Length;
        return _charArrayBuilder.CharSpan[new Range(start, end)];
    }

#region New Line
    public virtual TBuilder NewLine()
    {
        return Append(TextHelper.NewLineSpan);
    }

    public TBuilder NewLines(int count)
    {
        for (var i = 0; i < count; i++)
        {
            NewLine();
        }
        return _this;
    }
#endregion

#region Append
    public virtual TBuilder Append(char ch)
    {
        _charArrayBuilder.Write(ch);
        return _this;
    }

    public virtual TBuilder Append(string? str)
    {
        _charArrayBuilder.Write(str);
        return _this;
    }

    public virtual TBuilder Append(ReadOnlySpan<char> text)
    {
        _charArrayBuilder.Write(text);
        return _this;
    }

    public virtual TBuilder Append<T>(T? value)
    {
        _charArrayBuilder.Write<T>(value);
        return _this;
    }

    public virtual TBuilder Format<T>(T? value, string? format)
    {
        _charArrayBuilder.Write<T>(value, format);
        return _this;
    }

    public TBuilder AppendLine(char ch) => Append(ch).NewLine();
    public TBuilder AppendLine(string? str) => Append(str).NewLine();
    public TBuilder AppendLine(ReadOnlySpan<char> text) => Append(text).NewLine();
    public TBuilder AppendLine<T>(T? value) => Append<T>(value).NewLine();
#endregion

#region Align
    public TBuilder Align(char ch, int width, Alignment alignment)
    {
        if (width < 1)
            throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be 1 or greater");
        var appendSpan = _charArrayBuilder.Allocate(width);
        if (alignment == Alignment.Left)
        {
            appendSpan[0] = ch;
            appendSpan[1..].Fill(' ');
        }
        else if (alignment == Alignment.Right)
        {
            appendSpan[..^1].Fill(' ');
            appendSpan[^1] = ch;
        }
        else // Center
        {
            int padding;
            // Odd width?
            if (width % 2 == 1)
            {
                padding = width / 2;
            }
            else // even
            {
                if (alignment.HasFlag(Alignment.Right)) // Right|Center?
                {
                    padding = width / 2;
                }
                else // Left|Center / Default|Center
                {
                    padding = (width / 2) - 1;
                }
            }
            appendSpan[..padding].Fill(' ');
            appendSpan[padding] = ch;
            appendSpan[(padding + 1)..].Fill(' ');
        }
        return _this;
    }
    public TBuilder Align(string? str, int width, Alignment alignment) => Align(str.AsSpan(), width, alignment);

    public TBuilder Align(ReadOnlySpan<char> text, int width, Alignment alignment)
    {
        int textLen = text.Length;
        if (textLen == 0)
        {
            _charArrayBuilder.Allocate(width).Fill(' ');
            return _this;
        }
        int spaces = width - textLen;
        if (spaces < 0)
            throw new ArgumentOutOfRangeException(nameof(width), width, $"Width must be {textLen} or greater");
        if (spaces == 0)
        {
            _charArrayBuilder.Write(text);
            return _this;
        }
        var appendSpan = _charArrayBuilder.Allocate(width);
        if (alignment == Alignment.Left)
        {
            TextHelper.Unsafe.CopyBlock(text, appendSpan, textLen);
            appendSpan[textLen..].Fill(' ');
        }
        else if (alignment == Alignment.Right)
        {
            appendSpan[..spaces].Fill(' ');
            TextHelper.Unsafe.CopyBlock(text, appendSpan[spaces..], textLen);
        }
        else // Center
        {

            int frontPadding;
            // Even spacing is easy split
            if (spaces % 2 == 0)
            {
                frontPadding = spaces / 2;
            }
            else // Odd spacing we have to align
            {
                if (alignment.HasFlag(Alignment.Right)) // Right|Center
                {
                    frontPadding = (int)Math.Ceiling(spaces / 2d);
                }
                else // Center or Left|Center 
                {
                    frontPadding = (int)Math.Floor(spaces / 2d);
                }
            }
            appendSpan[..frontPadding].Fill(' ');
            TextHelper.Unsafe.CopyBlock(text, appendSpan[frontPadding..], textLen);
            appendSpan[(frontPadding + textLen)..].Fill(' ');
        }
        return _this;
    }
#endregion

#region Format
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
                    _charArrayBuilder.Write(remainder);
                    return;
                }

                // Append the text until the brace.
                _charArrayBuilder.Write(remainder.Slice(0, countUntilNextBrace));
                pos += countUntilNextBrace;

                // Get the brace.
                // It must be followed by another character, either a copy of itself in the case of being escaped,
                // or an arbitrary character that's part of the hole in the case of an opening brace.
                char brace = format[pos];
                ch = moveNext(format, ref pos);
                if (brace == ch)
                {
                    _charArrayBuilder.Write(ch);
                    pos++;
                    continue;
                }

                // This wasn't an escape, so it must be an opening brace.
                if (brace != '{')
                {
                    throw createFormatException(format, pos, "Missing opening brace");
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
                throw createFormatException(format, pos, "Invalid character in index");
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
                    throw createFormatException(format, pos, "Alignment is not supported");
                }

                // The next character needs to either be a closing brace for the end of the hole,
                // or a colon indicating the start of the format.
                if (ch != '}')
                {
                    if (ch != ':')
                    {
                        // Unexpected character
                        throw createFormatException(format, pos, "Unexpected character");
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
                            throw createFormatException(format, pos, "Braces inside the argument hole are not supported");
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
                throw createFormatException(format, pos, $"Invalid Format: Argument '{index}' does not exist");
            }

            string? itemFormat = null;
            if (itemFormatSpan.Length > 0)
                itemFormat = itemFormatSpan.ToString();

            object? arg = args[index];

            // Append this arg, allows for overridden behavior
            Format<object?>(arg, itemFormat);

            // Continue parsing the rest of the format string.
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static char moveNext(ReadOnlySpan<char> format, ref int pos)
        {
            pos++;
            if (pos < format.Length) 
                return format[pos];
            throw createFormatException(format, pos, "Attempted to move past final character");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static FormatException createFormatException(ReadOnlySpan<char> format, int pos, string? details = null)
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
            return new FormatException(message.ToString());
        }
    }

#if NET6_0_OR_GREATER
    public virtual TBuilder Format(string format, params object?[] args)
    {
        FormatHelper(format.AsSpan(), args);
        return _this;
    }

    public virtual TBuilder Format(
        [InterpolatedStringHandlerArgument("")]
        ref InterpolatedTextBuilder<TBuilder> interpolatedString)
    {
        // The writing has already happened by the time we get into this method!
        return _this;
    }

    public virtual TBuilder FormatLine(
        [InterpolatedStringHandlerArgument("")]
        ref InterpolatedTextBuilder<TBuilder> interpolatedString)
    {
        return NewLine();
    }
#else
    public virtual TBuilder Format(NonFormattableString format, params object?[] args)
    {
        FormatHelper(format.CharSpan, args);
        return _this;
    }
    public virtual TBuilder Format(FormattableString formattableString)
    {
        FormatHelper(formattableString.Format.AsSpan(), formattableString.GetArguments());
        return _this;
    }
    public TBuilder FormatLine(NonFormattableString format, params object?[] args) => Format(format, args).NewLine();
    public TBuilder FormatLine(FormattableString formattableString) => Format(formattableString).NewLine();
#endif
#endregion


#region Enumerate
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

    public TBuilder EnumerateAppend<T>(IEnumerable<T> enumerable) =>
        Enumerate(enumerable, static (tb, v) => tb.Append<T>(v));

    public TBuilder EnumerateAppendLines<T>(IEnumerable<T> enumerable) =>
        Enumerate(enumerable, static (tb, v) => tb.AppendLine<T>(v));

    public TBuilder EnumerateLines<T>(IEnumerable<T> enumerable, TextBuilderValueAction<TBuilder, T> perValue) =>
        Enumerate(enumerable,
            (tb, v) =>
            {
                perValue(tb, v);
                tb.NewLine();
            });
#endregion

#region Delimit
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
        return Delimit(w => w.Append(delimiter), values, perValue);
    }

    public TBuilder Delimit<T>(string delimiter, IEnumerable<T> values, TextBuilderValueIndexAction<TBuilder, T> perValueIndex)
    {
        return Delimit(w => w.Append(delimiter), values, perValueIndex);
    }

    public TBuilder DelimitLines<T>(IEnumerable<T> values, TextBuilderValueAction<TBuilder, T> perValue)
    {
        return Delimit(static w => w.NewLine(), values, perValue);
    }

    public TBuilder DelimitLines<T>(IEnumerable<T> values, TextBuilderValueIndexAction<TBuilder, T> perValueIndex)
    {
        return Delimit(static w => w.NewLine(), values, perValueIndex);
    }
#endregion

#region Replace
    public TBuilder Replace(char oldChar, char newChar)
    {
        var written = _charArrayBuilder.Written;
        for (var i = written.Length; i >= 0; i--)
        {
            if (written[i] == oldChar)
                written[i] = newChar;
        }
        return _this;
    }

    public TBuilder Replace(ReadOnlySpan<char> oldText, ReadOnlySpan<char> newText)
    {
        int oldTextLen = oldText.Length;
        int newTextLen = newText.Length;
        // Swap
        if (oldTextLen == newTextLen)
        {
            var written = _charArrayBuilder.Written;
            int index = 0;
            while ((index = written.IndexOf(oldText, index)) != -1)
            {
                TextHelper.Unsafe.CopyBlock(
                    in newText.GetPinnableReference(),
                    ref written[index],
                    oldTextLen);
            }
            return _this;
        }
        // Shrink
        else if (oldTextLen > newTextLen)
        {
            var written = _charArrayBuilder.Written;
            int index = 0;
            while ((index = written.IndexOf(oldText, index)) != -1)
            {
                TextHelper.Unsafe.CopyBlock(
                    in newText.GetPinnableReference(),
                    ref written[index],
                    oldTextLen);
            }
            return _this;
        }
        // Expand
        else
        {
            throw new NotImplementedException();
        }
    }
    
    public TBuilder Replace(ReadOnlySpan<char> oldText, ReadOnlySpan<char> newText, StringComparison stringComparison)
    {
        throw new NotImplementedException();
    }
    
    public TBuilder Replace(string oldText, string? newText)
    {
        throw new NotImplementedException();
    }
    
    public TBuilder Replace(string oldText, string? newText, StringComparison stringComparison)
    {
        throw new NotImplementedException();
    }
#endregion

    public virtual void Dispose() => _charArrayBuilder.Dispose();

    public string ToStringAndDispose() => _charArrayBuilder.ToStringAndDispose();

    public override string ToString() => _charArrayBuilder.ToString();
}