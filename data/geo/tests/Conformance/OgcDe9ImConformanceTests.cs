using geo;

namespace geo.Tests;

public class OgcDe9ImConformanceTests
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
    public void RelateConformsForEveryOrderedNonEmptyDimensionPair(
        string firstText, string secondText, string expectedMatrix)
    {
        IGeometryRelations relations = new GeometryRelations();

        RelationMatrix matrix = relations.Relate(
            Read(firstText), Read(secondText));

        Assert.That(CanonicalText(matrix), Is.EqualTo(expectedMatrix));
    }

    [TestCase("POINT (1 1)", "POINT (1 1)", De9ImPatterns.TopologicallyEquals)]
    [TestCase("POINT (0 0)", "POINT (2 2)", De9ImPatterns.Disjoint)]
    [TestCase("POINT (1 1)", "POLYGON ((0 0, 3 0, 3 3, 0 3, 0 0))", De9ImPatterns.Within)]
    [TestCase("POLYGON ((0 0, 3 0, 3 3, 0 3, 0 0))", "POINT (1 1)", De9ImPatterns.Contains)]
    [TestCase("POLYGON ((0 0, 6 0, 6 6, 0 6, 0 0))", "POLYGON ((1 1, 2 1, 2 2, 1 2, 1 1))", De9ImPatterns.ContainsProperly)]
    [TestCase("POINT (0 0)", "LINESTRING (0 0, 2 0)", De9ImPatterns.TouchesInteriorBoundary)]
    [TestCase("LINESTRING (0 0, 2 0)", "POINT (0 0)", De9ImPatterns.TouchesBoundaryInterior)]
    [TestCase("LINESTRING (0 0, 1 0)", "LINESTRING (1 0, 2 0)", De9ImPatterns.TouchesBoundaryBoundary)]
    [TestCase("LINESTRING (-1 1, 4 1)", "POLYGON ((0 0, 3 0, 3 3, 0 3, 0 0))", De9ImPatterns.CrossesLowerDimension)]
    [TestCase("POLYGON ((0 0, 3 0, 3 3, 0 3, 0 0))", "LINESTRING (-1 1, 4 1)", De9ImPatterns.CrossesHigherDimension)]
    [TestCase("LINESTRING (0 1, 2 1)", "LINESTRING (1 0, 1 2)", De9ImPatterns.CrossesLineLine)]
    [TestCase("POLYGON ((0 0, 3 0, 3 3, 0 3, 0 0))", "POLYGON ((2 1, 4 1, 4 4, 2 4, 2 1))", De9ImPatterns.OverlapsPointOrArea)]
    [TestCase("LINESTRING (0 0, 2 0)", "LINESTRING (1 0, 3 0)", De9ImPatterns.OverlapsLine)]
    [TestCase("POINT (1 1)", "POINT (1 1)", De9ImPatterns.IntersectsInteriorInterior)]
    [TestCase("POINT (0 0)", "LINESTRING (0 0, 2 0)", De9ImPatterns.IntersectsInteriorBoundary)]
    [TestCase("LINESTRING (0 0, 2 0)", "POINT (0 0)", De9ImPatterns.IntersectsBoundaryInterior)]
    [TestCase("LINESTRING (0 0, 1 0)", "LINESTRING (1 0, 2 0)", De9ImPatterns.IntersectsBoundaryBoundary)]
    [TestCase("POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))", "POINT (1 1)", De9ImPatterns.CoversInteriorInterior)]
    [TestCase("POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))", "LINESTRING (1 1, 3 1)", De9ImPatterns.CoversInteriorBoundary)]
    [TestCase("POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))", "LINESTRING (0 1, 0 3)", De9ImPatterns.CoversBoundaryInterior)]
    [TestCase("LINESTRING (0 0, 2 0)", "LINESTRING (0 0, 2 0)", De9ImPatterns.CoversBoundaryBoundary)]
    [TestCase("POINT (1 1)", "POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))", De9ImPatterns.CoveredByInteriorInterior)]
    [TestCase("LINESTRING (0 1, 0 3)", "POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))", De9ImPatterns.CoveredByInteriorBoundary)]
    [TestCase("LINESTRING (1 1, 3 1)", "POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))", De9ImPatterns.CoveredByBoundaryInterior)]
    [TestCase("LINESTRING (0 0, 2 0)", "LINESTRING (0 0, 2 0)", De9ImPatterns.CoveredByBoundaryBoundary)]
    public void EveryPublishedNamedPredicatePatternHasAConformingRelation(
        string firstText, string secondText, string pattern)
    {
        IGeometryRelations relations = new GeometryRelations();

        RelationMatrix matrix = relations.Relate(
            Read(firstText), Read(secondText));

        Assert.That(relations.MatchRelationPattern(matrix, pattern), Is.True);
    }

    [Test]
    public void NamedPredicatesMatchTheirOgcDe9ImPatterns()
    {
        IGeometryPredicates predicates = new GeometryPredicates();
        IGeometryRelations relations = new GeometryRelations();
        Geometry disjointFirst = Read("POINT (0 0)");
        Geometry disjointSecond = Read("POINT (2 2)");
        Geometry touchingFirst = Read("LINESTRING (0 0, 1 0)");
        Geometry touchingSecond = Read("LINESTRING (1 0, 2 0)");
        Geometry crossingFirst = Read("LINESTRING (0 1, 2 1)");
        Geometry crossingSecond = Read("LINESTRING (1 0, 1 2)");
        Geometry outer = Read(
            "POLYGON ((0 0, 6 0, 6 6, 0 6, 0 0))");
        Geometry inner = Read(
            "POLYGON ((1 1, 2 1, 2 2, 1 2, 1 1))");
        Geometry boundaryPoint = Read("POINT (0 3)");
        Geometry overlapping = Read(
            "POLYGON ((4 4, 8 4, 8 8, 4 8, 4 4))");

        RelationMatrix disjointMatrix = relations.Relate(
            disjointFirst, disjointSecond);
        RelationMatrix touchingMatrix = relations.Relate(
            touchingFirst, touchingSecond);
        RelationMatrix crossingMatrix = relations.Relate(
            crossingFirst, crossingSecond);
        RelationMatrix withinMatrix = relations.Relate(inner, outer);
        RelationMatrix containsMatrix = relations.Relate(outer, inner);
        RelationMatrix coveredByMatrix = relations.Relate(boundaryPoint, outer);
        RelationMatrix coversMatrix = relations.Relate(outer, boundaryPoint);
        RelationMatrix overlapMatrix = relations.Relate(outer, overlapping);

        Assert.Multiple(() =>
        {
            Assert.That(relations.MatchRelationPattern(
                disjointMatrix, De9ImPatterns.Disjoint), Is.True);
            Assert.That(predicates.Disjoint(
                disjointFirst, disjointSecond), Is.True);
            Assert.That(predicates.Intersects(
                disjointFirst, disjointSecond), Is.False);

            Assert.That(MatchesAnyTouchesPattern(
                relations, touchingMatrix), Is.True);
            Assert.That(predicates.Touches(
                touchingFirst, touchingSecond), Is.True);

            Assert.That(relations.MatchRelationPattern(
                crossingMatrix, De9ImPatterns.CrossesLineLine), Is.True);
            Assert.That(predicates.Crosses(
                crossingFirst, crossingSecond), Is.True);

            Assert.That(relations.MatchRelationPattern(
                withinMatrix, De9ImPatterns.Within), Is.True);
            Assert.That(predicates.Within(inner, outer), Is.True);
            Assert.That(relations.MatchRelationPattern(
                containsMatrix, De9ImPatterns.Contains), Is.True);
            Assert.That(predicates.Contains(outer, inner), Is.True);

            Assert.That(MatchesAnyCoveredByPattern(
                relations, coveredByMatrix), Is.True);
            Assert.That(predicates.CoveredBy(
                boundaryPoint, outer), Is.True);
            Assert.That(MatchesAnyCoversPattern(
                relations, coversMatrix), Is.True);
            Assert.That(predicates.Covers(outer, boundaryPoint), Is.True);

            Assert.That(relations.MatchRelationPattern(
                overlapMatrix, De9ImPatterns.OverlapsPointOrArea), Is.True);
            Assert.That(predicates.Overlaps(outer, overlapping), Is.True);

            Assert.That(relations.MatchRelationPattern(
                containsMatrix, De9ImPatterns.ContainsProperly), Is.True);
            Assert.That(predicates.ContainsProperly(outer, inner), Is.True);
            Assert.That(predicates.AreTopologicallyEqual(outer, outer), Is.True);
        });
    }

    [Test]
    public void CrossesSelectsConversePatternsForMixedDimensions()
    {
        IGeometryRelations relations = new GeometryRelations();
        IGeometryPredicates predicates = new GeometryPredicates();
        Geometry line = Read("LINESTRING (-1 1, 4 1)");
        Geometry polygon = Read(
            "POLYGON ((0 0, 3 0, 3 3, 0 3, 0 0))");
        RelationMatrix lowerFirst = relations.Relate(line, polygon);
        RelationMatrix higherFirst = relations.Relate(polygon, line);

        Assert.Multiple(() =>
        {
            Assert.That(relations.MatchRelationPattern(
                lowerFirst, De9ImPatterns.CrossesLowerDimension), Is.True);
            Assert.That(relations.MatchRelationPattern(
                higherFirst, De9ImPatterns.CrossesHigherDimension), Is.True);
            Assert.That(predicates.Crosses(line, polygon), Is.True);
            Assert.That(predicates.Crosses(polygon, line), Is.True);
        });
    }

    [TestCase("POINT (1 0)", "LINESTRING (0 0, 2 0)")]
    [TestCase("LINESTRING (-1 1, 4 1)", "POLYGON ((0 0, 3 0, 3 3, 0 3, 0 0))")]
    [TestCase("POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))", "POLYGON ((2 2, 6 2, 6 6, 2 6, 2 2))")]
    [TestCase("GEOMETRYCOLLECTION (POINT (8 8), LINESTRING (-1 1, 5 1))", "MULTIPOLYGON (((0 0, 4 0, 4 4, 0 4, 0 0)), ((10 0, 12 0, 12 2, 10 2, 10 0)))")]
    public void ConverseRelationsTransposeMatricesAndPredicates(
        string firstText, string secondText)
    {
        IGeometryRelations relations = new GeometryRelations();
        IGeometryPredicates predicates = new GeometryPredicates();
        Geometry first = Read(firstText);
        Geometry second = Read(secondText);
        RelationMatrix forward = relations.Relate(first, second);
        RelationMatrix reverse = relations.Relate(second, first);
        RelationMatrix transposed = relations.TransposeRelation(forward);

        Assert.Multiple(() =>
        {
            Assert.That(CanonicalText(reverse),
                Is.EqualTo(CanonicalText(transposed)));
            Assert.That(predicates.Disjoint(first, second),
                Is.EqualTo(predicates.Disjoint(second, first)));
            Assert.That(predicates.Intersects(first, second),
                Is.EqualTo(predicates.Intersects(second, first)));
            Assert.That(predicates.Touches(first, second),
                Is.EqualTo(predicates.Touches(second, first)));
            Assert.That(predicates.Crosses(first, second),
                Is.EqualTo(predicates.Crosses(second, first)));
            Assert.That(predicates.Overlaps(first, second),
                Is.EqualTo(predicates.Overlaps(second, first)));
            Assert.That(predicates.AreTopologicallyEqual(first, second),
                Is.EqualTo(predicates.AreTopologicallyEqual(second, first)));
            Assert.That(predicates.Within(first, second),
                Is.EqualTo(predicates.Contains(second, first)));
            Assert.That(predicates.CoveredBy(first, second),
                Is.EqualTo(predicates.Covers(second, first)));
        });
    }

    [TestCase(BoundaryNodeRule.Mod2, "0F1FF0FF2")]
    [TestCase(BoundaryNodeRule.Endpoint, "FF10F0FF2")]
    [TestCase(BoundaryNodeRule.MultivalentEndpoint, "FF10FFFF2")]
    [TestCase(BoundaryNodeRule.MonovalentEndpoint, "0F1FF0FF2")]
    public void BoundaryNodeRulesConformAtAValenceTwoJunction(
        BoundaryNodeRule rule, string expectedMatrix)
    {
        IGeometryRelations relations = new GeometryRelations();
        Geometry lines = Read(
            "MULTILINESTRING ((0 0, 1 0), (1 0, 2 0))");
        Geometry junction = Read("POINT (1 0)");

        RelationMatrix matrix = relations.RelateWithBoundaryRule(
            lines, junction, rule);

        Assert.That(CanonicalText(matrix), Is.EqualTo(expectedMatrix));
    }

    [TestCase("POINT EMPTY", "POINT (0 0)", "FFFFFF0F2")]
    [TestCase("LINESTRING EMPTY", "LINESTRING (0 0, 1 0)", "FFFFFF102")]
    [TestCase("POLYGON EMPTY", "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0))", "FFFFFF212")]
    [TestCase("POINT (0 0)", "POINT EMPTY", "FF0FFFFF2")]
    [TestCase("LINESTRING (0 0, 1 0)", "LINESTRING EMPTY", "FF1FF0FF2")]
    [TestCase("POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0))", "POLYGON EMPTY", "FF2FF1FF2")]
    [TestCase("GEOMETRYCOLLECTION EMPTY", "MULTIPOLYGON EMPTY", "FFFFFFFF2")]
    public void EmptyValuesConformAcrossDimensionsAndOrdering(
        string firstText, string secondText, string expectedMatrix)
    {
        IGeometryRelations relations = new GeometryRelations();
        IGeometryPredicates predicates = new GeometryPredicates();
        Geometry first = Read(firstText);
        Geometry second = Read(secondText);
        RelationMatrix matrix = relations.Relate(first, second);

        Assert.Multiple(() =>
        {
            Assert.That(CanonicalText(matrix), Is.EqualTo(expectedMatrix));
            Assert.That(predicates.Disjoint(first, second), Is.True);
            Assert.That(predicates.Intersects(first, second), Is.False);
            Assert.That(predicates.Touches(first, second), Is.False);
            Assert.That(predicates.Crosses(first, second), Is.False);
            Assert.That(predicates.Within(first, second), Is.False);
            Assert.That(predicates.Contains(first, second), Is.False);
            Assert.That(predicates.Overlaps(first, second), Is.False);
            Assert.That(predicates.Covers(first, second), Is.False);
            Assert.That(predicates.CoveredBy(first, second), Is.False);
            Assert.That(predicates.ContainsProperly(first, second), Is.False);
        });
    }

    [Test]
    public void HeterogeneousAndMultipartCollectionsRelateAsCombinedPointSets()
    {
        IGeometryRelations relations = new GeometryRelations();
        IGeometryPredicates predicates = new GeometryPredicates();
        Geometry heterogeneous = Read(
            "GEOMETRYCOLLECTION (POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0)), LINESTRING (1 1, 3 1))");
        Geometry containedLine = Read("LINESTRING (1 1, 3 1)");
        Geometry multipart = Read(
            "MULTIPOLYGON (((0 0, 2 0, 2 2, 0 2, 0 0)), ((4 0, 6 0, 6 2, 4 2, 4 0)))");
        Geometry firstPart = Read(
            "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0))");

        RelationMatrix heterogeneousMatrix = relations.Relate(
            heterogeneous, containedLine);
        RelationMatrix multipartMatrix = relations.Relate(
            multipart, firstPart);

        Assert.Multiple(() =>
        {
            Assert.That(CanonicalText(heterogeneousMatrix),
                Is.EqualTo("102FF1FF2"));
            Assert.That(predicates.Contains(
                heterogeneous, containedLine), Is.True);
            Assert.That(predicates.Within(
                containedLine, heterogeneous), Is.True);
            Assert.That(CanonicalText(multipartMatrix),
                Is.EqualTo("2F2F11FF2"));
            Assert.That(predicates.Contains(multipart, firstPart), Is.True);
            Assert.That(predicates.AreTopologicallyEqual(
                multipart, firstPart), Is.False);
            Assert.That(predicates.Overlaps(multipart, firstPart), Is.False);
        });
    }

    private static bool MatchesAnyTouchesPattern(
        IGeometryRelations relations, RelationMatrix matrix)
    {
        return relations.MatchRelationPattern(
            matrix, De9ImPatterns.TouchesInteriorBoundary) ||
            relations.MatchRelationPattern(
                matrix, De9ImPatterns.TouchesBoundaryInterior) ||
            relations.MatchRelationPattern(
                matrix, De9ImPatterns.TouchesBoundaryBoundary);
    }

    private static bool MatchesAnyCoversPattern(
        IGeometryRelations relations, RelationMatrix matrix)
    {
        return relations.MatchRelationPattern(
            matrix, De9ImPatterns.CoversInteriorInterior) ||
            relations.MatchRelationPattern(
                matrix, De9ImPatterns.CoversInteriorBoundary) ||
            relations.MatchRelationPattern(
                matrix, De9ImPatterns.CoversBoundaryInterior) ||
            relations.MatchRelationPattern(
                matrix, De9ImPatterns.CoversBoundaryBoundary);
    }

    private static bool MatchesAnyCoveredByPattern(
        IGeometryRelations relations, RelationMatrix matrix)
    {
        return relations.MatchRelationPattern(
            matrix, De9ImPatterns.CoveredByInteriorInterior) ||
            relations.MatchRelationPattern(
                matrix, De9ImPatterns.CoveredByInteriorBoundary) ||
            relations.MatchRelationPattern(
                matrix, De9ImPatterns.CoveredByBoundaryInterior) ||
            relations.MatchRelationPattern(
                matrix, De9ImPatterns.CoveredByBoundaryBoundary);
    }

    private static string CanonicalText(RelationMatrix matrix)
    {
        return RelationMatrixAlgorithms.GetCanonicalText(matrix);
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
