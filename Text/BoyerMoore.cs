/*using System.Runtime.CompilerServices;

namespace Jay.Text;

public static class BoyerMoore
{
    /// <summary>
    /// Represents a pre-calculated Needle for the BoyerMoore search algorithm.
    /// </summary>
    public sealed class Needle
    {
        private const int ALPHABET_SIZE = char.MaxValue + 1;

        internal readonly int _length;
        internal readonly char[] _needle;
        internal readonly int[] _charTable;
        internal readonly int[] _offsetTable;

        public int Length => _length;

        public Needle(string needle)
            : this(needle.ToCharArray()) { }

        public Needle(char[] needle)
        {
            _needle = needle ?? throw new ArgumentNullException(nameof(needle));
            _length = needle.Length;

            // Character Table
            _charTable = new int[ALPHABET_SIZE];
            for (var i = 0; i < ALPHABET_SIZE; ++i)
            {
                _charTable[i] = _length;
            }
            for (var i = 0; i < _length - 1; ++i)
            {
                _charTable[needle[i]] = _length - 1 - i;
            }

            // Offset Table
            _offsetTable = new int[_length];
            int lastPrefixPosition = _length;
            for (var i = _length; i > 0; --i)
            {
                if (IsPrefix(needle, i))
                    lastPrefixPosition = i;
                _offsetTable[_length - i] = lastPrefixPosition - i + _length;
            }
            for (var i = 0; i < _length - 1; ++i)
            {
                var suffixLength = SuffixLength(needle, i);
                _offsetTable[suffixLength] = _length - 1 - i + suffixLength;
            }
        }

        /// <summary>
        /// Is needle[p:end] a prefix of needle?
        /// </summary>
        /// <param name="needle"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsPrefix(char[] needle, int p)
        {
            for (int i = p, j = 0; i < needle.Length; ++i, ++j)
            {
                if (needle[i] != needle[j])
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Returns the maximum length of the substring ends at p and is a suffix.
        /// </summary>
        /// <param name="needle"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int SuffixLength(char[] needle, int p)
        {
            int len = 0;
            for (int i = p, j = needle.Length - 1;
                 i >= 0 && needle[i] == needle[j];
                 --i, --j)
            {
                len += 1;
            }
            return len;
        }
    }
        
    public static IEnumerable<int> Indexes(string haystack, string needle) => Indexes(haystack, new Needle(needle));

    public static IEnumerable<int> Indexes(string haystack, Needle needle)
    {
        if (haystack.Length == 0)
            yield break;
        var length = needle.Length;
        if (length == 0)
        {
            for (var i = 0; i < length; i++)
                yield return i;
            yield break;
        }
        var need = needle._needle;
        var charTable = needle._charTable;
        var offsetTable = needle._offsetTable;
        for (int i = length - 1, j; i < haystack.Length;)
        {
            for (j = length - 1; need[j] == haystack[i]; --i, --j)
            {
                if (j == 0)
                {
                    yield return i;
                    break;
                }
            }

            var offset = offsetTable[length - 1 - j];
            var chr = charTable[haystack[i]];

            // i += needle.length - j; // For naive method
            i += ((offset >= chr) ? offset : chr);
        }
    }
    public static IEnumerable<int> Indexes(char[] haystack, string needle) => Indexes(haystack, new Needle(needle));
    public static IEnumerable<int> Indexes(char[] haystack, Needle needle)
    {
        if (haystack.Length == 0)
            yield break;
        var length = needle.Length;
        if (length == 0)
        {
            for (var i = 0; i < length; i++)
                yield return i;
            yield break;
        }
        var need = needle._needle;
        var charTable = needle._charTable;
        var offsetTable = needle._offsetTable;
        for (int i = length - 1, j; i < haystack.Length;)
        {
            for (j = length - 1; need[j] == haystack[i]; --i, --j)
            {
                if (j == 0)
                {
                    yield return i;
                    break;
                }
            }

            var offset = offsetTable[length - 1 - j];
            var chr = charTable[haystack[i]];

            // i += needle.length - j; // For naive method
            i += ((offset >= chr) ? offset : chr);
        }
    }

    public static IReadOnlyList<int> Indexes(ReadOnlySpan<char> haystack, string needle) =>
        Indexes(haystack, new Needle(needle));
    public static IReadOnlyList<int> Indexes(ReadOnlySpan<char> haystack, Needle needle)
    {
        if (haystack.Length == 0)
            return Array.Empty<int>();
        var length = needle.Length;
        if (length == 0)
        {
            var indexes = new int[length];
            for (var i = 0; i < length; i++)
                indexes[i] = i;
            return indexes;
        }
        var need = needle._needle;
        var charTable = needle._charTable;
        var offsetTable = needle._offsetTable;
        var list = new List<int>();
        for (int i = length - 1, j; i < haystack.Length;)
        {
            for (j = length - 1; need[j] == haystack[i]; --i, --j)
            {
                if (j == 0)
                {
                    list.Add(i);
                    break;
                }
            }

            var offset = offsetTable[length - 1 - j];
            var chr = charTable[haystack[i]];

            // i += needle.length - j; // For naive method
            i += ((offset >= chr) ? offset : chr);
        }
        return list;
    }

}*/