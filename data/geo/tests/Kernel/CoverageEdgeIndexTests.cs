using geo;

namespace geo.Tests;

public class CoverageEdgeIndexTests
{
    [Test]
    public void BoundaryExtractionIncludesShellsHolesAndPolygonIdentity()
    {
        Geometry source = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 6 0, 6 6, 0 6, 0 0), " +
            "(2 2, 2 4, 4 4, 4 2, 2 2)), " +
            "POLYGON ((10 0, 12 0, 12 2, 10 2, 10 0)))");
        GeometryRepairComponents components =
            GeometryRepairEngine.CollectRepairComponents(source);

        SegmentString[] edges =
            CoverageEdgeIndex.ExtractBoundarySegmentStrings(
                components.Polygons);

        Assert.Multiple(() =>
        {
            Assert.That(edges, Has.Length.EqualTo(3));
            Assert.That(edges[0].SourceIndex, Is.Zero);
            Assert.That(edges[1].SourceIndex, Is.Zero);
            Assert.That(edges[2].SourceIndex, Is.EqualTo(1));
            Assert.That(edges[0].Coordinates, Has.Length.EqualTo(5));
            Assert.That(edges[1].Coordinates, Has.Length.EqualTo(5));
            Assert.That(edges[2].Coordinates, Has.Length.EqualTo(5));
        });
    }

    [Test]
    public void PolygonCandidateIndexHonorsMaximumDistance()
    {
        Geometry source = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0)), " +
            "POLYGON ((3 0, 5 0, 5 2, 3 2, 3 0)), " +
            "POLYGON ((20 0, 22 0, 22 2, 20 2, 20 0)))");
        GeometryRepairComponents components =
            GeometryRepairEngine.CollectRepairComponents(source);

        CoveragePolygonPair[] below =
            CoverageEdgeIndex.FindCandidatePairs(
                components.Polygons, 0.9);
        CoveragePolygonPair[] inclusive =
            CoverageEdgeIndex.FindCandidatePairs(
                components.Polygons, 1.0);

        Assert.Multiple(() =>
        {
            Assert.That(below, Is.Empty);
            Assert.That(inclusive, Has.Length.EqualTo(1));
            Assert.That(inclusive[0].FirstIndex, Is.Zero);
            Assert.That(inclusive[0].SecondIndex, Is.EqualTo(1));
        });
    }

    [Test]
    public void CoverageValidationReportsPointTJunction()
    {
        Geometry source = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0)), " +
            "POLYGON ((4 2, 6 1, 6 3, 4 2)))");

        CoverageValidationResult result =
            CoverageValidator.ValidateCoverage(source, 0.0);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Error,
                Is.EqualTo(ValidationError.NonNodedIntersection));
            Assert.That(result.ComponentPath,
                Is.EqualTo("Coverage/Polygons[1]/NonNodedWith[0]"));
            Assert.That(result.Coordinate, Is.Not.Null);
            AssertPlanar(result.Coordinate!, 4.0, 2.0);
            Assert.That(result.InvalidGeometry!.GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(result.InvalidGeometry.MultiLineString!.LineStrings,
                Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void CoverageValidationReportsPartialSharedEdgeEndpoint()
    {
        Geometry source = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 2 0, 2 4, 0 4, 0 0)), " +
            "POLYGON ((2 0, 4 0, 4 2, 2 2, 2 0)))");

        CoverageValidationResult result =
            CoverageValidator.ValidateCoverage(source, 0.0);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Error,
                Is.EqualTo(ValidationError.NonNodedIntersection));
            Assert.That(result.Coordinate, Is.Not.Null);
            AssertPlanar(result.Coordinate!, 2.0, 2.0);
            Assert.That(result.InvalidGeometry!.CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XY));
        });
    }

    [Test]
    public void MatchingSharedEdgeNodesFormValidCoverage()
    {
        Geometry source = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 2 0, 2 2, 2 4, 0 4, 0 0)), " +
            "POLYGON ((2 0, 4 0, 4 2, 2 2, 2 0)), " +
            "POLYGON ((2 2, 4 2, 4 4, 2 4, 2 2)))");

        CoverageValidationResult result =
            CoverageValidator.ValidateCoverage(source, 0.0);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Error, Is.EqualTo(ValidationError.None));
            Assert.That(result.InvalidGeometry, Is.Null);
        });
    }

    [Test]
    public void IndexedValidationFindsOverlapAndBoundedGap()
    {
        Geometry overlap = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((-20 0, -18 0, -18 2, -20 2, -20 0)), " +
            "POLYGON ((0 0, 3 0, 3 3, 0 3, 0 0)), " +
            "POLYGON ((2 1, 5 1, 5 4, 2 4, 2 1)), " +
            "POLYGON ((20 0, 22 0, 22 2, 20 2, 20 0)))");
        Geometry gap = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((-20 0, -18 0, -18 2, -20 2, -20 0)), " +
            "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0)), " +
            "POLYGON ((2.25 0, 4.25 0, 4.25 2, 2.25 2, 2.25 0)), " +
            "POLYGON ((20 0, 22 0, 22 2, 20 2, 20 0)))");

        CoverageValidationResult overlapResult =
            CoverageValidator.ValidateCoverage(overlap, 0.0);
        CoverageValidationResult narrowLimit =
            CoverageValidator.ValidateCoverage(gap, 0.2);
        CoverageValidationResult inclusiveLimit =
            CoverageValidator.ValidateCoverage(gap, 0.25);

        Assert.Multiple(() =>
        {
            Assert.That(overlapResult.Error,
                Is.EqualTo(ValidationError.CoverageOverlap));
            Assert.That(overlapResult.ComponentPath,
                Is.EqualTo("Coverage/Polygons[2]/OverlapWith[1]"));
            Assert.That(narrowLimit.IsValid, Is.True);
            Assert.That(inclusiveLimit.Error,
                Is.EqualTo(ValidationError.CoverageGap));
            Assert.That(inclusiveLimit.ComponentPath,
                Is.EqualTo("Coverage/Polygons[2]/GapWith[1]"));
            Assert.That(inclusiveLimit.InvalidGeometry!.GeometryType,
                Is.EqualTo(GeometryType.LineString));
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
