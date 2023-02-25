using System.Runtime.CompilerServices;
using Jay.Text.Compat;

namespace Jay.Text;

internal static class BuilderHelper
{
    public const int MinimumCapacity = 1024;
    public const int MaximumCapacity = 0x3FFFFFDF; // == string.MaxLength < Array.MaxLength

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetStartingCapacity(int literalLength, int formattedCount)
    {
        return (literalLength + (formattedCount * 16)).Clamp(MinimumCapacity, MaximumCapacity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetCapacityToAdd(int currentCapacity, int addingCharCount)
    {
        return ((currentCapacity + addingCharCount) * 2).Clamp(MinimumCapacity, MaximumCapacity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetCapacityMin(int currentCapacity, int minCapacity)
    {
        return (Math.Max(currentCapacity, minCapacity) * 2).Clamp(MinimumCapacity, MaximumCapacity);
    }
}