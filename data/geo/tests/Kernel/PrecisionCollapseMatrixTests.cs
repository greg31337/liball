using geo;

namespace geo.Tests;

public class PrecisionCollapseMatrixTests
{
    [Test]
    public void NestedCollapseReportPreservesDepthAndSourceOrder()
    {
        Geometry source = Read(
            "GEOMETRYCOLLECTION (" +
            "LINESTRING (0.1 0.1, 0.4 0.4), " +
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((2.1 0.1, 2.4 0.1, 2.4 0.4, 2.1 0.4, 2.1 0.1)), " +
            "POLYGON ((4.1 0.1, 10.1 0.1, 10.1 6.1, 4.1 6.1, 4.1 0.1), " +
            "(5.1 1.1, 5.4 1.1, 5.4 1.4, 5.1 1.4, 5.1 1.1))), " +
            "POINT (20.2 20.2))");

        PrecisionReductionResult result =
            TopologyPreservingPrecisionReducer.ReduceGeometry(
                source, FixedPrecision(1.0, 0.0, 0.0));

        Assert.Multiple(() =>
        {
            Assert.That(result.CollapsedComponentCount, Is.EqualTo(3));
            Assert.That(result.CollapsedComponentPaths, Is.EqualTo(new[]
            {
                "GeometryCollection/Geometries[0]/LineString",
                "GeometryCollection/Geometries[1]/GeometryCollection/" +
                    "Geometries[0]/Polygon/Shell",
                "GeometryCollection/Geometries[1]/GeometryCollection/" +
                    "Geometries[1]/Polygon/Holes[0]"
            }));
            Assert.That(GeometryValidityChecker.ValidateGeometry(
                result.Geometry).IsValid, Is.True);
            Assert.That(result.Geometry.GeometryType,
                Is.EqualTo(GeometryType.GeometryCollection));
        });
    }

    [Test]
    public void FullyCollapsedMixedInputUsesHighestNominalEmptyType()
    {
        Geometry source = Read(
            "GEOMETRYCOLLECTION (" +
            "LINESTRING (0.1 0.1, 0.4 0.4), " +
            "POLYGON ((2.1 0.1, 2.4 0.1, 2.4 0.4, 2.1 0.4, 2.1 0.1)))");

        PrecisionReductionResult result =
            TopologyPreservingPrecisionReducer.ReduceGeometry(
                source, FixedPrecision(1.0, 0.0, 0.0));

        Assert.Multiple(() =>
        {
            Assert.That(result.CollapsedComponentCount, Is.EqualTo(2));
            Assert.That(result.Geometry.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(GeometryInspection.IsEmptyValue(result.Geometry),
                Is.True);
            Assert.That(GeometryValidityChecker.ValidateGeometry(
                result.Geometry).IsValid, Is.True);
        });
    }

    [Test]
    public void GridOffsetsApplyToEveryReconstructedCoordinate()
    {
        Geometry source = ReadWithSrid(
            "SRID=4326;POLYGON ((0.26 0.26, 2.74 0.26, " +
            "2.74 2.74, 0.26 2.74, 0.26 0.26))");
        PrecisionModel precision = FixedPrecision(2.0, 0.25, 0.25);

        PrecisionReductionResult result =
            TopologyPreservingPrecisionReducer.ReduceGeometry(
                source, precision);
        Coordinate[] coordinates =
            GeometryInspection.ExtractValidatedCoordinates(result.Geometry);

        Assert.Multiple(() =>
        {
            Assert.That(coordinates,
                Has.All.Matches<Coordinate>(coordinate =>
                    (coordinate.X - 0.25) * 2.0 ==
                        System.Math.Round((coordinate.X - 0.25) * 2.0) &&
                    (coordinate.Y - 0.25) * 2.0 ==
                        System.Math.Round((coordinate.Y - 0.25) * 2.0)));
            Assert.That(result.Geometry.SpatialReferenceId,
                Is.EqualTo(4326));
            Assert.That(result.CollapsedComponentCount, Is.Zero);
        });
    }

    [Test]
    public void OneCollapsedHoleDoesNotDiscardSurvivingHoles()
    {
        Geometry source = Read(
            "POLYGON ((0.1 0.1, 10.1 0.1, 10.1 10.1, 0.1 10.1, 0.1 0.1), " +
            "(1.1 1.1, 1.4 1.1, 1.4 1.4, 1.1 1.4, 1.1 1.1), " +
            "(4.1 4.1, 4.1 6.1, 6.1 6.1, 6.1 4.1, 4.1 4.1))");

        PrecisionReductionResult result =
            TopologyPreservingPrecisionReducer.ReduceGeometry(
                source, FixedPrecision(1.0, 0.0, 0.0));

        Assert.Multiple(() =>
        {
            Assert.That(result.CollapsedComponentCount, Is.EqualTo(1));
            Assert.That(result.CollapsedComponentPaths,
                Is.EqualTo(new[] { "Polygon/Holes[0]" }));
            Assert.That(result.Geometry.Polygon!.Holes,
                Has.Length.EqualTo(1));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(
                result.Geometry), Is.EqualTo(96.0));
        });
    }

    [Test]
    public void GridContactMergesPolygonComponentsWithoutReportingCollapse()
    {
        Geometry source = Read(
            "MULTIPOLYGON (" +
            "((0.1 0.1, 1.6 0.1, 1.6 2.1, 0.1 2.1, 0.1 0.1)), " +
            "((1.7 0.1, 3.1 0.1, 3.1 2.1, 1.7 2.1, 1.7 0.1)))");

        PrecisionReductionResult result =
            TopologyPreservingPrecisionReducer.ReduceGeometry(
                source, FixedPrecision(1.0, 0.0, 0.0));

        Assert.Multiple(() =>
        {
            Assert.That(result.CollapsedComponentCount, Is.Zero);
            Assert.That(result.Geometry.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(
                result.Geometry), Is.EqualTo(6.0));
            Assert.That(GeometryValidityChecker.ValidateGeometry(
                result.Geometry).IsValid, Is.True);
        });
    }

    [Test]
    public void ReducingAResultAgainIsTopologicallyIdempotent()
    {
        PrecisionModel precision = FixedPrecision(1.0, 0.0, 0.0);
        PrecisionReductionResult first =
            TopologyPreservingPrecisionReducer.ReduceGeometry(
                Read(
                    "POLYGON ((0.1 0.1, 4.1 0.1, 4.1 4.1, " +
                    "2.4 4.1, 2.4 1.4, 1.6 1.4, 1.6 4.1, " +
                    "0.1 4.1, 0.1 0.1))"),
                precision);

        PrecisionReductionResult second =
            TopologyPreservingPrecisionReducer.ReduceGeometry(
                first.Geometry, precision);

        Assert.Multiple(() =>
        {
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                first.Geometry, second.Geometry), Is.True);
            Assert.That(second.CollapsedComponentCount, Is.Zero);
            Assert.That(second.CollapsedComponentPaths, Is.Empty);
        });
    }

    private static PrecisionModel FixedPrecision(
        double scale, double offsetX, double offsetY)
    {
        return new PrecisionModel
        {
            Type = PrecisionModelType.Fixed,
            Scale = scale,
            OffsetX = offsetX,
            OffsetY = offsetY
        };
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
