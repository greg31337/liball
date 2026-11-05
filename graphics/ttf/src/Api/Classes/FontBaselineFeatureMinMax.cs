namespace ttf;

/// <summary>Represents one BASE feature-specific extent.</summary>
public class FontBaselineFeatureMinMax : IFontBaselineFeatureMinMax
{
    SfntBaseFeatureMinMax extents;
    internal FontBaselineFeatureMinMax(SfntBaseFeatureMinMax extents)
    {
        this.extents = extents;
    }
    /// <summary>Gets the feature tag.</summary><returns>The big-endian tag.</returns>
    public int GetFeatureTag() { return extents.GetFeatureTag(); }
    /// <summary>Gets the minimum extent.</summary><returns>The coordinate, or null.</returns>
    public FontBaselineCoordinate? GetMinimum()
    {
        SfntBaseCoordinate? coordinate = extents.GetMinimum();
        return coordinate == null ? null : new FontBaselineCoordinate(0, coordinate);
    }
    /// <summary>Gets the maximum extent.</summary><returns>The coordinate, or null.</returns>
    public FontBaselineCoordinate? GetMaximum()
    {
        SfntBaseCoordinate? coordinate = extents.GetMaximum();
        return coordinate == null ? null : new FontBaselineCoordinate(0, coordinate);
    }
}
