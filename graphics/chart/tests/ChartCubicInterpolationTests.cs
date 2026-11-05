namespace chart.Tests;

[TestFixture]
public class ChartCubicInterpolationTests
{
    [Test]
    public void CatmullRomChoice_PreservesLegacySplineGeometry()
    {
        ChartLineAreaCharts charts = new ChartLineAreaCharts();
        double[] x = new double[] { 0.0, 1.0, 2.0, 3.0 };
        double[] y = new double[] { 0.0, 2.0, 1.0, 3.0 };
        ChartRenderStyle style = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);

        ChartPoint[] legacy = charts.CreateInterpolatedLineSeries("legacy", x, y, ChartLineInterpolation.Spline, 4, style).CommandAt(0).Points;
        ChartPoint[] named = charts.CreateInterpolatedLineSeries("named", x, y, ChartLineInterpolation.CatmullRom, 4, style).CommandAt(0).Points;

        Assert.That(named.Length, Is.EqualTo(13));
        AssertPointsEqual(named, legacy);
        AssertPoint(named[0], 0.0, 0.0);
        AssertPoint(named[4], 1.0, 2.0);
        AssertPoint(named[8], 2.0, 1.0);
        AssertPoint(named[12], 3.0, 3.0);
    }

    [Test]
    public void MonotoneCubic_PassesThroughValuesWithoutOvershootingSegments()
    {
        ChartLineAreaCharts charts = new ChartLineAreaCharts();
        double[] x = new double[] { 0.0, 1.0, 2.0, 3.0 };
        double[] y = new double[] { 0.0, 1.0, 1.0, 2.0 };
        ChartRenderStyle style = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);

        ChartPoint[] points = charts.CreateInterpolatedLineSeries("monotone", x, y, ChartLineInterpolation.MonotoneCubic, 8, style).CommandAt(0).Points;

        Assert.That(points.Length, Is.EqualTo(25));
        AssertPoint(points[0], 0.0, 0.0);
        AssertPoint(points[8], 1.0, 1.0);
        AssertPoint(points[16], 2.0, 1.0);
        AssertPoint(points[24], 3.0, 2.0);

        int index = 0;
        while (index <= 8)
        {
            Assert.That(points[index].Y, Is.InRange(0.0, 1.0));
            Assert.That(points[8 + index].Y, Is.EqualTo(1.0).Within(0.0000001));
            Assert.That(points[16 + index].Y, Is.InRange(1.0, 2.0));
            index++;
        }
    }

    [Test]
    public void NaturalCubic_PassesThroughKnotsAndReproducesStraightLines()
    {
        ChartLineAreaCharts charts = new ChartLineAreaCharts();
        double[] x = new double[] { 0.0, 1.0, 2.0, 3.0 };
        double[] y = new double[] { 1.0, 3.0, 5.0, 7.0 };
        ChartRenderStyle style = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);

        ChartPoint[] points = charts.CreateInterpolatedLineSeries("natural", x, y, ChartLineInterpolation.NaturalCubic, 4, style).CommandAt(0).Points;

        Assert.That(points.Length, Is.EqualTo(13));
        int index = 0;
        while (index < points.Length)
        {
            Assert.That(points[index].Y, Is.EqualTo(1.0 + 2.0 * points[index].X).Within(0.0000001));
            index++;
        }
    }

    [Test]
    public void LineAreaAndPath_UseIdenticalNaturalCubicSamples()
    {
        ChartLineAreaCharts charts = new ChartLineAreaCharts();
        double[] x = new double[] { 0.0, 1.0, 2.0, 3.0 };
        double[] y = new double[] { 0.0, 2.0, 1.0, 3.0 };
        ChartRenderStyle lineStyle = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartRenderStyle fillStyle = ChartRenderStyle.CreateFill(ChartColor.FromRgb(20, 40, 60));

        ChartPoint[] line = charts.CreateInterpolatedLineSeries("line", x, y, ChartLineInterpolation.NaturalCubic, 3, lineStyle).CommandAt(0).Points;
        ChartPoint[] area = charts.CreateInterpolatedAreaSeries("area", x, y, 0.0, ChartLineInterpolation.NaturalCubic, 3, fillStyle).CommandAt(0).Points;
        ChartPathPolyline[] path = charts.CreateInterpolatedPath(x, y, ChartLineInterpolation.NaturalCubic, 3).Flatten(0.1);

        Assert.That(line.Length, Is.EqualTo(10));
        Assert.That(area.Length, Is.EqualTo(20));
        Assert.That(path.Length, Is.EqualTo(1));
        Assert.That(path[0].Points.Length, Is.EqualTo(10));

        int index = 0;
        while (index < line.Length)
        {
            AssertPoint(area[index], line[index].X, line[index].Y);
            AssertPoint(path[0].Points[index], line[index].X, line[index].Y);
            index++;
        }

        AssertPoint(area[10], 3.0, 0.0);
        AssertPoint(area[19], 0.0, 0.0);
    }

    [Test]
    public void InterpolatedRangeArea_SamplesBothBoundariesWithTheSameChoice()
    {
        ChartLineAreaCharts charts = new ChartLineAreaCharts();
        double[] x = new double[] { 0.0, 1.0, 2.0 };
        double[] upper = new double[] { 2.0, 4.0, 3.0 };
        double[] lower = new double[] { 0.0, 1.0, 0.5 };

        ChartPoint[] polygon = charts.CreateInterpolatedRangeAreaSeries(
            "range",
            x,
            upper,
            lower,
            ChartLineInterpolation.MonotoneCubic,
            2,
            ChartRenderStyle.CreateFill(ChartColor.Black())).CommandAt(0).Points;

        Assert.That(polygon.Length, Is.EqualTo(10));
        AssertPoint(polygon[0], 0.0, 2.0);
        AssertPoint(polygon[4], 2.0, 3.0);
        AssertPoint(polygon[5], 2.0, 0.5);
        AssertPoint(polygon[9], 0.0, 0.0);
    }

    [Test]
    public void CubicChoices_RequirePositiveSamplingAndIncreasingHorizontalValues()
    {
        ChartLineAreaCharts charts = new ChartLineAreaCharts();
        ChartRenderStyle style = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        double[] nonIncreasing = new double[] { 0.0, 1.0, 1.0 };
        double[] y = new double[] { 0.0, 1.0, 2.0 };

        Assert.Throws<System.ArgumentException>(() => charts.CreateInterpolatedLineSeries("monotone", nonIncreasing, y, ChartLineInterpolation.MonotoneCubic, 4, style));
        Assert.Throws<System.ArgumentException>(() => charts.CreateInterpolatedLineSeries("natural", nonIncreasing, y, ChartLineInterpolation.NaturalCubic, 4, style));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => charts.CreateInterpolatedPath(new double[] { 0.0, 1.0 }, new double[] { 0.0, 1.0 }, ChartLineInterpolation.CatmullRom, 0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => charts.CreateInterpolatedAreaSeries("area", new double[] { 0.0, 1.0 }, new double[] { 0.0, 1.0 }, 0.0, (ChartLineInterpolation)99, 2, ChartRenderStyle.CreateFill(ChartColor.Black())));
    }

    static void AssertPointsEqual(ChartPoint[] actual, ChartPoint[] expected)
    {
        Assert.That(actual.Length, Is.EqualTo(expected.Length));
        int index = 0;
        while (index < actual.Length)
        {
            AssertPoint(actual[index], expected[index].X, expected[index].Y);
            index++;
        }
    }

    static void AssertPoint(ChartPoint point, double x, double y)
    {
        Assert.That(point.X, Is.EqualTo(x).Within(0.0000001));
        Assert.That(point.Y, Is.EqualTo(y).Within(0.0000001));
    }
}
