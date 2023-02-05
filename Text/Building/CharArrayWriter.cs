using System.Buffers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Jay.Text.Compat;
using Jay.Text.Extensions;

// ReSharper disable UnusedParameter.Local

namespace Jay.Text;

public sealed class CharArrayWriter : IDisposable
{
    /// <summary>
    /// Rented char[] from pool
    /// </summary>
    private char[]? _charArray;
    
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
        get => _charArray.AsSpan(0, _index);
    }

    /// <summary>
    /// Gets a <c>Span&lt;<see cref="char"/>&gt;</c> of characters available for writing<br/>
    /// <b>Caution</b>: If you write to Available, you must also update Length!
    /// </summary>
    public Span<char> Available
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _charArray.AsSpan(_index);
    }

    /// <summary>
    /// The current total capacity to store <see cref="char"/>acters<br/>
    /// Will be increased when required during Write operations
    /// </summary>
    public int Capacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _charArray!.Length;
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
    public CharArrayWriter()
    {
        _charArray = ArrayPool<char>.Shared.Rent(BuilderHelper.MinimumCapacity);
        _index = 0;
    }


    #region Grow

    /// <summary>
    /// Grow the size of <see cref="_charArray"/> to at least the specified <paramref name="minCapacity"/>.
    /// </summary>
    /// <param name="minCapacity">The minimum possible Capacity to grow to</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GrowCore(int minCapacity)
    {
        char[] newArray = ArrayPool<char>.Shared.Rent(minCapacity);
        TextHelper.Unsafe.CopyBlock(
            in _charArray!.GetPinnableReference(),
            ref newArray.GetPinnableReference(),
            _index);

        char[]? toReturn = _charArray;
        _charArray = newArray;

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
            ref _charArray![index],
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
            ref _charArray![index],
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
            ref _charArray![index],
            len);
        _index = index + len;
    }

    #endregion

    #region Write

    public void Write(char ch)
    {
        int pos = _index;
        Span<char> chars = _charArray;
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
        _charArray = null;
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
            fixed (char* ptr = _charArray)
            {
                return new string(ptr, 0, _index);
            }
        }
#else
        return new string(Written);
#endif
    }
}