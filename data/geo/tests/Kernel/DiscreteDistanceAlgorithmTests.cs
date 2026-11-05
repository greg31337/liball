using geo;

namespace geo.Tests;

public class DiscreteDistanceAlgorithmTests
{
    [Test]
    public void HausdorffDistanceReportsSymmetricLinearWitnesses()
    {
        Geometry first = Read("LINESTRING (0 0, 10 0)");
        Geometry second = Read("LINESTRING (0 3, 10 3)");

        DistanceResult result = HausdorffAlgorithm.GetDistance(first, second);
        first.LineString!.Coordinates[0].X = 100.0;
        second.LineString!.Coordinates[0].Y = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(result.HasResult, Is.True);
            Assert.That(result.Distance, Is.EqualTo(3.0));
            AssertCoordinate(result.FirstCoordinate,
                0.0, 0.0, double.NaN, double.NaN);
            AssertCoordinate(result.SecondCoordinate,
                0.0, 3.0, double.NaN, double.NaN);
            AssertLocation(result.FirstLocation, 0, 0, 0.0);
            AssertLocation(result.SecondLocation, 0, 0, 0.0);
        });
    }

    [Test]
    public void HausdorffDistanceUsesTheFarthestDirectedVertex()
    {
        Geometry first = Read("LINESTRING (0 0, 10 0)");
        Geometry second = Read("LINESTRING (0 0, 4 0)");

        DistanceResult forward = HausdorffAlgorithm.GetDistance(first, second);
        DistanceResult reverse = HausdorffAlgorithm.GetDistance(second, first);

        Assert.Multiple(() =>
        {
            Assert.That(forward.Distance, Is.EqualTo(6.0));
            AssertCoordinate(forward.FirstCoordinate,
                10.0, 0.0, double.NaN, double.NaN);
            AssertCoordinate(forward.SecondCoordinate,
                4.0, 0.0, double.NaN, double.NaN);
            AssertLocation(forward.FirstLocation, 0, 0, 1.0);
            AssertLocation(forward.SecondLocation, 0, 0, 1.0);
            Assert.That(reverse.Distance, Is.EqualTo(forward.Distance));
            AssertCoordinate(reverse.FirstCoordinate,
                4.0, 0.0, double.NaN, double.NaN);
            AssertCoordinate(reverse.SecondCoordinate,
                10.0, 0.0, double.NaN, double.NaN);
        });
    }

    [Test]
    public void DensifiedHausdorffSamplesSegmentInteriorsAndInterpolatesOrdinates()
    {
        Geometry line = Read("LINESTRING ZM (0 0 0 10, 10 0 20 30)");
        Geometry endpoints = Read("MULTIPOINT ZM ((0 0 2 4), (10 0 6 8))");

        DistanceResult discrete = HausdorffAlgorithm.GetDistance(
            line, endpoints);
        DistanceResult densified = HausdorffAlgorithm.GetDensifiedDistance(
            line, endpoints, 0.5);

        Assert.Multiple(() =>
        {
            Assert.That(discrete.Distance, Is.Zero);
            Assert.That(densified.Distance, Is.EqualTo(5.0));
            AssertCoordinate(densified.FirstCoordinate,
                5.0, 0.0, 10.0, 20.0);
            AssertCoordinate(densified.SecondCoordinate,
                0.0, 0.0, 2.0, 4.0);
            AssertLocation(densified.FirstLocation, 0, 0, 0.5);
            Assert.That(densified.SecondLocation, Is.Null);
        });
    }

    [Test]
    public void HausdorffSamplesAgainstSegmentsRatherThanOnlyTargetVertices()
    {
        Geometry bentLine = Read("LINESTRING (0 0, 5 2, 10 0)");
        Geometry line = Read("LINESTRING (0 0, 10 0)");

        DistanceResult result = HausdorffAlgorithm.GetDistance(bentLine, line);

        Assert.Multiple(() =>
        {
            Assert.That(result.Distance, Is.EqualTo(2.0));
            AssertCoordinate(result.FirstCoordinate,
                5.0, 2.0, double.NaN, double.NaN);
            AssertCoordinate(result.SecondCoordinate,
                5.0, 0.0, double.NaN, double.NaN);
            AssertLocation(result.FirstLocation, 0, 0, 1.0);
            AssertLocation(result.SecondLocation, 0, 0, 0.5);
        });
    }

    [Test]
    public void FrechetDistanceReportsOrderedCouplingWitnesses()
    {
        Geometry first = Read("LINESTRING (0 0, 10 0)");
        Geometry second = Read("LINESTRING (10 0, 0 0)");

        DistanceResult result = FrechetAlgorithm.GetDistance(first, second);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasResult, Is.True);
            Assert.That(result.Distance, Is.EqualTo(10.0));
            AssertCoordinate(result.FirstCoordinate,
                0.0, 0.0, double.NaN, double.NaN);
            AssertCoordinate(result.SecondCoordinate,
                10.0, 0.0, double.NaN, double.NaN);
            AssertLocation(result.FirstLocation, 0, 0, 0.0);
            AssertLocation(result.SecondLocation, 0, 0, 0.0);
        });
    }

    [Test]
    public void FrechetDistanceUsesMinimumMaximumMonotoneCoupling()
    {
        Geometry first = Read("LINESTRING (0 0, 5 0, 10 0)");
        Geometry second = Read("LINESTRING (0 0, 10 0)");

        DistanceResult result = FrechetAlgorithm.GetDistance(first, second);

        Assert.Multiple(() =>
        {
            Assert.That(result.Distance, Is.EqualTo(5.0));
            AssertCoordinate(result.FirstCoordinate,
                5.0, 0.0, double.NaN, double.NaN);
            Assert.That(result.SecondCoordinate, Is.Not.Null);
            Assert.That(result.SecondCoordinate!.X, Is.AnyOf(0.0, 10.0));
        });
    }

    [Test]
    public void DensifiedFrechetAddsOrderedInteriorSamples()
    {
        Geometry first = Read("LINESTRING (0 0, 10 0)");
        Geometry second = Read("LINESTRING (0 0, 5 0, 10 0)");

        DistanceResult discrete = FrechetAlgorithm.GetDistance(first, second);
        DistanceResult densified = FrechetAlgorithm.GetDensifiedDistance(
            first, second, 0.5);

        Assert.Multiple(() =>
        {
            Assert.That(discrete.Distance, Is.EqualTo(5.0));
            Assert.That(densified.Distance, Is.EqualTo(2.5));
            Assert.That(densified.FirstCoordinate, Is.Not.Null);
            Assert.That(densified.SecondCoordinate, Is.Not.Null);
            Assert.That(SegmentDistanceAlgorithms.GetPlanarDistance(
                densified.FirstCoordinate!, densified.SecondCoordinate!),
                Is.EqualTo(2.5));
            Assert.That(densified.FirstLocation, Is.Not.Null);
            Assert.That(densified.SecondLocation, Is.Not.Null);
        });
    }

    [Test]
    public void EmptyInputsReturnNoDiscreteDistanceWitness()
    {
        Geometry empty = Read("LINESTRING EMPTY");
        Geometry line = Read("LINESTRING (0 0, 1 0)");

        DistanceResult hausdorff = HausdorffAlgorithm.GetDistance(empty, line);
        DistanceResult frechet = FrechetAlgorithm.GetDistance(line, empty);

        Assert.Multiple(() =>
        {
            AssertNoResult(hausdorff);
            AssertNoResult(frechet);
        });
    }

    [Test]
    public void DensifyFractionsAreValidated()
    {
        Geometry line = Read("LINESTRING (0 0, 1 0)");

        Assert.Multiple(() =>
        {
            Assert.That(() => HausdorffAlgorithm.GetDensifiedDistance(
                line, line, 0.0), Throws.TypeOf<InvalidGeometryException>());
            Assert.That(() => FrechetAlgorithm.GetDensifiedDistance(
                line, line, double.NaN),
                Throws.TypeOf<InvalidGeometryException>());
            Assert.That(() => HausdorffAlgorithm.GetDensifiedDistance(
                line, line, 1.01), Throws.TypeOf<InvalidGeometryException>());
            Assert.That(() => FrechetAlgorithm.GetDensifiedDistance(
                line, line, 0.0000001),
                Throws.TypeOf<InvalidGeometryException>());
        });
    }

    [Test]
    public void DiscreteDistancesRequireCompatibleKnownSpatialReferences()
    {
        Geometry first = Read("POINT (0 0)");
        Geometry second = Read("POINT (1 0)");
        first.SpatialReferenceId = 4326;
        second.SpatialReferenceId = 3857;

        Assert.Multiple(() =>
        {
            Assert.That(() => HausdorffAlgorithm.GetDistance(first, second),
                Throws.TypeOf<InvalidGeometryException>());
            Assert.That(() => FrechetAlgorithm.GetDistance(first, second),
                Throws.TypeOf<InvalidGeometryException>());
        });
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
