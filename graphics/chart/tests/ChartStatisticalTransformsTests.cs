namespace chart.Tests;

[TestFixture]
public class ChartStatisticalTransformsTests
{
    [Test]
    public void RegressionAndMovingAverages_ReturnExpectedValues()
    {
        ChartLinearTrendline regression = ChartStatisticalTransformsOperations.LinearRegression(new double[] { 0.0, 1.0, 2.0 }, new double[] { 1.0, 3.0, 5.0 });
        double[] moving = ChartStatisticalTransformsOperations.MovingAverage(new double[] { 2.0, 4.0, 6.0, 8.0 }, 2);
        double[] exponential = ChartStatisticalTransformsOperations.ExponentialMovingAverage(new double[] { 2.0, 4.0, 6.0, 8.0 }, 0.5);

        Assert.That(regression.Slope, Is.EqualTo(2.0).Within(0.0000001));
        Assert.That(regression.Intercept, Is.EqualTo(1.0).Within(0.0000001));
        Assert.That(regression.RSquared, Is.EqualTo(1.0).Within(0.0000001));
        Assert.That(moving, Is.EqualTo(new double[] { 3.0, 5.0, 7.0 }).Within(0.0000001));
        Assert.That(exponential, Is.EqualTo(new double[] { 2.0, 3.0, 4.5, 6.25 }).Within(0.0000001));
    }

    [Test]
    public void PolynomialRegression_RecoversCoefficientsAndPredictions()
    {
        ChartStatisticalTransforms transforms = new ChartStatisticalTransforms();

        ChartPolynomialTrendline regression = transforms.PolynomialRegression(new double[] { -2.0, -1.0, 0.0, 1.0, 2.0 }, new double[] { 15.0, 6.0, 1.0, 0.0, 3.0 }, 2);
        double[] coefficients = regression.GetCoefficients();
        coefficients[0] = 99.0;

        Assert.That(regression.GetCoefficientCount(), Is.EqualTo(3));
        Assert.That(regression.CoefficientAt(0), Is.EqualTo(1.0).Within(0.0000001));
        Assert.That(regression.CoefficientAt(1), Is.EqualTo(-3.0).Within(0.0000001));
        Assert.That(regression.CoefficientAt(2), Is.EqualTo(2.0).Within(0.0000001));
        Assert.That(regression.GetRSquared(), Is.EqualTo(1.0).Within(0.0000001));
        Assert.That(regression.Predict(3.0), Is.EqualTo(10.0).Within(0.0000001));
        Assert.That(regression.Copy().Predict(-3.0), Is.EqualTo(28.0).Within(0.0000001));
    }

    [Test]
    public void RollingAndCumulativeTransforms_ReturnExpectedWindows()
    {
        double[] values = new double[] { 3.0, 1.0, 4.0, 2.0 };

        Assert.That(ChartStatisticalTransformsOperations.RollingMinimum(values, 2), Is.EqualTo(new double[] { 1.0, 1.0, 2.0 }).Within(0.0000001));
        Assert.That(ChartStatisticalTransformsOperations.RollingMaximum(values, 2), Is.EqualTo(new double[] { 3.0, 4.0, 4.0 }).Within(0.0000001));
        Assert.That(ChartStatisticalTransformsOperations.CumulativeSum(values), Is.EqualTo(new double[] { 3.0, 4.0, 8.0, 10.0 }).Within(0.0000001));
    }

    [Test]
    public void ConfigurableRollingAndCumulativeTransforms_ApplyRequestedAggregations()
    {
        ChartStatisticalTransforms transforms = new ChartStatisticalTransforms();
        double[] values = new double[] { 1.0, 2.0, 3.0, 4.0 };

        Assert.That(transforms.CalculateRolling(values, 2, ChartAggregationKind.Count, 50.0), Is.EqualTo(new double[] { 2.0, 2.0, 2.0 }).Within(0.0000001));
        Assert.That(transforms.CalculateRolling(values, 2, ChartAggregationKind.Sum, 50.0), Is.EqualTo(new double[] { 3.0, 5.0, 7.0 }).Within(0.0000001));
        Assert.That(transforms.CalculateRolling(values, 2, ChartAggregationKind.Mean, 50.0), Is.EqualTo(new double[] { 1.5, 2.5, 3.5 }).Within(0.0000001));
        Assert.That(transforms.CalculateRolling(values, 2, ChartAggregationKind.Median, 50.0), Is.EqualTo(new double[] { 1.5, 2.5, 3.5 }).Within(0.0000001));
        Assert.That(transforms.CalculateRolling(values, 2, ChartAggregationKind.Min, 50.0), Is.EqualTo(new double[] { 1.0, 2.0, 3.0 }).Within(0.0000001));
        Assert.That(transforms.CalculateRolling(values, 2, ChartAggregationKind.Max, 50.0), Is.EqualTo(new double[] { 2.0, 3.0, 4.0 }).Within(0.0000001));
        Assert.That(transforms.CalculateRolling(values, 2, ChartAggregationKind.First, 50.0), Is.EqualTo(new double[] { 1.0, 2.0, 3.0 }).Within(0.0000001));
        Assert.That(transforms.CalculateRolling(values, 2, ChartAggregationKind.Last, 50.0), Is.EqualTo(new double[] { 2.0, 3.0, 4.0 }).Within(0.0000001));
        Assert.That(transforms.CalculateRolling(values, 2, ChartAggregationKind.Variance, 50.0), Is.EqualTo(new double[] { 0.25, 0.25, 0.25 }).Within(0.0000001));
        Assert.That(transforms.CalculateRolling(values, 2, ChartAggregationKind.StandardDeviation, 50.0), Is.EqualTo(new double[] { 0.5, 0.5, 0.5 }).Within(0.0000001));
        Assert.That(transforms.CalculateRolling(values, 2, ChartAggregationKind.Percentile, 25.0), Is.EqualTo(new double[] { 1.25, 2.25, 3.25 }).Within(0.0000001));
        Assert.That(transforms.CalculateCumulative(values, ChartAggregationKind.Sum, 50.0), Is.EqualTo(new double[] { 1.0, 3.0, 6.0, 10.0 }).Within(0.0000001));
        Assert.That(transforms.CalculateCumulative(values, ChartAggregationKind.Mean, 50.0), Is.EqualTo(new double[] { 1.0, 1.5, 2.0, 2.5 }).Within(0.0000001));
        Assert.That(transforms.CalculateCumulative(values, ChartAggregationKind.Percentile, 50.0), Is.EqualTo(new double[] { 1.0, 1.5, 2.0, 2.5 }).Within(0.0000001));
    }

    [Test]
    public void Pareto_SortsDescendingAndComputesCumulativeFractions()
    {
        ChartParetoResult pareto = ChartStatisticalTransformsOperations.Pareto(new string[] { "A", "B", "C" }, new double[] { 2.0, 5.0, 3.0 });
        ChartParetoResult copy = pareto.Copy();

        Assert.That(pareto.Labels, Is.EqualTo(new string[] { "B", "C", "A" }));
        Assert.That(pareto.Values, Is.EqualTo(new double[] { 5.0, 3.0, 2.0 }).Within(0.0000001));
        Assert.That(pareto.CumulativeFractions, Is.EqualTo(new double[] { 0.5, 0.8, 1.0 }).Within(0.0000001));
        copy.Labels[0] = "changed";
        Assert.That(pareto.Labels[0], Is.EqualTo("B"));
    }

    [Test]
    public void CorrelationMatrix_ComputesColumnWiseCorrelations()
    {
        ChartDataMatrix values = ChartDataMatrix.FromValues(
            3,
            3,
            new double[]
            {
                1.0, 2.0, 3.0,
                2.0, 4.0, 2.0,
                3.0, 6.0, 1.0
            });

        ChartDataMatrix matrix = ChartStatisticalTransformsOperations.CorrelationMatrix(values);

        Assert.That(matrix.Rows, Is.EqualTo(3));
        Assert.That(matrix.Columns, Is.EqualTo(3));
        Assert.That(matrix.GetValue(0, 0), Is.EqualTo(1.0).Within(0.0000001));
        Assert.That(matrix.GetValue(0, 1), Is.EqualTo(1.0).Within(0.0000001));
        Assert.That(matrix.GetValue(1, 0), Is.EqualTo(1.0).Within(0.0000001));
        Assert.That(matrix.GetValue(0, 2), Is.EqualTo(-1.0).Within(0.0000001));
        Assert.That(matrix.GetValue(2, 1), Is.EqualTo(-1.0).Within(0.0000001));
    }

    [Test]
    public void StatisticalTransforms_ValidateInputs()
    {
        Assert.Throws<System.InvalidOperationException>(() => ChartStatisticalTransformsOperations.LinearRegression(new double[] { 1.0, 1.0 }, new double[] { 2.0, 3.0 }));
        Assert.Throws<System.ArgumentNullException>(() => ChartStatisticalTransformsOperations.MovingAverage(null!, 2));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartStatisticalTransformsOperations.ExponentialMovingAverage(new double[] { 1.0, 2.0 }, 0.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartStatisticalTransformsOperations.ExponentialMovingAverage(new double[] { 1.0, 2.0 }, 1.5));
        Assert.Throws<System.ArgumentException>(() => ChartStatisticalTransformsOperations.RollingMinimum(new double[] { 1.0, 2.0 }, 3));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartStatisticalTransformsOperations.RollingMaximum(new double[] { 1.0, 2.0 }, 0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartStatisticalTransforms().CalculateRolling(new double[] { 1.0, 2.0 }, 1, (ChartAggregationKind)99, 50.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartStatisticalTransforms().CalculateCumulative(new double[] { 1.0, 2.0 }, ChartAggregationKind.Percentile, 101.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartStatisticalTransforms().PolynomialRegression(new double[] { 0.0, 1.0 }, new double[] { 0.0, 1.0 }, 0));
        Assert.Throws<System.ArgumentException>(() => new ChartStatisticalTransforms().PolynomialRegression(new double[] { 0.0, 1.0 }, new double[] { 0.0, 1.0 }, 2));
        Assert.Throws<System.ArgumentException>(() => ChartStatisticalTransformsOperations.Pareto(new string[] { "A" }, new double[] { 1.0, 2.0 }));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartStatisticalTransformsOperations.Pareto(new string[] { "A" }, new double[] { -1.0 }));
        Assert.Throws<System.ArgumentException>(() => ChartStatisticalTransformsOperations.Pareto(new string[] { "A" }, new double[] { 0.0 }));
        Assert.Throws<System.ArgumentNullException>(() => ChartStatisticalTransformsOperations.CorrelationMatrix(null!));
        Assert.Throws<System.ArgumentException>(() => ChartStatisticalTransformsOperations.CorrelationMatrix(ChartDataMatrix.FromValues(1, 2, new double[] { 1.0, 2.0 })));
        Assert.Throws<System.InvalidOperationException>(() => ChartStatisticalTransformsOperations.CorrelationMatrix(ChartDataMatrix.FromValues(2, 2, new double[] { 1.0, 2.0, 1.0, 3.0 })));
        Assert.Throws<System.ArgumentException>(() => new ChartParetoResult(new string[] { "A" }, new double[] { 1.0 }, new double[] { 1.0, 2.0 }));
    }
}
