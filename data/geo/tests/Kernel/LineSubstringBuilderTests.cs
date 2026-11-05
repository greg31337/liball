using geo;

namespace geo.Tests;

public class LineSubstringBuilderTests
{
    [Test]
    public void SingleComponentSubstringInterpolatesOwningZAndMEndpoints()
    {
        Geometry source = Read(
            "LINESTRING ZM (0 0 0 0, 10 0 10 100, " +
            "10 10 20 200)");
        Geometry result = LineSubstringBuilder.ExtractLine(
            source, Location(0, 0, 0.25), Location(0, 1, 0.5));
        source.LineString!.Coordinates[1].X = 500.0;
        Coordinate[] coordinates = result.LineString!.Coordinates;

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.LineString));
            Assert.That(result.CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(coordinates, Has.Length.EqualTo(3));
            AssertCoordinate(coordinates[0], 2.5, 0.0, 2.5, 25.0);
            AssertCoordinate(coordinates[1], 10.0, 0.0, 10.0, 100.0);
            AssertCoordinate(coordinates[2], 10.0, 5.0, 15.0, 150.0);
        });
    }

    [Test]
    public void ReversedSubstringReversesCoordinates()
    {
        Geometry source = Read(
            "LINESTRING (0 0, 10 0, 10 10)");
        Geometry result = LineSubstringBuilder.ExtractLine(
            source, Location(0, 1, 0.5), Location(0, 0, 0.25));
        Coordinate[] coordinates = result.LineString!.Coordinates;

        Assert.Multiple(() =>
        {
            Assert.That(coordinates, Has.Length.EqualTo(3));
            AssertPlanar(coordinates[0], 10.0, 5.0);
            AssertPlanar(coordinates[1], 10.0, 0.0);
            AssertPlanar(coordinates[2], 2.5, 0.0);
        });
    }

    [Test]
    public void CrossComponentSubstringPreservesDiscontinuitiesAndRepeats()
    {
        Geometry source = MultiLineWithEmptyComponent();
        Geometry result = LineSubstringBuilder.ExtractLine(
            source, Location(0, 0, 0.5), Location(2, 0, 0.5));
        LineString[] lines = result.MultiLineString!.LineStrings;

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(lines, Has.Length.EqualTo(2));
            Assert.That(lines[0].Coordinates,
                Has.Length.EqualTo(3));
            AssertPlanar(lines[0].Coordinates[0], 1.5, 0.0);
            AssertPlanar(lines[0].Coordinates[1], 3.0, 0.0);
            AssertPlanar(lines[0].Coordinates[2], 3.0, 0.0);
            Assert.That(lines[1].Coordinates,
                Has.Length.EqualTo(2));
            AssertPlanar(lines[1].Coordinates[0], 10.0, 0.0);
            AssertPlanar(lines[1].Coordinates[1], 10.0, 2.0);
        });
    }

    [Test]
    public void ReversedCrossComponentSubstringReversesComponentOrder()
    {
        Geometry source = MultiLineWithEmptyComponent();
        Geometry result = LineSubstringBuilder.ExtractLine(
            source, Location(2, 0, 0.5), Location(0, 0, 0.5));
        LineString[] lines = result.MultiLineString!.LineStrings;

        Assert.Multiple(() =>
        {
            Assert.That(lines, Has.Length.EqualTo(2));
            AssertPlanar(lines[0].Coordinates[0], 10.0, 2.0);
            AssertPlanar(lines[0].Coordinates[1], 10.0, 0.0);
            AssertPlanar(lines[1].Coordinates[0], 3.0, 0.0);
            AssertPlanar(lines[1].Coordinates[2], 1.5, 0.0);
        });
    }

    [Test]
    public void ZeroLengthAndEmptySubstringsRemainValidLineResults()
    {
        Geometry source = Read("LINESTRING (0 0, 10 0)");
        Geometry pointRange = LineSubstringBuilder.ExtractLine(
            source, Location(0, 0, 0.4), Location(0, 0, 0.4));
        Geometry empty = LineSubstringBuilder.ExtractLine(
            Read("MULTILINESTRING EMPTY"),
            new LinearLocation { IsValid = false },
            new LinearLocation { IsValid = false });

        Assert.Multiple(() =>
        {
            Assert.That(pointRange.LineString!.Coordinates,
                Has.Length.EqualTo(2));
            AssertPlanar(pointRange.LineString.Coordinates[0], 4.0, 0.0);
            AssertPlanar(pointRange.LineString.Coordinates[1], 4.0, 0.0);
            Assert.That(empty.GeometryType,
                Is.EqualTo(GeometryType.LineString));
            Assert.That(empty.LineString!.Coordinates, Is.Empty);
        });
    }

    [Test]
    public void InvalidLocationsAndUnsupportedGeometryAreRejected()
    {
        Geometry line = Read("LINESTRING (0 0, 10 0)");

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                LineSubstringBuilder.ExtractLine(
                    line, null!, Location(0, 0, 0.5)));
            Assert.Throws<InvalidGeometryException>(() =>
                LineSubstringBuilder.ExtractLine(
                    line, Location(0, 0, double.NaN),
                    Location(0, 0, 0.5)));
            Assert.Throws<InvalidGeometryException>(() =>
                LineSubstringBuilder.ExtractLine(
                    Read("POINT (0 0)"),
                    Location(0, 0, 0.0),
                    Location(0, 0, 0.0)));
        });
    }

    private static Geometry MultiLineWithEmptyComponent()
    {
        return new Geometry
        {
            GeometryType = GeometryType.MultiLineString,
            Point = null,
            MultiLineString = new MultiLineString
            {
                LineStrings =
                [
                    new LineString
                    {
                        Coordinates =
                        [
                            new Coordinate { X = 0.0, Y = 0.0 },
                            new Coordinate { X = 3.0, Y = 0.0 },
                            new Coordinate { X = 3.0, Y = 0.0 }
                        ]
                    },
                    new LineString(),
                    new LineString
                    {
                        Coordinates =
                        [
                            new Coordinate { X = 10.0, Y = 0.0 },
                            new Coordinate { X = 10.0, Y = 4.0 }
                        ]
                    }
                ]
            }
        };
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

    private static void AssertCoordinate(
        Coordinate coordinate, double x, double y,
        double z, double m)
    {
        AssertPlanar(coordinate, x, y);
        Assert.That(coordinate.Z, Is.EqualTo(z).Within(1.0e-14));
        Assert.That(coordinate.M, Is.EqualTo(m).Within(1.0e-14));
    }

    private static void AssertPlanar(
        Coordinate coordinate, double x, double y)
    {
        Assert.That(coordinate.X, Is.EqualTo(x).Within(1.0e-14));
        Assert.That(coordinate.Y, Is.EqualTo(y).Within(1.0e-14));
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
