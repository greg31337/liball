namespace chart;
/// <summary>Creates and converts chart encoding binding values.</summary>
public class ChartEncodingBindingFactory : IChartEncodingBindingFactory
{
    /// <summary>Initializes a stateless chart encoding binding factory facade.</summary>
    public ChartEncodingBindingFactory()
    {
    }

    /// <summary>
    /// Creates the requested number constant from the supplied data and configuration.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <param name = "value">The value.</param>
    /// <returns>The newly created encoding binding.</returns>
    public ChartEncodingBinding CreateNumberConstant(ChartEncodingChannel channel, double value)
    {
        return ChartEncodingBinding.CreateNumberConstant(channel, value);
    }

    /// <summary>
    /// Creates the requested text constant from the supplied data and configuration.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <param name = "value">The value.</param>
    /// <returns>The newly created encoding binding.</returns>
    public ChartEncodingBinding CreateTextConstant(ChartEncodingChannel channel, string value)
    {
        return ChartEncodingBinding.CreateTextConstant(channel, value);
    }

    /// <summary>
    /// Creates the requested color constant from the supplied data and configuration.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <param name = "value">The value.</param>
    /// <returns>The newly created encoding binding.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartEncodingBinding CreateColorConstant(ChartEncodingChannel channel, ChartColor value)
    {
        return ChartEncodingBinding.CreateColorConstant(channel, value);
    }

    /// <summary>
    /// Creates the requested shape constant from the supplied data and configuration.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <param name = "value">The value.</param>
    /// <returns>The newly created encoding binding.</returns>
    public ChartEncodingBinding CreateShapeConstant(ChartEncodingChannel channel, ChartMarkerShape value)
    {
        return ChartEncodingBinding.CreateShapeConstant(channel, value);
    }

    /// <summary>
    /// Applies the channel name operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <returns>The resulting string.</returns>
    public string ChannelName(ChartEncodingChannel channel)
    {
        return ChartEncodingBinding.ChannelName(channel);
    }
}
