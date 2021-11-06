using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jay.Text
{
    public static class TextHelper
    {
        public static bool Equals(string? x, string? y)
        {
            return string.Equals(x, y);
        }

        public static bool Equals(ReadOnlySpan<char> x, ReadOnlySpan<char> y)
        {
            return MemoryExtensions.SequenceEqual(x, y);
        }

        public static bool Equals(string x, ReadOnlySpan<char> y)
        {
            return MemoryExtensions.SequenceEqual(x, y);
        }

        public static bool Equals(ReadOnlySpan<char> x, string y)
        {
            return MemoryExtensions.SequenceEqual(x, y);
        }
    }
}
