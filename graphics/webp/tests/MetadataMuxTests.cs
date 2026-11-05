using webp;

namespace webp.Tests;

[TestFixture]
public class MetadataMuxTests
{
    [Test]
    public void Extract_ReturnsRawMetadataCopies()
    {
        byte[] data = NewExtendedStill(new byte[] { 1 }, new byte[] { 2 }, new byte[] { 3 }, new byte[] { 4 });

        WebpMetadata metadata = WebpMetadataMux.Extract(data);
        data[data.Length - 2] = 0xFF;

        Assert.That(metadata.IccProfile, Is.EqualTo(new byte[] { 1 }));
        Assert.That(metadata.Exif, Is.EqualTo(new byte[] { 2 }));
        Assert.That(metadata.Xmp, Is.EqualTo(new byte[] { 3 }));
    }

    [Test]
    public void Replace_ReplacesSelectedMetadataAndPreservesImageAndUnknownPayloads()
    {
        byte[] data = NewExtendedStill(new byte[] { 1 }, new byte[] { 2 }, null, new byte[] { 9 });
        WebpMetadata replacement = new WebpMetadata();
        replacement.IccProfile = new byte[] { 10, 11 };
        replacement.Xmp = new byte[] { 12 };

        byte[] replaced = WebpMetadataMux.Replace(data, replacement);
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(replaced, WebpStrictness.Strict);

        Assert.That(parsed.IsValid, Is.True);
        Assert.That(parsed.Metadata.IccProfile, Is.EqualTo(new byte[] { 10, 11 }));
        Assert.That(parsed.Metadata.Exif, Is.Null);
        Assert.That(parsed.Metadata.Xmp, Is.EqualTo(new byte[] { 12 }));
        Assert.That(parsed.ImagePayload, Is.EqualTo(new byte[] { 4, 5, 6 }));
        Assert.That(parsed.UnknownChunks, Has.Count.EqualTo(1));
        Assert.That(parsed.UnknownChunks[0].Data, Is.EqualTo(new byte[] { 9 }));
        Assert.That(parsed.Vp8X.HasIccProfile, Is.True);
        Assert.That(parsed.Vp8X.HasExif, Is.False);
        Assert.That(parsed.Vp8X.HasXmp, Is.True);
    }

    [Test]
    public void Strip_RemovesMetadataChunksAndClearsVp8XMetadataFlags()
    {
        byte[] data = NewExtendedStill(new byte[] { 1 }, new byte[] { 2 }, new byte[] { 3 }, null);

        byte[] stripped = WebpMetadataMux.Strip(data);
        WebpMetadata metadata = WebpMetadataMux.Extract(stripped);
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(stripped, WebpStrictness.Strict);

        Assert.That(metadata.IccProfile, Is.Null);
        Assert.That(metadata.Exif, Is.Null);
        Assert.That(metadata.Xmp, Is.Null);
        Assert.That(parsed.IsValid, Is.True);
        Assert.That(parsed.Vp8X.HasIccProfile, Is.False);
        Assert.That(parsed.Vp8X.HasExif, Is.False);
        Assert.That(parsed.Vp8X.HasXmp, Is.False);
    }

    [Test]
    public void Replace_AddsVp8XWhenAddingMetadataToBasicStillImage()
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8 ", NewVp8Payload(7, 9)));
        byte[] data = WebpRiffWriter.Write("WEBP", chunks);
        WebpMetadata metadata = new WebpMetadata();
        metadata.Exif = new byte[] { 0xEE };

        byte[] replaced = WebpMetadataMux.Replace(data, metadata);
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(replaced, WebpStrictness.Strict);

        Assert.That(parsed.IsValid, Is.True);
        Assert.That(parsed.HasVp8X, Is.True);
        Assert.That(parsed.Vp8X.CanvasWidth, Is.EqualTo(7));
        Assert.That(parsed.Vp8X.CanvasHeight, Is.EqualTo(9));
        Assert.That(parsed.Vp8X.HasExif, Is.True);
        Assert.That(parsed.Metadata.Exif, Is.EqualTo(new byte[] { 0xEE }));
        Assert.That(parsed.ImagePayload, Is.EqualTo(NewVp8Payload(7, 9)));
    }

    [Test]
    public void Replace_CopiesReplacementMetadataBytes()
    {
        byte[] data = NewExtendedStill(null, null, null, null);
        WebpMetadata metadata = new WebpMetadata();
        metadata.IccProfile = new byte[] { 1, 2, 3 };

        byte[] replaced = WebpMetadataMux.Replace(data, metadata);
        metadata.IccProfile[0] = 99;
        WebpMetadata extracted = WebpMetadataMux.Extract(replaced);

        Assert.That(extracted.IccProfile, Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void Replace_RoundTripsIccExifAndXmpIndependently()
    {
        AssertMetadataRoundTrip(NewMetadata(new byte[] { 1, 2 }, null, null), true, false, false);
        AssertMetadataRoundTrip(NewMetadata(null, new byte[] { 3, 4 }, null), false, true, false);
        AssertMetadataRoundTrip(NewMetadata(null, null, new byte[] { 5, 6 }), false, false, true);
    }

    [Test]
    public void Strip_RemovesRoundTrippedIccExifAndXmpChunksAndPreservesImageData()
    {
        byte[] data = NewExtendedStill(null, null, null, new byte[] { 9, 10 });
        byte[] withMetadata = WebpMetadataMux.Replace(data, NewMetadata(new byte[] { 1 }, new byte[] { 2 }, new byte[] { 3 }));

        byte[] stripped = WebpMetadataMux.Strip(withMetadata);
        WebpMetadata metadata = WebpMetadataMux.Extract(stripped);
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(stripped, WebpStrictness.Strict);

        Assert.That(parsed.IsValid, Is.True);
        Assert.That(metadata.IccProfile, Is.Null);
        Assert.That(metadata.Exif, Is.Null);
        Assert.That(metadata.Xmp, Is.Null);
        Assert.That(parsed.Metadata.IccProfile, Is.Null);
        Assert.That(parsed.Metadata.Exif, Is.Null);
        Assert.That(parsed.Metadata.Xmp, Is.Null);
        Assert.That(parsed.Vp8X.HasIccProfile, Is.False);
        Assert.That(parsed.Vp8X.HasExif, Is.False);
        Assert.That(parsed.Vp8X.HasXmp, Is.False);
        Assert.That(parsed.AlphaData, Is.EqualTo(new byte[] { 7 }));
        Assert.That(parsed.ImagePayload, Is.EqualTo(new byte[] { 4, 5, 6 }));
        Assert.That(parsed.UnknownChunks, Has.Count.EqualTo(1));
        Assert.That(parsed.UnknownChunks[0].Data, Is.EqualTo(new byte[] { 9, 10 }));
        Assert.That(HasChunk(parsed, "ICCP"), Is.False);
        Assert.That(HasChunk(parsed, "EXIF"), Is.False);
        Assert.That(HasChunk(parsed, "XMP "), Is.False);
    }

    [Test]
    public void Extract_RejectsInvalidWebpData()
    {
        Assert.Throws<ArgumentException>(() => WebpMetadataMux.Extract(new byte[] { 1, 2, 3 }));
    }

    static void AssertMetadataRoundTrip(WebpMetadata replacement, bool hasIcc, bool hasExif, bool hasXmp)
    {
        byte[] data = NewExtendedStill(null, null, null, null);

        byte[] replaced = WebpMetadataMux.Replace(data, replacement);
        WebpMetadata extracted = WebpMetadataMux.Extract(replaced);
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(replaced, WebpStrictness.Strict);

        Assert.That(parsed.IsValid, Is.True);
        Assert.That(extracted.IccProfile, Is.EqualTo(replacement.IccProfile));
        Assert.That(extracted.Exif, Is.EqualTo(replacement.Exif));
        Assert.That(extracted.Xmp, Is.EqualTo(replacement.Xmp));
        Assert.That(parsed.Metadata.IccProfile, Is.EqualTo(replacement.IccProfile));
        Assert.That(parsed.Metadata.Exif, Is.EqualTo(replacement.Exif));
        Assert.That(parsed.Metadata.Xmp, Is.EqualTo(replacement.Xmp));
        Assert.That(parsed.Vp8X.HasIccProfile, Is.EqualTo(hasIcc));
        Assert.That(parsed.Vp8X.HasExif, Is.EqualTo(hasExif));
        Assert.That(parsed.Vp8X.HasXmp, Is.EqualTo(hasXmp));
        Assert.That(HasChunk(parsed, "ICCP"), Is.EqualTo(hasIcc));
        Assert.That(HasChunk(parsed, "EXIF"), Is.EqualTo(hasExif));
        Assert.That(HasChunk(parsed, "XMP "), Is.EqualTo(hasXmp));
    }

    static WebpMetadata NewMetadata(byte[]? icc, byte[]? exif, byte[]? xmp)
    {
        WebpMetadata metadata = new WebpMetadata();
        metadata.IccProfile = icc;
        metadata.Exif = exif;
        metadata.Xmp = xmp;
        return metadata;
    }

    static bool HasChunk(WebpExtendedContainerParseResult parsed, string fourCc)
    {
        int index = 0;
        while (index < parsed.Chunks.Count)
        {
            if (parsed.Chunks[index].FourCc == fourCc)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static byte[] NewExtendedStill(byte[]? icc, byte[]? exif, byte[]? xmp, byte[]? unknown)
    {
        WebpMetadata metadata = new WebpMetadata();
        metadata.IccProfile = icc;
        metadata.Exif = exif;
        metadata.Xmp = xmp;

        byte flags = WebpConstants.Vp8XFeatureAlpha;
        if (icc != null)
        {
            flags = (byte)(flags | WebpConstants.Vp8XFeatureIccProfile);
        }

        if (exif != null)
        {
            flags = (byte)(flags | WebpConstants.Vp8XFeatureExif);
        }

        if (xmp != null)
        {
            flags = (byte)(flags | WebpConstants.Vp8XFeatureXmp);
        }

        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8X", NewVp8XPayload(flags, 5, 6)));
        chunks.Add(new WebpRiffChunk("ALPH", new byte[] { 7 }));
        chunks.Add(new WebpRiffChunk("VP8 ", new byte[] { 4, 5, 6 }));
        if (icc != null)
        {
            chunks.Add(new WebpRiffChunk("ICCP", icc));
        }

        if (exif != null)
        {
            chunks.Add(new WebpRiffChunk("EXIF", exif));
        }

        if (xmp != null)
        {
            chunks.Add(new WebpRiffChunk("XMP ", xmp));
        }

        if (unknown != null)
        {
            chunks.Add(new WebpRiffChunk("zzzz", unknown));
        }

        return WebpRiffWriter.Write("WEBP", chunks);
    }

    static byte[] NewVp8XPayload(byte flags, int width, int height)
    {
        byte[] payload = new byte[WebpConstants.Vp8XChunkPayloadSize];
        payload[0] = flags;
        WebpEndianWriter.WriteUInt24(payload, 4, (uint)(width - 1));
        WebpEndianWriter.WriteUInt24(payload, 7, (uint)(height - 1));
        return payload;
    }

    static byte[] NewVp8Payload(int width, int height)
    {
        byte[] payload = new byte[WebpConstants.Vp8LossyKeyFrameHeaderSize];
        payload[3] = WebpConstants.Vp8LossyKeyFrameSignature0;
        payload[4] = WebpConstants.Vp8LossyKeyFrameSignature1;
        payload[5] = WebpConstants.Vp8LossyKeyFrameSignature2;
        WebpEndianWriter.WriteUInt16(payload, 6, (ushort)width);
        WebpEndianWriter.WriteUInt16(payload, 8, (ushort)height);
        return payload;
    }
}
