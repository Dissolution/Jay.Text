using System.Buffers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Jay.Text.Compat;
using Jay.Text.Extensions;

// ReSharper disable UnusedParameter.Local

namespace Jay.Text;

internal static class BuilderHelper
{
    public const int MinimumCapacity = 1024;
    public const int MaximumCapacity = 0x3FFFFFDF; // == string.MaxLength < Array.MaxLength

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetStartingCapacity(int literalLength, int formattedCount)
    {
        return (literalLength + (formattedCount * 16)).Clamp(MinimumCapacity, MaximumCapacity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetNextCapacity(int currentCapacity, int addingCharCount)
    {
        return (currentCapacity + addingCharCount) * 2;
    }
}

/// <summary>
/// A custom minimized implementation of an <c>Interpolated String Handler</c>
/// </summary>
#if NET6_0_OR_GREATER
[InterpolatedStringHandler]
#endif
public ref struct CharSpanBuilder
{
    /// <summary>Array rented from the array pool and used to back <see cref="_chars"/>.</summary>
    private char[]? _charArray;

    /// <summary>The span to write into.</summary>
    private Span<char> _chars;

    /// <summary>Position at which to write the next character.</summary>
    private int _index;

    /// <summary>
    /// Gets the <c>Span&lt;char&gt;</c> of written characters
    /// </summary>
    public Span<char> Written
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _chars.Slice(0, _index);
    }

    /// <summary>
    /// Gets the <c>Span&lt;char&gt;</c> of available characters
    /// </summary>
    public Span<char> Available
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _chars.Slice(_index);
    }

    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _index;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _index = value.Clamp(0, Capacity);
    }

    public int Capacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _chars.Length;
    }

    public CharSpanBuilder()
    {
        _chars = _charArray = ArrayPool<char>.Shared.Rent(BuilderHelper.MinimumCapacity);
        _index = 0;
    }

    public CharSpanBuilder(Span<char> initialBuffer)
    {
        _chars = initialBuffer;
        _charArray = null;
        _index = 0;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public CharSpanBuilder(int literalLength, int formattedCount)
    {
        _chars = _charArray =
            ArrayPool<char>.Shared.Rent(BuilderHelper.GetStartingCapacity(literalLength, formattedCount));
        _index = 0;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public CharSpanBuilder(int literalLength, int formattedCount, Span<char> initialBuffer)
    {
        _chars = initialBuffer;
        _charArray = null;
        _index = 0;
    }

    #region Grow

    /// <summary>Grow the size of <see cref="_chars"/> to at least the specified <paramref name="minCapacity"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GrowCore(int minCapacity)
    {
        char[] newArray = ArrayPool<char>.Shared.Rent(minCapacity);
        TextHelper.Unsafe.CopyBlock(
            in _chars.GetPinnableReference(),
            ref newArray.GetPinnableReference(),
            _index);

        char[]? toReturn = _charArray;
        _chars = _charArray = newArray;

        if (toReturn is not null)
        {
            ArrayPool<char>.Shared.Return(toReturn);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowBy(int addingCharCount)
    {
        GrowCore(BuilderHelper.GetNextCapacity(Capacity, addingCharCount));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowThenCopy(char ch)
    {
        int index = _index;
        GrowCore(BuilderHelper.GetNextCapacity(Capacity, 1));
        TextHelper.Unsafe.CopyBlock(
            in ch,
            ref Available.GetPinnableReference(),
            1);
        _index = index + 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowThenCopy(ReadOnlySpan<char> text)
    {
        int index = _index;
        int len = text.Length;
        GrowCore(BuilderHelper.GetNextCapacity(Capacity, len));
        TextHelper.Unsafe.CopyBlock(
            in text.GetPinnableReference(),
            ref Available.GetPinnableReference(),
            len);
        _index = index + len;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowThenCopy(string text)
    {
        int index = _index;
        int len = text.Length;
        GrowCore(BuilderHelper.GetNextCapacity(Capacity, len));
        TextHelper.Unsafe.CopyTo(text, Available);
        _index = index + len;
    }

    #endregion

    #region Interpolated String Handler implementations

    /// <summary>Writes the specified string to the handler.</summary>
    /// <param name="text">The string to write.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLiteral(string text)
    {
        if (text.Length == 1)
        {
            int pos = _index;
            Span<char> chars = _chars;
            if ((uint)pos < (uint)chars.Length)
            {
                chars[pos] = text[0];
                _index = pos + 1;
            }
            else
            {
                GrowThenCopy(text);
            }

            return;
        }

        if (text.Length == 2)
        {
            int pos = _index;
            Span<char> chars = _chars;
            if ((uint)pos < chars.Length - 1)
            {
                chars[pos++] = text[0];
                chars[pos++] = text[1];
                _index = pos;
            }
            else
            {
                GrowThenCopy(text);
            }

            return;
        }

        Write(text);
    }

    /// <summary>Writes the specified value to the handler.</summary>
    /// <param name="value">The value to write.</param>
    /// <typeparam name="T">The type of the value to write.</typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted<T>(T? value) => Write<T>(value);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted<T>(T value, string? format) => Write<T>(value, format);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted(char ch) => Write(ch);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted(ReadOnlySpan<char> value) => Write(value);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted(string? value) => Write(value);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted(object? obj) => Write<object>(obj);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted(object? value, string? format) => Write<object?>(value, format);

    #endregion

    #region Write

    public void Write(char ch)
    {
        int pos = _index;
        Span<char> chars = _chars;
        if ((uint)pos < (uint)chars.Length)
        {
            chars[pos] = ch;
            _index = pos + 1;
        }
        else
        {
            GrowThenCopy(ch);
        }
    }

    public void Write(ReadOnlySpan<char> text)
    {
        if (text.TryCopyTo(Available))
        {
            _index += text.Length;
        }
        else
        {
            GrowThenCopy(text);
        }
    }

    public void Write(params char[] chars) => Write(chars.AsSpan());

    public void Write(string? text)
    {
        if (text is not null)
        {
            if (TextHelper.TryCopyTo(text.AsSpan(), Available))
            {
                _index += text.Length;
            }
            else
            {
                GrowThenCopy(text);
            }
        }
    }

    public void Write<T>(T? value)
    {
        string? str;
        if (value is IFormattable)
        {
#if NET6_0_OR_GREATER
            // If the value can format itself directly into our buffer, do so.
            if (value is ISpanFormattable)
            {
                int charsWritten;
                // constrained call avoiding boxing for value types
                while (!((ISpanFormattable)value).TryFormat(_chars.Slice(_index),
                    out charsWritten, default, default))
                {
                    GrowBy(BuilderHelper.MinimumCapacity);
                }

                _index += charsWritten;
                return;
            }
#endif

            // constrained call avoiding boxing for value types
            str = ((IFormattable)value).ToString(default, default);
        }
        else
        {
            str = value?.ToString();
        }

        Write(str);
    }

    public void Write<T>(T? value, string? format)
    {
        string? str;
        if (value is IFormattable)
        {
#if NET6_0_OR_GREATER
            // If the value can format itself directly into our buffer, do so.
            if (value is ISpanFormattable)
            {
                int charsWritten;
                // constrained call avoiding boxing for value types
                while (!((ISpanFormattable)value).TryFormat(_chars.Slice(_index),
                    out charsWritten, format, default))
                {
                    GrowBy(BuilderHelper.MinimumCapacity);
                }

                _index += charsWritten;
                return;
            }
#endif

            // constrained call avoiding boxing for value types
            str = ((IFormattable)value).ToString(format, default);
        }
        else
        {
            str = value?.ToString();
        }

        Write(str);
    }

    #endregion


    /// <summary>Clears the handler, returning any rented array to the pool.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // used only on a few hot paths
    public void Dispose()
    {
        char[]? toReturn = _charArray;
        this = default; // defensive clear
        if (toReturn is not null)
        {
            ArrayPool<char>.Shared.Return(toReturn);
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override bool Equals(object? obj) => false;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override int GetHashCode() => 0;

    public string ToStringAndDispose()
    {
        string result = ToString();
        Dispose();
        return result;
    }

    public override string ToString()
    {
#if NET48 || NETSTANDARD2_0
        unsafe
        {
            fixed (char* ptr = Written)
            {
                return new string(ptr, 0, _index);
            }
        }
#else
        return new string(Written);
#endif
    }
}