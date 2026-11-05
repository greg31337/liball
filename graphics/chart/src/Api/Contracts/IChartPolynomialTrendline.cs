namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartPolynomialTrendline"/>.</summary>
public interface IChartPolynomialTrendline
{
    /// <summary>Predicts a value using the fitted polynomial.</summary>
    /// <param name = "x">The finite input value.</param>
    /// <returns>The predicted value.</returns>
    double Predict(double x);
    /// <summary>Creates an independent copy of the fitted trendline.</summary>
    /// <returns>The copied trendline.</returns>
    ChartPolynomialTrendline Copy();
    /// <summary>Returns the number of polynomial coefficients.</summary>
    /// <returns>The coefficient count.</returns>
    int GetCoefficientCount();
    /// <summary>Returns the coefficient for the requested power.</summary>
    /// <param name = "power">The zero-based polynomial power.</param>
    /// <returns>The requested coefficient.</returns>
    double CoefficientAt(int power);
    /// <summary>Returns a copy of all coefficients in ascending power order.</summary>
    /// <returns>The polynomial coefficients.</returns>
    double[] GetCoefficients();
    /// <summary>Returns the coefficient of determination.</summary>
    /// <returns>The fitted R-squared value.</returns>
    double GetRSquared();
}
