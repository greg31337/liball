using geo;

namespace geo.Tests;

public class CollectionRelationGraphBuilderTests
{
    [Test]
    public void MultiPointUsesTheUnionOfComponentInteriors()
    {
        RelationMatrix matrix = RelationGraphBuilder.BuildRelation(
            Read("MULTIPOINT ((0 0), (2 0))"), Read("POINT (0 0)"));

        Assert.That(RelationMatrixAlgorithms.GetCanonicalText(matrix),
            Is.EqualTo("0F0FFFFF2"));
    }

    [TestCase(BoundaryNodeRule.Mod2, "0F1FF0FF2")]
    [TestCase(BoundaryNodeRule.Endpoint, "FF10F0FF2")]
    [TestCase(BoundaryNodeRule.MultivalentEndpoint, "FF10FFFF2")]
    [TestCase(BoundaryNodeRule.MonovalentEndpoint, "0F1FF0FF2")]
    public void MultiLineJunctionUsesSelectedEndpointValenceRule(
        BoundaryNodeRule rule, string expected)
    {
        RelationMatrix matrix = Relate(
            "MULTILINESTRING ((0 0, 1 0), (1 0, 2 0))",
            "POINT (1 0)", rule);

        Assert.That(RelationMatrixAlgorithms.GetCanonicalText(matrix),
            Is.EqualTo(expected));
    }

    [TestCase(BoundaryNodeRule.Mod2, "0F1FFFFF2")]
    [TestCase(BoundaryNodeRule.Endpoint, "FF10FFFF2")]
    [TestCase(BoundaryNodeRule.MultivalentEndpoint, "FF10FFFF2")]
    [TestCase(BoundaryNodeRule.MonovalentEndpoint, "0F1FFFFF2")]
    public void ClosedLineEndpointValenceIsTwo(
        BoundaryNodeRule rule, string expected)
    {
        RelationMatrix matrix = Relate(
            "LINESTRING (0 0, 2 0, 2 2, 0 0)",
            "POINT (0 0)", rule);

        Assert.That(RelationMatrixAlgorithms.GetCanonicalText(matrix),
            Is.EqualTo(expected));
    }

    [Test]
    public void MultiPolygonLabelsMatchedAndDisjointComponentsAsOnePointSet()
    {
        RelationMatrix matrix = Relate(
            "MULTIPOLYGON (((0 0, 2 0, 2 2, 0 2, 0 0)), ((4 0, 6 0, 6 2, 4 2, 4 0)))",
            "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0))",
            BoundaryNodeRule.Mod2);

        Assert.That(RelationMatrixAlgorithms.GetCanonicalText(matrix),
            Is.EqualTo("2F2F11FF2"));
    }

    [Test]
    public void HeterogeneousPointPromotesAnOverlappedLineEndpointToInterior()
    {
        RelationMatrix matrix = Relate(
            "GEOMETRYCOLLECTION (POINT (0 0), LINESTRING (0 0, 2 0))",
            "POINT (0 0)", BoundaryNodeRule.Endpoint);

        Assert.That(RelationMatrixAlgorithms.GetCanonicalText(matrix),
            Is.EqualTo("0F1FF0FF2"));
    }

    [Test]
    public void HeterogeneousPolygonPromotesCoveredLineworkToAreaInterior()
    {
        RelationMatrix matrix = Relate(
            "GEOMETRYCOLLECTION (POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0)), LINESTRING (1 1, 3 1))",
            "LINESTRING (1 1, 3 1)", BoundaryNodeRule.Mod2);

        Assert.That(RelationMatrixAlgorithms.GetCanonicalText(matrix),
            Is.EqualTo("102FF1FF2"));
    }

    [Test]
    public void NestedCollectionsAndEmptyMembersAreFlattenedRecursively()
    {
        RelationMatrix nested = Relate(
            "GEOMETRYCOLLECTION (POINT EMPTY, GEOMETRYCOLLECTION (POINT (1 1), LINESTRING EMPTY))",
            "MULTIPOINT (EMPTY, (1 1))", BoundaryNodeRule.Mod2);
        RelationMatrix empty = Relate(
            "GEOMETRYCOLLECTION (POINT EMPTY, MULTILINESTRING EMPTY)",
            "MULTIPOLYGON EMPTY", BoundaryNodeRule.Mod2);

        Assert.Multiple(() =>
        {
            Assert.That(RelationMatrixAlgorithms.GetCanonicalText(nested),
                Is.EqualTo("0FFFFFFF2"));
            Assert.That(RelationMatrixAlgorithms.GetCanonicalText(empty),
                Is.EqualTo("FFFFFFFF2"));
            Assert.That(nested.LeftDimension, Is.EqualTo(TopologicalDimension.Point));
            Assert.That(nested.RightDimension, Is.EqualTo(TopologicalDimension.Point));
            Assert.That(empty.LeftDimension, Is.EqualTo(TopologicalDimension.Empty));
            Assert.That(empty.RightDimension, Is.EqualTo(TopologicalDimension.Empty));
        });
    }

    [Test]
    public void CollectionRelationTransposeMatchesReversedInputs()
    {
        string first = "GEOMETRYCOLLECTION (POINT (8 8), LINESTRING (-1 1, 5 1))";
        string second = "MULTIPOLYGON (((0 0, 4 0, 4 4, 0 4, 0 0)), ((10 0, 12 0, 12 2, 10 2, 10 0)))";

        RelationMatrix forward = Relate(first, second, BoundaryNodeRule.Mod2);
        RelationMatrix reverse = Relate(second, first, BoundaryNodeRule.Mod2);

        Assert.That(RelationMatrixAlgorithms.GetCanonicalText(reverse),
            Is.EqualTo(RelationMatrixAlgorithms.GetCanonicalText(
                RelationMatrixAlgorithms.Transpose(forward))));
    }

    [Test]
    public void InvalidBoundaryRuleAndUnsupportedNestedTypesAreRejected()
    {
        Geometry point = Read("POINT (0 0)");
        Geometry extended = Read(
            "GEOMETRYCOLLECTION (TRIANGLE ((0 0, 2 0, 0 2, 0 0)))");

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                RelationGraphBuilder.BuildRelationWithBoundaryRule(
                    point, point, (BoundaryNodeRule)4));
            Assert.Throws<InvalidGeometryException>(() =>
                RelationGraphBuilder.BuildRelation(
                    extended, point));
        });
    }

    private static RelationMatrix Relate(
        string first, string second, BoundaryNodeRule rule)
    {
        return RelationGraphBuilder.BuildRelationWithBoundaryRule(
            Read(first), Read(second), rule);
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
