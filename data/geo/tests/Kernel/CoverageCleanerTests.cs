using geo;

namespace geo.Tests;

public class CoverageCleanerTests
{
    [Test]
    public void CleaningReportsResolvedOverlapDiagnostics()
    {
        Geometry source = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0)), " +
            "POLYGON ((2 0, 6 0, 6 4, 2 4, 2 0)))");
        RepairParameters parameters = new RepairParameters();
        parameters.CoverageMergeStrategy =
            CoverageMergeStrategy.KeepLast;

        CoverageCleaningResult result =
            CoverageCleaner.CleanCoverage(source, parameters);

        Assert.Multiple(() =>
        {
            Assert.That(result.InputDiagnostic.IsValid, Is.False);
            Assert.That(result.InputDiagnostic.Error,
                Is.EqualTo(ValidationError.CoverageOverlap));
            Assert.That(result.InputDiagnostic.InvalidGeometry,
                Is.Not.Null);
            Assert.That(result.OutputDiagnostic.IsValid, Is.True);
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(
                result.Geometry), Is.EqualTo(24.0));
            Assert.That(LargestPolygonMinimumX(result.Geometry),
                Is.EqualTo(2.0));
        });
    }

    [Test]
    public void SnapToleranceClosesNearCoincidentBoundaries()
    {
        Geometry source = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0)), " +
            "POLYGON ((2.1 0, 4.1 0, 4.1 2, 2.1 2, 2.1 0)))");
        RepairParameters parameters = new RepairParameters();
        parameters.SnapTolerance = 0.2;

        CoverageCleaningResult result =
            CoverageCleaner.CleanCoverage(source, parameters);

        Assert.Multiple(() =>
        {
            Assert.That(result.InputDiagnostic.Error,
                Is.EqualTo(ValidationError.CoverageGap));
            Assert.That(result.OutputDiagnostic.IsValid, Is.True);
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(
                result.Geometry), Is.EqualTo(8.2).Within(1.0e-12));
            Assert.That(CoverageValidator.ValidateCoverage(
                result.Geometry, 0.2).IsValid, Is.True);
        });
    }

    [Test]
    public void GapWidthClosesEligibleGapButLeavesWiderGap()
    {
        Geometry source = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0)), " +
            "POLYGON ((2.1 0, 4.1 0, 4.1 2, 2.1 2, 2.1 0)), " +
            "POLYGON ((5 0, 7 0, 7 2, 5 2, 5 0)))");
        RepairParameters parameters = new RepairParameters();
        parameters.MaximumGapWidth = 0.2;

        CoverageCleaningResult result =
            CoverageCleaner.CleanCoverage(source, parameters);

        Assert.Multiple(() =>
        {
            Assert.That(result.OutputDiagnostic.IsValid, Is.True);
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(
                result.Geometry), Is.EqualTo(12.2).Within(1.0e-12));
            Assert.That(CoverageValidator.ValidateCoverage(
                result.Geometry, 0.2).IsValid, Is.True);
            Assert.That(CoverageValidator.ValidateCoverage(
                result.Geometry, 1.0).Error,
                Is.EqualTo(ValidationError.CoverageGap));
        });
    }

    [Test]
    public void ReconstructionNodesPartialSharedEdges()
    {
        Geometry source = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 2 0, 2 4, 0 4, 0 0)), " +
            "POLYGON ((2 0, 4 0, 4 2, 2 2, 2 0)))");

        CoverageCleaningResult result = CoverageCleaner.CleanCoverage(
            source, new RepairParameters());

        Assert.Multiple(() =>
        {
            Assert.That(result.InputDiagnostic.Error,
                Is.EqualTo(ValidationError.NonNodedIntersection));
            Assert.That(result.OutputDiagnostic.IsValid, Is.True);
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(
                result.Geometry), Is.EqualTo(12.0));
            Assert.That(HasCoordinate(result.Geometry, 2.0, 2.0),
                Is.True);
        });
    }

    [Test]
    public void CleaningPreservesMetadataAndDoesNotAliasInput()
    {
        Geometry source = ReadWithSrid(
            "SRID=3857;GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0)), " +
            "POLYGON ((2 0, 4 0, 4 2, 2 2, 2 0)))");

        CoverageCleaningResult result = CoverageCleaner.CleanCoverage(
            source, new RepairParameters());
        source.GeometryCollection!.Geometries[0]
            .Polygon!.Shell.Coordinates[0].X = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(result.Geometry.SpatialReferenceId,
                Is.EqualTo(3857));
            Assert.That(HasCoordinate(result.Geometry, 100.0, 0.0),
                Is.False);
            Assert.That(result.OutputDiagnostic.IsValid, Is.True);
        });
    }

    private static double LargestPolygonMinimumX(Geometry geometry)
    {
        Geometry[] leaves = ComponentWalker.FlattenLeafGeometries(
            geometry,
            GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        double largestArea = -1.0;
        double minimumX = double.NaN;
        int index = 0;
        while (index < leaves.Length)
        {
            if (leaves[index].GeometryType == GeometryType.Polygon)
            {
                double area = GeometryMeasureAlgorithms.GetProjectedArea(
                    leaves[index]);
                if (area > largestArea)
                {
                    largestArea = area;
                    Coordinate[] coordinates =
                        leaves[index].Polygon!.Shell.Coordinates;
                    minimumX = coordinates[0].X;
                    int coordinateIndex = 1;
                    while (coordinateIndex < coordinates.Length)
                    {
                        if (coordinates[coordinateIndex].X < minimumX)
                        {
                            minimumX = coordinates[coordinateIndex].X;
                        }
                        coordinateIndex++;
                    }
                }
            }
            index++;
        }
        return minimumX;
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
