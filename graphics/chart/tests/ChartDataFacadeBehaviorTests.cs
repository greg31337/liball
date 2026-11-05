namespace chart.Tests;

[TestFixture]
public class ChartDataFacadeBehaviorTests
{
    [Test]
    public void DataOperationsFacade_GroupsSortsFiltersAndAggregatesThroughPublicEntryPoints()
    {
        ChartDataOperations operations = new ChartDataOperations();
        ChartDataColumn numbers = new ChartDataColumnFactory().CreateNumber("number", new double[] { 3.0, 1.0, 2.0, 1.0 });
        ChartDataColumn text = new ChartDataColumnFactory().CreateText("text", new string? [] { "beta", "alpha", "alphabet", null });
        ChartDataColumn categories = new ChartDataColumnFactory().CreateCategory("category", new string? [] { "B", "A", "B", null });

        ChartDataGroup[] groups = operations.GroupByCategory(categories);
        Assert.That(groups.Length, Is.EqualTo(3));
        Assert.That(groups[0].GetKey(), Is.EqualTo("B"));
        Assert.That(groups[0].CopyIndices(), Is.EqualTo(new int[] { 0, 2 }));
        Assert.That(operations.SortIndicesByNumber(numbers, true), Is.EqualTo(new int[] { 1, 3, 2, 0 }));
        Assert.That(operations.SortIndicesByNumber(numbers, false), Is.EqualTo(new int[] { 0, 2, 1, 3 }));
        Assert.That(operations.SortIndicesByText(text, true), Is.EqualTo(new int[] { 3, 1, 2, 0 }));
        Assert.That(operations.SortIndicesByCustomOrder(categories, new string? [] { "A", "B", null }), Is.EqualTo(new int[] { 1, 0, 2, 3 }));
        Assert.That(operations.FilterNumberRange(numbers, 1.0, 2.0), Is.EqualTo(new int[] { 1, 2, 3 }));
        Assert.That(operations.FilterCategoryIn(categories, new string? [] { "B" }), Is.EqualTo(new int[] { 0, 2 }));
        Assert.That(operations.FilterTextContains(text, "alpha"), Is.EqualTo(new int[] { 1, 2 }));
        Assert.That(operations.FilterMissingCategories(categories), Is.EqualTo(new int[] { 3 }));

        int[] selection = new int[] { 0, 1, 2, 3 };
        Assert.That(operations.AggregateNumber(numbers, selection, ChartAggregationKind.Count), Is.EqualTo(4.0));
        Assert.That(operations.AggregateNumber(numbers, selection, ChartAggregationKind.Sum), Is.EqualTo(7.0));
        Assert.That(operations.AggregateNumber(numbers, selection, ChartAggregationKind.Mean), Is.EqualTo(1.75));
        Assert.That(operations.AggregateNumber(numbers, selection, ChartAggregationKind.Median), Is.EqualTo(1.5));
        Assert.That(operations.AggregateNumber(numbers, selection, ChartAggregationKind.Min), Is.EqualTo(1.0));
        Assert.That(operations.AggregateNumber(numbers, selection, ChartAggregationKind.Max), Is.EqualTo(3.0));
        Assert.That(operations.AggregateNumber(numbers, selection, ChartAggregationKind.First), Is.EqualTo(3.0));
        Assert.That(operations.AggregateNumber(numbers, selection, ChartAggregationKind.Last), Is.EqualTo(1.0));
        Assert.That(operations.AggregatePercentile(numbers, selection, 50.0), Is.EqualTo(1.5));
    }

    [Test]
    public void BinnerFacade_CoversEveryStrategyHelperAndFailureBoundary()
    {
        ChartBinner binner = new ChartBinner();
        double[] values = new double[] { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0 };

        Assert.That(binner.BinFixedCount(values, 4).GetStrategy(), Is.EqualTo(ChartHistogramBinStrategy.FixedCount));
        Assert.That(binner.BinFixedWidth(values, 2.0).GetStrategy(), Is.EqualTo(ChartHistogramBinStrategy.FixedWidth));
        Assert.That(binner.BinSturges(values).GetStrategy(), Is.EqualTo(ChartHistogramBinStrategy.Sturges));
        Assert.That(binner.BinScott(values).GetStrategy(), Is.EqualTo(ChartHistogramBinStrategy.Scott));
        Assert.That(binner.BinFreedmanDiaconis(values).GetStrategy(), Is.EqualTo(ChartHistogramBinStrategy.FreedmanDiaconis));

        Assert.That(binner.Bin(values, ChartHistogramBinStrategy.FixedCount, 2, 1.0).GetBinCount(), Is.EqualTo(2));
        Assert.That(binner.Bin(values, ChartHistogramBinStrategy.FixedWidth, 2, 1.0).GetBinWidth(), Is.EqualTo(1.0));
        Assert.That(binner.Bin(values, ChartHistogramBinStrategy.Sturges, 2, 1.0).GetStrategy(), Is.EqualTo(ChartHistogramBinStrategy.Sturges));
        Assert.That(binner.Bin(values, ChartHistogramBinStrategy.Scott, 2, 1.0).GetStrategy(), Is.EqualTo(ChartHistogramBinStrategy.Scott));
        Assert.That(binner.Bin(values, ChartHistogramBinStrategy.FreedmanDiaconis, 2, 1.0).GetStrategy(), Is.EqualTo(ChartHistogramBinStrategy.FreedmanDiaconis));
        Assert.That(binner.CalculateSturgesBinCount(values.Length), Is.EqualTo(4));
        Assert.That(binner.CalculateScottBinWidth(values), Is.EqualTo(4.0097535).Within(0.000001));
        Assert.That(binner.CalculateFreedmanDiaconisBinWidth(values), Is.EqualTo(3.5).Within(0.000001));
        Assert.That(binner.Percentile(values, 0.25), Is.EqualTo(1.75));

        Assert.Throws<System.ArgumentException>(() => binner.Bin(values, (ChartHistogramBinStrategy)99, 2, 1.0));
        Assert.Throws<System.ArgumentNullException>(() => binner.BinFixedCount(null!, 2));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => binner.BinFixedCount(values, 0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => binner.BinFixedWidth(values, 0.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => binner.Percentile(values, -0.1));
    }

    [Test]
    public void StackerFacade_CoversEveryModeAndDispatcher()
    {
        ChartStacker stacker = new ChartStacker();
        ChartDataMatrix positive = new ChartDataMatrixFactory().FromValues(1, 3, new double[] { 1.0, 2.0, 3.0 });
        ChartDataMatrix mixed = new ChartDataMatrixFactory().FromValues(1, 3, new double[] { 1.0, -2.0, 3.0 });

        Assert.That(stacker.StackNone(positive).GetMode(), Is.EqualTo(ChartStackMode.None));
        Assert.That(stacker.StackNormal(positive).GetMode(), Is.EqualTo(ChartStackMode.Zero));
        Assert.That(stacker.StackPercent(positive).GetMode(), Is.EqualTo(ChartStackMode.Percent));
        Assert.That(stacker.StackDiverging(mixed).GetMode(), Is.EqualTo(ChartStackMode.Diverging));
        Assert.That(stacker.StackStreamgraph(positive).GetMode(), Is.EqualTo(ChartStackMode.Streamgraph));
        Assert.That(stacker.Stack(positive, ChartStackMode.None).GetEnd(0, 2), Is.EqualTo(3.0));
        Assert.That(stacker.Stack(positive, ChartStackMode.Zero).GetEnd(0, 2), Is.EqualTo(6.0));
        Assert.That(stacker.Stack(positive, ChartStackMode.Percent).GetEnd(0, 2), Is.EqualTo(1.0));
        Assert.That(stacker.Stack(mixed, ChartStackMode.Diverging).GetEnd(0, 1), Is.EqualTo(-2.0));
        Assert.That(stacker.Stack(positive, ChartStackMode.Streamgraph).GetStart(0, 0), Is.EqualTo(-3.0));

        Assert.Throws<System.ArgumentNullException>(() => stacker.StackNormal(null!));
        Assert.Throws<System.ArgumentException>(() => stacker.Stack(positive, (ChartStackMode)99));
        Assert.Throws<System.ArgumentException>(() => stacker.StackPercent(new ChartDataMatrixFactory().FromValues(1, 2, new double[] { 1.0, -1.0 })));
    }

    [Test]
    public void PointReducerFacade_CoversEveryAlgorithmAndIndependentResults()
    {
        ChartPointReducer reducer = new ChartPointReducer();
        ChartPoint[] points = new ChartPoint[]
        {
            new ChartPoint(0.0, 0.0),
            new ChartPoint(0.1, 0.05),
            new ChartPoint(1.0, 0.0),
            new ChartPoint(2.0, 2.0),
            new ChartPoint(3.0, 0.0)
        };

        ChartPoint[] radial = reducer.SimplifyRadialDistance(points, 0.2);
        ChartPoint[] douglasPeucker = reducer.SimplifyDouglasPeucker(points, 0.2);
        ChartPoint[] line = reducer.SimplifyLine(points, 0.2);
        ChartPoint[] scatter = reducer.DecimateScatter(points, 3);
        Assert.That(radial.Length, Is.LessThan(points.Length));
        Assert.That(douglasPeucker.Length, Is.LessThan(points.Length));
        Assert.That(line.Length, Is.LessThan(points.Length));
        Assert.That(scatter.Length, Is.EqualTo(3));
        Assert.That(scatter[0].GetX(), Is.EqualTo(0.0));
        Assert.That(scatter[2].GetX(), Is.EqualTo(3.0));
        scatter[0].SetX(99.0);
        Assert.That(points[0].GetX(), Is.Zero);

        Assert.Throws<System.ArgumentNullException>(() => reducer.SimplifyLine(null!, 1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => reducer.SimplifyLine(points, -1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => reducer.DecimateScatter(points, 1));
    }

    [Test]
    public void StatisticsFacade_CoversBasicPairedAndDerivedOperations()
    {
        ChartStatistics statistics = new ChartStatistics();
        double[] values = new double[] { 1.0, 2.0, 3.0, 4.0 };
        double[] doubled = new double[] { 2.0, 4.0, 6.0, 8.0 };

        Assert.That(statistics.Min(values), Is.EqualTo(1.0));
        Assert.That(statistics.Max(values), Is.EqualTo(4.0));
        Assert.That(statistics.Sum(values), Is.EqualTo(10.0));
        Assert.That(statistics.Mean(values), Is.EqualTo(2.5));
        Assert.That(statistics.Median(values), Is.EqualTo(2.5));
        Assert.That(statistics.Variance(values), Is.EqualTo(1.25));
        Assert.That(statistics.StandardDeviation(values), Is.EqualTo(System.Math.Sqrt(1.25)).Within(0.0000001));
        Assert.That(statistics.Covariance(values, doubled), Is.EqualTo(2.5));
        Assert.That(statistics.Correlation(values, doubled), Is.EqualTo(1.0));
        Assert.That(statistics.Quantile(values, 0.25), Is.EqualTo(1.75));
        Assert.That(statistics.Percentile(values, 75.0), Is.EqualTo(3.25));
        Assert.That(statistics.Quartiles(values), Is.EqualTo(new double[] { 1.75, 2.5, 3.25 }));
        Assert.That(statistics.InterquartileRange(values), Is.EqualTo(1.5));
        Assert.That(statistics.MedianAbsoluteDeviation(values), Is.EqualTo(1.0));
        Assert.That(statistics.PopulationVariance(values), Is.EqualTo(1.25));
        Assert.That(statistics.SampleVariance(values), Is.EqualTo(5.0 / 3.0).Within(0.0000001));
        Assert.That(statistics.PopulationStandardDeviation(values), Is.EqualTo(System.Math.Sqrt(1.25)).Within(0.0000001));
        Assert.That(statistics.SampleStandardDeviation(values), Is.EqualTo(System.Math.Sqrt(5.0 / 3.0)).Within(0.0000001));
        Assert.That(statistics.PopulationCovariance(values, doubled), Is.EqualTo(2.5));
        Assert.That(statistics.SampleCovariance(values, doubled), Is.EqualTo(10.0 / 3.0).Within(0.0000001));
        Assert.That(statistics.Interpolate(0.0, 10.0, 0.25), Is.EqualTo(2.5));
        Assert.That(statistics.InverseInterpolate(0.0, 10.0, 2.5), Is.EqualTo(0.25));
        Assert.That(statistics.NiceNumber(4.2, true), Is.EqualTo(5.0));
        Assert.That(statistics.MinWithPolicy(new double[] { 1.0, double.NaN }, ChartNaNPolicy.Skip, ChartInfinityPolicy.Reject), Is.EqualTo(1.0));
        Assert.That(statistics.MaxWithPolicy(new double[] { 1.0, double.PositiveInfinity }, ChartNaNPolicy.Reject, ChartInfinityPolicy.Skip), Is.EqualTo(1.0));
        Assert.That(statistics.SumWithPolicy(new double[] { 1.0, double.NaN }, ChartNaNPolicy.Skip, ChartInfinityPolicy.Reject), Is.EqualTo(1.0));
        Assert.That(statistics.MeanWithPolicy(new double[] { 1.0, double.NaN }, ChartNaNPolicy.Skip, ChartInfinityPolicy.Reject), Is.EqualTo(1.0));
    }

    [Test]
    public void StatisticalTransformsFacade_CoversRegressionRollingCumulativeParetoAndCorrelation()
    {
        ChartStatisticalTransforms transforms = new ChartStatisticalTransforms();
        double[] values = new double[] { 1.0, 2.0, 3.0, 4.0 };
        ChartLinearTrendline linear = transforms.LinearRegression(new double[] { 0.0, 1.0, 2.0 }, new double[] { 1.0, 3.0, 5.0 });
        ChartPolynomialTrendline polynomial = transforms.PolynomialRegression(new double[] { -1.0, 0.0, 1.0 }, new double[] { 1.0, 0.0, 1.0 }, 2);

        Assert.That(linear.Predict(3.0), Is.EqualTo(7.0));
        Assert.That(polynomial.Predict(2.0), Is.EqualTo(4.0).Within(0.0000001));
        Assert.That(transforms.MovingAverage(values, 2), Is.EqualTo(new double[] { 1.5, 2.5, 3.5 }));
        Assert.That(transforms.ExponentialMovingAverage(values, 0.5), Is.EqualTo(new double[] { 1.0, 1.5, 2.25, 3.125 }));
        Assert.That(transforms.RollingMinimum(values, 2), Is.EqualTo(new double[] { 1.0, 2.0, 3.0 }));
        Assert.That(transforms.RollingMaximum(values, 2), Is.EqualTo(new double[] { 2.0, 3.0, 4.0 }));
        Assert.That(transforms.CumulativeSum(values), Is.EqualTo(new double[] { 1.0, 3.0, 6.0, 10.0 }));
        Assert.That(transforms.CalculateRolling(values, 2, ChartAggregationKind.Sum, 50.0), Is.EqualTo(new double[] { 3.0, 5.0, 7.0 }));
        Assert.That(transforms.CalculateCumulative(values, ChartAggregationKind.Mean, 50.0), Is.EqualTo(new double[] { 1.0, 1.5, 2.0, 2.5 }));
        Assert.That(transforms.Total(values), Is.EqualTo(10.0));

        ChartParetoResult pareto = transforms.Pareto(new string[] { "a", "b" }, new double[] { 1.0, 3.0 });
        Assert.That(pareto.GetLabels(), Is.EqualTo(new string[] { "b", "a" }));
        ChartDataMatrix correlation = transforms.CorrelationMatrix(new ChartDataMatrixFactory().FromValues(3, 2, new double[] { 1.0, 2.0, 2.0, 4.0, 3.0, 6.0 }));
        Assert.That(correlation.GetValue(0, 1), Is.EqualTo(1.0));
    }
}
