namespace ttf;

/// <summary>Defines one MATH glyph construction.</summary>
public interface IFontMathConstruction
{
    /// <summary>Gets the variant count.</summary><returns>The count.</returns>
    int GetVariantCount();
    /// <summary>Gets a variant.</summary><param name="index">The index.</param><returns>The variant.</returns>
    FontMathVariant GetVariant(int index);
    /// <summary>Gets the glyph assembly.</summary><returns>The assembly, or null.</returns>
    FontMathAssembly? GetAssembly();
}
