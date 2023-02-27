using System.Diagnostics.CodeAnalysis;

namespace Jay.Text.Utilities;

public static class Validate
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Index(int available, int index,
        [CallerArgumentExpression(nameof(index))] string? indexName = null)
    {
        if ((uint)index < available) return;
        throw new ArgumentOutOfRangeException(indexName, index,
            $"{indexName} {index} must be between 0 and {available - 1}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Index(int available, Index index,
        [CallerArgumentExpression(nameof(index))] string? indexName = null)
    {
        int offset = index.GetOffset(available);
        if ((uint)offset < available) return;
        throw new ArgumentOutOfRangeException(indexName, index,
            $"{indexName} {index} must be between 0 and {available - 1}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Range(int available, Range range,
        [CallerArgumentExpression(nameof(range))] string? rangeName = null)
    {
        (int offset, int length) = range.GetOffsetAndLength(available);
        if ((uint)offset + (uint)length <= (uint)available) return;
        throw new ArgumentOutOfRangeException(rangeName, range,
            $"{rangeName} {range} must be between 0 and {available - 1}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Range(int available, int start, int length,
        [CallerArgumentExpression(nameof(start))] string? startName = null,
        [CallerArgumentExpression(nameof(length))] string? lengthName = null)
    {
        if ((uint)start > available)
            throw new ArgumentOutOfRangeException(startName, start, $"{startName} {start} must be between 0 and {available - 1}");
        if (start + (uint)length > available)
            throw new ArgumentOutOfRangeException(lengthName, length, $"{lengthName} {length} must be between 0 and {available - start}");
    }


    public static void Insert(int available, int index,
        [CallerArgumentExpression(nameof(index))] string? indexName = null)
    {
        if ((uint)index <= available) return;
        throw new ArgumentOutOfRangeException(indexName, index,
            $"Insert {indexName} {index} must be between 0 and {available}");
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo(int available, Array? array, int arrayIndex = 0)
    {
        if (array is null)
            throw new ArgumentNullException(nameof(array));
        if (array.Rank != 1)
            throw new ArgumentException("Array must have a rank of 1", nameof(array));
        if (array.GetLowerBound(0) != 0)
            throw new ArgumentException("Array must have a lower bound of 0", nameof(array));
        if ((uint)arrayIndex > array.Length)
            throw new IndexOutOfRangeException($"Array Index '{arrayIndex}' must be between 0 and {array.Length - 1}");
        if (array.Length - arrayIndex < available)
            throw new ArgumentException($"Array must have a capacity of at least {arrayIndex + available}", nameof(array));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo<T>(int available, T[]? array, int arrayIndex = 0)
    {
        if (array is null)
            throw new ArgumentNullException(nameof(array));
        if ((uint)arrayIndex > array.Length)
            throw new IndexOutOfRangeException($"Array Index '{arrayIndex}' must be between 0 and {array.Length - 1}");
        if (array.Length - arrayIndex < available)
            throw new ArgumentException($"Array must have at a capacity of at least {arrayIndex + available}", nameof(array));
    }


    #region Replacement
    /// <summary>
    /// Replaces the given <see cref="string"/> with <see cref="string.Empty"/> if it is <see langword="null"/>.
    /// </summary>
    /// <param name="value"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReplaceIfNull([AllowNull, NotNull] ref string? value)
    {
        value ??= string.Empty;
    }

    /// <summary>
    /// Replaces the given <paramref name="value"/> if it is <see langword="null"/>
    /// </summary>
    /// <exception cref="ArgumentNullException">If <paramref name="replacementIfNull"/> is <see langword="null"/></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReplaceIfNull<T>([AllowNull, NotNull] ref T? value, [DisallowNull] T replacementIfNull)
    {
        if (value is null)
        {
            value = replacementIfNull ?? throw new ArgumentNullException(nameof(replacementIfNull));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReplaceIfNull<T>([AllowNull, NotNull] ref T? value, Func<T> replacementValueFactory)
    {
        if (value is null)
        {
            value = replacementValueFactory() ??
                    throw new ArgumentException("The replacement value must not be null", nameof(replacementValueFactory));
        }
    }
    #endregion


}