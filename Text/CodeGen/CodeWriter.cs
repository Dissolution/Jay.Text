using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Jay.Text.CodeGen;

/// <summary>
/// <see cref="CodeWriter"/> <see cref="Action"/>
/// </summary>
// ReSharper disable once InconsistentNaming
public delegate void CWA(CodeWriter writer);

/// <summary>
/// An <c>Action&lt;<see cref="CodeWriter"/>, <typeparamref name="T"/>&gt;</c> delegate
/// </summary>
/// <typeparam name="T">The type of <paramref name="value"/> passed to the action</typeparam>
/// <param name="writer">An instance of a <see cref="CodeWriter"/></param>
/// <param name="value">A stateful <typeparamref name="T"/> value</param>
// ReSharper disable once InconsistentNaming
public delegate void CWA<in T>(CodeWriter writer, T value);

public delegate void CWAI<in T>(CodeWriter writer, T value, int index);

public sealed class CodeWriter : IDisposable
{
    private readonly string _defaultNewLine = Environment.NewLine;
    private const string _defaultIndent = "    "; // 4 spaces 

    private readonly TextBuilder _writer;
    private string _newLineIndent;

    public CodeWriter()
    {
        _writer = new TextBuilder();
        _newLineIndent = _defaultNewLine;
    }

    #region Write / WriteLine / NewLine

    public CodeWriter Write(char ch)
    {
        _writer.Write(ch);
        return this;
    }

    public CodeWriter WriteLine(char ch)
    {
        return Write(ch).NewLine();
    }

    public CodeWriter Write(string? text)
    {
        _writer.Write(text);
        return this;
    }

    public CodeWriter WriteLine(string? text)
    {
        return Write(text).NewLine();
    }

    public CodeWriter Write(ReadOnlySpan<char> text)
    {
        _writer.Write(text);
        return this;
    }

    public CodeWriter WriteLine(ReadOnlySpan<char> text)
    {
        return Write(text).NewLine();
    }

    public CodeWriter Write<T>(T? value, string? format = null)
    {
        switch (value)
        {
            case null:
            {
                return this;
            }
            // CWA support for neat tricks
            case CWA codeWriterAction:
            {
                var oldIndent = _newLineIndent;
                var currentIndent = CurrentNewLineIndent();
                _newLineIndent = currentIndent;
                codeWriterAction(this);
                _newLineIndent = oldIndent;
                return this;
            }
            case string str:
            {
                return CodeBlock(str);
            }
            case IFormattable formattable:
            {
                return Write(formattable.ToString(format, default));
            }
            case IEnumerable enumerable:
            {
                if (!string.IsNullOrEmpty(format))
                {
                    return Delimit(format!, enumerable.Cast<object?>(), static (w, v) => w.Write(v));
                }
                else
                {
                    return EnumerateWrite(enumerable.Cast<object?>());
                }
            }
            default:
            {
                var tType = typeof(T);
                var valueType = value?.GetType();

                Debugger.Break();
                return Write(value?.ToString());
            }
        }
    }

    public CodeWriter WriteLine<T>(T? value, string? format = null)
    {
        return Write(value, format).NewLine();
    }

    /// <summary>
    /// Writes a new line (<c>Options.NewLine</c> + current indent)
    /// </summary>
    public CodeWriter NewLine()
    {
        return Write(_newLineIndent);
    }

    /// <summary>
    /// Writes a new line (<c>Options.NewLine</c> + current indent)
    /// </summary>
    public CodeWriter NewLines(int count)
    {
        for (var i = 0; i < count; i++)
        {
            NewLine();
        }

        return this;
    }

    #endregion

    #region CodeBlock

    public CodeWriter CodeBlock(ref InterpolatedTextBuilderHandler textHandler)
    {

    }

    public CodeWriter CodeBlock(NonFormattableString nonFormattableString)
    {
        ReadOnlySpan<char> text = nonFormattableString.CharSpan;
        int textLen = text.Length;

        ReadOnlySpan<char> newLine = Environment.NewLine.AsSpan();
        int newLineLen = newLine.Length;

        int start = 0;
        // if @ was used, there might be a leading newline, which we'll clean up
        if (text.StartsWith(newLine))
            start = newLineLen;

        // Slice up our text
        int index = start;

        int len;
        while (index < textLen)
        {
            if (text.Slice(index).StartsWith(newLine))
            {
                len = index - start;
                if (len > 0)
                {
                    // Write this chunk, then a new line
                    Write(text.Slice(start, len));
                    NewLine();
                }

                // Skip ahead of this whitespace
                start = index + newLine.Length;
                index = start;
            }
            else
            {
                // Just text
                index++;
            }
        }

        // Anything left?
        len = index - start;
        if (len > 0)
        {
            // Write this chunk, then a new line
            Write(text.Slice(start, len));
            //NewLine();
        }

        return this;
    }

    internal CodeWriter WriteFormatChunk(ReadOnlySpan<char> format, ReadOnlySpan<object?> args)
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
                if ((uint)pos >= (uint)format.Length)
                {
                    return this;
                }

                ReadOnlySpan<char> remainder = format.Slice(pos);
                int countUntilNextBrace = remainder.IndexOfAny('{', '}');
                if (countUntilNextBrace < 0)
                {
                    return Write(remainder);
                }

                // Append the text until the brace.
                Write(remainder.Slice(0, countUntilNextBrace));
                pos += countUntilNextBrace;

                // Get the brace.
                // It must be followed by another character, either a copy of itself in the case of being escaped,
                // or an arbitrary character that's part of the hole in the case of an opening brace.
                char brace = format[pos];
                ch = MoveNext(format, ref pos);
                if (brace == ch)
                {
                    Write(ch);
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
                while (IsAsciiDigit(ch) && index < IndexLimit)
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
            Write<object?>(arg, itemFormat);

            // Continue parsing the rest of the format string.
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static char MoveNext(ReadOnlySpan<char> format, ref int pos)
        {
            pos++;
            if ((uint)pos >= (uint)format.Length)
            {
                ThrowFormatException(format, pos, "Ran out of room");
            }

            return format[pos];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsAsciiDigit(char ch)
        {
            return (uint)(ch - '0') <= '9' - '0';
        }

        [DoesNotReturn]
        static void ThrowFormatException(ReadOnlySpan<char> format, int pos, string? details = null)
        {
            var message = new StringBuilder()
                .Append("Invalid Format at position ").Append(pos).AppendLine()
                .Append(
                    $"{format.SafeSlice(pos, -16).ToString()}→{format[pos]}←{format.SafeSlice(pos + 1, 16).ToString()}");
            if (details is not null)
            {
                message.AppendLine()
                    .Append("Details: ").Append(details);
            }

            throw new FormatException(message.ToString());
        }
    }

    public CodeWriter CodeBlock(FormattableString formattableString)
    {
        ReadOnlySpan<char> format = formattableString.Format.AsSpan();
        int formatLen = format.Length;
        object?[] formatArgs = formattableString.GetArguments();

        ReadOnlySpan<char> newLine = _defaultNewLine.AsSpan();
        int newLineLen = newLine.Length;

        int start = 0;
        // if @ was used, there might be a leading newline, which we'll clean up
        if (format.StartsWith(newLine))
            start = newLineLen;

        // Slice up our text
        int index = start;

        int len;
        while (index < formatLen)
        {
            // If we're at a newline, we split here
            if (format.Slice(index).StartsWith(newLine))
            {
                len = index - start;
                if (len > 0)
                {
                    // Write this chunk, then a new line
                    WriteFormatChunk(format.Slice(start, len), formatArgs);
                }

                NewLine();

                // Skip ahead of this whitespace
                start = index + newLine.Length;
                index = start;
            }
            else
            {
                // Just text
                index++;
            }
        }

        // Anything left?
        len = index - start;
        if (len > 0)
        {
            // Write this chunk, then a new line
            WriteFormatChunk(format.Slice(start, len), formatArgs);
            NewLine();
        }

        return this;
    }

    #endregion

    #region Fluent CS File

    /// <summary>
    /// Adds the `// &lt;auto-generated/&gt; ` line, optionally expanding it to include a <paramref name="comment"/>
    /// </summary>
    public CodeWriter AutoGeneratedHeader(string? comment = null)
    {
        if (comment is null)
        {
            return WriteLine("// <auto-generated/>");
        }

        var lines = comment.AsSpan().SplitLines();
        return WriteLine("// <auto-generated>")
            .IndentBlock("// ", ib =>
            {
                foreach (var (start, length) in lines)
                {
                    ib.WriteLine(comment.AsSpan(start, length));
                }
            })
            .WriteLine("// </auto-generated>");
    }

    public CodeWriter Nullable(bool enable)
    {
        return Write("#nullable ")
            .Write(enable ? "enable" : "disable")
            .NewLine();
    }

    /// <summary>
    /// Writes a `using <paramref name="nameSpace"/>;` line
    /// </summary>
    public CodeWriter Using(string nameSpace)
    {
        ReadOnlySpan<char> ns = nameSpace.AsSpan();
        ns = ns.TrimStart("using ".AsSpan()).TrimEnd(';');
        return Write("using ").Write(ns).Write(';').NewLine();
    }

    /// <summary>
    /// Writes multiple <see cref="Using(string)"/> <paramref name="namespaces"/>
    /// </summary>
    public CodeWriter Using(params string[] namespaces)
    {
        foreach (var nameSpace in namespaces)
        {
            Using(nameSpace);
        }

        return this;
    }

    public CodeWriter Namespace(string? nameSpace)
    {
        if (!string.IsNullOrWhiteSpace(nameSpace))
        {
            return Write("namespace ").Write(nameSpace).WriteLine(';');
        }

        return this;
    }


    /// <summary>
    /// Writes the given <paramref name="comment"/> as a comment line / lines
    /// </summary>
    public CodeWriter Comment(string? comment)
    {
        /* Most of the time, this is probably a single line.
         * But we do want to watch out for newline characters to turn
         * this into a multi-line comment */
        var lines = comment.AsSpan().SplitLines();
        switch (lines.Count)
        {
            case 0:
                // Empty comment
                return WriteLine("// ");
            case 1:
                // Single line
                return Write("// ").WriteLine(comment);
            default:
            {
                using var e = lines.GetEnumerator();
                e.MoveNext();
                Write("/* ").WriteLine(comment.AsSpan(e.Current.start, e.Current.length));
                while (e.MoveNext())
                {
                    Write(" * ").WriteLine(comment.AsSpan(e.Current.start, e.Current.length));
                }

                return WriteLine(" */");
            }
        }
    }

    public CodeWriter Comment(string? comment, CommentType commentType)
    {
        var lines = comment.AsSpan().SplitLines();
        if (commentType == CommentType.SingleLine)
        {
            foreach (var (start, length) in lines)
            {
                Write("// ").WriteLine(comment.AsSpan(start, length));
            }
        }
        else if (commentType == CommentType.XML)
        {
            foreach (var (start, length) in lines)
            {
                Write("/// ").WriteLine(comment.AsSpan(start, length));
            }
        }
        else
        {
            using var e = lines.GetEnumerator();
            e.MoveNext();
            Write("/* ").WriteLine(comment.AsSpan(e.Current.start, e.Current.length));
            while (e.MoveNext())
            {
                Write(" * ").WriteLine(comment.AsSpan(e.Current.start, e.Current.length));
            }

            return WriteLine(" */");
        }

        return this;
    }

    #endregion

    #region Blocks

    public CodeWriter BracketBlock(CWA bracketBlock, string indent = _defaultIndent)
    {
        _writer.TrimEndWhiteSpace();
        return NewLine()
            .WriteLine('{')
            .IndentBlock(indent, bracketBlock)
            .EnsureOnNewLine()
            .Write('}');
    }

    public CodeWriter IndentBlock(string indent, CWA indentBlock)
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
        // Did we do a nl that we need to decrease?
        if (_writer.Written.EndsWith(newIndent.AsSpan()))
        {
            _writer.Length -= newIndent.Length;
            return Write(oldIndent);
        }

        return this;
    }

    #endregion


    #region Enumerate

    public CodeWriter Enumerate<T>(IEnumerable<T> values, CWAI<T> perValue)
    {
        if (values is IList<T> list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                perValue(this, list[i], i);
            }
        }
        else
        {
            using var e = values.GetEnumerator();
            if (!e.MoveNext()) return this;
            int i = 0;
            perValue(this, e.Current, i);
            while (e.MoveNext())
            {
                perValue(this, e.Current, ++i);
            }
        }

        return this;
    }

    public CodeWriter Enumerate<T>(IEnumerable<T> values, CWA<T> perValue)
    {
        foreach (var value in values)
        {
            perValue(this, value);
        }

        return this;
    }

    public CodeWriter EnumerateWrite<T>(IEnumerable<T> enumerable) =>
        Enumerate(enumerable, static (cw, v) => cw.Write(v));

    public CodeWriter EnumerateWriteLines<T>(IEnumerable<T> enumerable) =>
        Enumerate(enumerable, static (cw, v) => cw.WriteLine(v));

    public CodeWriter EnumerateLines<T>(IEnumerable<T> enumerable, CWA<T> perValue) => Enumerate(enumerable, (cw, v) =>
    {
        perValue(cw, v);
        NewLine();
    });


    public CodeWriter Delimit<T>(CWA delimit, IEnumerable<T> values, CWAI<T> perValue)
    {
        if (values is IList<T> list)
        {
            var count = list.Count;
            if (count == 0) return this;
            perValue(this, list[0], 0);
            for (var i = 1; i < count; i++)
            {
                delimit(this);
                perValue(this, list[i], i);
            }
        }
        else
        {
            using var e = values.GetEnumerator();
            if (!e.MoveNext()) return this;
            int i = 0;
            perValue(this, e.Current, i);
            while (e.MoveNext())
            {
                delimit(this);
                perValue(this, e.Current, ++i);
            }
        }

        return this;
    }

    public CodeWriter Delimit<T>(CWA delimit, IEnumerable<T> values, CWA<T> perValue)
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


    public CodeWriter Delimit<T>(string delimiter, IEnumerable<T> values, CWA<T> perValue)
    {
        // Check delimiter for special cases
        if (delimiter == _defaultNewLine)
        {
            return Delimit(w => w.NewLine(), values, perValue);
        }
        else if (delimiter.Contains(_defaultNewLine))
        {
            return Delimit(w => w.CodeBlock(delimiter), values, perValue);
        }
        else
        {
            return Delimit(w => w.Write(delimiter), values, perValue);
        }
    }

    public CodeWriter Delimit<T>(string delimiter, IEnumerable<T> values, CWAI<T> perValue)
    {
        // Check delimiter for special cases
        if (delimiter == _defaultNewLine)
        {
            return Delimit(w => w.NewLine(), values, perValue);
        }
        else if (delimiter.Contains(_defaultNewLine))
        {
            return Delimit(w => w.CodeBlock(delimiter), values, perValue);
        }
        else
        {
            return Delimit(w => w.Write(delimiter), values, perValue);
        }
    }

    public CodeWriter DelimitLines<T>(IEnumerable<T> values, CWA<T> perValue)
    {
        return Delimit(w => w.NewLine(), values, perValue);
    }

    public CodeWriter DelimitLines<T>(IEnumerable<T> values, CWAI<T> perValue)
    {
        return Delimit(w => w.NewLine(), values, perValue);
    }

    #endregion


    #region Information about what has already been written

    internal string CurrentNewLineIndent()
    {
        var lastNewLineIndex = _writer.Written.LastIndexOf(_defaultNewLine.AsSpan());
        if (lastNewLineIndex == -1)
            return _defaultNewLine;
        return _writer.Written.Slice(lastNewLineIndex).ToString();
    }

    #endregion

    #region Formatting / Alignment

    /// <summary>
    /// Ensures that the writer is on the beginning of a new line
    /// </summary>
    public CodeWriter EnsureOnNewLine()
    {
        if (!_writer.Written.EndsWith(_newLineIndent.AsSpan()))
        {
            return NewLine();
        }

        return this;
    }

    #endregion

    public CodeWriter TrimEnd(string trimStr)
    {
        if (_writer.Written.EndsWith(trimStr.AsSpan()))
        {
            _writer.Length -= trimStr.Length;
        }

        return this;
    }


    /// <summary>
    /// Returns all resources back to the shared pool
    /// </summary>
    public void Dispose()
    {
        _writer.Dispose();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override bool Equals(object? obj)
    {
        throw new NotSupportedException();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override int GetHashCode()
    {
        throw new NotSupportedException();
    }

    public override string ToString()
    {
        return _writer.ToString();
    }
}