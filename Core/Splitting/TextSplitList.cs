namespace Jay.Text.Splitting;

public readonly ref struct TextSplitList
{
    private readonly ReadOnlySpan<char> _inputText;
    private readonly IReadOnlyList<Range> _ranges;
       
    public int Count => _ranges.Count;

    internal TextSplitList(ReadOnlySpan<char> inputText, IReadOnlyList<Range> ranges)
    {
        _inputText = inputText;
        _ranges = ranges;
    }

    public Range Range(int index)
    {
        if ((uint)index < _ranges.Count)
        {
            return _ranges[index];
        }
        throw new ArgumentOutOfRangeException(nameof(index), index, $"Index must be between 0 and {Count - 1}");
    }

    public ReadOnlySpan<char> Text(int index)
    {
        if ((uint)index < _ranges.Count)
        {
            return _inputText[_ranges[index]];
        }
        throw new ArgumentOutOfRangeException(nameof(index), index, $"Index must be between 0 and {Count - 1}");
    }

    /// <inheritdoc cref="IEnumerable{T}"/>
    public TextSplitListEnumerator GetEnumerator() => new(this);

    public ref struct TextSplitListEnumerator //: IEnumerator<ReadOnlySpan<char>>, IEnumerator
    {
        private readonly ReadOnlySpan<char> _inputText;
        private readonly IReadOnlyList<Range> _ranges;
        private int _position;

        private ReadOnlySpan<char> _currentSpan;
        private Range _currentRange;

        /// <inheritdoc cref="IEnumerator{T}"/>
        public ReadOnlySpan<char> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _currentSpan;
        }

        public int Index => _position;
        public ReadOnlySpan<char> Span => _currentSpan;
        public string String => _currentSpan.ToString();
        public Range Range => _currentRange;

        public TextSplitListEnumerator(TextSplitList textSplitList)
        {
            _inputText = textSplitList._inputText;
            _ranges = textSplitList._ranges;
            _position = -1;
            _currentRange = default;
            _currentSpan = default;
        }

        /// <inheritdoc cref="IEnumerator"/>
        public bool MoveNext()
        {
            var index = _position + 1;
            _position = index;
            if (index < _ranges.Count)
            {
                _currentRange = _ranges[index];
                _currentSpan = _inputText[_currentRange];
                return true;
            }
            else
            {
                _currentRange = default;
                _currentSpan = default;
                return false;
            }
        }

        /// <inheritdoc cref="IEnumerator"/>
        public void Reset()
        {
            _position = -1;
            _currentRange = default;
            _currentSpan = default;
        }
    }
}