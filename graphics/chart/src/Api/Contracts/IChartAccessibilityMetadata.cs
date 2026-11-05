namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartAccessibilityMetadata"/>.</summary>
public interface IChartAccessibilityMetadata
{
    /// <summary>
    /// Sets the title description configuration.
    /// </summary>
    /// <param name = "title">The title.</param>
    /// <param name = "description">The description.</param>
    void SetTitleDescription(string title, string description);
    /// <summary>
    /// Sets the title configuration.
    /// </summary>
    /// <param name = "title">The title.</param>
    void SetTitle(string title);
    /// <summary>
    /// Sets the description configuration.
    /// </summary>
    /// <param name = "description">The description.</param>
    void SetDescription(string description);
    /// <summary>
    /// Adds the supplied series description to this object.
    /// </summary>
    /// <param name = "seriesId">The series id.</param>
    /// <param name = "description">The description.</param>
    /// <returns>The resulting series accessibility.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    ChartSeriesAccessibility AddSeriesDescription(string seriesId, string description);
    /// <summary>
    /// Adds the supplied point description to this object.
    /// </summary>
    /// <param name = "seriesId">The series id.</param>
    /// <param name = "pointId">The point id.</param>
    /// <param name = "description">The description.</param>
    /// <param name = "rowIndex">The zero-based row index.</param>
    /// <returns>The resulting point accessibility.</returns>
    ChartPointAccessibility AddPointDescription(string seriesId, string pointId, string description, int rowIndex);
    /// <summary>
    /// Sets the data table sidecar configuration.
    /// </summary>
    /// <param name = "plan">The plan.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    void SetDataTableSidecar(ChartDataTableSidecarPlan plan);
    /// <summary>
    /// Replaces the sidecar plan with column metadata copied from a data table.
    /// </summary>
    /// <param name = "tableId">The table id.</param>
    /// <param name = "caption">The caption.</param>
    /// <param name = "table">The column-oriented data table that supplies encoded fields.</param>
    void PlanDataTableSidecar(string tableId, string caption, ChartDataTable table);
    /// <summary>Sets deterministic metadata for linked selections.</summary>
    /// <param name="sidecar">The linked-selection sidecar snapshot.</param>
    void SetSelectionSidecar(ChartSelectionSidecar sidecar);
    /// <summary>
    /// Determines whether any title, description, series, or data-table metadata is configured.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    bool HasMetadata();
    /// <summary>
    /// Applies the metadata text operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting string.</returns>
    string MetadataText();
    /// <summary>Builds a structured plain-text alternative containing descriptions, cues, and sidecars.</summary>
    /// <returns>The deterministic plain-text alternative.</returns>
    string StructuredText();
    /// <summary>Builds a structured JSON alternative containing descriptions, cues, and sidecars.</summary>
    /// <returns>The deterministic JSON alternative.</returns>
    string JsonAlternative();
    /// <summary>
    /// Finds series using the supplied data and configuration.
    /// </summary>
    /// <param name = "seriesId">The series id.</param>
    /// <returns>The matching value, or <c>null</c> when no match exists.</returns>
    ChartSeriesAccessibility? FindSeries(string seriesId);
    /// <summary>
    /// Returns the zero-based series index.
    /// </summary>
    /// <param name = "seriesId">The series id.</param>
    /// <returns>The resulting int.</returns>
    int SeriesIndex(string seriesId);
    /// <summary>
    /// Returns the requested series.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting series accessibility.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    ChartSeriesAccessibility SeriesAt(int index);
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartAccessibilityMetadata Copy();
    /// <summary>Returns the current title.</summary>
    /// <returns>The current field value.</returns>
    string GetTitle();
    /// <summary>Returns the current description.</summary>
    /// <returns>The current field value.</returns>
    string GetDescription();
    /// <summary>Returns the current series.</summary>
    /// <returns>The current field value.</returns>
    ChartSeriesAccessibility[] GetSeries();
    /// <summary>Replaces the current series.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetSeries(ChartSeriesAccessibility[] value);
    /// <summary>Returns the current series count.</summary>
    /// <returns>The current field value.</returns>
    int GetSeriesCount();
    /// <summary>Replaces the current series count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetSeriesCount(int value);
    /// <summary>Returns the current data table sidecar.</summary>
    /// <returns>The current field value.</returns>
    ChartDataTableSidecarPlan? GetDataTableSidecar();
    /// <summary>Returns linked-selection sidecar metadata.</summary>
    /// <returns>The sidecar, or <c>null</c> when none is configured.</returns>
    ChartSelectionSidecar? GetSelectionSidecar();
}
