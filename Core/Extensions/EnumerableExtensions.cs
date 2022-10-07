namespace Jay.Text.Extensions;

public static class EnumerableExtensions
{
    public delegate bool CanTransform<in TIn, TOut>(TIn input, [NotNullWhen(true)] out TOut output);

    public static IEnumerable<TOut> SelectWhere<TIn, TOut>(this IEnumerable<TIn> source, CanTransform<TIn, TOut> selectWhere)
    {
        foreach (var element in source)
        {
            if (selectWhere(element, out var newElement))
            {
                yield return newElement;
            }
        }
    }

    public static IEnumerable<T> SwallowEnumerate<T>(this IEnumerable<T> source)
    {
        using (var e = source.GetEnumerator())
        {
            while (true)
            {
                T current;
                try
                {
                    if (!e.MoveNext()) break;
                    current = e.Current;
                }
                catch (Exception ex)
                {
                    continue;
                }
                yield return current;
            }
        }
    }
}