namespace chart;
/// <summary>
/// Represents a log scale used by the chart model or rendering pipeline.
/// </summary>
public class ChartLogScale : IChartLogScale
{
    internal double DomainMinimum;
    internal double DomainMaximum;
    internal double RangeMinimum;
    internal double RangeMaximum;
    internal double BaseValue;
    /// <summary>
    /// Initializes a log scale from the supplied domain minimum, domain maximum, range minimum, range maximum, and base value.
    /// </summary>
    /// <param name = "domainMinimum">The domain minimum.</param>
    /// <param name = "domainMaximum">The domain maximum.</param>
    /// <param name = "rangeMinimum">The range minimum.</param>
    /// <param name = "rangeMaximum">The range maximum.</param>
    /// <param name = "baseValue">The base value.</param>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public ChartLogScale(double domainMinimum, double domainMaximum, double rangeMinimum, double rangeMaximum, double baseValue)
    {
        ChartLinearScale.ValidateDomain(domainMinimum, domainMaximum);
        ChartLinearScale.ValidateFinite(rangeMinimum, "rangeMinimum");
        ChartLinearScale.ValidateFinite(rangeMaximum, "rangeMaximum");
        ChartLinearScale.ValidateFinite(baseValue, "baseValue");
        if (domainMinimum <= 0.0 || domainMaximum <= 0.0 || baseValue <= 0.0 || baseValue == 1.0)
        {
            throw new System.ArgumentException("Log scale domain and base must be positive.");
        }

        DomainMinimum = domainMinimum;
        DomainMaximum = domainMaximum;
        RangeMinimum = rangeMinimum;
        RangeMaximum = rangeMaximum;
        BaseValue = baseValue;
    }

    /// <summary>
    /// Maps the configured value through the configured input and output domains.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting double.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public double Map(double value)
    {
        if (value <= 0.0)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        double domainMinimum = System.Math.Log(DomainMinimum, BaseValue);
        double domainMaximum = System.Math.Log(DomainMaximum, BaseValue);
        double domainValue = System.Math.Log(value, BaseValue);
        double amount = (domainValue - domainMinimum) / (domainMaximum - domainMinimum);
        return RangeMinimum + amount * (RangeMaximum - RangeMinimum);
    }

    /// <summary>Returns the current domain minimum.</summary>
    /// <returns>The current field value.</returns>
    public double GetDomainMinimum()
    {
        return DomainMinimum;
    }

    /// <summary>Replaces the current domain minimum.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetDomainMinimum(double value)
    {
        DomainMinimum = value;
    }

    /// <summary>Returns the current domain maximum.</summary>
    /// <returns>The current field value.</returns>
    public double GetDomainMaximum()
    {
        return DomainMaximum;
    }

    /// <summary>Replaces the current domain maximum.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetDomainMaximum(double value)
    {
        DomainMaximum = value;
    }

    /// <summary>Returns the current range minimum.</summary>
    /// <returns>The current field value.</returns>
    public double GetRangeMinimum()
    {
        return RangeMinimum;
    }

    /// <summary>Replaces the current range minimum.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetRangeMinimum(double value)
    {
        RangeMinimum = value;
    }

    /// <summary>Returns the current range maximum.</summary>
    /// <returns>The current field value.</returns>
    public double GetRangeMaximum()
    {
        return RangeMaximum;
    }

    /// <summary>Replaces the current range maximum.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetRangeMaximum(double value)
    {
        RangeMaximum = value;
    }

    /// <summary>Returns the current base value.</summary>
    /// <returns>The current field value.</returns>
    public double GetBaseValue()
    {
        return BaseValue;
    }

    /// <summary>Replaces the current base value.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetBaseValue(double value)
    {
        BaseValue = value;
    }
}
