using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jay.Text
{
    public delegate void WriteText<in TWriter>(TWriter textWriter)
        where TWriter : ITextWriter<TWriter>;

    public delegate void WriteStateText<in TWriter, in TState>(TWriter textWriter, TState state)
        where TWriter : ITextWriter<TWriter>;

    public delegate void WriteSpanText<in TWriter, T>(TWriter textWriter, ReadOnlySpan<T> span)
        where TWriter : ITextWriter<TWriter>;

    public delegate void WriteCharSpanText<in TWriter>(TWriter textWriter, ReadOnlySpan<char> text)
        where TWriter : ITextWriter<TWriter>;

    public delegate void RefCharAction(ref char character);
}
