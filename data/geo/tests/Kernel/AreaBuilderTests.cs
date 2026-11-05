using geo;

namespace geo.Tests;

public class AreaBuilderTests
{
    [Test]
    public void NestedRingsUseOddEvenContainmentForHoles()
    {
        Geometry source = Read(
            "MULTILINESTRING ((0 0, 4 0, 4 4, 0 4, 0 0), " +
            "(1 1, 3 1, 3 3, 1 3, 1 1))");

        Geometry result = AreaBuilder.BuildGeometry(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(result.Polygon!.Holes, Has.Length.EqualTo(1));
            Assert.That(new GeometryMeasures().Area(result),
                Is.EqualTo(12.0));
        });
    }

    [Test]
    public void AdjacentFacesAreDissolvedIntoOneArea()
    {
        Geometry source = Read(
            "MULTILINESTRING ((0 0, 1 0), (1 0, 2 0), " +
            "(2 0, 2 1), (2 1, 1 1), (1 1, 0 1), " +
            "(0 1, 0 0), (1 0, 1 1))");

        Geometry polygons = Polygonizer.PolygonizeGeometry(source);
        Geometry result = AreaBuilder.BuildGeometry(source);

        Assert.Multiple(() =>
        {
            Assert.That(polygons.GeometryType,
                Is.EqualTo(GeometryType.MultiPolygon));
            Assert.That(polygons.MultiPolygon!.Polygons,
                Has.Length.EqualTo(2));
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(result.Polygon!.Holes, Is.Empty);
            Assert.That(new GeometryMeasures().Area(result),
                Is.EqualTo(2.0));
            Assert.That(result.Polygon.Shell.Coordinates,
                Has.Length.EqualTo(7));
        });
    }

    [Test]
    public void PolygonBoundaryInputPreservesItsHoleAndMetadata()
    {
        Geometry source = ReadWithSrid(
            "SRID=4326;POLYGON ((0 0, 5 0, 5 5, 0 5, 0 0), " +
            "(1 1, 1 2, 2 2, 2 1, 1 1))");

        Geometry result = AreaBuilder.BuildGeometry(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(result.Polygon!.Holes, Has.Length.EqualTo(1));
            Assert.That(new GeometryMeasures().Area(result),
                Is.EqualTo(24.0));
            Assert.That(result.SpatialReferenceId, Is.EqualTo(4326));
        });
    }

    [Test]
    public void DanglesDoNotContributeArea()
    {
        Geometry source = Read(
            "MULTILINESTRING ((0 0, 2 0), (2 0, 2 2), " +
            "(2 2, 0 2), (0 2, 0 0), (2 2, 5 2))");

        Geometry result = AreaBuilder.BuildGeometry(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(new GeometryMeasures().Area(result),
                Is.EqualTo(4.0));
        });
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
