using Jay.Text.Scratch;
using Jay.Text.Scratch.WriteExtensions;

namespace Jay.Text.Tests.Scratch.TextBuilderTests;

public class WriteTests
{
    [Fact]
    public void CanBufferWriteChar()
    {
        StackTextBuilder textBuilder = stackalloc char[64];
        textBuilder.Write('A');
        textBuilder.Length.Should().Be(1);
        textBuilder.Written.Length.Should().Be(1);
        textBuilder.Written[0].Should().Be('A');
        
        textBuilder.Write('B');
        textBuilder.Length.Should().Be(2);
        textBuilder.Written.Length.Should().Be(2);
        textBuilder.Written[0].Should().Be('A');
        textBuilder.Written[1].Should().Be('B');
    }
}