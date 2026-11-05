namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartPlot"/>.</summary>
public interface IChartPlot
{
    /// <summary>
    /// Sets the bounds configuration.
    /// </summary>
    /// <param name = "bounds">The rectangle that constrains the generated layout or drawing.</param>
    void SetBounds(ChartRect bounds);
    /// <summary>
    /// Sets the margins configuration.
    /// </summary>
    /// <param name = "margins">The margins.</param>
    void SetMargins(ChartPadding margins);
    /// <summary>
    /// Sets the background configuration.
    /// </summary>
    /// <param name = "color">The color value to apply or encode.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    void SetBackground(ChartColor color);
    /// <summary>
    /// Sets the style configuration.
    /// </summary>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    void SetStyle(ChartStyle style);
    /// <summary>Sets the cartesian domains used by document composition rendering.</summary>
    /// <param name = "xDomain">The horizontal data domain.</param>
    /// <param name = "yDomain">The vertical data domain.</param>
    void SetCartesianDomains(ChartRange xDomain, ChartRange yDomain);
    /// <summary>Adds a copied cartesian series to this plot's composition content.</summary>
    /// <param name = "series">The chart series to render.</param>
    /// <returns>The copied stored series.</returns>
    ChartCartesianSeries AddSeries(ChartCartesianSeries series);
    /// <summary>Adds a copied cartesian annotation series rendered above this plot's axes and data.</summary>
    /// <param name = "series">The annotation series to render.</param>
    /// <returns>The copied stored annotation series.</returns>
    ChartCartesianSeries AddAnnotationSeries(ChartCartesianSeries series);
    /// <summary>Returns one stored composition series.</summary>
    /// <param name = "index">The zero-based series index.</param>
    /// <returns>The stored series.</returns>
    ChartCartesianSeries SeriesAt(int index);
    /// <summary>Returns one stored annotation series.</summary>
    /// <param name = "index">The zero-based annotation index.</param>
    /// <returns>The stored annotation series.</returns>
    ChartCartesianSeries AnnotationSeriesAt(int index);
    /// <summary>
    /// Adds the supplied axis to this object.
    /// </summary>
    /// <param name = "axis">The axis.</param>
    /// <returns>The resulting axis.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartAxis AddAxis(ChartAxis axis);
    /// <summary>
    /// Adds the supplied bottom axis to this object.
    /// </summary>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    /// <param name = "tickCount">The requested number of tick entries.</param>
    /// <returns>The resulting axis.</returns>
    ChartAxis AddBottomAxis(double minimum, double maximum, int tickCount);
    /// <summary>
    /// Adds the supplied top axis to this object.
    /// </summary>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    /// <param name = "tickCount">The requested number of tick entries.</param>
    /// <returns>The resulting axis.</returns>
    ChartAxis AddTopAxis(double minimum, double maximum, int tickCount);
    /// <summary>
    /// Adds the supplied left axis to this object.
    /// </summary>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    /// <param name = "tickCount">The requested number of tick entries.</param>
    /// <returns>The resulting axis.</returns>
    ChartAxis AddLeftAxis(double minimum, double maximum, int tickCount);
    /// <summary>
    /// Adds the supplied right axis to this object.
    /// </summary>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    /// <param name = "tickCount">The requested number of tick entries.</param>
    /// <returns>The resulting axis.</returns>
    ChartAxis AddRightAxis(double minimum, double maximum, int tickCount);
    /// <summary>
    /// Adds the supplied numeric axis to this object.
    /// </summary>
    /// <param name = "placement">The placement.</param>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    /// <param name = "tickCount">The requested number of tick entries.</param>
    /// <returns>The resulting axis.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    ChartAxis AddNumericAxis(ChartAxisPlacement placement, double minimum, double maximum, int tickCount);
    /// <summary>
    /// Creates the requested cartesian transform from the supplied data and configuration.
    /// </summary>
    /// <param name = "xDomain">The x domain.</param>
    /// <param name = "yDomain">The y domain.</param>
    /// <returns>The newly created cartesian transform.</returns>
    ChartCartesianTransform CreateCartesianTransform(ChartRange xDomain, ChartRange yDomain);
    /// <summary>
    /// Creates the requested cartesian transform from axes from the supplied data and configuration.
    /// </summary>
    /// <returns>The newly created cartesian transform.</returns>
    /// <exception cref = "System.InvalidOperationException">Thrown when the current object state cannot perform this operation.</exception>
    ChartCartesianTransform CreateCartesianTransformFromAxes();
    /// <summary>
    /// Creates the requested cartesian renderer from the supplied data and configuration.
    /// </summary>
    /// <param name = "xDomain">The x domain.</param>
    /// <param name = "yDomain">The y domain.</param>
    /// <returns>The newly created cartesian renderer.</returns>
    ChartCartesianRenderer CreateCartesianRenderer(ChartRange xDomain, ChartRange yDomain);
    /// <summary>
    /// Creates the requested cartesian renderer from axes from the supplied data and configuration.
    /// </summary>
    /// <returns>The newly created cartesian renderer.</returns>
    ChartCartesianRenderer CreateCartesianRendererFromAxes();
    /// <summary>Sets a ternary triangle using vertices relative to the plot's inner bounds.</summary>
    /// <param name = "firstVertex">The relative vertex representing a full first component.</param>
    /// <param name = "secondVertex">The relative vertex representing a full second component.</param>
    /// <param name = "thirdVertex">The relative vertex representing a full third component.</param>
    void SetTernaryRegion(ChartPoint firstVertex, ChartPoint secondVertex, ChartPoint thirdVertex);
    /// <summary>Creates a ternary transform fitted to the configured region of the current inner bounds.</summary>
    /// <returns>The newly created ternary transform.</returns>
    ChartTernaryTransform CreateTernaryTransform();
    /// <summary>Sets whether all series created for this plot are clipped to its inner bounds.</summary>
    /// <param name = "clipToPlotArea">Whether plot-level clipping is enabled.</param>
    void SetPlotClipping(bool clipToPlotArea);
    /// <summary>
    /// Finds first axis using the supplied data and configuration.
    /// </summary>
    /// <param name = "kind">The kind.</param>
    /// <returns>The matching value, or <c>null</c> when no match exists.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    ChartAxis? FindFirstAxis(ChartAxisKind kind);
    /// <summary>
    /// Returns the requested axis.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting axis.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    ChartAxis AxisAt(int index);
    /// <summary>
    /// Sets the grid position configuration.
    /// </summary>
    /// <param name = "row">The row.</param>
    /// <param name = "column">The column.</param>
    /// <param name = "rowSpan">The row span.</param>
    /// <param name = "columnSpan">The column span.</param>
    void SetGridPosition(int row, int column, int rowSpan, int columnSpan);
    /// <summary>
    /// Sets the inset configuration.
    /// </summary>
    /// <param name = "parentPlotId">The parent plot id.</param>
    /// <param name = "insetBounds">The inset bounds.</param>
    void SetInset(string parentPlotId, ChartRect insetBounds);
    /// <summary>
    /// Sets the shared axes configuration.
    /// </summary>
    /// <param name = "shareXAxisGroup">The share x axis group.</param>
    /// <param name = "shareYAxisGroup">The share y axis group.</param>
    void SetSharedAxes(string shareXAxisGroup, string shareYAxisGroup);
    /// <summary>Constrains cartesian transforms to a fixed horizontal-to-vertical data scale ratio.</summary>
    /// <param name = "ratio">The positive horizontal-to-vertical data scale ratio.</param>
    void SetFixedAspectRatio(double ratio);
    /// <summary>Restores unconstrained cartesian transform aspect ratios.</summary>
    void ClearFixedAspectRatio();
    /// <summary>
    /// Applies the inner bounds operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting rect.</returns>
    ChartRect InnerBounds();
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartPlot Copy();
    /// <summary>Returns the current id.</summary>
    /// <returns>The current field value.</returns>
    string GetId();
    /// <summary>Replaces the current id.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetId(string value);
    /// <summary>Returns the current bounds.</summary>
    /// <returns>The current field value.</returns>
    ChartRect GetBounds();
    /// <summary>Returns the current margins.</summary>
    /// <returns>The current field value.</returns>
    ChartPadding GetMargins();
    /// <summary>Returns the current background color.</summary>
    /// <returns>The current field value.</returns>
    ChartColor GetBackgroundColor();
    /// <summary>Replaces the current background color.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetBackgroundColor(ChartColor value);
    /// <summary>Returns the current has background.</summary>
    /// <returns>The current field value.</returns>
    bool GetHasBackground();
    /// <summary>Replaces the current has background.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetHasBackground(bool value);
    /// <summary>Returns the current style.</summary>
    /// <returns>The current field value.</returns>
    ChartStyle GetStyle();
    /// <summary>Returns the current axes.</summary>
    /// <returns>The current field value.</returns>
    ChartAxis[] GetAxes();
    /// <summary>Replaces the current axes.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetAxes(ChartAxis[] value);
    /// <summary>Returns the current axis count.</summary>
    /// <returns>The current field value.</returns>
    int GetAxisCount();
    /// <summary>Replaces the current axis count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetAxisCount(int value);
    /// <summary>Returns the current has grid position.</summary>
    /// <returns>The current field value.</returns>
    bool GetHasGridPosition();
    /// <summary>Replaces the current has grid position.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetHasGridPosition(bool value);
    /// <summary>Returns the current grid row.</summary>
    /// <returns>The current field value.</returns>
    int GetGridRow();
    /// <summary>Replaces the current grid row.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetGridRow(int value);
    /// <summary>Returns the current grid column.</summary>
    /// <returns>The current field value.</returns>
    int GetGridColumn();
    /// <summary>Replaces the current grid column.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetGridColumn(int value);
    /// <summary>Returns the current row span.</summary>
    /// <returns>The current field value.</returns>
    int GetRowSpan();
    /// <summary>Replaces the current row span.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetRowSpan(int value);
    /// <summary>Returns the current column span.</summary>
    /// <returns>The current field value.</returns>
    int GetColumnSpan();
    /// <summary>Replaces the current column span.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetColumnSpan(int value);
    /// <summary>Returns the current is inset.</summary>
    /// <returns>The current field value.</returns>
    bool GetIsInset();
    /// <summary>Replaces the current is inset.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetIsInset(bool value);
    /// <summary>Returns the current inset parent id.</summary>
    /// <returns>The current field value.</returns>
    string GetInsetParentId();
    /// <summary>Replaces the current inset parent id.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetInsetParentId(string value);
    /// <summary>Returns the current inset bounds.</summary>
    /// <returns>The current field value.</returns>
    ChartRect GetInsetBounds();
    /// <summary>Replaces the current inset bounds.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetInsetBounds(ChartRect value);
    /// <summary>Returns the current share x axis group.</summary>
    /// <returns>The current field value.</returns>
    string GetShareXAxisGroup();
    /// <summary>Replaces the current share x axis group.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetShareXAxisGroup(string value);
    /// <summary>Returns the current share y axis group.</summary>
    /// <returns>The current field value.</returns>
    string GetShareYAxisGroup();
    /// <summary>Returns the relative ternary vertex for the first component.</summary>
    /// <returns>A copy of the configured relative vertex.</returns>
    ChartPoint GetTernaryFirstVertex();
    /// <summary>Returns the relative ternary vertex for the second component.</summary>
    /// <returns>A copy of the configured relative vertex.</returns>
    ChartPoint GetTernarySecondVertex();
    /// <summary>Returns the relative ternary vertex for the third component.</summary>
    /// <returns>A copy of the configured relative vertex.</returns>
    ChartPoint GetTernaryThirdVertex();
    /// <summary>Returns whether plot-level clipping is enabled.</summary>
    /// <returns><c>true</c> when series created for this plot are clipped.</returns>
    bool GetPlotClipping();
    /// <summary>Replaces the current share y axis group.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetShareYAxisGroup(string value);
}
