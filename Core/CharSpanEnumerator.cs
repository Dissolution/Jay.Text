namespace Jay.Text;

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