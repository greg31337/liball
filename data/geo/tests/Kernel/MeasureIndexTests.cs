using geo;

namespace geo.Tests;

public class MeasureIndexTests
{
    [Test]
    public void MonotonicValidationRecognizesBothDirectionsAndPlateaus()
    {
        Geometry increasing = Read(
            "LINESTRING M (0 0 0, 1 0 10, 2 0 10, 3 0 20)");
        Geometry decreasing = Read(
            "LINESTRING M (0 0 20, 1 0 10, 2 0 0)");
        Geometry constant = Read(
            "LINESTRING M (0 0 7, 1 0 7)");
        Geometry nonmonotonic = Read(
            "LINESTRING M (0 0 0, 1 0 10, 2 0 5)");

        Assert.Multiple(() =>
        {
            Assert.That(MeasureIndex.IsMonotonicMeasure(increasing),
                Is.True);
            Assert.That(MeasureIndex.GetMonotonicDirection(increasing),
                Is.EqualTo(1));
            Assert.That(MeasureIndex.GetMonotonicDirection(decreasing),
                Is.EqualTo(-1));
            Assert.That(MeasureIndex.GetMonotonicDirection(constant),
                Is.Zero);
            Assert.That(MeasureIndex.IsMonotonicMeasure(nonmonotonic),
                Is.False);
            Assert.That(MeasureIndex.IsMonotonicMeasure(
                Read("LINESTRING (0 0, 1 0)")), Is.False);
        });
    }

    [Test]
    public void IncreasingLookupInterpolatesAndResolvesRepeatedMeasures()
    {
        Geometry source = Read(
            "LINESTRING ZM (0 0 0 0, 10 0 10 10, " +
            "20 0 20 10, 40 0 40 30)");
        LinearLocation interpolated = MeasureIndex.GetLocationAtMeasure(
            source, 20.0, false, true);
        LinearLocation earlier = MeasureIndex.GetLocationAtMeasure(
            source, 10.0, false, true);
        LinearLocation later = MeasureIndex.GetLocationAtMeasure(
            source, 10.0, false, false);
        Coordinate? coordinate = MeasureIndex.GetCoordinateAtMeasure(
            source, 20.0, false, true);

        Assert.Multiple(() =>
        {
            AssertLocation(interpolated, true, 0, 2, 0.5);
            AssertLocation(earlier, true, 0, 1, 0.0);
            AssertLocation(later, true, 0, 2, 0.0);
            Assert.That(coordinate, Is.Not.Null);
            AssertCoordinate(coordinate!, 30.0, 0.0, 30.0, 20.0);
            Assert.That(MeasureIndex.GetMeasureAtLocation(
                source, interpolated), Is.EqualTo(20.0));
        });
    }

    [Test]
    public void DecreasingLookupAndClampPolicyAreExplicit()
    {
        Geometry source = Read(
            "LINESTRING M (0 0 30, 10 0 20, 20 0 0)");
        LinearLocation middle = MeasureIndex.GetLocationAtMeasure(
            source, 10.0, false, true);
        LinearLocation above = MeasureIndex.GetLocationAtMeasure(
            source, 40.0, true, true);
        LinearLocation below = MeasureIndex.GetLocationAtMeasure(
            source, -5.0, true, true);

        Assert.Multiple(() =>
        {
            AssertLocation(middle, true, 0, 1, 0.5);
            AssertLocation(above, true, 0, 0, 0.0);
            AssertLocation(below, true, 0, 2, 0.0);
            Assert.Throws<InvalidGeometryException>(() =>
                MeasureIndex.GetLocationAtMeasure(
                    source, 31.0, false, true));
            Assert.Throws<InvalidGeometryException>(() =>
                MeasureIndex.GetLocationAtMeasure(
                    source, -1.0, false, true));
        });
    }

    [Test]
    public void DisconnectedMeasureGapsResolveToRequestedEndpoint()
    {
        Geometry source = Read(
            "MULTILINESTRING M ((0 0 0, 10 0 10), " +
            "(20 0 20, 30 0 30))");
        LinearLocation earlier = MeasureIndex.GetLocationAtMeasure(
            source, 15.0, false, true);
        LinearLocation later = MeasureIndex.GetLocationAtMeasure(
            source, 15.0, false, false);

        Assert.Multiple(() =>
        {
            AssertLocation(earlier, true, 0, 1, 0.0);
            AssertLocation(later, true, 1, 0, 0.0);
            Assert.That(MeasureIndex.GetMeasureAtLocation(
                source, earlier), Is.EqualTo(10.0));
            Assert.That(MeasureIndex.GetMeasureAtLocation(
                source, later), Is.EqualTo(20.0));
        });
    }

    [Test]
    public void MeasuredIntervalsCrossComponentsAndPreserveDirection()
    {
        Geometry source = Read(
            "MULTILINESTRING ZM ((0 0 0 0, 10 0 10 10), " +
            "(20 0 20 20, 40 0 40 40))");
        Geometry forward = MeasureIndex.ExtractMeasureInterval(
            source, 5.0, 30.0, false, true);
        Geometry reverse = MeasureIndex.ExtractMeasureInterval(
            source, 30.0, 5.0, false, true);
        LineString[] forwardLines =
            forward.MultiLineString!.LineStrings;
        LineString[] reverseLines =
            reverse.MultiLineString!.LineStrings;

        Assert.Multiple(() =>
        {
            Assert.That(forward.CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(forwardLines, Has.Length.EqualTo(2));
            AssertCoordinate(forwardLines[0].Coordinates[0],
                5.0, 0.0, 5.0, 5.0);
            AssertCoordinate(forwardLines[1].Coordinates[1],
                30.0, 0.0, 30.0, 30.0);
            AssertCoordinate(reverseLines[0].Coordinates[0],
                30.0, 0.0, 30.0, 30.0);
            AssertCoordinate(reverseLines[1].Coordinates[1],
                5.0, 0.0, 5.0, 5.0);
        });
    }

    [Test]
    public void EmptyMeasuredLineHasExplicitIndexResults()
    {
        Geometry empty = Read("LINESTRING M EMPTY");
        LinearLocation location = MeasureIndex.GetLocationAtMeasure(
            empty, 4.0, false, true);
        Geometry interval = MeasureIndex.ExtractMeasureInterval(
            empty, 1.0, 2.0, false, true);

        Assert.Multiple(() =>
        {
            Assert.That(MeasureIndex.IsMonotonicMeasure(empty), Is.True);
            Assert.That(location.IsValid, Is.False);
            Assert.That(MeasureIndex.GetCoordinateAtMeasure(
                empty, 4.0, false, true), Is.Null);
            Assert.That(MeasureIndex.GetMeasureAtLocation(
                empty, location), Is.NaN);
            Assert.That(interval.GeometryType,
                Is.EqualTo(GeometryType.LineString));
            Assert.That(interval.LineString!.Coordinates, Is.Empty);
        });
    }

    [Test]
    public void InvalidMeasureInputsAreRejected()
    {
        Geometry unmeasured = Read("LINESTRING (0 0, 10 0)");
        Geometry nonmonotonic = Read(
            "LINESTRING M (0 0 0, 10 0 10, 20 0 5)");

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                MeasureIndex.GetLocationAtMeasure(
                    unmeasured, 1.0, false, true));
            Assert.Throws<InvalidGeometryException>(() =>
                MeasureIndex.GetLocationAtMeasure(
                    nonmonotonic, 6.0, false, true));
            Assert.Throws<InvalidGeometryException>(() =>
                MeasureIndex.GetLocationAtMeasure(
                    Read("LINESTRING M (0 0 0, 1 0 1)"),
                    double.NaN, true, true));
            Assert.Throws<InvalidGeometryException>(() =>
                MeasureIndex.GetMeasureAtLocation(
                    nonmonotonic, Location(0, 0, 0.5)));
        });
    }

    [Test]
    public void SplitAndLineMergePreserveMeasuredOrdinates()
    {
        IGeometryOverlay overlay = new GeometryOverlay();
        Geometry split = overlay.Split(
            Read("LINESTRING ZM (0 0 0 0, 10 0 10 100)"),
            Read("LINESTRING (5 -1, 5 1)"));
        Geometry[] pieces = split.GeometryCollection!.Geometries;
        IGeometryAssembler assembler = new GeometryAssembler();
        Geometry merged = assembler.DirectedLineMerge(Read(
            "MULTILINESTRING ZM ((0 0 0 0, 5 0 5 50), " +
            "(5 0 5 50, 10 0 10 100))"));
        Coordinate[] mergedCoordinates = merged.LineString!.Coordinates;

        Assert.Multiple(() =>
        {
            Assert.That(split.CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(pieces, Has.Length.EqualTo(2));
            AssertCoordinate(pieces[0].LineString!.Coordinates[1],
                5.0, 0.0, 5.0, 50.0);
            AssertCoordinate(pieces[1].LineString!.Coordinates[0],
                5.0, 0.0, 5.0, 50.0);
            Assert.That(merged.CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(mergedCoordinates, Has.Length.EqualTo(3));
            AssertCoordinate(mergedCoordinates[0],
                0.0, 0.0, 0.0, 0.0);
            AssertCoordinate(mergedCoordinates[1],
                5.0, 0.0, 5.0, 50.0);
            AssertCoordinate(mergedCoordinates[2],
                10.0, 0.0, 10.0, 100.0);
        });
    }

    private static LinearLocation Location(
        int component, int segment, double fraction)
    {
        return new LinearLocation
        {
            ComponentIndex = component,
            SegmentIndex = segment,
            SegmentFraction = fraction
        };
    }

    private static void AssertLocation(
        LinearLocation location, bool valid,
        int component, int segment, double fraction)
    {
        Assert.Multiple(() =>
        {
            Assert.That(location.IsValid, Is.EqualTo(valid));
            Assert.That(location.ComponentIndex, Is.EqualTo(component));
            Assert.That(location.SegmentIndex, Is.EqualTo(segment));
            Assert.That(location.SegmentFraction,
                Is.EqualTo(fraction).Within(1.0e-14));
        });
    }

    private static void AssertCoordinate(
        Coordinate coordinate, double x, double y,
        double z, double measure)
    {
        Assert.Multiple(() =>
        {
            Assert.That(coordinate.X, Is.EqualTo(x).Within(1.0e-14));
            Assert.That(coordinate.Y, Is.EqualTo(y).Within(1.0e-14));
            Assert.That(coordinate.Z, Is.EqualTo(z).Within(1.0e-14));
            Assert.That(coordinate.M,
                Is.EqualTo(measure).Within(1.0e-14));
        });
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
