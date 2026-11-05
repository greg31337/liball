using geo;

namespace geo.Tests;

public class GeometryBoundaryTests
{
    [Test]
    public void PointAndMultiPointBoundariesAreEmptyGeometryCollections()
    {
        GeometryInspector inspector = new GeometryInspector();
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions
        {
            CoordinateLayout = CoordinateLayout.XYZ,
            SpatialReferenceId = 4979
        });
        Geometry point = factory.CreatePoint(CoordinateValue(1.0, 2.0, 3.0, double.NaN));
        Geometry multiPoint = factory.CreateMultiPoint([
            new Point { Coordinate = CoordinateValue(1.0, 2.0, 3.0, double.NaN) },
            new Point { Coordinate = CoordinateValue(4.0, 5.0, 6.0, double.NaN) }
        ]);

        Geometry pointBoundary = inspector.GetBoundary(point, BoundaryNodeRule.Mod2);
        Geometry multiPointBoundary = inspector.GetBoundary(multiPoint, BoundaryNodeRule.Endpoint);

        Assert.Multiple(() =>
        {
            Assert.That(pointBoundary.GeometryType, Is.EqualTo(GeometryType.GeometryCollection));
            Assert.That(pointBoundary.GeometryCollection!.Geometries, Is.Empty);
            Assert.That(pointBoundary.CoordinateLayout, Is.EqualTo(CoordinateLayout.XYZ));
            Assert.That(pointBoundary.SpatialReferenceId, Is.EqualTo(4979));
            Assert.That(multiPointBoundary.GeometryType, Is.EqualTo(GeometryType.GeometryCollection));
            Assert.That(multiPointBoundary.GeometryCollection!.Geometries, Is.Empty);
            Assert.That(multiPointBoundary.CoordinateLayout, Is.EqualTo(CoordinateLayout.XYZ));
            Assert.That(multiPointBoundary.SpatialReferenceId, Is.EqualTo(4979));
        });
    }

    [Test]
    public void OpenAndClosedLineBoundariesFollowEveryBoundaryNodeRule()
    {
        GeometryInspector inspector = new GeometryInspector();
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions());
        Geometry open = factory.CreateLineString([
            CoordinateValue(0.0, 0.0, double.NaN, double.NaN),
            CoordinateValue(2.0, 0.0, double.NaN, double.NaN)
        ]);
        Geometry closed = factory.CreateLineString([
            CoordinateValue(0.0, 0.0, double.NaN, double.NaN),
            CoordinateValue(2.0, 0.0, double.NaN, double.NaN),
            CoordinateValue(1.0, 2.0, double.NaN, double.NaN),
            CoordinateValue(0.0, 0.0, double.NaN, double.NaN)
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(BoundaryPoints(inspector.GetBoundary(open, BoundaryNodeRule.Mod2)), Has.Length.EqualTo(2));
            Assert.That(BoundaryPoints(inspector.GetBoundary(open, BoundaryNodeRule.Endpoint)), Has.Length.EqualTo(2));
            Assert.That(BoundaryPoints(inspector.GetBoundary(open, BoundaryNodeRule.MultivalentEndpoint)), Is.Empty);
            Assert.That(BoundaryPoints(inspector.GetBoundary(open, BoundaryNodeRule.MonovalentEndpoint)), Has.Length.EqualTo(2));
            Assert.That(BoundaryPoints(inspector.GetBoundary(closed, BoundaryNodeRule.Mod2)), Is.Empty);
            Assert.That(BoundaryPoints(inspector.GetBoundary(closed, BoundaryNodeRule.Endpoint)), Has.Length.EqualTo(1));
            Assert.That(BoundaryPoints(inspector.GetBoundary(closed, BoundaryNodeRule.MultivalentEndpoint)), Has.Length.EqualTo(1));
            Assert.That(BoundaryPoints(inspector.GetBoundary(closed, BoundaryNodeRule.MonovalentEndpoint)), Is.Empty);
        });
    }

    [Test]
    public void MultiLineBoundaryUsesAggregateEndpointValenceAndStableOrder()
    {
        GeometryInspector inspector = new GeometryInspector();
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions());
        LineString[] lines =
        {
            LineValue(0.0, 0.0, 1.0, 0.0),
            LineValue(1.0, 0.0, 2.0, 0.0),
            LineValue(1.0, 0.0, 3.0, 0.0),
            LineValue(1.0, 0.0, 4.0, 0.0)
        };
        Geometry multiLine = factory.CreateMultiLineString(lines);

        Point[] mod2 = BoundaryPoints(inspector.GetBoundary(multiLine, BoundaryNodeRule.Mod2));
        Point[] endpoint = BoundaryPoints(inspector.GetBoundary(multiLine, BoundaryNodeRule.Endpoint));
        Point[] multivalent = BoundaryPoints(inspector.GetBoundary(multiLine, BoundaryNodeRule.MultivalentEndpoint));
        Point[] monovalent = BoundaryPoints(inspector.GetBoundary(multiLine, BoundaryNodeRule.MonovalentEndpoint));

        Assert.Multiple(() =>
        {
            Assert.That(mod2, Has.Length.EqualTo(4));
            Assert.That(mod2[0].Coordinate!.X, Is.EqualTo(0.0));
            Assert.That(mod2[1].Coordinate!.X, Is.EqualTo(2.0));
            Assert.That(mod2[2].Coordinate!.X, Is.EqualTo(3.0));
            Assert.That(mod2[3].Coordinate!.X, Is.EqualTo(4.0));
            Assert.That(endpoint, Has.Length.EqualTo(5));
            Assert.That(endpoint[1].Coordinate!.X, Is.EqualTo(1.0));
            Assert.That(multivalent, Has.Length.EqualTo(1));
            Assert.That(multivalent[0].Coordinate!.X, Is.EqualTo(1.0));
            Assert.That(monovalent, Has.Length.EqualTo(4));
        });
    }

    [Test]
    public void EndpointMergingUsesPlanarIdentityAndPreservesFirstFullCoordinate()
    {
        GeometryInspector inspector = new GeometryInspector();
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions
        {
            CoordinateLayout = CoordinateLayout.XYZM,
            SpatialReferenceId = 4326
        });
        LineString first = new LineString
        {
            Coordinates =
            [
                CoordinateValue(0.0, 0.0, 1.0, 2.0),
                CoordinateValue(1.0, 0.0, 3.0, 4.0)
            ]
        };
        LineString second = new LineString
        {
            Coordinates =
            [
                CoordinateValue(1.0, 0.0, 9.0, 10.0),
                CoordinateValue(2.0, 0.0, 11.0, 12.0)
            ]
        };
        Geometry multiLine = factory.CreateMultiLineString([first, second]);

        Geometry boundary = inspector.GetBoundary(multiLine, BoundaryNodeRule.Endpoint);
        Point[] points = BoundaryPoints(boundary);
        first.Coordinates[1].Z = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(points, Has.Length.EqualTo(3));
            Assert.That(points[1].Coordinate!.X, Is.EqualTo(1.0));
            Assert.That(points[1].Coordinate!.Z, Is.EqualTo(3.0));
            Assert.That(points[1].Coordinate!.M, Is.EqualTo(4.0));
            Assert.That(boundary.CoordinateLayout, Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(boundary.SpatialReferenceId, Is.EqualTo(4326));
        });
    }

    [Test]
    public void PolygonBoundaryReturnsShellAndHoleLineComponentsAsSnapshots()
    {
        GeometryInspector inspector = new GeometryInspector();
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions { SpatialReferenceId = 4326 });
        LinearRing shell = RingValue(0.0, 0.0, 10.0, 10.0);
        LinearRing hole = RingValue(2.0, 2.0, 4.0, 4.0);
        Geometry polygon = factory.CreatePolygon(shell, [hole]);

        Geometry boundary = inspector.GetBoundary(polygon, BoundaryNodeRule.Mod2);
        shell.Coordinates[0].X = 100.0;
        polygon.Polygon!.Shell.Coordinates[0].X = 200.0;

        Assert.Multiple(() =>
        {
            Assert.That(boundary.GeometryType, Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(boundary.MultiLineString!.LineStrings, Has.Length.EqualTo(2));
            Assert.That(boundary.MultiLineString.LineStrings[0].Coordinates[0].X, Is.EqualTo(0.0));
            Assert.That(boundary.MultiLineString.LineStrings[1].Coordinates[0].X, Is.EqualTo(2.0));
            Assert.That(boundary.SpatialReferenceId, Is.EqualTo(4326));
            Assert.That(inspector.IsClosed(new Geometry
            {
                GeometryType = GeometryType.LineString,
                Point = null,
                SpatialReferenceId = 4326,
                LineString = boundary.MultiLineString.LineStrings[0]
            }), Is.True);
        });
    }

    [Test]
    public void MultiPolygonBoundaryAggregatesAllNonEmptyRingsInComponentOrder()
    {
        GeometryInspector inspector = new GeometryInspector();
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions());
        Polygon first = new Polygon
        {
            Shell = RingValue(0.0, 0.0, 5.0, 5.0),
            Holes = [RingValue(1.0, 1.0, 2.0, 2.0)]
        };
        Polygon empty = new Polygon();
        Polygon second = new Polygon { Shell = RingValue(10.0, 10.0, 12.0, 12.0) };
        Geometry multiPolygon = factory.CreateMultiPolygon([first, empty, second]);

        Geometry boundary = inspector.GetBoundary(multiPolygon, BoundaryNodeRule.MonovalentEndpoint);

        Assert.Multiple(() =>
        {
            Assert.That(boundary.GeometryType, Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(boundary.MultiLineString!.LineStrings, Has.Length.EqualTo(3));
            Assert.That(boundary.MultiLineString.LineStrings[0].Coordinates[0].X, Is.EqualTo(0.0));
            Assert.That(boundary.MultiLineString.LineStrings[1].Coordinates[0].X, Is.EqualTo(1.0));
            Assert.That(boundary.MultiLineString.LineStrings[2].Coordinates[0].X, Is.EqualTo(10.0));
        });
    }

    [Test]
    public void EmptyCoreBoundariesUseDimensionAppropriateCanonicalTypes()
    {
        GeometryInspector inspector = new GeometryInspector();
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions());

        Assert.Multiple(() =>
        {
            Assert.That(inspector.GetBoundary(factory.CreateEmptyGeometry(GeometryType.Point), BoundaryNodeRule.Mod2).GeometryType, Is.EqualTo(GeometryType.GeometryCollection));
            Assert.That(inspector.GetBoundary(factory.CreateEmptyGeometry(GeometryType.MultiPoint), BoundaryNodeRule.Mod2).GeometryType, Is.EqualTo(GeometryType.GeometryCollection));
            Assert.That(inspector.GetBoundary(factory.CreateEmptyGeometry(GeometryType.LineString), BoundaryNodeRule.Mod2).GeometryType, Is.EqualTo(GeometryType.MultiPoint));
            Assert.That(inspector.GetBoundary(factory.CreateEmptyGeometry(GeometryType.MultiLineString), BoundaryNodeRule.Mod2).GeometryType, Is.EqualTo(GeometryType.MultiPoint));
            Assert.That(inspector.GetBoundary(factory.CreateEmptyGeometry(GeometryType.Polygon), BoundaryNodeRule.Mod2).GeometryType, Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(inspector.GetBoundary(factory.CreateEmptyGeometry(GeometryType.MultiPolygon), BoundaryNodeRule.Mod2).GeometryType, Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(inspector.GetBoundary(factory.CreateEmptyGeometry(GeometryType.GeometryCollection), BoundaryNodeRule.Mod2).GeometryType, Is.EqualTo(GeometryType.GeometryCollection));
        });
    }

    [Test]
    public void LinearRingBoundaryUsesEndpointValenceRules()
    {
        GeometryInspector inspector = new GeometryInspector();
        LinearRing ring = RingValue(0.0, 0.0, 3.0, 3.0);
        Geometry geometry = new Geometry
        {
            GeometryType = GeometryType.LinearRing,
            Point = null,
            LinearRing = ring
        };

        Assert.Multiple(() =>
        {
            Assert.That(BoundaryPoints(inspector.GetBoundary(geometry, BoundaryNodeRule.Mod2)), Is.Empty);
            Assert.That(BoundaryPoints(inspector.GetBoundary(geometry, BoundaryNodeRule.Endpoint)), Has.Length.EqualTo(1));
            Assert.That(BoundaryPoints(inspector.GetBoundary(geometry, BoundaryNodeRule.MultivalentEndpoint)), Has.Length.EqualTo(1));
            Assert.That(BoundaryPoints(inspector.GetBoundary(geometry, BoundaryNodeRule.MonovalentEndpoint)), Is.Empty);
        });
    }

    [Test]
    public void BoundaryRejectsUndefinedCollectionRuleAndMalformedInputs()
    {
        GeometryInspector inspector = new GeometryInspector();
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions());
        Geometry collection = factory.CreateGeometryCollection([
            factory.CreatePoint(CoordinateValue(0.0, 0.0, double.NaN, double.NaN))
        ]);
        Geometry malformedLine = new Geometry
        {
            GeometryType = GeometryType.LineString,
            Point = null,
            LineString = new LineString { Coordinates = [CoordinateValue(0.0, 0.0, double.NaN, double.NaN)] }
        };
        Geometry malformedPolygon = factory.CreateEmptyGeometry(GeometryType.Polygon);
        malformedPolygon.Polygon!.Holes = [null!];

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() => inspector.GetBoundary(collection, BoundaryNodeRule.Mod2));
            Assert.Throws<InvalidGeometryException>(() => inspector.GetBoundary(malformedLine, BoundaryNodeRule.Mod2));
            Assert.Throws<InvalidGeometryException>(() => inspector.GetBoundary(malformedPolygon, BoundaryNodeRule.Mod2));
            Assert.Throws<InvalidGeometryException>(() => inspector.GetBoundary(factory.CreatePoint(null), (BoundaryNodeRule)99));
            Assert.Throws<InvalidGeometryException>(() => inspector.GetBoundary(null!, BoundaryNodeRule.Mod2));
        });
    }

    private static Point[] BoundaryPoints(Geometry boundary)
    {
        Assert.That(boundary.GeometryType, Is.EqualTo(GeometryType.MultiPoint));
        return boundary.MultiPoint!.Points;
    }

    private static Coordinate CoordinateValue(double x, double y, double z, double m)
    {
        return new Coordinate { X = x, Y = y, Z = z, M = m };
    }

    private static LineString LineValue(double startX, double startY, double endX, double endY)
    {
        return new LineString
        {
            Coordinates =
            [
                CoordinateValue(startX, startY, double.NaN, double.NaN),
                CoordinateValue(endX, endY, double.NaN, double.NaN)
            ]
        };
    }

    private static LinearRing RingValue(double minimumX, double minimumY, double maximumX, double maximumY)
    {
        Coordinate first = CoordinateValue(minimumX, minimumY, double.NaN, double.NaN);
        return new LinearRing
        {
            Coordinates =
            [
                first,
                CoordinateValue(maximumX, minimumY, double.NaN, double.NaN),
                CoordinateValue(maximumX, maximumY, double.NaN, double.NaN),
                CoordinateValue(minimumX, maximumY, double.NaN, double.NaN),
                first
            ]
        };
    }
}
