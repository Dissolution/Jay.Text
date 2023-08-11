namespace Jay.Text.Building;

internal static class BuilderHelper
{
    /// <summary>
    /// The minimum possible capacity of any builder<br/>
    /// 1024 (0x400)
    /// </summary>
    /// <remarks>
    /// We want to have a fairly large minimum capacity to minimize resizes
    /// </remarks>
    public const int MinimumCapacity = 1024;
    
    /// <summary>
    /// The maximum possible capacity of any builder<br/>
    /// 1_073_741_791 (0x3FFFFFDF)
    /// </summary>
    /// <remarks>
    /// This is equal to <c>string.MaxLength</c> (which is lower than <c>Array.MaxLength</c>)<br/>
    /// and thus the maximum possible <see cref="string"/> length that a builder could return
    /// </remarks>
    public const int MaximumCapacity = 0x3FFFFFDF;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetInterpolatedStartCapacity(int literalLength, int formattedCount)
    {
        return (literalLength + (formattedCount * 16)).Clamp(MinimumCapacity, MaximumCapacity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetGrowByCapacity(int currentCapacity, int addingCharCount)
    {
        return ((currentCapacity + addingCharCount) * 2).Clamp(MinimumCapacity, MaximumCapacity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetGrowToCapacity(int currentCapacity, int minCapacity)
    {
        return (Math.Max(currentCapacity, minCapacity) * 2).Clamp(MinimumCapacity, MaximumCapacity);
    }
}