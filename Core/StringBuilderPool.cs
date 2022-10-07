using System.Collections.Concurrent;
using System.Text;

namespace Jay.Text;

public static class StringBuilderPool
{
    private static readonly ConcurrentStack<StringBuilder> _stringBuilders;

    static StringBuilderPool()
    {
        _stringBuilders = new ConcurrentStack<StringBuilder>();
    }

    public static StringBuilder Rent()
    {
        if (_stringBuilders.TryPop(out var stringBuilder))
        {
            return stringBuilder;
        }
        return new StringBuilder(1024);
    }

    public static void Return(StringBuilder stringBuilder)
    {
        stringBuilder.Clear();
        _stringBuilders.Push(stringBuilder);
    }

    public static string ReturnGetString(StringBuilder stringBuilder)
    {
        string str = stringBuilder.ToString();
        stringBuilder.Clear();
        _stringBuilders.Push(stringBuilder);
        return str;
    }

    public static string Build(Action<StringBuilder> buildText)
    {
        var sb = Rent();
        buildText(sb);
        return ReturnGetString(sb);
    }

    public static string Build<TState>(TState state, Action<StringBuilder, TState> buildText)
    {
        var sb = Rent();
        buildText(sb, state);
        return ReturnGetString(sb);
    }
}