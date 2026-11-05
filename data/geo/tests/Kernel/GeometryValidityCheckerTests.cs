using geo;
using NtsIsValidOp = NetTopologySuite.Operation.Valid.IsValidOp;
using NtsWktReader = NetTopologySuite.IO.WKTReader;

namespace geo.Tests;

public class GeometryValidityCheckerTests
{
    [TestCase("POINT (1 2)")]
    [TestCase("MULTIPOINT ((1 2), (1 2))")]
    [TestCase("LINESTRING (0 0, 2 2, 0 2, 2 0)")]
    [TestCase("MULTILINESTRING ((0 0, 2 2), (0 2, 2 0))")]
    public void StructurallyValidNonAreaValuesAreValid(string text)
    {
        ValidationResult result = Validate(text);

        AssertValid(result);
    }

    [TestCase("POLYGON EMPTY")]
    [TestCase("POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0))")]
    [TestCase("POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), " +
        "(2 2, 2 4, 4 4, 4 2, 2 2))")]
    [TestCase("POLYGON ((0 0, 5 0, 10 0, 10 10, 0 10, 0 0), " +
        "(5 0, 7 2, 5 4, 3 2, 5 0))")]
    [TestCase("POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), " +
        "(2 2, 2 4, 4 4, 4 2, 2 2), " +
        "(4 4, 4 6, 6 6, 6 4, 4 4))")]
    public void EmptySimpleAndSingleTouchPolygonsAreValid(string text)
    {
        ValidationResult result = Validate(text);

        AssertValid(result);
    }

    [Test]
    public void LinearRingValidityChecksItsOwnTopology()
    {
        Geometry simple = RingGeometry(
            CoordinateValue(0.0, 0.0),
            CoordinateValue(2.0, 0.0),
            CoordinateValue(2.0, 2.0),
            CoordinateValue(0.0, 2.0),
            CoordinateValue(0.0, 0.0));
        Geometry crossing = RingGeometry(
            CoordinateValue(0.0, 0.0),
            CoordinateValue(2.0, 2.0),
            CoordinateValue(0.0, 2.0),
            CoordinateValue(2.0, 0.0),
            CoordinateValue(0.0, 0.0));

        ValidationResult simpleResult =
            GeometryValidityChecker.ValidateGeometry(simple);
        ValidationResult crossingResult =
            GeometryValidityChecker.ValidateGeometry(crossing);

        Assert.Multiple(() =>
        {
            AssertValid(simpleResult);
            AssertFailure(crossingResult,
                ValidationError.SelfIntersection,
                "LinearRing/Segments[2]");
            AssertCoordinate(crossingResult.Coordinate, 1.0, 1.0);
        });
    }

    [Test]
    public void RingSelfTouchIsDistinguishedFromProperCrossing()
    {
        ValidationResult result = Validate(
            "POLYGON ((0 0, 4 0, 2 2, 4 4, 0 4, 2 2, 0 0))");

        Assert.Multiple(() =>
        {
            AssertFailure(result, ValidationError.RingSelfTouch,
                "Polygon/Shell/Segments[4]");
            AssertCoordinate(result.Coordinate, 2.0, 2.0);
        });
    }

    [Test]
    public void RingVertexOnNonAdjacentSegmentIsNonNoded()
    {
        ValidationResult result = Validate(
            "POLYGON ((0 0, 4 0, 4 4, 2 0, 0 4, 0 0))");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Error,
                Is.EqualTo(ValidationError.NonNodedIntersection));
            AssertCoordinate(result.Coordinate, 2.0, 0.0);
            Assert.That(result.ComponentPath,
                Does.StartWith("Polygon/Shell/Segments["));
        });
    }

    [Test]
    public void HoleOutsideShellReportsHoleWitnessAndPath()
    {
        ValidationResult result = Validate(
            "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), " +
            "(20 20, 20 22, 22 22, 22 20, 20 20))");

        Assert.Multiple(() =>
        {
            AssertFailure(result, ValidationError.HoleOutsideShell,
                "Polygon/Holes[0]");
            AssertCoordinate(result.Coordinate, 20.0, 20.0);
        });
    }

    [Test]
    public void NestedHolesReportTheContainedHole()
    {
        ValidationResult result = Validate(
            "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), " +
            "(2 2, 2 8, 8 8, 8 2, 2 2), " +
            "(3 3, 3 4, 4 4, 4 3, 3 3))");

        Assert.Multiple(() =>
        {
            AssertFailure(result, ValidationError.NestedHoles,
                "Polygon/Holes[1]");
            AssertCoordinate(result.Coordinate, 3.0, 3.0);
        });
    }

    [Test]
    public void RotatedAndReversedRingsAreDetectedAsDuplicates()
    {
        ValidationResult result = Validate(
            "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), " +
            "(2 2, 2 4, 4 4, 4 2, 2 2), " +
            "(4 4, 2 4, 2 2, 4 2, 4 4))");

        Assert.Multiple(() =>
        {
            AssertFailure(result, ValidationError.DuplicateRings,
                "Polygon/Holes[1]");
            AssertCoordinate(result.Coordinate, 4.0, 4.0);
        });
    }

    [TestCase(
        "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), " +
        "(5 -1, 7 3, 3 3, 5 -1))")]
    [TestCase(
        "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), " +
        "(1 1, 1 6, 6 6, 6 1, 1 1), " +
        "(4 4, 4 8, 8 8, 8 4, 4 4))")]
    [TestCase(
        "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), " +
        "(2 0, 4 0, 4 2, 2 2, 2 0))")]
    public void CrossingPolygonRingsReportNonNodedIntersection(string text)
    {
        ValidationResult result = Validate(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Error,
                Is.EqualTo(ValidationError.NonNodedIntersection));
            Assert.That(result.Coordinate, Is.Not.Null);
            Assert.That(result.ComponentPath,
                Does.StartWith("Polygon/Holes["));
            Assert.That(result.ComponentPath, Does.Contain("/Segments["));
        });
    }

    [Test]
    public void TwoTouchesBetweenShellAndHoleDisconnectInterior()
    {
        ValidationResult result = Validate(
            "POLYGON ((0 0, 5 0, 10 0, 10 10, 5 10, 0 10, 0 0), " +
            "(5 0, 9 5, 5 10, 1 5, 5 0))");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Error,
                Is.EqualTo(ValidationError.DisconnectedInterior));
            Assert.That(result.Coordinate, Is.Not.Null);
            Assert.That(result.ComponentPath,
                Is.EqualTo("Polygon/Holes[0]"));
        });
    }

    [Test]
    public void RingTouchCycleDisconnectsInterior()
    {
        ValidationResult result = Validate(
            "POLYGON ((0 0, 2 0, 8 0, 10 0, 10 10, 0 10, 0 0), " +
            "(2 0, 5 5, 2 4, 2 0), " +
            "(8 0, 8 4, 5 5, 8 0))");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Error,
                Is.EqualTo(ValidationError.DisconnectedInterior));
            AssertCoordinate(result.Coordinate, 5.0, 5.0);
            Assert.That(result.ComponentPath,
                Is.EqualTo("Polygon/Holes[1]"));
        });
    }

    [TestCase("POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0))")]
    [TestCase("POLYGON ((0 0, 5 0, 10 0, 10 10, 0 10, 0 0), (5 0, 7 2, 5 4, 3 2, 5 0))")]
    [TestCase("POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), (20 20, 20 22, 22 22, 22 20, 20 20))")]
    [TestCase("POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), (2 2, 2 8, 8 8, 8 2, 2 2), (3 3, 3 4, 4 4, 4 3, 3 3))")]
    [TestCase("POLYGON ((0 0, 5 0, 10 0, 10 10, 5 10, 0 10, 0 0), (5 0, 9 5, 5 10, 1 5, 5 0))")]
    [TestCase("POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), (5 0, 7 2, 5 4, 3 2, 5 0))")]
    [TestCase("POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), (2 2, 2 4, 4 4, 4 2, 2 2), (4 4, 4 6, 6 6, 6 4, 4 4))")]
    [TestCase("POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), (2 2, 2 4, 4 4, 4 2, 2 2), (4 4, 2 4, 2 2, 4 2, 4 4))")]
    [TestCase("POLYGON ((0 0, 4 0, 2 2, 4 4, 0 4, 2 2, 0 0))")]
    [TestCase("POLYGON ((0 0, 5 0, 10 0, 10 10, 5 10, 0 10, 0 0), (5 0, 12 5, 5 10, 8 5, 5 0))")]
    [TestCase("POLYGON ((0 0, 2 0, 8 0, 10 0, 10 10, 0 10, 0 0), (2 0, 5 5, 2 4, 2 0), (8 0, 8 4, 5 5, 8 0))")]
    public void RepresentativePolygonValidityMatchesNetTopologySuite(
        string text)
    {
        bool reference = new NtsIsValidOp(
            new NtsWktReader().Read(text)).IsValid;

        ValidationResult result = Validate(text);

        Assert.That(result.IsValid, Is.EqualTo(reference));
    }

    [Test]
    public void StructuralFailureIsReturnedBeforeTopologyAnalysis()
    {
        Geometry openRing = RingGeometry(
            CoordinateValue(0.0, 0.0),
            CoordinateValue(2.0, 0.0),
            CoordinateValue(2.0, 2.0),
            CoordinateValue(0.0, 2.0));

        ValidationResult result =
            GeometryValidityChecker.ValidateGeometry(openRing);

        AssertFailure(result, ValidationError.RingNotClosed,
            "LinearRing/Coordinates");
    }

    private static Geometry RingGeometry(params Coordinate[] coordinates)
    {
        return new Geometry
        {
            GeometryType = GeometryType.LinearRing,
            Point = null,
            LinearRing = new LinearRing { Coordinates = coordinates }
        };
    }

    private static Coordinate CoordinateValue(double x, double y)
    {
        return new Coordinate { X = x, Y = y };
    }

    private static ValidationResult Validate(string text)
    {
        return GeometryValidityChecker.ValidateGeometry(Read(text));
    }

    private static void AssertCoordinate(
        Coordinate? coordinate, double x, double y)
    {
        Assert.That(coordinate, Is.Not.Null);
        Assert.That(coordinate!.X, Is.EqualTo(x).Within(1e-12));
        Assert.That(coordinate.Y, Is.EqualTo(y).Within(1e-12));
    }

    private static void AssertValid(ValidationResult result)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Error, Is.EqualTo(ValidationError.None));
            Assert.That(result.Message, Is.Empty);
            Assert.That(result.Coordinate, Is.Null);
            Assert.That(result.ComponentPath, Is.Empty);
        });
    }

    private static void AssertFailure(
        ValidationResult result, ValidationError error, string path)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Error, Is.EqualTo(error));
            Assert.That(result.Message, Is.Not.Empty);
            Assert.That(result.ComponentPath, Is.EqualTo(path));
        });
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
