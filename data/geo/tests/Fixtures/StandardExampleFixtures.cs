using geo;

namespace geo.Tests;

internal class StandardTextFixture
{
    internal string Name = string.Empty;
    internal string Source = string.Empty;
    internal string Section = string.Empty;
    internal string Input = string.Empty;
    internal string Expected = string.Empty;
    internal GeometryType GeometryType;
}

internal static class StandardExampleFixtures
{
    internal const string OgcSfaSource =
        "https://docs.ogc.org/is/06-104r4/06-104r4.pdf";
    internal const string OgcSfaSection =
        "Annex C.3.3.2-C.3.3.3, conformance items T6-T52";
    internal const string Rfc7946Source =
        "https://www.rfc-editor.org/rfc/rfc7946.html";
    internal const string Rfc8142Source =
        "https://www.rfc-editor.org/rfc/rfc8142.html";

    internal const string BlueLake =
        "POLYGON ((52 18, 66 23, 73 9, 48 6, 52 18), " +
        "(59 18, 67 18, 67 13, 59 13, 59 18))";
    internal const string Route5Segment102 =
        "LINESTRING (0 18, 10 21, 16 23, 28 26, 44 31)";
    internal const string RoadSegment106 =
        "LINESTRING (28 26, 28 0)";
    internal const string Route75 =
        "MULTILINESTRING ((10 48, 10 21, 10 0), " +
        "(16 0, 16 23, 16 48))";
    internal const string GreenForest =
        "MULTIPOLYGON (((28 26, 28 0, 84 0, 84 42, 28 26), " +
        "(52 18, 66 23, 73 9, 48 6, 52 18)), " +
        "((59 18, 67 18, 67 13, 59 13, 59 18)))";
    internal const string CamBridge = "POINT (44 31)";
    internal const string CamStream =
        "LINESTRING (38 48, 44 41, 41 36, 44 31, 52 18)";
    internal const string House123 = "POINT (52 30)";
    internal const string House215 = "POINT (64 33)";
    internal const string House215Footprint =
        "POLYGON ((66 34, 62 34, 62 32, 66 32, 66 34))";
    internal const string Ponds =
        "MULTIPOLYGON (((24 44, 22 42, 24 40, 24 44)), " +
        "((26 44, 26 40, 28 42, 26 44)))";
    internal const string Ashton =
        "POLYGON ((62 48, 84 48, 84 30, 56 30, 56 34, 62 48))";
    internal const string GooseIsland =
        "POLYGON ((67 13, 67 18, 59 18, 59 13, 67 13))";
    internal const string GooseIslandWkbLittleEndian =
        "010300000001000000050000000000000000C050400000000000002A40" +
        "0000000000C0504000000000000032400000000000804D400000000000" +
        "0032400000000000804D400000000000002A400000000000C050400000" +
        "000000002A40";

    internal static StandardTextFixture[] Rfc7946GeometryExamples()
    {
        return
        [
            Fixture("point", "Appendix A.1",
                """{"type":"Point","coordinates":[100.0,0.0]}""",
                """{"type":"Point","coordinates":[100,0]}""",
                GeometryType.Point),
            Fixture("line string", "Appendix A.2",
                """{"type":"LineString","coordinates":[[100.0,0.0],[101.0,1.0]]}""",
                """{"type":"LineString","coordinates":[[100,0],[101,1]]}""",
                GeometryType.LineString),
            Fixture("polygon without holes", "Appendix A.3",
                """{"type":"Polygon","coordinates":[[[100.0,0.0],[101.0,0.0],[101.0,1.0],[100.0,1.0],[100.0,0.0]]]}""",
                """{"type":"Polygon","coordinates":[[[100,0],[101,0],[101,1],[100,1],[100,0]]]}""",
                GeometryType.Polygon),
            Fixture("polygon with a hole", "Appendix A.3",
                """{"type":"Polygon","coordinates":[[[100.0,0.0],[101.0,0.0],[101.0,1.0],[100.0,1.0],[100.0,0.0]],[[100.8,0.8],[100.8,0.2],[100.2,0.2],[100.2,0.8],[100.8,0.8]]]}""",
                """{"type":"Polygon","coordinates":[[[100,0],[101,0],[101,1],[100,1],[100,0]],[[100.8,0.8],[100.8,0.2],[100.2,0.2],[100.2,0.8],[100.8,0.8]]]}""",
                GeometryType.Polygon),
            Fixture("multi point", "Appendix A.4",
                """{"type":"MultiPoint","coordinates":[[100.0,0.0],[101.0,1.0]]}""",
                """{"type":"MultiPoint","coordinates":[[100,0],[101,1]]}""",
                GeometryType.MultiPoint),
            Fixture("multi line string", "Appendix A.5",
                """{"type":"MultiLineString","coordinates":[[[100.0,0.0],[101.0,1.0]],[[102.0,2.0],[103.0,3.0]]]}""",
                """{"type":"MultiLineString","coordinates":[[[100,0],[101,1]],[[102,2],[103,3]]]}""",
                GeometryType.MultiLineString),
            Fixture("multi polygon", "Appendix A.6",
                """{"type":"MultiPolygon","coordinates":[[[[102.0,2.0],[103.0,2.0],[103.0,3.0],[102.0,3.0],[102.0,2.0]]],[[[100.0,0.0],[101.0,0.0],[101.0,1.0],[100.0,1.0],[100.0,0.0]],[[100.2,0.2],[100.2,0.8],[100.8,0.8],[100.8,0.2],[100.2,0.2]]]]}""",
                """{"type":"MultiPolygon","coordinates":[[[[102,2],[103,2],[103,3],[102,3],[102,2]]],[[[100,0],[101,0],[101,1],[100,1],[100,0]],[[100.2,0.2],[100.2,0.8],[100.8,0.8],[100.8,0.2],[100.2,0.2]]]]}""",
                GeometryType.MultiPolygon),
            Fixture("geometry collection", "Appendix A.7",
                """{"type":"GeometryCollection","geometries":[{"type":"Point","coordinates":[100.0,0.0]},{"type":"LineString","coordinates":[[101.0,0.0],[102.0,1.0]]}]}""",
                """{"type":"GeometryCollection","geometries":[{"type":"Point","coordinates":[100,0]},{"type":"LineString","coordinates":[[101,0],[102,1]]}]}""",
                GeometryType.GeometryCollection)
        ];
    }

    internal static StandardTextFixture Rfc7946FeatureCollectionExample()
    {
        return Fixture("feature collection", "Section 1.5",
            """{"type":"FeatureCollection","features":[{"type":"Feature","geometry":{"type":"Point","coordinates":[102.0,0.5]},"properties":{"prop0":"value0"}},{"type":"Feature","geometry":{"type":"LineString","coordinates":[[102.0,0.0],[103.0,1.0],[104.0,0.0],[105.0,1.0]]},"properties":{"prop0":"value0","prop1":0.0}},{"type":"Feature","geometry":{"type":"Polygon","coordinates":[[[100.0,0.0],[101.0,0.0],[101.0,1.0],[100.0,1.0],[100.0,0.0]]]},"properties":{"prop0":"value0","prop1":{"this":"that"}}}]}""",
            """{"type":"FeatureCollection","features":[{"type":"Feature","geometry":{"type":"Point","coordinates":[102,0.5]},"properties":{"prop0":"value0"}},{"type":"Feature","geometry":{"type":"LineString","coordinates":[[102,0],[103,1],[104,0],[105,1]]},"properties":{"prop0":"value0","prop1":0.0}},{"type":"Feature","geometry":{"type":"Polygon","coordinates":[[[100,0],[101,0],[101,1],[100,1],[100,0]]]},"properties":{"prop0":"value0","prop1":{"this":"that"}}}]}""",
            GeometryType.GeometryCollection);
    }

    internal static StandardTextFixture Rfc7946AntimeridianLineExample()
    {
        return Fixture("antimeridian line", "Section 3.1.9",
            """{"type":"LineString","coordinates":[[170.0,45.0],[-170.0,45.0]]}""",
            """{"type":"MultiLineString","coordinates":[[[170,45],[180,45]],[[-180,45],[-170,45]]]}""",
            GeometryType.MultiLineString);
    }

    internal static StandardTextFixture Rfc7946AntimeridianPolygonExample()
    {
        return Fixture("antimeridian polygon", "Section 3.1.9",
            """{"type":"Polygon","coordinates":[[[170.0,40.0],[170.0,50.0],[-170.0,50.0],[-170.0,40.0],[170.0,40.0]]]}""",
            """{"type":"MultiPolygon","coordinates":[[[[180,40],[180,50],[170,50],[170,40],[180,40]]],[[[-170,40],[-170,50],[-180,50],[-180,40],[-170,40]]]]}""",
            GeometryType.MultiPolygon);
    }

    internal static StandardTextFixture Rfc8142HeterogeneousSequence()
    {
        string sequence =
            "\u001e{\"type\":\"Point\",\"coordinates\":[100,0]}\n" +
            "\u001e{\"type\":\"Feature\",\"geometry\":null,\"properties\":null}\n" +
            "\u001e{\"type\":\"FeatureCollection\",\"features\":[]}\n";
        return new StandardTextFixture
        {
            Name = "heterogeneous GeoJSON text sequence",
            Source = Rfc8142Source,
            Section = "Section 2",
            Input = sequence,
            Expected = sequence
        };
    }

    private static StandardTextFixture Fixture(
        string name, string section, string input, string expected,
        GeometryType geometryType)
    {
        return new StandardTextFixture
        {
            Name = name,
            Source = Rfc7946Source,
            Section = section,
            Input = input,
            Expected = expected,
            GeometryType = geometryType
        };
    }
}
