using System.ComponentModel;

using Jay.Text.Utilities;


// ReSharper disable UnusedParameter.Local

namespace Jay.Text;

/// <summary>
/// A custom minimized implementation of an <c>Interpolated String Handler</c>
/// </summary>
#if NET6_0_OR_GREATER
[InterpolatedStringHandler]
#endif
public ref struct CharSpanBuilder
{
    public static implicit operator CharSpanBuilder(Span<char> buffer) => new(buffer);

    /// <summary>
    /// Rented char[] from pool, used to back <see cref="_chars"/>
    /// </summary>
    private char[]? _charArray;

    /// <summary>
    /// The active charspan we're writing to
    /// </summary>
    private Span<char> _chars;

    /// <summary>
    /// Current position we're writing to
    /// </summary>
    private int _position;

    /// <summary>
    /// Gets a <c>Span&lt;<see cref="char"/>&gt;</c> of characters written thus far
    /// </summary>
    public Span<char> Written
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _chars.Slice(0, _position);
    }

    /// <summary>
    /// Gets a <c>Span&lt;<see cref="char"/>&gt;</c> of characters available for writing<br/>
    /// <b>Caution</b>: If you write to Available, you must also update Length!
    /// </summary>
    public Span<char> Available
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _chars.Slice(_position);
    }

    public Span<char> CharSpan => _chars;

    /// <summary>
    /// The current total capacity to store <see cref="char"/>acters<br/>
    /// Will be increased when required during Write operations
    /// </summary>
    public int Capacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _chars.Length;
    }

    /// <summary>
    /// Gets or sets the number of <see cref="char"/>acters written 
    /// </summary>
    /// <remarks>
    /// A set Length will be clamped between 0 and Capacity
    /// </remarks>
    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _position;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _position = value.Clamp(0, Capacity);
    }


    /// <summary>
    /// Construct a new <see cref="CharSpanBuilder"/> with default starting Capacity
    /// </summary>
    public CharSpanBuilder()
    {
        _chars = _charArray = ArrayPool<char>.Shared.Rent(BuilderHelper.MinimumCapacity);
        _position = 0;
    }

    /// <summary>
    /// Construct a new <see cref="CharSpanBuilder"/> with an <paramref name="initialBuffer"/>,<br/>
    /// which will be discarded if Capacity increases
    /// </summary>
    /// <param name="initialBuffer">The initial <c>Span&lt;<see cref="char"/>&gt;</c> to write to</param>
    public CharSpanBuilder(Span<char> initialBuffer)
    {
        _chars = initialBuffer;
        _charArray = null;
        _position = 0;
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Support for <see cref="InterpolatedStringHandlerAttribute"/>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public CharSpanBuilder(int literalLength, int formattedCount)
    {
        _chars = _charArray = ArrayPool<char>.Shared
            .Rent(BuilderHelper.GetInterpolatedStartCapacity(literalLength, formattedCount));
        _position = 0;
    }

    /// <summary>
    /// Support for <see cref="InterpolatedStringHandlerAttribute"/>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public CharSpanBuilder(int literalLength, int formattedCount, Span<char> initialBuffer)
    {
        _chars = initialBuffer;
        _charArray = null;
        _position = 0;
    }
#endif

    #region Grow
    /// <summary>
    /// Grow the size of <see cref="_chars"/> to at least the specified <paramref name="minCapacity"/>.
    /// </summary>
    /// <param name="minCapacity">The minimum possible Capacity to grow to</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GrowCore(int minCapacity)
    {
        Debug.Assert(minCapacity > BuilderHelper.MinimumCapacity);
        Debug.Assert(minCapacity > Capacity);

        char[] newArray = ArrayPool<char>.Shared.Rent(minCapacity);
        TextHelper.Unsafe.CopyTo(_chars, newArray, _position);

        char[]? toReturn = _charArray;
        _chars = _charArray = newArray;

        if (toReturn is not null)
        {
            ArrayPool<char>.Shared.Return(toReturn);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowBy(int addingCharCount)
    {
        Debug.Assert(addingCharCount > 0);
        GrowCore(BuilderHelper.GetGrowByCapacity(Capacity, addingCharCount));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowThenCopy(char ch)
    {
        int index = _position;
        GrowCore(BuilderHelper.GetGrowByCapacity(Capacity, 1));
        TextHelper.Unsafe.CopyBlock(
            in ch,
            ref _chars[index],
            1);
        _position = index + 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowThenCopy(scoped ReadOnlySpan<char> text)
    {
        int index = _position;
        int len = text.Length;
        GrowCore(BuilderHelper.GetGrowByCapacity(Capacity, len));
        TextHelper.Unsafe.CopyTo(text, _chars[index..], len);
        _position = index + len;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowThenCopy(string text)
    {
        int index = _position;
        int len = text.Length;
        GrowCore(BuilderHelper.GetGrowByCapacity(Capacity, len));
        TextHelper.Unsafe.CopyBlock(
            in text.GetPinnableReference(),
            ref _chars[index],
            len);
        _position = index + len;
    }
    #endregion

    #region Interpolated String Handler implementations
    /// <summary>
    /// Append a literal <see cref="string"/>
    /// </summary>
    /// <param name="text">The string to write.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLiteral(string? text)
    {
        if (text is null) return;
        int len = text.Length;
        // Quick implementations for common length 1 and 2 (symbols: [ /\,"' ], [/r/n])
        if (len == 1)
        {
            int pos = _position;
            Span<char> chars = _chars;
            if (pos < chars.Length)
            {
                chars[pos] = text[0];
                _position = pos + 1;
            }
            else
            {
                GrowThenCopy(text);
            }
        }
        else if (len == 2)
        {
            int pos = _position;
            Span<char> chars = _chars;
            if (pos < chars.Length - 1)
            {
                chars[pos++] = text[0];
                chars[pos++] = text[1];
                _position = pos;
            }
            else
            {
                GrowThenCopy(text);
            }
        }
        else
        {
            // Prefer Write
            Write(text);
        }
    }

    /// <summary>Writes the specified value to the handler.</summary>
    /// <param name="value">The value to write.</param>
    /// <typeparam name="T">The type of the value to write.</typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted<T>(T? value) => Write<T>(value);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted<T>(T value, string? format) => Format<T>(value, format);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted(char ch) => Write(ch);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted(scoped ReadOnlySpan<char> text) => Write(text);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted(string? text) => Write(text);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted(object? obj) => Write<object?>(obj);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted(object? value, string? format) => Format<object?>(value, format);
    #endregion

    public void Write(char ch)
    {
        int pos = _position;
        Span<char> chars = _chars;
        if (pos < chars.Length)
        {
            chars[pos] = ch;
            _position = pos + 1;
        }
        else
        {
            GrowThenCopy(ch);
        }
    }
    public void WriteLine(char ch)
    {
        Write(ch);
        Write(TextHelper.NewLineSpan);
    }

    public void Write(scoped ReadOnlySpan<char> text)
    {
        if (TextHelper.TryCopyTo(text, Available))
        {
            _position += text.Length;
        }
        else
        {
            GrowThenCopy(text);
        }
    }
    public void WriteLine(scoped ReadOnlySpan<char> text)
    {
        Write(text);
        Write(TextHelper.NewLineSpan);
    }

    public void Write(params char[] chars) => Write(chars.AsSpan());

    public void WriteLine() => Write(TextHelper.NewLineSpan);

    public void Write(string? text)
    {
        if (text is not null)
        {
            if (TextHelper.TryCopyTo(text, Available))
            {
                _position += text.Length;
            }
            else
            {
                GrowThenCopy(text);
            }
        }
    }
    public void WriteLine(string? text)
    {
        Write(text);
        Write(TextHelper.NewLineSpan);
    }

    public void Write<T>(T? value)
    {
        string? str;
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
                    GrowBy(BuilderHelper.MinimumCapacity);
                }
                _position += charsWritten;
                return;
            }
#endif

            // constrained call avoiding boxing for value types
            str = ((IFormattable)value).ToString(default, default);
        }
        else
        {
            str = value?.ToString();
        }

        Write(str);
    }
    public void WriteLine<T>(T? value)
    {
        Write<T>(value);
        Write(TextHelper.NewLineSpan);
    }

    public void Format<T>(T? value, string? format, IFormatProvider? provider = null)
    {
        string? str;
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
                    GrowBy(BuilderHelper.MinimumCapacity);
                }
                _position += charsWritten;
                return;
            }
#endif

            // constrained call avoiding boxing for value types
            str = ((IFormattable)value).ToString(format, provider);
        }
        else
        {
            str = value?.ToString();
        }

        Write(str);
    }
    public void FormatLine<T>(T? value, string? format, IFormatProvider? provider = null)
    {
        Format<T>(value, format, provider);
        Write(TextHelper.NewLineSpan);
    }

    public void Repeat(char ch, int count)
    {
        if (count > 0)
        {
            Span<char> buffer = stackalloc char[count];
            buffer.Fill(' ');
            if (TextHelper.TryCopyTo(buffer, Available))
            {
                _position += count;
            }
            else
            {
                GrowThenCopy(buffer);
            }
        }
    }

    /// <summary>
    /// Returns any rented array to the pool.
    /// </summary>
    public void Dispose()
    {
        char[]? toReturn = _charArray;
        this = default; // defensive clear
        if (toReturn is not null)
        {
            ArrayPool<char>.Shared.Return(toReturn);
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override bool Equals(object? obj) => throw new NotSupportedException();

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override int GetHashCode() => throw new NotSupportedException();

    public string ToStringAndDispose()
    {
        string result = ToString();
        Dispose();
        return result;
    }

    public override string ToString()
    {
        return Written.ToString();
    }
}