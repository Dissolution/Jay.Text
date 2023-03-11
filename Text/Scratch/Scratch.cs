#if NET6_0_OR_GREATER
using System.ComponentModel;
using InlineIL;
#endif
using System.Diagnostics.CodeAnalysis;
using Jay.Text.Scratch.WriteExtensions;
using Jay.Text.Utilities;

namespace Jay.Text.Scratch;


#if NET6_0_OR_GREATER
[InterpolatedStringHandler]
#endif
/// <summary>
/// A stack-based fluent text builder
/// </summary>
public ref struct StackTextBuilder
{
    /// <summary>
    /// Implicit conversion from <see cref="System.Span{T}">Span&lt;char&gt;</see> to <see cref="StackTextBuilder"/>
    /// allows for use with <c>stackalloc</c> such as: <br/>
    /// <see cref="StackTextBuilder"/> textBuilder = stackalloc char[###]; 
    /// </summary>
    /// <param name="initialBuffer">The initial starting buffer the <see cref="StackTextBuilder"/> will use</param>
    public static implicit operator StackTextBuilder(Span<char> initialBuffer) => new(initialBuffer);
    
    /// <summary>
    /// Rented <see cref="char"/><c>[]</c> from pool used to back <see cref="_chars"/><br/>
    /// Note: will be <c>null</c> if we are working with an initial buffer and have not yet rented from the pool
    /// </summary>
    private char[]? _charArray;

    /// <summary>
    /// The span of <see cref="char"/>s we're writing to
    /// </summary>
    private Span<char> _chars;

    /// <summary>
    /// The current position in <see cref="_chars"/> we're writing to
    /// </summary>
    private int _position;
    
    /// <summary>
    /// Gets the <see cref="System.Span{T}">Span&lt;char&gt;</see> that has been written
    /// </summary>
    public Span<char> Written
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _chars.Slice(0, _position);
    }
    
    /// <summary>
    /// Gets the <see cref="System.Span{T}">Span&lt;char&gt;</see> available for writing
    /// <br/>
    /// <b>Caution</b>: If you write to <see cref="Available"/>, you must also update <see cref="Length"/>!
    /// </summary>
    public Span<char> Available
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _chars.Slice(_position);
    }
    
    /// <summary>
    /// The current total capacity to store <see cref="char"/>s
    /// <br/>
    /// Can be increased with <see cref="M:GrowBy"/> and <see cref="M:GrowTo"/>
    /// </summary>
    internal int Capacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _chars.Length;
    }

    /// <summary>
    /// Gets or sets the number of <see cref="Written"/> <see cref="char"/>s
    /// </summary>
    /// <remarks>
    /// <c>set</c> values will be clamped between 0 and <see cref="Capacity"/>
    /// </remarks>
    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _position;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            // Shrink
            if (value < _position)
            {
                /* We do not clear the removed values
                 * - we clear the array when we return it
                 * - we're not pretending to be secure
                 * 
                 * If we did:
                int oldPos = _position;
                int newPos = Math.Max(value, 0);
                Span<char> removed = _chars[newPos..oldPos];
                removed.Fill(default);
                _position = newPos;
                 */
                
                // Min is 0
                _position = Math.Max(value, 0);
            }
            // No Change
            else if (value == _position)
            {

            }
            // Grow
            else
            {
                // Always okay, we have to assume they've written to Available
                // Max is Capacity
                _position = Math.Min(value, Capacity);
            }
        }
    }

    public ref char this[int index]
    {
        get
        {
            Validate.Index(_position, index);
            return ref _chars[index];
        }
    }

    public Span<char> this[Range range]
    {
        get
        {
            Validate.Range(_position, range);
            return _chars[range];
        }
    }
    
    #region Constructors
#if NET6_0_OR_GREATER
    /// <summary>
    /// Support for <see cref="InterpolatedStringHandlerAttribute"/>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public StackTextBuilder(int literalLength, int formattedCount)
        : this(BuilderHelper.GetInterpolatedStartCapacity(literalLength, formattedCount))
    {
        Debug.Assert(true);
    }

    /// <summary>
    /// Support for <see cref="InterpolatedStringHandlerAttribute"/>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public StackTextBuilder(int literalLength, int formattedCount, scoped ref StackTextBuilder outerBuilder)
    {
        this = outerBuilder;

      
        
        //
        // _charArray = outerBuilder._charArray;
        // _chars = outerBuilder._chars;
        // _position = outerBuilder._position;
    }
#endif
    
    /// <summary>
    /// Construct a new <see cref="StackTextBuilder"/> with default starting <see cref="Capacity"/>
    /// </summary>
    public StackTextBuilder()
    {
        _chars = _charArray = ArrayPool<char>.Shared.Rent(BuilderHelper.MinimumCapacity);
        _position = 0;
    }
    
    /// <summary>
    /// Construct a new <see cref="StackTextBuilder"/> with specified minimum starting <paramref name="minCapacity"/>
    /// </summary>
    /// <param name="minCapacity">
    /// The minimum possible starting <see cref="Capacity"/> <br/>
    /// Actual starting <see cref="Capacity"/> may be larger
    /// </param>
    public StackTextBuilder(int minCapacity)
    {
        _chars = _charArray = ArrayPool<char>.Shared.Rent(Math.Max(minCapacity, BuilderHelper.MinimumCapacity));
        _position = 0;
    }
    
    /// <summary>
    /// Construct a new <see cref="StackTextBuilder"/> that starts with an initial
    /// <see cref="System.Span{T}">Span&lt;char&gt;</see> <paramref name="buffer"/>
    /// </summary>
    /// <param name="buffer">
    /// The initial <see cref="System.Span{T}">Span&lt;char&gt;</see> that this
    /// <see cref="StackTextBuilder"/> will write to. <br/>
    /// If capacity is needed beyond this buffer, an array will be rented from a pool and we will stop using it. <br/>
    /// If no further capacity is needed, <see cref="Dispose"/> will not be required.
    /// </param>
    public StackTextBuilder(Span<char> buffer)
    {
        _chars = buffer;
        _charArray = null;
        _position = 0;
    }
    #endregion

#region Grow
    /// <summary>
    /// Grow the size of <see cref="_charArray"/> (and thus <see cref="_chars"/>)
    /// to at least the specified <paramref name="minCapacity"/>.
    /// </summary>
    /// <param name="minCapacity">The minimum possible <see cref="Capacity"/> to grow to</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GrowCore(int minCapacity)
    {
        Debug.Assert(minCapacity > BuilderHelper.MinimumCapacity);
        Debug.Assert(minCapacity > Capacity);

        // Get a new array at least minCapacity big
        char[] newArray = ArrayPool<char>.Shared.Rent(minCapacity);
        // Copy our written to it
        TextHelper.Unsafe.CopyTo(_chars, newArray, _position);

        // Store an array to return (we may not have one)
        char[]? toReturn = _charArray;
        // Set our newarray to our current array + span
        _chars = _charArray = newArray;
        
        // Return an array if we had borrowed one
        if (toReturn is not null)
        {
            ArrayPool<char>.Shared.Return(toReturn, true);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowTo(int minCapacity)
    {
        int curCapacity = Capacity;
        Debug.Assert(minCapacity > curCapacity);
        int newCapacity = (minCapacity + curCapacity);
        GrowCore(newCapacity);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void GrowBy(int adding)
    {
        if (adding > 0)
        {
            int curCapacity = Capacity;
            int newCapacity = (adding + curCapacity) * 2;
            GrowCore(newCapacity);
        }
    }
#endregion
    
    public Span<char> Allocate(int count)
    {
        if (count > 0)
        {
            // Start + End of alloaction
            var start = _position;
            // The end of the allocation
            var end = start + count;
            // Check for growth
            if (end > Capacity)
            {
                GrowTo(end);
            }
            // Move position
            _position = end;
            // return allocated Span
            return _chars[start..end];
        }
        return Span<char>.Empty;
    }

#if NET6_0_OR_GREATER
#region InterpolatedStringHandler
    /// <summary>
    /// Support for <see cref="InterpolatedStringHandlerAttribute"/>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendLiteral(string str) => this.Write(str);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted(char ch) => this.Write(ch);
    
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted(scoped ReadOnlySpan<char> text) => this.Write(text);
    
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted(string? str) => this.Write(str);
    
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted<T>(T value) => this.Write<T>(value);
    
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted<T>(T value, string? format) => this.Write<T>(value, format);
#endregion
#endif

    public bool TryCopyTo(Span<char> destination)
    {
        return Written.TryCopyTo(destination);
    }

    public void Clear()
    {
        Length = 0;
    }
    
    public void Dispose()
    {
        // Get a possible array to return
        var toReturn = _charArray;
        // clear
        this = default;
        if (toReturn is not null)
        {
            ArrayPool<char>.Shared.Return(toReturn, true);
        }
    }

    public bool Equals(ReadOnlySpan<char> text)
    {
        return TextHelper.Equals(Written, text);
    }
    
    public bool Equals(ReadOnlySpan<char> text, StringComparison comparison)
    {
        return TextHelper.Equals(Written, text, comparison);
    }

    public bool Equals(params char[] characters) => Equals(characters.AsSpan());

    public bool Equals(string? str)
    {
        return TextHelper.Equals(Written, str);
    }
    
    public bool Equals(string? str, StringComparison comparison)
    {
        return TextHelper.Equals(Written, str, comparison);
    }
    
    public override bool Equals(object? obj)
    {
        throw new NotImplementedException();
    }
    
    [DoesNotReturn]
    public override int GetHashCode() => throw new NotSupportedException();

    public string ToStringAndDispose()
    {
        // Get our string
        string str = _chars[.._position].ToString();
        // Get a possible array to return
        var toReturn = _charArray;
        // clear
        this = default;
        if (toReturn is not null)
        {
            ArrayPool<char>.Shared.Return(toReturn, true);
        }
        // return the string
        return str;
    }
    
    public string ToStringAndClear()
    {
        // Get our string
        string str = _chars[.._position].ToString();
        Length = 0;
        return str;
    }
    
    public override string ToString()
    {
        return _chars[.._position].ToString();
    }
}