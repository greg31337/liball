namespace chart;
/// <summary>
/// Represents a document used by the chart model or rendering pipeline.
/// </summary>
/// <remarks>The document owns figure-level styling, plots, layout requests, and accessibility metadata. Rendering produces a separate retained scene.</remarks>
public class ChartDocument : IChartDocument
{
    internal double Width;
    internal double Height;
    internal double DevicePixelRatio;
    internal ChartPadding Margins;
    internal ChartColor BackgroundColor;
    internal bool HasBackground;
    internal ChartTheme Theme;
    internal ChartStyle Style;
    internal ChartTextBlock Title;
    internal ChartTextBlock Subtitle;
    internal ChartTextBlock Caption;
    internal ChartAccessibilityMetadata Accessibility;
    internal ChartConfiguration Configuration;
    internal ChartGrid Grid;
    internal ChartPlot[] Plots;
    internal int PlotCount;
    internal ChartLayoutRequest[] LayoutRequests;
    internal int LayoutRequestCount;
    internal ChartLegend[] Legends;
    internal int LegendCount;
    internal ChartLegend[] Colorbars;
    internal int ColorbarCount;
    internal string[] CompositionAnnotationIds;
    internal string[] CompositionAnnotationTexts;
    internal ChartRenderStyle[] CompositionAnnotationStyles;
    internal int CompositionAnnotationCount;
    /// <summary>
    /// Initializes a document from the supplied width and height.
    /// </summary>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    public ChartDocument(double width, double height)
    {
        ValidateSize(width, height);
        Width = width;
        Height = height;
        DevicePixelRatio = 1.0;
        Margins = new ChartPadding(48.0, 48.0, 48.0, 48.0);
        BackgroundColor = ChartColor.White();
        HasBackground = true;
        Theme = ChartTheme.LibraryDefault();
        Style = new ChartStyle();
        Title = new ChartTextBlock();
        Subtitle = new ChartTextBlock();
        Caption = new ChartTextBlock();
        Accessibility = new ChartAccessibilityMetadata();
        Configuration = new ChartConfiguration();
        Grid = new ChartGrid(1, 1);
        Plots = new ChartPlot[4];
        PlotCount = 0;
        LayoutRequests = new ChartLayoutRequest[4];
        LayoutRequestCount = 0;
        Legends = new ChartLegend[2];
        LegendCount = 0;
        Colorbars = new ChartLegend[2];
        ColorbarCount = 0;
        CompositionAnnotationIds = new string[2];
        CompositionAnnotationTexts = new string[2];
        CompositionAnnotationStyles = new ChartRenderStyle[2];
        CompositionAnnotationCount = 0;
    }

    /// <summary>
    /// Sets the size configuration.
    /// </summary>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    public void SetSize(double width, double height)
    {
        ValidateSize(width, height);
        Width = width;
        Height = height;
    }

    /// <summary>Clamps a requested logical size to responsive minimum and maximum bounds.</summary>
    /// <param name = "targetWidth">The requested logical width.</param>
    /// <param name = "targetHeight">The requested logical height.</param>
    /// <param name = "minimumWidth">The minimum logical width.</param>
    /// <param name = "minimumHeight">The minimum logical height.</param>
    /// <param name = "maximumWidth">The maximum logical width.</param>
    /// <param name = "maximumHeight">The maximum logical height.</param>
    public void SetResponsiveTargetSize(double targetWidth, double targetHeight, double minimumWidth, double minimumHeight, double maximumWidth, double maximumHeight)
    {
        ValidateSize(targetWidth, targetHeight);
        ValidateSize(minimumWidth, minimumHeight);
        ValidateSize(maximumWidth, maximumHeight);
        if (minimumWidth > maximumWidth || minimumHeight > maximumHeight)
        {
            throw new System.ArgumentException("Responsive minimum sizes cannot exceed maximum sizes.");
        }

        Width = ClampSize(targetWidth, minimumWidth, maximumWidth);
        Height = ClampSize(targetHeight, minimumHeight, maximumHeight);
    }

    /// <summary>Sets the number of output device pixels per logical chart pixel.</summary>
    /// <param name = "devicePixelRatio">The positive device-pixel ratio.</param>
    public void SetDevicePixelRatio(double devicePixelRatio)
    {
        ChartRenderStyle.ValidatePositive(devicePixelRatio, "devicePixelRatio");
        DevicePixelRatio = devicePixelRatio;
    }

    /// <summary>
    /// Sets the margins configuration.
    /// </summary>
    /// <param name = "margins">The margins.</param>
    public void SetMargins(ChartPadding margins)
    {
        ChartPlot.ValidatePadding(margins);
        Margins = ChartPlot.CopyPadding(margins);
    }

    /// <summary>
    /// Sets the background configuration.
    /// </summary>
    /// <param name = "color">The color value to apply or encode.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void SetBackground(ChartColor color)
    {
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        BackgroundColor = color.Copy();
        HasBackground = !color.IsTransparent();
    }

    /// <summary>
    /// Sets the theme configuration.
    /// </summary>
    /// <param name = "theme">The theme.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void SetTheme(ChartTheme theme)
    {
        if (theme == null)
        {
            throw new System.ArgumentNullException("theme");
        }

        Theme = theme.Copy();
    }

    /// <summary>
    /// Sets the style configuration.
    /// </summary>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void SetStyle(ChartStyle style)
    {
        if (style == null)
        {
            throw new System.ArgumentNullException("style");
        }

        Style = style.Copy();
    }

    /// <summary>
    /// Sets the title configuration.
    /// </summary>
    /// <param name = "title">The title.</param>
    public void SetTitle(string title)
    {
        Title.SetText(title);
    }

    /// <summary>
    /// Sets the subtitle configuration.
    /// </summary>
    /// <param name = "subtitle">The subtitle.</param>
    public void SetSubtitle(string subtitle)
    {
        Subtitle.SetText(subtitle);
    }

    /// <summary>
    /// Sets the caption configuration.
    /// </summary>
    /// <param name = "caption">The caption.</param>
    public void SetCaption(string caption)
    {
        Caption.SetText(caption);
    }

    /// <summary>
    /// Sets the accessibility configuration.
    /// </summary>
    /// <param name = "accessibility">The accessibility.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void SetAccessibility(ChartAccessibilityMetadata accessibility)
    {
        if (accessibility == null)
        {
            throw new System.ArgumentNullException("accessibility");
        }

        Accessibility = accessibility.Copy();
    }

    /// <summary>Sets the chart-wide configuration used by configured rendering and export operations.</summary>
    /// <param name = "configuration">The chart configuration to copy into the document.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void SetConfiguration(ChartConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new System.ArgumentNullException("configuration");
        }

        Configuration = configuration.Copy();
    }

    /// <summary>
    /// Sets the accessibility title description configuration.
    /// </summary>
    /// <param name = "title">The title.</param>
    /// <param name = "description">The description.</param>
    public void SetAccessibilityTitleDescription(string title, string description)
    {
        Accessibility.SetTitleDescription(title, description);
    }

    /// <summary>
    /// Sets the grid configuration.
    /// </summary>
    /// <param name = "grid">The grid.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void SetGrid(ChartGrid grid)
    {
        if (grid == null)
        {
            throw new System.ArgumentNullException("grid");
        }

        Grid = grid.Copy();
    }

    /// <summary>
    /// Adds the supplied plot to this object.
    /// </summary>
    /// <param name = "plot">The plot whose bounds, axes, and styles are processed.</param>
    /// <returns>The resulting plot.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartPlot AddPlot(ChartPlot plot)
    {
        if (plot == null)
        {
            throw new System.ArgumentNullException("plot");
        }

        if (ContainsPlot(plot.Id))
        {
            throw new System.ArgumentException("A plot with the same id already exists.");
        }

        EnsurePlotCapacity(PlotCount + 1);
        Plots[PlotCount] = plot.Copy();
        PlotCount = PlotCount + 1;
        return Plots[PlotCount - 1];
    }

    /// <summary>
    /// Adds the supplied plot to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <returns>The resulting plot.</returns>
    public ChartPlot AddPlotById(string id)
    {
        return AddPlot(new ChartPlot(id));
    }

    /// <summary>
    /// Adds the supplied plot to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "bounds">The rectangle that constrains the generated layout or drawing.</param>
    /// <returns>The resulting plot.</returns>
    public ChartPlot AddPlotWithBounds(string id, ChartRect bounds)
    {
        ChartPlot plot = new ChartPlot(id);
        plot.SetBounds(bounds);
        return AddPlot(plot);
    }

    /// <summary>
    /// Adds the supplied grid plot to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "row">The row.</param>
    /// <param name = "column">The column.</param>
    /// <param name = "rowSpan">The row span.</param>
    /// <param name = "columnSpan">The column span.</param>
    /// <returns>The resulting plot.</returns>
    public ChartPlot AddGridPlot(string id, int row, int column, int rowSpan, int columnSpan)
    {
        ChartPlot plot = new ChartPlot(id);
        plot.SetGridPosition(row, column, rowSpan, columnSpan);
        return AddPlot(plot);
    }

    /// <summary>Configures the grid used to arrange dashboard panels.</summary>
    /// <param name = "rows">The positive number of dashboard rows.</param>
    /// <param name = "columns">The positive number of dashboard columns.</param>
    /// <param name = "rowGap">The nonnegative gap between rows.</param>
    /// <param name = "columnGap">The nonnegative gap between columns.</param>
    public void ConfigureDashboard(int rows, int columns, double rowGap, double columnGap)
    {
        ChartGrid grid = new ChartGrid(rows, columns);
        grid.SetGaps(rowGap, columnGap);
        Grid = grid;
    }

    /// <summary>Adds a dashboard panel occupying one or more configured grid cells.</summary>
    /// <param name = "id">A stable panel identifier.</param>
    /// <param name = "row">The zero-based starting row.</param>
    /// <param name = "column">The zero-based starting column.</param>
    /// <param name = "rowSpan">The positive row span.</param>
    /// <param name = "columnSpan">The positive column span.</param>
    /// <returns>The added dashboard plot.</returns>
    public ChartPlot AddDashboardPanel(string id, int row, int column, int rowSpan, int columnSpan)
    {
        ChartPlot.ValidateGridPosition(row, column, rowSpan, columnSpan);
        if (rowSpan > Grid.Rows || row > Grid.Rows - rowSpan)
        {
            throw new System.ArgumentOutOfRangeException("rowSpan");
        }

        if (columnSpan > Grid.Columns || column > Grid.Columns - columnSpan)
        {
            throw new System.ArgumentOutOfRangeException("columnSpan");
        }

        return AddGridPlot(id, row, column, rowSpan, columnSpan);
    }

    /// <summary>Adds legend content and its docked or overlay layout request.</summary>
    /// <param name = "legend">The legend content to copy.</param>
    /// <param name = "side">The side used to dock or align the legend.</param>
    /// <param name = "width">The requested width, or zero to fill the available width.</param>
    /// <param name = "height">The requested height, or zero to fill the available height.</param>
    /// <param name = "spacing">The nonnegative spacing from adjacent content.</param>
    /// <param name = "overlay">Whether the legend overlays the plot area.</param>
    /// <returns>The copied stored legend.</returns>
    public ChartLegend AddLegend(ChartLegend legend, ChartSide side, double width, double height, double spacing, bool overlay)
    {
        if (legend == null)
        {
            throw new System.ArgumentNullException("legend");
        }

        ValidateGuideId(legend.Id);
        EnsureLegendCapacity(LegendCount + 1);
        Legends[LegendCount] = legend.Copy();
        LegendCount = LegendCount + 1;
        if (overlay)
        {
            AddOverlayLegendLayout(legend.Id, side, width, height, spacing);
        }
        else
        {
            AddLegendLayout(legend.Id, side, width, height, spacing);
        }

        return Legends[LegendCount - 1];
    }

    /// <summary>Adds gradient colorbar content and its docked or overlay layout request.</summary>
    /// <param name = "colorbar">The gradient legend used as colorbar content.</param>
    /// <param name = "side">The side used to dock or align the colorbar.</param>
    /// <param name = "width">The requested width, or zero to fill the available width.</param>
    /// <param name = "height">The requested height, or zero to fill the available height.</param>
    /// <param name = "spacing">The nonnegative spacing from adjacent content.</param>
    /// <param name = "overlay">Whether the colorbar overlays the plot area.</param>
    /// <returns>The copied stored colorbar.</returns>
    public ChartLegend AddColorbar(ChartLegend colorbar, ChartSide side, double width, double height, double spacing, bool overlay)
    {
        if (colorbar == null)
        {
            throw new System.ArgumentNullException("colorbar");
        }

        ValidateGuideId(colorbar.Id);
        EnsureColorbarCapacity(ColorbarCount + 1);
        Colorbars[ColorbarCount] = colorbar.Copy();
        ColorbarCount = ColorbarCount + 1;
        if (overlay)
        {
            AddOverlayColorbarLayout(colorbar.Id, side, width, height, spacing);
        }
        else
        {
            AddColorbarLayout(colorbar.Id, side, width, height, spacing);
        }

        return Colorbars[ColorbarCount - 1];
    }

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
    public ChartLayoutRequest AddCompositionAnnotation(string id, string text, ChartSide side, double width, double height, double spacing, bool overlay, ChartRenderStyle style)
    {
        ChartPlot.ValidateId(id);
        ChartTextBlock.ValidateText(text, "text");
        ChartLineAreaChartsOperations.ValidateStyle(style);
        ValidateGuideId(id);
        EnsureCompositionAnnotationCapacity(CompositionAnnotationCount + 1);
        CompositionAnnotationIds[CompositionAnnotationCount] = id;
        CompositionAnnotationTexts[CompositionAnnotationCount] = text;
        CompositionAnnotationStyles[CompositionAnnotationCount] = style.Copy();
        CompositionAnnotationCount = CompositionAnnotationCount + 1;
        return AddAnnotationLayout(id, side, width, height, spacing, overlay);
    }

    /// <summary>Lays out and renders all retained document content through one composition workflow.</summary>
    /// <returns>A retained scene containing the complete document.</returns>
    public ChartRenderScene RenderComposition()
    {
        return ChartDocumentCompositionOperations.Render(this);
    }

    /// <summary>Returns one stored legend.</summary>
    /// <param name = "index">The zero-based legend index.</param>
    /// <returns>The stored legend.</returns>
    public ChartLegend LegendAt(int index)
    {
        if (index < 0 || index >= LegendCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Legends[index];
    }

    /// <summary>Returns one stored colorbar.</summary>
    /// <param name = "index">The zero-based colorbar index.</param>
    /// <returns>The stored colorbar.</returns>
    public ChartLegend ColorbarAt(int index)
    {
        if (index < 0 || index >= ColorbarCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Colorbars[index];
    }

    /// <summary>
    /// Creates the requested scene from the supplied data and configuration.
    /// </summary>
    /// <returns>The newly created render scene.</returns>
    public ChartRenderScene CreateScene()
    {
        int pixelWidth = ScaleDevicePixels(Width, "width");
        int pixelHeight = ScaleDevicePixels(Height, "height");
        ChartRenderScene scene = new ChartRenderScene(pixelWidth, pixelHeight);
        if (HasBackground)
        {
            scene.AddClear(BackgroundColor);
        }

        return scene;
    }

    /// <summary>
    /// Renders chart content into a new scene sized and initialized by this document.
    /// </summary>
    /// <param name = "renderer">The renderer that supplies drawing operations.</param>
    /// <returns>The newly created render scene.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartRenderScene Render(IChartRenderer renderer)
    {
        if (renderer == null)
        {
            throw new System.ArgumentNullException("renderer");
        }

        ChartRenderScene scene = CreateScene();
        renderer.RenderTo(scene);
        return scene;
    }

    internal SvgWriter CreateSvgWriter()
    {
        SvgWriter writer = new SvgWriter(ChartRenderScene.RoundToInt(Width), ChartRenderScene.RoundToInt(Height));
        Accessibility.ApplyToSvgWriter(writer);
        return writer;
    }

    /// <summary>
    /// Applies the export scene svg operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <returns>The resulting string.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public string ExportSceneSvg(ChartRenderScene scene)
    {
        if (scene == null)
        {
            throw new System.ArgumentNullException("scene");
        }

        SvgWriter writer = new SvgWriter(scene.Width, scene.Height);
        Accessibility.ApplyToSvgWriter(writer);
        scene.RenderSvg(writer);
        return writer.ToSvgString();
    }

    /// <summary>
    /// Applies the export scene png operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <returns>The resulting byte array.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public byte[] ExportScenePng(ChartRenderScene scene)
    {
        if (scene == null)
        {
            throw new System.ArgumentNullException("scene");
        }

        return ChartExportOperations.ToPngBytes(scene.ToRawBitmap());
    }

    /// <summary>Exports a retained scene using this document's chart-wide configuration and accessibility metadata.</summary>
    /// <param name = "scene">The retained scene to render and encode.</param>
    /// <returns>The configured output bytes.</returns>
    public byte[] ExportConfiguredScene(ChartRenderScene scene)
    {
        return Configuration.ExportScene(scene, Accessibility);
    }

    /// <summary>
    /// Renders cartesian using the supplied data and configuration.
    /// </summary>
    /// <param name = "plotId">The plot id.</param>
    /// <param name = "series">The chart series to render, validate, or combine.</param>
    /// <param name = "xDomain">The x domain.</param>
    /// <param name = "yDomain">The y domain.</param>
    /// <returns>The resulting render scene.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public ChartRenderScene RenderCartesian(string plotId, ChartCartesianSeries[] series, ChartRange xDomain, ChartRange yDomain)
    {
        ChartPlot.ValidateId(plotId);
        ValidateSeriesArray(series);
        int plotIndex = PlotIndex(plotId);
        if (plotIndex < 0)
        {
            throw new System.ArgumentException("Plot was not found.");
        }

        ChartPlot plot = PlotAt(plotIndex);
        ChartCartesianRenderer renderer = plot.CreateCartesianRenderer(xDomain, yDomain);
        int index = 0;
        while (index < series.Length)
        {
            renderer.AddSeries(series[index]);
            index = index + 1;
        }

        return Render(renderer);
    }

    /// <summary>
    /// Renders cartesian svg using the supplied data and configuration.
    /// </summary>
    /// <param name = "plotId">The plot id.</param>
    /// <param name = "series">The chart series to render, validate, or combine.</param>
    /// <param name = "xDomain">The x domain.</param>
    /// <param name = "yDomain">The y domain.</param>
    /// <returns>The resulting string.</returns>
    public string RenderCartesianSvg(string plotId, ChartCartesianSeries[] series, ChartRange xDomain, ChartRange yDomain)
    {
        return ExportSceneSvg(RenderCartesian(plotId, series, xDomain, yDomain));
    }

    /// <summary>
    /// Adds the supplied legend layout to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "side">The side.</param>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    /// <param name = "spacing">The spacing.</param>
    /// <returns>The resulting layout request.</returns>
    public ChartLayoutRequest AddLegendLayout(string id, ChartSide side, double width, double height, double spacing)
    {
        ChartLayoutRequest request = ChartLayoutRequest.CreateLegend(id, side, width, height, spacing);
        return AddLayoutRequest(request);
    }

    /// <summary>
    /// Adds the supplied colorbar layout to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "side">The side.</param>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    /// <param name = "spacing">The spacing.</param>
    /// <returns>The resulting layout request.</returns>
    public ChartLayoutRequest AddColorbarLayout(string id, ChartSide side, double width, double height, double spacing)
    {
        ChartLayoutRequest request = ChartLayoutRequest.CreateColorbar(id, side, width, height, spacing);
        return AddLayoutRequest(request);
    }

    /// <summary>Adds a legend over the plot area without reserving layout space.</summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "side">The side used to align the overlay.</param>
    /// <param name = "width">The overlay width, or zero to fill the available width.</param>
    /// <param name = "height">The overlay height, or zero to fill the available height.</param>
    /// <param name = "spacing">The retained spacing metadata.</param>
    /// <returns>The resulting overlay layout request.</returns>
    public ChartLayoutRequest AddOverlayLegendLayout(string id, ChartSide side, double width, double height, double spacing)
    {
        return AddLayoutRequest(ChartLayoutRequest.CreateOverlayLegend(id, side, width, height, spacing));
    }

    /// <summary>Adds a colorbar over the plot area without reserving layout space.</summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "side">The side used to align the overlay.</param>
    /// <param name = "width">The overlay width, or zero to fill the available width.</param>
    /// <param name = "height">The overlay height, or zero to fill the available height.</param>
    /// <param name = "spacing">The retained spacing metadata.</param>
    /// <returns>The resulting overlay layout request.</returns>
    public ChartLayoutRequest AddOverlayColorbarLayout(string id, ChartSide side, double width, double height, double spacing)
    {
        return AddLayoutRequest(ChartLayoutRequest.CreateOverlayColorbar(id, side, width, height, spacing));
    }

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
    public ChartLayoutRequest AddAnnotationLayout(string id, ChartSide side, double width, double height, double spacing, bool overlay)
    {
        ChartLayoutRequest request = ChartLayoutRequest.CreateAnnotation(id, side, width, height, spacing, overlay);
        return AddLayoutRequest(request);
    }

    /// <summary>
    /// Adds the supplied layout request to this object.
    /// </summary>
    /// <param name = "request">The request.</param>
    /// <returns>The resulting layout request.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartLayoutRequest AddLayoutRequest(ChartLayoutRequest request)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }

        EnsureLayoutRequestCapacity(LayoutRequestCount + 1);
        LayoutRequests[LayoutRequestCount] = request.Copy();
        LayoutRequestCount = LayoutRequestCount + 1;
        return LayoutRequests[LayoutRequestCount - 1];
    }

    /// <summary>
    /// Computes the layout for request at within the supplied bounds.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting layout request.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartLayoutRequest LayoutRequestAt(int index)
    {
        if (index < 0 || index >= LayoutRequestCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return LayoutRequests[index];
    }

    /// <summary>
    /// Creates the requested small multiples from the supplied data and configuration.
    /// </summary>
    /// <param name = "idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name = "rows">The rows.</param>
    /// <param name = "columns">The columns.</param>
    public void CreateSmallMultiples(string idPrefix, int rows, int columns)
    {
        ChartPlot.ValidateId(idPrefix);
        ChartGrid.ValidateDimensions(rows, columns);
        Grid = new ChartGrid(rows, columns);
        int row = 0;
        int index = 0;
        while (row < rows)
        {
            int column = 0;
            while (column < columns)
            {
                ChartPlot plot = new ChartPlot(idPrefix + "-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture));
                plot.SetGridPosition(row, column, 1, 1);
                AddPlot(plot);
                index = index + 1;
                column = column + 1;
            }

            row = row + 1;
        }
    }

    /// <summary>Creates one facet plot per row.</summary>
    /// <param name = "idPrefix">The prefix used for stable facet identifiers.</param>
    /// <param name = "count">The positive number of facets.</param>
    public void CreateRowFacets(string idPrefix, int count)
    {
        CreateFacets(idPrefix, count, count, 1);
    }

    /// <summary>Creates one facet plot per column.</summary>
    /// <param name = "idPrefix">The prefix used for stable facet identifiers.</param>
    /// <param name = "count">The positive number of facets.</param>
    public void CreateColumnFacets(string idPrefix, int count)
    {
        CreateFacets(idPrefix, count, 1, count);
    }

    /// <summary>Creates facets that wrap across a fixed number of columns.</summary>
    /// <param name = "idPrefix">The prefix used for stable facet identifiers.</param>
    /// <param name = "count">The positive number of facets.</param>
    /// <param name = "columns">The positive number of columns before wrapping.</param>
    public void CreateWrapFacets(string idPrefix, int count, int columns)
    {
        if (columns <= 0)
        {
            throw new System.ArgumentOutOfRangeException("columns");
        }

        int rows = (count - 1) / columns + 1;
        CreateFacets(idPrefix, count, rows, columns);
    }

    /// <summary>
    /// Determines whether this value contains plot.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool ContainsPlot(string id)
    {
        return PlotIndex(id) >= 0;
    }

    /// <summary>
    /// Returns the zero-based plot index.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <returns>The resulting int.</returns>
    public int PlotIndex(string id)
    {
        ChartPlot.ValidateId(id);
        int index = 0;
        while (index < PlotCount)
        {
            if (Plots[index].Id == id)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    /// <summary>
    /// Returns the requested plot.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting plot.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartPlot PlotAt(int index)
    {
        if (index < 0 || index >= PlotCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Plots[index];
    }

    /// <summary>
    /// Applies the figure rect operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting rect.</returns>
    public ChartRect FigureRect()
    {
        return new ChartRect(0.0, 0.0, Width, Height);
    }

    /// <summary>
    /// Applies the content rect operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting rect.</returns>
    public ChartRect ContentRect()
    {
        return Margins.DeflateRect(FigureRect());
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartDocument Copy()
    {
        ChartDocument document = new ChartDocument(Width, Height);
        document.DevicePixelRatio = DevicePixelRatio;
        document.Margins = ChartPlot.CopyPadding(Margins);
        document.BackgroundColor = BackgroundColor.Copy();
        document.HasBackground = HasBackground;
        document.Theme = Theme.Copy();
        document.Style = Style.Copy();
        document.Title = Title.Copy();
        document.Subtitle = Subtitle.Copy();
        document.Caption = Caption.Copy();
        document.Accessibility = Accessibility.Copy();
        document.Configuration = Configuration.Copy();
        document.Grid = Grid.Copy();
        document.EnsurePlotCapacity(PlotCount);
        int index = 0;
        while (index < PlotCount)
        {
            document.Plots[index] = Plots[index].Copy();
            index = index + 1;
        }

        document.PlotCount = PlotCount;
        document.EnsureLayoutRequestCapacity(LayoutRequestCount);
        index = 0;
        while (index < LayoutRequestCount)
        {
            document.LayoutRequests[index] = LayoutRequests[index].Copy();
            index = index + 1;
        }

        document.LayoutRequestCount = LayoutRequestCount;
        document.EnsureLegendCapacity(LegendCount);
        index = 0;
        while (index < LegendCount)
        {
            document.Legends[index] = Legends[index].Copy();
            index = index + 1;
        }

        document.LegendCount = LegendCount;
        document.EnsureColorbarCapacity(ColorbarCount);
        index = 0;
        while (index < ColorbarCount)
        {
            document.Colorbars[index] = Colorbars[index].Copy();
            index = index + 1;
        }

        document.ColorbarCount = ColorbarCount;
        document.EnsureCompositionAnnotationCapacity(CompositionAnnotationCount);
        index = 0;
        while (index < CompositionAnnotationCount)
        {
            document.CompositionAnnotationIds[index] = CompositionAnnotationIds[index];
            document.CompositionAnnotationTexts[index] = CompositionAnnotationTexts[index];
            document.CompositionAnnotationStyles[index] = CompositionAnnotationStyles[index].Copy();
            index = index + 1;
        }

        document.CompositionAnnotationCount = CompositionAnnotationCount;
        return document;
    }

    internal void CreateFacets(string idPrefix, int count, int rows, int columns)
    {
        ChartPlot.ValidateId(idPrefix);
        if (count <= 0)
        {
            throw new System.ArgumentOutOfRangeException("count");
        }

        Grid = new ChartGrid(rows, columns);
        int index = 0;
        while (index < count)
        {
            int row = index / columns;
            int column = index - row * columns;
            ChartPlot plot = new ChartPlot(idPrefix + "-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture));
            plot.SetGridPosition(row, column, 1, 1);
            AddPlot(plot);
            index = index + 1;
        }
    }

    internal int ScaleDevicePixels(double logicalSize, string name)
    {
        double scaled = System.Math.Round(logicalSize * DevicePixelRatio);
        if (scaled < 1.0 || scaled > int.MaxValue)
        {
            throw new System.ArgumentOutOfRangeException(name);
        }

        return (int)scaled;
    }

    internal static double ClampSize(double value, double minimum, double maximum)
    {
        if (value < minimum)
        {
            return minimum;
        }

        if (value > maximum)
        {
            return maximum;
        }

        return value;
    }

    internal void EnsurePlotCapacity(int capacity)
    {
        if (capacity <= Plots.Length)
        {
            return;
        }

        int newCapacity = ChartArrayUtilities.GrowthCapacity(Plots.Length, capacity);
        ChartPlot[] plots = new ChartPlot[newCapacity];
        System.Array.Copy(Plots, plots, PlotCount);
        Plots = plots;
    }

    internal void EnsureLayoutRequestCapacity(int capacity)
    {
        if (capacity <= LayoutRequests.Length)
        {
            return;
        }

        int newCapacity = ChartArrayUtilities.GrowthCapacity(LayoutRequests.Length, capacity);
        ChartLayoutRequest[] requests = new ChartLayoutRequest[newCapacity];
        System.Array.Copy(LayoutRequests, requests, LayoutRequestCount);
        LayoutRequests = requests;
    }

    internal void EnsureLegendCapacity(int capacity)
    {
        if (capacity <= Legends.Length)
        {
            return;
        }

        int newCapacity = ChartArrayUtilities.GrowthCapacity(Legends.Length, capacity);
        ChartLegend[] legends = new ChartLegend[newCapacity];
        System.Array.Copy(Legends, legends, LegendCount);
        Legends = legends;
    }

    internal void EnsureColorbarCapacity(int capacity)
    {
        if (capacity <= Colorbars.Length)
        {
            return;
        }

        int newCapacity = ChartArrayUtilities.GrowthCapacity(Colorbars.Length, capacity);
        ChartLegend[] colorbars = new ChartLegend[newCapacity];
        System.Array.Copy(Colorbars, colorbars, ColorbarCount);
        Colorbars = colorbars;
    }

    internal void EnsureCompositionAnnotationCapacity(int capacity)
    {
        if (capacity <= CompositionAnnotationIds.Length)
        {
            return;
        }

        int newCapacity = ChartArrayUtilities.GrowthCapacity(CompositionAnnotationIds.Length, capacity);
        string[] ids = new string[newCapacity];
        string[] texts = new string[newCapacity];
        ChartRenderStyle[] styles = new ChartRenderStyle[newCapacity];
        System.Array.Copy(CompositionAnnotationIds, ids, CompositionAnnotationCount);
        System.Array.Copy(CompositionAnnotationTexts, texts, CompositionAnnotationCount);
        System.Array.Copy(CompositionAnnotationStyles, styles, CompositionAnnotationCount);
        CompositionAnnotationIds = ids;
        CompositionAnnotationTexts = texts;
        CompositionAnnotationStyles = styles;
    }

    internal void ValidateGuideId(string id)
    {
        int index = 0;
        while (index < LayoutRequestCount)
        {
            if (LayoutRequests[index].Id == id)
            {
                throw new System.ArgumentException("A guide or annotation with the same id already exists.");
            }

            index = index + 1;
        }
    }

    internal static void ValidateSize(double width, double height)
    {
        ChartLinearScale.ValidateFinite(width, "width");
        ChartLinearScale.ValidateFinite(height, "height");
        if (width <= 0.0)
        {
            throw new System.ArgumentOutOfRangeException("width");
        }

        if (height <= 0.0)
        {
            throw new System.ArgumentOutOfRangeException("height");
        }
    }

    internal static void ValidateSeriesArray(ChartCartesianSeries[] series)
    {
        if (series == null)
        {
            throw new System.ArgumentNullException("series");
        }

        int index = 0;
        while (index < series.Length)
        {
            if (series[index] == null)
            {
                throw new System.ArgumentException("Series array cannot contain null values.");
            }

            index = index + 1;
        }
    }

    /// <summary>Returns the current width.</summary>
    /// <returns>The current field value.</returns>
    public double GetWidth()
    {
        return Width;
    }

    /// <summary>Replaces the current width.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetWidth(double value)
    {
        Width = value;
    }

    /// <summary>Returns the current height.</summary>
    /// <returns>The current field value.</returns>
    public double GetHeight()
    {
        return Height;
    }

    /// <summary>Returns the current device-pixel ratio.</summary>
    /// <returns>The positive output device-pixel ratio.</returns>
    public double GetDevicePixelRatio()
    {
        return DevicePixelRatio;
    }

    /// <summary>Replaces the current height.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetHeight(double value)
    {
        Height = value;
    }

    /// <summary>Returns the current margins.</summary>
    /// <returns>The current field value.</returns>
    public ChartPadding GetMargins()
    {
        return Margins;
    }

    /// <summary>Returns the current background color.</summary>
    /// <returns>The current field value.</returns>
    public ChartColor GetBackgroundColor()
    {
        return BackgroundColor;
    }

    /// <summary>Replaces the current background color.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetBackgroundColor(ChartColor value)
    {
        BackgroundColor = value;
    }

    /// <summary>Returns the current has background.</summary>
    /// <returns>The current field value.</returns>
    public bool GetHasBackground()
    {
        return HasBackground;
    }

    /// <summary>Replaces the current has background.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetHasBackground(bool value)
    {
        HasBackground = value;
    }

    /// <summary>Returns the current theme.</summary>
    /// <returns>The current field value.</returns>
    public ChartTheme GetTheme()
    {
        return Theme;
    }

    /// <summary>Returns the current style.</summary>
    /// <returns>The current field value.</returns>
    public ChartStyle GetStyle()
    {
        return Style;
    }

    /// <summary>Returns the current title.</summary>
    /// <returns>The current field value.</returns>
    public ChartTextBlock GetTitle()
    {
        return Title;
    }

    /// <summary>Returns the current subtitle.</summary>
    /// <returns>The current field value.</returns>
    public ChartTextBlock GetSubtitle()
    {
        return Subtitle;
    }

    /// <summary>Returns the current caption.</summary>
    /// <returns>The current field value.</returns>
    public ChartTextBlock GetCaption()
    {
        return Caption;
    }

    /// <summary>Returns the current accessibility.</summary>
    /// <returns>The current field value.</returns>
    public ChartAccessibilityMetadata GetAccessibility()
    {
        return Accessibility;
    }

    /// <summary>Returns an independent copy of the chart-wide configuration.</summary>
    /// <returns>A configuration that can be modified without changing this document.</returns>
    public ChartConfiguration GetConfiguration()
    {
        return Configuration.Copy();
    }

    /// <summary>Returns the current grid.</summary>
    /// <returns>The current field value.</returns>
    public ChartGrid GetGrid()
    {
        return Grid;
    }

    /// <summary>Returns the current plots.</summary>
    /// <returns>The current field value.</returns>
    public ChartPlot[] GetPlots()
    {
        return Plots;
    }

    /// <summary>Replaces the current plots.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetPlots(ChartPlot[] value)
    {
        Plots = value;
    }

    /// <summary>Returns the current plot count.</summary>
    /// <returns>The current field value.</returns>
    public int GetPlotCount()
    {
        return PlotCount;
    }

    /// <summary>Replaces the current plot count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetPlotCount(int value)
    {
        PlotCount = value;
    }

    /// <summary>Returns the current layout requests.</summary>
    /// <returns>The current field value.</returns>
    public ChartLayoutRequest[] GetLayoutRequests()
    {
        return LayoutRequests;
    }

    /// <summary>Replaces the current layout requests.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetLayoutRequests(ChartLayoutRequest[] value)
    {
        LayoutRequests = value;
    }

    /// <summary>Returns the current layout request count.</summary>
    /// <returns>The current field value.</returns>
    public int GetLayoutRequestCount()
    {
        return LayoutRequestCount;
    }

    /// <summary>Replaces the current layout request count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetLayoutRequestCount(int value)
    {
        LayoutRequestCount = value;
    }
}
