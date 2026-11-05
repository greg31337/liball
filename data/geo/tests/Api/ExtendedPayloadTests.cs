using geo;

namespace geo.Tests;

public class ExtendedPayloadTests
{
    [Test]
    public void ExtendedPayloadsHaveCanonicalEmptyForms()
    {
        Triangle triangle = new Triangle();
        Tin tin = new Tin();
        PolyhedralSurface polyhedralSurface = new PolyhedralSurface();
        CircularString circularString = new CircularString();
        CompoundCurve compoundCurve = new CompoundCurve();
        CurvePolygon curvePolygon = new CurvePolygon();
        MultiCurve multiCurve = new MultiCurve();
        MultiSurface multiSurface = new MultiSurface();

        Assert.Multiple(() =>
        {
            Assert.That(triangle.Shell, Is.Not.Null);
            Assert.That(triangle.Shell.Coordinates, Is.Empty);
            Assert.That(tin.Triangles, Is.Empty);
            Assert.That(polyhedralSurface.Patches, Is.Empty);
            Assert.That(circularString.Coordinates, Is.Empty);
            Assert.That(compoundCurve.Curves, Is.Empty);
            Assert.That(curvePolygon.Shell, Is.Null);
            Assert.That(curvePolygon.Holes, Is.Empty);
            Assert.That(multiCurve.Curves, Is.Empty);
            Assert.That(multiSurface.Surfaces, Is.Empty);
        });
    }

    [Test]
    public void TriangleTinAndPolyhedralSurfaceRetainTypedCallerOwnedPatches()
    {
        Coordinate first = new Coordinate { X = 0.0, Y = 0.0, Z = 2.0 };
        Coordinate second = new Coordinate { X = 5.0, Y = 0.0, Z = 3.0 };
        Coordinate third = new Coordinate { X = 0.0, Y = 5.0, Z = 4.0 };
        Coordinate[] triangleCoordinates = new[] { first, second, third, first };
        LinearRing ring = new LinearRing { Coordinates = triangleCoordinates };
        Triangle triangle = new Triangle { Shell = ring };
        Triangle[] triangles = new[] { triangle };
        Tin tin = new Tin { Triangles = triangles };
        Polygon patch = new Polygon { Shell = ring };
        Polygon[] patches = new[] { patch };
        PolyhedralSurface polyhedralSurface = new PolyhedralSurface { Patches = patches };

        third.Z = 40.0;

        Assert.Multiple(() =>
        {
            Assert.That(triangle.Shell, Is.SameAs(ring));
            Assert.That(triangle.Shell.Coordinates, Is.SameAs(triangleCoordinates));
            Assert.That(triangle.Shell.Coordinates, Has.Length.EqualTo(4));
            Assert.That(triangle.Shell.Coordinates[2].Z, Is.EqualTo(40.0));
            Assert.That(tin.Triangles, Is.SameAs(triangles));
            Assert.That(tin.Triangles[0], Is.SameAs(triangle));
            Assert.That(polyhedralSurface.Patches, Is.SameAs(patches));
            Assert.That(polyhedralSurface.Patches[0], Is.SameAs(patch));
        });
    }

    [Test]
    public void CircularStringRetainsXyzmControlCoordinates()
    {
        Coordinate[] coordinates = new[]
        {
            new Coordinate { X = 0.0, Y = 0.0, Z = 10.0, M = 100.0 },
            new Coordinate { X = 1.0, Y = 1.0, Z = 11.0, M = 101.0 },
            new Coordinate { X = 2.0, Y = 0.0, Z = 12.0, M = 102.0 }
        };
        CircularString circularString = new CircularString { Coordinates = coordinates };

        coordinates[1].M = 201.0;

        Assert.Multiple(() =>
        {
            Assert.That(circularString.Coordinates, Is.SameAs(coordinates));
            Assert.That(circularString.Coordinates, Has.Length.EqualTo(3));
            Assert.That(circularString.Coordinates[0].Z, Is.EqualTo(10.0));
            Assert.That(circularString.Coordinates[1].M, Is.EqualTo(201.0));
            Assert.That(circularString.Coordinates[2].Z, Is.EqualTo(12.0));
            Assert.That(circularString.Coordinates[2].M, Is.EqualTo(102.0));
        });
    }

    [Test]
    public void CompoundCurveRetainsLinearAndCircularComponentsInSourceOrder()
    {
        Geometry line = new Geometry
        {
            GeometryType = GeometryType.LineString,
            Point = null,
            LineString = new LineString
            {
                Coordinates = new[]
                {
                    new Coordinate { X = 0.0, Y = 0.0 },
                    new Coordinate { X = 1.0, Y = 0.0 }
                }
            }
        };
        Geometry circular = new Geometry
        {
            GeometryType = GeometryType.CircularString,
            Point = null,
            CircularString = new CircularString
            {
                Coordinates = new[]
                {
                    new Coordinate { X = 1.0, Y = 0.0 },
                    new Coordinate { X = 2.0, Y = 1.0 },
                    new Coordinate { X = 3.0, Y = 0.0 }
                }
            }
        };
        Geometry[] curves = new[] { line, circular };
        CompoundCurve compoundCurve = new CompoundCurve { Curves = curves };

        Assert.Multiple(() =>
        {
            Assert.That(compoundCurve.Curves, Is.SameAs(curves));
            Assert.That(compoundCurve.Curves[0], Is.SameAs(line));
            Assert.That(compoundCurve.Curves[1], Is.SameAs(circular));
            Assert.That(compoundCurve.Curves[0].GeometryType, Is.EqualTo(GeometryType.LineString));
            Assert.That(compoundCurve.Curves[1].GeometryType, Is.EqualTo(GeometryType.CircularString));
            Assert.That(compoundCurve.Curves[0].LineString?.Coordinates[1].X, Is.EqualTo(1.0));
            Assert.That(compoundCurve.Curves[1].CircularString?.Coordinates[0].X, Is.EqualTo(1.0));
        });
    }

    [Test]
    public void CurvePolygonMultiCurveAndMultiSurfaceRetainTypedGeometryComponents()
    {
        Geometry ring = new Geometry
        {
            GeometryType = GeometryType.CircularString,
            Point = null,
            CircularString = new CircularString()
        };
        Geometry hole = new Geometry
        {
            GeometryType = GeometryType.CompoundCurve,
            Point = null,
            CompoundCurve = new CompoundCurve()
        };
        Geometry polygon = new Geometry
        {
            GeometryType = GeometryType.Polygon,
            Point = null,
            Polygon = new Polygon()
        };
        Geometry triangle = new Geometry
        {
            GeometryType = GeometryType.Triangle,
            Point = null,
            Triangle = new Triangle()
        };
        Geometry[] holes = new[] { hole };
        Geometry[] curves = new[] { ring, hole };
        Geometry[] surfaces = new[] { polygon, triangle };
        CurvePolygon curvePolygon = new CurvePolygon { Shell = ring, Holes = holes };
        MultiCurve multiCurve = new MultiCurve { Curves = curves };
        MultiSurface multiSurface = new MultiSurface { Surfaces = surfaces };

        curves[0] = hole;

        Assert.Multiple(() =>
        {
            Assert.That(curvePolygon.Shell, Is.SameAs(ring));
            Assert.That(curvePolygon.Holes, Is.SameAs(holes));
            Assert.That(curvePolygon.Holes[0], Is.SameAs(hole));
            Assert.That(multiCurve.Curves, Is.SameAs(curves));
            Assert.That(multiCurve.Curves[0], Is.SameAs(hole));
            Assert.That(multiSurface.Surfaces, Is.SameAs(surfaces));
            Assert.That(multiSurface.Surfaces[0], Is.SameAs(polygon));
            Assert.That(multiSurface.Surfaces[1], Is.SameAs(triangle));
        });
    }
}
