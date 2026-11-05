namespace ttf;

/// <summary>Represents one MATH glyph construction.</summary>
public class FontMathConstruction : IFontMathConstruction
{
    SfntMathGlyphConstruction construction;
    internal FontMathConstruction(SfntMathGlyphConstruction construction)
    {
        this.construction = construction;
    }
    /// <summary>Gets the variant count.</summary><returns>The count.</returns>
    public int GetVariantCount() { return construction.GetVariantCount(); }
    /// <summary>Gets a variant.</summary><param name="index">The index.</param><returns>The variant.</returns>
    public FontMathVariant GetVariant(int index)
    {
        return new FontMathVariant(construction.GetVariant(index));
    }
    /// <summary>Gets the glyph assembly.</summary><returns>The assembly, or null.</returns>
    public FontMathAssembly? GetAssembly()
    {
        SfntMathGlyphAssembly? assembly = construction.GetAssembly();
        return assembly == null ? null : new FontMathAssembly(assembly);
    }
}
