namespace chart;
/// <summary>
/// Specifies the supported encoding channel values.
/// </summary>
public enum ChartEncodingChannel
{
    /// <summary>
    /// Uses the x option for the encoding channel.
    /// </summary>
    X = 0,
    /// <summary>
    /// Uses the y option for the encoding channel.
    /// </summary>
    Y = 1,
    /// <summary>
    /// Uses the x2 option for the encoding channel.
    /// </summary>
    X2 = 2,
    /// <summary>
    /// Uses the y2 option for the encoding channel.
    /// </summary>
    Y2 = 3,
    /// <summary>
    /// Uses the color option for the encoding channel.
    /// </summary>
    Color = 4,
    /// <summary>
    /// Uses the size option for the encoding channel.
    /// </summary>
    Size = 5,
    /// <summary>
    /// Uses the shape option for the encoding channel.
    /// </summary>
    Shape = 6,
    /// <summary>
    /// Uses the opacity option for the encoding channel.
    /// </summary>
    Opacity = 7,
    /// <summary>
    /// Uses the text option for the encoding channel.
    /// </summary>
    Text = 8,
    /// <summary>
    /// Uses the tooltip option for the encoding channel.
    /// </summary>
    Tooltip = 9,
    /// <summary>
    /// Uses the order option for the encoding channel.
    /// </summary>
    Order = 10,
    /// <summary>
    /// Uses the detail option for the encoding channel.
    /// </summary>
    Detail = 11,
    /// <summary>
    /// Uses the row option for the encoding channel.
    /// </summary>
    Row = 12,
    /// <summary>
    /// Uses the column option for the encoding channel.
    /// </summary>
    Column = 13,
    /// <summary>Uses radial distance as a position channel.</summary>
    Radius = 14,
    /// <summary>Uses an angle in radians as a position channel.</summary>
    Angle = 15,
    /// <summary>Uses latitude as a position channel.</summary>
    Latitude = 16,
    /// <summary>Uses longitude as a position channel.</summary>
    Longitude = 17,
    /// <summary>Uses stroke color as a mark channel.</summary>
    StrokeColor = 18,
    /// <summary>Uses fill color as a mark channel.</summary>
    FillColor = 19,
    /// <summary>Uses stroke width as a mark channel.</summary>
    StrokeWidth = 20,
    /// <summary>Uses alternating dash and gap length as a line channel.</summary>
    Dash = 21,
    /// <summary>Uses text displayed beside a mark as a value-label channel.</summary>
    ValueLabel = 22,
    /// <summary>Uses the series rendering order as a z-index channel.</summary>
    ZIndex = 23,
    /// <summary>Uses a categorical field to split rows into wrapped facets.</summary>
    Wrap = 24
}
