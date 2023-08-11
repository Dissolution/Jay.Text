namespace Jay.Text.Tests;

public static class TestExtensions
{
    public static int CountInstances(this string? text, char occurence)
    {
        if (text is null) return 0;
        int count = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == occurence)
                count++;
        }
        return count;
    }

    public static int CountInstances(this Span<char> text, char occurence)
    {
        int count = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == occurence)
                count++;
        }
        return count;
    }
    
    public static int CountInstances(this ReadOnlySpan<char> text, char occurence)
    {
        int count = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == occurence)
                count++;
        }
        return count;
    }

    
    public static int CountInstances(this string? text, string? occurence) =>
        CountInstances(text.AsSpan(), occurence.AsSpan());

    public static int CountInstances(this ReadOnlySpan<char> text, string? occurence) =>
        CountInstances(text, occurence.AsSpan());

    public static int CountInstances(this string? text, ReadOnlySpan<char> occurence) =>
        CountInstances(text.AsSpan(), occurence);

    public static int CountInstances(this ReadOnlySpan<char> text, ReadOnlySpan<char> occurence)
    {
        int instances = 0;
        int offset = 0;
        int i;
        while ((i = text.Slice(offset).IndexOf(occurence)) >= 0)
        {
            instances++;
            offset += (i + occurence.Length);
        }
        return instances;
    }

    public static int CountInstances(this string? text, string? occurence, StringComparison comparison) =>
        CountInstances(text.AsSpan(), occurence.AsSpan(), comparison);

    public static int CountInstances(this ReadOnlySpan<char> text, string? occurence, StringComparison comparison) =>
        CountInstances(text, occurence.AsSpan(), comparison);

    public static int CountInstances(this string? text, ReadOnlySpan<char> occurence, StringComparison comparison) =>
        CountInstances(text.AsSpan(), occurence, comparison);
    
    public static int CountInstances(this ReadOnlySpan<char> text, ReadOnlySpan<char> occurence, StringComparison comparison)
    {
        int instances = 0;
        int offset = 0;
        int i = 0;
        while ((i = text.Slice(offset).IndexOf(occurence, comparison)) >= 0)
        {
            instances++;
            offset += (i + occurence.Length);
        }
        return instances;
    }
}