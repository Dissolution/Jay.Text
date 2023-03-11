using Jay.Text.Utilities;

namespace Jay.Text.Scratch.WrittenExtensions;

public static class WrittenExtensions
{
    public static ref StackTextBuilder TrimStart(this ref StackTextBuilder textBuilder)
    {
        int i = 0;
        while (i < textBuilder.Length && char.IsWhiteSpace(textBuilder[i]))
            i++;
        if (i > 0)
        {
            TextHelper.CopyTo(textBuilder.Written[i..], textBuilder.Written);
            textBuilder.Length -= i;
        }
        return ref textBuilder;
    }
    
    public static ref StackTextBuilder TrimEnd(this ref StackTextBuilder textBuilder)
    {
        int e = textBuilder.Length - 1;
        while (e >= 0 && char.IsWhiteSpace(textBuilder[e]))
            e--;
        if (e < textBuilder.Length-1)
        {
            textBuilder.Length = e + 1;
        }
        return ref textBuilder;
    }
}