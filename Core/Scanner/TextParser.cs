namespace Jay.Text.Scanner;

public ref struct TextParser
{
    private readonly ReadOnlySpan<char> _text;
    private int _index;

    public int Index => _index;

    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _text.Length;
    }

    public ref readonly char this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Validate.Index(_text.Length, index);
            return ref _text[index];
        }
    }

    public TextParser(ReadOnlySpan<char> text)
    {
        _text = text;
    }

    public ReadOnlySpan<char> Take(int count)
    {
        
    }
}