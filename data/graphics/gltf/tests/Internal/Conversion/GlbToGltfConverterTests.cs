using System.Text;
using System.Text.Json.Nodes;

namespace gltf.Tests.Internal.Conversion;

[TestFixture]
public class GlbToGltfConverterTests
{
    [Test]
    public void ConvertDocumentToGltf_ExtractsBinAndImagesWithMimeAwareNames()
    {
        GltfDocument source = CreateImageGlbDocument();
        GltfConversionOptions options = CreateOptions(
            GltfResourceMode.External,
            false);

        GltfConversionResult result =
            new GlbToGltfConverter().ConvertDocumentToGltf(source, options);
        JsonObject json = ParseJson(result.PrimaryBytes);
        JsonArray buffers = json["buffers"]!.AsArray();
        JsonArray images = json["images"]!.AsArray();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.Document, Is.Not.SameAs(source));
            Assert.That(result.Document!.Container!.Format,
                Is.EqualTo(GltfContainerFormat.Json));
            Assert.That(result.Document.Container.HasBinChunk, Is.False);
            Assert.That(result.Document.Container.JsonBytes,
                Is.EqualTo(result.PrimaryBytes));
            Assert.That(result.Document.Container.JsonBytes,
                Is.Not.SameAs(result.PrimaryBytes));
            Assert.That(buffers[0]!["uri"]!.GetValue<string>(),
                Is.EqualTo("buffer.bin"));
            Assert.That(images[0]!["uri"]!.GetValue<string>(),
                Is.EqualTo("1.png"));
            Assert.That(images[0]!["bufferView"], Is.Null);
            Assert.That(images[0]!["mimeType"]!.GetValue<string>(),
                Is.EqualTo(GltfMimeTypes.Png));
            Assert.That(result.Resources.Resources, Has.Count.EqualTo(2));
            Assert.That(result.Resources.Resources[0].Bytes,
                Is.EqualTo(new byte[] { 1, 2, 3, 0, 9, 8, 7 }));
            Assert.That(result.Resources.Resources[0].EffectiveUri,
                Is.EqualTo("buffer.bin"));
            Assert.That(result.Resources.Resources[1].Bytes,
                Is.EqualTo(new byte[] { 9, 8, 7 }));
            Assert.That(result.Resources.Resources[1].EffectiveUri,
                Is.EqualTo("1.png"));
            Assert.That(result.Manifest.Select(value => value.Mode),
                Is.EqualTo(new[]
                {
                    GltfResourceMode.External,
                    GltfResourceMode.External
                }));
            Assert.That(source.Buffers[0].Uri, Is.Null);
            Assert.That(source.Buffers[0].Resolved, Is.False);
            Assert.That(source.Images[0].Uri, Is.Null);
            Assert.That(source.Images[0].HasBufferView, Is.True);
            Assert.That(source.Container!.HasBinChunk, Is.True);
        });
    }

    [Test]
    public void ConvertDocumentToGltf_EmbedsAllResourcesAsCanonicalDataUris()
    {
        GltfDocument source = CreateImageGlbDocument();
        GltfConversionOptions options = CreateOptions(
            GltfResourceMode.DataUri,
            false);

        GltfConversionResult result =
            new GlbToGltfConverter().ConvertDocumentToGltf(source, options);
        JsonObject json = ParseJson(result.PrimaryBytes);
        string bufferUri = json["buffers"]![0]!["uri"]!.GetValue<string>();
        string imageUri = json["images"]![0]!["uri"]!.GetValue<string>();

        Assert.Multiple(() =>
        {
            Assert.That(bufferUri,
                Is.EqualTo("data:application/gltf-buffer;base64,AQIDAAkIBw=="));
            Assert.That(imageUri,
                Is.EqualTo("data:image/png;base64,CQgH"));
            Assert.That(result.Resources.Resources, Has.Count.EqualTo(2));
            Assert.That(result.Resources.Resources.All(value => value.Embedded),
                Is.True);
            Assert.That(result.Manifest.Select(value => value.Mode),
                Is.EqualTo(new[]
                {
                    GltfResourceMode.DataUri,
                    GltfResourceMode.DataUri
                }));
        });
    }

    [Test]
    public void ConvertDocumentToGltf_CanEmbedImagesWhileExtractingBuffers()
    {
        GltfDocument source = CreateImageGlbDocument();
        GltfConversionOptions options = CreateOptions(
            GltfResourceMode.External,
            true);

        GltfConversionResult result =
            new GlbToGltfConverter().ConvertDocumentToGltf(source, options);
        JsonObject json = ParseJson(result.PrimaryBytes);

        Assert.Multiple(() =>
        {
            Assert.That(json["buffers"]![0]!["uri"]!.GetValue<string>(),
                Is.EqualTo("buffer.bin"));
            Assert.That(json["images"]![0]!["uri"]!.GetValue<string>(),
                Is.EqualTo("data:image/png;base64,CQgH"));
            Assert.That(result.Manifest.Select(value => value.Mode),
                Is.EqualTo(new[]
                {
                    GltfResourceMode.External,
                    GltfResourceMode.DataUri
                }));
        });
    }

    [Test]
    public void ConvertDocumentToGltf_PartitionsAtWholeViewBoundariesAndRewritesBuffers()
    {
        GltfDocument source = CreatePartitionedGlbDocument();
        GltfConversionOptions options = CreateOptions(
            GltfResourceMode.External,
            false);
        options.PartitionBuffers = true;
        options.MaximumBufferByteLength = 8;

        GltfConversionResult result =
            new GlbToGltfConverter().ConvertDocumentToGltf(source, options);
        JsonObject json = ParseJson(result.PrimaryBytes);
        JsonArray buffers = json["buffers"]!.AsArray();
        JsonArray views = json["bufferViews"]!.AsArray();

        Assert.Multiple(() =>
        {
            Assert.That(result.Document!.Buffers, Has.Count.EqualTo(2));
            Assert.That(result.Document.Buffers[0].ResolvedBytes,
                Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }));
            Assert.That(result.Document.Buffers[1].ResolvedBytes,
                Is.EqualTo(new byte[] { 9, 10, 11, 12 }));
            Assert.That(buffers.Select(value =>
                value!["uri"]!.GetValue<string>()),
                Is.EqualTo(new[] { "buffer.bin", "buffer-1.bin" }));
            Assert.That(views[0]!["buffer"]!.GetValue<int>(), Is.Zero);
            Assert.That(views[1]!["buffer"]!.GetValue<int>(), Is.Zero);
            Assert.That(views[1]!["byteOffset"]!.GetValue<int>(), Is.EqualTo(4));
            Assert.That(views[2]!["buffer"]!.GetValue<int>(), Is.EqualTo(1));
            Assert.That(views[2]!["byteOffset"], Is.Null);
            Assert.That(result.Resources.Resources, Has.Count.EqualTo(2));
            Assert.That(source.Buffers, Has.Count.EqualTo(1));
            Assert.That(source.BufferViews.All(value => value.Buffer == 0), Is.True);
        });
    }

    [Test]
    public void ConvertDocumentToGltf_UsesJpegExtensionAndStableCollisionSuffixes()
    {
        GltfDocument source = CreateImageGlbDocument();
        source.Images.Add(new GltfImage
        {
            HasBufferView = true,
            BufferView = 1,
            MimeType = GltfMimeTypes.Jpeg
        });
        GltfConversionOptions options = CreateOptions(
            GltfResourceMode.External,
            false);

        GltfConversionResult first =
            new GlbToGltfConverter().ConvertDocumentToGltf(source, options);
        GltfConversionResult second =
            new GlbToGltfConverter().ConvertDocumentToGltf(source, options);

        Assert.Multiple(() =>
        {
            Assert.That(first.Document!.Images.Select(value => value.Uri),
                Is.EqualTo(new[] { "1.png", "1.jpg" }));
            Assert.That(second.PrimaryBytes, Is.EqualTo(first.PrimaryBytes));
            Assert.That(second.Manifest.Select(value => value.Uri),
                Is.EqualTo(first.Manifest.Select(value => value.Uri)));
        });
    }

    [Test]
    public void ConvertDocumentToGltf_RejectsRequiredExtensionBeforeResolvingExternalImage()
    {
        CountingResolver resolver = new CountingResolver();
        GltfDocument source = CreateImageGlbDocument();
        source.Images[0].Uri = "external.png";
        source.Images[0].HasBufferView = false;
        source.Images[0].BufferView = GltfConstants.AbsentIndex;
        source.ExtensionsUsed.Add("ACME_required");
        source.ExtensionsRequired.Add("ACME_required");
        GltfConversionOptions options = CreateOptions(
            GltfResourceMode.External,
            false);
        options.ReadOptions.ResourceResolver = resolver;

        Assert.That(
            () => new GlbToGltfConverter().ConvertDocumentToGltf(
                source,
                options),
            Throws.TypeOf<GltfUnsupportedFeatureException>()
                .With.Message.Contains("no registered metadata support"));
        Assert.That(resolver.Calls, Is.Zero);
    }

    [TestCase(GltfContainerFormat.Glb, GltfResourceMode.External,
        "The GLB-to-glTF stage requires JSON glTF output.")]
    [TestCase(GltfContainerFormat.Json, GltfResourceMode.BufferView,
        "The GLB-to-glTF stage requires Preserve, External, or DataUri resource mode.")]
    public void ConvertDocumentToGltf_RejectsImpossibleOutputPolicies(
        GltfContainerFormat outputFormat,
        GltfResourceMode mode,
        string message)
    {
        GltfDocument source = CreateImageGlbDocument();
        GltfConversionOptions options = CreateOptions(mode, false);
        options.OutputFormat = outputFormat;

        Assert.That(
            () => new GlbToGltfConverter().ConvertDocumentToGltf(
                source,
                options),
            Throws.TypeOf<GltfWriteException>().With.Message.EqualTo(message));
    }

    private static GltfDocument CreateImageGlbDocument()
    {
        GltfDocument result = new GltfDocument();
        result.Container = new GltfContainerData
        {
            Format = GltfContainerFormat.Glb,
            HasBinChunk = true,
            BinBytes = [1, 2, 3, 0, 9, 8, 7, 0]
        };
        result.Buffers.Add(new GltfBuffer
        {
            ByteLength = 7
        });
        result.BufferViews.Add(new GltfBufferView
        {
            Buffer = 0,
            ByteLength = 3
        });
        result.BufferViews.Add(new GltfBufferView
        {
            Buffer = 0,
            HasByteOffset = true,
            ByteOffset = 4,
            ByteLength = 3
        });
        result.Images.Add(new GltfImage
        {
            HasBufferView = true,
            BufferView = 1,
            MimeType = GltfMimeTypes.Png
        });
        return result;
    }

    private static GltfDocument CreatePartitionedGlbDocument()
    {
        GltfDocument result = new GltfDocument();
        result.Container = new GltfContainerData
        {
            Format = GltfContainerFormat.Glb,
            HasBinChunk = true,
            BinBytes = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]
        };
        result.Buffers.Add(new GltfBuffer
        {
            ByteLength = 12
        });
        int index = 0;
        while (index < 3)
        {
            result.BufferViews.Add(new GltfBufferView
            {
                Buffer = 0,
                HasByteOffset = index > 0,
                ByteOffset = index * 4,
                ByteLength = 4
            });
            index++;
        }

        return result;
    }

    private static GltfConversionOptions CreateOptions(
        GltfResourceMode mode,
        bool embedImages)
    {
        GltfConversionOptions result = new GltfConversionOptions();
        result.InputFormat = GltfContainerFormat.Glb;
        result.OutputFormat = GltfContainerFormat.Json;
        result.ResourceMode = mode;
        result.EmbedImages = embedImages;
        result.ReadOptions.ResolveResources = true;
        result.WriteOptions.Validate = false;
        return result;
    }

    private static JsonObject ParseJson(byte[] bytes)
    {
        return JsonNode.Parse(Encoding.UTF8.GetString(bytes))!.AsObject();
    }

    private sealed class CountingResolver : IGltfResourceResolver
    {
        public int Calls;

        public GltfResource ResolveResource(GltfResourceRequest request)
        {
            Calls++;
            return new GltfResource
            {
                Kind = request.Kind,
                Bytes = [1]
            };
        }
    }
}
