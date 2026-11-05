using geo;

namespace geo.Tests;

public class GeometryMeasureAlgorithmsTests
{
    [Test]
    public void CoordinateCountTraversesClosuresHolesAndNestedComponents()
    {
        Geometry geometry = Read(
            "GEOMETRYCOLLECTION (POINT (9 9), MULTILINESTRING ((0 0, 3 4), (0 0, 0 2)), POLYGON ((0 0, 6 0, 6 4, 0 4, 0 0), (1 1, 3 1, 3 3, 1 3, 1 1)), POINT EMPTY)");

        Assert.Multiple(() =>
        {
            Assert.That(GeometryMeasureAlgorithms.CountCoordinates(geometry),
                Is.EqualTo(15));
            Assert.That(GeometryMeasureAlgorithms.CountCoordinates(
                Read("GEOMETRYCOLLECTION EMPTY")), Is.Zero);
            Assert.That(GeometryMeasureAlgorithms.CountCoordinates(
                Read("TRIANGLE ((0 0, 2 0, 0 2, 0 0))")), Is.EqualTo(4));
        });
    }

    [Test]
    public void PolygonAreaSubtractsHolesIndependentOfRingOrientation()
    {
        Geometry counterClockwise = Read(
            "POLYGON ((0 0, 6 0, 6 4, 0 4, 0 0), (1 1, 3 1, 3 3, 1 3, 1 1))");
        Geometry clockwise = Read(
            "POLYGON ((0 0, 0 4, 6 4, 6 0, 0 0), (1 1, 1 3, 3 3, 3 1, 1 1))");

        Assert.Multiple(() =>
        {
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(
                counterClockwise), Is.EqualTo(20.0));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(
                clockwise), Is.EqualTo(20.0));
            Assert.That(GeometryMeasureAlgorithms.GetSignedProjectedArea(
                counterClockwise), Is.EqualTo(20.0));
            Assert.That(GeometryMeasureAlgorithms.GetSignedProjectedArea(
                clockwise), Is.EqualTo(-20.0));
        });
    }

    [Test]
    public void LengthUsesPlanarSegmentsAndPolygonBoundaryRings()
    {
        Geometry line = Read("LINESTRING Z (0 0 100, 3 4 -100)");
        Geometry polygon = Read(
            "POLYGON ((0 0, 6 0, 6 4, 0 4, 0 0), (1 1, 3 1, 3 3, 1 3, 1 1))");

        Assert.Multiple(() =>
        {
            Assert.That(GeometryMeasureAlgorithms.GetLength(line),
                Is.EqualTo(5.0));
            Assert.That(GeometryMeasureAlgorithms.GetPerimeter(line),
                Is.Zero);
            Assert.That(GeometryMeasureAlgorithms.GetLength(polygon),
                Is.EqualTo(28.0));
            Assert.That(GeometryMeasureAlgorithms.GetPerimeter(polygon),
                Is.EqualTo(28.0));
        });
    }

    [Test]
    public void NestedCollectionsAggregateEachApplicableMeasure()
    {
        Geometry collection = Read(
            "GEOMETRYCOLLECTION (POINT (9 9), MULTILINESTRING ((0 0, 3 4), (0 0, 0 2)), POLYGON ((0 0, 6 0, 6 4, 0 4, 0 0), (1 1, 3 1, 3 3, 1 3, 1 1)), GEOMETRYCOLLECTION (POINT EMPTY, LINESTRING EMPTY))");

        Assert.Multiple(() =>
        {
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(collection),
                Is.EqualTo(20.0));
            Assert.That(GeometryMeasureAlgorithms.GetSignedProjectedArea(collection),
                Is.EqualTo(20.0));
            Assert.That(GeometryMeasureAlgorithms.GetLength(collection),
                Is.EqualTo(35.0));
            Assert.That(GeometryMeasureAlgorithms.GetPerimeter(collection),
                Is.EqualTo(28.0));
        });
    }

    [Test]
    public void CompensatedCollectionAreaPreservesSmallSignedContributions()
    {
        Geometry collection = Read(
            "GEOMETRYCOLLECTION (POLYGON ((0 0, 100000000 0, 100000000 100000000, 0 100000000, 0 0)), POLYGON ((200000000 0, 200000001 0, 200000001 1, 200000000 1, 200000000 0)), POLYGON ((0 0, 0 100000000, 100000000 100000000, 100000000 0, 0 0)))");

        Assert.Multiple(() =>
        {
            Assert.That(GeometryMeasureAlgorithms.GetSignedProjectedArea(
                collection), Is.EqualTo(1.0));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(
                collection), Is.EqualTo(20000000000000000.0));
        });
    }

    [Test]
    public void AreaAndPerimeterRemainStableAfterLargeTranslation()
    {
        Geometry translated = Read(
            "POLYGON ((1000000000000000 1000000000000000, 1000000000000003 1000000000000000, 1000000000000003 1000000000000004, 1000000000000000 1000000000000004, 1000000000000000 1000000000000000))");

        Assert.Multiple(() =>
        {
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(translated),
                Is.EqualTo(12.0));
            Assert.That(GeometryMeasureAlgorithms.GetSignedProjectedArea(translated),
                Is.EqualTo(12.0));
            Assert.That(GeometryMeasureAlgorithms.GetLength(translated),
                Is.EqualTo(14.0));
            Assert.That(GeometryMeasureAlgorithms.GetPerimeter(translated),
                Is.EqualTo(14.0));
        });
    }

    [Test]
    public void EmptyAndPointMeasuresAreZero()
    {
        Geometry point = Read("POINT (2 3)");
        Geometry empty = Read("MULTIPOLYGON EMPTY");

        Assert.Multiple(() =>
        {
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(point),
                Is.Zero);
            Assert.That(GeometryMeasureAlgorithms.GetSignedProjectedArea(point),
                Is.Zero);
            Assert.That(GeometryMeasureAlgorithms.GetLength(point), Is.Zero);
            Assert.That(GeometryMeasureAlgorithms.GetPerimeter(point), Is.Zero);
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(empty),
                Is.Zero);
            Assert.That(GeometryMeasureAlgorithms.GetLength(empty), Is.Zero);
            Assert.That(GeometryMeasureAlgorithms.GetPerimeter(empty), Is.Zero);
        });
    }

    [Test]
    public void LengthMeasuresRejectMalformedValuesAndMeasureSurfaces()
    {
        Geometry invalidLine = new Geometry
        {
            GeometryType = GeometryType.LineString,
            Point = null,
            LineString = new LineString
            {
                Coordinates = [new Coordinate { X = 0.0, Y = 0.0 }]
            }
        };
        Geometry triangle = Read(
            "TRIANGLE ((0 0, 2 0, 0 2, 0 0))");

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                GeometryMeasureAlgorithms.GetLength(invalidLine));
            Assert.Throws<InvalidGeometryException>(() =>
                GeometryMeasureAlgorithms.GetProjectedArea(invalidLine));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(
                triangle), Is.EqualTo(2.0));
            Assert.That(GeometryMeasureAlgorithms.GetLength(triangle),
                Is.EqualTo(4.0 + Math.Sqrt(8.0)).Within(1.0e-14));
            Assert.Throws<InvalidGeometryException>(() =>
                GeometryMeasureAlgorithms.GetPerimeter(null!));
        });
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
