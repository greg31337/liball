using webp;

namespace webp.Tests;

[TestFixture]
public class WebpContainerWriterTests
{
    [Test]
    public void WriteBasicStill_WritesLossyVp8Container()
    {
        byte[] payload = NewVp8Payload(64, 48);

        byte[] data = WebpContainerWriter.WriteBasicStill(payload, WebpBitstreamKind.Lossy);
        WebpBasicContainerParseResult parsed = WebpBasicContainerParser.Parse(data, WebpStrictness.Strict);
        WebpRiffParseResult riff = WebpRiffParser.Parse(data);

        Assert.That(parsed.IsValid, Is.True);
        Assert.That(parsed.ContainerKind, Is.EqualTo(WebpContainerKind.SimpleLossy));
        Assert.That(parsed.BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossy));
        Assert.That(parsed.Width, Is.EqualTo(64));
        Assert.That(parsed.Height, Is.EqualTo(48));
        Assert.That(parsed.ImagePayload, Is.EqualTo(payload));
        Assert.That(parsed.ImagePayload, Is.Not.SameAs(payload));
        Assert.That(riff.Chunks, Has.Count.EqualTo(1));
        Assert.That(riff.Chunks[0].FourCc, Is.EqualTo("VP8 "));
    }

    [Test]
    public void WriteBasicStill_WritesLosslessVp8LContainerWithAlphaHint()
    {
        byte[] payload = NewVp8LPayload(17, 33, true, 0);

        byte[] data = WebpContainerWriter.WriteBasicStill(payload, WebpBitstreamKind.Lossless);
        WebpInfo info = WebpInfoReader.Read(data);

        Assert.That(info.ContainerKind, Is.EqualTo(WebpContainerKind.SimpleLossless));
        Assert.That(info.BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
        Assert.That(info.Width, Is.EqualTo(17));
        Assert.That(info.Height, Is.EqualTo(33));
        Assert.That(info.HasAlpha, Is.True);
        Assert.That(info.Chunks, Has.Count.EqualTo(1));
        Assert.That(info.Chunks[0].FourCc, Is.EqualTo("VP8L"));
    }

    [Test]
    public void WriteBasicStill_AddsPaddingForOddPayloadLengths()
    {
        byte[] payload = NewVp8Payload(2, 3);
        Array.Resize(ref payload, payload.Length + 1);
        payload[payload.Length - 1] = 99;

        byte[] data = WebpContainerWriter.WriteBasicStill(payload, WebpBitstreamKind.Lossy);
        WebpRiffParseResult riff = WebpRiffParser.Parse(data);

        Assert.That(riff.IsValid, Is.True);
        Assert.That(riff.Chunks[0].PayloadLength, Is.EqualTo(11));
        Assert.That(riff.Chunks[0].HasPadding, Is.True);
        Assert.That(data[data.Length - 1], Is.EqualTo(0));
    }

    [Test]
    public void WriteBasicStill_CopiesEncodedPayloadBytes()
    {
        byte[] payload = NewVp8Payload(4, 5);

        byte[] data = WebpContainerWriter.WriteBasicStill(payload, WebpBitstreamKind.Lossy);
        payload[3] = 0;
        WebpBasicContainerParseResult parsed = WebpBasicContainerParser.Parse(data, WebpStrictness.Strict);

        Assert.That(parsed.IsValid, Is.True);
        Assert.That(parsed.ImagePayload[3], Is.EqualTo(WebpConstants.Vp8LossyKeyFrameSignature0));
    }

    [Test]
    public void WriteBasicStill_WritesSingleFrameImageModel()
    {
        WebpImage image = new WebpImage();
        image.CanvasWidth = 8;
        image.CanvasHeight = 9;
        WebpFrame frame = new WebpFrame();
        frame.Width = 8;
        frame.Height = 9;
        frame.BitstreamKind = WebpBitstreamKind.Lossy;
        frame.EncodedBitstream = NewVp8Payload(8, 9);
        image.Frames.Add(frame);

        byte[] data = WebpContainerWriter.WriteBasicStill(image);
        WebpValidationResult validation = WebpValidator.Validate(data);

        Assert.That(validation.IsValid, Is.True);
        Assert.That(WebpInfoReader.Read(data).ContainerKind, Is.EqualTo(WebpContainerKind.SimpleLossy));
    }

    [Test]
    public void WriteBasicStill_RejectsInvalidBitstreamKindAndPayload()
    {
        Assert.Throws<ArgumentException>(() => WebpContainerWriter.WriteBasicStill(new byte[] { 1 }, WebpBitstreamKind.Unknown));
        Assert.Throws<ArgumentException>(() => WebpContainerWriter.WriteBasicStill(new byte[] { 1 }, WebpBitstreamKind.Lossy));
    }

    [Test]
    public void WriteBasicStill_RejectsFrameStateThatRequiresExtendedContainer()
    {
        WebpFrame offset = NewFrame(NewVp8Payload(1, 1), WebpBitstreamKind.Lossy);
        offset.X = 2;
        WebpFrame alpha = NewFrame(NewVp8Payload(1, 1), WebpBitstreamKind.Lossy);
        alpha.AlphaData = new byte[] { 1 };
        WebpFrame unknown = NewFrame(NewVp8Payload(1, 1), WebpBitstreamKind.Lossy);
        unknown.UnknownChunks.Add(new WebpUnknownChunk("frmx", new byte[] { 1 }));

        Assert.Throws<ArgumentException>(() => WebpContainerWriter.WriteBasicStill(offset));
        Assert.Throws<ArgumentException>(() => WebpContainerWriter.WriteBasicStill(alpha));
        Assert.Throws<ArgumentException>(() => WebpContainerWriter.WriteBasicStill(unknown));
    }

    [Test]
    public void WriteBasicStill_RejectsImageStateThatRequiresExtendedContainer()
    {
        WebpImage metadata = NewImage(NewVp8Payload(1, 1), WebpBitstreamKind.Lossy);
        metadata.Metadata.Exif = new byte[] { 1 };
        WebpImage animation = NewImage(NewVp8Payload(1, 1), WebpBitstreamKind.Lossy);
        animation.HasAnimation = true;
        WebpImage unknown = NewImage(NewVp8Payload(1, 1), WebpBitstreamKind.Lossy);
        unknown.UnknownChunks.Add(new WebpUnknownChunk("zzzz", new byte[] { 1 }));
        WebpImage lossyAlpha = NewImage(NewVp8Payload(1, 1), WebpBitstreamKind.Lossy);
        lossyAlpha.HasAlpha = true;

        Assert.Throws<ArgumentException>(() => WebpContainerWriter.WriteBasicStill(metadata));
        Assert.Throws<ArgumentException>(() => WebpContainerWriter.WriteBasicStill(animation));
        Assert.Throws<ArgumentException>(() => WebpContainerWriter.WriteBasicStill(unknown));
        Assert.Throws<ArgumentException>(() => WebpContainerWriter.WriteBasicStill(lossyAlpha));
    }

    [Test]
    public void WriteBasicStill_RejectsImageDimensionMismatch()
    {
        WebpImage image = NewImage(NewVp8Payload(3, 4), WebpBitstreamKind.Lossy);
        image.CanvasWidth = 9;

        Assert.Throws<ArgumentException>(() => WebpContainerWriter.WriteBasicStill(image));
    }

    [Test]
    public void WriteExtendedStill_WritesLossyContainerWithMetadataAndAlpha()
    {
        byte[] payload = NewVp8Payload(64, 48);
        byte[] alpha = new byte[] { 4, 5 };
        WebpMetadata metadata = NewMetadata(new byte[] { 1 }, new byte[] { 2 }, new byte[] { 3 });

        byte[] data = WebpContainerWriter.WriteExtendedStill(payload, WebpBitstreamKind.Lossy, metadata, alpha);
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(data, WebpStrictness.Strict);
        WebpValidationResult validation = WebpValidator.Validate(data);

        Assert.That(validation.IsValid, Is.True);
        Assert.That(parsed.IsValid, Is.True);
        Assert.That(parsed.Vp8X.CanvasWidth, Is.EqualTo(64));
        Assert.That(parsed.Vp8X.CanvasHeight, Is.EqualTo(48));
        Assert.That(parsed.Vp8X.HasIccProfile, Is.True);
        Assert.That(parsed.Vp8X.HasAlpha, Is.True);
        Assert.That(parsed.Vp8X.HasExif, Is.True);
        Assert.That(parsed.Vp8X.HasXmp, Is.True);
        Assert.That(parsed.Metadata.IccProfile, Is.EqualTo(new byte[] { 1 }));
        Assert.That(parsed.Metadata.Exif, Is.EqualTo(new byte[] { 2 }));
        Assert.That(parsed.Metadata.Xmp, Is.EqualTo(new byte[] { 3 }));
        Assert.That(parsed.AlphaData, Is.EqualTo(alpha));
        Assert.That(parsed.ImagePayload, Is.EqualTo(payload));
        Assert.That(parsed.Chunks[0].FourCc, Is.EqualTo("VP8X"));
        Assert.That(parsed.Chunks[1].FourCc, Is.EqualTo("ICCP"));
        Assert.That(parsed.Chunks[2].FourCc, Is.EqualTo("ALPH"));
        Assert.That(parsed.Chunks[3].FourCc, Is.EqualTo("VP8 "));
        Assert.That(parsed.Chunks[4].FourCc, Is.EqualTo("EXIF"));
        Assert.That(parsed.Chunks[5].FourCc, Is.EqualTo("XMP "));
    }

    [Test]
    public void WriteExtendedStill_CopiesMetadataAlphaAndImagePayloads()
    {
        byte[] payload = NewVp8Payload(4, 5);
        byte[] alpha = new byte[] { 0 };
        WebpMetadata metadata = NewMetadata(new byte[] { 1 }, null, null);

        byte[] data = WebpContainerWriter.WriteExtendedStill(payload, WebpBitstreamKind.Lossy, metadata, alpha);
        payload[3] = 0;
        alpha[0] = 99;
        metadata.IccProfile![0] = 88;
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(data, WebpStrictness.Strict);

        Assert.That(parsed.IsValid, Is.True);
        Assert.That(parsed.ImagePayload![3], Is.EqualTo(WebpConstants.Vp8LossyKeyFrameSignature0));
        Assert.That(parsed.AlphaData, Is.EqualTo(new byte[] { 0 }));
        Assert.That(parsed.Metadata.IccProfile, Is.EqualTo(new byte[] { 1 }));
    }

    [Test]
    public void WriteExtendedStill_WritesVp8LIntrinsicAlphaWithoutAlphChunk()
    {
        byte[] payload = NewVp8LPayload(17, 33, true, 0);
        WebpMetadata metadata = new WebpMetadata();

        byte[] data = WebpContainerWriter.WriteExtendedStill(payload, WebpBitstreamKind.Lossless, metadata, null);
        WebpInfo info = WebpInfoReader.Read(data);
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(data, WebpStrictness.Strict);

        Assert.That(info.ContainerKind, Is.EqualTo(WebpContainerKind.ExtendedStill));
        Assert.That(info.BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
        Assert.That(info.HasAlpha, Is.True);
        Assert.That(parsed.Vp8X.HasAlpha, Is.True);
        Assert.That(parsed.AlphaData, Is.Null);
        Assert.That(parsed.Chunks.Exists(chunk => chunk.FourCc == "ALPH"), Is.False);
    }

    [Test]
    public void WriteExtendedStill_WritesImageModelWithMetadataAlphaAndUnknownChunks()
    {
        WebpImage image = NewImage(NewVp8Payload(8, 9), WebpBitstreamKind.Lossy);
        image.HasAlpha = true;
        image.Metadata = NewMetadata(new byte[] { 1 }, new byte[] { 2 }, null);
        image.UnknownChunks.Add(new WebpUnknownChunk("zzzz", new byte[] { 7 }));
        image.Frames[0].AlphaData = new byte[] { 0 };

        byte[] data = WebpContainerWriter.WriteExtendedStill(image);
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(data, WebpStrictness.Strict);

        Assert.That(parsed.IsValid, Is.True);
        Assert.That(parsed.Vp8X.CanvasWidth, Is.EqualTo(8));
        Assert.That(parsed.Vp8X.CanvasHeight, Is.EqualTo(9));
        Assert.That(parsed.Metadata.IccProfile, Is.EqualTo(new byte[] { 1 }));
        Assert.That(parsed.Metadata.Exif, Is.EqualTo(new byte[] { 2 }));
        Assert.That(parsed.AlphaData, Is.EqualTo(new byte[] { 0 }));
        Assert.That(parsed.UnknownChunks, Has.Count.EqualTo(1));
        Assert.That(parsed.UnknownChunks[0].FourCc, Is.EqualTo("zzzz"));
        Assert.That(WebpValidator.Validate(data).IsValid, Is.True);
    }

    [Test]
    public void WriteExtendedStill_RejectsLosslessSideAlphaAndInvalidPayload()
    {
        WebpMetadata metadata = new WebpMetadata();

        Assert.Throws<ArgumentException>(() => WebpContainerWriter.WriteExtendedStill(NewVp8LPayload(1, 1, false, 0), WebpBitstreamKind.Lossless, metadata, new byte[] { 1 }));
        Assert.Throws<ArgumentException>(() => WebpContainerWriter.WriteExtendedStill(new byte[] { 1 }, WebpBitstreamKind.Lossy, metadata, null));
    }

    [Test]
    public void WriteExtendedStill_RejectsDimensionMismatchAndInvalidCanvas()
    {
        WebpMetadata metadata = new WebpMetadata();

        Assert.Throws<ArgumentException>(() => WebpContainerWriter.WriteExtendedStill(NewVp8Payload(3, 4), WebpBitstreamKind.Lossy, 5, 4, metadata, null));
        Assert.Throws<ArgumentException>(() => WebpContainerWriter.WriteExtendedStill(NewVp8Payload(3, 4), WebpBitstreamKind.Lossy, 0, 4, metadata, null));
    }

    [Test]
    public void WriteExtendedStill_RejectsImageStateThatRequiresAnimation()
    {
        WebpImage animation = NewImage(NewVp8Payload(1, 1), WebpBitstreamKind.Lossy);
        animation.HasAnimation = true;

        Assert.Throws<ArgumentException>(() => WebpContainerWriter.WriteExtendedStill(animation));
    }

    [Test]
    public void WriteExtendedStill_RejectsEmptyMetadataAndAlphaPayloads()
    {
        WebpMetadata emptyMetadata = new WebpMetadata();
        emptyMetadata.Exif = Array.Empty<byte>();

        Assert.Throws<ArgumentException>(() => WebpContainerWriter.WriteExtendedStill(NewVp8Payload(1, 1), WebpBitstreamKind.Lossy, emptyMetadata, null));
        Assert.Throws<ArgumentException>(() => WebpContainerWriter.WriteExtendedStill(NewVp8Payload(1, 1), WebpBitstreamKind.Lossy, new WebpMetadata(), Array.Empty<byte>()));
    }

    [Test]
    public void WriteAnimated_WritesAnimAndAnmfChunksFromImageModel()
    {
        WebpImage image = new WebpImage();
        image.CanvasWidth = 40;
        image.CanvasHeight = 30;
        image.HasAnimation = true;
        image.HasAlpha = true;
        image.LoopCount = 3;
        image.BackgroundColorBgra = 0x11223344U;
        image.Metadata = NewMetadata(new byte[] { 1 }, null, new byte[] { 3 });
        image.UnknownChunks.Add(new WebpUnknownChunk("zzzz", new byte[] { 9 }));

        WebpFrame first = NewFrame(NewVp8Payload(10, 8), WebpBitstreamKind.Lossy);
        first.X = 2;
        first.Y = 4;
        first.DurationMilliseconds = 75;
        first.DisposeMode = WebpFrameDisposeMode.DisposeToBackground;
        first.BlendMode = WebpFrameBlendMode.Replace;
        first.AlphaData = new byte[] { 0 };
        first.UnknownChunks.Add(new WebpUnknownChunk("frmx", new byte[] { 7 }));
        image.Frames.Add(first);

        WebpFrame second = NewFrame(NewVp8LPayload(5, 6, false, 0), WebpBitstreamKind.Lossless);
        second.X = 20;
        second.Y = 10;
        second.DurationMilliseconds = 120;
        image.Frames.Add(second);

        byte[] data = WebpContainerWriter.WriteAnimated(image);
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(data, WebpStrictness.Strict);
        WebpValidationResult validation = WebpValidator.Validate(data);

        Assert.That(validation.IsValid, Is.True);
        Assert.That(parsed.IsValid, Is.True);
        Assert.That(parsed.Vp8X.CanvasWidth, Is.EqualTo(40));
        Assert.That(parsed.Vp8X.CanvasHeight, Is.EqualTo(30));
        Assert.That(parsed.Vp8X.HasAnimation, Is.True);
        Assert.That(parsed.Vp8X.HasAlpha, Is.True);
        Assert.That(parsed.AnimationBackgroundColorBgra, Is.EqualTo(0x11223344U));
        Assert.That(parsed.AnimationLoopCount, Is.EqualTo(3));
        Assert.That(parsed.Metadata.IccProfile, Is.EqualTo(new byte[] { 1 }));
        Assert.That(parsed.Metadata.Xmp, Is.EqualTo(new byte[] { 3 }));
        Assert.That(parsed.Frames, Has.Count.EqualTo(2));
        Assert.That(parsed.Frames[0].X, Is.EqualTo(2));
        Assert.That(parsed.Frames[0].Y, Is.EqualTo(4));
        Assert.That(parsed.Frames[0].Width, Is.EqualTo(10));
        Assert.That(parsed.Frames[0].Height, Is.EqualTo(8));
        Assert.That(parsed.Frames[0].DurationMilliseconds, Is.EqualTo(75));
        Assert.That(parsed.Frames[0].DisposeMode, Is.EqualTo(WebpFrameDisposeMode.DisposeToBackground));
        Assert.That(parsed.Frames[0].BlendMode, Is.EqualTo(WebpFrameBlendMode.Replace));
        Assert.That(parsed.Frames[0].AlphaData, Is.EqualTo(new byte[] { 0 }));
        Assert.That(parsed.Frames[0].EncodedBitstream, Is.EqualTo(NewVp8Payload(10, 8)));
        Assert.That(parsed.Frames[0].UnknownChunks, Has.Count.EqualTo(1));
        Assert.That(parsed.Frames[0].UnknownChunks[0].FourCc, Is.EqualTo("frmx"));
        Assert.That(parsed.Frames[1].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
        Assert.That(parsed.UnknownChunks, Has.Count.EqualTo(1));
        Assert.That(parsed.UnknownChunks[0].FourCc, Is.EqualTo("zzzz"));
    }

    [Test]
    public void WriteAnimated_WritesFrameListWithMetadata()
    {
        List<WebpFrame> frames = new List<WebpFrame>();
        WebpFrame frame = NewFrame(NewVp8Payload(4, 5), WebpBitstreamKind.Lossy);
        frame.DurationMilliseconds = 20;
        frames.Add(frame);
        WebpMetadata metadata = NewMetadata(null, new byte[] { 2 }, null);

        byte[] data = WebpContainerWriter.WriteAnimated(frames, 4, 5, 0, 0x01020304U, metadata);
        WebpInfo info = WebpInfoReader.Read(data);
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(data, WebpStrictness.Strict);

        Assert.That(info.ContainerKind, Is.EqualTo(WebpContainerKind.ExtendedAnimation));
        Assert.That(info.HasAnimation, Is.True);
        Assert.That(info.HasExif, Is.True);
        Assert.That(parsed.AnimationLoopCount, Is.EqualTo(0));
        Assert.That(parsed.AnimationBackgroundColorBgra, Is.EqualTo(0x01020304U));
        Assert.That(parsed.Frames, Has.Count.EqualTo(1));
        Assert.That(parsed.Frames[0].DurationMilliseconds, Is.EqualTo(20));
    }

    [Test]
    public void WriteAnimated_DerivesAlphaFeatureFromVp8LLosslessFrame()
    {
        List<WebpFrame> frames = new List<WebpFrame>();
        WebpFrame frame = NewFrame(NewVp8LPayload(3, 4, true, 0), WebpBitstreamKind.Lossless);
        frame.DurationMilliseconds = 1;
        frames.Add(frame);

        byte[] data = WebpContainerWriter.WriteAnimated(frames, 3, 4, 1, 0);
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(data, WebpStrictness.Strict);

        Assert.That(parsed.IsValid, Is.True);
        Assert.That(parsed.Vp8X.HasAlpha, Is.True);
        Assert.That(parsed.Frames[0].AlphaData, Is.Null);
        Assert.That(parsed.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
    }

    [Test]
    public void WriteAnimated_RejectsMissingFramesAndMissingPayload()
    {
        List<WebpFrame> empty = new List<WebpFrame>();
        WebpFrame missingPayload = new WebpFrame();
        missingPayload.Width = 1;
        missingPayload.Height = 1;
        missingPayload.BitstreamKind = WebpBitstreamKind.Lossy;
        List<WebpFrame> frames = new List<WebpFrame>();
        frames.Add(missingPayload);

        Assert.Throws<ArgumentException>(() => WebpContainerWriter.WriteAnimated(empty, 1, 1, 1, 0));
        Assert.Throws<ArgumentException>(() => WebpContainerWriter.WriteAnimated(frames, 1, 1, 1, 0));
    }

    [Test]
    public void WriteAnimated_RejectsInvalidFrameGeometry()
    {
        WebpFrame oddPosition = NewFrame(NewVp8Payload(1, 1), WebpBitstreamKind.Lossy);
        oddPosition.X = 1;
        WebpFrame outside = NewFrame(NewVp8Payload(4, 4), WebpBitstreamKind.Lossy);
        outside.X = 8;
        WebpFrame mismatch = NewFrame(NewVp8Payload(3, 4), WebpBitstreamKind.Lossy);
        mismatch.Width = 5;

        Assert.Throws<ArgumentException>(() => WebpContainerWriter.WriteAnimated(NewFrameList(oddPosition), 10, 10, 1, 0));
        Assert.Throws<ArgumentException>(() => WebpContainerWriter.WriteAnimated(NewFrameList(outside), 10, 10, 1, 0));
        Assert.Throws<ArgumentException>(() => WebpContainerWriter.WriteAnimated(NewFrameList(mismatch), 10, 10, 1, 0));
    }

    [Test]
    public void WriteAnimated_RejectsInvalidFrameAlphaAndDuration()
    {
        WebpFrame losslessSideAlpha = NewFrame(NewVp8LPayload(1, 1, false, 0), WebpBitstreamKind.Lossless);
        losslessSideAlpha.AlphaData = new byte[] { 1 };
        WebpFrame emptyAlpha = NewFrame(NewVp8Payload(1, 1), WebpBitstreamKind.Lossy);
        emptyAlpha.AlphaData = Array.Empty<byte>();
        WebpFrame invalidDuration = NewFrame(NewVp8Payload(1, 1), WebpBitstreamKind.Lossy);
        invalidDuration.DurationMilliseconds = 16777216;

        Assert.Throws<ArgumentException>(() => WebpContainerWriter.WriteAnimated(NewFrameList(losslessSideAlpha), 1, 1, 1, 0));
        Assert.Throws<ArgumentException>(() => WebpContainerWriter.WriteAnimated(NewFrameList(emptyAlpha), 1, 1, 1, 0));
        Assert.Throws<ArgumentException>(() => WebpContainerWriter.WriteAnimated(NewFrameList(invalidDuration), 1, 1, 1, 0));
    }

    static WebpImage NewImage(byte[] payload, WebpBitstreamKind bitstreamKind)
    {
        WebpImage image = new WebpImage();
        WebpFrame frame = NewFrame(payload, bitstreamKind);
        image.CanvasWidth = frame.Width;
        image.CanvasHeight = frame.Height;
        image.Frames.Add(frame);
        return image;
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

    static List<WebpFrame> NewFrameList(WebpFrame frame)
    {
        List<WebpFrame> frames = new List<WebpFrame>();
        frames.Add(frame);
        return frames;
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
