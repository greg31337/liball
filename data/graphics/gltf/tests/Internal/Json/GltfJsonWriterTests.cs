using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

namespace gltf.Tests.Internal.Json;

[TestFixture]
public class GltfJsonWriterTests
{
    private const string CompleteDocument = """
        {
          "asset": {
            "version": "2.0", "minVersion": "2.0", "generator": "writer", "copyright": "owner"
          },
          "accessors": [{
            "bufferView": 0, "byteOffset": 4, "componentType": 5126, "normalized": true,
            "count": 1, "type": "VEC3", "max": [1, 2, 3], "min": [-1, -2, -3],
            "sparse": {
              "count": 1,
              "indices": { "bufferView": 0, "byteOffset": 1, "componentType": 5121 },
              "values": { "bufferView": 0, "byteOffset": 2 }
            },
            "name": "accessor"
          }],
          "animations": [{
            "channels": [{ "sampler": 0, "target": { "node": 0, "path": "rotation" } }],
            "samplers": [{ "input": 0, "interpolation": "STEP", "output": 0 }],
            "name": "animation"
          }],
          "buffers": [{ "uri": "buffer.bin", "byteLength": 64, "name": "buffer" }],
          "bufferViews": [{
            "buffer": 0, "byteOffset": 4, "byteLength": 32, "byteStride": 12,
            "target": 34962, "name": "view"
          }],
          "cameras": [
            {
              "type": "perspective",
              "perspective": { "aspectRatio": 1.5, "yfov": 1, "zfar": 100, "znear": 0.1 },
              "name": "perspective"
            },
            {
              "type": "orthographic",
              "orthographic": { "xmag": 2, "ymag": 3, "zfar": 10, "znear": 0 },
              "name": "orthographic"
            }
          ],
          "images": [{ "uri": "image.png", "mimeType": "image/png", "bufferView": 0, "name": "image" }],
          "materials": [{
            "pbrMetallicRoughness": {
              "baseColorFactor": [0.1, 0.2, 0.3, 0.4],
              "baseColorTexture": { "index": 0, "texCoord": 1 },
              "metallicFactor": 0.5, "roughnessFactor": 0.6,
              "metallicRoughnessTexture": { "index": 0 }
            },
            "normalTexture": { "index": 0, "texCoord": 1, "scale": 0.7 },
            "occlusionTexture": { "index": 0, "texCoord": 1, "strength": 0.8 },
            "emissiveTexture": { "index": 0, "texCoord": 1 },
            "emissiveFactor": [0.2, 0.3, 0.4], "alphaMode": "MASK",
            "alphaCutoff": 0.25, "doubleSided": true, "name": "material"
          }],
          "meshes": [{
            "primitives": [{
              "attributes": { "_CUSTOM": 0, "POSITION": 0 },
              "indices": 0, "material": 0, "mode": 5,
              "targets": [{ "POSITION": 0 }]
            }],
            "weights": [0.5], "name": "mesh"
          }],
          "nodes": [{
            "children": [0], "camera": 0, "skin": 0, "mesh": 0,
            "matrix": [1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 1, 2, 3, 1],
            "rotation": [0, 0, 0, 1], "scale": [2, 2, 2], "translation": [1, 2, 3],
            "weights": [0.75], "name": "node"
          }],
          "samplers": [{
            "magFilter": 9728, "minFilter": 9987, "wrapS": 33071, "wrapT": 33648, "name": "sampler"
          }],
          "scenes": [{ "nodes": [0], "name": "scene" }],
          "skins": [{ "inverseBindMatrices": 0, "skeleton": 0, "joints": [0], "name": "skin" }],
          "textures": [{ "sampler": 0, "source": 0, "name": "texture" }],
          "scene": 0,
          "extensionsUsed": ["EXT_a", "EXT_z"],
          "extensionsRequired": ["EXT_z"],
          "extensions": { "EXT_z": { "z": 1 }, "EXT_a": { "a": 2 } },
          "extras": { "label": "root" },
          "zFuture": 2,
          "aFuture": 1
        }
        """;

    [Test]
    public void WriteDocument_RoundTripsEveryCoreObjectFamily()
    {
        GltfJsonReadResult parsed = ParseJson(CompleteDocument);
        GltfDocument document = ReadDocument(parsed);

        JsonObject written = new GltfJsonWriter().WriteDocument(document, new GltfWriteOptions());
        GltfJsonReadResult reparsed = ParseJson(written.ToJsonString());

        Assert.That(JsonNode.DeepEquals(reparsed.Root, parsed.Root), Is.True);
    }

    [Test]
    public void WriteDocument_UsesFixedSchemaOrderAndSortsDictionaryMembers()
    {
        GltfDocument document = ReadDocument(ParseJson(CompleteDocument));

        JsonObject written = new GltfJsonWriter().WriteDocument(document, new GltfWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(
                written.Select(member => member.Key),
                Is.EqualTo(new[]
                {
                    "asset", "accessors", "animations", "buffers", "bufferViews", "cameras",
                    "images", "materials", "meshes", "nodes", "samplers", "scenes", "skins",
                    "textures", "scene", "extensionsUsed", "extensionsRequired", "extensions",
                    "extras", "aFuture", "zFuture"
                }));
            Assert.That(
                written["accessors"]![0]!.AsObject().Select(member => member.Key),
                Is.EqualTo(new[]
                {
                    "bufferView", "byteOffset", "componentType", "normalized", "count", "type",
                    "max", "min", "sparse", "name"
                }));
            Assert.That(
                written["materials"]![0]!.AsObject().Select(member => member.Key),
                Is.EqualTo(new[]
                {
                    "pbrMetallicRoughness", "normalTexture", "occlusionTexture", "emissiveTexture",
                    "emissiveFactor", "alphaMode", "alphaCutoff", "doubleSided", "name"
                }));
            Assert.That(
                written["nodes"]![0]!.AsObject().Select(member => member.Key),
                Is.EqualTo(new[]
                {
                    "children", "camera", "skin", "mesh", "matrix", "rotation", "scale",
                    "translation", "weights", "name"
                }));
            Assert.That(
                written["meshes"]![0]!["primitives"]![0]!["attributes"]!.AsObject()
                    .Select(member => member.Key),
                Is.EqualTo(new[] { "POSITION", "_CUSTOM" }));
            Assert.That(
                written["extensions"]!.AsObject().Select(member => member.Key),
                Is.EqualTo(new[] { "EXT_a", "EXT_z" }));
        });
    }

    [Test]
    public void WriteDocument_PreservesOrOmitsExplicitDefaultsByPolicy()
    {
        GltfDocument document = ReadDocument(ParseJson(
            """
            {
              "asset": { "version": "2.0" },
              "accessors": [{
                "byteOffset": 0, "componentType": 5126, "normalized": false,
                "count": 0, "type": "SCALAR"
              }],
              "animations": [{
                "channels": [{ "sampler": 0, "target": { "path": "translation" } }],
                "samplers": [{ "input": 0, "interpolation": "LINEAR", "output": 0 }]
              }],
              "materials": [{
                "pbrMetallicRoughness": {
                  "baseColorFactor": [1, 1, 1, 1],
                  "baseColorTexture": { "index": 0, "texCoord": 0 },
                  "metallicFactor": 1, "roughnessFactor": 1
                },
                "normalTexture": { "index": 0, "texCoord": 0, "scale": 1 },
                "occlusionTexture": { "index": 0, "texCoord": 0, "strength": 1 },
                "emissiveFactor": [0, 0, 0], "alphaMode": "OPAQUE",
                "alphaCutoff": 0.5, "doubleSided": false
              }],
              "meshes": [{ "primitives": [{ "attributes": { "POSITION": 0 }, "mode": 4 }] }],
              "nodes": [{
                "matrix": [1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1],
                "rotation": [0, 0, 0, 1], "scale": [1, 1, 1], "translation": [0, 0, 0]
              }],
              "samplers": [{ "wrapS": 10497, "wrapT": 10497 }]
            }
            """));
        GltfWriteOptions preserveOptions = new GltfWriteOptions();
        GltfWriteOptions omitOptions = new GltfWriteOptions();
        omitOptions.PreserveExplicitDefaults = false;

        JsonObject preserved = new GltfJsonWriter().WriteDocument(document, preserveOptions);
        JsonObject omitted = new GltfJsonWriter().WriteDocument(document, omitOptions);

        Assert.Multiple(() =>
        {
            Assert.That(preserved["accessors"]![0]!["byteOffset"], Is.Not.Null);
            Assert.That(preserved["accessors"]![0]!["normalized"], Is.Not.Null);
            Assert.That(preserved["materials"]![0]!["alphaMode"], Is.Not.Null);
            Assert.That(preserved["nodes"]![0]!["matrix"], Is.Not.Null);
            Assert.That(preserved["samplers"]![0]!["wrapS"], Is.Not.Null);
            Assert.That(omitted["accessors"]![0]!["byteOffset"], Is.Null);
            Assert.That(omitted["accessors"]![0]!["normalized"], Is.Null);
            Assert.That(omitted["animations"]![0]!["samplers"]![0]!["interpolation"], Is.Null);
            Assert.That(omitted["materials"]![0]!.AsObject().ContainsKey("emissiveFactor"), Is.False);
            Assert.That(omitted["materials"]![0]!["pbrMetallicRoughness"]!["baseColorFactor"], Is.Null);
            Assert.That(omitted["materials"]![0]!["normalTexture"]!["scale"], Is.Null);
            Assert.That(omitted["materials"]![0]!["occlusionTexture"]!["strength"], Is.Null);
            Assert.That(omitted["materials"]![0]!["alphaMode"], Is.Null);
            Assert.That(omitted["materials"]![0]!["alphaCutoff"], Is.Null);
            Assert.That(omitted["materials"]![0]!["doubleSided"], Is.Null);
            Assert.That(omitted["meshes"]![0]!["primitives"]![0]!["mode"], Is.Null);
            Assert.That(omitted["nodes"]![0]!.AsObject().ContainsKey("matrix"), Is.False);
            Assert.That(omitted["nodes"]![0]!.AsObject().ContainsKey("rotation"), Is.False);
            Assert.That(omitted["nodes"]![0]!.AsObject().ContainsKey("scale"), Is.False);
            Assert.That(omitted["nodes"]![0]!.AsObject().ContainsKey("translation"), Is.False);
            Assert.That(omitted["samplers"]![0]!["wrapS"], Is.Null);
            Assert.That(omitted["samplers"]![0]!["wrapT"], Is.Null);
        });

        document.Accessors[0].Normalized = true;
        document.Materials[0].PbrMetallicRoughness!.MetallicFactor = 0.25;
        document.Nodes[0].Translation = [1.0, 0.0, 0.0];
        document.Samplers[0].WrapT = GltfSamplerWrap.ClampToEdge;
        JsonObject nonDefaults = new GltfJsonWriter().WriteDocument(document, omitOptions);

        Assert.Multiple(() =>
        {
            Assert.That(nonDefaults["accessors"]![0]!["normalized"]!.GetValue<bool>(), Is.True);
            Assert.That(
                nonDefaults["materials"]![0]!["pbrMetallicRoughness"]!["metallicFactor"]!.GetValue<double>(),
                Is.EqualTo(0.25));
            Assert.That(nonDefaults["nodes"]![0]!["translation"], Is.Not.Null);
            Assert.That(nonDefaults["samplers"]![0]!["wrapT"]!.GetValue<int>(), Is.EqualTo(33071));
        });
    }

    [Test]
    public void WriteDocument_PreservesOwnedJsonAndHonorsUnknownMemberPolicy()
    {
        GltfDocument document = ReadDocument(ParseJson(CompleteDocument));
        GltfWriteOptions omitUnknown = new GltfWriteOptions();
        omitUnknown.PreserveUnknownMembers = false;

        JsonObject preserved = new GltfJsonWriter().WriteDocument(document, new GltfWriteOptions());
        JsonObject omitted = new GltfJsonWriter().WriteDocument(document, omitUnknown);

        Assert.Multiple(() =>
        {
            Assert.That(
                preserved["extensions"]!["EXT_a"],
                Is.Not.SameAs(document.PropertyJson.Extensions["EXT_a"]));
            Assert.That(preserved["extras"], Is.Not.SameAs(document.PropertyJson.Extras));
            Assert.That(preserved["aFuture"], Is.Not.SameAs(document.PropertyJson.UnknownMembers["aFuture"]));
            Assert.That(omitted["extensions"], Is.Not.Null);
            Assert.That(omitted["extras"], Is.Not.Null);
            Assert.That(omitted["aFuture"], Is.Null);
            Assert.That(omitted["zFuture"], Is.Null);
        });

        preserved["extensions"]!["EXT_a"]!["a"] = 99;
        preserved["extras"]!["label"] = "changed";
        Assert.Multiple(() =>
        {
            Assert.That(document.PropertyJson.Extensions["EXT_a"]!["a"]!.GetValue<double>(), Is.EqualTo(2.0));
            Assert.That(document.PropertyJson.Extras!["label"]!.GetValue<string>(), Is.EqualTo("root"));
        });
    }

    [Test]
    public void WriteDocument_DistinguishesNullExtrasFromAnAbsentMember()
    {
        GltfDocument withNullExtras = ReadDocument(ParseJson(
            "{\"asset\":{\"version\":\"2.0\"},\"extras\":null}"));
        GltfDocument withoutExtras = ReadDocument(ParseJson(
            "{\"asset\":{\"version\":\"2.0\"}}"));

        JsonObject writtenNull = new GltfJsonWriter().WriteDocument(withNullExtras, new GltfWriteOptions());
        JsonObject writtenAbsent = new GltfJsonWriter().WriteDocument(withoutExtras, new GltfWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(withNullExtras.PropertyJson.HasExtras, Is.True);
            Assert.That(writtenNull.ContainsKey("extras"), Is.True);
            Assert.That(writtenNull["extras"], Is.Null);
            Assert.That(withoutExtras.PropertyJson.HasExtras, Is.False);
            Assert.That(writtenAbsent.ContainsKey("extras"), Is.False);
        });
    }

    [Test]
    [NonParallelizable]
    public void WriteDocument_UsesJsonEscapingAndCultureInvariantNumbers()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            GltfDocument document = new GltfDocument();
            document.Asset.Generator = "line\n\"quoted\"";
            GltfCamera camera = new GltfCamera();
            camera.Perspective = new GltfPerspectiveCamera();
            camera.Perspective.VerticalFieldOfView = 1.25;
            camera.Perspective.NearPlane = 0.01;
            document.Cameras.Add(camera);

            string json = new GltfJsonWriter().WriteDocument(document, new GltfWriteOptions()).ToJsonString();
            JsonNode reparsed = JsonNode.Parse(json)!;

            Assert.Multiple(() =>
            {
                Assert.That(json, Does.Contain("\"yfov\":1.25"));
                Assert.That(json, Does.Not.Contain("1,25"));
                Assert.That(reparsed["asset"]!["generator"]!.GetValue<string>(), Is.EqualTo("line\n\"quoted\""));
            });
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Test]
    public void WriteDocument_RejectsUnknownStringEnumsAndNullArguments()
    {
        GltfDocument document = new GltfDocument();
        GltfAccessor accessor = new GltfAccessor();
        accessor.Type = (GltfAccessorType)99;
        document.Accessors.Add(accessor);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => new GltfJsonWriter().WriteDocument(document, new GltfWriteOptions()),
                Throws.TypeOf<GltfWriteException>().With.Message.EqualTo("Unknown accessor type 99."));
            Assert.That(
                () => new GltfJsonWriter().WriteDocument(null!, new GltfWriteOptions()),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("document"));
            Assert.That(
                () => new GltfJsonWriter().WriteDocument(new GltfDocument(), null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("options"));
        });
    }

    private static GltfJsonReadResult ParseJson(string json)
    {
        return new GltfJsonReader().Read(
            Encoding.UTF8.GetBytes(json),
            new GltfReadLimits(),
            GltfDuplicateMemberPolicy.Reject);
    }

    private static GltfDocument ReadDocument(GltfJsonReadResult parsed)
    {
        return new GltfSchemaReader().ReadDocument(
            parsed,
            new GltfReadLimits(),
            true,
            new List<IGltfExtensionHandler>());
    }
}
