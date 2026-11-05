namespace chart;
/// <summary>
/// Represents a plot used by the chart model or rendering pipeline.
/// </summary>
public class ChartPlot : IChartPlot
{
    internal string Id;
    internal ChartRect Bounds;
    internal ChartPadding Margins;
    internal ChartColor BackgroundColor;
    internal bool HasBackground;
    internal ChartStyle Style;
    internal ChartAxis[] Axes;
    internal int AxisCount;
    internal ChartCartesianSeries[] Series;
    internal int SeriesCount;
    internal ChartCartesianSeries[] AnnotationSeries;
    internal int AnnotationSeriesCount;
    internal ChartRange XDomain;
    internal ChartRange YDomain;
    internal bool HasCartesianDomains;
    internal bool HasGridPosition;
    internal int GridRow;
    internal int GridColumn;
    internal int RowSpan;
    internal int ColumnSpan;
    internal bool IsInset;
    internal string InsetParentId;
    internal ChartRect InsetBounds;
    internal string ShareXAxisGroup;
    internal string ShareYAxisGroup;
    internal bool HasFixedAspectRatio;
    internal double FixedAspectRatio;
    internal ChartPoint TernaryFirstVertex;
    internal ChartPoint TernarySecondVertex;
    internal ChartPoint TernaryThirdVertex;
    internal bool ClipToPlotArea;
    /// <summary>
    /// Initializes a plot from the supplied id.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    public ChartPlot(string id)
    {
        ValidateId(id);
        Id = id;
        Bounds = new ChartRect(0.0, 0.0, 1.0, 1.0);
        Margins = new ChartPadding(0.0, 0.0, 0.0, 0.0);
        BackgroundColor = ChartColor.Transparent();
        HasBackground = false;
        Style = new ChartStyle();
        Axes = new ChartAxis[4];
        AxisCount = 0;
        Series = new ChartCartesianSeries[4];
        SeriesCount = 0;
        AnnotationSeries = new ChartCartesianSeries[2];
        AnnotationSeriesCount = 0;
        XDomain = new ChartRange(0.0, 1.0);
        YDomain = new ChartRange(0.0, 1.0);
        HasCartesianDomains = false;
        HasGridPosition = false;
        GridRow = 0;
        GridColumn = 0;
        RowSpan = 1;
        ColumnSpan = 1;
        IsInset = false;
        InsetParentId = "";
        InsetBounds = new ChartRect(0.0, 0.0, 1.0, 1.0);
        ShareXAxisGroup = "";
        ShareYAxisGroup = "";
        HasFixedAspectRatio = false;
        FixedAspectRatio = 1.0;
        TernaryFirstVertex = new ChartPoint(0.0, 1.0);
        TernarySecondVertex = new ChartPoint(1.0, 1.0);
        TernaryThirdVertex = new ChartPoint(0.5, 0.0);
        ClipToPlotArea = true;
    }

    /// <summary>
    /// Sets the bounds configuration.
    /// </summary>
    /// <param name = "bounds">The rectangle that constrains the generated layout or drawing.</param>
    public void SetBounds(ChartRect bounds)
    {
        ChartAxis.ValidatePlotArea(bounds);
        Bounds = CopyRect(bounds);
    }

    /// <summary>
    /// Sets the margins configuration.
    /// </summary>
    /// <param name = "margins">The margins.</param>
    public void SetMargins(ChartPadding margins)
    {
        ValidatePadding(margins);
        Margins = CopyPadding(margins);
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

    /// <summary>Sets the cartesian domains used by document composition rendering.</summary>
    /// <param name = "xDomain">The horizontal data domain.</param>
    /// <param name = "yDomain">The vertical data domain.</param>
    public void SetCartesianDomains(ChartRange xDomain, ChartRange yDomain)
    {
        ChartAxis.ValidateDomain(xDomain);
        ChartAxis.ValidateDomain(yDomain);
        XDomain = new ChartRange(xDomain.Minimum, xDomain.Maximum);
        YDomain = new ChartRange(yDomain.Minimum, yDomain.Maximum);
        HasCartesianDomains = true;
    }

    /// <summary>Adds a copied cartesian series to this plot's composition content.</summary>
    /// <param name = "series">The chart series to render.</param>
    /// <returns>The copied stored series.</returns>
    public ChartCartesianSeries AddSeries(ChartCartesianSeries series)
    {
        if (series == null)
        {
            throw new System.ArgumentNullException("series");
        }

        EnsureSeriesCapacity(SeriesCount + 1);
        Series[SeriesCount] = series.Copy();
        SeriesCount = SeriesCount + 1;
        return Series[SeriesCount - 1];
    }

    /// <summary>Adds a copied cartesian annotation series rendered above this plot's axes and data.</summary>
    /// <param name = "series">The annotation series to render.</param>
    /// <returns>The copied stored annotation series.</returns>
    public ChartCartesianSeries AddAnnotationSeries(ChartCartesianSeries series)
    {
        if (series == null)
        {
            throw new System.ArgumentNullException("series");
        }

        EnsureAnnotationSeriesCapacity(AnnotationSeriesCount + 1);
        AnnotationSeries[AnnotationSeriesCount] = series.Copy();
        AnnotationSeriesCount = AnnotationSeriesCount + 1;
        return AnnotationSeries[AnnotationSeriesCount - 1];
    }

    /// <summary>Returns one stored composition series.</summary>
    /// <param name = "index">The zero-based series index.</param>
    /// <returns>The stored series.</returns>
    public ChartCartesianSeries SeriesAt(int index)
    {
        if (index < 0 || index >= SeriesCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Series[index];
    }

    /// <summary>Returns one stored annotation series.</summary>
    /// <param name = "index">The zero-based annotation index.</param>
    /// <returns>The stored annotation series.</returns>
    public ChartCartesianSeries AnnotationSeriesAt(int index)
    {
        if (index < 0 || index >= AnnotationSeriesCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return AnnotationSeries[index];
    }

    /// <summary>
    /// Adds the supplied axis to this object.
    /// </summary>
    /// <param name = "axis">The axis.</param>
    /// <returns>The resulting axis.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartAxis AddAxis(ChartAxis axis)
    {
        if (axis == null)
        {
            throw new System.ArgumentNullException("axis");
        }

        EnsureAxisCapacity(AxisCount + 1);
        Axes[AxisCount] = axis.Copy();
        AxisCount = AxisCount + 1;
        return Axes[AxisCount - 1];
    }

    /// <summary>
    /// Adds the supplied bottom axis to this object.
    /// </summary>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    /// <param name = "tickCount">The requested number of tick entries.</param>
    /// <returns>The resulting axis.</returns>
    public ChartAxis AddBottomAxis(double minimum, double maximum, int tickCount)
    {
        return AddNumericAxis(ChartAxisPlacement.Bottom, minimum, maximum, tickCount);
    }

    /// <summary>
    /// Adds the supplied top axis to this object.
    /// </summary>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    /// <param name = "tickCount">The requested number of tick entries.</param>
    /// <returns>The resulting axis.</returns>
    public ChartAxis AddTopAxis(double minimum, double maximum, int tickCount)
    {
        return AddNumericAxis(ChartAxisPlacement.Top, minimum, maximum, tickCount);
    }

    /// <summary>
    /// Adds the supplied left axis to this object.
    /// </summary>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    /// <param name = "tickCount">The requested number of tick entries.</param>
    /// <returns>The resulting axis.</returns>
    public ChartAxis AddLeftAxis(double minimum, double maximum, int tickCount)
    {
        return AddNumericAxis(ChartAxisPlacement.Left, minimum, maximum, tickCount);
    }

    /// <summary>
    /// Adds the supplied right axis to this object.
    /// </summary>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    /// <param name = "tickCount">The requested number of tick entries.</param>
    /// <returns>The resulting axis.</returns>
    public ChartAxis AddRightAxis(double minimum, double maximum, int tickCount)
    {
        return AddNumericAxis(ChartAxisPlacement.Right, minimum, maximum, tickCount);
    }

    /// <summary>
    /// Adds the supplied numeric axis to this object.
    /// </summary>
    /// <param name = "placement">The placement.</param>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    /// <param name = "tickCount">The requested number of tick entries.</param>
    /// <returns>The resulting axis.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public ChartAxis AddNumericAxis(ChartAxisPlacement placement, double minimum, double maximum, int tickCount)
    {
        ChartRange domain = ChartScaleUtilities.NiceDomain(minimum, maximum, tickCount);
        ChartTick[] ticks = ChartTickGeneratorOperations.GenerateNumericTicks(minimum, maximum, tickCount);
        ChartRect plotArea = InnerBounds();
        if (placement == ChartAxisPlacement.Bottom)
        {
            return AddAxis(ChartAxis.CreateBottom(ticks, domain, plotArea));
        }

        if (placement == ChartAxisPlacement.Top)
        {
            return AddAxis(ChartAxis.CreateTop(ticks, domain, plotArea));
        }

        if (placement == ChartAxisPlacement.Left)
        {
            return AddAxis(ChartAxis.CreateLeft(ticks, domain, plotArea));
        }

        if (placement == ChartAxisPlacement.Right)
        {
            return AddAxis(ChartAxis.CreateRight(ticks, domain, plotArea));
        }

        throw new System.ArgumentException("Numeric axis placement must be bottom, top, left, or right.");
    }

    /// <summary>
    /// Creates the requested cartesian transform from the supplied data and configuration.
    /// </summary>
    /// <param name = "xDomain">The x domain.</param>
    /// <param name = "yDomain">The y domain.</param>
    /// <returns>The newly created cartesian transform.</returns>
    public ChartCartesianTransform CreateCartesianTransform(ChartRange xDomain, ChartRange yDomain)
    {
        ChartCartesianTransform transform = new ChartCartesianTransform(InnerBounds(), xDomain, yDomain);
        if (HasFixedAspectRatio)
        {
            transform.SetFixedAspectRatio(FixedAspectRatio);
        }

        return transform;
    }

    /// <summary>
    /// Creates the requested cartesian transform from axes from the supplied data and configuration.
    /// </summary>
    /// <returns>The newly created cartesian transform.</returns>
    /// <exception cref = "System.InvalidOperationException">Thrown when the current object state cannot perform this operation.</exception>
    public ChartCartesianTransform CreateCartesianTransformFromAxes()
    {
        ChartAxis? xAxis = FindFirstAxis(ChartAxisKind.X);
        ChartAxis? yAxis = FindFirstAxis(ChartAxisKind.Y);
        if (xAxis == null || yAxis == null)
        {
            throw new System.InvalidOperationException("Plot requires both x and y axes.");
        }

        ChartCartesianTransform transform = ChartCartesianTransform.FromAxes(xAxis, yAxis);
        if (HasFixedAspectRatio)
        {
            transform.SetFixedAspectRatio(FixedAspectRatio);
        }

        return transform;
    }

    /// <summary>
    /// Creates the requested cartesian renderer from the supplied data and configuration.
    /// </summary>
    /// <param name = "xDomain">The x domain.</param>
    /// <param name = "yDomain">The y domain.</param>
    /// <returns>The newly created cartesian renderer.</returns>
    public ChartCartesianRenderer CreateCartesianRenderer(ChartRange xDomain, ChartRange yDomain)
    {
        ChartCartesianRenderer renderer = new ChartCartesianRenderer(CreateCartesianTransform(xDomain, yDomain));
        renderer.ClipToPlotArea = ClipToPlotArea;
        return renderer;
    }

    /// <summary>
    /// Creates the requested cartesian renderer from axes from the supplied data and configuration.
    /// </summary>
    /// <returns>The newly created cartesian renderer.</returns>
    public ChartCartesianRenderer CreateCartesianRendererFromAxes()
    {
        ChartCartesianRenderer renderer = new ChartCartesianRenderer(CreateCartesianTransformFromAxes());
        renderer.ClipToPlotArea = ClipToPlotArea;
        return renderer;
    }

    /// <summary>Sets a ternary triangle using vertices relative to the plot's inner bounds.</summary>
    /// <param name = "firstVertex">The relative vertex representing a full first component.</param>
    /// <param name = "secondVertex">The relative vertex representing a full second component.</param>
    /// <param name = "thirdVertex">The relative vertex representing a full third component.</param>
    public void SetTernaryRegion(ChartPoint firstVertex, ChartPoint secondVertex, ChartPoint thirdVertex)
    {
        ValidateRelativeVertex(firstVertex, "firstVertex");
        ValidateRelativeVertex(secondVertex, "secondVertex");
        ValidateRelativeVertex(thirdVertex, "thirdVertex");
        ChartTernaryTransform transform = new ChartTernaryTransform(firstVertex, secondVertex, thirdVertex);
        TernaryFirstVertex = transform.GetFirstVertex();
        TernarySecondVertex = transform.GetSecondVertex();
        TernaryThirdVertex = transform.GetThirdVertex();
    }

    /// <summary>Creates a ternary transform fitted to the configured region of the current inner bounds.</summary>
    /// <returns>The newly created ternary transform.</returns>
    public ChartTernaryTransform CreateTernaryTransform()
    {
        ChartRect inner = InnerBounds();
        ChartPoint first = MapRelativeVertex(inner, TernaryFirstVertex);
        ChartPoint second = MapRelativeVertex(inner, TernarySecondVertex);
        ChartPoint third = MapRelativeVertex(inner, TernaryThirdVertex);
        return new ChartTernaryTransform(first, second, third);
    }

    /// <summary>Sets whether all series created for this plot are clipped to its inner bounds.</summary>
    /// <param name = "clipToPlotArea">Whether plot-level clipping is enabled.</param>
    public void SetPlotClipping(bool clipToPlotArea)
    {
        ClipToPlotArea = clipToPlotArea;
    }

    /// <summary>
    /// Finds first axis using the supplied data and configuration.
    /// </summary>
    /// <param name = "kind">The kind.</param>
    /// <returns>The matching value, or <c>null</c> when no match exists.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartAxis? FindFirstAxis(ChartAxisKind kind)
    {
        int value = (int)kind;
        if (value < 0 || value > 4)
        {
            throw new System.ArgumentOutOfRangeException("kind");
        }

        int index = 0;
        while (index < AxisCount)
        {
            if (Axes[index].Kind == kind)
            {
                return Axes[index];
            }

            index = index + 1;
        }

        return null;
    }

    /// <summary>
    /// Returns the requested axis.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting axis.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartAxis AxisAt(int index)
    {
        if (index < 0 || index >= AxisCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Axes[index];
    }

    /// <summary>
    /// Sets the grid position configuration.
    /// </summary>
    /// <param name = "row">The row.</param>
    /// <param name = "column">The column.</param>
    /// <param name = "rowSpan">The row span.</param>
    /// <param name = "columnSpan">The column span.</param>
    public void SetGridPosition(int row, int column, int rowSpan, int columnSpan)
    {
        ValidateGridPosition(row, column, rowSpan, columnSpan);
        HasGridPosition = true;
        GridRow = row;
        GridColumn = column;
        RowSpan = rowSpan;
        ColumnSpan = columnSpan;
        IsInset = false;
    }

    /// <summary>
    /// Sets the inset configuration.
    /// </summary>
    /// <param name = "parentPlotId">The parent plot id.</param>
    /// <param name = "insetBounds">The inset bounds.</param>
    public void SetInset(string parentPlotId, ChartRect insetBounds)
    {
        ValidateId(parentPlotId);
        ValidateInsetBounds(insetBounds);
        IsInset = true;
        InsetParentId = parentPlotId;
        InsetBounds = CopyRect(insetBounds);
    }

    /// <summary>
    /// Sets the shared axes configuration.
    /// </summary>
    /// <param name = "shareXAxisGroup">The share x axis group.</param>
    /// <param name = "shareYAxisGroup">The share y axis group.</param>
    public void SetSharedAxes(string shareXAxisGroup, string shareYAxisGroup)
    {
        ChartTextBlock.ValidateText(shareXAxisGroup, "shareXAxisGroup");
        ChartTextBlock.ValidateText(shareYAxisGroup, "shareYAxisGroup");
        ShareXAxisGroup = shareXAxisGroup;
        ShareYAxisGroup = shareYAxisGroup;
    }

    /// <summary>Constrains cartesian transforms to a fixed horizontal-to-vertical data scale ratio.</summary>
    /// <param name = "ratio">The positive horizontal-to-vertical data scale ratio.</param>
    public void SetFixedAspectRatio(double ratio)
    {
        ChartRenderStyle.ValidatePositive(ratio, "ratio");
        HasFixedAspectRatio = true;
        FixedAspectRatio = ratio;
    }

    /// <summary>Restores unconstrained cartesian transform aspect ratios.</summary>
    public void ClearFixedAspectRatio()
    {
        HasFixedAspectRatio = false;
    }

    /// <summary>
    /// Applies the inner bounds operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting rect.</returns>
    public ChartRect InnerBounds()
    {
        return Margins.DeflateRect(Bounds);
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartPlot Copy()
    {
        ChartPlot plot = new ChartPlot(Id);
        plot.Bounds = CopyRect(Bounds);
        plot.Margins = CopyPadding(Margins);
        plot.BackgroundColor = BackgroundColor.Copy();
        plot.HasBackground = HasBackground;
        plot.Style = Style.Copy();
        plot.HasGridPosition = HasGridPosition;
        plot.GridRow = GridRow;
        plot.GridColumn = GridColumn;
        plot.RowSpan = RowSpan;
        plot.ColumnSpan = ColumnSpan;
        plot.IsInset = IsInset;
        plot.InsetParentId = InsetParentId;
        plot.InsetBounds = CopyRect(InsetBounds);
        plot.ShareXAxisGroup = ShareXAxisGroup;
        plot.ShareYAxisGroup = ShareYAxisGroup;
        plot.HasFixedAspectRatio = HasFixedAspectRatio;
        plot.FixedAspectRatio = FixedAspectRatio;
        plot.TernaryFirstVertex = TernaryFirstVertex.Copy();
        plot.TernarySecondVertex = TernarySecondVertex.Copy();
        plot.TernaryThirdVertex = TernaryThirdVertex.Copy();
        plot.ClipToPlotArea = ClipToPlotArea;
        plot.EnsureAxisCapacity(AxisCount);
        int index = 0;
        while (index < AxisCount)
        {
            plot.Axes[index] = Axes[index].Copy();
            index = index + 1;
        }

        plot.AxisCount = AxisCount;
        plot.EnsureSeriesCapacity(SeriesCount);
        index = 0;
        while (index < SeriesCount)
        {
            plot.Series[index] = Series[index].Copy();
            index = index + 1;
        }

        plot.SeriesCount = SeriesCount;
        plot.EnsureAnnotationSeriesCapacity(AnnotationSeriesCount);
        index = 0;
        while (index < AnnotationSeriesCount)
        {
            plot.AnnotationSeries[index] = AnnotationSeries[index].Copy();
            index = index + 1;
        }

        plot.AnnotationSeriesCount = AnnotationSeriesCount;
        plot.XDomain = new ChartRange(XDomain.Minimum, XDomain.Maximum);
        plot.YDomain = new ChartRange(YDomain.Minimum, YDomain.Maximum);
        plot.HasCartesianDomains = HasCartesianDomains;
        return plot;
    }

    internal void EnsureAxisCapacity(int capacity)
    {
        if (capacity <= Axes.Length)
        {
            return;
        }

        int newCapacity = ChartArrayUtilities.GrowthCapacity(Axes.Length, capacity);
        ChartAxis[] axes = new ChartAxis[newCapacity];
        System.Array.Copy(Axes, axes, AxisCount);
        Axes = axes;
    }

    internal void EnsureSeriesCapacity(int capacity)
    {
        if (capacity <= Series.Length)
        {
            return;
        }

        int newCapacity = ChartArrayUtilities.GrowthCapacity(Series.Length, capacity);
        ChartCartesianSeries[] series = new ChartCartesianSeries[newCapacity];
        System.Array.Copy(Series, series, SeriesCount);
        Series = series;
    }

    internal void EnsureAnnotationSeriesCapacity(int capacity)
    {
        if (capacity <= AnnotationSeries.Length)
        {
            return;
        }

        int newCapacity = ChartArrayUtilities.GrowthCapacity(AnnotationSeries.Length, capacity);
        ChartCartesianSeries[] series = new ChartCartesianSeries[newCapacity];
        System.Array.Copy(AnnotationSeries, series, AnnotationSeriesCount);
        AnnotationSeries = series;
    }

    internal static void ValidateId(string id)
    {
        if (id == null)
        {
            throw new System.ArgumentNullException("id");
        }

        if (id.Length == 0)
        {
            throw new System.ArgumentException("Plot id cannot be empty.");
        }
    }

    internal static void ValidatePadding(ChartPadding padding)
    {
        if (padding == null)
        {
            throw new System.ArgumentNullException("padding");
        }

        ChartLinearScale.ValidateFinite(padding.Left, "padding");
        ChartLinearScale.ValidateFinite(padding.Top, "padding");
        ChartLinearScale.ValidateFinite(padding.Right, "padding");
        ChartLinearScale.ValidateFinite(padding.Bottom, "padding");
        if (padding.Left < 0.0 || padding.Top < 0.0 || padding.Right < 0.0 || padding.Bottom < 0.0)
        {
            throw new System.ArgumentOutOfRangeException("padding");
        }
    }

    internal static ChartPadding CopyPadding(ChartPadding padding)
    {
        return new ChartPadding(padding.Left, padding.Top, padding.Right, padding.Bottom);
    }

    internal static ChartRect CopyRect(ChartRect rect)
    {
        return new ChartRect(rect.X, rect.Y, rect.Width, rect.Height);
    }

    internal static void ValidateGridPosition(int row, int column, int rowSpan, int columnSpan)
    {
        if (row < 0)
        {
            throw new System.ArgumentOutOfRangeException("row");
        }

        if (column < 0)
        {
            throw new System.ArgumentOutOfRangeException("column");
        }

        if (rowSpan <= 0)
        {
            throw new System.ArgumentOutOfRangeException("rowSpan");
        }

        if (columnSpan <= 0)
        {
            throw new System.ArgumentOutOfRangeException("columnSpan");
        }
    }

    internal static void ValidateInsetBounds(ChartRect insetBounds)
    {
        if (insetBounds == null)
        {
            throw new System.ArgumentNullException("insetBounds");
        }

        ChartLinearScale.ValidateFinite(insetBounds.X, "insetBounds");
        ChartLinearScale.ValidateFinite(insetBounds.Y, "insetBounds");
        ChartLinearScale.ValidateFinite(insetBounds.Width, "insetBounds");
        ChartLinearScale.ValidateFinite(insetBounds.Height, "insetBounds");
        if (insetBounds.X < 0.0 || insetBounds.Y < 0.0 || insetBounds.Width <= 0.0 || insetBounds.Height <= 0.0)
        {
            throw new System.ArgumentOutOfRangeException("insetBounds");
        }

        if (insetBounds.X + insetBounds.Width > 1.0 || insetBounds.Y + insetBounds.Height > 1.0)
        {
            throw new System.ArgumentOutOfRangeException("insetBounds");
        }
    }

    internal static void ValidateRelativeVertex(ChartPoint vertex, string name)
    {
        ChartTernaryTransform.ValidateVertex(vertex, name);
        if (vertex.X < 0.0 || vertex.X > 1.0 || vertex.Y < 0.0 || vertex.Y > 1.0)
        {
            throw new System.ArgumentOutOfRangeException(name);
        }
    }

    internal static ChartPoint MapRelativeVertex(ChartRect bounds, ChartPoint vertex)
    {
        double x = bounds.X + bounds.Width * vertex.X;
        double y = bounds.Y + bounds.Height * vertex.Y;
        return new ChartPoint(x, y);
    }

    /// <summary>Returns the current id.</summary>
    /// <returns>The current field value.</returns>
    public string GetId()
    {
        return Id;
    }

    /// <summary>Replaces the current id.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetId(string value)
    {
        Id = value;
    }

    /// <summary>Returns the current bounds.</summary>
    /// <returns>The current field value.</returns>
    public ChartRect GetBounds()
    {
        return Bounds;
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

    /// <summary>Returns the current style.</summary>
    /// <returns>The current field value.</returns>
    public ChartStyle GetStyle()
    {
        return Style;
    }

    /// <summary>Returns the current axes.</summary>
    /// <returns>The current field value.</returns>
    public ChartAxis[] GetAxes()
    {
        return Axes;
    }

    /// <summary>Replaces the current axes.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetAxes(ChartAxis[] value)
    {
        Axes = value;
    }

    /// <summary>Returns the current axis count.</summary>
    /// <returns>The current field value.</returns>
    public int GetAxisCount()
    {
        return AxisCount;
    }

    /// <summary>Replaces the current axis count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetAxisCount(int value)
    {
        AxisCount = value;
    }

    /// <summary>Returns the current has grid position.</summary>
    /// <returns>The current field value.</returns>
    public bool GetHasGridPosition()
    {
        return HasGridPosition;
    }

    /// <summary>Replaces the current has grid position.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetHasGridPosition(bool value)
    {
        HasGridPosition = value;
    }

    /// <summary>Returns the current grid row.</summary>
    /// <returns>The current field value.</returns>
    public int GetGridRow()
    {
        return GridRow;
    }

    /// <summary>Replaces the current grid row.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetGridRow(int value)
    {
        GridRow = value;
    }

    /// <summary>Returns the current grid column.</summary>
    /// <returns>The current field value.</returns>
    public int GetGridColumn()
    {
        return GridColumn;
    }

    /// <summary>Replaces the current grid column.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetGridColumn(int value)
    {
        GridColumn = value;
    }

    /// <summary>Returns the current row span.</summary>
    /// <returns>The current field value.</returns>
    public int GetRowSpan()
    {
        return RowSpan;
    }

    /// <summary>Replaces the current row span.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetRowSpan(int value)
    {
        RowSpan = value;
    }

    /// <summary>Returns the current column span.</summary>
    /// <returns>The current field value.</returns>
    public int GetColumnSpan()
    {
        return ColumnSpan;
    }

    /// <summary>Replaces the current column span.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetColumnSpan(int value)
    {
        ColumnSpan = value;
    }

    /// <summary>Returns the current is inset.</summary>
    /// <returns>The current field value.</returns>
    public bool GetIsInset()
    {
        return IsInset;
    }

    /// <summary>Replaces the current is inset.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetIsInset(bool value)
    {
        IsInset = value;
    }

    /// <summary>Returns the current inset parent id.</summary>
    /// <returns>The current field value.</returns>
    public string GetInsetParentId()
    {
        return InsetParentId;
    }

    /// <summary>Replaces the current inset parent id.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetInsetParentId(string value)
    {
        InsetParentId = value;
    }

    /// <summary>Returns the current inset bounds.</summary>
    /// <returns>The current field value.</returns>
    public ChartRect GetInsetBounds()
    {
        return InsetBounds;
    }

    /// <summary>Replaces the current inset bounds.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetInsetBounds(ChartRect value)
    {
        InsetBounds = value;
    }

    /// <summary>Returns the current share x axis group.</summary>
    /// <returns>The current field value.</returns>
    public string GetShareXAxisGroup()
    {
        return ShareXAxisGroup;
    }

    /// <summary>Replaces the current share x axis group.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetShareXAxisGroup(string value)
    {
        ShareXAxisGroup = value;
    }

    /// <summary>Returns the current share y axis group.</summary>
    /// <returns>The current field value.</returns>
    public string GetShareYAxisGroup()
    {
        return ShareYAxisGroup;
    }

    /// <summary>Returns the relative ternary vertex for the first component.</summary>
    /// <returns>A copy of the configured relative vertex.</returns>
    public ChartPoint GetTernaryFirstVertex()
    {
        return TernaryFirstVertex.Copy();
    }

    /// <summary>Returns the relative ternary vertex for the second component.</summary>
    /// <returns>A copy of the configured relative vertex.</returns>
    public ChartPoint GetTernarySecondVertex()
    {
        return TernarySecondVertex.Copy();
    }

    /// <summary>Returns the relative ternary vertex for the third component.</summary>
    /// <returns>A copy of the configured relative vertex.</returns>
    public ChartPoint GetTernaryThirdVertex()
    {
        return TernaryThirdVertex.Copy();
    }

    /// <summary>Returns whether plot-level clipping is enabled.</summary>
    /// <returns><c>true</c> when series created for this plot are clipped.</returns>
    public bool GetPlotClipping()
    {
        return ClipToPlotArea;
    }

    /// <summary>Replaces the current share y axis group.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetShareYAxisGroup(string value)
    {
        ShareYAxisGroup = value;
    }
}
