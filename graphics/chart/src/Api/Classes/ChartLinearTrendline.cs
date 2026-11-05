namespace chart;
/// <summary>
/// Represents a linear trendline used by the chart model or rendering pipeline.
/// </summary>
public class ChartLinearTrendline : IChartLinearTrendline
{
    internal double Slope;
    internal double Intercept;
    internal double RSquared;
    /// <summary>
    /// Initializes a linear trendline from the supplied slope, intercept, and r squared.
    /// </summary>
    /// <param name = "slope">The slope.</param>
    /// <param name = "intercept">The intercept.</param>
    /// <param name = "rSquared">The r squared.</param>
    public ChartLinearTrendline(double slope, double intercept, double rSquared)
    {
        ChartLinearScale.ValidateFinite(slope, "slope");
        ChartLinearScale.ValidateFinite(intercept, "intercept");
        ChartLinearScale.ValidateFinite(rSquared, "rSquared");
        Slope = slope;
        Intercept = intercept;
        RSquared = rSquared;
    }

    /// <summary>
    /// Applies the predict operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "x">The x.</param>
    /// <returns>The resulting double.</returns>
    public double Predict(double x)
    {
        ChartLinearScale.ValidateFinite(x, "x");
        return Slope * x + Intercept;
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartLinearTrendline Copy()
    {
        return new ChartLinearTrendline(Slope, Intercept, RSquared);
    }

    /// <summary>Returns the current slope.</summary>
    /// <returns>The current field value.</returns>
    public double GetSlope()
    {
        return Slope;
    }

    /// <summary>Replaces the current slope.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetSlope(double value)
    {
        Slope = value;
    }

    /// <summary>Returns the current intercept.</summary>
    /// <returns>The current field value.</returns>
    public double GetIntercept()
    {
        return Intercept;
    }

    /// <summary>Replaces the current intercept.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetIntercept(double value)
    {
        Intercept = value;
    }

    /// <summary>Returns the current r squared.</summary>
    /// <returns>The current field value.</returns>
    public double GetRSquared()
    {
        return RSquared;
    }

    /// <summary>Replaces the current r squared.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetRSquared(double value)
    {
        RSquared = value;
    }
}
