

namespace Jay.Text.Extensions;

public static class CharExtensions
{
    /// <summary>
    /// Returns a <c>ReadOnlySpan&lt;char&gt;</c> containing this <see cref="char"/>
    /// </summary>
    public static ReadOnlySpan<char> AsReadOnlySpan(in this char ch) => new ReadOnlySpan<char>(in ch);

    /// <summary>
    /// Is this <see cref="char"/> a digit?
    /// </summary>
    public static bool IsDigit(this char ch) => char.IsDigit(ch);

    /// <summary>
    /// Is this <see cref="char"/> considered white-space?
    /// </summary>
    public static bool IsWhiteSpace(this char ch) => char.IsWhiteSpace(ch);

    /// <summary>
    /// Converts this <see cref="char"/> into its UpperCase equivalent.
    /// </summary>
    public static char ToUpper(this char ch) => char.ToUpper(ch);

    /// <summary>
    /// Converts this <see cref="char"/> into its UpperCase equivalent.
    /// </summary>
    public static char ToUpper(this char ch, CultureInfo culture) => char.ToUpper(ch, culture);

    /// <summary>
    /// Converts this <see cref="char"/> into its LowerCase equivalent.
    /// </summary>
    public static char ToLower(this char ch) => char.ToLower(ch);

    /// <summary>
    /// Converts this <see cref="char"/> into its LowerCase equivalent.
    /// </summary>
    public static char ToLower(this char ch, CultureInfo culture) => char.ToLower(ch, culture);
}