namespace ttf;

/// <summary>Defines one GDEF ligature caret.</summary>
public interface IFontGlyphCaret
{
    /// <summary>Gets the caret format.</summary><returns>The format.</returns>
    int GetFormat();
    /// <summary>Gets the design coordinate.</summary><returns>The coordinate.</returns>
    int GetCoordinate();
    /// <summary>Reports whether a contour point is used.</summary><returns>True for format 2.</returns>
    bool HasContourPointIndex();
    /// <summary>Gets the contour point.</summary><returns>The point index.</returns>
    int GetContourPointIndex();
    /// <summary>Reports whether an adjustment exists.</summary><returns>True when present.</returns>
    bool HasAdjustment();
    /// <summary>Reports whether the adjustment is device data.</summary><returns>True for device data.</returns>
    bool IsDeviceAdjustment();
    /// <summary>Reports whether the adjustment is a variation index.</summary><returns>True for a variation index.</returns>
    bool IsVariationAdjustment();
}
