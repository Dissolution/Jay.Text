﻿namespace Jay.Text.Tests.TextBuilderTests;

public class InterpolatedTextBuilderTests
{
    [Fact]
    public void InterpolatedWrite()
    {
        using var text = new TextBuilder();
        text.Length.Should().Be(0);
        string abc = "ABC";
        text.Append($"{abc}");
        text.Length.Should().Be(3);
        text.ToString().Should().Be(abc);
        text.Clear();
        text.Length.Should().Be(0);
        text.Append($"{DateTime.Now:s}");
        text.Length.Should().Be(19);
    }
}