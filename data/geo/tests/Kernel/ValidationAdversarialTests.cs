using geo;
using NtsBoundaryNodeRules = NetTopologySuite.Algorithm.BoundaryNodeRules;
using NtsIsSimpleOp = NetTopologySuite.Operation.Valid.IsSimpleOp;
using NtsIsValidOp = NetTopologySuite.Operation.Valid.IsValidOp;
using NtsWktReader = NetTopologySuite.IO.WKTReader;

namespace geo.Tests;

public class ValidationAdversarialTests
{
    [TestCase(
        "POLYGON ((1000000000000000 1000000000000000, " +
        "1000000000000004 1000000000000004, " +
        "1000000000000000 1000000000000004, " +
        "1000000000000004 1000000000000000, " +
        "1000000000000000 1000000000000000))")]
    [TestCase(
        "POLYGON ((0 0, 10000000000000000 1, " +
        "10000000000000000 2, 0 1, 0 0))")]
    [TestCase(
        "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), " +
        "(0.000000000001 1, 2 1, 2 2, 0.000000000001 2, " +
        "0.000000000001 1))")]
    [TestCase(
        "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), " +
        "(-0.000000000001 1, 2 1, 2 2, -0.000000000001 2, " +
        "-0.000000000001 1))")]
    [TestCase(
        "POLYGON ((0 0, 5 0, 5 5, 3 3, 5 1, 0 1, 0 0))")]
    [TestCase(
        "POLYGON ((0 0, 6 0, 6 6, 0 6, 0 0), " +
        "(1 1, 1 5, 5 5, 5 1, 1 1), " +
        "(2 2, 4 2, 4 4, 2 4, 2 2))")]
    [TestCase(
        "MULTIPOLYGON (((0 0, 3 0, 3 3, 0 3, 0 0)), " +
        "((3 3, 6 3, 6 6, 3 6, 3 3)))")]
    [TestCase(
        "MULTIPOLYGON (((0 0, 3 0, 3 3, 0 3, 0 0)), " +
        "((3 0, 6 0, 6 3, 3 3, 3 0)))")]
    public void AdversarialPolygonValidityMatchesReference(string text)
    {
        bool expected = new NtsIsValidOp(
            new NtsWktReader().Read(text)).IsValid;

        ValidationResult result =
            GeometryValidityChecker.ValidateGeometry(Read(text));

        Assert.That(result.IsValid, Is.EqualTo(expected),
            $"{result.Error}: {result.Message} at {result.ComponentPath}");
        Assert.That(result.Error == ValidationError.None,
            Is.EqualTo(result.IsValid));
        Assert.That(string.IsNullOrEmpty(result.ComponentPath),
            Is.EqualTo(result.IsValid));
    }

    [TestCase(
        "LINESTRING (0 0, 0 0, 4 4, 0 4, 4 0, 4 0)")]
    [TestCase(
        "LINESTRING (0 0, 8 0, 2 0, 6 0, 10 0)")]
    [TestCase(
        "MULTILINESTRING ((0 0, 4 0), (4 0, 4 0), " +
        "(2 0, 2 0, 2 3))")]
    [TestCase(
        "MULTILINESTRING ((0 0, 4 0, 4 4, 0 4, 0 0), " +
        "(0 0, -2 0), (0 0, 0 -2))")]
    [TestCase(
        "LINESTRING (1000000000000000 1000000000000000, " +
        "1000000000000004 1000000000000004, " +
        "1000000000000000 1000000000000004, " +
        "1000000000000004 1000000000000000)")]
    public void AdversarialLineSimplicityMatchesReference(string text)
    {
        NetTopologySuite.Geometries.Geometry reference =
            new NtsWktReader().Read(text);
        bool expected = new NtsIsSimpleOp(
            reference, NtsBoundaryNodeRules.Mod2BoundaryRule).IsSimple();

        ValidationResult result = SimplicityChecker.CheckSimplicity(
            Read(text), BoundaryNodeRule.Mod2);

        Assert.That(result.IsValid, Is.EqualTo(expected),
            $"{result.Error}: {result.Message} at {result.ComponentPath}");
        if (!result.IsValid)
        {
            Assert.That(result.Error,
                Is.EqualTo(ValidationError.SelfIntersection));
            Assert.That(result.Coordinate, Is.Not.Null);
            Assert.That(result.ComponentPath, Does.Contain("/Segments["));
        }
    }

    [Test]
    public void ValidityWitnessIsOwnedAndDeterministicAcrossRingDirection()
    {
        Geometry forward = Read(
            "POLYGON ((0 0, 6 6, 0 6, 6 0, 0 0))");
        Geometry reverse = Read(
            "POLYGON ((0 0, 6 0, 0 6, 6 6, 0 0))");

        ValidationResult first =
            GeometryValidityChecker.ValidateGeometry(forward);
        ValidationResult second =
            GeometryValidityChecker.ValidateGeometry(reverse);
        forward.Polygon!.Shell.Coordinates[0].X = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(first.Error,
                Is.EqualTo(ValidationError.SelfIntersection));
            Assert.That(second.Error,
                Is.EqualTo(ValidationError.SelfIntersection));
            Assert.That(first.Coordinate!.X, Is.EqualTo(3.0));
            Assert.That(first.Coordinate.Y, Is.EqualTo(3.0));
            Assert.That(second.Coordinate!.X, Is.EqualTo(3.0));
            Assert.That(second.Coordinate.Y, Is.EqualTo(3.0));
        });
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
