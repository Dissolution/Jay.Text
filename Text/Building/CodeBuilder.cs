using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Jay.Text;


public sealed class CodeBuilder : CodeBuilder<CodeBuilder>
{
    public CodeBuilder() : base() { }
}

public abstract class CodeBuilder<TBuilder> : TextBuilder<TBuilder>
    where TBuilder : CodeBuilder<TBuilder>
{
    protected internal string _newLineIndent;

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

#if !NET6_0_OR_GREATER
    public override TBuilder Format(FormattableString text)
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
                    FormatHelper(format.Slice(sliceStart, sliceLen), formatArgs);
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
            FormatHelper(format.Slice(sliceStart, sliceLen), formatArgs);
        }

        return _this;
    }
#endif

    public override TBuilder Write<T>([AllowNull] T value)
    {
        return Write<T>(value, default);
    }
    public override TBuilder Write<T>([AllowNull] T value, string? format)
    {
        switch (value)
        {
            case null:
            {
                return _this;
            }
            case CBA cba:
            {
                if (_this is CodeBuilder cb)
                {
                    var oldIndent = _newLineIndent;
                    var currentIndent = CurrentNewLineIndent();
                    _newLineIndent = currentIndent;
                    cba(cb);
                    _newLineIndent = oldIndent;
                    return _this;
                }
                throw new ArgumentException();
            }
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
                AppendString(value.ToString());
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