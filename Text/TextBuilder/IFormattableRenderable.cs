namespace Jay.Text;

public interface IFormattableRenderable : IFormattable, IRenderable
{
    void Render(TextBuilder textBuilder, string? format = null, IFormatProvider? provider = null);
}