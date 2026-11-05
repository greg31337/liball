namespace ttf;

/// <summary>Represents BASE minimum and maximum extents.</summary>
public class FontBaselineMinMax : IFontBaselineMinMax
{
    SfntBaseMinMax extents;
    internal FontBaselineMinMax(SfntBaseMinMax extents) { this.extents = extents; }
    /// <summary>Gets the default minimum extent.</summary><returns>The untagged coordinate, or null.</returns>
    public FontBaselineCoordinate? GetMinimum()
    {
        SfntBaseCoordinate? coordinate = extents.GetMinimum();
        return coordinate == null ? null : new FontBaselineCoordinate(0, coordinate);
    }
    /// <summary>Gets the default maximum extent.</summary><returns>The untagged coordinate, or null.</returns>
    public FontBaselineCoordinate? GetMaximum()
    {
        SfntBaseCoordinate? coordinate = extents.GetMaximum();
        return coordinate == null ? null : new FontBaselineCoordinate(0, coordinate);
    }
    /// <summary>Gets the feature-specific extent count.</summary><returns>The count.</returns>
    public int GetFeatureCount() { return extents.GetFeatureCount(); }
    /// <summary>Gets a feature-specific extent.</summary><param name="index">The index.</param><returns>The extent.</returns>
    public FontBaselineFeatureMinMax GetFeature(int index)
    {
        if (index < 0 || index >= extents.GetFeatureCount())
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
        return new FontBaselineFeatureMinMax(extents.GetFeature(index));
    }
    /// <summary>Finds a feature-specific extent.</summary><param name="featureTag">The feature tag.</param><returns>The extent, or null.</returns>
    public FontBaselineFeatureMinMax? FindFeature(int featureTag)
    {
        SfntBaseFeatureMinMax? feature = extents.FindFeature(featureTag);
        return feature == null ? null : new FontBaselineFeatureMinMax(feature);
    }
}
