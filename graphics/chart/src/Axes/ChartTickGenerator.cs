namespace chart;

/// <summary>
/// Generates tick values from chart domains and configuration.
/// </summary>
internal static class ChartTickGeneratorOperations
{
    /// <summary>
    /// Generates numeric ticks from the supplied data and configuration.
    /// </summary>
    /// <param name="minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name="maximum">The inclusive upper end of the numeric domain.</param>
    /// <param name="tickCount">The requested number of tick entries.</param>
    /// <returns>The resulting tick array.</returns>
    public static ChartTick[] GenerateNumericTicks(double minimum, double maximum, int tickCount)
    {
        ChartRange domain = ChartScaleUtilities.NiceDomain(minimum, maximum, tickCount);
        ChartTick[] ticks = new ChartTick[tickCount];
        if (tickCount == 1)
        {
            ticks[0] = new ChartTick(ChartTickKind.Numeric, domain.Minimum, "", 0);
            return ticks;
        }

        double step = (domain.Maximum - domain.Minimum) / (tickCount - 1);
        int index = 0;
        while (index < tickCount)
        {
            double value = domain.Minimum + step * index;
            ticks[index] = new ChartTick(ChartTickKind.Numeric, value, "", index);
            index = index + 1;
        }

        return ticks;
    }

    /// <summary>
    /// Generates explicit ticks from the supplied data and configuration.
    /// </summary>
    /// <param name="values">The input values to process.</param>
    /// <returns>The resulting tick array.</returns>
    public static ChartTick[] GenerateExplicitTicks(double[] values)
    {
        ChartStatisticsOperations.ValidateFiniteValues(values);
        ChartTick[] ticks = new ChartTick[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            ticks[index] = new ChartTick(ChartTickKind.Explicit, values[index], "", index);
            index = index + 1;
        }

        return ticks;
    }

    /// <summary>
    /// Generates log ticks from the supplied data and configuration.
    /// </summary>
    /// <param name="minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name="maximum">The inclusive upper end of the numeric domain.</param>
    /// <param name="baseValue">The base value.</param>
    /// <returns>The resulting tick array.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public static ChartTick[] GenerateLogTicks(double minimum, double maximum, double baseValue)
    {
        ChartLinearScale.ValidateDomain(minimum, maximum);
        ChartLinearScale.ValidateFinite(baseValue, "baseValue");
        if (minimum <= 0.0 || maximum <= 0.0 || baseValue <= 0.0 || baseValue == 1.0)
        {
            throw new System.ArgumentException("Log ticks require a positive domain and base.");
        }

        double tolerance = 0.000000000001;
        double minimumPowerValue = System.Math.Ceiling(System.Math.Log(minimum, baseValue) - tolerance);
        double maximumPowerValue = System.Math.Floor(System.Math.Log(maximum, baseValue) + tolerance);
        if (double.IsNaN(minimumPowerValue) || double.IsInfinity(minimumPowerValue) || minimumPowerValue < int.MinValue || minimumPowerValue > int.MaxValue)
        {
            throw new System.ArgumentOutOfRangeException("baseValue");
        }

        if (double.IsNaN(maximumPowerValue) || double.IsInfinity(maximumPowerValue) || maximumPowerValue < int.MinValue || maximumPowerValue > int.MaxValue)
        {
            throw new System.ArgumentOutOfRangeException("baseValue");
        }

        int minimumPower = (int)minimumPowerValue;
        int maximumPower = (int)maximumPowerValue;
        if (maximumPower < minimumPower)
        {
            return new ChartTick[0];
        }

        long countValue = (long)maximumPower - minimumPower + 1;
        if (countValue > int.MaxValue)
        {
            throw new System.ArgumentOutOfRangeException("baseValue");
        }

        int count = (int)countValue;
        ChartTick[] ticks = new ChartTick[count];
        int index = 0;
        while (index < count)
        {
            double value = System.Math.Pow(baseValue, minimumPower + index);
            ticks[index] = new ChartTick(ChartTickKind.Log, value, "", index);
            index = index + 1;
        }

        return ticks;
    }

    internal static ChartTick[] GenerateMinorLogTicks(double minimum, double maximum, double baseValue)
    {
        ChartLinearScale.ValidateDomain(minimum, maximum);
        ChartLinearScale.ValidateFinite(baseValue, "baseValue");
        if (minimum <= 0.0 || maximum <= minimum || baseValue < 2.0 || baseValue > 100.0 || baseValue != System.Math.Floor(baseValue))
        {
            throw new System.ArgumentException("Minor log ticks require an increasing positive domain and an integer base from two through 100.");
        }

        int integerBase = (int)baseValue;
        int minimumPower = (int)System.Math.Floor(System.Math.Log(minimum, baseValue));
        int maximumPower = (int)System.Math.Floor(System.Math.Log(maximum, baseValue));
        System.Collections.Generic.List<ChartTick> ticks = new System.Collections.Generic.List<ChartTick>();
        int power = minimumPower;
        while (power <= maximumPower)
        {
            double major = System.Math.Pow(baseValue, power);
            int multiplier = 2;
            while (multiplier < integerBase)
            {
                double value = major * multiplier;
                if (value >= minimum && value <= maximum)
                {
                    ticks.Add(new ChartTick(ChartTickKind.Log, value, "", ticks.Count));
                }

                multiplier = multiplier + 1;
            }

            power = power + 1;
        }

        return ticks.ToArray();
    }

    /// <summary>
    /// Generates time ticks from the supplied data and configuration.
    /// </summary>
    /// <param name="start">The start.</param>
    /// <param name="end">The end.</param>
    /// <param name="tickCount">The requested number of tick entries.</param>
    /// <returns>The resulting tick array.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public static ChartTick[] GenerateTimeTicks(System.DateTime start, System.DateTime end, int tickCount)
    {
        if (tickCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException("tickCount");
        }

        if (start.Ticks == end.Ticks)
        {
            throw new System.ArgumentException("Time ticks require a non-zero range.");
        }

        ChartTick[] ticks = new ChartTick[tickCount];
        if (tickCount == 1)
        {
            ticks[0] = ChartTick.CreateTime(start, 0);
            return ticks;
        }

        double step = (end.Ticks - start.Ticks) / (double)(tickCount - 1);
        int index = 0;
        while (index < tickCount)
        {
            long ticksValue = start.Ticks + (long)System.Math.Round(step * index);
            ticks[index] = ChartTick.CreateTime(new System.DateTime(ticksValue), index);
            index = index + 1;
        }

        return ticks;
    }

    internal static ChartTick[] GenerateCalendarTicks(System.DateTime start, System.DateTime end, ChartTimeInterval interval, int step)
    {
        if (start.Kind != end.Kind)
        {
            throw new System.ArgumentException("Calendar tick boundaries must use the same date-time kind.");
        }

        if (start.Ticks >= end.Ticks)
        {
            throw new System.ArgumentException("Calendar ticks require an increasing range.");
        }

        int intervalValue = (int)interval;
        if (intervalValue < 0 || intervalValue > 8)
        {
            throw new System.ArgumentOutOfRangeException("interval");
        }

        if (step <= 0)
        {
            throw new System.ArgumentOutOfRangeException("step");
        }

        System.DateTime current = AlignCalendarStart(start, interval);
        if (current.Ticks < start.Ticks)
        {
            current = AdvanceCalendarValue(current, interval, step);
        }

        System.Collections.Generic.List<ChartTick> ticks = new System.Collections.Generic.List<ChartTick>();
        while (current.Ticks <= end.Ticks)
        {
            ticks.Add(ChartTick.CreateTime(current, ticks.Count));
            if (current.Ticks == end.Ticks)
            {
                break;
            }

            current = AdvanceCalendarValue(current, interval, step);
        }

        return ticks.ToArray();
    }

    internal static System.DateTime AlignCalendarStart(System.DateTime value, ChartTimeInterval interval)
    {
        System.DateTimeKind kind = value.Kind;
        if (interval == ChartTimeInterval.Millisecond)
        {
            long ticks = value.Ticks - value.Ticks % System.TimeSpan.TicksPerMillisecond;
            return new System.DateTime(ticks, kind);
        }

        if (interval == ChartTimeInterval.Second)
        {
            return new System.DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, kind);
        }

        if (interval == ChartTimeInterval.Minute)
        {
            return new System.DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, kind);
        }

        if (interval == ChartTimeInterval.Hour)
        {
            return new System.DateTime(value.Year, value.Month, value.Day, value.Hour, 0, 0, kind);
        }

        System.DateTime date = new System.DateTime(value.Year, value.Month, value.Day, 0, 0, 0, kind);
        if (interval == ChartTimeInterval.Day)
        {
            return date;
        }

        if (interval == ChartTimeInterval.Week)
        {
            int day = (int)date.DayOfWeek;
            int distance = day == 0 ? 6 : day - 1;
            return date.AddDays(-distance);
        }

        if (interval == ChartTimeInterval.Month)
        {
            return new System.DateTime(value.Year, value.Month, 1, 0, 0, 0, kind);
        }

        if (interval == ChartTimeInterval.Quarter)
        {
            int month = ((value.Month - 1) / 3) * 3 + 1;
            return new System.DateTime(value.Year, month, 1, 0, 0, 0, kind);
        }

        return new System.DateTime(value.Year, 1, 1, 0, 0, 0, kind);
    }

    internal static System.DateTime AdvanceCalendarValue(System.DateTime value, ChartTimeInterval interval, int step)
    {
        if (interval == ChartTimeInterval.Millisecond)
        {
            return value.AddMilliseconds(step);
        }

        if (interval == ChartTimeInterval.Second)
        {
            return value.AddSeconds(step);
        }

        if (interval == ChartTimeInterval.Minute)
        {
            return value.AddMinutes(step);
        }

        if (interval == ChartTimeInterval.Hour)
        {
            return value.AddHours(step);
        }

        if (interval == ChartTimeInterval.Day)
        {
            return value.AddDays(step);
        }

        if (interval == ChartTimeInterval.Week)
        {
            return value.AddDays(step * 7.0);
        }

        if (interval == ChartTimeInterval.Month)
        {
            return value.AddMonths(step);
        }

        if (interval == ChartTimeInterval.Quarter)
        {
            return value.AddMonths(step * 3);
        }

        return value.AddYears(step);
    }

    /// <summary>
    /// Generates category ticks from the supplied data and configuration.
    /// </summary>
    /// <param name="categories">The categories.</param>
    /// <returns>The resulting tick array.</returns>
    public static ChartTick[] GenerateCategoryTicks(string[] categories)
    {
        ChartCategoryBandScale.ValidateCategories(categories);
        ChartTick[] ticks = new ChartTick[categories.Length];
        int index = 0;
        while (index < categories.Length)
        {
            ticks[index] = new ChartTick(ChartTickKind.Category, index, categories[index], index);
            index = index + 1;
        }

        return ticks;
    }

    /// <summary>
    /// Generates radial ticks from the supplied data and configuration.
    /// </summary>
    /// <param name="maximum">The inclusive upper end of the numeric domain.</param>
    /// <param name="tickCount">The requested number of tick entries.</param>
    /// <returns>The resulting tick array.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public static ChartTick[] GenerateRadialTicks(double maximum, int tickCount)
    {
        ChartLinearScale.ValidateFinite(maximum, "maximum");
        if (maximum <= 0.0)
        {
            throw new System.ArgumentOutOfRangeException("maximum");
        }

        if (tickCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException("tickCount");
        }

        ChartTick[] ticks = new ChartTick[tickCount];
        if (tickCount == 1)
        {
            ticks[0] = new ChartTick(ChartTickKind.Radial, maximum, "", 0);
            return ticks;
        }

        double step = maximum / (tickCount - 1);
        int index = 0;
        while (index < tickCount)
        {
            ticks[index] = new ChartTick(ChartTickKind.Radial, step * index, "", index);
            index = index + 1;
        }

        return ticks;
    }

    /// <summary>
    /// Generates angular ticks from the supplied data and configuration.
    /// </summary>
    /// <param name="tickCount">The requested number of tick entries.</param>
    /// <returns>The resulting tick array.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public static ChartTick[] GenerateAngularTicks(int tickCount)
    {
        if (tickCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException("tickCount");
        }

        ChartTick[] ticks = new ChartTick[tickCount];
        double step = 360.0 / tickCount;
        int index = 0;
        while (index < tickCount)
        {
            ticks[index] = new ChartTick(ChartTickKind.Angular, step * index, "", index);
            index = index + 1;
        }

        return ticks;
    }

    internal static ChartTick[] CopyTicks(ChartTick[] ticks)
    {
        if (ticks == null)
        {
            throw new System.ArgumentNullException("ticks");
        }

        ChartTick[] copy = new ChartTick[ticks.Length];
        int index = 0;
        while (index < ticks.Length)
        {
            if (ticks[index] == null)
            {
                throw new System.ArgumentException("Tick cannot be null.");
            }

            copy[index] = ticks[index].Copy();
            index = index + 1;
        }

        return copy;
    }
}
