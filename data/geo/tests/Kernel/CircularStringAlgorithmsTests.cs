using geo;

namespace geo.Tests;

public class CircularStringAlgorithmsTests
{
    [Test]
    public void MinorAndMajorArcsSelectTheSweepContainingTheControlPoint()
    {
        Geometry minor = CircularGeometry([
            CoordinateValue(1.0, 0.0),
            CoordinateValue(Math.Sqrt(0.5), Math.Sqrt(0.5)),
            CoordinateValue(0.0, 1.0)
        ]);
        Geometry major = CircularGeometry([
            CoordinateValue(1.0, 0.0),
            CoordinateValue(-1.0, 0.0),
            CoordinateValue(0.0, 1.0)
        ]);
        GeometryMeasures measures = new GeometryMeasures();
        GeometryInspector inspector = new GeometryInspector();

        Envelope minorEnvelope = inspector.GetEnvelope(minor);
        Envelope majorEnvelope = inspector.GetEnvelope(major);

        Assert.Multiple(() =>
        {
            Assert.That(measures.Length(minor),
                Is.EqualTo(Math.PI / 2.0).Within(1.0e-14));
            Assert.That(measures.Length(major),
                Is.EqualTo(Math.PI * 1.5).Within(1.0e-14));
            Assert.That(measures.Area(minor), Is.Zero);
            Assert.That(measures.SurfaceArea3D(minor), Is.Zero);
            Assert.That(measures.Perimeter(minor), Is.Zero);
            AssertEnvelope(minorEnvelope, 0.0, 0.0, 1.0, 1.0);
            AssertEnvelope(majorEnvelope, -1.0, -1.0, 1.0, 1.0);
            Assert.That(CircularStringAlgorithms.GetSignedAreaContribution(
                minor.CircularString!, CoordinateValue(0.0, 0.0)),
                Is.EqualTo(Math.PI / 4.0).Within(1.0e-14));
            Assert.That(CircularStringAlgorithms.GetSignedAreaContribution(
                major.CircularString!, CoordinateValue(0.0, 0.0)),
                Is.EqualTo(-Math.PI * 0.75).Within(1.0e-14));
        });
    }

    [Test]
    public void ClosedCircleHasExactLengthEnvelopeAndAreaContribution()
    {
        Geometry circle = CircularGeometry([
            CoordinateValue(1.0, 0.0), CoordinateValue(0.0, 1.0),
            CoordinateValue(-1.0, 0.0), CoordinateValue(0.0, -1.0),
            CoordinateValue(1.0, 0.0)
        ]);
        GeometryMeasures measures = new GeometryMeasures();
        GeometryInspector inspector = new GeometryInspector();

        Assert.Multiple(() =>
        {
            Assert.That(inspector.IsClosed(circle), Is.True);
            Assert.That(measures.Length(circle),
                Is.EqualTo(Math.PI * 2.0).Within(1.0e-14));
            AssertEnvelope(inspector.GetEnvelope(circle),
                -1.0, -1.0, 1.0, 1.0);
            Assert.That(CircularStringAlgorithms.GetSignedAreaContribution(
                circle.CircularString!, circle.CircularString!.Coordinates[0]),
                Is.EqualTo(Math.PI).Within(1.0e-14));
        });
    }

    [Test]
    public void AdaptiveLinearizationPreservesControlAndInterpolatesEachArcPart()
    {
        Geometry source = CircularGeometry([
            CoordinateValue(1.0, 0.0, 0.0, 0.0),
            CoordinateValue(Math.Sqrt(0.5), Math.Sqrt(0.5), 10.0, 20.0),
            CoordinateValue(0.0, 1.0, 30.0, 50.0)
        ]);
        CurveLinearizationParameters parameters = new CurveLinearizationParameters
        {
            Mode = CurveLinearizationMode.MaximumAngle,
            Tolerance = Math.PI / 8.0,
            MaximumSegments = 20
        };

        Coordinate[] result = CurveLinearizer.LinearizeCurveGeometry(
            source, parameters);

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Length.EqualTo(5));
            AssertCoordinate(result[0], 1.0, 0.0, 0.0, 0.0);
            AssertCoordinate(result[1], Math.Cos(Math.PI / 8.0),
                Math.Sin(Math.PI / 8.0), 5.0, 10.0);
            AssertCoordinate(result[2], Math.Sqrt(0.5),
                Math.Sqrt(0.5), 10.0, 20.0);
            AssertCoordinate(result[3], Math.Cos(Math.PI * 3.0 / 8.0),
                Math.Sin(Math.PI * 3.0 / 8.0), 20.0, 35.0);
            AssertCoordinate(result[4], 0.0, 1.0, 30.0, 50.0);
        });
    }

    [TestCase(CurveLinearizationMode.MaximumDeviation, 0.05)]
    [TestCase(CurveLinearizationMode.MaximumSegmentLength, 0.5)]
    public void AdaptiveLinearizationHonorsDistanceTolerance(
        CurveLinearizationMode mode, double tolerance)
    {
        Geometry source = CircularGeometry([
            CoordinateValue(10.0, 0.0), CoordinateValue(0.0, 10.0),
            CoordinateValue(-10.0, 0.0)
        ]);
        CurveLinearizationParameters parameters = new CurveLinearizationParameters
        {
            Mode = mode,
            Tolerance = tolerance,
            MaximumSegments = 1000
        };

        Coordinate[] result = CurveLinearizer.LinearizeCurveGeometry(
            source, parameters);

        int index = 0;
        while (index + 1 < result.Length)
        {
            double chord = Distance(result[index], result[index + 1]);
            if (mode == CurveLinearizationMode.MaximumSegmentLength)
            {
                Assert.That(chord, Is.LessThanOrEqualTo(tolerance + 1.0e-12));
            }
            else
            {
                double halfChord = chord * 0.5;
                double deviation = 10.0 -
                    Math.Sqrt(100.0 - halfChord * halfChord);
                Assert.That(deviation,
                    Is.LessThanOrEqualTo(tolerance + 1.0e-12));
            }
            index++;
        }
    }

    [Test]
    public void CollinearControlsRemainPiecewiseLinearAndInvalidClosureIsRejected()
    {
        Geometry collinear = CircularGeometry([
            CoordinateValue(0.0, 0.0), CoordinateValue(1.0, 0.0),
            CoordinateValue(3.0, 0.0)
        ]);
        Geometry malformed = CircularGeometry([
            CoordinateValue(0.0, 0.0), CoordinateValue(0.0, 0.0)
        ]);
        CurveLinearizationParameters parameters = new CurveLinearizationParameters();
        GeometryMeasures measures = new GeometryMeasures();
        GeometryInspector inspector = new GeometryInspector();

        Coordinate[] result = CurveLinearizer.LinearizeCurveGeometry(
            collinear, parameters);

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Length.EqualTo(3));
            Assert.That(result[1].X, Is.EqualTo(1.0));
            Assert.That(measures.Length(collinear), Is.EqualTo(3.0));
            Assert.That(CircularStringAlgorithms.GetSignedAreaContribution(
                collinear.CircularString!, CoordinateValue(0.0, 0.0)), Is.Zero);
            Assert.Throws<InvalidGeometryException>(() =>
                inspector.IsClosed(malformed));
        });
    }

    private static Geometry CircularGeometry(Coordinate[] coordinates)
    {
        return new Geometry
        {
            GeometryType = GeometryType.CircularString,
            Point = null,
            CircularString = new CircularString { Coordinates = coordinates },
            CoordinateLayout = HasOptionalOrdinates(coordinates) ?
                CoordinateLayout.XYZM : CoordinateLayout.XY
        };
    }

    private static bool HasOptionalOrdinates(Coordinate[] coordinates)
    {
        return coordinates.Length > 0 &&
            (!double.IsNaN(coordinates[0].Z) ||
             !double.IsNaN(coordinates[0].M));
    }

    private static Coordinate CoordinateValue(double x, double y)
    {
        return new Coordinate { X = x, Y = y };
    }

    private static Coordinate CoordinateValue(
        double x, double y, double z, double m)
    {
        return new Coordinate { X = x, Y = y, Z = z, M = m };
    }

    private static double Distance(Coordinate first, Coordinate second)
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
            Assert.That(actual.MinX, Is.EqualTo(minimumX).Within(1.0e-14));
            Assert.That(actual.MinY, Is.EqualTo(minimumY).Within(1.0e-14));
            Assert.That(actual.MaxX, Is.EqualTo(maximumX).Within(1.0e-14));
            Assert.That(actual.MaxY, Is.EqualTo(maximumY).Within(1.0e-14));
        });
    }

    private static void AssertCoordinate(
        Coordinate actual, double x, double y, double z, double m)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.X, Is.EqualTo(x).Within(1.0e-14));
            Assert.That(actual.Y, Is.EqualTo(y).Within(1.0e-14));
            Assert.That(actual.Z, Is.EqualTo(z).Within(1.0e-14));
            Assert.That(actual.M, Is.EqualTo(m).Within(1.0e-14));
        });
    }
}
