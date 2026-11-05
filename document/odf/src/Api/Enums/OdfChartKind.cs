namespace odf;

/// <summary>
/// Identifies a standard OpenDocument chart class.
/// </summary>
public enum OdfChartKind
{
    /// <summary>
    /// The chart kind is absent or unrecognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// A bar or column chart.
    /// </summary>
    Bar = 1,
    /// <summary>
    /// A line chart.
    /// </summary>
    Line = 2,
    /// <summary>
    /// An area chart.
    /// </summary>
    Area = 3,
    /// <summary>
    /// A pie chart.
    /// </summary>
    Pie = 4,
    /// <summary>
    /// A ring chart.
    /// </summary>
    Ring = 5,
    /// <summary>
    /// A scatter chart.
    /// </summary>
    Scatter = 6,
    /// <summary>
    /// A radar chart.
    /// </summary>
    Radar = 7,
    /// <summary>
    /// A filled radar chart.
    /// </summary>
    FilledRadar = 8,
    /// <summary>
    /// A stock chart.
    /// </summary>
    Stock = 9,
    /// <summary>
    /// A bubble chart.
    /// </summary>
    Bubble = 10,
    /// <summary>
    /// A surface chart.
    /// </summary>
    Surface = 11,
    /// <summary>
    /// A Gantt chart.
    /// </summary>
    Gantt = 12
}
