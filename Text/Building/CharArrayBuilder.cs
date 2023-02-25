using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Jay.Text.Compat;

namespace Jay.Text;

public sealed class CharArrayBuilder :
    IList<char>, IReadOnlyList<char>,
    ICollection<char>, IReadOnlyCollection<char>,
    IEnumerable<char>, IEnumerable,
#if NET6_0_OR_GREATER
    ISpanFormattable, IFormattable,
#endif
    IDisposable
{
    /// <summary>
    /// Rented char[] from pool
    /// </summary>
    private char[]? _charArray;
    
    /// <summary>
    /// Current position we're writing to
    /// </summary>
    private int _position;


    /// <inheritdoc cref="ICollection{T}"/>
    int ICollection<char>.Count => _position;

    /// <inheritdoc cref="IReadOnlyCollection{T}"/>
    int IReadOnlyCollection<char>.Count => _position;

    /// <inheritdoc cref="ICollection{T}"/>
    bool ICollection<char>.IsReadOnly => false;

    /// <inheritdoc cref="IList{T}"/>
    char IList<char>.this[int index]
    {
        get => Written[index];
        set => Written[index] = value;
    }
    /// <inheritdoc cref="IReadOnlyList{T}"/>
    char IReadOnlyList<char>.this[int index]
    {
        get => Written[index];
    }

    /// <summary>
    /// Gets a <c>Span&lt;<see cref="char"/>&gt;</c> of characters written thus far
    /// </summary>
    public Span<char> Written
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _charArray.AsSpan(0, _position);
    }

    /// <summary>
    /// Gets a <c>Span&lt;<see cref="char"/>&gt;</c> of characters available for writing<br/>
    /// <b>Caution</b>: If you write to Available, you must also update Length!
    /// </summary>
    public Span<char> Available
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _charArray.AsSpan(_position);
    }

    public Span<char> CharSpan => _charArray.AsSpan();

    /// <summary>
    /// The current total capacity to store <see cref="char"/>acters<br/>
    /// Will be increased when required during Write operations
    /// </summary>
    public int Capacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _charArray?.Length ?? 0;
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

    public CharArrayBuilder()
    {
        _charArray = ArrayPool<char>.Shared.Rent(BuilderHelper.MinimumCapacity);
        _position = 0;
    }

#region Grow

    /// <summary>
    /// Grow the size of <see cref="_charArray"/> to at least the specified <paramref name="minCapacity"/>.
    /// </summary>
    /// <param name="minCapacity">The minimum possible Capacity to grow to -- already validated</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GrowCore(int minCapacity)
    {
        Debug.Assert(minCapacity > BuilderHelper.MinimumCapacity);
        Debug.Assert(minCapacity > Capacity);

        char[] newArray = ArrayPool<char>.Shared.Rent(minCapacity);
        TextHelper.Unsafe.CopyBlock(
            in _charArray!.GetPinnableReference(),
            ref newArray.GetPinnableReference(),
            _position);

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
        Debug.Assert(addingCharCount > 0);
        GrowCore(BuilderHelper.GetCapacityToAdd(Capacity, addingCharCount));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowThenCopy(char ch)
    {
        int index = _position;
        GrowCore(BuilderHelper.GetCapacityToAdd(Capacity, 1));
        TextHelper.Unsafe.CopyBlock(
            in ch,
            ref _charArray![index],
            1);
        _position = index + 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowThenCopy(ReadOnlySpan<char> text)
    {
        int index = _position;
        int len = text.Length;
        GrowCore(BuilderHelper.GetCapacityToAdd(Capacity, len));
        TextHelper.Unsafe.CopyBlock(
            in text.GetPinnableReference(),
            ref _charArray![index],
            len);
        _position = index + len;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowThenCopy(string text)
    {
        int index = _position;
        int len = text.Length;
        GrowCore(BuilderHelper.GetCapacityToAdd(Capacity, len));
        TextHelper.Unsafe.CopyBlock(
            in text.GetPinnableReference(),
            ref _charArray![index],
            len);
        _position = index + len;
    }

#endregion

#region Capacity Modification

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void EnsureCapacityMin(int minCapacity)
    {
        int currentCapacity = Capacity;
        if (currentCapacity < minCapacity)
        {
            GrowCore(BuilderHelper.GetCapacityMin(currentCapacity, minCapacity));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void EnsureCapacityToAdd(int adding)
    {
        if (adding > 0)
        {
            int currentCapacity = Capacity;
            if (_position + adding > currentCapacity)
            {
                GrowCore(BuilderHelper.GetCapacityToAdd(currentCapacity, adding));
            }
        }
    }

#endregion

#region Allocate

    /// <summary>
    /// Allocates a <c>Span&lt;char&gt;</c> of the given <paramref name="length"/>, updates this builder's <see cref="Length"/> and returns the allocated span
    /// </summary>
    public Span<char> Allocate(int length)
    {
        if (length > 0)
        {
            int curLen = _position;
            int newLen = curLen + length;
            if (newLen > Capacity)
            {
                GrowBy(length);
            }

            _position = newLen;
            return _charArray.AsSpan(curLen, length);
        }

        return default;
    }

    public Span<char> AllocateAt(int index, int length)
    {
        int curLen = _position;
        Validate.Insert(curLen, index);
        if (length > 0)
        {
            // Check for growth
            int newLen = curLen + length;
            if (newLen > Capacity)
            {
                GrowBy(length);
            }

            // We're adding this much
            _position = newLen;
            // At end?
            if (index == curLen)
            {
                // The same as Allocate(length)
                return _charArray.AsSpan(curLen, length);
            }
            // Insert
            else
            {
                Span<char> all = _charArray.AsSpan();
                // Shift existing to the right
                TextHelper.Unsafe.CopyBlock(source: all[new Range(index, curLen)], all[(index + length)..]);

                // return where we allocated
                return _charArray.AsSpan(index, length);
            }
        }

        return Span<char>.Empty;
    }

#endregion

#region Write

    public void Write(char ch)
    {
        int pos = _position;
        Span<char> chars = _charArray;
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

    public void Write(ReadOnlySpan<char> text)
    {
        int len = text.Length;
        if (TextHelper.TryCopyTo(text, Available, len))
        {
            _position += len;
        }
        else
        {
            GrowThenCopy(text);
        }
    }

    public void Write(string? text)
    {
        if (text is not null)
        {
            int len = text.Length;
            if (TextHelper.TryCopyTo(text, Available, len))
            {
                _position += len;
            }
            else
            {
                GrowThenCopy(text);
            }
        }
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

    public void Write<T>(T? value, string? format, IFormatProvider? provider = null)
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

#endregion


    public void RemoveAt(int index)
    {
        if ((uint)index >= (uint)_position)
            throw new IndexOutOfRangeException();
        Written.Slice(index + 1).CopyTo(Written.Slice(index));
        _position--;
    }

    // Trim Methods
    public void TrimEnd()
    {
        var written = Written;
        for (var i = written.Length - 1; i >= 0; i--)
        {
            if (!char.IsWhiteSpace(written[i]))
            {
                this.Length = (i + 1);
                return;
            }
        }

        this.Length = 0;
    }

    public void TrimEnd(ReadOnlySpan<char> trimSpan)
    {
        if (Written.EndsWith(trimSpan))
        {
            this.Length -= trimSpan.Length;
        }
    }

    public void Clear()
    {
        _position = 0;
    }

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default,
        IFormatProvider? provider = default)
    {
        if (_position <= destination.Length)
        {
            TextHelper.Unsafe.CopyBlock(
                in _charArray!.GetPinnableReference(),
                ref destination.GetPinnableReference(),
                _position);
            charsWritten = _position;
            return true;
        }

        charsWritten = 0;
        return false;
    }


#region Interface Implementations

    void ICollection<char>.Add(char ch)
    {
        Write(ch);
    }

    void IList<char>.Insert(int index, char ch)
    {
        Validate.Insert(_position, index);
        EnsureCapacityToAdd(1);
        Written.Slice(index).CopyTo(Written.Slice(index + 1));
        Written[index] = ch;
        _position++;
    }

    bool ICollection<char>.Remove(char ch)
    {
        int i = Written.IndexOf(ch);
        if (i >= 0)
        {
            RemoveAt(i);
            return true;
        }

        return false;
    }

    bool ICollection<char>.Contains(char ch)
    {
        for (var i = 0; i < _position; i++)
        {
            if (_charArray![i] == ch)
                return true;
        }

        return false;
    }

    int IList<char>.IndexOf(char ch)
    {
        for (var i = 0; i < _position; i++)
        {
            if (_charArray![i] == ch)
                return i;
        }

        return -1;
    }

    void ICollection<char>.CopyTo(char[] array, int arrayIndex)
    {
        Validate.Index(array.Length, arrayIndex);
        if (arrayIndex + _position > array.Length)
            throw new ArgumentException("Cannot contain text", nameof(array));
        Written.CopyTo(array.AsSpan(arrayIndex));
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        var chars = _charArray!;
        var len = _position;
        for (var i = 0; i < len; i++)
        {
            yield return chars[i];
        }
    }

    IEnumerator<char> IEnumerable<char>.GetEnumerator()
    {
        var chars = _charArray!;
        var len = _position;
        for (var i = 0; i < len; i++)
        {
            yield return chars[i];
        }
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
    public override bool Equals(object? obj) => throw new NotSupportedException();

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override int GetHashCode() => throw new NotSupportedException();

    public string ToStringAndDispose()
    {
        string result = ToString();
        Dispose();
        return result;
    }

#if NET6_0_OR_GREATER
    string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => ToString();
#endif

    public override string ToString() => Written.AsString();
}