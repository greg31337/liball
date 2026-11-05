namespace ttf;

/// <summary>Represents one MATH glyph assembly.</summary>
public class FontMathAssembly : IFontMathAssembly
{
    SfntMathGlyphAssembly assembly;
    internal FontMathAssembly(SfntMathGlyphAssembly assembly)
    {
        this.assembly = assembly;
    }
    /// <summary>Gets the assembly italics correction.</summary><returns>The value.</returns>
    public FontMathValue GetItalicsCorrection()
    {
        return new FontMathValue(assembly.GetItalicsCorrection());
    }
    /// <summary>Gets the part count.</summary><returns>The count.</returns>
    public int GetPartCount() { return assembly.GetPartCount(); }
    /// <summary>Gets an assembly part.</summary><param name="index">The index.</param><returns>The part.</returns>
    public FontMathPart GetPart(int index)
    {
        return new FontMathPart(assembly.GetPart(index));
    }
}
