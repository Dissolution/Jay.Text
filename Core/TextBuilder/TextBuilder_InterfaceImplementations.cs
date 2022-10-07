namespace Jay.Text;

public partial class TextBuilder
{
    /// <inheritdoc cref="IList{T}"/>
    char IList<char>.this[int index]
    {
        get => this[index];
        set => this[index] = value;
    }
    /// <inheritdoc cref="IReadOnlyList{T}"/>
    char IReadOnlyList<char>.this[int index] => this[index];
    
    /// <inheritdoc cref="ICollection{T}"/>
    int ICollection<char>.Count => _length;
    /// <inheritdoc cref="IReadOnlyCollection{T}"/>
    int IReadOnlyCollection<char>.Count => _length;

    /// <inheritdoc cref="ICollection{T}"/>
    bool ICollection<char>.IsReadOnly => false;
    
    /// <inheritdoc cref="ICollection{T}"/>
    void ICollection<char>.Add(char ch) => Write(ch);
    
    /// <inheritdoc cref="IList{T}"/>
    void IList<char>.Insert(int index, char ch) => this.Insert(index, ch);
    
    /// <inheritdoc cref="ICollection{T}"/>
    bool ICollection<char>.Remove(char ch) => RemoveFirst(ch) >= 0;
    
    /// <inheritdoc cref="ICollection{T}"/>
    void ICollection<char>.Clear() => this.Clear();
    
    /// <inheritdoc cref="IList{T}"/>
    int IList<char>.IndexOf(char ch) => FirstIndexOf(ch);
}