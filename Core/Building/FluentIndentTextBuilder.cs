using System.Collections;
using System.Diagnostics;
using Jay.Text.Splitting;

namespace Jay.Text.Building;

public class FluentIndentTextBuilder<TBuilder>
: FluentTextBuilder<TBuilder>
    where TBuilder : FluentIndentTextBuilder<TBuilder>
{
    protected Stack<string> _indents;
    
    public FluentIndentTextBuilder() : base()
    {
        _indents = new(0);
    }
    
    internal string GetCurrentPositionAsIndent()
    {
        // Start searching all Written for the last newline
        var lastNewLineIndex = Written.LastIndexOf(_newline.AsSpan());
        // If we never wrote one, there's no indent
        if (lastNewLineIndex == -1)
            return string.Empty;
        /* everything after is our indent
         * it would seem we might only want to capture whitespace
         * but this lets us do hacks like indent('-') or indent('*')
         */
        var after = Written.Slice(lastNewLineIndex + _newline.Length);
        return after.ToString();
    }

    public override TBuilder NewLine()
    {
        // newline
        base.Write(_newline);
        // all indents
        foreach (var indent in _indents)
        {
            base.Write(indent);
        }
        return _builder;
    }
    
    public override void Write(string? str)
    {
        if (string.IsNullOrEmpty(str)) return;
        // We're going to be splitting on NewLine
        var e = new TextSplitEnumerator(str.AsSpan(), _newline.AsSpan());
        if (!e.MoveNext()) return;
        this.Write(e.Current);
        while (e.MoveNext())
        {
            // Delimit with NewLine
            this.NewLine();
            // Write this slice
            this.Write(e.Current);
        }
    }

    public override void Write(scoped ReadOnlySpan<char> text)
    {
        int textLen = text.Length;
        if (textLen == 0) return;

        // We're going to be splitting on NewLine
        var e = new TextSplitEnumerator(text, _newline.AsSpan());
        if (!e.MoveNext()) return;
        this.Write(e.Current);
        while (e.MoveNext())
        {
            // Delimit with NewLine
            this.NewLine();
            // Write this slice
            this.Write(e.Current);
        }
    }

    public override void Format<T>(T? value, string? format = null, IFormatProvider? provider = null) where T : default
    {
        switch (value)
        {
            case null:
            {
                return;
            }
            case TBA<TBuilder> tba:
            {
                // Capture our original indents
                var originalIndents = _indents;
                // Replace them with a single indent based upon this position
                _indents = new Stack<string>(1);
                _indents.Push(GetCurrentPositionAsIndent());
                // perform the action
                tba(_builder);
                // restore the indents
                _indents = originalIndents;
                return;
            }
            case string str:
            {
                this.Write(str);
                return;
            }
            case IFormattable formattable:
            {
                base.Format(formattable, format, provider);
                return;
            }
            case IEnumerable enumerable:
            {
                format ??= ",";
                Delimit(
                    format,
                    enumerable.Cast<object?>(),
                    (w, v) => w.Append<object?>(v, format, provider)
                );
                return;
            }
            default:
            {
                base.Format<T>(value);
                return;
            }
        }
    }
    
    public TBuilder Indented(string indent, TBA<TBuilder> indentedAction)
    {
        _indents.Push(indent);
        indentedAction(_builder);
        var popped = _indents.Pop();
        Debug.Assert(indent == popped);
        return _builder;
    }
}