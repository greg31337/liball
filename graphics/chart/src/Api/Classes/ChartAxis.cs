namespace chart;
/// <summary>
/// Represents an axis used by the chart model or rendering pipeline.
/// </summary>
public class ChartAxis : IChartAxis
{
    internal ChartAxisKind Kind;
    internal ChartAxisPlacement Placement;
    internal ChartTick[] Ticks;
    internal ChartRange Domain;
    internal ChartRect PlotArea;
    internal ChartPoint Center;
    internal double Radius;
    internal double TickLength;
    internal double MinorTickLength;
    internal int MinorTickCount;
    internal double LabelOffset;
    internal double TitleOffset;
    internal double GridLength;
    internal double MinimumLabelSpacing;
    internal ChartColor StrokeColor;
    internal ChartColor GridColor;
    internal ChartColor ZeroLineColor;
    internal ChartColor BandColor;
    internal ChartColor TextColor;
    internal double StrokeWidth;
    internal double GridStrokeWidth;
    internal double ZeroLineStrokeWidth;
    internal string Id;
    internal string Title;
    internal string FontFamily;
    internal int FontSize;
    internal bool ShowTickLabels;
    internal bool ShowMinorTicks;
    internal bool ShowGridLines;
    internal bool ShowZeroLine;
    internal bool ShowGridBands;
    internal bool AutoSkipTickLabels;
    internal IChartTickFormatter TickFormatter;
    internal double TickLabelRotation;
    internal int TickLabelMaximumCharacters;
    internal bool TrimTickLabels;
    internal bool EllipsizeTickLabels;
    internal bool MultilineTickLabels;
    internal string Subtitle;
    internal string Unit;
    internal double AxisOffset;
    internal string LinkedAxisId;
    internal double LinkScale;
    internal double LinkOffset;
    internal ChartColor TickColor;
    internal ChartColor MinorTickColor;
    internal double TickStrokeWidth;
    internal double MinorTickStrokeWidth;
    internal double[] ThresholdValues;
    internal string[] ThresholdLabels;
    internal ChartColor[] ThresholdColors;
    internal double[] ThresholdWidths;
    internal int ThresholdCount;
    internal double[] BreakMinimums;
    internal double[] BreakMaximums;
    internal int BreakCount;
    internal ChartPoint TernaryStart;
    internal ChartPoint TernaryEnd;
    internal ChartPoint TernaryOpposite;
    /// <summary>
    /// Initializes an axis from the supplied kind, placement, ticks, domain, and plot area.
    /// </summary>
    /// <param name = "kind">The kind.</param>
    /// <param name = "placement">The placement.</param>
    /// <param name = "ticks">The ticks.</param>
    /// <param name = "domain">The input data range mapped by the scale or axis.</param>
    /// <param name = "plotArea">The rectangle available for plotting data.</param>
    public ChartAxis(ChartAxisKind kind, ChartAxisPlacement placement, ChartTick[] ticks, ChartRange domain, ChartRect plotArea)
    {
        ValidatePlacement(kind, placement);
        ValidateDomain(domain);
        ValidatePlotArea(plotArea);
        Kind = kind;
        Placement = placement;
        Ticks = ChartTickGeneratorOperations.CopyTicks(ticks);
        Domain = new ChartRange(domain.Minimum, domain.Maximum);
        PlotArea = new ChartRect(plotArea.X, plotArea.Y, plotArea.Width, plotArea.Height);
        Center = PlotArea.Center();
        Radius = System.Math.Min(PlotArea.Width, PlotArea.Height) * 0.5;
        TickLength = 6.0;
        MinorTickLength = 3.0;
        MinorTickCount = 0;
        LabelOffset = 12.0;
        TitleOffset = 32.0;
        GridLength = 0.0;
        MinimumLabelSpacing = 30.0;
        StrokeColor = ChartColor.Black();
        GridColor = ChartColor.FromRgb(220, 220, 220);
        ZeroLineColor = ChartColor.Black();
        BandColor = ChartColor.FromRgb(245, 245, 245);
        TextColor = ChartColor.Black();
        StrokeWidth = 1.0;
        GridStrokeWidth = 1.0;
        ZeroLineStrokeWidth = 1.0;
        Id = DefaultId(kind, placement);
        Title = "";
        FontFamily = "Arial";
        FontSize = 10;
        ShowTickLabels = false;
        ShowMinorTicks = false;
        ShowGridLines = false;
        ShowZeroLine = false;
        ShowGridBands = false;
        AutoSkipTickLabels = false;
        TickFormatter = new ChartDefaultTickFormatter();
        TickLabelRotation = 0.0;
        TickLabelMaximumCharacters = 0;
        TrimTickLabels = false;
        EllipsizeTickLabels = false;
        MultilineTickLabels = false;
        Subtitle = "";
        Unit = "";
        AxisOffset = 0.0;
        LinkedAxisId = "";
        LinkScale = 1.0;
        LinkOffset = 0.0;
        TickColor = StrokeColor.Copy();
        MinorTickColor = StrokeColor.Copy();
        TickStrokeWidth = StrokeWidth;
        MinorTickStrokeWidth = StrokeWidth;
        ThresholdValues = new double[2];
        ThresholdLabels = new string[2];
        ThresholdColors = new ChartColor[2];
        ThresholdWidths = new double[2];
        ThresholdCount = 0;
        BreakMinimums = new double[2];
        BreakMaximums = new double[2];
        BreakCount = 0;
        TernaryStart = new ChartPoint(0.0, 0.0);
        TernaryEnd = new ChartPoint(1.0, 0.0);
        TernaryOpposite = new ChartPoint(0.5, 1.0);
    }

    internal static ChartAxis CreateBottom(ChartTick[] ticks, ChartRange domain, ChartRect plotArea)
    {
        return new ChartAxis(ChartAxisKind.X, ChartAxisPlacement.Bottom, ticks, domain, plotArea);
    }

    internal static ChartAxis CreateTop(ChartTick[] ticks, ChartRange domain, ChartRect plotArea)
    {
        return new ChartAxis(ChartAxisKind.X, ChartAxisPlacement.Top, ticks, domain, plotArea);
    }

    internal static ChartAxis CreateLeft(ChartTick[] ticks, ChartRange domain, ChartRect plotArea)
    {
        return new ChartAxis(ChartAxisKind.Y, ChartAxisPlacement.Left, ticks, domain, plotArea);
    }

    internal static ChartAxis CreateRight(ChartTick[] ticks, ChartRange domain, ChartRect plotArea)
    {
        return new ChartAxis(ChartAxisKind.Y, ChartAxisPlacement.Right, ticks, domain, plotArea);
    }

    internal static ChartAxis CreateRadial(ChartPoint center, double radius, ChartTick[] ticks, ChartRange domain)
    {
        ValidateCenter(center);
        ValidateRadius(radius);
        ChartRect plotArea = new ChartRect(center.X - radius, center.Y - radius, radius * 2.0, radius * 2.0);
        ChartAxis axis = new ChartAxis(ChartAxisKind.Radial, ChartAxisPlacement.Outside, ticks, domain, plotArea);
        axis.Center = center.Copy();
        axis.Radius = radius;
        axis.Id = "axis-radial";
        return axis;
    }

    internal static ChartAxis CreateAngular(ChartPoint center, double radius, ChartTick[] ticks)
    {
        ValidateCenter(center);
        ValidateRadius(radius);
        ChartRect plotArea = new ChartRect(center.X - radius, center.Y - radius, radius * 2.0, radius * 2.0);
        ChartAxis axis = new ChartAxis(ChartAxisKind.Angular, ChartAxisPlacement.Outside, ticks, new ChartRange(0.0, 360.0), plotArea);
        axis.Center = center.Copy();
        axis.Radius = radius;
        axis.Id = "axis-angular";
        return axis;
    }

    internal static ChartAxis CreateTernary(ChartPoint startVertex, ChartPoint endVertex, ChartPoint oppositeVertex, ChartTick[] ticks)
    {
        ChartTernaryTransform.ValidateVertex(startVertex, "startVertex");
        ChartTernaryTransform.ValidateVertex(endVertex, "endVertex");
        ChartTernaryTransform.ValidateVertex(oppositeVertex, "oppositeVertex");
        ChartTernaryTransform transform = new ChartTernaryTransform(startVertex, endVertex, oppositeVertex);
        double left = System.Math.Min(startVertex.X, System.Math.Min(endVertex.X, oppositeVertex.X));
        double top = System.Math.Min(startVertex.Y, System.Math.Min(endVertex.Y, oppositeVertex.Y));
        double right = System.Math.Max(startVertex.X, System.Math.Max(endVertex.X, oppositeVertex.X));
        double bottom = System.Math.Max(startVertex.Y, System.Math.Max(endVertex.Y, oppositeVertex.Y));
        ChartAxis axis = new ChartAxis(ChartAxisKind.Ternary, ChartAxisPlacement.Outside, ticks, new ChartRange(0.0, 1.0), new ChartRect(left, top, right - left, bottom - top));
        axis.TernaryStart = transform.GetFirstVertex();
        axis.TernaryEnd = transform.GetSecondVertex();
        axis.TernaryOpposite = transform.GetThirdVertex();
        axis.Id = "axis-ternary";
        return axis;
    }

    /// <summary>
    /// Sets the tick length configuration.
    /// </summary>
    /// <param name = "tickLength">The tick length.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public void SetTickLength(double tickLength)
    {
        ChartLinearScale.ValidateFinite(tickLength, "tickLength");
        if (tickLength < 0.0)
        {
            throw new System.ArgumentOutOfRangeException("tickLength");
        }

        TickLength = tickLength;
    }

    /// <summary>
    /// Sets the minor ticks configuration.
    /// </summary>
    /// <param name = "showMinorTicks">The show minor ticks.</param>
    /// <param name = "minorTickCount">The requested number of minor tick entries.</param>
    /// <param name = "minorTickLength">The minor tick length.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public void SetMinorTicks(bool showMinorTicks, int minorTickCount, double minorTickLength)
    {
        if (minorTickCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("minorTickCount");
        }

        ChartLinearScale.ValidateFinite(minorTickLength, "minorTickLength");
        if (minorTickLength < 0.0)
        {
            throw new System.ArgumentOutOfRangeException("minorTickLength");
        }

        ShowMinorTicks = showMinorTicks;
        MinorTickCount = minorTickCount;
        MinorTickLength = minorTickLength;
    }

    /// <summary>
    /// Sets the tick labels configuration.
    /// </summary>
    /// <param name = "showTickLabels">The show tick labels.</param>
    /// <param name = "autoSkipTickLabels">The auto skip tick labels.</param>
    /// <param name = "minimumLabelSpacing">The minimum label spacing.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public void SetTickLabels(bool showTickLabels, bool autoSkipTickLabels, double minimumLabelSpacing)
    {
        ChartLinearScale.ValidateFinite(minimumLabelSpacing, "minimumLabelSpacing");
        if (minimumLabelSpacing < 0.0)
        {
            throw new System.ArgumentOutOfRangeException("minimumLabelSpacing");
        }

        ShowTickLabels = showTickLabels;
        AutoSkipTickLabels = autoSkipTickLabels;
        MinimumLabelSpacing = minimumLabelSpacing;
    }

    /// <summary>Configures tick-label rotation, trimming, ellipsis, and multiline rendering.</summary>
    /// <param name = "rotationDegrees">The finite clockwise rotation in degrees.</param>
    /// <param name = "maximumCharacters">The maximum characters per line, or zero for no limit.</param>
    /// <param name = "trim"><c>true</c> to remove surrounding whitespace from each label line.</param>
    /// <param name = "ellipsis"><c>true</c> to use an ellipsis when a line exceeds the maximum.</param>
    /// <param name = "multiline"><c>true</c> to preserve line breaks.</param>
    public void SetTickLabelLayout(double rotationDegrees, int maximumCharacters, bool trim, bool ellipsis, bool multiline)
    {
        ChartLinearScale.ValidateFinite(rotationDegrees, "rotationDegrees");
        if (maximumCharacters < 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumCharacters");
        }

        TickLabelRotation = rotationDegrees;
        TickLabelMaximumCharacters = maximumCharacters;
        TrimTickLabels = trim;
        EllipsizeTickLabels = ellipsis;
        MultilineTickLabels = multiline;
    }

    /// <summary>
    /// Sets the strategy used to format tick labels.
    /// </summary>
    /// <param name = "formatter">The formatter that converts ticks to display text.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void SetTickFormatter(IChartTickFormatter formatter)
    {
        if (formatter == null)
        {
            throw new System.ArgumentNullException("formatter");
        }

        TickFormatter = formatter;
    }

    /// <summary>
    /// Sets the title configuration.
    /// </summary>
    /// <param name = "title">The title.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void SetTitle(string title)
    {
        if (title == null)
        {
            throw new System.ArgumentNullException("title");
        }

        Title = title;
    }

    /// <summary>Sets the subtitle rendered beneath the axis title.</summary>
    /// <param name = "subtitle">The subtitle, which may be empty.</param>
    public void SetSubtitle(string subtitle)
    {
        if (subtitle == null)
        {
            throw new System.ArgumentNullException("subtitle");
        }

        Subtitle = subtitle;
    }

    /// <summary>Sets the unit suffix appended to tick labels.</summary>
    /// <param name = "unit">The unit suffix, which may be empty.</param>
    public void SetUnit(string unit)
    {
        if (unit == null)
        {
            throw new System.ArgumentNullException("unit");
        }

        Unit = unit;
    }

    /// <summary>Sets the outward offset used to place multiple axes on the same side.</summary>
    /// <param name = "offset">The nonnegative offset in chart units.</param>
    public void SetAxisOffset(double offset)
    {
        ChartLinearScale.ValidateFinite(offset, "offset");
        if (offset < 0.0)
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        AxisOffset = offset;
    }

    /// <summary>Links this axis domain and ticks to a numeric source axis.</summary>
    /// <param name = "source">The source axis.</param>
    /// <param name = "scale">The positive conversion scale.</param>
    /// <param name = "offset">The conversion offset.</param>
    public void SetLinkedAxis(ChartAxis source, double scale, double offset)
    {
        ValidateLink(source, scale, offset);
        if (source.Kind != Kind)
        {
            throw new System.ArgumentException("Linked axes must have the same axis kind.");
        }

        LinkedAxisId = source.Id;
        LinkScale = scale;
        LinkOffset = offset;
        ApplyLinkedAxis(source);
    }

    /// <summary>Refreshes this axis from its configured linked source.</summary>
    /// <param name = "source">The linked source axis.</param>
    public void SynchronizeLinkedAxis(ChartAxis source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        if (LinkedAxisId.Length == 0 || source.Id != LinkedAxisId)
        {
            throw new System.ArgumentException("The supplied axis is not the configured linked source.");
        }

        ApplyLinkedAxis(source);
    }

    /// <summary>Adds a styled threshold line at a domain value.</summary>
    /// <param name = "value">The threshold domain value.</param>
    /// <param name = "label">The optional threshold label.</param>
    /// <param name = "color">The threshold line color.</param>
    /// <param name = "strokeWidth">The positive threshold line width.</param>
    /// <returns>The zero-based threshold index.</returns>
    public int AddThresholdLine(double value, string label, ChartColor color, double strokeWidth)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        if (label == null)
        {
            throw new System.ArgumentNullException("label");
        }

        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        ChartRenderStyle.ValidatePositive(strokeWidth, "strokeWidth");
        EnsureThresholdCapacity(ThresholdCount + 1);
        ThresholdValues[ThresholdCount] = value;
        ThresholdLabels[ThresholdCount] = label;
        ThresholdColors[ThresholdCount] = color.Copy();
        ThresholdWidths[ThresholdCount] = strokeWidth;
        ThresholdCount = ThresholdCount + 1;
        return ThresholdCount - 1;
    }

    /// <summary>Removes all threshold lines.</summary>
    public void ClearThresholdLines()
    {
        ThresholdCount = 0;
    }

    /// <summary>Adds a collapsed break inside the axis domain.</summary>
    /// <param name = "minimum">The break minimum.</param>
    /// <param name = "maximum">The break maximum.</param>
    /// <returns>The zero-based break index.</returns>
    public int AddBreak(double minimum, double maximum)
    {
        ChartLinearScale.ValidateFinite(minimum, "minimum");
        ChartLinearScale.ValidateFinite(maximum, "maximum");
        if (minimum >= maximum || minimum <= Domain.Minimum || maximum >= Domain.Maximum)
        {
            throw new System.ArgumentException("Axis breaks must be increasing and strictly inside the domain.");
        }

        if (BreakCount > 0 && minimum <= BreakMaximums[BreakCount - 1])
        {
            throw new System.ArgumentException("Axis breaks must be added in increasing nonoverlapping order.");
        }

        EnsureBreakCapacity(BreakCount + 1);
        BreakMinimums[BreakCount] = minimum;
        BreakMaximums[BreakCount] = maximum;
        BreakCount = BreakCount + 1;
        return BreakCount - 1;
    }

    /// <summary>Removes all axis breaks.</summary>
    public void ClearBreaks()
    {
        BreakCount = 0;
    }

    /// <summary>
    /// Sets the offsets configuration.
    /// </summary>
    /// <param name = "labelOffset">The label offset.</param>
    /// <param name = "titleOffset">The title offset.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public void SetOffsets(double labelOffset, double titleOffset)
    {
        ChartLinearScale.ValidateFinite(labelOffset, "labelOffset");
        ChartLinearScale.ValidateFinite(titleOffset, "titleOffset");
        if (labelOffset < 0.0 || titleOffset < 0.0)
        {
            throw new System.ArgumentOutOfRangeException("labelOffset");
        }

        LabelOffset = labelOffset;
        TitleOffset = titleOffset;
    }

    /// <summary>
    /// Sets the grid lines configuration.
    /// </summary>
    /// <param name = "showGridLines">The show grid lines.</param>
    /// <param name = "gridLength">The grid length.</param>
    /// <param name = "gridColor">The grid color.</param>
    /// <param name = "gridStrokeWidth">The grid stroke width.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public void SetGridLines(bool showGridLines, double gridLength, ChartColor gridColor, double gridStrokeWidth)
    {
        if (gridColor == null)
        {
            throw new System.ArgumentNullException("gridColor");
        }

        ChartLinearScale.ValidateFinite(gridLength, "gridLength");
        ChartLinearScale.ValidateFinite(gridStrokeWidth, "gridStrokeWidth");
        if (gridLength < 0.0 || gridStrokeWidth < 0.0)
        {
            throw new System.ArgumentOutOfRangeException("gridLength");
        }

        ShowGridLines = showGridLines;
        GridLength = gridLength;
        GridColor = gridColor.Copy();
        GridStrokeWidth = gridStrokeWidth;
    }

    /// <summary>
    /// Sets the zero line configuration.
    /// </summary>
    /// <param name = "showZeroLine">The show zero line.</param>
    /// <param name = "zeroLineColor">The zero line color.</param>
    /// <param name = "zeroLineStrokeWidth">The zero line stroke width.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public void SetZeroLine(bool showZeroLine, ChartColor zeroLineColor, double zeroLineStrokeWidth)
    {
        if (zeroLineColor == null)
        {
            throw new System.ArgumentNullException("zeroLineColor");
        }

        ChartLinearScale.ValidateFinite(zeroLineStrokeWidth, "zeroLineStrokeWidth");
        if (zeroLineStrokeWidth < 0.0)
        {
            throw new System.ArgumentOutOfRangeException("zeroLineStrokeWidth");
        }

        ShowZeroLine = showZeroLine;
        ZeroLineColor = zeroLineColor.Copy();
        ZeroLineStrokeWidth = zeroLineStrokeWidth;
    }

    /// <summary>
    /// Sets the grid bands configuration.
    /// </summary>
    /// <param name = "showGridBands">The show grid bands.</param>
    /// <param name = "bandColor">The band color.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void SetGridBands(bool showGridBands, ChartColor bandColor)
    {
        if (bandColor == null)
        {
            throw new System.ArgumentNullException("bandColor");
        }

        ShowGridBands = showGridBands;
        BandColor = bandColor.Copy();
    }

    /// <summary>
    /// Sets the text style configuration.
    /// </summary>
    /// <param name = "textColor">The text color.</param>
    /// <param name = "fontFamily">The SVG font-family name; bitmap output uses the built-in glyph shapes.</param>
    /// <param name = "fontSize">The font size in pixels, which must be positive.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public void SetTextStyle(ChartColor textColor, string fontFamily, int fontSize)
    {
        if (textColor == null)
        {
            throw new System.ArgumentNullException("textColor");
        }

        if (fontFamily == null)
        {
            throw new System.ArgumentNullException("fontFamily");
        }

        if (fontSize <= 0)
        {
            throw new System.ArgumentOutOfRangeException("fontSize");
        }

        TextColor = textColor.Copy();
        FontFamily = fontFamily;
        FontSize = fontSize;
    }

    /// <summary>
    /// Sets the stroke configuration.
    /// </summary>
    /// <param name = "color">The color value to apply or encode.</param>
    /// <param name = "strokeWidth">The stroke width in chart units, which must be positive.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public void SetStroke(ChartColor color, double strokeWidth)
    {
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        ChartLinearScale.ValidateFinite(strokeWidth, "strokeWidth");
        if (strokeWidth < 0.0)
        {
            throw new System.ArgumentOutOfRangeException("strokeWidth");
        }

        StrokeColor = color.Copy();
        StrokeWidth = strokeWidth;
        TickColor = color.Copy();
        MinorTickColor = color.Copy();
        TickStrokeWidth = strokeWidth;
        MinorTickStrokeWidth = strokeWidth;
    }

    /// <summary>Sets the style used for major tick marks.</summary>
    /// <param name = "color">The major tick color.</param>
    /// <param name = "strokeWidth">The positive major tick width.</param>
    public void SetTickStyle(ChartColor color, double strokeWidth)
    {
        ValidateLineStyle(color, strokeWidth, "strokeWidth");
        TickColor = color.Copy();
        TickStrokeWidth = strokeWidth;
    }

    /// <summary>Sets the style used for minor tick marks.</summary>
    /// <param name = "color">The minor tick color.</param>
    /// <param name = "strokeWidth">The positive minor tick width.</param>
    public void SetMinorTickStyle(ChartColor color, double strokeWidth)
    {
        ValidateLineStyle(color, strokeWidth, "strokeWidth");
        MinorTickColor = color.Copy();
        MinorTickStrokeWidth = strokeWidth;
    }

    /// <summary>Sets the style used for axis text including tick labels.</summary>
    /// <param name = "color">The label text color.</param>
    /// <param name = "fontFamily">The SVG font-family name.</param>
    /// <param name = "fontSize">The positive font size.</param>
    public void SetLabelStyle(ChartColor color, string fontFamily, int fontSize)
    {
        SetTextStyle(color, fontFamily, fontSize);
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartAxis Copy()
    {
        ChartAxis axis = new ChartAxis(Kind, Placement, Ticks, Domain, PlotArea);
        axis.Center = Center.Copy();
        axis.Radius = Radius;
        axis.TickLength = TickLength;
        axis.MinorTickLength = MinorTickLength;
        axis.MinorTickCount = MinorTickCount;
        axis.LabelOffset = LabelOffset;
        axis.TitleOffset = TitleOffset;
        axis.GridLength = GridLength;
        axis.MinimumLabelSpacing = MinimumLabelSpacing;
        axis.StrokeColor = StrokeColor.Copy();
        axis.GridColor = GridColor.Copy();
        axis.ZeroLineColor = ZeroLineColor.Copy();
        axis.BandColor = BandColor.Copy();
        axis.TextColor = TextColor.Copy();
        axis.StrokeWidth = StrokeWidth;
        axis.GridStrokeWidth = GridStrokeWidth;
        axis.ZeroLineStrokeWidth = ZeroLineStrokeWidth;
        axis.Id = Id;
        axis.Title = Title;
        axis.FontFamily = FontFamily;
        axis.FontSize = FontSize;
        axis.ShowTickLabels = ShowTickLabels;
        axis.ShowMinorTicks = ShowMinorTicks;
        axis.ShowGridLines = ShowGridLines;
        axis.ShowZeroLine = ShowZeroLine;
        axis.ShowGridBands = ShowGridBands;
        axis.AutoSkipTickLabels = AutoSkipTickLabels;
        axis.TickFormatter = TickFormatter;
        axis.TickLabelRotation = TickLabelRotation;
        axis.TickLabelMaximumCharacters = TickLabelMaximumCharacters;
        axis.TrimTickLabels = TrimTickLabels;
        axis.EllipsizeTickLabels = EllipsizeTickLabels;
        axis.MultilineTickLabels = MultilineTickLabels;
        axis.Subtitle = Subtitle;
        axis.Unit = Unit;
        axis.AxisOffset = AxisOffset;
        axis.LinkedAxisId = LinkedAxisId;
        axis.LinkScale = LinkScale;
        axis.LinkOffset = LinkOffset;
        axis.TickColor = TickColor.Copy();
        axis.MinorTickColor = MinorTickColor.Copy();
        axis.TickStrokeWidth = TickStrokeWidth;
        axis.MinorTickStrokeWidth = MinorTickStrokeWidth;
        axis.EnsureThresholdCapacity(ThresholdCount);
        int thresholdIndex = 0;
        while (thresholdIndex < ThresholdCount)
        {
            axis.ThresholdValues[thresholdIndex] = ThresholdValues[thresholdIndex];
            axis.ThresholdLabels[thresholdIndex] = ThresholdLabels[thresholdIndex];
            axis.ThresholdColors[thresholdIndex] = ThresholdColors[thresholdIndex].Copy();
            axis.ThresholdWidths[thresholdIndex] = ThresholdWidths[thresholdIndex];
            thresholdIndex = thresholdIndex + 1;
        }

        axis.ThresholdCount = ThresholdCount;
        axis.EnsureBreakCapacity(BreakCount);
        int breakIndex = 0;
        while (breakIndex < BreakCount)
        {
            axis.BreakMinimums[breakIndex] = BreakMinimums[breakIndex];
            axis.BreakMaximums[breakIndex] = BreakMaximums[breakIndex];
            breakIndex = breakIndex + 1;
        }

        axis.BreakCount = BreakCount;
        axis.TernaryStart = TernaryStart.Copy();
        axis.TernaryEnd = TernaryEnd.Copy();
        axis.TernaryOpposite = TernaryOpposite.Copy();
        return axis;
    }

    internal void EnsureThresholdCapacity(int capacity)
    {
        if (capacity <= ThresholdValues.Length)
        {
            return;
        }

        int newCapacity = ChartArrayUtilities.GrowthCapacity(ThresholdValues.Length, capacity);
        double[] values = new double[newCapacity];
        string[] labels = new string[newCapacity];
        ChartColor[] colors = new ChartColor[newCapacity];
        double[] widths = new double[newCapacity];
        System.Array.Copy(ThresholdValues, values, ThresholdCount);
        System.Array.Copy(ThresholdLabels, labels, ThresholdCount);
        System.Array.Copy(ThresholdColors, colors, ThresholdCount);
        System.Array.Copy(ThresholdWidths, widths, ThresholdCount);
        ThresholdValues = values;
        ThresholdLabels = labels;
        ThresholdColors = colors;
        ThresholdWidths = widths;
    }

    internal void EnsureBreakCapacity(int capacity)
    {
        if (capacity <= BreakMinimums.Length)
        {
            return;
        }

        int newCapacity = ChartArrayUtilities.GrowthCapacity(BreakMinimums.Length, capacity);
        double[] minimums = new double[newCapacity];
        double[] maximums = new double[newCapacity];
        System.Array.Copy(BreakMinimums, minimums, BreakCount);
        System.Array.Copy(BreakMaximums, maximums, BreakCount);
        BreakMinimums = minimums;
        BreakMaximums = maximums;
    }

    internal static void ValidateLink(ChartAxis source, double scale, double offset)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        ChartLinearScale.ValidateFinite(scale, "scale");
        ChartLinearScale.ValidateFinite(offset, "offset");
        if (scale <= 0.0)
        {
            throw new System.ArgumentOutOfRangeException("scale");
        }
    }

    internal void ApplyLinkedAxis(ChartAxis source)
    {
        Domain = new ChartRange(source.Domain.Minimum * LinkScale + LinkOffset, source.Domain.Maximum * LinkScale + LinkOffset);
        Ticks = ChartTickGeneratorOperations.CopyTicks(source.Ticks);
        int index = 0;
        while (index < Ticks.Length)
        {
            Ticks[index].Value = Ticks[index].Value * LinkScale + LinkOffset;
            Ticks[index].Label = "";
            index = index + 1;
        }
    }

    internal static void ValidateLineStyle(ChartColor color, double strokeWidth, string name)
    {
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        ChartRenderStyle.ValidatePositive(strokeWidth, name);
    }

    internal static string DefaultId(ChartAxisKind kind, ChartAxisPlacement placement)
    {
        if (kind == ChartAxisKind.Radial)
        {
            return "axis-radial";
        }

        if (kind == ChartAxisKind.Angular)
        {
            return "axis-angular";
        }

        if (placement == ChartAxisPlacement.Bottom)
        {
            return "axis-bottom";
        }

        if (placement == ChartAxisPlacement.Top)
        {
            return "axis-top";
        }

        if (placement == ChartAxisPlacement.Left)
        {
            return "axis-left";
        }

        if (placement == ChartAxisPlacement.Right)
        {
            return "axis-right";
        }

        return "axis";
    }

    internal static void ValidatePlacement(ChartAxisKind kind, ChartAxisPlacement placement)
    {
        if (kind == ChartAxisKind.X)
        {
            if (placement != ChartAxisPlacement.Bottom && placement != ChartAxisPlacement.Top)
            {
                throw new System.ArgumentException("X axes must be bottom or top.");
            }

            return;
        }

        if (kind == ChartAxisKind.Y)
        {
            if (placement != ChartAxisPlacement.Left && placement != ChartAxisPlacement.Right)
            {
                throw new System.ArgumentException("Y axes must be left or right.");
            }

            return;
        }

        if (kind == ChartAxisKind.Radial || kind == ChartAxisKind.Angular || kind == ChartAxisKind.Ternary)
        {
            if (placement != ChartAxisPlacement.Outside && placement != ChartAxisPlacement.Inside)
            {
                throw new System.ArgumentException("Polar axes must be inside or outside.");
            }

            return;
        }

        throw new System.ArgumentException("Unsupported axis kind.");
    }

    internal static void ValidateDomain(ChartRange domain)
    {
        if (domain == null)
        {
            throw new System.ArgumentNullException("domain");
        }

        if (!domain.IsValid() || domain.Minimum == domain.Maximum)
        {
            throw new System.ArgumentException("Axis domain must be finite and non-zero.");
        }
    }

    internal static void ValidatePlotArea(ChartRect plotArea)
    {
        if (plotArea == null)
        {
            throw new System.ArgumentNullException("plotArea");
        }

        ChartLinearScale.ValidateFinite(plotArea.X, "plotArea");
        ChartLinearScale.ValidateFinite(plotArea.Y, "plotArea");
        ChartLinearScale.ValidateFinite(plotArea.Width, "plotArea");
        ChartLinearScale.ValidateFinite(plotArea.Height, "plotArea");
        if (plotArea.IsEmpty())
        {
            throw new System.ArgumentException("Axis plot area cannot be empty.");
        }
    }

    internal static void ValidateCenter(ChartPoint center)
    {
        if (center == null)
        {
            throw new System.ArgumentNullException("center");
        }

        ChartLinearScale.ValidateFinite(center.X, "center");
        ChartLinearScale.ValidateFinite(center.Y, "center");
    }

    internal static void ValidateRadius(double radius)
    {
        ChartLinearScale.ValidateFinite(radius, "radius");
        if (radius <= 0.0)
        {
            throw new System.ArgumentOutOfRangeException("radius");
        }
    }

    /// <summary>Returns the current kind.</summary>
    /// <returns>The current field value.</returns>
    public ChartAxisKind GetKind()
    {
        return Kind;
    }

    /// <summary>Replaces the current kind.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetKind(ChartAxisKind value)
    {
        Kind = value;
    }

    /// <summary>Returns the current placement.</summary>
    /// <returns>The current field value.</returns>
    public ChartAxisPlacement GetPlacement()
    {
        return Placement;
    }

    /// <summary>Replaces the current placement.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetPlacement(ChartAxisPlacement value)
    {
        Placement = value;
    }

    /// <summary>Returns the current ticks.</summary>
    /// <returns>The current field value.</returns>
    public ChartTick[] GetTicks()
    {
        return Ticks;
    }

    /// <summary>Replaces the current ticks.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetTicks(ChartTick[] value)
    {
        Ticks = value;
    }

    /// <summary>Returns the current domain.</summary>
    /// <returns>The current field value.</returns>
    public ChartRange GetDomain()
    {
        return Domain;
    }

    /// <summary>Replaces the current domain.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetDomain(ChartRange value)
    {
        Domain = value;
    }

    /// <summary>Returns the current plot area.</summary>
    /// <returns>The current field value.</returns>
    public ChartRect GetPlotArea()
    {
        return PlotArea;
    }

    /// <summary>Replaces the current plot area.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetPlotArea(ChartRect value)
    {
        PlotArea = value;
    }

    /// <summary>Returns the current center.</summary>
    /// <returns>The current field value.</returns>
    public ChartPoint GetCenter()
    {
        return Center;
    }

    /// <summary>Replaces the current center.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetCenter(ChartPoint value)
    {
        Center = value;
    }

    /// <summary>Returns the current radius.</summary>
    /// <returns>The current field value.</returns>
    public double GetRadius()
    {
        return Radius;
    }

    /// <summary>Replaces the current radius.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetRadius(double value)
    {
        Radius = value;
    }

    /// <summary>Returns the current tick length.</summary>
    /// <returns>The current field value.</returns>
    public double GetTickLength()
    {
        return TickLength;
    }

    /// <summary>Returns the current minor tick length.</summary>
    /// <returns>The current field value.</returns>
    public double GetMinorTickLength()
    {
        return MinorTickLength;
    }

    /// <summary>Replaces the current minor tick length.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetMinorTickLength(double value)
    {
        MinorTickLength = value;
    }

    /// <summary>Returns the current minor tick count.</summary>
    /// <returns>The current field value.</returns>
    public int GetMinorTickCount()
    {
        return MinorTickCount;
    }

    /// <summary>Replaces the current minor tick count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetMinorTickCount(int value)
    {
        MinorTickCount = value;
    }

    /// <summary>Returns the current label offset.</summary>
    /// <returns>The current field value.</returns>
    public double GetLabelOffset()
    {
        return LabelOffset;
    }

    /// <summary>Replaces the current label offset.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetLabelOffset(double value)
    {
        LabelOffset = value;
    }

    /// <summary>Returns the current title offset.</summary>
    /// <returns>The current field value.</returns>
    public double GetTitleOffset()
    {
        return TitleOffset;
    }

    /// <summary>Replaces the current title offset.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetTitleOffset(double value)
    {
        TitleOffset = value;
    }

    /// <summary>Returns the current grid length.</summary>
    /// <returns>The current field value.</returns>
    public double GetGridLength()
    {
        return GridLength;
    }

    /// <summary>Replaces the current grid length.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetGridLength(double value)
    {
        GridLength = value;
    }

    /// <summary>Returns the current minimum label spacing.</summary>
    /// <returns>The current field value.</returns>
    public double GetMinimumLabelSpacing()
    {
        return MinimumLabelSpacing;
    }

    /// <summary>Replaces the current minimum label spacing.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetMinimumLabelSpacing(double value)
    {
        MinimumLabelSpacing = value;
    }

    /// <summary>Returns the current stroke color.</summary>
    /// <returns>The current field value.</returns>
    public ChartColor GetStrokeColor()
    {
        return StrokeColor;
    }

    /// <summary>Replaces the current stroke color.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetStrokeColor(ChartColor value)
    {
        StrokeColor = value;
    }

    /// <summary>Returns the current grid color.</summary>
    /// <returns>The current field value.</returns>
    public ChartColor GetGridColor()
    {
        return GridColor;
    }

    /// <summary>Replaces the current grid color.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetGridColor(ChartColor value)
    {
        GridColor = value;
    }

    /// <summary>Returns the current zero line color.</summary>
    /// <returns>The current field value.</returns>
    public ChartColor GetZeroLineColor()
    {
        return ZeroLineColor;
    }

    /// <summary>Replaces the current zero line color.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetZeroLineColor(ChartColor value)
    {
        ZeroLineColor = value;
    }

    /// <summary>Returns the current band color.</summary>
    /// <returns>The current field value.</returns>
    public ChartColor GetBandColor()
    {
        return BandColor;
    }

    /// <summary>Replaces the current band color.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetBandColor(ChartColor value)
    {
        BandColor = value;
    }

    /// <summary>Returns the current text color.</summary>
    /// <returns>The current field value.</returns>
    public ChartColor GetTextColor()
    {
        return TextColor;
    }

    /// <summary>Replaces the current text color.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetTextColor(ChartColor value)
    {
        TextColor = value;
    }

    /// <summary>Returns the current stroke width.</summary>
    /// <returns>The current field value.</returns>
    public double GetStrokeWidth()
    {
        return StrokeWidth;
    }

    /// <summary>Replaces the current stroke width.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetStrokeWidth(double value)
    {
        StrokeWidth = value;
    }

    /// <summary>Returns the current grid stroke width.</summary>
    /// <returns>The current field value.</returns>
    public double GetGridStrokeWidth()
    {
        return GridStrokeWidth;
    }

    /// <summary>Replaces the current grid stroke width.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetGridStrokeWidth(double value)
    {
        GridStrokeWidth = value;
    }

    /// <summary>Returns the current zero line stroke width.</summary>
    /// <returns>The current field value.</returns>
    public double GetZeroLineStrokeWidth()
    {
        return ZeroLineStrokeWidth;
    }

    /// <summary>Replaces the current zero line stroke width.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetZeroLineStrokeWidth(double value)
    {
        ZeroLineStrokeWidth = value;
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

    /// <summary>Returns the current title.</summary>
    /// <returns>The current field value.</returns>
    public string GetTitle()
    {
        return Title;
    }

    /// <summary>Returns the current font family.</summary>
    /// <returns>The current field value.</returns>
    public string GetFontFamily()
    {
        return FontFamily;
    }

    /// <summary>Replaces the current font family.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetFontFamily(string value)
    {
        FontFamily = value;
    }

    /// <summary>Returns the current font size.</summary>
    /// <returns>The current field value.</returns>
    public int GetFontSize()
    {
        return FontSize;
    }

    /// <summary>Replaces the current font size.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetFontSize(int value)
    {
        FontSize = value;
    }

    /// <summary>Returns the current show tick labels.</summary>
    /// <returns>The current field value.</returns>
    public bool GetShowTickLabels()
    {
        return ShowTickLabels;
    }

    /// <summary>Replaces the current show tick labels.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetShowTickLabels(bool value)
    {
        ShowTickLabels = value;
    }

    /// <summary>Returns the current show minor ticks.</summary>
    /// <returns>The current field value.</returns>
    public bool GetShowMinorTicks()
    {
        return ShowMinorTicks;
    }

    /// <summary>Replaces the current show minor ticks.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetShowMinorTicks(bool value)
    {
        ShowMinorTicks = value;
    }

    /// <summary>Returns the current show grid lines.</summary>
    /// <returns>The current field value.</returns>
    public bool GetShowGridLines()
    {
        return ShowGridLines;
    }

    /// <summary>Replaces the current show grid lines.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetShowGridLines(bool value)
    {
        ShowGridLines = value;
    }

    /// <summary>Returns the current show zero line.</summary>
    /// <returns>The current field value.</returns>
    public bool GetShowZeroLine()
    {
        return ShowZeroLine;
    }

    /// <summary>Replaces the current show zero line.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetShowZeroLine(bool value)
    {
        ShowZeroLine = value;
    }

    /// <summary>Returns the current show grid bands.</summary>
    /// <returns>The current field value.</returns>
    public bool GetShowGridBands()
    {
        return ShowGridBands;
    }

    /// <summary>Replaces the current show grid bands.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetShowGridBands(bool value)
    {
        ShowGridBands = value;
    }

    /// <summary>Returns the current auto skip tick labels.</summary>
    /// <returns>The current field value.</returns>
    public bool GetAutoSkipTickLabels()
    {
        return AutoSkipTickLabels;
    }

    /// <summary>Replaces the current auto skip tick labels.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetAutoSkipTickLabels(bool value)
    {
        AutoSkipTickLabels = value;
    }

    /// <summary>Returns the current tick formatter.</summary>
    /// <returns>The current field value.</returns>
    public IChartTickFormatter GetTickFormatter()
    {
        return TickFormatter;
    }
}
