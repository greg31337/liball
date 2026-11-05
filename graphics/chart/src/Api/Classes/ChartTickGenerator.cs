namespace chart;
/// <summary>Provides the public chart tick generator operations.</summary>
public class ChartTickGenerator : IChartTickGenerator
{
    /// <summary>Initializes a stateless chart tick generator facade.</summary>
    public ChartTickGenerator()
    {
    }

    /// <summary>
    /// Generates numeric ticks from the supplied data and configuration.
    /// </summary>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    /// <param name = "tickCount">The requested number of tick entries.</param>
    /// <returns>The resulting tick array.</returns>
    public ChartTick[] GenerateNumericTicks(double minimum, double maximum, int tickCount)
    {
        return ChartTickGeneratorOperations.GenerateNumericTicks(minimum, maximum, tickCount);
    }

    /// <summary>
    /// Generates explicit ticks from the supplied data and configuration.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting tick array.</returns>
    public ChartTick[] GenerateExplicitTicks(double[] values)
    {
        return ChartTickGeneratorOperations.GenerateExplicitTicks(values);
    }

    /// <summary>
    /// Generates log ticks from the supplied data and configuration.
    /// </summary>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    /// <param name = "baseValue">The base value.</param>
    /// <returns>The resulting tick array.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public ChartTick[] GenerateLogTicks(double minimum, double maximum, double baseValue)
    {
        return ChartTickGeneratorOperations.GenerateLogTicks(minimum, maximum, baseValue);
    }

    /// <summary>Generates minor logarithmic ticks between major powers.</summary>
    /// <param name = "minimum">The inclusive lower domain boundary.</param>
    /// <param name = "maximum">The inclusive upper domain boundary.</param>
    /// <param name = "baseValue">The integer logarithm base from two through 100.</param>
    /// <returns>The minor logarithmic ticks.</returns>
    public ChartTick[] GenerateMinorLogTicks(double minimum, double maximum, double baseValue)
    {
        return ChartTickGeneratorOperations.GenerateMinorLogTicks(minimum, maximum, baseValue);
    }

    /// <summary>
    /// Generates time ticks from the supplied data and configuration.
    /// </summary>
    /// <param name = "start">The start.</param>
    /// <param name = "end">The end.</param>
    /// <param name = "tickCount">The requested number of tick entries.</param>
    /// <returns>The resulting tick array.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartTick[] GenerateTimeTicks(System.DateTime start, System.DateTime end, int tickCount)
    {
        return ChartTickGeneratorOperations.GenerateTimeTicks(start, end, tickCount);
    }

    /// <summary>Generates ticks aligned to a calendar interval.</summary>
    /// <param name = "start">The inclusive start.</param>
    /// <param name = "end">The inclusive end.</param>
    /// <param name = "interval">The calendar interval.</param>
    /// <param name = "step">The positive number of intervals between ticks.</param>
    /// <returns>The aligned time ticks.</returns>
    public ChartTick[] GenerateCalendarTicks(System.DateTime start, System.DateTime end, ChartTimeInterval interval, int step)
    {
        return ChartTickGeneratorOperations.GenerateCalendarTicks(start, end, interval, step);
    }

    /// <summary>
    /// Generates category ticks from the supplied data and configuration.
    /// </summary>
    /// <param name = "categories">The categories.</param>
    /// <returns>The resulting tick array.</returns>
    public ChartTick[] GenerateCategoryTicks(string[] categories)
    {
        return ChartTickGeneratorOperations.GenerateCategoryTicks(categories);
    }

    /// <summary>
    /// Generates radial ticks from the supplied data and configuration.
    /// </summary>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    /// <param name = "tickCount">The requested number of tick entries.</param>
    /// <returns>The resulting tick array.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartTick[] GenerateRadialTicks(double maximum, int tickCount)
    {
        return ChartTickGeneratorOperations.GenerateRadialTicks(maximum, tickCount);
    }

    /// <summary>
    /// Generates angular ticks from the supplied data and configuration.
    /// </summary>
    /// <param name = "tickCount">The requested number of tick entries.</param>
    /// <returns>The resulting tick array.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartTick[] GenerateAngularTicks(int tickCount)
    {
        return ChartTickGeneratorOperations.GenerateAngularTicks(tickCount);
    }
}
