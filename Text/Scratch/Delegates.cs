// ReSharper disable InconsistentNaming
namespace Jay.Text.Scratch;

public delegate void STBAction(ref StackTextBuilder textBuilder);
public delegate void STBValueAction<in T>(ref StackTextBuilder textBuilder, T value);
public delegate void STBValueIndexAction<in T>(ref StackTextBuilder textBuilder, T value, int index);
public delegate void STBSpanAction<T>(ref StackTextBuilder textBuilder, ReadOnlySpan<T> span);
