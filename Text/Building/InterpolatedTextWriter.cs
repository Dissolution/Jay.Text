using System.Runtime.CompilerServices;
using Jay.Text.Building;

namespace Jay.Text;

#if NET6_0_OR_GREATER
[InterpolatedStringHandler]
#endif
public ref struct InterpolatedTextWriter
{
    private readonly TextWriter _writer;

    public InterpolatedTextWriter(int literalLength, int formattedCount,
        TextWriter textBuilder)
    {
        _writer = textBuilder ?? throw new ArgumentNullException(nameof(textBuilder));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLiteral(string? text)
    {
        _writer.Write(text);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(ReadOnlySpan<char> text)
    {
        _writer.Write(text);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>(T value, string? format = null)
    {
        _writer.WriteValue<T>(value, format);
    }

    public override bool Equals(object? obj) => throw new NotSupportedException();

    public override int GetHashCode() => throw new NotSupportedException();

    public override string ToString()
    {
        return _writer.ToString();
    }
}