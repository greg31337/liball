using geo;

namespace geo.Tests;

public class PreparedGeometryDataTests
{
    [Test]
    public void PointDataSnapshotsCoordinatesEnvelopeAndRepresentatives()
    {
        Geometry source = Read(
            "MULTIPOINT Z ((1 2 3), (4 6 7))");

        PreparedPointData prepared = new PreparedPointData(source);
        source.MultiPoint!.Points[0].Coordinate!.X = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(prepared.GeometrySnapshot, Is.Not.SameAs(source));
            AssertEnvelope(prepared.Bounds, 1.0, 2.0, 4.0, 6.0);
            Assert.That(prepared.Coordinates, Has.Length.EqualTo(2));
            AssertCoordinate(prepared.Coordinates[0], 1.0, 2.0, 3.0);
            AssertCoordinate(prepared.Coordinates[1], 4.0, 6.0, 7.0);
            Assert.That(prepared.RepresentativePoints,
                Has.Length.EqualTo(2));
            AssertCoordinate(
                prepared.RepresentativePoints[0], 1.0, 2.0, 3.0);
            AssertCoordinate(
                prepared.RepresentativePoints[1], 4.0, 6.0, 7.0);
        });
    }

    [Test]
    public void LineDataCachesSegmentsAndOneRepresentativePerComponent()
    {
        Geometry source = Read(
            "MULTILINESTRING ((0 0, 4 0), (10 2, 10 8))");

        PreparedLineData prepared = new PreparedLineData(source);
        SegmentSpatialIndex originalIndex = prepared.SegmentIndex;
        source.MultiLineString!.LineStrings[0].Coordinates[0].Y = 50.0;
        DistanceSegmentFacet[] candidates = prepared.SegmentIndex.Query(
            EnvelopeAlgorithms.CreateBoundsEnvelope(3.0, -1.0, 5.0, 1.0));

        Assert.Multiple(() =>
        {
            AssertEnvelope(prepared.Bounds, 0.0, 0.0, 10.0, 8.0);
            Assert.That(prepared.Parts.Segments, Has.Count.EqualTo(2));
            Assert.That(candidates, Has.Length.EqualTo(1));
            Assert.That(candidates[0].Start.X, Is.Zero);
            Assert.That(candidates[0].Start.Y, Is.Zero);
            Assert.That(prepared.RepresentativePoints,
                Has.Length.EqualTo(2));
            AssertCoordinate(
                prepared.RepresentativePoints[0], 2.0, 0.0, double.NaN);
            AssertCoordinate(
                prepared.RepresentativePoints[1], 10.0, 5.0, double.NaN);
            Assert.That(prepared.SegmentIndex, Is.SameAs(originalIndex));
        });
    }

    [Test]
    public void PolygonDataCachesRingSegmentsAndHoleAwarePointLocation()
    {
        Geometry source = Read(
            "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), " +
            "(4 4, 6 4, 6 6, 4 6, 4 4))");

        PreparedPolygonData prepared = new PreparedPolygonData(source);
        source.Polygon!.Shell.Coordinates[0].X = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(prepared.Parts.Segments, Has.Count.EqualTo(8));
            Assert.That(prepared.SegmentIndex.GetAllSegments(),
                Has.Length.EqualTo(8));
            Assert.That(prepared.PointLocator.Locate(Coordinate(1.0, 1.0)),
                Is.EqualTo(TopologicalLocation.Interior));
            Assert.That(prepared.PointLocator.Locate(Coordinate(0.0, 5.0)),
                Is.EqualTo(TopologicalLocation.Boundary));
            Assert.That(prepared.PointLocator.Locate(Coordinate(5.0, 5.0)),
                Is.EqualTo(TopologicalLocation.Exterior));
            Assert.That(prepared.PointLocator.Locate(Coordinate(20.0, 5.0)),
                Is.EqualTo(TopologicalLocation.Exterior));
            Assert.That(prepared.RepresentativePoints,
                Has.Length.EqualTo(1));
            Assert.That(prepared.PointLocator.Locate(
                prepared.RepresentativePoints[0]),
                Is.EqualTo(TopologicalLocation.Interior));
        });
    }

    [Test]
    public void PolygonLocatorCombinesMultipleAreaComponents()
    {
        PreparedPolygonData prepared = new PreparedPolygonData(Read(
            "MULTIPOLYGON (((0 0, 3 0, 3 3, 0 3, 0 0)), " +
            "((10 10, 14 10, 14 14, 10 14, 10 10)))"));

        Assert.Multiple(() =>
        {
            Assert.That(prepared.PointLocator.Components,
                Has.Length.EqualTo(2));
            Assert.That(prepared.RepresentativePoints,
                Has.Length.EqualTo(2));
            Assert.That(prepared.PointLocator.Locate(Coordinate(12.0, 12.0)),
                Is.EqualTo(TopologicalLocation.Interior));
            Assert.That(prepared.PointLocator.Locate(Coordinate(10.0, 12.0)),
                Is.EqualTo(TopologicalLocation.Boundary));
            Assert.That(prepared.PointLocator.Locate(Coordinate(6.0, 6.0)),
                Is.EqualTo(TopologicalLocation.Exterior));
        });
    }

    [Test]
    public void CollectionDataBuildsUnifiedAndDimensionSpecificCaches()
    {
        Geometry source = Read(
            "GEOMETRYCOLLECTION (POINT (1 1), " +
            "LINESTRING (5 5, 8 5), " +
            "POLYGON ((20 20, 30 20, 30 30, 20 30, 20 20)))");

        PreparedCollectionData prepared = new PreparedCollectionData(source);
        source.GeometryCollection!.Geometries[0].Point!.Coordinate!.X = 99.0;

        Assert.Multiple(() =>
        {
            AssertEnvelope(prepared.Bounds, 1.0, 1.0, 30.0, 30.0);
            Assert.That(prepared.PointData.Coordinates,
                Has.Length.EqualTo(1));
            AssertCoordinate(
                prepared.PointData.Coordinates[0], 1.0, 1.0, double.NaN);
            Assert.That(prepared.LineData.Parts.Segments,
                Has.Count.EqualTo(1));
            Assert.That(prepared.PolygonData.Parts.Segments,
                Has.Count.EqualTo(4));
            Assert.That(prepared.Parts.Segments, Has.Count.EqualTo(5));
            Assert.That(prepared.SegmentIndex.GetAllSegments(),
                Has.Length.EqualTo(5));
            Assert.That(prepared.RepresentativePoints,
                Has.Length.EqualTo(3));
            Assert.That(prepared.PointLocator.Locate(Coordinate(25.0, 25.0)),
                Is.EqualTo(TopologicalLocation.Interior));
        });
    }

    [Test]
    public void EmptySnapshotsHaveEmptyCachesAndEnvelopes()
    {
        PreparedCollectionData prepared = new PreparedCollectionData(
            Read("GEOMETRYCOLLECTION EMPTY"));

        Assert.Multiple(() =>
        {
            Assert.That(double.IsNaN(prepared.Bounds.MinX), Is.True);
            Assert.That(double.IsNaN(prepared.Bounds.MinY), Is.True);
            Assert.That(double.IsNaN(prepared.Bounds.MaxX), Is.True);
            Assert.That(double.IsNaN(prepared.Bounds.MaxY), Is.True);
            Assert.That(prepared.PointData.Coordinates, Is.Empty);
            Assert.That(prepared.Parts.Segments, Is.Empty);
            Assert.That(prepared.SegmentIndex.GetAllSegments(), Is.Empty);
            Assert.That(prepared.RepresentativePoints, Is.Empty);
            Assert.That(prepared.PointLocator.Locate(Coordinate(0.0, 0.0)),
                Is.EqualTo(TopologicalLocation.Exterior));
        });
    }

    [Test]
    public void PreparedSnapshotsRejectWrongDimensionsInvalidAndExtendedInputs()
    {
        Geometry line = Read("LINESTRING (0 0, 1 1)");
        Geometry point = Read("POINT (0 0)");
        Geometry polygon = Read(
            "POLYGON ((0 0, 4 4, 0 4, 4 0, 0 0))");
        Geometry triangle = Read(
            "TRIANGLE ((0 0, 2 0, 0 2, 0 0))");

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                new PreparedPointData(line));
            Assert.Throws<InvalidGeometryException>(() =>
                new PreparedLineData(point));
            Assert.Throws<InvalidGeometryException>(() =>
                new PreparedPolygonData(line));
            Assert.Throws<InvalidGeometryException>(() =>
                new PreparedCollectionData(polygon));
            Assert.Throws<InvalidGeometryException>(() =>
                new PreparedCollectionData(triangle));
        });
    }

    [Test]
    public void PreparedIndexesAndLocatorsSupportConcurrentReads()
    {
        PreparedPolygonData prepared = new PreparedPolygonData(Read(
            "POLYGON ((0 0, 100 0, 100 100, 0 100, 0 0), " +
            "(40 40, 60 40, 60 60, 40 60, 40 40))"));
        int failures = 0;

        Parallel.For(0, 256, index =>
        {
            Coordinate coordinate = index % 2 == 0 ?
                Coordinate(10.0, 10.0) : Coordinate(50.0, 50.0);
            TopologicalLocation expected = index % 2 == 0 ?
                TopologicalLocation.Interior : TopologicalLocation.Exterior;
            TopologicalLocation actual = prepared.PointLocator.Locate(
                coordinate);
            DistanceSegmentFacet[] segments = prepared.SegmentIndex.Query(
                EnvelopeAlgorithms.CreateCoordinateEnvelope(coordinate));
            if (actual != expected || segments == null)
            {
                Interlocked.Increment(ref failures);
            }
        });

        Assert.That(failures, Is.Zero);
    }

    private static Coordinate Coordinate(double x, double y)
    {
        return new Coordinate { X = x, Y = y };
    }

    private static void AssertCoordinate(
        Coordinate coordinate, double x, double y, double z)
    {
        Assert.That(coordinate.X, Is.EqualTo(x));
        Assert.That(coordinate.Y, Is.EqualTo(y));
        if (double.IsNaN(z))
        {
            Assert.That(double.IsNaN(coordinate.Z), Is.True);
        }
        else
        {
            Assert.That(coordinate.Z, Is.EqualTo(z));
        }
    }

    private static void AssertEnvelope(
        Envelope envelope, double minimumX, double minimumY,
        double maximumX, double maximumY)
    {
        Assert.That(envelope.MinX, Is.EqualTo(minimumX));
        Assert.That(envelope.MinY, Is.EqualTo(minimumY));
        Assert.That(envelope.MaxX, Is.EqualTo(maximumX));
        Assert.That(envelope.MaxY, Is.EqualTo(maximumY));
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
