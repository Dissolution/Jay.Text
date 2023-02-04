using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;

using static InlineIL.IL;
// ReSharper disable EntityNameCapturedOnly.Local

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
        // We want to test a variety of loads
        _testStrings = new string?[]
        {
            // Null always needs to be accounted for
            (string?)null,
            // An empty string
            "",
            // A decently sized semi-random string
            Guid.NewGuid().ToString("N"),
            // Two long strings that barely differ
            $"{new string('X', 255)}Y",
            new string('X', 256),
        };
    }

    [Benchmark(Baseline = true)]
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
    public bool InstanceEquals()
    {
        if (A is null) return B is null;
        return A.Equals(B);
    }

    [Benchmark]
    public bool InstanceSequenceEqual()
    {
        if (A is null) return B is null;
        if (B is null) return false;
        return A.SequenceEqual(B);
    }
    
    [Benchmark]
    public bool MemoryExtensionsSequenceEqual()
    {
        return MemoryExtensions.SequenceEqual<char>(A.AsSpan(), B.AsSpan());
    }
    
    [Benchmark]
    public bool MemoryExtensionsEqualsOrdinal()
    {
        return MemoryExtensions.Equals(A.AsSpan(), B.AsSpan(), StringComparison.Ordinal);
    }

    [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
    private unsafe static extern int memcmp(byte* b1, byte* b2, nuint count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe static byte* AsBytePointer(ReadOnlySpan<char> text)
    {
        Emit.Ldarg(nameof(text));
        return ReturnPointer<byte>();
    }

    [Benchmark]
    public unsafe bool MemCmp()
    {
        ReadOnlySpan<char> a = A.AsSpan();
        ReadOnlySpan<char> b = B.AsSpan();
        if (a.Length != b.Length) return false;
        return memcmp(AsBytePointer(a), AsBytePointer(b), (nuint)(a.Length * 2)) == 0;
    }

    [Benchmark]
    public bool StructurallyEquals()
    {
        return StructuralComparisons.StructuralEqualityComparer.Equals(A, B);
    }
}