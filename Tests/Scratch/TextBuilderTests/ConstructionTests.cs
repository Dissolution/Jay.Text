using Jay.Text.Scratch;
using Jay.Text.Utilities;

using Jay.Text.Scratch.WriteExtensions;

namespace Jay.Text.Tests.Scratch.TextBuilderTests;

public class ConstructionTests
{
    [Fact]
    public void CanConstructDefault()
    {
        StackTextBuilder tb = default;
        tb.Length.Should().Be(0);
        tb.Written.Length.Should().Be(0);
        tb.Available.Length.Should().Be(0);
        tb.Capacity.Should().Be(0);
        tb.Dispose();
    }

    [Fact]
    public void CanInteractWithDefault()
    {
        StackTextBuilder tb = default;
        tb.Write('a');
        tb.Length.Should().Be(1);
        tb.Written.Length.Should().Be(1);
        tb.Available.Length.Should().BeGreaterThanOrEqualTo(1);
        tb.Capacity.Should().BeGreaterThanOrEqualTo(1);
        tb.Dispose();
    }
    
    [Fact]
    public void CanConstructNew()
    {
        StackTextBuilder tb = new StackTextBuilder();
        tb.Length.Should().Be(0);
        tb.Written.Length.Should().Be(0);
        tb.Capacity.Should().BeGreaterThanOrEqualTo(BuilderHelper.MinimumCapacity);
        tb.Available.Length.Should().Be(tb.Capacity);
        tb.Dispose();
    }
    
    [Fact]
    public void CanInteractWithNew()
    {
        StackTextBuilder tb = new StackTextBuilder();
        tb.Write('a');
        tb.Length.Should().Be(1);
        tb.Written.Length.Should().Be(1);
        tb.Available.Length.Should().BeGreaterThanOrEqualTo(1);
        tb.Capacity.Should().BeGreaterThanOrEqualTo(1);
        tb.Dispose();
    }
    
    [Fact]
    public void CanConstructBuffer()
    {
        StackTextBuilder tb = stackalloc char[40];
        tb.Length.Should().Be(0);
        tb.Written.Length.Should().Be(0);
        tb.Capacity.Should().Be(40);
        tb.Available.Length.Should().Be(tb.Capacity);
    }
}