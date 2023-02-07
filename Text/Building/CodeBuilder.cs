using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Jay.Text;


public sealed class CodeBuilder : CodeBuilder<CodeBuilder>
{
    public CodeBuilder() : base() { }
}

public abstract class CodeBuilder<TBuilder> : TextBuilder<TBuilder>
    where TBuilder : CodeBuilder<TBuilder>
{
    protected string _newLineIndent;

    protected CodeBuilder()
        : base()
    {
        _newLineIndent = DefaultNewLine;
    }

    protected string CurrentNewLineIndent()
    {
        var lastNewLineIndex = Written.LastIndexOf(DefaultNewLine.AsSpan());
        if (lastNewLineIndex == -1)
            return DefaultNewLine;
        return Written.Slice(lastNewLineIndex).AsString();
    }

    public override TBuilder NewLine()
    {
        AppendNonNullString(_newLineIndent);
        return _this;
    }

    public override TBuilder Write(string? str)
    {
        return Write(str.AsSpan());
    }

    public override TBuilder Write(ReadOnlySpan<char> text)
    {
        int textLen = text.Length;
        if (textLen == 0) return _this;

        // We're going to be splitting on NewLine
        ReadOnlySpan<char> newLine = DefaultNewLine.AsSpan();

        // Index search
        int sliceStart = 0;
        // If @ was used (literal string), there might be a leading newline, clean it up
        if (text.StartsWith(newLine))
            sliceStart = newLine.Length;

        // Slice
        int index = sliceStart;
        int sliceLen;
        while (index < textLen)
        {
            var nli = text.Slice(index).IndexOf(newLine);
            if (nli >= 0)
            {
                index = (nli + index);
                sliceLen = index - sliceStart;
                if (sliceLen > 0)
                {
                    // Write this chunk
                    AppendCharSpan(text.Slice(sliceStart, sliceLen));
                    // Write current NewLine
                    NewLine();
                }

                // Skip this newline
                sliceStart = index + newLine.Length;
                index = sliceStart;
            }
            else
            {
                break;
            }
        }

        // Anything left?
        sliceLen = textLen - sliceStart;
        if (sliceLen > 0)
        {
            // write it
            AppendCharSpan(text.Slice(sliceStart, sliceLen));
        }

        return _this;
    }

#if NET6_0_OR_GREATER
    public TBuilder WriteFormat(
        [InterpolatedStringHandlerArgument("")]
        ref InterpolatedTextBuilder<TBuilder> interpolatedString)
    {
        // The writing has already happened by the time we get into this method!
        return _this;
    }
#else
    internal void WriteFormatChunk(ReadOnlySpan<char> format, ReadOnlySpan<object?> args)
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
                ch = MoveNext(format, ref pos);
                if (brace == ch)
                {
                    AppendChar(ch);
                    pos++;
                    continue;
                }

                // This wasn't an escape, so it must be an opening brace.
                if (brace != '{')
                {
                    ThrowFormatException(format, pos, "Missing opening brace");
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
                ThrowFormatException(format, pos, "Invalid character in index");
            }

            // Common case is a single digit index followed by a closing brace.  If it's not a closing brace,
            // proceed to finish parsing the full hole format.
            ch = MoveNext(format, ref pos);
            if (ch != '}')
            {
                // Continue consuming optional additional digits.
                while (ch.IsAsciiDigit() && index < IndexLimit)
                {
                    // Shift by power of 10
                    index = index * 10 + (ch - '0');
                    ch = MoveNext(format, ref pos);
                }

                // Consume optional whitespace.
                while (ch == ' ')
                {
                    ch = MoveNext(format, ref pos);
                }

                // We do not support alignment
                if (ch == ',')
                {
                    ThrowFormatException(format, pos, "Alignment is not supported");
                }

                // The next character needs to either be a closing brace for the end of the hole,
                // or a colon indicating the start of the format.
                if (ch != '}')
                {
                    if (ch != ':')
                    {
                        // Unexpected character
                        ThrowFormatException(format, pos, "Unexpected character");
                    }

                    // Search for the closing brace; everything in between is the format,
                    // but opening braces aren't allowed.
                    int startingPos = pos;
                    while (true)
                    {
                        ch = MoveNext(format, ref pos);

                        if (ch == '}')
                        {
                            // Argument hole closed
                            break;
                        }

                        if (ch == '{')
                        {
                            // Braces inside the argument hole are not supported
                            ThrowFormatException(format, pos, "Braces inside the argument hole are not supported");
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
            AppendFormat<object?>(arg, itemFormat);

            // Continue parsing the rest of the format string.
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static char MoveNext(ReadOnlySpan<char> format, ref int pos)
        {
            pos++;
            if (pos >= format.Length)
            {
                ThrowFormatException(format, pos, "Ran out of room");
            }

            return format[pos];
        }

        [DoesNotReturn]
        static void ThrowFormatException(ReadOnlySpan<char> format, int pos, string? details = null)
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


    public TBuilder Format(FormattableString text)
    {
        ReadOnlySpan<char> format = text.Format.AsSpan();
        int formatLen = format.Length;
        object?[] formatArgs = text.GetArguments();

        // We're going to be splitting on NewLine
        ReadOnlySpan<char> newLine = DefaultNewLine.AsSpan();

        // Index search
        int sliceStart = 0;
        // If @ was used (literal string), there might be a leading newline, clean it up
        if (format.StartsWith(newLine))
            sliceStart = newLine.Length;

        // Slice
        int index = sliceStart;
        int sliceLen;
        while (index < formatLen)
        {
            var nli = format.Slice(index).IndexOf(newLine);
            if (nli >= 0)
            {
                index = (nli + index);
                sliceLen = index - sliceStart;
                if (sliceLen > 0)
                {
                    // Write this chunk
                    WriteFormatChunk(format.Slice(sliceStart, sliceLen), formatArgs);
                    // Write current NewLine
                    NewLine();
                }

                // Skip this newline
                sliceStart = index + newLine.Length;
                index = sliceStart;
            }
            else
            {
                break;
            }
        }

        // Anything left?
        sliceLen = formatLen - sliceStart;
        if (sliceLen > 0)
        {
            // write it
            WriteFormatChunk(format.Slice(sliceStart, sliceLen), formatArgs);
        }

        return _this;
    }
#endif

    public override TBuilder Write<T>([AllowNull] T value)
    {
        return Format<T>(value, default);
    }
    public override TBuilder Format<T>([AllowNull] T value, string? format)
    {
        switch (value)
        {
            case null:
            {
                return _this;
            }
            // CBA support for neat tricks
            case TextBuilderAction<TBuilder> textBuilderAction:
            {
                var oldIndent = _newLineIndent;
                var currentIndent = CurrentNewLineIndent();
                _newLineIndent = currentIndent;
                textBuilderAction(_this);
                _newLineIndent = oldIndent;
                return _this;
            }
            case string str:
            {
                AppendNonNullString(str);
                return _this;
            }
            case IFormattable formattable:
            {
                AppendFormat(formattable, format);
                return _this;
            }
            case IEnumerable enumerable:
            {
                format ??= ",";
                return Delimit(format, enumerable.Cast<object?>(), static (w, v) => w.AppendValue<object?>(v));
            }
            default:
            {
                AppendString(value?.ToString());
                return _this;
            }
        }
    }

    public TBuilder IndentBlock(string indent, TextBuilderAction<TBuilder> indentBlock)
    {
        var oldIndent = _newLineIndent;
        // We might be on a new line, but not yet indented
        if (CurrentNewLineIndent() == oldIndent)
        {
            AppendNonNullString(indent);
        }

        var newIndent = oldIndent + indent;
        _newLineIndent = newIndent;
        indentBlock(_this);
        _newLineIndent = oldIndent;
        // Did we do a newline that we need to decrease?
        if (Written.EndsWith(newIndent.AsSpan()))
        {
            Length -= newIndent.Length;
            AppendNonNullString(oldIndent);
        }
        return _this;
    }
}