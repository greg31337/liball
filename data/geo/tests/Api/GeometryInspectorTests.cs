using geo;

namespace geo.Tests;

public class GeometryInspectorTests
{
    [Test]
    public void MetadataAndDimensionReflectValidatedGeometryValues()
    {
        IGeometryInspector inspector = new GeometryInspector();
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions
        {
            CoordinateLayout = CoordinateLayout.XYZ,
            SpatialReferenceId = 4979
        });
        Geometry point = factory.CreatePoint(CoordinateValue(1.0, 2.0, 3.0));
        Geometry line = factory.CreateLineString([CoordinateValue(0.0, 0.0, 3.0), CoordinateValue(2.0, 2.0, 3.0)]);
        Geometry polygon = factory.CreateRectangle(CoordinateValue(0.0, 0.0, 3.0), CoordinateValue(4.0, 4.0, 3.0));
        Geometry mixed = factory.CreateGeometryCollection([point, line, polygon]);
        Geometry empty = factory.CreateEmptyGeometry(GeometryType.Polygon);

        Assert.Multiple(() =>
        {
            Assert.That(inspector.GetGeometryType(point), Is.EqualTo(GeometryType.Point));
            Assert.That(inspector.GetCoordinateLayout(point), Is.EqualTo(CoordinateLayout.XYZ));
            Assert.That(inspector.GetSpatialReferenceId(point), Is.EqualTo(4979));
            Assert.That(inspector.GetTopologicalDimension(point), Is.EqualTo(TopologicalDimension.Point));
            Assert.That(inspector.GetTopologicalDimension(line), Is.EqualTo(TopologicalDimension.Curve));
            Assert.That(inspector.GetTopologicalDimension(polygon), Is.EqualTo(TopologicalDimension.Surface));
            Assert.That(inspector.GetTopologicalDimension(mixed), Is.EqualTo(TopologicalDimension.Surface));
            Assert.That(inspector.GetTopologicalDimension(empty), Is.EqualTo(TopologicalDimension.Empty));
        });
    }

    [Test]
    public void EveryGeometryTagReportsCanonicalEmptyStateAndCounts()
    {
        GeometryInspector inspector = new GeometryInspector();
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions());
        GeometryType[] types = AllGeometryTypes();
        foreach (GeometryType type in types)
        {
            Geometry geometry = factory.CreateEmptyGeometry(type);
            Assert.Multiple(() =>
            {
                Assert.That(inspector.IsEmpty(geometry), Is.True, type.ToString());
                Assert.That(inspector.GetTopologicalDimension(geometry), Is.EqualTo(TopologicalDimension.Empty), type.ToString());
                Assert.That(inspector.CountCoordinates(geometry), Is.EqualTo(0), type.ToString());
                Assert.That(inspector.GetStartPoint(geometry), Is.Null, type.ToString());
                Assert.That(inspector.GetEndPoint(geometry), Is.Null, type.ToString());
                Assert.That(inspector.IsClosed(geometry), Is.False, type.ToString());
                Assert.That(inspector.IsRing(geometry), Is.False, type.ToString());
                Assert.That(inspector.IsRectangle(geometry), Is.False, type.ToString());
            });

            int expectedComponents = IsContainerType(type) ? 0 : 1;
            Assert.That(inspector.CountComponents(geometry), Is.EqualTo(expectedComponents), type.ToString());
        }
    }

    [Test]
    public void ClosureAndRingInspectionHandleAtomicMultiAndCollectionCurves()
    {
        GeometryInspector inspector = new GeometryInspector();
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions());
        Geometry open = factory.CreateLineString([
            CoordinateValue(0.0, 0.0, double.NaN),
            CoordinateValue(2.0, 0.0, double.NaN)
        ]);
        Geometry closed = factory.CreateLineString([
            CoordinateValue(0.0, 0.0, double.NaN),
            CoordinateValue(2.0, 0.0, double.NaN),
            CoordinateValue(1.0, 2.0, double.NaN),
            CoordinateValue(0.0, 0.0, double.NaN)
        ]);
        Geometry collapsedClosed = factory.CreateLineString([
            CoordinateValue(0.0, 0.0, double.NaN),
            CoordinateValue(1.0, 0.0, double.NaN),
            CoordinateValue(1.0, 0.0, double.NaN),
            CoordinateValue(0.0, 0.0, double.NaN)
        ]);
        Geometry ring = new Geometry
        {
            GeometryType = GeometryType.LinearRing,
            Point = null,
            LinearRing = new LinearRing { Coordinates = closed.LineString!.Coordinates }
        };
        Geometry polygon = factory.CreateEnvelopePolygon(new Envelope { MinX = 0.0, MinY = 0.0, MaxX = 2.0, MaxY = 2.0 });
        Geometry allClosed = factory.CreateMultiLineString([closed.LineString]);
        Geometry notAllClosed = factory.CreateMultiLineString([closed.LineString, open.LineString!]);
        Geometry collection = factory.CreateGeometryCollection([closed, polygon]);

        Assert.Multiple(() =>
        {
            Assert.That(inspector.IsClosed(open), Is.False);
            Assert.That(inspector.IsRing(open), Is.False);
            Assert.That(inspector.IsClosed(closed), Is.True);
            Assert.That(inspector.IsRing(closed), Is.True);
            Assert.That(inspector.IsClosed(collapsedClosed), Is.True);
            Assert.That(inspector.IsRing(collapsedClosed), Is.False);
            Assert.That(inspector.IsClosed(ring), Is.True);
            Assert.That(inspector.IsRing(ring), Is.True);
            Assert.That(inspector.IsClosed(polygon), Is.True);
            Assert.That(inspector.IsRing(polygon), Is.False);
            Assert.That(inspector.IsClosed(allClosed), Is.True);
            Assert.That(inspector.IsClosed(notAllClosed), Is.False);
            Assert.That(inspector.IsClosed(collection), Is.True);
            Assert.That(inspector.IsClosed(factory.CreatePoint(CoordinateValue(0.0, 0.0, double.NaN))), Is.False);
        });
    }

    [Test]
    public void RectangleInspectionAcceptsEitherOrientationAndRejectsOtherPolygons()
    {
        GeometryInspector inspector = new GeometryInspector();
        GeometryFactory counterClockwiseFactory = new GeometryFactory(new GeometryFactoryOptions
        {
            RingOrientation = RingOrientation.CounterClockwise
        });
        GeometryFactory clockwiseFactory = new GeometryFactory(new GeometryFactoryOptions
        {
            RingOrientation = RingOrientation.Clockwise
        });
        Geometry counterClockwise = counterClockwiseFactory.CreateEnvelopePolygon(new Envelope { MinX = 0.0, MinY = 1.0, MaxX = 4.0, MaxY = 5.0 });
        Geometry clockwise = clockwiseFactory.CreateEnvelopePolygon(new Envelope { MinX = 0.0, MinY = 1.0, MaxX = 4.0, MaxY = 5.0 });
        LinearRing trapezoid = new LinearRing
        {
            Coordinates =
            [
                CoordinateValue(0.0, 0.0, double.NaN),
                CoordinateValue(4.0, 0.0, double.NaN),
                CoordinateValue(3.0, 3.0, double.NaN),
                CoordinateValue(0.0, 3.0, double.NaN),
                CoordinateValue(0.0, 0.0, double.NaN)
            ]
        };
        Geometry nonRectangle = counterClockwiseFactory.CreatePolygon(trapezoid, []);
        Geometry withHole = counterClockwiseFactory.CreatePolygon(
            counterClockwise.Polygon!.Shell,
            [RingValue(1.0, 2.0, 2.0, 3.0)]);

        Assert.Multiple(() =>
        {
            Assert.That(inspector.IsRectangle(counterClockwise), Is.True);
            Assert.That(inspector.IsRectangle(clockwise), Is.True);
            Assert.That(inspector.IsRectangle(nonRectangle), Is.False);
            Assert.That(inspector.IsRectangle(withHole), Is.False);
            Assert.That(inspector.IsRectangle(counterClockwiseFactory.CreateLineString([CoordinateValue(0.0, 0.0, double.NaN), CoordinateValue(1.0, 1.0, double.NaN)])), Is.False);
        });
    }

    [Test]
    public void CoordinateAccessCountsTraversalOrderAndReturnsOwningCopies()
    {
        GeometryInspector inspector = new GeometryInspector();
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions { CoordinateLayout = CoordinateLayout.XYZM });
        Geometry point = factory.CreatePoint(CoordinateValue(1.0, 1.0, 2.0, 3.0));
        Geometry line = factory.CreateLineString([
            CoordinateValue(2.0, 2.0, 4.0, 5.0),
            CoordinateValue(3.0, 3.0, 6.0, 7.0)
        ]);
        Geometry polygon = factory.CreateRectangle(
            CoordinateValue(0.0, 0.0, 8.0, 9.0),
            CoordinateValue(4.0, 4.0, 8.0, 9.0));
        Geometry collection = factory.CreateGeometryCollection([point, line, polygon]);

        Coordinate[] extracted = inspector.ExtractCoordinates(collection);
        Coordinate atTwo = inspector.GetPointAt(collection, 2);
        Coordinate start = inspector.GetStartPoint(collection)!;
        Coordinate end = inspector.GetEndPoint(collection)!;
        extracted[0].X = 100.0;
        atTwo.X = 100.0;
        start.X = 100.0;
        end.X = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(inspector.CountCoordinates(collection), Is.EqualTo(8));
            Assert.That(extracted, Has.Length.EqualTo(8));
            Assert.That(extracted[0].X, Is.EqualTo(100.0));
            Assert.That(inspector.GetPointAt(collection, 0).X, Is.EqualTo(1.0));
            Assert.That(inspector.GetPointAt(collection, 1).X, Is.EqualTo(2.0));
            Assert.That(inspector.GetPointAt(collection, 2).X, Is.EqualTo(3.0));
            Assert.That(inspector.GetStartPoint(collection)!.X, Is.EqualTo(1.0));
            Assert.That(inspector.GetEndPoint(collection)!.X, Is.EqualTo(0.0));
            Assert.That(inspector.GetPointAt(collection, 2).Z, Is.EqualTo(6.0));
            Assert.That(inspector.GetPointAt(collection, 2).M, Is.EqualTo(7.0));
            Assert.Throws<InvalidGeometryException>(() => inspector.GetPointAt(collection, -1));
            Assert.Throws<InvalidGeometryException>(() => inspector.GetPointAt(collection, 8));
        });
    }

    [Test]
    public void UniqueCoordinateExtractionUsesAllOrdinatesAndStableFirstOccurrenceOrder()
    {
        GeometryInspector inspector = new GeometryInspector();
        Geometry geometry = new Geometry
        {
            CoordinateLayout = CoordinateLayout.XYZM,
            GeometryType = GeometryType.LineString,
            Point = null,
            LineString = new LineString
            {
                Coordinates =
                [
                    CoordinateValue(0.0, 0.0, 1.0, 2.0),
                    CoordinateValue(-0.0, 0.0, 1.0, 2.0),
                    CoordinateValue(0.0, 0.0, 1.0, 3.0),
                    CoordinateValue(2.0, 2.0, 4.0, 5.0),
                    CoordinateValue(0.0, 0.0, 1.0, 2.0)
                ]
            }
        };

        Coordinate[] unique = inspector.ExtractUniqueCoordinates(geometry);
        geometry.LineString!.Coordinates[0].X = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(unique, Has.Length.EqualTo(3));
            Assert.That(unique[0].X, Is.EqualTo(0.0));
            Assert.That(unique[0].M, Is.EqualTo(2.0));
            Assert.That(unique[1].M, Is.EqualTo(3.0));
            Assert.That(unique[2].X, Is.EqualTo(2.0));
        });
    }

    [Test]
    public void ComponentAccessUsesDirectComponentsAndReturnsDeepCopies()
    {
        GeometryInspector inspector = new GeometryInspector();
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions { SpatialReferenceId = 4326 });
        Geometry point = factory.CreatePoint(CoordinateValue(1.0, 2.0, double.NaN));
        Geometry line = factory.CreateLineString([CoordinateValue(3.0, 4.0, double.NaN), CoordinateValue(5.0, 6.0, double.NaN)]);
        Geometry collection = factory.CreateGeometryCollection([point, line]);

        Geometry pointCopy = inspector.GetComponentAt(collection, 0);
        Geometry lineCopy = inspector.GetComponentAt(collection, 1);
        Geometry atomicCopy = inspector.GetComponentAt(point, 0);
        point.Point!.Coordinate!.X = 100.0;
        line.LineString!.Coordinates[0].X = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(inspector.CountComponents(collection), Is.EqualTo(2));
            Assert.That(inspector.CountComponents(point), Is.EqualTo(1));
            Assert.That(pointCopy.Point!.Coordinate!.X, Is.EqualTo(1.0));
            Assert.That(lineCopy.LineString!.Coordinates[0].X, Is.EqualTo(3.0));
            Assert.That(atomicCopy.Point!.Coordinate!.X, Is.EqualTo(1.0));
            Assert.That(pointCopy, Is.Not.SameAs(point));
            Assert.Throws<InvalidGeometryException>(() => inspector.GetComponentAt(collection, -1));
            Assert.Throws<InvalidGeometryException>(() => inspector.GetComponentAt(collection, 2));
            Assert.Throws<InvalidGeometryException>(() => inspector.GetComponentAt(point, 1));
        });
    }

    [Test]
    public void EnvelopeInspectionReturnsIndependentBoundsForNestedValues()
    {
        GeometryInspector inspector = new GeometryInspector();
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions());
        Geometry collection = factory.CreateGeometryCollection([
            factory.CreatePoint(CoordinateValue(-3.0, 7.0, double.NaN)),
            factory.CreateEnvelopePolygon(new Envelope { MinX = 1.0, MinY = -5.0, MaxX = 9.0, MaxY = 4.0 })
        ]);

        Envelope first = inspector.GetEnvelope(collection);
        first.MinX = 100.0;
        Envelope second = inspector.GetEnvelope(collection);

        Assert.Multiple(() =>
        {
            Assert.That(second.MinX, Is.EqualTo(-3.0));
            Assert.That(second.MinY, Is.EqualTo(-5.0));
            Assert.That(second.MaxX, Is.EqualTo(9.0));
            Assert.That(second.MaxY, Is.EqualTo(7.0));
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(EnvelopeAlgorithms.ValidateAndCheckEmpty(inspector.GetEnvelope(factory.CreatePoint(null))), Is.True);
        });
    }

    [Test]
    public void InspectorRejectsInvalidTagsMetadataComponentsCoordinatesAndCycles()
    {
        GeometryInspector inspector = new GeometryInspector();
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions());
        Geometry mismatch = new Geometry { GeometryType = GeometryType.LineString };
        Geometry invalidLayout = new Geometry { CoordinateLayout = (CoordinateLayout)99 };
        Geometry invalidSrid = new Geometry { SpatialReferenceId = -1 };
        Geometry invalidCoordinate = factory.CreateLineString([CoordinateValue(0.0, 0.0, double.NaN), CoordinateValue(1.0, 1.0, double.NaN)]);
        invalidCoordinate.LineString!.Coordinates[0].X = double.NaN;
        Geometry nullComponent = factory.CreateEmptyGeometry(GeometryType.MultiPoint);
        nullComponent.MultiPoint!.Points = [null!];
        Geometry child = factory.CreatePoint(CoordinateValue(1.0, 1.0, double.NaN));
        Geometry inconsistent = factory.CreateGeometryCollection([child]);
        inconsistent.GeometryCollection!.Geometries[0].SpatialReferenceId = 4326;
        Geometry cyclic = factory.CreateEmptyGeometry(GeometryType.GeometryCollection);
        cyclic.GeometryCollection!.Geometries = [cyclic];

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() => inspector.GetGeometryType(null!));
            Assert.Throws<InvalidGeometryException>(() => inspector.GetGeometryType(mismatch));
            Assert.Throws<InvalidGeometryException>(() => inspector.GetCoordinateLayout(invalidLayout));
            Assert.Throws<InvalidGeometryException>(() => inspector.GetSpatialReferenceId(invalidSrid));
            Assert.Throws<InvalidGeometryException>(() => inspector.ExtractCoordinates(invalidCoordinate));
            Assert.Throws<InvalidGeometryException>(() => inspector.CountComponents(nullComponent));
            Assert.Throws<InvalidGeometryException>(() => inspector.CountCoordinates(inconsistent));
            Assert.Throws<InvalidGeometryException>(() => inspector.IsEmpty(cyclic));
        });
    }

    private static Coordinate CoordinateValue(double x, double y, double z)
    {
        return new Coordinate { X = x, Y = y, Z = z };
    }

    private static Coordinate CoordinateValue(double x, double y, double z, double m)
    {
        return new Coordinate { X = x, Y = y, Z = z, M = m };
    }

    private static LinearRing RingValue(double minimumX, double minimumY, double maximumX, double maximumY)
    {
        Coordinate first = CoordinateValue(minimumX, minimumY, double.NaN);
        return new LinearRing
        {
            Coordinates =
            [
                first,
                CoordinateValue(maximumX, minimumY, double.NaN),
                CoordinateValue(maximumX, maximumY, double.NaN),
                CoordinateValue(minimumX, maximumY, double.NaN),
                first
            ]
        };
    }

    private static GeometryType[] AllGeometryTypes()
    {
        return
        [
            GeometryType.Point,
            GeometryType.LineString,
            GeometryType.LinearRing,
            GeometryType.Polygon,
            GeometryType.MultiPoint,
            GeometryType.MultiLineString,
            GeometryType.MultiPolygon,
            GeometryType.GeometryCollection,
            GeometryType.CircularString,
            GeometryType.CompoundCurve,
            GeometryType.CurvePolygon,
            GeometryType.MultiCurve,
            GeometryType.MultiSurface,
            GeometryType.PolyhedralSurface,
            GeometryType.Tin,
            GeometryType.Triangle
        ];
    }

    private static bool IsContainerType(GeometryType type)
    {
        switch (type)
        {
            case GeometryType.MultiPoint:
            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
            case GeometryType.GeometryCollection:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            case GeometryType.MultiCurve:
            case GeometryType.MultiSurface:
            case GeometryType.PolyhedralSurface:
            case GeometryType.Tin:
                return true;
            default:
                return false;
        }
    }
}
