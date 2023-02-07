#if NET6_0_OR_GREATER
using System.Runtime.CompilerServices;

namespace Jay.Text;

[InterpolatedStringHandler]
public ref struct InterpolatedTextBuilder<TBuilder>
    where TBuilder : TextBuilder<TBuilder>
{
    private readonly TBuilder _builder;

    public InterpolatedTextBuilder(int literalLength, int formattedCount, TBuilder textBuilder)
    {
        _builder = textBuilder ?? throw new ArgumentNullException(nameof(textBuilder));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLiteral(string? text)
    {
        _builder.Write(text);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(ReadOnlySpan<char> text)
    {
        _builder.Write(text);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>(T value)
    {
        _builder.Write<T>(value);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>(T value, string? format)
    {
        _builder.Format<T>(value, format);
    }

    public override bool Equals(object? obj) => throw new NotSupportedException();

    public override int GetHashCode() => throw new NotSupportedException();

    public override string ToString()
    {
        return _builder.ToString();
    }
}
#endif