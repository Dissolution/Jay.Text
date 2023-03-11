/*using System.Diagnostics.CodeAnalysis;

using Jay.Text.Code.CSharpCodeBuilderExtensions;

using Jay.Text.Utilities;

namespace Jay.Text.Code;

public sealed class CSBuilder : CSBuilder<CSBuilder>
{
    public CSBuilder() : base() { }
}

public delegate TReturn TBFunc<TBuilder, TReturn>(TBuilder builder)
    where TBuilder : TextBuilder<TBuilder>;
public delegate void TBAction<TBuilder>(TBuilder builder)
    where TBuilder : TextBuilder<TBuilder>;


public abstract class CSBuilder<TBuilder> : Buffer
    where TBuilder : CSBuilder<TBuilder>
{
    internal TBuilder Invoke(TBAction<TBuilder> action)
    {
        action?.Invoke(_this);
        return _this;
    }

    internal TBuilder EnsureOnStartOfNewLine()
    {
        if (!Written.EndsWith(_newLineIndent.AsSpan()))
        {
            return NewLine();
        }
        return _this;
    }



    public override TBuilder Format<T>([AllowNull] T arg, string? itemFormat = null, IFormatProvider? provider = null)
    {
        string? str;
        if (arg is IFormattable)
        {
#if NET6_0_OR_GREATER
            // If the value can format itself directly into our buffer, do so.
            if (arg is ISpanFormattable)
            {
                int charsWritten;
                // constrained call avoiding boxing for value types
                while (!((ISpanFormattable)arg).TryFormat(Available, out charsWritten, itemFormat.AsSpan(), provider))
                {
                    GrowBy(BuilderHelper.MinimumCapacity);
                }
                Length += charsWritten;
                return _this;
            }
#endif
            // constrained call avoiding boxing for value types
            str = ((IFormattable)arg).ToString(itemFormat, provider);
        }
        else
        {
            str = arg?.ToString();
        }

        return Code(str);
    }

    public TBuilder Format<T>(T? arg, ReadOnlySpan<char> itemFormat = default, IFormatProvider? provider = null)
    {
        string? str;
        if (arg is IFormattable)
        {
#if NET6_0_OR_GREATER
            // If the value can format itself directly into our buffer, do so.
            if (arg is ISpanFormattable)
            {
                int charsWritten;
                // constrained call avoiding boxing for value types
                while (!((ISpanFormattable)arg).TryFormat(Available, out charsWritten, itemFormat, provider))
                {
                    GrowBy(BuilderHelper.MinimumCapacity);
                }
                Length += charsWritten;
                return _this;
            }
#endif
            // constrained call avoiding boxing for value types
            // but we do have to allocate itemFormat
            str = ((IFormattable)arg).ToString(itemFormat.ToString(), provider);
        }
        else
        {
            str = arg?.ToString();
        }

        return Code(str);
    }

    internal void WriteFormatLine2(ReadOnlySpan<char> format, object?[] args)
    {
        // Even if args is empty, we still need to search and process {}'s

        int formatIndex = 0;

        // Repeatedly find the next hole and process it
        while (true)
        {
            char ch;

            // Skip until we find a possible argument hole or the end of input
            while (true)
            {
                // Always check for end of input
                if (formatIndex >= format.Length) return;

                ReadOnlySpan<char> formatRemainder = format.Slice(formatIndex);

                // Look for a brace
                int braceIndex = formatRemainder.IndexOfAny('{', '}');
                // If we did not find one, the rest of the formatLine is valid text
                if (braceIndex == -1)
                {
                    // Write it and we're done
                    this.Write(formatRemainder);
                    return;
                }

                // Write all the text before the brace
                this.Write(formatRemainder.Slice(0, braceIndex));
                // update our index to look at the brace
                formatIndex += braceIndex;

                // Get the brace
                char brace = format[formatIndex];
                // it must be followed by another character
                ch = getNextChar(format, ref formatIndex);

                // A copy of itself is an escape
                if (ch == brace)
                {
                    // Write a single brace
                    this.Write(brace);
                    // Move to the next char for the next loop
                    formatIndex++;
                    continue;
                }

                // If it wasn't an escape, it must be an opening brace
                if (brace != '{')
                {
                    // Orphaned closing brace
                    throw createFormatException(format, formatIndex, "Closing brace '}' is missing an opening brace '{'");
                }

                // We found a hole, parse it
                break;
            }

            /* We're now positioned just after the opening brace of an argument hole
             * Which looks like this:
             *  {#} or {#:FMT}
             * - An opening brace, a numeric index, and an optional format preceded by a colon
             * - Arbitrary spaces will be ignored
             #1#

            // First up is the index parameter,
            // which has at least one digits and any amount of whitespace surrounding them.
            // We've already read the first digit into ch.
            Debug.Assert(format[formatIndex - 1] == '{');
            Debug.Assert(ch != '{');
            // We can quickly convert it to a number
            int index = ch - '0';
            // Has to be between 0 and 9
            if ((uint)index >= 10U)
            {
                throw createFormatException(format, formatIndex, "Invalid character in argument index");
            }

            ReadOnlySpan<char> itemFormatSpan = default;

            // Common case is a single digit index followed by a closing brace.
            // If it's not a closing brace, proceed to finish parsing the full hole format.
            ch = getNextChar(format, ref formatIndex);
            if (ch != '}')
            {
                // Continue consuming optional additional digits.
                while (ch.IsAsciiDigit())
                {
                    // Shift by power of 10
                    index = (index * 10) + (ch - '0');
                    ch = getNextChar(format, ref formatIndex);
                }

                // Consume optional whitespace.
                while (ch == ' ')
                {
                    ch = getNextChar(format, ref formatIndex);
                }

                // We do not support alignment
                if (ch == ',')
                {
                    throw createFormatException(format, formatIndex, "Alignment is not supported");
                }

                // The next character needs to either be a closing brace for the end of the hole,
                // or a colon indicating the start of the format.
                if (ch != '}')
                {
                    if (ch != ':')
                    {
                        // Unexpected character
                        throw createFormatException(format, formatIndex, "Unexpected character in argument hole");
                    }

                    // Search for the closing brace; everything in between is the format,
                    // but opening braces aren't allowed.
                    int startingPos = formatIndex + 1;
                    while (true)
                    {
                        ch = getNextChar(format, ref formatIndex);

                        if (ch == '}')
                        {
                            // Argument hole closed
                            break;
                        }

                        if (ch == '{')
                        {
                            // Braces inside the argument hole are not supported
                            throw createFormatException(format, formatIndex, "Braces inside the argument hole are not supported");
                        }
                    }

                    // This is our item's format
                    var rangeSpan = format[new Range(start: startingPos, end: formatIndex)];
                    itemFormatSpan = format.Slice(startingPos, formatIndex - startingPos);
                    Debug.Assert(TextHelper.Equals(rangeSpan, itemFormatSpan));
                }
            }

            // Construct the output for this arg hole.
            Debug.Assert(format[formatIndex] == '}');
            formatIndex++;

            if ((uint)index >= (uint)args.Length)
            {
                throw createFormatException(format, formatIndex,
                    $"Invalid Format: Argument '{index}' does not exist");
            }

            object? arg = args[index];

            // Append this arg, allows for overridden behavior
            Format<object?>(arg, itemFormatSpan);

            // Continue parsing the rest of the format string.
        }




        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static char getNextChar(ReadOnlySpan<char> format, ref int formatIndex)
        {
            formatIndex++;
            if (formatIndex < format.Length)
                return format[formatIndex];
            throw createFormatException(format, formatIndex, "Attempted to move past final character");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static FormatException createFormatException(
            ReadOnlySpan<char> format,
            int formatIndex,
            string? details = null)
        {
            using var message = new CharSpanBuilder();
            message.WriteLine($"Invalid Format at position {formatIndex}");

            // Write the previous and next 16 characters around the format error position
            int start = (formatIndex - 16).Clamp(0);
            int end = (formatIndex + 16).Clamp(0, format.Length);
            var segment = format[new Range(start, end)];
            message.WriteLine(segment);
            // Add start spaces
            message.Repeat(' ', start);
            // And then point to the position char
            message.Write("^__here");

            if (details is not null)
            {
                message.WriteLine();
                message.WriteLine();
                message.Write("Details: ");
                message.Write(details);
            }
            return new FormatException(message.ToString());
        }
    }

    public TBuilder BracketBlock(TextBuilderAction<TBuilder> bracketBlock, string? indent = null)
    {
        indent ??= "    ";
        // Trim all trailing whitespace
        return TrimEnd()
            // Start a new line
            .NewLine()
            // Starting bracket
            .AppendLine('{')
            // Starts an indented block inside of that bracket
            .IndentBlock(indent, bracketBlock)
            // Be sure that we're not putting the end bracket at the end of text
            .EnsureOnStartOfNewLine()
            // Ending bracket
            .Append('}');
    }


    public TBuilder Member(
        TBAction<TBuilder> buildDeclaration,
        TextBuilderAction<TBuilder> buildBody)
    {
        return Invoke(buildDeclaration)
            .BracketBlock(buildBody)
            .NewLine();
    }

    public TBuilder Member(TBFunc<TBuilder, string> declaration,
        TBAction<TBuilder> buildBody)
    {
        return Invoke(tb => tb.Append(declaration(tb)))
            .BracketBlock(buildBody)
            .NewLine();
    }


    public TBuilder Code(NonFormattableString code)
    {
        IndentAwareSplitWrite(code.Text, line => this.Write(line));
        return _this;
    }

    public TBuilder Code(FormattableString code)
    {
        var args = code.GetArguments();
        IndentAwareSplitWrite(code.Format.AsSpan(), line => WriteFormatLine2(line, args));
        return _this;
    }

    public delegate void LineAction(ReadOnlySpan<char> line);

    protected internal void IndentAwareSplitWrite(ReadOnlySpan<char> text, LineAction lineAction)
    {
        var splitList = text.TextSplit(TextHelper.NewLineSpan).ToList();
        if (splitList.Count == 0) return;

        var first = splitList.Text(0);

        // Does first look indented?
        if (first.StartsWith(char.IsWhiteSpace))
        {
            var cnli = CurrentNewLineIndent();

            // Get indent
            var rdr = new CharSpanReader(first);
            // All the whitespace is the indent this line was expecting
            var indent = rdr.TakeWhiteSpace();

            // if The cnli ends with indent, 
            if (cnli.AsSpan().EndsWith(indent))
            {
                // we need to overlap
                Length -= indent.Length;
                // And this is incoming
                var eq = TextHelper.Equals(cnli, _newLineIndent);
                if (!eq) Debugger.Break();
                var newIndent = _newLineIndent.AsSpan(0, _newLineIndent.Length - indent.Length).ToString();
                _newLineIndent = newIndent;
            }
        }

        for (var i = 0; i < splitList.Count; i++)
        {
            // Between lines, add our newline with indent
            if (i > 0)
                NewLine();
            // Do our line action
            lineAction(splitList.Text(i));
        }

        // Does the last end with whitespace? Could indicate a possible indent change
        var last = splitList.Text(splitList.Count - 1);
        if (last.EndsWith(char.IsWhiteSpace))
        {
            // Can we determine a previous indent?
            var lastNewLineIndex = Written.LastIndexOf(TextHelper.NewLineSpan);
            if (lastNewLineIndex != -1)
            {
                var prev = Written.Slice(lastNewLineIndex);
                var rdr = new CharSpanReader(prev);
                // All the whitespace is our new indent
                var indent = rdr.TakeWhiteSpace();
                // Different?
                if (!TextHelper.Equals(indent, _newLineIndent))
                {
                    _newLineIndent = indent.ToString();
                }
                else
                {
                    // No need to change the indent
                    Debugger.Break();
                }
            }
            else
            {
                // Not an indent
                Debugger.Break();
            }
        }
        return;
    }
}*/