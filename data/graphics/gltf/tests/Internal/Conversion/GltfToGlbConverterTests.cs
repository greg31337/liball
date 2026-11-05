using System.Text;
using System.Text.Json.Nodes;

namespace gltf.Tests.Internal.Conversion;

[TestFixture]
public class GltfToGlbConverterTests
{
    [Test]
    public void ConvertDocumentToGlb_ResolvesConsolidatesAndEmbedsImages()
    {
        ConversionResourceResolver resolver = new ConversionResourceResolver();
        GltfDocument source = CreateUnresolvedDocument(true);
        GltfConversionOptions options = CreateOptions(resolver, true);

        GltfConversionResult result =
            new GltfToGlbConverter().ConvertDocumentToGlb(source, options);
        GltfContainerData container = ReadContainer(result.PrimaryBytes);
        JsonObject json = ParseJson(container);
        JsonArray buffers = json["buffers"]!.AsArray();
        JsonArray views = json["bufferViews"]!.AsArray();
        JsonArray images = json["images"]!.AsArray();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.PrimaryBytes, Is.Not.Empty);
            Assert.That(result.Document, Is.Not.SameAs(source));
            Assert.That(result.Document!.Buffers, Has.Count.EqualTo(1));
            Assert.That(result.Document.Buffers[0].Uri, Is.Null);
            Assert.That(result.Document.Buffers[0].ByteLength, Is.EqualTo(6));
            Assert.That(result.Document.Buffers[0].ResolvedBytes,
                Is.EqualTo(new byte[] { 1, 2, 3, 0, 9, 8 }));
            Assert.That(container.Format, Is.EqualTo(GltfContainerFormat.Glb));
            Assert.That(container.BinBytes,
                Is.EqualTo(new byte[] { 1, 2, 3, 0, 9, 8, 0, 0 }));
            Assert.That(buffers, Has.Count.EqualTo(1));
            Assert.That(buffers[0]!["uri"], Is.Null);
            Assert.That(buffers[0]!["byteLength"]!.GetValue<int>(), Is.EqualTo(6));
            Assert.That(views, Has.Count.EqualTo(2));
            Assert.That(views[0]!["byteOffset"], Is.Null);
            Assert.That(views[0]!["byteLength"]!.GetValue<int>(), Is.EqualTo(3));
            Assert.That(views[1]!["byteOffset"]!.GetValue<int>(), Is.EqualTo(4));
            Assert.That(views[1]!["byteLength"]!.GetValue<int>(), Is.EqualTo(2));
            Assert.That(images, Has.Count.EqualTo(2));
            Assert.That(images[0]!["uri"], Is.Null);
            Assert.That(images[0]!["bufferView"]!.GetValue<int>(), Is.EqualTo(1));
            Assert.That(images[1]!["bufferView"]!.GetValue<int>(), Is.EqualTo(1));
            Assert.That(images[0]!["mimeType"]!.GetValue<string>(),
                Is.EqualTo(GltfMimeTypes.Png));
            Assert.That(result.Resources.Resources, Has.Count.EqualTo(1));
            Assert.That(result.Resources.Resources[0].Embedded, Is.True);
            Assert.That(resolver.Calls, Is.EqualTo(2));
            Assert.That(source.Buffers[0].Resolved, Is.False);
            Assert.That(source.Buffers[0].Uri, Is.EqualTo("geometry.bin"));
            Assert.That(source.Images[0].Uri, Is.EqualTo("image.png"));
            Assert.That(source.Images[0].HasBufferView, Is.False);
            Assert.That(source.Resources.Resources, Is.Empty);
        });
    }

    [Test]
    public void ConvertDocumentToGlb_RetainsExternalImageUrisWhenNotEmbedded()
    {
        ConversionResourceResolver resolver = new ConversionResourceResolver();
        GltfDocument source = CreateUnresolvedDocument(false);
        GltfConversionOptions options = CreateOptions(resolver, false);

        GltfConversionResult result =
            new GltfToGlbConverter().ConvertDocumentToGlb(source, options);
        JsonObject json = ParseJson(ReadContainer(result.PrimaryBytes));
        JsonArray buffers = json["buffers"]!.AsArray();
        JsonArray images = json["images"]!.AsArray();

        Assert.Multiple(() =>
        {
            Assert.That(buffers[0]!["uri"], Is.Null);
            Assert.That(buffers[0]!["byteLength"]!.GetValue<int>(), Is.EqualTo(3));
            Assert.That(images[0]!["uri"]!.GetValue<string>(),
                Is.EqualTo("image.png"));
            Assert.That(images[0]!["bufferView"], Is.Null);
            Assert.That(result.Document!.Images[0].Uri, Is.EqualTo("image.png"));
            Assert.That(result.Document.Images[0].HasBufferView, Is.False);
            Assert.That(result.Resources.Resources, Has.Count.EqualTo(2));
            Assert.That(result.Resources.Resources[0].Kind,
                Is.EqualTo(GltfResourceKind.Buffer));
            Assert.That(result.Resources.Resources[1].Kind,
                Is.EqualTo(GltfResourceKind.Image));
            Assert.That(result.Resources.Resources[1].SourceUri,
                Is.EqualTo("image.png"));
        });
    }

    [Test]
    public void ConvertDocumentToGlb_RewritesTypedMeshoptFallbackMetadata()
    {
        GltfDocument source = CreateMeshoptFallbackDocument();
        GltfConversionOptions options = CreateOptions(null, false);

        GltfConversionResult result =
            new GltfToGlbConverter().ConvertDocumentToGlb(source, options);
        GltfDocument output = result.Document!;
        GltfTypedExtensionData outputTyped = output.BufferViews[0]
            .PropertyJson.TypedExtensions[
                GltfExtensionNames.KhrMeshoptCompression];
        GltfMeshoptCompressionBufferViewExtension outputMeshopt =
            outputTyped.KhrMeshoptCompressionBufferView!;
        JsonObject json = ParseJson(ReadContainer(result.PrimaryBytes));
        JsonObject extension = json["bufferViews"]![0]!["extensions"]![
            GltfExtensionNames.KhrMeshoptCompression]!.AsObject();
        GltfMeshoptCompressionBufferViewExtension sourceMeshopt = source.BufferViews[0]
            .PropertyJson.TypedExtensions[
                GltfExtensionNames.KhrMeshoptCompression]
            .KhrMeshoptCompressionBufferView!;

        Assert.Multiple(() =>
        {
            Assert.That(output.Buffers, Has.Count.EqualTo(1));
            Assert.That(output.Buffers[0].ResolvedBytes,
                Is.EqualTo(new byte[] { 1, 2, 3, 0, 4, 5 }));
            Assert.That(outputMeshopt.Buffer, Is.Zero);
            Assert.That(outputMeshopt.ByteOffset, Is.EqualTo(4));
            Assert.That(outputMeshopt.HasByteOffset, Is.True);
            Assert.That(extension["buffer"]!.GetValue<int>(), Is.Zero);
            Assert.That(extension["byteOffset"]!.GetValue<int>(), Is.EqualTo(4));
            Assert.That(sourceMeshopt.Buffer, Is.EqualTo(1));
            Assert.That(sourceMeshopt.ByteOffset, Is.Zero);
            Assert.That(source.Buffers, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void ConvertDocumentToGlb_IsByteDeterministic()
    {
        GltfDocument source = CreateUnresolvedDocument(false);
        GltfConversionOptions firstOptions = CreateOptions(
            new ConversionResourceResolver(),
            true);
        GltfConversionOptions secondOptions = CreateOptions(
            new ConversionResourceResolver(),
            true);

        GltfConversionResult first =
            new GltfToGlbConverter().ConvertDocumentToGlb(
                source,
                firstOptions);
        GltfConversionResult second =
            new GltfToGlbConverter().ConvertDocumentToGlb(
                source,
                secondOptions);

        Assert.Multiple(() =>
        {
            Assert.That(second.PrimaryBytes, Is.EqualTo(first.PrimaryBytes));
            Assert.That(second.Resources.Resources.Select(value => value.Bytes),
                Is.EqualTo(first.Resources.Resources.Select(value => value.Bytes)));
        });
    }

    [Test]
    public void ConvertDocumentToGlb_RejectsMissingRequiredExtensionBeforeResolution()
    {
        ConversionResourceResolver resolver = new ConversionResourceResolver();
        GltfDocument source = CreateUnresolvedDocument(false);
        source.ExtensionsUsed.Add("ACME_required");
        source.ExtensionsRequired.Add("ACME_required");
        GltfConversionOptions options = CreateOptions(resolver, false);

        Assert.That(
            () => new GltfToGlbConverter().ConvertDocumentToGlb(
                source,
                options),
            Throws.TypeOf<GltfUnsupportedFeatureException>()
                .With.Message.Contains("no registered metadata support"));
        Assert.That(resolver.Calls, Is.Zero);
    }

    [TestCase(GltfContainerFormat.Json, GltfResourceMode.GlbBin,
        "The glTF-to-GLB stage requires GLB output.")]
    [TestCase(GltfContainerFormat.Glb, GltfResourceMode.External,
        "The glTF-to-GLB stage requires Preserve or GlbBin resource mode.")]
    public void ConvertDocumentToGlb_RejectsImpossibleOutputPolicies(
        GltfContainerFormat outputFormat,
        GltfResourceMode mode,
        string message)
    {
        GltfDocument source = new GltfDocument();
        GltfConversionOptions options = CreateOptions(null, false);
        options.OutputFormat = outputFormat;
        options.ResourceMode = mode;

        Assert.That(
            () => new GltfToGlbConverter().ConvertDocumentToGlb(
                source,
                options),
            Throws.TypeOf<GltfWriteException>().With.Message.EqualTo(message));
    }

    private static GltfDocument CreateUnresolvedDocument(bool duplicateImage)
    {
        GltfDocument result = new GltfDocument();
        result.Buffers.Add(new GltfBuffer
        {
            Uri = "geometry.bin",
            ByteLength = 3
        });
        result.BufferViews.Add(new GltfBufferView
        {
            Buffer = 0,
            ByteLength = 3
        });
        result.Images.Add(new GltfImage
        {
            Uri = "image.png"
        });
        if (duplicateImage)
        {
            result.Images.Add(new GltfImage
            {
                Uri = "image.png"
            });
        }

        return result;
    }

    private static GltfDocument CreateMeshoptFallbackDocument()
    {
        GltfDocument result = new GltfDocument();
        result.ExtensionsUsed.Add(GltfExtensionNames.KhrMeshoptCompression);
        result.Resources.Resources.Add(CreateResolvedResource(
            GltfResourceKind.Buffer,
            "fallback.bin",
            GltfMimeTypes.GltfBuffer,
            [1, 2, 3]));
        result.Resources.Resources.Add(CreateResolvedResource(
            GltfResourceKind.Buffer,
            "compressed.bin",
            GltfMimeTypes.GltfBuffer,
            [4, 5]));
        result.Buffers.Add(CreateResolvedBuffer("fallback.bin", [1, 2, 3], 0));
        result.Buffers.Add(CreateResolvedBuffer("compressed.bin", [4, 5], 1));
        GltfBufferView view = new GltfBufferView
        {
            Buffer = 0,
            ByteLength = 3
        };
        JsonObject extension = new JsonObject
        {
            ["buffer"] = 1,
            ["byteOffset"] = 0,
            ["byteLength"] = 2,
            ["byteStride"] = 1,
            ["count"] = 3,
            ["mode"] = "ATTRIBUTES"
        };
        view.PropertyJson.Extensions[
            GltfExtensionNames.KhrMeshoptCompression] = extension;
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.KhrMeshoptCompressionBufferView =
            new GltfMeshoptCompressionBufferViewExtension
            {
                Buffer = 1,
                HasByteOffset = true,
                ByteOffset = 0,
                ByteLength = 2,
                ByteStride = 1,
                Count = 3,
                Mode = "ATTRIBUTES"
            };
        view.PropertyJson.TypedExtensions[
            GltfExtensionNames.KhrMeshoptCompression] = typed;
        result.BufferViews.Add(view);
        return result;
    }

    private static GltfBuffer CreateResolvedBuffer(
        string uri,
        byte[] bytes,
        int resource)
    {
        return new GltfBuffer
        {
            Uri = uri,
            ByteLength = bytes.Length,
            Resolved = true,
            ResolvedBytes = bytes,
            HasResource = true,
            Resource = resource
        };
    }

    private static GltfResource CreateResolvedResource(
        GltfResourceKind kind,
        string uri,
        string mimeType,
        byte[] bytes)
    {
        return new GltfResource
        {
            Kind = kind,
            UriKind = GltfUriKind.Relative,
            SourceUri = uri,
            EffectiveUri = uri,
            MimeType = mimeType,
            Bytes = bytes
        };
    }

    private static GltfConversionOptions CreateOptions(
        IGltfResourceResolver? resolver,
        bool embedImages)
    {
        GltfConversionOptions result = new GltfConversionOptions();
        result.InputFormat = GltfContainerFormat.Json;
        result.OutputFormat = GltfContainerFormat.Glb;
        result.ResourceMode = GltfResourceMode.GlbBin;
        result.EmbedImages = embedImages;
        result.ReadOptions.ResolveResources = true;
        result.ReadOptions.ResourceResolver = resolver;
        result.WriteOptions.Validate = false;
        return result;
    }

    private static GltfContainerData ReadContainer(byte[] bytes)
    {
        GlbReader reader = new GlbReader();
        GlbHeader header = reader.ReadHeader(bytes, bytes.Length);
        return reader.ReadChunks(bytes, header, false);
    }

    private static JsonObject ParseJson(GltfContainerData container)
    {
        return JsonNode.Parse(Encoding.UTF8.GetString(container.JsonBytes))!
            .AsObject();
    }

    private sealed class ConversionResourceResolver : IGltfResourceResolver
    {
        public int Calls;

        public GltfResource ResolveResource(GltfResourceRequest request)
        {
            Calls++;
            if (request.Kind == GltfResourceKind.Buffer)
            {
                return CreateResolvedResource(
                    GltfResourceKind.Buffer,
                    request.SourceUri ?? "",
                    GltfMimeTypes.GltfBuffer,
                    [1, 2, 3]);
            }

            GltfResource result = CreateResolvedResource(
                GltfResourceKind.Image,
                request.SourceUri ?? "",
                GltfMimeTypes.Png,
                [9, 8]);
            result.SourceMimeType = GltfMimeTypes.Png;
            result.ImageFormat = GltfImageFormat.Png;
            return result;
        }
    }
}
