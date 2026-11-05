namespace chart;
/// <summary>
/// Represents a series accessibility used by the chart model or rendering pipeline.
/// </summary>
public class ChartSeriesAccessibility : IChartSeriesAccessibility
{
    internal string SeriesId;
    internal string Description;
    internal ChartPointAccessibility[] Points;
    internal int PointCount;
    internal bool HasNonColorCues;
    internal ChartMarkerShape CueShape;
    internal double[] CueDashArray;
    internal string CueLabel;
    internal string CuePatternDescription;
    /// <summary>
    /// Initializes a series accessibility from the supplied series id and description.
    /// </summary>
    /// <param name = "seriesId">The series id.</param>
    /// <param name = "description">The description.</param>
    public ChartSeriesAccessibility(string seriesId, string description)
    {
        ChartPlot.ValidateId(seriesId);
        ChartTextBlock.ValidateText(description, "description");
        SeriesId = seriesId;
        Description = description;
        Points = new ChartPointAccessibility[4];
        PointCount = 0;
        HasNonColorCues = false;
        CueShape = ChartMarkerShape.Circle;
        CueDashArray = new double[0];
        CueLabel = "";
        CuePatternDescription = "";
    }

    /// <summary>
    /// Sets the description configuration.
    /// </summary>
    /// <param name = "description">The description.</param>
    public void SetDescription(string description)
    {
        ChartTextBlock.ValidateText(description, "description");
        Description = description;
    }

    /// <summary>Describes the shape, dash, label, and pattern cues used in addition to color.</summary>
    /// <param name = "shape">The marker shape cue.</param>
    /// <param name = "dashArray">The positive alternating dash and gap lengths, or an empty array.</param>
    /// <param name = "label">The visible value or series label cue, or an empty string.</param>
    /// <param name = "patternDescription">The fill or stroke pattern description, or an empty string.</param>
    public void SetNonColorCues(ChartMarkerShape shape, double[] dashArray, string label, string patternDescription)
    {
        ChartRenderCommand.ValidateMarkerShape(shape);
        if (dashArray == null)
        {
            throw new System.ArgumentNullException("dashArray");
        }

        int index = 0;
        while (index < dashArray.Length)
        {
            ChartRenderStyle.ValidatePositive(dashArray[index], "dashArray");
            index = index + 1;
        }

        ChartTextBlock.ValidateText(label, "label");
        ChartTextBlock.ValidateText(patternDescription, "patternDescription");
        HasNonColorCues = true;
        CueShape = shape;
        CueDashArray = ChartStyle.CopyDashArrayAllowEmpty(dashArray);
        CueLabel = label;
        CuePatternDescription = patternDescription;
    }

    /// <summary>Determines whether non-color cue metadata is configured.</summary>
    /// <returns><c>true</c> when cue metadata is configured.</returns>
    public bool UsesNonColorCues()
    {
        return HasNonColorCues;
    }

    /// <summary>Returns the marker shape cue.</summary>
    /// <returns>The configured marker shape.</returns>
    public ChartMarkerShape GetCueShape()
    {
        return CueShape;
    }

    /// <summary>Returns an independent copy of the dash cue.</summary>
    /// <returns>The configured dash and gap lengths.</returns>
    public double[] CopyCueDashArray()
    {
        return ChartStyle.CopyDashArrayAllowEmpty(CueDashArray);
    }

    /// <summary>Returns the visible label cue.</summary>
    /// <returns>The configured label.</returns>
    public string GetCueLabel()
    {
        return CueLabel;
    }

    /// <summary>Returns the pattern cue description.</summary>
    /// <returns>The configured pattern description.</returns>
    public string GetCuePatternDescription()
    {
        return CuePatternDescription;
    }

    /// <summary>
    /// Adds the supplied point description to this object.
    /// </summary>
    /// <param name = "pointId">The point id.</param>
    /// <param name = "description">The description.</param>
    /// <param name = "rowIndex">The zero-based row index.</param>
    /// <returns>The resulting point accessibility.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public ChartPointAccessibility AddPointDescription(string pointId, string description, int rowIndex)
    {
        if (PointIndex(pointId) >= 0)
        {
            throw new System.ArgumentException("A point with the same id already has an accessibility description.");
        }

        EnsurePointCapacity(PointCount + 1);
        Points[PointCount] = new ChartPointAccessibility(pointId, description, rowIndex);
        PointCount = PointCount + 1;
        return Points[PointCount - 1];
    }

    /// <summary>
    /// Returns the zero-based point index.
    /// </summary>
    /// <param name = "pointId">The point id.</param>
    /// <returns>The resulting int.</returns>
    public int PointIndex(string pointId)
    {
        ChartPlot.ValidateId(pointId);
        int index = 0;
        while (index < PointCount)
        {
            if (Points[index].PointId == pointId)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    /// <summary>
    /// Returns the requested point.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting point accessibility.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartPointAccessibility PointAt(int index)
    {
        if (index < 0 || index >= PointCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Points[index];
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartSeriesAccessibility Copy()
    {
        ChartSeriesAccessibility series = new ChartSeriesAccessibility(SeriesId, Description);
        series.EnsurePointCapacity(PointCount);
        int index = 0;
        while (index < PointCount)
        {
            series.Points[index] = Points[index].Copy();
            index = index + 1;
        }

        series.PointCount = PointCount;
        series.HasNonColorCues = HasNonColorCues;
        series.CueShape = CueShape;
        series.CueDashArray = ChartStyle.CopyDashArrayAllowEmpty(CueDashArray);
        series.CueLabel = CueLabel;
        series.CuePatternDescription = CuePatternDescription;
        return series;
    }

    internal void EnsurePointCapacity(int capacity)
    {
        if (capacity <= Points.Length)
        {
            return;
        }

        int newCapacity = ChartArrayUtilities.GrowthCapacity(Points.Length, capacity);
        ChartPointAccessibility[] points = new ChartPointAccessibility[newCapacity];
        System.Array.Copy(Points, points, PointCount);
        Points = points;
    }

    /// <summary>Returns the current series id.</summary>
    /// <returns>The current field value.</returns>
    public string GetSeriesId()
    {
        return SeriesId;
    }

    /// <summary>Replaces the current series id.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetSeriesId(string value)
    {
        SeriesId = value;
    }

    /// <summary>Returns the current description.</summary>
    /// <returns>The current field value.</returns>
    public string GetDescription()
    {
        return Description;
    }

    /// <summary>Returns the current points.</summary>
    /// <returns>The current field value.</returns>
    public ChartPointAccessibility[] GetPoints()
    {
        return Points;
    }

    /// <summary>Replaces the current points.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetPoints(ChartPointAccessibility[] value)
    {
        Points = value;
    }

    /// <summary>Returns the current point count.</summary>
    /// <returns>The current field value.</returns>
    public int GetPointCount()
    {
        return PointCount;
    }

    /// <summary>Replaces the current point count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetPointCount(int value)
    {
        PointCount = value;
    }
}
