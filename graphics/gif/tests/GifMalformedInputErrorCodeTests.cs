using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifMalformedInputErrorCodeTests
{
    [Test]
    public void MalformedInputs_ExerciseEveryGifErrorCode()
    {
        GifErrorCode[] observed = new GifErrorCode[]
        {
            new GifBlockReader().Read(new byte[] { 0, 0, 0, 0, 0, 0 }).Error.Code,
            new GifHeaderParser().Parse(new byte[] { 71, 73, 70, 48, 48, 97 }).Error.Code,
            new GifHeaderParser().Parse(new byte[] { 71, 73 }).Error.Code,
            Gif.Validate((GifDocument)null!).Code,
            new GifColorTableWriter().Create(new GifPalette(new GifColor[] { new GifColor(1, 2, 3) })).Error.Code,
            CreateInvalidBackgroundResult(),
            new GifBlockReader().Read(CreateUnknownTopLevelBlockGif()).Error.Code,
            new GifGraphicControlExtensionParser().Parse(new byte[] { 0x21, 0xF9, 3, 0, 0, 0, 0, 0 }).Error.Code,
            new GifCommentExtensionParser().Parse(new byte[] { 0x21, 0xFE, 1, 65 }).Error.Code,
            Gif.Validate((GifBitmap)null!).Code,
            new GifCompositor().InitializeCanvasFromScreen(new GifLogicalScreen(1, 1), new GifPalette(null), new GifDecodeOptions()).Error.Code,
            new GifRawFrameDecoder().DecodeIndexedPixels(new byte[] { 68, 1 }, 1, 1, 1, false).Error.Code,
            new GifRawFrameDecoder().DecodeIndexedPixels(new byte[] { 0x4D }, 2, 1, 1, false).Error.Code,
            new GifRawFrameDecoder().DecodeIndexedPixels(new byte[] { 68, 1 }, 2, 2, 1, false).Error.Code,
            CreateDecodedPixelLimitResult(),
            CreateFrameLimitResult(),
            CreateDimensionLimitResult(),
            CreateByteLimitResult(),
            new GifRawFrameDecoder().DecodeIndexedPixels(new byte[0], 2, 46341, 46341, false).Error.Code,
            Gif.Validate(CreateIndexedBitmap(9)).Code
        };

        GifErrorCode[] expected = new GifErrorCode[]
        {
            GifErrorCode.InvalidSignature,
            GifErrorCode.UnsupportedVersion,
            GifErrorCode.UnexpectedEndOfStream,
            GifErrorCode.MissingLogicalScreenDescriptor,
            GifErrorCode.InvalidColorTableSize,
            GifErrorCode.InvalidBackgroundColorIndex,
            GifErrorCode.UnknownBlockType,
            GifErrorCode.MalformedExtension,
            GifErrorCode.UnterminatedSubBlocks,
            GifErrorCode.InvalidImageRectangle,
            GifErrorCode.MissingColorTable,
            GifErrorCode.InvalidLzwMinimumCodeSize,
            GifErrorCode.InvalidLzwCodeStream,
            GifErrorCode.DecodedPixelCountMismatch,
            GifErrorCode.DecodedPixelLimitExceeded,
            GifErrorCode.FrameLimitExceeded,
            GifErrorCode.DimensionLimitExceeded,
            GifErrorCode.InputByteLimitExceeded,
            GifErrorCode.MemoryLimitExceeded,
            GifErrorCode.InvalidPaletteIndex
        };

        Assert.That(observed.Length, Is.EqualTo(expected.Length));
        int index = 0;
        while (index < expected.Length)
        {
            AssertHasCode(observed, expected[index]);
            index = index + 1;
        }
    }

    static GifErrorCode CreateInvalidBackgroundResult()
    {
        GifLogicalScreen screen = new GifLogicalScreen(1, 1);
        screen.BackgroundColorIndex = 2;
        GifPalette palette = new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(255, 255, 255)
        });

        return new GifCompositor().InitializeCanvasFromScreen(screen, palette, new GifDecodeOptions()).Error.Code;
    }

    static GifErrorCode CreateDecodedPixelLimitResult()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MaxDecodedPixels = 3;

        return new GifRawFrameDecoder().DecodeIndexedPixelsWithOptions(CreateLiteralLzwData(new byte[] { 0, 1, 2, 3 }), 2, 2, 2, false, options).Error.Code;
    }

    static GifErrorCode CreateFrameLimitResult()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MaxFrames = 1;

        return new GifBlockReader().ReadBytesWithOptions(CreateTwoOnePixelFramesGif(), options).Error.Code;
    }

    static GifErrorCode CreateDimensionLimitResult()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MaxWidth = 2;

        return new GifBlockReader().ReadBytesWithOptions(CreateTrailerOnlyGif(3, 1), options).Error.Code;
    }

    static GifErrorCode CreateByteLimitResult()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MaxBytes = 12;

        return new GifBlockReader().ReadBytesWithOptions(CreateTrailerOnlyGif(1, 1), options).Error.Code;
    }

    static GifBitmap CreateIndexedBitmap(byte index)
    {
        GifPalette palette = new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(255, 255, 255)
        });
        return GifBitmap.CreateIndexed(1, 1, new byte[] { index }, palette);
    }

    static byte[] CreateUnknownTopLevelBlockGif()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0, 0, 0,
            0
        };
    }

    static byte[] CreateTrailerOnlyGif(int width, int height)
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            (byte)(width & 255), (byte)((width >> 8) & 255),
            (byte)(height & 255), (byte)((height >> 8) & 255),
            0, 0, 0,
            0x3B
        };
    }

    static byte[] CreateTwoOnePixelFramesGif()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            2, 0, 1, 0, 0, 0, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 1, 77, 0,
            0x2C, 1, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 1, 88, 0,
            0x3B
        };
    }

    static byte[] CreateLiteralLzwData(byte[] pixels)
    {
        GifLzwBitWriter writer = new GifLzwBitWriter(16);
        int minimumCodeSize = 2;
        int clearCode = 1 << minimumCodeSize;
        int endCode = clearCode + 1;
        int nextCode = endCode + 1;
        int codeSize = minimumCodeSize + 1;
        bool hasPreviousCode = false;

        writer.WriteCode(clearCode, codeSize);
        int index = 0;
        while (index < pixels.Length)
        {
            writer.WriteCode(pixels[index], codeSize);
            if (hasPreviousCode)
            {
                nextCode = nextCode + 1;
                if (nextCode >= (1 << codeSize))
                {
                    codeSize = codeSize + 1;
                }
            }

            hasPreviousCode = true;
            index = index + 1;
        }

        writer.WriteCode(endCode, codeSize);
        return writer.ToArray();
    }

    static void AssertHasCode(GifErrorCode[] codes, GifErrorCode expected)
    {
        bool found = false;
        int index = 0;
        while (index < codes.Length)
        {
            if (codes[index] == expected)
            {
                found = true;
            }

            index = index + 1;
        }

        Assert.That(found, Is.True, "Expected malformed input coverage for " + expected + ".");
    }
}
