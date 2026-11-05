using geo;

namespace geo.Tests;

public class PolygonizerTests
{
    [Test]
    public void ClosedLineworkProducesPolygonAndEmptyDiagnostics()
    {
        Geometry source = ReadWithSrid(
            "SRID=3857;LINESTRING Z (0 0 7, 4 0 7, " +
            "4 3 7, 0 3 7, 0 0 7)");

        PolygonizationResult result =
            Polygonizer.GetPolygonization(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Polygons.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(new GeometryMeasures().Area(result.Polygons),
                Is.EqualTo(12.0));
            Assert.That(result.Polygons.SpatialReferenceId,
                Is.EqualTo(3857));
            Assert.That(result.Polygons.CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XYZ));
            Assert.That(GeometryInspection.IsEmptyValue(result.Dangles),
                Is.True);
            Assert.That(GeometryInspection.IsEmptyValue(result.CutEdges),
                Is.True);
            Assert.That(GeometryInspection.IsEmptyValue(
                result.InvalidRings), Is.True);
        });

        source.LineString!.Coordinates[0].X = 99.0;
        Assert.That(result.Polygons.Polygon!.Shell.Coordinates[0].X,
            Is.EqualTo(0.0));
    }

    [Test]
    public void FullResultClassifiesDanglesInDocumentedOrder()
    {
        Geometry source = Read(
            "MULTILINESTRING ((0 0, 2 0), (2 0, 2 2), " +
            "(2 2, 0 2), (0 2, 0 0), (2 2, 3 3), (3 3, 4 3))");

        Geometry full = Polygonizer.PolygonizeFullGeometry(source);
        Geometry[] members = full.GeometryCollection!.Geometries;

        Assert.Multiple(() =>
        {
            Assert.That(members, Has.Length.EqualTo(4));
            Assert.That(members[0].GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(new GeometryMeasures().Area(members[0]),
                Is.EqualTo(4.0));
            Assert.That(members[1].GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(members[1].MultiLineString!.LineStrings,
                Has.Length.EqualTo(2));
            Assert.That(GeometryInspection.IsEmptyValue(members[2]),
                Is.True);
            Assert.That(GeometryInspection.IsEmptyValue(members[3]),
                Is.True);
        });
    }

    [Test]
    public void BridgeBetweenRingsIsReportedAsCutEdge()
    {
        Geometry source = Read(
            "MULTILINESTRING (" +
            "(0 0, 1 0), (1 0, 1 1), (1 1, 0 1), (0 1, 0 0), " +
            "(3 0, 4 0), (4 0, 4 1), (4 1, 3 1), (3 1, 3 0), " +
            "(1 0, 3 0))");

        PolygonizationResult result =
            Polygonizer.GetPolygonization(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Polygons.GeometryType,
                Is.EqualTo(GeometryType.MultiPolygon));
            Assert.That(result.Polygons.MultiPolygon!.Polygons,
                Has.Length.EqualTo(2));
            Assert.That(GeometryInspection.IsEmptyValue(result.Dangles),
                Is.True);
            Assert.That(result.CutEdges.MultiLineString!.LineStrings,
                Has.Length.EqualTo(1));
            Assert.That(GeometryInspection.IsEmptyValue(
                result.InvalidRings), Is.True);
        });
    }

    [Test]
    public void SelfCrossingRingIsReportedOnceAsInvalid()
    {
        Geometry source = Read(
            "LINESTRING (0 0, 2 2, 0 2, 2 0, 0 0)");

        PolygonizationResult result =
            Polygonizer.GetPolygonization(source);

        Assert.Multiple(() =>
        {
            Assert.That(GeometryInspection.IsEmptyValue(result.Polygons),
                Is.True);
            Assert.That(GeometryInspection.IsEmptyValue(result.Dangles),
                Is.True);
            Assert.That(GeometryInspection.IsEmptyValue(result.CutEdges),
                Is.True);
            Assert.That(result.InvalidRings.MultiLineString!.LineStrings,
                Has.Length.EqualTo(1));
            Assert.That(result.InvalidRings.MultiLineString.LineStrings[0]
                .Coordinates, Has.Length.EqualTo(5));
        });
    }

    [Test]
    public void NestedRingsCreateEveryBoundedFace()
    {
        Geometry source = Read(
            "MULTILINESTRING ((0 0, 4 0, 4 4, 0 4, 0 0), " +
            "(1 1, 3 1, 3 3, 1 3, 1 1))");

        Geometry result = Polygonizer.PolygonizeGeometry(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.MultiPolygon));
            Assert.That(result.MultiPolygon!.Polygons,
                Has.Length.EqualTo(2));
            Assert.That(result.MultiPolygon.Polygons[0].Holes.Length +
                result.MultiPolygon.Polygons[1].Holes.Length,
                Is.EqualTo(1));
            Assert.That(new GeometryMeasures().Area(result),
                Is.EqualTo(16.0));
        });
    }

    [Test]
    public void PointOnlyInputProducesTypedEmptyOutputs()
    {
        PolygonizationResult result = Polygonizer.GetPolygonization(
            Read("GEOMETRYCOLLECTION (POINT (1 2), LINESTRING EMPTY)"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Polygons.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(result.Dangles.GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(result.CutEdges.GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(result.InvalidRings.GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(GeometryInspection.IsEmptyValue(result.Polygons),
                Is.True);
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
