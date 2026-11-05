namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfImageCodecTests
{
    [Test]
    public void InspectImage_UsesTheBuiltInHeaderInspector()
    {
        GltfImageRequest request = new GltfImageRequest();
        request.Bytes = CreatePngBytes();

        GltfImageInfo result = new GltfImageCodec().InspectImage(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Format, Is.EqualTo(GltfImageFormat.Png));
            Assert.That(result.MimeType, Is.EqualTo(GltfMimeTypes.Png));
            Assert.That(result.HasDimensions, Is.True);
            Assert.That(result.Width, Is.EqualTo(2));
            Assert.That(result.Height, Is.EqualTo(3));
            Assert.That(result.ComponentCount, Is.EqualTo(4));
            Assert.That(result.BitDepth, Is.EqualTo(8));
        });
    }

    [Test]
    public void TranscodeImage_ReturnsAnOwnedPassthroughWhenNothingChanges()
    {
        GltfImageRequest request = new GltfImageRequest();
        request.Bytes = CreatePngBytes();
        request.TargetFormat = GltfImageFormat.Png;
        request.HasTargetWidth = true;
        request.TargetWidth = 2;
        request.HasTargetHeight = true;
        request.TargetHeight = 3;
        request.MaximumOutputByteLength = request.Bytes.Length;

        GltfImageResult result = new GltfImageCodec().TranscodeImage(request);
        request.Bytes[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.Codec, Is.EqualTo("gltf.core"));
            Assert.That(result.Bytes[0], Is.EqualTo(0x89));
            Assert.That(result.Bytes, Is.Not.SameAs(request.Bytes));
            Assert.That(result.Info.Format, Is.EqualTo(GltfImageFormat.Png));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void TranscodeImage_PassesThroughOpaqueCustomDataWhenNoTargetIsRequested()
    {
        GltfImageRequest request = new GltfImageRequest();
        request.Bytes = [1, 2, 3];
        request.SourceFormat = GltfImageFormat.Custom;

        GltfImageResult result = new GltfImageCodec().TranscodeImage(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.Bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(result.Bytes, Is.Not.SameAs(request.Bytes));
            Assert.That(result.Info.Format, Is.EqualTo(GltfImageFormat.Unknown));
        });
    }

    [Test]
    public void DecodeImage_ReturnsUnavailableWithSourceMetadataAndDiagnostic()
    {
        GltfImageRequest request = new GltfImageRequest();
        request.Bytes = CreatePngBytes();

        GltfImageResult result = new GltfImageCodec().DecodeImage(request);

        AssertUnavailable(
            result,
            "Image pixel decoding requires a registered external image codec.");
        Assert.That(result.Info.Format, Is.EqualTo(GltfImageFormat.Png));
    }

    [Test]
    public void EncodeImage_ReturnsUnavailableWithDiagnostic()
    {
        GltfImageRequest request = new GltfImageRequest();
        request.Bytes = [1, 2, 3, 4];
        request.TargetFormat = GltfImageFormat.Png;

        GltfImageResult result = new GltfImageCodec().EncodeImage(request);

        AssertUnavailable(
            result,
            "Image pixel encoding requires a registered external image codec.");
    }

    [TestCase(GltfImageFormat.Jpeg, false)]
    [TestCase(GltfImageFormat.Png, true)]
    public void TranscodeImage_ReturnsUnavailableWhenPixelWorkIsRequested(
        GltfImageFormat targetFormat,
        bool resize)
    {
        GltfImageRequest request = new GltfImageRequest();
        request.Bytes = CreatePngBytes();
        request.TargetFormat = targetFormat;
        request.HasTargetWidth = resize;
        request.TargetWidth = 4;

        GltfImageResult result = new GltfImageCodec().TranscodeImage(request);

        AssertUnavailable(
            result,
            "Image format conversion or resizing requires a registered external image codec.");
        Assert.That(result.Info.Format, Is.EqualTo(GltfImageFormat.Png));
    }

    [Test]
    public void TranscodeImage_EnforcesTheMaximumPassthroughLength()
    {
        GltfImageRequest request = new GltfImageRequest();
        request.Bytes = [1, 2, 3];
        request.SourceFormat = GltfImageFormat.Custom;
        request.MaximumOutputByteLength = 2;

        Assert.That(
            () => new GltfImageCodec().TranscodeImage(request),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(
                "Image passthrough output length 3 exceeds the configured maximum 2."));
    }

    private static void AssertUnavailable(GltfImageResult result, string message)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Unavailable));
            Assert.That(result.Codec, Is.EqualTo("gltf.core"));
            Assert.That(result.Bytes, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(GltfIssueCodes.ImageCodecUnavailable));
            Assert.That(result.Diagnostics[0].Severity,
                Is.EqualTo(GltfValidationSeverity.Error));
            Assert.That(result.Diagnostics[0].Message, Is.EqualTo(message));
        });
    }

    private static byte[] CreatePngBytes()
    {
        return
        [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x03,
            0x08, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44,
            0x00, 0x00, 0x00, 0x00
        ];
    }
}
