using System.IO;

namespace png.Tests;

[TestFixture]
public class PngPublicApngDecodeTests
{
    [Test]
    public void DecodeBytes_ReturnsRawRectanglesAndComposedCanvases()
    {
        PngIhdr ihdr = CreateIhdr(3, 1);
        PngFrame[] frames = new PngFrame[]
        {
            CreateFrame(3, 1, 0, new byte[]
            {
                255, 0, 0, 255,
                255, 0, 0, 255,
                255, 0, 0, 255
            }, 1, 0, PngApngDisposeOperation.None, PngApngBlendOperation.Source, true),
            CreateFrame(1, 1, 1, new byte[] { 0, 0, 255, 128 }, 2, 50, PngApngDisposeOperation.Background, PngApngBlendOperation.Over, false),
            CreateFrame(1, 1, 0, new byte[] { 0, 255, 0, 255 }, 3, 100, PngApngDisposeOperation.Previous, PngApngBlendOperation.Source, false),
            CreateFrame(1, 1, 1, new byte[] { 255, 255, 255, 255 }, 4, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source, false)
        };
        byte[] png = SplitFirstFrameData(CreateIncludedAnimation(ihdr, frames, 7));

        PngImage image = new PngCodec().DecodeBytes(png, new PngDecoderOptions());

        Assert.That(image.Animation.FrameCount, Is.EqualTo(4));
        Assert.That(image.Animation.PlayCount, Is.EqualTo(7));
        Assert.That(image.Animation.Frames, Has.Length.EqualTo(4));
        Assert.That(image.Animation.ComposedFrames, Has.Length.EqualTo(4));
        Assert.That(image.Animation.Frames[0].UsesDefaultImage, Is.True);
        Assert.That(image.Animation.Frames[0].DelayDenominator, Is.EqualTo(100));
        Assert.That(image.Animation.Frames[1].Width, Is.EqualTo(1));
        Assert.That(image.Animation.Frames[1].XOffset, Is.EqualTo(1));
        Assert.That(image.Animation.Frames[1].Pixels.Data, Is.EqualTo(new byte[] { 0, 0, 255, 128 }));
        AssertCanvas(image.Animation.ComposedFrames[0], new byte[]
        {
            255, 0, 0, 255,
            255, 0, 0, 255,
            255, 0, 0, 255
        });
        AssertCanvas(image.Animation.ComposedFrames[1], new byte[]
        {
            255, 0, 0, 255,
            127, 0, 128, 255,
            255, 0, 0, 255
        });
        AssertCanvas(image.Animation.ComposedFrames[2], new byte[]
        {
            0, 255, 0, 255,
            0, 0, 0, 0,
            255, 0, 0, 255
        });
        AssertCanvas(image.Animation.ComposedFrames[3], new byte[]
        {
            255, 0, 0, 255,
            255, 255, 255, 255,
            255, 0, 0, 255
        });
    }

    [Test]
    public void DecodeBytes_LeavesDefaultImageOutsideAnimationWhenFrameControlFollowsIdat()
    {
        PngIhdr ihdr = CreateIhdr(2, 1);
        PngFrame[] includedFrames = new PngFrame[]
        {
            CreateFrame(2, 1, 0, new byte[] { 10, 20, 30, 255, 40, 50, 60, 255 }, 1, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source, true),
            CreateFrame(1, 1, 1, new byte[] { 0, 0, 255, 255 }, 2, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source, false),
            CreateFrame(1, 1, 0, new byte[] { 0, 255, 0, 255 }, 3, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source, false)
        };
        byte[] png = RemoveDefaultFrameControl(CreateIncludedAnimation(ihdr, includedFrames, 0));

        PngImage image = new PngCodec().DecodeBytes(png, new PngDecoderOptions());

        Assert.That(image.Pixels.Data, Is.EqualTo(new byte[] { 10, 20, 30, 255, 40, 50, 60, 255 }));
        Assert.That(image.Animation.FrameCount, Is.EqualTo(2));
        Assert.That(image.Animation.Frames, Has.Length.EqualTo(2));
        Assert.That(image.Animation.Frames.All(value => !value.UsesDefaultImage), Is.True);
        Assert.That(image.Animation.Frames[0].XOffset, Is.EqualTo(1));
        Assert.That(image.Animation.Frames[0].Pixels.Data, Is.EqualTo(new byte[] { 0, 0, 255, 255 }));
        AssertCanvas(image.Animation.ComposedFrames[0], new byte[]
        {
            0, 0, 0, 0,
            0, 0, 255, 255
        });
        AssertCanvas(image.Animation.ComposedFrames[1], new byte[]
        {
            0, 255, 0, 255,
            0, 0, 255, 255
        });
    }

    [Test]
    public void DecodeBytes_RejectsInvalidApngSequenceAndFrameRectangle()
    {
        PngIhdr ihdr = CreateIhdr(2, 1);
        PngFrame[] frames = new PngFrame[]
        {
            CreateFrame(2, 1, 0, new byte[] { 1, 2, 3, 255, 4, 5, 6, 255 }, 1, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source, true),
            CreateFrame(1, 1, 1, new byte[] { 7, 8, 9, 255 }, 2, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source, false)
        };
        byte[] valid = CreateIncludedAnimation(ihdr, frames, 0);
        PngChunk[] sequenceChunks = ReadChunks(valid);
        PngChunk[] rectangleChunks = ReadChunks(valid);
        PngChunk sequenceControl = FindFrameControl(sequenceChunks, 1);
        PngChunk rectangleControl = FindFrameControl(rectangleChunks, 1);
        PngBinaryWriter.WriteUInt32(sequenceControl.Data, 0, 9);
        PngBinaryWriter.WriteUInt32(rectangleControl.Data, 12, 2);

        byte[] invalidSequence = PngChunkDatastreamWriter.WriteToBytes(sequenceChunks);
        byte[] invalidRectangle = PngChunkDatastreamWriter.WriteToBytes(rectangleChunks);
        PngCodec codec = new PngCodec();

        Assert.Throws<InvalidDataException>(() => codec.DecodeBytes(invalidSequence, new PngDecoderOptions()));
        Assert.Throws<InvalidDataException>(() => codec.DecodeBytes(invalidRectangle, new PngDecoderOptions()));
    }

    static byte[] CreateIncludedAnimation(PngIhdr ihdr, PngFrame[] frames, int playCount)
    {
        PngAnimation animation = new PngAnimation();
        animation.FrameCount = frames.Length;
        animation.PlayCount = playCount;
        animation.Frames = frames;
        PngChunk[] animationChunks = PngApngFrameRectangleEncoder.Encode(ihdr, animation);
        PngChunk[] chunks = new PngChunk[animationChunks.Length + 2];
        chunks[0] = PngIhdrWriter.WriteChunk(ihdr);
        Array.Copy(animationChunks, 0, chunks, 1, animationChunks.Length);
        chunks[chunks.Length - 1] = PngIendWriter.WriteChunk();
        return PngChunkDatastreamWriter.WriteToBytes(chunks);
    }

    static byte[] SplitFirstFrameData(byte[] png)
    {
        PngChunk[] chunks = ReadChunks(png);
        List<PngChunk> result = new List<PngChunk>();
        uint sequence = 0;
        bool split = false;
        int index = 0;
        while (index < chunks.Length)
        {
            PngChunk chunk = chunks[index];
            if (chunk.Type == PngConstants.ChunkFrameControl)
            {
                PngFrameControl control = PngFrameControlParser.Parse(chunk);
                control.SequenceNumber = sequence;
                sequence += 1;
                result.Add(PngFrameControlWriter.WriteChunk(control));
            }
            else if (chunk.Type == PngConstants.ChunkFrameData)
            {
                PngFrameData frameData = PngFrameDataParser.Parse(chunk);
                if (!split)
                {
                    int splitOffset = frameData.Data.Length / 2;
                    result.Add(PngFrameDataWriter.WriteChunk(CreateFrameData(sequence, frameData.Data, 0, splitOffset)));
                    sequence += 1;
                    result.Add(PngFrameDataWriter.WriteChunk(CreateFrameData(sequence, frameData.Data, splitOffset, frameData.Data.Length - splitOffset)));
                    sequence += 1;
                    split = true;
                }
                else
                {
                    frameData.SequenceNumber = sequence;
                    sequence += 1;
                    result.Add(PngFrameDataWriter.WriteChunk(frameData));
                }
            }
            else
            {
                result.Add(chunk);
            }

            index += 1;
        }

        return PngChunkDatastreamWriter.WriteToBytes(result.ToArray());
    }

    static byte[] RemoveDefaultFrameControl(byte[] png)
    {
        PngChunk[] chunks = ReadChunks(png);
        List<PngChunk> result = new List<PngChunk>();
        uint sequence = 0;
        bool skippedFirstControl = false;
        int index = 0;
        while (index < chunks.Length)
        {
            PngChunk chunk = chunks[index];
            if (chunk.Type == PngConstants.ChunkAnimationControl)
            {
                PngAnimationControl control = PngAnimationControlParser.Parse(chunk);
                control.FrameCount -= 1;
                result.Add(PngAnimationControlWriter.WriteChunk(control));
            }
            else if (chunk.Type == PngConstants.ChunkFrameControl && !skippedFirstControl)
            {
                skippedFirstControl = true;
            }
            else if (chunk.Type == PngConstants.ChunkFrameControl)
            {
                PngFrameControl control = PngFrameControlParser.Parse(chunk);
                control.SequenceNumber = sequence;
                sequence += 1;
                result.Add(PngFrameControlWriter.WriteChunk(control));
            }
            else if (chunk.Type == PngConstants.ChunkFrameData)
            {
                PngFrameData frameData = PngFrameDataParser.Parse(chunk);
                frameData.SequenceNumber = sequence;
                sequence += 1;
                result.Add(PngFrameDataWriter.WriteChunk(frameData));
            }
            else
            {
                result.Add(chunk);
            }

            index += 1;
        }

        return PngChunkDatastreamWriter.WriteToBytes(result.ToArray());
    }

    static PngFrameData CreateFrameData(uint sequence, byte[] data, int offset, int length)
    {
        byte[] copy = new byte[length];
        Array.Copy(data, offset, copy, 0, length);
        PngFrameData frameData = new PngFrameData();
        frameData.SequenceNumber = sequence;
        frameData.Data = copy;
        return frameData;
    }

    static PngChunk FindFrameControl(PngChunk[] chunks, int frameIndex)
    {
        int seen = 0;
        int index = 0;
        while (index < chunks.Length)
        {
            if (chunks[index].Type == PngConstants.ChunkFrameControl)
            {
                if (seen == frameIndex)
                {
                    return chunks[index];
                }

                seen += 1;
            }

            index += 1;
        }

        throw new InvalidOperationException();
    }

    static PngChunk[] ReadChunks(byte[] png)
    {
        return PngChunkListReader.Read(png, PngConstants.SignatureLength).Chunks;
    }

    static PngIhdr CreateIhdr(int width, int height)
    {
        PngIhdr ihdr = new PngIhdr();
        ihdr.Width = width;
        ihdr.Height = height;
        ihdr.BitDepth = PngBitDepth.Eight;
        ihdr.ColorType = PngColorType.TruecolorAlpha;
        ihdr.CompressionMethod = PngCompressionMethod.Deflate;
        ihdr.FilterMethod = PngFilterMethod.Adaptive;
        ihdr.InterlaceMethod = PngInterlaceMethod.None;
        return ihdr;
    }

    static PngFrame CreateFrame(int width, int height, int xOffset, byte[] data, int delayNumerator, int delayDenominator, PngApngDisposeOperation disposeOperation, PngApngBlendOperation blendOperation, bool usesDefaultImage)
    {
        PngPixelBuffer pixels = new PngPixelBuffer();
        pixels.Format = PngPixelFormat.Rgba8;
        pixels.Width = width;
        pixels.Height = height;
        pixels.Stride = width * 4;
        pixels.Data = data;
        PngFrame frame = new PngFrame();
        frame.Width = width;
        frame.Height = height;
        frame.XOffset = xOffset;
        frame.DelayNumerator = delayNumerator;
        frame.DelayDenominator = delayDenominator;
        frame.DisposeOperation = disposeOperation;
        frame.BlendOperation = blendOperation;
        frame.Pixels = pixels;
        frame.UsesDefaultImage = usesDefaultImage;
        return frame;
    }

    static void AssertCanvas(PngPixelBuffer canvas, byte[] data)
    {
        Assert.That(canvas.Format, Is.EqualTo(PngPixelFormat.Rgba8));
        Assert.That(canvas.Width, Is.EqualTo(data.Length / 4));
        Assert.That(canvas.Height, Is.EqualTo(1));
        Assert.That(canvas.Stride, Is.EqualTo(data.Length));
        Assert.That(canvas.Data, Is.EqualTo(data));
    }
}
