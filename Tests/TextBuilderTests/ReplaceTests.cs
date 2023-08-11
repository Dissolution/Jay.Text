namespace Jay.Text.Tests.TextBuilderTests;

public class ReplaceTests
{
    [Fact]
    public void ReplaceChar()
    {
        using var text = new TextBuilder();
        text.Append(TestData.LoremIpsum);
        text.Length.Should().Be(TestData.LoremIpsum.Length);
        text.Written.CountInstances('o').Should().Be(4);
        text.Written.CountInstances('.').Should().Be(1);
        text.Replace('o', '.');
        text.Length.Should().Be(TestData.LoremIpsum.Length);
        text.Written.CountInstances('o').Should().Be(0);
        text.Written.CountInstances('.').Should().Be(5);
    }

    

    [Fact]
    public void ReplaceStringExact()
    {
        using var text = new TextBuilder();
        text.Append(TestData.LoremIpsum);
        text.Length.Should().Be(TestData.LoremIpsum.Length);
        text.Written.CountInstances('o').Should().Be(4);
        text.Written.CountInstances('.').Should().Be(1);
        text.Replace("o", ".");
        text.Length.Should().Be(TestData.LoremIpsum.Length);
        text.Written.CountInstances('o').Should().Be(0);
        text.Written.CountInstances('.').Should().Be(5);

        // ip / it
        int ipCount = TestExtensions.CountInstances(text.Written, "ip");
        ipCount.Should().Be(2);
        int itCount = TestExtensions.CountInstances(text.Written, "it");
        itCount.Should().Be(2);

        text.Replace("ip", "it");
        ipCount = TestExtensions.CountInstances(text.Written, "ip");
        ipCount.Should().Be(0);
        itCount = TestExtensions.CountInstances(text.Written, "it");
        itCount.Should().Be(4);
    }

    [Fact]
    public void ReplaceStringShrink()
    {
        using var text = new TextBuilder();
        text.Append(TestData.Stutter);
        text.Length.Should().Be(TestData.Stutter.Length);
        int thCount = TestExtensions.CountInstances(text.Written, "th");
        thCount.Should().Be(3);

        text.Replace("th", "d");
        text.Length.Should().Be(TestData.Stutter.Length - 3);
        thCount = TestExtensions.CountInstances(text.Written, "th");
        thCount.Should().Be(0);
    }
    
    [Fact]
    public void ReplaceStringShrinkIgnoreCase()
    {
        using var text = new TextBuilder();
        text.Append(TestData.Stutter);
        text.Length.Should().Be(TestData.Stutter.Length);
        int thCount = TestExtensions.CountInstances(text.Written, "TH", StringComparison.OrdinalIgnoreCase);
        thCount.Should().Be(4);

        text.Replace("th", "d", StringComparison.OrdinalIgnoreCase);
        text.Length.Should().Be(TestData.Stutter.Length - 4);
        thCount = TestExtensions.CountInstances(text.Written, "TH", StringComparison.OrdinalIgnoreCase);
        thCount.Should().Be(0);
    }
    
    [Fact]
    public void ReplaceStringGrow()
    {
        using var text = new TextBuilder();
        text.Append(TestData.Stutter);
        text.Length.Should().Be(TestData.Stutter.Length);
        int thCount = TestExtensions.CountInstances(text.Written, "th");
        thCount.Should().Be(3);

        text.Replace("th", "bgi");
        text.Length.Should().Be(TestData.Stutter.Length + 3);
        thCount = TestExtensions.CountInstances(text.Written, "th");
        thCount.Should().Be(0);
    }
    
    [Fact]
    public void ReplaceStringGrowIgnoreCase()
    {
        using var text = new TextBuilder();
        text.Append(TestData.Stutter);
        text.Length.Should().Be(TestData.Stutter.Length);
        int thCount = TestExtensions.CountInstances(text.Written, "th", StringComparison.OrdinalIgnoreCase);
        thCount.Should().Be(4);

        text.Replace("th", "bgi", StringComparison.OrdinalIgnoreCase);
        text.Length.Should().Be(TestData.Stutter.Length + 4);
        thCount = TestExtensions.CountInstances(text.Written, "th");
        thCount.Should().Be(0);
    }
}