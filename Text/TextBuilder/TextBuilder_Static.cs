namespace Jay.Text;

public partial class TextBuilder
{
    /// <summary>
    /// The minimum length of a borrowed TextBuilder instance
    /// </summary>
    internal const int MinimumCapacity = 1024;

    /// <summary>
    /// A locally stored copy of <see cref="Environment.NewLine"/>
    /// </summary>
    internal static readonly string NewLineString = Environment.NewLine;
    
    /// <summary>
    /// Builds a <see cref="string"/> using a temporary <see cref="TextBuilder"/> instance
    /// </summary>
    /// <param name="buildText">The action to perform on a <see cref="TextBuilder"/> instance.</param>
    /// <returns>The <see cref="string"/> built by the <see cref="TextBuilder"/> instance.</returns>
    public static string Build(Action<TextBuilder> buildText)
    {
        using (var builder = new TextBuilder())
        {
            buildText(builder);
            return builder.ToString();
        }
    }

    /// <summary>
    /// Builds a <see cref="string"/> using a temporary <see cref="TextBuilder"/> instance
    /// </summary>
    /// <typeparam name="TState">The <see cref="Type"/> of the <paramref name="state"/> parameter.</list></typeparam>
    /// <param name="buildText">The action to perform on a <see cref="TextBuilder"/> instance and a <typeparamref name="TState"/> instance.</param>
    /// <returns>The <see cref="string"/> built by the <see cref="TextBuilder"/> instance.</returns>
    public static string Build<TState>(TState state, Action<TextBuilder, TState> buildText)
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