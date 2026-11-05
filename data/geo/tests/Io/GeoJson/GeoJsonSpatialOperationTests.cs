using System.Text.Json;
using geo;

namespace geo.Tests;

public class GeoJsonSpatialOperationTests
{
    [Test]
    public void BoundingBoxUsesTheSmallestWrappedLongitudeInterval()
    {
        GeoJsonDocument document = Read(
            """
            {
              "type": "FeatureCollection",
              "features": [
                {"type":"Feature","properties":{},"geometry":{"type":"Point","coordinates":[170,-10]}},
                {"type":"Feature","properties":{},"geometry":{"type":"LineString","coordinates":[[-175,5],[-170,20]]}},
                {"type":"Feature","properties":{},"geometry":null}
              ]
            }
            """);

        double[] boundingBox =
            GeoJsonBoundingBox.ComputeBoundingBox(document);

        Assert.That(boundingBox,
            Is.EqualTo(new[] { 170.0, -10.0, -170.0, 20.0 }));
    }

    [Test]
    public void BoundingBoxSupportsEveryGeometryKindAndElevation()
    {
        GeoJsonDocument document = Read(
            """
            {
              "type":"GeometryCollection",
              "geometries":[
                {"type":"Point","coordinates":[10,20,30]},
                {"type":"MultiPoint","coordinates":[[20,-5,-10],[30,2,7]]},
                {"type":"MultiLineString","coordinates":[[[40,1,4],[50,3,9]]]},
                {"type":"MultiPolygon","coordinates":[[[[60,0,2],[70,0,3],[70,5,8],[60,0,2]]]]}
              ]
            }
            """);

        double[] boundingBox =
            GeoJsonBoundingBox.ComputeBoundingBox(document);

        Assert.That(boundingBox, Is.EqualTo(new[]
        {
            10.0, -5.0, -10.0,
            70.0, 20.0, 30.0
        }));
    }

    [Test]
    public void EmptyAndHalfWorldInputsHaveDeterministicBounds()
    {
        GeoJsonDocument empty = Read(
            """{"type":"GeometryCollection","geometries":[]}""");
        GeoJsonDocument halfWorld = Read(
            """{"type":"MultiPoint","coordinates":[[-90,2],[90,8]]}""");

        Assert.Multiple(() =>
        {
            Assert.That(GeoJsonBoundingBox.ComputeBoundingBox(empty),
                Is.Empty);
            Assert.That(GeoJsonBoundingBox.ComputeBoundingBox(halfWorld),
                Is.EqualTo(new[] { -90.0, 2.0, 90.0, 8.0 }));
        });
    }

    [Test]
    public void PoleEnclosingPolygonUsesTheRfcFullLongitudeBounds()
    {
        Geometry polarCircle =
            GeographicCircleAlgorithm.CreateGeographicCircle(
                new Coordinate { X = 0.0, Y = 90.0 },
                10.0, SpatialUnit.Degrees, 8);
        GeoJsonDocument source = CreateGeometryDocument(polarCircle);

        double[] boundingBox =
            GeoJsonBoundingBox.ComputeBoundingBox(source);
        Geometry cut = AntimeridianCutter.CutAntimeridian(source)
            .Geometry!.Value!;

        Assert.Multiple(() =>
        {
            Assert.That(boundingBox[0], Is.EqualTo(-180.0));
            Assert.That(boundingBox[1], Is.EqualTo(80.0).Within(1.0e-12));
            Assert.That(boundingBox[2], Is.EqualTo(180.0));
            Assert.That(boundingBox[3], Is.EqualTo(90.0));
            Assert.That(cut.GeometryType,
                Is.EqualTo(GeometryType.MultiPolygon));
            Assert.That(HasCrossingSegment(cut), Is.False);
        });
    }

    [Test]
    public void LineCutInterpolatesCrossingPositionAndElevation()
    {
        GeoJsonDocument source = Read(
            """
            {"type":"LineString","coordinates":[[170,0,0],[-170,10,20]]}
            """);

        GeoJsonDocument cut =
            AntimeridianCutter.CutAntimeridian(source);
        Geometry value = cut.Geometry!.Value!;
        LineString[] lines = value.MultiLineString!.LineStrings;

        Assert.Multiple(() =>
        {
            Assert.That(value.GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(value.CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XYZ));
            Assert.That(lines, Has.Length.EqualTo(2));
            AssertCoordinate(lines[0].Coordinates[0],
                170.0, 0.0, 0.0);
            AssertCoordinate(lines[0].Coordinates[1],
                180.0, 5.0, 10.0);
            AssertCoordinate(lines[1].Coordinates[0],
                -180.0, 5.0, 10.0);
            AssertCoordinate(lines[1].Coordinates[1],
                -170.0, 10.0, 20.0);
            Assert.That(source.Geometry!.Value!.GeometryType,
                Is.EqualTo(GeometryType.LineString));
        });
    }

    [Test]
    public void MultiLineCutFlattensPiecesWithoutCrossingSegments()
    {
        GeoJsonDocument source = Read(
            """
            {
              "type":"MultiLineString",
              "coordinates":[
                [[-170,0],[170,10]],
                [[0,0],[10,0]],
                [[170,-10],[-170,-10],[170,-20]]
              ]
            }
            """);

        Geometry result = AntimeridianCutter.CutAntimeridian(source)
            .Geometry!.Value!;

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(result.MultiLineString!.LineStrings,
                Has.Length.EqualTo(6));
            Assert.That(HasCrossingSegment(result), Is.False);
        });
    }

    [Test]
    public void PolygonCutCreatesTwoPolygonsAndPreservesArea()
    {
        GeoJsonDocument source = Read(
            """
            {
              "type":"Polygon",
              "coordinates":[
                [[170,-10],[170,10],[-170,10],[-170,-10],[170,-10]]
              ]
            }
            """);
        GeoJsonDocument cut =
            AntimeridianCutter.CutAntimeridian(source);
        Geometry result = cut.Geometry!.Value!;

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.MultiPolygon));
            Assert.That(result.MultiPolygon!.Polygons,
                Has.Length.EqualTo(2));
            Assert.That(HasCrossingSegment(result), Is.False);
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(result),
                Is.EqualTo(400.0).Within(1.0e-12));
            Assert.That(GeoJsonBoundingBox.ComputeBoundingBox(cut),
                Is.EqualTo(new[] { 170.0, -10.0, -170.0, 10.0 }));
        });
    }

    [Test]
    public void PolygonHoleIsClippedIntoTheMatchingShellPieces()
    {
        GeoJsonDocument source = Read(
            """
            {
              "type":"Polygon",
              "coordinates":[
                [[160,-20],[160,20],[-160,20],[-160,-20],[160,-20]],
                [[170,-10],[-170,-10],[-170,10],[170,10],[170,-10]]
              ]
            }
            """);
        Geometry result = AntimeridianCutter.CutAntimeridian(source)
            .Geometry!.Value!;

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.MultiPolygon));
            Assert.That(result.MultiPolygon!.Polygons,
                Has.Length.EqualTo(2));
            Assert.That(result.MultiPolygon.Polygons[0].Holes,
                Has.Length.EqualTo(0));
            Assert.That(result.MultiPolygon.Polygons[1].Holes,
                Has.Length.EqualTo(0));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(result),
                Is.EqualTo(1200.0).Within(1.0e-12));
        });
    }

    [Test]
    public void PointsCollectionsAndFeaturesAreCutRecursively()
    {
        GeoJsonDocument source = Read(
            """
            {
              "type":"FeatureCollection",
              "bbox":[170,-20,-170,20],
              "collection-extra":{"retained":true},
              "features":[
                {
                  "type":"Feature",
                  "id":"route-a",
                  "properties":{"name":"crossing"},
                  "feature-extra":12,
                  "geometry":{
                    "type":"GeometryCollection",
                    "geometry-extra":"yes",
                    "geometries":[
                      {"type":"Point","coordinates":[190,1]},
                      {"type":"MultiPoint","coordinates":[[-190,2],[540,3]]},
                      {"type":"LineString","coordinates":[[170,0],[-170,0]]}
                    ]
                  }
                },
                {"type":"Feature","properties":null,"geometry":null}
              ]
            }
            """);

        GeoJsonDocument cut =
            AntimeridianCutter.CutAntimeridian(source);
        GeoJsonFeatureCollection collection = cut.FeatureCollection!;
        GeoJsonFeature feature = collection.Features[0];
        GeoJsonGeometry geometry = feature.Geometry!;
        GeometryCollection values = geometry.Value!.GeometryCollection!;
        MultiPoint points = values.Geometries[1].MultiPoint!;

        Assert.Multiple(() =>
        {
            Assert.That(values.Geometries[0].Point!.Coordinate!.X,
                Is.EqualTo(-170.0));
            Assert.That(points.Points[0]
                .Coordinate!.X, Is.EqualTo(170.0));
            Assert.That(points.Points[1]
                .Coordinate!.X, Is.EqualTo(-180.0));
            Assert.That(values.Geometries[2].GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(geometry.Geometries[2].GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(feature.Id.GetString(), Is.EqualTo("route-a"));
            Assert.That(feature.Properties.GetProperty("name").GetString(),
                Is.EqualTo("crossing"));
            Assert.That(feature.ForeignMembers["feature-extra"].GetInt32(),
                Is.EqualTo(12));
            Assert.That(geometry.ForeignMembers["geometry-extra"].GetString(),
                Is.EqualTo("yes"));
            Assert.That(collection.ForeignMembers["collection-extra"]
                .GetProperty("retained").GetBoolean(), Is.True);
            Assert.That(collection.BoundingBox,
                Is.EqualTo(new[] { 170.0, -20.0, -170.0, 20.0 }));
            Assert.That(collection.Features[1].Geometry, Is.Null);
            Assert.That(collection.Features[1].Properties.ValueKind,
                Is.EqualTo(JsonValueKind.Null));
            Assert.That(collection, Is.Not.SameAs(source.FeatureCollection));
        });
    }

    [Test]
    public void MultiPolygonCutRetainsItsMultiPolygonType()
    {
        GeoJsonDocument source = Read(
            """
            {
              "type":"MultiPolygon",
              "coordinates":[
                [[[170,-5],[170,5],[-170,5],[-170,-5],[170,-5]]],
                [[[0,0],[5,0],[0,5],[0,0]]]
              ]
            }
            """);

        Geometry result = AntimeridianCutter.CutAntimeridian(source)
            .Geometry!.Value!;

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.MultiPolygon));
            Assert.That(result.MultiPolygon!.Polygons,
                Has.Length.EqualTo(3));
            Assert.That(HasCrossingSegment(result), Is.False);
        });
    }

    [Test]
    public void CutResultsRemainStrictGeoJsonWritable()
    {
        GeoJsonDocument source = Read(
            """
            {
              "type":"Feature",
              "id":7,
              "properties":{"kind":"zone"},
              "geometry":{
                "type":"Polygon",
                "coordinates":[
                  [[170,-10],[170,10],[-170,10],[-170,-10],[170,-10]]
                ]
              }
            }
            """);
        IGeoJsonCodec codec = new GeoJsonCodec(
            new GeoJsonReadOptions(),
            new GeoJsonWriteOptions
            {
                IncludeBoundingBox = true,
                IncludeForeignMembers = true
            });

        GeoJsonDocument cut =
            AntimeridianCutter.CutAntimeridian(source);
        string json = codec.WriteGeoJson(cut);
        GeoJsonDocument reparsed = codec.ReadGeoJson(json);

        Assert.Multiple(() =>
        {
            Assert.That(reparsed.Feature!.Geometry!.GeometryType,
                Is.EqualTo(GeometryType.MultiPolygon));
            Assert.That(reparsed.Feature.Id.GetInt32(), Is.EqualTo(7));
            Assert.That(reparsed.Feature.Properties.GetProperty("kind")
                .GetString(), Is.EqualTo("zone"));
        });
    }

    [Test]
    public void InvalidDocumentsAndGeographicCoordinatesAreRejected()
    {
        GeoJsonDocument missing = new GeoJsonDocument();
        GeoJsonDocument mismatched = new GeoJsonDocument();
        mismatched.ObjectType = GeoJsonObjectType.Feature;
        mismatched.Geometry = Read(
            """{"type":"Point","coordinates":[0,0]}""")
            .Geometry;
        GeoJsonDocument invalidLatitude = Read(
            """{"type":"Point","coordinates":[0,0]}""");
        invalidLatitude.Geometry!.Value!.Point!.Coordinate!.Y = 91.0;

        Assert.Multiple(() =>
        {
            Assert.Throws<GeoJsonException>(() =>
                GeoJsonBoundingBox.ComputeBoundingBox(missing));
            Assert.Throws<GeoJsonException>(() =>
                AntimeridianCutter.CutAntimeridian(mismatched));
            Assert.Throws<InvalidGeometryException>(() =>
                GeoJsonBoundingBox.ComputeBoundingBox(invalidLatitude));
            Assert.Throws<InvalidGeometryException>(() =>
                AntimeridianCutter.CutAntimeridian(invalidLatitude));
        });
    }

    private static bool HasCrossingSegment(Geometry geometry)
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
                sequences[index + 1] = polygon.Holes[index].Coordinates;
                index++;
            }
            return sequences;
        }
        return [];
    }

    private static void AssertCoordinate(
        Coordinate coordinate, double x, double y, double z)
    {
        Assert.Multiple(() =>
        {
            Assert.That(coordinate.X, Is.EqualTo(x).Within(1.0e-12));
            Assert.That(coordinate.Y, Is.EqualTo(y).Within(1.0e-12));
            Assert.That(coordinate.Z, Is.EqualTo(z).Within(1.0e-12));
        });
    }

    private static GeoJsonDocument Read(string text)
    {
        IGeoJsonCodec codec = new GeoJsonCodec(
            new GeoJsonReadOptions(), new GeoJsonWriteOptions());
        return codec.ReadGeoJson(text);
    }

    private static GeoJsonDocument CreateGeometryDocument(
        Geometry value)
    {
        GeoJsonGeometry geometry = new GeoJsonGeometry();
        geometry.GeometryType = value.GeometryType;
        geometry.Value = value;
        GeoJsonDocument document = new GeoJsonDocument();
        document.ObjectType = GeoJsonObjectType.Geometry;
        document.Geometry = geometry;
        return document;
    }
}
