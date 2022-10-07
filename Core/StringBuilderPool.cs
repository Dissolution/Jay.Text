/*using System.Text;
using Jay.Collections.Pools;

namespace Jay.Text;

public static class StringBuilderPool
{
    private static readonly ObjectPool<StringBuilder> _pool;

    static StringBuilderPool()
    {
        _pool = new ObjectPool<StringBuilder>(() => new StringBuilder(1024),
            sb => sb.Clear());
    }

    public static string Build(Action<StringBuilder> buildText)
    {
        var sb = _pool.Rent();
        buildText(sb);
        string str = sb.ToString();
        _pool.Return(sb);
        return str;
    }
        
    public static string Build<TState>(TState state, Action<StringBuilder, TState> buildText)
    {
        var sb = _pool.Rent();
        buildText(sb, state);
        string str = sb.ToString();
        _pool.Return(sb);
        return str;
    }
}*/