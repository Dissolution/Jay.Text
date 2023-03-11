namespace Jay.Text.Scratch.EnumerableExtensions;

public static class EnumerableExtensions
{
    public static ref StackTextBuilder Enumerate<T>(
        this ref StackTextBuilder textBuilder,
        IEnumerable<T>? values,
        STBValueIndexAction<T>? perValueAction)
    {
        if (values is null || perValueAction is null) return ref textBuilder;
        using var e = values.GetEnumerator();
        int index = 0;
        if (!e.MoveNext()) return ref textBuilder;
        perValueAction?.Invoke(ref textBuilder, e.Current, index);
        while (e.MoveNext())
        {
            index++;
            perValueAction?.Invoke(ref textBuilder, e.Current, index);
        }
        return ref textBuilder;
    }
    
    public static ref StackTextBuilder Enumerate<T>(
        this ref StackTextBuilder textBuilder,
        IEnumerable<T>? values,
        STBValueAction<T>? perValueAction)
    {
        if (values is null || perValueAction is null) return ref textBuilder;
        using var e = values.GetEnumerator();
        if (!e.MoveNext()) return ref textBuilder;
        perValueAction?.Invoke(ref textBuilder, e.Current);
        while (e.MoveNext())
        {
            perValueAction?.Invoke(ref textBuilder, e.Current);
        }
        return ref textBuilder;
    }
    
    public static ref StackTextBuilder Delimit<T>(
        this ref StackTextBuilder textBuilder,
        IEnumerable<T>? values,
        STBAction? delimitAction,
        STBValueIndexAction<T>? perValueAction)
    {
        if (values is null || (delimitAction is null && perValueAction is null)) return ref textBuilder;
        using var e = values.GetEnumerator();
        int index = 0;
        if (!e.MoveNext()) return ref textBuilder;
        perValueAction?.Invoke(ref textBuilder, e.Current, index);
        while (e.MoveNext())
        {
            delimitAction?.Invoke(ref textBuilder);
            index++;
            perValueAction?.Invoke(ref textBuilder, e.Current, index);
        }
        return ref textBuilder;
    }
    
    public static ref StackTextBuilder Delimit<T>(
        this ref StackTextBuilder textBuilder,
        IEnumerable<T>? values,
        STBAction? delimitAction,
        STBValueAction<T>? perValueAction)
    {
        if (values is null || (delimitAction is null && perValueAction is null)) return ref textBuilder;
        using var e = values.GetEnumerator();
        if (!e.MoveNext()) return ref textBuilder;
        perValueAction?.Invoke(ref textBuilder, e.Current);
        while (e.MoveNext())
        {
            delimitAction?.Invoke(ref textBuilder);
            perValueAction?.Invoke(ref textBuilder, e.Current);
        }
        return ref textBuilder;
    }
  
}