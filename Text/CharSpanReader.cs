using System.Runtime.CompilerServices;
using Jay.Text.Compat;

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

    public ReadOnlySpan<char> this[Range range]
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

    public ReadOnlySpan<char> Read
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

    public char Peek()
    {
        if (_index < Length)
        {
            return _text[_index];
        }
        throw new InvalidOperationException($"There are no characters left to read");
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
        if (!TryPeek(count, out var text))
            throw new InvalidOperationException($"There are not {count} characters available to read");
        return text;
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

    public char Take()
    {
        if (!TryTake(out char ch))
            throw new InvalidOperationException();
        return ch;
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
        if (!TryTake(count, out var text))
            throw new InvalidOperationException();
        return text;
    }

    public bool TryTake(int count, out ReadOnlySpan<char> text)
    {
        var result = TryPeek(count, out text);
        if (result)
            _index+=count;
        return result;
    }

    public void Skip()
    {
        if (!TrySkip())
            throw new InvalidOperationException();
    }

    public bool TrySkip()
    {
        var result = TryPeek(out _);
        if (result)
            _index++;
        return result;
    }

    public void Skip(int count)
    {
        if (!TrySkip(count))
            throw new InvalidOperationException();
    }

    public bool TrySkip(int count)
    {
        var result = TryPeek(count, out _);
        if (result)
            _index += count;
        return result;
    }


    public void SkipWhiteSpace()
    {
        var text = _text;
        var i = _index;
        var capacity = Length;
        while (i < capacity && char.IsWhiteSpace(text[i]))
        {
            i++;
        }
        _index = i;
    }

    public ReadOnlySpan<char> TakeWhiteSpace()
    {
        var text = _text;
        var i = _index;
        var start = i;
        var capacity = Length;
        while (i < capacity && char.IsWhiteSpace(text[i]))
        {
            i++;
        }

        _index = i;
        return _text[start..i];
    }

    public void SkipDigits()
    {
        var text = _text;
        var i = _index;
        var capacity = Length;
        while (i < capacity && char.IsDigit(text[i]))
        {
            i++;
        }

        _index = i;
    }

    public ReadOnlySpan<char> TakeDigits()
    {
        var text = _text;
        var i = _index;
        var start = i;
        var capacity = Length;
        while (i < capacity && char.IsDigit(text[i]))
        {
            i++;
        }

        _index = i;
        return _text[start..i];
    }

    public void SkipWhile(Func<char, bool> predicate)
    {
        var text = _text;
        var i = _index;
        var capacity = Length;
        while (i < capacity && predicate(text[i]))
        {
            i++;
        }

        _index = i;
    }

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

    public void SkipUntil(char matchChar)
    {
        var text = _text;
        var i = _index;
        var capacity = Length;
        while (i < capacity && text[i] != matchChar)
        {
            i++;
        }

        _index = i;
    }

    public ReadOnlySpan<char> TakeUntil(char matchChar)
    {
        var text = _text;
        var i = _index;
        var start = i;
        var capacity = Length;
        while (i < capacity && text[i] != matchChar)
        {
            i++;
        }

        _index = i;
        return _text[start..i];
    }

    public void SkipUntil(Func<char, bool> predicate)
    {
        var text = _text;
        var i = _index;
        var capacity = Length;
        while (i < capacity && !predicate(text[i]))
        {
            i++;
        }

        _index = i;
    }

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

    public void SkipAny(params char[] chars)
    {
        var text = _text;
        var i = _index;
        var capacity = Length;
        while (i < capacity && Enumerable.Contains(chars, text[i]))
        {
            i++;
        }

        _index = i;
    }

    public ReadOnlySpan<char> TakeAny(params char[] chars)
    {
        var text = _text;
        var i = _index;
        var start = i;
        var capacity = Length;
        while (i < capacity && Enumerable.Contains(chars, text[i]))
        {
            i++;
        }

        _index = i;
        return _text[start..i];
    }

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
        var builder = new CharSpanWriter();
        builder.Write(Read);
        builder.Write("|");
        builder.Write(Available);
        return builder.ToStringAndDispose();
    }
}