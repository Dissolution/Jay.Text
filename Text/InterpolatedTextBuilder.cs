#if NET6_0_OR_GREATER
namespace Jay.Text;

[InterpolatedStringHandler]
public ref struct InterpolatedTextBuilder<TBuilder>
    where TBuilder : TextBuilder<TBuilder>
{
    private readonly TBuilder _builder;

    public InterpolatedTextBuilder(int literalLength, int formattedCount, TBuilder textBuilder)
    {
        ArgumentNullException.ThrowIfNull(textBuilder);
        _builder = textBuilder;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLiteral(string? text)
    {
        _builder.Append(text);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(ReadOnlySpan<char> text)
    {
        _builder.Append(text);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>(T value)
    {
        _builder.Append<T>(value);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>(T value, string? format)
    {
        _builder.Format<T>(value, format);
    }

    public override bool Equals(object? obj) => throw new NotSupportedException();

    public override int GetHashCode() => throw new NotSupportedException();

    public override string ToString() => _builder.ToString();
}
#endif