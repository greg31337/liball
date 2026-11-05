using System.Text;
using System.Text.Json.Nodes;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfConverterDocumentTests
{
    [Test]
    public void ConvertDocument_NormalizesJsonWithoutMutatingSource()
    {
        GltfDocument source = new GltfDocument();
        source.Container = new GltfContainerData
        {
            Format = GltfContainerFormat.Json,
            JsonBytes = Encoding.UTF8.GetBytes("{ \"asset\": { \"version\": \"2.0\" } }")
        };
        source.PropertyJson.UnknownMembers["zeta"] = JsonValue.Create(2);
        source.PropertyJson.UnknownMembers["alpha"] = JsonValue.Create(1);
        GltfConversionOptions options = CreateOptions(
            GltfContainerFormat.Auto,
            GltfResourceMode.Preserve);

        GltfConversionResult first = new GltfConverter().ConvertDocument(
            source,
            options);
        GltfConversionResult second = new GltfConverter().ConvertDocument(
            source,
            options);
        string text = Encoding.UTF8.GetString(first.PrimaryBytes);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(first.Document, Is.Not.SameAs(source));
            Assert.That(first.Document!.Container!.Format,
                Is.EqualTo(GltfContainerFormat.Json));
            Assert.That(first.Document.Container.JsonBytes,
                Is.EqualTo(first.PrimaryBytes));
            Assert.That(first.Document.Container.JsonBytes,
                Is.Not.SameAs(first.PrimaryBytes));
            Assert.That(text,
                Is.EqualTo("{\"asset\":{\"version\":\"2.0\"},\"alpha\":1,\"zeta\":2}"));
            Assert.That(second.PrimaryBytes, Is.EqualTo(first.PrimaryBytes));
            Assert.That(source.Container.JsonBytes,
                Is.EqualTo(Encoding.UTF8.GetBytes(
                    "{ \"asset\": { \"version\": \"2.0\" } }")));
        });
    }

    [Test]
    public void ConvertDocument_RewritesJsonResourcesAsDataUrisWithoutCodecRoundTrips()
    {
        CodecProbe codec = new CodecProbe();
        ResourceProbe resolver = new ResourceProbe();
        GltfDocument source = CreateExternalJsonDocument();
        GltfConversionOptions options = CreateOptions(
            GltfContainerFormat.Json,
            GltfResourceMode.DataUri);
        options.ReadOptions.ResourceResolver = resolver;
        options.ReadOptions.CompressionCodecs.Add(codec);
        options.ReadOptions.ImageCodecs.Add(codec);
        options.WriteOptions.CompressionCodecs.Add(codec);
        options.WriteOptions.ImageCodecs.Add(codec);

        GltfConversionResult result = new GltfConverter().ConvertDocument(
            source,
            options);
        JsonObject json = ParseJson(result.PrimaryBytes);

        Assert.Multiple(() =>
        {
            Assert.That(json["buffers"]![0]!["uri"]!.GetValue<string>(),
                Is.EqualTo("data:application/gltf-buffer;base64,AQIDBA=="));
            Assert.That(json["images"]![0]!["uri"]!.GetValue<string>(),
                Is.EqualTo("data:image/png;base64,iVBORw=="));
            Assert.That(result.Resources.Resources.Select(value => value.Bytes),
                Is.EqualTo(new[]
                {
                    new byte[] { 1, 2, 3, 4 },
                    new byte[] { 137, 80, 78, 71 }
                }));
            Assert.That(result.Manifest.Select(value => value.Mode),
                Is.EqualTo(new[]
                {
                    GltfResourceMode.DataUri,
                    GltfResourceMode.DataUri
                }));
            Assert.That(resolver.Calls, Is.EqualTo(2));
            Assert.That(codec.TotalCalls, Is.Zero);
            Assert.That(source.Buffers[0].Resolved, Is.False);
            Assert.That(source.Buffers[0].Uri, Is.EqualTo("geometry.bin"));
            Assert.That(source.Images[0].Uri, Is.EqualTo("texture.png"));
        });
    }

    [Test]
    public void ConvertDocument_PartitionsJsonBuffersAndRewritesReferences()
    {
        GltfDocument source = CreatePartitionJsonDocument();
        GltfConversionOptions options = CreateOptions(
            GltfContainerFormat.Json,
            GltfResourceMode.Preserve);
        options.PartitionBuffers = true;
        options.MaximumBufferByteLength = 4;

        GltfConversionResult result = new GltfConverter().ConvertDocument(
            source,
            options);
        JsonObject json = ParseJson(result.PrimaryBytes);

        Assert.Multiple(() =>
        {
            Assert.That(result.Document!.Buffers, Has.Count.EqualTo(2));
            Assert.That(json["buffers"]!.AsArray().Select(value =>
                value!["uri"]!.GetValue<string>()),
                Is.EqualTo(new[] { "buffer.bin", "buffer-1.bin" }));
            Assert.That(json["bufferViews"]![0]!["buffer"]!.GetValue<int>(),
                Is.Zero);
            Assert.That(json["bufferViews"]![1]!["buffer"]!.GetValue<int>(),
                Is.EqualTo(1));
            Assert.That(result.Resources.Resources[0].Bytes,
                Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(result.Resources.Resources[1].Bytes,
                Is.EqualTo(new byte[] { 5, 6, 7, 8 }));
            Assert.That(source.Buffers, Has.Count.EqualTo(1));
            Assert.That(source.BufferViews.All(value => value.Buffer == 0),
                Is.True);
        });
    }

    [Test]
    public void ConvertDocument_PacksJsonImagesIntoBufferViewsWithoutTranscoding()
    {
        CodecProbe codec = new CodecProbe();
        GltfDocument source = CreateResolvedJsonDocument();
        GltfConversionOptions options = CreateOptions(
            GltfContainerFormat.Json,
            GltfResourceMode.BufferView);
        options.ReadOptions.ImageCodecs.Add(codec);
        options.WriteOptions.ImageCodecs.Add(codec);

        GltfConversionResult result = new GltfConverter().ConvertDocument(
            source,
            options);
        JsonObject json = ParseJson(result.PrimaryBytes);

        Assert.Multiple(() =>
        {
            Assert.That(json["buffers"]!.AsArray(), Has.Count.EqualTo(2));
            Assert.That(json["buffers"]![1]!["uri"]!.GetValue<string>(),
                Is.EqualTo("data:application/gltf-buffer;base64,iVBORw=="));
            Assert.That(json["images"]![0]!["uri"], Is.Null);
            Assert.That(json["images"]![0]!["bufferView"]!.GetValue<int>(),
                Is.EqualTo(1));
            Assert.That(json["images"]![0]!["mimeType"]!.GetValue<string>(),
                Is.EqualTo(GltfMimeTypes.Png));
            Assert.That(result.Document!.Images[0].HasBufferView, Is.True);
            Assert.That(result.Document.Images[0].ResolvedBytes,
                Is.EqualTo(new byte[] { 137, 80, 78, 71 }));
            Assert.That(codec.TotalCalls, Is.Zero);
        });
    }

    [Test]
    public void ConvertDocument_RewritesGlbWhilePreservingOpaqueChunks()
    {
        CodecProbe codec = new CodecProbe();
        GltfDocument source = CreateOpaqueGlbDocument();
        GltfConversionOptions options = CreateOptions(
            GltfContainerFormat.Auto,
            GltfResourceMode.Preserve);
        options.ReadOptions.CompressionCodecs.Add(codec);
        options.ReadOptions.ImageCodecs.Add(codec);
        options.WriteOptions.CompressionCodecs.Add(codec);
        options.WriteOptions.ImageCodecs.Add(codec);

        GltfConversionResult result = new GltfConverter().ConvertDocument(
            source,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.Document, Is.Not.SameAs(source));
            Assert.That(result.Document!.Container!.Format,
                Is.EqualTo(GltfContainerFormat.Glb));
            Assert.That(result.Document.Container.BinBytes,
                Is.EqualTo(new byte[] { 222, 173, 190, 239, 137, 80, 78, 71 }));
            Assert.That(result.Document.Container.UnknownChunks,
                Has.Count.EqualTo(1));
            Assert.That(result.Document.Container.UnknownChunks[0].Data,
                Is.EqualTo(new byte[] { 7, 6, 5, 4 }));
            Assert.That(codec.TotalCalls, Is.Zero);
            Assert.That(source.Container!.BinBytes,
                Is.EqualTo(new byte[] { 222, 173, 190, 239, 137, 80, 78, 71 }));
            Assert.That(source.Images[0].HasBufferView, Is.True);
        });
    }

    [Test]
    public void ConvertDocument_RepacksGlbBinAndEmbedsOpaqueImageBytes()
    {
        CodecProbe codec = new CodecProbe();
        ResourceProbe resolver = new ResourceProbe();
        GltfDocument source = new GltfDocument();
        source.Container = new GltfContainerData
        {
            Format = GltfContainerFormat.Glb,
            HasBinChunk = true,
            BinBytes = [1, 2, 3, 4]
        };
        source.Buffers.Add(new GltfBuffer
        {
            ByteLength = 4
        });
        source.BufferViews.Add(new GltfBufferView
        {
            Buffer = 0,
            ByteLength = 4
        });
        source.Images.Add(new GltfImage
        {
            Uri = "texture.png"
        });
        GltfConversionOptions options = CreateOptions(
            GltfContainerFormat.Glb,
            GltfResourceMode.GlbBin);
        options.EmbedImages = true;
        options.ReadOptions.ResourceResolver = resolver;
        options.ReadOptions.CompressionCodecs.Add(codec);
        options.ReadOptions.ImageCodecs.Add(codec);
        options.WriteOptions.CompressionCodecs.Add(codec);
        options.WriteOptions.ImageCodecs.Add(codec);

        GltfConversionResult result = new GltfConverter().ConvertDocument(
            source,
            options);
        GltfContainerData container = result.Document!.Container!;
        JsonObject json = ParseJson(container.JsonBytes);

        Assert.Multiple(() =>
        {
            Assert.That(container.BinBytes,
                Is.EqualTo(new byte[] { 1, 2, 3, 4, 137, 80, 78, 71 }));
            Assert.That(json["buffers"]![0]!["uri"], Is.Null);
            Assert.That(json["images"]![0]!["uri"], Is.Null);
            Assert.That(json["images"]![0]!["bufferView"]!.GetValue<int>(),
                Is.EqualTo(1));
            Assert.That(json["images"]![0]!["mimeType"]!.GetValue<string>(),
                Is.EqualTo(GltfMimeTypes.Png));
            Assert.That(result.Resources.Resources, Has.Count.EqualTo(1));
            Assert.That(result.Resources.Resources[0].Bytes,
                Is.EqualTo(new byte[] { 1, 2, 3, 4, 137, 80, 78, 71 }));
            Assert.That(codec.TotalCalls, Is.Zero);
            Assert.That(resolver.Calls, Is.EqualTo(1));
            Assert.That(source.Images[0].Uri, Is.EqualTo("texture.png"));
            Assert.That(source.Container!.BinBytes,
                Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        });
    }

    [Test]
    public void ConvertDocument_DispatchesBothCrossContainerDirections()
    {
        GltfDocument json = new GltfDocument();
        json.Container = new GltfContainerData
        {
            Format = GltfContainerFormat.Json
        };
        GltfConversionOptions toGlb = CreateOptions(
            GltfContainerFormat.Glb,
            GltfResourceMode.GlbBin);
        GltfConversionResult glb = new GltfConverter().ConvertDocument(
            json,
            toGlb);

        GltfConversionOptions toJson = CreateOptions(
            GltfContainerFormat.Json,
            GltfResourceMode.External);
        GltfConversionResult roundTrip = new GltfConverter().ConvertDocument(
            glb.Document!,
            toJson);

        Assert.Multiple(() =>
        {
            Assert.That(glb.Document!.Container!.Format,
                Is.EqualTo(GltfContainerFormat.Glb));
            Assert.That(roundTrip.Document!.Container!.Format,
                Is.EqualTo(GltfContainerFormat.Json));
            Assert.That(Encoding.UTF8.GetString(roundTrip.PrimaryBytes),
                Is.EqualTo("{\"asset\":{\"version\":\"2.0\"}}"));
        });
    }

    [Test]
    public void ConvertDocument_RejectsInputFormatThatConflictsWithProvenance()
    {
        GltfDocument source = new GltfDocument();
        source.Container = new GltfContainerData
        {
            Format = GltfContainerFormat.Glb
        };
        GltfConversionOptions options = CreateOptions(
            GltfContainerFormat.Json,
            GltfResourceMode.Preserve);
        options.InputFormat = GltfContainerFormat.Json;

        Assert.That(
            () => new GltfConverter().ConvertDocument(source, options),
            Throws.TypeOf<GltfWriteException>().With.Message.EqualTo(
                "The requested input format does not match the document container format."));
    }

    private static GltfConversionOptions CreateOptions(
        GltfContainerFormat outputFormat,
        GltfResourceMode mode)
    {
        GltfConversionOptions result = new GltfConversionOptions();
        result.InputFormat = GltfContainerFormat.Auto;
        result.OutputFormat = outputFormat;
        result.ResourceMode = mode;
        result.ReadOptions.ResolveResources = true;
        result.WriteOptions.Validate = false;
        return result;
    }

    private static GltfDocument CreateExternalJsonDocument()
    {
        GltfDocument result = new GltfDocument();
        result.Container = new GltfContainerData
        {
            Format = GltfContainerFormat.Json
        };
        result.Buffers.Add(new GltfBuffer
        {
            Uri = "geometry.bin",
            ByteLength = 4
        });
        result.BufferViews.Add(new GltfBufferView
        {
            Buffer = 0,
            ByteLength = 4
        });
        result.Images.Add(new GltfImage
        {
            Uri = "texture.png"
        });
        return result;
    }

    private static GltfDocument CreateResolvedJsonDocument()
    {
        GltfDocument result = CreateExternalJsonDocument();
        GltfResource bufferResource = new GltfResource
        {
            Kind = GltfResourceKind.Buffer,
            UriKind = GltfUriKind.Relative,
            SourceUri = "geometry.bin",
            EffectiveUri = "geometry.bin",
            MimeType = GltfMimeTypes.GltfBuffer,
            Bytes = [1, 2, 3, 4]
        };
        GltfResource imageResource = new GltfResource
        {
            Kind = GltfResourceKind.Image,
            UriKind = GltfUriKind.Relative,
            SourceUri = "texture.png",
            EffectiveUri = "texture.png",
            MimeType = GltfMimeTypes.Png,
            SourceMimeType = GltfMimeTypes.Png,
            ImageFormat = GltfImageFormat.Png,
            Bytes = [137, 80, 78, 71]
        };
        result.Resources.Resources.Add(bufferResource);
        result.Resources.Resources.Add(imageResource);
        result.Buffers[0].HasResource = true;
        result.Buffers[0].Resource = 0;
        result.Images[0].HasResource = true;
        result.Images[0].Resource = 1;
        return result;
    }

    private static GltfDocument CreatePartitionJsonDocument()
    {
        GltfDocument result = new GltfDocument();
        result.Container = new GltfContainerData
        {
            Format = GltfContainerFormat.Json
        };
        result.Buffers.Add(new GltfBuffer
        {
            Uri = "source.bin",
            ByteLength = 8,
            Resolved = true,
            ResolvedBytes = [1, 2, 3, 4, 5, 6, 7, 8]
        });
        result.BufferViews.Add(new GltfBufferView
        {
            Buffer = 0,
            ByteLength = 4
        });
        result.BufferViews.Add(new GltfBufferView
        {
            Buffer = 0,
            HasByteOffset = true,
            ByteOffset = 4,
            ByteLength = 4
        });
        return result;
    }

    private static GltfDocument CreateOpaqueGlbDocument()
    {
        GltfDocument result = new GltfDocument();
        result.Container = new GltfContainerData
        {
            Format = GltfContainerFormat.Glb,
            HasBinChunk = true,
            BinBytes = [222, 173, 190, 239, 137, 80, 78, 71]
        };
        result.Container.UnknownChunks.Add(new GltfGlbChunk
        {
            Type = 0x12345678U,
            Data = [7, 6, 5, 4]
        });
        result.Buffers.Add(new GltfBuffer
        {
            ByteLength = 8
        });
        result.BufferViews.Add(new GltfBufferView
        {
            Buffer = 0,
            ByteLength = 4
        });
        result.BufferViews.Add(new GltfBufferView
        {
            Buffer = 0,
            HasByteOffset = true,
            ByteOffset = 4,
            ByteLength = 4
        });
        result.Images.Add(new GltfImage
        {
            HasBufferView = true,
            BufferView = 1,
            MimeType = GltfMimeTypes.Png
        });
        return result;
    }

    private static JsonObject ParseJson(byte[] bytes)
    {
        return JsonNode.Parse(Encoding.UTF8.GetString(bytes))!.AsObject();
    }

    private sealed class ResourceProbe : IGltfResourceResolver
    {
        public int Calls;

        public GltfResource ResolveResource(GltfResourceRequest request)
        {
            Calls++;
            if (request.Kind == GltfResourceKind.Buffer)
            {
                return new GltfResource
                {
                    Status = GltfOperationStatus.Success,
                    Kind = GltfResourceKind.Buffer,
                    UriKind = GltfUriKind.Relative,
                    SourceUri = request.SourceUri,
                    EffectiveUri = request.SourceUri,
                    MimeType = GltfMimeTypes.GltfBuffer,
                    Bytes = [1, 2, 3, 4]
                };
            }

            return new GltfResource
            {
                Status = GltfOperationStatus.Success,
                Kind = GltfResourceKind.Image,
                UriKind = GltfUriKind.Relative,
                SourceUri = request.SourceUri,
                EffectiveUri = request.SourceUri,
                MimeType = GltfMimeTypes.Png,
                SourceMimeType = GltfMimeTypes.Png,
                ImageFormat = GltfImageFormat.Png,
                Bytes = [137, 80, 78, 71]
            };
        }
    }

    private sealed class CodecProbe : IGltfCompressionCodec, IGltfImageCodec
    {
        public int TotalCalls;

        public GltfCompressionResult DecodeGeometry(
            GltfCompressionRequest request)
        {
            TotalCalls++;
            return new GltfCompressionResult();
        }

        public GltfCompressionResult EncodeGeometry(
            GltfCompressionRequest request)
        {
            TotalCalls++;
            return new GltfCompressionResult();
        }

        public GltfCompressionResult DecodeMeshoptData(
            GltfCompressionRequest request)
        {
            TotalCalls++;
            return new GltfCompressionResult();
        }

        public GltfCompressionResult EncodeMeshoptData(
            GltfCompressionRequest request)
        {
            TotalCalls++;
            return new GltfCompressionResult();
        }

        public GltfImageInfo InspectImage(GltfImageRequest request)
        {
            TotalCalls++;
            return new GltfImageInfo();
        }

        public GltfImageResult DecodeImage(GltfImageRequest request)
        {
            TotalCalls++;
            return new GltfImageResult();
        }

        public GltfImageResult EncodeImage(GltfImageRequest request)
        {
            TotalCalls++;
            return new GltfImageResult();
        }

        public GltfImageResult TranscodeImage(GltfImageRequest request)
        {
            TotalCalls++;
            return new GltfImageResult();
        }
    }
}
