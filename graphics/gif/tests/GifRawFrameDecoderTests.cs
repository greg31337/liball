using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifRawFrameDecoderTests
{
    [Test]
    public void DecodeIndexedPixels_DecodesLiteralLzwCodesToIndexedPixels()
    {
        GifRawFrameDecoder decoder = new GifRawFrameDecoder();
        byte[] compressedData = CreateLiteralLzwData(2, new byte[] { 0, 1, 2, 3 });

        GifRawFrameDecodeResult result = decoder.DecodeIndexedPixels(compressedData, 2, 2, 2, false);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IndexedPixels, Is.EqualTo(new byte[] { 0, 1, 2, 3 }));
    }

    [Test]
    public void DecodeFrame_PopulatesFrameIndexedPixels()
    {
        GifFrame frame = new GifFrame(0, 0, 3, 1);
        frame.LzwMinimumCodeSize = 2;
        frame.CompressedImageData = CreateLiteralLzwData(2, new byte[] { 3, 2, 1 });
        GifRawFrameDecoder decoder = new GifRawFrameDecoder();

        GifRawFrameDecodeResult result = decoder.DecodeFrame(frame);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IndexedPixels, Is.EqualTo(new byte[] { 3, 2, 1 }));
        Assert.That(frame.IndexedPixels, Is.EqualTo(new byte[] { 3, 2, 1 }));
    }

    [Test]
    public void DecodeIndexedPixels_DeinterlacesInterlacedFrames()
    {
        GifRawFrameDecoder decoder = new GifRawFrameDecoder();
        byte[] interlacedPixels = new byte[]
        {
            0, 0,
            4, 4,
            2, 2,
            1, 1,
            3, 3
        };
        byte[] compressedData = CreateLiteralLzwData(3, interlacedPixels);

        GifRawFrameDecodeResult result = decoder.DecodeIndexedPixels(compressedData, 3, 2, 5, true);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IndexedPixels, Is.EqualTo(new byte[]
        {
            0, 0,
            1, 1,
            2, 2,
            3, 3,
            4, 4
        }));
    }

    [Test]
    public void DecodeIndexedPixels_RejectsInvalidLzwStream()
    {
        GifRawFrameDecoder decoder = new GifRawFrameDecoder();

        GifRawFrameDecodeResult result = decoder.DecodeIndexedPixels(new byte[] { 0x4D }, 2, 1, 1, false);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidLzwCodeStream));
        Assert.That(result.IndexedPixels, Is.Empty);
    }

    [Test]
    public void DecodeIndexedPixels_RejectsOutputCountMismatch()
    {
        GifRawFrameDecoder decoder = new GifRawFrameDecoder();
        byte[] compressedData = CreateLiteralLzwData(2, new byte[] { 0 });

        GifRawFrameDecodeResult result = decoder.DecodeIndexedPixels(compressedData, 2, 2, 1, false);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelCountMismatch));
    }

    [Test]
    public void DecodeIndexedPixels_EnforcesConfiguredDecodedPixelLimit()
    {
        GifRawFrameDecoder decoder = new GifRawFrameDecoder();
        GifDecodeOptions options = new GifDecodeOptions();
        options.MaxDecodedPixels = 3;
        byte[] compressedData = CreateLiteralLzwData(2, new byte[] { 0, 1, 2, 3 });

        GifRawFrameDecodeResult result = decoder.DecodeIndexedPixelsWithOptions(compressedData, 2, 2, 2, false, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelLimitExceeded));
    }

    [Test]
    public void DecodeIndexedPixels_RejectsInvalidDimensionsAndMinimumCodeSize()
    {
        GifRawFrameDecoder decoder = new GifRawFrameDecoder();
        byte[] compressedData = CreateLiteralLzwData(2, new byte[] { 0 });

        GifRawFrameDecodeResult invalidDimensions = decoder.DecodeIndexedPixels(compressedData, 2, 0, 1, false);
        GifRawFrameDecodeResult invalidMinimumCodeSize = decoder.DecodeIndexedPixels(compressedData, 1, 1, 1, false);

        Assert.That(invalidDimensions.Success, Is.False);
        Assert.That(invalidDimensions.Error.Code, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(invalidMinimumCodeSize.Success, Is.False);
        Assert.That(invalidMinimumCodeSize.Error.Code, Is.EqualTo(GifErrorCode.InvalidLzwMinimumCodeSize));
    }

    [Test]
    public void DecodeIndexedPixels_TolerantModeAllowsMissingEndCodeWithExactPixelCount()
    {
        GifRawFrameDecoder decoder = new GifRawFrameDecoder();
        GifDecodeOptions options = new GifDecodeOptions();
        options.TolerateMalformedStreams = true;
        byte[] compressedData = CreateLiteralLzwDataWithoutEndCode(2, new byte[] { 0 });

        GifRawFrameDecodeResult result = decoder.DecodeIndexedPixelsWithOptions(compressedData, 2, 1, 1, false, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IndexedPixels, Is.EqualTo(new byte[] { 0 }));
        Assert.That(result.Warnings.Length, Is.EqualTo(1));
        Assert.That(result.Warnings[0].Code, Is.EqualTo(GifWarningCode.ToleratedMalformedBlock));
    }

    static byte[] CreateLiteralLzwData(int minimumCodeSize, byte[] pixels)
    {
        return CreateLiteralLzwDataCore(minimumCodeSize, pixels, true);
    }

    static byte[] CreateLiteralLzwDataWithoutEndCode(int minimumCodeSize, byte[] pixels)
    {
        return CreateLiteralLzwDataCore(minimumCodeSize, pixels, false);
    }

    static byte[] CreateLiteralLzwDataCore(int minimumCodeSize, byte[] pixels, bool writeEndCode)
    {
        GifLzwBitWriter writer = new GifLzwBitWriter(16);
        int rootCodeCount = 1 << minimumCodeSize;
        int clearCode = rootCodeCount;
        int endCode = clearCode + 1;
        int nextCode = endCode + 1;
        int currentCodeSize = minimumCodeSize + 1;
        bool hasPreviousCode = false;

        writer.WriteCode(clearCode, currentCodeSize);

        int index = 0;
        while (index < pixels.Length)
        {
            writer.WriteCode(pixels[index], currentCodeSize);

            if (hasPreviousCode)
            {
                nextCode = nextCode + 1;
                if (currentCodeSize < GifConstants.MaximumLzwCodeSize)
                {
                    if (nextCode >= (1 << currentCodeSize))
                    {
                        currentCodeSize = currentCodeSize + 1;
                    }
                }
            }

            hasPreviousCode = true;
            index = index + 1;
        }

        if (writeEndCode)
        {
            writer.WriteCode(endCode, currentCodeSize);
        }

        return writer.ToArray();
    }
}
