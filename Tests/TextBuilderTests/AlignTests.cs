namespace Jay.Text.Tests.TextBuilderTests;

public class AlignTests
{
    [Fact]
    public void CanAlignChar()
    {
        using var textBuilder = new TextBuilder();

        Assert.Throws<ArgumentOutOfRangeException>(() => textBuilder.Align('a', 0, Alignment.Center));
        textBuilder.Align('b', 1, Alignment.Left);
        textBuilder[0].Should().Be('b');
        textBuilder.Align('c', 1, Alignment.Right);
        textBuilder[1].Should().Be('c');
        textBuilder.Align('d', 1, Alignment.Center);
        textBuilder[2].Should().Be('d');

        textBuilder.Align('e', 2, Alignment.Left);
        textBuilder[^2..].ToString().Should().BeEquivalentTo("e ");
        textBuilder.Align('f', 2, Alignment.Right);
        textBuilder[^2..].ToString().Should().BeEquivalentTo(" f");
        textBuilder.Align('g', 2, Alignment.Center);
        textBuilder[^2..].ToString().Should().BeEquivalentTo("g ");
        textBuilder.Align('h', 2, Alignment.Center | Alignment.Right);
        textBuilder[^2..].ToString().Should().BeEquivalentTo(" h");
        
        textBuilder.Align('i', 3, Alignment.Left);
        textBuilder[^3..].ToString().Should().BeEquivalentTo("i  ");
        textBuilder.Align('j', 3, Alignment.Right);
        textBuilder[^3..].ToString().Should().BeEquivalentTo("  j");
        textBuilder.Align('k', 3, Alignment.Center);
        textBuilder[^3..].ToString().Should().BeEquivalentTo(" k ");
    }

    [Fact]
    public void CanAlignText()
    {
        using var textBuilder = new TextBuilder();

        var testStrings = new string?[] { (string?)null, string.Empty, "1", "22" };
        var testWidths = new int[] { 0, 1, 2, 3, 4, 5, 6 };
        
        foreach (string? testString in testStrings)
        foreach (int testWidth in testWidths)
        {
            int len = testString?.Length ?? 0;
            if (len > testWidth)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => textBuilder.Align(testString, testWidth, Alignment.Center));
            }
            else
            {
                ReadOnlySpan<char> testText = testString.AsSpan();
                Span<char> wrote;
                
                // Left
                textBuilder.Align(testString, testWidth, Alignment.Left);
                wrote = textBuilder.Written[^testWidth..];
                wrote.Length.Should().Be(testWidth);
                wrote.StartsWith(testText).Should().BeTrue();
                
                // Right
                textBuilder.Align(testString, testWidth, Alignment.Right);
                wrote = textBuilder.Written[^testWidth..];
                wrote.Length.Should().Be(testWidth);
                wrote.EndsWith(testText).Should().BeTrue();
                
                // Center
                int spaces = testWidth - len;

                foreach (Alignment alignment in new[] { Alignment.Center, Alignment.Center | Alignment.Left, Alignment.Center | Alignment.Right })
                {
                    textBuilder.Align(testString, testWidth, alignment);
                    wrote = textBuilder.Written[^testWidth..];
                    var reader = new TextIterator(wrote);
                    reader.Available.Should().Be(testWidth);
                    
                    var frontSpaces = reader.TakeWhile(static ch => ch == ' ');
                    
                    var text = reader.TakeUntil(' ');
                    text.Length.Should().Be(len);
                    text.SequenceEqual(testText).Should().BeTrue();
                    
                    var backSpaces = reader.TakeWhile(static ch => ch == ' ');
                    reader.Available.Should().Be(0);
                    
                    (frontSpaces.Length + backSpaces.Length).Should().Be(spaces);
                    
                    if (len > 0)
                    {
                        if (spaces % 2 == 0)
                        {
                            frontSpaces.Length.Should().Be(backSpaces.Length);
                        }
                        else if (alignment.HasFlag(Alignment.Right))
                        {
                            frontSpaces.Length.Should().BeGreaterThan(backSpaces.Length);
                        }
                        else
                        {
                            frontSpaces.Length.Should().BeLessThan(backSpaces.Length);
                        }
                    }
                    else
                    {
                        frontSpaces.Length.Should().Be(testWidth);
                    }
                }
            }
        }
    }
}