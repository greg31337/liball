namespace ttf;

/// <summary>Defines BASE minimum and maximum extents.</summary>
public interface IFontBaselineMinMax
{
    /// <summary>Gets the default minimum extent.</summary><returns>The untagged coordinate, or null.</returns>
    FontBaselineCoordinate? GetMinimum();
    /// <summary>Gets the default maximum extent.</summary><returns>The untagged coordinate, or null.</returns>
    FontBaselineCoordinate? GetMaximum();
    /// <summary>Gets the feature-specific extent count.</summary><returns>The count.</returns>
    int GetFeatureCount();
    /// <summary>Gets a feature-specific extent.</summary><param name="index">The index.</param><returns>The extent.</returns>
    FontBaselineFeatureMinMax GetFeature(int index);
    /// <summary>Finds a feature-specific extent.</summary><param name="featureTag">The feature tag.</param><returns>The extent, or null.</returns>
    FontBaselineFeatureMinMax? FindFeature(int featureTag);
}
