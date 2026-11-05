namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartSeriesAccessibility"/>.</summary>
public interface IChartSeriesAccessibility
{
    /// <summary>
    /// Sets the description configuration.
    /// </summary>
    /// <param name = "description">The description.</param>
    void SetDescription(string description);
    /// <summary>Describes the shape, dash, label, and pattern cues used in addition to color.</summary>
    /// <param name = "shape">The marker shape cue.</param>
    /// <param name = "dashArray">The positive alternating dash and gap lengths, or an empty array.</param>
    /// <param name = "label">The visible value or series label cue, or an empty string.</param>
    /// <param name = "patternDescription">The fill or stroke pattern description, or an empty string.</param>
    void SetNonColorCues(ChartMarkerShape shape, double[] dashArray, string label, string patternDescription);
    /// <summary>Determines whether non-color cue metadata is configured.</summary>
    /// <returns><c>true</c> when cue metadata is configured.</returns>
    bool UsesNonColorCues();
    /// <summary>Returns the marker shape cue.</summary>
    /// <returns>The configured marker shape.</returns>
    ChartMarkerShape GetCueShape();
    /// <summary>Returns an independent copy of the dash cue.</summary>
    /// <returns>The configured dash and gap lengths.</returns>
    double[] CopyCueDashArray();
    /// <summary>Returns the visible label cue.</summary>
    /// <returns>The configured label.</returns>
    string GetCueLabel();
    /// <summary>Returns the pattern cue description.</summary>
    /// <returns>The configured pattern description.</returns>
    string GetCuePatternDescription();
    /// <summary>
    /// Adds the supplied point description to this object.
    /// </summary>
    /// <param name = "pointId">The point id.</param>
    /// <param name = "description">The description.</param>
    /// <param name = "rowIndex">The zero-based row index.</param>
    /// <returns>The resulting point accessibility.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    ChartPointAccessibility AddPointDescription(string pointId, string description, int rowIndex);
    /// <summary>
    /// Returns the zero-based point index.
    /// </summary>
    /// <param name = "pointId">The point id.</param>
    /// <returns>The resulting int.</returns>
    int PointIndex(string pointId);
    /// <summary>
    /// Returns the requested point.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting point accessibility.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    ChartPointAccessibility PointAt(int index);
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartSeriesAccessibility Copy();
    /// <summary>Returns the current series id.</summary>
    /// <returns>The current field value.</returns>
    string GetSeriesId();
    /// <summary>Replaces the current series id.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetSeriesId(string value);
    /// <summary>Returns the current description.</summary>
    /// <returns>The current field value.</returns>
    string GetDescription();
    /// <summary>Returns the current points.</summary>
    /// <returns>The current field value.</returns>
    ChartPointAccessibility[] GetPoints();
    /// <summary>Replaces the current points.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetPoints(ChartPointAccessibility[] value);
    /// <summary>Returns the current point count.</summary>
    /// <returns>The current field value.</returns>
    int GetPointCount();
    /// <summary>Replaces the current point count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetPointCount(int value);
}
