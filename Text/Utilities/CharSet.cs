namespace Jay.Text.Utilities;

public readonly ref struct CharSet
{
    private const int BLOCK_BIT_COUNT = sizeof(ulong) * 8;

    private static (int Block, ulong Mask) BlockMask(char ch)
    {
        int index = ch / BLOCK_BIT_COUNT;
        var offset = ch % BLOCK_BIT_COUNT;
        Debug.Assert(offset >= 0 && offset < BLOCK_BIT_COUNT);
        var mask = 1UL << offset;
        return (index, mask);
    }

    private static char Char(int block, int maskBit)
    {
        char ch = (char)((block * BLOCK_BIT_COUNT) + maskBit);
        return ch;
    }


    public static explicit operator CharSet(string? str) => From(str);
    public static explicit operator CharSet(char[] chars) => From(chars);
    public static explicit operator CharSet(ReadOnlySpan<char> span) => From(span);


    public static CharSet From(string? str) => From(str.AsSpan());
    public static CharSet From(params char[]? chars) => From(chars.AsSpan());
    public static CharSet From(ReadOnlySpan<char> span)
    {
        // Allocate for ASCII
        Span<ulong> blocks = stackalloc ulong[2];
        
        // Process
        for (var i = 0; i < span.Length; i++)
        {
            char ch = span[i];
            (int index, ulong mask) = BlockMask(ch);
            if (index > blocks.Length)
            {
                Span<ulong> newBlocks = stackalloc ulong[index + 1];
                blocks.CopyTo(newBlocks);
                blocks = newBlocks;
            }
            // Add this char (regardless if set or not)
            blocks[index] |= mask;
        }

        // Now we have to allocate
        var blockArray = blocks.ToArray();
        return new CharSet(blockArray);
    }

    public static CharSet From(IEnumerable<char> chars)
    {
        // Allocate for ASCII
        Span<ulong> blocks = stackalloc ulong[2];
        
        // Process
        foreach (char ch in chars)
        {
            (int index, ulong mask) = BlockMask(ch);
            if (index > blocks.Length)
            {
                Span<ulong> newBlocks = stackalloc ulong[index + 1];
                blocks.CopyTo(newBlocks);
                blocks = newBlocks;
            }
            // Add this char (regardless if set or not)
            blocks[index] |= mask;
        }

        // Now we have to allocate
        var blockArray = blocks.ToArray();
        return new CharSet(blockArray);
    }


    private readonly ReadOnlySpan<ulong> _blocks;

    private int BlockCount => _blocks.Length;

    private CharSet(ReadOnlySpan<ulong> blocks)
    {
        _blocks = blocks;
    }

    public bool Contains(char ch)
    {
        (int index, ulong mask) = BlockMask(ch);
        if (index >= BlockCount) return false;
        bool contains = (_blocks[index] & mask) != 0;
        return contains;
    }

    public ref struct Enumerator // : IEnumerator<char>, IEnumerator
    {
        private readonly ReadOnlySpan<ulong> _blocks;
        private int _blockIndex;
        private int _maskIndex;

        public char Current
        {
            get
            {
                if ((uint)_maskIndex >= 64 || _blockIndex >= _blocks.Length)
                    throw new InvalidOperationException("Enumeration has not yet begun or it has completed");
                return Char(_blockIndex, _maskIndex);
            }
        }

        public Enumerator(ReadOnlySpan<ulong> blocks)
        {
            _blocks = blocks;
            _blockIndex = 0;
            _maskIndex = -1;
        }

        public bool MoveNext()
        {
            int b = _blockIndex;
            var blocks = _blocks;
            int m = _maskIndex;
            // Start scanning remaining blocks
            while (b < blocks.Length)
            {
                ulong blockMask = blocks[b];
                // next mask index
                m++;

                // If block is empty or mask is after end
                if (blockMask == 0UL || m == 64)
                {
                    // Next block, start at -1, as it will be incremented
                    b++;
                    m = -1;
                    continue;
                }

                // Check if the block contains this mask
                ulong charMask = 1UL << m;
                bool contains = (blockMask & charMask) != 0;
                if (contains)
                {
                    // It does, save progress + return
                    _blockIndex = b;
                    _maskIndex = m;
                    return true;
                }

                // Check next mask
            }

            // We hit the end
            return false;
        }

        public void Reset()
        {
            _blockIndex = 0;
            _maskIndex = -1;
        }
    }

    public Enumerator GetEnumerator() => new Enumerator(_blocks);

    public override string ToString()
    {
        // Do not have to dispose, I will not be expanding
        CharSpanBuilder buffer = stackalloc char[BlockCount * BLOCK_BIT_COUNT];
        for (int block = 0; block < BlockCount; block++)
        {
            ulong blockMask = _blocks[block];
            if (blockMask == 0UL) continue; // Skip empty blocks
            for (var m = 0; m < BLOCK_BIT_COUNT; m++)
            {
                ulong charMask = 1UL << m;
                bool contains = (blockMask & charMask) != 0;
                if (contains)
                {
                    buffer.Write(Char(block, m));
                }
            }
        }
        return buffer.ToString();
    }
}