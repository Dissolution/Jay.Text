namespace Jay.Text.Utilities;

public readonly ref struct RangeSplitEnumerable // : IEnumerable<Range>, IEnumerable
{
    public readonly ReadOnlySpan<char> InputText;
    public readonly ReadOnlySpan<char> Separator;
    public readonly StringSplitOptions SplitOptions;
    public readonly StringComparison StringComparison;

    public RangeSplitEnumerable(
        ReadOnlySpan<char> inputText,
        ReadOnlySpan<char> separator,
        StringSplitOptions splitOptions = StringSplitOptions.None,
        StringComparison stringComparison = StringComparison.Ordinal)
    {
        InputText = inputText;
        Separator = separator;
        SplitOptions = splitOptions;
        StringComparison = stringComparison;
    }

    /// <inheritdoc cref="IEnumerable{T}"/>
    public RangeSplitEnumerator GetEnumerator()
    {
        return new RangeSplitEnumerator(this);
    }

    public ref struct RangeSplitEnumerator //: IEnumerator<Range>, IEnumerator
    {
        private int _position = 0;
        private Range _currentRangeSlice = default;

        public readonly ReadOnlySpan<char> InputText;
        public readonly ReadOnlySpan<char> Separator;
        public readonly StringSplitOptions SplitOptions;
        public readonly StringComparison StringComparison;

        /// <inheritdoc cref="IEnumerator{T}"/>
        public Range Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _currentRangeSlice;
        }

        public bool AtEnd
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _position >= InputText.Length;
        }

        public RangeSplitEnumerator(RangeSplitEnumerable splitEnumerable)
        {
            InputText = splitEnumerable.InputText;
            Separator = splitEnumerable.Separator;
            SplitOptions = splitEnumerable.SplitOptions;
            StringComparison = splitEnumerable.StringComparison;
        }

        /// <inheritdoc cref="IEnumerator"/>
        public bool MoveNext()
        {
            // Are we at the end?
            if (AtEnd)
            {
                _currentRangeSlice = default; // clear after enumeration ends
                return false;
            }

            // inclusive start index
            int sliceStart = _position;

            do
            {
                // Scan for next separator
                var separatorIndex = InputText.NextIndexOf(Separator, _position, StringComparison);

                // exclusive end index
                int sliceEnd;
                if (separatorIndex == -1)
                {
                    // End of slice is end of text
                    sliceEnd = InputText.Length;
                    _position = sliceEnd;
                }
                else
                {
                    sliceEnd = separatorIndex;
                    _position = sliceEnd + Separator.Length;
                }

                // Respect StringSplitOptions
#if NET6_0_OR_GREATER
                if (SplitOptions.HasFlag(StringSplitOptions.TrimEntries))
                {
                    // Copied from ReadOnlySpan<char>.Trim()
                    for (; sliceStart < sliceEnd; sliceStart++)
                    {
                        if (!char.IsWhiteSpace(InputText[sliceStart]))
                        {
                            break;
                        }
                    }

                    for (; sliceEnd > sliceStart; sliceEnd--)
                    {
                        if (!char.IsWhiteSpace(InputText[(sliceEnd - 1)]))
                        {
                            break;
                        }
                    }
                }
#endif

                Range sliceRange = new Range(
                    /* inclusive */ start: sliceStart,
                    /* exclusive */ end: sliceEnd);

                // Respect StringSplitOptions
                if (sliceRange.Length() > 0 || !SplitOptions.HasFlag(StringSplitOptions.RemoveEmptyEntries))
                {
                    // This is a valid return slice
                    _currentRangeSlice = sliceRange;

                    return true;
                }

                // Continue scanning for a valid return range
            } // Until we have returned every possible slice
            while (!AtEnd);

            // At end
            _currentRangeSlice = default;
            return false;
        }

        /// <inheritdoc cref="IEnumerator"/>
        public void Reset()
        {
            _position = 0;
            _currentRangeSlice = default;
        }
    }
}