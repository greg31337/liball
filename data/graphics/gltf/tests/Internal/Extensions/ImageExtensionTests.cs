using System.Text;
using System.Text.Json.Nodes;

namespace gltf.Tests.Internal.Extensions;

[TestFixture]
public class ImageExtensionTests
{
    [TestCase(GltfExtensionNames.KhrTextureBasisu)]
    [TestCase(GltfExtensionNames.ExtTextureWebp)]
    public void Metadata_ReadsWritesAndRemapsImageSourceWithoutMutatingInput(
        string extension)
    {
        JsonObject json = new JsonObject
        {
            ["source"] = 2,
            ["future"] = "kept"
        };
        GltfExtensionContext context = new GltfExtensionContext
        {
            Name = extension,
            ObjectLocation = "texture",
            HasObjectIndex = true,
            ObjectIndex = 1,
            Json = json
        };
        GltfExtensionHandler handler = new GltfExtensionHandler();

        GltfExtensionResult read = handler.ReadExtension(context);
        context.TypedData = read.TypedData;
        GltfExtensionResult written = handler.WriteExtension(context);
        GltfIndexRemap remap = new GltfIndexRemap();
        remap.Images[2] = 5;
        GltfExtensionResult remapped = handler.RemapExtensionReferences(context, remap);
        int originalSource = GetSource(read.TypedData!, extension);

        Assert.Multiple(() =>
        {
            Assert.That(read.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(originalSource, Is.EqualTo(2));
            Assert.That(written.Json!["source"]!.GetValue<int>(), Is.EqualTo(2));
            Assert.That(written.Json["future"]!.GetValue<string>(), Is.EqualTo("kept"));
            Assert.That(remapped.Json!["source"]!.GetValue<int>(), Is.EqualTo(5));
            Assert.That(remapped.References.Single().TargetCollection, Is.EqualTo("images"));
            Assert.That(GetSource(read.TypedData!, extension), Is.EqualTo(2));
        });
    }

    [TestCase(GltfExtensionNames.KhrTextureBasisu, GltfMimeTypes.Ktx2)]
    [TestCase(GltfExtensionNames.ExtTextureWebp, GltfMimeTypes.WebP)]
    public void Validation_RequiresFallbackOrRequiredDeclarationAndExpectedImageFormat(
        string extension,
        string mimeType)
    {
        GltfDocument document = new GltfDocument();
        GltfImage image = new GltfImage
        {
            Uri = "image.bin",
            MimeType = GltfMimeTypes.Png,
            Resolved = true,
            ResolvedBytes = CreatePngSignature()
        };
        document.Images.Add(image);
        GltfTexture texture = new GltfTexture();
        document.Textures.Add(texture);
        GltfExtensionContext context = CreateValidationContext(document, extension, 0);

        GltfExtensionResult invalid = new GltfExtensionHandler().ValidateExtension(context);
        document.ExtensionsRequired.Add(extension);
        image.MimeType = mimeType;
        image.ResolvedMimeType = mimeType;
        image.ResolvedBytes = CreateExtensionSignature(extension);
        GltfExtensionResult valid = new GltfExtensionHandler().ValidateExtension(context);

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Diagnostics.Any(issue =>
                issue.Code == GltfIssueCodes.TextureInvalidImageMimeType), Is.True);
            Assert.That(invalid.Diagnostics.Any(issue =>
                issue.JsonPointer == "/extensionsRequired"), Is.True);
            Assert.That(valid.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(valid.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void Validation_RejectsNonCoreFallbackImageFormats()
    {
        GltfDocument document = new GltfDocument();
        document.Images.Add(new GltfImage
        {
            Uri = "fallback.ktx2",
            MimeType = GltfMimeTypes.Ktx2,
            Resolved = true,
            ResolvedBytes = CreateExtensionSignature(GltfExtensionNames.KhrTextureBasisu)
        });
        document.Images.Add(new GltfImage
        {
            Uri = "extension.ktx2",
            MimeType = GltfMimeTypes.Ktx2,
            Resolved = true,
            ResolvedBytes = CreateExtensionSignature(GltfExtensionNames.KhrTextureBasisu)
        });
        GltfTexture texture = new GltfTexture { HasSource = true, Source = 0 };
        document.Textures.Add(texture);

        GltfExtensionResult result = new GltfExtensionHandler().ValidateExtension(
            CreateValidationContext(
                document,
                GltfExtensionNames.KhrTextureBasisu,
                1));

        Assert.That(result.Diagnostics.Any(issue =>
            issue.Code == GltfIssueCodes.TextureInvalidImageMimeType
            && issue.JsonPointer == "/textures/0/source"), Is.True);
    }

    [TestCase(GltfExtensionNames.KhrTextureBasisu, GltfImageFormat.Ktx2)]
    [TestCase(GltfExtensionNames.ExtTextureWebp, GltfImageFormat.WebP)]
    public void Reader_SelectsAndDecodesRequiredExtensionSourceThroughCodec(
        string extension,
        GltfImageFormat format)
    {
        byte[] encoded = CreateExtensionSignature(extension);
        ProbeImageCodec codec = new ProbeImageCodec
        {
            RecognizedFormat = format,
            Output = new byte[] { 8, 9, 10 }
        };
        GltfReadOptions options = new GltfReadOptions
        {
            ResolveResources = true,
            Validate = true
        };
        options.ImageCodecs.Add(codec);

        GltfReadResult read = new GltfReader().ReadBytes(
            Encoding.UTF8.GetBytes(CreateRequiredDocumentJson(extension, encoded)),
            options);
        GltfTexture texture = read.Document!.Textures[0];
        codec.Output[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(read.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(read.Validation.IsValid, Is.True);
            Assert.That(read.Validation.Issues.Any(issue =>
                issue.Code == GltfIssueCodes.ImageCodecUnavailable), Is.False);
            Assert.That(codec.DecodeCalls, Is.EqualTo(1));
            Assert.That(codec.LastRequest!.Codec, Is.EqualTo(extension));
            Assert.That(codec.LastRequest.HasTexture, Is.True);
            Assert.That(codec.LastRequest.Texture, Is.Zero);
            Assert.That(codec.LastRequest.HasImage, Is.True);
            Assert.That(codec.LastRequest.Image, Is.Zero);
            Assert.That(codec.LastRequest.SourceFormat, Is.EqualTo(format));
            Assert.That(codec.LastRequest.Bytes, Is.EqualTo(encoded));
            Assert.That(texture.HasSelectedSource, Is.True);
            Assert.That(texture.SelectedSource, Is.Zero);
            Assert.That(texture.DecodedImage!.Bytes, Is.EqualTo(new byte[] { 8, 9, 10 }));
            Assert.That(texture.DecodedImage.Bytes, Is.Not.SameAs(codec.Output));
        });
    }

    [TestCase(GltfExtensionNames.KhrTextureBasisu, GltfImageFormat.Ktx2)]
    [TestCase(GltfExtensionNames.ExtTextureWebp, GltfImageFormat.WebP)]
    public void Reader_FailedImageDecodeDoesNotSelectOrMutateCodecData(
        string extension,
        GltfImageFormat format)
    {
        byte[] encoded = CreateExtensionSignature(extension);
        GltfValidationIssue codecIssue = new GltfValidationIssue
        {
            Code = "TEST_IMAGE_FAILURE",
            Message = "The test image codec failed."
        };
        ProbeImageCodec codec = new ProbeImageCodec
        {
            Status = GltfOperationStatus.Failed,
            RecognizedFormat = format,
            Output = new byte[] { 8, 9, 10 },
            MutateRequestBytes = true
        };
        codec.Diagnostics.Add(codecIssue);
        GltfReadOptions options = new GltfReadOptions { ResolveResources = true };
        options.ImageCodecs.Add(codec);

        GltfReadResult read = new GltfReader().ReadBytes(
            Encoding.UTF8.GetBytes(CreateRequiredDocumentJson(extension, encoded)),
            options);
        GltfTexture texture = read.Document!.Textures[0];

        Assert.Multiple(() =>
        {
            Assert.That(read.Status, Is.EqualTo(GltfOperationStatus.Failed));
            Assert.That(texture.HasSelectedSource, Is.False);
            Assert.That(texture.DecodedImage, Is.Null);
            Assert.That(read.Document.Images[0].ResolvedBytes, Is.EqualTo(encoded));
            Assert.That(codec.Output, Is.EqualTo(new byte[] { 8, 9, 10 }));
            Assert.That(codecIssue.JsonPointer, Is.Empty);
            Assert.That(read.Diagnostics.Single(issue =>
                issue.Code == "TEST_IMAGE_FAILURE").JsonPointer,
                Is.EqualTo("/textures/0/extensions/" + extension + "/source"));
            Assert.That(read.Diagnostics.Single(issue =>
                issue.Code == "TEST_IMAGE_FAILURE"), Is.Not.SameAs(codecIssue));
        });
    }

    [Test]
    public void Reader_UsesCoreFallbackWhenNoImageCodecIsRegistered()
    {
        const string json = "{\"asset\":{\"version\":\"2.0\"},"
            + "\"extensionsUsed\":[\"EXT_texture_webp\"],"
            + "\"images\":[{\"uri\":\"fallback.png\"},{\"uri\":\"extension.webp\"}],"
            + "\"textures\":[{\"source\":0,\"extensions\":{\"EXT_texture_webp\":{"
            + "\"source\":1}}}]}";

        GltfReadResult read = new GltfReader().ReadBytes(
            Encoding.UTF8.GetBytes(json),
            new GltfReadOptions());
        GltfTexture texture = read.Document!.Textures[0];

        Assert.Multiple(() =>
        {
            Assert.That(read.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(texture.HasSelectedSource, Is.True);
            Assert.That(texture.SelectedSource, Is.Zero);
            Assert.That(texture.DecodedImage, Is.Null);
        });
    }

    [Test]
    public void Dispatcher_TriesUnavailableCodecsAndBoundsEveryOperation()
    {
        ProbeImageCodec unavailable = new ProbeImageCodec
        {
            Status = GltfOperationStatus.Unavailable
        };
        ProbeImageCodec success = new ProbeImageCodec
        {
            RecognizedFormat = GltfImageFormat.WebP,
            Output = new byte[] { 1, 2, 3 }
        };
        GltfImageRequest request = new GltfImageRequest
        {
            Codec = GltfExtensionNames.ExtTextureWebp,
            Bytes = CreateExtensionSignature(GltfExtensionNames.ExtTextureWebp),
            SourceFormat = GltfImageFormat.WebP,
            MaximumOutputByteLength = 3
        };
        ImageCodecDispatcher dispatcher = new ImageCodecDispatcher();

        GltfImageInfo inspected = dispatcher.InspectRecognized(
            request,
            new List<IGltfImageCodec> { unavailable, success },
            GltfImageFormat.WebP);
        GltfImageResult decoded = dispatcher.DecodeImage(
            request,
            new List<IGltfImageCodec> { unavailable, success });
        GltfImageResult encoded = dispatcher.EncodeImage(
            request,
            new List<IGltfImageCodec> { unavailable, success });
        GltfImageResult transcoded = dispatcher.TranscodeImage(
            request,
            new List<IGltfImageCodec> { unavailable, success });
        request.MaximumOutputByteLength = 2;

        Assert.Multiple(() =>
        {
            Assert.That(inspected.Format, Is.EqualTo(GltfImageFormat.WebP));
            Assert.That(decoded.Bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(encoded.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(transcoded.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(unavailable.DecodeCalls, Is.EqualTo(1));
            Assert.That(success.DecodeCalls, Is.EqualTo(1));
            Assert.That(unavailable.EncodeCalls, Is.EqualTo(1));
            Assert.That(success.EncodeCalls, Is.EqualTo(1));
            Assert.That(unavailable.TranscodeCalls, Is.EqualTo(1));
            Assert.That(success.TranscodeCalls, Is.EqualTo(1));
            Assert.Throws<GltfResourceException>(() => dispatcher.DecodeImage(
                request,
                new List<IGltfImageCodec> { success }));
        });
    }

    [Test]
    public void ResourceValidation_RequiresFormatRecognitionAndTracksExtensionReferences()
    {
        GltfDocument resolved = new GltfDocument();
        resolved.ExtensionsUsed.Add(GltfExtensionNames.KhrTextureBasisu);
        resolved.Images.Add(new GltfImage
        {
            Uri = "image.ktx2",
            MimeType = GltfMimeTypes.Ktx2,
            Resolved = true,
            ResolvedBytes = CreateExtensionSignature(GltfExtensionNames.KhrTextureBasisu)
        });
        resolved.Textures.Add(CreateTypedTexture(GltfExtensionNames.KhrTextureBasisu, 0));
        GltfValidationOptions mismatchedOptions = new GltfValidationOptions();
        mismatchedOptions.ImageCodecs.Add(new ProbeImageCodec
        {
            RecognizedFormat = GltfImageFormat.WebP
        });

        GltfValidationReport mismatched = new ResourceValidator()
            .ValidateDocumentResourceRules(resolved, mismatchedOptions);

        GltfDocument unresolved = new GltfDocument();
        unresolved.Images.Add(new GltfImage { Uri = "image.ktx2" });
        unresolved.Textures.Add(CreateTypedTexture(GltfExtensionNames.KhrTextureBasisu, 0));
        GltfValidationReport referenced = new ResourceValidator()
            .ValidateDocumentResourceRules(unresolved, new GltfValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(mismatched.Issues.Any(issue =>
                issue.Code == GltfIssueCodes.ImageCodecUnavailable), Is.True);
            Assert.That(referenced.Issues.Any(issue =>
                issue.Code == GltfIssueCodes.ResourceUnavailable
                && issue.Severity == GltfValidationSeverity.Warning), Is.True);
        });
    }

    private static GltfExtensionContext CreateValidationContext(
        GltfDocument document,
        string extension,
        int image)
    {
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        if (extension == GltfExtensionNames.KhrTextureBasisu)
        {
            typed.TextureBasisu = new GltfTextureBasisuExtension
            {
                HasSource = true,
                Source = image
            };
        }
        else
        {
            typed.TextureWebp = new GltfTextureWebpExtension
            {
                HasSource = true,
                Source = image
            };
        }

        return new GltfExtensionContext
        {
            Document = document,
            Name = extension,
            ObjectLocation = "texture",
            HasObjectIndex = true,
            ObjectIndex = 0,
            TypedData = typed
        };
    }

    private static int GetSource(GltfTypedExtensionData typed, string extension)
    {
        if (extension == GltfExtensionNames.KhrTextureBasisu)
        {
            return typed.TextureBasisu!.Source;
        }

        return typed.TextureWebp!.Source;
    }

    private static GltfTexture CreateTypedTexture(string extension, int image)
    {
        GltfTexture texture = new GltfTexture();
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        if (extension == GltfExtensionNames.KhrTextureBasisu)
        {
            typed.TextureBasisu = new GltfTextureBasisuExtension
            {
                HasSource = true,
                Source = image
            };
        }
        else
        {
            typed.TextureWebp = new GltfTextureWebpExtension
            {
                HasSource = true,
                Source = image
            };
        }

        texture.PropertyJson.TypedExtensions[extension] = typed;
        return texture;
    }

    private static string CreateRequiredDocumentJson(string extension, byte[] bytes)
    {
        string mimeType = extension == GltfExtensionNames.KhrTextureBasisu
            ? GltfMimeTypes.Ktx2
            : GltfMimeTypes.WebP;
        return "{\"asset\":{\"version\":\"2.0\"},\"extensionsUsed\":[\""
            + extension
            + "\"],\"extensionsRequired\":[\""
            + extension
            + "\"],\"images\":[{\"uri\":\"data:"
            + mimeType
            + ";base64,"
            + Convert.ToBase64String(bytes)
            + "\"}],\"textures\":[{\"extensions\":{\""
            + extension
            + "\":{\"source\":0}}}]}";
    }

    private static byte[] CreateExtensionSignature(string extension)
    {
        if (extension == GltfExtensionNames.KhrTextureBasisu)
        {
            return new byte[]
            {
                0xAB, 0x4B, 0x54, 0x58, 0x20, 0x32,
                0x30, 0xBB, 0x0D, 0x0A, 0x1A, 0x0A
            };
        }

        return new byte[]
        {
            0x52, 0x49, 0x46, 0x46,
            0x04, 0x00, 0x00, 0x00,
            0x57, 0x45, 0x42, 0x50
        };
    }

    private static byte[] CreatePngSignature()
    {
        return new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    }

    private class ProbeImageCodec : IGltfImageCodec
    {
        public GltfOperationStatus Status = GltfOperationStatus.Success;
        public GltfImageFormat RecognizedFormat = GltfImageFormat.Unknown;
        public byte[] Output = [];
        public int DecodeCalls;
        public int EncodeCalls;
        public int TranscodeCalls;
        public GltfImageRequest? LastRequest;
        public bool MutateRequestBytes;
        public List<GltfValidationIssue> Diagnostics = new List<GltfValidationIssue>();

        public GltfImageInfo InspectImage(GltfImageRequest request)
        {
            return new GltfImageInfo { Format = RecognizedFormat };
        }

        public GltfImageResult DecodeImage(GltfImageRequest request)
        {
            DecodeCalls++;
            LastRequest = request;
            if (MutateRequestBytes && request.Bytes.Length > 0)
            {
                request.Bytes[0] = 99;
            }

            return CreateResult();
        }

        public GltfImageResult EncodeImage(GltfImageRequest request)
        {
            EncodeCalls++;
            return CreateResult();
        }

        public GltfImageResult TranscodeImage(GltfImageRequest request)
        {
            TranscodeCalls++;
            return CreateResult();
        }

        private GltfImageResult CreateResult()
        {
            return new GltfImageResult
            {
                Status = Status,
                Bytes = Output,
                Info = new GltfImageInfo { Format = RecognizedFormat },
                Diagnostics = Diagnostics
            };
        }
    }
}
