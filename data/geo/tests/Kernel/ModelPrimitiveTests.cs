using geo;

namespace geo.Tests;

public class ModelPrimitiveTests
{
    [Test]
    public void TagDispatchReturnsSelectedPayloadDimensionsAndRejectsMismatches()
    {
        Geometry point = CreateEmptyGeometry(GeometryType.Point);
        Geometry line = CreateEmptyGeometry(GeometryType.LineString);
        Geometry polygon = CreateEmptyGeometry(GeometryType.Polygon);
        Geometry collection = CreateEmptyGeometry(GeometryType.GeometryCollection);
        Geometry mismatch = new Geometry { GeometryType = GeometryType.LineString };
        Geometry multiple = new Geometry
        {
            GeometryType = GeometryType.LineString,
            LineString = new LineString()
        };

        Assert.Multiple(() =>
        {
            Assert.That(GeometryDispatch.GetSelectedPayload(point), Is.SameAs(point.Point));
            Assert.That(GeometryDispatch.GetSelectedPayload(line), Is.SameAs(line.LineString));
            Assert.That(GeometryDispatch.GetSelectedPayload(polygon), Is.SameAs(polygon.Polygon));
            Assert.That(GeometryDispatch.GetSelectedPayload(collection), Is.SameAs(collection.GeometryCollection));
            Assert.That(GeometryDispatch.GetDeclaredDimension(point), Is.EqualTo(TopologicalDimension.Point));
            Assert.That(GeometryDispatch.GetDeclaredDimension(line), Is.EqualTo(TopologicalDimension.Curve));
            Assert.That(GeometryDispatch.GetDeclaredDimension(polygon), Is.EqualTo(TopologicalDimension.Surface));
            Assert.That(GeometryDispatch.GetDeclaredDimension(collection), Is.EqualTo(TopologicalDimension.Empty));
            Assert.That(GeometryDispatch.CountPopulatedPayloads(multiple), Is.EqualTo(2));
        });
        Assert.Throws<InvalidGeometryException>(() => GeometryDispatch.GetSelectedPayload(mismatch));
        Assert.Throws<InvalidGeometryException>(() => GeometryDispatch.GetSelectedPayload(multiple));
    }

    [Test]
    public void CycleGuardRejectsCyclesAndEnforcesDepthAndComponentBounds()
    {
        Geometry point = CreateEmptyGeometry(GeometryType.Point);
        Geometry inner = CreateEmptyGeometry(GeometryType.GeometryCollection);
        inner.GeometryCollection!.Geometries = new[] { point };
        Geometry outer = CreateEmptyGeometry(GeometryType.GeometryCollection);
        outer.GeometryCollection!.Geometries = new[] { inner };
        Geometry cyclic = CreateEmptyGeometry(GeometryType.GeometryCollection);
        cyclic.GeometryCollection!.Geometries = new[] { cyclic };

        Assert.DoesNotThrow(() => GeometryCycleGuard.EnsureAcyclic(outer, 2, 3));
        Assert.Throws<InvalidGeometryException>(() => GeometryCycleGuard.EnsureAcyclic(cyclic, 8, 8));
        Assert.Throws<InvalidGeometryException>(() => GeometryCycleGuard.EnsureAcyclic(outer, 1, 3));
        Assert.Throws<InvalidGeometryException>(() => GeometryCycleGuard.EnsureAcyclic(outer, 2, 2));
        Assert.Throws<InvalidGeometryException>(() => GeometryCycleGuard.EnsureAcyclic(outer, -1, 3));
        Assert.Throws<InvalidGeometryException>(() => GeometryCycleGuard.EnsureAcyclic(outer, 2, 0));
    }

    [Test]
    public void ComponentWalkerFlattensNestedAndHomogeneousComponentsInSourceOrder()
    {
        Point firstPoint = new Point { Coordinate = new Coordinate { X = 1.0, Y = 1.0, Z = 2.0, M = 3.0 } };
        Point secondPoint = new Point { Coordinate = new Coordinate { X = 2.0, Y = 2.0, Z = 4.0, M = 5.0 } };
        Geometry multiPoint = CreateEmptyGeometry(GeometryType.MultiPoint);
        multiPoint.CoordinateLayout = CoordinateLayout.XYZM;
        multiPoint.SpatialReferenceId = 4326;
        multiPoint.MultiPoint!.Points = new[] { firstPoint, secondPoint };
        Geometry line = CreateEmptyGeometry(GeometryType.LineString);
        line.LineString!.Coordinates = new[]
        {
            new Coordinate { X = 3.0, Y = 3.0 },
            new Coordinate { X = 4.0, Y = 4.0 }
        };
        Geometry nested = CreateEmptyGeometry(GeometryType.GeometryCollection);
        nested.GeometryCollection!.Geometries = new[] { multiPoint, line };

        Geometry[] direct = ComponentWalker.GetDirectChildren(multiPoint);
        Geometry[] leaves = ComponentWalker.FlattenLeafGeometries(nested, 4, 8);

        Assert.Multiple(() =>
        {
            Assert.That(direct, Has.Length.EqualTo(2));
            Assert.That(direct[0].GeometryType, Is.EqualTo(GeometryType.Point));
            Assert.That(direct[0].Point, Is.SameAs(firstPoint));
            Assert.That(direct[0].CoordinateLayout, Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(direct[0].SpatialReferenceId, Is.EqualTo(4326));
            Assert.That(direct[1].Point, Is.SameAs(secondPoint));
            Assert.That(leaves, Has.Length.EqualTo(3));
            Assert.That(leaves[0].Point, Is.SameAs(firstPoint));
            Assert.That(leaves[1].Point, Is.SameAs(secondPoint));
            Assert.That(leaves[2], Is.SameAs(line));
        });
    }

    [Test]
    public void CoordinateSequenceAccessPreservesOrderAndCopiesEveryOrdinate()
    {
        Coordinate shellStart = new Coordinate { X = 0.0, Y = 0.0, Z = 1.0, M = 10.0 };
        Coordinate shellSecond = new Coordinate { X = 5.0, Y = 0.0, Z = 2.0, M = 20.0 };
        Coordinate holeStart = new Coordinate { X = 1.0, Y = 1.0, Z = 3.0, M = 30.0 };
        Polygon polygonPayload = new Polygon
        {
            Shell = new LinearRing { Coordinates = new[] { shellStart, shellSecond, shellStart } },
            Holes = new[] { new LinearRing { Coordinates = new[] { holeStart, holeStart } } }
        };
        Geometry polygon = CreateEmptyGeometry(GeometryType.Polygon);
        polygon.Polygon = polygonPayload;
        Geometry point = CreateEmptyGeometry(GeometryType.Point);
        point.Point!.Coordinate = new Coordinate { X = 9.0, Y = 9.0 };
        Geometry collection = CreateEmptyGeometry(GeometryType.GeometryCollection);
        collection.GeometryCollection!.Geometries = new[] { polygon, point };

        Coordinate[] extracted = CoordinateSequences.ExtractCoordinates(collection, 4, 8);
        Coordinate[] copied = CoordinateSequences.CopyCoordinateSequence(extracted);
        Coordinate[] directPointCoordinates = CoordinateSequences.GetDirectCoordinates(point);

        shellStart.X = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(extracted, Has.Length.EqualTo(6));
            Assert.That(extracted[0], Is.SameAs(shellStart));
            Assert.That(extracted[1], Is.SameAs(shellSecond));
            Assert.That(extracted[3], Is.SameAs(holeStart));
            Assert.That(extracted[5], Is.SameAs(point.Point.Coordinate));
            Assert.That(directPointCoordinates, Has.Length.EqualTo(1));
            Assert.That(directPointCoordinates[0], Is.SameAs(point.Point.Coordinate));
            Assert.That(copied, Is.Not.SameAs(extracted));
            Assert.That(copied[0], Is.Not.SameAs(shellStart));
            Assert.That(copied[0].X, Is.EqualTo(0.0));
            Assert.That(copied[0].Y, Is.EqualTo(0.0));
            Assert.That(copied[0].Z, Is.EqualTo(1.0));
            Assert.That(copied[0].M, Is.EqualTo(10.0));
        });
    }

    [Test]
    public void DeepCopySupportsEveryGeometryKindAndPreservesMetadata()
    {
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
            GeometryType.Triangle,
            GeometryType.Tin,
            GeometryType.PolyhedralSurface,
            GeometryType.CircularString,
            GeometryType.CompoundCurve,
            GeometryType.CurvePolygon,
            GeometryType.MultiCurve,
            GeometryType.MultiSurface
        };

        foreach (GeometryType type in types)
        {
            Geometry source = CreateEmptyGeometry(type);
            source.CoordinateLayout = CoordinateLayout.XYM;
            source.SpatialReferenceId = 3857;
            Geometry copy = GeometryCopy.CopyGeometry(source);

            Assert.Multiple(() =>
            {
                Assert.That(copy, Is.Not.SameAs(source));
                Assert.That(copy.GeometryType, Is.EqualTo(type));
                Assert.That(copy.CoordinateLayout, Is.EqualTo(CoordinateLayout.XYM));
                Assert.That(copy.SpatialReferenceId, Is.EqualTo(3857));
                Assert.That(GeometryDispatch.CountPopulatedPayloads(copy), Is.EqualTo(1));
                Assert.That(GeometryDispatch.GetSelectedPayload(copy), Is.Not.SameAs(GeometryDispatch.GetSelectedPayload(source)));
            });
        }
    }

    [Test]
    public void DeepCopyIsolatesNestedArraysCoordinatesAndCurvedComponents()
    {
        Coordinate sourceCoordinate = new Coordinate { X = 1.0, Y = 2.0, Z = 3.0, M = 4.0 };
        Geometry circular = CreateEmptyGeometry(GeometryType.CircularString);
        circular.CoordinateLayout = CoordinateLayout.XYZM;
        circular.CircularString!.Coordinates = new[]
        {
            sourceCoordinate,
            new Coordinate { X = 2.0, Y = 3.0, Z = 4.0, M = 5.0 },
            new Coordinate { X = 3.0, Y = 2.0, Z = 5.0, M = 6.0 }
        };
        Geometry compound = CreateEmptyGeometry(GeometryType.CompoundCurve);
        compound.CoordinateLayout = CoordinateLayout.XYZM;
        compound.CompoundCurve!.Curves = new[] { circular };
        Geometry root = CreateEmptyGeometry(GeometryType.GeometryCollection);
        root.CoordinateLayout = CoordinateLayout.XYZM;
        root.SpatialReferenceId = 4979;
        root.GeometryCollection!.Geometries = new[] { compound };

        Geometry copy = GeometryCopy.CopyGeometry(root);
        Geometry copiedCompound = copy.GeometryCollection!.Geometries[0];
        Geometry copiedCircular = copiedCompound.CompoundCurve!.Curves[0];
        Coordinate copiedCoordinate = copiedCircular.CircularString!.Coordinates[0];

        sourceCoordinate.X = 99.0;
        circular.CircularString.Coordinates[0] = new Coordinate { X = -1.0, Y = -1.0 };

        Assert.Multiple(() =>
        {
            Assert.That(copy.GeometryCollection, Is.Not.SameAs(root.GeometryCollection));
            Assert.That(copy.GeometryCollection.Geometries, Is.Not.SameAs(root.GeometryCollection.Geometries));
            Assert.That(copiedCompound, Is.Not.SameAs(compound));
            Assert.That(copiedCompound.CompoundCurve, Is.Not.SameAs(compound.CompoundCurve));
            Assert.That(copiedCircular, Is.Not.SameAs(circular));
            Assert.That(copiedCircular.CircularString, Is.Not.SameAs(circular.CircularString));
            Assert.That(copiedCircular.CircularString.Coordinates, Is.Not.SameAs(circular.CircularString.Coordinates));
            Assert.That(copiedCoordinate, Is.Not.SameAs(sourceCoordinate));
            Assert.That(copiedCoordinate.X, Is.EqualTo(1.0));
            Assert.That(copiedCoordinate.Z, Is.EqualTo(3.0));
            Assert.That(copiedCoordinate.M, Is.EqualTo(4.0));
            Assert.That(copy.SpatialReferenceId, Is.EqualTo(4979));
        });
    }

    [Test]
    public void DeepCopyRejectsCyclicAndMismatchedGeometryGraphs()
    {
        Geometry cyclic = CreateEmptyGeometry(GeometryType.GeometryCollection);
        cyclic.GeometryCollection!.Geometries = new[] { cyclic };
        Geometry mismatch = new Geometry { GeometryType = GeometryType.Polygon };

        Assert.Throws<InvalidGeometryException>(() => GeometryCopy.CopyGeometry(cyclic));
        Assert.Throws<InvalidGeometryException>(() => GeometryCopy.CopyGeometry(mismatch));
    }

    private static Geometry CreateEmptyGeometry(GeometryType type)
    {
        Geometry geometry = new Geometry();
        if (type == GeometryType.Point)
        {
            return geometry;
        }

        geometry.Point = null;
        geometry.GeometryType = type;
        switch (type)
        {
            case GeometryType.LineString:
                geometry.LineString = new LineString();
                break;
            case GeometryType.LinearRing:
                geometry.LinearRing = new LinearRing();
                break;
            case GeometryType.Polygon:
                geometry.Polygon = new Polygon();
                break;
            case GeometryType.MultiPoint:
                geometry.MultiPoint = new MultiPoint();
                break;
            case GeometryType.MultiLineString:
                geometry.MultiLineString = new MultiLineString();
                break;
            case GeometryType.MultiPolygon:
                geometry.MultiPolygon = new MultiPolygon();
                break;
            case GeometryType.GeometryCollection:
                geometry.GeometryCollection = new GeometryCollection();
                break;
            case GeometryType.Triangle:
                geometry.Triangle = new Triangle();
                break;
            case GeometryType.Tin:
                geometry.Tin = new Tin();
                break;
            case GeometryType.PolyhedralSurface:
                geometry.PolyhedralSurface = new PolyhedralSurface();
                break;
            case GeometryType.CircularString:
                geometry.CircularString = new CircularString();
                break;
            case GeometryType.CompoundCurve:
                geometry.CompoundCurve = new CompoundCurve();
                break;
            case GeometryType.CurvePolygon:
                geometry.CurvePolygon = new CurvePolygon();
                break;
            case GeometryType.MultiCurve:
                geometry.MultiCurve = new MultiCurve();
                break;
            case GeometryType.MultiSurface:
                geometry.MultiSurface = new MultiSurface();
                break;
            default:
                throw new AssertionException("Unsupported test geometry type");
        }

        return geometry;
    }
}
