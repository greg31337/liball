using geo;

namespace geo.Tests;

public class CoverageUnionBuilderTests
{
    [Test]
    public void SharedEdgeCancellationRetainsOnlyExteriorSegments()
    {
        Geometry source = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0)), " +
            "POLYGON ((2 0, 4 0, 4 2, 2 2, 2 0)))");
        GeometryRepairComponents components =
            GeometryRepairEngine.CollectRepairComponents(source);

        Coordinate[][] boundary = CoverageUnionBuilder.CancelSharedEdges(
            components.Polygons);

        bool containsSharedEdge = false;
        int index = 0;
        while (index < boundary.Length)
        {
            if (boundary[index][0].X == 2.0 &&
                boundary[index][1].X == 2.0)
            {
                containsSharedEdge = true;
            }
            index++;
        }
        Assert.Multiple(() =>
        {
            Assert.That(boundary, Has.Length.EqualTo(6));
            Assert.That(containsSharedEdge, Is.False);
        });
    }

    [Test]
    public void AdjacentPolygonsReconstructAsOneOwningPolygon()
    {
        Geometry source = ReadWithSrid(
            "SRID=3857;GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0)), " +
            "POLYGON ((2 0, 4 0, 4 2, 2 2, 2 0)))");

        Geometry result = CoverageUnionBuilder.UnionCoverage(source);
        source.GeometryCollection!.Geometries[0]
            .Polygon!.Shell.Coordinates[0].X = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(result.SpatialReferenceId, Is.EqualTo(3857));
            Assert.That(result.Polygon!.Holes, Is.Empty);
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(result),
                Is.EqualTo(8.0));
            Assert.That(result.Polygon.Shell.Coordinates[0].X,
                Is.Not.EqualTo(100.0));
            Assert.That(GeometryValidityChecker.ValidateGeometry(result)
                .IsValid, Is.True);
        });
    }

    [Test]
    public void FourPolygonMosaicDropsEveryInternalEdge()
    {
        Geometry source = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0)), " +
            "POLYGON ((2 0, 4 0, 4 2, 2 2, 2 0)), " +
            "POLYGON ((0 2, 2 2, 2 4, 0 4, 0 2)), " +
            "POLYGON ((2 2, 4 2, 4 4, 2 4, 2 2)))");

        Geometry result = CoverageUnionBuilder.UnionCoverage(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(result),
                Is.EqualTo(16.0));
            Assert.That(result.Polygon!.Holes, Is.Empty);
        });
    }

    [Test]
    public void PolygonFillingCoverageHoleRemovesTheHoleBoundary()
    {
        Geometry source = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0), " +
            "(1 1, 1 3, 3 3, 3 1, 1 1)), " +
            "POLYGON ((1 1, 3 1, 3 3, 1 3, 1 1)))");

        Geometry result = CoverageUnionBuilder.UnionCoverage(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(result.Polygon!.Holes, Is.Empty);
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(result),
                Is.EqualTo(16.0));
        });
    }

    [Test]
    public void DisconnectedPolygonsAndRetainedHolesAreReconstructed()
    {
        Geometry source = Read(
            "MULTIPOLYGON (" +
            "((0 0, 4 0, 4 4, 0 4, 0 0), " +
            "(1 1, 1 2, 2 2, 2 1, 1 1)), " +
            "((10 0, 12 0, 12 2, 10 2, 10 0)))");

        Geometry result = CoverageUnionBuilder.UnionCoverage(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.MultiPolygon));
            Assert.That(result.MultiPolygon!.Polygons,
                Has.Length.EqualTo(2));
            Assert.That(result.MultiPolygon.Polygons[0].Holes,
                Has.Length.EqualTo(1));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(result),
                Is.EqualTo(19.0));
        });
    }

    [Test]
    public void EmptyCoverageProducesMetadataPreservingEmptyPolygon()
    {
        Geometry source = Read("MULTIPOLYGON Z EMPTY");

        Geometry result = CoverageUnionBuilder.UnionCoverage(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(result.CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XYZ));
            Assert.That(GeometryInspection.IsEmptyValue(result), Is.True);
        });
    }

    [Test]
    public void InvalidCoverageInputsAreRejectedBeforeReconstruction()
    {
        Geometry overlap = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 3 0, 3 3, 0 3, 0 0)), " +
            "POLYGON ((2 0, 5 0, 5 3, 2 3, 2 0)))");
        Geometry nonNoded = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 2 0, 2 4, 0 4, 0 0)), " +
            "POLYGON ((2 0, 4 0, 4 2, 2 2, 2 0)))");

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                CoverageUnionBuilder.UnionCoverage(overlap));
            Assert.Throws<InvalidGeometryException>(() =>
                CoverageUnionBuilder.UnionCoverage(nonNoded));
            Assert.Throws<InvalidGeometryException>(() =>
                CoverageUnionBuilder.UnionCoverage(
                    Read("LINESTRING (0 0, 1 1)")));
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
