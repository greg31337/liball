using geo;

namespace geo.Tests;

public class LinearReferencerTests
{
    [Test]
    public void LengthIndexesInterpolateAndHonorRangeOptions()
    {
        ILinearReferencer referencer = new LinearReferencer();
        Geometry source = Read(
            "LINESTRING ZM (0 0 0 0, 3 4 5 10, 6 4 8 20)");
        LinearReferenceOptions strict = new LinearReferenceOptions();
        LinearReferenceOptions clamp = new LinearReferenceOptions
        {
            ClampOutOfRange = true
        };
        LinearLocation absolute = referencer.GetLocationAtAbsoluteIndex(
            source, 2.5, strict);
        Coordinate? absoluteCoordinate =
            referencer.GetCoordinateAtAbsoluteIndex(
                source, 2.5, strict);
        Coordinate? normalizedCoordinate =
            referencer.GetCoordinateAtNormalizedIndex(
                source, 0.8125, strict);
        LinearLocation clamped = referencer.GetLocationAtAbsoluteIndex(
            source, -4.0, clamp);

        Assert.Multiple(() =>
        {
            Assert.That(referencer.GetTotalLength(source),
                Is.EqualTo(8.0));
            AssertLocation(absolute, true, 0, 0, 0.5);
            Assert.That(referencer.GetAbsoluteIndex(source, absolute),
                Is.EqualTo(2.5));
            Assert.That(referencer.GetNormalizedIndex(source, absolute),
                Is.EqualTo(0.3125));
            AssertCoordinate(absoluteCoordinate!,
                1.5, 2.0, 2.5, 5.0);
            AssertCoordinate(normalizedCoordinate!,
                4.5, 4.0, 6.5, 15.0);
            AssertLocation(clamped, true, 0, 0, 0.0);
            Assert.Throws<InvalidGeometryException>(() =>
                referencer.GetLocationAtNormalizedIndex(
                    source, 1.1, strict));
        });
    }

    [Test]
    public void LocationsProjectionOffsetsAndSubstringsUsePublicService()
    {
        ILinearReferencer referencer = new LinearReferencer();
        Geometry source = Read(
            "LINESTRING (0 0, 10 0, 10 10)");
        LinearLocation start = referencer.GetStartLocation(source);
        LinearLocation end = referencer.GetEndLocation(source);
        LinearLocation projected = referencer.ProjectCoordinate(
            source, new Coordinate { X = 8.0, Y = 3.0 });
        Coordinate? projectedCoordinate =
            referencer.GetCoordinateAtLocation(source, projected);
        Coordinate? offset = referencer.GetOffsetCoordinate(
            source, projected, 2.0);
        Geometry substring = referencer.ExtractLine(
            source, Location(0, 0, 0.5), Location(0, 1, 0.5));

        Assert.Multiple(() =>
        {
            AssertLocation(start, true, 0, 0, 0.0);
            AssertLocation(end, true, 0, 2, 0.0);
            Assert.That(referencer.IsEndpoint(source, end), Is.True);
            Assert.That(referencer.CompareLocations(start, end),
                Is.Negative);
            AssertLocation(projected, true, 0, 1, 0.3);
            AssertPlanar(projectedCoordinate!, 10.0, 3.0);
            AssertPlanar(offset!, 8.0, 3.0);
            Assert.That(substring.LineString!.Coordinates,
                Has.Length.EqualTo(3));
            AssertPlanar(substring.LineString.Coordinates[0], 5.0, 0.0);
            AssertPlanar(substring.LineString.Coordinates[2], 10.0, 5.0);
        });
    }

    [Test]
    public void CoincidentComponentBoundariesUseOptionResolutionPolicy()
    {
        ILinearReferencer referencer = new LinearReferencer();
        Geometry source = Read(
            "MULTILINESTRING ((0 0, 3 0), (10 0, 10 4))");
        LinearReferenceOptions lower = new LinearReferenceOptions();
        LinearReferenceOptions upper = new LinearReferenceOptions
        {
            ResolveLowerLocation = false
        };
        LinearLocation lowerLength =
            referencer.GetLocationAtAbsoluteIndex(
                source, 3.0, lower);
        LinearLocation upperLength =
            referencer.GetLocationAtAbsoluteIndex(
                source, 3.0, upper);
        LinearLocation lowerEndpoint =
            referencer.ResolveComponentEndpoint(
                source, Location(1, 0, 0.0), lower);
        LinearLocation upperEndpoint =
            referencer.ResolveComponentEndpoint(
                source, Location(0, 1, 0.0), upper);

        Assert.Multiple(() =>
        {
            AssertLocation(lowerLength, true, 0, 1, 0.0);
            AssertLocation(upperLength, true, 1, 0, 0.0);
            AssertLocation(lowerEndpoint, true, 0, 1, 0.0);
            AssertLocation(upperEndpoint, true, 1, 0, 0.0);
        });
    }

    [Test]
    public void MeasuredOperationsExposeResolutionAndClampingPolicies()
    {
        ILinearReferencer referencer = new LinearReferencer();
        Geometry source = Read(
            "LINESTRING ZM (0 0 0 0, 10 0 10 10, " +
            "20 0 20 10, 40 0 40 30)");
        LinearReferenceOptions earlier = new LinearReferenceOptions();
        LinearReferenceOptions later = new LinearReferenceOptions
        {
            ResolveLowerLocation = false
        };
        LinearReferenceOptions clamp = new LinearReferenceOptions
        {
            ClampOutOfRange = true
        };
        LinearLocation earlierLocation = referencer.GetLocationAtMeasure(
            source, 10.0, earlier);
        LinearLocation laterLocation = referencer.GetLocationAtMeasure(
            source, 10.0, later);
        Coordinate? measured = referencer.GetCoordinateAtMeasure(
            source, 20.0, earlier);
        Coordinate? clamped = referencer.GetCoordinateAtMeasure(
            source, 50.0, clamp);
        Geometry interval = referencer.ExtractMeasureInterval(
            source, 5.0, 20.0, earlier);

        Assert.Multiple(() =>
        {
            Assert.That(referencer.IsMonotonicMeasure(source), Is.True);
            AssertLocation(earlierLocation, true, 0, 1, 0.0);
            AssertLocation(laterLocation, true, 0, 2, 0.0);
            Assert.That(referencer.GetMeasureAtLocation(
                source, laterLocation), Is.EqualTo(10.0));
            AssertCoordinate(measured!, 30.0, 0.0, 30.0, 20.0);
            AssertCoordinate(clamped!, 40.0, 0.0, 40.0, 30.0);
            AssertCoordinate(interval.LineString!.Coordinates[0],
                5.0, 0.0, 5.0, 5.0);
            Assert.That(interval.LineString.Coordinates,
                Has.Length.EqualTo(4));
            AssertCoordinate(interval.LineString.Coordinates[2],
                20.0, 0.0, 20.0, 10.0);
            AssertCoordinate(interval.LineString.Coordinates[3],
                30.0, 0.0, 30.0, 20.0);
            Assert.Throws<InvalidGeometryException>(() =>
                referencer.GetLocationAtMeasure(
                    source, 31.0, earlier));
        });
    }

    [Test]
    public void EmptyLineworkAndMissingOptionsHaveExplicitOutcomes()
    {
        ILinearReferencer referencer = new LinearReferencer();
        Geometry empty = Read("LINESTRING EMPTY");
        LinearReferenceOptions options = new LinearReferenceOptions();
        LinearLocation normalized = referencer.NormalizeLocation(
            empty, Location(0, 0, 0.0));

        Assert.Multiple(() =>
        {
            Assert.That(referencer.GetStartLocation(empty).IsValid,
                Is.False);
            Assert.That(referencer.GetEndLocation(empty).IsValid,
                Is.False);
            Assert.That(normalized.IsValid, Is.False);
            Assert.That(referencer.GetCoordinateAtAbsoluteIndex(
                empty, 0.0, options), Is.Null);
            Assert.Throws<InvalidGeometryException>(() =>
                referencer.GetLocationAtAbsoluteIndex(
                    empty, 0.0, null!));
            Assert.Throws<InvalidGeometryException>(() =>
                referencer.ResolveComponentEndpoint(
                    empty, normalized, null!));
        });
    }

    private static LinearLocation Location(
        int component, int segment, double fraction)
    {
        return new LinearLocation
        {
            ComponentIndex = component,
            SegmentIndex = segment,
            SegmentFraction = fraction
        };
    }

    private static void AssertLocation(
        LinearLocation location, bool valid,
        int component, int segment, double fraction)
    {
        Assert.Multiple(() =>
        {
            Assert.That(location.IsValid, Is.EqualTo(valid));
            Assert.That(location.ComponentIndex, Is.EqualTo(component));
            Assert.That(location.SegmentIndex, Is.EqualTo(segment));
            Assert.That(location.SegmentFraction,
                Is.EqualTo(fraction).Within(1.0e-14));
        });
    }

    private static void AssertCoordinate(
        Coordinate coordinate, double x, double y,
        double z, double measure)
    {
        Assert.Multiple(() =>
        {
            Assert.That(coordinate.X, Is.EqualTo(x).Within(1.0e-14));
            Assert.That(coordinate.Y, Is.EqualTo(y).Within(1.0e-14));
            Assert.That(coordinate.Z, Is.EqualTo(z).Within(1.0e-14));
            Assert.That(coordinate.M,
                Is.EqualTo(measure).Within(1.0e-14));
        });
    }

    private static void AssertPlanar(
        Coordinate coordinate, double x, double y)
    {
        Assert.Multiple(() =>
        {
            Assert.That(coordinate.X, Is.EqualTo(x).Within(1.0e-14));
            Assert.That(coordinate.Y, Is.EqualTo(y).Within(1.0e-14));
        });
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
