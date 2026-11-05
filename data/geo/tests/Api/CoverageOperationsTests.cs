using geo;

namespace geo.Tests;

public class CoverageOperationsTests
{
    [Test]
    public void ValidationReturnsCoverageDiagnostics()
    {
        ICoverageOperations operations = new CoverageOperations();
        Geometry overlap = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 3 0, 3 3, 0 3, 0 0)), " +
            "POLYGON ((2 0, 5 0, 5 3, 2 3, 2 0)))");
        Geometry gap = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0)), " +
            "POLYGON ((2.1 0, 4.1 0, 4.1 2, 2.1 2, 2.1 0)))");

        CoverageValidationResult overlapResult =
            operations.ValidatePolygonCoverage(overlap, 0.0);
        CoverageValidationResult gapResult =
            operations.ValidatePolygonCoverage(gap, 0.2);

        Assert.Multiple(() =>
        {
            Assert.That(overlapResult.Error,
                Is.EqualTo(ValidationError.CoverageOverlap));
            Assert.That(overlapResult.InvalidGeometry,
                Is.Not.Null);
            Assert.That(gapResult.Error,
                Is.EqualTo(ValidationError.CoverageGap));
            Assert.That(gapResult.InvalidGeometry!.GeometryType,
                Is.EqualTo(GeometryType.LineString));
        });
    }

    [Test]
    public void UnionDissolvesSharedCoverageEdges()
    {
        ICoverageOperations operations = new CoverageOperations();
        Geometry source = ReadWithSrid(
            "SRID=3857;GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0)), " +
            "POLYGON ((2 0, 4 0, 4 2, 2 2, 2 0)))");

        Geometry result = operations.UnionPolygonCoverage(source);
        source.GeometryCollection!.Geometries[0]
            .Polygon!.Shell.Coordinates[0].X = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(result.SpatialReferenceId, Is.EqualTo(3857));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(result),
                Is.EqualTo(8.0));
            Assert.That(HasCoordinate(result, 100.0, 0.0), Is.False);
        });
    }

    [Test]
    public void SimplificationUsesOneSharedEdgeForEveryPolygon()
    {
        ICoverageOperations operations = new CoverageOperations();
        Geometry source = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 2 0, 2.1 1, 1.9 2, " +
            "2.1 3, 2 4, 0 4, 0 0)), " +
            "POLYGON ((2 0, 4 0, 4 4, 2 4, " +
            "2.1 3, 1.9 2, 2.1 1, 2 0)))");
        SimplifyParameters parameters = new SimplifyParameters();
        parameters.Algorithm =
            SimplificationAlgorithm.VisvalingamWhyatt;
        parameters.Tolerance = 1.0;

        Geometry result = operations.SimplifyPolygonCoverage(
            source, parameters);

        Assert.Multiple(() =>
        {
            Assert.That(CoverageValidator.ValidateCoverage(
                result, 0.0).IsValid, Is.True);
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(result),
                Is.EqualTo(16.0).Within(1.0e-12));
            Assert.That(GeometryInspection.ExtractValidatedCoordinates(
                result).Length,
                Is.LessThan(GeometryInspection.ExtractValidatedCoordinates(
                    source).Length));
        });
    }

    [Test]
    public void CleaningAppliesGapAndOwnershipPolicies()
    {
        ICoverageOperations operations = new CoverageOperations();
        Geometry source = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 3 0, 3 2, 0 2, 0 0)), " +
            "POLYGON ((2.9 0, 5 0, 5 2, 2.9 2, 2.9 0)), " +
            "POLYGON ((5.1 0, 7.1 0, 7.1 2, 5.1 2, 5.1 0)))");
        RepairParameters parameters = new RepairParameters();
        parameters.CoverageMergeStrategy =
            CoverageMergeStrategy.KeepLast;
        parameters.MaximumGapWidth = 0.2;

        Geometry result = operations.CleanPolygonCoverage(
            source, parameters);

        Assert.Multiple(() =>
        {
            Assert.That(CoverageValidator.ValidateCoverage(
                result, 0.2).IsValid, Is.True);
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(result),
                Is.EqualTo(14.2).Within(1.0e-12));
        });
    }

    [Test]
    public void EdgeExtractionIncludesShellsAndHolesAsOwningLines()
    {
        ICoverageOperations operations = new CoverageOperations();
        Geometry source = ReadWithSrid(
            "SRID=4326;MULTIPOLYGON (" +
            "((0 0, 4 0, 4 4, 0 4, 0 0), " +
            "(1 1, 1 2, 2 2, 2 1, 1 1)), " +
            "((10 0, 11 0, 11 1, 10 1, 10 0)))");

        Geometry result =
            operations.ExtractPolygonCoverageEdges(source);
        source.MultiPolygon!.Polygons[0]
            .Shell.Coordinates[0].X = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(result.MultiLineString!.LineStrings,
                Has.Length.EqualTo(3));
            Assert.That(result.SpatialReferenceId, Is.EqualTo(4326));
            Assert.That(result.MultiLineString.LineStrings[0]
                .Coordinates[0].X, Is.EqualTo(0.0));
        });
    }

    [Test]
    public void InvalidInputsRetainKernelFailurePolicies()
    {
        ICoverageOperations operations = new CoverageOperations();
        Geometry line = Read("LINESTRING (0 0, 1 1)");
        Geometry overlap = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 3 0, 3 3, 0 3, 0 0)), " +
            "POLYGON ((2 0, 5 0, 5 3, 2 3, 2 0)))");
        SimplifyParameters wrong = new SimplifyParameters();

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                operations.ValidatePolygonCoverage(line, -1.0));
            Assert.Throws<InvalidGeometryException>(() =>
                operations.UnionPolygonCoverage(overlap));
            Assert.Throws<InvalidGeometryException>(() =>
                operations.SimplifyPolygonCoverage(
                    Read("POLYGON ((0 0, 2 0, 0 2, 0 0))"), wrong));
            Assert.Throws<InvalidGeometryException>(() =>
                operations.CleanPolygonCoverage(
                    line, new RepairParameters()));
            Assert.Throws<InvalidGeometryException>(() =>
                operations.ExtractPolygonCoverageEdges(line));
        });
    }

    private static bool HasCoordinate(
        Geometry geometry, double x, double y)
    {
        Coordinate[] coordinates =
            GeometryInspection.ExtractValidatedCoordinates(geometry);
        int index = 0;
        while (index < coordinates.Length)
        {
            if (coordinates[index].X == x && coordinates[index].Y == y)
            {
                return true;
            }
            index++;
        }
        return false;
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
