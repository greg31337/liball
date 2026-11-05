namespace jpeg.Tests;

internal class AdobeMetadataParserTests
{
    [Test]
    public void Parser_ReadsAdobeApp14Metadata()
    {
        byte[] payload = CreateAdobePayload(100, 0x0102, 0x0304, global::jpeg.JpegConstants.AdobeTransformYCCK);
        global::jpeg.JpegAdobeMetadataParser parser = new global::jpeg.JpegAdobeMetadataParser();

        global::jpeg.JpegAdobeMetadata metadata = parser.Parse(payload);

        Assert.That(metadata.IsPresent, Is.True);
        Assert.That(metadata.Version, Is.EqualTo(100));
        Assert.That(metadata.Flags0, Is.EqualTo(0x0102));
        Assert.That(metadata.Flags1, Is.EqualTo(0x0304));
        Assert.That(metadata.Transform, Is.EqualTo(global::jpeg.JpegConstants.AdobeTransformYCCK));
        Assert.That(metadata.IsYCCKTransform, Is.True);
        Assert.That(metadata.IsYCbCrTransform, Is.False);
        Assert.That(metadata.IsUnknownTransform, Is.False);
        Assert.That(metadata.TransformColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.YCCK));
    }

    [Test]
    public void Parser_InterpretsTransformWithComponentCount()
    {
        global::jpeg.JpegAdobeMetadataParser parser = new global::jpeg.JpegAdobeMetadataParser();
        global::jpeg.JpegAdobeMetadata rgb = parser.Parse(CreateAdobePayload(100, 0, 0, global::jpeg.JpegConstants.AdobeTransformUnknown));
        global::jpeg.JpegAdobeMetadata cmyk = parser.Parse(CreateAdobePayload(100, 0, 0, global::jpeg.JpegConstants.AdobeTransformUnknown));
        global::jpeg.JpegAdobeMetadata ycbcr = parser.Parse(CreateAdobePayload(100, 0, 0, global::jpeg.JpegConstants.AdobeTransformYCbCr));
        global::jpeg.JpegAdobeMetadata ycck = parser.Parse(CreateAdobePayload(100, 0, 0, global::jpeg.JpegConstants.AdobeTransformYCCK));

        parser.ApplyComponentInterpretation(rgb, 3);
        parser.ApplyComponentInterpretation(cmyk, 4);
        parser.ApplyComponentInterpretation(ycbcr, 3);
        parser.ApplyComponentInterpretation(ycck, 4);

        Assert.That(rgb.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.RGB));
        Assert.That(rgb.InvertsCmyk, Is.False);
        Assert.That(cmyk.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.CMYK));
        Assert.That(cmyk.InvertsCmyk, Is.True);
        Assert.That(ycbcr.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.YCbCr));
        Assert.That(ycbcr.InvertsCmyk, Is.False);
        Assert.That(ycck.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.YCCK));
        Assert.That(ycck.InvertsCmyk, Is.True);
    }

    [Test]
    public void Identifier_StoresAdobeMetadataFromApp14()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App14, CreateAdobePayload(100, 1, 2, global::jpeg.JpegConstants.AdobeTransformYCCK));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, CreateFourComponentSofPayload());
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, CreateFourComponentSosPayload());
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.Metadata.HasAdobe, Is.True);
        Assert.That(info.Metadata.Adobe.IsPresent, Is.True);
        Assert.That(info.Metadata.AdobeVersion, Is.EqualTo(100));
        Assert.That(info.Metadata.AdobeTransform, Is.EqualTo(global::jpeg.JpegConstants.AdobeTransformYCCK));
        Assert.That(info.Metadata.Adobe.Flags0, Is.EqualTo(1));
        Assert.That(info.Metadata.Adobe.Flags1, Is.EqualTo(2));
        Assert.That(info.Metadata.Adobe.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.YCCK));
        Assert.That(info.Metadata.Adobe.InvertsCmyk, Is.True);
        Assert.That(info.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.YCCK));
    }

    [Test]
    public void Parser_IgnoresNonAdobePayloadWhenCheckingSignature()
    {
        global::jpeg.JpegAdobeMetadataParser parser = new global::jpeg.JpegAdobeMetadataParser();

        Assert.That(parser.IsAdobe(null), Is.False);
        Assert.That(parser.IsAdobe(new byte[] { 1, 2, 3 }), Is.False);
        Assert.That(parser.IsAdobe(new byte[] { 0x41, 0x64, 0x6F, 0x62, 0x6F }), Is.False);
    }

    [Test]
    public void Parser_RejectsMalformedAdobePayloads()
    {
        global::jpeg.JpegAdobeMetadataParser parser = new global::jpeg.JpegAdobeMetadataParser();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                parser.Parse(null!);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(new byte[] { 1, 2, 3 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(new byte[] { 0x41, 0x64, 0x6F, 0x62, 0x65 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(CreateAdobePayload(100, 0, 0, 3));
            });
    }

    public static byte[] CreateAdobePayload(int version, int flags0, int flags1, int transform)
    {
        byte[] payload = new byte[12];
        payload[0] = 0x41;
        payload[1] = 0x64;
        payload[2] = 0x6F;
        payload[3] = 0x62;
        payload[4] = 0x65;
        global::jpeg.JpegByteWriter.WriteUInt16(payload, 5, version);
        global::jpeg.JpegByteWriter.WriteUInt16(payload, 7, flags0);
        global::jpeg.JpegByteWriter.WriteUInt16(payload, 9, flags1);
        payload[11] = (byte)transform;
        return payload;
    }

    public static byte[] CreateFourComponentSofPayload()
    {
        return new byte[]
        {
            8, 0, 16, 0, 32, 4,
            1, 0x11, 0,
            2, 0x11, 1,
            3, 0x11, 1,
            4, 0x11, 1
        };
    }

    public static byte[] CreateFourComponentSosPayload()
    {
        return new byte[]
        {
            4,
            1, 0,
            2, 0x11,
            3, 0x11,
            4, 0x11,
            0, 63, 0
        };
    }
}
