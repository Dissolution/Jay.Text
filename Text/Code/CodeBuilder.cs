using System.Diagnostics.CodeAnalysis;
using Jay.Text.Utilities;

namespace Jay.Text.Code;

// ReSharper disable once InconsistentNaming
public delegate void CBA(CodeBuilder builder);

public sealed class CodeBuilder : CodeBuilder<CodeBuilder>
{
    public CodeBuilder()
        : base() { }
}

public abstract class CodeBuilder<TBuilder> : TextBuilder<TBuilder>
    where TBuilder : CodeBuilder<TBuilder>
{
    protected internal string _newLineIndent;

    protected CodeBuilder()
        : base()
    {
        _newLineIndent = TextHelper.NewLine;
    }

    protected string CurrentNewLineIndent()
    {
        var lastNewLineIndex = Written.LastIndexOf(TextHelper.NewLineSpan);
        if (lastNewLineIndex == -1)
            return TextHelper.NewLine;
        return Written.Slice(lastNewLineIndex).ToString();
    }

    public override TBuilder NewLine()
    {
        this.Write(_newLineIndent);
        return _this;
    }

    public override TBuilder Append(string? str)
    {
        return Append(str.AsSpan());
    }

    public override TBuilder Append(ReadOnlySpan<char> text)
    {
        int textLen = text.Length;
        if (textLen == 0)
            return _this;

        // We're going to be splitting on NewLine
        ReadOnlySpan<char> newLine = TextHelper.NewLineSpan;

        var e = text.TextSplit(newLine).GetEnumerator();
        if (!e.MoveNext()) return _this;
        this.Write(e.Current);
        while (e.MoveNext())
        {
            // Delimit with NewLine
            NewLine();
            // Write this slice
            this.Write(e.Current);
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
        ReadOnlySpan<char> newLine = TextHelper.NewLineSpan;

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
            index = format.NextIndexOf(newLine, startIndex: index);
            if (index >= 0)
            {
                sliceLen = index - sliceStart;
                if (sliceLen > 0)
                {
                    // Write this chunk
                    WriteFormatLine(format.Slice(sliceStart, sliceLen), formatArgs);
                }

                // Write current NewLine
                NewLine();

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
            WriteFormatLine(format.Slice(sliceStart, sliceLen), formatArgs);
        }

        return _this;
    }
#endif

    public override TBuilder Append<T>([AllowNull] T value)
    {
        return Format<T>(value, default);
    }

    public override TBuilder Format<T>(
        [AllowNull] T value,
        string? format,
        IFormatProvider? provider = null
    )
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
                this.Write(str);
                return _this;
            }
            case IFormattable formattable:
            {
                this.WriteFormat(formattable, format, provider);
                return _this;
            }
            case IEnumerable enumerable:
            {
                format ??= ",";
                return Delimit(
                    format,
                    enumerable.Cast<object?>(),
                    static (w, v) => w.Append<object?>(v)
                );
            }
            default:
            {
                this.Write<T>(value);
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
            this.Write(indent);
        }

        var newIndent = oldIndent + indent;
        _newLineIndent = newIndent;
        indentBlock(_this);
        _newLineIndent = oldIndent;
        // Did we do a newline that we need to decrease?
        if (Written.EndsWith(newIndent.AsSpan()))
        {
            this.Length -= newIndent.Length;
            this.Write(oldIndent);
        }
        return _this;
    }

    public TBuilder If(
        bool predicateResult,
        TextBuilderAction<TBuilder>? ifTrue,
        TextBuilderAction<TBuilder>? ifFalse = null
    )
    {
        return If(() => predicateResult, ifTrue, ifFalse);
    }

    public TBuilder If(
        Func<bool> predicate,
        TextBuilderAction<TBuilder>? ifTrue,
        TextBuilderAction<TBuilder>? ifFalse = null
    )
    {
        if (predicate())
        {
            ifTrue?.Invoke(_this);
        }
        else
        {
            ifFalse?.Invoke(_this);
        }
        return _this;
    }






}


