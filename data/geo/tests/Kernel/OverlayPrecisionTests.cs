using geo;

namespace geo.Tests;

public class OverlayPrecisionTests
{
    [Test]
    public void FixedPrecisionChangesTopologyDuringSnapRounding()
    {
        Geometry horizontal = Read(
            "LINESTRING (0.1 0.1, 2.1 0.1)");
        Geometry vertical = Read(
            "LINESTRING (1.1 0.4, 1.1 2.1)");
        PrecisionModel precision = FixedPrecision(1.0, 0.0, 0.0);

        Geometry floatingResult =
            OverlayEngine.GetIntersection(horizontal, vertical);
        Geometry fixedResult = OverlayEngine.GetIntersectionWithPrecision(
            horizontal, vertical, precision);

        Assert.Multiple(() =>
        {
            Assert.That(GeometryInspection.IsEmptyValue(floatingResult),
                Is.True);
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                fixedResult, Read("POINT (1 0)")), Is.True);
            Assert.That(fixedResult.GeometryType,
                Is.EqualTo(GeometryType.Point));
        });
    }

    [Test]
    public void FixedPrecisionHonorsGridOffsetsForAreaOverlay()
    {
        Geometry first = Read(
            "POLYGON ((0.2 0.2, 2.2 0.2, 2.2 2.2, 0.2 2.2, 0.2 0.2))");
        Geometry second = Read(
            "POLYGON ((1.8 0.2, 3.8 0.2, 3.8 2.2, 1.8 2.2, 1.8 0.2))");
        PrecisionModel precision = FixedPrecision(2.0, 0.25, 0.25);

        Geometry intersection = OverlayEngine.GetIntersectionWithPrecision(
            first, second, precision);
        Geometry union = OverlayEngine.GetUnionWithPrecision(
            first, second, precision);
        Geometry difference = OverlayEngine.GetDifferenceWithPrecision(
            first, second, precision);
        Geometry symmetricDifference =
            OverlayEngine.GetSymmetricDifferenceWithPrecision(
                first, second, precision);

        Assert.Multiple(() =>
        {
            Assert.That(intersection.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(
                intersection),
                Is.EqualTo(1.0));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(union),
                Is.EqualTo(7.0));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(
                difference), Is.EqualTo(3.0));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(
                symmetricDifference), Is.EqualTo(6.0));
            Assert.That(AllCoordinatesUseGrid(intersection, precision),
                Is.True);
            Assert.That(AllCoordinatesUseGrid(union, precision), Is.True);
            Assert.That(AllCoordinatesUseGrid(difference, precision), Is.True);
            Assert.That(AllCoordinatesUseGrid(
                symmetricDifference, precision), Is.True);
        });
    }

    [Test]
    public void FixedPrecisionRemovesCollapsedLinesSurfacesAndHoles()
    {
        PrecisionModel precision = FixedPrecision(1.0, 0.0, 0.0);
        Geometry collapsedLine = OverlayEngine.GetUnionWithPrecision(
            Read("LINESTRING (0.1 0.1, 0.4 0.4)"),
            Read("LINESTRING EMPTY"), precision);
        Geometry collapsedSurface = OverlayEngine.GetUnionWithPrecision(
            Read("POLYGON ((0.1 0.1, 0.4 0.1, 0.4 0.4, 0.1 0.4, 0.1 0.1))"),
            Read("POLYGON EMPTY"), precision);
        Geometry collapsedHole = OverlayEngine.GetUnionWithPrecision(
            Read(
                "POLYGON ((0.1 0.1, 4.1 0.1, 4.1 4.1, 0.1 4.1, 0.1 0.1), (1.1 1.1, 1.4 1.1, 1.4 1.4, 1.1 1.4, 1.1 1.1))"),
            Read("POLYGON EMPTY"), precision);

        Assert.Multiple(() =>
        {
            AssertEmptyType(collapsedLine, GeometryType.LineString);
            AssertEmptyType(collapsedSurface, GeometryType.Polygon);
            Assert.That(collapsedHole.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(
                collapsedHole), Is.EqualTo(16.0));
            Assert.That(collapsedHole.Polygon!.Holes, Is.Empty);
        });
    }

    [Test]
    public void FixedPrecisionPreservesLayoutSridAndOwningOrdinates()
    {
        Geometry first = ReadWithSrid(
            "SRID=4979;LINESTRING ZM (0.1 0.1 0 0, 2.1 0.1 2 4)");
        Geometry second = ReadWithSrid(
            "SRID=4979;LINESTRING ZM (1.1 0.4 10 20, 1.1 2.1 12 24)");
        PrecisionModel precision = FixedPrecision(1.0, 0.0, 0.0);

        Geometry result = OverlayEngine.GetIntersectionWithPrecision(
            first, second, precision);
        Point point = (Point)GeometryDispatch.GetSelectedPayload(result);

        Assert.Multiple(() =>
        {
            Assert.That(result.CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(result.SpatialReferenceId, Is.EqualTo(4979));
            Assert.That(point.Coordinate, Is.Not.Null);
            Assert.That(point.Coordinate!.X, Is.EqualTo(1.0));
            Assert.That(point.Coordinate.Y, Is.EqualTo(0.0));
        });
    }

    [Test]
    public void FixedPrecisionResultsAreDeterministicByInputOrder()
    {
        Geometry first = Read(
            "POLYGON ((0.2 0.2, 3.8 0.2, 3.8 3.8, 0.2 3.8, 0.2 0.2))");
        Geometry second = Read(
            "POLYGON ((2.2 -0.2, 6.2 -0.2, 6.2 3.2, 2.2 3.2, 2.2 -0.2))");
        PrecisionModel precision = FixedPrecision(1.0, 0.0, 0.0);

        Geometry firstUnion = OverlayEngine.GetUnionWithPrecision(
            first, second, precision);
        Geometry secondUnion = OverlayEngine.GetUnionWithPrecision(
            second, first, precision);
        Geometry firstIntersection =
            OverlayEngine.GetIntersectionWithPrecision(
                first, second, precision);
        Geometry secondIntersection =
            OverlayEngine.GetIntersectionWithPrecision(
                second, first, precision);

        Assert.Multiple(() =>
        {
            Assert.That(GeometryEquality.AreExactlyEqual(
                firstUnion, secondUnion), Is.True);
            Assert.That(GeometryEquality.AreExactlyEqual(
                firstIntersection, secondIntersection), Is.True);
        });
    }

    [Test]
    public void DerivedSafePrecisionUsesStableLocalOffsetsAndScale()
    {
        Geometry first = Read(
            "LINESTRING (1000000000000000 1000000000000000, 1000000000000004 1000000000000004)");
        Geometry second = Read(
            "LINESTRING (1000000000000000 1000000000000004, 1000000000000004 1000000000000000)");

        PrecisionModel firstModel =
            OverlayEngine.DeriveSafePrecisionModel(first, second);
        PrecisionModel secondModel =
            OverlayEngine.DeriveSafePrecisionModel(second, first);
        Geometry result = OverlayEngine.GetIntersectionWithPrecision(
            first, second, firstModel);

        Assert.Multiple(() =>
        {
            Assert.That(firstModel.Type,
                Is.EqualTo(PrecisionModelType.Fixed));
            Assert.That(firstModel.Scale, Is.EqualTo(1.0e12));
            Assert.That(firstModel.OffsetX,
                Is.EqualTo(1000000000000000.0));
            Assert.That(firstModel.OffsetY,
                Is.EqualTo(1000000000000000.0));
            Assert.That(secondModel.Scale, Is.EqualTo(firstModel.Scale));
            Assert.That(secondModel.OffsetX, Is.EqualTo(firstModel.OffsetX));
            Assert.That(secondModel.OffsetY, Is.EqualTo(firstModel.OffsetY));
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                result,
                Read("POINT (1000000000000002 1000000000000002)")),
                Is.True);
        });
    }

    [Test]
    public void FixedPrecisionRejectsInvalidPrecisionModels()
    {
        Geometry point = Read("POINT (0 0)");

        Assert.Multiple(() =>
        {
            Assert.Throws<PrecisionException>(() =>
                OverlayEngine.GetUnionWithPrecision(
                    point, point, null!));
            Assert.Throws<PrecisionException>(() =>
                OverlayEngine.GetDifferenceWithPrecision(
                    point, point, new PrecisionModel()));
            Assert.Throws<PrecisionException>(() =>
                OverlayEngine.GetSymmetricDifferenceWithPrecision(
                    point, point,
                    FixedPrecision(0.0, 0.0, 0.0)));
            Assert.Throws<PrecisionException>(() =>
                OverlayEngine.GetIntersectionWithPrecision(
                    point, point,
                    FixedPrecision(1.0, double.NaN, 0.0)));
        });
    }

    private static bool AllCoordinatesUseGrid(
        Geometry geometry, PrecisionModel precision)
    {
        Coordinate[] coordinates =
            GeometryInspection.ExtractValidatedCoordinates(geometry);
        return coordinates.All(coordinate =>
            (coordinate.X - precision.OffsetX) * precision.Scale ==
                System.Math.Round(
                    (coordinate.X - precision.OffsetX) * precision.Scale) &&
            (coordinate.Y - precision.OffsetY) * precision.Scale ==
                System.Math.Round(
                    (coordinate.Y - precision.OffsetY) * precision.Scale));
    }

    private static void AssertEmptyType(
        Geometry geometry, GeometryType geometryType)
    {
        Assert.That(geometry.GeometryType, Is.EqualTo(geometryType));
        Assert.That(GeometryInspection.IsEmptyValue(geometry), Is.True);
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
