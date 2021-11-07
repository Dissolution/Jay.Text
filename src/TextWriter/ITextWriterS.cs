using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;

namespace Jay.Text
{
    public interface ITextWriter<TSelf>
        where TSelf : ITextWriter<TSelf>
    {
        int Length { get; }
        Encoding DefaultEncoding { get; set; }
        IFormatProvider? DefaultFormatProvider { get; set; }
        string NewLine { get; set; }

        void Write(bool value);
        void Write(char value);
        void Write(params char[] text);
        void Write(text text);
        void Write(string? text);

        void Write(byte value);
        void Write(sbyte value);
        void Write(short value);
        void Write(ushort value);
        void Write(int value);
        void Write(uint value);
        void Write(long value);
        void Write(ulong value);
        
        void Write(float value);
        void Write(double value);
        void Write(decimal value);

        void Write(TimeSpan value);
        void Write(DateTime value);
        void Write(DateTimeOffset value);
        void Write(Guid value);

        void Write(object? value);
        void Write<T>(T? value);

        void WriteFormat(byte value, string? format = null, IFormatProvider? provider = null);
        void WriteFormat(sbyte value, string? format = null, IFormatProvider? provider = null);
        void WriteFormat(short value, string? format = null, IFormatProvider? provider = null);
        void WriteFormat(ushort value, string? format = null, IFormatProvider? provider = null);
        void WriteFormat(int value, string? format = null, IFormatProvider? provider = null);
        void WriteFormat(uint value, string? format = null, IFormatProvider? provider = null);
        void WriteFormat(long value, string? format = null, IFormatProvider? provider = null);
        void WriteFormat(ulong value, string? format = null, IFormatProvider? provider = null);
        void WriteFormat(float value, string? format = null, IFormatProvider? provider = null);
        void WriteFormat(double value, string? format = null, IFormatProvider? provider = null);
        void WriteFormat(decimal value, string? format = null, IFormatProvider? provider = null);
        void WriteFormat(TimeSpan value, string? format = null, IFormatProvider? provider = null);
        void WriteFormat(DateTime value, string? format = null, IFormatProvider? provider = null);
        void WriteFormat(DateTimeOffset value, string? format = null, IFormatProvider? provider = null);
        void WriteFormat(Guid value, string? format = null, IFormatProvider? provider = null);

        void WriteFormat(string format, params object?[] args);
        void WriteFormat<T>(string format, params T?[] args);
        void WriteFormat<T>(string format, IEnumerable<T?> args);
        void WriteFormat<T1>(string format, T1? arg1);
        void WriteFormat<T1, T2>(string format, T1? arg1, T2? arg2);
        void WriteFormat<T1, T2, T3>(string format, T1? arg1, T2? arg2, T3? arg3);
        void WriteFormat<T1, T2, T3, T4>(string format, T1? arg1, T2? arg2, T3? arg3, T4? arg4);
        void WriteFormat<T1, T2, T3, T4, T5>(string format, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5);

        void WriteLine();
    }

    public interface ITextBuilder<TSelf> : ITextWriter<TSelf>, IDisposable,
                                           IList<char>, IReadOnlyList<char>,
                                           ICollection<char>, IReadOnlyCollection<char>,
                                           IEnumerable<char>
        where TSelf : ITextBuilder<TSelf>
    {
        new ref char this[int index] { get; }
        Span<char> this[Range range] { get; }

        TSelf Append(bool value);
        TSelf Append(char value);
        TSelf Append(params char[] text);
        TSelf Append(text text);
        TSelf Append(string? text);

        TSelf Append(byte value);
        TSelf Append(sbyte value);
        TSelf Append(short value);
        TSelf Append(ushort value);
        TSelf Append(int value);
        TSelf Append(uint value);
        TSelf Append(long value);
        TSelf Append(ulong value);

        TSelf Append(float value);
        TSelf Append(double value);
        TSelf Append(decimal value);

        TSelf Append(TimeSpan value);
        TSelf Append(DateTime value);
        TSelf Append(DateTimeOffset value);
        TSelf Append(Guid value);
        TSelf Append(nint value);
        TSelf Append(nuint value);

        TSelf Append(object? value);
        TSelf Append<T>(T? value);

        TSelf AppendFormat(byte value, string? format = null, IFormatProvider? provider = null);
        TSelf AppendFormat(sbyte value, string? format = null, IFormatProvider? provider = null);
        TSelf AppendFormat(short value, string? format = null, IFormatProvider? provider = null);
        TSelf AppendFormat(ushort value, string? format = null, IFormatProvider? provider = null);
        TSelf AppendFormat(int value, string? format = null, IFormatProvider? provider = null);
        TSelf AppendFormat(uint value, string? format = null, IFormatProvider? provider = null);
        TSelf AppendFormat(long value, string? format = null, IFormatProvider? provider = null);
        TSelf AppendFormat(ulong value, string? format = null, IFormatProvider? provider = null);
        TSelf AppendFormat(float value, string? format = null, IFormatProvider? provider = null);
        TSelf AppendFormat(double value, string? format = null, IFormatProvider? provider = null);
        TSelf AppendFormat(decimal value, string? format = null, IFormatProvider? provider = null);
        TSelf AppendFormat(TimeSpan value, string? format = null, IFormatProvider? provider = null);
        TSelf AppendFormat(DateTime value, string? format = null, IFormatProvider? provider = null);
        TSelf AppendFormat(DateTimeOffset value, string? format = null, IFormatProvider? provider = null);
        TSelf AppendFormat(Guid value, string? format = null, IFormatProvider? provider = null);

        TSelf AppendFormat(nint value, string? format = null, IFormatProvider? provider = null);
        TSelf AppendFormat(nuint value, string? format = null, IFormatProvider? provider = null);

        TSelf AppendFormat<T>(T? formattable, string? format = null, IFormatProvider? provider = null)
            where T : IFormattable;

        TSelf AppendFormat(FormattableString formatString);
        TSelf AppendFormat(RawString format, params object?[] args);
        TSelf AppendFormat<T>(RawString format, params T?[] args);
        TSelf AppendFormat<T>(string format, IEnumerable<T?> args);
        TSelf AppendFormat<T1>(string format, T1? arg1);
        TSelf AppendFormat<T1, T2>(string format, T1? arg1, T2? arg2);
        TSelf AppendFormat<T1, T2, T3>(string format, T1? arg1, T2? arg2, T3? arg3);
        TSelf AppendFormat<T1, T2, T3, T4>(string format, T1? arg1, T2? arg2, T3? arg3, T4? arg4);
        TSelf AppendFormat<T1, T2, T3, T4, T5>(string format, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5);
        
        TSelf AppendLine();

        TSelf AppendDelimit(char delimiter, params object?[] values);
        TSelf AppendDelimit<T>(char delimiter, params T?[] values);
        TSelf AppendDelimit<T>(char delimiter, IEnumerable<T?> values);
        TSelf AppendDelimit<T>(char delimiter, IEnumerable<T?> values, Func<T?, string?> toString);
        TSelf AppendDelimit<T>(char delimiter, IEnumerable<T?> values, Func<T?, int, string> toString);
        TSelf AppendDelimit<T>(char delimiter, IEnumerable<T?> values, Action<TSelf, T?> append);
        TSelf AppendDelimit<T>(char delimiter, IEnumerable<T?> values, Action<TSelf, T?, int> append);

        new TSelf Insert(int index, char character);
        TSelf Insert(int index, string? text);
        TSelf Insert(int index, text text);

        TSelf Trim();

        TSelf TrimStart();
        TSelf TrimStart(char trimChar);
        TSelf TrimStart(params char[] trimChars);
        TSelf TrimStart(string? trimString, StringComparison comparison = StringComparison.CurrentCulture);
        TSelf TrimStart(text trimText, StringComparison comparison = StringComparison.CurrentCulture);
        TSelf TrimStart(Func<char, bool> isTrimChar);

        TSelf TrimEnd();
        TSelf TrimEnd(char trimChar);
        TSelf TrimEnd(params char[] trimChars);
        TSelf TrimEnd(string? trimString, StringComparison comparison = StringComparison.CurrentCulture);
        TSelf TrimEnd(text trimText, StringComparison comparison = StringComparison.CurrentCulture);
        TSelf TrimEnd(Func<char, bool> isTrimChar);

        TSelf Terminate(char character);
        TSelf Terminate(string? text, StringComparison comparison = StringComparison.CurrentCulture);
        TSelf Terminate(text text, StringComparison comparison = StringComparison.CurrentCulture);

        new TSelf Clear();

        TSelf Transform(Func<char, char> charTransform);
        TSelf Transform(Func<char, int, char> charTransform);
        TSelf Transform(RefCharAction charTransform);

        TSelf Replace(char oldChar, char newChar);
        TSelf Replace(string oldText, string newText, StringComparison comparison = StringComparison.CurrentCulture);
        TSelf Replace(text oldText, text newText, StringComparison comparison = StringComparison.CurrentCulture);
    }

    
    public static class TextExtensions
    {
        public static void Transform(Span<char> text, Func<char, char> charTransform)
        {
            throw new NotImplementedException();
        }

        public static void Transform(Span<char> text, Func<char, int, char> charTransform)
        {
            throw new NotImplementedException();
        }

        public static void Transform(Span<char> text, RefCharAction charTransform)
        {
            throw new NotImplementedException();
        }
    }

    // public partial class TextBuilder : ITextBuilder<TextBuilder>
    // {
    //     private static readonly ArrayPool<char> _charArrayPool;
    //
    //     static TextBuilder()
    //     {
    //         _charArrayPool = ArrayPool<char>.Create();
    //     }
    //     
    //     public static string Build(WriteText<TextBuilder> buildText)
    //     {
    //         using (var builder = new TextBuilder())
    //         {
    //             buildText(builder);
    //             return builder.ToString();
    //         }
    //     }
    // }

//     public partial class TextBuilder : ITextBuilder<TextBuilder>
//     {
//         protected char[] _characters;
//         protected int _length;
//
// #if NET5_0
//         internal Span<char> Written => _characters.AsSpan(0, _length);
// #endif
//
//         public TextBuilder()
//         {
//             _characters = _charArrayPool.Rent(1024);
//         }
//
//         public override bool Equals(object? obj)
//         {
//             if (obj is string text)
//                 return TextHelper.Equals(text, Written);
//             if (obj is char character)
//                 return _length > 0 && _characters[0] == character;
//             if (obj is char[] charArray)
//                 return TextHelper.Equals(charArray, Written);
//             return false;
//         }
//
//         public override int GetHashCode()
//         {
//             var hash = new HashCode();
//             var written = Written;
//             for (var c = 0; c < written.Length; c++)
//             {
//                 hash.Add<char>(written[c]);
//             }
//             return hash.ToHashCode();
//         }
//
//         public override string ToString()
//         {
//             return new string(_characters, 0, _length);
//         }
//     }
}
