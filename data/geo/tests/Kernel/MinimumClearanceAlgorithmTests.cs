using geo;

namespace geo.Tests;

public class MinimumClearanceAlgorithmTests
{
    [Test]
    public void SinglePointHasNoMinimumClearance()
    {
        DistanceResult result = MinimumClearanceAlgorithm.GetMinimumClearance(
            Read("POINT (1 2)"));

        AssertNoResult(result);
    }

    [Test]
    public void IdenticalMultiPointMembersDoNotDefineClearance()
    {
        DistanceResult result = MinimumClearanceAlgorithm.GetMinimumClearance(
            Read("MULTIPOINT ((1 2), (1 2), (1 2))"));

        AssertNoResult(result);
    }

    [Test]
    public void MultiPointClearanceSkipsDuplicatesAndReturnsOwningWitnesses()
    {
        Geometry geometry = Read(
            "MULTIPOINT ZM ((0 0 1 2), (0 0 3 4), (3 4 5 6))");

        DistanceResult result =
            MinimumClearanceAlgorithm.GetMinimumClearance(geometry);
        geometry.MultiPoint!.Points[0].Coordinate!.X = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(result.HasResult, Is.True);
            Assert.That(result.Distance, Is.EqualTo(5.0));
            AssertCoordinate(result.FirstCoordinate, 0.0, 0.0, 1.0, 2.0);
            AssertCoordinate(result.SecondCoordinate, 3.0, 4.0, 5.0, 6.0);
            Assert.That(result.FirstLocation, Is.Null);
            Assert.That(result.SecondLocation, Is.Null);
        });
    }

    [Test]
    public void LineClearanceCanBeRealizedByTwoVertices()
    {
        Geometry line = Read(
            "LINESTRING ZM (0 0 1 2, 10 0 3 4, 10 1 5 6)");

        DistanceResult result =
            MinimumClearanceAlgorithm.GetMinimumClearance(line);

        Assert.Multiple(() =>
        {
            Assert.That(result.Distance, Is.EqualTo(1.0));
            AssertCoordinate(result.FirstCoordinate, 10.0, 0.0, 3.0, 4.0);
            AssertCoordinate(result.SecondCoordinate, 10.0, 1.0, 5.0, 6.0);
            AssertLocation(result.FirstLocation, 0, 0, 1.0);
            AssertLocation(result.SecondLocation, 0, 1, 1.0);
        });
    }

    [Test]
    public void VertexToNonIncidentSegmentCanRealizeClearance()
    {
        Geometry line = Read("LINESTRING Z (0 0 0, 10 0 10, 5 1 30)");

        DistanceResult result =
            MinimumClearanceAlgorithm.GetMinimumClearance(line);

        Assert.Multiple(() =>
        {
            Assert.That(result.Distance, Is.EqualTo(1.0));
            AssertCoordinate(result.FirstCoordinate,
                5.0, 1.0, 30.0, double.NaN);
            AssertCoordinate(result.SecondCoordinate,
                5.0, 0.0, 5.0, double.NaN);
            AssertLocation(result.FirstLocation, 0, 1, 1.0);
            AssertLocation(result.SecondLocation, 0, 0, 0.5);
        });
    }

    [Test]
    public void VertexOnNonIncidentSegmentProducesZeroClearance()
    {
        Geometry line = Read("LINESTRING (0 0, 4 0, 2 0)");

        DistanceResult result =
            MinimumClearanceAlgorithm.GetMinimumClearance(line);

        Assert.Multiple(() =>
        {
            Assert.That(result.Distance, Is.Zero);
            AssertCoordinate(result.FirstCoordinate,
                2.0, 0.0, double.NaN, double.NaN);
            AssertCoordinate(result.SecondCoordinate,
                2.0, 0.0, double.NaN, double.NaN);
            AssertLocation(result.FirstLocation, 0, 1, 1.0);
            AssertLocation(result.SecondLocation, 0, 0, 0.5);
        });
    }

    [Test]
    public void EmptyComponentsCountTowardWitnessComponentLocations()
    {
        Geometry geometry = Read(
            "MULTILINESTRING (EMPTY, (0 0, 10 0), (5 2, 5 20))");

        DistanceResult result =
            MinimumClearanceAlgorithm.GetMinimumClearance(geometry);

        Assert.Multiple(() =>
        {
            Assert.That(result.Distance, Is.EqualTo(2.0));
            AssertCoordinate(result.FirstCoordinate,
                5.0, 2.0, double.NaN, double.NaN);
            AssertCoordinate(result.SecondCoordinate,
                5.0, 0.0, double.NaN, double.NaN);
            AssertLocation(result.FirstLocation, 2, 0, 0.0);
            AssertLocation(result.SecondLocation, 1, 0, 0.5);
        });
    }

    [Test]
    public void EmptyGeometryHasNoMinimumClearance()
    {
        DistanceResult result = MinimumClearanceAlgorithm.GetMinimumClearance(
            Read("GEOMETRYCOLLECTION EMPTY"));

        AssertNoResult(result);
    }

    private static void AssertNoResult(DistanceResult result)
    {
        Assert.That(result.HasResult, Is.False);
        Assert.That(result.Distance, Is.EqualTo(double.PositiveInfinity));
        Assert.That(result.FirstCoordinate, Is.Null);
        Assert.That(result.SecondCoordinate, Is.Null);
        Assert.That(result.FirstLocation, Is.Null);
        Assert.That(result.SecondLocation, Is.Null);
    }

    private static void AssertLocation(
        LinearLocation? location, int componentIndex,
        int segmentIndex, double segmentFraction)
    {
        Assert.That(location, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(location!.IsValid, Is.True);
            Assert.That(location.ComponentIndex, Is.EqualTo(componentIndex));
            Assert.That(location.SegmentIndex, Is.EqualTo(segmentIndex));
            Assert.That(location.SegmentFraction,
                Is.EqualTo(segmentFraction).Within(1.0e-15));
        });
    }

    private static void AssertCoordinate(
        Coordinate? coordinate, double x, double y, double z, double m)
    {
        Assert.That(coordinate, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(coordinate!.X, Is.EqualTo(x));
            Assert.That(coordinate.Y, Is.EqualTo(y));
            AssertOrdinate(coordinate.Z, z);
            AssertOrdinate(coordinate.M, m);
        });
    }

    private static void AssertOrdinate(double actual, double expected)
    {
        if (double.IsNaN(expected))
        {
            Assert.That(double.IsNaN(actual), Is.True);
            return;
        }
        Assert.That(actual, Is.EqualTo(expected));
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
