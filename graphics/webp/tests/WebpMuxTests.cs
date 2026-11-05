using webp;

namespace webp.Tests;

[TestFixture]
public class WebpMuxTests
{
    [Test]
    public void ExtractMetadata_ReturnsRawMetadataCopies()
    {
        WebpMetadata metadata = NewMetadata(new byte[] { 1 }, new byte[] { 2 }, new byte[] { 3 });
        byte[] data = WebpContainerWriter.WriteExtendedStill(NewVp8Payload(4, 5), WebpBitstreamKind.Lossy, metadata, null);

        WebpMetadata extracted = WebpMux.ExtractMetadata(data);
        data[data.Length - 2] = 0xFF;

        Assert.That(extracted.IccProfile, Is.EqualTo(new byte[] { 1 }));
        Assert.That(extracted.Exif, Is.EqualTo(new byte[] { 2 }));
        Assert.That(extracted.Xmp, Is.EqualTo(new byte[] { 3 }));
    }

    [Test]
    public void ReplaceMetadata_OnBasicStillImageAddsVp8XAndPreservesImagePayload()
    {
        byte[] imagePayload = NewVp8Payload(7, 9);
        byte[] data = WebpContainerWriter.WriteBasicStill(imagePayload, WebpBitstreamKind.Lossy);
        WebpMetadata metadata = NewMetadata(null, new byte[] { 0xEE }, null);

        byte[] replaced = WebpMux.ReplaceMetadata(data, metadata);
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(replaced, WebpStrictness.Strict);
        WebpValidationResult validation = WebpValidator.Validate(replaced);

        Assert.That(validation.IsValid, Is.True);
        Assert.That(parsed.HasVp8X, Is.True);
        Assert.That(parsed.Vp8X.CanvasWidth, Is.EqualTo(7));
        Assert.That(parsed.Vp8X.CanvasHeight, Is.EqualTo(9));
        Assert.That(parsed.Vp8X.HasExif, Is.True);
        Assert.That(parsed.Metadata.Exif, Is.EqualTo(new byte[] { 0xEE }));
        Assert.That(parsed.ImagePayload, Is.EqualTo(imagePayload));
    }

    [Test]
    public void ReplaceMetadata_OnExtendedStillPreservesImageAlphaAndUnknownPayloads()
    {
        WebpImage image = NewStillImage();
        byte[] data = WebpContainerWriter.WriteExtendedStill(image);
        byte[] beforeImage = GetChunkPayload(data, "VP8 ", 0);
        byte[] beforeAlpha = GetChunkPayload(data, "ALPH", 0);
        byte[] beforeUnknown = GetChunkPayload(data, "zzzz", 0);
        WebpMetadata replacement = NewMetadata(new byte[] { 8 }, null, new byte[] { 9 });

        byte[] replaced = WebpMux.ReplaceMetadata(data, replacement);

        Assert.That(GetChunkPayload(replaced, "VP8 ", 0), Is.EqualTo(beforeImage));
        Assert.That(GetChunkPayload(replaced, "ALPH", 0), Is.EqualTo(beforeAlpha));
        Assert.That(GetChunkPayload(replaced, "zzzz", 0), Is.EqualTo(beforeUnknown));
        Assert.That(WebpMux.ExtractMetadata(replaced).IccProfile, Is.EqualTo(new byte[] { 8 }));
        Assert.That(WebpMux.ExtractMetadata(replaced).Exif, Is.Null);
        Assert.That(WebpMux.ExtractMetadata(replaced).Xmp, Is.EqualTo(new byte[] { 9 }));
    }

    [Test]
    public void ReplaceMetadata_OnAnimationPreservesRawAnmfPayloads()
    {
        byte[] data = NewAnimatedContainer();
        byte[] firstFrameBefore = GetChunkPayload(data, "ANMF", 0);
        byte[] secondFrameBefore = GetChunkPayload(data, "ANMF", 1);
        WebpMetadata replacement = NewMetadata(new byte[] { 4 }, new byte[] { 5 }, null);

        byte[] replaced = WebpMux.ReplaceMetadata(data, replacement);
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(replaced, WebpStrictness.Strict);

        Assert.That(WebpValidator.Validate(replaced).IsValid, Is.True);
        Assert.That(GetChunkPayload(replaced, "ANMF", 0), Is.EqualTo(firstFrameBefore));
        Assert.That(GetChunkPayload(replaced, "ANMF", 1), Is.EqualTo(secondFrameBefore));
        Assert.That(parsed.Vp8X.HasAnimation, Is.True);
        Assert.That(parsed.Metadata.IccProfile, Is.EqualTo(new byte[] { 4 }));
        Assert.That(parsed.Metadata.Exif, Is.EqualTo(new byte[] { 5 }));
        Assert.That(parsed.Metadata.Xmp, Is.Null);
        Assert.That(parsed.Frames, Has.Count.EqualTo(2));
    }

    [Test]
    public void StripMetadata_RemovesMetadataFlagsAndPreservesAnimationFrames()
    {
        byte[] data = NewAnimatedContainer();
        byte[] firstFrameBefore = GetChunkPayload(data, "ANMF", 0);

        byte[] stripped = WebpMux.StripMetadata(data);
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(stripped, WebpStrictness.Strict);
        WebpMetadata metadata = WebpMux.GetMetadata(stripped);

        Assert.That(WebpValidator.Validate(stripped).IsValid, Is.True);
        Assert.That(GetChunkPayload(stripped, "ANMF", 0), Is.EqualTo(firstFrameBefore));
        Assert.That(parsed.Vp8X.HasIccProfile, Is.False);
        Assert.That(parsed.Vp8X.HasExif, Is.False);
        Assert.That(parsed.Vp8X.HasXmp, Is.False);
        Assert.That(parsed.Vp8X.HasAnimation, Is.True);
        Assert.That(metadata.IccProfile, Is.Null);
        Assert.That(metadata.Exif, Is.Null);
        Assert.That(metadata.Xmp, Is.Null);
    }

    [Test]
    public void SetIndividualMetadataFieldsPreservesOtherMetadataAndCopiesInputs()
    {
        byte[] data = WebpContainerWriter.WriteExtendedStill(NewVp8Payload(3, 4), WebpBitstreamKind.Lossy, NewMetadata(null, new byte[] { 2 }, new byte[] { 3 }), null);
        byte[] icc = new byte[] { 1 };

        byte[] withIcc = WebpMux.SetIccProfile(data, icc);
        icc[0] = 99;
        byte[] withoutExif = WebpMux.SetExif(withIcc, null);
        WebpMetadata metadata = WebpMux.GetMetadata(withoutExif);

        Assert.That(metadata.IccProfile, Is.EqualTo(new byte[] { 1 }));
        Assert.That(metadata.Exif, Is.Null);
        Assert.That(metadata.Xmp, Is.EqualTo(new byte[] { 3 }));
    }

    [Test]
    public void MetadataOperationsRejectInvalidInput()
    {
        Assert.Throws<ArgumentException>(() => WebpMux.ExtractMetadata(new byte[] { 1, 2, 3 }));
        Assert.Throws<ArgumentException>(() => WebpMux.ReplaceMetadata(new byte[] { 1, 2, 3 }, new WebpMetadata()));
        Assert.Throws<ArgumentException>(() => WebpMux.StripMetadata(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void ExtractFrames_ReturnsEncodedFrameCopies()
    {
        byte[] data = NewAnimatedContainerWithFrameDetails();

        List<WebpFrame> frames = WebpMux.ExtractFrames(data);
        frames[0].EncodedBitstream![0] = 99;
        frames[0].AlphaData![0] = 88;
        frames[0].UnknownChunks[0].Data[0] = 77;
        WebpFrame first = WebpMux.ExtractFrame(data, 0);

        Assert.That(frames, Has.Count.EqualTo(2));
        Assert.That(first.X, Is.EqualTo(2));
        Assert.That(first.Y, Is.EqualTo(4));
        Assert.That(first.Width, Is.EqualTo(4));
        Assert.That(first.Height, Is.EqualTo(5));
        Assert.That(first.DurationMilliseconds, Is.EqualTo(10));
        Assert.That(first.BlendMode, Is.EqualTo(WebpFrameBlendMode.Replace));
        Assert.That(first.DisposeMode, Is.EqualTo(WebpFrameDisposeMode.DisposeToBackground));
        Assert.That(first.BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossy));
        Assert.That(first.EncodedBitstream, Is.EqualTo(NewVp8Payload(4, 5)));
        Assert.That(first.AlphaData, Is.EqualTo(new byte[] { 0 }));
        Assert.That(first.UnknownChunks, Has.Count.EqualTo(1));
        Assert.That(first.UnknownChunks[0].FourCc, Is.EqualTo("frmx"));
        Assert.That(first.UnknownChunks[0].Data, Is.EqualTo(new byte[] { 7 }));
    }

    [Test]
    public void ExtractFrame_RejectsNonAnimatedInputAndInvalidIndexes()
    {
        byte[] basic = WebpContainerWriter.WriteBasicStill(NewVp8Payload(1, 1), WebpBitstreamKind.Lossy);
        byte[] animated = NewAnimatedContainer();

        Assert.Throws<ArgumentException>(() => WebpMux.ExtractFrames(basic));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpMux.ExtractFrame(animated, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpMux.ExtractFrame(animated, 2));
    }

    [Test]
    public void SetAnimationLoopCount_UpdatesOnlyLoopCountAndPreservesFrames()
    {
        byte[] data = NewAnimatedContainer();
        byte[] firstFrameBefore = GetChunkPayload(data, "ANMF", 0);
        WebpExtendedContainerParseResult before = WebpExtendedContainerParser.Parse(data, WebpStrictness.Strict);

        byte[] changed = WebpMux.SetAnimationLoopCount(data, 7);
        WebpExtendedContainerParseResult after = WebpExtendedContainerParser.Parse(changed, WebpStrictness.Strict);

        Assert.That(WebpValidator.Validate(changed).IsValid, Is.True);
        Assert.That(after.AnimationLoopCount, Is.EqualTo(7));
        Assert.That(after.AnimationBackgroundColorBgra, Is.EqualTo(before.AnimationBackgroundColorBgra));
        Assert.That(GetChunkPayload(changed, "ANMF", 0), Is.EqualTo(firstFrameBefore));
    }

    [Test]
    public void SetAnimationBackgroundColor_UpdatesOnlyBackgroundAndPreservesFrames()
    {
        byte[] data = NewAnimatedContainer();
        byte[] secondFrameBefore = GetChunkPayload(data, "ANMF", 1);
        WebpExtendedContainerParseResult before = WebpExtendedContainerParser.Parse(data, WebpStrictness.Strict);

        byte[] changed = WebpMux.SetAnimationBackgroundColor(data, 0xAABBCCDDU);
        WebpExtendedContainerParseResult after = WebpExtendedContainerParser.Parse(changed, WebpStrictness.Strict);

        Assert.That(WebpValidator.Validate(changed).IsValid, Is.True);
        Assert.That(after.AnimationBackgroundColorBgra, Is.EqualTo(0xAABBCCDDU));
        Assert.That(after.AnimationLoopCount, Is.EqualTo(before.AnimationLoopCount));
        Assert.That(GetChunkPayload(changed, "ANMF", 1), Is.EqualTo(secondFrameBefore));
    }

    [Test]
    public void SetAnimationLoopAndBackground_UpdatesBothAnimFields()
    {
        byte[] data = NewAnimatedContainer();

        byte[] changed = WebpMux.SetAnimationLoopAndBackground(data, 0, 0x01020304U);
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(changed, WebpStrictness.Strict);

        Assert.That(WebpValidator.Validate(changed).IsValid, Is.True);
        Assert.That(parsed.AnimationLoopCount, Is.EqualTo(0));
        Assert.That(parsed.AnimationBackgroundColorBgra, Is.EqualTo(0x01020304U));
    }

    [Test]
    public void AnimationEditsRejectNonAnimatedInput()
    {
        byte[] basic = WebpContainerWriter.WriteBasicStill(NewVp8Payload(1, 1), WebpBitstreamKind.Lossy);

        Assert.Throws<ArgumentException>(() => WebpMux.SetAnimationLoopCount(basic, 1));
        Assert.Throws<ArgumentException>(() => WebpMux.SetAnimationBackgroundColor(basic, 0));
        Assert.Throws<ArgumentException>(() => WebpMux.SetAnimationLoopAndBackground(basic, 1, 0));
    }

    static byte[] GetChunkPayload(byte[] data, string fourCc, int occurrence)
    {
        WebpRiffParseResult riff = WebpRiffParser.Parse(data);
        int seen = 0;
        int index = 0;
        while (index < riff.Chunks.Count)
        {
            WebpChunkInfo chunk = riff.Chunks[index];
            if (chunk.FourCc == fourCc)
            {
                if (seen == occurrence)
                {
                    return WebpEndianReader.ReadSlice(data, (int)chunk.PayloadOffset, (int)chunk.PayloadLength).ToArray();
                }

                seen = seen + 1;
            }

            index = index + 1;
        }

        throw new InvalidOperationException("chunk was not found");
    }

    static WebpImage NewStillImage()
    {
        WebpImage image = new WebpImage();
        image.CanvasWidth = 8;
        image.CanvasHeight = 9;
        image.HasAlpha = true;
        image.Metadata = NewMetadata(new byte[] { 1 }, new byte[] { 2 }, null);
        image.UnknownChunks.Add(new WebpUnknownChunk("zzzz", new byte[] { 7 }));
        WebpFrame frame = NewFrame(NewVp8Payload(8, 9), WebpBitstreamKind.Lossy);
        frame.AlphaData = new byte[] { 0 };
        image.Frames.Add(frame);
        return image;
    }

    static byte[] NewAnimatedContainer()
    {
        WebpImage image = new WebpImage();
        image.CanvasWidth = 20;
        image.CanvasHeight = 20;
        image.HasAnimation = true;
        image.LoopCount = 3;
        image.BackgroundColorBgra = 0x11223344U;
        image.Metadata = NewMetadata(new byte[] { 1 }, null, new byte[] { 3 });

        WebpFrame first = NewFrame(NewVp8Payload(4, 5), WebpBitstreamKind.Lossy);
        first.DurationMilliseconds = 10;
        image.Frames.Add(first);

        WebpFrame second = NewFrame(NewVp8LPayload(3, 4, false, 0), WebpBitstreamKind.Lossless);
        second.X = 6;
        second.Y = 8;
        second.DurationMilliseconds = 20;
        image.Frames.Add(second);

        return WebpContainerWriter.WriteAnimated(image);
    }

    static byte[] NewAnimatedContainerWithFrameDetails()
    {
        WebpImage image = new WebpImage();
        image.CanvasWidth = 20;
        image.CanvasHeight = 20;
        image.HasAnimation = true;
        image.HasAlpha = true;

        WebpFrame first = NewFrame(NewVp8Payload(4, 5), WebpBitstreamKind.Lossy);
        first.X = 2;
        first.Y = 4;
        first.DurationMilliseconds = 10;
        first.BlendMode = WebpFrameBlendMode.Replace;
        first.DisposeMode = WebpFrameDisposeMode.DisposeToBackground;
        first.AlphaData = new byte[] { 0 };
        first.UnknownChunks.Add(new WebpUnknownChunk("frmx", new byte[] { 7 }));
        image.Frames.Add(first);

        WebpFrame second = NewFrame(NewVp8Payload(2, 3), WebpBitstreamKind.Lossy);
        second.DurationMilliseconds = 20;
        image.Frames.Add(second);

        return WebpContainerWriter.WriteAnimated(image);
    }

    static WebpFrame NewFrame(byte[] payload, WebpBitstreamKind bitstreamKind)
    {
        WebpBasicContainerParseResult parsed = ParsePayload(payload, bitstreamKind);
        WebpFrame frame = new WebpFrame();
        frame.Width = parsed.Width;
        frame.Height = parsed.Height;
        frame.BitstreamKind = bitstreamKind;
        frame.EncodedBitstream = payload;
        return frame;
    }

    static WebpBasicContainerParseResult ParsePayload(byte[] payload, WebpBitstreamKind bitstreamKind)
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        if (bitstreamKind == WebpBitstreamKind.Lossy)
        {
            chunks.Add(new WebpRiffChunk("VP8 ", payload));
        }
        else
        {
            chunks.Add(new WebpRiffChunk("VP8L", payload));
        }

        return WebpBasicContainerParser.Parse(WebpRiffWriter.Write("WEBP", chunks), WebpStrictness.Strict);
    }

    static WebpMetadata NewMetadata(byte[]? icc, byte[]? exif, byte[]? xmp)
    {
        WebpMetadata metadata = new WebpMetadata();
        metadata.IccProfile = icc;
        metadata.Exif = exif;
        metadata.Xmp = xmp;
        return metadata;
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

    static byte[] NewVp8LPayload(int width, int height, bool alpha, int version)
    {
        byte[] payload = new byte[WebpConstants.Vp8LHeaderSize];
        uint bits = (uint)(width - 1);
        bits = bits | ((uint)(height - 1) << 14);
        if (alpha)
        {
            bits = bits | (1U << 28);
        }

        bits = bits | ((uint)version << 29);
        payload[0] = WebpConstants.Vp8LSignature;
        WebpEndianWriter.WriteUInt32(payload, 1, bits);
        return payload;
    }
}
