namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartDocument"/>.</summary>
public interface IChartDocument
{
    /// <summary>
    /// Sets the size configuration.
    /// </summary>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    void SetSize(double width, double height);
    /// <summary>Clamps a requested logical size to responsive minimum and maximum bounds.</summary>
    /// <param name = "targetWidth">The requested logical width.</param>
    /// <param name = "targetHeight">The requested logical height.</param>
    /// <param name = "minimumWidth">The minimum logical width.</param>
    /// <param name = "minimumHeight">The minimum logical height.</param>
    /// <param name = "maximumWidth">The maximum logical width.</param>
    /// <param name = "maximumHeight">The maximum logical height.</param>
    void SetResponsiveTargetSize(double targetWidth, double targetHeight, double minimumWidth, double minimumHeight, double maximumWidth, double maximumHeight);
    /// <summary>Sets the number of output device pixels per logical chart pixel.</summary>
    /// <param name = "devicePixelRatio">The positive device-pixel ratio.</param>
    void SetDevicePixelRatio(double devicePixelRatio);
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
    /// Sets the theme configuration.
    /// </summary>
    /// <param name = "theme">The theme.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    void SetTheme(ChartTheme theme);
    /// <summary>
    /// Sets the style configuration.
    /// </summary>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    void SetStyle(ChartStyle style);
    /// <summary>
    /// Sets the title configuration.
    /// </summary>
    /// <param name = "title">The title.</param>
    void SetTitle(string title);
    /// <summary>
    /// Sets the subtitle configuration.
    /// </summary>
    /// <param name = "subtitle">The subtitle.</param>
    void SetSubtitle(string subtitle);
    /// <summary>
    /// Sets the caption configuration.
    /// </summary>
    /// <param name = "caption">The caption.</param>
    void SetCaption(string caption);
    /// <summary>
    /// Sets the accessibility configuration.
    /// </summary>
    /// <param name = "accessibility">The accessibility.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    void SetAccessibility(ChartAccessibilityMetadata accessibility);
    /// <summary>Sets the chart-wide configuration used by configured rendering and export operations.</summary>
    /// <param name = "configuration">The chart configuration to copy into the document.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    void SetConfiguration(ChartConfiguration configuration);
    /// <summary>
    /// Sets the accessibility title description configuration.
    /// </summary>
    /// <param name = "title">The title.</param>
    /// <param name = "description">The description.</param>
    void SetAccessibilityTitleDescription(string title, string description);
    /// <summary>
    /// Sets the grid configuration.
    /// </summary>
    /// <param name = "grid">The grid.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    void SetGrid(ChartGrid grid);
    /// <summary>
    /// Adds the supplied plot to this object.
    /// </summary>
    /// <param name = "plot">The plot whose bounds, axes, and styles are processed.</param>
    /// <returns>The resulting plot.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartPlot AddPlot(ChartPlot plot);
    /// <summary>
    /// Adds the supplied plot to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <returns>The resulting plot.</returns>
    ChartPlot AddPlotById(string id);
    /// <summary>
    /// Adds the supplied plot to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "bounds">The rectangle that constrains the generated layout or drawing.</param>
    /// <returns>The resulting plot.</returns>
    ChartPlot AddPlotWithBounds(string id, ChartRect bounds);
    /// <summary>
    /// Adds the supplied grid plot to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "row">The row.</param>
    /// <param name = "column">The column.</param>
    /// <param name = "rowSpan">The row span.</param>
    /// <param name = "columnSpan">The column span.</param>
    /// <returns>The resulting plot.</returns>
    ChartPlot AddGridPlot(string id, int row, int column, int rowSpan, int columnSpan);
    /// <summary>Configures the grid used to arrange dashboard panels.</summary>
    /// <param name = "rows">The positive number of dashboard rows.</param>
    /// <param name = "columns">The positive number of dashboard columns.</param>
    /// <param name = "rowGap">The nonnegative gap between rows.</param>
    /// <param name = "columnGap">The nonnegative gap between columns.</param>
    void ConfigureDashboard(int rows, int columns, double rowGap, double columnGap);
    /// <summary>Adds a dashboard panel occupying one or more configured grid cells.</summary>
    /// <param name = "id">A stable panel identifier.</param>
    /// <param name = "row">The zero-based starting row.</param>
    /// <param name = "column">The zero-based starting column.</param>
    /// <param name = "rowSpan">The positive row span.</param>
    /// <param name = "columnSpan">The positive column span.</param>
    /// <returns>The added dashboard plot.</returns>
    ChartPlot AddDashboardPanel(string id, int row, int column, int rowSpan, int columnSpan);
    /// <summary>Adds legend content and its docked or overlay layout request.</summary>
    /// <param name = "legend">The legend content to copy.</param>
    /// <param name = "side">The side used to dock or align the legend.</param>
    /// <param name = "width">The requested width, or zero to fill the available width.</param>
    /// <param name = "height">The requested height, or zero to fill the available height.</param>
    /// <param name = "spacing">The nonnegative spacing from adjacent content.</param>
    /// <param name = "overlay">Whether the legend overlays the plot area.</param>
    /// <returns>The copied stored legend.</returns>
    ChartLegend AddLegend(ChartLegend legend, ChartSide side, double width, double height, double spacing, bool overlay);
    /// <summary>Adds gradient colorbar content and its docked or overlay layout request.</summary>
    /// <param name = "colorbar">The gradient legend used as colorbar content.</param>
    /// <param name = "side">The side used to dock or align the colorbar.</param>
    /// <param name = "width">The requested width, or zero to fill the available width.</param>
    /// <param name = "height">The requested height, or zero to fill the available height.</param>
    /// <param name = "spacing">The nonnegative spacing from adjacent content.</param>
    /// <param name = "overlay">Whether the colorbar overlays the plot area.</param>
    /// <returns>The copied stored colorbar.</returns>
    ChartLegend AddColorbar(ChartLegend colorbar, ChartSide side, double width, double height, double spacing, bool overlay);
    /// <summary>Adds text annotation content and its docked or overlay layout request.</summary>
    /// <param name = "id">A stable annotation identifier.</param>
    /// <param name = "text">The annotation text.</param>
    /// <param name = "side">The side used to dock or align the annotation.</param>
    /// <param name = "width">The requested width, or zero to fill the available width.</param>
    /// <param name = "height">The requested height, or zero to fill the available height.</param>
    /// <param name = "spacing">The nonnegative spacing from adjacent content.</param>
    /// <param name = "overlay">Whether the annotation overlays the plot area.</param>
    /// <param name = "style">The annotation render style.</param>
    /// <returns>The created layout request.</returns>
    ChartLayoutRequest AddCompositionAnnotation(string id, string text, ChartSide side, double width, double height, double spacing, bool overlay, ChartRenderStyle style);
    /// <summary>Lays out and renders all retained document content through one composition workflow.</summary>
    /// <returns>A retained scene containing the complete document.</returns>
    ChartRenderScene RenderComposition();
    /// <summary>Returns one stored legend.</summary>
    /// <param name = "index">The zero-based legend index.</param>
    /// <returns>The stored legend.</returns>
    ChartLegend LegendAt(int index);
    /// <summary>Returns one stored colorbar.</summary>
    /// <param name = "index">The zero-based colorbar index.</param>
    /// <returns>The stored colorbar.</returns>
    ChartLegend ColorbarAt(int index);
    /// <summary>
    /// Creates the requested scene from the supplied data and configuration.
    /// </summary>
    /// <returns>The newly created render scene.</returns>
    ChartRenderScene CreateScene();
    /// <summary>
    /// Renders chart content into a new scene sized and initialized by this document.
    /// </summary>
    /// <param name = "renderer">The renderer that supplies drawing operations.</param>
    /// <returns>The newly created render scene.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartRenderScene Render(IChartRenderer renderer);
    /// <summary>
    /// Applies the export scene svg operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <returns>The resulting string.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    string ExportSceneSvg(ChartRenderScene scene);
    /// <summary>
    /// Applies the export scene png operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <returns>The resulting byte array.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    byte[] ExportScenePng(ChartRenderScene scene);
    /// <summary>Exports a retained scene using this document's chart-wide configuration and accessibility metadata.</summary>
    /// <param name = "scene">The retained scene to render and encode.</param>
    /// <returns>The configured output bytes.</returns>
    byte[] ExportConfiguredScene(ChartRenderScene scene);
    /// <summary>
    /// Renders cartesian using the supplied data and configuration.
    /// </summary>
    /// <param name = "plotId">The plot id.</param>
    /// <param name = "series">The chart series to render, validate, or combine.</param>
    /// <param name = "xDomain">The x domain.</param>
    /// <param name = "yDomain">The y domain.</param>
    /// <returns>The resulting render scene.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    ChartRenderScene RenderCartesian(string plotId, ChartCartesianSeries[] series, ChartRange xDomain, ChartRange yDomain);
    /// <summary>
    /// Renders cartesian svg using the supplied data and configuration.
    /// </summary>
    /// <param name = "plotId">The plot id.</param>
    /// <param name = "series">The chart series to render, validate, or combine.</param>
    /// <param name = "xDomain">The x domain.</param>
    /// <param name = "yDomain">The y domain.</param>
    /// <returns>The resulting string.</returns>
    string RenderCartesianSvg(string plotId, ChartCartesianSeries[] series, ChartRange xDomain, ChartRange yDomain);
    /// <summary>
    /// Adds the supplied legend layout to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "side">The side.</param>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    /// <param name = "spacing">The spacing.</param>
    /// <returns>The resulting layout request.</returns>
    ChartLayoutRequest AddLegendLayout(string id, ChartSide side, double width, double height, double spacing);
    /// <summary>
    /// Adds the supplied colorbar layout to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "side">The side.</param>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    /// <param name = "spacing">The spacing.</param>
    /// <returns>The resulting layout request.</returns>
    ChartLayoutRequest AddColorbarLayout(string id, ChartSide side, double width, double height, double spacing);
    /// <summary>Adds a legend over the plot area without reserving layout space.</summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "side">The side used to align the overlay.</param>
    /// <param name = "width">The overlay width, or zero to fill the available width.</param>
    /// <param name = "height">The overlay height, or zero to fill the available height.</param>
    /// <param name = "spacing">The retained spacing metadata.</param>
    /// <returns>The resulting overlay layout request.</returns>
    ChartLayoutRequest AddOverlayLegendLayout(string id, ChartSide side, double width, double height, double spacing);
    /// <summary>Adds a colorbar over the plot area without reserving layout space.</summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "side">The side used to align the overlay.</param>
    /// <param name = "width">The overlay width, or zero to fill the available width.</param>
    /// <param name = "height">The overlay height, or zero to fill the available height.</param>
    /// <param name = "spacing">The retained spacing metadata.</param>
    /// <returns>The resulting overlay layout request.</returns>
    ChartLayoutRequest AddOverlayColorbarLayout(string id, ChartSide side, double width, double height, double spacing);
    /// <summary>
    /// Adds the supplied annotation layout to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "side">The side.</param>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    /// <param name = "spacing">The spacing.</param>
    /// <param name = "overlay">The overlay.</param>
    /// <returns>The resulting layout request.</returns>
    ChartLayoutRequest AddAnnotationLayout(string id, ChartSide side, double width, double height, double spacing, bool overlay);
    /// <summary>
    /// Adds the supplied layout request to this object.
    /// </summary>
    /// <param name = "request">The request.</param>
    /// <returns>The resulting layout request.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartLayoutRequest AddLayoutRequest(ChartLayoutRequest request);
    /// <summary>
    /// Computes the layout for request at within the supplied bounds.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting layout request.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    ChartLayoutRequest LayoutRequestAt(int index);
    /// <summary>
    /// Creates the requested small multiples from the supplied data and configuration.
    /// </summary>
    /// <param name = "idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name = "rows">The rows.</param>
    /// <param name = "columns">The columns.</param>
    void CreateSmallMultiples(string idPrefix, int rows, int columns);
    /// <summary>Creates one facet plot per row.</summary>
    /// <param name = "idPrefix">The prefix used for stable facet identifiers.</param>
    /// <param name = "count">The positive number of facets.</param>
    void CreateRowFacets(string idPrefix, int count);
    /// <summary>Creates one facet plot per column.</summary>
    /// <param name = "idPrefix">The prefix used for stable facet identifiers.</param>
    /// <param name = "count">The positive number of facets.</param>
    void CreateColumnFacets(string idPrefix, int count);
    /// <summary>Creates facets that wrap across a fixed number of columns.</summary>
    /// <param name = "idPrefix">The prefix used for stable facet identifiers.</param>
    /// <param name = "count">The positive number of facets.</param>
    /// <param name = "columns">The positive number of columns before wrapping.</param>
    void CreateWrapFacets(string idPrefix, int count, int columns);
    /// <summary>
    /// Determines whether this value contains plot.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    bool ContainsPlot(string id);
    /// <summary>
    /// Returns the zero-based plot index.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <returns>The resulting int.</returns>
    int PlotIndex(string id);
    /// <summary>
    /// Returns the requested plot.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting plot.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    ChartPlot PlotAt(int index);
    /// <summary>
    /// Applies the figure rect operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting rect.</returns>
    ChartRect FigureRect();
    /// <summary>
    /// Applies the content rect operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting rect.</returns>
    ChartRect ContentRect();
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartDocument Copy();
    /// <summary>Returns the current width.</summary>
    /// <returns>The current field value.</returns>
    double GetWidth();
    /// <summary>Replaces the current width.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetWidth(double value);
    /// <summary>Returns the current height.</summary>
    /// <returns>The current field value.</returns>
    double GetHeight();
    /// <summary>Returns the current device-pixel ratio.</summary>
    /// <returns>The positive output device-pixel ratio.</returns>
    double GetDevicePixelRatio();
    /// <summary>Replaces the current height.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetHeight(double value);
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
    /// <summary>Returns the current theme.</summary>
    /// <returns>The current field value.</returns>
    ChartTheme GetTheme();
    /// <summary>Returns the current style.</summary>
    /// <returns>The current field value.</returns>
    ChartStyle GetStyle();
    /// <summary>Returns the current title.</summary>
    /// <returns>The current field value.</returns>
    ChartTextBlock GetTitle();
    /// <summary>Returns the current subtitle.</summary>
    /// <returns>The current field value.</returns>
    ChartTextBlock GetSubtitle();
    /// <summary>Returns the current caption.</summary>
    /// <returns>The current field value.</returns>
    ChartTextBlock GetCaption();
    /// <summary>Returns the current accessibility.</summary>
    /// <returns>The current field value.</returns>
    ChartAccessibilityMetadata GetAccessibility();
    /// <summary>Returns an independent copy of the chart-wide configuration.</summary>
    /// <returns>A configuration that can be modified without changing this document.</returns>
    ChartConfiguration GetConfiguration();
    /// <summary>Returns the current grid.</summary>
    /// <returns>The current field value.</returns>
    ChartGrid GetGrid();
    /// <summary>Returns the current plots.</summary>
    /// <returns>The current field value.</returns>
    ChartPlot[] GetPlots();
    /// <summary>Replaces the current plots.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetPlots(ChartPlot[] value);
    /// <summary>Returns the current plot count.</summary>
    /// <returns>The current field value.</returns>
    int GetPlotCount();
    /// <summary>Replaces the current plot count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetPlotCount(int value);
    /// <summary>Returns the current layout requests.</summary>
    /// <returns>The current field value.</returns>
    ChartLayoutRequest[] GetLayoutRequests();
    /// <summary>Replaces the current layout requests.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetLayoutRequests(ChartLayoutRequest[] value);
    /// <summary>Returns the current layout request count.</summary>
    /// <returns>The current field value.</returns>
    int GetLayoutRequestCount();
    /// <summary>Replaces the current layout request count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetLayoutRequestCount(int value);
}
