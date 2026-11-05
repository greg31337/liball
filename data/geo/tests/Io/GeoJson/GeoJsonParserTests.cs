using geo;
using System.Text;

namespace geo.Tests;

public class GeoJsonParserTests
{
    [Test]
    public void ParsesEveryRfc7946GeometryType()
    {
        Dictionary<GeometryType, string> fixtures = new Dictionary<GeometryType, string>
        {
            [GeometryType.Point] = """{"type":"Point","coordinates":[1,2,3]}""",
            [GeometryType.LineString] = """{"type":"LineString","coordinates":[[0,0],[2,3]]}""",
            [GeometryType.Polygon] = """{"type":"Polygon","coordinates":[[[0,0],[4,0],[4,4],[0,0]],[[1,1],[1,2],[2,1],[1,1]]]}""",
            [GeometryType.MultiPoint] = """{"type":"MultiPoint","coordinates":[[1,2],[3,4]]}""",
            [GeometryType.MultiLineString] = """{"type":"MultiLineString","coordinates":[[[0,0],[1,1]],[[2,2],[3,3]]]}""",
            [GeometryType.MultiPolygon] = """{"type":"MultiPolygon","coordinates":[[[[0,0],[2,0],[0,2],[0,0]]],[]]}""",
            [GeometryType.GeometryCollection] = """{"type":"GeometryCollection","geometries":[{"type":"Point","coordinates":[1,2]},{"type":"LineString","coordinates":[[0,0],[1,1]]}]}"""
        };

        foreach (KeyValuePair<GeometryType, string> fixture in fixtures)
        {
            GeoJsonReadResult result = Parse(fixture.Value, new GeoJsonReadOptions());

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True, fixture.Key + ": " + result.Error?.Message);
                Assert.That(result.Document!.ObjectType, Is.EqualTo(GeoJsonObjectType.Geometry));
                Assert.That(result.Document.Geometry!.GeometryType, Is.EqualTo(fixture.Key));
                Assert.That(result.Document.Geometry.Value!.GeometryType, Is.EqualTo(fixture.Key));
                Assert.That(result.Document.Geometry.Value.SpatialReferenceId,
                    Is.EqualTo(GeometryConstants.Crs84SpatialReferenceId));
                Assert.That(result.Consumed, Is.EqualTo(Encoding.UTF8.GetByteCount(fixture.Value)));
            });
        }
    }

    [Test]
    public void BuildsPayloadsAndInfersAConsistentXyzLayout()
    {
        string json = """
            {
              "type":"GeometryCollection",
              "geometries":[
                {"type":"Point","coordinates":[]},
                {"type":"Point","coordinates":[1,2,3]},
                {"type":"LineString","coordinates":[[0,0,4],[5,6,7]]}
              ]
            }
            """;

        Geometry geometry = ParseSuccess(json).Value!;

        Assert.Multiple(() =>
        {
            Assert.That(geometry.CoordinateLayout, Is.EqualTo(CoordinateLayout.XYZ));
            Assert.That(geometry.GeometryCollection!.Geometries, Has.Length.EqualTo(3));
            Assert.That(geometry.GeometryCollection.Geometries[0].CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XYZ));
            Assert.That(geometry.GeometryCollection.Geometries[0].Point!.Coordinate, Is.Null);
            Assert.That(geometry.GeometryCollection.Geometries[1].Point!.Coordinate!.Z, Is.EqualTo(3.0));
            Assert.That(geometry.GeometryCollection.Geometries[2].LineString!.Coordinates[1].Z,
                Is.EqualTo(7.0));
        });
    }

    [Test]
    public void PreservesRootAndNestedForeignMembersAfterJsonDocumentDisposal()
    {
        string json = """
            {
              "type":"GeometryCollection",
              "bbox":[170,-10,-170,10],
              "title":"root",
              "geometries":[
                {
                  "type":"Point",
                  "coordinates":[175,2],
                  "bbox":[175,2,175,2],
                  "metadata":{"active":true}
                }
              ]
            }
            """;

        GeoJsonReadResult result = Parse(json, new GeoJsonReadOptions());
        GeoJsonDocument document = result.Document!;
        GeoJsonGeometry root = document.Geometry!;
        GeoJsonGeometry child = root.Geometries[0];

        Assert.Multiple(() =>
        {
            Assert.That(document.BoundingBox, Is.EqualTo(new[] { 170.0, -10.0, -170.0, 10.0 }));
            Assert.That(root.BoundingBox, Is.EqualTo(document.BoundingBox));
            Assert.That(document.ForeignMembers["title"].GetString(), Is.EqualTo("root"));
            Assert.That(root.ForeignMembers["title"].GetString(), Is.EqualTo("root"));
            Assert.That(child.BoundingBox, Is.EqualTo(new[] { 175.0, 2.0, 175.0, 2.0 }));
            Assert.That(child.ForeignMembers["metadata"].GetProperty("active").GetBoolean(), Is.True);
            Assert.That(child.Value, Is.SameAs(root.Value!.GeometryCollection!.Geometries[0]));
        });
    }

    [Test]
    public void CanDiscardForeignMembersWithoutDiscardingStandardMetadata()
    {
        GeoJsonReadOptions options = new GeoJsonReadOptions { PreserveForeignMembers = false };
        GeoJsonGeometry geometry = ParseSuccess(
            """{"type":"Point","coordinates":[1,2],"bbox":[1,2,1,2],"extra":[1,2]}""", options);

        Assert.Multiple(() =>
        {
            Assert.That(geometry.ForeignMembers, Is.Empty);
            Assert.That(geometry.BoundingBox, Is.EqualTo(new[] { 1.0, 2.0, 1.0, 2.0 }));
        });
    }

    [Test]
    public void StrictOrientationRejectsLegacyRingsWhileTolerantModeAcceptsThem()
    {
        string clockwiseShell =
            """{"type":"Polygon","coordinates":[[[0,0],[0,4],[4,0],[0,0]]]}""";

        GeoJsonReadResult strict = Parse(clockwiseShell, new GeoJsonReadOptions());
        GeoJsonReadResult tolerant = Parse(
            clockwiseShell, new GeoJsonReadOptions { Strict = false });

        Assert.Multiple(() =>
        {
            AssertFailure(strict, ParseErrorKind.InvalidGeoJson, "$.coordinates[0]", "counter-clockwise");
            Assert.That(tolerant.Success, Is.True, tolerant.Error?.Message);
            Assert.That(GeometryNormalization.GetSignedRingArea(
                tolerant.Document!.Geometry!.Value!.Polygon!.Shell.Coordinates), Is.LessThan(0.0));
        });
    }

    [Test]
    public void RejectsMalformedPositionsSequencesAndRings()
    {
        GeoJsonReadResult nonNumber = Parse(
            """{"type":"Point","coordinates":[1,"two"]}""", new GeoJsonReadOptions());
        GeoJsonReadResult tooManyOrdinates = Parse(
            """{"type":"Point","coordinates":[1,2,3,4]}""", new GeoJsonReadOptions());
        GeoJsonReadResult mixedLayout = Parse(
            """{"type":"LineString","coordinates":[[0,0],[1,1,1]]}""", new GeoJsonReadOptions());
        GeoJsonReadResult shortLine = Parse(
            """{"type":"LineString","coordinates":[[0,0]]}""", new GeoJsonReadOptions());
        GeoJsonReadResult openRing = Parse(
            """{"type":"Polygon","coordinates":[[[0,0],[4,0],[0,4],[1,1]]]}""", new GeoJsonReadOptions());
        GeoJsonReadResult tooShortRing = Parse(
            """{"type":"Polygon","coordinates":[[[0,0],[4,0],[0,0]]]}""", new GeoJsonReadOptions());
        GeoJsonReadResult infinite = Parse(
            """{"type":"Point","coordinates":[1e400,2]}""", new GeoJsonReadOptions());

        Assert.Multiple(() =>
        {
            AssertFailure(nonNumber, ParseErrorKind.InvalidNumber, "$.coordinates[1]", "number");
            AssertFailure(tooManyOrdinates, ParseErrorKind.InvalidCoordinateLayout, "$.coordinates", "two or three");
            AssertFailure(mixedLayout, ParseErrorKind.InvalidCoordinateLayout, "$.coordinates[1]", "does not match");
            AssertFailure(shortLine, ParseErrorKind.InvalidCount, "$.coordinates", "at least two");
            AssertFailure(openRing, ParseErrorKind.InvalidCoordinate, "$.coordinates[0]", "closed");
            AssertFailure(tooShortRing, ParseErrorKind.InvalidCount, "$.coordinates[0]", "four");
            AssertFailure(infinite, ParseErrorKind.InvalidNumber, "$.coordinates[0]", "finite");
        });
    }

    [Test]
    public void RejectsMissingWrongAndIllegallyNestedGeometryMembers()
    {
        GeoJsonReadResult missingType = Parse(
            """{"coordinates":[1,2]}""", new GeoJsonReadOptions());
        GeoJsonReadResult unknownType = Parse(
            """{"type":"Triangle","coordinates":[]}""", new GeoJsonReadOptions());
        GeoJsonReadResult missingCoordinates = Parse(
            """{"type":"Point"}""", new GeoJsonReadOptions());
        GeoJsonReadResult wrongCollectionMember = Parse(
            """{"type":"GeometryCollection","coordinates":[]}""", new GeoJsonReadOptions());
        GeoJsonReadResult nestedFeature = Parse(
            """{"type":"GeometryCollection","geometries":[{"type":"Feature","properties":null,"geometry":null}]}""",
            new GeoJsonReadOptions());
        GeoJsonReadResult duplicate = Parse(
            """{"type":"Point","type":"Point","coordinates":[1,2]}""", new GeoJsonReadOptions());

        Assert.Multiple(() =>
        {
            AssertFailure(missingType, ParseErrorKind.InvalidGeometryType, "$.type", "string type");
            AssertFailure(unknownType, ParseErrorKind.InvalidGeometryType, "$.type", "Triangle");
            AssertFailure(missingCoordinates, ParseErrorKind.InvalidGeoJson, "$.coordinates", "requires");
            AssertFailure(wrongCollectionMember, ParseErrorKind.InvalidGeoJson, "$.geometries", "requires");
            AssertFailure(nestedFeature, ParseErrorKind.InvalidGeometryType, "$.geometries[0].type", "Feature");
            AssertFailure(duplicate, ParseErrorKind.InvalidGeoJson, "$.type", "duplicated");
        });
    }

    [Test]
    public void ValidatesBoundingBoxesAndRfc7946CrsRules()
    {
        GeoJsonReadResult antimeridian = Parse(
            """{"type":"Point","coordinates":[175,2],"bbox":[170,-10,-170,10]}""",
            new GeoJsonReadOptions());
        GeoJsonReadResult wrongCount = Parse(
            """{"type":"Point","coordinates":[1,2],"bbox":[1,2,3]}""",
            new GeoJsonReadOptions());
        GeoJsonReadResult wrongLatitude = Parse(
            """{"type":"Point","coordinates":[1,2],"bbox":[0,10,5,-10]}""",
            new GeoJsonReadOptions());
        GeoJsonReadResult wrongDimensions = Parse(
            """{"type":"Point","coordinates":[1,2,3],"bbox":[1,2,1,2]}""",
            new GeoJsonReadOptions());
        GeoJsonReadResult strictCrs = Parse(
            """{"type":"Point","coordinates":[1,2],"crs":{"name":"legacy"}}""",
            new GeoJsonReadOptions());
        GeoJsonReadResult tolerantCrs = Parse(
            """{"type":"Point","coordinates":[1,2],"crs":{"name":"legacy"}}""",
            new GeoJsonReadOptions { Strict = false });

        Assert.Multiple(() =>
        {
            Assert.That(antimeridian.Success, Is.True, antimeridian.Error?.Message);
            Assert.That(antimeridian.Document!.BoundingBox[0], Is.GreaterThan(
                antimeridian.Document.BoundingBox[2]));
            AssertFailure(wrongCount, ParseErrorKind.InvalidCount, "$.bbox", "four or six");
            AssertFailure(wrongLatitude, ParseErrorKind.InvalidGeoJson, "$.bbox", "south");
            AssertFailure(wrongDimensions, ParseErrorKind.InvalidCoordinateLayout, "$.bbox", "dimensionality");
            AssertFailure(strictCrs, ParseErrorKind.InvalidGeoJson, "$.crs", "cannot contain");
            Assert.That(tolerantCrs.Success, Is.True, tolerantCrs.Error?.Message);
            Assert.That(tolerantCrs.Document!.ForeignMembers["crs"].GetProperty("name").GetString(),
                Is.EqualTo("legacy"));
        });
    }

    [Test]
    public void EnforcesInputCoordinateComponentAndGeometryDepthLimits()
    {
        string point = """{"type":"Point","coordinates":[1,2]}""";
        string line = """{"type":"LineString","coordinates":[[0,0],[1,1]]}""";
        string multiPoint = """{"type":"MultiPoint","coordinates":[[0,0],[1,1]]}""";
        string collection =
            """{"type":"GeometryCollection","geometries":[{"type":"Point","coordinates":[1,2]}]}""";

        GeoJsonReadResult bytes = Parse(point,
            new GeoJsonReadOptions { MaximumInputBytes = Encoding.UTF8.GetByteCount(point) - 1 });
        GeoJsonReadResult coordinates = Parse(line,
            new GeoJsonReadOptions { MaximumCoordinates = 1 });
        GeoJsonReadResult components = Parse(multiPoint,
            new GeoJsonReadOptions { MaximumComponents = 2 });
        GeoJsonReadResult depth = Parse(collection,
            new GeoJsonReadOptions { MaximumNestingDepth = 0 });

        Assert.Multiple(() =>
        {
            AssertFailure(bytes, ParseErrorKind.LimitExceeded, "$", "input size");
            AssertFailure(coordinates, ParseErrorKind.LimitExceeded, "$.coordinates", "coordinate count");
            AssertFailure(components, ParseErrorKind.LimitExceeded, "$.coordinates", "component count");
            AssertFailure(depth, ParseErrorKind.LimitExceeded, "$.geometries[0]", "nesting depth");
        });
    }

    [Test]
    public void ReturnsStructuredSyntaxAndConfigurationFailures()
    {
        GeoJsonReadResult truncated = Parse(
            """{"type":"Point","coordinates":[1,2]""", new GeoJsonReadOptions());
        GeoJsonReadResult trailing = Parse(
            """{"type":"Point","coordinates":[1,2]} false""", new GeoJsonReadOptions());
        GeoJsonReadResult nullInput = new GeoJsonParser(null!, new GeoJsonReadOptions()).ParseDocument();
        GeoJsonReadResult nullOptions = new GeoJsonParser(Encoding.UTF8.GetBytes("{}"), null!).ParseDocument();
        GeoJsonReadResult coordinateOption = Parse("{}", new GeoJsonReadOptions { MaximumCoordinates = 0 });
        GeoJsonReadResult componentOption = Parse("{}", new GeoJsonReadOptions { MaximumComponents = 0 });
        GeoJsonReadResult depthOption = Parse("{}", new GeoJsonReadOptions { MaximumNestingDepth = -1 });
        GeoJsonReadResult inputOption = Parse("{}", new GeoJsonReadOptions { MaximumInputBytes = 0 });

        Assert.Multiple(() =>
        {
            AssertFailure(truncated, ParseErrorKind.UnexpectedEnd, "$", "object");
            AssertFailure(trailing, ParseErrorKind.TrailingContent, "$", "follows");
            AssertFailure(nullInput, ParseErrorKind.UnexpectedEnd, "$", "null");
            AssertFailure(nullOptions, ParseErrorKind.LimitExceeded, "$", "options");
            AssertFailure(coordinateOption, ParseErrorKind.LimitExceeded, "$", "coordinate count");
            AssertFailure(componentOption, ParseErrorKind.LimitExceeded, "$", "component count");
            AssertFailure(depthOption, ParseErrorKind.LimitExceeded, "$", "nesting depth");
            AssertFailure(inputOption, ParseErrorKind.LimitExceeded, "$", "input size");
        });
    }

    [Test]
    public void RejectsInvalidUtf8BeforeSystemTextJsonParsing()
    {
        byte[] prefix = Encoding.UTF8.GetBytes("{\"type\":\"Point\",\"coordinates\":[1,2],\"x\":\"");
        byte[] suffix = Encoding.UTF8.GetBytes("\"}");
        byte[] source = new byte[prefix.Length + 1 + suffix.Length];
        prefix.CopyTo(source, 0);
        source[prefix.Length] = 0xff;
        suffix.CopyTo(source, prefix.Length + 1);

        GeoJsonReadResult result = new GeoJsonParser(source, new GeoJsonReadOptions()).ParseDocument();

        AssertFailure(result, ParseErrorKind.UnexpectedToken, "$", "UTF-8");
    }

    private static GeoJsonGeometry ParseSuccess(string json)
    {
        return ParseSuccess(json, new GeoJsonReadOptions());
    }

    private static GeoJsonGeometry ParseSuccess(string json, GeoJsonReadOptions options)
    {
        GeoJsonReadResult result = Parse(json, options);
        Assert.That(result.Success, Is.True, result.Error?.Message);
        return result.Document!.Geometry!;
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
