using webp;

namespace webp.Tests;

[TestFixture]
public class WebpMuxConformanceTests
{
    [Test]
    public void MetadataEditsPromoteBasicStillAndDeriveEveryFeatureFlag()
    {
        byte[] imagePayload = NewVp8Payload(7, 5);
        byte[] source = WebpContainerWriter.WriteBasicStill(imagePayload, WebpBitstreamKind.Lossy);
        IWebpMux mux = new WebpCodec();

        byte[] withIcc = mux.SetIccProfile(source, new byte[] { 1, 2, 3 });
        byte[] withExif = mux.SetExif(withIcc, new byte[] { 4, 5 });
        byte[] withAll = mux.SetXmp(withExif, new byte[] { 6 });

        AssertMetadataState(withAll, true, true, true, imagePayload);
        Assert.That(GetFourCcs(withAll), Is.EqualTo(new[] { "VP8X", "ICCP", "VP8 ", "EXIF", "XMP " }));

        byte[] exifOnly = mux.SetIccProfile(withAll, null);
        exifOnly = mux.SetXmp(exifOnly, null);

        AssertMetadataState(exifOnly, false, true, false, imagePayload);
        Assert.That(mux.GetMetadata(exifOnly).Exif, Is.EqualTo(new byte[] { 4, 5 }));
    }

    [Test]
    public void UnknownChunkReplacementHonorsPlacementAndPreservesRecognizedPayloads()
    {
        byte[] source = NewAnimation();
        List<WebpUnknownChunk> replacements = new List<WebpUnknownChunk>();
        replacements.Add(new WebpUnknownChunk("u001", new byte[] { 10, 11, 12 }));
        replacements.Add(new WebpUnknownChunk("u002", new byte[] { 20 }));
        IWebpMux mux = new WebpCodec();
        List<WebpRiffChunk> recognizedBefore = GetRecognizedChunks(source);

        byte[] before = mux.ReplaceUnknownChunks(source, replacements, WebpUnknownChunkPlacement.BeforeRecognizedChunks);
        byte[] after = mux.ReplaceUnknownChunksSpan(source, replacements, WebpUnknownChunkPlacement.AfterRecognizedChunks);
        replacements[0].Data[0] = 99;

        Assert.That(GetFourCcs(before).GetRange(0, 2), Is.EqualTo(new[] { "u001", "u002" }));
        List<string> afterNames = GetFourCcs(after);
        Assert.That(afterNames.GetRange(afterNames.Count - 2, 2), Is.EqualTo(new[] { "u001", "u002" }));
        AssertRecognizedChunksEqual(before, recognizedBefore);
        AssertRecognizedChunksEqual(after, recognizedBefore);
        Assert.That(mux.EnumerateUnknownChunks(before)[0].Data, Is.EqualTo(new byte[] { 10, 11, 12 }));
        Assert.That(mux.EnumerateUnknownChunks(after)[1].Data, Is.EqualTo(new byte[] { 20 }));
    }

    [Test]
    public void FrameInsertionReplacementAndRemovalPreserveUntouchedEncodedPayloads()
    {
        byte[] source = NewAnimation();
        List<byte[]> sourceFrames = GetPayloads(source, WebpConstants.FourCcAnimationFrameString);
        IWebpMux mux = new WebpCodec();
        WebpFrame insertedFrame = NewLossyFrame(2, 2, 2, 2, 30);
        insertedFrame.BlendMode = WebpFrameBlendMode.Replace;
        insertedFrame.DisposeMode = WebpFrameDisposeMode.DisposeToBackground;
        insertedFrame.AlphaData = new byte[] { 0, 255, 128, 64, 32 };
        insertedFrame.UnknownChunks.Add(new WebpUnknownChunk("f001", new byte[] { 7 }));

        byte[] inserted = mux.InsertAnimationFrame(source, 1, insertedFrame);
        List<byte[]> insertedPayloads = GetPayloads(inserted, WebpConstants.FourCcAnimationFrameString);
        List<WebpFrame> extracted = mux.ExtractFrames(inserted);

        Assert.That(insertedPayloads[0], Is.EqualTo(sourceFrames[0]));
        Assert.That(insertedPayloads[2], Is.EqualTo(sourceFrames[1]));
        Assert.That(extracted[1].EncodedBitstream, Is.EqualTo(insertedFrame.EncodedBitstream));
        Assert.That(extracted[1].AlphaData, Is.EqualTo(insertedFrame.AlphaData));
        Assert.That(extracted[1].UnknownChunks[0].Data, Is.EqualTo(new byte[] { 7 }));
        Assert.That(ReadExtended(inserted).Vp8X.HasAlpha, Is.True);

        WebpFrame opaqueReplacement = NewLosslessFrame(2, 2, 2, 2, 31);
        byte[] replaced = mux.ReplaceAnimationFrame(inserted, 1, opaqueReplacement);
        List<byte[]> replacedPayloads = GetPayloads(replaced, WebpConstants.FourCcAnimationFrameString);

        Assert.That(replacedPayloads[0], Is.EqualTo(sourceFrames[0]));
        Assert.That(replacedPayloads[2], Is.EqualTo(sourceFrames[1]));
        Assert.That(ReadExtended(replaced).Vp8X.HasAlpha, Is.False);

        byte[] removed = mux.RemoveAnimationFrame(replaced, 1);
        Assert.That(GetPayloads(removed, WebpConstants.FourCcAnimationFrameString), Is.EqualTo(sourceFrames));
        AssertAnimationAncillaryState(removed, mux);
    }

    [Test]
    public void LoopAndBackgroundEditsChangeOnlyTheAnimPayload()
    {
        byte[] source = NewAnimation();
        List<WebpRiffChunk> before = GetChunks(source);
        IWebpMux mux = new WebpCodec();

        byte[] changed = mux.SetAnimationLoopAndBackground(source, 65535, 0xA1B2C3D4U);
        List<WebpRiffChunk> after = GetChunks(changed);
        WebpExtendedContainerParseResult parsed = ReadExtended(changed);

        Assert.That(parsed.AnimationLoopCount, Is.EqualTo(65535));
        Assert.That(parsed.AnimationBackgroundColorBgra, Is.EqualTo(0xA1B2C3D4U));
        Assert.That(after, Has.Count.EqualTo(before.Count));
        int index = 0;
        while (index < before.Count)
        {
            Assert.That(after[index].FourCc, Is.EqualTo(before[index].FourCc));
            if (before[index].FourCc != WebpConstants.FourCcAnimString)
            {
                Assert.That(after[index].Payload, Is.EqualTo(before[index].Payload));
            }

            index = index + 1;
        }
    }

    [Test]
    public void NormalizationIsIdempotentAndNeverRecompressesImagePayloads()
    {
        byte[] imagePayload = NewVp8Payload(6, 4);
        byte[] vp8x = WebpContainerWriter.CreateVp8XPayload(6, 4, new WebpMetadata(), false, false);
        vp8x[0] = 0xFF;
        vp8x[1] = 0x7F;
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk(WebpConstants.FourCcXmpString, new byte[] { 9 }));
        chunks.Add(new WebpRiffChunk("keep", new byte[] { 8, 7, 6 }));
        chunks.Add(new WebpRiffChunk(WebpConstants.FourCcVp8String, imagePayload));
        chunks.Add(new WebpRiffChunk(WebpConstants.FourCcIccProfileString, new byte[] { 1, 2 }));
        chunks.Add(new WebpRiffChunk(WebpConstants.FourCcExifString, new byte[] { 3, 4, 5 }));
        chunks.Add(new WebpRiffChunk(WebpConstants.FourCcVp8XString, vp8x));
        byte[] source = WebpUnknownChunkMux.WritePreservingOrder(WebpConstants.FourCcWebpString, chunks);
        IWebpMux mux = new WebpCodec();

        WebpNormalizationResult first = mux.NormalizeContainer(source);
        WebpNormalizationResult second = mux.NormalizeContainerSpan(first.Data);

        Assert.That(first.IsNormalized, Is.True);
        Assert.That(first.WasChanged, Is.True);
        Assert.That(first.Diagnostics, Is.Empty);
        Assert.That(GetPayload(first.Data, WebpConstants.FourCcVp8String), Is.EqualTo(imagePayload));
        Assert.That(GetPayload(first.Data, "keep"), Is.EqualTo(new byte[] { 8, 7, 6 }));
        Assert.That(mux.GetMetadata(first.Data).IccProfile, Is.EqualTo(new byte[] { 1, 2 }));
        Assert.That(mux.GetMetadata(first.Data).Exif, Is.EqualTo(new byte[] { 3, 4, 5 }));
        Assert.That(mux.GetMetadata(first.Data).Xmp, Is.EqualTo(new byte[] { 9 }));
        AssertMetadataFlags(ReadExtended(first.Data), true, true, true);
        Assert.That(second.IsNormalized, Is.True);
        Assert.That(second.WasChanged, Is.False);
        Assert.That(second.Data, Is.EqualTo(first.Data));
    }

    static void AssertMetadataState(byte[] data, bool hasIcc, bool hasExif, bool hasXmp, byte[] imagePayload)
    {
        WebpExtendedContainerParseResult parsed = ReadExtended(data);
        AssertMetadataFlags(parsed, hasIcc, hasExif, hasXmp);
        Assert.That(parsed.Vp8X.CanvasWidth, Is.EqualTo(7));
        Assert.That(parsed.Vp8X.CanvasHeight, Is.EqualTo(5));
        Assert.That(GetPayload(data, WebpConstants.FourCcVp8String), Is.EqualTo(imagePayload));
        Assert.That(new WebpCodec().Validate(data, new WebpDecodeOptions()).IsValid, Is.True);
    }

    static void AssertMetadataFlags(WebpExtendedContainerParseResult parsed, bool hasIcc, bool hasExif, bool hasXmp)
    {
        Assert.That(parsed.Vp8X.HasIccProfile, Is.EqualTo(hasIcc));
        Assert.That(parsed.Vp8X.HasExif, Is.EqualTo(hasExif));
        Assert.That(parsed.Vp8X.HasXmp, Is.EqualTo(hasXmp));
        Assert.That(parsed.Vp8X.HasAlpha, Is.False);
        Assert.That(parsed.Vp8X.HasAnimation, Is.False);
        Assert.That(parsed.Vp8X.HasReservedFeatureBits, Is.False);
        Assert.That(parsed.Vp8X.HasReservedBytes, Is.False);
    }

    static void AssertRecognizedChunksEqual(byte[] actual, List<WebpRiffChunk> expected)
    {
        List<WebpRiffChunk> actualChunks = GetRecognizedChunks(actual);
        Assert.That(actualChunks, Has.Count.EqualTo(expected.Count));
        int index = 0;
        while (index < expected.Count)
        {
            Assert.That(actualChunks[index].FourCc, Is.EqualTo(expected[index].FourCc));
            Assert.That(actualChunks[index].Payload, Is.EqualTo(expected[index].Payload));
            index = index + 1;
        }
    }

    static void AssertAnimationAncillaryState(byte[] data, IWebpMux mux)
    {
        WebpExtendedContainerParseResult parsed = ReadExtended(data);
        WebpMetadata metadata = mux.GetMetadata(data);
        List<WebpUnknownChunk> unknown = mux.EnumerateUnknownChunks(data);
        Assert.That(parsed.AnimationLoopCount, Is.EqualTo(4));
        Assert.That(parsed.AnimationBackgroundColorBgra, Is.EqualTo(0xFF010203U));
        Assert.That(metadata.IccProfile, Is.EqualTo(new byte[] { 1 }));
        Assert.That(metadata.Exif, Is.EqualTo(new byte[] { 2 }));
        Assert.That(metadata.Xmp, Is.EqualTo(new byte[] { 3 }));
        Assert.That(unknown, Has.Count.EqualTo(1));
        Assert.That(unknown[0].FourCc, Is.EqualTo("top1"));
        Assert.That(unknown[0].Data, Is.EqualTo(new byte[] { 4 }));
    }

    static WebpExtendedContainerParseResult ReadExtended(byte[] data)
    {
        return WebpExtendedContainerParser.Parse(data, WebpStrictness.Strict);
    }

    static List<WebpRiffChunk> GetRecognizedChunks(byte[] data)
    {
        List<WebpRiffChunk> source = GetChunks(data);
        List<WebpRiffChunk> recognized = new List<WebpRiffChunk>();
        int index = 0;
        while (index < source.Count)
        {
            if (WebpUnknownChunkMux.IsKnownWebpChunk(source[index].FourCc))
            {
                recognized.Add(source[index]);
            }

            index = index + 1;
        }

        return recognized;
    }

    static List<WebpRiffChunk> GetChunks(byte[] data)
    {
        WebpRiffParseResult riff = WebpRiffParser.Parse(data);
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        int index = 0;
        while (index < riff.Chunks.Count)
        {
            WebpChunkInfo chunk = riff.Chunks[index];
            chunks.Add(new WebpRiffChunk(chunk.FourCc, WebpEndianReader.ReadSlice(data, (int)chunk.PayloadOffset, (int)chunk.PayloadLength).ToArray()));
            index = index + 1;
        }

        return chunks;
    }

    static List<string> GetFourCcs(byte[] data)
    {
        List<WebpRiffChunk> chunks = GetChunks(data);
        List<string> names = new List<string>();
        int index = 0;
        while (index < chunks.Count)
        {
            names.Add(chunks[index].FourCc);
            index = index + 1;
        }

        return names;
    }

    static List<byte[]> GetPayloads(byte[] data, string fourCc)
    {
        List<WebpRiffChunk> chunks = GetChunks(data);
        List<byte[]> payloads = new List<byte[]>();
        int index = 0;
        while (index < chunks.Count)
        {
            if (chunks[index].FourCc == fourCc)
            {
                payloads.Add(chunks[index].Payload);
            }

            index = index + 1;
        }

        return payloads;
    }

    static byte[] GetPayload(byte[] data, string fourCc)
    {
        List<byte[]> payloads = GetPayloads(data, fourCc);
        if (payloads.Count == 0)
        {
            throw new AssertionException("chunk not found: " + fourCc);
        }

        return payloads[0];
    }

    static byte[] NewAnimation()
    {
        WebpImage image = new WebpImage();
        image.CanvasWidth = 8;
        image.CanvasHeight = 8;
        image.HasAnimation = true;
        image.LoopCount = 4;
        image.BackgroundColorBgra = 0xFF010203U;
        image.Metadata.IccProfile = new byte[] { 1 };
        image.Metadata.Exif = new byte[] { 2 };
        image.Metadata.Xmp = new byte[] { 3 };
        image.UnknownChunks.Add(new WebpUnknownChunk("top1", new byte[] { 4 }));
        image.Frames.Add(NewLossyFrame(0, 0, 2, 2, 10));
        image.Frames.Add(NewLosslessFrame(4, 4, 2, 2, 20));
        return WebpContainerWriter.WriteAnimated(image);
    }

    static WebpFrame NewLossyFrame(int x, int y, int width, int height, int duration)
    {
        WebpFrame frame = new WebpFrame();
        frame.X = x;
        frame.Y = y;
        frame.Width = width;
        frame.Height = height;
        frame.DurationMilliseconds = duration;
        frame.BitstreamKind = WebpBitstreamKind.Lossy;
        frame.EncodedBitstream = NewVp8Payload(width, height);
        return frame;
    }

    static WebpFrame NewLosslessFrame(int x, int y, int width, int height, int duration)
    {
        WebpFrame frame = new WebpFrame();
        frame.X = x;
        frame.Y = y;
        frame.Width = width;
        frame.Height = height;
        frame.DurationMilliseconds = duration;
        frame.BitstreamKind = WebpBitstreamKind.Lossless;
        frame.EncodedBitstream = NewVp8LPayload(width, height);
        return frame;
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

    static byte[] NewVp8LPayload(int width, int height)
    {
        byte[] payload = new byte[WebpConstants.Vp8LHeaderSize];
        uint bits = (uint)(width - 1);
        bits = bits | ((uint)(height - 1) << 14);
        payload[0] = WebpConstants.Vp8LSignature;
        WebpEndianWriter.WriteUInt32(payload, 1, bits);
        return payload;
    }
}
