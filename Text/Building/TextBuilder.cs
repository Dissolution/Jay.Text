using System.Diagnostics.CodeAnalysis;

namespace Jay.Text;

public sealed class TextBuilder : TextBuilder<TextBuilder>
{
    public TextBuilder() : base() { }
}

public abstract class TextBuilder<TBuilder> : CharArrayBuilder
    where TBuilder : TextBuilder<TBuilder>
{
    protected readonly TBuilder _this;

    protected TextBuilder()
        : base()
    {
        _this = (TBuilder)this;
    }

    public virtual TBuilder NewLine()
    {
        AppendNonNullString(DefaultNewLine);
        return _this;
    }

    public TBuilder NewLines(int count)
    {
        for (var i = 0; i < count; i++)
        {
            NewLine();
        }
        return _this;
    }

    public virtual TBuilder Write(char ch)
    {
        AppendChar(ch);
        return _this;
    }

    public virtual TBuilder Write(string? str)
    {
        AppendString(str);
        return _this;
    }

    public virtual TBuilder Write(ReadOnlySpan<char> text)
    {
        AppendCharSpan(text);
        return _this;
    }

    public virtual TBuilder Write<T>([AllowNull] T value)
    {
        AppendValue<T>(value);
        return _this;
    }

    public virtual TBuilder Format<T>([AllowNull] T value, string? format)
    {
        AppendFormat<T>(value, format);
        return _this;
    }

    public TBuilder WriteLine(char ch) => Write(ch).NewLine();
    public TBuilder WriteLine(string? str) => Write(str).NewLine();
    public TBuilder WriteLine(ReadOnlySpan<char> text) => Write(text).NewLine();
    public TBuilder WriteLine<T>(T? value) => Write<T>(value).NewLine();

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

    public TBuilder EnumerateWrite<T>(IEnumerable<T> enumerable) =>
        Enumerate(enumerable, static (tb, v) => tb.Write<T>(v));

    public TBuilder EnumerateWriteLines<T>(IEnumerable<T> enumerable) =>
        Enumerate(enumerable, static (tb, v) => tb.Write<T>(v).NewLine());

    public TBuilder EnumerateLines<T>(IEnumerable<T> enumerable, TextBuilderValueAction<TBuilder, T> perValue) =>
        Enumerate(enumerable,
            (tb, v) =>
            {
                perValue(tb, v);
                tb.NewLine();
            });

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
        return Delimit(w => w.Write(delimiter), values, perValue);
    }

    public TBuilder Delimit<T>(string delimiter, IEnumerable<T> values, TextBuilderValueIndexAction<TBuilder, T> perValueIndex)
    {
        return Delimit(w => w.Write(delimiter), values, perValueIndex);
    }

    public TBuilder DelimitLines<T>(IEnumerable<T> values, TextBuilderValueAction<TBuilder, T> perValue)
    {
        return Delimit(static w => w.NewLine(), values, perValue);
    }

    public TBuilder DelimitLines<T>(IEnumerable<T> values, TextBuilderValueIndexAction<TBuilder, T> perValueIndex)
    {
        return Delimit(static w => w.NewLine(), values, perValueIndex);
    }
}