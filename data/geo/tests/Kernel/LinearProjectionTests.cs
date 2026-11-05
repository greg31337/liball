using geo;

namespace geo.Tests;

public class LinearProjectionTests
{
    [Test]
    public void ProjectionAndInterpolationPreserveZAndM()
    {
        Geometry line = Read(
            "LINESTRING ZM (0 0 0 0, 10 0 10 100)");
        LinearLocation location = LinearLocationMath.ProjectCoordinate(
            line, new Coordinate { X = 3.0, Y = 4.0 });
        Coordinate? coordinate =
            LinearLocationMath.GetCoordinateAtLocation(line, location);

        Assert.Multiple(() =>
        {
            AssertLocation(location, true, 0, 0, 0.3);
            AssertCoordinate(coordinate, 3.0, 0.0, 3.0, 30.0);
        });
    }

    [Test]
    public void ProjectionUsesEarliestLocationForDegenerateAndComponentTies()
    {
        Geometry multiLine = MultiLineWithDegenerateSegment();
        LinearLocation middle = LinearLocationMath.ProjectCoordinate(
            multiLine, new Coordinate { X = 5.0, Y = 2.0 });
        LinearLocation endpoint = LinearLocationMath.ProjectCoordinate(
            multiLine, new Coordinate { X = 0.0, Y = 2.0 });

        Assert.Multiple(() =>
        {
            AssertLocation(middle, true, 0, 1, 0.5);
            AssertLocation(endpoint, true, 0, 0, 0.0);
        });
    }

    [Test]
    public void SignedOffsetsUseNearbyNonDegenerateDirection()
    {
        Geometry line = Read(
            "LINESTRING ZM (0 0 0 0, 0 0 0 0, " +
            "10 0 10 100, 10 10 20 200)");
        Coordinate? repeatedStart =
            LinearLocationMath.GetOffsetCoordinate(
                line, Location(0, 0, 0.0), 2.0);
        Coordinate? horizontal =
            LinearLocationMath.GetOffsetCoordinate(
                line, Location(0, 1, 0.5), -2.0);
        Coordinate? corner =
            LinearLocationMath.GetOffsetCoordinate(
                line, Location(0, 2, 0.0), 2.0);
        Coordinate? endpoint =
            LinearLocationMath.GetOffsetCoordinate(
                line, Location(0, 3, 0.0), 2.0);

        Assert.Multiple(() =>
        {
            AssertCoordinate(repeatedStart, 0.0, 2.0, 0.0, 0.0);
            AssertCoordinate(horizontal, 5.0, -2.0, 5.0, 50.0);
            AssertCoordinate(corner, 8.0, 0.0, 10.0, 100.0);
            AssertCoordinate(endpoint, 8.0, 10.0, 20.0, 200.0);
        });
    }

    [Test]
    public void EmptyAndWhollyDegenerateLinesHaveExplicitResults()
    {
        Geometry empty = Read("LINESTRING EMPTY");
        Geometry repeated = Read("LINESTRING (2 3, 2 3)");
        LinearLocation projected = LinearLocationMath.ProjectCoordinate(
            empty, new Coordinate { X = 1.0, Y = 1.0 });

        Assert.Multiple(() =>
        {
            Assert.That(projected.IsValid, Is.False);
            Assert.That(LinearLocationMath.GetCoordinateAtLocation(
                empty, projected), Is.Null);
            Assert.That(LinearLocationMath.GetOffsetCoordinate(
                empty, projected, 1.0), Is.Null);
            Assert.Throws<InvalidGeometryException>(() =>
                LinearLocationMath.GetOffsetCoordinate(
                    repeated, Location(0, 0, 0.0), 1.0));
            Assert.Throws<InvalidGeometryException>(() =>
                LinearLocationMath.GetOffsetCoordinate(
                    repeated, Location(0, 0, 0.0), double.NaN));
        });
    }

    private static Geometry MultiLineWithDegenerateSegment()
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
                            new Coordinate { X = 0.0, Y = 0.0 },
                            new Coordinate { X = 10.0, Y = 0.0 }
                        ]
                    },
                    new LineString
                    {
                        Coordinates =
                        [
                            new Coordinate { X = 0.0, Y = 4.0 },
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
        Coordinate? coordinate, double x, double y,
        double z, double m)
    {
        Assert.That(coordinate, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(coordinate!.X, Is.EqualTo(x).Within(1.0e-14));
            Assert.That(coordinate.Y, Is.EqualTo(y).Within(1.0e-14));
            Assert.That(coordinate.Z, Is.EqualTo(z).Within(1.0e-14));
            Assert.That(coordinate.M, Is.EqualTo(m).Within(1.0e-14));
        });
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
