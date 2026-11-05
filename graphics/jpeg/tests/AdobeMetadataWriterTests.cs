namespace jpeg.Tests;

internal class AdobeMetadataWriterTests
{
    [Test]
    public void Writer_PreservesParsedAdobePayload()
    {
        byte[] original = AdobeMetadataParserTests.CreateAdobePayload(100, 0x0102, 0x0304, global::jpeg.JpegConstants.AdobeTransformYCCK);
        global::jpeg.JpegAdobeMetadata metadata = new global::jpeg.JpegAdobeMetadataParser().Parse(original);
        global::jpeg.JpegAdobeMetadataWriter writer = new global::jpeg.JpegAdobeMetadataWriter();

        byte[] payload = writer.WritePayload(metadata);

        Assert.That(payload, Is.EqualTo(original));
    }

    [Test]
    public void Writer_WritesConstructedAdobeMetadataThatParsesBack()
    {
        global::jpeg.JpegAdobeMetadata metadata = CreateAdobeMetadata();
        global::jpeg.JpegAdobeMetadataWriter writer = new global::jpeg.JpegAdobeMetadataWriter();

        byte[] payload = writer.WritePayload(metadata);
        global::jpeg.JpegAdobeMetadata parsed = new global::jpeg.JpegAdobeMetadataParser().Parse(payload);

        Assert.That(payload, Is.EqualTo(AdobeMetadataParserTests.CreateAdobePayload(100, 1, 2, global::jpeg.JpegConstants.AdobeTransformYCbCr)));
        Assert.That(parsed.IsPresent, Is.True);
        Assert.That(parsed.Version, Is.EqualTo(100));
        Assert.That(parsed.Flags0, Is.EqualTo(1));
        Assert.That(parsed.Flags1, Is.EqualTo(2));
        Assert.That(parsed.Transform, Is.EqualTo(global::jpeg.JpegConstants.AdobeTransformYCbCr));
        Assert.That(parsed.IsYCbCrTransform, Is.True);
    }

    [Test]
    public void Writer_WritesApp14MarkerSegment()
    {
        global::jpeg.JpegAdobeMetadata metadata = CreateAdobeMetadata();
        global::jpeg.JpegMarkerSegmentWriter segmentWriter = new global::jpeg.JpegMarkerSegmentWriter();
        global::jpeg.JpegAdobeMetadataWriter writer = new global::jpeg.JpegAdobeMetadataWriter();

        writer.WriteMarkerSegment(segmentWriter, metadata);

        global::jpeg.JpegMarkerSegmentReader reader = new global::jpeg.JpegMarkerSegmentReader(segmentWriter.ToArray());
        global::jpeg.JpegMarkerSegment segment = reader.ReadNextSegment();
        Assert.That(segment.Marker, Is.EqualTo(global::jpeg.JpegMarker.App14));
        Assert.That(segment.Payload, Is.EqualTo(AdobeMetadataParserTests.CreateAdobePayload(100, 1, 2, global::jpeg.JpegConstants.AdobeTransformYCbCr)));
    }

    [Test]
    public void Writer_RejectsInvalidAdobeMetadata()
    {
        global::jpeg.JpegAdobeMetadataWriter writer = new global::jpeg.JpegAdobeMetadataWriter();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                writer.WritePayload(null!);
            });

        global::jpeg.JpegAdobeMetadata badVersion = CreateAdobeMetadata();
        badVersion.Version = 65536;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(badVersion);
            });

        global::jpeg.JpegAdobeMetadata badFlags = CreateAdobeMetadata();
        badFlags.Flags0 = -1;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(badFlags);
            });

        global::jpeg.JpegAdobeMetadata badTransform = CreateAdobeMetadata();
        badTransform.Transform = 3;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(badTransform);
            });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                writer.WriteMarkerSegment(null!, CreateAdobeMetadata());
            });
    }

    public static global::jpeg.JpegAdobeMetadata CreateAdobeMetadata()
    {
        global::jpeg.JpegAdobeMetadata metadata = new global::jpeg.JpegAdobeMetadata();
        metadata.IsPresent = true;
        metadata.Version = 100;
        metadata.Flags0 = 1;
        metadata.Flags1 = 2;
        metadata.Transform = global::jpeg.JpegConstants.AdobeTransformYCbCr;
        metadata.IsYCbCrTransform = true;
        metadata.TransformColorSpace = global::jpeg.JpegColorSpace.YCbCr;
        return metadata;
    }
}
