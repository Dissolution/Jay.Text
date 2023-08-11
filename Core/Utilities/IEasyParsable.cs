#if NET7_0_OR_GREATER
namespace Jay.Text.Utilities;

public interface IEasyParsable<T> : ISpanParsable<T>, IParsable<T>
    where T : IEasyParsable<T>
{
    static T ISpanParsable<T>.Parse(ReadOnlySpan<char> text, IFormatProvider? _)
    {
        if (T.TryParse(text, out var value))
            return value;
        throw new ArgumentException($"Cannot parse '{text}' to a {typeof(T)}", nameof(text));
    }
    static T IParsable<T>.Parse([AllowNull, NotNullWhen(true)] string? str, IFormatProvider? _)
    {
        if (T.TryParse(str, out var value))
            return value;
        throw new ArgumentException($"Cannot parse '{str}' to a {typeof(T)}", nameof(str));
    }

    static bool ISpanParsable<T>.TryParse(ReadOnlySpan<char> text, IFormatProvider? _, [NotNullWhen(true)] out T? value)
        => T.TryParse(text, out value);

    static bool IParsable<T>.TryParse([AllowNull, NotNullWhen(true)] string? str, IFormatProvider? _, [NotNullWhen(true)] out T? value)
        => T.TryParse(str, out value);

    static virtual bool TryParse([AllowNull, NotNullWhen(true)] string? str, [NotNullWhen(true)] out T? value) => T.TryParse(str.AsSpan(), out value);
    static abstract bool TryParse(ReadOnlySpan<char> text, [NotNullWhen(true)] out T? value);
}
#endif