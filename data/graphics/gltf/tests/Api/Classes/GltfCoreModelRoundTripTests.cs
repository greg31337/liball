using System.Text;
using System.Text.Json.Nodes;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfCoreModelRoundTripTests
{
    [Test]
    public void JsonRoundTrip_PreservesEveryCoreObjectFieldAndVariant()
    {
        byte[] source = CreateCompleteJson();
        GltfReadOptions readOptions = new GltfReadOptions();
        GltfReadResult firstRead = new GltfReader().ReadBytes(source, readOptions);
        GltfWriteOptions writeOptions = CreateWriteOptions(GltfContainerFormat.Json);

        GltfWriteResult written = new GltfWriter().WriteBytes(
            firstRead.Document!,
            writeOptions);
        GltfReadResult secondRead = new GltfReader().ReadBytes(
            written.PrimaryBytes,
            readOptions);

        Assert.Multiple(() =>
        {
            Assert.That(firstRead.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(written.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(secondRead.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(
                JsonNode.DeepEquals(
                    JsonNode.Parse(source),
                    JsonNode.Parse(written.PrimaryBytes)),
                Is.True);
        });
        AssertCompleteModel(firstRead.Document!);
        AssertCompleteModel(secondRead.Document!);
    }

    [Test]
    public void GlbRoundTrip_PreservesCompleteModelAndEmbeddedBuffer()
    {
        byte[] source = CreateCompleteJson();
        GltfConversionOptions conversionOptions = CreateGlbOptions();
        GltfConversionResult converted = new GltfConverter().ConvertBytes(
            source,
            conversionOptions);
        GltfReadResult read = new GltfReader().ReadBytes(
            converted.PrimaryBytes,
            new GltfReadOptions());
        GltfWriteResult json = new GltfWriter().WriteBytes(
            read.Document!,
            CreateWriteOptions(GltfContainerFormat.Json));
        GltfConversionResult repeated = new GltfConverter().ConvertDocument(
            read.Document!,
            conversionOptions);

        JsonNode actualContainerJson = JsonNode.Parse(
            read.Container!.JsonBytes)!;
        Assert.Multiple(() =>
        {
            Assert.That(converted.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(read.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(read.Container, Is.Not.Null);
            Assert.That(read.Container!.Format, Is.EqualTo(GltfContainerFormat.Glb));
            Assert.That(read.Container.HasBinChunk, Is.True);
            Assert.That(read.Document!.Buffers[0].Resolved, Is.True);
            Assert.That(read.Document.Buffers[0].ResolvedBytes, Has.Length.EqualTo(56));
            Assert.That(read.Document.Accessors[0].BufferView, Is.EqualTo(3));
            Assert.That(read.Document.Accessors[0].Sparse!.Indices.BufferView, Is.EqualTo(1));
            Assert.That(read.Document.Accessors[0].Sparse!.Values.BufferView, Is.EqualTo(2));
            Assert.That(read.Document.Accessors[1].BufferView, Is.EqualTo(4));
            Assert.That(read.Document.Images[2].BufferView, Is.Zero);
            Assert.That(
                JsonNode.DeepEquals(actualContainerJson, JsonNode.Parse(json.PrimaryBytes)),
                Is.True);
            Assert.That(repeated.PrimaryBytes, Is.EqualTo(converted.PrimaryBytes));
        });
        AssertCompleteModel(read.Document!);
    }

    private static GltfWriteOptions CreateWriteOptions(GltfContainerFormat format)
    {
        GltfWriteOptions options = new GltfWriteOptions();
        options.Format = format;
        options.Validate = false;
        return options;
    }

    private static GltfConversionOptions CreateGlbOptions()
    {
        GltfConversionOptions options = new GltfConversionOptions();
        options.OutputFormat = GltfContainerFormat.Glb;
        options.ResourceMode = GltfResourceMode.GlbBin;
        options.ReadOptions.ResourceResolver = new ExternalImageResolver();
        options.WriteOptions.Validate = false;
        return options;
    }

    private static void AssertCompleteModel(GltfDocument document)
    {
        string[] semantics = document.Meshes[0].Primitives[0].Attributes
            .Select(attribute => attribute.Semantic)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        GltfPrimitiveMode[] modes = document.Meshes[0].Primitives
            .Select(primitive => primitive.Mode)
            .ToArray();
        GltfMaterial material = document.Materials[0];
        GltfAnimation animation = document.Animations[0];
        GltfAccessorSparse sparse = document.Accessors[0].Sparse!;
        GltfPerspectiveCamera perspective = document.Cameras[0].Perspective!;
        GltfBufferView vertexView = document.BufferViews.Single(view => view.Name == "vertices");

        Assert.Multiple(() =>
        {
            Assert.That(document.Asset.Version, Is.EqualTo("2.0"));
            Assert.That(document.Asset.MinimumVersion, Is.EqualTo("2.0"));
            Assert.That(document.Asset.Generator, Is.EqualTo("core-round-trip"));
            Assert.That(document.Asset.Copyright, Is.EqualTo("owner"));
            Assert.That(document.Accessors, Has.Count.EqualTo(2));
            Assert.That(document.Animations, Has.Count.EqualTo(1));
            Assert.That(document.Buffers, Has.Count.EqualTo(1));
            Assert.That(document.BufferViews, Has.Count.EqualTo(5));
            Assert.That(document.Cameras, Has.Count.EqualTo(3));
            Assert.That(document.Images, Has.Count.EqualTo(3));
            Assert.That(document.Materials, Has.Count.EqualTo(1));
            Assert.That(document.Meshes, Has.Count.EqualTo(1));
            Assert.That(document.Nodes, Has.Count.EqualTo(2));
            Assert.That(document.Samplers, Has.Count.EqualTo(1));
            Assert.That(document.Scenes, Has.Count.EqualTo(1));
            Assert.That(document.Skins, Has.Count.EqualTo(1));
            Assert.That(document.Textures, Has.Count.EqualTo(3));
            Assert.That(document.HasDefaultScene, Is.True);
            Assert.That(document.DefaultScene, Is.Zero);
            Assert.That(document.ExtensionsUsed, Is.EqualTo(new[]
            {
                "EXT_root",
                GltfExtensionNames.KhrMeshQuantization
            }));
            Assert.That(document.ExtensionsRequired, Is.EqualTo(new[]
            {
                GltfExtensionNames.KhrMeshQuantization
            }));
        });

        Assert.Multiple(() =>
        {
            Assert.That(document.Accessors[0].HasBufferView, Is.True);
            Assert.That(document.Accessors[0].HasByteOffset, Is.True);
            Assert.That(document.Accessors[0].HasNormalized, Is.True);
            Assert.That(document.Accessors[0].Maximum, Is.EqualTo(new[] { 1.0, 2.0, 3.0 }));
            Assert.That(document.Accessors[0].Minimum, Is.EqualTo(new[] { -1.0, -2.0, -3.0 }));
            Assert.That(document.Accessors[0].Sparse, Is.Not.Null);
            Assert.That(sparse.Indices.HasByteOffset, Is.True);
            Assert.That(sparse.Values.HasByteOffset, Is.True);
            Assert.That(vertexView.HasByteOffset, Is.True);
            Assert.That(vertexView.HasByteStride, Is.True);
            Assert.That(vertexView.HasTarget, Is.True);
        });

        Assert.Multiple(() =>
        {
            Assert.That(modes, Is.EqualTo(new[]
            {
                GltfPrimitiveMode.Points,
                GltfPrimitiveMode.Lines,
                GltfPrimitiveMode.LineLoop,
                GltfPrimitiveMode.LineStrip,
                GltfPrimitiveMode.Triangles,
                GltfPrimitiveMode.TriangleStrip,
                GltfPrimitiveMode.TriangleFan
            }));
            Assert.That(semantics, Is.EqualTo(new[]
            {
                "COLOR_0",
                "COLOR_1",
                "JOINTS_0",
                "JOINTS_1",
                "NORMAL",
                "POSITION",
                "TANGENT",
                "TEXCOORD_0",
                "TEXCOORD_1",
                "WEIGHTS_0",
                "WEIGHTS_1",
                "_CUSTOM"
            }.OrderBy(value => value, StringComparer.Ordinal)));
            Assert.That(
                document.Meshes[0].Primitives[0].Targets[0].Attributes
                    .Select(attribute => attribute.Semantic)
                    .OrderBy(value => value, StringComparer.Ordinal),
                Is.EqualTo(new[] { "NORMAL", "POSITION", "TANGENT" }));
            Assert.That(document.Meshes[0].Weights, Is.EqualTo(new[] { 0.25, 0.75 }));
            Assert.That(document.Nodes[0].HasMatrix, Is.True);
            Assert.That(document.Nodes[1].HasRotation, Is.True);
            Assert.That(document.Nodes[1].HasScale, Is.True);
            Assert.That(document.Nodes[1].HasTranslation, Is.True);
            Assert.That(document.Nodes[1].Weights, Is.EqualTo(new[] { 0.4, 0.6 }));
        });

        Assert.Multiple(() =>
        {
            Assert.That(material.PbrMetallicRoughness, Is.Not.Null);
            Assert.That(material.PbrMetallicRoughness!.BaseColorTexture, Is.Not.Null);
            Assert.That(material.PbrMetallicRoughness.MetallicRoughnessTexture, Is.Not.Null);
            Assert.That(material.NormalTexture, Is.Not.Null);
            Assert.That(material.OcclusionTexture, Is.Not.Null);
            Assert.That(material.EmissiveTexture, Is.Not.Null);
            Assert.That(material.AlphaMode, Is.EqualTo(GltfAlphaMode.Blend));
            Assert.That(material.AlphaCutoff, Is.EqualTo(0.35));
            Assert.That(material.DoubleSided, Is.True);
            Assert.That(document.Images[0].Uri, Is.EqualTo("textures/external.png"));
            Assert.That(document.Images[0].HasBufferView, Is.False);
            Assert.That(document.Images[1].Uri, Does.StartWith("data:image/png;base64,"));
            Assert.That(document.Images[2].Uri, Is.Null);
            Assert.That(document.Images[2].HasBufferView, Is.True);
            Assert.That(document.Images[2].MimeType, Is.EqualTo(GltfMimeTypes.Png));
        });

        Assert.Multiple(() =>
        {
            Assert.That(document.Cameras[0].Perspective, Is.Not.Null);
            Assert.That(perspective.HasAspectRatio, Is.True);
            Assert.That(perspective.HasFarPlane, Is.True);
            Assert.That(document.Cameras[1].Orthographic, Is.Not.Null);
            Assert.That(document.Cameras[2].Perspective, Is.Not.Null);
            Assert.That(document.Cameras[2].Perspective!.HasFarPlane, Is.False);
            Assert.That(document.Skins[0].HasInverseBindMatrices, Is.True);
            Assert.That(document.Skins[0].HasSkeleton, Is.True);
            Assert.That(document.Skins[0].Joints, Is.EqualTo(new[] { 0, 1 }));
            Assert.That(
                animation.Samplers.Select(sampler => sampler.Interpolation),
                Is.EqualTo(new[]
                {
                    GltfAnimationInterpolation.Step,
                    GltfAnimationInterpolation.Linear,
                    GltfAnimationInterpolation.CubicSpline
                }));
            Assert.That(
                animation.Channels.Select(channel => channel.Target.Path),
                Is.EqualTo(new[]
                {
                    GltfAnimationPath.Translation,
                    GltfAnimationPath.Rotation,
                    GltfAnimationPath.Scale,
                    GltfAnimationPath.Weights
                }));
        });
    }

    private static byte[] CreateCompleteJson()
    {
        string bufferUri = "data:application/gltf-buffer;base64,"
            + Convert.ToBase64String(new byte[64]);
        string json = $$"""
            {
              "asset": {
                "version": "2.0",
                "minVersion": "2.0",
                "generator": "core-round-trip",
                "copyright": "owner",
                "extras": { "kind": "asset" }
              },
              "accessors": [
                {
                  "bufferView": 0,
                  "byteOffset": 0,
                  "componentType": 5126,
                  "normalized": false,
                  "count": 2,
                  "type": "VEC3",
                  "max": [1, 2, 3],
                  "min": [-1, -2, -3],
                  "sparse": {
                    "count": 1,
                    "indices": { "bufferView": 3, "byteOffset": 0, "componentType": 5121 },
                    "values": { "bufferView": 4, "byteOffset": 0 }
                  },
                  "name": "positions"
                },
                {
                  "bufferView": 1,
                  "componentType": 5125,
                  "count": 2,
                  "type": "SCALAR",
                  "name": "indices"
                }
              ],
              "animations": [{
                "channels": [
                  { "sampler": 0, "target": { "node": 1, "path": "translation" } },
                  { "sampler": 1, "target": { "node": 1, "path": "rotation" } },
                  { "sampler": 1, "target": { "node": 1, "path": "scale" } },
                  { "sampler": 2, "target": { "node": 1, "path": "weights" } }
                ],
                "samplers": [
                  { "input": 1, "interpolation": "STEP", "output": 0 },
                  { "input": 1, "interpolation": "LINEAR", "output": 0 },
                  { "input": 1, "interpolation": "CUBICSPLINE", "output": 0 }
                ],
                "name": "all-animation-forms"
              }],
              "buffers": [{ "uri": "{{bufferUri}}", "byteLength": 64, "name": "binary" }],
              "bufferViews": [
                { "buffer": 0, "byteOffset": 0, "byteLength": 24, "byteStride": 12, "target": 34962, "name": "vertices" },
                { "buffer": 0, "byteOffset": 24, "byteLength": 8, "target": 34963, "name": "indices" },
                { "buffer": 0, "byteOffset": 32, "byteLength": 8, "name": "image" },
                { "buffer": 0, "byteOffset": 40, "byteLength": 1, "name": "sparse-indices" },
                { "buffer": 0, "byteOffset": 44, "byteLength": 12, "name": "sparse-values" }
              ],
              "cameras": [
                {
                  "type": "perspective",
                  "perspective": { "aspectRatio": 1.5, "yfov": 1, "zfar": 100, "znear": 0.1 },
                  "name": "perspective"
                },
                {
                  "type": "orthographic",
                  "orthographic": { "xmag": 2, "ymag": 3, "zfar": 10, "znear": 0.01 },
                  "name": "orthographic"
                },
                {
                  "type": "perspective",
                  "perspective": { "yfov": 0.8, "znear": 0.2 },
                  "name": "infinite-perspective"
                }
              ],
              "images": [
                { "uri": "textures/external.png", "name": "external-image" },
                { "uri": "data:image/png;base64,iVBORw==", "mimeType": "image/png", "name": "data-image" },
                { "mimeType": "image/png", "bufferView": 2, "name": "buffer-view-image" }
              ],
              "materials": [{
                "pbrMetallicRoughness": {
                  "baseColorFactor": [0.1, 0.2, 0.3, 0.4],
                  "baseColorTexture": { "index": 0, "texCoord": 1 },
                  "metallicFactor": 0.5,
                  "roughnessFactor": 0.6,
                  "metallicRoughnessTexture": { "index": 1, "texCoord": 2 }
                },
                "normalTexture": { "index": 1, "texCoord": 1, "scale": 0.7 },
                "occlusionTexture": { "index": 2, "texCoord": 2, "strength": 0.8 },
                "emissiveTexture": { "index": 2, "texCoord": 3 },
                "emissiveFactor": [0.2, 0.3, 0.4],
                "alphaMode": "BLEND",
                "alphaCutoff": 0.35,
                "doubleSided": true,
                "name": "material"
              }],
              "meshes": [{
                "primitives": [
                  {
                    "attributes": {
                      "POSITION": 0,
                      "NORMAL": 0,
                      "TANGENT": 0,
                      "TEXCOORD_0": 0,
                      "TEXCOORD_1": 0,
                      "COLOR_0": 0,
                      "COLOR_1": 0,
                      "JOINTS_0": 0,
                      "JOINTS_1": 0,
                      "WEIGHTS_0": 0,
                      "WEIGHTS_1": 0,
                      "_CUSTOM": 0
                    },
                    "indices": 1,
                    "material": 0,
                    "mode": 0,
                    "targets": [{ "POSITION": 0, "NORMAL": 0, "TANGENT": 0 }]
                  },
                  { "attributes": { "POSITION": 0 }, "mode": 1 },
                  { "attributes": { "POSITION": 0 }, "mode": 2 },
                  { "attributes": { "POSITION": 0 }, "mode": 3 },
                  { "attributes": { "POSITION": 0 }, "mode": 4 },
                  { "attributes": { "POSITION": 0 }, "mode": 5 },
                  { "attributes": { "POSITION": 0 }, "mode": 6 }
                ],
                "weights": [0.25, 0.75],
                "name": "all-primitives"
              }],
              "nodes": [
                {
                  "children": [1],
                  "camera": 0,
                  "skin": 0,
                  "mesh": 0,
                  "matrix": [1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 1, 2, 3, 1],
                  "name": "matrix-node"
                },
                {
                  "rotation": [0, 0, 0.7071067811865476, 0.7071067811865476],
                  "scale": [2, 3, 4],
                  "translation": [5, 6, 7],
                  "weights": [0.4, 0.6],
                  "name": "trs-node"
                }
              ],
              "samplers": [{
                "magFilter": 9728,
                "minFilter": 9987,
                "wrapS": 33071,
                "wrapT": 33648,
                "name": "sampler"
              }],
              "scenes": [{ "nodes": [0, 1], "name": "scene" }],
              "skins": [{ "inverseBindMatrices": 0, "skeleton": 0, "joints": [0, 1], "name": "skin" }],
              "textures": [
                { "sampler": 0, "source": 0, "name": "external-texture" },
                { "sampler": 0, "source": 1, "name": "data-texture" },
                { "sampler": 0, "source": 2, "name": "buffer-view-texture" }
              ],
              "scene": 0,
              "extensionsUsed": ["EXT_root", "KHR_mesh_quantization"],
              "extensionsRequired": ["KHR_mesh_quantization"],
              "extensions": { "EXT_root": { "enabled": true } },
              "extras": { "root": true }
            }
            """;
        return Encoding.UTF8.GetBytes(json);
    }

    private class ExternalImageResolver : IGltfResourceResolver
    {
        public GltfResource ResolveResource(GltfResourceRequest request)
        {
            GltfResource resource = new GltfResource();
            resource.Status = GltfOperationStatus.Success;
            resource.Kind = request.Kind;
            if (request.Kind == GltfResourceKind.Buffer)
            {
                resource.UriKind = GltfUriKind.Data;
                resource.SourceUri = request.SourceUri;
                resource.EffectiveUri = request.SourceUri;
                resource.MimeType = GltfMimeTypes.GltfBuffer;
                resource.SourceMimeType = GltfMimeTypes.GltfBuffer;
                resource.Bytes = new byte[64];
                return resource;
            }

            resource.UriKind = GltfUriKind.Relative;
            resource.SourceUri = request.SourceUri;
            resource.EffectiveUri = request.SourceUri;
            resource.MimeType = GltfMimeTypes.Png;
            resource.SourceMimeType = GltfMimeTypes.Png;
            resource.ImageFormat = GltfImageFormat.Png;
            resource.Bytes = new byte[] { 137, 80, 78, 71 };
            return resource;
        }
    }
}
