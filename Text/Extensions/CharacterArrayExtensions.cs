#if NET6_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

namespace Jay.Text.Extensions;

public static class CharacterArrayExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(this char[] charArray, char ch)
    {
#if NET6_0_OR_GREATER
        return MemoryExtensions.Contains<char>(charArray, ch);
#else
        for (var i = 0; i < charArray.Length; i++)
        {
            if (charArray[i] == ch) return true;
        }

        return false;
#endif
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref char GetPinnableReference(this char[] charArray)
    {
#if NET6_0_OR_GREATER
        return ref MemoryMarshal.GetArrayDataReference<char>(charArray);
#else
        return ref charArray[0];
#endif
    }
}