using geo;

namespace geo.Tests;

public class CurvedGeometryConformanceTests
{
    [Test]
    public void ArcDirectionAndControlPointSelectMinorAndMajorSweeps()
    {
        GeometryFactory factory = Factory();
        double diagonal = Math.Sqrt(0.5);
        Geometry counterClockwise = factory.CreateCircularString([
            CoordinateValue(1.0, 0.0),
            CoordinateValue(diagonal, diagonal),
            CoordinateValue(0.0, 1.0)
        ]);
        Geometry clockwise = factory.CreateCircularString([
            CoordinateValue(0.0, 1.0),
            CoordinateValue(diagonal, diagonal),
            CoordinateValue(1.0, 0.0)
        ]);
        Geometry major = factory.CreateCircularString([
            CoordinateValue(1.0, 0.0),
            CoordinateValue(-1.0, 0.0),
            CoordinateValue(0.0, 1.0)
        ]);
        GeometryMeasures measures = new GeometryMeasures();
        GeometryInspector inspector = new GeometryInspector();
        CurveLinearizationParameters parameters = new CurveLinearizationParameters
        {
            Mode = CurveLinearizationMode.MaximumAngle,
            Tolerance = Math.PI / 8.0,
            MaximumSegments = 100
        };
        GeometrySimplifier simplifier = new GeometrySimplifier();
        Geometry firstLine = simplifier.LinearizeCurves(
            counterClockwise, parameters);
        Geometry secondLine = simplifier.LinearizeCurves(
            clockwise, parameters);

        Assert.Multiple(() =>
        {
            Assert.That(measures.Length(counterClockwise),
                Is.EqualTo(Math.PI / 2.0).Within(1.0e-14));
            Assert.That(measures.Length(clockwise),
                Is.EqualTo(Math.PI / 2.0).Within(1.0e-14));
            Assert.That(measures.Length(major),
                Is.EqualTo(Math.PI * 1.5).Within(1.0e-14));
            AssertEnvelope(inspector.GetEnvelope(counterClockwise),
                0.0, 0.0, 1.0, 1.0);
            AssertEnvelope(inspector.GetEnvelope(major),
                -1.0, -1.0, 1.0, 1.0);
            Assert.That(firstLine.LineString!.Coordinates[1].Y,
                Is.GreaterThan(firstLine.LineString.Coordinates[0].Y));
            Assert.That(secondLine.LineString!.Coordinates[1].X,
                Is.GreaterThan(secondLine.LineString.Coordinates[0].X));
            Assert.That(firstLine.LineString.Coordinates[0].X,
                Is.EqualTo(secondLine.LineString.Coordinates[^1].X));
            Assert.That(firstLine.LineString.Coordinates[^1].Y,
                Is.EqualTo(secondLine.LineString.Coordinates[0].Y));
        });
    }

    [Test]
    public void CompoundContinuityAndCurveRingClosureAreEnforced()
    {
        GeometryFactory factory = Factory();
        Geometry first = factory.CreateLineString([
            CoordinateValue(0.0, 0.0), CoordinateValue(1.0, 0.0)
        ]);
        Geometry arc = factory.CreateCircularString([
            CoordinateValue(1.0, 0.0), CoordinateValue(2.0, 1.0),
            CoordinateValue(3.0, 0.0)
        ]);
        Geometry closing = factory.CreateLineString([
            CoordinateValue(3.0, 0.0), CoordinateValue(1.5, -1.0),
            CoordinateValue(0.0, 0.0)
        ]);
        Geometry compound = factory.CreateCompoundCurve([
            first, arc, closing
        ]);
        Geometry polygon = factory.CreateCurvePolygon(compound, []);
        Geometry disconnected = factory.CreateLineString([
            CoordinateValue(4.0, 0.0), CoordinateValue(5.0, 0.0)
        ]);
        GeometryInspector inspector = new GeometryInspector();
        GeometryValidator validator = new GeometryValidator();

        Assert.Multiple(() =>
        {
            Assert.That(inspector.IsClosed(compound), Is.True);
            Assert.That(inspector.IsRing(compound), Is.True);
            Assert.That(validator.ValidateGeometry(compound).IsValid,
                Is.True);
            Assert.That(validator.ValidateGeometry(polygon).IsValid,
                Is.True);
            Assert.Throws<InvalidGeometryException>(() =>
                factory.CreateCompoundCurve([first, disconnected]));
            Assert.Throws<InvalidGeometryException>(() =>
                factory.CreateCurvePolygon(arc, []));
        });
    }

    [Test]
    public void CurvedMetricsAggregateExactArcContributions()
    {
        GeometryFactory factory = Factory();
        Geometry circle = factory.CreateCircularString([
            CoordinateValue(2.0, 0.0), CoordinateValue(0.0, 2.0),
            CoordinateValue(-2.0, 0.0), CoordinateValue(0.0, -2.0),
            CoordinateValue(2.0, 0.0)
        ]);
        Geometry polygon = factory.CreateCurvePolygon(circle, []);
        Geometry line = factory.CreateLineString([
            CoordinateValue(5.0, 0.0), CoordinateValue(8.0, 0.0)
        ]);
        Geometry multiCurve = factory.CreateMultiCurve([circle, line]);
        Geometry rectangle = factory.CreateRectangle(
            CoordinateValue(5.0, 0.0), CoordinateValue(7.0, 1.0));
        Geometry multiSurface = factory.CreateMultiSurface([
            polygon, rectangle
        ]);
        GeometryMeasures measures = new GeometryMeasures();
        GeometryInspector inspector = new GeometryInspector();

        Assert.Multiple(() =>
        {
            Assert.That(measures.Length(circle),
                Is.EqualTo(4.0 * Math.PI).Within(1.0e-13));
            Assert.That(measures.Area(polygon),
                Is.EqualTo(4.0 * Math.PI).Within(1.0e-13));
            Assert.That(measures.Perimeter(polygon),
                Is.EqualTo(4.0 * Math.PI).Within(1.0e-13));
            Assert.That(measures.Length(multiCurve),
                Is.EqualTo(4.0 * Math.PI + 3.0).Within(1.0e-13));
            Assert.That(measures.Area(multiSurface),
                Is.EqualTo(4.0 * Math.PI + 2.0).Within(1.0e-13));
            Assert.That(measures.Perimeter(multiSurface),
                Is.EqualTo(4.0 * Math.PI + 6.0).Within(1.0e-13));
            AssertEnvelope(inspector.GetEnvelope(circle),
                -2.0, -2.0, 2.0, 2.0);
            Assert.That(inspector.IsClosed(circle), Is.True);
            Assert.That(inspector.IsClosed(polygon), Is.True);
        });
    }

    [Test]
    public void CurvedFamiliesRoundTripAcrossWktAndWkb()
    {
        string[] values =
        [
            "SRID=4979;CIRCULARSTRING ZM " +
                "(1 0 1 10, 0 1 2 20, -1 0 3 30)",
            "SRID=4979;COMPOUNDCURVE ZM (" +
                "(0 0 1 10, 1 0 2 20), " +
                "CIRCULARSTRING ZM " +
                "(1 0 2 20, 2 1 3 30, 3 0 4 40))",
            "SRID=4979;CURVEPOLYGON ZM (" +
                "CIRCULARSTRING ZM " +
                "(2 0 1 10, 0 2 2 20, -2 0 3 30, " +
                "0 -2 4 40, 2 0 1 10))",
            "SRID=4979;MULTICURVE ZM (" +
                "(0 0 1 10, 1 0 2 20), " +
                "CIRCULARSTRING ZM " +
                "(2 0 3 30, 3 1 4 40, 4 0 5 50))",
            "SRID=4979;MULTISURFACE ZM (" +
                "CURVEPOLYGON ZM (CIRCULARSTRING ZM " +
                "(2 0 1 10, 0 2 2 20, -2 0 3 30, " +
                "0 -2 4 40, 2 0 1 10)), " +
                "((5 0 1 10, 6 0 1 10, 6 1 1 10, " +
                "5 1 1 10, 5 0 1 10)))"
        ];
        WktCodec textCodec = new WktCodec(
            new WktReadOptions { AllowSpatialReferenceId = true },
            new WktWriteOptions { IncludeSpatialReferenceId = true });
        WkbCodec binaryCodec = new WkbCodec(
            new WkbReadOptions { Flavor = WkbFlavor.Ewkb },
            new WkbWriteOptions
            {
                ByteOrder = ByteOrder.BigEndian,
                Flavor = WkbFlavor.Ewkb,
                IncludeSpatialReferenceId = true
            });
        GeometryPredicates predicates = new GeometryPredicates();

        foreach (string value in values)
        {
            Geometry source = textCodec.ReadWkt(value);
            Geometry textRoundTrip = textCodec.ReadWkt(
                textCodec.WriteWkt(source));
            Geometry binaryRoundTrip = binaryCodec.ReadWkb(
                binaryCodec.WriteWkb(source));

            Assert.Multiple(() =>
            {
                Assert.That(predicates.AreExactlyEqual(
                    source, textRoundTrip), Is.True, value);
                Assert.That(predicates.AreExactlyEqual(
                    source, binaryRoundTrip), Is.True, value);
                Assert.That(binaryRoundTrip.CoordinateLayout,
                    Is.EqualTo(CoordinateLayout.XYZM), value);
                Assert.That(binaryRoundTrip.SpatialReferenceId,
                    Is.EqualTo(4979), value);
            });
        }
    }

    [TestCase(CurveLinearizationMode.MaximumDeviation, 0.01)]
    [TestCase(CurveLinearizationMode.MaximumSegmentLength, 0.5)]
    [TestCase(CurveLinearizationMode.MaximumAngle, 0.1)]
    public void MajorArcLinearizationHonorsTheSelectedErrorBound(
        CurveLinearizationMode mode, double tolerance)
    {
        const double radius = 10.0;
        GeometryFactory factory = Factory();
        Geometry major = factory.CreateCircularString([
            CoordinateValue(radius, 0.0),
            CoordinateValue(-radius, 0.0),
            CoordinateValue(0.0, radius)
        ]);
        CurveLinearizationParameters parameters =
            new CurveLinearizationParameters
            {
                Mode = mode,
                Tolerance = tolerance,
                MaximumSegments = 10000
            };
        Geometry result = new GeometrySimplifier().LinearizeCurves(
            major, parameters);
        Coordinate[] coordinates = result.LineString!.Coordinates;

        int index = 0;
        while (index + 1 < coordinates.Length)
        {
            double chord = PlanarDistance(
                coordinates[index], coordinates[index + 1]);
            if (mode == CurveLinearizationMode.MaximumDeviation)
            {
                double halfChord = chord * 0.5;
                double deviation = radius - Math.Sqrt(
                    radius * radius - halfChord * halfChord);
                Assert.That(deviation,
                    Is.LessThanOrEqualTo(tolerance + 1.0e-12));
            }
            else if (mode ==
                CurveLinearizationMode.MaximumSegmentLength)
            {
                Assert.That(chord,
                    Is.LessThanOrEqualTo(tolerance + 1.0e-12));
            }
            else
            {
                double angle = 2.0 * Math.Asin(chord / (2.0 * radius));
                Assert.That(angle,
                    Is.LessThanOrEqualTo(tolerance + 1.0e-12));
            }
            index++;
        }
        Assert.That(coordinates, Has.Length.GreaterThan(3));
    }

    private static GeometryFactory Factory()
    {
        return new GeometryFactory(new GeometryFactoryOptions());
    }

    private static Coordinate CoordinateValue(double x, double y)
    {
        return new Coordinate { X = x, Y = y };
    }

    private static double PlanarDistance(
        Coordinate first, Coordinate second)
    {
        double x = second.X - first.X;
        double y = second.Y - first.Y;
        return Math.Sqrt(x * x + y * y);
    }

    private static void AssertEnvelope(
        Envelope actual, double minimumX, double minimumY,
        double maximumX, double maximumY)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.MinX,
                Is.EqualTo(minimumX).Within(1.0e-14));
            Assert.That(actual.MinY,
                Is.EqualTo(minimumY).Within(1.0e-14));
            Assert.That(actual.MaxX,
                Is.EqualTo(maximumX).Within(1.0e-14));
            Assert.That(actual.MaxY,
                Is.EqualTo(maximumY).Within(1.0e-14));
        });
    }
}
