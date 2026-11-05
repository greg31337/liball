using geo;
using NtsBufferOp = NetTopologySuite.Operation.Buffer.BufferOp;
using NtsBufferParameters = NetTopologySuite.Operation.Buffer.BufferParameters;
using NtsEndCapStyle = NetTopologySuite.Operation.Buffer.EndCapStyle;
using NtsGeometry = NetTopologySuite.Geometries.Geometry;
using NtsGeometryFixer = NetTopologySuite.Geometries.Utilities.GeometryFixer;
using NtsIsValidOp = NetTopologySuite.Operation.Valid.IsValidOp;
using NtsJoinStyle = NetTopologySuite.Operation.Buffer.JoinStyle;

namespace geo.Tests;

public class NtsDifferentialAdapterTests
{
    [TestCase("POINT (2 3)")]
    [TestCase("LINESTRING (0 0, 2 3, 5 1)")]
    [TestCase("POLYGON ((0 0, 8 0, 8 6, 0 6, 0 0), (2 2, 2 4, 4 4, 4 2, 2 2))")]
    [TestCase("MULTIPOINT ((1 1), (4 3))")]
    [TestCase("MULTILINESTRING ((0 0, 3 1), (5 2, 8 2))")]
    [TestCase("MULTIPOLYGON (((0 0, 2 0, 2 2, 0 2, 0 0)), ((4 0, 7 0, 7 3, 4 3, 4 0)))")]
    [TestCase("GEOMETRYCOLLECTION (POINT (1 1), LINESTRING (3 0, 3 4))")]
    public void CoreGeometryAdaptersPreserveEquivalentPointSets(string text)
    {
        Geometry geoGeometry = NtsGeometryAdapter.ReadGeo(text);
        NtsGeometry ntsGeometry = NtsGeometryAdapter.ReadNts(text);

        Geometry fromNts = NtsGeometryAdapter.ToGeo(ntsGeometry);
        NtsGeometry fromGeo = NtsGeometryAdapter.ToNts(geoGeometry);

        Assert.Multiple(() =>
        {
            Assert.That(fromGeo.EqualsTopologically(ntsGeometry), Is.True);
            Assert.That(new GeometryPredicates().AreTopologicallyEqual(
                fromNts, geoGeometry), Is.True);
        });
    }

    [TestCase(
        "POLYGON ((0 0, 6 0, 6 6, 0 6, 0 0))",
        "POLYGON ((4 2, 9 2, 9 5, 4 5, 4 2))", 2.5)]
    [TestCase(
        "LINESTRING (0 0, 6 6)",
        "LINESTRING (0 6, 6 0)", 0.0)]
    [TestCase(
        "POLYGON ((0 0, 8 0, 8 8, 0 8, 0 0))",
        "POINT (0 4)", 0.0)]
    [TestCase(
        "POINT (20 20)",
        "POLYGON ((0 0, 8 0, 8 8, 0 8, 0 0))", 10.0)]
    public void PredicatesMatchNetTopologySuite(
        string firstText, string secondText, double maximumDistance)
    {
        Geometry first = NtsGeometryAdapter.ReadGeo(firstText);
        Geometry second = NtsGeometryAdapter.ReadGeo(secondText);
        NtsGeometry ntsFirst = NtsGeometryAdapter.ReadNts(firstText);
        NtsGeometry ntsSecond = NtsGeometryAdapter.ReadNts(secondText);
        IGeometryPredicates predicates = new GeometryPredicates();

        Assert.Multiple(() =>
        {
            Assert.That(predicates.AreTopologicallyEqual(first, second),
                Is.EqualTo(ntsFirst.EqualsTopologically(ntsSecond)));
            Assert.That(predicates.Disjoint(first, second),
                Is.EqualTo(ntsFirst.Disjoint(ntsSecond)));
            Assert.That(predicates.Intersects(first, second),
                Is.EqualTo(ntsFirst.Intersects(ntsSecond)));
            Assert.That(predicates.Touches(first, second),
                Is.EqualTo(ntsFirst.Touches(ntsSecond)));
            Assert.That(predicates.Crosses(first, second),
                Is.EqualTo(ntsFirst.Crosses(ntsSecond)));
            Assert.That(predicates.Within(first, second),
                Is.EqualTo(ntsFirst.Within(ntsSecond)));
            Assert.That(predicates.Contains(first, second),
                Is.EqualTo(ntsFirst.Contains(ntsSecond)));
            Assert.That(predicates.Overlaps(first, second),
                Is.EqualTo(ntsFirst.Overlaps(ntsSecond)));
            Assert.That(predicates.Covers(first, second),
                Is.EqualTo(ntsFirst.Covers(ntsSecond)));
            Assert.That(predicates.CoveredBy(first, second),
                Is.EqualTo(ntsFirst.CoveredBy(ntsSecond)));
            Assert.That(predicates.ContainsProperly(first, second),
                Is.EqualTo(ntsFirst.Relate(
                    ntsSecond, De9ImPatterns.ContainsProperly)));
            Assert.That(predicates.WithinDistance(
                    first, second, maximumDistance),
                Is.EqualTo(ntsFirst.IsWithinDistance(
                    ntsSecond, maximumDistance)));
        });
    }

    [Test]
    public void MetricsAndOverlayMatchNetTopologySuite()
    {
        const string firstText =
            "POLYGON ((0 0, 8 0, 8 6, 0 6, 0 0), (2 2, 2 4, 4 4, 4 2, 2 2))";
        const string secondText =
            "POLYGON ((5 -1, 10 -1, 10 4, 5 4, 5 -1))";
        Geometry first = NtsGeometryAdapter.ReadGeo(firstText);
        Geometry second = NtsGeometryAdapter.ReadGeo(secondText);
        NtsGeometry ntsFirst = NtsGeometryAdapter.ReadNts(firstText);
        NtsGeometry ntsSecond = NtsGeometryAdapter.ReadNts(secondText);
        IGeometryMeasures measures = new GeometryMeasures();
        IGeometryOverlay overlay = new GeometryOverlay();

        Assert.Multiple(() =>
        {
            Assert.That(measures.Area(first),
                Is.EqualTo(ntsFirst.Area).Within(1.0e-12));
            Assert.That(measures.Length(first),
                Is.EqualTo(ntsFirst.Length).Within(1.0e-12));
            Assert.That(measures.Distance(first, second),
                Is.EqualTo(ntsFirst.Distance(ntsSecond)).Within(1.0e-12));
        });
        NtsGeometryComparison.AssertEquivalent(
            ntsFirst.Intersection(ntsSecond),
            overlay.Intersection(first, second), "intersection");
        NtsGeometryComparison.AssertEquivalent(
            ntsFirst.Union(ntsSecond),
            overlay.Union(first, second), "union");
        NtsGeometryComparison.AssertEquivalent(
            ntsFirst.Difference(ntsSecond),
            overlay.Difference(first, second), "difference");
        NtsGeometryComparison.AssertEquivalent(
            ntsFirst.SymmetricDifference(ntsSecond),
            overlay.SymmetricDifference(first, second),
            "symmetric difference");
    }

    [Test]
    public void BufferMatchesNetTopologySuiteWithEquivalentParameters()
    {
        const string text = "LINESTRING (0 0, 7 0, 7 4)";
        Geometry source = NtsGeometryAdapter.ReadGeo(text);
        NtsGeometry ntsSource = NtsGeometryAdapter.ReadNts(text);
        BufferParameters parameters = new BufferParameters
        {
            CapStyle = BufferCapStyle.Flat,
            JoinStyle = BufferJoinStyle.Mitre,
            QuadrantSegments = 8,
            MitreLimit = 5.0
        };
        NtsBufferParameters ntsParameters = new NtsBufferParameters
        {
            EndCapStyle = NtsEndCapStyle.Flat,
            JoinStyle = NtsJoinStyle.Mitre,
            QuadrantSegments = 8,
            MitreLimit = 5.0
        };

        Geometry actual = new GeometryConstructive().Buffer(
            source, 1.25, parameters);
        NtsGeometry expected = NtsBufferOp.Buffer(
            ntsSource, 1.25, ntsParameters);

        NtsGeometryComparison.AssertAreaEquivalent(
            expected, actual, 1.0e-9, "mitre/flat line buffer");
    }

    [TestCase("POLYGON ((0 0, 6 0, 6 6, 0 6, 0 0))")]
    [TestCase("POLYGON ((0 0, 5 5, 0 5, 5 0, 0 0))")]
    [TestCase("POLYGON ((0 0, 8 0, 8 8, 0 8, 0 0), (2 2, 2 6, 6 6, 6 2, 2 2))")]
    public void ValidityMatchesNetTopologySuite(string text)
    {
        Geometry source = NtsGeometryAdapter.ReadGeo(text);
        NtsGeometry ntsSource = NtsGeometryAdapter.ReadNts(text);

        bool expected = new NtsIsValidOp(ntsSource).IsValid;
        bool actual = new GeometryValidator().ValidateGeometry(source).IsValid;

        Assert.That(actual, Is.EqualTo(expected));
    }

    [TestCase("POLYGON ((0 0, 4 4, 0 4, 4 0, 0 0))")]
    [TestCase("MULTIPOLYGON (((0 0, 4 0, 4 4, 0 4, 0 0)), ((2 2, 6 2, 6 6, 2 6, 2 2)))")]
    public void StructureRepairMatchesNetTopologySuite(string text)
    {
        Geometry source = NtsGeometryAdapter.ReadGeo(text);
        NtsGeometry ntsSource = NtsGeometryAdapter.ReadNts(text);
        RepairParameters parameters = new RepairParameters
        {
            Method = RepairMethod.Structure,
            KeepCollapsedComponents = false
        };

        Geometry actual = new GeometryRepair().MakeValid(
            source, parameters);
        NtsGeometry expected = NtsGeometryFixer.Fix(ntsSource);

        Assert.That(new GeometryValidator().ValidateGeometry(actual).IsValid,
            Is.True);
        NtsGeometryComparison.AssertEquivalent(
            expected, actual, "structure repair");
    }

    [TestCase("POINT (2.5 -3)")]
    [TestCase("LINESTRING (0 0, 2 3, 5 1)")]
    [TestCase("POLYGON ((0 0, 8 0, 8 6, 0 6, 0 0), (2 2, 2 4, 4 4, 4 2, 2 2))")]
    [TestCase("MULTIPOLYGON (((0 0, 2 0, 2 2, 0 2, 0 0)), ((4 0, 7 0, 7 3, 4 3, 4 0)))")]
    public void WktAndWkbAreInteroperableWithNetTopologySuite(string text)
    {
        Geometry geoSource = NtsGeometryAdapter.ReadGeo(text);
        NtsGeometry ntsSource = NtsGeometryAdapter.ReadNts(text);

        NtsGeometry ntsFromGeoWkt = NtsGeometryAdapter.ReadNts(
            NtsGeometryAdapter.WriteGeoWkt(geoSource));
        Geometry geoFromNtsWkt = NtsGeometryAdapter.ReadGeo(
            NtsGeometryAdapter.WriteNtsWkt(ntsSource));
        NtsGeometry ntsFromGeoWkb = NtsGeometryAdapter.ReadNtsWkb(
            NtsGeometryAdapter.WriteGeoWkb(geoSource));
        Geometry geoFromNtsWkb = NtsGeometryAdapter.ReadGeoWkb(
            NtsGeometryAdapter.WriteNtsWkb(ntsSource));

        Assert.Multiple(() =>
        {
            Assert.That(ntsFromGeoWkt.EqualsTopologically(ntsSource),
                Is.True, "geo WKT read by NTS");
            Assert.That(new GeometryPredicates().AreTopologicallyEqual(
                geoFromNtsWkt, geoSource), Is.True,
                "NTS WKT read by geo");
            Assert.That(ntsFromGeoWkb.EqualsTopologically(ntsSource),
                Is.True, "geo WKB read by NTS");
            Assert.That(new GeometryPredicates().AreTopologicallyEqual(
                geoFromNtsWkb, geoSource), Is.True,
                "NTS WKB read by geo");
        });
    }
}
