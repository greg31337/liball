namespace ttf;

/// <summary>Defines one tagged BASE coordinate and its provenance.</summary>
public interface IFontBaselineCoordinate
{
    /// <summary>Gets the baseline tag.</summary>
    /// <returns>The big-endian baseline tag.</returns>
    int GetBaselineTag();

    /// <summary>Gets the BASE coordinate format.</summary>
    /// <returns>Format 1, 2, or 3.</returns>
    int GetFormat();

    /// <summary>Gets the coordinate in design units.</summary>
    /// <returns>The design-unit coordinate.</returns>
    int GetCoordinate();

    /// <summary>Reports whether a contour point defines the coordinate.</summary>
    /// <returns>True for format 2.</returns>
    bool HasContourPointReference();

    /// <summary>Gets the reference glyph identifier.</summary>
    /// <returns>The glyph identifier.</returns>
    /// <exception cref="System.InvalidOperationException">No contour reference exists.</exception>
    int GetReferenceGlyphId();

    /// <summary>Gets the reference contour point index.</summary>
    /// <returns>The point index.</returns>
    /// <exception cref="System.InvalidOperationException">No contour reference exists.</exception>
    int GetContourPointIndex();

    /// <summary>Reports whether a device or variation adjustment is present.</summary>
    /// <returns>True when format 3 has a nonzero adjustment offset.</returns>
    bool HasAdjustment();

    /// <summary>Reports whether the adjustment is a device table.</summary>
    /// <returns>True for device data.</returns>
    bool IsDeviceAdjustment();

    /// <summary>Reports whether the adjustment is a variation index.</summary>
    /// <returns>True for variation data.</returns>
    bool IsVariationAdjustment();
}
