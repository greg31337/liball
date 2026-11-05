namespace ttf;

/// <summary>Defines access to BASE baselines and legacy metric tables.</summary>
public interface IFontMetricInspection
{
    /// <summary>Gets parsed BASE baseline data.</summary>
    /// <returns>The baseline data, or null when unavailable.</returns>
    FontBaselines? GetBaselines();

    /// <summary>Gets parsed horizontal device metrics.</summary>
    /// <returns>The hdmx data, or null when unavailable.</returns>
    FontHorizontalDeviceMetrics? GetHorizontalDeviceMetrics();

    /// <summary>Gets parsed vertical device metrics.</summary>
    /// <returns>The VDMX data, or null when unavailable.</returns>
    FontVerticalDeviceMetrics? GetVerticalDeviceMetrics();

    /// <summary>Gets parsed linear-threshold data.</summary>
    /// <returns>The LTSH data, or null when unavailable.</returns>
    FontLinearThresholds? GetLinearThresholds();

    /// <summary>Gets parsed grid-fitting and smoothing behavior.</summary>
    /// <returns>The gasp data, or null when unavailable.</returns>
    FontGridFittingBehavior? GetGridFittingBehavior();

    /// <summary>Gets parsed legacy kerning data.</summary>
    /// <returns>The kern data, or null when unavailable.</returns>
    FontLegacyKerning? GetLegacyKerning();
}
