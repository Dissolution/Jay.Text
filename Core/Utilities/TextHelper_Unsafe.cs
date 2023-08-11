using static InlineIL.IL;
// ReSharper disable EntityNameCapturedOnly.Local

namespace Jay.Text.Utilities;

public static partial class TextHelper
{
    /// <summary>
    /// Unsafe / Unchecked Methods -- Nothing here has bounds checks!
    /// </summary>
    internal static class Unsafe
    {
#if NET48 || NETSTANDARD2_0 || NETSTANDARD2_1
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void CopyBlock(char* sourcePtr, ref char destPtr, int charCount)
        {
            Emit.Ldarg(nameof(destPtr));
            Emit.Ldarg(nameof(sourcePtr));
            Emit.Ldarg(nameof(charCount));
            Emit.Sizeof<char>();
            Emit.Mul();
            Emit.Cpblk();
        }
#endif
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CopyBlock(in char sourcePtr, ref char destPtr, int charCount)
        {
            Emit.Ldarg(nameof(destPtr));
            Emit.Ldarg(nameof(sourcePtr));
            Emit.Ldarg(nameof(charCount));
            Emit.Sizeof<char>();
            Emit.Mul();
            Emit.Cpblk();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CopyTo(ReadOnlySpan<char> source, Span<char> dest, int sourceLen)
        {
            CopyBlock(
                in source.GetPinnableReference(),
                ref dest.GetPinnableReference(),
                sourceLen);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CopyTo(string source, Span<char> dest, int sourceLen)
        {
#if !NETCOREAPP3_1_OR_GREATER
            unsafe
            {
                fixed (char* ptr = source)
                {
                    CopyBlock(
                        ptr,
                        ref dest.GetPinnableReference(),
                        sourceLen);
                }
            }
#else
            CopyBlock(
                in source.GetPinnableReference(),
                ref dest.GetPinnableReference(),
                sourceLen);
#endif
        }
    }
}