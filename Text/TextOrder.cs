namespace Jay.Text;

/// <summary>
/// The order text is read/processed
/// </summary>
public enum TextOrder : byte
{
    /// <summary>
    /// From front to back <br/>
    /// <c>[start..end]</c>
    /// </summary>
    LeftToRight = 0,

    /// <summary>
    /// From back to front <br/>
    /// <c>[end..start]</c>
    /// </summary>
    RightToLeft = 1,
}
