using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static InlineIL.IL;

namespace Jay.Text
{
    internal static unsafe class Unsafe
    {
        public static char* AsPointer(in char ch)
        {
            Emit.Ldarg(nameof(ch));
            return ReturnPointer<char>();
        }
    }
}
