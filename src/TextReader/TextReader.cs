using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jay.Text.TextReader
{
    public delegate bool TextPredicate(text text);

    public ref struct TextReader
    {
        public readonly text Text;
        public int Index;
        public int Length => Text.Length;

        public text Remaining => Text.Slice(Index);

        public TextReader(text text)
        {
            this.Text = text;
            this.Index = 0;
        }

        public void SkipWhiteSpace()
        {
            while (Index < Length && char.IsWhiteSpace(Text[Index]))
            {
                Index++;
            }
        }

        public text TakeDigits()
        {
            int start = Index;
            while (Index < Length && char.IsDigit(Text[Index]))
            {
                Index++;
            }
            return Text.Slice(start, Index - start);
        }

        public text TakeUntil(TextPredicate examineRemaining)
        {
            int start = Index;
            while (Index < Length)
            {
                if (examineRemaining(Text.Slice(Index)))
                {
                    break;
                }
            }
            return Text.Slice(start, Index - start);
        }

        public text TakeWhile(TextPredicate examineRemaining)
        {
            int start = Index;
            while (Index < Length)
            {
                if (!examineRemaining(Text.Slice(Index)))
                {
                    break;
                }
            }
            return Text.Slice(start, Index - start);
        }

    }
}
