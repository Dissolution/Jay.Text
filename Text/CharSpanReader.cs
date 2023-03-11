using Jay.Text.Utilities;

namespace Jay.Text;

public ref struct CharSpanReader
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator CharSpanReader(ReadOnlySpan<char> text) => new(text);

    private readonly ReadOnlySpan<char> _text;
    private int _index;

    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _text.Length;
    }

    public char this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _text[index];
    }

    internal ReadOnlySpan<char> this[Range range]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _text[range];
    }

    public int Index
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _index;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _index = value.Clamp(0, _text.Length);
    }

    public ReadOnlySpan<char> Seen
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _text.Slice(0, _index);
    }

    public ReadOnlySpan<char> Available
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _text.Slice(_index);
    }

    public bool EndOfText
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _index >= _text.Length;
    }

    public CharSpanReader(ReadOnlySpan<char> text)
    {
        _text = text;
        _index = 0;
    }

#region Peek

    public char Peek()
    {
        if (TryPeek(out char ch)) return ch;
        throw new InvalidOperationException("Cannot Peek(): No characters available");
    }

    public bool TryPeek(out char ch)
    {
        if (_index < Length)
        {
            ch = _text[_index];
            return true;
        }

        ch = default;
        return false;
    }

    public ReadOnlySpan<char> Peek(int count)
    {
        if (TryPeek(count, out var text)) return text;
        throw new InvalidOperationException($"Cannot Peek({count}): Only {Available.Length} characters available");
    }

    public bool TryPeek(int count, out ReadOnlySpan<char> text)
    {
        if (_index + (uint)count <= Length)
        {
            text = _text.Slice(_index, count);
            return true;
        }

        text = default;
        return false;
    }

#endregion

#region Take

    public char Take()
    {
        if (TryTake(out char ch)) return ch;
        throw new InvalidOperationException("Cannot Take(): No characters available");
    }

    public bool TryTake(out char ch)
    {
        var result = TryPeek(out ch);
        if (result)
            _index++;
        return result;
    }

    public ReadOnlySpan<char> Take(int count)
    {
        if (TryTake(count, out var text)) return text;
        throw new InvalidOperationException($"Cannot Take({count}): Only {Available.Length} characters available");
    }

    public bool TryTake(int count, out ReadOnlySpan<char> text)
    {
        var result = TryPeek(count, out text);
        if (result)
            _index += count;
        return result;
    }

#endregion

#region Skip

    public void Skip()
    {
        if (TrySkip()) return;
        throw new InvalidOperationException("Cannot Skip(): No characters available");
    }

    public bool TrySkip()
    {
        return TryTake(out _);
    }

    public void Skip(int count)
    {
        if (TrySkip(count)) return;
        throw new InvalidOperationException($"Cannot Skip({count}): Only {Available.Length} characters available");
    }

    public bool TrySkip(int count)
    {
        return TryTake(count, out _);
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
        var i = _index;
        var start = i;
        var capacity = Length;
        while (i < capacity && predicate(text[i]))
        {
            i++;
        }

        _index = i;
        return _text[start..i];
    }


    public void SkipWhile(ReadOnlySpan<char> matchText) => TakeWhile(matchText);

    public ReadOnlySpan<char> TakeWhile(ReadOnlySpan<char> matchText,
        StringComparison comparison = StringComparison.Ordinal)
    {
        var text = _text;
        var i = _index;
        var start = i;
        var capacity = Length;
        while (i < capacity && text[i..].StartsWith(matchText, comparison))
        {
            i += matchText.Length;
        }

        _index = i;
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
        var i = _index;
        var start = i;
        var capacity = Length;
        while (i < capacity && !predicate(text[i]))
        {
            i++;
        }

        _index = i;
        return _text[start..i];
    }


    public void SkipUntil(ReadOnlySpan<char> matchText) => TakeUntil(matchText);

    public ReadOnlySpan<char> TakeUntil(ReadOnlySpan<char> matchText,
        StringComparison comparison = StringComparison.Ordinal)
    {
        var text = _text;
        var i = _index;
        var start = i;
        var capacity = Length;
        while (i < capacity && !text[i..].StartsWith(matchText, comparison))
        {
            i += matchText.Length;
        }

        _index = i;
        return _text[start..i];
    }

#endregion

    private ReadOnlySpan<char> TakeAny(CharSet charSet)
    {
        var text = _text;
        var i = _index;
        var start = i;
        var capacity = Length;
        while (i < capacity && charSet.Contains(text[i]))
        {
            i++;
        }

        _index = i;
        return _text[start..i];
    }


    public void SkipAny(params char[] chars) => TakeAny((CharSet)chars);

    public ReadOnlySpan<char> TakeAny(params char[] chars) => TakeAny((CharSet)chars);

    public void SkipAny(HashSet<char> chars)
    {
        var text = _text;
        var i = _index;
        var capacity = Length;
        while (i < capacity && chars.Contains(text[i]))
        {
            i++;
        }

        _index = i;
    }

    public ReadOnlySpan<char> TakeAny(HashSet<char> chars)
    {
        var text = _text;
        var i = _index;
        var start = i;
        var capacity = Length;
        while (i < capacity && chars.Contains(text[i]))
        {
            i++;
        }

        _index = i;
        return _text[start..i];
    }

    public override bool Equals(object? obj) => throw new NotSupportedException();

    public override int GetHashCode() => throw new NotSupportedException();

    public override string ToString()
    {
        var builder = new CharSpanBuilder();
        builder.Write(Seen);
        builder.Write("|");
        builder.Write(Available);
        return builder.ToStringAndDispose();
    }
}