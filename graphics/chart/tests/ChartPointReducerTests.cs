namespace chart.Tests;

[TestFixture]
public class ChartPointReducerTests
{
    [Test]
    public void SimplifyRadialDistance_RemovesNearbyInteriorPoints()
    {
        ChartPoint[] points = new ChartPoint[]
        {
            new ChartPoint(0.0, 0.0),
            new ChartPoint(0.1, 0.1),
            new ChartPoint(1.0, 0.0),
            new ChartPoint(1.1, 0.05),
            new ChartPoint(2.0, 0.0)
        };

        ChartPoint[] simplified = ChartPointReducerOperations.SimplifyRadialDistance(points, 0.25);

        AssertPoints(simplified, new double[] { 0.0, 0.0, 1.0, 0.0, 2.0, 0.0 });
    }

    [Test]
    public void SimplifyDouglasPeucker_RemovesCollinearNoiseAndKeepsSpikes()
    {
        ChartPoint[] nearLine = new ChartPoint[]
        {
            new ChartPoint(0.0, 0.0),
            new ChartPoint(1.0, 0.05),
            new ChartPoint(2.0, -0.04),
            new ChartPoint(3.0, 0.0)
        };
        ChartPoint[] spike = new ChartPoint[]
        {
            new ChartPoint(0.0, 0.0),
            new ChartPoint(1.0, 0.0),
            new ChartPoint(2.0, 2.0),
            new ChartPoint(3.0, 0.0)
        };

        ChartPoint[] nearLineSimplified = ChartPointReducerOperations.SimplifyDouglasPeucker(nearLine, 0.1);
        ChartPoint[] spikeSimplified = ChartPointReducerOperations.SimplifyLine(spike, 0.75);

        AssertPoints(nearLineSimplified, new double[] { 0.0, 0.0, 3.0, 0.0 });
        AssertPoints(spikeSimplified, new double[] { 0.0, 0.0, 2.0, 2.0, 3.0, 0.0 });
    }

    [Test]
    public void DecimateScatter_SelectsEvenlySpacedPointsAndKeepsEndpoints()
    {
        ChartPoint[] points = new ChartPoint[10];
        int index = 0;
        while (index < points.Length)
        {
            points[index] = new ChartPoint(index, index * 10.0);
            index = index + 1;
        }

        ChartPoint[] decimated = ChartPointReducerOperations.DecimateScatter(points, 4);

        AssertPoints(decimated, new double[] { 0.0, 0.0, 3.0, 30.0, 6.0, 60.0, 9.0, 90.0 });
    }

    [Test]
    public void Reducers_ReturnIndependentPointCopies()
    {
        ChartPoint[] points = new ChartPoint[]
        {
            new ChartPoint(0.0, 0.0),
            new ChartPoint(1.0, 1.0)
        };

        ChartPoint[] copy = ChartPointReducerOperations.DecimateScatter(points, 5);
        points[0].X = 99.0;
        copy[0].Y = 77.0;

        Assert.That(copy[0].X, Is.EqualTo(0.0));
        Assert.That(points[0].Y, Is.EqualTo(0.0));
    }

    [Test]
    public void DistanceHelpers_ComputeSquaredDistancesToSegments()
    {
        ChartPoint point = new ChartPoint(1.0, 1.0);
        ChartPoint start = new ChartPoint(0.0, 0.0);
        ChartPoint end = new ChartPoint(2.0, 0.0);

        Assert.That(ChartPointReducerOperations.DistanceSquared(start, end), Is.EqualTo(4.0));
        Assert.That(ChartPointReducerOperations.PerpendicularDistanceSquared(point, start, end), Is.EqualTo(1.0).Within(0.0000001));
        Assert.That(ChartPointReducerOperations.PerpendicularDistanceSquared(point, start, start), Is.EqualTo(2.0).Within(0.0000001));
    }

    [Test]
    public void PointReducer_ValidatesInputs()
    {
        Assert.Throws<System.ArgumentNullException>(() => ChartPointReducerOperations.SimplifyLine(null!, 1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartPointReducerOperations.SimplifyLine(new ChartPoint[0], -1.0));
        Assert.Throws<System.ArgumentException>(() => ChartPointReducerOperations.SimplifyLine(new ChartPoint[] { new ChartPoint(double.NaN, 0.0) }, 1.0));
        Assert.Throws<System.ArgumentNullException>(() => ChartPointReducerOperations.SimplifyLine(new ChartPoint[] { null! }, 1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartPointReducerOperations.DecimateScatter(new ChartPoint[] { new ChartPoint(0.0, 0.0) }, 1));
    }

    static void AssertPoints(ChartPoint[] points, double[] coordinates)
    {
        Assert.That(points.Length * 2, Is.EqualTo(coordinates.Length));
        int index = 0;
        while (index < points.Length)
        {
            Assert.That(points[index].X, Is.EqualTo(coordinates[index * 2]).Within(0.0000001));
            Assert.That(points[index].Y, Is.EqualTo(coordinates[index * 2 + 1]).Within(0.0000001));
            index = index + 1;
        }
    }
}
