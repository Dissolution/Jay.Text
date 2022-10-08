namespace Jay.Text;

public ref struct CharSpanReader // : IEnumerator<char>, IEnumerator
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator CharSpanReader(ReadOnlySpan<char> text) => new CharSpanReader(text);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator CharSpanReader(string? text) => new CharSpanReader((ReadOnlySpan<char>)text);
    private ReadOnlySpan<char> _textSpan;
    
    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _textSpan.Length;
    }

    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _textSpan.Length == 0;
    }

    public ref readonly char this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Validate.Index(Length, index);
            return ref _textSpan[index];
        }
    }
    
    public ReadOnlySpan<char> Text
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _textSpan;
    }

    public CharSpanReader(ReadOnlySpan<char> text)
    {
        _textSpan = text;
    }
  
    public void Skip(int count)
    {
        if (count >= Length)
        {
            _textSpan = default;
        }
        else if (count > 0)
        {
            _textSpan = _textSpan[count..];
        }
    }
    
    public void SkipWhile(Func<char, bool> predicate)
    {
        var text = _textSpan;
        int i = 0;
        int len = Length;
        while (i < len && predicate(text[i]))
        {
            i++;
        }
        _textSpan = _textSpan[i..];
    }
    
    public void SkipUntil(Func<char, bool> predicate)
    {
        var text = _textSpan;
        int i = 0;
        int len = Length;
        while (i < len && !predicate(text[i]))
        {
            i++;
        }
        _textSpan = _textSpan[i..];
    }
    

    public void SkipWhiteSpace() => SkipWhile(char.IsWhiteSpace);

    public void SkipDigits() => SkipWhile(char.IsDigit);
   
    public void SkipLetters() => SkipWhile(char.IsLetter);
    
    public CharSpanReader Take(int count)
    {
        CharSpanReader taken;
        if (count >= Length)
        {
            taken = _textSpan;
            _textSpan = default;
        }
        else if (count > 0)
        {
            taken = _textSpan[..count];
            _textSpan = _textSpan[count..];
        }
        else
        {
            taken = default;
        }
        return taken;
    }
    
    public CharSpanReader TakeWhile(Func<char, bool> predicate)
    {
        var text = _textSpan;
        int i = 0;
        int len = Length;
        while (i < len && predicate(text[i]))
        {
            i++;
        }
        CharSpanReader taken = _textSpan[..i];
        _textSpan = _textSpan[i..];
        return taken;
    }
    
    public CharSpanReader TakeUntil(Func<char, bool> predicate)
    {
        var text = _textSpan;
        int i = 0;
        int len = Length;
        while (i < len && !predicate(text[i]))
        {
            i++;
        }
        CharSpanReader taken = _textSpan[..i];
        _textSpan = _textSpan[i..];
        return taken;
    }

    public CharSpanReader TakeWhiteSpace() => TakeWhile(char.IsWhiteSpace);

    public CharSpanReader TakeDigits() => TakeWhile(char.IsDigit);
   
    public CharSpanReader TakeLetters() => TakeWhile(char.IsLetter);

    public bool Equals(ReadOnlySpan<char> text)
    {
        return text.SequenceEqual(_textSpan);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is string str) return Equals(str);
        if (obj is char[] chars) return Equals(chars);
        return false;
    }
    public override int GetHashCode()
    {
        return string.GetHashCode(_textSpan);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString()
    {
        return new string(_textSpan);
    }
}