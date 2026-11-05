using System.Text;

namespace gltf.Tests.Internal.Json;

[TestFixture]
public class GltfSchemaReaderTests
{
    private const string CompleteDocument = """
        {
          "asset": {
            "version": "2.0",
            "minVersion": "2.0",
            "generator": "tests",
            "copyright": "owner",
            "extensions": { "EXT_asset": { "enabled": true } },
            "extras": { "tag": "asset" },
            "futureAsset": 7
          },
          "accessors": [{
            "bufferView": 0,
            "byteOffset": 4,
            "componentType": 5126,
            "normalized": false,
            "count": 1,
            "type": "VEC3",
            "max": [1, 2, 3],
            "min": [-1, -2, -3],
            "sparse": {
              "count": 1,
              "indices": { "bufferView": 0, "byteOffset": 1, "componentType": 5121 },
              "values": { "bufferView": 0, "byteOffset": 2 }
            },
            "name": "positions"
          }],
          "animations": [{
            "channels": [{ "sampler": 0, "target": { "node": 0, "path": "rotation" } }],
            "samplers": [{ "input": 0, "interpolation": "STEP", "output": 0 }],
            "name": "clip"
          }],
          "buffers": [{ "uri": "buffer.bin", "byteLength": 64, "name": "buffer" }],
          "bufferViews": [{
            "buffer": 0, "byteOffset": 4, "byteLength": 32, "byteStride": 12,
            "target": 34962, "name": "view"
          }],
          "cameras": [
            { "type": "perspective", "perspective": { "aspectRatio": 1.5, "yfov": 1, "zfar": 100, "znear": 0.1 }, "name": "p" },
            { "type": "orthographic", "orthographic": { "xmag": 2, "ymag": 3, "zfar": 10, "znear": 0 }, "name": "o" }
          ],
          "images": [{ "uri": "image.png", "mimeType": "image/png", "bufferView": 0, "name": "image" }],
          "materials": [{
            "pbrMetallicRoughness": {
              "baseColorFactor": [0.1, 0.2, 0.3, 0.4],
              "baseColorTexture": { "index": 0, "texCoord": 1 },
              "metallicFactor": 0.5,
              "roughnessFactor": 0.6,
              "metallicRoughnessTexture": { "index": 0 }
            },
            "normalTexture": { "index": 0, "texCoord": 1, "scale": 0.7 },
            "occlusionTexture": { "index": 0, "texCoord": 1, "strength": 0.8 },
            "emissiveTexture": { "index": 0, "texCoord": 1 },
            "emissiveFactor": [0.2, 0.3, 0.4],
            "alphaMode": "MASK",
            "alphaCutoff": 0.25,
            "doubleSided": true,
            "name": "material"
          }],
          "meshes": [{
            "primitives": [{
              "attributes": { "POSITION": 0, "_CUSTOM": 0 },
              "indices": 0,
              "material": 0,
              "mode": 5,
              "targets": [{ "POSITION": 0 }]
            }],
            "weights": [0.5],
            "name": "mesh"
          }],
          "nodes": [{
            "children": [0], "camera": 0, "skin": 0, "mesh": 0,
            "matrix": [1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 1, 2, 3, 1],
            "rotation": [0, 0, 0, 1], "scale": [2, 2, 2], "translation": [1, 2, 3],
            "weights": [0.75], "name": "node"
          }],
          "samplers": [{ "magFilter": 9728, "minFilter": 9987, "wrapS": 33071, "wrapT": 33648, "name": "sampler" }],
          "scenes": [{ "nodes": [0], "name": "scene" }],
          "skins": [{ "inverseBindMatrices": 0, "skeleton": 0, "joints": [0], "name": "skin" }],
          "textures": [{ "sampler": 0, "source": 0, "name": "texture" }],
          "scene": 0,
          "extensionsUsed": ["EXT_asset"],
          "extensionsRequired": ["EXT_asset"],
          "extensions": { "EXT_root": { "value": 1 } },
          "extras": [1, 2],
          "futureRoot": { "value": 2 }
        }
        """;

    [Test]
    public void ReadDocument_MapsEveryCoreObjectFamilyAndExactPresence()
    {
        GltfDocument document = ReadDocument(CompleteDocument, true, new GltfReadLimits());

        Assert.Multiple(() =>
        {
            Assert.That(document.Asset.Version, Is.EqualTo("2.0"));
            Assert.That(document.Asset.MinimumVersion, Is.EqualTo("2.0"));
            Assert.That(document.Asset.Generator, Is.EqualTo("tests"));
            Assert.That(document.Asset.Copyright, Is.EqualTo("owner"));
            Assert.That(document.Accessors, Has.Count.EqualTo(1));
            Assert.That(document.Animations, Has.Count.EqualTo(1));
            Assert.That(document.Buffers, Has.Count.EqualTo(1));
            Assert.That(document.BufferViews, Has.Count.EqualTo(1));
            Assert.That(document.Cameras, Has.Count.EqualTo(2));
            Assert.That(document.Images, Has.Count.EqualTo(1));
            Assert.That(document.Materials, Has.Count.EqualTo(1));
            Assert.That(document.Meshes, Has.Count.EqualTo(1));
            Assert.That(document.Nodes, Has.Count.EqualTo(1));
            Assert.That(document.Samplers, Has.Count.EqualTo(1));
            Assert.That(document.Scenes, Has.Count.EqualTo(1));
            Assert.That(document.Skins, Has.Count.EqualTo(1));
            Assert.That(document.Textures, Has.Count.EqualTo(1));
            Assert.That(document.HasDefaultScene, Is.True);
            Assert.That(document.DefaultScene, Is.Zero);
            Assert.That(document.ExtensionsUsed, Is.EqualTo(new[] { "EXT_asset" }));
            Assert.That(document.ExtensionsRequired, Is.EqualTo(new[] { "EXT_asset" }));
        });

        GltfAccessor accessor = document.Accessors[0];
        Assert.Multiple(() =>
        {
            Assert.That(accessor.HasBufferView, Is.True);
            Assert.That(accessor.BufferView, Is.Zero);
            Assert.That(accessor.HasByteOffset, Is.True);
            Assert.That(accessor.ByteOffset, Is.EqualTo(4));
            Assert.That(accessor.ComponentType, Is.EqualTo(GltfComponentType.Float));
            Assert.That(accessor.HasNormalized, Is.True);
            Assert.That(accessor.Normalized, Is.False);
            Assert.That(accessor.Count, Is.EqualTo(1));
            Assert.That(accessor.Type, Is.EqualTo(GltfAccessorType.Vector3));
            Assert.That(accessor.Maximum, Is.EqualTo(new[] { 1.0, 2.0, 3.0 }));
            Assert.That(accessor.Minimum, Is.EqualTo(new[] { -1.0, -2.0, -3.0 }));
            Assert.That(accessor.Sparse, Is.Not.Null);
            Assert.That(accessor.Sparse!.Count, Is.EqualTo(1));
            Assert.That(accessor.Sparse.Indices.BufferView, Is.Zero);
            Assert.That(accessor.Sparse.Indices.HasByteOffset, Is.True);
            Assert.That(accessor.Sparse.Indices.ComponentType, Is.EqualTo(GltfComponentType.UnsignedByte));
            Assert.That(accessor.Sparse.Values.BufferView, Is.Zero);
            Assert.That(accessor.Sparse.Values.HasByteOffset, Is.True);
            Assert.That(accessor.Name, Is.EqualTo("positions"));
        });

        GltfAnimation animation = document.Animations[0];
        Assert.Multiple(() =>
        {
            Assert.That(animation.Channels[0].Sampler, Is.Zero);
            Assert.That(animation.Channels[0].Target.HasNode, Is.True);
            Assert.That(animation.Channels[0].Target.Path, Is.EqualTo(GltfAnimationPath.Rotation));
            Assert.That(animation.Samplers[0].Input, Is.Zero);
            Assert.That(animation.Samplers[0].HasInterpolation, Is.True);
            Assert.That(animation.Samplers[0].Interpolation, Is.EqualTo(GltfAnimationInterpolation.Step));
            Assert.That(animation.Samplers[0].Output, Is.Zero);
            Assert.That(animation.Name, Is.EqualTo("clip"));
        });

        Assert.Multiple(() =>
        {
            Assert.That(document.Buffers[0].Uri, Is.EqualTo("buffer.bin"));
            Assert.That(document.Buffers[0].ByteLength, Is.EqualTo(64));
            Assert.That(document.BufferViews[0].HasByteOffset, Is.True);
            Assert.That(document.BufferViews[0].HasByteStride, Is.True);
            Assert.That(document.BufferViews[0].HasTarget, Is.True);
            Assert.That(document.BufferViews[0].Target, Is.EqualTo(GltfBufferViewTarget.ArrayBuffer));
            Assert.That(document.Cameras[0].Type, Is.EqualTo(GltfCameraType.Perspective));
            Assert.That(document.Cameras[0].Perspective!.HasAspectRatio, Is.True);
            Assert.That(document.Cameras[0].Perspective!.HasFarPlane, Is.True);
            Assert.That(document.Cameras[1].Type, Is.EqualTo(GltfCameraType.Orthographic));
            Assert.That(document.Cameras[1].Orthographic!.HorizontalMagnification, Is.EqualTo(2.0));
            Assert.That(document.Images[0].HasBufferView, Is.True);
            Assert.That(document.Images[0].MimeType, Is.EqualTo("image/png"));
        });

        GltfMaterial material = document.Materials[0];
        Assert.Multiple(() =>
        {
            Assert.That(material.PbrMetallicRoughness, Is.Not.Null);
            Assert.That(material.PbrMetallicRoughness!.HasBaseColorFactor, Is.True);
            Assert.That(material.PbrMetallicRoughness.BaseColorTexture!.HasTextureCoordinate, Is.True);
            Assert.That(material.PbrMetallicRoughness.HasMetallicFactor, Is.True);
            Assert.That(material.PbrMetallicRoughness.HasRoughnessFactor, Is.True);
            Assert.That(material.PbrMetallicRoughness.MetallicRoughnessTexture!.Index, Is.Zero);
            Assert.That(material.NormalTexture!.HasScale, Is.True);
            Assert.That(material.OcclusionTexture!.HasStrength, Is.True);
            Assert.That(material.EmissiveTexture!.HasTextureCoordinate, Is.True);
            Assert.That(material.HasEmissiveFactor, Is.True);
            Assert.That(material.HasAlphaMode, Is.True);
            Assert.That(material.AlphaMode, Is.EqualTo(GltfAlphaMode.Mask));
            Assert.That(material.HasAlphaCutoff, Is.True);
            Assert.That(material.HasDoubleSided, Is.True);
            Assert.That(material.DoubleSided, Is.True);
        });

        GltfMeshPrimitive primitive = document.Meshes[0].Primitives[0];
        GltfNode node = document.Nodes[0];
        Assert.Multiple(() =>
        {
            Assert.That(primitive.Attributes.Select(attribute => attribute.Semantic), Is.EqualTo(new[] { "POSITION", "_CUSTOM" }));
            Assert.That(primitive.HasIndices, Is.True);
            Assert.That(primitive.HasMaterial, Is.True);
            Assert.That(primitive.HasMode, Is.True);
            Assert.That(primitive.Mode, Is.EqualTo(GltfPrimitiveMode.TriangleStrip));
            Assert.That(primitive.Targets[0].Attributes[0].Semantic, Is.EqualTo("POSITION"));
            Assert.That(document.Meshes[0].Weights, Is.EqualTo(new[] { 0.5 }));
            Assert.That(node.Children, Is.EqualTo(new[] { 0 }));
            Assert.That(node.HasCamera, Is.True);
            Assert.That(node.HasSkin, Is.True);
            Assert.That(node.HasMesh, Is.True);
            Assert.That(node.HasMatrix, Is.True);
            Assert.That(node.HasRotation, Is.True);
            Assert.That(node.HasScale, Is.True);
            Assert.That(node.HasTranslation, Is.True);
            Assert.That(node.Matrix, Has.Length.EqualTo(16));
            Assert.That(node.Weights, Is.EqualTo(new[] { 0.75 }));
        });

        Assert.Multiple(() =>
        {
            Assert.That(document.Samplers[0].MagnificationFilter, Is.EqualTo(GltfSamplerFilter.Nearest));
            Assert.That(document.Samplers[0].MinificationFilter, Is.EqualTo(GltfSamplerFilter.LinearMipmapLinear));
            Assert.That(document.Samplers[0].WrapS, Is.EqualTo(GltfSamplerWrap.ClampToEdge));
            Assert.That(document.Samplers[0].WrapT, Is.EqualTo(GltfSamplerWrap.MirroredRepeat));
            Assert.That(document.Scenes[0].Nodes, Is.EqualTo(new[] { 0 }));
            Assert.That(document.Skins[0].HasInverseBindMatrices, Is.True);
            Assert.That(document.Skins[0].HasSkeleton, Is.True);
            Assert.That(document.Skins[0].Joints, Is.EqualTo(new[] { 0 }));
            Assert.That(document.Textures[0].HasSampler, Is.True);
            Assert.That(document.Textures[0].HasSource, Is.True);
        });
    }

    [Test]
    public void ReadDocument_PreservesExtensionsExtrasAndUnknownMembersByPolicy()
    {
        GltfDocument preserved = ReadDocument(CompleteDocument, true, new GltfReadLimits());
        GltfDocument omitted = ReadDocument(CompleteDocument, false, new GltfReadLimits());

        Assert.Multiple(() =>
        {
            Assert.That(preserved.PropertyJson.Extensions.Keys, Is.EqualTo(new[] { "EXT_root" }));
            Assert.That(preserved.PropertyJson.Extras, Is.Not.Null);
            Assert.That(preserved.PropertyJson.UnknownMembers.Keys, Is.EqualTo(new[] { "futureRoot" }));
            Assert.That(preserved.Asset.PropertyJson.Extensions.Keys, Is.EqualTo(new[] { "EXT_asset" }));
            Assert.That(preserved.Asset.PropertyJson.Extras, Is.Not.Null);
            Assert.That(preserved.Asset.PropertyJson.UnknownMembers.Keys, Is.EqualTo(new[] { "futureAsset" }));
            Assert.That(omitted.PropertyJson.Extensions.Keys, Is.EqualTo(new[] { "EXT_root" }));
            Assert.That(omitted.PropertyJson.Extras, Is.Not.Null);
            Assert.That(omitted.PropertyJson.UnknownMembers, Is.Empty);
            Assert.That(omitted.Asset.PropertyJson.UnknownMembers, Is.Empty);
        });
    }

    [Test]
    public void ReadDocument_RetainsSchemaDefaultsAndAbsenceFlags()
    {
        const string json = """
            {
              "asset": { "version": "2.0" },
              "materials": [{}],
              "nodes": [{}],
              "samplers": [{}],
              "textures": [{}]
            }
            """;

        GltfDocument document = ReadDocument(json, true, new GltfReadLimits());

        Assert.Multiple(() =>
        {
            Assert.That(document.HasDefaultScene, Is.False);
            Assert.That(document.DefaultScene, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(document.Materials[0].HasAlphaMode, Is.False);
            Assert.That(document.Materials[0].AlphaMode, Is.EqualTo(GltfAlphaMode.Opaque));
            Assert.That(document.Materials[0].HasAlphaCutoff, Is.False);
            Assert.That(document.Materials[0].AlphaCutoff, Is.EqualTo(0.5));
            Assert.That(document.Materials[0].HasDoubleSided, Is.False);
            Assert.That(document.Nodes[0].HasMatrix, Is.False);
            Assert.That(document.Nodes[0].Matrix, Is.EqualTo(new[]
            {
                1.0, 0.0, 0.0, 0.0,
                0.0, 1.0, 0.0, 0.0,
                0.0, 0.0, 1.0, 0.0,
                0.0, 0.0, 0.0, 1.0
            }));
            Assert.That(document.Samplers[0].HasWrapS, Is.False);
            Assert.That(document.Samplers[0].WrapS, Is.EqualTo(GltfSamplerWrap.Repeat));
            Assert.That(document.Textures[0].HasSampler, Is.False);
            Assert.That(document.Textures[0].Sampler, Is.EqualTo(GltfConstants.AbsentIndex));
        });
    }

    [TestCase("{\"asset\":{}}", "Required JSON member 'version' is missing", "/asset")]
    [TestCase("{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{}]}", "Required JSON member 'byteLength' is missing", "/buffers/0")]
    [TestCase("{\"asset\":{\"version\":\"2.0\"},\"meshes\":[{\"primitives\":[]}]}", "Expected a non-empty JSON collection", "/meshes/0/primitives")]
    public void ReadDocument_RejectsMissingRequiredMembersAndEmptyRequiredCollections(
        string json,
        string expectedMessage,
        string expectedPointer)
    {
        Assert.That(
            () => ReadDocument(json, true, new GltfReadLimits()),
            Throws.TypeOf<GltfParseException>()
                .With.Message.Contains(expectedMessage)
                .And.Message.EndsWith($"JSON pointer '{expectedPointer}'."));
    }

    [TestCase("{\"asset\":{\"version\":2}}", "Expected a JSON string", "/asset/version")]
    [TestCase("{\"asset\":{\"version\":\"2.0\"},\"scene\":1.5}", "Expected a JSON 32-bit integer", "/scene")]
    [TestCase("{\"asset\":{\"version\":\"2.0\"},\"nodes\":[{\"scale\":[1,1]}]}", "exactly 3 elements", "/nodes/0/scale")]
    [TestCase("{\"asset\":{\"version\":\"2.0\"},\"samplers\":[{\"wrapS\":1}]}", "Unknown sampler wrap mode 1", "/samplers/0/wrapS")]
    public void ReadDocument_RejectsWrongScalarArrayAndEnumShapes(
        string json,
        string expectedMessage,
        string expectedPointer)
    {
        Assert.That(
            () => ReadDocument(json, true, new GltfReadLimits()),
            Throws.TypeOf<GltfParseException>()
                .With.Message.Contains(expectedMessage)
                .And.Message.EndsWith($"JSON pointer '{expectedPointer}'."));
    }

    [Test]
    public void ReadDocument_EnforcesCollectionLimitsBeforeAllocatingModelLists()
    {
        GltfReadLimits limits = new GltfReadLimits();
        limits.MaximumCollectionCount = 1;

        Assert.That(
            () => ReadDocument(
                "{\"asset\":{\"version\":\"2.0\"},\"nodes\":[{},{}]}",
                true,
                limits),
            Throws.TypeOf<GltfParseException>()
                .With.Message.EqualTo(
                    "JSON collection count 2 exceeds the configured maximum 1 at byte 27 and JSON pointer '/nodes'."));
    }

    [Test]
    public void DocumentReadBytes_ValidatesSchemaAndReturnsTheDocument()
    {
        GltfReader reader = new GltfReader();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => reader.ReadBytes(Encoding.UTF8.GetBytes("{}"), new GltfReadOptions()),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.EqualTo(
                        "Required JSON member 'asset' is missing at byte 0 and JSON pointer ''."));
            Assert.That(
                reader.ReadBytes(
                    Encoding.UTF8.GetBytes("{\"asset\":{\"version\":\"2.0\"}}"),
                    new GltfReadOptions()).Document,
                Is.Not.Null);
        });
    }

    private static GltfDocument ReadDocument(
        string json,
        bool preserveUnknownMembers,
        GltfReadLimits limits)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        GltfJsonReadResult parsed = new GltfJsonReader().Read(
            bytes,
            limits,
            GltfDuplicateMemberPolicy.Reject);
        return new GltfSchemaReader().ReadDocument(
            parsed,
            limits,
            preserveUnknownMembers,
            new List<IGltfExtensionHandler>());
    }
}
