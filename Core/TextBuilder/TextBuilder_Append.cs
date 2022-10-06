using System.Collections;

namespace Jay.Text;

public partial class TextBuilder
{
    public TextBuilder Append(char ch)
    {
        Write(ch);
        return this;
    }

    public TextBuilder Append(ReadOnlySpan<char> text)
    {
        Write(text);
        return this;
    }

    public TextBuilder Append(string? text)
    {
        Write(text);
        return this;
    }

    public TextBuilder Append<T>(T? value)
    {
        Write<T>(value);
        return this;
    }

    #region AppendFormatted
    public TextBuilder AppendFormatted<T>(T? value, string? format = null, IFormatProvider? provider = null)
    {
        WriteFormatted<T>(value, format, provider);
        return this;
    }
    #endregion

    #region Append / Repeat
    public TextBuilder Repeat(int count, Action<TextBuilder> buildText)
    {
        for (var i = 0; i < count; i++)
        {
            buildText(this);
        }
        return this;
    }


    public TextBuilder AppendRepeat(int count, char ch)
    {
        Allocate(count).Fill(ch);
        return this;
    }

    public TextBuilder AppendRepeat(int count, ReadOnlySpan<char> text)
    {
        var len = text.Length;
        if (len == 0 || count <= 0) return this;
        var buffer = Allocate(count * len);
        var textChar = text.GetPinnableReference();
        for (var i = 0; i < count; i++)
        {
            TextHelper.Unsafe.Copy(in textChar,
                ref buffer[i * len],
                len);
        }
        return this;
    }

    public TextBuilder AppendRepeat<T>(int count, T? value)
    {
        if (value is null || count <= 0) return this;
        int start = _length;
        Write<T>(value);
        int end = _length;
        int len = end - start;
        if (len <= 0) return this;
        count--;
        var buffer = Allocate(count * len);
        ref char textChar = ref this[start];
        for (var i = 0; i < count; i++)
        {
            TextHelper.Unsafe.Copy(in textChar,
                ref buffer[i * len],
                len);
        }
        return this;
    }

    public TextBuilder AppendFormattedRepeat<T>(int count, T? value, string? format = null, IFormatProvider? provider = null)
    {
        var formatted = GetWriteFormatted<T>(value, format, provider);
        var formattedLength = formatted.Length;
        var buffer = Allocate(count * formattedLength);
        for (var i = 0; i < count; i++)
        {
            TextHelper.Unsafe.Copy(formatted,
                buffer.Slice(i * formattedLength));
        }
        return this;
    }
    #endregion

    #region Append Join
    public TextBuilder AppendJoin<T>(params T[]? values)
    {
        if (values is not null)
        {
            for (var i = 0; i < values.Length; i++)
            {
                Write<T>(values[i]);
            }
        }
        return this;
    }

    public TextBuilder AppendJoin<T>(IEnumerable<T> values)
    {
        foreach (var value in values)
        {
            Write<T>(value);
        }
        return this;
    }

    public TextBuilder AppendJoin(IEnumerable? enumerable)
    {
        if (enumerable is null) return this;
        if (enumerable is string str) return Append(str);
        foreach (object? value in enumerable)
        {
            Write<object>(value);
        }
        return this;
    }
    #endregion

    #region Append Delimit

    public TextBuilder AppendDelimit<T>(char delimiter, ReadOnlySpan<T> values)
    {
        var len = values.Length;
        if (len == 0) return this;
        Write<T>(values[0]);
        for (var i = 1; i < len; i++)
        {
            Write(delimiter);
            Write<T>(values[i]);
        }
        return this;
    }
    
    
    public TextBuilder AppendDelimit<T>(ReadOnlySpan<char> delimiter, ReadOnlySpan<T> values)
    {
        var len = values.Length;
        if (len == 0) return this;
        Write<T>(values[0]);
        for (var i = 1; i < len; i++)
        {
            Write(delimiter);
            Write<T>(values[i]);
        }
        return this;
    }

    public TextBuilder AppendDelimit<T>(char delimiter, params T[]? values)
    {
        if (values is null) return this;
        var len = values.Length;
        if (len == 0) return this;
        Write<T>(values[0]);
        for (var i = 1; i < len; i++)
        {
            Write(delimiter);
            Write<T>(values[i]);
        }
        return this;
    }
    
    public TextBuilder AppendDelimit<T>(ReadOnlySpan<char> delimiter, params T[]? values)
    {
        if (values is null) return this;
        var len = values.Length;
        if (len == 0) return this;
        Write<T>(values[0]);
        for (var i = 1; i < len; i++)
        {
            Write(delimiter);
            Write<T>(values[i]);
        }
        return this;
    }

    public TextBuilder AppendDelimit<T>(char delimiter, IEnumerable<T> values)
    {
        if (values.TryGetNonEnumeratedCount(out int count))
        {
            if (count == 0) return this;
            
            if (values is IList<T> list)
            {
                Write<T>(list[0]);
                for (var i = 1; i < count; i++)
                {
                    Write(delimiter);
                    Write<T>(list[i]);
                }
                return this;
            }
        }
        
        using (var e = values.GetEnumerator())
        {
            e.MoveNext();
            Write<T>(e.Current);
            while (e.MoveNext())
            {
                Write(delimiter);
                Write<T>(e.Current);
            }
        }
        return this;
    }
    
    public TextBuilder AppendDelimit<T>(ReadOnlySpan<char> delimiter, IEnumerable<T> values)
    {
        if (values.TryGetNonEnumeratedCount(out int count))
        {
            if (count == 0) return this;
            
            if (values is IList<T> list)
            {
                Write<T>(list[0]);
                for (var i = 1; i < count; i++)
                {
                    Write(delimiter);
                    Write<T>(list[i]);
                }
                return this;
            }
        }
        
        using (var e = values.GetEnumerator())
        {
            e.MoveNext();
            Write<T>(e.Current);
            while (e.MoveNext())
            {
                Write(delimiter);
                Write<T>(e.Current);
            }
        }
        return this;
    }

    public TextBuilder AppendDelimit(char delimiter, IEnumerable values)
    {
        IEnumerator enumerator = values.GetEnumerator();
        try
        {
            if (enumerator.MoveNext())
            {
                Write<object>(enumerator.Current);
                while (enumerator.MoveNext())
                {
                    Write(delimiter);
                    Write<object>(enumerator.Current);
                }
            }
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }

        return this;
    }
    
    public TextBuilder AppendDelimit(ReadOnlySpan<char> delimiter, IEnumerable values)
    {
        IEnumerator enumerator = values.GetEnumerator();
        try
        {
            if (enumerator.MoveNext())
            {
                Write<object>(enumerator.Current);
                while (enumerator.MoveNext())
                {
                    Write(delimiter);
                    Write<object>(enumerator.Current);
                }
            }
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }

        return this;
    }

    
    public TextBuilder AppendDelimit<T>(char delimiter, ReadOnlySpan<T> values, Action<TextBuilder, T> buildValueText)
    {
        var len = values.Length;
        if (len == 0) return this;
        buildValueText(this, values[0]);
        for (var i = 1; i < len; i++)
        {
            Write(delimiter);
            buildValueText(this, values[i]);
        }
        return this;
    }
    
    public TextBuilder AppendDelimit<T>(ReadOnlySpan<char> delimiter, ReadOnlySpan<T> values, Action<TextBuilder, T> buildValueText)
    {
        var len = values.Length;
        if (len == 0) return this;
        buildValueText(this, values[0]);
        for (var i = 1; i < len; i++)
        {
            Write(delimiter);
            buildValueText(this, values[i]);
        }
        return this;
    }

    public TextBuilder AppendDelimit<T>(char delimiter, T[]? values, Action<TextBuilder, T> buildValueText)
    {
        if (values is null) return this;
        var len = values.Length;
        if (len == 0) return this;
        buildValueText(this, values[0]);
        for (var i = 1; i < len; i++)
        {
            Write(delimiter);
            buildValueText(this, values[i]);
        }
        return this;
    }
    
    public TextBuilder AppendDelimit<T>(ReadOnlySpan<char> delimiter, T[]? values, Action<TextBuilder, T> buildValueText)
    {
        if (values is null) return this;
        var len = values.Length;
        if (len == 0) return this;
        buildValueText(this, values[0]);
        for (var i = 1; i < len; i++)
        {
            Write(delimiter);
            buildValueText(this, values[i]);
        }
        return this;
    }

    public TextBuilder AppendDelimit<T>(char delimiter, IEnumerable<T> values, Action<TextBuilder, T> buildValueText)
    {
        if (values.TryGetNonEnumeratedCount(out int count))
        {
            if (count == 0) return this;
            
            if (values is IList<T> list)
            {
                buildValueText(this, list[0]);
                for (var i = 1; i < count; i++)
                {
                    Write(delimiter);
                    buildValueText(this, list[i]);
                }
                return this;
            }
        }
        
        using (var e = values.GetEnumerator())
        {
            e.MoveNext();
            buildValueText(this, e.Current);
            while (e.MoveNext())
            {
                Write(delimiter);
                buildValueText(this, e.Current);
            }
        }
        return this;
    }
    
    public TextBuilder AppendDelimit<T>(ReadOnlySpan<char> delimiter, IEnumerable<T> values, Action<TextBuilder, T> buildValueText)
    {
        if (values.TryGetNonEnumeratedCount(out int count))
        {
            if (count == 0) return this;
            
            if (values is IList<T> list)
            {
                buildValueText(this, list[0]);
                for (var i = 1; i < count; i++)
                {
                    Write(delimiter);
                    buildValueText(this, list[i]);
                }
                return this;
            }
        }
        
        using (var e = values.GetEnumerator())
        {
            e.MoveNext();
            buildValueText(this, e.Current);
            while (e.MoveNext())
            {
                Write(delimiter);
                buildValueText(this, e.Current);
            }
        }
        return this;
    }
    
    public TextBuilder AppendDelimit(char delimiter, IEnumerable values, Action<TextBuilder, object?> buildValueText)
    {
        IEnumerator enumerator = values.GetEnumerator();
        try
        {
            if (enumerator.MoveNext())
            {
                buildValueText(this, enumerator.Current);
                while (enumerator.MoveNext())
                {
                    Write(delimiter);
                    buildValueText(this, enumerator.Current);
                }
            }
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }

        return this;
    }
    
    public TextBuilder AppendDelimit(ReadOnlySpan<char> delimiter, IEnumerable values, Action<TextBuilder, object?> buildValueText)
    {
        IEnumerator enumerator = values.GetEnumerator();
        try
        {
            if (enumerator.MoveNext())
            {
                buildValueText(this, enumerator.Current);
                while (enumerator.MoveNext())
                {
                    Write(delimiter);
                    buildValueText(this, enumerator.Current);
                }
            }
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }

        return this;
    }
#endregion
    
    #region Append New Line
    public TextBuilder AppendNewLine()
    {
        return Append(NewLineString);
    }

    public TextBuilder AppendNewLines(int count)
    {
        ReadOnlySpan<char> newLine = NewLineString;
        int newLineLen = newLine.Length;
        var buffer = Allocate(count * newLineLen);
        for (var i = 0; i < count; i++)
        {
            TextHelper.Unsafe.Copy(newLine, buffer.Slice(i*newLineLen));
        }
        return this;
    }
    #endregion
}