using geo;

namespace geo.Tests;

public class FactoryConstructionTests
{
    [Test]
    public void CreateRectangleAppliesPrecisionLayoutAndConfiguredOrientation()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions
        {
            CoordinateLayout = CoordinateLayout.XYZM,
            PrecisionModelType = PrecisionModelType.Fixed,
            PrecisionScale = 1.0,
            RingOrientation = RingOrientation.Clockwise
        });
        Coordinate minimum = CoordinateValue(0.2, 0.2, 5.0, 7.0);
        Coordinate maximum = CoordinateValue(2.8, 3.2, 5.0, 7.0);

        Geometry rectangle = factory.CreateRectangle(minimum, maximum);
        minimum.X = 100.0;
        Coordinate[] coordinates = rectangle.Polygon!.Shell.Coordinates;

        Assert.Multiple(() =>
        {
            Assert.That(coordinates, Has.Length.EqualTo(5));
            Assert.That(coordinates[0].X, Is.EqualTo(0.0));
            Assert.That(coordinates[0].Y, Is.EqualTo(0.0));
            Assert.That(coordinates[1].X, Is.EqualTo(0.0));
            Assert.That(coordinates[1].Y, Is.EqualTo(3.0));
            Assert.That(coordinates[2].X, Is.EqualTo(3.0));
            Assert.That(coordinates[2].Y, Is.EqualTo(3.0));
            Assert.That(coordinates[0].Z, Is.EqualTo(5.0));
            Assert.That(coordinates[0].M, Is.EqualTo(7.0));
            Assert.That(SignedArea(coordinates), Is.LessThan(0.0));
        });
    }

    [Test]
    public void CreateRectangleRejectsInvalidOrCollapsedCorners()
    {
        GeometryFactory xyzFactory = new GeometryFactory(new GeometryFactoryOptions { CoordinateLayout = CoordinateLayout.XYZ });
        GeometryFactory fixedFactory = new GeometryFactory(new GeometryFactoryOptions
        {
            PrecisionModelType = PrecisionModelType.Fixed,
            PrecisionScale = 1.0
        });

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() => xyzFactory.CreateRectangle(
                CoordinateValue(0.0, 0.0, 1.0, double.NaN),
                CoordinateValue(2.0, 2.0, 2.0, double.NaN)));
            Assert.Throws<InvalidGeometryException>(() => fixedFactory.CreateRectangle(
                CoordinateValue(0.1, 0.1, double.NaN, double.NaN),
                CoordinateValue(0.4, 2.0, double.NaN, double.NaN)));
            Assert.Throws<InvalidGeometryException>(() => fixedFactory.CreateRectangle(
                CoordinateValue(2.0, 0.0, double.NaN, double.NaN),
                CoordinateValue(1.0, 2.0, double.NaN, double.NaN)));
        });
    }

    [Test]
    public void CreateEnvelopePolygonCopiesAValidXyEnvelope()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions());
        Envelope envelope = new Envelope { MinX = -2.0, MinY = -1.0, MaxX = 4.0, MaxY = 3.0 };

        Geometry polygon = factory.CreateEnvelopePolygon(envelope);
        envelope.MinX = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(polygon.Polygon!.Shell.Coordinates[0].X, Is.EqualTo(-2.0));
            Assert.That(polygon.Polygon.Shell.Coordinates[2].Y, Is.EqualTo(3.0));
            Assert.That(SignedArea(polygon.Polygon.Shell.Coordinates), Is.GreaterThan(0.0));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateEnvelopePolygon(new Envelope()));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateEnvelopePolygon(new Envelope { MinX = 1.0, MinY = 0.0, MaxX = 1.0, MaxY = 2.0 }));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateEnvelopePolygon(null!));
        });

        GeometryFactory xyzFactory = new GeometryFactory(new GeometryFactoryOptions { CoordinateLayout = CoordinateLayout.XYZ });
        Assert.Throws<InvalidGeometryException>(() => xyzFactory.CreateEnvelopePolygon(new Envelope { MinX = 0.0, MinY = 0.0, MaxX = 1.0, MaxY = 1.0 }));
    }

    [Test]
    public void CreateCircleUsesFourTimesTheQuadrantResolution()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions { CoordinateLayout = CoordinateLayout.XYM });
        Coordinate center = CoordinateValue(10.0, 20.0, double.NaN, 4.0);

        Geometry circle = factory.CreateCircle(center, 2.0, 2);
        Coordinate[] coordinates = circle.Polygon!.Shell.Coordinates;
        center.X = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(coordinates, Has.Length.EqualTo(9));
            Assert.That(coordinates[0].X, Is.EqualTo(12.0).Within(1e-12));
            Assert.That(coordinates[0].Y, Is.EqualTo(20.0).Within(1e-12));
            Assert.That(coordinates[2].X, Is.EqualTo(10.0).Within(1e-12));
            Assert.That(coordinates[2].Y, Is.EqualTo(22.0).Within(1e-12));
            Assert.That(coordinates[0].M, Is.EqualTo(4.0));
            Assert.That(coordinates[8].X, Is.EqualTo(coordinates[0].X));
            Assert.That(coordinates[8].Y, Is.EqualTo(coordinates[0].Y));
            Assert.That(SignedArea(coordinates), Is.GreaterThan(0.0));
        });

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() => factory.CreateCircle(CoordinateValue(0.0, 0.0, double.NaN, 1.0), 0.0, 2));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateCircle(CoordinateValue(0.0, 0.0, double.NaN, 1.0), 1.0, 0));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateCircle(CoordinateValue(0.0, 0.0, double.NaN, 1.0), double.PositiveInfinity, 2));
        });
    }

    [Test]
    public void CreateRegularPolygonHonorsVertexCountAndClockwiseOrientation()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions { RingOrientation = RingOrientation.Clockwise });

        Geometry polygon = factory.CreateRegularPolygon(CoordinateValue(0.0, 0.0, double.NaN, double.NaN), 3.0, 6);
        Coordinate[] coordinates = polygon.Polygon!.Shell.Coordinates;

        Assert.Multiple(() =>
        {
            Assert.That(coordinates, Has.Length.EqualTo(7));
            Assert.That(coordinates[0].X, Is.EqualTo(3.0).Within(1e-12));
            Assert.That(coordinates[0].Y, Is.EqualTo(0.0).Within(1e-12));
            Assert.That(SignedArea(coordinates), Is.LessThan(0.0));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateRegularPolygon(CoordinateValue(0.0, 0.0, double.NaN, double.NaN), 1.0, 2));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateRegularPolygon(CoordinateValue(0.0, 0.0, double.NaN, double.NaN), -1.0, 4));
        });
    }

    [Test]
    public void CreateSegmentAppliesPrecisionAndSnapshotsEndpoints()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions
        {
            PrecisionModelType = PrecisionModelType.Fixed,
            PrecisionScale = 2.0
        });
        Coordinate start = CoordinateValue(0.24, 1.26, double.NaN, double.NaN);
        Coordinate end = CoordinateValue(2.24, 3.26, double.NaN, double.NaN);

        Geometry segment = factory.CreateSegment(start, end);
        start.X = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(segment.LineString!.Coordinates, Has.Length.EqualTo(2));
            Assert.That(segment.LineString.Coordinates[0].X, Is.EqualTo(0.0));
            Assert.That(segment.LineString.Coordinates[0].Y, Is.EqualTo(1.5));
            Assert.That(segment.LineString.Coordinates[1].X, Is.EqualTo(2.0));
            Assert.That(segment.LineString.Coordinates[1].Y, Is.EqualTo(3.5));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateSegment(null!, end));
        });
    }

    [Test]
    public void CreateNarrowestCollectionSelectsSingletonMultiOrHeterogeneousResults()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions { SpatialReferenceId = 4326 });
        Geometry pointA = factory.CreatePoint(CoordinateValue(1.0, 2.0, double.NaN, double.NaN));
        Geometry pointB = factory.CreatePoint(CoordinateValue(3.0, 4.0, double.NaN, double.NaN));
        Geometry lineA = factory.CreateSegment(CoordinateValue(0.0, 0.0, double.NaN, double.NaN), CoordinateValue(1.0, 1.0, double.NaN, double.NaN));
        Geometry lineB = factory.CreateSegment(CoordinateValue(2.0, 2.0, double.NaN, double.NaN), CoordinateValue(3.0, 3.0, double.NaN, double.NaN));
        Geometry polygonA = factory.CreateEnvelopePolygon(new Envelope { MinX = 0.0, MinY = 0.0, MaxX = 1.0, MaxY = 1.0 });
        Geometry polygonB = factory.CreateEnvelopePolygon(new Envelope { MinX = 2.0, MinY = 2.0, MaxX = 3.0, MaxY = 3.0 });

        Geometry empty = factory.CreateNarrowestCollection([]);
        Geometry singleton = factory.CreateNarrowestCollection([pointA]);
        Geometry points = factory.CreateNarrowestCollection([pointA, pointB]);
        Geometry lines = factory.CreateNarrowestCollection([lineA, lineB]);
        Geometry polygons = factory.CreateNarrowestCollection([polygonA, polygonB]);
        Geometry mixed = factory.CreateNarrowestCollection([pointA, lineA]);
        pointA.Point!.Coordinate!.X = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(empty.GeometryType, Is.EqualTo(GeometryType.GeometryCollection));
            Assert.That(singleton.GeometryType, Is.EqualTo(GeometryType.Point));
            Assert.That(singleton, Is.Not.SameAs(pointA));
            Assert.That(singleton.Point!.Coordinate!.X, Is.EqualTo(1.0));
            Assert.That(points.GeometryType, Is.EqualTo(GeometryType.MultiPoint));
            Assert.That(points.MultiPoint!.Points, Has.Length.EqualTo(2));
            Assert.That(points.MultiPoint.Points[0].Coordinate!.X, Is.EqualTo(1.0));
            Assert.That(lines.GeometryType, Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(polygons.GeometryType, Is.EqualTo(GeometryType.MultiPolygon));
            Assert.That(mixed.GeometryType, Is.EqualTo(GeometryType.GeometryCollection));
        });

        Geometry wrongSrid = new Geometry { SpatialReferenceId = 3857 };
        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() => factory.CreateNarrowestCollection([pointB, wrongSrid]));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateNarrowestCollection(null!));
        });
    }

    private static Coordinate CoordinateValue(double x, double y, double z, double m)
    {
        return new Coordinate { X = x, Y = y, Z = z, M = m };
    }

    private static double SignedArea(Coordinate[] coordinates)
    {
        double sum = 0.0;
        int index = 0;
        while (index < coordinates.Length - 1)
        {
            sum += coordinates[index].X * coordinates[index + 1].Y - coordinates[index + 1].X * coordinates[index].Y;
            index++;
        }

        return sum / 2.0;
    }
}
