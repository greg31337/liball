using geo;

namespace geo.Tests;

public class GeoJsonOperationsTests
{
    [Test]
    public void BoundingBoxAndAntimeridianCutAreAvailableThroughContract()
    {
        IGeoJsonOperations operations = new GeoJsonOperations();
        GeoJsonDocument source = Read(
            """
            {"type":"LineString","coordinates":[[170,-5],[-170,8]]}
            """);

        double[] boundingBox =
            operations.ComputeGeoJsonBoundingBox(source);
        GeoJsonDocument cut = operations.CutAntimeridian(source);

        Assert.Multiple(() =>
        {
            Assert.That(boundingBox,
                Is.EqualTo(new[] { 170.0, -5.0, -170.0, 8.0 }));
            Assert.That(cut.Geometry!.GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(cut.Geometry.Value!.MultiLineString!.LineStrings,
                Has.Length.EqualTo(2));
            Assert.That(Write(cut), Does.Contain("MultiLineString"));
            Assert.That(source.Geometry!.GeometryType,
                Is.EqualTo(GeometryType.LineString));
        });
    }

    [Test]
    public void RewindAndTruncateReturnOwningStrictGeoJson()
    {
        IGeoJsonOperations operations = new GeoJsonOperations();
        GeoJsonDocument source = ReadTolerant(
            """
            {
              "type":"Feature",
              "id":"area",
              "properties":{"name":"sample"},
              "geometry":{
                "type":"Polygon",
                "coordinates":[[[0.04,0.04],[0.04,2.26],[2.26,2.26],[2.26,0.04],[0.04,0.04]]]
              }
            }
            """);

        GeoJsonDocument rewound =
            operations.RewindGeoJson(source);
        GeoJsonDocument truncated =
            operations.TruncateGeoJsonCoordinates(rewound, 1);
        LinearRing resultRing = truncated.Feature!.Geometry!
            .Value!.Polygon!.Shell;
        LinearRing sourceRing = source.Feature!.Geometry!
            .Value!.Polygon!.Shell;

        Assert.Multiple(() =>
        {
            Assert.That(RingMath.GetSignedArea(resultRing.Coordinates),
                Is.GreaterThan(0.0));
            Assert.That(resultRing.Coordinates[2].X,
                Is.EqualTo(2.3));
            Assert.That(resultRing.Coordinates[2].Y,
                Is.EqualTo(2.3));
            Assert.That(RingMath.GetSignedArea(sourceRing.Coordinates),
                Is.LessThan(0.0));
            Assert.That(truncated.Feature.Id.GetString(),
                Is.EqualTo("area"));
            Assert.That(Write(truncated), Does.Contain("\"Polygon\""));
        });
    }

    [Test]
    public void FeatureTransformsAreAvailableThroughContract()
    {
        IGeoJsonOperations operations = new GeoJsonOperations();
        GeoJsonDocument source = Read(
            """
            {
              "type":"FeatureCollection",
              "features":[
                {"type":"Feature","id":"points","properties":{"kind":"p"},"geometry":{"type":"MultiPoint","coordinates":[[0,0],[1,1]]}},
                {"type":"Feature","id":"line","properties":{"kind":"l"},"geometry":{"type":"LineString","coordinates":[[0,0],[2,0]]}},
                {"type":"Feature","id":"area","properties":{"kind":"a"},"geometry":{"type":"Polygon","coordinates":[[[0,0],[2,0],[0,2],[0,0]]]}},
                {"type":"Feature","id":"null","properties":null,"geometry":null}
              ]
            }
            """);

        GeoJsonDocument flattened = operations.FlattenFeatures(source);
        GeoJsonDocument exploded = operations.ExplodeFeatures(source);
        GeoJsonDocument combined = operations.CombineFeatures(source);

        Assert.Multiple(() =>
        {
            Assert.That(flattened.FeatureCollection!.Features,
                Has.Length.EqualTo(5));
            Assert.That(exploded.FeatureCollection!.Features,
                Has.Length.EqualTo(8));
            Assert.That(combined.FeatureCollection!.Features,
                Has.Length.EqualTo(4));
            Assert.That(combined.FeatureCollection.Features[0]
                .Geometry!.GeometryType,
                Is.EqualTo(GeometryType.MultiPoint));
            Assert.That(combined.FeatureCollection.Features[3]
                .Geometry, Is.Null);
            Assert.That(Write(flattened),
                Does.Contain("FeatureCollection"));
            Assert.That(Write(exploded),
                Does.Contain("FeatureCollection"));
            Assert.That(Write(combined),
                Does.Contain("FeatureCollection"));
        });
    }

    [Test]
    public void SpatialJoinsReturnMatchingFeaturesFromFirstInput()
    {
        IGeoJsonOperations operations = new GeoJsonOperations();
        GeoJsonDocument points = Read(
            """
            {
              "type":"FeatureCollection",
              "features":[
                {"type":"Feature","id":"inside","properties":null,"geometry":{"type":"Point","coordinates":[1,1]}},
                {"type":"Feature","id":"outside","properties":null,"geometry":{"type":"Point","coordinates":[4,4]}}
              ]
            }
            """);
        GeoJsonDocument polygons = Read(
            """
            {
              "type":"FeatureCollection",
              "features":[
                {"type":"Feature","id":"zone","properties":null,"geometry":{"type":"Polygon","coordinates":[[[0,0],[2,0],[2,2],[0,2],[0,0]]]}}
              ]
            }
            """);

        GeoJsonDocument within =
            operations.PointsWithinPolygons(points, polygons);
        GeoJsonDocument intersecting =
            operations.IntersectingFeatures(points, polygons);

        Assert.Multiple(() =>
        {
            Assert.That(within.FeatureCollection!.Features,
                Has.Length.EqualTo(1));
            Assert.That(within.FeatureCollection.Features[0]
                .Id.GetString(), Is.EqualTo("inside"));
            Assert.That(intersecting.FeatureCollection!.Features,
                Has.Length.EqualTo(1));
            Assert.That(intersecting.FeatureCollection.Features[0]
                .Id.GetString(), Is.EqualTo("inside"));
            Assert.That(Write(within), Does.Contain("inside"));
            Assert.That(Write(intersecting), Does.Contain("inside"));
        });
    }

    [Test]
    public void EveryOperationRejectsNestedExtendedGeometry()
    {
        IGeoJsonOperations operations = new GeoJsonOperations();
        GeoJsonDocument extended = CreateNestedTriangleDocument();
        GeoJsonDocument supported = Read(
            """
            {"type":"Point","coordinates":[0,0]}
            """);

        Assert.Multiple(() =>
        {
            Assert.Throws<GeoJsonException>(() =>
                operations.ComputeGeoJsonBoundingBox(extended));
            Assert.Throws<GeoJsonException>(() =>
                operations.CutAntimeridian(extended));
            Assert.Throws<GeoJsonException>(() =>
                operations.RewindGeoJson(extended));
            Assert.Throws<GeoJsonException>(() =>
                operations.TruncateGeoJsonCoordinates(extended, 2));
            Assert.Throws<GeoJsonException>(() =>
                operations.FlattenFeatures(extended));
            Assert.Throws<GeoJsonException>(() =>
                operations.ExplodeFeatures(extended));
            Assert.Throws<GeoJsonException>(() =>
                operations.CombineFeatures(extended));
            Assert.Throws<GeoJsonException>(() =>
                operations.PointsWithinPolygons(supported, extended));
            Assert.Throws<GeoJsonException>(() =>
                operations.IntersectingFeatures(supported, extended));
        });
    }

    [Test]
    public void OperationsRejectUnsupportedCoordinateLayouts()
    {
        IGeoJsonOperations operations = new GeoJsonOperations();
        GeoJsonDocument document = Read(
            """
            {"type":"Point","coordinates":[1,2]}
            """);
        document.Geometry!.Value!.CoordinateLayout =
            CoordinateLayout.XYM;

        Assert.Multiple(() =>
        {
            Assert.Throws<GeoJsonException>(() =>
                operations.ComputeGeoJsonBoundingBox(document));
            Assert.Throws<GeoJsonException>(() =>
                operations.RewindGeoJson(document));
            Assert.Throws<GeoJsonException>(() =>
                operations.FlattenFeatures(document));
        });
    }

    private static GeoJsonDocument CreateNestedTriangleDocument()
    {
        Geometry triangle = new Geometry();
        triangle.GeometryType = GeometryType.Triangle;
        triangle.Point = null;
        triangle.Triangle = new Triangle();
        triangle.Triangle.Shell.Coordinates =
        [
            new Coordinate { X = 0.0, Y = 0.0 },
            new Coordinate { X = 1.0, Y = 0.0 },
            new Coordinate { X = 0.0, Y = 1.0 },
            new Coordinate { X = 0.0, Y = 0.0 }
        ];

        Geometry collection = new Geometry();
        collection.GeometryType = GeometryType.GeometryCollection;
        collection.Point = null;
        collection.GeometryCollection = new GeometryCollection();
        collection.GeometryCollection.Geometries = [triangle];

        GeoJsonGeometry metadata = new GeoJsonGeometry();
        metadata.GeometryType = GeometryType.GeometryCollection;
        metadata.Value = collection;
        GeoJsonDocument document = new GeoJsonDocument();
        document.ObjectType = GeoJsonObjectType.Geometry;
        document.Geometry = metadata;
        return document;
    }

    private static GeoJsonDocument Read(string text)
    {
        return CreateCodec(true).ReadGeoJson(text);
    }

    private static GeoJsonDocument ReadTolerant(string text)
    {
        return CreateCodec(false).ReadGeoJson(text);
    }

    private static string Write(GeoJsonDocument document)
    {
        return CreateCodec(true).WriteGeoJson(document);
    }

    private static IGeoJsonCodec CreateCodec(bool strict)
    {
        GeoJsonReadOptions readOptions = new GeoJsonReadOptions();
        readOptions.Strict = strict;
        return new GeoJsonCodec(
            readOptions, new GeoJsonWriteOptions());
    }
}
