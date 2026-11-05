namespace ttf;

/// <summary>Provides read-only GDEF, BASE, JSTF, and MATH inspection.</summary>
public class FontAdvancedLayoutInspection : IFontAdvancedLayoutInspection
{
    FontGlyphDefinition? glyphDefinition;
    FontBaselines? baselines;
    FontJustification? justification;
    FontMath? math;

    internal FontAdvancedLayoutInspection(FontFace face)
    {
        SfntGdefTable? gdef = face.GetGdefTable();
        SfntBaseTable? baseTable = face.GetBaseTable();
        SfntJstfTable? jstf = face.GetJstfTable();
        SfntMathTable? mathTable = face.GetMathTable();
        if (gdef == null && face.HasTable(FontTags.Gdef))
        {
            throw FontFormatException.Create(
                "Advanced layout inspection requires a supported GDEF table.");
        }
        if (baseTable == null && face.HasTable(FontTags.Base))
        {
            throw FontFormatException.Create(
                "Advanced layout inspection requires a supported BASE table.");
        }
        if (jstf == null && face.HasTable(FontTags.Jstf))
        {
            throw FontFormatException.Create(
                "Advanced layout inspection requires a supported JSTF table.");
        }
        if (mathTable == null && face.HasTable(FontTags.Math))
        {
            throw FontFormatException.Create(
                "Advanced layout inspection requires a supported MATH table.");
        }
        glyphDefinition = gdef == null ? null : new FontGlyphDefinition(gdef);
        baselines = baseTable == null ? null : new FontBaselines(baseTable);
        justification = jstf == null ? null : new FontJustification(jstf);
        math = mathTable == null ? null : new FontMath(mathTable);
    }

    /// <summary>Gets glyph-definition data.</summary><returns>The GDEF model, or null.</returns>
    public FontGlyphDefinition? GetGlyphDefinition() { return glyphDefinition; }
    /// <summary>Gets baseline data.</summary><returns>The BASE model, or null.</returns>
    public FontBaselines? GetBaselines() { return baselines; }
    /// <summary>Gets justification data.</summary><returns>The JSTF model, or null.</returns>
    public FontJustification? GetJustification() { return justification; }
    /// <summary>Gets mathematical typesetting data.</summary><returns>The MATH model, or null.</returns>
    public FontMath? GetMath() { return math; }
}
