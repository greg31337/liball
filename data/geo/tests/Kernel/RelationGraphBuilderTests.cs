using geo;

namespace geo.Tests;

public class RelationGraphBuilderTests
{
    [TestCase("POINT (1 1)", "POINT (1 1)", "0FFFFFFF2")]
    [TestCase("POINT (1 0)", "LINESTRING (0 0, 2 0)", "0FFFFF102")]
    [TestCase("POINT (1 1)", "POLYGON ((0 0, 3 0, 3 3, 0 3, 0 0))", "0FFFFF212")]
    [TestCase("LINESTRING (0 0, 2 0)", "POINT (1 0)", "0F1FF0FF2")]
    [TestCase("LINESTRING (0 1, 2 1)", "LINESTRING (1 0, 1 2)", "0F1FF0102")]
    [TestCase("LINESTRING (-1 1, 4 1)", "POLYGON ((0 0, 3 0, 3 3, 0 3, 0 0))", "101FF0212")]
    [TestCase("POLYGON ((0 0, 3 0, 3 3, 0 3, 0 0))", "POINT (1 1)", "0F2FF1FF2")]
    [TestCase("POLYGON ((0 0, 3 0, 3 3, 0 3, 0 0))", "LINESTRING (-1 1, 4 1)", "1F20F1102")]
    [TestCase("POLYGON ((0 0, 3 0, 3 3, 0 3, 0 0))", "POLYGON ((2 1, 4 1, 4 4, 2 4, 2 1))", "212101212")]
    public void LabelsEveryOrderedAtomicDimensionPair(
        string firstText, string secondText, string expected)
    {
        RelationMatrix matrix = Relate(firstText, secondText);

        Assert.That(RelationMatrixAlgorithms.GetCanonicalText(matrix),
            Is.EqualTo(expected));
    }

    [TestCase("POINT EMPTY", "POINT (0 0)", "FFFFFF0F2")]
    [TestCase("LINESTRING EMPTY", "LINESTRING (0 0, 1 0)", "FFFFFF102")]
    [TestCase("POLYGON EMPTY", "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0))", "FFFFFF212")]
    [TestCase("POINT (0 0)", "POINT EMPTY", "FF0FFFFF2")]
    [TestCase("LINESTRING (0 0, 1 0)", "LINESTRING EMPTY", "FF1FF0FF2")]
    [TestCase("POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0))", "POLYGON EMPTY", "FF2FF1FF2")]
    [TestCase("LINESTRING EMPTY", "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0))", "FFFFFF212")]
    [TestCase("POLYGON EMPTY", "LINESTRING EMPTY", "FFFFFFFF2")]
    public void LabelsEmptyInteriorsBoundariesAndUnboundedExteriors(
        string firstText, string secondText, string expected)
    {
        RelationMatrix matrix = Relate(firstText, secondText);

        Assert.That(RelationMatrixAlgorithms.GetCanonicalText(matrix),
            Is.EqualTo(expected));
    }

    [TestCase("POINT (0 0)", "LINESTRING (0 0, 2 0)", "F0FFFF102")]
    [TestCase("POINT (3 0)", "LINESTRING (0 0, 2 0)", "FF0FFF102")]
    [TestCase("POINT (0 1)", "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0))", "F0FFFF212")]
    [TestCase("POINT (3 1)", "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0))", "FF0FFF212")]
    public void PointRelationsDistinguishInteriorBoundaryAndExterior(
        string firstText, string secondText, string expected)
    {
        Assert.That(RelationMatrixAlgorithms.GetCanonicalText(
            Relate(firstText, secondText)), Is.EqualTo(expected));
    }

    [TestCase("LINESTRING (0 0, 2 0)", "LINESTRING (0 0, 2 0)", "1FFF0FFF2")]
    [TestCase("LINESTRING (0 0, 1 0)", "LINESTRING (1 0, 2 0)", "FF1F00102")]
    [TestCase("LINESTRING (0 0, 2 0)", "LINESTRING (1 0, 3 0)", "1010F0102")]
    [TestCase("LINESTRING (0 0, 1 0)", "LINESTRING (2 0, 3 0)", "FF1FF0102")]
    public void CurveRelationsDistinguishEqualityCrossingTouchOverlapAndDisjointness(
        string firstText, string secondText, string expected)
    {
        Assert.That(RelationMatrixAlgorithms.GetCanonicalText(
            Relate(firstText, secondText)), Is.EqualTo(expected));
    }

    [TestCase("LINESTRING (1 1, 3 1)", "POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))", "1FF0FF212")]
    [TestCase("LINESTRING (0 0, 4 0)", "POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))", "F1FF0F212")]
    [TestCase("LINESTRING (-2 0, 0 0)", "POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))", "FF1F00212")]
    [TestCase("LINESTRING (5 1, 6 1)", "POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))", "FF1FF0212")]
    public void CurveSurfaceRelationsLabelInteriorBoundaryAndExteriorFragments(
        string firstText, string secondText, string expected)
    {
        Assert.That(RelationMatrixAlgorithms.GetCanonicalText(
            Relate(firstText, secondText)), Is.EqualTo(expected));
    }

    [TestCase("POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))", "POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))", "2FFF1FFF2")]
    [TestCase("POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))", "POLYGON ((1 1, 3 1, 3 3, 1 3, 1 1))", "212FF1FF2")]
    [TestCase("POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0))", "POLYGON ((2 0, 4 0, 4 2, 2 2, 2 0))", "FF2F11212")]
    [TestCase("POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0))", "POLYGON ((2 2, 4 2, 4 4, 2 4, 2 2))", "FF2F01212")]
    [TestCase("POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0))", "POLYGON ((3 0, 5 0, 5 2, 3 2, 3 0))", "FF2FF1212")]
    public void SurfaceRelationsDistinguishEqualityContainmentAndTouches(
        string firstText, string secondText, string expected)
    {
        Assert.That(RelationMatrixAlgorithms.GetCanonicalText(
            Relate(firstText, secondText)), Is.EqualTo(expected));
    }

    [Test]
    public void PolygonHolesParticipateInAreaAndBoundaryLabels()
    {
        string donut = "POLYGON ((0 0, 8 0, 8 8, 0 8, 0 0), (2 2, 6 2, 6 6, 2 6, 2 2))";
        string holeFill = "POLYGON ((2 2, 6 2, 6 6, 2 6, 2 2))";

        RelationMatrix matrix = Relate(donut, holeFill);

        Assert.That(RelationMatrixAlgorithms.GetCanonicalText(matrix),
            Is.EqualTo("FF2F112F2"));
    }

    [Test]
    public void LinearRingIsAnAtomicClosedCurveWithNoBoundary()
    {
        Geometry ring = new Geometry
        {
            GeometryType = GeometryType.LinearRing,
            Point = null,
            LinearRing = new LinearRing
            {
                Coordinates =
                [
                    CoordinateValue(0.0, 0.0), CoordinateValue(2.0, 0.0),
                    CoordinateValue(2.0, 2.0), CoordinateValue(0.0, 0.0)
                ]
            }
        };
        Geometry point = Read("POINT (1 0)");

        RelationMatrix matrix = RelationGraphBuilder.BuildRelation(point, ring);

        Assert.That(RelationMatrixAlgorithms.GetCanonicalText(matrix),
            Is.EqualTo("0FFFFF1F2"));
    }

    [Test]
    public void RelationGraphRejectsUnsupportedSurfaceTypes()
    {
        Geometry triangle = Read("TRIANGLE ((0 0, 2 0, 0 2, 0 0))");

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                RelationGraphBuilder.BuildRelation(triangle, Read("POINT (0 0)")));
            Assert.Throws<InvalidGeometryException>(() =>
                RelationGraphBuilder.BuildRelation(null!, Read("POINT (0 0)")));
        });
    }

    private static RelationMatrix Relate(string first, string second)
    {
        return RelationGraphBuilder.BuildRelation(Read(first), Read(second));
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }

    private static Coordinate CoordinateValue(double x, double y)
    {
        return new Coordinate { X = x, Y = y };
    }
}
