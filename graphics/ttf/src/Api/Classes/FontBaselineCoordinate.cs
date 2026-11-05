namespace ttf;

/// <summary>Describes one tagged BASE coordinate and its provenance.</summary>
public class FontBaselineCoordinate : IFontBaselineCoordinate
{
    int baselineTag;
    SfntBaseCoordinate coordinate;

    internal FontBaselineCoordinate(int baselineTag, SfntBaseCoordinate coordinate)
    {
        this.baselineTag = baselineTag;
        this.coordinate = coordinate;
    }

    /// <summary>Gets the baseline tag.</summary>
    /// <returns>The big-endian baseline tag.</returns>
    public int GetBaselineTag() { return baselineTag; }

    /// <summary>Gets the BASE coordinate format.</summary>
    /// <returns>Format 1, 2, or 3.</returns>
    public int GetFormat() { return coordinate.GetFormat(); }

    /// <summary>Gets the coordinate in design units.</summary>
    /// <returns>The design-unit coordinate.</returns>
    public int GetCoordinate() { return coordinate.GetCoordinate(); }

    /// <summary>Reports whether a contour point defines the coordinate.</summary>
    /// <returns>True for format 2.</returns>
    public bool HasContourPointReference()
    {
        return coordinate.HasContourPointReference();
    }

    /// <summary>Gets the reference glyph identifier.</summary>
    /// <returns>The glyph identifier.</returns>
    /// <exception cref="System.InvalidOperationException">No contour reference exists.</exception>
    public int GetReferenceGlyphId()
    {
        EnsureContourPointReference();
        return coordinate.GetReferenceGlyphId();
    }

    /// <summary>Gets the reference contour point index.</summary>
    /// <returns>The point index.</returns>
    /// <exception cref="System.InvalidOperationException">No contour reference exists.</exception>
    public int GetContourPointIndex()
    {
        EnsureContourPointReference();
        return coordinate.GetContourPointIndex();
    }

    /// <summary>Reports whether a device or variation adjustment is present.</summary>
    /// <returns>True when format 3 has a nonzero adjustment offset.</returns>
    public bool HasAdjustment() { return coordinate.GetAdjustment() != null; }

    /// <summary>Reports whether the adjustment is a device table.</summary>
    /// <returns>True for device data.</returns>
    public bool IsDeviceAdjustment()
    {
        SfntDeviceOrVariationIndex? adjustment = coordinate.GetAdjustment();
        return adjustment != null && adjustment.IsDevice();
    }

    /// <summary>Reports whether the adjustment is a variation index.</summary>
    /// <returns>True for variation data.</returns>
    public bool IsVariationAdjustment()
    {
        SfntDeviceOrVariationIndex? adjustment = coordinate.GetAdjustment();
        return adjustment != null && adjustment.IsVariationIndex();
    }

    void EnsureContourPointReference()
    {
        if (!coordinate.HasContourPointReference())
        {
            throw new System.InvalidOperationException(
                "The BASE coordinate has no contour-point reference.");
        }
    }
}
