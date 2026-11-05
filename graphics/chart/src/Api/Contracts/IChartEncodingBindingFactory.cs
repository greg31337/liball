namespace chart;
/// <summary>Defines substitutable chart encoding binding factory operations.</summary>
public interface IChartEncodingBindingFactory
{
    /// <summary>
    /// Creates the requested number constant from the supplied data and configuration.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <param name = "value">The value.</param>
    /// <returns>The newly created encoding binding.</returns>
    ChartEncodingBinding CreateNumberConstant(ChartEncodingChannel channel, double value);
    /// <summary>
    /// Creates the requested text constant from the supplied data and configuration.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <param name = "value">The value.</param>
    /// <returns>The newly created encoding binding.</returns>
    ChartEncodingBinding CreateTextConstant(ChartEncodingChannel channel, string value);
    /// <summary>
    /// Creates the requested color constant from the supplied data and configuration.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <param name = "value">The value.</param>
    /// <returns>The newly created encoding binding.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartEncodingBinding CreateColorConstant(ChartEncodingChannel channel, ChartColor value);
    /// <summary>
    /// Creates the requested shape constant from the supplied data and configuration.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <param name = "value">The value.</param>
    /// <returns>The newly created encoding binding.</returns>
    ChartEncodingBinding CreateShapeConstant(ChartEncodingChannel channel, ChartMarkerShape value);
    /// <summary>
    /// Applies the channel name operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <returns>The resulting string.</returns>
    string ChannelName(ChartEncodingChannel channel);
}
