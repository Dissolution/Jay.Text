using System;

namespace Jay.Text
{
    /// <summary>
    /// A constraint on a value that implicitly converts to and from <see cref="string"/> and allows for a <see cref="FormattableString"/> to be passed directly to a method.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class allows for <see cref="string"/>/<see langword="params"/> <see cref="object"/>[] args methods to exist alongside
    /// <see cref="FormattableString"/> methods without the compiler automatically converting the <see cref="FormattableString"/>
    /// to a <see cref="string"/>.
    /// </para>
    /// <para>
    /// e.g.:
    /// void Thing(string format, params object[] args)
    /// void Thing(FormattableString fStr)
    /// In that case, passing in `$"Blah{1}"` would use the first overload
    /// </para>
    /// <para>
    /// Whereas:
    /// void Thing(RawString format, params object[] args)
    /// void Thing(FormattableString fstr)
    /// In this case, passing in `$"Blah{1}"` would use the second overload
    /// </para>
    /// </remarks>
    public readonly struct RawString : IEquatable<string>
    {
        public static implicit operator RawString(string? str) => new RawString(str);
        public static explicit operator string(RawString nfStr) => nfStr.String;
        // This exists to ensure that the compiler does the right behavior
        public static implicit operator RawString(FormattableString fStr) => throw new InvalidOperationException();

        private readonly string? _string;

        public string String => _string ?? string.Empty;

        public RawString(string? str)
        {
            _string = str;
        }

        public bool Equals(string? str)
        {
            return string.Equals(_string, str);
        }

        public bool Equals(RawString nfs)
        {
            return string.Equals(_string, nfs._string);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (obj is string str)
                return string.Equals(str, _string);
            if (obj is RawString nfs)
                return string.Equals(nfs._string, _string);
            return false;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(_string);
        }

        /// <inheritdoc />
        public override string ToString() => String;
    }
}