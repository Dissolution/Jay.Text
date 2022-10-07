namespace Jay.Text;

public partial class TextBuilder
{
    /// <summary>
    /// Replace all occurrences of <paramref name="oldChar"/> with <paramref name="newChar"/> in this <see cref="TextBuilder"/>
    /// </summary>
    public TextBuilder Replace(char oldChar, char newChar)
    {
        var writ = Written;
        ref char ch = ref Unsafe.NullRef<char>();
        for (var i = writ.Length - 1; i >= 0; i--)
        {
            ch = ref writ[i];
            if (ch == oldChar)
            {
                ch = newChar;
            }
        }
        return this;
    }

    public TextBuilder Replace(ReadOnlySpan<char> oldText, ReadOnlySpan<char> newText)
    {
        int oldTextLen = oldText.Length;
        if (oldTextLen == 0 || oldTextLen > Length) return this;
        int newTextLen = newText.Length;

        // Stores the area of written text we're scanning for replacements
        Span<char> scan = Written;
        int i;

        // What we do depends on the differences between the text sizes
        int gap = oldTextLen - newTextLen;

        // Same text size
        if (gap == 0)
        {
            ref readonly char newChar = ref newText.GetPinnableReference();
            // Scan until we find no further matches
            while ((i = MemoryExtensions.IndexOf(scan, oldText)) >= 0)
            {
                // Copy new onto old
                TextHelper.Unsafe.Copy(in newChar, ref scan[i], newTextLen);
                // Start our scan after this replacement
                scan = scan.Slice(i + oldTextLen);
            }
        }
        // NewText is smaller (shrinks length)
        else if (gap > 0)
        {
            ref readonly char newChar = ref newText.GetPinnableReference();
            // Scan until we find no further matches
            while ((i = MemoryExtensions.IndexOf(scan, oldText)) >= 0)
            {
                // Copy new onto old
                TextHelper.Unsafe.Copy(in newChar, ref scan[i], newTextLen);

                // Slide everything to the right over the gap
                TextHelper.Unsafe.Copy(scan.Slice(i + oldTextLen), scan.Slice(i + newTextLen));
                
                // Length is smaller
                _length -= gap;

                // Start our scan after this replacement
                scan = scan.Slice(i + newTextLen);
            }
        }
        // NewText is bigger (increases length)
        else // gap < 0
        {
            using (var tempBuilder = new TextBuilder(Length * 2))
            {
                // Scan until we find no further matches
                while ((i = MemoryExtensions.IndexOf(scan, oldText)) >= 0)
                {
                    // Do we have to write anything before this?
                    if (i > 0)
                    {
                        // Write before
                        tempBuilder.Write(scan[..i]);
                    }
                    // Write replacement
                    tempBuilder.Write(newText);
                    //_length -= gap; // gap is negative, length grows

                    // Update scan to right after oldText
                    scan = scan.Slice(i + oldTextLen);
                }

                // Did we have anything left to write?
                if (scan.Length > 0)
                {
                    tempBuilder.Write(scan);
                }

                // Swap our arrays + lengths! HACKHACKHACK
                (_charArray, tempBuilder._charArray) = (tempBuilder._charArray, _charArray);
                (_length, tempBuilder._length) = (tempBuilder._length, _length);
            } // Dispose the temp builder
            // Now we have the correct text
        }

        // Fluent
        return this;
    }

    public TextBuilder Replace(ReadOnlySpan<char> oldText, ReadOnlySpan<char> newText, StringComparison comparison)
    {
        int oldTextLen = oldText.Length;
        if (oldTextLen == 0 || oldTextLen > Length) return this;
        int newTextLen = newText.Length;

        // Stores the area of written text we're scanning for replacements
        Span<char> scan = Written;
        int i;

        // What we do depends on the differences between the text sizes
        int gap = oldTextLen - newTextLen;

        // Same text size
        if (gap == 0)
        {
            ref readonly char newChar = ref newText.GetPinnableReference();
            // Scan until we find no further matches
            while ((i = MemoryExtensions.IndexOf(scan, oldText, comparison)) >= 0)
            {
                // Copy new onto old
                TextHelper.Unsafe.Copy(in newChar, ref scan[i], newTextLen);
                // Start our scan after this replacement
                scan = scan.Slice(i + oldTextLen);
            }
        }
        // NewText is smaller (shrinks length)
        else if (gap > 0)
        {
            ref readonly char newChar = ref newText.GetPinnableReference();
            // Scan until we find no further matches
            while ((i = MemoryExtensions.IndexOf(scan, oldText, comparison)) >= 0)
            {
                // Copy new onto old
                TextHelper.Unsafe.Copy(in newChar, ref scan[i], newTextLen);

                // Slide everything to the right over the gap
                TextHelper.Unsafe.Copy(scan.Slice(i + oldTextLen), scan.Slice(i + newTextLen));
                // Length is smaller
                _length -= gap;

                // Start our scan after this replacement
                scan = scan.Slice(i + newTextLen);
            }
        }
        // NewText is bigger (increases length)
        else // gap < 0
        {
            using (var tempBuilder = new TextBuilder(Length * 2))
            {
                // Scan until we find no further matches
                while ((i = MemoryExtensions.IndexOf(scan, oldText, comparison)) >= 0)
                {
                    // Do we have to write anything before this?
                    if (i > 0)
                    {
                        // Write before
                        tempBuilder.Write(scan[..i]);
                    }
                    // Write replacement
                    tempBuilder.Write(newText);
                    //_length -= gap; // gap is negative, length grows

                    // Update scan to right after oldText
                    scan = scan.Slice(i + oldTextLen);
                }

                // Did we have anything left to write?
                if (scan.Length > 0)
                {
                    tempBuilder.Write(scan);
                }

                // Swap our arrays + lengths! HACKHACKHACK
                (_charArray, tempBuilder._charArray) = (tempBuilder._charArray, _charArray);
                (_length, tempBuilder._length) = (tempBuilder._length, _length);
            } // Dispose the temp builder
            // Now we have the correct text
        }

        // Fluent
        return this;
    }
}