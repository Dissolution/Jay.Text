namespace Jay.Text;

public ref struct TextEnumerator // : IEnumerator<char>, IEnumerator
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator TextEnumerator(ReadOnlySpan<char> text) => new TextEnumerator(text);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator TextEnumerator(string? text) => new TextEnumerator((ReadOnlySpan<char>)text);
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

    /// <inheritdoc cref="IEnumerator{T}"/>
    public char Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (Length == 0) return default;
            return _textSpan[0];
        }
    }
    
    public ReadOnlySpan<char> Text
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _textSpan;
    }

    public TextEnumerator(ReadOnlySpan<char> text)
    {
        _textSpan = text;
    }
    
    /// <inheritdoc cref="IEnumerator{T}"/>
    public bool MoveNext()
    {
        if (_textSpan.Length > 0)
        {
            _textSpan = _textSpan.Slice(1);
            return true;
        }
        return false;
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
    
    public TextEnumerator Take(int count)
    {
        TextEnumerator taken;
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
    
    public TextEnumerator TakeWhile(Func<char, bool> predicate)
    {
        TextEnumerator taken;
        var text = _textSpan;
        int i = 0;
        int len = Length;
        while (i < len && predicate(text[i]))
        {
            i++;
        }
        taken = _textSpan[..i];
        _textSpan = _textSpan[i..];
        return taken;
    }
    
    public TextEnumerator TakeUntil(Func<char, bool> predicate)
    {
        TextEnumerator taken;
        var text = _textSpan;
        int i = 0;
        int len = Length;
        while (i < len && !predicate(text[i]))
        {
            i++;
        }
        taken = _textSpan[..i];
        _textSpan = _textSpan[i..];
        return taken;
    }

    public TextEnumerator TakeWhiteSpace() => TakeWhile(char.IsWhiteSpace);

    public TextEnumerator TakeDigits() => TakeWhile(char.IsDigit);
   
    public TextEnumerator TakeLetters() => TakeWhile(char.IsLetter);

    public TextEnumerator GetEnumerator() => this;
    
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