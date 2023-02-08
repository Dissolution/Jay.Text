using System.Buffers;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Jay.Text.Compat;

namespace Jay.Text;

public abstract class CharArrayBuilderBase :
    IList<char>, IReadOnlyList<char>,
    ICollection<char>, IReadOnlyCollection<char>,
    IEnumerable<char>, IEnumerable,
#if NET6_0_OR_GREATER
    ISpanFormattable, IFormattable,
#endif
    IDisposable
{
    protected static readonly string DefaultNewLine = Environment.NewLine;

    /// <summary>
    /// Rented char[] from pool
    /// </summary>
    private char[]? _charArray;

    /// <summary>
    /// Current position we're writing to
    /// </summary>
    private int _index;

    /// <inheritdoc cref="ICollection{T}"/>
    int ICollection<char>.Count => _index;
    /// <inheritdoc cref="IReadOnlyCollection{T}"/>
    int IReadOnlyCollection<char>.Count => _index;
    /// <inheritdoc cref="ICollection{T}"/>
    bool ICollection<char>.IsReadOnly => false;

    public char this[int index]
    {
        get => Written[index];
        set => Written[index] = value;
    }

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

    protected CharArrayBuilderBase()
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
    protected void AppendChar(char ch)
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

    protected void AppendCharSpan(ReadOnlySpan<char> text)
    {
        int len = text.Length;
        if (TextHelper.TryCopyTo(text, Available, len))
        {
            _index += len;
        }
        else
        {
            GrowThenCopy(text);
        }
    }

    protected void AppendString(string? text)
    {
        if (text is not null)
        {
            int len = text.Length;
            if (TextHelper.TryCopyTo(text, Available, len))
            {
                _index += len;
            }
            else
            {
                GrowThenCopy(text);
            }
        }
    }

    protected void AppendNonNullString(string text)
    {
        int len = text.Length;
        if (TextHelper.TryCopyTo(text, Available, len))
        {
            _index += len;
        }
        else
        {
            GrowThenCopy(text);
        }
    }

    protected void AppendValue<T>(T? value)
    {
        if (value is null) return;

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
        if (str is null) return;
        int len = str.Length;
        if (TextHelper.TryCopyTo(str, Available, len))
        {
            _index += len;
        }
        else
        {
            GrowThenCopy(str);
        }
    }

    protected void AppendFormat<T>(T? value, string? format)
    {
        if (value is null) return;

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

        if (str is null) return;
        int len = str.Length;
        if (TextHelper.TryCopyTo(str, Available, len))
        {
            _index += len;
        }
        else
        {
            GrowThenCopy(str);
        }
    }
#endregion

    public void EnsureCapacityToAdd(int adding)
    {
        if (adding > 0)
        {
            var newCapacity = _index + adding;
            if (newCapacity > Capacity)
            {
                GrowBy(adding);
            }
        }
    }

    public void RemoveAt(int index)
    {
        if ((uint)index >= (uint)_index)
            throw new IndexOutOfRangeException();
        Written.Slice(index + 1).CopyTo(Written.Slice(index));
        _index--;
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

    public void Clear()
    {
        _index = 0;
    }

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = default)
    {
        if (_index <= destination.Length)
        {
            TextHelper.Unsafe.CopyBlock(
                in _charArray!.GetPinnableReference(),
                ref destination.GetPinnableReference(),
                _index);
            charsWritten = _index;
            return true;
        }
        charsWritten = 0;
        return false;
    }


#region Interface Implementations
    void ICollection<char>.Add(char ch)
    {
        AppendChar(ch);
    }
    void IList<char>.Insert(int index, char ch)
    {
        Validate.Insert(_index, index);
        EnsureCapacityToAdd(1);
        Written.Slice(index).CopyTo(Written.Slice(index + 1));
        Written[index] = ch;
        _index++;
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
        for (var i = 0; i < _index; i++)
        {
            if (_charArray![i] == ch)
                return true;
        }
        return false;
    }
    int IList<char>.IndexOf(char ch)
    {
        for (var i = 0; i < _index; i++)
        {
            if (_charArray![i] == ch)
                return i;
        }
        return -1;
    }

    void ICollection<char>.CopyTo(char[] array, int arrayIndex)
    {
        Validate.Index(array.Length, arrayIndex);
        if (arrayIndex + _index > array.Length)
            throw new ArgumentException("Cannot contain text", nameof(array));
        Written.CopyTo(array.AsSpan(arrayIndex));
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        var chars = _charArray!;
        var len = _index;
        for (var i = 0; i < len; i++)
        {
            yield return chars[i];
        }
    }
    IEnumerator<char> IEnumerable<char>.GetEnumerator()
    {
        var chars = _charArray!;
        var len = _index;
        for (var i = 0; i < len; i++)
        {
            yield return chars[i];
        }
    }
#endregion

    /// <summary>
    /// Returns any rented array to the pool.
    /// </summary>
    public virtual void Dispose()
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

#if NET6_0_OR_GREATER
    string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => ToString();
#endif

    public override string ToString() => Written.AsString();
}

