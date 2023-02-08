namespace Jay.Text;

// ReSharper disable once InconsistentNaming
public delegate void CBA(CodeBuilder builder);

public delegate void TextBuilderAction<in TBuilder>(TBuilder builder)
    where TBuilder : TextBuilder<TBuilder>;

public delegate void TextBuilderValueAction<in TBuilder, in TValue>(TBuilder builder, TValue value)
    where TBuilder : TextBuilder<TBuilder>;

public delegate void TextBuilderValueIndexAction<in TBuilder, in TValue>(TBuilder builder, TValue value, int index)
    where TBuilder : TextBuilder<TBuilder>;