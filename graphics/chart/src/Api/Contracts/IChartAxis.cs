namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartAxis"/>.</summary>
public interface IChartAxis
{
    /// <summary>
    /// Sets the tick length configuration.
    /// </summary>
    /// <param name = "tickLength">The tick length.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    void SetTickLength(double tickLength);
    /// <summary>
    /// Sets the minor ticks configuration.
    /// </summary>
    /// <param name = "showMinorTicks">The show minor ticks.</param>
    /// <param name = "minorTickCount">The requested number of minor tick entries.</param>
    /// <param name = "minorTickLength">The minor tick length.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    void SetMinorTicks(bool showMinorTicks, int minorTickCount, double minorTickLength);
    /// <summary>
    /// Sets the tick labels configuration.
    /// </summary>
    /// <param name = "showTickLabels">The show tick labels.</param>
    /// <param name = "autoSkipTickLabels">The auto skip tick labels.</param>
    /// <param name = "minimumLabelSpacing">The minimum label spacing.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    void SetTickLabels(bool showTickLabels, bool autoSkipTickLabels, double minimumLabelSpacing);
    /// <summary>Configures tick-label rotation, trimming, ellipsis, and multiline rendering.</summary>
    /// <param name = "rotationDegrees">The finite clockwise rotation in degrees.</param>
    /// <param name = "maximumCharacters">The maximum characters per line, or zero for no limit.</param>
    /// <param name = "trim"><c>true</c> to remove surrounding whitespace from each label line.</param>
    /// <param name = "ellipsis"><c>true</c> to use an ellipsis when a line exceeds the maximum.</param>
    /// <param name = "multiline"><c>true</c> to preserve line breaks.</param>
    void SetTickLabelLayout(double rotationDegrees, int maximumCharacters, bool trim, bool ellipsis, bool multiline);
    /// <summary>
    /// Sets the strategy used to format tick labels.
    /// </summary>
    /// <param name = "formatter">The formatter that converts ticks to display text.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    void SetTickFormatter(IChartTickFormatter formatter);
    /// <summary>
    /// Sets the title configuration.
    /// </summary>
    /// <param name = "title">The title.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    void SetTitle(string title);
    /// <summary>Sets the subtitle rendered beneath the axis title.</summary>
    /// <param name = "subtitle">The subtitle, which may be empty.</param>
    void SetSubtitle(string subtitle);
    /// <summary>Sets the unit suffix appended to tick labels.</summary>
    /// <param name = "unit">The unit suffix, which may be empty.</param>
    void SetUnit(string unit);
    /// <summary>Sets the outward offset used to place multiple axes on the same side.</summary>
    /// <param name = "offset">The nonnegative offset in chart units.</param>
    void SetAxisOffset(double offset);
    /// <summary>Links this axis domain and ticks to a numeric source axis.</summary>
    /// <param name = "source">The source axis.</param>
    /// <param name = "scale">The positive conversion scale.</param>
    /// <param name = "offset">The conversion offset.</param>
    void SetLinkedAxis(ChartAxis source, double scale, double offset);
    /// <summary>Refreshes this axis from its configured linked source.</summary>
    /// <param name = "source">The linked source axis.</param>
    void SynchronizeLinkedAxis(ChartAxis source);
    /// <summary>Adds a styled threshold line at a domain value.</summary>
    /// <param name = "value">The threshold domain value.</param>
    /// <param name = "label">The optional threshold label.</param>
    /// <param name = "color">The threshold line color.</param>
    /// <param name = "strokeWidth">The positive threshold line width.</param>
    /// <returns>The zero-based threshold index.</returns>
    int AddThresholdLine(double value, string label, ChartColor color, double strokeWidth);
    /// <summary>Removes all threshold lines.</summary>
    void ClearThresholdLines();
    /// <summary>Adds a collapsed break inside the axis domain.</summary>
    /// <param name = "minimum">The break minimum.</param>
    /// <param name = "maximum">The break maximum.</param>
    /// <returns>The zero-based break index.</returns>
    int AddBreak(double minimum, double maximum);
    /// <summary>Removes all axis breaks.</summary>
    void ClearBreaks();
    /// <summary>
    /// Sets the offsets configuration.
    /// </summary>
    /// <param name = "labelOffset">The label offset.</param>
    /// <param name = "titleOffset">The title offset.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    void SetOffsets(double labelOffset, double titleOffset);
    /// <summary>
    /// Sets the grid lines configuration.
    /// </summary>
    /// <param name = "showGridLines">The show grid lines.</param>
    /// <param name = "gridLength">The grid length.</param>
    /// <param name = "gridColor">The grid color.</param>
    /// <param name = "gridStrokeWidth">The grid stroke width.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    void SetGridLines(bool showGridLines, double gridLength, ChartColor gridColor, double gridStrokeWidth);
    /// <summary>
    /// Sets the zero line configuration.
    /// </summary>
    /// <param name = "showZeroLine">The show zero line.</param>
    /// <param name = "zeroLineColor">The zero line color.</param>
    /// <param name = "zeroLineStrokeWidth">The zero line stroke width.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    void SetZeroLine(bool showZeroLine, ChartColor zeroLineColor, double zeroLineStrokeWidth);
    /// <summary>
    /// Sets the grid bands configuration.
    /// </summary>
    /// <param name = "showGridBands">The show grid bands.</param>
    /// <param name = "bandColor">The band color.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    void SetGridBands(bool showGridBands, ChartColor bandColor);
    /// <summary>
    /// Sets the text style configuration.
    /// </summary>
    /// <param name = "textColor">The text color.</param>
    /// <param name = "fontFamily">The SVG font-family name; bitmap output uses the built-in glyph shapes.</param>
    /// <param name = "fontSize">The font size in pixels, which must be positive.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    void SetTextStyle(ChartColor textColor, string fontFamily, int fontSize);
    /// <summary>
    /// Sets the stroke configuration.
    /// </summary>
    /// <param name = "color">The color value to apply or encode.</param>
    /// <param name = "strokeWidth">The stroke width in chart units, which must be positive.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    void SetStroke(ChartColor color, double strokeWidth);
    /// <summary>Sets the style used for major tick marks.</summary>
    /// <param name = "color">The major tick color.</param>
    /// <param name = "strokeWidth">The positive major tick width.</param>
    void SetTickStyle(ChartColor color, double strokeWidth);
    /// <summary>Sets the style used for minor tick marks.</summary>
    /// <param name = "color">The minor tick color.</param>
    /// <param name = "strokeWidth">The positive minor tick width.</param>
    void SetMinorTickStyle(ChartColor color, double strokeWidth);
    /// <summary>Sets the style used for axis text including tick labels.</summary>
    /// <param name = "color">The label text color.</param>
    /// <param name = "fontFamily">The SVG font-family name.</param>
    /// <param name = "fontSize">The positive font size.</param>
    void SetLabelStyle(ChartColor color, string fontFamily, int fontSize);
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartAxis Copy();
    /// <summary>Returns the current kind.</summary>
    /// <returns>The current field value.</returns>
    ChartAxisKind GetKind();
    /// <summary>Replaces the current kind.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetKind(ChartAxisKind value);
    /// <summary>Returns the current placement.</summary>
    /// <returns>The current field value.</returns>
    ChartAxisPlacement GetPlacement();
    /// <summary>Replaces the current placement.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetPlacement(ChartAxisPlacement value);
    /// <summary>Returns the current ticks.</summary>
    /// <returns>The current field value.</returns>
    ChartTick[] GetTicks();
    /// <summary>Replaces the current ticks.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetTicks(ChartTick[] value);
    /// <summary>Returns the current domain.</summary>
    /// <returns>The current field value.</returns>
    ChartRange GetDomain();
    /// <summary>Replaces the current domain.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetDomain(ChartRange value);
    /// <summary>Returns the current plot area.</summary>
    /// <returns>The current field value.</returns>
    ChartRect GetPlotArea();
    /// <summary>Replaces the current plot area.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetPlotArea(ChartRect value);
    /// <summary>Returns the current center.</summary>
    /// <returns>The current field value.</returns>
    ChartPoint GetCenter();
    /// <summary>Replaces the current center.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetCenter(ChartPoint value);
    /// <summary>Returns the current radius.</summary>
    /// <returns>The current field value.</returns>
    double GetRadius();
    /// <summary>Replaces the current radius.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetRadius(double value);
    /// <summary>Returns the current tick length.</summary>
    /// <returns>The current field value.</returns>
    double GetTickLength();
    /// <summary>Returns the current minor tick length.</summary>
    /// <returns>The current field value.</returns>
    double GetMinorTickLength();
    /// <summary>Replaces the current minor tick length.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetMinorTickLength(double value);
    /// <summary>Returns the current minor tick count.</summary>
    /// <returns>The current field value.</returns>
    int GetMinorTickCount();
    /// <summary>Replaces the current minor tick count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetMinorTickCount(int value);
    /// <summary>Returns the current label offset.</summary>
    /// <returns>The current field value.</returns>
    double GetLabelOffset();
    /// <summary>Replaces the current label offset.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetLabelOffset(double value);
    /// <summary>Returns the current title offset.</summary>
    /// <returns>The current field value.</returns>
    double GetTitleOffset();
    /// <summary>Replaces the current title offset.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetTitleOffset(double value);
    /// <summary>Returns the current grid length.</summary>
    /// <returns>The current field value.</returns>
    double GetGridLength();
    /// <summary>Replaces the current grid length.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetGridLength(double value);
    /// <summary>Returns the current minimum label spacing.</summary>
    /// <returns>The current field value.</returns>
    double GetMinimumLabelSpacing();
    /// <summary>Replaces the current minimum label spacing.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetMinimumLabelSpacing(double value);
    /// <summary>Returns the current stroke color.</summary>
    /// <returns>The current field value.</returns>
    ChartColor GetStrokeColor();
    /// <summary>Replaces the current stroke color.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetStrokeColor(ChartColor value);
    /// <summary>Returns the current grid color.</summary>
    /// <returns>The current field value.</returns>
    ChartColor GetGridColor();
    /// <summary>Replaces the current grid color.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetGridColor(ChartColor value);
    /// <summary>Returns the current zero line color.</summary>
    /// <returns>The current field value.</returns>
    ChartColor GetZeroLineColor();
    /// <summary>Replaces the current zero line color.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetZeroLineColor(ChartColor value);
    /// <summary>Returns the current band color.</summary>
    /// <returns>The current field value.</returns>
    ChartColor GetBandColor();
    /// <summary>Replaces the current band color.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetBandColor(ChartColor value);
    /// <summary>Returns the current text color.</summary>
    /// <returns>The current field value.</returns>
    ChartColor GetTextColor();
    /// <summary>Replaces the current text color.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetTextColor(ChartColor value);
    /// <summary>Returns the current stroke width.</summary>
    /// <returns>The current field value.</returns>
    double GetStrokeWidth();
    /// <summary>Replaces the current stroke width.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetStrokeWidth(double value);
    /// <summary>Returns the current grid stroke width.</summary>
    /// <returns>The current field value.</returns>
    double GetGridStrokeWidth();
    /// <summary>Replaces the current grid stroke width.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetGridStrokeWidth(double value);
    /// <summary>Returns the current zero line stroke width.</summary>
    /// <returns>The current field value.</returns>
    double GetZeroLineStrokeWidth();
    /// <summary>Replaces the current zero line stroke width.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetZeroLineStrokeWidth(double value);
    /// <summary>Returns the current id.</summary>
    /// <returns>The current field value.</returns>
    string GetId();
    /// <summary>Replaces the current id.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetId(string value);
    /// <summary>Returns the current title.</summary>
    /// <returns>The current field value.</returns>
    string GetTitle();
    /// <summary>Returns the current font family.</summary>
    /// <returns>The current field value.</returns>
    string GetFontFamily();
    /// <summary>Replaces the current font family.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetFontFamily(string value);
    /// <summary>Returns the current font size.</summary>
    /// <returns>The current field value.</returns>
    int GetFontSize();
    /// <summary>Replaces the current font size.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetFontSize(int value);
    /// <summary>Returns the current show tick labels.</summary>
    /// <returns>The current field value.</returns>
    bool GetShowTickLabels();
    /// <summary>Replaces the current show tick labels.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetShowTickLabels(bool value);
    /// <summary>Returns the current show minor ticks.</summary>
    /// <returns>The current field value.</returns>
    bool GetShowMinorTicks();
    /// <summary>Replaces the current show minor ticks.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetShowMinorTicks(bool value);
    /// <summary>Returns the current show grid lines.</summary>
    /// <returns>The current field value.</returns>
    bool GetShowGridLines();
    /// <summary>Replaces the current show grid lines.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetShowGridLines(bool value);
    /// <summary>Returns the current show zero line.</summary>
    /// <returns>The current field value.</returns>
    bool GetShowZeroLine();
    /// <summary>Replaces the current show zero line.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetShowZeroLine(bool value);
    /// <summary>Returns the current show grid bands.</summary>
    /// <returns>The current field value.</returns>
    bool GetShowGridBands();
    /// <summary>Replaces the current show grid bands.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetShowGridBands(bool value);
    /// <summary>Returns the current auto skip tick labels.</summary>
    /// <returns>The current field value.</returns>
    bool GetAutoSkipTickLabels();
    /// <summary>Replaces the current auto skip tick labels.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetAutoSkipTickLabels(bool value);
    /// <summary>Returns the current tick formatter.</summary>
    /// <returns>The current field value.</returns>
    IChartTickFormatter GetTickFormatter();
}
