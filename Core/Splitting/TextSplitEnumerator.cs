namespace Jay.Text.Splitting;

public ref struct TextSplitEnumerator 
    //: IEnumerator<ReadOnlySpan<char>>, IEnumerator
{
    private int _position = 0;
    private ReadOnlySpan<char> _currentTextSlice = default;
    private Range _currentRange = default;

    public readonly ReadOnlySpan<char> InputText;
    public readonly ReadOnlySpan<char> Separator;
    public readonly TextSplitOptions SplitOptions;
    public readonly StringComparison StringComparison;

    /// <inheritdoc cref="IEnumerator{T}"/>
    public ReadOnlySpan<char> Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _currentTextSlice;
    }

    public ReadOnlySpan<char> Text => _currentTextSlice;
    public string String => _currentTextSlice.ToString();
    public Range Range => _currentRange;

    public TextSplitEnumerator(TextSplitEnumerable splitEnumerable)
    {
        InputText = splitEnumerable.InputText;
        Separator = splitEnumerable.Separator;
        SplitOptions = splitEnumerable.SplitOptions;
        StringComparison = splitEnumerable.StringComparison;
    }

    /// <inheritdoc cref="IEnumerator"/>
    public bool MoveNext()
    {
        int inputTextLen = InputText.Length;
        // inclusive start index
        int sliceStart;
        // exclusive end index
        int sliceEnd;

        while (true)
        {
            sliceStart = _position;

            // After the end = done enumerating
            if (sliceStart > inputTextLen)
            {
                _currentTextSlice = default; // clear after enumeration ends
                _currentRange = default;
                return false;
            }
            
            // If our position is at the end, we might need to yield the last bit
            if (sliceStart == inputTextLen)
            {
                // Finish enumeration                 
                _position = sliceStart + 1;
                if (!SplitOptions.HasFlag(TextSplitOptions.RemoveEmptyLines))
                {
                    // Empty
                    _currentTextSlice = ReadOnlySpan<char>.Empty;
                    _currentRange = new Range(start: sliceStart, end: sliceStart);
                    return true;
                }
                
                // clear
                _currentTextSlice = default;
                _currentRange = default;
                return false;
            }
            
            // Scan for next separator
            var separatorIndex = InputText.NextIndexOf(
                Separator,
                _position,
                StringComparison
            );
            // None found or an empty separator yield the original
            if (separatorIndex == -1 || Separator.Length == 0)
            {
                // End of slice is end of text
                sliceEnd = InputText.Length;
                // We're done enumerating
                _position = sliceEnd + 1;
            }
            else
            {
                // This slice ends where the separator starts
                sliceEnd = separatorIndex;
                // We'll start again where the separator ends
                _position = sliceEnd + Separator.Length;
            }


            // Respect StringSplitOptions
            if (SplitOptions.HasFlag(TextSplitOptions.TrimLines))
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


            _currentRange = new Range(
                /* inclusive */start: sliceStart,
                /* exclusive */end: sliceEnd
            );

            // Respect StringSplitOptions
            _currentTextSlice = InputText[_currentRange];
            if (_currentTextSlice.Length > 0 || !SplitOptions.HasFlag(TextSplitOptions.RemoveEmptyLines))
            {
                // This is a valid return slice
                return true;
            }
            // We're not going to return this slice, told not to

            // _position has been updated, start the next scan
        }
    }

    /// <inheritdoc cref="IEnumerator"/>
    public void Reset()
    {
        _position = 0;
        _currentTextSlice = default;
    }
}