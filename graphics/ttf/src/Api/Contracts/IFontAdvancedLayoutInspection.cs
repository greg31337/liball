namespace ttf;

/// <summary>Defines read-only GDEF, BASE, JSTF, and MATH inspection.</summary>
public interface IFontAdvancedLayoutInspection
{
    /// <summary>Gets glyph-definition data.</summary>
    /// <returns>The GDEF model, or null.</returns>
    FontGlyphDefinition? GetGlyphDefinition();

    /// <summary>Gets baseline data.</summary>
    /// <returns>The BASE model, or null.</returns>
    FontBaselines? GetBaselines();

    /// <summary>Gets justification data.</summary>
    /// <returns>The JSTF model, or null.</returns>
    FontJustification? GetJustification();

    /// <summary>Gets mathematical typesetting data.</summary>
    /// <returns>The MATH model, or null.</returns>
    FontMath? GetMath();
}
