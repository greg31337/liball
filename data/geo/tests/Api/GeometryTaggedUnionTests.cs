using geo;

namespace geo.Tests;

public class GeometryTaggedUnionTests
{
    [Test]
    public void GeometrySupportsExactlyOneTypedPayloadForEveryGeometryKind()
    {
        Point pointPayload = new Point();
        LineString linePayload = new LineString();
        LinearRing ringPayload = new LinearRing();
        Polygon polygonPayload = new Polygon();
        MultiPoint multiPointPayload = new MultiPoint();
        MultiLineString multiLinePayload = new MultiLineString();
        MultiPolygon multiPolygonPayload = new MultiPolygon();
        GeometryCollection collectionPayload = new GeometryCollection();
        Triangle trianglePayload = new Triangle();
        Tin tinPayload = new Tin();
        PolyhedralSurface polyhedralPayload = new PolyhedralSurface();
        CircularString circularPayload = new CircularString();
        CompoundCurve compoundPayload = new CompoundCurve();
        CurvePolygon curvePolygonPayload = new CurvePolygon();
        MultiCurve multiCurvePayload = new MultiCurve();
        MultiSurface multiSurfacePayload = new MultiSurface();

        Geometry point = new Geometry { GeometryType = GeometryType.Point, Point = pointPayload };
        Geometry line = new Geometry { GeometryType = GeometryType.LineString, Point = null, LineString = linePayload };
        Geometry ring = new Geometry { GeometryType = GeometryType.LinearRing, Point = null, LinearRing = ringPayload };
        Geometry polygon = new Geometry { GeometryType = GeometryType.Polygon, Point = null, Polygon = polygonPayload };
        Geometry multiPoint = new Geometry { GeometryType = GeometryType.MultiPoint, Point = null, MultiPoint = multiPointPayload };
        Geometry multiLine = new Geometry { GeometryType = GeometryType.MultiLineString, Point = null, MultiLineString = multiLinePayload };
        Geometry multiPolygon = new Geometry { GeometryType = GeometryType.MultiPolygon, Point = null, MultiPolygon = multiPolygonPayload };
        Geometry collection = new Geometry { GeometryType = GeometryType.GeometryCollection, Point = null, GeometryCollection = collectionPayload };
        Geometry triangle = new Geometry { GeometryType = GeometryType.Triangle, Point = null, Triangle = trianglePayload };
        Geometry tin = new Geometry { GeometryType = GeometryType.Tin, Point = null, Tin = tinPayload };
        Geometry polyhedral = new Geometry { GeometryType = GeometryType.PolyhedralSurface, Point = null, PolyhedralSurface = polyhedralPayload };
        Geometry circular = new Geometry { GeometryType = GeometryType.CircularString, Point = null, CircularString = circularPayload };
        Geometry compound = new Geometry { GeometryType = GeometryType.CompoundCurve, Point = null, CompoundCurve = compoundPayload };
        Geometry curvePolygon = new Geometry { GeometryType = GeometryType.CurvePolygon, Point = null, CurvePolygon = curvePolygonPayload };
        Geometry multiCurve = new Geometry { GeometryType = GeometryType.MultiCurve, Point = null, MultiCurve = multiCurvePayload };
        Geometry multiSurface = new Geometry { GeometryType = GeometryType.MultiSurface, Point = null, MultiSurface = multiSurfacePayload };
        Geometry[] geometries = new[]
        {
            point,
            line,
            ring,
            polygon,
            multiPoint,
            multiLine,
            multiPolygon,
            collection,
            triangle,
            tin,
            polyhedral,
            circular,
            compound,
            curvePolygon,
            multiCurve,
            multiSurface
        };

        Assert.Multiple(() =>
        {
            Assert.That(point.Point, Is.SameAs(pointPayload));
            Assert.That(line.LineString, Is.SameAs(linePayload));
            Assert.That(ring.LinearRing, Is.SameAs(ringPayload));
            Assert.That(polygon.Polygon, Is.SameAs(polygonPayload));
            Assert.That(multiPoint.MultiPoint, Is.SameAs(multiPointPayload));
            Assert.That(multiLine.MultiLineString, Is.SameAs(multiLinePayload));
            Assert.That(multiPolygon.MultiPolygon, Is.SameAs(multiPolygonPayload));
            Assert.That(collection.GeometryCollection, Is.SameAs(collectionPayload));
            Assert.That(triangle.Triangle, Is.SameAs(trianglePayload));
            Assert.That(tin.Tin, Is.SameAs(tinPayload));
            Assert.That(polyhedral.PolyhedralSurface, Is.SameAs(polyhedralPayload));
            Assert.That(circular.CircularString, Is.SameAs(circularPayload));
            Assert.That(compound.CompoundCurve, Is.SameAs(compoundPayload));
            Assert.That(curvePolygon.CurvePolygon, Is.SameAs(curvePolygonPayload));
            Assert.That(multiCurve.MultiCurve, Is.SameAs(multiCurvePayload));
            Assert.That(multiSurface.MultiSurface, Is.SameAs(multiSurfacePayload));
        });

        foreach (Geometry geometry in geometries)
        {
            Assert.That(CountPopulatedPayloads(geometry), Is.EqualTo(1));
        }
    }

    [Test]
    public void GeometryMetadataRetainsEveryCoordinateLayoutAndKnownOrUnknownSrid()
    {
        Geometry xy = new Geometry { CoordinateLayout = CoordinateLayout.XY };
        Geometry xyz = new Geometry { CoordinateLayout = CoordinateLayout.XYZ, SpatialReferenceId = 4326 };
        Geometry xym = new Geometry { CoordinateLayout = CoordinateLayout.XYM, SpatialReferenceId = 3857 };
        Geometry xyzm = new Geometry { CoordinateLayout = CoordinateLayout.XYZM, SpatialReferenceId = 4979 };

        Assert.Multiple(() =>
        {
            Assert.That(xy.CoordinateLayout, Is.EqualTo(CoordinateLayout.XY));
            Assert.That(xy.SpatialReferenceId, Is.EqualTo(GeometryConstants.UnknownSpatialReferenceId));
            Assert.That(xyz.CoordinateLayout, Is.EqualTo(CoordinateLayout.XYZ));
            Assert.That(xyz.SpatialReferenceId, Is.EqualTo(4326));
            Assert.That(xym.CoordinateLayout, Is.EqualTo(CoordinateLayout.XYM));
            Assert.That(xym.SpatialReferenceId, Is.EqualTo(3857));
            Assert.That(xyzm.CoordinateLayout, Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(xyzm.SpatialReferenceId, Is.EqualTo(4979));
        });
    }

    private static int CountPopulatedPayloads(Geometry geometry)
    {
        object?[] payloads =
        {
            geometry.Point,
            geometry.LineString,
            geometry.LinearRing,
            geometry.Polygon,
            geometry.MultiPoint,
            geometry.MultiLineString,
            geometry.MultiPolygon,
            geometry.GeometryCollection,
            geometry.Triangle,
            geometry.Tin,
            geometry.PolyhedralSurface,
            geometry.CircularString,
            geometry.CompoundCurve,
            geometry.CurvePolygon,
            geometry.MultiCurve,
            geometry.MultiSurface
        };
        int count = 0;
        foreach (object? payload in payloads)
        {
            if (payload != null)
            {
                count++;
            }
        }

        return count;
    }
}
