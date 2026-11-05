using geo;

namespace geo.Tests;

public class FactoryModelMatrixTests
{
    [Test]
    public void EveryGeometryTagHasCanonicalFactoryEmptyFormsForEveryLayoutAndSridConvention()
    {
        CoordinateLayout[] layouts = { CoordinateLayout.XY, CoordinateLayout.XYZ, CoordinateLayout.XYM, CoordinateLayout.XYZM };
        int[] spatialReferenceIds = { GeometryConstants.UnknownSpatialReferenceId, 4326 };
        GeometryType[] types = AllGeometryTypes();

        foreach (CoordinateLayout layout in layouts)
        {
            foreach (int spatialReferenceId in spatialReferenceIds)
            {
                GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions
                {
                    CoordinateLayout = layout,
                    SpatialReferenceId = spatialReferenceId
                });
                foreach (GeometryType type in types)
                {
                    Geometry geometry = factory.CreateEmptyGeometry(type);
                    Assert.Multiple(() =>
                    {
                        Assert.That(geometry.GeometryType, Is.EqualTo(type));
                        Assert.That(geometry.CoordinateLayout, Is.EqualTo(layout));
                        Assert.That(geometry.SpatialReferenceId, Is.EqualTo(spatialReferenceId));
                        Assert.That(GeometryDispatch.CountPopulatedPayloads(geometry), Is.EqualTo(1));
                    });
                    AssertCanonicalEmptyPayload(geometry);
                }
            }
        }
    }

    [Test]
    public void EveryCoreFactoryFamilySupportsEveryCoordinateLayoutAndPropagatesSrid()
    {
        CoordinateLayout[] layouts = { CoordinateLayout.XY, CoordinateLayout.XYZ, CoordinateLayout.XYM, CoordinateLayout.XYZM };
        foreach (CoordinateLayout layout in layouts)
        {
            GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions
            {
                CoordinateLayout = layout,
                SpatialReferenceId = 4979
            });
            Coordinate first = CoordinateForLayout(layout, 0.0, 0.0);
            Coordinate second = CoordinateForLayout(layout, 4.0, 0.0);
            Coordinate third = CoordinateForLayout(layout, 4.0, 4.0);
            Coordinate fourth = CoordinateForLayout(layout, 0.0, 4.0);
            LinearRing ring = new LinearRing { Coordinates = [first, second, third, fourth, first] };
            Point pointPayload = new Point { Coordinate = first };
            LineString linePayload = new LineString { Coordinates = [first, second] };
            Polygon polygonPayload = new Polygon { Shell = ring };
            Geometry point = factory.CreatePoint(first);
            Geometry line = factory.CreateLineString([first, second]);
            Geometry polygon = factory.CreatePolygon(ring, []);
            Geometry[] geometries =
            {
                point,
                line,
                polygon,
                factory.CreateMultiPoint([pointPayload]),
                factory.CreateMultiLineString([linePayload]),
                factory.CreateMultiPolygon([polygonPayload]),
                factory.CreateGeometryCollection([point, line, polygon]),
                factory.CreateRectangle(first, third),
                factory.CreateCircle(first, 1.0, 2),
                factory.CreateRegularPolygon(first, 1.0, 5),
                factory.CreateSegment(first, second),
                factory.CreateNarrowestCollection([point, factory.CreatePoint(second)])
            };

            foreach (Geometry geometry in geometries)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(geometry.CoordinateLayout, Is.EqualTo(layout));
                    Assert.That(geometry.SpatialReferenceId, Is.EqualTo(4979));
                    Assert.That(GeometryDispatch.CountPopulatedPayloads(geometry), Is.EqualTo(1));
                });
                Coordinate[] coordinates = CoordinateSequences.ExtractCoordinates(
                    geometry,
                    GeometryConstants.DefaultMaximumNestingDepth,
                    GeometryConstants.DefaultMaximumComponents);
                foreach (Coordinate coordinate in coordinates)
                {
                    AssertCoordinateMatchesLayout(coordinate, layout);
                }
            }

            if (layout == CoordinateLayout.XY)
            {
                Geometry envelopePolygon = factory.CreateEnvelopePolygon(new Envelope
                {
                    MinX = 0.0,
                    MinY = 0.0,
                    MaxX = 4.0,
                    MaxY = 4.0
                });
                Assert.That(envelopePolygon.SpatialReferenceId, Is.EqualTo(4979));
                Assert.That(envelopePolygon.CoordinateLayout, Is.EqualTo(CoordinateLayout.XY));
            }
        }
    }

    [Test]
    public void FactorySnapshotsEveryNestedCorePayloadAndCallerArray()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions());
        Coordinate pointCoordinate = CoordinateForLayout(CoordinateLayout.XY, 1.0, 1.0);
        Coordinate lineStart = CoordinateForLayout(CoordinateLayout.XY, 2.0, 2.0);
        Coordinate lineEnd = CoordinateForLayout(CoordinateLayout.XY, 3.0, 3.0);
        Coordinate[] lineCoordinates = { lineStart, lineEnd };
        LinearRing shell = RingForLayout(CoordinateLayout.XY, 0.0, 0.0, 10.0, 10.0);
        LinearRing hole = RingForLayout(CoordinateLayout.XY, 2.0, 2.0, 4.0, 4.0);
        LinearRing[] holes = { hole };
        Point pointPayload = new Point { Coordinate = pointCoordinate };
        Point[] points = { pointPayload };
        LineString linePayload = new LineString { Coordinates = lineCoordinates };
        LineString[] lines = { linePayload };
        Polygon polygonPayload = new Polygon { Shell = shell, Holes = holes };
        Polygon[] polygons = { polygonPayload };
        Geometry pointGeometry = factory.CreatePoint(pointCoordinate);
        Geometry[] members = { pointGeometry };

        Geometry point = factory.CreatePoint(pointCoordinate);
        Geometry line = factory.CreateLineString(lineCoordinates);
        Geometry polygon = factory.CreatePolygon(shell, holes);
        Geometry multiPoint = factory.CreateMultiPoint(points);
        Geometry multiLine = factory.CreateMultiLineString(lines);
        Geometry multiPolygon = factory.CreateMultiPolygon(polygons);
        Geometry collection = factory.CreateGeometryCollection(members);
        Geometry promoted = factory.CreateNarrowestCollection([pointGeometry]);

        pointCoordinate.X = 100.0;
        lineStart.X = 100.0;
        shell.Coordinates[0].X = 100.0;
        hole.Coordinates[0].X = 100.0;
        points[0] = new Point();
        lines[0] = new LineString();
        polygons[0] = new Polygon();
        members[0] = factory.CreatePoint(null);
        pointGeometry.Point!.Coordinate!.X = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(point.Point!.Coordinate!.X, Is.EqualTo(1.0));
            Assert.That(line.LineString!.Coordinates[0].X, Is.EqualTo(2.0));
            Assert.That(polygon.Polygon!.Shell.Coordinates[0].X, Is.EqualTo(0.0));
            Assert.That(polygon.Polygon.Holes[0].Coordinates[0].X, Is.EqualTo(2.0));
            Assert.That(multiPoint.MultiPoint!.Points[0].Coordinate!.X, Is.EqualTo(1.0));
            Assert.That(multiLine.MultiLineString!.LineStrings[0].Coordinates[0].X, Is.EqualTo(2.0));
            Assert.That(multiPolygon.MultiPolygon!.Polygons[0].Shell.Coordinates[0].X, Is.EqualTo(0.0));
            Assert.That(collection.GeometryCollection!.Geometries[0].Point!.Coordinate!.X, Is.EqualTo(1.0));
            Assert.That(promoted.Point!.Coordinate!.X, Is.EqualTo(1.0));
        });
    }

    [Test]
    public void FactoryRejectsNullAtEveryRequiredOwningBoundary()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions());
        Coordinate coordinate = CoordinateForLayout(CoordinateLayout.XY, 0.0, 0.0);
        Coordinate other = CoordinateForLayout(CoordinateLayout.XY, 1.0, 1.0);
        LinearRing shell = RingForLayout(CoordinateLayout.XY, 0.0, 0.0, 2.0, 2.0);

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() => factory.CreateLineString(null!));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateLineString([coordinate, null!]));
            Assert.Throws<InvalidGeometryException>(() => factory.CreatePolygon(null!, []));
            Assert.Throws<InvalidGeometryException>(() => factory.CreatePolygon(shell, null!));
            Assert.Throws<InvalidGeometryException>(() => factory.CreatePolygon(shell, [null!]));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateMultiPoint(null!));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateMultiPoint([null!]));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateMultiLineString(null!));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateMultiLineString([null!]));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateMultiPolygon(null!));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateMultiPolygon([null!]));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateGeometryCollection(null!));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateGeometryCollection([null!]));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateRectangle(null!, other));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateRectangle(coordinate, null!));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateEnvelopePolygon(null!));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateCircle(null!, 1.0, 2));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateRegularPolygon(null!, 1.0, 4));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateSegment(null!, other));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateSegment(coordinate, null!));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateNarrowestCollection(null!));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateNarrowestCollection([null!]));
        });
    }

    [Test]
    public void EveryGeometryTagRejectsMissingOrMismatchedSelectedPayloads()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions());
        GeometryType[] types = AllGeometryTypes();
        foreach (GeometryType type in types)
        {
            Geometry missing = factory.CreateEmptyGeometry(type);
            ClearSelectedPayload(missing);
            Assert.Throws<InvalidGeometryException>(() => GeometryDispatch.GetSelectedPayload(missing), type.ToString());
            Assert.Throws<InvalidGeometryException>(() => GeometryCopy.CopyGeometry(missing), type.ToString());

            Geometry mismatch = factory.CreateEmptyGeometry(type);
            if (type == GeometryType.Point)
            {
                mismatch.GeometryType = GeometryType.LineString;
            }
            else
            {
                mismatch.GeometryType = GeometryType.Point;
            }

            Assert.Throws<InvalidGeometryException>(() => GeometryDispatch.GetSelectedPayload(mismatch), type.ToString());
            Assert.Throws<InvalidGeometryException>(() => GeometryCopy.CopyGeometry(mismatch), type.ToString());
        }
    }

    [Test]
    public void CollectionFactoriesRejectEveryLayoutAndSridMismatchWithoutRelabelingInputs()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions
        {
            CoordinateLayout = CoordinateLayout.XYZ,
            SpatialReferenceId = 4326
        });
        Geometry wrongLayout = new Geometry
        {
            CoordinateLayout = CoordinateLayout.XY,
            SpatialReferenceId = 4326,
            Point = new Point { Coordinate = CoordinateForLayout(CoordinateLayout.XY, 1.0, 1.0) }
        };
        Geometry wrongKnownSrid = new Geometry
        {
            CoordinateLayout = CoordinateLayout.XYZ,
            SpatialReferenceId = 3857,
            Point = new Point { Coordinate = CoordinateForLayout(CoordinateLayout.XYZ, 1.0, 1.0) }
        };
        Geometry wrongUnknownSrid = new Geometry
        {
            CoordinateLayout = CoordinateLayout.XYZ,
            SpatialReferenceId = GeometryConstants.UnknownSpatialReferenceId,
            Point = new Point { Coordinate = CoordinateForLayout(CoordinateLayout.XYZ, 1.0, 1.0) }
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() => factory.CreateGeometryCollection([wrongLayout]));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateGeometryCollection([wrongKnownSrid]));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateGeometryCollection([wrongUnknownSrid]));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateNarrowestCollection([wrongLayout]));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateNarrowestCollection([wrongKnownSrid]));
            Assert.Throws<InvalidGeometryException>(() => factory.CreateNarrowestCollection([wrongUnknownSrid]));
            Assert.That(wrongLayout.CoordinateLayout, Is.EqualTo(CoordinateLayout.XY));
            Assert.That(wrongKnownSrid.SpatialReferenceId, Is.EqualTo(3857));
            Assert.That(wrongUnknownSrid.SpatialReferenceId, Is.EqualTo(GeometryConstants.UnknownSpatialReferenceId));
        });
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

    private static Coordinate CoordinateForLayout(CoordinateLayout layout, double x, double y)
    {
        Coordinate coordinate = new Coordinate { X = x, Y = y };
        if (layout == CoordinateLayout.XYZ || layout == CoordinateLayout.XYZM)
        {
            coordinate.Z = 5.0;
        }

        if (layout == CoordinateLayout.XYM || layout == CoordinateLayout.XYZM)
        {
            coordinate.M = 7.0;
        }

        return coordinate;
    }

    private static LinearRing RingForLayout(CoordinateLayout layout, double minimumX, double minimumY, double maximumX, double maximumY)
    {
        Coordinate first = CoordinateForLayout(layout, minimumX, minimumY);
        return new LinearRing
        {
            Coordinates =
            [
                first,
                CoordinateForLayout(layout, maximumX, minimumY),
                CoordinateForLayout(layout, maximumX, maximumY),
                CoordinateForLayout(layout, minimumX, maximumY),
                first
            ]
        };
    }

    private static void AssertCoordinateMatchesLayout(Coordinate coordinate, CoordinateLayout layout)
    {
        Assert.That(double.IsFinite(coordinate.X), Is.True);
        Assert.That(double.IsFinite(coordinate.Y), Is.True);
        if (layout == CoordinateLayout.XYZ || layout == CoordinateLayout.XYZM)
        {
            Assert.That(double.IsFinite(coordinate.Z), Is.True);
        }
        else
        {
            Assert.That(double.IsNaN(coordinate.Z), Is.True);
        }

        if (layout == CoordinateLayout.XYM || layout == CoordinateLayout.XYZM)
        {
            Assert.That(double.IsFinite(coordinate.M), Is.True);
        }
        else
        {
            Assert.That(double.IsNaN(coordinate.M), Is.True);
        }
    }

    private static void AssertCanonicalEmptyPayload(Geometry geometry)
    {
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
                Assert.That(geometry.Point!.Coordinate, Is.Null);
                break;
            case GeometryType.LineString:
                Assert.That(geometry.LineString!.Coordinates, Is.Empty);
                break;
            case GeometryType.LinearRing:
                Assert.That(geometry.LinearRing!.Coordinates, Is.Empty);
                break;
            case GeometryType.Polygon:
                Assert.That(geometry.Polygon!.Shell.Coordinates, Is.Empty);
                Assert.That(geometry.Polygon.Holes, Is.Empty);
                break;
            case GeometryType.MultiPoint:
                Assert.That(geometry.MultiPoint!.Points, Is.Empty);
                break;
            case GeometryType.MultiLineString:
                Assert.That(geometry.MultiLineString!.LineStrings, Is.Empty);
                break;
            case GeometryType.MultiPolygon:
                Assert.That(geometry.MultiPolygon!.Polygons, Is.Empty);
                break;
            case GeometryType.GeometryCollection:
                Assert.That(geometry.GeometryCollection!.Geometries, Is.Empty);
                break;
            case GeometryType.CircularString:
                Assert.That(geometry.CircularString!.Coordinates, Is.Empty);
                break;
            case GeometryType.CompoundCurve:
                Assert.That(geometry.CompoundCurve!.Curves, Is.Empty);
                break;
            case GeometryType.CurvePolygon:
                Assert.That(geometry.CurvePolygon!.Shell, Is.Null);
                Assert.That(geometry.CurvePolygon.Holes, Is.Empty);
                break;
            case GeometryType.MultiCurve:
                Assert.That(geometry.MultiCurve!.Curves, Is.Empty);
                break;
            case GeometryType.MultiSurface:
                Assert.That(geometry.MultiSurface!.Surfaces, Is.Empty);
                break;
            case GeometryType.PolyhedralSurface:
                Assert.That(geometry.PolyhedralSurface!.Patches, Is.Empty);
                break;
            case GeometryType.Tin:
                Assert.That(geometry.Tin!.Triangles, Is.Empty);
                break;
            case GeometryType.Triangle:
                Assert.That(geometry.Triangle!.Shell.Coordinates, Is.Empty);
                break;
            default:
                Assert.Fail("Unexpected geometry type.");
                break;
        }
    }

    private static void ClearSelectedPayload(Geometry geometry)
    {
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
                geometry.Point = null;
                break;
            case GeometryType.LineString:
                geometry.LineString = null;
                break;
            case GeometryType.LinearRing:
                geometry.LinearRing = null;
                break;
            case GeometryType.Polygon:
                geometry.Polygon = null;
                break;
            case GeometryType.MultiPoint:
                geometry.MultiPoint = null;
                break;
            case GeometryType.MultiLineString:
                geometry.MultiLineString = null;
                break;
            case GeometryType.MultiPolygon:
                geometry.MultiPolygon = null;
                break;
            case GeometryType.GeometryCollection:
                geometry.GeometryCollection = null;
                break;
            case GeometryType.CircularString:
                geometry.CircularString = null;
                break;
            case GeometryType.CompoundCurve:
                geometry.CompoundCurve = null;
                break;
            case GeometryType.CurvePolygon:
                geometry.CurvePolygon = null;
                break;
            case GeometryType.MultiCurve:
                geometry.MultiCurve = null;
                break;
            case GeometryType.MultiSurface:
                geometry.MultiSurface = null;
                break;
            case GeometryType.PolyhedralSurface:
                geometry.PolyhedralSurface = null;
                break;
            case GeometryType.Tin:
                geometry.Tin = null;
                break;
            case GeometryType.Triangle:
                geometry.Triangle = null;
                break;
            default:
                Assert.Fail("Unexpected geometry type.");
                break;
        }
    }
}
