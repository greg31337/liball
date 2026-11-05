using geo;
using System.IO;
using System.Text;
using System.Text.Json;

namespace geo.Tests;

public class GeoJsonFormatterTests
{
    [Test]
    public void FormatsAndStrictlyRoundTripsEveryRfc7946GeometryType()
    {
        string[] fixtures =
        [
            """{"type":"Point","coordinates":[1,2,3]}""",
            """{"type":"LineString","coordinates":[[0,0],[2,3]]}""",
            """{"type":"Polygon","coordinates":[[[0,0],[4,0],[4,4],[0,0]],[[1,1],[1,2],[2,1],[1,1]]]}""",
            """{"type":"MultiPoint","coordinates":[[1,2],[3,4]]}""",
            """{"type":"MultiLineString","coordinates":[[[0,0],[1,1]],[[2,2],[3,3]]]}""",
            """{"type":"MultiPolygon","coordinates":[[[[0,0],[2,0],[0,2],[0,0]]],[]]}""",
            """{"type":"GeometryCollection","geometries":[{"type":"Point","coordinates":[1,2],"child-extra":"kept"},{"type":"LineString","coordinates":[[0,0],[1,1]]}]}"""
        ];

        foreach (string fixture in fixtures)
        {
            GeoJsonDocument source = ParseSuccess(fixture);
            GeoJsonFormatter formatter = new GeoJsonFormatter(source, new GeoJsonWriteOptions());
            GeoJsonReadResult result = Parse(formatter.FormatString(), new GeoJsonReadOptions());

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True, result.Error?.Message);
                Assert.That(result.Document!.Geometry!.GeometryType,
                    Is.EqualTo(source.Geometry!.GeometryType));
                Assert.That(result.Document.Geometry.Value!.CoordinateLayout,
                    Is.EqualTo(source.Geometry.Value!.CoordinateLayout));
                Assert.That(formatter.FormatUtf8Bytes(), Is.EqualTo(
                    Encoding.UTF8.GetBytes(formatter.FormatString())));
            });
        }
    }

    [Test]
    public void CoordinatePrecisionWritesTrimmedXyAndXyzNumbers()
    {
        GeoJsonDocument xyz = ParseSuccess(
            """{"type":"Point","coordinates":[1.235,2,3.456]}""");
        GeoJsonFormatter twoDigits = new GeoJsonFormatter(
            xyz, new GeoJsonWriteOptions { CoordinatePrecision = 2 });
        GeoJsonDocument integerPoint = ParseSuccess(
            """{"type":"Point","coordinates":[100.4,-0.4]}""");
        GeoJsonFormatter integer = new GeoJsonFormatter(
            integerPoint, new GeoJsonWriteOptions { CoordinatePrecision = 0 });

        Assert.Multiple(() =>
        {
            Assert.That(twoDigits.FormatString(),
                Is.EqualTo("{\"type\":\"Point\",\"coordinates\":[1.24,2,3.46]}"));
            Assert.That(integer.FormatString(),
                Is.EqualTo("{\"type\":\"Point\",\"coordinates\":[100,0]}"));
        });
    }

    [Test]
    public void RewindsPolygonRingsWithoutMutatingTheInput()
    {
        string legacy =
            """{"type":"Polygon","coordinates":[[[0,0],[0,4],[4,0],[0,0]],[[1,1],[2,1],[1,2],[1,1]]]}""";
        GeoJsonDocument source = ParseSuccess(legacy, new GeoJsonReadOptions { Strict = false });
        Coordinate[] originalShell = source.Geometry!.Value!.Polygon!.Shell.Coordinates;
        Coordinate[] originalHole = source.Geometry.Value.Polygon.Holes[0].Coordinates;
        double shellArea = GeometryNormalization.GetSignedRingArea(originalShell);
        double holeArea = GeometryNormalization.GetSignedRingArea(originalHole);

        string output = new GeoJsonFormatter(source, new GeoJsonWriteOptions()).FormatString();
        GeoJsonDocument parsed = ParseSuccess(output);

        Assert.Multiple(() =>
        {
            Assert.That(shellArea, Is.LessThan(0.0));
            Assert.That(holeArea, Is.GreaterThan(0.0));
            Assert.That(GeometryNormalization.GetSignedRingArea(originalShell), Is.EqualTo(shellArea));
            Assert.That(GeometryNormalization.GetSignedRingArea(originalHole), Is.EqualTo(holeArea));
            Assert.That(GeometryNormalization.GetSignedRingArea(
                parsed.Geometry!.Value!.Polygon!.Shell.Coordinates), Is.GreaterThan(0.0));
            Assert.That(GeometryNormalization.GetSignedRingArea(
                parsed.Geometry.Value.Polygon.Holes[0].Coordinates), Is.LessThan(0.0));
        });
    }

    [Test]
    public void StrictOutputRejectsWrongOrientationWhenRewindingIsDisabled()
    {
        string legacy =
            """{"type":"Polygon","coordinates":[[[0,0],[0,4],[4,0],[0,0]]]}""";
        GeoJsonDocument wrong = ParseSuccess(legacy, new GeoJsonReadOptions { Strict = false });
        GeoJsonDocument correct = ParseSuccess(
            """{"type":"Polygon","coordinates":[[[0,0],[4,0],[0,4],[0,0]]]}""");
        GeoJsonWriteOptions options = new GeoJsonWriteOptions { NormalizeRingOrientation = false };

        Assert.Multiple(() =>
        {
            Assert.Throws<GeoJsonException>(() => new GeoJsonFormatter(wrong, options));
            Assert.DoesNotThrow(() => new GeoJsonFormatter(correct, options));
            Assert.DoesNotThrow(() => new GeoJsonFormatter(
                wrong, new GeoJsonWriteOptions { Strict = false, NormalizeRingOrientation = false }));
        });
    }

    [Test]
    public void EmitsConfiguredBoundingBoxesAndPreservesAntimeridianOrder()
    {
        GeoJsonDocument document = ParseSuccess(
            """{"type":"Point","coordinates":[175.1234,2.9876],"bbox":[170.1234,-10.5555,-170.9876,10.4444]}""");
        GeoJsonFormatter excluded = new GeoJsonFormatter(document, new GeoJsonWriteOptions());
        GeoJsonFormatter included = new GeoJsonFormatter(document, new GeoJsonWriteOptions
        {
            CoordinatePrecision = 2,
            IncludeBoundingBox = true
        });
        JsonElement root = ParseJson(included.FormatString());
        JsonElement bbox = root.GetProperty("bbox");

        Assert.Multiple(() =>
        {
            Assert.That(excluded.FormatString(), Does.Not.Contain("bbox"));
            Assert.That(bbox[0].GetDouble(), Is.EqualTo(170.12));
            Assert.That(bbox[2].GetDouble(), Is.EqualTo(-170.99));
            Assert.That(bbox[0].GetDouble(), Is.GreaterThan(bbox[2].GetDouble()));
            Assert.That(root.GetProperty("coordinates")[0].GetDouble(), Is.EqualTo(175.12));
        });
    }

    [Test]
    public void FormatsFeaturePropertiesIdsNullGeometryAndForeignMembers()
    {
        string json = """
            {
              "type":"Feature",
              "id":"a",
              "geometry":null,
              "properties":{"name":"alpha","values":[1,null]},
              "z-extra":2,
              "a-extra":{"enabled":true}
            }
            """;
        GeoJsonDocument document = ParseSuccess(json);
        string output = new GeoJsonFormatter(document, new GeoJsonWriteOptions()).FormatString();
        JsonElement root = ParseJson(output);

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("type").GetString(), Is.EqualTo("Feature"));
            Assert.That(root.GetProperty("id").GetString(), Is.EqualTo("a"));
            Assert.That(root.GetProperty("geometry").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(root.GetProperty("properties").GetProperty("name").GetString(), Is.EqualTo("alpha"));
            Assert.That(root.GetProperty("a-extra").GetProperty("enabled").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("z-extra").GetInt32(), Is.EqualTo(2));
            Assert.That(output.IndexOf("a-extra", StringComparison.Ordinal),
                Is.LessThan(output.IndexOf("z-extra", StringComparison.Ordinal)));
        });
    }

    [Test]
    public void FormatsFeatureCollectionsAndNestedGeometryMetadata()
    {
        string json = """
            {
              "type":"FeatureCollection",
              "collection-extra":true,
              "features":[
                {"type":"Feature","id":1,"properties":{},"geometry":{"type":"Point","coordinates":[1,2],"point-extra":"one"}},
                {"type":"Feature","properties":null,"geometry":null}
              ]
            }
            """;
        GeoJsonDocument source = ParseSuccess(json);
        string output = new GeoJsonFormatter(source, new GeoJsonWriteOptions()).FormatString();
        GeoJsonDocument result = ParseSuccess(output);

        Assert.Multiple(() =>
        {
            Assert.That(result.ObjectType, Is.EqualTo(GeoJsonObjectType.FeatureCollection));
            Assert.That(result.FeatureCollection!.Features, Has.Length.EqualTo(2));
            Assert.That(result.FeatureCollection.Features[0].Id.GetInt32(), Is.EqualTo(1));
            Assert.That(result.FeatureCollection.Features[0].Geometry!.ForeignMembers["point-extra"].GetString(),
                Is.EqualTo("one"));
            Assert.That(result.FeatureCollection.Features[1].Geometry, Is.Null);
            Assert.That(result.ForeignMembers["collection-extra"].GetBoolean(), Is.True);
        });
    }

    [Test]
    public void ForeignMemberAndCrsOptionsControlOutput()
    {
        GeoJsonDocument document = ParseSuccess(
            """{"type":"Point","coordinates":[1,2],"extra":3}""");
        string omitted = new GeoJsonFormatter(document,
            new GeoJsonWriteOptions { IncludeForeignMembers = false }).FormatString();
        document.ForeignMembers["crs"] = JsonValue("{\"name\":\"legacy\"}");

        Assert.Multiple(() =>
        {
            Assert.That(omitted, Does.Not.Contain("extra"));
            Assert.Throws<GeoJsonException>(() => new GeoJsonFormatter(document, new GeoJsonWriteOptions()));
            Assert.That(new GeoJsonFormatter(document,
                new GeoJsonWriteOptions { IncludeForeignMembers = false }).FormatString(),
                Does.Not.Contain("crs"));
            Assert.That(new GeoJsonFormatter(document,
                new GeoJsonWriteOptions { Strict = false }).FormatString(), Does.Contain("crs"));
        });
    }

    [Test]
    public void Utf8SpanAndStreamOutputsMatchAndDoNotPartiallyWrite()
    {
        GeoJsonFormatter formatter = new GeoJsonFormatter(
            ParseSuccess("""{"type":"Point","coordinates":[1,2]}"""),
            new GeoJsonWriteOptions());
        byte[] expected = formatter.FormatUtf8Bytes();
        byte[] exact = new byte[expected.Length];
        byte[] small = Enumerable.Repeat((byte)0xaa, expected.Length - 1).ToArray();
        MemoryStream stream = new MemoryStream();
        stream.WriteByte(0xff);

        WriteResult success = formatter.TryWriteUtf8Bytes(exact);
        WriteResult failure = formatter.TryWriteUtf8Bytes(small);
        formatter.WriteToStream(stream);
        stream.WriteByte(0xee);

        Assert.Multiple(() =>
        {
            Assert.That(success.Success, Is.True);
            Assert.That(success.RequiredSize, Is.EqualTo(expected.Length));
            Assert.That(success.WrittenSize, Is.EqualTo(expected.Length));
            Assert.That(exact, Is.EqualTo(expected));
            Assert.That(failure.Success, Is.False);
            Assert.That(failure.RequiredSize, Is.EqualTo(expected.Length));
            Assert.That(failure.WrittenSize, Is.Zero);
            Assert.That(failure.Error!.Kind, Is.EqualTo(ParseErrorKind.LimitExceeded));
            Assert.That(small, Is.All.EqualTo(0xaa));
            Assert.That(stream.ToArray().Skip(1).Take(expected.Length), Is.EqualTo(expected));
            Assert.That(stream.ToArray()[stream.ToArray().Length - 1], Is.EqualTo(0xee));
            Assert.Throws<GeoJsonException>(() => formatter.WriteToStream(null!));
        });
    }

    [Test]
    public void IndentationAndOwningResultsAreStableSnapshots()
    {
        GeoJsonWriteOptions options = new GeoJsonWriteOptions { Indented = true };
        GeoJsonDocument document = ParseSuccess(
            """{"type":"Point","coordinates":[1.234,2]}""");
        GeoJsonFormatter formatter = new GeoJsonFormatter(document, options);
        options.Indented = false;
        options.CoordinatePrecision = 0;
        byte[] first = formatter.FormatUtf8Bytes();
        first[0] = 0;
        byte[] second = formatter.FormatUtf8Bytes();

        Assert.Multiple(() =>
        {
            Assert.That(formatter.FormatString(), Does.Contain(Environment.NewLine));
            Assert.That(formatter.FormatString(), Does.Contain("1.234"));
            Assert.That(second[0], Is.EqualTo((byte)'{'));
        });
    }

    [Test]
    public void RejectsUnsupportedLayoutsTypesSridsAndMalformedDocuments()
    {
        GeoJsonDocument xym = GeometryDocument(ParseWkt("POINT M (1 2 3)"));
        GeoJsonDocument triangle = GeometryDocument(ParseWkt(
            "TRIANGLE ((0 0, 2 0, 0 2, 0 0))"));
        GeoJsonDocument wrongSrid = ParseSuccess(
            """{"type":"Point","coordinates":[1,2]}""");
        wrongSrid.Geometry!.Value!.SpatialReferenceId = 3857;
        GeoJsonDocument wrongUnion = ParseSuccess(
            """{"type":"Point","coordinates":[1,2]}""");
        wrongUnion.Feature = new GeoJsonFeature { Properties = JsonValue("null") };
        GeoJsonDocument missingProperties = new GeoJsonDocument
        {
            ObjectType = GeoJsonObjectType.Feature,
            Feature = new GeoJsonFeature()
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<GeoJsonException>(() => new GeoJsonFormatter(xym, new GeoJsonWriteOptions()));
            Assert.Throws<GeoJsonException>(() => new GeoJsonFormatter(triangle, new GeoJsonWriteOptions()));
            Assert.Throws<GeoJsonException>(() => new GeoJsonFormatter(wrongSrid, new GeoJsonWriteOptions()));
            Assert.Throws<GeoJsonException>(() => new GeoJsonFormatter(wrongUnion, new GeoJsonWriteOptions()));
            Assert.Throws<GeoJsonException>(() => new GeoJsonFormatter(
                missingProperties, new GeoJsonWriteOptions()));
            Assert.Throws<GeoJsonException>(() => new GeoJsonFormatter(null!, new GeoJsonWriteOptions()));
            Assert.Throws<GeoJsonException>(() => new GeoJsonFormatter(
                ParseSuccess("""{"type":"Point","coordinates":[1,2]}"""), null!));
        });
    }

    [Test]
    public void RejectsInvalidPrecisionBboxesForeignMembersAndCollapsedRings()
    {
        GeoJsonDocument point = ParseSuccess(
            """{"type":"Point","coordinates":[1,2]}""");
        GeoJsonDocument badBbox = ParseSuccess(
            """{"type":"Point","coordinates":[1,2]}""");
        badBbox.BoundingBox = [0.0, 10.0, 5.0, -10.0];
        GeoJsonDocument conflict = ParseSuccess(
            """{"type":"Point","coordinates":[1,2]}""");
        conflict.ForeignMembers["coordinates"] = JsonValue("[3,4]");
        GeoJsonDocument undefinedForeign = ParseSuccess(
            """{"type":"Point","coordinates":[1,2]}""");
        undefinedForeign.ForeignMembers["bad"] = default(JsonElement);
        GeoJsonDocument collapsed = ParseSuccess(
            """{"type":"Polygon","coordinates":[[[0.1,0.1],[0.4,0.1],[0.1,0.4],[0.1,0.1]]]}""");

        Assert.Multiple(() =>
        {
            Assert.Throws<GeoJsonException>(() => new GeoJsonFormatter(
                point, new GeoJsonWriteOptions { CoordinatePrecision = -1 }));
            Assert.Throws<GeoJsonException>(() => new GeoJsonFormatter(
                point, new GeoJsonWriteOptions { CoordinatePrecision = 16 }));
            Assert.Throws<GeoJsonException>(() => new GeoJsonFormatter(
                badBbox, new GeoJsonWriteOptions { IncludeBoundingBox = true }));
            Assert.Throws<GeoJsonException>(() => new GeoJsonFormatter(conflict, new GeoJsonWriteOptions()));
            Assert.Throws<GeoJsonException>(() => new GeoJsonFormatter(
                undefinedForeign, new GeoJsonWriteOptions()));
            Assert.Throws<GeoJsonException>(() => new GeoJsonFormatter(
                collapsed, new GeoJsonWriteOptions { CoordinatePrecision = 0 }));
        });
    }

    private static GeoJsonDocument ParseSuccess(string json)
    {
        return ParseSuccess(json, new GeoJsonReadOptions());
    }

    private static GeoJsonDocument ParseSuccess(string json, GeoJsonReadOptions options)
    {
        GeoJsonReadResult result = Parse(json, options);
        Assert.That(result.Success, Is.True, result.Error?.Message);
        return result.Document!;
    }

    private static GeoJsonReadResult Parse(string json, GeoJsonReadOptions options)
    {
        return new GeoJsonParser(Encoding.UTF8.GetBytes(json), options).ParseDocument();
    }

    private static JsonElement ParseJson(string json)
    {
        using (JsonDocument document = JsonDocument.Parse(json))
        {
            return document.RootElement.Clone();
        }
    }

    private static JsonElement JsonValue(string json)
    {
        return ParseJson(json);
    }

    private static Geometry ParseWkt(string text)
    {
        GeometryReadResult result = new WktParser(text, new WktReadOptions()).ParseDocument();
        Assert.That(result.Success, Is.True, result.Error?.Message);
        return result.Geometry!;
    }

    private static GeoJsonDocument GeometryDocument(Geometry geometry)
    {
        GeoJsonGeometry wrapper = new GeoJsonGeometry();
        wrapper.GeometryType = geometry.GeometryType;
        wrapper.Value = geometry;
        GeoJsonDocument document = new GeoJsonDocument();
        document.ObjectType = GeoJsonObjectType.Geometry;
        document.Geometry = wrapper;
        return document;
    }
}
