using geo;
using System.Text;
using System.Text.Json;

namespace geo.Tests;

public class GeoJsonConformanceTests
{
    [Test]
    public void PublicCodecCanonicallyRoundTripsEveryGeometryTypeAndLayout()
    {
        IGeoJsonCodec codec = CreateCodec();
        string[] fixtures = GeometryFixtures();

        foreach (string fixture in fixtures)
        {
            GeoJsonDocument source = codec.ReadGeoJson(fixture);
            string canonical = codec.WriteGeoJson(source);
            GeoJsonDocument fromText = codec.ReadGeoJson(canonical);
            GeoJsonDocument fromUtf8 = codec.ReadGeoJsonUtf8(Encoding.UTF8.GetBytes(canonical));

            Assert.Multiple(() =>
            {
                Assert.That(source.ObjectType, Is.EqualTo(GeoJsonObjectType.Geometry), fixture);
                Assert.That(fromText.Geometry!.GeometryType,
                    Is.EqualTo(source.Geometry!.GeometryType), fixture);
                Assert.That(fromText.Geometry.Value!.CoordinateLayout,
                    Is.EqualTo(source.Geometry.Value!.CoordinateLayout), fixture);
                Assert.That(fromUtf8.Geometry!.GeometryType,
                    Is.EqualTo(source.Geometry.GeometryType), fixture);
                Assert.That(GeometryNormalization.CompareGeometryValues(
                    fromText.Geometry.Value, source.Geometry.Value), Is.Zero, fixture);
                Assert.That(codec.WriteGeoJson(fromText), Is.EqualTo(canonical), fixture);
            });
        }

        foreach (string fixture in EmptyGeometryFixtures())
        {
            GeoJsonDocument document = codec.ReadGeoJson(fixture);
            GeoJsonDocument roundTrip = codec.ReadGeoJson(codec.WriteGeoJson(document));
            Assert.Multiple(() =>
            {
                Assert.That(roundTrip.Geometry!.GeometryType,
                    Is.EqualTo(document.Geometry!.GeometryType), fixture);
                Assert.That(GeometryInspection.IsEmptyValue(roundTrip.Geometry.Value!), Is.True, fixture);
            });
        }
    }

    [Test]
    public void FeaturesCollectionsIdsPropertiesNullsAndForeignMembersRoundTrip()
    {
        string json = """
            {
              "type":"FeatureCollection",
              "bbox":[170,-10,-170,10],
              "collection-extra":{"version":2},
              "features":[
                {
                  "type":"Feature",
                  "id":"alpha",
                  "properties":{"name":"東京","flags":[true,null]},
                  "bbox":[175,2,175,2],
                  "feature-extra":"kept",
                  "geometry":{"type":"Point","coordinates":[175,2],"geometry-extra":1}
                },
                {
                  "type":"Feature",
                  "id":42.5,
                  "properties":[1,{"nested":true}],
                  "geometry":null
                }
              ]
            }
            """;
        IGeoJsonCodec codec = new GeoJsonCodec(
            new GeoJsonReadOptions(), new GeoJsonWriteOptions { IncludeBoundingBox = true });

        GeoJsonDocument source = codec.ReadGeoJson(json);
        string output = codec.WriteGeoJson(source);
        GeoJsonDocument result = codec.ReadGeoJson(output);
        GeoJsonFeatureCollection collection = result.FeatureCollection!;

        Assert.Multiple(() =>
        {
            Assert.That(result.ObjectType, Is.EqualTo(GeoJsonObjectType.FeatureCollection));
            Assert.That(collection.Features, Has.Length.EqualTo(2));
            Assert.That(collection.Features[0].Id.GetString(), Is.EqualTo("alpha"));
            Assert.That(collection.Features[0].Properties.GetProperty("name").GetString(),
                Is.EqualTo("東京"));
            Assert.That(collection.Features[0].Properties.GetProperty("flags")[1].ValueKind,
                Is.EqualTo(JsonValueKind.Null));
            Assert.That(collection.Features[0].ForeignMembers["feature-extra"].GetString(),
                Is.EqualTo("kept"));
            Assert.That(collection.Features[0].Geometry!.ForeignMembers["geometry-extra"].GetInt32(),
                Is.EqualTo(1));
            Assert.That(collection.Features[1].Id.GetDouble(), Is.EqualTo(42.5));
            Assert.That(collection.Features[1].Properties.ValueKind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(collection.Features[1].Geometry, Is.Null);
            Assert.That(result.ForeignMembers["collection-extra"].GetProperty("version").GetInt32(),
                Is.EqualTo(2));
            Assert.That(result.BoundingBox, Is.EqualTo(new[] { 170.0, -10.0, -170.0, 10.0 }));
            Assert.That(result.BoundingBox[0], Is.GreaterThan(result.BoundingBox[2]));
        });
    }

    [Test]
    public void BoundingBoxesPreserveDimensionsAndAntimeridianWrapping()
    {
        IGeoJsonCodec codec = new GeoJsonCodec(
            new GeoJsonReadOptions(), new GeoJsonWriteOptions { IncludeBoundingBox = true });
        string xy =
            """{"type":"Point","coordinates":[175,2],"bbox":[170,-10,-170,10]}""";
        string xyz =
            """{"type":"Point","coordinates":[175,2,5],"bbox":[170,-10,0,-170,10,100]}""";

        GeoJsonDocument xyResult = codec.ReadGeoJson(codec.WriteGeoJson(codec.ReadGeoJson(xy)));
        GeoJsonDocument xyzResult = codec.ReadGeoJson(codec.WriteGeoJson(codec.ReadGeoJson(xyz)));
        GeoJsonReadResult mismatch = codec.TryReadGeoJson(
            """{"type":"Point","coordinates":[1,2,3],"bbox":[1,2,1,2]}""");
        GeoJsonReadResult invertedLatitude = codec.TryReadGeoJson(
            """{"type":"Point","coordinates":[1,2],"bbox":[0,10,5,-10]}""");

        Assert.Multiple(() =>
        {
            Assert.That(xyResult.BoundingBox, Has.Length.EqualTo(4));
            Assert.That(xyResult.BoundingBox[0], Is.GreaterThan(xyResult.BoundingBox[2]));
            Assert.That(xyzResult.BoundingBox, Has.Length.EqualTo(6));
            Assert.That(xyzResult.Geometry!.Value!.CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XYZ));
            Assert.That(mismatch.Error!.Kind, Is.EqualTo(ParseErrorKind.InvalidCoordinateLayout));
            Assert.That(mismatch.Error.JsonPath, Is.EqualTo("$.bbox"));
            Assert.That(invertedLatitude.Error!.Kind, Is.EqualTo(ParseErrorKind.InvalidGeoJson));
            Assert.That(invertedLatitude.Error.JsonPath, Is.EqualTo("$.bbox"));
        });
    }

    [Test]
    public void StrictAndTolerantRingRulesAndEveryOptionAreSnapshotted()
    {
        string legacyRing =
            """{"type":"Polygon","coordinates":[[[0.123,0.123],[0.123,4.567],[4.567,0.123],[0.123,0.123]]]}""";
        string legacy =
            """{"type":"Polygon","coordinates":[[[0.123,0.123],[0.123,4.567],[4.567,0.123],[0.123,0.123]]],"bbox":[170.123,-10.555,-170.987,10.444],"crs":{"name":"legacy"},"extra":1}""";
        IGeoJsonCodec strict = CreateCodec();
        GeoJsonReadOptions readOptions = new GeoJsonReadOptions
        {
            Strict = false,
            PreserveForeignMembers = true,
            MaximumCoordinates = 4,
            MaximumComponents = 2,
            MaximumNestingDepth = 0,
            MaximumInputBytes = 512
        };
        GeoJsonWriteOptions writeOptions = new GeoJsonWriteOptions
        {
            Strict = false,
            CoordinatePrecision = 2,
            IncludeBoundingBox = true,
            IncludeForeignMembers = true,
            NormalizeRingOrientation = false,
            Indented = true
        };
        IGeoJsonCodec tolerant = new GeoJsonCodec(readOptions, writeOptions);
        readOptions.Strict = true;
        readOptions.PreserveForeignMembers = false;
        readOptions.MaximumCoordinates = 1;
        readOptions.MaximumComponents = 1;
        readOptions.MaximumNestingDepth = -1;
        readOptions.MaximumInputBytes = 1;
        writeOptions.Strict = true;
        writeOptions.CoordinatePrecision = 0;
        writeOptions.IncludeBoundingBox = false;
        writeOptions.IncludeForeignMembers = false;
        writeOptions.NormalizeRingOrientation = true;
        writeOptions.Indented = false;

        GeoJsonReadResult strictResult = strict.TryReadGeoJson(legacyRing);
        GeoJsonDocument document = tolerant.ReadGeoJson(legacy);
        string output = tolerant.WriteGeoJson(document);
        GeoJsonDocument roundTrip = tolerant.ReadGeoJson(output);
        double area = GeometryNormalization.GetSignedRingArea(
            roundTrip.Geometry!.Value!.Polygon!.Shell.Coordinates);

        Assert.Multiple(() =>
        {
            Assert.That(strictResult.Error!.Kind, Is.EqualTo(ParseErrorKind.InvalidGeoJson));
            Assert.That(strictResult.Error.JsonPath, Is.EqualTo("$.coordinates[0]"));
            Assert.That(output, Does.Contain(Environment.NewLine));
            Assert.That(output, Does.Contain("\"bbox\""));
            Assert.That(output, Does.Contain("\"crs\""));
            Assert.That(output, Does.Contain("\"extra\""));
            Assert.That(output, Does.Contain("0.12"));
            Assert.That(area, Is.LessThan(0.0));
            Assert.That(roundTrip.BoundingBox[0], Is.GreaterThan(roundTrip.BoundingBox[2]));
        });
    }

    [Test]
    public void StrictFailuresExposeStableKindsAndJsonPaths()
    {
        IGeoJsonCodec codec = CreateCodec();

        AssertFailure(codec,
            """{"type":"Triangle","coordinates":[]}""",
            ParseErrorKind.InvalidGeometryType, "$.type");
        AssertFailure(codec,
            """{"type":"Point","coordinates":[1,"two"]}""",
            ParseErrorKind.InvalidNumber, "$.coordinates[1]");
        AssertFailure(codec,
            """{"type":"LineString","coordinates":[[0,0],[1,1,1]]}""",
            ParseErrorKind.InvalidCoordinateLayout, "$.coordinates[1]");
        AssertFailure(codec,
            """{"type":"Polygon","coordinates":[[[0,0],[4,0],[0,4],[1,1]]]}""",
            ParseErrorKind.InvalidCoordinate, "$.coordinates[0]");
        AssertFailure(codec,
            """{"type":"Feature","id":true,"properties":{},"geometry":null}""",
            ParseErrorKind.InvalidGeoJson, "$.id");
        AssertFailure(codec,
            """{"type":"FeatureCollection","features":[null]}""",
            ParseErrorKind.InvalidGeoJson, "$.features[0]");
        AssertFailure(codec,
            """{"type":"Point","coordinates":[1,2],"bbox":[1,2,3]}""",
            ParseErrorKind.InvalidCount, "$.bbox");
        AssertFailure(codec,
            """{"type":"Point","coordinates":[1,2],"crs":{}}""",
            ParseErrorKind.InvalidGeoJson, "$.crs");
        AssertFailure(codec,
            """{"type":"Point","coordinates":[1,2],}""",
            ParseErrorKind.UnexpectedToken, "$");
    }

    [Test]
    public void ChunkedDocumentAndSequenceStreamsCoverEveryRootAndRecoverMalformedRecords()
    {
        IGeoJsonCodec codec = CreateCodec();
        List<GeoJsonDocument> documents = new List<GeoJsonDocument>();
        foreach (string fixture in GeometryFixtures().Take(7))
        {
            documents.Add(codec.ReadGeoJson(fixture));
        }
        documents.Add(codec.ReadGeoJson(
            """{"type":"Feature","id":"null-geometry","properties":null,"geometry":null}"""));
        documents.Add(codec.ReadGeoJson(
            """{"type":"FeatureCollection","features":[{"type":"Feature","id":1,"properties":{},"geometry":{"type":"Point","coordinates":[1,2]}}]}"""));

        MemoryStream documentStream = new MemoryStream();
        codec.WriteGeoJsonStream(documents[7], documentStream);
        byte[] documentBytes = documentStream.ToArray();
        GeoJsonDocument streamedDocument = codec.ReadGeoJsonStream(
            new ChunkedReadStream(documentBytes, 2));
        documentStream.WriteByte(0xaa);

        IGeoJsonSequenceCodec sequenceCodec = new GeoJsonSequenceCodec(
            new GeoJsonReadOptions(), new GeoJsonWriteOptions());
        MemoryStream sequenceStream = new MemoryStream();
        sequenceCodec.WriteGeoJsonSequence(documents.ToArray(), sequenceStream);
        byte[] validSequence = sequenceStream.ToArray();
        byte[] malformed = Encoding.UTF8.GetBytes(
            "\u001e{\"type\":\"Point\",\"coordinates\":[1,]}\n");
        byte[] combined = new byte[malformed.Length + validSequence.Length];
        malformed.CopyTo(combined, 0);
        validSequence.CopyTo(combined, malformed.Length);

        GeoJsonReadResult[] results = sequenceCodec.ReadGeoJsonSequence(
            new ChunkedReadStream(combined, 3));
        int separators = validSequence.Count(value => value == 0x1e);

        Assert.Multiple(() =>
        {
            Assert.That(streamedDocument.Feature!.Geometry, Is.Null);
            Assert.That(documentStream.Length, Is.EqualTo(documentBytes.Length + 1));
            Assert.That(separators, Is.EqualTo(documents.Count));
            Assert.That(results, Has.Length.EqualTo(documents.Count + 1));
            Assert.That(results[0].Success, Is.False);
            Assert.That(results[0].Error!.ComponentPath, Is.EqualTo("Record[0]"));
            Assert.That(results.Skip(1).All(result => result.Success), Is.True,
                string.Join(" | ", results.Skip(1).Select(result => result.Error?.Message)));
            Assert.That(results[1].Document!.Geometry!.GeometryType, Is.EqualTo(GeometryType.Point));
            Assert.That(results[8].Document!.ObjectType, Is.EqualTo(GeoJsonObjectType.Feature));
            Assert.That(results[9].Document!.ObjectType,
                Is.EqualTo(GeoJsonObjectType.FeatureCollection));
        });
    }

    private static void AssertFailure(
        IGeoJsonCodec codec, string json, ParseErrorKind kind, string path)
    {
        GeoJsonReadResult result = codec.TryReadGeoJson(json);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False, json);
            Assert.That(result.Document, Is.Null, json);
            Assert.That(result.Error, Is.Not.Null, json);
            Assert.That(result.Error!.Kind, Is.EqualTo(kind), json);
            Assert.That(result.Error.JsonPath, Is.EqualTo(path), json);
            Assert.That(result.Error.Offset, Is.GreaterThanOrEqualTo(0), json);
            Assert.That(result.Consumed, Is.EqualTo(result.Error.Offset), json);
        });
    }

    private static IGeoJsonCodec CreateCodec()
    {
        return new GeoJsonCodec(new GeoJsonReadOptions(), new GeoJsonWriteOptions());
    }

    private static string[] GeometryFixtures()
    {
        return
        [
            """{"type":"Point","coordinates":[1,2]}""",
            """{"type":"LineString","coordinates":[[0,0],[2,3]]}""",
            """{"type":"Polygon","coordinates":[[[0,0],[4,0],[4,4],[0,4],[0,0]]]}""",
            """{"type":"MultiPoint","coordinates":[[1,2],[3,4]]}""",
            """{"type":"MultiLineString","coordinates":[[[0,0],[1,1]],[[2,2],[3,3]]]}""",
            """{"type":"MultiPolygon","coordinates":[[[[0,0],[2,0],[2,2],[0,2],[0,0]]]]}""",
            """{"type":"GeometryCollection","geometries":[{"type":"Point","coordinates":[1,2]},{"type":"LineString","coordinates":[[0,0],[1,1]]}]}""",
            """{"type":"Point","coordinates":[1,2,3]}""",
            """{"type":"LineString","coordinates":[[0,0,1],[2,3,4]]}""",
            """{"type":"Polygon","coordinates":[[[0,0,1],[4,0,1],[4,4,1],[0,4,1],[0,0,1]]]}""",
            """{"type":"MultiPoint","coordinates":[[1,2,3],[3,4,5]]}""",
            """{"type":"MultiLineString","coordinates":[[[0,0,1],[1,1,2]],[[2,2,3],[3,3,4]]]}""",
            """{"type":"MultiPolygon","coordinates":[[[[0,0,1],[2,0,1],[2,2,1],[0,2,1],[0,0,1]]]]}""",
            """{"type":"GeometryCollection","geometries":[{"type":"Point","coordinates":[1,2,3]},{"type":"LineString","coordinates":[[0,0,1],[1,1,2]]}]}"""
        ];
    }

    private static string[] EmptyGeometryFixtures()
    {
        return
        [
            """{"type":"Point","coordinates":[]}""",
            """{"type":"LineString","coordinates":[]}""",
            """{"type":"Polygon","coordinates":[]}""",
            """{"type":"MultiPoint","coordinates":[]}""",
            """{"type":"MultiLineString","coordinates":[]}""",
            """{"type":"MultiPolygon","coordinates":[]}""",
            """{"type":"GeometryCollection","geometries":[]}"""
        ];
    }

    private sealed class ChunkedReadStream : MemoryStream
    {
        private readonly int maximumChunk;

        public ChunkedReadStream(byte[] bytes, int maximumChunk) : base(bytes)
        {
            this.maximumChunk = maximumChunk;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return base.Read(buffer, offset, Math.Min(count, maximumChunk));
        }
    }
}
