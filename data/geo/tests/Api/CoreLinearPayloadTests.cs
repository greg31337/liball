using geo;

namespace geo.Tests;

public class CoreLinearPayloadTests
{
    [Test]
    public void DefaultPayloadsRepresentCanonicalEmptyValues()
    {
        Point point = new Point();
        LineString lineString = new LineString();
        LinearRing linearRing = new LinearRing();
        Polygon polygon = new Polygon();
        MultiPoint multiPoint = new MultiPoint();
        MultiLineString multiLineString = new MultiLineString();
        MultiPolygon multiPolygon = new MultiPolygon();
        GeometryCollection geometryCollection = new GeometryCollection();

        Assert.Multiple(() =>
        {
            Assert.That(point.Coordinate, Is.Null);
            Assert.That(lineString.Coordinates, Is.Empty);
            Assert.That(linearRing.Coordinates, Is.Empty);
            Assert.That(polygon.Shell, Is.Not.Null);
            Assert.That(polygon.Shell.Coordinates, Is.Empty);
            Assert.That(polygon.Holes, Is.Empty);
            Assert.That(multiPoint.Points, Is.Empty);
            Assert.That(multiLineString.LineStrings, Is.Empty);
            Assert.That(multiPolygon.Polygons, Is.Empty);
            Assert.That(geometryCollection.Geometries, Is.Empty);
        });
    }

    [Test]
    public void AtomicPayloadsRetainCallerOwnedCoordinatesAndArrays()
    {
        Coordinate first = new Coordinate { X = 1.0, Y = 2.0 };
        Coordinate second = new Coordinate { X = 3.0, Y = 4.0 };
        Coordinate replacement = new Coordinate { X = 5.0, Y = 6.0 };
        Coordinate[] lineCoordinates = new[] { first, second };
        Coordinate[] ringCoordinates = new[] { first, second, replacement, first };
        Point point = new Point { Coordinate = first };
        LineString lineString = new LineString { Coordinates = lineCoordinates };
        LinearRing linearRing = new LinearRing { Coordinates = ringCoordinates };

        lineCoordinates[0] = replacement;
        first.X = 10.0;

        Assert.Multiple(() =>
        {
            Assert.That(point.Coordinate, Is.SameAs(first));
            Assert.That(point.Coordinate?.X, Is.EqualTo(10.0));
            Assert.That(lineString.Coordinates, Is.SameAs(lineCoordinates));
            Assert.That(lineString.Coordinates[0], Is.SameAs(replacement));
            Assert.That(linearRing.Coordinates, Is.SameAs(ringCoordinates));
        });
    }

    [Test]
    public void PolygonRetainsShellAndHolePayloads()
    {
        LinearRing shell = new LinearRing
        {
            Coordinates = new[]
            {
                new Coordinate { X = 0.0, Y = 0.0 },
                new Coordinate { X = 10.0, Y = 0.0 },
                new Coordinate { X = 10.0, Y = 10.0 },
                new Coordinate { X = 0.0, Y = 0.0 }
            }
        };
        LinearRing hole = new LinearRing
        {
            Coordinates = new[]
            {
                new Coordinate { X = 2.0, Y = 2.0 },
                new Coordinate { X = 3.0, Y = 2.0 },
                new Coordinate { X = 2.0, Y = 3.0 },
                new Coordinate { X = 2.0, Y = 2.0 }
            }
        };
        LinearRing[] holes = new[] { hole };
        Polygon polygon = new Polygon { Shell = shell, Holes = holes };

        Assert.Multiple(() =>
        {
            Assert.That(polygon.Shell, Is.SameAs(shell));
            Assert.That(polygon.Holes, Is.SameAs(holes));
            Assert.That(polygon.Holes[0], Is.SameAs(hole));
        });
    }

    [Test]
    public void HomogeneousMultiPayloadsRetainTheirTypedComponents()
    {
        Point point = new Point { Coordinate = new Coordinate { X = 1.0, Y = 2.0 } };
        LineString line = new LineString
        {
            Coordinates = new[]
            {
                new Coordinate { X = 0.0, Y = 0.0 },
                new Coordinate { X = 1.0, Y = 1.0 }
            }
        };
        Polygon polygon = new Polygon();
        Point[] points = new[] { point };
        LineString[] lines = new[] { line };
        Polygon[] polygons = new[] { polygon };
        MultiPoint multiPoint = new MultiPoint { Points = points };
        MultiLineString multiLine = new MultiLineString { LineStrings = lines };
        MultiPolygon multiPolygon = new MultiPolygon { Polygons = polygons };

        Assert.Multiple(() =>
        {
            Assert.That(multiPoint.Points, Is.SameAs(points));
            Assert.That(multiPoint.Points[0], Is.SameAs(point));
            Assert.That(multiLine.LineStrings, Is.SameAs(lines));
            Assert.That(multiLine.LineStrings[0], Is.SameAs(line));
            Assert.That(multiPolygon.Polygons, Is.SameAs(polygons));
            Assert.That(multiPolygon.Polygons[0], Is.SameAs(polygon));
        });
    }

    [Test]
    public void GeometryCollectionRetainsHeterogeneousAndEmptyMembers()
    {
        Geometry point = new Geometry
        {
            GeometryType = GeometryType.Point,
            CoordinateLayout = CoordinateLayout.XYZ,
            SpatialReferenceId = 4326,
            Point = new Point { Coordinate = new Coordinate { X = 1.0, Y = 2.0, Z = 3.0 } }
        };
        Geometry emptyLine = new Geometry
        {
            GeometryType = GeometryType.LineString,
            LineString = new LineString()
        };
        Geometry[] members = new[] { point, emptyLine };
        GeometryCollection collection = new GeometryCollection { Geometries = members };

        Assert.Multiple(() =>
        {
            Assert.That(collection.Geometries, Is.SameAs(members));
            Assert.That(collection.Geometries[0], Is.SameAs(point));
            Assert.That(collection.Geometries[1], Is.SameAs(emptyLine));
            Assert.That(point.GeometryType, Is.EqualTo(GeometryType.Point));
            Assert.That(point.CoordinateLayout, Is.EqualTo(CoordinateLayout.XYZ));
            Assert.That(point.SpatialReferenceId, Is.EqualTo(4326));
            Assert.That(point.Point?.Coordinate?.Z, Is.EqualTo(3.0));
            Assert.That(emptyLine.LineString?.Coordinates, Is.Empty);
        });
    }

    [Test]
    public void GeometryDefaultsUsePointXyAndUnknownSpatialReferenceConventions()
    {
        Geometry geometry = new Geometry();

        Assert.Multiple(() =>
        {
            Assert.That(geometry.GeometryType, Is.EqualTo(GeometryType.Point));
            Assert.That(geometry.CoordinateLayout, Is.EqualTo(CoordinateLayout.XY));
            Assert.That(geometry.SpatialReferenceId, Is.EqualTo(GeometryConstants.UnknownSpatialReferenceId));
            Assert.That(geometry.Point, Is.Not.Null);
            Assert.That(geometry.Point?.Coordinate, Is.Null);
            Assert.That(geometry.LineString, Is.Null);
            Assert.That(geometry.LinearRing, Is.Null);
            Assert.That(geometry.Polygon, Is.Null);
            Assert.That(geometry.MultiPoint, Is.Null);
            Assert.That(geometry.MultiLineString, Is.Null);
            Assert.That(geometry.MultiPolygon, Is.Null);
            Assert.That(geometry.GeometryCollection, Is.Null);
            Assert.That(geometry.Triangle, Is.Null);
            Assert.That(geometry.Tin, Is.Null);
            Assert.That(geometry.PolyhedralSurface, Is.Null);
            Assert.That(geometry.CircularString, Is.Null);
            Assert.That(geometry.CompoundCurve, Is.Null);
            Assert.That(geometry.CurvePolygon, Is.Null);
            Assert.That(geometry.MultiCurve, Is.Null);
            Assert.That(geometry.MultiSurface, Is.Null);
        });
    }
}
