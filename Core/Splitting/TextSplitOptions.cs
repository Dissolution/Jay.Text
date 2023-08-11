namespace Jay.Text.Splitting;

[Flags]
public enum TextSplitOptions
{
    None = 0,
    RemoveEmptyLines = 1 << 0,
    TrimLines = 1 << 1,
}