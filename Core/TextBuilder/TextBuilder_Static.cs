namespace Jay.Text;

public partial class TextBuilder
{
    /// <summary>
    /// The minimum length of a borrowed TextBuilder instance
    /// </summary>
    internal const int MinimumCapacity = 1024;

    internal static readonly string NewLineString = Environment.NewLine;
    
    public static string Build(Action<TextBuilder> buildText)
    {
        using (var builder = new TextBuilder())
        {
            buildText(builder);
            return builder.ToString();
        }
    }

    public static string Build<TState>(TState? state, Action<TextBuilder, TState?> buildText)
    {
        using (var builder = new TextBuilder())
        {
            buildText(builder, state);
            return builder.ToString();
        }
    }

    /// <summary>
    /// Borrow an empty <see cref="TextBuilder"/> instance that will be returned when it is Disposed
    /// </summary>
    public static TextBuilder Borrow() => new TextBuilder();

    /// <summary>
    /// Borrow an empty <see cref="TextBuilder"/> instance that will be returned when it is Disposed
    /// </summary>
    /// <param name="minCapacity">The minimum capacity for the borrowed <see cref="TextBuilder"/> instance.</param>
    public static TextBuilder Borrow(int minCapacity) => new TextBuilder(minCapacity);
}