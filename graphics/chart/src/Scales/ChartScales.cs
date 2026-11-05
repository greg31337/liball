namespace chart;
internal static class ChartScaleUtilities
{
    internal static ChartRange IncludeZero(double minimum, double maximum)
    {
        ChartLinearScale.ValidateDomain(minimum, maximum);
        double rangeMinimum = minimum;
        double rangeMaximum = maximum;
        if (rangeMinimum > 0.0)
        {
            rangeMinimum = 0.0;
        }

        if (rangeMaximum < 0.0)
        {
            rangeMaximum = 0.0;
        }

        return new ChartRange(rangeMinimum, rangeMaximum);
    }

    internal static ChartRange PadRange(double minimum, double maximum, double fraction)
    {
        ChartLinearScale.ValidateDomain(minimum, maximum);
        if (fraction < 0.0 || double.IsNaN(fraction) || double.IsInfinity(fraction))
        {
            throw new System.ArgumentOutOfRangeException("fraction");
        }

        double amount = (maximum - minimum) * fraction;
        return new ChartRange(minimum - amount, maximum + amount);
    }

    internal static ChartRange NiceDomain(double minimum, double maximum, int tickCount)
    {
        ChartLinearScale.ValidateDomain(minimum, maximum);
        if (tickCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException("tickCount");
        }

        if (tickCount == 1)
        {
            return new ChartRange(minimum, maximum);
        }

        double range = NiceNumber(maximum - minimum, false);
        double step = NiceNumber(range / (tickCount - 1), true);
        double niceMinimum = System.Math.Floor(minimum / step) * step;
        double niceMaximum = System.Math.Ceiling(maximum / step) * step;
        return new ChartRange(niceMinimum, niceMaximum);
    }

    internal static double NiceNumber(double value, bool round)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        if (value <= 0.0)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        double exponent = System.Math.Floor(System.Math.Log10(value));
        double power = System.Math.Pow(10.0, exponent);
        double fraction = value / power;
        double niceFraction = 10.0;
        if (round)
        {
            if (fraction < 1.5)
            {
                niceFraction = 1.0;
            }
            else if (fraction < 3.0)
            {
                niceFraction = 2.0;
            }
            else if (fraction < 7.0)
            {
                niceFraction = 5.0;
            }
        }
        else
        {
            if (fraction <= 1.0)
            {
                niceFraction = 1.0;
            }
            else if (fraction <= 2.0)
            {
                niceFraction = 2.0;
            }
            else if (fraction <= 5.0)
            {
                niceFraction = 5.0;
            }
        }

        return niceFraction * power;
    }

    internal static double ClampUnit(double value)
    {
        if (value < 0.0)
        {
            return 0.0;
        }

        if (value > 1.0)
        {
            return 1.0;
        }

        return value;
    }
}
