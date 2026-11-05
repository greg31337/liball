namespace ttf;

/// <summary>Defines public GSUB and GPOS layout discovery.</summary>
public interface IFontLayoutInspection
{
    /// <summary>Gets the glyph-substitution layout table.</summary>
    /// <returns>The GSUB model, or null when absent.</returns>
    FontLayoutTable? GetSubstitution();

    /// <summary>Gets the glyph-positioning layout table.</summary>
    /// <returns>The GPOS model, or null when absent.</returns>
    FontLayoutTable? GetPositioning();
}
