using webp;

namespace webp.Tests;

[TestFixture]
public class WebpAnimationFrameMuxApiTests
{
    [Test]
    public void InsertAnimationFrame_AllInputFormsPreserveEncodedFramesAndAnimationData()
    {
        byte[] data = NewAnimation();
        WebpFrame inserted = NewLossyFrame(2, 2, 2, 2, 15);
        inserted.BlendMode = WebpFrameBlendMode.Replace;
        inserted.DisposeMode = WebpFrameDisposeMode.DisposeToBackground;
        inserted.AlphaData = new byte[] { 0, 255, 255, 255, 255 };
        inserted.UnknownChunks.Add(new WebpUnknownChunk("frmx", new byte[] { 7 }));
        IWebpMux mux = new WebpCodec();
        MemoryStream stream = new MemoryStream(data);
        string path = NewTempPath();
        File.WriteAllBytes(path, data);

        try
        {
            byte[] arrayResult = mux.InsertAnimationFrame(data, 1, inserted);
            byte[] spanResult = mux.InsertAnimationFrameSpan(data, 1, inserted);
            byte[] streamResult = mux.InsertAnimationFrameStream(stream, 1, inserted);
            byte[] fileResult = mux.InsertAnimationFrameFile(path, 1, inserted);

            Assert.That(spanResult, Is.EqualTo(arrayResult));
            Assert.That(streamResult, Is.EqualTo(arrayResult));
            Assert.That(fileResult, Is.EqualTo(arrayResult));
            AssertInsertedAnimation(arrayResult, data, inserted, mux);
            AssertFrameMatches(mux.ExtractFrame(arrayResult, 1), inserted);
            Assert.That(stream.Position, Is.EqualTo(stream.Length));
            Assert.That(stream.CanRead, Is.True);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void ReplaceAnimationFrame_ChangesOnlySelectedFrameAndUpdatesAlphaFlag()
    {
        byte[] data = NewAnimation();
        List<byte[]> beforePayloads = GetFramePayloads(data);
        WebpFrame replacement = NewLossyFrame(2, 0, 2, 2, 33);
        replacement.BlendMode = WebpFrameBlendMode.Replace;
        replacement.AlphaData = new byte[] { 0, 10, 20, 30, 40 };
        IWebpMux mux = new WebpCodec();
        MemoryStream stream = new MemoryStream(data);
        string path = NewTempPath();
        File.WriteAllBytes(path, data);

        try
        {
            byte[] result = mux.ReplaceAnimationFrame(data, 0, replacement);
            byte[] spanResult = mux.ReplaceAnimationFrameSpan(data, 0, replacement);
            byte[] streamResult = mux.ReplaceAnimationFrameStream(stream, 0, replacement);
            byte[] fileResult = mux.ReplaceAnimationFrameFile(path, 0, replacement);
            List<WebpFrame> frames = mux.ExtractFrames(result);
            List<byte[]> afterPayloads = GetFramePayloads(result);
            WebpInfo info = new WebpCodec().Read(result, new WebpDecodeOptions());

            Assert.That(spanResult, Is.EqualTo(result));
            Assert.That(streamResult, Is.EqualTo(result));
            Assert.That(fileResult, Is.EqualTo(result));
            Assert.That(frames, Has.Count.EqualTo(2));
            AssertFrameMatches(frames[0], replacement);
            Assert.That(frames[1].EncodedBitstream, Is.EqualTo(NewVp8Payload(2, 2)));
            Assert.That(afterPayloads[1], Is.EqualTo(beforePayloads[1]));
            Assert.That(info.HasAlpha, Is.True);
            AssertAnimationGlobalsAndAncillaryData(result, mux);
            Assert.That(stream.Position, Is.EqualTo(stream.Length));
            Assert.That(stream.CanRead, Is.True);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void RemoveAnimationFrame_StreamAndFileRemoveOnlySelectedFrameAndClearAlphaFlag()
    {
        byte[] data = NewAnimationWithAlphaFirstFrame();
        List<byte[]> beforePayloads = GetFramePayloads(data);
        IWebpMux mux = new WebpCodec();
        MemoryStream stream = new MemoryStream(data);
        string path = NewTempPath();
        File.WriteAllBytes(path, data);

        try
        {
            byte[] spanResult = mux.RemoveAnimationFrameSpan(data, 0);
            byte[] streamResult = mux.RemoveAnimationFrameStream(stream, 0);
            byte[] fileResult = mux.RemoveAnimationFrameFile(path, 0);
            List<WebpFrame> frames = mux.ExtractFrames(streamResult);
            WebpInfo info = new WebpCodec().Read(streamResult, new WebpDecodeOptions());

            Assert.That(spanResult, Is.EqualTo(streamResult));
            Assert.That(fileResult, Is.EqualTo(streamResult));
            Assert.That(frames, Has.Count.EqualTo(1));
            Assert.That(frames[0].DurationMilliseconds, Is.EqualTo(20));
            Assert.That(GetFramePayloads(streamResult)[0], Is.EqualTo(beforePayloads[1]));
            Assert.That(info.HasAlpha, Is.False);
            AssertAnimationGlobalsAndAncillaryData(streamResult, mux);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void FrameMutation_ValidatesIndicesGeometryTimingModesAndPayloadCompatibility()
    {
        byte[] data = NewAnimation();
        IWebpMux mux = new WebpCodec();
        WebpFrame valid = NewLossyFrame(0, 0, 2, 2, 1);
        WebpFrame oddPosition = NewLossyFrame(1, 0, 2, 2, 1);
        WebpFrame outsideCanvas = NewLossyFrame(8, 0, 2, 2, 1);
        WebpFrame badDuration = NewLossyFrame(0, 0, 2, 2, 16777216);
        WebpFrame badBlend = NewLossyFrame(0, 0, 2, 2, 1);
        badBlend.BlendMode = (WebpFrameBlendMode)99;
        WebpFrame badDispose = NewLossyFrame(0, 0, 2, 2, 1);
        badDispose.DisposeMode = (WebpFrameDisposeMode)99;
        WebpFrame mismatchedSize = NewLossyFrame(0, 0, 2, 2, 1);
        mismatchedSize.Width = 3;
        WebpFrame incompatibleAlpha = NewLosslessFrame(0, 0, 2, 2, 1);
        incompatibleAlpha.AlphaData = new byte[] { 0 };

        Assert.Throws<ArgumentOutOfRangeException>(() => mux.InsertAnimationFrame(data, 3, valid));
        Assert.Throws<ArgumentOutOfRangeException>(() => mux.ReplaceAnimationFrame(data, -1, valid));
        Assert.Throws<ArgumentOutOfRangeException>(() => mux.RemoveAnimationFrame(data, 2));
        Assert.Throws<ArgumentException>(() => mux.InsertAnimationFrame(data, 0, oddPosition));
        Assert.Throws<ArgumentException>(() => mux.InsertAnimationFrame(data, 0, outsideCanvas));
        Assert.Throws<ArgumentException>(() => mux.InsertAnimationFrame(data, 0, badDuration));
        Assert.Throws<ArgumentOutOfRangeException>(() => mux.InsertAnimationFrame(data, 0, badBlend));
        Assert.Throws<ArgumentOutOfRangeException>(() => mux.InsertAnimationFrame(data, 0, badDispose));
        Assert.Throws<ArgumentException>(() => mux.InsertAnimationFrame(data, 0, mismatchedSize));
        Assert.Throws<ArgumentException>(() => mux.InsertAnimationFrame(data, 0, incompatibleAlpha));

        byte[] oneFrame = mux.RemoveAnimationFrame(data, 1);
        Assert.Throws<ArgumentException>(() => mux.RemoveAnimationFrameSpan(oneFrame, 0));
    }

    static void AssertInsertedAnimation(byte[] result, byte[] source, WebpFrame inserted, IWebpMux mux)
    {
        List<WebpFrame> frames = mux.ExtractFrames(result);
        List<byte[]> sourcePayloads = GetFramePayloads(source);
        List<byte[]> resultPayloads = GetFramePayloads(result);
        WebpInfo info = new WebpCodec().Read(result, new WebpDecodeOptions());

        Assert.That(frames, Has.Count.EqualTo(3));
        Assert.That(frames[0].DurationMilliseconds, Is.EqualTo(10));
        AssertFrameMatches(frames[1], inserted);
        Assert.That(frames[1].Pixels, Is.Null);
        Assert.That(frames[2].DurationMilliseconds, Is.EqualTo(20));
        Assert.That(resultPayloads[0], Is.EqualTo(sourcePayloads[0]));
        Assert.That(resultPayloads[2], Is.EqualTo(sourcePayloads[1]));
        Assert.That(info.HasAlpha, Is.True);
        AssertAnimationGlobalsAndAncillaryData(result, mux);
    }

    static void AssertFrameMatches(WebpFrame actual, WebpFrame expected)
    {
        Assert.That(actual.X, Is.EqualTo(expected.X));
        Assert.That(actual.Y, Is.EqualTo(expected.Y));
        Assert.That(actual.Width, Is.EqualTo(expected.Width));
        Assert.That(actual.Height, Is.EqualTo(expected.Height));
        Assert.That(actual.DurationMilliseconds, Is.EqualTo(expected.DurationMilliseconds));
        Assert.That(actual.BlendMode, Is.EqualTo(expected.BlendMode));
        Assert.That(actual.DisposeMode, Is.EqualTo(expected.DisposeMode));
        Assert.That(actual.BitstreamKind, Is.EqualTo(expected.BitstreamKind));
        Assert.That(actual.EncodedBitstream, Is.EqualTo(expected.EncodedBitstream));
        Assert.That(actual.AlphaData, Is.EqualTo(expected.AlphaData));
    }

    static void AssertAnimationGlobalsAndAncillaryData(byte[] data, IWebpMux mux)
    {
        WebpInfo info = new WebpCodec().Read(data, new WebpDecodeOptions());
        WebpMetadata metadata = mux.GetMetadata(data);
        List<WebpUnknownChunk> unknown = mux.EnumerateUnknownChunks(data);

        Assert.That(info.LoopCount, Is.EqualTo(4));
        Assert.That(info.BackgroundColorBgra, Is.EqualTo(0xFF010203U));
        Assert.That(metadata.Exif, Is.EqualTo(new byte[] { 9 }));
        Assert.That(unknown, Has.Count.EqualTo(1));
        Assert.That(unknown[0].FourCc, Is.EqualTo("top1"));
        Assert.That(unknown[0].Data, Is.EqualTo(new byte[] { 8 }));
    }

    static byte[] NewAnimation()
    {
        WebpImage image = new WebpImage();
        image.CanvasWidth = 8;
        image.CanvasHeight = 8;
        image.HasAnimation = true;
        image.LoopCount = 4;
        image.BackgroundColorBgra = 0xFF010203U;
        image.Metadata.Exif = new byte[] { 9 };
        image.UnknownChunks.Add(new WebpUnknownChunk("top1", new byte[] { 8 }));
        image.Frames.Add(NewLosslessFrame(0, 0, 2, 2, 10));
        image.Frames.Add(NewLossyFrame(4, 4, 2, 2, 20));
        return WebpContainerWriter.WriteAnimated(image);
    }

    static byte[] NewAnimationWithAlphaFirstFrame()
    {
        WebpImage image = new WebpImage();
        image.CanvasWidth = 8;
        image.CanvasHeight = 8;
        image.HasAnimation = true;
        image.HasAlpha = true;
        image.LoopCount = 4;
        image.BackgroundColorBgra = 0xFF010203U;
        image.Metadata.Exif = new byte[] { 9 };
        image.UnknownChunks.Add(new WebpUnknownChunk("top1", new byte[] { 8 }));
        WebpFrame first = NewLossyFrame(0, 0, 2, 2, 10);
        first.AlphaData = new byte[] { 0, 255, 255, 255, 255 };
        image.Frames.Add(first);
        image.Frames.Add(NewLossyFrame(4, 4, 2, 2, 20));
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

    static List<byte[]> GetFramePayloads(byte[] data)
    {
        WebpRiffParseResult riff = WebpRiffParser.Parse(data);
        List<byte[]> payloads = new List<byte[]>();
        int index = 0;
        while (index < riff.Chunks.Count)
        {
            WebpChunkInfo chunk = riff.Chunks[index];
            if (chunk.FourCc == "ANMF")
            {
                payloads.Add(WebpEndianReader.ReadSlice(data, (int)chunk.PayloadOffset, (int)chunk.PayloadLength).ToArray());
            }

            index = index + 1;
        }

        return payloads;
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

    static string NewTempPath()
    {
        return Path.Combine(TestContext.CurrentContext.WorkDirectory, "animation-frame-mux-" + Guid.NewGuid().ToString("N") + ".webp");
    }
}
