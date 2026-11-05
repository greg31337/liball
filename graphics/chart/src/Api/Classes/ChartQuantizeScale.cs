namespace chart;
/// <summary>Represents a scale that partitions a continuous domain into equal-width buckets.</summary>
public class ChartQuantizeScale : IChartQuantizeScale
{
    internal double DomainMinimum;
    internal double DomainMaximum;
    internal double[] RangeValues;

    /// <summary>Initializes an equal-width quantize scale.</summary>
    /// <param name = "domainMinimum">The domain minimum.</param>
    /// <param name = "domainMaximum">The domain maximum.</param>
    /// <param name = "rangeValues">The values returned for successive buckets.</param>
    public ChartQuantizeScale(double domainMinimum, double domainMaximum, double[] rangeValues)
    {
        ChartLinearScale.ValidateDomain(domainMinimum, domainMaximum);
        if (domainMinimum > domainMaximum)
        {
            throw new System.ArgumentException("Quantize scale domain must be increasing.");
        }

        ChartStatisticsOperations.ValidateFiniteValues(rangeValues);
        DomainMinimum = domainMinimum;
        DomainMaximum = domainMaximum;
        RangeValues = ChartDataMatrix.CopyDoubleArray(rangeValues);
    }

    /// <summary>Maps a value to its equal-width bucket value.</summary>
    /// <param name = "value">The value to classify.</param>
    /// <returns>The configured bucket value.</returns>
    public double Map(double value)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        double amount = (value - DomainMinimum) / (DomainMaximum - DomainMinimum);
        int index = (int)System.Math.Floor(amount * RangeValues.Length);
        if (index < 0)
        {
            index = 0;
        }

        if (index >= RangeValues.Length)
        {
            index = RangeValues.Length - 1;
        }

        return RangeValues[index];
    }

    /// <summary>Returns the domain extent for a bucket.</summary>
    /// <param name = "index">The zero-based bucket index.</param>
    /// <returns>The bucket domain extent.</returns>
    public ChartRange InvertExtent(int index)
    {
        if (index < 0 || index >= RangeValues.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        double step = (DomainMaximum - DomainMinimum) / RangeValues.Length;
        return new ChartRange(DomainMinimum + index * step, DomainMinimum + (index + 1) * step);
    }
}
