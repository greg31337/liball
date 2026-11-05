namespace ttf;

/// <summary>Contains BASE baseline and legacy metric-table inspection data.</summary>
public class FontMetricInspection : IFontMetricInspection
{
    FontBaselines? baselines;
    FontHorizontalDeviceMetrics? horizontalDeviceMetrics;
    FontVerticalDeviceMetrics? verticalDeviceMetrics;
    FontLinearThresholds? linearThresholds;
    FontGridFittingBehavior? gridFittingBehavior;
    FontLegacyKerning? legacyKerning;

    internal FontMetricInspection(FontFace face)
    {
        SfntBaseTable? baseTable = face.GetBaseTable();
        SfntHdmxTable? hdmx = face.GetHdmxTable();
        SfntVdmxTable? vdmx = face.GetVdmxTable();
        SfntLtshTable? ltsh = face.GetLtshTable();
        SfntGaspTable? gasp = face.GetGaspTable();
        SfntKernTable? kern = face.GetKernTable();
        SfntMaxpTable? maxp = face.GetMaxpTable();
        baselines = baseTable == null ? null : new FontBaselines(baseTable);
        horizontalDeviceMetrics = hdmx == null ?
            null : new FontHorizontalDeviceMetrics(hdmx);
        verticalDeviceMetrics = vdmx == null ?
            null : new FontVerticalDeviceMetrics(vdmx);
        linearThresholds = ltsh == null ? null : new FontLinearThresholds(ltsh);
        gridFittingBehavior = gasp == null ?
            null : new FontGridFittingBehavior(gasp);
        legacyKerning = kern == null || maxp == null ?
            null : new FontLegacyKerning(kern, maxp.GetGlyphCount());
    }

    /// <summary>Gets parsed BASE baseline data.</summary>
    /// <returns>The baseline data, or null when unavailable.</returns>
    public FontBaselines? GetBaselines() { return baselines; }

    /// <summary>Gets parsed horizontal device metrics.</summary>
    /// <returns>The hdmx data, or null when unavailable.</returns>
    public FontHorizontalDeviceMetrics? GetHorizontalDeviceMetrics()
    {
        return horizontalDeviceMetrics;
    }

    /// <summary>Gets parsed vertical device metrics.</summary>
    /// <returns>The VDMX data, or null when unavailable.</returns>
    public FontVerticalDeviceMetrics? GetVerticalDeviceMetrics()
    {
        return verticalDeviceMetrics;
    }

    /// <summary>Gets parsed linear-threshold data.</summary>
    /// <returns>The LTSH data, or null when unavailable.</returns>
    public FontLinearThresholds? GetLinearThresholds() { return linearThresholds; }

    /// <summary>Gets parsed grid-fitting and smoothing behavior.</summary>
    /// <returns>The gasp data, or null when unavailable.</returns>
    public FontGridFittingBehavior? GetGridFittingBehavior()
    {
        return gridFittingBehavior;
    }

    /// <summary>Gets parsed legacy kerning data.</summary>
    /// <returns>The kern data, or null when unavailable.</returns>
    public FontLegacyKerning? GetLegacyKerning() { return legacyKerning; }
}
