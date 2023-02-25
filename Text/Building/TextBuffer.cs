using System.Buffers;
using System.ComponentModel;
using Jay.Text.Compat;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Jay.Text.Building
{
    public class TextBuffer :
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
        private char[] _buffer;

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
            get => this[index];
            set => this[index] = value;
        }

        /// <inheritdoc cref="IReadOnlyList{T}"/>
        char IReadOnlyList<char>.this[int index]
        {
            get => this[index];
        }

        public ref char this[int index]
        {
            get
            {
                Validate.Index(_position, index);
                return ref _buffer[index];
            }
        }

        /// <summary>
        /// Gets a <c>Span&lt;<see cref="char"/>&gt;</c> of characters written thus far
        /// </summary>
        public Span<char> Written
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer.AsSpan(0, _position);
        }

        /// <summary>
        /// Gets a <c>Span&lt;<see cref="char"/>&gt;</c> of characters available for writing<br/>
        /// <b>Caution</b>: If you write to Available, you must also update Length!
        /// </summary>
        public ReadOnlySpan<char> Available
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer.AsSpan(_position);
        }


        /// <summary>
        /// The current total capacity to store <see cref="char"/>acters<br/>
        /// Will be increased when required during Write operations
        /// </summary>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer.Length;
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

        public TextBuffer()
        {
            _buffer = ArrayPool<char>.Shared.Rent(BuilderHelper.MinimumCapacity);
            _position = 0;
        }

#region Grow

        /// <summary>
        /// Grow the size of <see cref="_buffer"/> to at least the specified <paramref name="minCapacity"/>.
        /// </summary>
        /// <param name="minCapacity">The minimum possible Capacity to grow to -- already validated</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GrowCore(int minCapacity)
        {
            Debug.Assert(minCapacity >= BuilderHelper.MinimumCapacity);
            Debug.Assert(minCapacity > Capacity);
            Debug.Assert(Capacity <= BuilderHelper.MaximumCapacity);

            char[] newArray = ArrayPool<char>.Shared.Rent(minCapacity);
            TextHelper.Unsafe.CopyBlock(
                in _buffer.GetPinnableReference(),
                ref newArray.GetPinnableReference(),
                _position);

            char[] toReturn = _buffer;
            _buffer = newArray;
            ArrayPool<char>.Shared.Return(toReturn);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void GrowBy(int addingCharCount)
        {
            if (addingCharCount > 0)
            {
                GrowCore(BuilderHelper.GetCapacityToAdd(Capacity, addingCharCount));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void GrowTo(int minCapacity)
        {
            if (minCapacity > 0)
            {
                GrowCore(BuilderHelper.GetCapacityMin(Capacity, minCapacity));
            }
        }

#endregion

#region Allocate

        public ref char Allocate()
        {
            int curLen = _position;
            int newLen = curLen + 1;
            // Check for growth
            if (newLen > Capacity)
            {
                GrowBy(1);
            }

            // Add to our current position
            _position = newLen;
            // Return the allocated (at end of Written)
            return ref _buffer[curLen];
        }

        /// <summary>
        /// Allocates a <c>Span&lt;char&gt;</c> of the given <paramref name="length"/>, updates this builder's <see cref="Length"/> and returns the allocated span
        /// </summary>
        public Span<char> Allocate(int length)
        {
            if (length > 0)
            {
                int curLen = _position;
                int newLen = curLen + length;
                // Check for growth
                if (newLen > Capacity)
                {
                    GrowBy(length);
                }

                // Add to our current position
                _position = newLen;
                // Return the allocated (at end of Written)
                return _buffer.AsSpan(curLen, length);
            }

            // Asked for nothing
            return default;
        }

        public ref char AllocateAt(int index)
        {
            int curLen = _position;
            Validate.Insert(curLen, index);
            int newLen = curLen + 1;

            // Check for growth
            if (newLen > Capacity)
            {
                GrowBy(1);
            }

            // We're adding this much
            _position = newLen;

            // At end?
            if (index == curLen)
            {
                // The same as Allocate()
                return ref _buffer[curLen];
            }
            // Insert
            else
            {
                // Shift existing to the right
                var keep = _buffer.AsSpan(new Range(start: index, end: curLen));
                var keepLength = keep.Length;
                // We know we have enough space to grow to
                var rightBuffer = _buffer.AsSpan(index + 1, keepLength);
                TextHelper.Unsafe.CopyBlock(
                    source: keep,
                    dest: rightBuffer,
                    sourceLen: keepLength);
                // return where we allocated
                return ref _buffer[index];
            }
        }

        public Span<char> AllocateAt(int index, int length)
        {
            int curLen = _position;
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
                _position = newLen;

                // At end?
                if (index == curLen)
                {
                    // The same as Allocate(length)
                    return _buffer.AsSpan(curLen, length);
                }
                // Insert
                else
                {
                    // Shift existing to the right
                    var rightSide = _buffer.AsSpan(new Range(start: index, end: curLen));
                    var rsLen = rightSide.Length;
                    // We know we have enough space to grow to
                    var rightBuffer = _buffer.AsSpan(index + length, rsLen);
                    TextHelper.Unsafe.CopyBlock(
                        source: rightSide,
                        dest: rightBuffer,
                        sourceLen: rsLen);
                    // return where we allocated
                    return _buffer.AsSpan(index, length);
                }
            }

            // Asked for nothing
            return Span<char>.Empty;
        }

#endregion

#region Remove

        public void Remove(int index)
        {
            int curLen = _position;
            Validate.Index(curLen, index);
            // Everything we're keeping after the cut
            var keep = _buffer.AsSpan(new Range(start: index + 1, end: curLen));
            var keepLen = keep.Length;
            // The place to put it at the cut
            var rightBuffer = _buffer.AsSpan(index, keepLen);
            TextHelper.Unsafe.CopyBlock(keep, rightBuffer, keepLen);
            // Length is shorter
            _position = curLen - 1;
        }

        public void Remove(int index, int length)
        {
            int curLen = _position;
            Validate.Range(curLen, index, length);
            // Everything we're keeping after the cut
            var keep = _buffer.AsSpan(new Range(start: index + length, end: curLen));
            var keepLen = keep.Length;
            // The place to put it at the cut
            var rightBuffer = _buffer.AsSpan(index, keepLen);
            TextHelper.Unsafe.CopyBlock(keep, rightBuffer, keepLen);
            // Length is shorter
            _position = curLen - length;
        }

        public void Remove(Range range)
        {
            (int offset, int length) = range.GetOffsetAndLength(_position);
            Remove(offset, length);
        }

        public void RemoveFirst(int length)
        {
            Remove(0, length);
        }

        public void RemoveLast(int length)
        {
            if (length > 0)
            {
                // Happy hack
                _position -= length;
            }
        }

#endregion

#region Interface Implementations

        void ICollection<char>.Add(char ch)
        {
            Allocate() = ch;
        }

        void IList<char>.Insert(int index, char ch)
        {
            AllocateAt(index) = ch;
        }

        void IList<char>.RemoveAt(int index)
        {
            Remove(index);
        }

        bool ICollection<char>.Remove(char ch)
        {
            int i = Written.IndexOf(ch);
            if (i >= 0)
            {
                Remove(i);
                return true;
            }

            return false;
        }

        bool ICollection<char>.Contains(char ch)
        {
            int i = Written.IndexOf(ch);
            return i >= 0;
        }

        int IList<char>.IndexOf(char ch)
        {
            int i = Written.IndexOf(ch);
            if (i >= 0)
            {
                return i;
            }

            return -1;
        }

        void ICollection<char>.CopyTo(char[] array, int arrayIndex)
        {
            Validate.CopyTo(_position, array, arrayIndex);
            TextHelper.CopyTo(Written, array.AsSpan(arrayIndex));
        }

        void ICollection<char>.Clear()
        {
            // Happy Hack!
            _position = 0;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            var chars = _buffer;
            var len = _position;
            for (var i = 0; i < len; i++)
            {
                yield return chars[i];
            }
        }

        IEnumerator<char> IEnumerable<char>.GetEnumerator()
        {
            var chars = _buffer;
            var len = _position;
            for (var i = 0; i < len; i++)
            {
                yield return chars[i];
            }
        }

#if NET6_0_OR_GREATER
        bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, 
            ReadOnlySpan<char> format,
            IFormatProvider? provider)
        {
            int curLen = _position;
            if (curLen <= destination.Length)
            {
                TextHelper.Unsafe.CopyBlock(
                    in _buffer.GetPinnableReference(),
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

        /// <summary>
        /// Returns any rented array to the pool.
        /// </summary>
        public virtual void Dispose()
        {
            char[]? toReturn = _buffer;
            _buffer = null!;
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
        
        public override string ToString() => Written.AsString();
    }
}