using geo;

namespace geo.Tests;

public class GeometryFactoryTests
{
    [Test]
    public void FactorySnapshotsAndReturnsIndependentOptions()
    {
        GeometryFactoryOptions source = new GeometryFactoryOptions
        {
            SpatialReferenceId = 4326,
            CoordinateLayout = CoordinateLayout.XYM,
            PrecisionModelType = PrecisionModelType.Fixed,
            RingOrientation = RingOrientation.CounterClockwise,
            BoundaryNodeRule = BoundaryNodeRule.Endpoint,
            PrecisionScale = 10.0,
            PrecisionOffsetX = 1.0,
            PrecisionOffsetY = 2.0
        };
        GeometryFactory factory = new GeometryFactory(source);
        source.SpatialReferenceId = 3857;
        source.PrecisionScale = 1.0;

        GeometryFactoryOptions first = factory.GetFactoryOptions();
        first.SpatialReferenceId = 9999;
        first.PrecisionScale = 100.0;
        GeometryFactoryOptions second = factory.GetFactoryOptions();

        Assert.Multiple(() =>
        {
            Assert.That(second.SpatialReferenceId, Is.EqualTo(4326));
            Assert.That(second.CoordinateLayout, Is.EqualTo(CoordinateLayout.XYM));
            Assert.That(second.PrecisionModelType, Is.EqualTo(PrecisionModelType.Fixed));
            Assert.That(second.RingOrientation, Is.EqualTo(RingOrientation.CounterClockwise));
            Assert.That(second.BoundaryNodeRule, Is.EqualTo(BoundaryNodeRule.Endpoint));
            Assert.That(second.PrecisionScale, Is.EqualTo(10.0));
            Assert.That(second.PrecisionOffsetX, Is.EqualTo(1.0));
            Assert.That(second.PrecisionOffsetY, Is.EqualTo(2.0));
            Assert.That(first, Is.Not.SameAs(second));
        });
    }

    [Test]
    public void FactoryRejectsInvalidConfiguration()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<PrecisionException>(() => new GeometryFactory(null!));
            Assert.Throws<InvalidGeometryException>(() => new GeometryFactory(new GeometryFactoryOptions { SpatialReferenceId = -1 }));
            Assert.Throws<InvalidGeometryException>(() => new GeometryFactory(new GeometryFactoryOptions { CoordinateLayout = (CoordinateLayout)99 }));
            Assert.Throws<PrecisionException>(() => new GeometryFactory(new GeometryFactoryOptions { PrecisionModelType = (PrecisionModelType)99 }));
            Assert.Throws<PrecisionException>(() => new GeometryFactory(new GeometryFactoryOptions { PrecisionScale = 0.0 }));
            Assert.Throws<PrecisionException>(() => new GeometryFactory(new GeometryFactoryOptions { PrecisionOffsetX = double.PositiveInfinity }));
        });
    }

    [Test]
    public void CreateEmptyGeometrySupportsEveryGeometryKind()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions
        {
            SpatialReferenceId = 4326,
            CoordinateLayout = CoordinateLayout.XYZ
        });
        GeometryType[] types =
        {
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
        };

        foreach (GeometryType type in types)
        {
            Geometry empty = factory.CreateEmptyGeometry(type);
            Assert.Multiple(() =>
            {
                Assert.That(empty.GeometryType, Is.EqualTo(type));
                Assert.That(empty.CoordinateLayout, Is.EqualTo(CoordinateLayout.XYZ));
                Assert.That(empty.SpatialReferenceId, Is.EqualTo(4326));
                Assert.That(GeometryDispatch.CountPopulatedPayloads(empty), Is.EqualTo(1));
                Assert.That(GeometryDispatch.GetSelectedPayload(empty), Is.Not.Null);
            });
        }

        Assert.Throws<InvalidGeometryException>(() => factory.CreateEmptyGeometry((GeometryType)999));
    }

    [Test]
    public void CreatePointAppliesPrecisionPreservesExtraOrdinatesAndSnapshotsInput()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions
        {
            CoordinateLayout = CoordinateLayout.XYZM,
            PrecisionModelType = PrecisionModelType.Fixed,
            PrecisionScale = 2.0,
            PrecisionOffsetX = 0.1,
            PrecisionOffsetY = -0.1
        });
        Coordinate source = CoordinateValue(1.36, 2.14, 7.0, 8.0);

        Geometry result = factory.CreatePoint(source);
        source.X = 100.0;
        source.Z = 200.0;

        Assert.Multiple(() =>
        {
            Assert.That(result.Point, Is.Not.Null);
            Assert.That(result.Point!.Coordinate, Is.Not.Null);
            Assert.That(result.Point.Coordinate!.X, Is.EqualTo(1.6));
            Assert.That(result.Point.Coordinate.Y, Is.EqualTo(1.9));
            Assert.That(result.Point.Coordinate.Z, Is.EqualTo(7.0));
            Assert.That(result.Point.Coordinate.M, Is.EqualTo(8.0));
            Assert.That(result.Point.Coordinate, Is.Not.SameAs(source));
            Assert.That(factory.CreatePoint(null).Point!.Coordinate, Is.Null);
        });

        GeometryFactory singleFactory = new GeometryFactory(new GeometryFactoryOptions
        {
            PrecisionModelType = PrecisionModelType.FloatingSingle
        });
        double input = 1.0 / 3.0;
        Assert.That(singleFactory.CreatePoint(CoordinateValue(input, 0.0, double.NaN, double.NaN)).Point!.Coordinate!.X,
            Is.EqualTo((double)(float)input));
    }

    [Test]
    public void CreateLineStringValidatesStructureLayoutAndFiniteCoordinates()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions());
        Coordinate first = CoordinateValue(0.0, 1.0, double.NaN, double.NaN);
        Coordinate second = CoordinateValue(2.0, 3.0, double.NaN, double.NaN);
        Coordinate[] source = { first, second };

        Geometry line = factory.CreateLineString(source);
        first.X = 50.0;
        source[1] = first;

        Assert.Multiple(() =>
        {
            Assert.That(line.LineString!.Coordinates[0].X, Is.EqualTo(0.0));
            Assert.That(line.LineString.Coordinates[1].X, Is.EqualTo(2.0));
            Assert.That(line.LineString.Coordinates, Is.Not.SameAs(source));
            Assert.That(factory.CreateLineString([]).LineString!.Coordinates, Is.Empty);
            Assert.Throws<InvalidGeometryException>(() => factory.CreateLineString([CoordinateValue(0.0, 0.0, double.NaN, double.NaN)]));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateLineString([CoordinateValue(double.NaN, 0.0, double.NaN, double.NaN), second]));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateLineString([CoordinateValue(0.0, 0.0, 1.0, double.NaN), second]));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateLineString(null!));
        });
    }

    [Test]
    public void CreatePolygonValidatesAndSnapshotsRings()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions());
        LinearRing shell = RingValue(0.0, 0.0, 10.0, 10.0);
        LinearRing hole = RingValue(2.0, 2.0, 3.0, 3.0);
        LinearRing[] holes = { hole };

        Geometry polygon = factory.CreatePolygon(shell, holes);
        shell.Coordinates[0].X = 99.0;
        hole.Coordinates[0].Y = 99.0;
        holes[0] = shell;

        Assert.Multiple(() =>
        {
            Assert.That(polygon.Polygon!.Shell.Coordinates[0].X, Is.EqualTo(0.0));
            Assert.That(polygon.Polygon.Holes[0].Coordinates[0].Y, Is.EqualTo(2.0));
            Assert.That(polygon.Polygon.Shell, Is.Not.SameAs(shell));
            Assert.Throws<InvalidGeometryException>(() => factory.CreatePolygon(new LinearRing { Coordinates = [CoordinateValue(0.0, 0.0, double.NaN, double.NaN), CoordinateValue(1.0, 0.0, double.NaN, double.NaN), CoordinateValue(1.0, 1.0, double.NaN, double.NaN), CoordinateValue(0.0, 1.0, double.NaN, double.NaN)] }, []));
            Assert.Throws<InvalidGeometryException>(() => factory.CreatePolygon(new LinearRing { Coordinates = [CoordinateValue(0.0, 0.0, double.NaN, double.NaN), CoordinateValue(1.0, 0.0, double.NaN, double.NaN), CoordinateValue(0.0, 0.0, double.NaN, double.NaN), CoordinateValue(0.0, 0.0, double.NaN, double.NaN)] }, []));
            Assert.Throws<InvalidGeometryException>(() => factory.CreatePolygon(new LinearRing(), [new LinearRing()]));
            Assert.Throws<InvalidGeometryException>(() => factory.CreatePolygon(shell, null!));
        });
    }

    [Test]
    public void CreateHomogeneousCollectionsValidatesComponentsAndSnapshotsPayloads()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions());
        Point point = new Point { Coordinate = CoordinateValue(1.0, 2.0, double.NaN, double.NaN) };
        LineString line = new LineString { Coordinates = [CoordinateValue(0.0, 0.0, double.NaN, double.NaN), CoordinateValue(1.0, 1.0, double.NaN, double.NaN)] };
        Polygon polygon = new Polygon { Shell = RingValue(0.0, 0.0, 2.0, 2.0) };

        Geometry multiPoint = factory.CreateMultiPoint([point]);
        Geometry multiLine = factory.CreateMultiLineString([line]);
        Geometry multiPolygon = factory.CreateMultiPolygon([polygon]);
        point.Coordinate!.X = 50.0;
        line.Coordinates[0].X = 50.0;
        polygon.Shell.Coordinates[0].X = 50.0;

        Assert.Multiple(() =>
        {
            Assert.That(multiPoint.MultiPoint!.Points[0].Coordinate!.X, Is.EqualTo(1.0));
            Assert.That(multiLine.MultiLineString!.LineStrings[0].Coordinates[0].X, Is.EqualTo(0.0));
            Assert.That(multiPolygon.MultiPolygon!.Polygons[0].Shell.Coordinates[0].X, Is.EqualTo(0.0));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateMultiPoint([null!]));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateMultiLineString([new LineString { Coordinates = [CoordinateValue(0.0, 0.0, double.NaN, double.NaN)] }]));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateMultiPolygon([null!]));
        });
    }

    [Test]
    public void CreateGeometryCollectionRequiresCompatibleAcyclicCoreMembersAndSnapshotsThem()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions { SpatialReferenceId = 4326 });
        Geometry point = factory.CreatePoint(CoordinateValue(1.0, 2.0, double.NaN, double.NaN));
        Geometry nested = factory.CreateGeometryCollection([point]);

        Geometry result = factory.CreateGeometryCollection([nested]);
        point.Point!.Coordinate!.X = 99.0;

        Geometry nestedCopy = result.GeometryCollection!.Geometries[0];
        Geometry pointCopy = nestedCopy.GeometryCollection!.Geometries[0];
        Assert.Multiple(() =>
        {
            Assert.That(pointCopy.Point!.Coordinate!.X, Is.EqualTo(1.0));
            Assert.That(pointCopy, Is.Not.SameAs(point));
            Assert.That(nestedCopy, Is.Not.SameAs(nested));
        });

        Geometry wrongSrid = new Geometry { SpatialReferenceId = 3857 };
        Geometry extended = factory.CreateEmptyGeometry(GeometryType.CircularString);
        Geometry cyclic = factory.CreateEmptyGeometry(GeometryType.GeometryCollection);
        cyclic.GeometryCollection!.Geometries = [cyclic];
        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() => factory.CreateGeometryCollection([wrongSrid]));
            Assert.That(factory.CreateGeometryCollection([extended])
                .GeometryCollection!.Geometries[0].GeometryType,
                Is.EqualTo(GeometryType.CircularString));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateGeometryCollection([cyclic]));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateGeometryCollection([null!]));
        });
    }

    private static Coordinate CoordinateValue(double x, double y, double z, double m)
    {
        return new Coordinate { X = x, Y = y, Z = z, M = m };
    }

    private static LinearRing RingValue(double minimumX, double minimumY, double maximumX, double maximumY)
    {
        return new LinearRing
        {
            Coordinates =
            [
                CoordinateValue(minimumX, minimumY, double.NaN, double.NaN),
                CoordinateValue(maximumX, minimumY, double.NaN, double.NaN),
                CoordinateValue(maximumX, maximumY, double.NaN, double.NaN),
                CoordinateValue(minimumX, minimumY, double.NaN, double.NaN)
            ]
        };
    }
}
