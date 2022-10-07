namespace Jay.Text;

public ref struct TextPosition
{
    private Span<char> _text;
    private int _index;

    public int Index
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _index;
        set => _index = Math.Clamp(value, 0, _text.Length);
    }

    public ref char Current
    {
        get
        {
            if (_index >= Capacity)
                throw new IndexOutOfRangeException();
            return ref _text[Index];
        }
    }

    public Span<char> Previous
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _text.Slice(0, Index);
    }

    public Span<char> Text
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (_index < Capacity)
                return _text.Slice(Index);
            return default;
        }
    }

    public Span<char> Next
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (_index < Capacity - 1)
                return _text.Slice(_index + 1);
            return default;
        }
    }

    public int Capacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _text.Length;
    }

    public TextPosition(Span<char> text, int index = 0)
    {
        _text = text;
        _index = index;
    }

    public void SkipWhiteSpace()
    {
        var text = _text;
        var i = _index;
        var capacity = Capacity;
        while (i < capacity && char.IsWhiteSpace(text[i]))
        {
            i++;
        }
        _index = i;
    }

    public Span<char> TakeWhiteSpace()
    {
        var text = _text;
        var i = _index;
        var start = i;
        var capacity = Capacity;
        while (i < capacity && char.IsWhiteSpace(text[i]))
        {
            i++;
        }
        _index = i;
        return _text.Slice(start, i - start);
    }

    public void SkipDigits()
    {
        var text = _text;
        var i = _index;
        var capacity = Capacity;
        while (i < capacity && char.IsDigit(text[i]))
        {
            i++;
        }
        _index = i;
    }

    public Span<char> TakeDigits()
    {
        var text = _text;
        var i = _index;
        var start = i;
        var capacity = Capacity;
        while (i < capacity && char.IsDigit(text[i]))
        {
            i++;
        }
        _index = i;
        return _text.Slice(start, i - start);
    }

    public void SkipWhile(Func<char, bool> predicate)
    {
        var text = _text;
        var i = _index;
        var capacity = Capacity;
        while (i < capacity && predicate(text[i]))
        {
            i++;
        }
        _index = i;
    }

    public Span<char> TakeWhile(Func<char, bool> predicate)
    {
        var text = _text;
        var i = _index;
        var start = i;
        var capacity = Capacity;
        while (i < capacity && predicate(text[i]))
        {
            i++;
        }
        _index = i;
        return _text.Slice(start, i - start);
    }

    public void SkipUntil(Func<char, bool> predicate)
    {
        var text = _text;
        var i = _index;
        var capacity = Capacity;
        while (i < capacity && !predicate(text[i]))
        {
            i++;
        }
        _index = i;
    }

    public Span<char> TakeUntil(Func<char, bool> predicate)
    {
        var text = _text;
        var i = _index;
        var start = i;
        var capacity = Capacity;
        while (i < capacity && !predicate(text[i]))
        {
            i++;
        }
        _index = i;
        return _text.Slice(start, i - start);
    }
}