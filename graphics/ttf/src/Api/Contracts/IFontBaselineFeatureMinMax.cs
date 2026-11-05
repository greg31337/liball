namespace ttf;

/// <summary>Defines one BASE feature-specific extent.</summary>
public interface IFontBaselineFeatureMinMax
{
    /// <summary>Gets the feature tag.</summary><returns>The big-endian tag.</returns>
    int GetFeatureTag();
    /// <summary>Gets the minimum extent.</summary><returns>The coordinate, or null.</returns>
    FontBaselineCoordinate? GetMinimum();
    /// <summary>Gets the maximum extent.</summary><returns>The coordinate, or null.</returns>
    FontBaselineCoordinate? GetMaximum();
}
