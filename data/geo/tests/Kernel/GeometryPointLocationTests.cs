using geo;

namespace geo.Tests;

public class GeometryPointLocationTests
{
    [Test]
    public void PointAndMultiPointUseInteriorOrExteriorWithoutBoundary()
    {
        Geometry point = PointGeometry(2.0, 3.0);
        Geometry empty = EmptyPointGeometry();
        Geometry multi = MultiPointGeometry([
            new Point(), new Point { Coordinate = CoordinateValue(5.0, 6.0) }
        ]);

        Assert.Multiple(() =>
        {
            AssertLocation(2.0, 3.0, point, TopologicalLocation.Interior);
            AssertLocation(2.0, 4.0, point, TopologicalLocation.Exterior);
            AssertLocation(0.0, 0.0, empty, TopologicalLocation.Exterior);
            AssertLocation(5.0, 6.0, multi, TopologicalLocation.Interior);
            AssertLocation(5.0, 7.0, multi, TopologicalLocation.Exterior);
        });
    }

    [Test]
    public void OpenClosedAndMultiLinesApplyEndpointBoundaryRules()
    {
        Geometry open = LineGeometry([
            CoordinateValue(0.0, 0.0), CoordinateValue(2.0, 0.0), CoordinateValue(4.0, 0.0)
        ]);
        Geometry closed = LineGeometry([
            CoordinateValue(0.0, 0.0), CoordinateValue(2.0, 0.0),
            CoordinateValue(2.0, 2.0), CoordinateValue(0.0, 0.0)
        ]);
        Geometry multi = MultiLineGeometry([
            new LineString { Coordinates = [CoordinateValue(0.0, 0.0), CoordinateValue(2.0, 0.0)] },
            new LineString { Coordinates = [CoordinateValue(2.0, 0.0), CoordinateValue(4.0, 0.0)] }
        ]);

        Assert.Multiple(() =>
        {
            AssertLocation(0.0, 0.0, open, TopologicalLocation.Boundary);
            AssertLocation(2.0, 0.0, open, TopologicalLocation.Interior);
            AssertLocation(2.0, 1.0, open, TopologicalLocation.Exterior);
            AssertLocation(0.0, 0.0, closed, TopologicalLocation.Interior);
            AssertLocation(2.0, 0.0, multi, TopologicalLocation.Interior);
            AssertLocation(0.0, 0.0, multi, TopologicalLocation.Boundary);
            AssertLocation(4.0, 0.0, multi, TopologicalLocation.Boundary);
        });
    }

    [Test]
    public void PolygonShellAndHolesClassifyAllTopologicalLocations()
    {
        Geometry polygon = PolygonGeometry(PolygonValue(
            Ring(0.0, 0.0, 10.0, 10.0),
            [Ring(3.0, 3.0, 7.0, 7.0)]));

        Assert.Multiple(() =>
        {
            AssertLocation(1.0, 1.0, polygon, TopologicalLocation.Interior);
            AssertLocation(0.0, 5.0, polygon, TopologicalLocation.Boundary);
            AssertLocation(3.0, 5.0, polygon, TopologicalLocation.Boundary);
            AssertLocation(5.0, 5.0, polygon, TopologicalLocation.Exterior);
            AssertLocation(12.0, 5.0, polygon, TopologicalLocation.Exterior);
        });
    }

    [Test]
    public void MultiPolygonAndMixedCollectionUseInteriorThenBoundaryPrecedence()
    {
        Polygon first = PolygonValue(Ring(0.0, 0.0, 4.0, 4.0), []);
        Polygon second = PolygonValue(Ring(5.0, 0.0, 9.0, 4.0), []);
        Geometry multi = MultiPolygonGeometry([first, second]);
        Geometry collection = CollectionGeometry([
            PolygonGeometry(first), LineGeometry([
                CoordinateValue(4.0, 2.0), CoordinateValue(8.0, 2.0)
            ]), PointGeometry(20.0, 20.0)
        ]);

        Assert.Multiple(() =>
        {
            AssertLocation(1.0, 1.0, multi, TopologicalLocation.Interior);
            AssertLocation(5.0, 2.0, multi, TopologicalLocation.Boundary);
            AssertLocation(4.5, 2.0, multi, TopologicalLocation.Exterior);
            AssertLocation(4.0, 2.0, collection, TopologicalLocation.Boundary);
            AssertLocation(6.0, 2.0, collection, TopologicalLocation.Interior);
            AssertLocation(20.0, 20.0, collection, TopologicalLocation.Interior);
        });
    }

    [Test]
    public void TriangleTinPolyhedralAndMultiSurfaceLocateAcrossPatches()
    {
        LinearRing triangleRing = new LinearRing { Coordinates = [
            CoordinateValue(0.0, 0.0), CoordinateValue(4.0, 0.0),
            CoordinateValue(0.0, 4.0), CoordinateValue(0.0, 0.0)
        ] };
        Triangle triangle = new Triangle { Shell = triangleRing };
        Geometry triangleGeometry = TriangleGeometry(triangle);
        Geometry tin = TinGeometry([triangle]);
        Polygon patch = PolygonValue(Ring(10.0, 10.0, 14.0, 14.0), []);
        Geometry polyhedral = PolyhedralGeometry([patch]);
        Geometry multiSurface = MultiSurfaceGeometry([triangleGeometry, PolygonGeometry(patch)]);

        Assert.Multiple(() =>
        {
            AssertLocation(1.0, 1.0, triangleGeometry, TopologicalLocation.Interior);
            AssertLocation(2.0, 0.0, tin, TopologicalLocation.Boundary);
            AssertLocation(12.0, 12.0, polyhedral, TopologicalLocation.Interior);
            AssertLocation(12.0, 12.0, multiSurface, TopologicalLocation.Interior);
            AssertLocation(7.0, 7.0, multiSurface, TopologicalLocation.Exterior);
        });
    }

    [Test]
    public void CircularStringLinearizationPreservesControlsAndCurveBoundaryRules()
    {
        Geometry circular = CircularGeometry([
            CoordinateValue(0.0, 0.0), CoordinateValue(1.0, 1.0), CoordinateValue(2.0, 0.0)
        ]);
        Coordinate[] linearized = CurveLinearizer.LinearizeCurveGeometry(circular, Parameters());

        Assert.Multiple(() =>
        {
            Assert.That(linearized, Has.Length.EqualTo(3));
            Assert.That(linearized[1].X, Is.EqualTo(1.0));
            Assert.That(linearized[1].Y, Is.EqualTo(1.0));
            AssertLocation(0.0, 0.0, circular, TopologicalLocation.Boundary);
            AssertLocation(1.0, 1.0, circular, TopologicalLocation.Interior);
            AssertLocation(2.0, 0.0, circular, TopologicalLocation.Boundary);
            AssertLocation(1.0, 0.0, circular, TopologicalLocation.Exterior);
        });
    }

    [Test]
    public void CompoundAndMultiCurveTreatJunctionsAndEndpointParityCorrectly()
    {
        Geometry line = LineGeometry([CoordinateValue(0.0, 0.0), CoordinateValue(2.0, 0.0)]);
        Geometry arc = CircularGeometry([
            CoordinateValue(2.0, 0.0), CoordinateValue(3.0, 1.0), CoordinateValue(4.0, 0.0)
        ]);
        Geometry compound = CompoundGeometry([line, arc]);
        Geometry multiCurve = MultiCurveGeometry([
            LineGeometry([CoordinateValue(0.0, 0.0), CoordinateValue(2.0, 0.0)]),
            LineGeometry([CoordinateValue(2.0, 0.0), CoordinateValue(4.0, 0.0)])
        ]);

        Assert.Multiple(() =>
        {
            AssertLocation(0.0, 0.0, compound, TopologicalLocation.Boundary);
            AssertLocation(2.0, 0.0, compound, TopologicalLocation.Interior);
            AssertLocation(3.0, 1.0, compound, TopologicalLocation.Interior);
            AssertLocation(4.0, 0.0, compound, TopologicalLocation.Boundary);
            AssertLocation(2.0, 0.0, multiCurve, TopologicalLocation.Interior);
            AssertLocation(0.0, 0.0, multiCurve, TopologicalLocation.Boundary);
        });
    }

    [Test]
    public void CurvePolygonUsesLinearizedShellAndLinearHole()
    {
        Geometry shell = CircularGeometry([
            CoordinateValue(2.0, 0.0), CoordinateValue(0.0, 2.0),
            CoordinateValue(-2.0, 0.0), CoordinateValue(0.0, -2.0),
            CoordinateValue(2.0, 0.0)
        ]);
        Geometry hole = RingGeometry(Ring(-0.5, -0.5, 0.5, 0.5));
        Geometry curvePolygon = CurvePolygonGeometry(shell, [hole]);

        Assert.Multiple(() =>
        {
            AssertLocation(1.0, 0.0, curvePolygon, TopologicalLocation.Interior);
            AssertLocation(2.0, 0.0, curvePolygon, TopologicalLocation.Boundary);
            AssertLocation(0.5, 0.0, curvePolygon, TopologicalLocation.Boundary);
            AssertLocation(0.0, 0.0, curvePolygon, TopologicalLocation.Exterior);
            AssertLocation(3.0, 0.0, curvePolygon, TopologicalLocation.Exterior);
        });
    }

    [Test]
    public void EmptyNestedAndInvalidInputsHaveDeterministicBehavior()
    {
        Geometry emptyCollection = CollectionGeometry([]);
        Geometry cyclic = CollectionGeometry([]);
        cyclic.GeometryCollection!.Geometries = [cyclic];
        Geometry malformedCircular = CircularGeometry([
            CoordinateValue(0.0, 0.0), CoordinateValue(1.0, 1.0)
        ]);
        CurveLinearizationParameters invalid = Parameters();
        invalid.Tolerance = 0.0;

        Assert.Multiple(() =>
        {
            AssertLocation(0.0, 0.0, emptyCollection, TopologicalLocation.Exterior);
            Assert.Throws<InvalidGeometryException>(() => GeometryPointLocation.LocatePoint(
                CoordinateValue(0.0, 0.0), cyclic, Parameters()));
            Assert.Throws<InvalidGeometryException>(() => GeometryPointLocation.LocatePoint(
                CoordinateValue(double.NaN, 0.0), emptyCollection, Parameters()));
            Assert.Throws<InvalidGeometryException>(() => GeometryPointLocation.LocatePoint(
                CoordinateValue(0.0, 0.0), malformedCircular, Parameters()));
            Assert.Throws<InvalidGeometryException>(() => GeometryPointLocation.LocatePoint(
                CoordinateValue(0.0, 0.0), emptyCollection, invalid));
        });
    }

    private static void AssertLocation(
        double x, double y, Geometry geometry, TopologicalLocation expected)
    {
        Assert.That(GeometryPointLocation.LocatePoint(
            CoordinateValue(x, y), geometry, Parameters()), Is.EqualTo(expected));
    }

    private static CurveLinearizationParameters Parameters()
    {
        return new CurveLinearizationParameters
        {
            Mode = CurveLinearizationMode.MaximumAngle,
            Tolerance = Math.PI / 2.0,
            MaximumSegments = 32
        };
    }

    private static Geometry PointGeometry(double x, double y)
    {
        return new Geometry { Point = new Point { Coordinate = CoordinateValue(x, y) } };
    }

    private static Geometry EmptyPointGeometry()
    {
        return new Geometry { Point = new Point() };
    }

    private static Geometry LineGeometry(Coordinate[] coordinates)
    {
        return new Geometry { GeometryType = GeometryType.LineString, Point = null,
            LineString = new LineString { Coordinates = coordinates } };
    }

    private static Geometry RingGeometry(LinearRing ring)
    {
        return new Geometry { GeometryType = GeometryType.LinearRing, Point = null, LinearRing = ring };
    }

    private static Geometry PolygonGeometry(Polygon polygon)
    {
        return new Geometry { GeometryType = GeometryType.Polygon, Point = null, Polygon = polygon };
    }

    private static Geometry MultiPointGeometry(Point[] points)
    {
        return new Geometry { GeometryType = GeometryType.MultiPoint, Point = null,
            MultiPoint = new MultiPoint { Points = points } };
    }

    private static Geometry MultiLineGeometry(LineString[] lines)
    {
        return new Geometry { GeometryType = GeometryType.MultiLineString, Point = null,
            MultiLineString = new MultiLineString { LineStrings = lines } };
    }

    private static Geometry MultiPolygonGeometry(Polygon[] polygons)
    {
        return new Geometry { GeometryType = GeometryType.MultiPolygon, Point = null,
            MultiPolygon = new MultiPolygon { Polygons = polygons } };
    }

    private static Geometry CollectionGeometry(Geometry[] geometries)
    {
        return new Geometry { GeometryType = GeometryType.GeometryCollection, Point = null,
            GeometryCollection = new GeometryCollection { Geometries = geometries } };
    }

    private static Geometry TriangleGeometry(Triangle triangle)
    {
        return new Geometry { GeometryType = GeometryType.Triangle, Point = null, Triangle = triangle };
    }

    private static Geometry TinGeometry(Triangle[] triangles)
    {
        return new Geometry { GeometryType = GeometryType.Tin, Point = null,
            Tin = new Tin { Triangles = triangles } };
    }

    private static Geometry PolyhedralGeometry(Polygon[] patches)
    {
        return new Geometry { GeometryType = GeometryType.PolyhedralSurface, Point = null,
            PolyhedralSurface = new PolyhedralSurface { Patches = patches } };
    }

    private static Geometry CircularGeometry(Coordinate[] coordinates)
    {
        return new Geometry { GeometryType = GeometryType.CircularString, Point = null,
            CircularString = new CircularString { Coordinates = coordinates } };
    }

    private static Geometry CompoundGeometry(Geometry[] curves)
    {
        return new Geometry { GeometryType = GeometryType.CompoundCurve, Point = null,
            CompoundCurve = new CompoundCurve { Curves = curves } };
    }

    private static Geometry CurvePolygonGeometry(Geometry shell, Geometry[] holes)
    {
        return new Geometry { GeometryType = GeometryType.CurvePolygon, Point = null,
            CurvePolygon = new CurvePolygon { Shell = shell, Holes = holes } };
    }

    private static Geometry MultiCurveGeometry(Geometry[] curves)
    {
        return new Geometry { GeometryType = GeometryType.MultiCurve, Point = null,
            MultiCurve = new MultiCurve { Curves = curves } };
    }

    private static Geometry MultiSurfaceGeometry(Geometry[] surfaces)
    {
        return new Geometry { GeometryType = GeometryType.MultiSurface, Point = null,
            MultiSurface = new MultiSurface { Surfaces = surfaces } };
    }

    private static Polygon PolygonValue(LinearRing shell, LinearRing[] holes)
    {
        return new Polygon { Shell = shell, Holes = holes };
    }

    private static LinearRing Ring(double minX, double minY, double maxX, double maxY)
    {
        return new LinearRing { Coordinates = [
            CoordinateValue(minX, minY), CoordinateValue(maxX, minY),
            CoordinateValue(maxX, maxY), CoordinateValue(minX, maxY),
            CoordinateValue(minX, minY)
        ] };
    }

    private static Coordinate CoordinateValue(double x, double y)
    {
        return new Coordinate { X = x, Y = y };
    }
}
