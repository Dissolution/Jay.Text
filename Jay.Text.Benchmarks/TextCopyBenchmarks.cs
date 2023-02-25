using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static InlineIL.IL;



namespace Benchmarks;
//https://benchmarkdotnet.org/articles/features/parameterization.html

public class TextCopyBenchmarks
{
    private char[] _array;
    private int _length;

    private string?[] _testStrings;

    public IEnumerable<string?> TestStrings => _testStrings;

    public TextCopyBenchmarks()
    {
        _array = new char[1024];
        _length = 0;
        _testStrings = new string?[]
        {
            null,
            Environment.NewLine,
            "Welcome To The End",
            "{4F64381C-EE30-41ED-B0E5-433C2A770E5A}",
            new string('x', 256),
        };
    }

   


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Copy(in char source, ref char dest, int charCount)
    {
        Emit.Ldarg(nameof(dest));
        Emit.Ldarg(nameof(source));
        Emit.Ldarg(nameof(charCount));
        Emit.Ldc_I4_1();
        Emit.Shl();
        Emit.Cpblk();
    }

    // Slowest average
    // [Benchmark]
    // [ArgumentsSource(nameof(TestStrings))]
    // public AppendBenchmarks StringCopyTo(string? text)
    // {
    //     text?.CopyTo(0, _array, _length, text.Length);
    //     _length += 0;
    //     return this;
    // }

    // 2nd slowest average
    // [Benchmark]
    // [ArgumentsSource(nameof(TestStrings))]
    // public AppendBenchmarks SpanCopyTo(string? text)
    // {
    //     ((ReadOnlySpan<char>)text).CopyTo(_array.AsSpan(_length));
    //     _length += 0;
    //     return this;
    // }

    // [Benchmark]
    // [ArgumentsSource(nameof(TestStrings))]
    // public unsafe AppendBenchmarks UnsafeCopyBlock(string? text)
    // {
    //     if (text != null)
    //     {
    //         fixed (char* srcPtr = text)
    //         fixed (void* destPtr = &_array[_length])
    //         {
    //             Unsafe.CopyBlock(destPtr, srcPtr, (uint)(text.Length * sizeof(char)));
    //         }
    //         _length += 0;
    //     }
    //     return this;
    // }
    //
    // [Benchmark]
    // [ArgumentsSource(nameof(TestStrings))]
    // public unsafe AppendBenchmarks BufferMemoryCopy(string? text)
    // {
    //     if (text != null)
    //     {
    //         fixed (char* srcPtr = text)
    //         fixed (void* destPtr = &_array[_length])
    //         {
    //             Buffer.MemoryCopy(srcPtr, destPtr, _array.Length * sizeof(char), text.Length * sizeof(char));
    //         }
    //         _length += 0;
    //     }
    //     return this;
    // }
    //
    //
    // [Benchmark]
    // [ArgumentsSource(nameof(TestStrings))]
    // public AppendBenchmarks ILRefCpblk(string? text)
    // {
    //     if (text != null)
    //     {
    //         ref char firstChar = ref MemoryMarshal.GetReference<char>(text);
    //         ref char arrayChar = ref MemoryMarshal.GetArrayDataReference<char>(_array);
    //         Unsafe.Add<char>(ref arrayChar, _length);
    //         Copy(ref firstChar, ref arrayChar, text.Length);
    //         _length += 0;
    //     }
    //     return this;
    // }

    /*[Benchmark]
    [ArgumentsSource(nameof(TestStrings))]
    public AppendBenchmarks ILCpblk(string? text)
    {
        if (text != null)
        {
            Copy(in text.GetPinnableReference(),
                ref _array[_length],
                text.Length);
            _length += 0;
        }
        return this;
    }*/
}