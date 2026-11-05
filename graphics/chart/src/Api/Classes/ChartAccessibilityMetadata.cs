namespace chart;
/// <summary>
/// Represents an accessibility metadata used by the chart model or rendering pipeline.
/// </summary>
public class ChartAccessibilityMetadata : IChartAccessibilityMetadata
{
    internal string Title;
    internal string Description;
    internal ChartSeriesAccessibility[] Series;
    internal int SeriesCount;
    internal ChartDataTableSidecarPlan? DataTableSidecar;
    internal ChartSelectionSidecar? SelectionSidecar;
    /// <summary>
    /// Initializes an empty accessibility metadata with library defaults.
    /// </summary>
    public ChartAccessibilityMetadata()
    {
        Title = "";
        Description = "";
        Series = new ChartSeriesAccessibility[4];
        SeriesCount = 0;
        DataTableSidecar = null;
        SelectionSidecar = null;
    }

    /// <summary>
    /// Sets the title description configuration.
    /// </summary>
    /// <param name = "title">The title.</param>
    /// <param name = "description">The description.</param>
    public void SetTitleDescription(string title, string description)
    {
        SetTitle(title);
        SetDescription(description);
    }

    /// <summary>
    /// Sets the title configuration.
    /// </summary>
    /// <param name = "title">The title.</param>
    public void SetTitle(string title)
    {
        ChartTextBlock.ValidateText(title, "title");
        Title = title;
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

    /// <summary>
    /// Adds the supplied series description to this object.
    /// </summary>
    /// <param name = "seriesId">The series id.</param>
    /// <param name = "description">The description.</param>
    /// <returns>The resulting series accessibility.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public ChartSeriesAccessibility AddSeriesDescription(string seriesId, string description)
    {
        if (SeriesIndex(seriesId) >= 0)
        {
            throw new System.ArgumentException("A series with the same id already has an accessibility description.");
        }

        EnsureSeriesCapacity(SeriesCount + 1);
        Series[SeriesCount] = new ChartSeriesAccessibility(seriesId, description);
        SeriesCount = SeriesCount + 1;
        return Series[SeriesCount - 1];
    }

    /// <summary>
    /// Adds the supplied point description to this object.
    /// </summary>
    /// <param name = "seriesId">The series id.</param>
    /// <param name = "pointId">The point id.</param>
    /// <param name = "description">The description.</param>
    /// <param name = "rowIndex">The zero-based row index.</param>
    /// <returns>The resulting point accessibility.</returns>
    public ChartPointAccessibility AddPointDescription(string seriesId, string pointId, string description, int rowIndex)
    {
        ChartSeriesAccessibility? series = FindSeries(seriesId);
        if (series == null)
        {
            series = AddSeriesDescription(seriesId, "");
        }

        return series.AddPointDescription(pointId, description, rowIndex);
    }

    /// <summary>
    /// Sets the data table sidecar configuration.
    /// </summary>
    /// <param name = "plan">The plan.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void SetDataTableSidecar(ChartDataTableSidecarPlan plan)
    {
        if (plan == null)
        {
            throw new System.ArgumentNullException("plan");
        }

        DataTableSidecar = plan.Copy();
    }

    /// <summary>
    /// Replaces the sidecar plan with column metadata copied from a data table.
    /// </summary>
    /// <param name = "tableId">The table id.</param>
    /// <param name = "caption">The caption.</param>
    /// <param name = "table">The column-oriented data table that supplies encoded fields.</param>
    public void PlanDataTableSidecar(string tableId, string caption, ChartDataTable table)
    {
        DataTableSidecar = ChartDataTableSidecarPlan.FromTable(tableId, caption, table);
    }

    /// <summary>Sets deterministic metadata for linked selections.</summary>
    /// <param name="sidecar">The linked-selection sidecar snapshot.</param>
    public void SetSelectionSidecar(ChartSelectionSidecar sidecar)
    {
        if (sidecar == null)
        {
            throw new System.ArgumentNullException("sidecar");
        }

        SelectionSidecar = sidecar.Copy();
    }

    /// <summary>
    /// Determines whether any title, description, series, or data-table metadata is configured.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool HasMetadata()
    {
        return Title.Length > 0 || Description.Length > 0 || SeriesCount > 0 || DataTableSidecar != null || (SelectionSidecar != null && (SelectionSidecar.SelectionCount > 0 || SelectionSidecar.BrushCount > 0));
    }

    internal void ApplyToSvgWriter(SvgWriter writer)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        writer.AddTitleDescription(Title, Description);
        string metadata = MetadataText();
        if (metadata.Length > 0)
        {
            writer.AddMetadata(metadata);
        }
    }

    /// <summary>
    /// Applies the metadata text operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting string.</returns>
    public string MetadataText()
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < SeriesCount)
        {
            ChartSeriesAccessibility series = Series[index];
            builder.Append("series ");
            builder.Append(series.SeriesId);
            builder.Append(": ");
            builder.Append(series.Description);
            if (series.HasNonColorCues)
            {
                builder.Append("; non-color-cues=");
                ChartAccessibilityOutput.AppendCueText(builder, series);
            }
            int pointIndex = 0;
            while (pointIndex < series.PointCount)
            {
                ChartPointAccessibility point = series.PointAt(pointIndex);
                builder.Append("\npoint ");
                builder.Append(series.SeriesId);
                builder.Append("/");
                builder.Append(point.PointId);
                builder.Append(" row ");
                builder.Append(point.RowIndex.ToString(System.Globalization.CultureInfo.InvariantCulture));
                builder.Append(": ");
                builder.Append(point.Description);
                pointIndex = pointIndex + 1;
            }

            if (index < SeriesCount - 1 || DataTableSidecar != null || SelectionSidecar != null)
            {
                builder.Append("\n");
            }

            index = index + 1;
        }

        if (DataTableSidecar != null)
        {
            builder.Append(DataTableSidecar.SummaryText());
            if (SelectionSidecar != null)
            {
                builder.Append("\n");
            }
        }

        if (SelectionSidecar != null)
        {
            builder.Append(SelectionSidecar.MetadataText());
        }

        return builder.ToString();
    }

    /// <summary>Builds a structured plain-text alternative containing descriptions, cues, and sidecars.</summary>
    /// <returns>The deterministic plain-text alternative.</returns>
    public string StructuredText()
    {
        return ChartAccessibilityOutput.StructuredText(this);
    }

    /// <summary>Builds a structured JSON alternative containing descriptions, cues, and sidecars.</summary>
    /// <returns>The deterministic JSON alternative.</returns>
    public string JsonAlternative()
    {
        return ChartAccessibilityOutput.JsonAlternative(this);
    }

    /// <summary>
    /// Finds series using the supplied data and configuration.
    /// </summary>
    /// <param name = "seriesId">The series id.</param>
    /// <returns>The matching value, or <c>null</c> when no match exists.</returns>
    public ChartSeriesAccessibility? FindSeries(string seriesId)
    {
        int index = SeriesIndex(seriesId);
        if (index < 0)
        {
            return null;
        }

        return Series[index];
    }

    /// <summary>
    /// Returns the zero-based series index.
    /// </summary>
    /// <param name = "seriesId">The series id.</param>
    /// <returns>The resulting int.</returns>
    public int SeriesIndex(string seriesId)
    {
        ChartPlot.ValidateId(seriesId);
        int index = 0;
        while (index < SeriesCount)
        {
            if (Series[index].SeriesId == seriesId)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    /// <summary>
    /// Returns the requested series.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting series accessibility.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartSeriesAccessibility SeriesAt(int index)
    {
        if (index < 0 || index >= SeriesCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Series[index];
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartAccessibilityMetadata Copy()
    {
        ChartAccessibilityMetadata metadata = new ChartAccessibilityMetadata();
        metadata.Title = Title;
        metadata.Description = Description;
        metadata.EnsureSeriesCapacity(SeriesCount);
        int index = 0;
        while (index < SeriesCount)
        {
            metadata.Series[index] = Series[index].Copy();
            index = index + 1;
        }

        metadata.SeriesCount = SeriesCount;
        if (DataTableSidecar != null)
        {
            metadata.DataTableSidecar = DataTableSidecar.Copy();
        }

        if (SelectionSidecar != null)
        {
            metadata.SelectionSidecar = SelectionSidecar.Copy();
        }

        return metadata;
    }

    internal void EnsureSeriesCapacity(int capacity)
    {
        if (capacity <= Series.Length)
        {
            return;
        }

        int newCapacity = ChartArrayUtilities.GrowthCapacity(Series.Length, capacity);
        ChartSeriesAccessibility[] series = new ChartSeriesAccessibility[newCapacity];
        System.Array.Copy(Series, series, SeriesCount);
        Series = series;
    }

    /// <summary>Returns the current title.</summary>
    /// <returns>The current field value.</returns>
    public string GetTitle()
    {
        return Title;
    }

    /// <summary>Returns the current description.</summary>
    /// <returns>The current field value.</returns>
    public string GetDescription()
    {
        return Description;
    }

    /// <summary>Returns the current series.</summary>
    /// <returns>The current field value.</returns>
    public ChartSeriesAccessibility[] GetSeries()
    {
        return Series;
    }

    /// <summary>Replaces the current series.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetSeries(ChartSeriesAccessibility[] value)
    {
        Series = value;
    }

    /// <summary>Returns the current series count.</summary>
    /// <returns>The current field value.</returns>
    public int GetSeriesCount()
    {
        return SeriesCount;
    }

    /// <summary>Replaces the current series count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetSeriesCount(int value)
    {
        SeriesCount = value;
    }

    /// <summary>Returns the current data table sidecar.</summary>
    /// <returns>The current field value.</returns>
    public ChartDataTableSidecarPlan? GetDataTableSidecar()
    {
        return DataTableSidecar;
    }

    /// <summary>Returns linked-selection sidecar metadata.</summary>
    /// <returns>The sidecar, or <c>null</c> when none is configured.</returns>
    public ChartSelectionSidecar? GetSelectionSidecar()
    {
        return SelectionSidecar;
    }
}
