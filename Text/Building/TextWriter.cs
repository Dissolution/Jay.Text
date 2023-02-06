using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using Jay.Text.Extensions;

namespace Jay.Text.Building
{
    public delegate void TWA(TextWriter textWriter);

    public delegate void TWA<in T>(TextWriter textWriter, T value);

    public delegate void TWAI<in T>(TextWriter textWriter, T value, int index);

    public sealed class TextWriter : IDisposable
    {
        private readonly string _defaultNewLine = Environment.NewLine;

        private readonly CharArrayWriter _writer;
        private string _newLineIndent;

        public TextWriter()
        {
            _writer = new CharArrayWriter();
            _newLineIndent = _defaultNewLine;
        }


        internal string CurrentNewLineIndent()
        {
            var lastNewLineIndex = _writer.Written.LastIndexOf(_defaultNewLine.AsSpan());
            if (lastNewLineIndex == -1)
                return _defaultNewLine;
            return _writer.Written.Slice(lastNewLineIndex).ToString();
        }


        /// <summary>
        /// Writes a new line (<c>Options.NewLine</c> + current indent)
        /// </summary>
        public TextWriter NewLine()
        {
            _writer.Write(_newLineIndent);
            return this;
        }

        /// <summary>
        /// Writes a new line (<c>Options.NewLine</c> + current indent)
        /// </summary>
        public TextWriter NewLine(int count)
        {
            for (var i = 0; i < count; i++)
            {
                _writer.Write(_newLineIndent);
            }

            return this;
        }

        public TextWriter Write(char ch)
        {
            _writer.Write(ch);
            return this;
        }

        public TextWriter WriteLine(char ch)
        {
            return Write(ch).NewLine();
        }

        public TextWriter WriteSpan(ReadOnlySpan<char> text)
        {
            int textLen = text.Length;
            if (textLen == 0) return this;

            // We're going to be splitting on NewLine
            ReadOnlySpan<char> newLine = _defaultNewLine.AsSpan();

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
                        _writer.Write(text.Slice(sliceStart, sliceLen));
                        // Write current NewLine
                        _writer.Write(_newLineIndent);
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
                _writer.Write(text.Slice(sliceStart, sliceLen));
            }

            return this;
        }

#if NET6_0_OR_GREATER
        public TextWriter Write(ReadOnlySpan<char> text)
        {
            return WriteSpan(text);
        }

        public TextWriter Write(string? text)
        {
            return WriteSpan(text);
        }

        public TextWriter Write(
            [InterpolatedStringHandlerArgument("")]
            ref InterpolatedTextWriter interpolatedString)
        {
            // The writing has already happened by the time we get into this method!
            return this;
        }

#else
        internal TextWriter WriteFormatChunk(ReadOnlySpan<char> format, ReadOnlySpan<object?> args)
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
                        return this;
                    }

                    ReadOnlySpan<char> remainder = format.Slice(pos);
                    int countUntilNextBrace = remainder.IndexOfAny('{', '}');
                    if (countUntilNextBrace < 0)
                    {
                        _writer.Write(remainder);
                        return this;
                    }

                    // Append the text until the brace.
                    _writer.Write(remainder.Slice(0, countUntilNextBrace));
                    pos += countUntilNextBrace;

                    // Get the brace.
                    // It must be followed by another character, either a copy of itself in the case of being escaped,
                    // or an arbitrary character that's part of the hole in the case of an opening brace.
                    char brace = format[pos];
                    ch = MoveNext(format, ref pos);
                    if (brace == ch)
                    {
                        _writer.Write(ch);
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
                _writer.WriteFormat<object?>(arg, itemFormat);

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
                using var message = new CharSpanWriter();
                message.Write("Invalid Format at position ");
                message.WriteValue(pos);
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


        public TextWriter Write(FormattableString text)
        {
            ReadOnlySpan<char> format = text.Format.AsSpan();
            int formatLen = format.Length;
            object?[] formatArgs = text.GetArguments();

            // We're going to be splitting on NewLine
            ReadOnlySpan<char> newLine = _defaultNewLine.AsSpan();

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
                        _writer.Write(_newLineIndent);
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

            return this;
        }

        public TextWriter Write(NonFormattableString text)
        {
            return WriteSpan(text.CharSpan);
        }
#endif

        public TextWriter WriteValue<T>(T? value, string? format = null)
        {
            switch (value)
            {
                case null:
                {
                    return this;
                }
                // TWA support for neat tricks
                case TWA textWriterAction:
                {
                    var oldIndent = _newLineIndent;
                    var currentIndent = CurrentNewLineIndent();
                    _newLineIndent = currentIndent;
                    textWriterAction(this);
                    _newLineIndent = oldIndent;
                    return this;
                }
                case string str:
                {
                    _writer.Write(str);
                    return this;
                }
                case IFormattable formattable:
                {
                    _writer.WriteFormat(formattable, format);
                    return this;
                }
                case IEnumerable enumerable:
                {
                    format ??= ",";
                    return Delimit(format, enumerable.Cast<object?>(), static (w, v) => w._writer.WriteValue<object?>(v));
                }
                default:
                {
                    _writer.Write(value?.ToString());
                    return this;
                }
            }
        }

        public TextWriter WriteValueLine<T>(T? value) => WriteValue<T>(value).NewLine();


        public TextWriter IndentBlock(string indent, TWA indentBlock)
        {
            var oldIndent = _newLineIndent;
            // We might be on a new line, but not yet indented
            if (CurrentNewLineIndent() == oldIndent)
            {
                Write(indent);
            }

            var newIndent = oldIndent + indent;
            _newLineIndent = newIndent;
            indentBlock(this);
            _newLineIndent = oldIndent;
            // Did we do a newline that we need to decrease?
            if (_writer.Written.EndsWith(newIndent.AsSpan()))
            {
                _writer.Length -= newIndent.Length;
                return Write(oldIndent);
            }

            return this;
        }

#region Enumerate

        public TextWriter Enumerate<T>(IEnumerable<T> values, TWA<T> perValue)
        {
            foreach (var value in values)
            {
                perValue(this, value);
            }

            return this;
        }

        public TextWriter Enumerate<T>(IEnumerable<T> values, TWAI<T> perValueIndex)
        {
            if (values is IList<T> list)
            {
                for (var i = 0; i < list.Count; i++)
                {
                    perValueIndex(this, list[i], i);
                }
            }
            else
            {
                using var e = values.GetEnumerator();
                if (!e.MoveNext()) return this;
                int i = 0;
                perValueIndex(this, e.Current, i);
                while (e.MoveNext())
                {
                    i++;
                    perValueIndex(this, e.Current, i);
                }
            }

            return this;
        }

        public TextWriter EnumerateWrite<T>(IEnumerable<T> enumerable) =>
            Enumerate(enumerable, static (w, v) => w.WriteValue<T>(v));

        public TextWriter EnumerateWriteLines<T>(IEnumerable<T> enumerable) =>
            Enumerate(enumerable, static (cw, v) => cw.WriteValueLine<T>(v));

        public TextWriter EnumerateLines<T>(IEnumerable<T> enumerable, TWA<T> perValue) =>
            Enumerate(enumerable, (cw, v) =>
            {
                perValue(cw, v);
                NewLine();
            });


        public TextWriter Delimit<T>(TWA delimit, IEnumerable<T> values, TWA<T> perValue)
        {
            if (values is IList<T> list)
            {
                var count = list.Count;
                if (count == 0) return this;
                perValue(this, list[0]);
                for (var i = 1; i < count; i++)
                {
                    delimit(this);
                    perValue(this, list[i]);
                }
            }
            else
            {
                using var e = values.GetEnumerator();
                if (!e.MoveNext()) return this;
                perValue(this, e.Current);
                while (e.MoveNext())
                {
                    delimit(this);
                    perValue(this, e.Current);
                }
            }

            return this;
        }

        public TextWriter Delimit<T>(TWA delimit, IEnumerable<T> values, TWAI<T> perValueIndex)
        {
            if (values is IList<T> list)
            {
                var count = list.Count;
                if (count == 0) return this;
                perValueIndex(this, list[0], 0);
                for (var i = 1; i < count; i++)
                {
                    delimit(this);
                    perValueIndex(this, list[i], i);
                }
            }
            else
            {
                using var e = values.GetEnumerator();
                if (!e.MoveNext()) return this;
                int i = 0;
                perValueIndex(this, e.Current, i);
                while (e.MoveNext())
                {
                    i++;
                    delimit(this);
                    perValueIndex(this, e.Current, i);
                }
            }

            return this;
        }

        public TextWriter Delimit<T>(string delimiter, IEnumerable<T> values, TWA<T> perValue)
        {
            // Check delimiter for special cases
            if (delimiter == _defaultNewLine)
            {
                return Delimit(static w => w.NewLine(), values, perValue);
            }

            return Delimit(w => w.Write(delimiter), values, perValue);
        }

        public TextWriter Delimit<T>(string delimiter, IEnumerable<T> values, TWAI<T> perValueIndex)
        {
            // Check delimiter for special cases
            if (delimiter == _defaultNewLine)
            {
                return Delimit(static w => w.NewLine(), values, perValueIndex);
            }

            return Delimit(w => w.Write(delimiter), values, perValueIndex);
        }

        public TextWriter DelimitLines<T>(IEnumerable<T> values, TWA<T> perValue)
        {
            return Delimit(static w => w.NewLine(), values, perValue);
        }

        public TextWriter DelimitLines<T>(IEnumerable<T> values, TWAI<T> perValueIndex)
        {
            return Delimit(w => w.NewLine(), values, perValueIndex);
        }

#endregion

        /// <summary>
        /// Ensures that the writer is on the beginning of a new line
        /// </summary>
        public TextWriter EnsureOnNewLine()
        {
            if (!_writer.Written.EndsWith(_newLineIndent.AsSpan()))
            {
                return NewLine();
            }

            return this;
        }

        public TextWriter TrimEnd(string trimStr)
        {
            if (_writer.Written.EndsWith(trimStr.AsSpan()))
            {
                _writer.Length -= trimStr.Length;
            }

            return this;
        }

        public void Dispose()
        {
            _writer.Dispose();
        }

        public override string ToString()
        {
            return _writer.ToString();
        }
    }
}