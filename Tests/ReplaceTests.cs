using FluentAssertions;

namespace Jay.Text.Tests;

internal static class TestData
{
    public const string QBF = "The quick brown fox jumped over the lazy dogs.";
    public const string LoremIpsum = "Lorem ipsum dolor sit amet, consectetur adipiscing elit.";
    public const string Stutter = "Th-th-th-that's all, folks!";
}

public class InterpolatedTextBuilderTests
{
    [Fact]
    public void InterpolatedWrite()
    {
        using var text = TextBuilder.Borrow();
        text.Length.Should().Be(0);
        string abc = "ABC";
        text.Write($"{abc}");
        text.Length.Should().Be(3);
        text.ToString().Should().Be(abc);
        text.Clear();
        text.Length.Should().Be(0);
        text.Write($"{DateTime.Now:s}");
        text.Length.Should().Be(19);
    }
}

public class ReplaceTests
{
    [Fact]
    public void ReplaceChar()
    {
        using var text = TextBuilder.Borrow();
        text.Write(TestData.LoremIpsum);
        text.Length.Should().Be(TestData.LoremIpsum.Length);
        text.Count(ch => ch == 'o').Should().Be(4);
        text.Count(ch => ch == '.').Should().Be(1);
        text.Replace('o', '.');
        text.Length.Should().Be(TestData.LoremIpsum.Length);
        text.Count(ch => ch == 'o').Should().Be(0);
        text.Count(ch => ch == '.').Should().Be(5);
    }

    private static int CountInstances(ReadOnlySpan<char> text, ReadOnlySpan<char> occurence)
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
    
    private static int CountInstances(ReadOnlySpan<char> text, ReadOnlySpan<char> occurence, StringComparison comparison)
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

    [Fact]
    public void ReplaceStringExact()
    {
        using var text = TextBuilder.Borrow();
        text.Write(TestData.LoremIpsum);
        text.Length.Should().Be(TestData.LoremIpsum.Length);
        text.Count(ch => ch == 'o').Should().Be(4);
        text.Count(ch => ch == '.').Should().Be(1);
        text.Replace("o", ".");
        text.Length.Should().Be(TestData.LoremIpsum.Length);
        text.Count(ch => ch == 'o').Should().Be(0);
        text.Count(ch => ch == '.').Should().Be(5);

        // ip / it
        int ipCount = CountInstances(text.Written, "ip");
        ipCount.Should().Be(2);
        int itCount = CountInstances(text.Written, "it");
        itCount.Should().Be(2);

        text.Replace("ip", "it");
        ipCount = CountInstances(text.Written, "ip");
        ipCount.Should().Be(0);
        itCount = CountInstances(text.Written, "it");
        itCount.Should().Be(4);
    }

    [Fact]
    public void ReplaceStringShrink()
    {
        using var text = TextBuilder.Borrow();
        text.Write(TestData.Stutter);
        text.Length.Should().Be(TestData.Stutter.Length);
        int thCount = CountInstances(text.Written, "th");
        thCount.Should().Be(3);

        text.Replace("th", "d");
        text.Length.Should().Be(TestData.Stutter.Length - 3);
        thCount = CountInstances(text.Written, "th");
        thCount.Should().Be(0);
    }
    
    [Fact]
    public void ReplaceStringShrinkIgnoreCase()
    {
        using var text = TextBuilder.Borrow();
        text.Write(TestData.Stutter);
        text.Length.Should().Be(TestData.Stutter.Length);
        int thCount = CountInstances(text.Written, "TH", StringComparison.OrdinalIgnoreCase);
        thCount.Should().Be(4);

        text.Replace("th", "d", StringComparison.OrdinalIgnoreCase);
        text.Length.Should().Be(TestData.Stutter.Length - 4);
        thCount = CountInstances(text.Written, "TH", StringComparison.OrdinalIgnoreCase);
        thCount.Should().Be(0);
    }
    
    [Fact]
    public void ReplaceStringGrow()
    {
        using var text = TextBuilder.Borrow();
        text.Write(TestData.Stutter);
        text.Length.Should().Be(TestData.Stutter.Length);
        int thCount = CountInstances(text.Written, "th");
        thCount.Should().Be(3);

        text.Replace("th", "bgi");
        text.Length.Should().Be(TestData.Stutter.Length + 3);
        thCount = CountInstances(text.Written, "th");
        thCount.Should().Be(0);
    }
    
    [Fact]
    public void ReplaceStringGrowIgnoreCase()
    {
        using var text = TextBuilder.Borrow();
        text.Write(TestData.Stutter);
        text.Length.Should().Be(TestData.Stutter.Length);
        int thCount = CountInstances(text.Written, "th", StringComparison.OrdinalIgnoreCase);
        thCount.Should().Be(4);

        text.Replace("th", "bgi", StringComparison.OrdinalIgnoreCase);
        text.Length.Should().Be(TestData.Stutter.Length + 4);
        thCount = CountInstances(text.Written, "th");
        thCount.Should().Be(0);
    }
}