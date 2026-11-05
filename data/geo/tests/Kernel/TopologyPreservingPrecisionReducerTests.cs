using geo;

namespace geo.Tests;

public class TopologyPreservingPrecisionReducerTests
{
    [Test]
    public void PrecisionReductionReconstructsPolygonTopologyAfterRounding()
    {
        Geometry source = Read(
            "POLYGON ((0.1 0.1, 4.1 0.1, 4.1 4.1, 2.4 4.1, " +
            "2.4 1.4, 1.6 1.4, 1.6 4.1, 0.1 4.1, 0.1 0.1))");
        PrecisionModel precision = FixedPrecision(1.0, 0.0, 0.0);
        Geometry coordinateRounded = OverlayPrecisionReducer.ReduceInput(
            new RelationGeometryParts(source), precision);

        PrecisionReductionResult result =
            TopologyPreservingPrecisionReducer.ReduceGeometry(
                source, precision);
        ValidationResult validation =
            GeometryValidityChecker.ValidateGeometry(result.Geometry);

        Assert.Multiple(() =>
        {
            Assert.That(
                GeometryValidityChecker.ValidateGeometry(
                    coordinateRounded).IsValid,
                Is.False);
            Assert.That(validation.IsValid, Is.True);
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                result.Geometry,
                Read("POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))")),
                Is.True);
            Assert.That(result.CollapsedComponentCount, Is.Zero);
            Assert.That(result.CollapsedComponentPaths, Is.Empty);
        });
    }

    [Test]
    public void PrecisionReductionReportsCollapsedCurvesAndHolesByPath()
    {
        Geometry source = Read(
            "GEOMETRYCOLLECTION (" +
            "LINESTRING (0.1 0.1, 0.4 0.4), " +
            "POLYGON ((0.1 0.1, 4.1 0.1, 4.1 4.1, 0.1 4.1, 0.1 0.1), " +
            "(1.1 1.1, 1.4 1.1, 1.4 1.4, 1.1 1.4, 1.1 1.1)))");

        PrecisionReductionResult result =
            TopologyPreservingPrecisionReducer.ReduceGeometry(
                source, FixedPrecision(1.0, 0.0, 0.0));

        Assert.Multiple(() =>
        {
            Assert.That(result.CollapsedComponentCount, Is.EqualTo(2));
            Assert.That(result.CollapsedComponentPaths, Is.EqualTo(new[]
            {
                "GeometryCollection/Geometries[0]/LineString",
                "GeometryCollection/Geometries[1]/Polygon/Holes[0]"
            }));
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                result.Geometry,
                Read("POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))")),
                Is.True);
            Assert.That(
                GeometryValidityChecker.ValidateGeometry(
                    result.Geometry).IsValid,
                Is.True);
        });
    }

    [Test]
    public void PrecisionReductionReportsCollapsedPolygonShell()
    {
        Geometry source = Read(
            "MULTIPOLYGON (" +
            "((0.1 0.1, 0.4 0.1, 0.4 0.4, 0.1 0.4, 0.1 0.1)), " +
            "((2.1 2.1, 4.1 2.1, 4.1 4.1, 2.1 4.1, 2.1 2.1)))");

        PrecisionReductionResult result =
            TopologyPreservingPrecisionReducer.ReduceGeometry(
                source, FixedPrecision(1.0, 0.0, 0.0));

        Assert.Multiple(() =>
        {
            Assert.That(result.CollapsedComponentCount, Is.EqualTo(1));
            Assert.That(result.CollapsedComponentPaths,
                Is.EqualTo(new[] { "MultiPolygon/Polygons[0]/Shell" }));
            Assert.That(result.Geometry.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                result.Geometry,
                Read("POLYGON ((2 2, 4 2, 4 4, 2 4, 2 2))")),
                Is.True);
        });
    }

    [Test]
    public void PrecisionReductionPreservesMetadataAndDoesNotMutateInput()
    {
        Geometry source = ReadWithSrid(
            "SRID=4979;POLYGON ZM ((0.1 0.1 1 10, 2.1 0.1 2 20, " +
            "2.1 2.1 3 30, 0.1 2.1 4 40, 0.1 0.1 1 10))");
        Coordinate original = source.Polygon!.Shell.Coordinates[0];

        PrecisionReductionResult result =
            TopologyPreservingPrecisionReducer.ReduceGeometry(
                source, FixedPrecision(1.0, 0.0, 0.0));
        Coordinate[] reducedCoordinates =
            GeometryInspection.ExtractValidatedCoordinates(result.Geometry);

        Assert.Multiple(() =>
        {
            Assert.That(result.Geometry.CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(result.Geometry.SpatialReferenceId,
                Is.EqualTo(4979));
            Assert.That(reducedCoordinates,
                Has.All.Matches<Coordinate>(coordinate =>
                    coordinate.X == System.Math.Round(coordinate.X) &&
                    coordinate.Y == System.Math.Round(coordinate.Y) &&
                    double.IsFinite(coordinate.Z) &&
                    double.IsFinite(coordinate.M)));
            Assert.That(original.X, Is.EqualTo(0.1));
            Assert.That(original.Y, Is.EqualTo(0.1));
        });
    }

    [Test]
    public void PrecisionReductionRejectsNonFixedPrecisionAndInvalidInput()
    {
        Geometry valid = Read("LINESTRING (0 0, 1 1)");
        Geometry invalid = Read(
            "POLYGON ((0 0, 2 2, 0 2, 2 0, 0 0))");

        Assert.Multiple(() =>
        {
            Assert.Throws<PrecisionException>(() =>
                TopologyPreservingPrecisionReducer.ReduceGeometry(
                    valid, new PrecisionModel()));
            Assert.Throws<InvalidGeometryException>(() =>
                TopologyPreservingPrecisionReducer.ReduceGeometry(
                    invalid, FixedPrecision(1.0, 0.0, 0.0)));
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
