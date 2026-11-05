using System.Text.Json;
using geo;

namespace geo.Tests;

public class GeoJsonFeatureOperationsConformanceTests
{
    [Test]
    public void WrappedBoundingBoxAndCutPreserveTheCompleteFeatureObject()
    {
        IGeoJsonOperations operations = new GeoJsonOperations();
        GeoJsonDocument source = Read(
            """
            {
              "type":"FeatureCollection",
              "bbox":[170,-10,-170,10],
              "collection-extra":{"version":2},
              "features":[
                {
                  "type":"Feature",
                  "id":17,
                  "properties":{"name":"dateline-zone","tags":["a","b"]},
                  "feature-extra":true,
                  "geometry":{
                    "type":"Polygon",
                    "geometry-extra":"retained",
                    "coordinates":[[[170,-10],[170,10],[-170,10],[-170,-10],[170,-10]]]
                  }
                },
                {"type":"Feature","id":"empty","properties":null,"geometry":null}
              ]
            }
            """);

        double[] boundingBox =
            operations.ComputeGeoJsonBoundingBox(source);
        GeoJsonDocument cut = operations.CutAntimeridian(source);
        GeoJsonFeatureCollection collection = cut.FeatureCollection!;
        GeoJsonFeature feature = collection.Features[0];

        Assert.Multiple(() =>
        {
            Assert.That(boundingBox,
                Is.EqualTo(new[] { 170.0, -10.0, -170.0, 10.0 }));
            Assert.That(feature.Geometry!.GeometryType,
                Is.EqualTo(GeometryType.MultiPolygon));
            Assert.That(feature.Geometry.Value!.MultiPolygon!.Polygons,
                Has.Length.EqualTo(2));
            Assert.That(HasAntimeridianCrossing(
                feature.Geometry.Value), Is.False);
            Assert.That(feature.Id.GetInt32(), Is.EqualTo(17));
            Assert.That(feature.Properties.GetProperty("name").GetString(),
                Is.EqualTo("dateline-zone"));
            Assert.That(feature.Properties.GetProperty("tags")
                .GetArrayLength(), Is.EqualTo(2));
            Assert.That(feature.ForeignMembers["feature-extra"]
                .GetBoolean(), Is.True);
            Assert.That(feature.Geometry.ForeignMembers["geometry-extra"]
                .GetString(), Is.EqualTo("retained"));
            Assert.That(collection.ForeignMembers["collection-extra"]
                .GetProperty("version").GetInt32(), Is.EqualTo(2));
            Assert.That(collection.Features[1].Geometry, Is.Null);
            Assert.That(collection.Features[1].Properties.ValueKind,
                Is.EqualTo(JsonValueKind.Null));
            Assert.That(source.FeatureCollection!.Features[0]
                .Geometry!.GeometryType, Is.EqualTo(GeometryType.Polygon));
        });
        AssertStrictRoundTrip(cut);
    }

    [Test]
    public void RewindAndTruncateApplyRfcWindingAndPreserveMetadata()
    {
        IGeoJsonOperations operations = new GeoJsonOperations();
        GeoJsonDocument source = ReadTolerant(
            """
            {
              "type":"Feature",
              "id":"precision-area",
              "properties":{"nested":{"enabled":true}},
              "feature-extra":9,
              "geometry":{
                "type":"Polygon",
                "geometry-extra":{"source":"survey"},
                "coordinates":[
                  [[0.004,0.004],[0.004,4.006],[4.006,4.006],[4.006,0.004],[0.004,0.004]],
                  [[1.004,1.004],[3.006,1.004],[3.006,3.006],[1.004,3.006],[1.004,1.004]]
                ]
              }
            }
            """);

        GeoJsonDocument rewound = operations.RewindGeoJson(source);
        GeoJsonDocument truncated =
            operations.TruncateGeoJsonCoordinates(rewound, 2);
        GeoJsonFeature feature = truncated.Feature!;
        Polygon polygon = feature.Geometry!.Value!.Polygon!;

        Assert.Multiple(() =>
        {
            Assert.That(RingMath.GetSignedArea(
                polygon.Shell.Coordinates), Is.GreaterThan(0.0));
            Assert.That(RingMath.GetSignedArea(
                polygon.Holes[0].Coordinates), Is.LessThan(0.0));
            Assert.That(polygon.Shell.Coordinates[2].X,
                Is.EqualTo(4.01));
            Assert.That(polygon.Shell.Coordinates[2].Y,
                Is.EqualTo(4.01));
            Assert.That(polygon.Holes[0].Coordinates[1].X,
                Is.EqualTo(1.0));
            Assert.That(polygon.Holes[0].Coordinates[1].Y,
                Is.EqualTo(3.01));
            Assert.That(feature.Id.GetString(),
                Is.EqualTo("precision-area"));
            Assert.That(feature.Properties.GetProperty("nested")
                .GetProperty("enabled").GetBoolean(), Is.True);
            Assert.That(feature.ForeignMembers["feature-extra"].GetInt32(),
                Is.EqualTo(9));
            Assert.That(feature.Geometry.ForeignMembers["geometry-extra"]
                .GetProperty("source").GetString(),
                Is.EqualTo("survey"));
            Assert.That(source.Feature!.Geometry!.Value!.Polygon!
                .Shell.Coordinates[2].X, Is.EqualTo(4.006));
        });
        AssertStrictRoundTrip(truncated);
    }

    [Test]
    public void FlattenExplodeAndCombineCoverEverySimpleFamilyAndNulls()
    {
        IGeoJsonOperations operations = new GeoJsonOperations();
        GeoJsonDocument source = Read(
            """
            {
              "type":"FeatureCollection",
              "collection-extra":"kept",
              "features":[
                {
                  "type":"Feature",
                  "id":"mixed",
                  "properties":{"category":"all"},
                  "feature-extra":5,
                  "geometry":{
                    "type":"GeometryCollection",
                    "parent-extra":"parent",
                    "geometries":[
                      {"type":"MultiPoint","multi-extra":"points","coordinates":[[0,0],[1,1]]},
                      {"type":"MultiLineString","multi-extra":"lines","coordinates":[[[0,0],[1,0]],[[2,0],[3,0]]]},
                      {"type":"MultiPolygon","multi-extra":"areas","coordinates":[[[[0,0],[1,0],[0,1],[0,0]]],[[[2,0],[3,0],[2,1],[2,0]]]]}
                    ]
                  }
                },
                {"type":"Feature","id":"nil","properties":null,"geometry":null}
              ]
            }
            """);

        GeoJsonDocument flattened = operations.FlattenFeatures(source);
        GeoJsonDocument exploded = operations.ExplodeFeatures(source);
        GeoJsonDocument combined = operations.CombineFeatures(source);
        GeoJsonFeature[] flatFeatures =
            flattened.FeatureCollection!.Features;
        GeoJsonFeature[] combinedFeatures =
            combined.FeatureCollection!.Features;

        Assert.Multiple(() =>
        {
            Assert.That(flatFeatures, Has.Length.EqualTo(7));
            Assert.That(flatFeatures[0].Geometry!.GeometryType,
                Is.EqualTo(GeometryType.Point));
            Assert.That(flatFeatures[2].Geometry!.GeometryType,
                Is.EqualTo(GeometryType.LineString));
            Assert.That(flatFeatures[4].Geometry!.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(flatFeatures[6].Geometry, Is.Null);
            Assert.That(flatFeatures[0].Geometry!.ForeignMembers[
                "multi-extra"].GetString(), Is.EqualTo("points"));
            Assert.That(flatFeatures[0].Geometry!.ForeignMembers[
                "parent-extra"].GetString(), Is.EqualTo("parent"));
            Assert.That(flatFeatures[5].Id.GetString(),
                Is.EqualTo("mixed"));
            Assert.That(flatFeatures[5].Properties
                .GetProperty("category").GetString(), Is.EqualTo("all"));
            Assert.That(exploded.FeatureCollection!.Features,
                Has.Length.EqualTo(14));
            Assert.That(AllPointFeatures(
                exploded.FeatureCollection.Features), Is.True);
            Assert.That(combinedFeatures, Has.Length.EqualTo(4));
            Assert.That(combinedFeatures[0].Geometry!.GeometryType,
                Is.EqualTo(GeometryType.MultiPoint));
            Assert.That(combinedFeatures[0].Geometry!.Value!
                .MultiPoint!.Points, Has.Length.EqualTo(2));
            Assert.That(combinedFeatures[1].Geometry!.Value!
                .MultiLineString!.LineStrings, Has.Length.EqualTo(2));
            Assert.That(combinedFeatures[2].Geometry!.Value!
                .MultiPolygon!.Polygons, Has.Length.EqualTo(2));
            Assert.That(combinedFeatures[0].Properties
                .GetProperty("collectedProperties").GetArrayLength(),
                Is.EqualTo(2));
            Assert.That(combinedFeatures[3].Geometry, Is.Null);
            Assert.That(combinedFeatures[3].Id.GetString(),
                Is.EqualTo("nil"));
            Assert.That(combined.FeatureCollection.ForeignMembers[
                "collection-extra"].GetString(), Is.EqualTo("kept"));
        });
        AssertStrictRoundTrip(flattened);
        AssertStrictRoundTrip(exploded);
        AssertStrictRoundTrip(combined);
    }

    [Test]
    public void SpatialJoinsHandleDatelineBoundariesNullsAndMetadata()
    {
        IGeoJsonOperations operations = new GeoJsonOperations();
        GeoJsonDocument points = Read(
            """
            {
              "type":"FeatureCollection",
              "features":[
                {"type":"Feature","id":"east","properties":{"rank":1},"point-extra":"e","geometry":{"type":"Point","coordinates":[179,0]}},
                {"type":"Feature","id":"west","properties":{"rank":2},"point-extra":"w","geometry":{"type":"Point","coordinates":[-179,0]}},
                {"type":"Feature","id":"edge","properties":{"rank":3},"geometry":{"type":"Point","coordinates":[170,0]}},
                {"type":"Feature","id":"outside","properties":{"rank":4},"geometry":{"type":"Point","coordinates":[160,0]}},
                {"type":"Feature","id":"nil","properties":null,"geometry":null}
              ]
            }
            """);
        GeoJsonDocument polygon = Read(
            """
            {"type":"Polygon","coordinates":[[[170,-10],[170,10],[-170,10],[-170,-10],[170,-10]]]}
            """);
        GeoJsonDocument lines = Read(
            """
            {
              "type":"FeatureCollection",
              "features":[
                {"type":"Feature","id":"crossing","properties":{"hit":true},"line-extra":7,"geometry":{"type":"LineString","coordinates":[[160,0],[-175,0]]}},
                {"type":"Feature","id":"far","properties":{"hit":false},"geometry":{"type":"LineString","coordinates":[[0,0],[10,0]]}},
                {"type":"Feature","id":"nil","properties":null,"geometry":null}
              ]
            }
            """);

        GeoJsonDocument within =
            operations.PointsWithinPolygons(points, polygon);
        GeoJsonDocument intersecting =
            operations.IntersectingFeatures(lines, polygon);
        GeoJsonFeature[] pointResults =
            within.FeatureCollection!.Features;
        GeoJsonFeature[] lineResults =
            intersecting.FeatureCollection!.Features;

        Assert.Multiple(() =>
        {
            Assert.That(pointResults, Has.Length.EqualTo(3));
            Assert.That(pointResults[0].Id.GetString(), Is.EqualTo("east"));
            Assert.That(pointResults[1].Id.GetString(), Is.EqualTo("west"));
            Assert.That(pointResults[2].Id.GetString(), Is.EqualTo("edge"));
            Assert.That(pointResults[1].Properties.GetProperty("rank")
                .GetInt32(), Is.EqualTo(2));
            Assert.That(pointResults[0].ForeignMembers["point-extra"]
                .GetString(), Is.EqualTo("e"));
            Assert.That(lineResults, Has.Length.EqualTo(1));
            Assert.That(lineResults[0].Id.GetString(),
                Is.EqualTo("crossing"));
            Assert.That(lineResults[0].Geometry!.GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(lineResults[0].Properties.GetProperty("hit")
                .GetBoolean(), Is.True);
            Assert.That(lineResults[0].ForeignMembers["line-extra"]
                .GetInt32(), Is.EqualTo(7));
        });
        AssertStrictRoundTrip(within);
        AssertStrictRoundTrip(intersecting);
    }

    private static bool AllPointFeatures(GeoJsonFeature[] features)
    {
        int index = 0;
        while (index < features.Length)
        {
            if (features[index].Geometry == null ||
                features[index].Geometry!.GeometryType !=
                    GeometryType.Point)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    private static bool HasAntimeridianCrossing(Geometry geometry)
    {
        Geometry[] leaves = ComponentWalker.FlattenLeafGeometries(
            geometry,
            GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        int leafIndex = 0;
        while (leafIndex < leaves.Length)
        {
            Coordinate[][] sequences = GetSequences(leaves[leafIndex]);
            int sequenceIndex = 0;
            while (sequenceIndex < sequences.Length)
            {
                Coordinate[] coordinates = sequences[sequenceIndex];
                int coordinateIndex = 0;
                while (coordinateIndex + 1 < coordinates.Length)
                {
                    if (Math.Abs(coordinates[coordinateIndex + 1].X -
                        coordinates[coordinateIndex].X) > 180.0)
                    {
                        return true;
                    }
                    coordinateIndex++;
                }
                sequenceIndex++;
            }
            leafIndex++;
        }
        return false;
    }

    private static Coordinate[][] GetSequences(Geometry geometry)
    {
        if (geometry.GeometryType == GeometryType.LineString)
        {
            return [geometry.LineString!.Coordinates];
        }
        if (geometry.GeometryType == GeometryType.Polygon)
        {
            Polygon polygon = geometry.Polygon!;
            Coordinate[][] sequences =
                new Coordinate[polygon.Holes.Length + 1][];
            sequences[0] = polygon.Shell.Coordinates;
            int index = 0;
            while (index < polygon.Holes.Length)
            {
                sequences[index + 1] =
                    polygon.Holes[index].Coordinates;
                index++;
            }
            return sequences;
        }
        return [];
    }

    private static void AssertStrictRoundTrip(
        GeoJsonDocument document)
    {
        IGeoJsonCodec codec = CreateCodec(true);
        string json = codec.WriteGeoJson(document);
        GeoJsonDocument reparsed = codec.ReadGeoJson(json);
        Assert.That(reparsed.ObjectType,
            Is.EqualTo(document.ObjectType));
    }

    private static GeoJsonDocument Read(string text)
    {
        return CreateCodec(true).ReadGeoJson(text);
    }

    private static GeoJsonDocument ReadTolerant(string text)
    {
        return CreateCodec(false).ReadGeoJson(text);
    }

    private static IGeoJsonCodec CreateCodec(bool strict)
    {
        GeoJsonReadOptions readOptions = new GeoJsonReadOptions();
        readOptions.Strict = strict;
        GeoJsonWriteOptions writeOptions = new GeoJsonWriteOptions();
        writeOptions.Strict = true;
        writeOptions.IncludeBoundingBox = true;
        writeOptions.IncludeForeignMembers = true;
        return new GeoJsonCodec(readOptions, writeOptions);
    }
}
