using geo;

namespace geo.Tests;

public class GeometryRelationsTests
{
    [Test]
    public void PublicContractRelatesAtomicAndCollectionGeometry()
    {
        IGeometryRelations relations = new GeometryRelations();
        Geometry line = Read("LINESTRING (-1 1, 4 1)");
        Geometry polygon = Read(
            "POLYGON ((0 0, 3 0, 3 3, 0 3, 0 0))");
        Geometry collection = Read(
            "GEOMETRYCOLLECTION (POINT (8 8), LINESTRING (-1 1, 4 1))");

        RelationMatrix atomic = relations.Relate(line, polygon);
        RelationMatrix collected = relations.Relate(collection, polygon);

        Assert.Multiple(() =>
        {
            Assert.That(RelationMatrixAlgorithms.GetCanonicalText(atomic),
                Is.EqualTo("101FF0212"));
            Assert.That(RelationMatrixAlgorithms.GetCanonicalText(collected),
                Is.EqualTo("101FF0212"));
            Assert.That(atomic.LeftDimension,
                Is.EqualTo(TopologicalDimension.Curve));
            Assert.That(atomic.RightDimension,
                Is.EqualTo(TopologicalDimension.Surface));
        });
    }

    [Test]
    public void BoundaryRuleRelationExposesEndpointValenceChoices()
    {
        IGeometryRelations relations = new GeometryRelations();
        Geometry lines = Read(
            "MULTILINESTRING ((0 0, 1 0), (1 0, 2 0))");
        Geometry junction = Read("POINT (1 0)");

        RelationMatrix mod2 = relations.Relate(lines, junction);
        RelationMatrix endpoint = relations.RelateWithBoundaryRule(
            lines, junction, BoundaryNodeRule.Endpoint);
        RelationMatrix monovalent = relations.RelateWithBoundaryRule(
            lines, junction, BoundaryNodeRule.MonovalentEndpoint);
        RelationMatrix multivalent = relations.RelateWithBoundaryRule(
            lines, junction, BoundaryNodeRule.MultivalentEndpoint);

        Assert.Multiple(() =>
        {
            Assert.That(RelationMatrixAlgorithms.GetCanonicalText(mod2),
                Is.EqualTo("0F1FF0FF2"));
            Assert.That(RelationMatrixAlgorithms.GetCanonicalText(endpoint),
                Is.EqualTo("FF10F0FF2"));
            Assert.That(RelationMatrixAlgorithms.GetCanonicalText(monovalent),
                Is.EqualTo("0F1FF0FF2"));
            Assert.That(RelationMatrixAlgorithms.GetCanonicalText(multivalent),
                Is.EqualTo("FF10FFFF2"));
        });
    }

    [Test]
    public void PatternAndTransposeOperateOnCallerSuppliedMatrices()
    {
        IGeometryRelations relations = new GeometryRelations();
        RelationMatrix matrix = relations.Relate(
            Read("POINT (1 1)"),
            Read("POLYGON ((0 0, 3 0, 3 3, 0 3, 0 0))"));

        bool within = relations.MatchRelationPattern(
            matrix, De9ImPatterns.Within);
        bool disjoint = relations.MatchRelationPattern(
            matrix, De9ImPatterns.Disjoint);
        RelationMatrix transpose = relations.TransposeRelation(matrix);

        Assert.Multiple(() =>
        {
            Assert.That(within, Is.True);
            Assert.That(disjoint, Is.False);
            Assert.That(RelationMatrixAlgorithms.GetCanonicalText(transpose),
                Is.EqualTo("0F2FF1FF2"));
            Assert.That(transpose.LeftDimension,
                Is.EqualTo(TopologicalDimension.Surface));
            Assert.That(transpose.RightDimension,
                Is.EqualTo(TopologicalDimension.Point));
            Assert.That(transpose.Cells, Is.Not.SameAs(matrix.Cells));
        });

        transpose.Cells[0] = RelationDimension.False;
        Assert.That(matrix.Cells[0], Is.EqualTo(RelationDimension.Point));
    }

    [Test]
    public void PointLocationCoversInteriorBoundaryExteriorAndEmptyValues()
    {
        IGeometryRelations relations = new GeometryRelations();
        Geometry polygon = Read(
            "POLYGON ((0 0, 6 0, 6 6, 0 6, 0 0), (2 2, 4 2, 4 4, 2 4, 2 2))");
        Geometry empty = Read("GEOMETRYCOLLECTION EMPTY");

        Assert.Multiple(() =>
        {
            Assert.That(relations.LocatePoint(
                CoordinateValue(1.0, 1.0), polygon),
                Is.EqualTo(TopologicalLocation.Interior));
            Assert.That(relations.LocatePoint(
                CoordinateValue(0.0, 3.0), polygon),
                Is.EqualTo(TopologicalLocation.Boundary));
            Assert.That(relations.LocatePoint(
                CoordinateValue(3.0, 3.0), polygon),
                Is.EqualTo(TopologicalLocation.Exterior));
            Assert.That(relations.LocatePoint(
                CoordinateValue(3.0, 3.0), empty),
                Is.EqualTo(TopologicalLocation.Exterior));
        });
    }

    [Test]
    public void PointLocationUsesAggregateMod2EndpointValence()
    {
        IGeometryRelations relations = new GeometryRelations();
        Geometry lines = Read(
            "MULTILINESTRING ((0 0, 2 0), (1 0, 1 1))");
        Geometry promoted = Read(
            "GEOMETRYCOLLECTION (MULTILINESTRING ((0 0, 2 0), (1 0, 1 1)), POINT (1 0))");

        Assert.Multiple(() =>
        {
            Assert.That(relations.LocatePoint(
                CoordinateValue(1.0, 0.0), lines),
                Is.EqualTo(TopologicalLocation.Boundary));
            Assert.That(relations.LocatePoint(
                CoordinateValue(1.0, 0.0), promoted),
                Is.EqualTo(TopologicalLocation.Interior));
        });
    }

    [Test]
    public void RelationResultsAreIndependentAcrossCallsAndInputMutation()
    {
        IGeometryRelations relations = new GeometryRelations();
        Geometry first = Read("LINESTRING (0 0, 2 0)");
        Geometry second = Read("LINESTRING (1 -1, 1 1)");

        RelationMatrix initial = relations.Relate(first, second);
        first.LineString!.Coordinates[0].X = 100.0;
        RelationMatrix changed = relations.Relate(first, second);
        initial.Cells[0] = RelationDimension.Surface;

        Assert.Multiple(() =>
        {
            Assert.That(RelationMatrixAlgorithms.GetCanonicalText(initial),
                Is.EqualTo("2F1FF0102"));
            Assert.That(RelationMatrixAlgorithms.GetCanonicalText(changed),
                Is.Not.EqualTo("0F1FF0102"));
            Assert.That(initial.Cells, Is.Not.SameAs(changed.Cells));
        });
    }

    [Test]
    public void PublicSurfaceRejectsMalformedInputsPatternsRulesAndKnownSridConflicts()
    {
        IGeometryRelations relations = new GeometryRelations();
        Geometry srid4326 = ReadSrid("SRID=4326;POINT (0 0)");
        Geometry srid3857 = ReadSrid("SRID=3857;POINT (0 0)");
        Geometry unknown = Read("POINT (0 0)");
        RelationMatrix malformed = new RelationMatrix
        {
            Cells = new RelationDimension[8]
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                relations.Relate(srid4326, srid3857));
            Assert.DoesNotThrow(() => relations.Relate(srid4326, unknown));
            Assert.Throws<InvalidGeometryException>(() =>
                relations.RelateWithBoundaryRule(
                    unknown, unknown, (BoundaryNodeRule)4));
            Assert.Throws<InvalidGeometryException>(() =>
                relations.Relate(null!, unknown));
            Assert.Throws<InvalidGeometryException>(() =>
                relations.MatchRelationPattern(new RelationMatrix(), "T***X****"));
            Assert.Throws<InvalidGeometryException>(() =>
                relations.MatchRelationPattern(null!, "*********"));
            Assert.Throws<InvalidGeometryException>(() =>
                relations.TransposeRelation(malformed));
            Assert.Throws<InvalidGeometryException>(() =>
                relations.LocatePoint(null!, unknown));
        });
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }

    private static Geometry ReadSrid(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions { AllowSpatialReferenceId = true },
            new WktWriteOptions());
        return codec.ReadWkt(text);
    }

    private static Coordinate CoordinateValue(double x, double y)
    {
        return new Coordinate { X = x, Y = y };
    }
}
