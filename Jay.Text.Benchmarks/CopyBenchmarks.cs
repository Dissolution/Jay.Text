using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

using static InlineIL.IL;

namespace Jay.Text.Benchmarks;

public class CopyBenchmarks
{
    public IEnumerable<object?[]> TestArgs()
    {
        yield return new object?[2] { ",", new char[64] };
        yield return new object?[2] { Environment.NewLine, new char[64] };
        yield return new object?[2] { Guid.NewGuid().ToString("N"), new char[64] };
    }

    [Benchmark]
    [ArgumentsSource(nameof(TestArgs))]
    public void CopyTo(ReadOnlySpan<char> source, Span<char> dest)
    {
        if (source.Length <= dest.Length)
        {
            source.CopyTo(dest);
        }
        else
        {
            throw new ArgumentException("Destination is not large enough to contain Source", nameof(dest));
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void BlockCopy<T>(in T source, ref T dest, int itemCount)
        where T : unmanaged
    {
        Emit.Ldarg(nameof(dest));
        Emit.Ldarg(nameof(source));
        Emit.Ldarg(nameof(itemCount));
        Emit.Sizeof<T>();
        Emit.Mul();
        Emit.Cpblk();
    }
    
    [Benchmark]
    [ArgumentsSource(nameof(TestArgs))]
    public void BlockCopy(ReadOnlySpan<char> source, Span<char> dest)
    {
        if (source.Length <= dest.Length)
        {
            BlockCopy(in source.GetPinnableReference(),
                ref dest.GetPinnableReference(),
                source.Length);
        }
        else
        {
            throw new ArgumentException("Destination is not large enough to contain Source", nameof(dest));
        }
    }
    
    [Benchmark]
    [ArgumentsSource(nameof(TestArgs))]
    public void BlockCopyLocal(ReadOnlySpan<char> source, Span<char> dest)
    {
        int sourceLen = source.Length;
        if (sourceLen <= dest.Length)
        {
            BlockCopy(in source.GetPinnableReference(),
                ref dest.GetPinnableReference(),
                sourceLen);
        }
        else
        {
            throw new ArgumentException("Destination is not large enough to contain Source", nameof(dest));
        }
    }
}