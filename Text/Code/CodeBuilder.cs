using System.Diagnostics.CodeAnalysis;

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
      
    }



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








}


