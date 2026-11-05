namespace ttf;

/// <summary>Defines read-only MATH inspection.</summary>
public interface IFontMath
{
    /// <summary>Gets the major version.</summary><returns>The major version.</returns>
    int GetMajorVersion();
    /// <summary>Gets the minor version.</summary><returns>The minor version.</returns>
    int GetMinorVersion();
    /// <summary>Gets the script scale percentage.</summary><returns>The percentage.</returns>
    int GetScriptPercentScaleDown();
    /// <summary>Gets the script-script scale percentage.</summary><returns>The percentage.</returns>
    int GetScriptScriptPercentScaleDown();
    /// <summary>Gets the delimited sub-formula minimum height.</summary><returns>The design-unit height.</returns>
    int GetDelimitedSubFormulaMinimumHeight();
    /// <summary>Gets the display operator minimum height.</summary><returns>The design-unit height.</returns>
    int GetDisplayOperatorMinimumHeight();
    /// <summary>Gets the indexed math-value constant count.</summary><returns>The count.</returns>
    int GetConstantCount();
    /// <summary>Gets an indexed math-value constant.</summary><param name="index">The constant index in OpenType order.</param><returns>The value.</returns>
    FontMathValue GetConstant(int index);
    /// <summary>Gets radical degree bottom raise percentage.</summary><returns>The percentage.</returns>
    int GetRadicalDegreeBottomRaisePercent();
    /// <summary>Finds a glyph italics correction.</summary><param name="glyphId">The glyph ID.</param><returns>The value, or null.</returns>
    FontMathValue? FindItalicsCorrection(int glyphId);
    /// <summary>Finds a top accent attachment.</summary><param name="glyphId">The glyph ID.</param><returns>The value, or null.</returns>
    FontMathValue? FindTopAccentAttachment(int glyphId);
    /// <summary>Reports whether a glyph is an extended shape.</summary><param name="glyphId">The glyph ID.</param><returns>True when covered.</returns>
    bool IsExtendedShape(int glyphId);
    /// <summary>Gets the minimum connector overlap.</summary><returns>The design-unit overlap.</returns>
    int GetMinimumConnectorOverlap();
    /// <summary>Gets the vertical construction count.</summary><returns>The count.</returns>
    int GetVerticalConstructionCount();
    /// <summary>Gets a vertical construction glyph ID.</summary><param name="index">The index.</param><returns>The glyph ID.</returns>
    int GetVerticalConstructionGlyphId(int index);
    /// <summary>Gets a vertical construction.</summary><param name="index">The index.</param><returns>The construction.</returns>
    FontMathConstruction GetVerticalConstruction(int index);
    /// <summary>Finds a vertical construction.</summary><param name="glyphId">The glyph ID.</param><returns>The construction, or null.</returns>
    FontMathConstruction? FindVerticalConstruction(int glyphId);
    /// <summary>Gets the horizontal construction count.</summary><returns>The count.</returns>
    int GetHorizontalConstructionCount();
    /// <summary>Gets a horizontal construction glyph ID.</summary><param name="index">The index.</param><returns>The glyph ID.</returns>
    int GetHorizontalConstructionGlyphId(int index);
    /// <summary>Gets a horizontal construction.</summary><param name="index">The index.</param><returns>The construction.</returns>
    FontMathConstruction GetHorizontalConstruction(int index);
    /// <summary>Finds a horizontal construction.</summary><param name="glyphId">The glyph ID.</param><returns>The construction, or null.</returns>
    FontMathConstruction? FindHorizontalConstruction(int glyphId);
}
