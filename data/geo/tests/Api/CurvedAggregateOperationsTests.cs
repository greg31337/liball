using geo;

namespace geo.Tests;

public class CurvedAggregateOperationsTests
{
    [Test]
    public void FactoryBuildsOwningCurvedFamiliesAndRejectsDiscontinuity()
    {
        GeometryFactory factory = new GeometryFactory(
            new GeometryFactoryOptions { SpatialReferenceId = 4326 });
        Geometry line = factory.CreateLineString([
            CoordinateValue(0.0, 0.0), CoordinateValue(2.0, 0.0)
        ]);
        Geometry arc = factory.CreateCircularString([
            CoordinateValue(2.0, 0.0), CoordinateValue(1.0, 1.0),
            CoordinateValue(0.0, 0.0)
        ]);
        Geometry compound = factory.CreateCompoundCurve([line, arc]);
        Geometry polygon = factory.CreateCurvePolygon(compound, []);
        Geometry multiCurve = factory.CreateMultiCurve([line, arc, compound]);
        Geometry multiSurface = factory.CreateMultiSurface([polygon]);
        Geometry collection = factory.CreateGeometryCollection([
            multiCurve, multiSurface
        ]);

        line.LineString!.Coordinates[0].X = 99.0;
        arc.CircularString!.Coordinates[0].X = 99.0;
        compound.CompoundCurve!.Curves[0].LineString!.Coordinates[0].X = 99.0;

        Geometry disconnected = factory.CreateLineString([
            CoordinateValue(5.0, 0.0), CoordinateValue(6.0, 0.0)
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(polygon.CurvePolygon!.Shell!.CompoundCurve!
                .Curves[0].LineString!.Coordinates[0].X, Is.Zero);
            Assert.That(multiCurve.MultiCurve!.Curves, Has.Length.EqualTo(3));
            Assert.That(multiSurface.MultiSurface!.Surfaces,
                Has.Length.EqualTo(1));
            Assert.That(collection.GeometryCollection!.Geometries,
                Has.Length.EqualTo(2));
            Assert.That(collection.SpatialReferenceId, Is.EqualTo(4326));
            Assert.Throws<InvalidGeometryException>(() =>
                factory.CreateCompoundCurve([line, disconnected]));
            Assert.Throws<InvalidGeometryException>(() =>
                factory.CreateCurvePolygon(line, []));
            Assert.Throws<InvalidGeometryException>(() =>
                factory.CreateMultiCurve([polygon]));
            Assert.Throws<InvalidGeometryException>(() =>
                factory.CreateMultiSurface([line]));
            Assert.Throws<InvalidGeometryException>(() =>
                factory.CreateCircularString([
                    CoordinateValue(0.0, 0.0),
                    CoordinateValue(1.0, 1.0)
                ]));
        });
    }

    [Test]
    public void CurvedAggregateMeasuresUseExactArcContributions()
    {
        GeometryFactory factory = Factory();
        Geometry shell = SemicircleRing(factory);
        Geometry curvePolygon = factory.CreateCurvePolygon(shell, []);
        Geometry extraArc = factory.CreateCircularString([
            CoordinateValue(0.0, 0.0), CoordinateValue(1.0, 1.0),
            CoordinateValue(2.0, 0.0)
        ]);
        Geometry multiCurve = factory.CreateMultiCurve([shell, extraArc]);
        Geometry rectangle = factory.CreateRectangle(
            CoordinateValue(4.0, 0.0), CoordinateValue(6.0, 1.0));
        Geometry multiSurface = factory.CreateMultiSurface([
            curvePolygon, rectangle
        ]);
        GeometryMeasures measures = new GeometryMeasures();

        Assert.Multiple(() =>
        {
            Assert.That(measures.Length(shell),
                Is.EqualTo(2.0 + Math.PI).Within(1.0e-13));
            Assert.That(measures.Area(curvePolygon),
                Is.EqualTo(Math.PI / 2.0).Within(1.0e-13));
            Assert.That(measures.Perimeter(curvePolygon),
                Is.EqualTo(2.0 + Math.PI).Within(1.0e-13));
            Assert.That(measures.Length(multiCurve),
                Is.EqualTo(2.0 + Math.PI * 2.0).Within(1.0e-13));
            Assert.That(measures.Area(multiSurface),
                Is.EqualTo(2.0 + Math.PI / 2.0).Within(1.0e-13));
            Assert.That(measures.Perimeter(multiSurface),
                Is.EqualTo(8.0 + Math.PI).Within(1.0e-13));
            Assert.That(measures.Length(multiSurface),
                Is.EqualTo(8.0 + Math.PI).Within(1.0e-13));
        });
    }

    [Test]
    public void InspectorBuildsExactCurvedBoundariesAndChecksContinuity()
    {
        GeometryFactory factory = Factory();
        Geometry shell = SemicircleRing(factory);
        Geometry curvePolygon = factory.CreateCurvePolygon(shell, []);
        Geometry rectangle = factory.CreateRectangle(
            CoordinateValue(4.0, 0.0), CoordinateValue(6.0, 1.0));
        Geometry multiSurface = factory.CreateMultiSurface([
            curvePolygon, rectangle
        ]);
        Geometry openArc = factory.CreateCircularString([
            CoordinateValue(10.0, 0.0), CoordinateValue(11.0, 1.0),
            CoordinateValue(12.0, 0.0)
        ]);
        Geometry multiCurve = factory.CreateMultiCurve([shell, openArc]);
        GeometryInspector inspector = new GeometryInspector();

        Geometry curveBoundary = inspector.GetBoundary(
            curvePolygon, BoundaryNodeRule.Mod2);
        Geometry surfaceBoundary = inspector.GetBoundary(
            multiSurface, BoundaryNodeRule.Mod2);
        Geometry endpointBoundary = inspector.GetBoundary(
            multiCurve, BoundaryNodeRule.Mod2);

        Assert.Multiple(() =>
        {
            Assert.That(inspector.IsRing(shell), Is.True);
            Assert.That(inspector.IsClosed(shell), Is.True);
            Assert.That(curveBoundary.GeometryType,
                Is.EqualTo(GeometryType.MultiCurve));
            Assert.That(curveBoundary.MultiCurve!.Curves[0].GeometryType,
                Is.EqualTo(GeometryType.CompoundCurve));
            Assert.That(surfaceBoundary.MultiCurve!.Curves,
                Has.Length.EqualTo(2));
            Assert.That(endpointBoundary.MultiPoint!.Points,
                Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void CurveAwareEditsPreserveContinuityOrientationAndNarrowTypes()
    {
        GeometryFactory factory = Factory();
        Geometry shell = SemicircleRing(factory);
        Geometry clockwise = new GeometryEditor(
            new GeometryFactoryOptions()).Reverse(shell);
        Geometry polygon = factory.CreateCurvePolygon(clockwise, []);
        GeometryEditor editor = new GeometryEditor(
            new GeometryFactoryOptions());

        Geometry normalized = editor.Normalize(clockwise);
        Geometry normalizedAgain = editor.Normalize(normalized);
        Geometry oriented = editor.OrientRings(
            polygon, RingOrientation.CounterClockwise);
        Geometry transformed = editor.ApplyAffineTransform(
            shell, new AffineTransform { M02 = 5.0, M12 = -2.0 });
        Geometry combinedCurves = editor.Combine([
            factory.CreateLineString([
                CoordinateValue(0.0, 0.0), CoordinateValue(1.0, 0.0)
            ]),
            factory.CreateCircularString([
                CoordinateValue(2.0, 0.0), CoordinateValue(3.0, 1.0),
                CoordinateValue(4.0, 0.0)
            ])
        ]);
        Geometry combinedSurfaces = editor.Combine([
            polygon,
            factory.CreateRectangle(
                CoordinateValue(4.0, 0.0), CoordinateValue(5.0, 1.0))
        ]);

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() =>
                CurveAggregateAlgorithms.ValidateCompoundValue(
                    normalized.CompoundCurve!));
            Assert.That(GeometryNormalization.CompareGeometryValues(
                normalized, normalizedAgain), Is.Zero);
            Assert.That(CurveAggregateAlgorithms.GetSignedCurveRingArea(
                oriented.CurvePolygon!.Shell!), Is.GreaterThan(0.0));
            Assert.DoesNotThrow(() =>
                CurveAggregateAlgorithms.ValidateCompoundValue(
                    transformed.CompoundCurve!));
            Assert.That(combinedCurves.GeometryType,
                Is.EqualTo(GeometryType.MultiCurve));
            Assert.That(combinedSurfaces.GeometryType,
                Is.EqualTo(GeometryType.MultiSurface));
        });
    }

    [Test]
    public void MultiSurfaceAggregatesTinAndPolyhedralPatches()
    {
        GeometryFactory factory = Factory();
        LinearRing firstRing = new LinearRing
        {
            Coordinates = [
                CoordinateValue(0.0, 0.0), CoordinateValue(1.0, 0.0),
                CoordinateValue(0.0, 1.0), CoordinateValue(0.0, 0.0)
            ]
        };
        LinearRing secondRing = new LinearRing
        {
            Coordinates = [
                CoordinateValue(3.0, 0.0), CoordinateValue(4.0, 0.0),
                CoordinateValue(3.0, 1.0), CoordinateValue(3.0, 0.0)
            ]
        };
        LinearRing adjacentRing = new LinearRing
        {
            Coordinates = [
                CoordinateValue(1.0, 0.0), CoordinateValue(1.0, 1.0),
                CoordinateValue(0.0, 1.0), CoordinateValue(1.0, 0.0)
            ]
        };
        Geometry tin = new Geometry
        {
            GeometryType = GeometryType.Tin,
            Point = null,
            Tin = new Tin
            {
                Triangles = [
                    new Triangle { Shell = firstRing },
                    new Triangle { Shell = adjacentRing }
                ]
            }
        };
        Geometry polyhedral = new Geometry
        {
            GeometryType = GeometryType.PolyhedralSurface,
            Point = null,
            PolyhedralSurface = new PolyhedralSurface
            {
                Patches = [new Polygon { Shell = secondRing }]
            }
        };
        Geometry multiSurface = factory.CreateMultiSurface([
            tin, polyhedral
        ]);
        Geometry linearized = new GeometrySimplifier().LinearizeCurves(
            multiSurface, new CurveLinearizationParameters());
        Geometry boundary = new GeometryInspector().GetBoundary(
            multiSurface, BoundaryNodeRule.Mod2);
        GeometryMeasures measures = new GeometryMeasures();
        ValidationResult validity =
            GeometryValidityChecker.ValidateGeometry(multiSurface);

        Assert.Multiple(() =>
        {
            Assert.That(validity.IsValid, Is.True, validity.Message);
            Assert.That(measures.Area(multiSurface), Is.EqualTo(1.5));
            Assert.That(measures.Length(multiSurface),
                Is.EqualTo(6.0 + Math.Sqrt(18.0)).Within(1.0e-14));
            Assert.That(linearized.MultiPolygon!.Polygons,
                Has.Length.EqualTo(3));
            Assert.That(boundary.MultiCurve!.Curves,
                Has.Length.EqualTo(2));
        });
    }

    private static GeometryFactory Factory()
    {
        return new GeometryFactory(new GeometryFactoryOptions());
    }

    private static Geometry SemicircleRing(GeometryFactory factory)
    {
        Geometry line = factory.CreateLineString([
            CoordinateValue(0.0, 0.0), CoordinateValue(2.0, 0.0)
        ]);
        Geometry arc = factory.CreateCircularString([
            CoordinateValue(2.0, 0.0), CoordinateValue(1.0, 1.0),
            CoordinateValue(0.0, 0.0)
        ]);
        return factory.CreateCompoundCurve([line, arc]);
    }

    private static Coordinate CoordinateValue(double x, double y)
    {
        return new Coordinate { X = x, Y = y };
    }
}
