namespace ttf;

/// <summary>Provides read-only MATH inspection.</summary>
public class FontMath : IFontMath
{
    SfntMathTable table;
    internal FontMath(SfntMathTable table) { this.table = table; }
    /// <summary>Gets the major version.</summary><returns>The major version.</returns>
    public int GetMajorVersion() { return table.GetMajorVersion(); }
    /// <summary>Gets the minor version.</summary><returns>The minor version.</returns>
    public int GetMinorVersion() { return table.GetMinorVersion(); }
    /// <summary>Gets the script scale percentage.</summary><returns>The percentage.</returns>
    public int GetScriptPercentScaleDown()
    {
        return table.GetConstants().GetScriptPercentScaleDown();
    }
    /// <summary>Gets the script-script scale percentage.</summary><returns>The percentage.</returns>
    public int GetScriptScriptPercentScaleDown()
    {
        return table.GetConstants().GetScriptScriptPercentScaleDown();
    }
    /// <summary>Gets the delimited sub-formula minimum height.</summary><returns>The design-unit height.</returns>
    public int GetDelimitedSubFormulaMinimumHeight()
    {
        return table.GetConstants().GetDelimitedSubFormulaMinimumHeight();
    }
    /// <summary>Gets the display operator minimum height.</summary><returns>The design-unit height.</returns>
    public int GetDisplayOperatorMinimumHeight()
    {
        return table.GetConstants().GetDisplayOperatorMinimumHeight();
    }
    /// <summary>Gets the indexed math-value constant count.</summary><returns>The count.</returns>
    public int GetConstantCount() { return table.GetConstants().GetValueCount(); }
    /// <summary>Gets an indexed math-value constant.</summary><param name="index">The constant index in OpenType order.</param><returns>The value.</returns>
    public FontMathValue GetConstant(int index)
    {
        if (index < 0 || index >= GetConstantCount())
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
        return new FontMathValue(
            table.GetConstants().GetValue((SfntMathConstant)index));
    }
    /// <summary>Gets radical degree bottom raise percentage.</summary><returns>The percentage.</returns>
    public int GetRadicalDegreeBottomRaisePercent()
    {
        return table.GetConstants().GetRadicalDegreeBottomRaisePercent();
    }
    /// <summary>Finds a glyph italics correction.</summary><param name="glyphId">The glyph ID.</param><returns>The value, or null.</returns>
    public FontMathValue? FindItalicsCorrection(int glyphId)
    {
        SfntMathGlyphValueTable? values =
            table.GetGlyphInfo().GetItalicsCorrections();
        SfntMathValue? value = values == null ? null : values.FindValue(glyphId);
        return value == null ? null : new FontMathValue(value);
    }
    /// <summary>Finds a top accent attachment.</summary><param name="glyphId">The glyph ID.</param><returns>The value, or null.</returns>
    public FontMathValue? FindTopAccentAttachment(int glyphId)
    {
        SfntMathGlyphValueTable? values =
            table.GetGlyphInfo().GetTopAccentAttachments();
        SfntMathValue? value = values == null ? null : values.FindValue(glyphId);
        return value == null ? null : new FontMathValue(value);
    }
    /// <summary>Reports whether a glyph is an extended shape.</summary><param name="glyphId">The glyph ID.</param><returns>True when covered.</returns>
    public bool IsExtendedShape(int glyphId)
    {
        return table.GetGlyphInfo().IsExtendedShape(glyphId);
    }
    /// <summary>Gets the minimum connector overlap.</summary><returns>The design-unit overlap.</returns>
    public int GetMinimumConnectorOverlap()
    {
        return table.GetVariants().GetMinimumConnectorOverlap();
    }
    /// <summary>Gets the vertical construction count.</summary><returns>The count.</returns>
    public int GetVerticalConstructionCount()
    {
        return table.GetVariants().GetVerticalConstructionCount();
    }
    /// <summary>Gets a vertical construction glyph ID.</summary><param name="index">The index.</param><returns>The glyph ID.</returns>
    public int GetVerticalConstructionGlyphId(int index)
    {
        return table.GetVariants().GetVerticalCoverage().GetGlyphId(index);
    }
    /// <summary>Gets a vertical construction.</summary><param name="index">The index.</param><returns>The construction.</returns>
    public FontMathConstruction GetVerticalConstruction(int index)
    {
        return new FontMathConstruction(
            table.GetVariants().GetVerticalConstruction(index));
    }
    /// <summary>Finds a vertical construction.</summary><param name="glyphId">The glyph ID.</param><returns>The construction, or null.</returns>
    public FontMathConstruction? FindVerticalConstruction(int glyphId)
    {
        SfntMathGlyphConstruction? construction =
            table.GetVariants().FindVerticalConstruction(glyphId);
        return construction == null ? null :
            new FontMathConstruction(construction);
    }
    /// <summary>Gets the horizontal construction count.</summary><returns>The count.</returns>
    public int GetHorizontalConstructionCount()
    {
        return table.GetVariants().GetHorizontalConstructionCount();
    }
    /// <summary>Gets a horizontal construction glyph ID.</summary><param name="index">The index.</param><returns>The glyph ID.</returns>
    public int GetHorizontalConstructionGlyphId(int index)
    {
        return table.GetVariants().GetHorizontalCoverage().GetGlyphId(index);
    }
    /// <summary>Gets a horizontal construction.</summary><param name="index">The index.</param><returns>The construction.</returns>
    public FontMathConstruction GetHorizontalConstruction(int index)
    {
        return new FontMathConstruction(
            table.GetVariants().GetHorizontalConstruction(index));
    }
    /// <summary>Finds a horizontal construction.</summary><param name="glyphId">The glyph ID.</param><returns>The construction, or null.</returns>
    public FontMathConstruction? FindHorizontalConstruction(int glyphId)
    {
        SfntMathGlyphConstruction? construction =
            table.GetVariants().FindHorizontalConstruction(glyphId);
        return construction == null ? null :
            new FontMathConstruction(construction);
    }
}
