using System.Text.Json;
using geo;

namespace geo.Tests;

public class GeoJsonDataTests
{
    [Test]
    public void GeoJsonDataDefaultsRepresentAbsentOptionalValuesAndEmptyCollections()
    {
        GeoJsonDocument document = new GeoJsonDocument();
        GeoJsonGeometry geometry = new GeoJsonGeometry();
        GeoJsonFeature feature = new GeoJsonFeature();
        GeoJsonFeatureCollection collection = new GeoJsonFeatureCollection();

        Assert.Multiple(() =>
        {
            Assert.That(document.ObjectType, Is.EqualTo(GeoJsonObjectType.Geometry));
            Assert.That(document.Geometry, Is.Null);
            Assert.That(document.Feature, Is.Null);
            Assert.That(document.FeatureCollection, Is.Null);
            Assert.That(document.BoundingBox, Is.Empty);
            Assert.That(document.ForeignMembers, Is.Empty);

            Assert.That(geometry.GeometryType, Is.EqualTo(GeometryType.Point));
            Assert.That(geometry.Value, Is.Null);
            Assert.That(geometry.BoundingBox, Is.Empty);
            Assert.That(geometry.ForeignMembers, Is.Empty);

            Assert.That(feature.Geometry, Is.Null);
            Assert.That(feature.Id.ValueKind, Is.EqualTo(JsonValueKind.Undefined));
            Assert.That(feature.Properties.ValueKind, Is.EqualTo(JsonValueKind.Undefined));
            Assert.That(feature.BoundingBox, Is.Empty);
            Assert.That(feature.ForeignMembers, Is.Empty);

            Assert.That(collection.Features, Is.Empty);
            Assert.That(collection.BoundingBox, Is.Empty);
            Assert.That(collection.ForeignMembers, Is.Empty);
        });
    }

    [Test]
    public void GeoJsonRootUnionRetainsGeometryFeatureAndFeatureCollectionValues()
    {
        Geometry pointValue = new Geometry
        {
            GeometryType = GeometryType.Point,
            Point = new Point { Coordinate = new Coordinate { X = 12.5, Y = -4.25 } }
        };
        GeoJsonGeometry geometry = new GeoJsonGeometry
        {
            GeometryType = GeometryType.Point,
            Value = pointValue,
            BoundingBox = new[] { 12.5, -4.25, 12.5, -4.25 }
        };
        GeoJsonFeature feature = new GeoJsonFeature { Geometry = null };
        GeoJsonFeatureCollection collection = new GeoJsonFeatureCollection
        {
            Features = new[] { feature },
            BoundingBox = new[] { 170.0, -10.0, -170.0, 10.0 }
        };
        GeoJsonDocument geometryDocument = new GeoJsonDocument
        {
            ObjectType = GeoJsonObjectType.Geometry,
            Geometry = geometry
        };
        GeoJsonDocument featureDocument = new GeoJsonDocument
        {
            ObjectType = GeoJsonObjectType.Feature,
            Feature = feature
        };
        GeoJsonDocument collectionDocument = new GeoJsonDocument
        {
            ObjectType = GeoJsonObjectType.FeatureCollection,
            FeatureCollection = collection
        };

        Assert.Multiple(() =>
        {
            Assert.That(geometryDocument.Geometry, Is.SameAs(geometry));
            Assert.That(geometryDocument.Geometry?.Value, Is.SameAs(pointValue));
            Assert.That(geometryDocument.Geometry?.Value?.Point?.Coordinate?.X, Is.EqualTo(12.5));
            Assert.That(featureDocument.Feature, Is.SameAs(feature));
            Assert.That(featureDocument.Feature?.Geometry, Is.Null);
            Assert.That(collectionDocument.FeatureCollection, Is.SameAs(collection));
            Assert.That(collectionDocument.FeatureCollection?.Features[0], Is.SameAs(feature));
            Assert.That(collectionDocument.FeatureCollection?.BoundingBox, Is.EqualTo(new[] { 170.0, -10.0, -170.0, 10.0 }));
        });
    }

    [Test]
    public void ClonedJsonElementsOutliveTheirSourceDocumentAtEveryNestingLevel()
    {
        GeoJsonDocument model;
        double[] rootBoundingBox = new[] { 170.0, -20.0, -170.0, 20.0 };
        double[] collectionBoundingBox = new[] { 171.0, -19.0, -171.0, 19.0 };
        double[] featureBoundingBox = new[] { 172.0, -18.0, -172.0, 18.0 };
        double[] geometryBoundingBox = new[] { 173.0, -17.0, -173.0, 17.0 };
        GeoJsonFeature[] featureArray;
        Dictionary<string, JsonElement> rootForeign;
        Dictionary<string, JsonElement> collectionForeign;
        Dictionary<string, JsonElement> featureForeign;
        Dictionary<string, JsonElement> geometryForeign;

        using (JsonDocument source = JsonDocument.Parse("""
            {
              "stringId": "feature-a",
              "numberId": 42,
              "properties": { "name": "alpha", "values": [1, 2, 3] },
              "nullProperties": null,
              "rootForeign": { "enabled": true },
              "collectionForeign": ["a", "b"],
              "featureForeign": 12.75,
              "geometryForeign": "retained"
            }
            """))
        {
            JsonElement root = source.RootElement;
            GeoJsonGeometry geometry = new GeoJsonGeometry
            {
                GeometryType = GeometryType.Point,
                Value = new Geometry(),
                BoundingBox = geometryBoundingBox,
                ForeignMembers = new Dictionary<string, JsonElement>
                {
                    ["geometry-extra"] = root.GetProperty("geometryForeign")
                }
            };
            GeoJsonFeature stringIdFeature = new GeoJsonFeature
            {
                Geometry = geometry,
                Id = root.GetProperty("stringId"),
                Properties = root.GetProperty("properties"),
                BoundingBox = featureBoundingBox,
                ForeignMembers = new Dictionary<string, JsonElement>
                {
                    ["feature-extra"] = root.GetProperty("featureForeign")
                }
            };
            GeoJsonFeature numberIdFeature = new GeoJsonFeature
            {
                Geometry = null,
                Id = root.GetProperty("numberId"),
                Properties = root.GetProperty("nullProperties")
            };
            featureArray = new[] { stringIdFeature, numberIdFeature };
            geometryForeign = geometry.ForeignMembers;
            featureForeign = stringIdFeature.ForeignMembers;
            GeoJsonFeatureCollection collection = new GeoJsonFeatureCollection
            {
                Features = featureArray,
                BoundingBox = collectionBoundingBox,
                ForeignMembers = new Dictionary<string, JsonElement>
                {
                    ["collection-extra"] = root.GetProperty("collectionForeign")
                }
            };
            collectionForeign = collection.ForeignMembers;
            model = new GeoJsonDocument
            {
                ObjectType = GeoJsonObjectType.FeatureCollection,
                FeatureCollection = collection,
                BoundingBox = rootBoundingBox,
                ForeignMembers = new Dictionary<string, JsonElement>
                {
                    ["root-extra"] = root.GetProperty("rootForeign")
                }
            };
            rootForeign = model.ForeignMembers;

            model.CloneJsonValues();
        }

        GeoJsonFeatureCollection clonedCollection = model.FeatureCollection ?? throw new AssertionException("Missing collection");
        GeoJsonFeature clonedStringFeature = clonedCollection.Features[0];
        GeoJsonFeature clonedNumberFeature = clonedCollection.Features[1];
        GeoJsonGeometry clonedGeometry = clonedStringFeature.Geometry ?? throw new AssertionException("Missing geometry");

        Assert.Multiple(() =>
        {
            Assert.That(model.BoundingBox, Is.Not.SameAs(rootBoundingBox));
            Assert.That(clonedCollection.BoundingBox, Is.Not.SameAs(collectionBoundingBox));
            Assert.That(clonedStringFeature.BoundingBox, Is.Not.SameAs(featureBoundingBox));
            Assert.That(clonedGeometry.BoundingBox, Is.Not.SameAs(geometryBoundingBox));
            Assert.That(clonedCollection.Features, Is.Not.SameAs(featureArray));
            Assert.That(model.ForeignMembers, Is.Not.SameAs(rootForeign));
            Assert.That(clonedCollection.ForeignMembers, Is.Not.SameAs(collectionForeign));
            Assert.That(clonedStringFeature.ForeignMembers, Is.Not.SameAs(featureForeign));
            Assert.That(clonedGeometry.ForeignMembers, Is.Not.SameAs(geometryForeign));

            Assert.That(clonedStringFeature.Id.GetString(), Is.EqualTo("feature-a"));
            Assert.That(clonedNumberFeature.Id.GetInt32(), Is.EqualTo(42));
            Assert.That(clonedStringFeature.Properties.GetProperty("name").GetString(), Is.EqualTo("alpha"));
            Assert.That(clonedStringFeature.Properties.GetProperty("values").GetArrayLength(), Is.EqualTo(3));
            Assert.That(clonedNumberFeature.Properties.ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(model.ForeignMembers["root-extra"].GetProperty("enabled").GetBoolean(), Is.True);
            Assert.That(clonedCollection.ForeignMembers["collection-extra"].GetArrayLength(), Is.EqualTo(2));
            Assert.That(clonedStringFeature.ForeignMembers["feature-extra"].GetDouble(), Is.EqualTo(12.75));
            Assert.That(clonedGeometry.ForeignMembers["geometry-extra"].GetString(), Is.EqualTo("retained"));
        });
    }

    [Test]
    public void CloningLeavesAbsentIdAndPropertiesUndefined()
    {
        GeoJsonFeature feature = new GeoJsonFeature();

        feature.CloneJsonValues();

        Assert.Multiple(() =>
        {
            Assert.That(feature.Id.ValueKind, Is.EqualTo(JsonValueKind.Undefined));
            Assert.That(feature.Properties.ValueKind, Is.EqualTo(JsonValueKind.Undefined));
            Assert.That(feature.Geometry, Is.Null);
        });
    }
}
