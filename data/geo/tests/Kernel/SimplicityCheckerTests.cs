using geo;
using NtsBoundaryNodeRules = NetTopologySuite.Algorithm.BoundaryNodeRules;
using NtsBoundaryNodeRule = NetTopologySuite.Algorithm.IBoundaryNodeRule;
using NtsIsSimpleOp = NetTopologySuite.Operation.Valid.IsSimpleOp;
using NtsWktReader = NetTopologySuite.IO.WKTReader;

namespace geo.Tests;

public class SimplicityCheckerTests
{
    [TestCase("POINT EMPTY")]
    [TestCase("POINT (1 2)")]
    [TestCase("MULTIPOINT EMPTY")]
    [TestCase("MULTIPOINT ((0 0), (1 1), (2 2))")]
    [TestCase("LINESTRING EMPTY")]
    [TestCase("LINESTRING (0 0, 1 1, 2 0)")]
    [TestCase("LINESTRING (3 4, 3 4)")]
    [TestCase("LINESTRING (0 0, 1 0, 1 0, 2 0)")]
    public void SimplePointMultiPointAndLinealValuesPass(string text)
    {
        ValidationResult result = Check(text, BoundaryNodeRule.Mod2);

        AssertValid(result);
    }

    [Test]
    public void SimpleLinearRingPasses()
    {
        Geometry ring = new Geometry
        {
            GeometryType = GeometryType.LinearRing,
            Point = null,
            LinearRing = new LinearRing
            {
                Coordinates =
                [
                    new Coordinate { X = 0.0, Y = 0.0 },
                    new Coordinate { X = 2.0, Y = 0.0 },
                    new Coordinate { X = 2.0, Y = 2.0 },
                    new Coordinate { X = 0.0, Y = 2.0 },
                    new Coordinate { X = 0.0, Y = 0.0 }
                ]
            }
        };

        ValidationResult result = SimplicityChecker.CheckSimplicity(
            ring, BoundaryNodeRule.Mod2);

        AssertValid(result);
    }

    [Test]
    public void MultiPointDuplicateUsesPlanarIdentityAndReportsSecondPoint()
    {
        Geometry geometry = Read(
            "MULTIPOINT Z ((1 2 3), (4 5 6), (1 2 9))");

        ValidationResult result = SimplicityChecker.CheckSimplicity(
            geometry, BoundaryNodeRule.Mod2);
        geometry.MultiPoint!.Points[2].Coordinate!.X = 20.0;

        Assert.Multiple(() =>
        {
            AssertFailure(result, ValidationError.RepeatedCoordinate,
                "MultiPoint/Points[2]");
            Assert.That(result.Coordinate, Is.Not.Null);
            Assert.That(result.Coordinate!.X, Is.EqualTo(1.0));
            Assert.That(result.Coordinate.Y, Is.EqualTo(2.0));
            Assert.That(result.Coordinate.Z, Is.EqualTo(9.0));
        });
    }

    [TestCase(
        "LINESTRING (0 0, 2 2, 0 2, 2 0)",
        1.0, 1.0, "LineString/Segments[2]")]
    [TestCase(
        "LINESTRING (0 0, 2 0, 1 1, 2 0, 3 0)",
        2.0, 0.0, "LineString/Segments[2]")]
    [TestCase(
        "LINESTRING (0 0, 3 0, 1 0, 4 0)",
        1.0, 0.0, "LineString/Segments[1]")]
    public void LineSelfIntersectionsReportDeterministicWitnessAndSegment(
        string text, double x, double y, string path)
    {
        ValidationResult result = Check(text, BoundaryNodeRule.Mod2);

        Assert.Multiple(() =>
        {
            AssertFailure(result, ValidationError.SelfIntersection, path);
            Assert.That(result.Coordinate, Is.Not.Null);
            Assert.That(result.Coordinate!.X, Is.EqualTo(x).Within(1e-12));
            Assert.That(result.Coordinate.Y, Is.EqualTo(y).Within(1e-12));
        });
    }

    [TestCase(
        "MULTILINESTRING ((0 0, 4 4), (0 4, 4 0))",
        2.0, 2.0)]
    [TestCase(
        "MULTILINESTRING ((0 0, 4 0), (2 0, 2 2))",
        2.0, 0.0)]
    [TestCase(
        "MULTILINESTRING ((0 0, 4 0), (2 0, 6 0))",
        2.0, 0.0)]
    public void MultiLineInteriorIntersectionsAndOverlapsAreNotSimple(
        string text, double x, double y)
    {
        ValidationResult result = Check(text, BoundaryNodeRule.Mod2);

        Assert.Multiple(() =>
        {
            AssertFailure(result, ValidationError.SelfIntersection,
                "MultiLineString/LineStrings[1]/Segments[0]");
            Assert.That(result.Coordinate, Is.Not.Null);
            Assert.That(result.Coordinate!.X, Is.EqualTo(x).Within(1e-12));
            Assert.That(result.Coordinate.Y, Is.EqualTo(y).Within(1e-12));
        });
    }

    [TestCase(BoundaryNodeRule.Mod2)]
    [TestCase(BoundaryNodeRule.Endpoint)]
    [TestCase(BoundaryNodeRule.MultivalentEndpoint)]
    [TestCase(BoundaryNodeRule.MonovalentEndpoint)]
    public void OpenComponentsMayMeetAtTheirEndpointsUnderEveryRule(
        BoundaryNodeRule rule)
    {
        ValidationResult result = Check(
            "MULTILINESTRING ((0 0, 1 0), (1 0, 2 0), (1 0, 1 1))",
            rule);

        AssertValid(result);
    }

    [TestCase(BoundaryNodeRule.Mod2, false)]
    [TestCase(BoundaryNodeRule.Endpoint, true)]
    [TestCase(BoundaryNodeRule.MultivalentEndpoint, true)]
    [TestCase(BoundaryNodeRule.MonovalentEndpoint, false)]
    public void ClosedEndpointTouchUsesSelectedBoundaryRule(
        BoundaryNodeRule rule, bool expectedSimple)
    {
        const string text =
            "MULTILINESTRING ((0 0, 2 0, 2 2, 0 2, 0 0), " +
            "(0 0, -1 0))";

        ValidationResult result = Check(text, rule);

        Assert.That(result.IsValid, Is.EqualTo(expectedSimple));
        if (!expectedSimple)
        {
            AssertFailure(result, ValidationError.SelfIntersection,
                "MultiLineString/LineStrings[1]/Segments[0]");
            Assert.That(result.Coordinate!.X, Is.Zero);
            Assert.That(result.Coordinate.Y, Is.Zero);
        }
    }

    [TestCase("LINESTRING (0 0, 2 2, 0 2, 2 0)", BoundaryNodeRule.Mod2)]
    [TestCase("MULTILINESTRING ((0 0, 1 0), (1 0, 2 0))", BoundaryNodeRule.Mod2)]
    [TestCase("MULTILINESTRING ((0 0, 2 0, 2 2, 0 2, 0 0), (0 0, -1 0))", BoundaryNodeRule.Mod2)]
    [TestCase("MULTILINESTRING ((0 0, 2 0, 2 2, 0 2, 0 0), (0 0, -1 0))", BoundaryNodeRule.Endpoint)]
    [TestCase("MULTILINESTRING ((0 0, 4 0), (2 0, 2 2))", BoundaryNodeRule.Endpoint)]
    [TestCase("LINESTRING (0 0, 2 0, 1 1, 0 0, -1 1, -2 0, 0 0)", BoundaryNodeRule.Mod2)]
    [TestCase("LINESTRING (0 0, 2 0, 1 1, 0 0, -1 1, -2 0, 0 0)", BoundaryNodeRule.Endpoint)]
    public void RepresentativeLinealCasesMatchNetTopologySuite(
        string text, BoundaryNodeRule rule)
    {
        NetTopologySuite.Geometries.Geometry referenceGeometry =
            new NtsWktReader().Read(text);
        NtsBoundaryNodeRule referenceRule = ReferenceRule(rule);
        bool reference = new NtsIsSimpleOp(
            referenceGeometry, referenceRule).IsSimple();

        ValidationResult result = Check(text, rule);

        Assert.That(result.IsValid, Is.EqualTo(reference));
    }

    [Test]
    public void StructuralFailurePrecedesSimplicityAndUnknownRuleIsRejected()
    {
        Geometry shortLine = new Geometry
        {
            GeometryType = GeometryType.LineString,
            Point = null,
            LineString = new LineString
            {
                Coordinates = [new Coordinate { X = 1.0, Y = 2.0 }]
            }
        };

        ValidationResult result = SimplicityChecker.CheckSimplicity(
            shortLine, BoundaryNodeRule.Mod2);

        Assert.Multiple(() =>
        {
            AssertFailure(result, ValidationError.TooFewCoordinates,
                "LineString/Coordinates");
            Assert.Throws<InvalidGeometryException>(() =>
                SimplicityChecker.CheckSimplicity(
                    shortLine, (BoundaryNodeRule)99));
        });
    }

    private static NtsBoundaryNodeRule ReferenceRule(BoundaryNodeRule rule)
    {
        switch (rule)
        {
            case BoundaryNodeRule.Mod2:
                return NtsBoundaryNodeRules.Mod2BoundaryRule;
            case BoundaryNodeRule.Endpoint:
                return NtsBoundaryNodeRules.EndpointBoundaryRule;
            case BoundaryNodeRule.MultivalentEndpoint:
                return NtsBoundaryNodeRules.MultivalentEndpointBoundaryRule;
            default:
                return NtsBoundaryNodeRules.MonoValentEndpointBoundaryRule;
        }
    }

    private static ValidationResult Check(
        string text, BoundaryNodeRule rule)
    {
        return SimplicityChecker.CheckSimplicity(Read(text), rule);
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
