using geo;
using System.Text;
using System.Text.Json;

namespace geo.Tests;

public class GeoJsonFeatureParserTests
{
    [Test]
    public void ParsesFeatureWithStringIdArbitraryPropertiesAndGeometry()
    {
        string json = """
            {
              "type":"Feature",
              "id":"road-7",
              "properties":{"name":"Main","lanes":2,"tags":["local",null]},
              "geometry":{"type":"LineString","coordinates":[[0,1],[2,3]]},
              "bbox":[0,1,2,3]
            }
            """;

        GeoJsonReadResult result = Parse(json, new GeoJsonReadOptions());
        GeoJsonDocument document = result.Document!;
        GeoJsonFeature feature = document.Feature!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.Error?.Message);
            Assert.That(result.Consumed, Is.EqualTo(Encoding.UTF8.GetByteCount(json)));
            Assert.That(document.ObjectType, Is.EqualTo(GeoJsonObjectType.Feature));
            Assert.That(document.Geometry, Is.Null);
            Assert.That(document.FeatureCollection, Is.Null);
            Assert.That(feature.Id.GetString(), Is.EqualTo("road-7"));
            Assert.That(feature.Properties.GetProperty("name").GetString(), Is.EqualTo("Main"));
            Assert.That(feature.Properties.GetProperty("lanes").GetInt32(), Is.EqualTo(2));
            Assert.That(feature.Properties.GetProperty("tags")[1].ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(feature.Geometry!.Value!.LineString!.Coordinates[1].Y, Is.EqualTo(3.0));
            Assert.That(feature.Geometry.Value.SpatialReferenceId,
                Is.EqualTo(GeometryConstants.Crs84SpatialReferenceId));
            Assert.That(feature.BoundingBox, Is.EqualTo(new[] { 0.0, 1.0, 2.0, 3.0 }));
            Assert.That(document.BoundingBox, Is.EqualTo(feature.BoundingBox));
        });
    }

    [Test]
    public void ParsesNullGeometryAndNumberId()
    {
        GeoJsonFeature feature = ParseSuccess(
            """{"type":"Feature","id":42.5,"properties":null,"geometry":null}""").Feature!;

        Assert.Multiple(() =>
        {
            Assert.That(feature.Geometry, Is.Null);
            Assert.That(feature.Id.GetDouble(), Is.EqualTo(42.5));
            Assert.That(feature.Properties.ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(feature.BoundingBox, Is.Empty);
        });
    }

    [Test]
    public void AcceptsAnyJsonPropertiesValue()
    {
        string[] propertyValues = ["true", "12", "\"label\"", "[1,{\"x\":2}]", "null"];
        JsonValueKind[] kinds =
        [
            JsonValueKind.True,
            JsonValueKind.Number,
            JsonValueKind.String,
            JsonValueKind.Array,
            JsonValueKind.Null
        ];

        int index = 0;
        while (index < propertyValues.Length)
        {
            string json = "{\"type\":\"Feature\",\"properties\":" + propertyValues[index] +
                ",\"geometry\":null}";
            GeoJsonFeature feature = ParseSuccess(json).Feature!;
            Assert.That(feature.Properties.ValueKind, Is.EqualTo(kinds[index]), propertyValues[index]);
            index++;
        }
    }

    [Test]
    public void ParsesFeatureCollectionInOrderWithMixedGeometryLayouts()
    {
        string json = """
            {
              "type":"FeatureCollection",
              "features":[
                {"type":"Feature","id":"xy","properties":{},"geometry":{"type":"Point","coordinates":[1,2]}},
                {"type":"Feature","id":2,"properties":[],"geometry":{"type":"Point","coordinates":[3,4,5]}},
                {"type":"Feature","properties":null,"geometry":null}
              ],
              "bbox":[1,2,3,4]
            }
            """;

        GeoJsonDocument document = ParseSuccess(json);
        GeoJsonFeatureCollection collection = document.FeatureCollection!;

        Assert.Multiple(() =>
        {
            Assert.That(document.ObjectType, Is.EqualTo(GeoJsonObjectType.FeatureCollection));
            Assert.That(document.Feature, Is.Null);
            Assert.That(collection.Features, Has.Length.EqualTo(3));
            Assert.That(collection.Features[0].Id.GetString(), Is.EqualTo("xy"));
            Assert.That(collection.Features[0].Geometry!.Value!.CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XY));
            Assert.That(collection.Features[1].Id.GetInt32(), Is.EqualTo(2));
            Assert.That(collection.Features[1].Geometry!.Value!.CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XYZ));
            Assert.That(collection.Features[1].Geometry!.Value!.Point!.Coordinate!.Z, Is.EqualTo(5.0));
            Assert.That(collection.Features[2].Geometry, Is.Null);
            Assert.That(collection.BoundingBox, Is.EqualTo(new[] { 1.0, 2.0, 3.0, 4.0 }));
        });
    }

    [Test]
    public void ParsesEmptyFeatureCollection()
    {
        GeoJsonDocument document = ParseSuccess(
            """{"type":"FeatureCollection","features":[]}""");

        Assert.Multiple(() =>
        {
            Assert.That(document.ObjectType, Is.EqualTo(GeoJsonObjectType.FeatureCollection));
            Assert.That(document.FeatureCollection!.Features, Is.Empty);
            Assert.That(document.FeatureCollection.BoundingBox, Is.Empty);
        });
    }

    [Test]
    public void ClonesIdsPropertiesAndForeignMembersAtEveryLevel()
    {
        string json = """
            {
              "type":"FeatureCollection",
              "bbox":[170,-10,-170,10],
              "collection-extra":{"version":1},
              "features":[
                {
                  "type":"Feature",
                  "id":"a",
                  "properties":{"nested":{"enabled":true}},
                  "bbox":[175,2,175,2],
                  "feature-extra":[1,2],
                  "geometry":{
                    "type":"Point",
                    "coordinates":[175,2],
                    "geometry-extra":"kept"
                  }
                }
              ]
            }
            """;

        GeoJsonDocument document = ParseSuccess(json);
        GeoJsonFeatureCollection collection = document.FeatureCollection!;
        GeoJsonFeature feature = collection.Features[0];

        Assert.Multiple(() =>
        {
            Assert.That(document.ForeignMembers["collection-extra"].GetProperty("version").GetInt32(),
                Is.EqualTo(1));
            Assert.That(collection.ForeignMembers["collection-extra"].GetProperty("version").GetInt32(),
                Is.EqualTo(1));
            Assert.That(feature.Id.GetString(), Is.EqualTo("a"));
            Assert.That(feature.Properties.GetProperty("nested").GetProperty("enabled").GetBoolean(), Is.True);
            Assert.That(feature.ForeignMembers["feature-extra"].GetArrayLength(), Is.EqualTo(2));
            Assert.That(feature.Geometry!.ForeignMembers["geometry-extra"].GetString(), Is.EqualTo("kept"));
            Assert.That(feature.BoundingBox, Is.EqualTo(new[] { 175.0, 2.0, 175.0, 2.0 }));
        });
    }

    [Test]
    public void CanDiscardForeignMembersThroughoutAFeatureCollection()
    {
        string json = """
            {
              "type":"FeatureCollection",
              "root-extra":1,
              "features":[
                {
                  "type":"Feature",
                  "properties":{},
                  "feature-extra":2,
                  "geometry":{"type":"Point","coordinates":[1,2],"geometry-extra":3}
                }
              ]
            }
            """;
        GeoJsonDocument document = ParseSuccess(
            json, new GeoJsonReadOptions { PreserveForeignMembers = false });
        GeoJsonFeature feature = document.FeatureCollection!.Features[0];

        Assert.Multiple(() =>
        {
            Assert.That(document.ForeignMembers, Is.Empty);
            Assert.That(document.FeatureCollection.ForeignMembers, Is.Empty);
            Assert.That(feature.ForeignMembers, Is.Empty);
            Assert.That(feature.Geometry!.ForeignMembers, Is.Empty);
        });
    }

    [Test]
    public void StrictFeaturesRequireGeometryAndPropertiesWhileTolerantFeaturesMayOmitThem()
    {
        string missingGeometry = """{"type":"Feature","properties":{}}""";
        string missingProperties = """{"type":"Feature","geometry":null}""";

        GeoJsonReadResult strictGeometry = Parse(missingGeometry, new GeoJsonReadOptions());
        GeoJsonReadResult strictProperties = Parse(missingProperties, new GeoJsonReadOptions());
        GeoJsonDocument tolerantGeometry = ParseSuccess(
            missingGeometry, new GeoJsonReadOptions { Strict = false });
        GeoJsonDocument tolerantProperties = ParseSuccess(
            missingProperties, new GeoJsonReadOptions { Strict = false });

        Assert.Multiple(() =>
        {
            AssertFailure(strictGeometry, ParseErrorKind.InvalidGeoJson, "$.geometry", "requires");
            AssertFailure(strictProperties, ParseErrorKind.InvalidGeoJson, "$.properties", "requires");
            Assert.That(tolerantGeometry.Feature!.Geometry, Is.Null);
            Assert.That(tolerantProperties.Feature!.Properties.ValueKind, Is.EqualTo(JsonValueKind.Undefined));
        });
    }

    [Test]
    public void RejectsInvalidFeatureIdsAndFeatureCollectionMembers()
    {
        GeoJsonReadResult booleanId = Parse(
            """{"type":"Feature","id":true,"properties":{},"geometry":null}""",
            new GeoJsonReadOptions());
        GeoJsonReadResult nullId = Parse(
            """{"type":"Feature","id":null,"properties":{},"geometry":null}""",
            new GeoJsonReadOptions());
        GeoJsonReadResult missingFeatures = Parse(
            """{"type":"FeatureCollection"}""", new GeoJsonReadOptions());
        GeoJsonReadResult wrongFeatures = Parse(
            """{"type":"FeatureCollection","features":{}}""", new GeoJsonReadOptions());
        GeoJsonReadResult geometryMember = Parse(
            """{"type":"FeatureCollection","features":[{"type":"Point","coordinates":[1,2]}]}""",
            new GeoJsonReadOptions());
        GeoJsonReadResult nullFeature = Parse(
            """{"type":"FeatureCollection","features":[null]}""", new GeoJsonReadOptions());

        Assert.Multiple(() =>
        {
            AssertFailure(booleanId, ParseErrorKind.InvalidGeoJson, "$.id", "string or number");
            AssertFailure(nullId, ParseErrorKind.InvalidGeoJson, "$.id", "string or number");
            AssertFailure(missingFeatures, ParseErrorKind.InvalidGeoJson, "$.features", "requires");
            AssertFailure(wrongFeatures, ParseErrorKind.InvalidGeoJson, "$.features", "array");
            AssertFailure(geometryMember, ParseErrorKind.InvalidGeoJson, "$.features[0].type", "Feature");
            AssertFailure(nullFeature, ParseErrorKind.InvalidGeoJson, "$.features[0]", "object");
        });
    }

    [Test]
    public void RejectsDuplicateMembersAndStrictLegacyCrsAtContainerLevels()
    {
        GeoJsonReadResult duplicateFeature = Parse(
            """{"type":"Feature","geometry":null,"properties":{},"properties":null}""",
            new GeoJsonReadOptions());
        GeoJsonReadResult duplicateCollection = Parse(
            """{"type":"FeatureCollection","features":[],"features":[]}""",
            new GeoJsonReadOptions());
        GeoJsonReadResult featureCrs = Parse(
            """{"type":"Feature","geometry":null,"properties":{},"crs":{}}""",
            new GeoJsonReadOptions());
        GeoJsonReadResult collectionCrs = Parse(
            """{"type":"FeatureCollection","features":[],"crs":{}}""",
            new GeoJsonReadOptions());
        GeoJsonDocument tolerant = ParseSuccess(
            """{"type":"Feature","geometry":null,"properties":{},"crs":{"legacy":true}}""",
            new GeoJsonReadOptions { Strict = false });

        Assert.Multiple(() =>
        {
            AssertFailure(duplicateFeature, ParseErrorKind.InvalidGeoJson, "$.properties", "duplicated");
            AssertFailure(duplicateCollection, ParseErrorKind.InvalidGeoJson, "$.features", "duplicated");
            AssertFailure(featureCrs, ParseErrorKind.InvalidGeoJson, "$.crs", "cannot contain");
            AssertFailure(collectionCrs, ParseErrorKind.InvalidGeoJson, "$.crs", "cannot contain");
            Assert.That(tolerant.Feature!.ForeignMembers["crs"].GetProperty("legacy").GetBoolean(), Is.True);
        });
    }

    [Test]
    public void ValidatesFeatureBoundingBoxAgainstItsGeometryLayout()
    {
        GeoJsonReadResult mismatch = Parse(
            """{"type":"Feature","properties":{},"bbox":[1,2,1,2],"geometry":{"type":"Point","coordinates":[1,2,3]}}""",
            new GeoJsonReadOptions());
        GeoJsonDocument tolerant = ParseSuccess(
            """{"type":"Feature","properties":{},"bbox":[1,2,1,2],"geometry":{"type":"Point","coordinates":[1,2,3]}}""",
            new GeoJsonReadOptions { Strict = false });

        Assert.Multiple(() =>
        {
            AssertFailure(mismatch, ParseErrorKind.InvalidCoordinateLayout, "$.bbox", "dimensionality");
            Assert.That(tolerant.Feature!.BoundingBox, Has.Length.EqualTo(4));
            Assert.That(tolerant.Feature.Geometry!.Value!.CoordinateLayout, Is.EqualTo(CoordinateLayout.XYZ));
        });
    }

    [Test]
    public void EnforcesCumulativeCoordinateComponentAndObjectDepthLimits()
    {
        string collection = """
            {
              "type":"FeatureCollection",
              "features":[
                {"type":"Feature","properties":{},"geometry":{"type":"Point","coordinates":[1,2]}},
                {"type":"Feature","properties":{},"geometry":{"type":"Point","coordinates":[3,4]}}
              ]
            }
            """;

        GeoJsonReadResult coordinates = Parse(
            collection, new GeoJsonReadOptions { MaximumCoordinates = 1 });
        GeoJsonReadResult components = Parse(
            collection, new GeoJsonReadOptions { MaximumComponents = 4 });
        GeoJsonReadResult depth = Parse(
            collection, new GeoJsonReadOptions { MaximumNestingDepth = 0 });
        GeoJsonReadResult allowedDepth = Parse(
            collection, new GeoJsonReadOptions { MaximumNestingDepth = 2 });

        Assert.Multiple(() =>
        {
            AssertFailure(coordinates, ParseErrorKind.LimitExceeded,
                "$.features[1].geometry.coordinates", "coordinate count");
            AssertFailure(components, ParseErrorKind.LimitExceeded,
                "$.features[1].geometry", "component count");
            AssertFailure(depth, ParseErrorKind.LimitExceeded,
                "$.features[0]", "nesting depth");
            Assert.That(allowedDepth.Success, Is.True, allowedDepth.Error?.Message);
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

    private static void AssertFailure(
        GeoJsonReadResult result, ParseErrorKind kind, string path, string message)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Document, Is.Null);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error!.Kind, Is.EqualTo(kind));
            Assert.That(result.Error.JsonPath, Is.EqualTo(path));
            Assert.That(result.Error.Message, Does.Contain(message).IgnoreCase);
        });
    }
}
