using System.Buffers;
using System.Runtime.CompilerServices;
using static InlineIL.IL;
using Xunit;

public delegate void TBA(ref TextBuilder textBuilder);
public delegate void TBA<T>(ref TextBuilder textBuilder, T value);

public ref struct TextBuilder
{
    private char[]? _borrowedCharArray;
    private Span<char> _charSpan;
    private int _position;

    public TextBuilder()
    {
        _charSpan = _borrowedCharArray = ArrayPool<char>.Shared.Rent(256);
        _position = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref TextBuilder RefThis()
    {
        Emit.Ldarg_0(); // the first argument to an instance method is the instance itself
        Emit.Ret();
        throw Unreachable();
    }
    
    public ref TextBuilder Fluent(scoped ReadOnlySpan<char> text)
    {
        text.CopyTo(_charSpan.Slice(_position));
        _position += text.Length;
        return ref RefThis();
    }

    public ref TextBuilder Nested(TBA tba)
    {
        tba(ref RefThis());
        return ref RefThis();
    }

    public ref TextBuilder Enumerate<T>(IEnumerable<T> enumerable, TBA<T> perValue)
    {
        foreach (var value in enumerable)
        {
            perValue(ref RefThis(), value);
        }
        return ref RefThis();
    }

    public void Dispose()
    {
        char[]? borrowed = _borrowedCharArray;
        // clear
        this = default;
        if (borrowed is not null)
        {
            ArrayPool<char>.Shared.Return(borrowed, true);
        }
    }

    public override string ToString()
    {
        return new string(_charSpan.Slice(0, _position));
    }
}






public class Tests
{
    [Fact]
    public void TestFluent()
    {
        TextBuilder builder = new();
        builder.Fluent("ABC").Fluent("DEF").Fluent("GHI");
        builder.Fluent("JKL").Fluent("MNO");
        builder.Fluent("PQR");
        Assert.Equal("ABCDEFGHIJKLMNOPQR", builder.ToString());
    }
    
    [Fact]
    public void TestNested()
    {
        TextBuilder builder = new();
        builder.Fluent("ABC").Nested((ref TextBuilder tb) => tb.Fluent("DEF"));
        builder.Nested((ref TextBuilder tb) => tb.Fluent("GHI"));
        Assert.Equal("ABCDEFGHI", builder.ToString());
    }
    
    [Fact]
    public void TestEnumerate()
    {
        TextBuilder builder = new();
        builder.Enumerate("ABC", (ref TextBuilder tb, char ch) => tb.Fluent(ch.ToString()));
        Assert.Equal("ABC", builder.ToString());
    }
}