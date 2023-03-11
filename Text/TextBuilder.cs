
/* Unmerged change from project 'Text (netstandard2.1)'
Before:
using Jay.Text.Utilities;
After:
using Jay.Text.Text;
using Jay.Text.Buffer;
using Jay.Text.Utilities;
*/
using Jay.Text.Utilities;

namespace Jay.Text;

public delegate void TextBuilderAction<in TBuilder>(TBuilder builder)
    where TBuilder : TextBuilder<TBuilder>;

public delegate void TextBuilderValueAction<in TBuilder, in TValue>(TBuilder builder, TValue value)
    where TBuilder : TextBuilder<TBuilder>;

public delegate void TextBuilderValueIndexAction<in TBuilder, in TValue>(TBuilder builder, TValue value, int index)
    where TBuilder : TextBuilder<TBuilder>;

public delegate void TextBuilderTextAction<in TBuilder>(TBuilder builder, ReadOnlySpan<char> text)
    where TBuilder : TextBuilder<TBuilder>;


public sealed class TextBuilder : TextBuilder<TextBuilder>
{
    public TextBuilder() : base()
    {
    }
}

public abstract class TextBuilder<TBuilder> : TextBuffer
    where TBuilder : TextBuilder<TBuilder>
{
    protected readonly TBuilder _this;

    protected TextBuilder()
        : base()
    {
        _this = (TBuilder)this;
    }

    public Span<char> GetWrittenSpan(TextBuilderAction<TBuilder> textBuilderAction)
    {
        int start = Length;
        textBuilderAction(_this);
        int end = Length;
        return Written[new Range(start: start, end: end)];
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
        this.Write(ch);
        return _this;
    }

    public virtual TBuilder Append(string? str)
    {
        this.Write(str);
        return _this;
    }

    public virtual TBuilder Append(ReadOnlySpan<char> text)
    {
        this.Write(text);
        return _this;
    }

    public virtual TBuilder Append<T>(T? value)
    {
        this.Write(value);
        return _this;
    }

    public virtual TBuilder Format<T>(T? value, string? format, IFormatProvider? provider = null)
    {
        this.WriteFormat(value, format, provider);
        return _this;
    }

    public TBuilder AppendLine(char ch) => Append(ch).NewLine();
    public TBuilder AppendLine(string? str) => Append(str).NewLine();
    public TBuilder AppendLine(ReadOnlySpan<char> text) => Append(text).NewLine();
    public TBuilder AppendLine<T>(T? value) => Append(value).NewLine();
    #endregion

    #region Align
    public TBuilder Align(char ch, int width, Alignment alignment)
    {
        if (width < 1)
            throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be 1 or greater");
        var appendSpan = Allocate(width);
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
                    padding = width / 2 - 1;
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
            Allocate(width).Fill(' ');
            return _this;
        }
        int spaces = width - textLen;
        if (spaces < 0)
            throw new ArgumentOutOfRangeException(nameof(width), width, $"Width must be {textLen} or greater");
        if (spaces == 0)
        {
            this.Write(text);
            return _this;
        }
        var appendSpan = Allocate(width);
        if (alignment == Alignment.Left)
        {
            TextHelper.Unsafe.CopyTo(text, appendSpan, textLen);
            appendSpan[textLen..].Fill(' ');
        }
        else if (alignment == Alignment.Right)
        {
            appendSpan[..spaces].Fill(' ');
            TextHelper.Unsafe.CopyTo(text, appendSpan[spaces..], textLen);
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
            TextHelper.Unsafe.CopyTo(text, appendSpan[frontPadding..], textLen);
            appendSpan[(frontPadding + textLen)..].Fill(' ');
        }
        return _this;
    }
    #endregion

    #region Format
    protected void WriteFormatLine(ReadOnlySpan<char> format, object?[] args)
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
                    this.Write(remainder);
                    return;
                }

                // Append the text until the brace.
                this.Write(remainder.Slice(0, countUntilNextBrace));
                pos += countUntilNextBrace;

                // Get the brace.
                // It must be followed by another character, either a copy of itself in the case of being escaped,
                // or an arbitrary character that's part of the hole in the case of an opening brace.
                char brace = format[pos];
                ch = moveNext(format, ref pos);
                if (brace == ch)
                {
                    this.Write(ch);
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
        WriteFormatLine(format.AsSpan(), args);
        return _this;
    }

    public TBuilder FormatLine(string format, params object?[] args) => Format(format, args).NewLine();

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
        WriteFormatLine(format.Text, args);
        return _this;
    }

    public virtual TBuilder Format(FormattableString formattableString)
    {
        WriteFormatLine(formattableString.Format.AsSpan(), formattableString.GetArguments());
        return _this;
    }

    public TBuilder FormatLine(NonFormattableString format, params object?[] args) => Format(format, args).NewLine();

    public TBuilder FormatLine(FormattableString formattableString) => Format(formattableString).NewLine();
#endif
    #endregion


    #region Enumerate
    public TBuilder Enumerate(
        TextSplitEnumerable splitEnumerable,
        TextBuilderTextAction<TBuilder> perSplitSection)
    {
        foreach (var splitSection in splitEnumerable)
        {
            perSplitSection(_this, splitSection);
        }
        return _this;
    }

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
        Enumerate(enumerable, static (tb, v) => tb.Append(v));

    public TBuilder EnumerateAppendLines<T>(IEnumerable<T> enumerable) =>
        Enumerate(enumerable, static (tb, v) => tb.AppendLine(v));

    public TBuilder EnumerateLines<T>(IEnumerable<T> enumerable, TextBuilderValueAction<TBuilder, T> perValue) =>
        Enumerate(enumerable,
            (tb, v) =>
            {
                perValue(tb, v);
                tb.NewLine();
            });
    #endregion

    #region Delimit
    public TBuilder Delimit(
        ReadOnlySpan<char> delimiter,
        TextSplitEnumerable splitEnumerable,
        TextBuilderTextAction<TBuilder> perSplitSection)
    {
        var splitEnumerator = splitEnumerable.GetEnumerator();
        if (!splitEnumerator.MoveNext()) return _this;
        perSplitSection(_this, splitEnumerator.Current);
        while (splitEnumerator.MoveNext())
        {
            Append(delimiter);
            perSplitSection(_this, splitEnumerator.Current);
        }
        return _this;
    }

    public TBuilder Delimit(
        TextBuilderAction<TBuilder> delimit,
        TextSplitEnumerable splitEnumerable,
        TextBuilderTextAction<TBuilder> perSplitSection)
    {
        var splitEnumerator = splitEnumerable.GetEnumerator();
        if (!splitEnumerator.MoveNext()) return _this;
        perSplitSection(_this, splitEnumerator.Current);
        while (splitEnumerator.MoveNext())
        {
            delimit(_this);
            perSplitSection(_this, splitEnumerator.Current);
        }
        return _this;
    }


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
        var written = Written;
        ref char ch = ref CompatExtensions.NullRef<char>();
        for (var i = written.Length - 1; i >= 0; i--)
        {
            ch = ref written[i];
            if (ch == oldChar)
                ch = newChar;
        }
        return _this;
    }

    public TBuilder Replace(ReadOnlySpan<char> oldText, ReadOnlySpan<char> newText, StringComparison comparison = StringComparison.Ordinal)
    {
        int oldTextLen = oldText.Length;
        if (oldTextLen == 0)
            throw new ArgumentException("Cannot replace null or empty text", nameof(oldText));
        int newTextLen = newText.Length;
        // Length zero is okay


        // Three possible modes:
        var written = Written;

        // Swap
        if (oldTextLen == newTextLen)
        {
            int index = 0;
            while ((index = written.NextIndexOf(oldText, index, comparison)) != -1)
            {
                TextHelper.Unsafe.CopyBlock(
                    in newText.GetPinnableReference(),
                    ref written[index],
                    newTextLen);
                // Increase index to not continue swapping the same thing forever
                index++;
            }
            return _this;
        }

        // Shrink
        if (newTextLen < oldTextLen)
        {
            int writePos = 0;
            var rangeSplit = written.TextSplit(oldText, stringComparison: comparison);
            var splitList = rangeSplit.ToList();
            for (var i = 0; i < splitList.Count; i++)
            {
                // Write the range
                Range range = splitList.Range(i);
                (int offset, int length) = range.GetOffsetAndLength(Length);
                TextHelper.Unsafe.CopyTo(
                    written.Slice(offset, length),
                    written.Slice(writePos), length);
                writePos += length;

                // If we're at end, we are done
                if (i == (splitList.Count - 1))
                {
                    Debug.Assert(offset + length == Length);
                    Length = writePos;
                    return _this;
                }

                // Write our new text
                TextHelper.Unsafe.CopyTo(newText, written.Slice(writePos), newTextLen);
                writePos += newTextLen;
            }

            // Done
            Length = writePos;
            return _this;
        }

        // Expand
        Debug.Assert(newTextLen > oldTextLen);
        {
            // Move current to a buffer
            Span<char> buffer = stackalloc char[Length];
            TextHelper.Unsafe.CopyTo(written, buffer, Length);
            // Set us to zero
            Length = 0;

            int writePos = 0;
            var rangeSplit = buffer.TextSplit(oldText, stringComparison: comparison);
            var splitList = rangeSplit.ToList();
            for (var i = 0; i < splitList.Count; i++)
            {
                // Write the range
                Range range = splitList.Range(i);
                (int offset, int length) = range.GetOffsetAndLength(buffer.Length);
                this.Write(buffer.Slice(offset, length));
                writePos += length;

                // If we're at end, we are done
                if (i == (splitList.Count-1))
                {
                    Debug.Assert(offset + length == buffer.Length);
                    //Length = writePos;
                    Debug.Assert(Length == writePos);
                    return _this;
                }

                // Write our new text
                this.Write(newText);
                writePos += newTextLen;
                Debug.Assert(Length == writePos);
            }

            // Done
            //Length = writePos;
            Debug.Assert(Length == writePos);
            return _this;
        }
    }

    public TBuilder Replace(string oldText, string? newText, StringComparison comparison = StringComparison.Ordinal)
        => Replace(oldText.AsSpan(), newText.AsSpan(), comparison);

    #endregion

    public TBuilder TrimStart()
    {
        int start = 0;
        for (; start < Length; start++)
        {
            if (!char.IsWhiteSpace(Written[start]))
            {
                break;
            }
        }

        if (start > 0)
        {
            RemoveFirst(start);
        }

        return _this;
    }

    public TBuilder TrimEnd()
    {
        int end = Length - 1;

        for (; end >= 0; end--)
        {
            if (!char.IsWhiteSpace(Written[end]))
            {
                break;
            }
        }

        Length = end + 1;
        return _this;
    }

    public TBuilder Clear()
    {
        // Nice hack
        Length = 0;
        return _this;
    }
}