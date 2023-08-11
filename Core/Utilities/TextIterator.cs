using Jay.Text.Building;

namespace Jay.Text.Utilities;

public ref struct TextIterator
{
    private readonly ReadOnlySpan<char> _text;
    private int _position;
    
    private int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _text.Length;
    }
    internal int Available
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _text.Length - _position;
    }
    
    public int Index => _position;
    
  

    public TextIterator(ReadOnlySpan<char> text)
    {
        _text = text;
        _position = 0;
    }

    #region Peek

    public char Peek()
    {
        if (_position < Length)
            return _text[_position];
        throw new InvalidOperationException("Cannot Peek(): No characters available");
    }

    public bool TryPeek(out char ch)
    {
        if (_position < Length)
        {
            ch = _text[_position];
            return true;
        }

        ch = default;
        return false;
    }

    public ReadOnlySpan<char> Peek(int count)
    {
        if (_position + (uint)count <= Length)
        {
            return _text.Slice(_position, count);
        }
        throw new InvalidOperationException($"Cannot Peek({count}): Only {Available} characters available");
    }

    public bool TryPeek(int count, out ReadOnlySpan<char> text)
    {
        if (_position + (uint)count <= Length)
        {
            text = _text.Slice(_position, count);
            return true;
        }

        text = default;
        return false;
    }

#endregion

#region Take

    public char Take()
    {
        int index = _position;
        if (index < Length)
        {
            _position = index + 1;
            return _text[index];
        }
        throw new InvalidOperationException("Cannot Take(): No characters available");
    }

    public bool TryTake(out char ch)
    {
        int index = _position;
        if (index < Length)
        {
            _position = index + 1;
            ch = _text[index];
            return true;
        }

        ch = default;
        return false;
    }

    public ReadOnlySpan<char> Take(int count)
    {
        if (count <= 0) return default;
        int index = _position;
        int newIndex = index + count;
        if (newIndex <= Length)
        {
            _position = newIndex;
            return _text.Slice(index, count);
        }
        throw new InvalidOperationException($"Cannot Take({count}): Only {Available} characters available");
    }

    public bool TryTake(int count, out ReadOnlySpan<char> text)
    {
        if (count <= 0)
        {
            text = default;
            return true;
        }
        
        int index = _position;
        int newIndex = index + count;
        if (newIndex <= Length)
        {
            _position = newIndex;
            text = _text.Slice(index, count);
            return true;
        }

        text = default;
        return false;
    }

#endregion

#region Skip

    public void Skip()
    {
        int index = _position;
        if (index < Length)
        {
            _position = index + 1;
        }
        else
        {
            throw new InvalidOperationException("Cannot Skip(): No characters available");  
        }
    }

    public bool TrySkip()
    {
        int index = _position;
        if (index < Length)
        {
            _position = index + 1;
            return true;
        }
        
        return false;
    }

    public void Skip(int count)
    {
        if (count <= 0) return;

        int index = _position;
        int newIndex = index + count;
        if (newIndex <= Length)
        {
            _position = newIndex;
        }
        else
        {
            throw new InvalidOperationException($"Cannot Skip({count}): Only {Available} characters available");
        }
    }

    public bool TrySkip(int count)
    {
        if (count <= 0) return true;

        int index = _position;
        int newIndex = index + count;
        if (newIndex <= Length)
        {
            _position = newIndex;
            return true;
        }
        
        return false;
    }

#endregion

#region Skip|Take While

    public void SkipWhiteSpace() => TakeWhiteSpace();

    public ReadOnlySpan<char> TakeWhiteSpace() => TakeWhile(static ch => char.IsWhiteSpace(ch));

    public void SkipDigits() => TakeDigits();

    public ReadOnlySpan<char> TakeDigits() => TakeWhile(static ch => ch.IsAsciiDigit());

    public void SkipLetters() => TakeLetters();

    public ReadOnlySpan<char> TakeLetters() => TakeWhile(static ch => ch.IsAsciiLetter());

    public void SkipWhile(char matchChar) => TakeWhile(matchChar);

    public ReadOnlySpan<char> TakeWhile(char matchChar) => TakeWhile(ch => ch == matchChar);


    public void SkipWhile(Func<char, bool> predicate) => TakeWhile(predicate);

    public ReadOnlySpan<char> TakeWhile(Func<char, bool> predicate)
    {
        var text = _text;
        var i = _position;
        var start = i;
        var capacity = Length;
        while (i < capacity && predicate(text[i]))
        {
            i++;
        }

        _position = i;
        return _text[start..i];
    }


    public void SkipWhile(ReadOnlySpan<char> matchText) => TakeWhile(matchText);

    public ReadOnlySpan<char> TakeWhile(ReadOnlySpan<char> matchText,
        StringComparison comparison = StringComparison.Ordinal)
    {
        var text = _text;
        var i = _position;
        var start = i;
        var capacity = Length;
        while (i < capacity && text[i..].StartsWith(matchText, comparison))
        {
            i += matchText.Length;
        }

        _position = i;
        return _text[start..i];
    }

#endregion


#region Skip|Take Until

    // public void SkipWhiteSpace() => TakeWhiteSpace();
    //
    // public ReadOnlySpan<char> TakeWhiteSpace() => TakeWhile(static ch => char.IsWhiteSpace(ch));
    //
    // public void SkipDigits() => TakeDigits();
    //
    // public ReadOnlySpan<char> TakeDigits() => TakeWhile(static ch => char.IsAsciiDigit(ch));
    //
    // public void SkipLetters() => TakeLetters();
    //
    // public ReadOnlySpan<char> TakeLetters() => TakeWhile(static ch => ch.IsAsciiLetter());

    public void SkipUntil(char matchChar) => TakeUntil(matchChar);

    public ReadOnlySpan<char> TakeUntil(char matchChar) => TakeUntil(ch => ch == matchChar);


    public void SkipUntil(Func<char, bool> predicate) => TakeUntil(predicate);

    public ReadOnlySpan<char> TakeUntil(Func<char, bool> predicate)
    {
        var text = _text;
        var i = _position;
        var start = i;
        var capacity = Length;
        while (i < capacity && !predicate(text[i]))
        {
            i++;
        }

        _position = i;
        return _text[start..i];
    }


    public void SkipUntil(ReadOnlySpan<char> matchText) => TakeUntil(matchText);

    public ReadOnlySpan<char> TakeUntil(ReadOnlySpan<char> matchText,
        StringComparison comparison = StringComparison.Ordinal)
    {
        var text = _text;
        var i = _position;
        var start = i;
        var capacity = Length;
        while (i < capacity && !text[i..].StartsWith(matchText, comparison))
        {
            i += matchText.Length;
        }

        _position = i;
        return _text[start..i];
    }

#endregion
    
    public void SkipAny(params char[] chars) => TakeAny(chars.ToHashSet());

    public ReadOnlySpan<char> TakeAny(params char[] chars) => TakeAny(chars.ToHashSet());

    public void SkipAny(HashSet<char> chars)
    {
        var text = _text;
        var i = _position;
        var capacity = Length;
        while (i < capacity && chars.Contains(text[i]))
        {
            i++;
        }

        _position = i;
    }

    public ReadOnlySpan<char> TakeAny(HashSet<char> chars)
    {
        var text = _text;
        var i = _position;
        var start = i;
        var capacity = Length;
        while (i < capacity && chars.Contains(text[i]))
        {
            i++;
        }

        _position = i;
        return _text[start..i];
    }
    
   

    public bool MoveNext(out char ch)
    {
        int start = _position;
        int next = start + 1;
        if (next < Length)
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

    public override string ToString()
    {
        // prev 16 chars
        var pre = new Range(start: (_position - 16).Clamp(0, Length), end: _position);
        // next 16 chars
        var post = new Range(start: _position, end: (_position+16).Clamp(0, Length));

        return new TextBuilder()
            .Append(_text[pre])
            .Append('|')
            .Append(_text[post])
            .ToStringAndDispose();
    }
}