using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Jay.Text
{
    public readonly ref struct text
    {
        public static implicit operator text(in char ch) => new text(in ch);
        public static implicit operator text(ReadOnlySpan<char> text) => new text(text);
        public static implicit operator text(string? text) => new text(text);
        public static implicit operator text(char[]? characters) => new text(characters);

        public static implicit operator ReadOnlySpan<char>(text text) => text.ToSpan();
        public static implicit operator string(text text) => text.ToString();

        private readonly unsafe char* _firstChar;
        private readonly int _length;

        public text(in char ch)
        {
            unsafe
            {
                _firstChar = Unsafe.AsPointer(in ch);
                _length = 1;
            }
        }
        public text(ReadOnlySpan<char> text)
        {
            unsafe
            {
                _firstChar = Unsafe.AsPointer(in text.GetPinnableReference());
                _length = text.Length;
            }
        }

        public text(string? text)
        {
            unsafe
            {
                if (text is null)
                {
                    _firstChar = Unsafe.AsPointer('\0');
                    _length = 0;
                }
                else
                {
                    _firstChar = Unsafe.AsPointer(in text.GetPinnableReference());
                    _length = text.Length;
                }
            }
        }

        public text(params char[]? characters)
        {
            unsafe
            {
                if (characters is null)
                {
                    _firstChar = Unsafe.AsPointer('\0');
                    _length = 0;
                }
                else
                {
                    _firstChar = Unsafe.AsPointer(in MemoryMarshal.GetArrayDataReference(characters));
                    _length = characters.Length;
                }
            }
        }

        public bool Equals(text text)
        {
            return TextHelper.Equals()
        }

        public bool Equals(char ch)
        {
            unsafe
            {
                return _length == 1 && *_firstChar == ch;
            }
        }

        public bool Equals(ReadOnlySpan<char> text)
        {
            return ToSpan().SequenceEqual(text);
        }

        public bool Equals(ReadOnlySpan<char> text, StringComparison comparison)
        {
            return ToSpan().Equals(text, comparison);
        }

        public bool Equals(string? str)
        {
            return ToSpan().SequenceEqual(str);
        }

        public bool Equals(string? str, StringComparison comparison)
        {
            return ToSpan().Equals(str, comparison);
        }

        public bool Equals(params char[]? chars)
        {
            return ToSpan().SequenceEqual(chars);
        }

        public bool Equals(char[]? chars, StringComparison comparison)
        {
            return ToSpan().Equals(chars, comparison);
        }

        public override bool Equals(object? obj)
        {
            if (obj is char ch)
                return Equals(ch);
            if (obj is string str)
                return Equals(str);
            if (obj is char[] chars)
                return Equals(chars);
            return false;
        }

        public override int GetHashCode()
        {
            var hasher = new HashCode();
            hasher.AddBytes(MemoryMarshal.Cast<char, byte>(ToSpan()));
            return hasher.ToHashCode();
        }

        public ReadOnlySpan<char> ToSpan()
        {
            unsafe
            {
                return new ReadOnlySpan<char>(_firstChar, _length);
            }
        }
        
        public override string ToString()
        {
            unsafe
            {
                return new string(_firstChar, 0, _length);
            }
        }
    }
}
