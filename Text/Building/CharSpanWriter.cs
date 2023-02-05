using System.Buffers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Jay.Text.Compat;
using Jay.Text.Extensions;

// ReSharper disable UnusedParameter.Local

namespace Jay.Text;

/// <summary>
/// A custom minimized implementation of an <c>Interpolated String Handler</c>
/// </summary>
#if NET6_0_OR_GREATER
[InterpolatedStringHandler]
#endif
public ref struct CharSpanWriter
{
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
    private int _index;

    /// <summary>
    /// Gets a <c>Span&lt;<see cref="char"/>&gt;</c> of characters written thus far
    /// </summary>
    public Span<char> Written
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _chars.Slice(0, _index);
    }

    /// <summary>
    /// Gets a <c>Span&lt;<see cref="char"/>&gt;</c> of characters available for writing<br/>
    /// <b>Caution</b>: If you write to Available, you must also update Length!
    /// </summary>
    public Span<char> Available
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _chars.Slice(_index);
    }

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
        get => _index;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _index = value.Clamp(0, Capacity);
    }


    /// <summary>
    /// Construct a new <see cref="CharSpanWriter"/> with default starting Capacity
    /// </summary>
    public CharSpanWriter()
    {
        _chars = _charArray = ArrayPool<char>.Shared.Rent(BuilderHelper.MinimumCapacity);
        _index = 0;
    }

    /// <summary>
    /// Construct a new <see cref="CharSpanWriter"/> with an <paramref name="initialBuffer"/>,<br/>
    /// which will be discarded if Capacity increases
    /// </summary>
    /// <param name="initialBuffer">The initial <c>Span&lt;<see cref="char"/>&gt;</c> to write to</param>
    public CharSpanWriter(Span<char> initialBuffer)
    {
        _chars = initialBuffer;
        _charArray = null;
        _index = 0;
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Support for <see cref="InterpolatedStringHandlerAttribute"/>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public CharSpanWriter(int literalLength, int formattedCount)
    {
        _chars = _charArray = ArrayPool<char>.Shared
            .Rent(BuilderHelper.GetStartingCapacity(literalLength, formattedCount));
        _index = 0;
    }

    /// <summary>
    /// Support for <see cref="InterpolatedStringHandlerAttribute"/>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public CharSpanWriter(int literalLength, int formattedCount, Span<char> initialBuffer)
    {
        _chars = initialBuffer;
        _charArray = null;
        _index = 0;
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
        char[] newArray = ArrayPool<char>.Shared.Rent(minCapacity);
        TextHelper.Unsafe.CopyBlock(
            in _chars.GetPinnableReference(),
            ref newArray.GetPinnableReference(),
            _index);

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
        GrowCore(BuilderHelper.GetCapacityAdding(Capacity, addingCharCount));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowThenCopy(char ch)
    {
        int index = _index;
        GrowCore(BuilderHelper.GetCapacityAdding(Capacity, 1));
        TextHelper.Unsafe.CopyBlock(
            in ch,
            ref _chars[index],
            1);
        _index = index + 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowThenCopy(ReadOnlySpan<char> text)
    {
        int index = _index;
        int len = text.Length;
        GrowCore(BuilderHelper.GetCapacityAdding(Capacity, len));
        TextHelper.Unsafe.CopyBlock(
            in text.GetPinnableReference(),
            ref _chars[index],
            len);
        _index = index + len;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowThenCopy(string text)
    {
        int index = _index;
        int len = text.Length;
        GrowCore(BuilderHelper.GetCapacityAdding(Capacity, len));
        TextHelper.Unsafe.CopyBlock(
            in text.GetPinnableReference(),
            ref _chars[index],
            len);
        _index = index + len;
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
            int pos = _index;
            Span<char> chars = _chars;
            if (pos < chars.Length)
            {
                chars[pos] = text[0];
                _index = pos + 1;
            }
            else
            {
                GrowThenCopy(text);
            }
        }
        else if (len == 2)
        {
            int pos = _index;
            Span<char> chars = _chars;
            if (pos < chars.Length - 1)
            {
                chars[pos++] = text[0];
                chars[pos++] = text[1];
                _index = pos;
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
    public void AppendFormatted<T>(T? value) => WriteValue<T>(value);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted<T>(T value, string? format) => WriteFormat<T>(value, format);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted(char ch) => Write(ch);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted(ReadOnlySpan<char> text) => Write(text);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted(string? text) => Write(text);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted(object? obj) => WriteValue(obj);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted(object? value, string? format) => WriteFormat<object?>(value, format);

    #endregion

    #region Write

    public void Write(char ch)
    {
        int pos = _index;
        Span<char> chars = _chars;
        if (pos < chars.Length)
        {
            chars[pos] = ch;
            _index = pos + 1;
        }
        else
        {
            GrowThenCopy(ch);
        }
    }

    public void Write(ReadOnlySpan<char> text)
    {
        if (text.TryCopyTo(Available))
        {
            _index += text.Length;
        }
        else
        {
            GrowThenCopy(text);
        }
    }

    public void Write(params char[] chars) => Write(chars.AsSpan());

    public void Write(string? text)
    {
        if (text is not null)
        {
            if (TextHelper.TryCopyTo(text, Available))
            {
                _index += text.Length;
            }
            else
            {
                GrowThenCopy(text);
            }
        }
    }

    public void WriteValue<T>(T? value)
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
                _index += charsWritten;
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

    public void WriteFormat<T>(T? value, string? format)
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
                while (!((ISpanFormattable)value).TryFormat(Available, out charsWritten, format, default))
                {
                    GrowBy(BuilderHelper.MinimumCapacity);
                }
                _index += charsWritten;
                return;
            }
#endif

            // constrained call avoiding boxing for value types
            str = ((IFormattable)value).ToString(format, default);
        }
        else
        {
            str = value?.ToString();
        }

        Write(str);
    }

    #endregion


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
    public override bool Equals(object? obj) => throw new NotImplementedException();

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override int GetHashCode() => throw new NotImplementedException();

    public string ToStringAndDispose()
    {
        string result = ToString();
        Dispose();
        return result;
    }

    public override string ToString()
    {
#if NET48 || NETSTANDARD2_0
        unsafe
        {
            fixed (char* ptr = _chars)
            {
                return new string(ptr, 0, _index);
            }
        }
#else
        return new string(Written);
#endif
    }
}