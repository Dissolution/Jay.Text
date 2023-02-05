using System.Buffers;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using InlineIL;
using Jay.Text.Compat;

// ReSharper disable MergeCastWithTypeCheck
// ReSharper disable UnusedMember.Global
// ReSharper disable InvokeAsExtensionMethod

namespace Jay.Text;

public partial class TextBuilder
{
    internal const int MinLength = 1024;

    /// <summary>
    /// Builds a <see cref="string"/> with a <see cref="TextBuilder"/> instance delegate
    /// </summary>
    /// <param name="buildText"></param>
    /// <returns></returns>
    public static string Build(Action<TextBuilder>? buildText)
    {
        if (buildText is null) return string.Empty;
        using (var builder = new TextBuilder())
        {
            buildText(builder);
            return builder.ToString();
        }
    }

    public static string Build<TState>(TState? state, Action<TextBuilder, TState?>? buildText)
    {
        if (buildText is null) return string.Empty;
        using (var builder = new TextBuilder())
        {
            buildText(builder, state);
            return builder.ToString();
        }
    }

    public static TextBuilder Borrow() => new TextBuilder();
    public static TextBuilder Borrow(int minCapacity) => new TextBuilder(minCapacity);
}

public partial class TextBuilder : IList<char>, IReadOnlyList<char>,
    ICollection<char>, IReadOnlyCollection<char>,
    IEnumerable<char>,
    IDisposable
{
    protected char[]? _charArray;
    protected int _length;
    protected string _newLine;

    /// <summary>
    /// Gets the <see cref="Span{T}"/> of <see cref="char"/>s that have been written.
    /// </summary>
    public Span<char> Written
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new Span<char>(_charArray, 0, _length);
    }

    /// <summary>
    /// Gets the <see cref="Span{T}"/> of <see cref="char"/>s available without growing.
    /// </summary>
    internal Span<char> Available
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _charArray.AsSpan(_length);
    }

    /// <summary>
    /// Gets a <see langword="ref"/> to the <see cref="char"/> at the given <paramref name="index"/>.
    /// </summary>
    /// <param name="index">The index of the <see cref="char"/> to reference.</param>
    /// <returns>A <see langword="ref"/> to the <see cref="char"/> at <paramref name="index"/>.</returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown if <paramref name="index"/> is not within the current bounds.
    /// </exception>
    public ref char this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Validate.Index(_length, index);
            // Length is >0 if _charArray is not null
            return ref _charArray![index];
        }
    }

    /// <inheritdoc cref="IList{T}"/>
    char IList<char>.this[int index]
    {
        get => this[index];
        set => this[index] = value;
    }

    /// <inheritdoc cref="IReadOnlyList{T}"/>
    char IReadOnlyList<char>.this[int index]
    {
        get => this[index];
    }

    public Span<char> this[Range range]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _charArray.AsSpan(range);
    }

    /// <summary>
    /// Gets the number of characters that have been written.
    /// </summary>
    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _length;
        internal set => _length = value;
    }

    /// <inheritdoc cref="ICollection{T}"/>
    int ICollection<char>.Count => _length;

    /// <inheritdoc cref="IReadOnlyCollection{T}"/>
    int IReadOnlyCollection<char>.Count => _length;

    /// <inheritdoc cref="ICollection{T}"/>
    bool ICollection<char>.IsReadOnly => false;

    /// <summary>
    /// Construct a new <see cref="TextBuilder"/>.
    /// </summary>
    public TextBuilder()
    {
        _charArray = ArrayPool<char>.Shared.Rent(MinLength);
        _length = 0;
        _newLine = Environment.NewLine;
    }

    public TextBuilder(int minCapacity)
    {
        _charArray = ArrayPool<char>.Shared.Rent(Math.Max(minCapacity, MinLength));
        _length = 0;
        _newLine = Environment.NewLine;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowThenCopy(char ch)
    {
        Grow(1);
        int index = _length;
        _charArray![index] = ch;
        _length = index + 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowThenCopy(ReadOnlySpan<char> text)
    {
        Grow(text.Length);
        TextHelper.Unsafe.CopyTo(text, Available);
        _length += text.Length;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowThenCopy(string text)
    {
        Grow(text.Length);
        TextHelper.Unsafe.CopyTo(text, Available, text.Length);
        _length += text.Length;
    }

    [MethodImpl(MethodImplOptions.NoInlining)] // keep consumers as streamlined as possible
    protected void Grow(int additionalChars)
    {
        // This method is called when the remaining space (_chars.Length - _pos) is
        // insufficient to store a specific number of additional characters.  Thus, we
        // need to grow to at least that new total. GrowCore will handle growing by more
        // than that if possible.
        Debug.Assert(additionalChars > _charArray!.Length - _length);
        GrowCore(_length + additionalChars);
    }

    [MethodImpl(MethodImplOptions
        .AggressiveInlining)] // but reuse this grow logic directly in both of the above grow routines
    private void GrowCore(int minCapacity)
    {
        // We want the max of how much space we actually required and doubling our capacity (without going beyond the max allowed length).
        // We also want to avoid asking for small arrays to reduce the number of times we need to grow.

        // string.MaxLength < array.MaxLength
        const int stringMaxLength = 0x3FFFFFDF;
        // This is larger than stringMaxLength, so the clamp below covers this already
        //const int arrayMaxLength =  0X7FFFFFC7; 
        int newCapacity;
        if (_charArray is null)
        {
            newCapacity = Math.Max(minCapacity, MinLength);
        }
        else
        {
            newCapacity = Math.Max(minCapacity, _charArray.Length * 2)
                .Clamp(MinLength, stringMaxLength);
        }

        // Get our new array, copy what we have written to it
        char[] newArray = ArrayPool<char>.Shared.Rent(newCapacity);
        TextHelper.Unsafe.CopyTo(Written, newArray);
        // Return the array and then point at the new one
        char[]? toReturn = _charArray;
        _charArray = newArray;
        // We may not have had anything to return (if we started with an initial buffer or some weird dispose)
        if (toReturn is not null)
        {
            ArrayPool<char>.Shared.Return(toReturn);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RemoveSpan(int index, int length)
    {
        TextHelper.Unsafe.CopyTo(Written[(index + length)..], this[index..]);
        _length -= length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Span<char> AllocateAt(int index, int length)
    {
        if (length > 0)
        {
            int newLen = _length + length;
            if (newLen > _charArray!.Length)
            {
                Grow(length);
            }

            TextHelper.Unsafe.CopyTo(Written[index..], this[(index + length)..]);
            _length = newLen;
            return _charArray.AsSpan(index, length);
        }

        return default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Span<char> Allocate(int length)
    {
        if (length > 0)
        {
            int curLen = _length;
            int newLen = _length + length;
            if (newLen > _charArray!.Length)
            {
                Grow(length);
            }

            _length = newLen;
            return _charArray.AsSpan(curLen, length);
        }

        return default;
    }

    /// <summary>
    /// Writes a single <see cref="char"/>.
    /// </summary>
    /// <param name="ch">The single <see cref="char"/> to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(char ch)
    {
        Span<char> chars = _charArray;
        int index = _length;
        if (index < chars.Length)
        {
            chars[index] = ch;
            _length = index + 1;
        }
        else
        {
            GrowThenCopy(ch);
        }
    }

    void ICollection<char>.Add(char item) => Write(item);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ReadOnlySpan<char> text)
    {
        if (TextHelper.TryCopyTo(text, Available))
        {
            _length += text.Length;
        }
        else
        {
            GrowThenCopy(text);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(string? text)
    {
        if (text is null) return;
        if (TextHelper.TryCopyTo(text, Available))
        {
            _length += text.Length;
        }
        else
        {
            GrowThenCopy(text);
        }
    }

    /// <summary>
    /// Writes the text representation of a <paramref name="value"/> to this <see cref="TextBuilder"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write<T>(T? value)
    {
        string? strValue;
        if (value is IFormattable)
        {
#if NET6_0_OR_GREATER
            // If the value can format itself directly into our buffer, do so.
            if (value is ISpanFormattable)
            {
                int charsWritten;
                // constrained call avoiding boxing for value types
                while (!((ISpanFormattable)value).TryFormat(Available, out charsWritten, default, default))
                {
                    Grow(1);
                }

                _length += charsWritten;
                return;
            }
#endif
            // constrained call avoiding boxing for value types
            strValue = ((IFormattable)value).ToString(null, null);
        }
        else
        {
            strValue = value?.ToString();
        }

        if (strValue is not null)
        {
            if (TextHelper.TryCopyTo(strValue, Available))
            {
                _length += strValue.Length;
            }
            else
            {
                GrowThenCopy(strValue);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteLine()
    {
        ReadOnlySpan<char> newLine = _newLine.AsSpan();
        if (TextHelper.TryCopyTo(newLine, Available))
        {
            _length += newLine.Length;
        }
        else
        {
            GrowThenCopy(newLine);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteFormatted<T>(T? value, string? format = null, IFormatProvider? provider = null)
    {
        string? strValue;
        if (value is IFormattable)
        {
#if NET6_0_OR_GREATER
            // If the value can format itself directly into our buffer, do so.
            if (value is ISpanFormattable)
            {
                int charsWritten;
                // constrained call avoiding boxing for value types
                while (!((ISpanFormattable)value).TryFormat(Available, out charsWritten, format, provider))
                {
                    Grow(1);
                }

                _length += charsWritten;
                return;
            }
#endif
            // constrained call avoiding boxing for value types
            strValue = ((IFormattable)value).ToString(format, provider);
        }
        else
        {
            strValue = value?.ToString();
        }

        if (strValue is not null)
        {
            if (TextHelper.TryCopyTo(strValue, Available))
            {
                _length += strValue.Length;
            }
            else
            {
                GrowThenCopy(strValue);
            }
        }
    }

    public void WriteAligned(char ch, Alignment alignment, int width, char fillChar = ' ')
    {
        if (width > 0)
        {
            if (width == 1 || alignment == default)
            {
                Write(ch);
            }
            else
            {
                var spaces = width - 1;
                if (alignment == Alignment.Left)
                {
                    Write(ch);
                    Allocate(spaces).Fill(fillChar);
                }
                else if (alignment == Alignment.Right)
                {
                    Allocate(spaces).Fill(fillChar);
                    Write(ch);
                }
                else
                {
                    Debug.Assert(alignment.HasFlag(Alignment.Center));
                    // Is even?
                    if (spaces % 2 == 0)
                    {
                        int half = spaces / 2;
                        Allocate(half).Fill(fillChar);
                        Write(ch);
                        Allocate(half).Fill(fillChar);
                    }
                    else
                    {
                        double half = spaces / 2d;
                        // CenterLeft or CenterRight are valid ways of indicating a tiebreaker
                        if (alignment.HasFlag(Alignment.Right))
                        {
                            Allocate((int)Math.Ceiling(half)).Fill(fillChar);
                            Write(ch);
                            Allocate((int)Math.Floor(half)).Fill(fillChar);
                        }
                        // Defaults to Left
                        else
                        {
                            Allocate((int)Math.Floor(half)).Fill(fillChar);
                            Write(ch);
                            Allocate((int)Math.Ceiling(half)).Fill(fillChar);
                        }
                    }
                }
            }
        }
    }

    public void WriteAligned(string? text, Alignment alignment, int width, char fillChar = ' ')
    {
        WriteAligned(text.AsSpan(), alignment, width, fillChar);
    }

    public void WriteAligned(ReadOnlySpan<char> text, Alignment alignment, int width, char fillChar = ' ')
    {
        if (width > 0)
        {
            if (alignment == default)
            {
                Write(text);
            }
            else
            {
                var spaces = width - text.Length;
                if (spaces == 0)
                {
                    Write(text);
                }
                else if (spaces < 0)
                {
                    if (alignment == Alignment.Right)
                    {
                        Write(text[^width..]);
                    }
                    else if (alignment == Alignment.Left)
                    {
                        Write(text[..width]);
                    }
                    else
                    {
                        Debug.Assert(alignment.HasFlag(Alignment.Center));
                        spaces = Math.Abs(spaces);
                        // Is even?
                        if (spaces % 2 == 0)
                        {
                            int half = spaces / 2;
                            Write(text[half..^half]);
                        }
                        else
                        {
                            double half = spaces / 2d;
                            int front;
                            int back;
                            // CenterLeft or CenterRight are valid ways of indicating a tiebreaker
                            if (alignment.HasFlag(Alignment.Right))
                            {
                                front = (int)Math.Ceiling(half);
                                back = (int)Math.Floor(half);
                            }
                            // Defaults to Left
                            else
                            {
                                front = (int)Math.Floor(half);
                                back = (int)Math.Ceiling(half);
                            }

                            Write(text[front..^back]);
                        }
                    }
                }
                else // spaces > 0
                {
                    if (alignment == Alignment.Left)
                    {
                        Write(text);
                        Allocate(spaces).Fill(fillChar);
                    }
                    else if (alignment == Alignment.Right)
                    {
                        Allocate(spaces).Fill(fillChar);
                        Write(text);
                    }
                    else
                    {
                        Debug.Assert(alignment.HasFlag(Alignment.Center));
                        // Is even?
                        if (spaces % 2 == 0)
                        {
                            int half = spaces / 2;
                            Allocate(half).Fill(fillChar);
                            Write(text);
                            Allocate(half).Fill(fillChar);
                        }
                        else
                        {
                            double half = spaces / 2d;
                            // CenterLeft or CenterRight are valid ways of indicating a tiebreaker
                            if (alignment.HasFlag(Alignment.Right))
                            {
                                Allocate((int)Math.Ceiling(half)).Fill(fillChar);
                                Write(text);
                                Allocate((int)Math.Floor(half)).Fill(fillChar);
                            }
                            // Defaults to Left
                            else
                            {
                                Allocate((int)Math.Floor(half)).Fill(fillChar);
                                Write(text);
                                Allocate((int)Math.Ceiling(half)).Fill(fillChar);
                            }
                        }
                    }
                }
            }
        }
    }

    public void WriteAligned<T>(T? value, Alignment alignment, int width, char fillChar = ' ')
    {
        if (width <= 0) return;
        if (alignment == default)
        {
            Write<T>(value);
            return;
        }

        // We don't know how big value will turn out to be once formatted,
        // so we can just let another temp TextBuilder do the work
        // and then we use the great logic above
        using var temp = TextBuilder.Borrow();
        temp.Write<T>(value);
        WriteAligned(temp.Written, alignment, width, fillChar);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Span<char> GetWriteFormatted<T>(T? value, string? format = null, IFormatProvider? provider = null)
    {
        string? strValue;
        if (value is IFormattable)
        {
#if NET6_0_OR_GREATER
            // If the value can format itself directly into our buffer, do so.
            if (value is ISpanFormattable)
            {
                int start = _length;
                int charsWritten;
                // constrained call avoiding boxing for value types
                while (!((ISpanFormattable)value).TryFormat(Available, out charsWritten, format, provider))
                {
                    Grow(1);
                }

                _length = start + charsWritten;
                return new Span<char>(_charArray, start, charsWritten);
            }
#endif
            // constrained call avoiding boxing for value types
            strValue = ((IFormattable)value).ToString(format, provider);
        }
        else
        {
            strValue = value?.ToString();
        }

        if (strValue is not null)
        {
            var span = Allocate(strValue.Length);
            TextHelper.Unsafe.CopyTo(strValue, span);
            return span;
        }
        else
        {
            return default;
        }
    }

#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable CA1822 // Mark members as static
    public void Write(
#if NET6_0_OR_GREATER
        [InterpolatedStringHandlerArgument("")]
#endif
        ref InterpolatedTextBuilderHandler interpolatedString)
#pragma warning restore CA1822 // Mark members as static
#pragma warning restore IDE0060 // Remove unused parameter
    {
        // The writing has already happened by the time we get into this method!
        IL.Emit.Nop();
    }

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

    public TextBuilder AppendFormat<T>(T? value, string? format = null, IFormatProvider? provider = null)
    {
        WriteFormatted<T>(value, format, provider);
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
            TextHelper.Unsafe.CopyBlock(in textChar,
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
            TextHelper.Unsafe.CopyBlock(in textChar,
                ref buffer[i * len],
                len);
        }

        return this;
    }

    public TextBuilder AppendFormatRepeat<T>(int count, T? value, string? format = null,
        IFormatProvider? provider = null)
    {
        if (value is null || count <= 1)
        {
            Write<T>(value);
            return this;
        }

        int start = _length;
        WriteFormatted<T>(value, format, provider);
        int end = _length;
        int len = end - start;
        if (len <= 0) return this;
        count--;
        var buffer = Allocate(count * len);
        ref char textChar = ref this[start];
        for (var i = 0; i < count; i++)
        {
            TextHelper.Unsafe.CopyBlock(in textChar,
                ref buffer[i * len],
                len);
        }

        return this;
    }

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

    public TextBuilder AppendDelimit<T>(ReadOnlySpan<char> delimiter, ReadOnlySpan<T> values)
    {
        switch (values.Length)
        {
            case 0:
                break;
            case 1:
            {
                Write<T>(values[0]);
                break;
            }
            default:
            {
                Write<T>(values[0]);
                for (var i = 1; i < values.Length; i++)
                {
                    Write(delimiter);
                    Write<T>(values[i]);
                }

                break;
            }
        }

        return this;
    }

    public TextBuilder AppendDelimit<T>(ReadOnlySpan<char> delimiter, params T[]? values)
    {
        if (values is not null)
        {
            switch (values.Length)
            {
                case 0:
                    break;
                case 1:
                {
                    Write<T>(values[0]);
                    break;
                }
                default:
                {
                    Write<T>(values[0]);
                    for (var i = 1; i < values.Length; i++)
                    {
                        Write(delimiter);
                        Write<T>(values[i]);
                    }

                    break;
                }
            }
        }

        return this;
    }

    public TextBuilder AppendDelimit<T>(ReadOnlySpan<char> delimiter, IEnumerable<T> values)
    {
        using (var e = values.GetEnumerator())
        {
            if (e.MoveNext())
            {
                Write<T>(e.Current);
                while (e.MoveNext())
                {
                    Write(delimiter);
                    Write(e.Current);
                }
            }
        }

        return this;
    }

    public TextBuilder AppendDelimit<T>(ReadOnlySpan<char> delimiter, ReadOnlySpan<T> values,
        Action<TextBuilder, T> appendValue)
    {
        switch (values.Length)
        {
            case 0:
                break;
            case 1:
                appendValue(this, values[0]);
                break;
            default:
            {
                appendValue(this, values[0]);
                for (var i = 1; i < values.Length; i++)
                {
                    Write(delimiter);
                    appendValue(this, values[i]);
                }

                break;
            }
        }

        return this;
    }

    public TextBuilder AppendDelimit<T>(ReadOnlySpan<char> delimiter, T[] values, Action<TextBuilder, T> appendValue)
    {
        switch (values.Length)
        {
            case 0:
                break;
            case 1:
                appendValue(this, values[0]);
                break;
            default:
            {
                appendValue(this, values[0]);
                for (var i = 1; i < values.Length; i++)
                {
                    Write(delimiter);
                    appendValue(this, values[i]);
                }

                break;
            }
        }

        return this;
    }

    public TextBuilder AppendDelimit<T>(ReadOnlySpan<char> delimiter, IEnumerable<T> values,
        Action<TextBuilder, T> appendValue)
    {
        using (var e = values.GetEnumerator())
        {
            if (e.MoveNext())
            {
                appendValue(this, e.Current);
                while (e.MoveNext())
                {
                    Write(delimiter);
                    appendValue(this, e.Current);
                }
            }
        }

        return this;
    }

    public TextBuilder NewLine()
    {
        return Append(_newLine);
    }

    public TextBuilder AppendNewLines(int count)
    {
        for (var i = 0; i < count; i++)
        {
            Write(_newLine);
        }

        return this;
    }

    #region Indenting

    public TextBuilder Indent(string? indent,
        Action<TextBuilder> indentedText)
    {
        var oldIndent = _newLine;
        _newLine = $"{_newLine}{indent}";
        indentedText(this);
        _newLine = oldIndent;
        return this;
    }

    #endregion

    public TextBuilder Insert(int index, char ch)
    {
        Validate.Insert(index, _length);
        if (index == _length)
            return Append(ch);
        AllocateAt(index, 1)[0] = ch;
        return this;
    }

    void IList<char>.Insert(int index, char ch) => this.Insert(index, ch);

    public TextBuilder Insert(int index, ReadOnlySpan<char> text)
    {
        Validate.Insert(index, _length);
        if (index == _length)
            return Append(text);
        TextHelper.Unsafe.CopyTo(text, AllocateAt(index, text.Length));
        return this;
    }

    public TextBuilder Insert<T>(int index, T? value)
    {
        return Insert(index, tb => tb.Write<T>(value));
    }

    public TextBuilder Insert(int index, Action<TextBuilder> buildInsertText)
    {
        Validate.Insert(index, _length);
        using var temp = TextBuilder.Borrow();
        buildInsertText(temp);
        if (index == _length)
        {
            return Append(temp.Written);
        }

        TextHelper.Unsafe.CopyTo(temp.Written, AllocateAt(index, temp.Length));
        return this;
    }

    public int FirstIndexOf(char ch)
    {
        ReadOnlySpan<char> chars = Written;
        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] == ch) return i;
        }

        return -1;
    }

    int IList<char>.IndexOf(char ch) => FirstIndexOf(ch);

    public int LastIndexOf(char ch)
    {
        ReadOnlySpan<char> chars = Written;
        for (var i = chars.Length - 1; i >= 0; i--)
        {
            if (chars[i] == ch) return i;
        }

        return -1;
    }

    public int FirstIndexOf(ReadOnlySpan<char> text)
    {
        return MemoryExtensions.IndexOf(Written, text);
    }

    public int FirstIndexOf(ReadOnlySpan<char> text, StringComparison comparison)
    {
        return MemoryExtensions.IndexOf(Written, text, comparison);
    }

    public int LastIndexOf(ReadOnlySpan<char> text)
    {
        return MemoryExtensions.LastIndexOf(Written, text);
    }

    public int LastIndexOf(ReadOnlySpan<char> text, StringComparison comparison)
    {
#if NET6_0_OR_GREATER
        return MemoryExtensions.LastIndexOf(Written, text, comparison);
#else
        int textLen = text.Length;
        if (textLen == 0) return -1;
        for (var i = _length - textLen; i >= 0; i--)
        {
            if (text.Equals(_charArray.AsSpan(i, textLen), comparison))
                return i;
        }

        return -1;
#endif
    }

    public bool Contains(char ch) => FirstIndexOf(ch) >= 0;

    public TextBuilder Replace(char oldChar, char newChar)
    {
        var writ = Written;
        for (var i = writ.Length - 1; i >= 0; i--)
        {
            ref char ch = ref writ[i];
            if (ch == oldChar)
            {
                ch = newChar;
            }
        }

        return this;
    }

    private void ReplaceSwap(ReadOnlySpan<char> oldText, ReadOnlySpan<char> newText, StringComparison comparison)
    {
        ref readonly char refNewTextChar = ref newText.GetPinnableReference();
        var len = newText.Length;
        Span<char> scan = Written;
        int i;
        while ((i = MemoryExtensions.IndexOf(scan, oldText, comparison)) >= 0)
        {
            TextHelper.Unsafe.CopyBlock(in refNewTextChar,
                ref scan[i],
                len);
            scan = scan.Slice(i + len);
        }
    }

    private void ReplaceShrink(ReadOnlySpan<char> oldText, ReadOnlySpan<char> newText, StringComparison comparison)
    {
        ref readonly char refNewTextChar = ref newText.GetPinnableReference();
        var newLen = newText.Length;
        var oldLen = oldText.Length;
        Span<char> scan = Written;
        int i;
        while ((i = MemoryExtensions.IndexOf(scan, oldText, comparison)) >= 0)
        {
            TextHelper.Unsafe.CopyBlock(in refNewTextChar,
                ref scan[i],
                newLen);
            var right = scan.Slice(i + newLen);
            TextHelper.Unsafe.CopyTo(scan.Slice(i + oldLen), right);
            scan = right;
        }
    }

    private void ReplaceGrow(ReadOnlySpan<char> oldText, ReadOnlySpan<char> newText, StringComparison comparison)
    {
        /* Thoughts:
         * What we need to do is hold a starting (end of last write) position
         * Find i = IndexOf, if I == -1, be sure we write everything AFTER start
         * Otherwise, write [i-s] to temp, then write new to temp, then update s and i to their new positions
         * But when we scan chararray, we need to offset i to later position for IndexOf, then offset BACK for slicing (s + i)
         
         */


        throw new NotImplementedException();
    }

    public TextBuilder Replace(string? oldText, string? newText,
        StringComparison comparison = StringComparison.Ordinal)
    {
        return Replace(oldText.AsSpan(), newText.AsSpan(), comparison);
    }

    public TextBuilder Replace(ReadOnlySpan<char> oldText, ReadOnlySpan<char> newText,
        StringComparison comparison = StringComparison.Ordinal)
    {
        var oldLen = oldText.Length;
        if (oldLen == 0 || oldLen > _length) return this;
        if (oldLen == newText.Length)
        {
            ReplaceSwap(oldText, newText, comparison);
        }
        else if (oldLen > newText.Length)
        {
            ReplaceShrink(oldText, newText, comparison);
        }
        else
        {
            ReplaceGrow(oldText, newText, comparison);
        }

        return this;
    }


    public void RemoveAt(int index)
    {
        Validate.Index(index, _length);
        RemoveSpan(index, 1);
    }

    public void RemoveAt(Range range)
    {
        var (offset, length) = range.GetOffsetAndLength(_length);
        RemoveSpan(offset, length);
    }

    public int RemoveFirst(char ch)
    {
        ReadOnlySpan<char> chars = Written;
        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] == ch)
            {
                RemoveSpan(i, 1);
                return i;
            }
        }

        return -1;
    }

    bool ICollection<char>.Remove(char ch) => RemoveFirst(ch) >= 0;

    public int RemoveLast(char ch)
    {
        ReadOnlySpan<char> chars = Written;
        for (var i = chars.Length - 1; i >= 0; i--)
        {
            if (chars[i] == ch)
            {
                RemoveSpan(i, 1);
                return i;
            }
        }

        return -1;
    }

    public int RemoveFirst(ReadOnlySpan<char> text)
    {
        var i = FirstIndexOf(text);
        if (i >= 0)
        {
            RemoveSpan(i, text.Length);
        }

        return i;
    }

    public int RemoveFirst(ReadOnlySpan<char> text, StringComparison comparison)
    {
        var i = FirstIndexOf(text, comparison);
        if (i >= 0)
        {
            RemoveSpan(i, text.Length);
        }

        return i;
    }

    public int RemoveLast(ReadOnlySpan<char> text)
    {
        var i = LastIndexOf(text);
        if (i >= 0)
        {
            RemoveSpan(i, text.Length);
        }

        return i;
    }

    public int RemoveLast(ReadOnlySpan<char> text, StringComparison comparison)
    {
        var i = LastIndexOf(text, comparison);
        if (i >= 0)
        {
            RemoveSpan(i, text.Length);
        }

        return i;
    }

    public TextBuilder Clear()
    {
        // We do not clear the contents of the array
        _length = 0;
        return this;
    }

    void ICollection<char>.Clear() => this.Clear();

    public int Measure(Action<TextBuilder> buildText)
    {
        int start = _length;
        buildText(this);
        return _length - start;
    }

    public int[] Measure(params Action<TextBuilder>[] writeTexts)
    {
        int len = writeTexts.Length;
        int[] measurements = new int[len];
        for (var i = 0; i < writeTexts.Length; i++)
        {
            measurements[i] = Measure(writeTexts[i]);
        }

        return measurements;
    }

    public void WriteWrap(int maxLengthBeforeWrap,
        params Action<TextBuilder>[] writeTexts)
    {
        var len = writeTexts.Length;
        var measurements = Measure(writeTexts);
        var total = measurements.Sum();
        if (total > maxLengthBeforeWrap)
        {
            var index = _length;
            // Not the first
            for (var i = len - 1; i > 0; i--)
            {
                index -= measurements[i];
                Insert(index, _newLine);
            }
        }
    }

    public void WriteWrap<T>(int maxLengthBeforeWrap,
        IEnumerable<T> values,
        Action<TextBuilder, T> writeValue)
    {
        var writeLengths = new List<int>();
        foreach (var value in values)
        {
            int start = _length;
            writeValue(this, value);
            writeLengths.Add(_length - start);
        }

        var total = writeLengths.Sum();
        if (total > maxLengthBeforeWrap)
        {
            var index = _length;
            // Not the first
            for (var i = writeLengths.Count - 1; i > 0; i--)
            {
                index -= writeLengths[i];
                Insert(index, _newLine);
            }
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(char[] array, int arrayIndex = 0) => TextHelper.Unsafe.CopyTo(Written, array.AsSpan(arrayIndex));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(Span<char> destination) => TextHelper.Unsafe.CopyTo(Written, destination);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryCopyTo(Span<char> destination) => TextHelper.TryCopyTo(Written, destination);

    /// <summary>Enumerates the elements of a <see cref="Span{T}"/>.</summary>
    public ref struct Enumerator
    {
        /// <summary>The span being enumerated.</summary>
        private readonly Span<char> _span;

        /// <summary>The next index to yield.</summary>
        private int _index;

        /// <summary>Gets the element at the current position of the enumerator.</summary>
        public ref char Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _span[_index];
        }

        public int Index
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _index;
        }

        /// <summary>Initialize the enumerator.</summary>
        /// <param name="span">The span to enumerate.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(Span<char> span)
        {
            _span = span;
            _index = -1;
        }

        /// <summary>Advances the enumerator to the next element of the span.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            int index = _index + 1;
            if (index < _span.Length)
            {
                _index = index;
                return true;
            }

            return false;
        }
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(Written);
    }

    IEnumerator<char> IEnumerable<char>.GetEnumerator()
    {
        for (var i = 0; i < Written.Length; i++)
        {
            yield return Written[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        for (var i = 0; i < Written.Length; i++)
        {
            yield return Written[i];
        }
    }

    public void Dispose()
    {
        char[]? toReturn = _charArray;
        _length = 0;
        _charArray = null;
        if (toReturn is not null)
        {
            // We do not clear the array
            ArrayPool<char>.Shared.Return(toReturn);
        }
    }

    public bool Equals(string? text)
    {
        return TextHelper.Equals(Written, text);
    }

    public bool Equals(ReadOnlySpan<char> text)
    {
        return TextHelper.Equals(Written, text);
    }

    public override bool Equals(object? obj) => throw new NotSupportedException();

    public override int GetHashCode() => throw new NotSupportedException();

    public string ToString(Range range)
    {
#if NET6_0_OR_GREATER
        return new string(Written[range]);
#else
        return TextHelper.AsString(Written[range]);
#endif
    }

    public override string ToString()
    {
        return new string(_charArray!, 0, _length);
    }

    public string ToStringAndClear()
    {
        int len = _length;
        _length = 0;
        return new string(_charArray!, 0, len);
    }
}