namespace gltf.Tests.Internal.Validation;

[TestFixture]
public class ResourceValidatorTests
{
    [Test]
    public void ValidateDocumentResourceRules_SkipsResourceChecksWhenDisabled()
    {
        GltfDocument document = new GltfDocument();
        GltfBuffer buffer = new GltfBuffer();
        buffer.Uri = "data:application/octet-stream;base64,%%%";
        buffer.ByteLength = 4;
        document.Buffers.Add(buffer);
        GltfValidationOptions options = new GltfValidationOptions();
        options.ValidateResources = false;

        GltfValidationReport report = new ResourceValidator()
            .ValidateDocumentResourceRules(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.True);
            Assert.That(report.Issues, Is.Empty);
        });
    }

    [Test]
    public void ValidateDocumentResourceRules_DistinguishesRequiredAndOptionalUnresolvedResources()
    {
        GltfDocument document = new GltfDocument();
        GltfBuffer required = new GltfBuffer();
        required.Uri = "required.bin";
        required.ByteLength = 4;
        required.PendingResourceRequest = CreateRequest("required.bin", "/assets/model.gltf");
        document.Buffers.Add(required);
        GltfBuffer optional = new GltfBuffer();
        optional.Uri = "optional.bin";
        optional.ByteLength = 4;
        optional.PendingResourceRequest = CreateRequest("optional.bin", "/assets/model.gltf");
        document.Buffers.Add(optional);
        GltfBufferView view = new GltfBufferView();
        view.Buffer = 0;
        view.ByteLength = 4;
        document.BufferViews.Add(view);

        GltfValidationReport report = new ResourceValidator()
            .ValidateDocumentResourceRules(document, new GltfValidationOptions());

        Assert.Multiple(() =>
        {
            AssertIssue(
                report,
                GltfIssueCodes.ResourceUnavailable,
                "/buffers/0",
                GltfValidationSeverity.Warning);
            AssertIssue(
                report,
                GltfIssueCodes.ResourceUnavailable,
                "/buffers/1",
                GltfValidationSeverity.Information);
            Assert.That(report.IsValid, Is.True);
        });
    }

    [Test]
    public void ValidateDocumentResourceRules_ReportsResolverPolicyDenial()
    {
        GltfDocument document = new GltfDocument();
        GltfImage image = new GltfImage();
        image.Uri = "file:///private/image.png";
        image.PendingResourceRequest = CreateRequest(image.Uri, null);
        document.Images.Add(image);

        GltfValidationReport report = new ResourceValidator()
            .ValidateDocumentResourceRules(document, new GltfValidationOptions());

        GltfValidationIssue issue = AssertIssue(
            report,
            GltfIssueCodes.ResourcePolicyDenied,
            "/images/0/uri",
            GltfValidationSeverity.Error);
        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.False);
            Assert.That(issue.ResourceUri, Is.EqualTo(image.Uri));
        });
    }

    [Test]
    public void ValidateDocumentResourceRules_ReportsMalformedAndOversizedDataUris()
    {
        GltfDocument document = new GltfDocument();
        GltfBuffer malformed = new GltfBuffer();
        malformed.Uri = "data:application/octet-stream;base64,A===%";
        malformed.ByteLength = 1;
        document.Buffers.Add(malformed);
        GltfImage oversized = new GltfImage();
        oversized.Uri = "data:image/png;base64,AQID";
        oversized.PendingResourceRequest = CreateRequest(oversized.Uri, null);
        oversized.PendingResourceRequest.MaximumByteLength = 2;
        document.Images.Add(oversized);

        GltfValidationReport report = new ResourceValidator()
            .ValidateDocumentResourceRules(document, new GltfValidationOptions());

        Assert.Multiple(() =>
        {
            AssertIssue(
                report,
                GltfIssueCodes.DataUriInvalid,
                "/buffers/0/uri",
                GltfValidationSeverity.Error);
            AssertIssue(
                report,
                GltfIssueCodes.ResourceLimitExceeded,
                "/images/0/uri",
                GltfValidationSeverity.Error);
            Assert.That(
                CountIssues(report, GltfIssueCodes.ResourceUnavailable),
                Is.Zero);
            Assert.That(report.IsValid, Is.False);
        });
    }

    [Test]
    public void ValidateDocumentResourceRules_ReportsShortResolvedBuffer()
    {
        GltfDocument document = new GltfDocument();
        GltfBuffer buffer = new GltfBuffer();
        buffer.Uri = "buffer.bin";
        buffer.ByteLength = 4;
        buffer.Resolved = true;
        buffer.ResolvedBytes = [1, 2, 3];
        document.Buffers.Add(buffer);

        GltfValidationReport report = new ResourceValidator()
            .ValidateDocumentResourceRules(document, new GltfValidationOptions());

        GltfValidationIssue issue = AssertIssue(
            report,
            GltfIssueCodes.ResourceLengthMismatch,
            "/buffers/0/byteLength",
            GltfValidationSeverity.Error);
        Assert.Multiple(() =>
        {
            Assert.That(issue.ResourceUri, Is.EqualTo("buffer.bin"));
            Assert.That(report.IsValid, Is.False);
        });
    }

    [Test]
    public void ValidateDocumentResourceRules_ReportsInvalidResolvedImage()
    {
        GltfDocument document = new GltfDocument();
        GltfImage image = new GltfImage();
        image.Uri = "invalid.png";
        image.MimeType = GltfMimeTypes.Png;
        image.Resolved = true;
        image.ResolvedBytes = CreateInvalidPng();
        document.Images.Add(image);

        GltfValidationReport report = new ResourceValidator()
            .ValidateDocumentResourceRules(document, new GltfValidationOptions());

        Assert.Multiple(() =>
        {
            AssertIssue(
                report,
                GltfIssueCodes.ImageDataInvalid,
                "/images/0",
                GltfValidationSeverity.Error);
            Assert.That(report.IsValid, Is.False);
        });
    }

    [Test]
    public void ValidateDocumentResourceRules_ReportsMissingExternalImageCodec()
    {
        GltfDocument document = new GltfDocument();
        document.ExtensionsUsed.Add(GltfExtensionNames.KhrTextureBasisu);
        GltfImage image = new GltfImage();
        image.Uri = "texture.ktx2";
        image.MimeType = GltfMimeTypes.Ktx2;
        image.Resolved = true;
        image.ResolvedBytes = CreateKtx2Header();
        document.Images.Add(image);
        ResourceValidator validator = new ResourceValidator();

        GltfValidationReport withoutCodec = validator
            .ValidateDocumentResourceRules(document, new GltfValidationOptions());
        GltfValidationOptions configured = new GltfValidationOptions();
        configured.ImageCodecs.Add(new ImageCodecProbe());
        GltfValidationReport withCodec = validator
            .ValidateDocumentResourceRules(document, configured);

        Assert.Multiple(() =>
        {
            AssertIssue(
                withoutCodec,
                GltfIssueCodes.ImageCodecUnavailable,
                "/images/0",
                GltfValidationSeverity.Warning);
            Assert.That(withoutCodec.IsValid, Is.True);
            Assert.That(
                CountIssues(withCodec, GltfIssueCodes.ImageCodecUnavailable),
                Is.Zero);
            Assert.That(configured.ImageCodecs, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void ValidateDocumentResourceRules_RejectsNullArguments()
    {
        ResourceValidator validator = new ResourceValidator();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => validator.ValidateDocumentResourceRules(
                    null!,
                    new GltfValidationOptions()),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("document"));
            Assert.That(
                () => validator.ValidateDocumentResourceRules(
                    new GltfDocument(),
                    null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("options"));
        });
    }

    private static GltfResourceRequest CreateRequest(string? sourceUri, string? baseUri)
    {
        GltfResourceRequest result = new GltfResourceRequest();
        result.SourceUri = sourceUri;
        result.BaseUri = baseUri;
        return result;
    }

    private static byte[] CreateInvalidPng()
    {
        return
        [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x01,
            0x08, 0x06, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44,
            0x00, 0x00, 0x00, 0x00
        ];
    }

    private static byte[] CreateKtx2Header()
    {
        return
        [
            0xAB, 0x4B, 0x54, 0x58, 0x20, 0x32,
            0x30, 0xBB, 0x0D, 0x0A, 0x1A, 0x0A
        ];
    }

    private static GltfValidationIssue AssertIssue(
        GltfValidationReport report,
        string code,
        string pointer,
        GltfValidationSeverity severity)
    {
        int index = 0;
        while (index < report.Issues.Count)
        {
            GltfValidationIssue issue = report.Issues[index];
            if (issue.Code == code && issue.JsonPointer == pointer)
            {
                Assert.That(issue.Severity, Is.EqualTo(severity));
                return issue;
            }

            index++;
        }

        Assert.Fail($"Expected issue {code} at {pointer}.");
        return null!;
    }

    private static int CountIssues(GltfValidationReport report, string code)
    {
        int result = 0;
        int index = 0;
        while (index < report.Issues.Count)
        {
            if (report.Issues[index].Code == code)
            {
                result++;
            }

            index++;
        }

        return result;
    }

    private class ImageCodecProbe : IGltfImageCodec
    {
        public GltfImageInfo InspectImage(GltfImageRequest request)
        {
            GltfImageInfo result = new GltfImageInfo();
            result.Format = request.SourceFormat;
            return result;
        }

        public GltfImageResult DecodeImage(GltfImageRequest request)
        {
            return new GltfImageResult();
        }

        public GltfImageResult EncodeImage(GltfImageRequest request)
        {
            return new GltfImageResult();
        }

        public GltfImageResult TranscodeImage(GltfImageRequest request)
        {
            return new GltfImageResult();
        }
    }
}
