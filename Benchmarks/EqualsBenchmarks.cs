using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using InlineIL;

namespace Benchmarks;

[ShortRunJob]
public class EqualsBenchmarks
{
    private string?[] _testStrings;

    public IEnumerable<string?> TestStrings => _testStrings;

    [ParamsSource(nameof(TestStrings))]
    public string? A { get; set; }

    [ParamsSource(nameof(TestStrings))]
    public string? B { get; set; }

    public EqualsBenchmarks()
    {
        _testStrings = new string?[]
        {
            //null,
            Environment.NewLine,
            //"Welcome To The End",
            "{4F64381C-EE30-41ED-B0E5-433C2A770E5A}",
            new string('x', 256),
        };
    }

    [Benchmark]
    public bool StringEquals()
    {
        return string.Equals(A, B);
    }

    [Benchmark]
    public bool StringEqualsOperator()
    {
        return A == B;
    }

    [Benchmark]
    public bool MESequenceEqual()
    {
        return MemoryExtensions.SequenceEqual<char>(A, B);
    }

    [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
    private unsafe static extern int memcmp(byte* b1, byte* b2, nuint count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void* AsVoidPointer<T>(ReadOnlySpan<T> span)
    {
        IL.Emit.Ldarg(nameof(span));
        return IL.ReturnPointer();
    }

    [Benchmark]
    public unsafe bool MemCmp()
    {
        text a = A;
        text b = B;
        if (a.Length != b.Length) return false;
        return memcmp((byte*)AsVoidPointer(a), (byte*)AsVoidPointer(b), (nuint)a.Length * 2) == 0;
    }
}