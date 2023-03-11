/*
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Jay.Text.Utilities;

public class PoolList<T> :
    IList<T>, IReadOnlyList<T>,
    ICollection<T>, IReadOnlyCollection<T>,
    IEnumerable<T>, IEnumerable,
    IDisposable
{
    private T[] _rentedArray;
    private int _length;

    /// <inheritdoc cref="ICollection{T}"/>
    public bool IsReadOnly => false;

    /// <inheritdoc cref="IList{T}"/>
    T IList<T>.this[int index]
    {
        get => this[index];
        set => this[index] = value;
    }

    /// <inheritdoc cref="IReadOnlyList{T}"/>
    T IReadOnlyList<T>.this[int index]
    {
        get => this[index];
    }

    public ref T this[int index]
    {
        get
        {
            Validate.Index(_length, index);
            return ref _rentedArray[index];
        }
    }

    /// <summary>
    /// The current total capacity to store <see cref="char"/>acters<br/>
    /// Will be increased when required during Write operations
    /// </summary>
    public int Capacity
    {
        get => _rentedArray.Length;
    }

    /// <summary>
    /// Gets or sets the number of <see cref="char"/>acters written 
    /// </summary>
    /// <remarks>
    /// A set Length will be clamped between 0 and Capacity
    /// </remarks>
    public int Count
    {
        get => _length;
        protected internal set
        {
            if (value < _length)
            {
                // Cap at 0
                int newLength = Math.Max(value, 0);
                // Clear the values removed
                _rentedArray[newLength..].Initialize();
                _length = newLength;
            }
            else if (value == _length)
            {
                // No change
                return;
            }
            else
            {
                // Cap at Capacity
                _length = Math.Min(value, Capacity);
            }
        }
    }

    public PoolList(int minCapacity = BuilderHelper.MinimumCapacity)
    {
        _rentedArray = ArrayPool<T>.Shared.Rent(minCapacity);
        _length = 0;
    }

    #region Grow

    /// <summary>
    /// Grow the size of <see cref="_rentedArray"/> to at least the specified <paramref name="minCapacity"/>.
    /// </summary>
    /// <param name="minCapacity">The minimum possible Capacity to grow to -- already validated</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GrowCore(int minCapacity)
    {
        Debug.Assert(minCapacity >= BuilderHelper.MinimumCapacity);
        Debug.Assert(minCapacity <= BuilderHelper.MaximumCapacity);
        Debug.Assert(minCapacity > Capacity);

        T[] newArray = ArrayPool<T>.Shared.Rent(minCapacity);
        _rentedArray.AsSpan(0, _length).CopyTo(newArray.AsSpan());

        T[] returnArray = _rentedArray;
        _rentedArray = newArray;
        ArrayPool<T>.Shared.Return(returnArray, true);
    }

    /// <summary>
    /// Grows the <see cref="Capacity"/> by at least the <paramref name="addingCharCount"/>
    /// </summary>
    /// <param name="addingCharCount">The minimum number of characters to increase the <see cref="Capacity"/> by</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void GrowBy(int addingCharCount)
    {
        if (addingCharCount > 0)
        {
            GrowCore(BuilderHelper.GetGrowByCapacity(Capacity, addingCharCount));
        }
    }

    /// <summary>
    /// Grows the <see cref="Capacity"/> to at least the <paramref name="minCapacity"/>
    /// </summary>
    /// <param name="minCapacity">The minimum the <see cref="Capacity"/> must be</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void GrowTo(int minCapacity)
    {
        if (minCapacity > 0)
        {
            GrowCore(BuilderHelper.GetGrowToCapacity(Capacity, minCapacity));
        }
    }

    #endregion

    #region Allocate

    /// <summary>
    /// Allocates space for a new <typeparamref name="T"/>, 
    /// increases <see cref="Count"/> by 1, 
    /// and returns a <c>ref</c> to the allocated <typeparamref name="T"/> item
    /// </summary>
    /// <returns>A <c>ref</c> to the allocated <typeparamref name="T"/> item</returns>
    public ref T Allocate()
    {
        int curLen = _length;
        int newLen = curLen + 1;
        // Check for growth
        if (newLen > Capacity)
        {
            GrowBy(1);
        }

        // Add to our current length
        _length = newLen;
        // Return the allocated item
        return ref _rentedArray[curLen];
    }

    /// <summary>
    /// Allocates space for <paramref name="length"/> <typeparamref name="T"/> items,
    /// increases <see cref="Count"/> by <paramref name="length"/>,
    /// and returns the allocated <see cref="Span{T}"/>
    /// </summary>
    /// <param name="length">The number of items to allocate space for</param>
    /// <returns>A <see cref="Span{T}"/> to the allocated <typeparamref name="T"/> items</returns>
    public Span<T> Allocate(int length)
    {
        if (length > 0)
        {
            int curLen = _length;
            int newLen = curLen + length;
            // Check for growth
            if (newLen > Capacity)
            {
                GrowBy(length);
            }

            // Add to our current length
            _length = newLen;

            // Return the allocated items
            return _rentedArray.AsSpan(curLen, length);
        }

        // Asked for nothing
        return Span<T>.Empty;
    }

    /// <summary>
    /// Allocates space for a new <typeparamref name="T"/> at <paramref name="index"/>,
    /// shifts existing items to make an empty hole,
    /// increases <see cref="Count"/> by 1,
    /// and returns a <c>ref</c> to that allocated <typeparamref name="T"/> item
    /// </summary>
    /// <param name="index">The index to allocate an item at</param>
    /// <returns>A <c>ref</c> to the allocated <typeparamref name="T"/> item at <paramref name="index"/></returns>
    public ref T AllocateAt(int index)
    {
        int curLen = _length;
        Validate.Insert(curLen, index);
        int newLen = curLen + 1;

        // Check for growth
        if (newLen > Capacity)
        {
            GrowBy(1);
        }

        // We're adding this much
        _length = newLen;

        // At end?
        if (index == curLen)
        {
            // The same as Allocate()
            return ref _rentedArray[curLen];
        }

        // Insert

        // Shift existing to the right
        var keep = _rentedArray.AsSpan()[new Range(start: index, end: curLen)];
        // We know we have enough space to grow to
        var keepDestination = _rentedArray.AsSpan(index + 1, keep.Length);
        // Copy
        keep.CopyTo(keepDestination);
        // return where we allocated
        return ref _rentedArray[index];
    }

    /// <summary>
    /// Allocates space for <paramref name="length"/> <typeparamref name="T"/> items at <paramref name="index"/>,
    /// shifts existing items to make an empty hole,
    /// increases <see cref="Count"/> by <paramref name="length"/>
    /// and returns the allocated <see cref="Span{T}"/>
    /// </summary>
    /// <param name="index">The index to allocate the items at</param>
    /// <param name="length">The number of items to allocate space for</param>
    /// <returns>A <see cref="Span{T}"/> to the allocated <typeparamref name="T"/> items</returns>
    public Span<T> AllocateAt(int index, int length)
    {
        int curLen = _length;
        Validate.Insert(curLen, index);
        if (length > 0)
        {
            int newLen = curLen + length;

            // Check for growth
            if (newLen > Capacity)
            {
                GrowBy(length);
            }

            // We're adding this much
            _length = newLen;

            // At end?
            if (index == curLen)
            {
                // The same as Allocate(length)
                return _rentedArray.AsSpan(curLen, length);
            }
            // Insert

            // Shift existing to the right
            var keep = _rentedArray.AsSpan()[new Range(start: index, end: curLen)];
            // We know we have enough space to grow to
            var keepDest = _rentedArray.AsSpan(index + length, keep.Length);
            keep.CopyTo(keepDest);
            // return where we allocated
            return _rentedArray.AsSpan(index, length);
        }

        // Asked for nothing
        return Span<T>.Empty;
    }

    #endregion

    #region Remove
    void IList<T>.RemoveAt(int index)
    {
        TryRemove(index);
    }

    bool ICollection<T>.Remove(T item)
    {
        var span = AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            if (EqualityComparer<T>.Default.Equals(span[i], item))
            {
                TryRemove(i);
                return true;
            }
        }
        return false;
    }

    public bool TryRemove(Range range)
    {
        int currentCount = Count;
        (int offset, int length) = range.GetOffsetAndLength(currentCount);
        if ((uint)offset + (uint)length <= (uint)currentCount)
        {
            // Everything we're keeping after the cut
            var keep = _rentedArray.AsSpan()[new Range(start: offset + length, end: currentCount)];
            // The place to put it starts where the cut does
            var keepDest = _rentedArray.AsSpan(offset, keep.Length);
            // Copy right -> left, removing the range
            keep.CopyTo(keepDest);
            // Set our new length
            Count = currentCount - length;
            // We removed
            return true;
        }
        else
        {
            // Cannot remove, bad range
            return false;
        }
    }
    public bool TryRemove(int index, int count = 1)
    {
        return TryRemove(new Range(Index.FromStart(index), Index.FromStart(index + count)));
    }

    #endregion


    public void Add(T item)
    {
        Allocate() = item;
    }

    public void Add(params T[] items)
    {
        var dest = Allocate(items.Length);
        items.CopyTo(dest);
    }

    public void Add(ReadOnlySpan<T> items)
    {
        var dest = Allocate(items.Length);
        items.CopyTo(dest);
    }

    public void Insert(int index, T item)
    {
        AllocateAt(index) = item;
    }

    public void Insert(int index, params T[] items)
    {
        var dest = AllocateAt(index, items.Length);
        items.CopyTo(dest);
    }
    public void Insert(int index, ReadOnlySpan<T> items)
    {
        var dest = AllocateAt(index, items.Length);
        items.CopyTo(dest);
    }


    #region Indexing
    bool ICollection<T>.Contains(T item)
    {

    }

    int IList<T>.IndexOf(T item)
    {
        int i = Written.IndexOf(ch);
        if (i >= 0)
        {
            return i;
        }

        return -1;
    }

    public int FirstIndexOf(T item, IEqualityComparer<T>? comparer = default)
    {
        var span = AsSpan();
        comparer ??= EqualityComparer<T>.Default;
        for (var i = 0; i < span.Length; i++)
        {
            if (comparer.Equals(span[i], item))
            {
                return i;
            }
        }
        return -1;
    }

    public int LastIndexOf(T item, IEqualityComparer<T>? comparer = default)
    {
        var span = AsSpan();
        comparer ??= EqualityComparer<T>.Default;
        for (var i = span.Length - 1; i >= 0; i--)
        {
            if (comparer.Equals(span[i], item))
            {
                return i;
            }
        }
        return -1;
    }

    public int NextIndexOf(T item, int startIndex, IEqualityComparer<T>? comparer = default)
    {
        if ((uint)startIndex >= _length) return -1;
        var span = AsSpan();
        comparer ??= EqualityComparer<T>.Default;
        for (var i = startIndex; i < span.Length; i++)
        {
            if (comparer.Equals(span[i], item))
            {
                return i;
            }
        }
        return -1;
    }

    public int PrevIndexOf(T item, int startIndex, IEqualityComparer<T>? comparer = default)
    {
        if ((uint)startIndex >= _length) return -1;
        var span = AsSpan();
        comparer ??= EqualityComparer<T>.Default;
        for (var i = span.Length - 1; i >= 0; i--)
        {
            if (comparer.Equals(span[i], item))
            {
                return i;
            }
        }
        return -1;
    }
    #endregion


    void ICollection<T>.CopyTo(T[] array, int arrayIndex)
    {
        Validate.CopyTo(_length, array, arrayIndex);
        AsSpan().CopyTo(array.AsSpan(arrayIndex));
    }

    public bool TryCopyTo(Span<T> destination)
    {
        return AsSpan().TryCopyTo(destination);
    }

    public void Clear()
    {
        // Happy Hack!
        Count = 0;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        var array = _rentedArray;
        var len = _length;
        for (var i = 0; i < len; i++)
        {
            yield return array[i];
        }
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        var chars = _rentedArray;
        var len = _length;
        for (var i = 0; i < len; i++)
        {
            yield return chars[i];
        }
    }

    public struct Enumerator : IEnumerator<T>, IEnumerator
    {
        private readonly T[] _items;
        private int _index;
        private T _currentItem;

        public T Current => _currentItem;
    }

#if NET6_0_OR_GREATER
    bool ISpanFormattable.TryFormat(Span<T> destination, out int charsWritten,
        ReadOnlySpan<T> format,
        IFormatProvider? provider)
    {
        int curLen = _length;
        if (curLen <= destination.Length)
        {
            TextHelper.Unsafe.CopyBlock(
                in _rentedArray.GetPinnableReference(),
                ref destination.GetPinnableReference(),
                curLen);
            charsWritten = curLen;
            return true;
        }

        charsWritten = 0;
        return false;
    }
    string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => ToString();
#endif

#endregion

    public Span<T> AsSpan()
    {
        return _rentedArray.AsSpan(0, _length);
    }

    public ReadOnlySpan<T> AsReadOnlySpan()
    {
        return _rentedArray.AsSpan(0, _length);
    }

    /// <summary>
    /// Returns any rented array to the pool.
    /// </summary>
    public virtual void Dispose()
    {
        char[]? toReturn = _rentedArray;
        _rentedArray = null!;
        if (toReturn is not null)
        {
            ArrayPool<T>.Shared.Return(toReturn);
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

    public override string ToString() => Written.ToString();
}
*/
