using geo;

namespace geo.Tests;

public class GeometryOverlayTests
{
    [Test]
    public void OrdinaryOverlayMethodsExposeCanonicalKernelResults()
    {
        IGeometryOverlay overlay = new GeometryOverlay();
        Geometry first = Read(
            "POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))");
        Geometry second = Read(
            "POLYGON ((2 0, 6 0, 6 4, 2 4, 2 0))");

        Geometry intersection = overlay.Intersection(first, second);
        Geometry union = overlay.Union(first, second);
        Geometry difference = overlay.Difference(first, second);
        Geometry symmetricDifference =
            overlay.SymmetricDifference(first, second);

        Assert.Multiple(() =>
        {
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(
                intersection), Is.EqualTo(8.0));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(union),
                Is.EqualTo(24.0));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(
                difference), Is.EqualTo(8.0));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(
                symmetricDifference), Is.EqualTo(16.0));
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                intersection,
                Read("POLYGON ((2 0, 4 0, 4 4, 2 4, 2 0))")),
                Is.True);
        });
    }

    [Test]
    public void ExplicitPrecisionMethodsUseTheRequestedFixedGrid()
    {
        IGeometryOverlay overlay = new GeometryOverlay();
        Geometry first = Read(
            "POLYGON ((0.2 0.2, 2.2 0.2, 2.2 2.2, 0.2 2.2, 0.2 0.2))");
        Geometry second = Read(
            "POLYGON ((1.8 0.2, 3.8 0.2, 3.8 2.2, 1.8 2.2, 1.8 0.2))");
        PrecisionModel precision = new PrecisionModel
        {
            Type = PrecisionModelType.Fixed,
            Scale = 2.0,
            OffsetX = 0.25,
            OffsetY = 0.25
        };

        Geometry intersection = overlay.IntersectionAtPrecision(
            first, second, precision);
        Geometry union = overlay.UnionAtPrecision(
            first, second, precision);
        Geometry difference = overlay.DifferenceAtPrecision(
            first, second, precision);
        Geometry symmetricDifference =
            overlay.SymmetricDifferenceAtPrecision(
                first, second, precision);

        Assert.Multiple(() =>
        {
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(
                intersection), Is.EqualTo(1.0));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(union),
                Is.EqualTo(7.0));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(
                difference), Is.EqualTo(3.0));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(
                symmetricDifference), Is.EqualTo(6.0));
            Assert.That(CoordinatesUseGrid(intersection, precision),
                Is.True);
            Assert.That(CoordinatesUseGrid(union, precision), Is.True);
            Assert.That(CoordinatesUseGrid(difference, precision), Is.True);
            Assert.That(CoordinatesUseGrid(
                symmetricDifference, precision), Is.True);
        });
    }

    [Test]
    public void UnionStrategiesAndDissolveAreAvailableThroughTheContract()
    {
        IGeometryOverlay overlay = new GeometryOverlay();
        Geometry lines = Read(
            "MULTILINESTRING ((0 0, 2 0), (2 0, 4 0), (2 0, 0 0))");
        Geometry polygons = Read(
            "MULTIPOLYGON (((0 0, 4 0, 4 2, 0 2, 0 0)), ((2 0, 6 0, 6 2, 2 2, 2 0)))");
        Geometry left = Read(
            "MULTIPOLYGON (((0 0, 3 0, 3 2, 0 2, 0 0)), ((10 0, 12 0, 12 2, 10 2, 10 0)))");
        Geometry right = Read(
            "MULTIPOLYGON (((2 0, 5 0, 5 2, 2 2, 2 0)), ((11 0, 13 0, 13 2, 11 2, 11 0)))");

        Geometry unary = overlay.UnaryUnion(lines);
        Geometry dissolved = overlay.Dissolve(lines);
        Geometry cascaded = overlay.CascadedUnion(polygons);
        Geometry disjoint = overlay.DisjointSubsetUnion(left, right);

        Assert.Multiple(() =>
        {
            Assert.That(GeometryEquality.AreExactlyEqual(
                unary, dissolved), Is.True);
            Assert.That(GeometryMeasureAlgorithms.GetLength(unary),
                Is.EqualTo(4.0));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(
                cascaded), Is.EqualTo(12.0));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(
                disjoint), Is.EqualTo(16.0));
        });
    }

    [Test]
    public void ClipSplitAndSnapReturnOwningGeometryResults()
    {
        IGeometryOverlay overlay = new GeometryOverlay();
        Geometry line = Read("LINESTRING (0 0, 10 0)");
        Geometry clipped = overlay.ClipToEnvelope(
            line, Bounds(2.0, -1.0, 8.0, 1.0));
        Geometry split = overlay.Split(
            line, Read("MULTIPOINT ((3 0), (7 0))"));
        Geometry snapped = overlay.Snap(
            line, Read("POINT (5 0.1)"), 0.2);
        line.LineString!.Coordinates[0].X = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                clipped, Read("LINESTRING (2 0, 8 0)")), Is.True);
            Assert.That(split.GeometryCollection!.Geometries,
                Has.Length.EqualTo(3));
            Assert.That(snapped.LineString!.Coordinates,
                Has.Length.EqualTo(3));
            Assert.That(snapped.LineString.Coordinates[0].X,
                Is.EqualTo(0.0));
        });
    }

    [Test]
    public void NodeAndSharedPathsExposeNodedDirectionalLinework()
    {
        IGeometryOverlay overlay = new GeometryOverlay();
        Geometry noded = overlay.Node(
            Read("MULTILINESTRING ((0 0, 4 0), (2 -2, 2 2))"));
        Geometry shared = overlay.SharedPaths(
            Read("LINESTRING (0 0, 10 0)"),
            Read("MULTILINESTRING ((2 0, 5 0), (8 0, 5 0))"));
        Geometry[] groups = shared.GeometryCollection!.Geometries;

        Assert.Multiple(() =>
        {
            Assert.That(noded.GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(noded.MultiLineString!.LineStrings,
                Has.Length.EqualTo(4));
            Assert.That(groups, Has.Length.EqualTo(2));
            Assert.That(groups[0].MultiLineString!.LineStrings,
                Has.Length.EqualTo(1));
            Assert.That(groups[1].MultiLineString!.LineStrings,
                Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void PublicOverlayMethodsPreserveSpecificValidationExceptions()
    {
        IGeometryOverlay overlay = new GeometryOverlay();
        Geometry xy = Read("POINT (0 0)");
        Geometry xyz = Read("POINT Z (0 0 1)");
        PrecisionModel floating = new PrecisionModel();

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                overlay.Intersection(xy, xyz));
            Assert.Throws<InvalidGeometryException>(() =>
                overlay.Union(xy, xyz));
            Assert.Throws<InvalidGeometryException>(() =>
                overlay.Difference(xy, xyz));
            Assert.Throws<InvalidGeometryException>(() =>
                overlay.SymmetricDifference(xy, xyz));
            Assert.Throws<PrecisionException>(() =>
                overlay.IntersectionAtPrecision(xy, xy, floating));
            Assert.Throws<PrecisionException>(() =>
                overlay.UnionAtPrecision(xy, xy, floating));
            Assert.Throws<PrecisionException>(() =>
                overlay.DifferenceAtPrecision(xy, xy, floating));
            Assert.Throws<PrecisionException>(() =>
                overlay.SymmetricDifferenceAtPrecision(
                    xy, xy, floating));
            Assert.Throws<InvalidGeometryException>(() =>
                overlay.CascadedUnion(Read("LINESTRING (0 0, 1 0)")));
            Assert.Throws<InvalidGeometryException>(() =>
                overlay.Split(xy, xy));
            Assert.Throws<PrecisionException>(() =>
                overlay.Snap(xy, xy, -1.0));
            Assert.Throws<InvalidGeometryException>(() =>
                overlay.SharedPaths(xy, xy));
        });
    }

    private static bool CoordinatesUseGrid(
        Geometry geometry, PrecisionModel precision)
    {
        Coordinate[] coordinates =
            GeometryInspection.ExtractValidatedCoordinates(geometry);
        return coordinates.All(coordinate =>
            (coordinate.X - precision.OffsetX) * precision.Scale ==
                Math.Round(
                    (coordinate.X - precision.OffsetX) *
                    precision.Scale) &&
            (coordinate.Y - precision.OffsetY) * precision.Scale ==
                Math.Round(
                    (coordinate.Y - precision.OffsetY) *
                    precision.Scale));
    }

    private static Envelope Bounds(
        double minimumX, double minimumY,
        double maximumX, double maximumY)
    {
        return EnvelopeAlgorithms.CreateBoundsEnvelope(
            minimumX, minimumY, maximumX, maximumY);
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
