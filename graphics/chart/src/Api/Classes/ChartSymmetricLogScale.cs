namespace chart;
/// <summary>Represents a continuous scale that is logarithmic away from zero and linear near zero.</summary>
public class ChartSymmetricLogScale : IChartSymmetricLogScale
{
    internal double DomainMinimum;
    internal double DomainMaximum;
    internal double RangeMinimum;
    internal double RangeMaximum;
    internal double Constant;

    /// <summary>Initializes a symmetric-log scale.</summary>
    /// <param name = "domainMinimum">The domain minimum.</param>
    /// <param name = "domainMaximum">The domain maximum.</param>
    /// <param name = "rangeMinimum">The range minimum.</param>
    /// <param name = "rangeMaximum">The range maximum.</param>
    /// <param name = "constant">The positive size of the linear region near zero.</param>
    public ChartSymmetricLogScale(double domainMinimum, double domainMaximum, double rangeMinimum, double rangeMaximum, double constant)
    {
        ChartLinearScale.ValidateDomain(domainMinimum, domainMaximum);
        ChartLinearScale.ValidateDomain(rangeMinimum, rangeMaximum);
        ChartLinearScale.ValidateFinite(constant, "constant");
        if (constant <= 0.0)
        {
            throw new System.ArgumentOutOfRangeException("constant");
        }

        DomainMinimum = domainMinimum;
        DomainMaximum = domainMaximum;
        RangeMinimum = rangeMinimum;
        RangeMaximum = rangeMaximum;
        Constant = constant;
    }

    /// <summary>Maps a domain value to the configured range.</summary>
    /// <param name = "value">The domain value.</param>
    /// <returns>The mapped range value.</returns>
    public double Map(double value)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        double transformedMinimum = Transform(DomainMinimum, Constant);
        double transformedMaximum = Transform(DomainMaximum, Constant);
        double amount = (Transform(value, Constant) - transformedMinimum) / (transformedMaximum - transformedMinimum);
        return RangeMinimum + amount * (RangeMaximum - RangeMinimum);
    }

    /// <summary>Maps a range value back to the configured domain.</summary>
    /// <param name = "value">The range value.</param>
    /// <returns>The mapped domain value.</returns>
    public double Invert(double value)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        double amount = (value - RangeMinimum) / (RangeMaximum - RangeMinimum);
        double transformedMinimum = Transform(DomainMinimum, Constant);
        double transformedMaximum = Transform(DomainMaximum, Constant);
        return Untransform(transformedMinimum + amount * (transformedMaximum - transformedMinimum), Constant);
    }

    internal static double Transform(double value, double constant)
    {
        if (value < 0.0)
        {
            return -System.Math.Log(1.0 + -value / constant);
        }

        return System.Math.Log(1.0 + value / constant);
    }

    internal static double Untransform(double value, double constant)
    {
        if (value < 0.0)
        {
            return -constant * (System.Math.Exp(-value) - 1.0);
        }

        return constant * (System.Math.Exp(value) - 1.0);
    }
}
