namespace ttf;

/// <summary>Defines one MATH glyph assembly.</summary>
public interface IFontMathAssembly
{
    /// <summary>Gets the assembly italics correction.</summary><returns>The value.</returns>
    FontMathValue GetItalicsCorrection();
    /// <summary>Gets the part count.</summary><returns>The count.</returns>
    int GetPartCount();
    /// <summary>Gets an assembly part.</summary><param name="index">The index.</param><returns>The part.</returns>
    FontMathPart GetPart(int index);
}
