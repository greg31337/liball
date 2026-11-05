using System.Text.Json;
using geo;

namespace geo.Tests;

public class GeoJsonFeatureOperationTests
{
    [Test]
    public void RewindUsesRightHandRuleAndPreservesFeatureMetadata()
    {
        GeoJsonDocument source = ReadTolerant(
            """
            {
              "type":"Feature",
              "id":"zone-a",
              "properties":{"name":"park"},
              "feature-extra":5,
              "geometry":{
                "type":"Polygon",
                "geometry-extra":"retained",
                "coordinates":[
                  [[0,0],[0,4],[4,4],[4,0],[0,0]],
                  [[1,1],[3,1],[3,3],[1,3],[1,1]]
                ]
              }
            }
            """);
        Polygon sourcePolygon =
            source.Feature!.Geometry!.Value!.Polygon!;

        GeoJsonDocument rewound =
            GeoJsonObjectTransformer.RewindDocument(source);
        GeoJsonFeature feature = rewound.Feature!;
        Polygon polygon = feature.Geometry!.Value!.Polygon!;

        Assert.Multiple(() =>
        {
            Assert.That(RingMath.GetSignedArea(polygon.Shell.Coordinates),
                Is.GreaterThan(0.0));
            Assert.That(RingMath.GetSignedArea(
                polygon.Holes[0].Coordinates), Is.LessThan(0.0));
            Assert.That(RingMath.GetSignedArea(
                sourcePolygon.Shell.Coordinates), Is.LessThan(0.0));
            Assert.That(feature.Id.GetString(), Is.EqualTo("zone-a"));
            Assert.That(feature.Properties.GetProperty("name").GetString(),
                Is.EqualTo("park"));
            Assert.That(feature.ForeignMembers["feature-extra"].GetInt32(),
                Is.EqualTo(5));
            Assert.That(feature.Geometry.ForeignMembers["geometry-extra"]
                .GetString(), Is.EqualTo("retained"));
        });
    }

    [Test]
    public void TruncationRoundsXyAndElevationWithoutMutatingSource()
    {
        GeoJsonDocument source = Read(
            """
            {
              "type":"Feature",
              "id":3,
              "properties":{"precision":"fine"},
              "geometry":{
                "type":"LineString",
                "coordinates":[[1.23456,-2.34567,9.87654],[3.45678,4.56789,-1.23456]]
              }
            }
            """);

        GeoJsonDocument truncated =
            GeoJsonObjectTransformer.TruncateDocumentCoordinates(
                source, 3);
        Coordinate[] coordinates = truncated.Feature!.Geometry!
            .Value!.LineString!.Coordinates;
        Coordinate sourceCoordinate = source.Feature!.Geometry!
            .Value!.LineString!.Coordinates[0];

        Assert.Multiple(() =>
        {
            AssertCoordinate(coordinates[0],
                1.235, -2.346, 9.877, 0.0);
            AssertCoordinate(coordinates[1],
                3.457, 4.568, -1.235, 0.0);
            Assert.That(sourceCoordinate.X, Is.EqualTo(1.23456));
            Assert.That(truncated.Feature.Id.GetInt32(), Is.EqualTo(3));
            Assert.That(truncated.Feature.Properties
                .GetProperty("precision").GetString(), Is.EqualTo("fine"));
        });
    }

    [Test]
    public void BoundingBoxAssignmentUpdatesEveryObjectLevel()
    {
        GeoJsonDocument source = Read(
            """
            {
              "type":"FeatureCollection",
              "features":[
                {
                  "type":"Feature",
                  "properties":{"name":"route"},
                  "geometry":{
                    "type":"GeometryCollection",
                    "geometries":[
                      {"type":"Point","coordinates":[170,-5]},
                      {"type":"LineString","coordinates":[[-175,2],[-170,8]]}
                    ]
                  }
                },
                {"type":"Feature","properties":null,"geometry":null}
              ]
            }
            """);

        GeoJsonDocument assigned =
            GeoJsonObjectTransformer.AssignBoundingBoxes(source);
        GeoJsonFeatureCollection collection =
            assigned.FeatureCollection!;
        GeoJsonFeature feature = collection.Features[0];
        GeoJsonGeometry geometry = feature.Geometry!;

        Assert.Multiple(() =>
        {
            Assert.That(assigned.BoundingBox,
                Is.EqualTo(new[] { 170.0, -5.0, -170.0, 8.0 }));
            Assert.That(collection.BoundingBox,
                Is.EqualTo(assigned.BoundingBox));
            Assert.That(feature.BoundingBox,
                Is.EqualTo(assigned.BoundingBox));
            Assert.That(geometry.BoundingBox,
                Is.EqualTo(assigned.BoundingBox));
            Assert.That(geometry.Geometries[0].BoundingBox,
                Is.EqualTo(new[] { 170.0, -5.0, 170.0, -5.0 }));
            Assert.That(geometry.Geometries[1].BoundingBox,
                Is.EqualTo(new[] { -175.0, 2.0, -170.0, 8.0 }));
            Assert.That(collection.Features[1].BoundingBox, Is.Empty);
            Assert.That(source.BoundingBox, Is.Empty);
        });
    }

    [Test]
    public void FlattenExpandsMultisAndCollectionsIncludingNullGeometry()
    {
        GeoJsonDocument source = Read(
            """
            {
              "type":"FeatureCollection",
              "collection-extra":true,
              "features":[
                {
                  "type":"Feature",
                  "id":"mixed",
                  "properties":{"value":7},
                  "feature-extra":"kept",
                  "geometry":{
                    "type":"GeometryCollection",
                    "parent-extra":"also-kept",
                    "geometries":[
                      {"type":"MultiPoint","coordinates":[[0,0],[1,1]],"point-extra":1},
                      {"type":"LineString","coordinates":[[0,0],[2,0]],"line-extra":2},
                      {"type":"Polygon","coordinates":[[[0,0],[2,0],[0,2],[0,0]]]}
                    ]
                  }
                },
                {"type":"Feature","id":"empty","properties":null,"geometry":null}
              ]
            }
            """);

        GeoJsonDocument flattened =
            GeoJsonFeatureOperations.FlattenFeatures(source);
        GeoJsonFeature[] features =
            flattened.FeatureCollection!.Features;

        Assert.Multiple(() =>
        {
            Assert.That(features, Has.Length.EqualTo(5));
            Assert.That(features[0].Geometry!.GeometryType,
                Is.EqualTo(GeometryType.Point));
            Assert.That(features[1].Geometry!.GeometryType,
                Is.EqualTo(GeometryType.Point));
            Assert.That(features[2].Geometry!.GeometryType,
                Is.EqualTo(GeometryType.LineString));
            Assert.That(features[3].Geometry!.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(features[4].Geometry, Is.Null);
            Assert.That(features[0].Id.GetString(), Is.EqualTo("mixed"));
            Assert.That(features[3].Properties.GetProperty("value").GetInt32(),
                Is.EqualTo(7));
            Assert.That(features[2].ForeignMembers["feature-extra"].GetString(),
                Is.EqualTo("kept"));
            Assert.That(features[0].Geometry!.ForeignMembers["point-extra"]
                .GetInt32(), Is.EqualTo(1));
            Assert.That(features[2].Geometry!.ForeignMembers["line-extra"]
                .GetInt32(), Is.EqualTo(2));
            Assert.That(features[3].Geometry!.ForeignMembers["parent-extra"]
                .GetString(), Is.EqualTo("also-kept"));
            Assert.That(flattened.FeatureCollection.ForeignMembers[
                "collection-extra"].GetBoolean(), Is.True);
        });
    }

    [Test]
    public void ExplodeCreatesPointFeaturesForEveryPosition()
    {
        GeoJsonDocument source = Read(
            """
            {
              "type":"Feature",
              "id":"triangle",
              "properties":{"name":"a"},
              "feature-extra":9,
              "geometry":{
                "type":"Polygon",
                "coordinates":[[[0,0],[2,0],[0,2],[0,0]]]
              }
            }
            """);

        GeoJsonDocument exploded =
            GeoJsonFeatureOperations.ExplodeFeatures(source);
        GeoJsonFeature[] features =
            exploded.FeatureCollection!.Features;

        Assert.Multiple(() =>
        {
            Assert.That(features, Has.Length.EqualTo(4));
            Assert.That(features[0].Geometry!.Value!.Point!.Coordinate!.X,
                Is.EqualTo(0.0));
            Assert.That(features[1].Geometry!.Value!.Point!.Coordinate!.X,
                Is.EqualTo(2.0));
            Assert.That(features[3].Geometry!.Value!.Point!.Coordinate!.X,
                Is.EqualTo(0.0));
            Assert.That(features[2].Id.GetString(), Is.EqualTo("triangle"));
            Assert.That(features[2].Properties.GetProperty("name").GetString(),
                Is.EqualTo("a"));
            Assert.That(features[2].ForeignMembers["feature-extra"].GetInt32(),
                Is.EqualTo(9));
        });
    }

    [Test]
    public void CombineGroupsAtomicFeaturesAndCollectsProperties()
    {
        GeoJsonDocument source = Read(
            """
            {
              "type":"FeatureCollection",
              "collection-extra":"kept",
              "features":[
                {"type":"Feature","id":"p1","properties":{"n":1},"shared":true,"geometry":{"type":"Point","coordinates":[0,0]}},
                {"type":"Feature","id":"p2","properties":{"n":2},"shared":true,"geometry":{"type":"MultiPoint","coordinates":[[1,1],[2,2]]}},
                {"type":"Feature","id":"l1","properties":{"n":3},"geometry":{"type":"LineString","coordinates":[[0,0],[2,0]]}},
                {"type":"Feature","id":"a1","properties":{"n":4},"geometry":{"type":"Polygon","coordinates":[[[0,0],[1,0],[0,1],[0,0]]]}},
                {"type":"Feature","id":"null","properties":null,"geometry":null}
              ]
            }
            """);

        GeoJsonDocument combined =
            GeoJsonFeatureOperations.CombineFeatures(source);
        GeoJsonFeature[] features =
            combined.FeatureCollection!.Features;

        Assert.Multiple(() =>
        {
            Assert.That(features, Has.Length.EqualTo(4));
            Assert.That(features[0].Geometry!.GeometryType,
                Is.EqualTo(GeometryType.MultiPoint));
            Assert.That(features[0].Geometry!.Value!.MultiPoint!.Points,
                Has.Length.EqualTo(3));
            Assert.That(features[0].Properties
                .GetProperty("collectedProperties").GetArrayLength(),
                Is.EqualTo(3));
            Assert.That(features[0].ForeignMembers["shared"].GetBoolean(),
                Is.True);
            Assert.That(features[1].Geometry!.GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(features[1].Id.GetString(), Is.EqualTo("l1"));
            Assert.That(features[2].Geometry!.GeometryType,
                Is.EqualTo(GeometryType.MultiPolygon));
            Assert.That(features[3].Geometry, Is.Null);
            Assert.That(features[3].Id.GetString(), Is.EqualTo("null"));
            Assert.That(combined.FeatureCollection.ForeignMembers[
                "collection-extra"].GetString(), Is.EqualTo("kept"));
        });
    }

    [Test]
    public void PointsWithinPolygonsIncludesBoundariesAndDatelinePoints()
    {
        GeoJsonDocument points = Read(
            """
            {
              "type":"FeatureCollection",
              "features":[
                {"type":"Feature","id":"east","properties":{"side":1},"geometry":{"type":"Point","coordinates":[179,0]}},
                {"type":"Feature","id":"west","properties":{"side":2},"geometry":{"type":"Point","coordinates":[-179,0]}},
                {"type":"Feature","id":"edge","properties":{"side":3},"geometry":{"type":"Point","coordinates":[170,0]}},
                {"type":"Feature","id":"outside","properties":{"side":4},"geometry":{"type":"Point","coordinates":[160,0]}}
              ]
            }
            """);
        GeoJsonDocument polygons = Read(
            """
            {
              "type":"Polygon",
              "coordinates":[[[170,-10],[170,10],[-170,10],[-170,-10],[170,-10]]]
            }
            """);

        GeoJsonDocument selected =
            GeoJsonFeatureOperations.PointsWithinPolygons(
                points, polygons);
        GeoJsonFeature[] features =
            selected.FeatureCollection!.Features;

        Assert.Multiple(() =>
        {
            Assert.That(features, Has.Length.EqualTo(3));
            Assert.That(features[0].Id.GetString(), Is.EqualTo("east"));
            Assert.That(features[1].Id.GetString(), Is.EqualTo("west"));
            Assert.That(features[2].Id.GetString(), Is.EqualTo("edge"));
            Assert.That(features[1].Properties.GetProperty("side").GetInt32(),
                Is.EqualTo(2));
        });
    }

    [Test]
    public void IntersectingJoinSelectsWholeFeaturesAcrossDateline()
    {
        GeoJsonDocument first = Read(
            """
            {
              "type":"FeatureCollection",
              "features":[
                {"type":"Feature","id":"crossing","properties":{"hit":true},"route-extra":1,"geometry":{"type":"LineString","coordinates":[[160,0],[-175,0]]}},
                {"type":"Feature","id":"far","properties":{"hit":false},"geometry":{"type":"LineString","coordinates":[[0,0],[10,0]]}},
                {"type":"Feature","id":"empty","properties":null,"geometry":null}
              ]
            }
            """);
        GeoJsonDocument second = Read(
            """
            {
              "type":"Polygon",
              "coordinates":[[[170,-5],[170,5],[-170,5],[-170,-5],[170,-5]]]
            }
            """);

        GeoJsonDocument selected =
            GeoJsonFeatureOperations.IntersectingFeatures(
                first, second);
        GeoJsonFeature[] features =
            selected.FeatureCollection!.Features;

        Assert.Multiple(() =>
        {
            Assert.That(features, Has.Length.EqualTo(1));
            Assert.That(features[0].Id.GetString(),
                Is.EqualTo("crossing"));
            Assert.That(features[0].Geometry!.GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(features[0].Properties.GetProperty("hit").GetBoolean(),
                Is.True);
            Assert.That(features[0].ForeignMembers["route-extra"].GetInt32(),
                Is.EqualTo(1));
        });
    }

    [Test]
    public void InvalidPrecisionCollapseJoinTypesAndCombineLayoutsFail()
    {
        GeoJsonDocument triangle = Read(
            """{"type":"Polygon","coordinates":[[[0.1,0.1],[0.2,0.1],[0.1,0.2],[0.1,0.1]]]}""");
        GeoJsonDocument point = Read(
            """{"type":"Point","coordinates":[0,0]}""");
        GeoJsonDocument line = Read(
            """{"type":"LineString","coordinates":[[0,0],[1,1]]}""");
        GeoJsonDocument mixed = Read(
            """
            {
              "type":"FeatureCollection",
              "features":[
                {"type":"Feature","properties":{},"geometry":{"type":"Point","coordinates":[0,0]}},
                {"type":"Feature","properties":{},"geometry":{"type":"Point","coordinates":[1,1,1]}}
              ]
            }
            """);

        Assert.Multiple(() =>
        {
            Assert.Throws<GeoJsonException>(() =>
                GeoJsonObjectTransformer.TruncateDocumentCoordinates(
                    triangle, -1));
            Assert.Throws<InvalidGeometryException>(() =>
                GeoJsonObjectTransformer.TruncateDocumentCoordinates(
                    triangle, 0));
            Assert.Throws<GeoJsonException>(() =>
                GeoJsonFeatureOperations.PointsWithinPolygons(
                    line, point));
            Assert.Throws<GeoJsonException>(() =>
                GeoJsonFeatureOperations.CombineFeatures(mixed));
        });
    }

    [Test]
    public void FeatureOperationResultsRemainStrictGeoJsonWritable()
    {
        GeoJsonDocument source = Read(
            """
            {
              "type":"FeatureCollection",
              "features":[
                {"type":"Feature","properties":{"n":1},"geometry":{"type":"MultiPoint","coordinates":[[0,0],[1,1]]}},
                {"type":"Feature","properties":{"n":2},"geometry":{"type":"LineString","coordinates":[[0,0],[2,0]]}}
              ]
            }
            """);
        IGeoJsonCodec codec = new GeoJsonCodec(
            new GeoJsonReadOptions(), new GeoJsonWriteOptions());
        GeoJsonDocument flattened =
            GeoJsonFeatureOperations.FlattenFeatures(source);
        GeoJsonDocument combined =
            GeoJsonFeatureOperations.CombineFeatures(source);
        GeoJsonDocument assigned =
            GeoJsonObjectTransformer.AssignBoundingBoxes(combined);

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() =>
                codec.ReadGeoJson(codec.WriteGeoJson(flattened)));
            Assert.DoesNotThrow(() =>
                codec.ReadGeoJson(codec.WriteGeoJson(combined)));
            Assert.DoesNotThrow(() =>
                codec.ReadGeoJson(codec.WriteGeoJson(assigned)));
        });
    }

    private static void AssertCoordinate(
        Coordinate coordinate, double x, double y,
        double z, double tolerance)
    {
        Assert.Multiple(() =>
        {
            Assert.That(coordinate.X, Is.EqualTo(x).Within(tolerance));
            Assert.That(coordinate.Y, Is.EqualTo(y).Within(tolerance));
            Assert.That(coordinate.Z, Is.EqualTo(z).Within(tolerance));
        });
    }

    private static GeoJsonDocument Read(string text)
    {
        IGeoJsonCodec codec = new GeoJsonCodec(
            new GeoJsonReadOptions(), new GeoJsonWriteOptions());
        return codec.ReadGeoJson(text);
    }

    private static GeoJsonDocument ReadTolerant(string text)
    {
        IGeoJsonCodec codec = new GeoJsonCodec(
            new GeoJsonReadOptions { Strict = false },
            new GeoJsonWriteOptions());
        return codec.ReadGeoJson(text);
    }
}
