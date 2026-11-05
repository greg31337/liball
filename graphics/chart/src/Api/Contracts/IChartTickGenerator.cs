namespace chart;
/// <summary>Defines substitutable chart tick generator operations.</summary>
public interface IChartTickGenerator
{
    /// <summary>
    /// Generates numeric ticks from the supplied data and configuration.
    /// </summary>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    /// <param name = "tickCount">The requested number of tick entries.</param>
    /// <returns>The resulting tick array.</returns>
    ChartTick[] GenerateNumericTicks(double minimum, double maximum, int tickCount);
    /// <summary>
    /// Generates explicit ticks from the supplied data and configuration.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting tick array.</returns>
    ChartTick[] GenerateExplicitTicks(double[] values);
    /// <summary>
    /// Generates log ticks from the supplied data and configuration.
    /// </summary>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    /// <param name = "baseValue">The base value.</param>
    /// <returns>The resulting tick array.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    ChartTick[] GenerateLogTicks(double minimum, double maximum, double baseValue);
    /// <summary>Generates minor logarithmic ticks between major powers.</summary>
    /// <param name = "minimum">The inclusive lower domain boundary.</param>
    /// <param name = "maximum">The inclusive upper domain boundary.</param>
    /// <param name = "baseValue">The integer logarithm base from two through 100.</param>
    /// <returns>The minor logarithmic ticks.</returns>
    ChartTick[] GenerateMinorLogTicks(double minimum, double maximum, double baseValue);
    /// <summary>
    /// Generates time ticks from the supplied data and configuration.
    /// </summary>
    /// <param name = "start">The start.</param>
    /// <param name = "end">The end.</param>
    /// <param name = "tickCount">The requested number of tick entries.</param>
    /// <returns>The resulting tick array.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    ChartTick[] GenerateTimeTicks(System.DateTime start, System.DateTime end, int tickCount);
    /// <summary>Generates ticks aligned to a calendar interval.</summary>
    /// <param name = "start">The inclusive start.</param>
    /// <param name = "end">The inclusive end.</param>
    /// <param name = "interval">The calendar interval.</param>
    /// <param name = "step">The positive number of intervals between ticks.</param>
    /// <returns>The aligned time ticks.</returns>
    ChartTick[] GenerateCalendarTicks(System.DateTime start, System.DateTime end, ChartTimeInterval interval, int step);
    /// <summary>
    /// Generates category ticks from the supplied data and configuration.
    /// </summary>
    /// <param name = "categories">The categories.</param>
    /// <returns>The resulting tick array.</returns>
    ChartTick[] GenerateCategoryTicks(string[] categories);
    /// <summary>
    /// Generates radial ticks from the supplied data and configuration.
    /// </summary>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    /// <param name = "tickCount">The requested number of tick entries.</param>
    /// <returns>The resulting tick array.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    ChartTick[] GenerateRadialTicks(double maximum, int tickCount);
    /// <summary>
    /// Generates angular ticks from the supplied data and configuration.
    /// </summary>
    /// <param name = "tickCount">The requested number of tick entries.</param>
    /// <returns>The resulting tick array.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    ChartTick[] GenerateAngularTicks(int tickCount);
}
