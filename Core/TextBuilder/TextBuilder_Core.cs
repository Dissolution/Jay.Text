using System.Buffers;
using System.Collections;
using System.Diagnostics;

// ReSharper disable MergeCastWithTypeCheck
// ReSharper disable InvokeAsExtensionMethod

namespace Jay.Text;

public partial class TextBuilder :
    IList<char>, IReadOnlyList<char>,
    ICollection<char>, IReadOnlyCollection<char>,
    IEnumerable<char>, IEnumerable,
    IDisposable
{
    /// <summary>
    /// The borrowed array where characters are written.
    /// Will always be non-<c>null</c> if <c>_length > 0</c>
    /// </summary>
    private char[] _charArray;
    /// <summary>
    /// The number of characters written to <c>_charArray</c>
    /// </summary>
    private int _length;

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
            // If index < _length, then _charArray is not null
            return ref _charArray[index];
        }
    }
    /// <inheritdoc cref="IList{T}"/>
    char IList<char>.this[int index]
    {
        get => this[index];
        set => this[index] = value;
    }
    /// <inheritdoc cref="IReadOnlyList{T}"/>
    char IReadOnlyList<char>.this[int index] => this[index];

    /// <summary>
    /// Gets a <c>Span&lt;</c><see cref="char"/><c>&gt;</c> for the given <paramref name="range"/>
    /// </summary>
    public Span<char> this[Range range]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _charArray.AsSpan(range);
    }

    /// <summary>
    /// Gets a <c>Span&lt;char&gt;</c> of the written characters
    /// </summary>
    public Span<char> Written
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new Span<char>(_charArray, 0, _length);
    }

    /// <summary>
    /// Gets a <c>Span&lt;char&gt;</c> of the currently available characters to write without resize
    /// </summary>
    internal Span<char> Available
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _charArray.AsSpan(_length);
    }

    /// <summary>
    /// Gets the current character capacity for this instance before resize
    /// </summary>
    public int Capacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _charArray.Length;
    }

    /// <summary>
    /// Gets the count of characters written
    /// </summary>
    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _length;
    }
    /// <inheritdoc cref="ICollection{T}"/>
    int ICollection<char>.Count => _length;
    /// <inheritdoc cref="IReadOnlyCollection{T}"/>
    int IReadOnlyCollection<char>.Count => _length;

    /// <inheritdoc cref="ICollection{T}"/>
    bool ICollection<char>.IsReadOnly => false;

    /// <summary>
    /// Construct a new <see cref="TextBuilder"/>
    /// </summary>
    /// <param name="minCapacity">The minimum capacity for the new instance.</param>
    private TextBuilder(int minCapacity = MinimumCapacity)
    {
        _charArray = ArrayPool<char>.Shared.Rent(Math.Max(minCapacity, MinimumCapacity));
        _length = 0;
    }
    
    #region Grow
    /// <summary>
    /// Core grow method, aggressively inlined
    /// </summary>
    /// <param name="minCapacity">The minimum capacity to grow to.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GrowCore(int minCapacity)
    {
        // We want the max of how much space we actually required and doubling our capacity
        // (without going beyond the max allowed length).
        // We also want to avoid asking for small arrays to reduce the number of times we need to grow.

        // the lower of `string.MaxLength` and `array.MaxLength`
        const int maxCapacity = 0x3FFFFFDF;

        int newCapacity = Math.Clamp(
            minCapacity,
            _charArray.Length * 2,
            maxCapacity);

        // Get our new array
        char[] newArray = ArrayPool<char>.Shared.Rent(newCapacity);
        //copy what we have written to it
        TextHelper.Unsafe.Copy(Written, newArray);
        // Return the array
        char[] toReturn = _charArray;
        // point at the new one
        _charArray = newArray;
        // return our old array
        if (toReturn.Length > 0)
        {
            ArrayPool<char>.Shared.Return(toReturn, true);
        }
    }

    /// <summary>
    /// Consumable grow method
    /// </summary>
    /// <param name="additionalChars"></param>
    /// <remarks>keep consumers as streamlined as possible</remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    protected internal void GrowBy(int additionalChars)
    {
        /* This method is called when the remaining space is insufficient to store a specific number of additional characters.
         * Thus, we need to grow to at least that new total.
         * GrowCore will handle growing by more than that if possible.
         */
        Debug.Assert(additionalChars > (_charArray.Length - _length));
        GrowCore(_length + additionalChars);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    protected internal void EnsureCanAdd(int count)
    {
        if (count > (Capacity - Length))
            GrowCore(Length + count);
    }

    /// <summary>
    /// Slow path to grow and then write a <see cref="char"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowThenWrite(char ch)
    {
        GrowBy(1);
        _charArray[_length++] = ch;
    }

    /// <summary>
    /// Slow path to grow and then write <paramref name="text"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowThenWrite(ReadOnlySpan<char> text)
    {
        GrowBy(text.Length);
        TextHelper.Unsafe.Copy(text, Available);
        _length += text.Length;
    }

    /// <summary>
    /// Slow path to grow and then write a <see cref="string"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowThenWrite(string text)
    {
        GrowBy(text.Length);
        TextHelper.Unsafe.Copy(text, Available);
        _length += text.Length;
    }
    #endregion

    #region Allocate
    /// <summary>
    /// Allocates a <c>Span&lt;char&gt;</c> of the given <paramref name="length"/>, updates the <see cref="Length"/> and returns the span
    /// </summary>
    public Span<char> Allocate(int length)
    {
        if (length > 0)
        {
            int curLen = _length;
            int newLen = curLen + length;
            if (newLen > Capacity)
            {
                GrowBy(length);
            }
            _length = newLen;
            return _charArray.AsSpan(curLen, length);
        }
        return default;
    }

    public Span<char> AllocateAt(int index, int length)
    {
        Validate.Range(_length, index, length);
        if (index == _length) return Allocate(length);
        if (length > 0)
        {
            int curLen = _length;
            int newLen = curLen + length;
            if (newLen > Capacity)
            {
                GrowBy(length);
            }
            // Shift existing to the right
            Span<char> charSpan = _charArray;
            TextHelper.Unsafe.Copy(charSpan[index..], charSpan[(index + length)..]);
            // Update length and return where we allocated
            _length = newLen;
            return charSpan.Slice(index, length);
        }

        return default;
    }
    #endregion

    #region Write
    /// <summary>
    /// Writes a single <see cref="char"/> to this <see cref="TextBuilder"/>
    /// </summary>
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
            GrowThenWrite(ch);
        }
    }
    /// <inheritdoc cref="ICollection{T}"/>
    void ICollection<char>.Add(char ch) => Write(ch);

    /// <summary>
    /// Writes a <c>ReadOnlySpan&lt;char&gt;</c> to this <see cref="TextBuilder"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ReadOnlySpan<char> text)
    {
        if (TextHelper.TryCopyTo(text, Available))
        {
            _length += text.Length;
        }
        else
        {
            GrowThenWrite(text);
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
            GrowThenWrite(text);
        }
    }

    /// <summary>
    /// Writes the <see cref="string"/> representation of a <paramref name="value"/> to this <see cref="TextBuilder"/>
    /// </summary>
    /// <typeparam name="T">The <see cref="Type"/> of value to convert to a <see cref="string"/>.</typeparam>
    /// <param name="value">The value to convert to a <see cref="string"/></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write<T>(T? value)
    {
        string? strValue;
        if (value is IFormattable)
        {
            // If the value can format itself directly into our buffer, do so.
            if (value is ISpanFormattable)
            {
                int charsWritten;
                // constrained call avoiding boxing for value types
                while (!((ISpanFormattable)value).TryFormat(Available, out charsWritten, default, default))
                {
                    GrowBy(1);
                }

                _length += charsWritten;
                return;
            }

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
                GrowThenWrite(strValue);
            }
        }
    }

    #region WriteLine
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteLine()
    {
        ReadOnlySpan<char> newLine = Environment.NewLine;
        if (TextHelper.TryCopyTo(newLine, Available))
        {
            _length += newLine.Length;
        }
        else
        {
            GrowThenWrite(newLine);
        }
    }

    public void WriteLine(char ch) => Append(ch).WriteLine();
    public void WriteLine(ReadOnlySpan<char> text) => Append(text).WriteLine();
    public void WriteLine(string? text) => Append(text).WriteLine();
    public void WriteLine<T>(T? value) => Append<T>(value).WriteLine();
    #endregion

    #region WriteFormatted
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteFormatted<T>(T? value, string? format = null, IFormatProvider? provider = null)
    {
        string? strValue;
        if (value is IFormattable)
        {
            // If the value can format itself directly into our buffer, do so.
            if (value is ISpanFormattable)
            {
                int charsWritten;
                // constrained call avoiding boxing for value types
                while (!((ISpanFormattable)value).TryFormat(Available, out charsWritten, format, provider))
                {
                    GrowBy(1);
                }

                _length += charsWritten;
                return;
            }

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
                GrowThenWrite(strValue);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Span<char> GetWriteFormatted<T>(T? value, string? format = null, IFormatProvider? provider = null)
    {
        string? strValue;
        if (value is IFormattable)
        {
            // If the value can format itself directly into our buffer, do so.
            if (value is ISpanFormattable)
            {
                int start = _length;
                int charsWritten;
                // constrained call avoiding boxing for value types
                while (!((ISpanFormattable)value).TryFormat(Available, out charsWritten, format, provider))
                {
                    GrowBy(1);
                }

                _length = start + charsWritten;
                return new Span<char>(_charArray, start, charsWritten);
            }

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
            TextHelper.Unsafe.Copy(strValue, span);
            return span;
        }
        else
        {
            return default;
        }
    }
    #endregion

    #region WriteAligned
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
                else // None | Center
                {
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
        WriteAligned((ReadOnlySpan<char>)text, alignment, width, fillChar);
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
    #endregion

    #region Write + InterpolatedTextBuilder
    /// <summary>
    /// Writes the given <paramref name="interpolatedString"/> to this <see cref="TextBuilder"/>
    /// </summary>
    /// <remarks>
    /// We pass ourselves into the <see cref="InterpolatedTextBuilder"/>'s constructor so it writes to us during construction
    /// </remarks>
    public void Write([InterpolatedStringHandlerArgument("")] ref InterpolatedTextBuilder interpolatedString)
    {
        // The writing is done
    }
    #endregion
    #endregion

/*#region Indenting

    public TextBuilder Indent(ReadOnlySpan<char> indent,
        Action<TextBuilder> indentedText)
    {
        var oldIndent = _newLine;
        _newLine = $"{_newLine}{indent}";
        indentedText(this);
        _newLine = oldIndent;
        return this;
    }
    
#endregion*/

    #region Insert
    public TextBuilder Insert(int index, char ch)
    {
        Validate.Insert(_length, index);
        if (index == _length)
            return Append(ch);
        AllocateAt(index, 1)[0] = ch;
        return this;
    }
    /// <inheritdoc cref="IList{T}"/>
    void IList<char>.Insert(int index, char ch) => this.Insert(index, ch);

    public TextBuilder Insert(int index, ReadOnlySpan<char> text)
    {
        Validate.Insert(_length, index);
        if (index == _length)
            return Append(text);
        TextHelper.Unsafe.Copy(text, AllocateAt(index, text.Length));
        return this;
    }

    public TextBuilder Insert(int index, string? text)
    {
        Validate.Insert(_length, index);
        if (string.IsNullOrEmpty(text)) return this;
        if (index == _length)
            return Append(text);
        TextHelper.Unsafe.Copy(text, AllocateAt(index, text.Length));
        return this;
    }

    public TextBuilder Insert<T>(int index, T? value)
    {
        return Insert(index, value?.ToString());
    }

    public TextBuilder Insert(int index, Action<TextBuilder> buildInsertText)
    {
        return Insert(index, Build(buildInsertText));
    }
#endregion

    #region Replace
    /// <summary>
    /// Replace all occurrences of <paramref name="oldChar"/> with <paramref name="newChar"/> in this <see cref="TextBuilder"/>
    /// </summary>
    public TextBuilder Replace(char oldChar, char newChar)
    {
        var writ = Written;
        ref char ch = ref Unsafe.NullRef<char>();
        for (var i = writ.Length - 1; i >= 0; i--)
        {
            ch = ref writ[i];
            if (ch == oldChar)
            {
                ch = newChar;
            }
        }
        return this;
    }

    public TextBuilder Replace(ReadOnlySpan<char> oldText, ReadOnlySpan<char> newText)
    {
        int oldTextLen = oldText.Length;
        if (oldTextLen == 0 || oldTextLen > Length) return this;
        int newTextLen = newText.Length;

        // Stores the area of written text we're scanning for replacements
        Span<char> scan = Written;
        int i;

        // What we do depends on the differences between the text sizes
        int gap = oldTextLen - newTextLen;

        // Same text size
        if (gap == 0)
        {
            ref readonly char newChar = ref newText.GetPinnableReference();
            // Scan until we find no further matches
            while ((i = MemoryExtensions.IndexOf(scan, oldText)) >= 0)
            {
                // Copy new onto old
                TextHelper.Unsafe.Copy(in newChar, ref scan[i], newTextLen);
                // Start our scan after this replacement
                scan = scan.Slice(i + oldTextLen);
            }
        }
        // NewText is smaller (shrinks length)
        else if (gap > 0)
        {
            ref readonly char newChar = ref newText.GetPinnableReference();
            // Scan until we find no further matches
            while ((i = MemoryExtensions.IndexOf(scan, oldText)) >= 0)
            {
                // Copy new onto old
                TextHelper.Unsafe.Copy(in newChar, ref scan[i], newTextLen);

                // Slide everything to the right over the gap
                TextHelper.Unsafe.Copy(scan.Slice(i + oldTextLen), scan.Slice(i + newTextLen));
                // Length is smaller
                _length -= gap;

                // Start our scan after this replacement
                scan = scan.Slice(i + newTextLen);
            }
        }
        // NewText is bigger (increases length)
        else // gap < 0
        {
            using (var tempBuilder = new TextBuilder(Length * 2))
            {
                // Scan until we find no further matches
                while ((i = MemoryExtensions.IndexOf(scan, oldText)) >= 0)
                {
                    // Do we have to write anything before this?
                    if (i > 0)
                    {
                        // Write before
                        tempBuilder.Write(scan[..i]);
                    }
                    // Write replacement
                    tempBuilder.Write(newText);
                    //_length -= gap; // gap is negative, length grows

                    // Update scan to right after oldText
                    scan = scan.Slice(i + oldTextLen);
                }

                // Did we have anything left to write?
                if (scan.Length > 0)
                {
                    tempBuilder.Write(scan);
                }

                // Swap our arrays + lengths! HACKHACKHACK
                (_charArray, tempBuilder._charArray) = (tempBuilder._charArray, _charArray);
                (_length, tempBuilder._length) = (tempBuilder._length, _length);
            } // Dispose the temp builder
            // Now we have the correct text
        }

        // Fluent
        return this;
    }

    public TextBuilder Replace(ReadOnlySpan<char> oldText, ReadOnlySpan<char> newText, StringComparison comparison)
    {
        int oldTextLen = oldText.Length;
        if (oldTextLen == 0 || oldTextLen > Length) return this;
        int newTextLen = newText.Length;

        // Stores the area of written text we're scanning for replacements
        Span<char> scan = Written;
        int i;

        // What we do depends on the differences between the text sizes
        int gap = oldTextLen - newTextLen;

        // Same text size
        if (gap == 0)
        {
            ref readonly char newChar = ref newText.GetPinnableReference();
            // Scan until we find no further matches
            while ((i = MemoryExtensions.IndexOf(scan, oldText, comparison)) >= 0)
            {
                // Copy new onto old
                TextHelper.Unsafe.Copy(in newChar, ref scan[i], newTextLen);
                // Start our scan after this replacement
                scan = scan.Slice(i + oldTextLen);
            }
        }
        // NewText is smaller (shrinks length)
        else if (gap > 0)
        {
            ref readonly char newChar = ref newText.GetPinnableReference();
            // Scan until we find no further matches
            while ((i = MemoryExtensions.IndexOf(scan, oldText, comparison)) >= 0)
            {
                // Copy new onto old
                TextHelper.Unsafe.Copy(in newChar, ref scan[i], newTextLen);

                // Slide everything to the right over the gap
                TextHelper.Unsafe.Copy(scan.Slice(i + oldTextLen), scan.Slice(i + newTextLen));
                // Length is smaller
                _length -= gap;

                // Start our scan after this replacement
                scan = scan.Slice(i + newTextLen);
            }
        }
        // NewText is bigger (increases length)
        else // gap < 0
        {
            using (var tempBuilder = new TextBuilder(Length * 2))
            {
                // Scan until we find no further matches
                while ((i = MemoryExtensions.IndexOf(scan, oldText, comparison)) >= 0)
                {
                    // Do we have to write anything before this?
                    if (i > 0)
                    {
                        // Write before
                        tempBuilder.Write(scan[..i]);
                    }
                    // Write replacement
                    tempBuilder.Write(newText);
                    //_length -= gap; // gap is negative, length grows

                    // Update scan to right after oldText
                    scan = scan.Slice(i + oldTextLen);
                }

                // Did we have anything left to write?
                if (scan.Length > 0)
                {
                    tempBuilder.Write(scan);
                }

                // Swap our arrays + lengths! HACKHACKHACK
                (_charArray, tempBuilder._charArray) = (tempBuilder._charArray, _charArray);
                (_length, tempBuilder._length) = (tempBuilder._length, _length);
            } // Dispose the temp builder
            // Now we have the correct text
        }

        // Fluent
        return this;
    }
    #endregion

    #region Remove
    /// <summary>
    /// Removes the characters from <paramref name="index"/> to <paramref name="length"/> and moves any written characters over to fill in the gap
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RemoveRangeCore(int index, int length)
    {
        if ((uint)index + length >= _length)
        {
            _length = index;
        }
        else
        {
            var written = _charArray.AsSpan(index + length);
            TextHelper.Unsafe.Copy(written, _charArray.AsSpan(index));
            _length -= length;
        }
    }

    public void RemoveAt(int index)
    {
        Validate.Index(_length, index);
        RemoveRangeCore(index, 1);
    }

    public void RemoveRange(int index, int length)
    {
        Validate.Range(_length, index, length);
        RemoveRangeCore(index, length);
    }

    public void RemoveRange(Range range)
    {
        Validate.Range(_length, range);
        var (offset, length) = range.GetOffsetAndLength(_length);
        RemoveRangeCore(offset, length);
    }

    public int RemoveFirst(char ch)
    {
        ReadOnlySpan<char> chars = Written;
        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] == ch)
            {
                RemoveRangeCore(i, 1);
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
                RemoveRangeCore(i, 1);
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
            RemoveRangeCore(i, text.Length);
        }
        return i;
    }
    public int RemoveFirst(ReadOnlySpan<char> text, StringComparison comparison)
    {
        var i = FirstIndexOf(text, comparison);
        if (i >= 0)
        {
            RemoveRangeCore(i, text.Length);
        }
        return i;
    }

    public int RemoveLast(ReadOnlySpan<char> text)
    {
        var i = LastIndexOf(text);
        if (i >= 0)
        {
            RemoveRangeCore(i, text.Length);
        }
        return i;
    }
    public int RemoveLast(ReadOnlySpan<char> text, StringComparison comparison)
    {
        var i = LastIndexOf(text, comparison);
        if (i >= 0)
        {
            RemoveRangeCore(i, text.Length);
        }
        return i;
    }
    #endregion

    #region Clear
    public TextBuilder Clear()
    {
        // We do not clear the contents of the array here, only on dispose
        _length = 0;
        return this;
    }
    /// <inheritdoc cref="ICollection{T}"/>
    void ICollection<char>.Clear() => this.Clear();

    // ToStringAndClear lives in ToString (below)
    #endregion

#region Search
    #region IndexOf
    /// <summary>
    /// Returns the index of the first occurrence of the given <see cref="char"/> or -1 if none is found
    /// </summary>
    public int FirstIndexOf(char ch)
    {
        ReadOnlySpan<char> chars = Written;
        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] == ch) return i;
        }
        return -1;
    }
    /// <inheritdoc cref="IList{T}"/>
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
        return MemoryExtensions.LastIndexOf(Written, text, comparison);
    }
    #endregion
    #region Contains
    public bool Contains(char ch) => FirstIndexOf(ch) >= 0;
    public bool Contains(ReadOnlySpan<char> text) => FirstIndexOf(text) >= 0;
    public bool Contains(ReadOnlySpan<char> text, StringComparison comparison) => FirstIndexOf(text, comparison) >= 0;
#endregion
#endregion

    #region Measure / GetSpan
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

    public Span<char> GetSpan(Action<TextBuilder> buildText)
    {
        int start = _length;
        buildText(this);
        return _charArray[new Range(start: start, end: _length)];
    }
    #endregion

#region Copy
    public void CopyTo(char[] array, int arrayIndex = 0)
    {
        if ((uint)arrayIndex >= array.Length)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex), arrayIndex, "Array Index does not fit in the specified array");
        TextHelper.Unsafe.Copy(Written, array.AsSpan(arrayIndex));
    }

    public void CopyTo(Span<char> destination)
    {
        if (Length > destination.Length)
            throw new ArgumentException("Destination cannot fit source", nameof(destination));
        TextHelper.Unsafe.Copy(Written, destination);
    }

    public bool TryCopyTo(Span<char> destination)
    {
        return TextHelper.TryCopyTo(Written, destination);
    }
#endregion

    #region Slice
    #endregion

    #region Enumeration
    /// <summary>
    /// An efficient <c>ref struct</c> <see cref="IEnumerator"/>&lt;<see cref="char"/>&gt; over a <c>Span&lt;</c><see cref="char"/><c>&gt;</c>
    /// </summary>
    public ref struct CharSpanEnumerator
    {
        /// <summary>
        /// The span being enumerated over
        /// </summary>
        private readonly Span<char> _span;
        /// <summary>
        /// The current index that _current points at
        /// </summary>
        private int _index;

        /// <summary>Gets the element at the current position of the enumerator.</summary>
        public ref char Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if ((uint)_index >= _span.Length)
                    return ref Unsafe.NullRef<char>();
                return ref _span[_index];
            }
        }

        public int Index
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _index;
        }

        /// <summary>
        /// Initialize the enumerator
        /// </summary>
        /// <param name="span">The span to enumerate.</param>
        internal CharSpanEnumerator(Span<char> span)
        {
            _span = span;
            _index = -1;
        }

        /// <summary>Advances the enumerator to the next element of the span.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            int index = _index + 1;
            _index = index;
            return index < _span.Length;
        }
    }

    public CharSpanEnumerator GetEnumerator()
    {
        return new CharSpanEnumerator(Written);
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
#endregion

    #region Dispose
    public void Dispose()
    {
        char[] toReturn = _charArray;
        _length = 0;
        _charArray = Array.Empty<char>();
        if (toReturn.Length > 0)
        {
            ArrayPool<char>.Shared.Return(toReturn, true);
        }
    }
    #endregion

    #region Equals / GetHashCode
    public bool Equals(string? text)
    {
        return TextHelper.Equals(Written, text);
    }
    public bool Equals(ReadOnlySpan<char> text)
    {
        return TextHelper.Equals(Written, text);
    }

    public bool Equals(params char[] characters)
    {
        return TextHelper.Equals(Written, characters);
    }

    public override bool Equals(object? obj)
    {
        if (obj is string text) return Equals(text);
        if (obj is char[] chars) return Equals(chars);
        return false;
    }

    public override int GetHashCode()
    {
        return string.GetHashCode(Written);
    }
    #endregion

    #region ToString
    public string ToStringAndClear()
    {
        var str = new string(_charArray, 0, _length);
        _length = 0;
        return str;
    }

    public string ToString(int start, int length)
    {
        Validate.Range(_length, start, length);
        if (length <= 0) return "";
        return new string(Written.Slice(start, length));
    }

    public string ToString(Range range)
    {
        Validate.Range(_length, range);
        return new string(Written[range]);
    }

    public override string ToString()
    {
        return new string(_charArray, 0, _length);
    }
    #endregion
}