namespace Jay.Text.Utilities;

/// <summary>
/// Provides a way for methods capturing <see cref="FormattableString"/> to exist alongside methods that only care about <see cref="string"/>
/// </summary>
public readonly struct NonFormattableString : 
    IEquatable<NonFormattableString>,
    IEquatable<string>
{
    public static implicit operator NonFormattableString(string? str) => new NonFormattableString(str);
    public static implicit operator NonFormattableString(FormattableString _) => throw new InvalidOperationException();
    public static implicit operator NonFormattableString(ReadOnlySpan<char> _) => throw new InvalidOperationException();

    public static bool operator ==(NonFormattableString left, NonFormattableString right) => left.Equals(right);
    public static bool operator !=(NonFormattableString left, NonFormattableString right) => !left.Equals(right);

    public static bool operator ==(NonFormattableString nfs, string? str) => nfs.Equals(str);
    public static bool operator !=(NonFormattableString nfs, string? str) => !nfs.Equals(str);

    private readonly string? _str;

    /// <summary>
    /// Gets the contained <c>ReadOnlySpan&lt;char&gt;</c>
    /// </summary>
    public ReadOnlySpan<char> Text => _str.AsSpan();
    
    /// <summary>
    /// Gets the contained <see cref="string"/>
    /// </summary>
    public string String => _str ?? "";

    private NonFormattableString(string? str)
    {
        _str = str;
    }

    public bool Equals(NonFormattableString nfs)
    {
        return string.Equals(_str, nfs._str);
    }
    
    public bool Equals(string? str)
    {
        return string.Equals(_str, str);
    }

    public override bool Equals(object? obj)
    {
        if (obj is string str) return Equals(str);
        if (obj is NonFormattableString nfs) return Equals(nfs);
        return false;
    }

    public override int GetHashCode()
    {
        if (_str is null) return 0;
        return _str.GetHashCode();
    }

    public override string ToString()
    {
        return _str ?? "";
    }
}