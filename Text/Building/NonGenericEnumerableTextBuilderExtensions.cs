using System.Collections;

namespace Jay.Text;

public static class NonGenericEnumerableTextBuilderExtensions
{
    public static TextBuilder AppendDelimit(
        this TextBuilder textBuilder,
        ReadOnlySpan<char> delimiter,
        IEnumerable? enumerable,
        Action<TextBuilder, object?> writeItem)
    {
        if (enumerable is null) return textBuilder;
        if (enumerable is IList list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (i > 0)
                    textBuilder.Write(delimiter);
                writeItem(textBuilder, list[i]);
            }
        }
        else
        {
            IEnumerator? e = null;
            try
            {
                e = enumerable.GetEnumerator();
                if (e.MoveNext())
                {
                    writeItem(textBuilder, e.Current);
                }
                while (e.MoveNext())
                {
                    textBuilder.Write(delimiter);
                    writeItem(textBuilder, e.Current);
                }
            }
            finally
            {
                if (e is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        return textBuilder;
    }
}