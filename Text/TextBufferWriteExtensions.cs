
/* Unmerged change from project 'Text (netstandard2.1)'
Before:
using Jay.Text.Utilities;
After:
using Jay;
using Jay.Text;
using Jay.Text;
using Jay.Text.Text.Utilities;
*/
using Jay.Text.Utilities;

namespace Jay.Text;

internal static class TextBufferWriteExtensions
{
    public static void Write(this TextBuffer textBuffer, char ch)
    {
        textBuffer.Allocate() = ch;
    }

    public static void Write(this TextBuffer textBuffer, ReadOnlySpan<char> text)
    {
        int textLen = text.Length;
        TextHelper.Unsafe.CopyTo(text, textBuffer.Allocate(textLen), textLen);
    }

    public static void Write(this TextBuffer textBuffer, string? str)
    {
        if (str is null) return;
        var strLen = str.Length;
        if (strLen == 0) return;
        TextHelper.Unsafe.CopyTo(str, textBuffer.Allocate(strLen), strLen);
    }

    public static void Write<T>(this TextBuffer textBuffer, T? value)
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
                while (!((ISpanFormattable)value).TryFormat(textBuffer.Available, out charsWritten, default, default))
                {
                    textBuffer.GrowBy(BuilderHelper.MinimumCapacity);
                }
                textBuffer.Length += charsWritten;
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

        textBuffer.Write(str);
    }

    public static void WriteFormat<T>(this TextBuffer textBuffer, T? value, string? format, IFormatProvider? provider = null)
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
                while (!((ISpanFormattable)value).TryFormat(textBuffer.Available, out charsWritten, format, provider))
                {
                    textBuffer.GrowBy(BuilderHelper.MinimumCapacity);
                }
                textBuffer.Length += charsWritten;
                return;
            }
#endif

            // constrained call avoiding boxing for value types
            str = ((IFormattable)value).ToString(format, provider);
        }
        else
        {
            str = value?.ToString();
        }

        textBuffer.Write(str);
    }
}