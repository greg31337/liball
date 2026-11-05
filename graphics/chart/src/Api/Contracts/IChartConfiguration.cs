namespace chart;
/// <summary>Defines the common configuration vocabulary used across chart creation and rendering.</summary>
public interface IChartConfiguration
{
    /// <summary>Returns the coordinate unit used to interpret positions and dimensions.</summary>
    /// <returns>The configured coordinate unit.</returns>
    ChartCoordinateUnit GetCoordinateUnit();
    /// <summary>Returns the direction used when mapping ordered values.</summary>
    /// <returns>The configured direction.</returns>
    ChartDirection GetDirection();
    /// <summary>Returns the policy used to process missing values.</summary>
    /// <returns>The configured missing value policy.</returns>
    ChartMissingValuePolicy GetMissingValuePolicy();
    /// <summary>Returns the requested output format.</summary>
    /// <returns>The configured output format.</returns>
    ChartOutputFormat GetOutputFormat();
    /// <summary>Returns the backend used to render chart output.</summary>
    /// <returns>The configured render backend.</returns>
    ChartRenderBackend GetRenderBackend();
    /// <summary>Returns the scale kind used to map data values.</summary>
    /// <returns>The configured scale kind.</returns>
    ChartScaleKind GetScaleKind();
    /// <summary>Returns an independent copy of the scale policy.</summary>
    /// <returns>The configured scale policy.</returns>
    ChartScalePolicy GetScalePolicy();
    /// <summary>Returns the kind of series represented by the configuration.</summary>
    /// <returns>The configured series kind.</returns>
    ChartSeriesKind GetSeriesKind();
    /// <summary>Returns the ordering applied to chart values.</summary>
    /// <returns>The configured sort kind.</returns>
    ChartSortKind GetSortKind();
    /// <summary>Returns the horizontal alignment used for text.</summary>
    /// <returns>The configured text alignment.</returns>
    ChartTextAlignment GetTextAlignment();
    /// <summary>Returns the vertical alignment used for text.</summary>
    /// <returns>The configured vertical text alignment.</returns>
    ChartVerticalTextAlignment GetVerticalTextAlignment();
    /// <summary>Sets the coordinate unit used to interpret positions and dimensions.</summary>
    /// <param name = "value">The configured coordinate unit.</param>
    void SetCoordinateUnit(ChartCoordinateUnit value);
    /// <summary>Sets the direction used when mapping ordered values.</summary>
    /// <param name = "value">The configured direction.</param>
    void SetDirection(ChartDirection value);
    /// <summary>Sets the policy used to process missing values.</summary>
    /// <param name = "value">The configured missing value policy.</param>
    void SetMissingValuePolicy(ChartMissingValuePolicy value);
    /// <summary>Sets the requested output format.</summary>
    /// <param name = "value">The configured output format.</param>
    void SetOutputFormat(ChartOutputFormat value);
    /// <summary>Sets the backend used to render chart output.</summary>
    /// <param name = "value">The configured render backend.</param>
    void SetRenderBackend(ChartRenderBackend value);
    /// <summary>Sets the scale kind used to map data values.</summary>
    /// <param name = "value">The configured scale kind.</param>
    void SetScaleKind(ChartScaleKind value);
    /// <summary>Sets the policies used for domain resolution and numeric scale mapping.</summary>
    /// <param name = "policy">The scale policy.</param>
    void SetScalePolicy(ChartScalePolicy policy);
    /// <summary>Resolves a domain using the configured scale policy.</summary>
    /// <param name = "values">The values used for automatic domain calculation.</param>
    /// <param name = "explicitDomain">The explicit domain used when automatic calculation is disabled.</param>
    /// <returns>The resolved scale domain.</returns>
    ChartRange ResolveScaleDomain(double[] values, ChartRange? explicitDomain);
    /// <summary>Resolves scale domains for independent, shared, or grouped subplots.</summary>
    /// <param name = "subplotValues">The values for each subplot.</param>
    /// <param name = "sharingGroups">The sharing-group identifier for each subplot.</param>
    /// <returns>One resolved domain per subplot.</returns>
    ChartRange[] ResolveSubplotScaleDomains(double[][] subplotValues, string[] sharingGroups);
    /// <summary>Resolves scale domains using sharing groups configured on plots.</summary>
    /// <param name = "plots">The subplots whose sharing groups are used.</param>
    /// <param name = "subplotValues">The values for each subplot.</param>
    /// <param name = "axisKind">The horizontal or vertical axis whose group is used.</param>
    /// <returns>One resolved domain per subplot.</returns>
    ChartRange[] ResolvePlotScaleDomains(ChartPlot[] plots, double[][] subplotValues, ChartAxisKind axisKind);
    /// <summary>Sets the kind of series represented by the configuration.</summary>
    /// <param name = "value">The configured series kind.</param>
    void SetSeriesKind(ChartSeriesKind value);
    /// <summary>Sets the ordering applied to chart values.</summary>
    /// <param name = "value">The configured sort kind.</param>
    void SetSortKind(ChartSortKind value);
    /// <summary>Sets the horizontal alignment used for text.</summary>
    /// <param name = "value">The configured text alignment.</param>
    void SetTextAlignment(ChartTextAlignment value);
    /// <summary>Sets the vertical alignment used for text.</summary>
    /// <param name = "value">The configured vertical text alignment.</param>
    void SetVerticalTextAlignment(ChartVerticalTextAlignment value);
    /// <summary>Maps a numeric value with the configured scale and direction.</summary>
    /// <param name = "value">The data value to map.</param>
    /// <param name = "domainMinimum">The lower end of the input domain.</param>
    /// <param name = "domainMaximum">The upper end of the input domain.</param>
    /// <param name = "rangeMinimum">The lower end of the output range.</param>
    /// <param name = "rangeMaximum">The upper end of the output range.</param>
    /// <returns>The mapped value.</returns>
    double MapNumericValue(double value, double domainMinimum, double domainMaximum, double rangeMinimum, double rangeMaximum);
    /// <summary>Maps a point expressed in the configured coordinate unit into a reference rectangle.</summary>
    /// <param name = "point">The point to map.</param>
    /// <param name = "bounds">The reference rectangle for plot, paper, axis, and fraction coordinates.</param>
    /// <param name = "xDomain">The horizontal data domain.</param>
    /// <param name = "yDomain">The vertical data domain.</param>
    /// <returns>The mapped point.</returns>
    ChartPoint MapPoint(ChartPoint point, ChartRect bounds, ChartRange xDomain, ChartRange yDomain);
    /// <summary>Returns the top-left text origin for an anchor and measured text size.</summary>
    /// <param name = "anchor">The alignment anchor.</param>
    /// <param name = "measuredSize">The measured text size.</param>
    /// <returns>The aligned text origin.</returns>
    ChartPoint AlignText(ChartPoint anchor, ChartSize measuredSize);
    /// <summary>Processes non-finite numeric values using the configured missing-value policy.</summary>
    /// <param name = "values">The values to process.</param>
    /// <returns>A new array containing the resolved values.</returns>
    double[] ResolveMissingValues(double[] values);
    /// <summary>Sorts a column according to the configured sort kind.</summary>
    /// <param name = "column">The column whose values determine ordering.</param>
    /// <param name = "ascending"><c>true</c> for ascending order; otherwise descending.</param>
    /// <returns>The ordered row indices.</returns>
    int[] SortIndices(ChartDataColumn column, bool ascending);
    /// <summary>Creates a cartesian series using the configured series kind.</summary>
    /// <param name = "id">A stable series identifier.</param>
    /// <param name = "xValues">The horizontal data values.</param>
    /// <param name = "yValues">The vertical data values.</param>
    /// <param name = "baseline">The baseline used by area and bar series.</param>
    /// <param name = "itemSize">The marker size or bar width.</param>
    /// <param name = "style">The render style.</param>
    /// <returns>The configured cartesian series.</returns>
    ChartCartesianSeries CreateCartesianSeries(string id, double[] xValues, double[] yValues, double baseline, double itemSize, ChartRenderStyle style);
    /// <summary>Renders and encodes a retained scene using the configured backend and output format.</summary>
    /// <param name = "scene">The retained scene to render.</param>
    /// <param name = "accessibility">The accessibility metadata used by SVG output.</param>
    /// <returns>The encoded output bytes.</returns>
    byte[] ExportScene(ChartRenderScene scene, ChartAccessibilityMetadata accessibility);
    /// <summary>Creates a deep copy that can be modified independently.</summary>
    /// <returns>A new configuration independent of this instance.</returns>
    ChartConfiguration Copy();
}
