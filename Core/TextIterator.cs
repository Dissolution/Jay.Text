namespace Jay.Text;

public ref struct TextIterator
{
    private readonly ReadOnlySpan<char> _text;
    private int _position;
    
    private int Capacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _text.Length;
    }

    public int Index => _position;
    
    public int RemainingLength => _text.Length - _position;
    
    public TextIterator(ReadOnlySpan<char> text)
    {
        _text = text;
        _position = 0;
    }

    public ReadOnlySpan<char> TakeWhile(char ch)
    {
        return TakeWhile(c => c == ch);
    }
    
    public ReadOnlySpan<char> TakeWhile(Func<char, bool> charPredicate)
    {
        int start = _position;
        int end = this.Capacity;
        int i = start;
        var text = _text;
        for (; i < end; i++)
        {
            if (!charPredicate(text[i])) break;
        }
        _position = i;
        return text.Slice(start, i - start);
    }


    public ReadOnlySpan<char> TakeUntil(char ch)
    {
        return TakeUntil(c => c == ch);
    }

    public ReadOnlySpan<char> TakeUntil(Func<char, bool> charPredicate)
    {
        int start = _position;
        int end = this.Capacity;
        int i = start;
        var text = _text;
        for (; i < end; i++)
        {
            if (charPredicate(text[i])) break;
        }
        _position = i;
        return text.Slice(start, i - start);
    }

    public ReadOnlySpan<char> Take(int count)
    {
        if (count <= 0) return default;
        int start = _position;
        int end = start + count;
        if (end > Capacity)
            throw new ArgumentOutOfRangeException(nameof(count));
        _position = end;
        return _text.Slice(start, count);
    }

    public bool MoveNext(out char ch)
    {
        int start = _position;
        int next = start + 1;
        if (next < Capacity)
        {
            ch = _text[start];
            _position = next;
            return true;
        }
        else
        {
            ch = default;
            return false;
        }
    }
}