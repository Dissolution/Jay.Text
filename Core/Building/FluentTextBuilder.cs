using System.Diagnostics;
using Jay.Text.Splitting;

namespace Jay.Text.Building;

public delegate void TBA<in TBuilder>(TBuilder builder)
    where TBuilder : TextWriter;

public delegate void TBA<in TBuilder, in T>(TBuilder builder, T value)
    where TBuilder : TextWriter;

public delegate void TBAI<in TBuilder, in T>(TBuilder builder, T value, int index)
    where TBuilder : TextWriter;

public abstract class FluentTextBuilder<TBuilder> : TextWriter
    where TBuilder : FluentTextBuilder<TBuilder>
{
    protected readonly TBuilder _builder;
    protected readonly string _newline;

    protected FluentTextBuilder()
        : base()
    {
        _builder = (TBuilder)this;
        _newline = Environment.NewLine;
    }

    public Span<char> GetWrittenSpan(TBA<TBuilder> action)
    {
        int start = Length;
        action(_builder);
        int end = Length;
        return Written[new Range(start: start, end: end)];
    }
    
    public TBuilder Act(TBA<TBuilder> tba)
    {
        tba(_builder);
        return _builder;
    }

    public virtual TBuilder NewLine() => Append(_newline);
    public TBuilder NewLines(int count)
    {
        for (var i = 0; i < count; i++)
        {
            this.NewLine();
        }
        return _builder;
    }
    
#region Append
    public TBuilder Append(char ch)
    {
        this.Write(ch);
        return _builder;
    }
    public TBuilder AppendLine(char ch) 
        => this.Append(ch).NewLine();

    public TBuilder Append(scoped ReadOnlySpan<char> text)
    {
        this.Write(text);
        return _builder;
    }
    public TBuilder AppendLine(scoped ReadOnlySpan<char> text) 
        => this.Append(text).NewLine();
    
    public TBuilder Append(string? str)
    {
        this.Write(str);
        return _builder;
    }
    public TBuilder AppendLine(string? str) 
        => this.Append(str).NewLine();

    
    public TBuilder Append([InterpolatedStringHandlerArgument("")] ref InterpolatedTextBuilder text)
    {
        this.Write(ref text);
        return _builder;
    }
    public TBuilder AppendLine(ref InterpolatedTextBuilder text) 
        => this.Append(ref text).NewLine();

    public TBuilder Append<T>(T? value, string? format = null, IFormatProvider? provider = null)
    {
        base.Format<T?>(value, format, provider);
        return _builder;
    }
  
    public TBuilder AppendLine<T>(T? value, string? format = null, IFormatProvider? provider = null) 
        => this.Append(value, format, provider).NewLine();
#endregion

    public override void Format<T>(T? value, string? format = null, IFormatProvider? provider = null) where T : default
    {
        if (value is TBA<TBuilder> tba)
        {
            tba(_builder);
        }
        else
        {
            base.Format(value, format, provider);
        }
    }

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
        return _builder;
    }
    public TBuilder Align(string? str, int width, Alignment alignment) => Align(str.AsSpan(), width, alignment);

    public TBuilder Align(ReadOnlySpan<char> text, int width, Alignment alignment)
    {
        int textLen = text.Length;
        if (textLen == 0)
        {
            Allocate(width).Fill(' ');
            return _builder;
        }
        int spaces = width - textLen;
        if (spaces < 0)
            throw new ArgumentOutOfRangeException(nameof(width), width, $"Width must be {textLen} or greater");
        if (spaces == 0)
        {
            this.Write(text);
            return _builder;
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
        return _builder;
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
            using var message = new InterpolatedTextBuilder();
            message.AppendLiteral("Invalid Format at position ");
            message.AppendFormatted(pos);
            message.AppendLiteral(Environment.NewLine);
            int start = pos - 16;
            if (start < 0)
                start = 0;
            int end = pos + 16;
            if (end > format.Length)
                end = format.Length - 1;
            message.AppendFormatted(format[new Range(start, end)]);
            if (details is not null)
            {
                message.AppendLiteral(Environment.NewLine);
                message.AppendLiteral("Details: ");
                message.AppendFormatted(details);
            }
            return new FormatException(message.ToString());
        }
    }
    
    public TBuilder Format(string format, params object?[] args)
    {
        WriteFormatLine(format.AsSpan(), args);
        return _builder;
    }

    public TBuilder FormatLine(string format, params object?[] args) => Format(format, args).NewLine();
    

#endregion


#region Enumerate
    // public TBuilder Enumerate(
    //     TextSplitEnumerable splitEnumerable,
    //     TextBuilderTextAction<TBuilder> perSplitSection)
    // {
    //     foreach (var splitSection in splitEnumerable)
    //     {
    //         perSplitSection(_builder, splitSection);
    //     }
    //     return _builder;
    // }

    public TBuilder Enumerate<T>(IEnumerable<T> values, TBA<TBuilder, T> perValue)
    {
        foreach (var value in values)
        {
            perValue(_builder, value);
        }
        return _builder;
    }

    public TBuilder Iterate<T>(IEnumerable<T> values, TBAI<TBuilder, T> perValueIndex)
    {
        if (values is IList<T> list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                perValueIndex(_builder, list[i], i);
            }
        }
        else
        {
            using var e = values.GetEnumerator();
            if (!e.MoveNext()) return _builder;
            int i = 0;
            perValueIndex(_builder, e.Current, i);
            while (e.MoveNext())
            {
                i++;
                perValueIndex(_builder, e.Current, i);
            }
        }
        return _builder;
    }

    public TBuilder EnumerateAppend<T>(IEnumerable<T> enumerable) =>
        Enumerate(enumerable, static (tb, v) => tb.Append(v));

    public TBuilder EnumerateAppendLines<T>(IEnumerable<T> enumerable) =>
        Enumerate(enumerable, static (tb, v) => tb.AppendLine(v));

    public TBuilder EnumerateLines<T>(IEnumerable<T> enumerable, TBA<TBuilder, T> perValue) =>
        Enumerate(enumerable,
            (tb, v) =>
            {
                perValue(tb, v);
                tb.NewLine();
            });
#endregion

#region Delimit
    // public TBuilder Delimit(
    //     ReadOnlySpan<char> delimiter,
    //     TextSplitEnumerable splitEnumerable,
    //     TextBuilderTextAction<TBuilder> perSplitSection)
    // {
    //     var splitEnumerator = splitEnumerable.GetEnumerator();
    //     if (!splitEnumerator.MoveNext()) return _builder;
    //     perSplitSection(_builder, splitEnumerator.Current);
    //     while (splitEnumerator.MoveNext())
    //     {
    //         Append(delimiter);
    //         perSplitSection(_builder, splitEnumerator.Current);
    //     }
    //     return _builder;
    // }

    // public TBuilder Delimit(
    //     TextBuilderAction<TBuilder> delimit,
    //     TextSplitEnumerable splitEnumerable,
    //     TextBuilderTextAction<TBuilder> perSplitSection)
    // {
    //     var splitEnumerator = splitEnumerable.GetEnumerator();
    //     if (!splitEnumerator.MoveNext()) return _builder;
    //     perSplitSection(_builder, splitEnumerator.Current);
    //     while (splitEnumerator.MoveNext())
    //     {
    //         delimit(_builder);
    //         perSplitSection(_builder, splitEnumerator.Current);
    //     }
    //     return _builder;
    // }


    public TBuilder Delimit<T>(TBA<TBuilder> delimit, IEnumerable<T> values, TBA<TBuilder, T> perValue)
    {
        if (values is IList<T> list)
        {
            var count = list.Count;
            if (count == 0) return _builder;
            perValue(_builder, list[0]);
            for (var i = 1; i < count; i++)
            {
                delimit(_builder);
                perValue(_builder, list[i]);
            }
        }
        else
        {
            using var e = values.GetEnumerator();
            if (!e.MoveNext()) return _builder;
            perValue(_builder, e.Current);
            while (e.MoveNext())
            {
                delimit(_builder);
                perValue(_builder, e.Current);
            }
        }

        return _builder;
    }

    public TBuilder Delimit<T>(TBA<TBuilder> delimit, IEnumerable<T> values, TBAI<TBuilder, T> perValueIndex)
    {
        if (values is IList<T> list)
        {
            var count = list.Count;
            if (count == 0) return _builder;
            perValueIndex(_builder, list[0], 0);
            for (var i = 1; i < count; i++)
            {
                delimit(_builder);
                perValueIndex(_builder, list[i], i);
            }
        }
        else
        {
            using var e = values.GetEnumerator();
            if (!e.MoveNext()) return _builder;
            int i = 0;
            perValueIndex(_builder, e.Current, i);
            while (e.MoveNext())
            {
                i++;
                delimit(_builder);
                perValueIndex(_builder, e.Current, i);
            }
        }

        return _builder;
    }

    public TBuilder Delimit<T>(string delimiter, IEnumerable<T> values, TBA<TBuilder, T> perValue)
    {
        return Delimit(w => w.Append(delimiter), values, perValue);
    }

    public TBuilder Delimit<T>(string delimiter, IEnumerable<T> values, TBAI<TBuilder, T> perValueIndex)
    {
        return Delimit(w => w.Append(delimiter), values, perValueIndex);
    }

    public TBuilder DelimitLines<T>(IEnumerable<T> values, TBA<TBuilder, T> perValue)
    {
        return Delimit(static w => w.NewLine(), values, perValue);
    }

    public TBuilder DelimitLines<T>(IEnumerable<T> values, TBAI<TBuilder, T> perValueIndex)
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
        return _builder;
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
            return _builder;
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
                    return _builder;
                }

                // Write our new text
                TextHelper.Unsafe.CopyTo(newText, written.Slice(writePos), newTextLen);
                writePos += newTextLen;
            }

            // Done
            Length = writePos;
            return _builder;
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
                    return _builder;
                }

                // Write our new text
                this.Write(newText);
                writePos += newTextLen;
                Debug.Assert(Length == writePos);
            }

            // Done
            //Length = writePos;
            Debug.Assert(Length == writePos);
            return _builder;
        }
    }

    public TBuilder Replace(string oldText, string? newText, StringComparison comparison = StringComparison.Ordinal)
        => Replace(oldText.AsSpan(), newText.AsSpan(), comparison);

#endregion

    #region Trim
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

        return _builder;
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
        return _builder;
    }
    #endregion

    public TBuilder Clear()
    {
        // Nice hack
        Length = 0;
        return _builder;
    }
    
    
    public TBuilder If(
        bool predicateResult,
        TBA<TBuilder>? ifTrue,
        TBA<TBuilder>? ifFalse = null
    )
    {
        if (predicateResult)
        {
            ifTrue?.Invoke(_builder);
        }
        else
        {
            ifFalse?.Invoke(_builder);
        }
        return _builder;
    }

    public TBuilder If(
        Func<bool> predicate,
        TBA<TBuilder>? ifTrue,
        TBA<TBuilder>? ifFalse = null
    )
    {
        if (predicate())
        {
            ifTrue?.Invoke(_builder);
        }
        else
        {
            ifFalse?.Invoke(_builder);
        }
        return _builder;
    }
}