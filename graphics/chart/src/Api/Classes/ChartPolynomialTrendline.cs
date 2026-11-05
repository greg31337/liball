namespace chart;
/// <summary>Represents a polynomial least-squares trendline.</summary>
public class ChartPolynomialTrendline : IChartPolynomialTrendline
{
    internal double[] Coefficients;
    internal double RSquared;
    /// <summary>Initializes a polynomial trendline from ascending-power coefficients and R-squared.</summary>
    /// <param name = "coefficients">The coefficients from the constant term through the highest power.</param>
    /// <param name = "rSquared">The coefficient of determination.</param>
    public ChartPolynomialTrendline(double[] coefficients, double rSquared)
    {
        ChartStatisticsOperations.ValidateFiniteValues(coefficients);
        ChartLinearScale.ValidateFinite(rSquared, "rSquared");
        Coefficients = ChartDataMatrix.CopyDoubleArray(coefficients);
        RSquared = rSquared;
    }

    /// <summary>Predicts a value using the fitted polynomial.</summary>
    /// <param name = "x">The finite input value.</param>
    /// <returns>The predicted value.</returns>
    public double Predict(double x)
    {
        ChartLinearScale.ValidateFinite(x, "x");
        double value = 0.0;
        int index = Coefficients.Length - 1;
        while (index >= 0)
        {
            value = value * x + Coefficients[index];
            index = index - 1;
        }

        return value;
    }

    /// <summary>Creates an independent copy of the fitted trendline.</summary>
    /// <returns>The copied trendline.</returns>
    public ChartPolynomialTrendline Copy()
    {
        return new ChartPolynomialTrendline(Coefficients, RSquared);
    }

    /// <summary>Returns the number of polynomial coefficients.</summary>
    /// <returns>The coefficient count.</returns>
    public int GetCoefficientCount()
    {
        return Coefficients.Length;
    }

    /// <summary>Returns the coefficient for the requested power.</summary>
    /// <param name = "power">The zero-based polynomial power.</param>
    /// <returns>The requested coefficient.</returns>
    public double CoefficientAt(int power)
    {
        if (power < 0 || power >= Coefficients.Length)
        {
            throw new System.ArgumentOutOfRangeException("power");
        }

        return Coefficients[power];
    }

    /// <summary>Returns a copy of all coefficients in ascending power order.</summary>
    /// <returns>The polynomial coefficients.</returns>
    public double[] GetCoefficients()
    {
        return ChartDataMatrix.CopyDoubleArray(Coefficients);
    }

    /// <summary>Returns the coefficient of determination.</summary>
    /// <returns>The fitted R-squared value.</returns>
    public double GetRSquared()
    {
        return RSquared;
    }
}
