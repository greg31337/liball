using geo;

namespace geo.Tests;

public class GeometryAssemblerTests
{
    [Test]
    public void LineMergeMethodsExposeDirectionPolicy()
    {
        IGeometryAssembler assembler = new GeometryAssembler();
        Geometry source = Read(
            "MULTILINESTRING ((0 0, 1 0), (2 0, 1 0))");

        Geometry undirected = assembler.LineMerge(source);
        Geometry directed = assembler.DirectedLineMerge(source);

        Assert.Multiple(() =>
        {
            Assert.That(undirected.GeometryType,
                Is.EqualTo(GeometryType.LineString));
            Assert.That(undirected.LineString!.Coordinates,
                Has.Length.EqualTo(3));
            Assert.That(directed.GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(directed.MultiLineString!.LineStrings,
                Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void PolygonizeAndFullResultExposeFacesAndDiagnostics()
    {
        IGeometryAssembler assembler = new GeometryAssembler();
        Geometry source = Read(
            "MULTILINESTRING ((0 0, 2 0), (2 0, 2 2), " +
            "(2 2, 0 2), (0 2, 0 0), (2 2, 3 3))");

        Geometry polygons = assembler.Polygonize(source);
        Geometry full = assembler.PolygonizeFull(source);
        Geometry[] members = full.GeometryCollection!.Geometries;

        Assert.Multiple(() =>
        {
            Assert.That(polygons.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(new GeometryMeasures().Area(polygons),
                Is.EqualTo(4.0));
            Assert.That(members, Has.Length.EqualTo(4));
            Assert.That(members[0].GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(members[1].MultiLineString!.LineStrings,
                Has.Length.EqualTo(1));
            Assert.That(GeometryInspection.IsEmptyValue(members[2]),
                Is.True);
            Assert.That(GeometryInspection.IsEmptyValue(members[3]),
                Is.True);
        });
    }

    [Test]
    public void BuildAreaPreservesNestedHoleSemantics()
    {
        IGeometryAssembler assembler = new GeometryAssembler();
        Geometry source = Read(
            "MULTILINESTRING ((0 0, 4 0, 4 4, 0 4, 0 0), " +
            "(1 1, 3 1, 3 3, 1 3, 1 1))");

        Geometry result = assembler.BuildArea(source);

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
    public void CoverageEdgesIncludeEveryShellAndHole()
    {
        IGeometryAssembler assembler = new GeometryAssembler();
        Geometry source = ReadWithSrid(
            "SRID=3857;MULTIPOLYGON (" +
            "((0 0, 4 0, 4 4, 0 4, 0 0), " +
            "(1 1, 1 2, 2 2, 2 1, 1 1)), " +
            "((10 0, 11 0, 11 1, 10 1, 10 0)))");

        Geometry result = assembler.ExtractCoverageEdges(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(result.MultiLineString!.LineStrings,
                Has.Length.EqualTo(3));
            Assert.That(result.SpatialReferenceId, Is.EqualTo(3857));
        });

        source.MultiPolygon!.Polygons[0].Shell.Coordinates[0].X = 99.0;
        Assert.That(result.MultiLineString!.LineStrings[0]
            .Coordinates[0].X, Is.EqualTo(0.0));
        Assert.Throws<InvalidGeometryException>(() =>
            assembler.ExtractCoverageEdges(Read("LINESTRING (0 0, 1 1)")));
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
