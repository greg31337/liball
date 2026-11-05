using geo;

namespace geo.Tests;

public class DensifierTests
{
    [Test]
    public void LongSegmentsAreSplitAtTheMaximumLengthWithInterpolatedOrdinates()
    {
        Geometry source = ReadWithSrid(
            "SRID=3857;LINESTRING ZM (0 0 0 100, 10 0 40 200)");

        Geometry result = Densifier.DensifyGeometry(
            source, 3.0, FloatingPrecision());
        Coordinate[] coordinates = result.LineString!.Coordinates;

        Assert.Multiple(() =>
        {
            Assert.That(result.SpatialReferenceId, Is.EqualTo(3857));
            Assert.That(result.CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(coordinates, Has.Length.EqualTo(5));
            AssertCoordinate(coordinates[0], 0.0, 0.0, 0.0, 100.0);
            AssertCoordinate(coordinates[1], 2.5, 0.0, 10.0, 125.0);
            AssertCoordinate(coordinates[2], 5.0, 0.0, 20.0, 150.0);
            AssertCoordinate(coordinates[3], 7.5, 0.0, 30.0, 175.0);
            AssertCoordinate(coordinates[4], 10.0, 0.0, 40.0, 200.0);
            AssertMaximumSegmentLength(coordinates, 3.0);
        });

        source.LineString!.Coordinates[0].X = 99.0;
        Assert.That(coordinates[0].X, Is.EqualTo(0.0));
    }

    [Test]
    public void EachSourceEdgeIsSubdividedIndependentlyAndKeepsVertices()
    {
        Geometry source = Read(
            "LINESTRING (0 0, 2 0, 2 2)");

        Geometry result = Densifier.DensifyGeometry(
            source, 1.0, FloatingPrecision());
        Coordinate[] coordinates = result.LineString!.Coordinates;

        Assert.Multiple(() =>
        {
            Assert.That(coordinates, Has.Length.EqualTo(5));
            AssertCoordinate(coordinates[0], 0.0, 0.0);
            AssertCoordinate(coordinates[1], 1.0, 0.0);
            AssertCoordinate(coordinates[2], 2.0, 0.0);
            AssertCoordinate(coordinates[3], 2.0, 1.0);
            AssertCoordinate(coordinates[4], 2.0, 2.0);
        });
    }

    [Test]
    public void FixedPrecisionIsAppliedAfterSourceEdgeInterpolation()
    {
        Geometry source = Read(
            "LINESTRING ZM (0.2 0.2 0 0, 2.2 0.2 6 12)");
        PrecisionModel precision = new PrecisionModel
        {
            Type = PrecisionModelType.Fixed,
            Scale = 2.0
        };

        Geometry result = Densifier.DensifyGeometry(
            source, 0.75, precision);
        Coordinate[] coordinates = result.LineString!.Coordinates;

        Assert.Multiple(() =>
        {
            Assert.That(coordinates, Has.Length.EqualTo(4));
            AssertCoordinate(coordinates[0], 0.0, 0.0, 0.0, 0.0);
            AssertCoordinate(coordinates[1], 1.0, 0.0, 2.0, 4.0);
            AssertCoordinate(coordinates[2], 1.5, 0.0, 4.0, 8.0);
            AssertCoordinate(coordinates[3], 2.0, 0.0, 6.0, 12.0);
        });
    }

    [Test]
    public void PolygonRingsAreDensifiedAndRemainClosed()
    {
        Geometry source = Read(
            "POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0), " +
            "(1 1, 1 3, 3 3, 3 1, 1 1))");

        Geometry result = Densifier.DensifyGeometry(
            source, 1.5, FloatingPrecision());
        Coordinate[] shell = result.Polygon!.Shell.Coordinates;
        Coordinate[] hole = result.Polygon.Holes[0].Coordinates;

        Assert.Multiple(() =>
        {
            Assert.That(shell, Has.Length.EqualTo(13));
            Assert.That(hole, Has.Length.EqualTo(9));
            Assert.That(shell[0].X, Is.EqualTo(shell[shell.Length - 1].X));
            Assert.That(shell[0].Y, Is.EqualTo(shell[shell.Length - 1].Y));
            Assert.That(hole[0].X, Is.EqualTo(hole[hole.Length - 1].X));
            Assert.That(hole[0].Y, Is.EqualTo(hole[hole.Length - 1].Y));
            AssertMaximumSegmentLength(shell, 1.5);
            AssertMaximumSegmentLength(hole, 1.5);
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(result),
                Is.EqualTo(12.0));
        });
    }

    [Test]
    public void CollectionsAndPointsReceivePrecisionAndKeepTheirShape()
    {
        Geometry source = ReadWithSrid(
            "SRID=4326;GEOMETRYCOLLECTION (POINT (16777217 2.25), " +
            "MULTILINESTRING ((0 0, 2 0), (3 0, 3 2)))");
        PrecisionModel precision = new PrecisionModel
        {
            Type = PrecisionModelType.FloatingSingle
        };

        Geometry result = Densifier.DensifyGeometry(
            source, 1.0, precision);
        Geometry[] components =
            result.GeometryCollection!.Geometries;

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.GeometryCollection));
            Assert.That(result.SpatialReferenceId, Is.EqualTo(4326));
            Assert.That(components[0].Point!.Coordinate!.X,
                Is.EqualTo(16777216.0));
            Assert.That(components[1].GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(components[1].MultiLineString!.LineStrings[0]
                .Coordinates, Has.Length.EqualTo(3));
            Assert.That(components[1].MultiLineString!.LineStrings[1]
                .Coordinates, Has.Length.EqualTo(3));
        });
    }

    [Test]
    public void PrecisionDuplicatesAreRemovedWithoutInvalidLineCollapse()
    {
        Geometry line = Read(
            "LINESTRING Z (0.1 0.1 1, 0.2 0.2 2)");
        PrecisionModel precision = new PrecisionModel
        {
            Type = PrecisionModelType.Fixed,
            Scale = 1.0
        };

        Geometry result = Densifier.DensifyGeometry(
            line, 1.0, precision);

        Assert.Multiple(() =>
        {
            Assert.That(result.LineString!.Coordinates,
                Has.Length.EqualTo(2));
            AssertCoordinate(
                result.LineString.Coordinates[0], 0.0, 0.0, 1.0);
            AssertCoordinate(
                result.LineString.Coordinates[1], 0.0, 0.0, 2.0);
            Assert.Throws<PrecisionException>(() =>
                Densifier.DensifyGeometry(
                    Read("POLYGON ((0.1 0.1, 0.2 0.1, 0.2 0.2, 0.1 0.2, 0.1 0.1))"),
                    1.0, precision));
        });
    }

    [Test]
    public void EmptyValuesRemainEmptyWithTheirDeclaredType()
    {
        Geometry line = Densifier.DensifyGeometry(
            Read("LINESTRING EMPTY"), 1.0, FloatingPrecision());
        Geometry polygon = Densifier.DensifyGeometry(
            Read("POLYGON EMPTY"), 1.0, FloatingPrecision());

        Assert.Multiple(() =>
        {
            Assert.That(line.GeometryType,
                Is.EqualTo(GeometryType.LineString));
            Assert.That(GeometryInspection.IsEmptyValue(line), Is.True);
            Assert.That(polygon.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(GeometryInspection.IsEmptyValue(polygon), Is.True);
        });
    }

    [Test]
    public void InvalidLengthsPrecisionAndUnsupportedGeometryAreRejected()
    {
        Geometry line = Read("LINESTRING (0 0, 1 1)");
        PrecisionModel invalidType = new PrecisionModel
        {
            Type = (PrecisionModelType)99
        };
        PrecisionModel invalidFixed = new PrecisionModel
        {
            Type = PrecisionModelType.Fixed,
            Scale = 0.0
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                Densifier.DensifyGeometry(
                    line, 0.0, FloatingPrecision()));
            Assert.Throws<InvalidGeometryException>(() =>
                Densifier.DensifyGeometry(
                    line, double.NaN, FloatingPrecision()));
            Assert.Throws<PrecisionException>(() =>
                Densifier.DensifyGeometry(line, 1.0, null!));
            Assert.Throws<PrecisionException>(() =>
                Densifier.DensifyGeometry(line, 1.0, invalidType));
            Assert.Throws<PrecisionException>(() =>
                Densifier.DensifyGeometry(line, 1.0, invalidFixed));
            Assert.Throws<InvalidGeometryException>(() =>
                Densifier.DensifyGeometry(
                    Read("LINESTRING (0 0, 1000001 0)"),
                    1.0, FloatingPrecision()));
            Assert.Throws<InvalidGeometryException>(() =>
                Densifier.DensifyGeometry(
                    Read("TRIANGLE ((0 0, 2 0, 0 2, 0 0))"),
                    1.0, FloatingPrecision()));
        });
    }

    private static void AssertMaximumSegmentLength(
        Coordinate[] coordinates, double maximum)
    {
        int index = 0;
        while (index + 1 < coordinates.Length)
        {
            double length = SegmentDistanceAlgorithms.GetPlanarDistance(
                coordinates[index], coordinates[index + 1]);
            Assert.That(length, Is.LessThanOrEqualTo(maximum));
            index++;
        }
    }

    private static PrecisionModel FloatingPrecision()
    {
        return new PrecisionModel
        {
            Type = PrecisionModelType.Floating
        };
    }

    private static void AssertCoordinate(
        Coordinate coordinate, double x, double y,
        double z = double.NaN, double m = double.NaN)
    {
        Assert.That(coordinate.X, Is.EqualTo(x).Within(1e-12));
        Assert.That(coordinate.Y, Is.EqualTo(y).Within(1e-12));
        if (!double.IsNaN(z))
        {
            Assert.That(coordinate.Z, Is.EqualTo(z).Within(1e-12));
        }
        if (!double.IsNaN(m))
        {
            Assert.That(coordinate.M, Is.EqualTo(m).Within(1e-12));
        }
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }

    private static Geometry ReadWithSrid(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions { AllowSpatialReferenceId = true },
            new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
