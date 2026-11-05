namespace ttf;

/// <summary>Provides public GSUB and GPOS layout discovery.</summary>
public class FontLayoutInspection : IFontLayoutInspection
{
    FontLayoutTable? substitution;
    FontLayoutTable? positioning;

    internal FontLayoutInspection(FontFace face)
    {
        SfntGsubTable? gsub = face.GetGsubTable();
        SfntGposTable? gpos = face.GetGposTable();
        if (gsub == null && face.HasTable(FontTags.Gsub))
        {
            throw FontFormatException.Create(
                "Layout discovery requires a supported GSUB table.");
        }
        if (gpos == null && face.HasTable(FontTags.Gpos))
        {
            throw FontFormatException.Create(
                "Layout discovery requires a supported GPOS table.");
        }
        substitution = gsub == null ? null : new FontLayoutTable(
            gsub.GetLayoutTable(),
            gsub,
            null);
        positioning = gpos == null ? null : new FontLayoutTable(
            gpos.GetLayoutTable(),
            null,
            gpos);
    }

    /// <summary>Gets the glyph-substitution layout table.</summary>
    /// <returns>The GSUB model, or null when absent.</returns>
    public FontLayoutTable? GetSubstitution() { return substitution; }

    /// <summary>Gets the glyph-positioning layout table.</summary>
    /// <returns>The GPOS model, or null when absent.</returns>
    public FontLayoutTable? GetPositioning() { return positioning; }
}
