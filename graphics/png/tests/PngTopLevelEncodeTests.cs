using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PngTopLevelEncodeTests
{
    [Test]
    public void EncodeToBytes_RoundTripsRgba8Image()
    {
        PngImage image = CreateImage(CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 8, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }));

        byte[] encoded = Png.EncodeToBytes(image, CreateFixedOptions(PngInterlaceMethod.None));
        PngImage decoded = Png.DecodeBytes(encoded);

        Assert.That(PngSignature.HasSignature(encoded), Is.True);
        AssertImage(decoded, 2, 1, PngInterlaceMethod.None, PngPixelFormat.Rgba8, 8, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        Assert.That(Png.IdentifyBytes(encoded).IsValid, Is.True);
    }

    [Test]
    public void EncodeToBytes_DefaultOptionsRoundTripRgba8Image()
    {
        byte[] pixels = new byte[]
        {
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16
        };
        PngImage image = Png.CreateImage(2, 2, PngPixelFormat.Rgba8, pixels);

        byte[] encoded = Png.EncodeToBytes(image);
        PngIdentifyResult identify = Png.IdentifyBytes(encoded);
        PngImage decoded = Png.DecodeBytes(encoded);

        Assert.That(identify.IsValid, Is.True);
        Assert.That(identify.Info.ColorType, Is.EqualTo(PngColorType.TruecolorAlpha));
        Assert.That(identify.Info.BitDepth, Is.EqualTo(PngBitDepth.Eight));
        AssertImage(decoded, 2, 2, PngInterlaceMethod.None, PngPixelFormat.Rgba8, 8, pixels);
    }

    [Test]
    public void EncodeToBytes_DefaultOptionsReadsRgba8RowsUsingStride()
    {
        PngImage image = CreateImage(CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 12, new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8, 99, 99, 99, 99,
            9, 10, 11, 12, 13, 14, 15, 16, 88, 88, 88, 88
        }));

        byte[] encoded = Png.EncodeToBytes(image);
        PngImage decoded = Png.DecodeBytes(encoded);

        AssertImage(decoded, 2, 2, PngInterlaceMethod.None, PngPixelFormat.Rgba8, 8, new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8,
            9, 10, 11, 12, 13, 14, 15, 16
        });
    }

    [Test]
    public void EncodeToBytes_SplitsIdatChunksByConfiguredMaximum()
    {
        PngImage image = CreateImage(CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 8, new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8,
            9, 10, 11, 12, 13, 14, 15, 16
        }));
        PngEncoderOptions options = CreateFixedOptions(PngInterlaceMethod.None);
        options.MaxIdatChunkBytes = 1;

        byte[] encoded = Png.EncodeToBytes(image, options);
        PngChunkListResult chunkList = Png.ReadChunks(encoded);
        PngImage decoded = Png.DecodeBytes(encoded);

        AssertSplitIdatChunks(chunkList.Chunks, 1, 1);
        AssertImage(decoded, 2, 2, PngInterlaceMethod.None, PngPixelFormat.Rgba8, 8, new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8,
            9, 10, 11, 12, 13, 14, 15, 16
        });
    }

    [Test]
    public void EncodeToBytes_PreservesMetadataChunksWhenPolicyPreserve()
    {
        PngImage image = CreateImage(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 1, 2, 3, 4 }));
        image.Metadata.Chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkIHDR, new byte[] { 1 }),
            CreateChunk(PngConstants.ChunkGamma, new byte[] { 0, 0, 177, 143 }),
            CreateChunk(PngConstants.ChunkPhysicalPixels, new byte[] { 0, 0, 11, 19, 0, 0, 11, 19, 1 }),
            CreateChunk(PngConstants.ChunkIDAT, new byte[] { 2 }),
            CreateChunk(PngConstants.ChunkText, new byte[] { 75, 0, 86 }),
            CreateChunk(PngConstants.ChunkIEND, new byte[0])
        };

        byte[] encoded = Png.EncodeToBytes(image, CreateFixedOptions(PngInterlaceMethod.None));
        PngChunkListResult chunkList = Png.ReadChunks(encoded);
        PngImage decoded = Png.DecodeBytes(encoded);

        Assert.That(chunkList.Chunks, Has.Length.EqualTo(6));
        Assert.That(chunkList.Chunks[0].Type, Is.EqualTo(PngConstants.ChunkIHDR));
        Assert.That(chunkList.Chunks[1].Type, Is.EqualTo(PngConstants.ChunkGamma));
        Assert.That(chunkList.Chunks[1].Data, Is.EqualTo(new byte[] { 0, 0, 177, 143 }));
        Assert.That(chunkList.Chunks[2].Type, Is.EqualTo(PngConstants.ChunkPhysicalPixels));
        Assert.That(chunkList.Chunks[3].Type, Is.EqualTo(PngConstants.ChunkIDAT));
        Assert.That(chunkList.Chunks[4].Type, Is.EqualTo(PngConstants.ChunkText));
        Assert.That(chunkList.Chunks[4].Data, Is.EqualTo(new byte[] { 75, 0, 86 }));
        Assert.That(chunkList.Chunks[5].Type, Is.EqualTo(PngConstants.ChunkIEND));
        AssertImage(decoded, 1, 1, PngInterlaceMethod.None, PngPixelFormat.Rgba8, 4, new byte[] { 1, 2, 3, 4 });
    }

    [Test]
    public void EncodeToBytes_StripsMetadataChunksWhenPolicyStrip()
    {
        PngImage image = CreateImage(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 5, 6, 7, 8 }));
        image.Metadata.Chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkGamma, new byte[] { 0, 0, 177, 143 }),
            CreateChunk(PngConstants.ChunkPhysicalPixels, new byte[] { 0, 0, 11, 19, 0, 0, 11, 19, 1 }),
            CreateChunk(PngConstants.ChunkText, new byte[] { 75, 0, 86 })
        };
        PngEncoderOptions options = CreateFixedOptions(PngInterlaceMethod.None);
        options.MetadataPolicy = PngMetadataPolicy.Strip;

        byte[] encoded = Png.EncodeToBytes(image, options);
        PngChunkListResult chunkList = Png.ReadChunks(encoded);
        PngImage decoded = Png.DecodeBytes(encoded);

        Assert.That(chunkList.Chunks, Has.Length.EqualTo(3));
        Assert.That(chunkList.Chunks[0].Type, Is.EqualTo(PngConstants.ChunkIHDR));
        Assert.That(chunkList.Chunks[1].Type, Is.EqualTo(PngConstants.ChunkIDAT));
        Assert.That(chunkList.Chunks[2].Type, Is.EqualTo(PngConstants.ChunkIEND));
        Assert.That(HasChunk(chunkList.Chunks, PngConstants.ChunkGamma), Is.False);
        Assert.That(HasChunk(chunkList.Chunks, PngConstants.ChunkPhysicalPixels), Is.False);
        Assert.That(HasChunk(chunkList.Chunks, PngConstants.ChunkText), Is.False);
        AssertImage(decoded, 1, 1, PngInterlaceMethod.None, PngPixelFormat.Rgba8, 4, new byte[] { 5, 6, 7, 8 });
    }

    [Test]
    public void EncodeToBytes_PreservesUnknownChunksWhenDispositionPreserve()
    {
        PngImage image = CreateImage(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 1, 2, 3, 4 }));
        image.Metadata.Chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkGamma, new byte[] { 0, 0, 177, 143 }),
            CreateChunk("vpAg", new byte[] { 9, 8, 7 })
        };

        byte[] encoded = Png.EncodeToBytes(image, CreateFixedOptions(PngInterlaceMethod.None));
        PngChunkListResult chunkList = Png.ReadChunks(encoded);
        PngImage decoded = Png.DecodeBytes(encoded);

        Assert.That(chunkList.Chunks, Has.Length.EqualTo(5));
        Assert.That(chunkList.Chunks[0].Type, Is.EqualTo(PngConstants.ChunkIHDR));
        Assert.That(chunkList.Chunks[1].Type, Is.EqualTo(PngConstants.ChunkGamma));
        Assert.That(chunkList.Chunks[2].Type, Is.EqualTo(PngConstants.ChunkIDAT));
        Assert.That(chunkList.Chunks[3].Type, Is.EqualTo("vpAg"));
        Assert.That(chunkList.Chunks[3].Data, Is.EqualTo(new byte[] { 9, 8, 7 }));
        Assert.That(chunkList.Chunks[4].Type, Is.EqualTo(PngConstants.ChunkIEND));
        AssertImage(decoded, 1, 1, PngInterlaceMethod.None, PngPixelFormat.Rgba8, 4, new byte[] { 1, 2, 3, 4 });
    }

    [Test]
    public void EncodeToBytes_DropsUnknownChunksWhenDispositionDrop()
    {
        PngImage image = CreateImage(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 5, 6, 7, 8 }));
        image.Metadata.Chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkGamma, new byte[] { 0, 0, 177, 143 }),
            CreateChunk("vpAg", new byte[] { 9, 8, 7 })
        };
        PngEncoderOptions options = CreateFixedOptions(PngInterlaceMethod.None);
        options.UnknownChunkDisposition = PngChunkDisposition.Drop;

        byte[] encoded = Png.EncodeToBytes(image, options);
        PngChunkListResult chunkList = Png.ReadChunks(encoded);
        PngImage decoded = Png.DecodeBytes(encoded);

        Assert.That(chunkList.Chunks, Has.Length.EqualTo(4));
        Assert.That(chunkList.Chunks[0].Type, Is.EqualTo(PngConstants.ChunkIHDR));
        Assert.That(chunkList.Chunks[1].Type, Is.EqualTo(PngConstants.ChunkGamma));
        Assert.That(chunkList.Chunks[2].Type, Is.EqualTo(PngConstants.ChunkIDAT));
        Assert.That(chunkList.Chunks[3].Type, Is.EqualTo(PngConstants.ChunkIEND));
        Assert.That(HasChunk(chunkList.Chunks, "vpAg"), Is.False);
        AssertImage(decoded, 1, 1, PngInterlaceMethod.None, PngPixelFormat.Rgba8, 4, new byte[] { 5, 6, 7, 8 });
    }

    [Test]
    public void EncodeToStream_DefaultOptionsEncodesBlankRgba8Image()
    {
        PngImage image = Png.CreateImage(1, 2, PngPixelFormat.Rgba8);
        MemoryStream stream = new MemoryStream();

        Png.EncodeToStream(stream, image);

        PngDecoderOptions decoderOptions = new PngDecoderOptions();
        decoderOptions.PreferredPixelFormat = PngPixelFormat.Rgba8;
        PngImage decoded = Png.DecodeBytes(stream.ToArray(), decoderOptions);
        AssertImage(decoded, 1, 2, PngInterlaceMethod.None, PngPixelFormat.Rgba8, PngColorType.GrayscaleAlpha, PngBitDepth.Eight, 4, new byte[]
        {
            0, 0, 0, 0,
            0, 0, 0, 0
        });
    }

    [Test]
    public void EncodeToBytes_DefaultOptionsSelectColorTypeFromPixelFormat()
    {
        AssertAutomaticColorType(PngPixelFormat.Rgb8, PngColorType.Truecolor, 2, 1, new byte[] { 1, 2, 3, 4, 5, 6 });
        AssertAutomaticColorType(PngPixelFormat.GrayscaleAlpha8, PngColorType.GrayscaleAlpha, 2, 1, new byte[] { 1, 255, 2, 128 });
        AssertAutomaticColorType(PngPixelFormat.Grayscale8, PngColorType.Grayscale, 2, 1, new byte[] { 1, 2 });

        PngImage indexed = Png.CreateImage(2, 1, PngPixelFormat.Indexed8, new byte[] { 0, 1 });
        indexed.Metadata.Palette = CreatePalette(
            CreatePaletteEntry(0, 0, 0),
            CreatePaletteEntry(255, 255, 255));

        byte[] encoded = Png.EncodeToBytes(indexed);
        PngIdentifyResult identify = Png.IdentifyBytes(encoded);
        PngImage decoded = Png.DecodeBytes(encoded);

        Assert.That(identify.Info.ColorType, Is.EqualTo(PngColorType.IndexedColor));
        Assert.That(identify.Info.BitDepth, Is.EqualTo(PngBitDepth.One));
        AssertImage(decoded, 2, 1, PngInterlaceMethod.None, PngPixelFormat.Indexed1, PngColorType.IndexedColor, PngBitDepth.One, 1, new byte[] { 0x40 });
    }

    [Test]
    public void EncodeToBytes_DefaultOptionsSelectBitDepthFromPixelFormat()
    {
        AssertAutomaticFormat(PngPixelFormat.Rgba16, PngColorType.TruecolorAlpha, PngBitDepth.Sixteen, 1, 1, 8, CreateBigEndianSamples(0x0102, 0x0304, 0x0506, 0x0708));
        AssertAutomaticFormat(PngPixelFormat.Rgb16, PngColorType.Truecolor, PngBitDepth.Sixteen, 1, 1, 6, CreateBigEndianSamples(0x1112, 0x1314, 0x1516));
        AssertAutomaticFormat(PngPixelFormat.GrayscaleAlpha16, PngColorType.GrayscaleAlpha, PngBitDepth.Sixteen, 1, 1, 4, CreateBigEndianSamples(0x2122, 0x2324));
        AssertAutomaticFormat(PngPixelFormat.Grayscale16, PngColorType.Grayscale, PngBitDepth.Sixteen, 1, 1, 2, CreateBigEndianSamples(0x3132));
        AssertAutomaticFormat(PngPixelFormat.Grayscale4, PngColorType.Grayscale, PngBitDepth.Four, 3, 1, 2, new byte[] { 0x12, 0x30 });

        PngImage indexed = Png.CreateImage(8, 1, PngPixelFormat.Indexed1, new byte[] { 0xB2 });
        indexed.Metadata.Palette = CreatePalette(
            CreatePaletteEntry(0, 0, 0),
            CreatePaletteEntry(255, 255, 255));

        byte[] encoded = Png.EncodeToBytes(indexed);
        PngImage decoded = Png.DecodeBytes(encoded);
        PngIdentifyResult identify = Png.IdentifyBytes(encoded);

        Assert.That(identify.Info.ColorType, Is.EqualTo(PngColorType.IndexedColor));
        Assert.That(identify.Info.BitDepth, Is.EqualTo(PngBitDepth.One));
        AssertImage(decoded, 8, 1, PngInterlaceMethod.None, PngPixelFormat.Indexed1, PngColorType.IndexedColor, PngBitDepth.One, 1, new byte[] { 0xB2 });
    }

    [Test]
    public void EncodeToBytes_RoundTripsRgb8ImageWhenRequested()
    {
        byte[] pixels = new byte[]
        {
            1, 2, 3,
            4, 5, 6,
            7, 8, 9,
            10, 11, 12
        };
        PngImage image = Png.CreateImage(2, 2, PngPixelFormat.Rgb8, pixels);

        byte[] encoded = Png.EncodeToBytes(image, CreateRgb8Options(PngInterlaceMethod.None));
        PngIdentifyResult identify = Png.IdentifyBytes(encoded);
        PngImage decoded = Png.DecodeBytes(encoded);

        Assert.That(identify.IsValid, Is.True);
        Assert.That(identify.Info.ColorType, Is.EqualTo(PngColorType.Truecolor));
        Assert.That(identify.Info.BitDepth, Is.EqualTo(PngBitDepth.Eight));
        AssertImage(decoded, 2, 2, PngInterlaceMethod.None, PngPixelFormat.Rgb8, PngColorType.Truecolor, PngBitDepth.Eight, 6, pixels);
    }

    [Test]
    public void EncodeToBytes_ReadsRgb8RowsUsingStrideWhenRequested()
    {
        PngImage image = CreateImage(CreateBuffer(PngPixelFormat.Rgb8, 2, 2, 8, new byte[]
        {
            1, 2, 3, 4, 5, 6, 99, 99,
            7, 8, 9, 10, 11, 12, 88, 88
        }));

        byte[] encoded = Png.EncodeToBytes(image, CreateRgb8Options(PngInterlaceMethod.None));
        PngImage decoded = Png.DecodeBytes(encoded);

        AssertImage(decoded, 2, 2, PngInterlaceMethod.None, PngPixelFormat.Rgb8, PngColorType.Truecolor, PngBitDepth.Eight, 6, new byte[]
        {
            1, 2, 3, 4, 5, 6,
            7, 8, 9, 10, 11, 12
        });
    }

    [Test]
    public void EncodeToBytes_RoundTripsAdam7Rgb8ImageWhenRequested()
    {
        byte[] pixels = new byte[] { 11, 12, 13 };
        PngImage image = Png.CreateImage(1, 1, PngPixelFormat.Rgb8, pixels);

        byte[] encoded = Png.EncodeToBytes(image, CreateRgb8Options(PngInterlaceMethod.Adam7));
        PngImage decoded = Png.DecodeBytes(encoded);

        AssertImage(decoded, 1, 1, PngInterlaceMethod.Adam7, PngPixelFormat.Rgb8, PngColorType.Truecolor, PngBitDepth.Eight, 3, pixels);
    }

    [Test]
    public void EncodeToBytes_RoundTripsGrayscaleAlpha8ImageWhenRequested()
    {
        byte[] pixels = new byte[]
        {
            1, 255,
            2, 128,
            3, 64,
            4, 0
        };
        PngImage image = Png.CreateImage(2, 2, PngPixelFormat.GrayscaleAlpha8, pixels);

        byte[] encoded = Png.EncodeToBytes(image, CreateGrayscaleAlpha8Options(PngInterlaceMethod.None));
        PngIdentifyResult identify = Png.IdentifyBytes(encoded);
        PngImage decoded = Png.DecodeBytes(encoded);

        Assert.That(identify.IsValid, Is.True);
        Assert.That(identify.Info.ColorType, Is.EqualTo(PngColorType.GrayscaleAlpha));
        Assert.That(identify.Info.BitDepth, Is.EqualTo(PngBitDepth.Eight));
        AssertImage(decoded, 2, 2, PngInterlaceMethod.None, PngPixelFormat.GrayscaleAlpha8, PngColorType.GrayscaleAlpha, PngBitDepth.Eight, 4, pixels);
    }

    [Test]
    public void EncodeToBytes_ReadsGrayscaleAlpha8RowsUsingStrideWhenRequested()
    {
        PngImage image = CreateImage(CreateBuffer(PngPixelFormat.GrayscaleAlpha8, 2, 2, 6, new byte[]
        {
            1, 255, 2, 128, 99, 99,
            3, 64, 4, 0, 88, 88
        }));

        byte[] encoded = Png.EncodeToBytes(image, CreateGrayscaleAlpha8Options(PngInterlaceMethod.None));
        PngImage decoded = Png.DecodeBytes(encoded);

        AssertImage(decoded, 2, 2, PngInterlaceMethod.None, PngPixelFormat.GrayscaleAlpha8, PngColorType.GrayscaleAlpha, PngBitDepth.Eight, 4, new byte[]
        {
            1, 255, 2, 128,
            3, 64, 4, 0
        });
    }

    [Test]
    public void EncodeToBytes_RoundTripsAdam7GrayscaleAlpha8ImageWhenRequested()
    {
        byte[] pixels = new byte[] { 7, 123 };
        PngImage image = Png.CreateImage(1, 1, PngPixelFormat.GrayscaleAlpha8, pixels);

        byte[] encoded = Png.EncodeToBytes(image, CreateGrayscaleAlpha8Options(PngInterlaceMethod.Adam7));
        PngImage decoded = Png.DecodeBytes(encoded);

        AssertImage(decoded, 1, 1, PngInterlaceMethod.Adam7, PngPixelFormat.GrayscaleAlpha8, PngColorType.GrayscaleAlpha, PngBitDepth.Eight, 2, pixels);
    }

    [Test]
    public void EncodeToBytes_RoundTripsGrayscale8ImageWhenRequested()
    {
        byte[] pixels = new byte[]
        {
            1, 2,
            3, 4
        };
        PngImage image = Png.CreateImage(2, 2, PngPixelFormat.Grayscale8, pixels);

        byte[] encoded = Png.EncodeToBytes(image, CreateGrayscale8Options(PngInterlaceMethod.None));
        PngIdentifyResult identify = Png.IdentifyBytes(encoded);
        PngImage decoded = Png.DecodeBytes(encoded);

        Assert.That(identify.IsValid, Is.True);
        Assert.That(identify.Info.ColorType, Is.EqualTo(PngColorType.Grayscale));
        Assert.That(identify.Info.BitDepth, Is.EqualTo(PngBitDepth.Eight));
        AssertImage(decoded, 2, 2, PngInterlaceMethod.None, PngPixelFormat.Grayscale8, PngColorType.Grayscale, PngBitDepth.Eight, 2, pixels);
    }

    [Test]
    public void EncodeToBytes_ReadsGrayscale8RowsUsingStrideWhenRequested()
    {
        PngImage image = CreateImage(CreateBuffer(PngPixelFormat.Grayscale8, 3, 2, 5, new byte[]
        {
            1, 2, 3, 99, 99,
            4, 5, 6, 88, 88
        }));

        byte[] encoded = Png.EncodeToBytes(image, CreateGrayscale8Options(PngInterlaceMethod.None));
        PngImage decoded = Png.DecodeBytes(encoded);

        AssertImage(decoded, 3, 2, PngInterlaceMethod.None, PngPixelFormat.Grayscale8, PngColorType.Grayscale, PngBitDepth.Eight, 3, new byte[]
        {
            1, 2, 3,
            4, 5, 6
        });
    }

    [Test]
    public void EncodeToBytes_RoundTripsAdam7Grayscale8ImageWhenRequested()
    {
        byte[] pixels = new byte[] { 77 };
        PngImage image = Png.CreateImage(1, 1, PngPixelFormat.Grayscale8, pixels);

        byte[] encoded = Png.EncodeToBytes(image, CreateGrayscale8Options(PngInterlaceMethod.Adam7));
        PngImage decoded = Png.DecodeBytes(encoded);

        AssertImage(decoded, 1, 1, PngInterlaceMethod.Adam7, PngPixelFormat.Grayscale8, PngColorType.Grayscale, PngBitDepth.Eight, 1, pixels);
    }

    [Test]
    public void EncodeToBytes_RoundTrips16BitFormatsWhenRequested()
    {
        Assert16BitRoundTrip(PngPixelFormat.Rgba16, PngColorType.TruecolorAlpha, PngInterlaceMethod.None, 2, 1, 16, CreateBigEndianSamples(
            0x0102, 0x0304, 0x0506, 0x0708,
            0x1112, 0x1314, 0x1516, 0x1718));
        Assert16BitRoundTrip(PngPixelFormat.Rgb16, PngColorType.Truecolor, PngInterlaceMethod.None, 2, 1, 12, CreateBigEndianSamples(
            0x2122, 0x2324, 0x2526,
            0x3132, 0x3334, 0x3536));
        Assert16BitRoundTrip(PngPixelFormat.GrayscaleAlpha16, PngColorType.GrayscaleAlpha, PngInterlaceMethod.None, 2, 1, 8, CreateBigEndianSamples(
            0x4142, 0x4344,
            0x5152, 0x5354));
        Assert16BitRoundTrip(PngPixelFormat.Grayscale16, PngColorType.Grayscale, PngInterlaceMethod.None, 2, 1, 4, CreateBigEndianSamples(
            0x6162,
            0x7172));
    }

    [Test]
    public void EncodeToBytes_Reads16BitRowsUsingStrideWhenRequested()
    {
        PngImage image = CreateImage(CreateBuffer(PngPixelFormat.Rgba16, 1, 2, 12, new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8, 99, 99, 99, 99,
            9, 10, 11, 12, 13, 14, 15, 16, 88, 88, 88, 88
        }));

        byte[] encoded = Png.EncodeToBytes(image, Create16BitOptions(PngColorType.TruecolorAlpha, PngInterlaceMethod.None));
        PngImage decoded = Png.DecodeBytes(encoded);

        AssertImage(decoded, 1, 2, PngInterlaceMethod.None, PngPixelFormat.Rgba16, PngColorType.TruecolorAlpha, PngBitDepth.Sixteen, 8, new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8,
            9, 10, 11, 12, 13, 14, 15, 16
        });
    }

    [Test]
    public void EncodeToBytes_RoundTripsAdam716BitFormatsWhenRequested()
    {
        Assert16BitRoundTrip(PngPixelFormat.Rgba16, PngColorType.TruecolorAlpha, PngInterlaceMethod.Adam7, 1, 1, 8, CreateBigEndianSamples(0x0102, 0x0304, 0x0506, 0x0708));
        Assert16BitRoundTrip(PngPixelFormat.Rgb16, PngColorType.Truecolor, PngInterlaceMethod.Adam7, 1, 1, 6, CreateBigEndianSamples(0x1112, 0x1314, 0x1516));
        Assert16BitRoundTrip(PngPixelFormat.GrayscaleAlpha16, PngColorType.GrayscaleAlpha, PngInterlaceMethod.Adam7, 1, 1, 4, CreateBigEndianSamples(0x2122, 0x2324));
        Assert16BitRoundTrip(PngPixelFormat.Grayscale16, PngColorType.Grayscale, PngInterlaceMethod.Adam7, 1, 1, 2, CreateBigEndianSamples(0x3132));
    }

    [Test]
    public void EncodeToBytes_RoundTripsIndexed8ImageWithSuppliedPalette()
    {
        byte[] indices = new byte[]
        {
            0, 1, 2,
            3, 2, 1
        };
        PngImage image = Png.CreateImage(3, 2, PngPixelFormat.Indexed8, indices);
        image.Metadata.Palette = CreatePalette(
            CreatePaletteEntry(0, 0, 0),
            CreatePaletteEntry(255, 0, 0),
            CreatePaletteEntry(0, 255, 0),
            CreatePaletteEntry(0, 0, 255));

        byte[] encoded = Png.EncodeToBytes(image, CreateIndexedOptions(PngBitDepth.Eight, PngInterlaceMethod.None));
        PngChunkListResult chunkList = Png.ReadChunks(encoded);
        PngImage decoded = Png.DecodeBytes(encoded);

        Assert.That(Png.ValidateBytes(encoded).IsValid, Is.True);
        Assert.That(chunkList.Chunks, Has.Length.EqualTo(4));
        Assert.That(chunkList.Chunks[0].Type, Is.EqualTo(PngConstants.ChunkIHDR));
        Assert.That(chunkList.Chunks[1].Type, Is.EqualTo(PngConstants.ChunkPLTE));
        Assert.That(chunkList.Chunks[2].Type, Is.EqualTo(PngConstants.ChunkIDAT));
        Assert.That(chunkList.Chunks[3].Type, Is.EqualTo(PngConstants.ChunkIEND));
        AssertImage(decoded, 3, 2, PngInterlaceMethod.None, PngPixelFormat.Indexed8, PngColorType.IndexedColor, PngBitDepth.Eight, 3, indices);
        Assert.That(decoded.Metadata.Palette.Entries, Has.Length.EqualTo(4));
        AssertPaletteEntry(decoded.Metadata.Palette.Entries[1], 255, 0, 0);
        AssertPaletteEntry(decoded.Metadata.Palette.Entries[2], 0, 255, 0);
    }

    [Test]
    public void EncodeToBytes_RoundTripsPackedIndexed1ImageWithSuppliedPalette()
    {
        byte[] indices = new byte[] { 0xB2 };
        PngImage image = Png.CreateImage(8, 1, PngPixelFormat.Indexed1, indices);
        image.Metadata.Palette = CreatePalette(
            CreatePaletteEntry(0, 0, 0),
            CreatePaletteEntry(255, 255, 255));

        byte[] encoded = Png.EncodeToBytes(image, CreateIndexedOptions(PngBitDepth.One, PngInterlaceMethod.None));
        PngImage decoded = Png.DecodeBytes(encoded);

        AssertImage(decoded, 8, 1, PngInterlaceMethod.None, PngPixelFormat.Indexed1, PngColorType.IndexedColor, PngBitDepth.One, 1, indices);
        AssertPaletteEntry(decoded.Metadata.Palette.Entries[0], 0, 0, 0);
        AssertPaletteEntry(decoded.Metadata.Palette.Entries[1], 255, 255, 255);
    }

    [Test]
    public void EncodeToBytes_ReadsIndexedRowsUsingStrideWithSuppliedPalette()
    {
        PngImage image = CreateImage(CreateBuffer(PngPixelFormat.Indexed4, 3, 2, 3, new byte[]
        {
            0x12, 0x30, 99,
            0x23, 0x10, 88
        }));
        image.Metadata.Palette = CreatePalette(
            CreatePaletteEntry(0, 0, 0),
            CreatePaletteEntry(10, 20, 30),
            CreatePaletteEntry(40, 50, 60),
            CreatePaletteEntry(70, 80, 90));

        byte[] encoded = Png.EncodeToBytes(image, CreateIndexedOptions(PngBitDepth.Four, PngInterlaceMethod.None));
        PngImage decoded = Png.DecodeBytes(encoded);

        AssertImage(decoded, 3, 2, PngInterlaceMethod.None, PngPixelFormat.Indexed4, PngColorType.IndexedColor, PngBitDepth.Four, 2, new byte[]
        {
            0x12, 0x30,
            0x23, 0x10
        });
    }

    [Test]
    public void EncodeToBytes_RoundTripsAdam7Indexed8ImageWithSuppliedPalette()
    {
        byte[] indices = new byte[] { 1 };
        PngImage image = Png.CreateImage(1, 1, PngPixelFormat.Indexed8, indices);
        image.Metadata.Palette = CreatePalette(
            CreatePaletteEntry(0, 0, 0),
            CreatePaletteEntry(1, 2, 3));

        byte[] encoded = Png.EncodeToBytes(image, CreateIndexedOptions(PngBitDepth.Eight, PngInterlaceMethod.Adam7));
        PngImage decoded = Png.DecodeBytes(encoded);

        AssertImage(decoded, 1, 1, PngInterlaceMethod.Adam7, PngPixelFormat.Indexed8, PngColorType.IndexedColor, PngBitDepth.Eight, 1, indices);
        AssertPaletteEntry(decoded.Metadata.Palette.Entries[1], 1, 2, 3);
    }

    [Test]
    public void EncodeToBytes_SplitsIndexedIdatChunksAfterPalette()
    {
        byte[] indices = new byte[]
        {
            0, 1, 2, 3,
            3, 2, 1, 0
        };
        PngImage image = Png.CreateImage(4, 2, PngPixelFormat.Indexed8, indices);
        image.Metadata.Palette = CreatePalette(
            CreatePaletteEntry(0, 0, 0),
            CreatePaletteEntry(255, 0, 0),
            CreatePaletteEntry(0, 255, 0),
            CreatePaletteEntry(0, 0, 255));
        PngEncoderOptions options = CreateIndexedOptions(PngBitDepth.Eight, PngInterlaceMethod.None);
        options.MaxIdatChunkBytes = 1;

        byte[] encoded = Png.EncodeToBytes(image, options);
        PngChunkListResult chunkList = Png.ReadChunks(encoded);
        PngImage decoded = Png.DecodeBytes(encoded);

        Assert.That(chunkList.Chunks[0].Type, Is.EqualTo(PngConstants.ChunkIHDR));
        Assert.That(chunkList.Chunks[1].Type, Is.EqualTo(PngConstants.ChunkPLTE));
        AssertSplitIdatChunks(chunkList.Chunks, 1, 2);
        AssertImage(decoded, 4, 2, PngInterlaceMethod.None, PngPixelFormat.Indexed8, PngColorType.IndexedColor, PngBitDepth.Eight, 4, indices);
    }

    [Test]
    public void EncodeToBytes_PreservesIndexedMetadataAroundPaletteAndIdat()
    {
        byte[] indices = new byte[] { 0, 1 };
        PngImage image = Png.CreateImage(2, 1, PngPixelFormat.Indexed8, indices);
        image.Metadata.Palette = CreatePalette(
            CreatePaletteEntry(0, 0, 0),
            CreatePaletteEntry(255, 255, 255));
        image.Metadata.Chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkGamma, new byte[] { 0, 0, 177, 143 }),
            CreateChunk(PngConstants.ChunkPLTE, new byte[] { 9, 9, 9 }),
            CreateChunk(PngConstants.ChunkPhysicalPixels, new byte[] { 0, 0, 11, 19, 0, 0, 11, 19, 1 }),
            CreateChunk(PngConstants.ChunkText, new byte[] { 65, 0, 66 })
        };

        byte[] encoded = Png.EncodeToBytes(image, CreateIndexedOptions(PngBitDepth.Eight, PngInterlaceMethod.None));
        PngChunkListResult chunkList = Png.ReadChunks(encoded);
        PngImage decoded = Png.DecodeBytes(encoded);

        Assert.That(chunkList.Chunks, Has.Length.EqualTo(7));
        Assert.That(chunkList.Chunks[0].Type, Is.EqualTo(PngConstants.ChunkIHDR));
        Assert.That(chunkList.Chunks[1].Type, Is.EqualTo(PngConstants.ChunkGamma));
        Assert.That(chunkList.Chunks[2].Type, Is.EqualTo(PngConstants.ChunkPLTE));
        Assert.That(chunkList.Chunks[2].Data, Is.EqualTo(new byte[] { 0, 0, 0, 255, 255, 255 }));
        Assert.That(chunkList.Chunks[3].Type, Is.EqualTo(PngConstants.ChunkPhysicalPixels));
        Assert.That(chunkList.Chunks[4].Type, Is.EqualTo(PngConstants.ChunkIDAT));
        Assert.That(chunkList.Chunks[5].Type, Is.EqualTo(PngConstants.ChunkText));
        Assert.That(chunkList.Chunks[6].Type, Is.EqualTo(PngConstants.ChunkIEND));
        AssertImage(decoded, 2, 1, PngInterlaceMethod.None, PngPixelFormat.Indexed8, PngColorType.IndexedColor, PngBitDepth.Eight, 2, indices);
    }

    [Test]
    public void EncodeToBytes_StripsIndexedMetadataButKeepsGeneratedPalette()
    {
        byte[] indices = new byte[] { 0, 1 };
        PngImage image = Png.CreateImage(2, 1, PngPixelFormat.Indexed8, indices);
        image.Metadata.Palette = CreatePalette(
            CreatePaletteEntry(0, 0, 0),
            CreatePaletteEntry(255, 255, 255));
        image.Metadata.Chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkGamma, new byte[] { 0, 0, 177, 143 }),
            CreateChunk(PngConstants.ChunkPhysicalPixels, new byte[] { 0, 0, 11, 19, 0, 0, 11, 19, 1 }),
            CreateChunk(PngConstants.ChunkText, new byte[] { 65, 0, 66 })
        };
        PngEncoderOptions options = CreateIndexedOptions(PngBitDepth.Eight, PngInterlaceMethod.None);
        options.MetadataPolicy = PngMetadataPolicy.Strip;

        byte[] encoded = Png.EncodeToBytes(image, options);
        PngChunkListResult chunkList = Png.ReadChunks(encoded);
        PngImage decoded = Png.DecodeBytes(encoded);

        Assert.That(chunkList.Chunks, Has.Length.EqualTo(4));
        Assert.That(chunkList.Chunks[0].Type, Is.EqualTo(PngConstants.ChunkIHDR));
        Assert.That(chunkList.Chunks[1].Type, Is.EqualTo(PngConstants.ChunkPLTE));
        Assert.That(chunkList.Chunks[1].Data, Is.EqualTo(new byte[] { 0, 0, 0, 255, 255, 255 }));
        Assert.That(chunkList.Chunks[2].Type, Is.EqualTo(PngConstants.ChunkIDAT));
        Assert.That(chunkList.Chunks[3].Type, Is.EqualTo(PngConstants.ChunkIEND));
        Assert.That(HasChunk(chunkList.Chunks, PngConstants.ChunkGamma), Is.False);
        Assert.That(HasChunk(chunkList.Chunks, PngConstants.ChunkPhysicalPixels), Is.False);
        Assert.That(HasChunk(chunkList.Chunks, PngConstants.ChunkText), Is.False);
        AssertImage(decoded, 2, 1, PngInterlaceMethod.None, PngPixelFormat.Indexed8, PngColorType.IndexedColor, PngBitDepth.Eight, 2, indices);
    }

    [Test]
    public void EncodeToStream_WritesAtCurrentPosition()
    {
        PngImage image = CreateImage(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 9, 8, 7, 6 }));
        MemoryStream stream = new MemoryStream();
        stream.WriteByte(0xAA);

        Png.EncodeToStream(stream, image, CreateFixedOptions(PngInterlaceMethod.None));

        byte[] encoded = stream.ToArray();
        PngImage decoded = Png.DecodeBytes(encoded, 1, new PngDecoderOptions());
        AssertImage(decoded, 1, 1, PngInterlaceMethod.None, PngPixelFormat.Rgba8, 4, new byte[] { 9, 8, 7, 6 });
        Assert.That(encoded[0], Is.EqualTo(0xAA));
    }

    [Test]
    public void EncodeToBytes_RoundTripsAdam7Image()
    {
        PngImage image = CreateImage(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 11, 12, 13, 14 }));

        byte[] encoded = Png.EncodeToBytes(image, CreateFixedOptions(PngInterlaceMethod.Adam7));
        PngImage decoded = Png.DecodeBytes(encoded);

        AssertImage(decoded, 1, 1, PngInterlaceMethod.Adam7, PngPixelFormat.Rgba8, 4, new byte[] { 11, 12, 13, 14 });
    }

    [Test]
    public void EncodeToBytes_EncodesAnimationWhenRequested()
    {
        PngImage image = CreateImage(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 1, 2, 3, 4 }));
        image.Animation.FrameCount = 2;
        image.Animation.PlayCount = 7;
        image.Animation.Frames = new PngFrame[]
        {
            CreateFrame(1, 1, 0, 0, CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 1, 2, 3, 4 })),
            CreateFrame(1, 1, 0, 0, CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 5, 6, 7, 8 }))
        };
        PngEncoderOptions options = CreateFixedOptions(PngInterlaceMethod.None);
        options.EncodeAnimation = true;

        byte[] encoded = Png.EncodeToBytes(image, options);
        PngIdentifyResult identify = Png.IdentifyBytes(encoded);
        PngChunkListResult chunkList = PngChunkListReader.Read(encoded, PngConstants.SignatureLength);

        Assert.That(identify.Info.IsAnimated, Is.True);
        Assert.That(identify.Info.FrameCount, Is.EqualTo(2));
        Assert.That(identify.Info.PlayCount, Is.EqualTo(7));
        Assert.That(HasChunk(chunkList.Chunks, PngConstants.ChunkAnimationControl), Is.True);
        Assert.That(HasChunk(chunkList.Chunks, PngConstants.ChunkFrameControl), Is.True);
        Assert.That(HasChunk(chunkList.Chunks, PngConstants.ChunkFrameData), Is.True);
    }

    [Test]
    public void Encode_RejectsInvalidInputs()
    {
        PngImage image = CreateImage(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 1, 2, 3, 4 }));
        PngImage nullPixels = new PngImage();
        nullPixels.Pixels = null!;
        PngImage nullData = CreateImage(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, null!));
        PngImage wrongFormat = CreateImage(CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 1, 2, 3 }));
        PngImage shortData = CreateImage(CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 8, new byte[] { 1, 2, 3, 4 }));
        PngEncoderOptions nullLimits = CreateFixedOptions(PngInterlaceMethod.None);
        nullLimits.Limits = null!;
        PngEncoderOptions lowIdatLimit = CreateFixedOptions(PngInterlaceMethod.None);
        lowIdatLimit.Limits.MaxIdatBytes = 1;
        PngEncoderOptions negativeIdatChunkSize = CreateFixedOptions(PngInterlaceMethod.None);
        negativeIdatChunkSize.MaxIdatChunkBytes = -1;
        PngImage unknownChunk = CreateImage(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 1, 2, 3, 4 }));
        unknownChunk.Metadata.Chunks = new PngChunk[] { CreateChunk("vpAg", new byte[] { 1 }) };
        PngEncoderOptions invalidUnknownDisposition = CreateFixedOptions(PngInterlaceMethod.None);
        invalidUnknownDisposition.UnknownChunkDisposition = (PngChunkDisposition)99;

        Assert.Throws<ArgumentNullException>(() => Png.EncodeToBytes(null!));
        Assert.Throws<ArgumentNullException>(() => Png.EncodeToBytes(nullPixels));
        Assert.Throws<ArgumentNullException>(() => Png.EncodeToBytes(nullData));
        Assert.Throws<ArgumentNullException>(() => Png.EncodeToBytes(image, null!));
        Assert.Throws<ArgumentNullException>(() => Png.EncodeToBytes(image, nullLimits));
        Assert.Throws<ArgumentNullException>(() => Png.EncodeToStream(null!, image));
        Assert.Throws<ArgumentNullException>(() => Png.EncodeToStream(new MemoryStream(), null!));
        Assert.Throws<ArgumentNullException>(() => Png.EncodeToStream(new MemoryStream(), image, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => Png.EncodeToBytes(wrongFormat, CreateFixedOptions(PngInterlaceMethod.None)));
        Assert.Throws<ArgumentOutOfRangeException>(() => Png.EncodeToBytes(shortData));
        Assert.Throws<ArgumentOutOfRangeException>(() => Png.EncodeToBytes(image, negativeIdatChunkSize));
        Assert.Throws<ArgumentOutOfRangeException>(() => Png.EncodeToBytes(unknownChunk, invalidUnknownDisposition));
        Assert.Throws<OverflowException>(() => Png.EncodeToBytes(image, lowIdatLimit));
    }

    [Test]
    public void EncodeToBytes_RejectsInvalidIndexedPaletteInputs()
    {
        PngImage missingPalette = Png.CreateImage(1, 1, PngPixelFormat.Indexed8, new byte[] { 0 });
        PngImage nullMetadata = Png.CreateImage(1, 1, PngPixelFormat.Indexed8, new byte[] { 0 });
        nullMetadata.Metadata = null!;
        PngImage nullPalette = Png.CreateImage(1, 1, PngPixelFormat.Indexed8, new byte[] { 0 });
        nullPalette.Metadata.Palette = null!;
        PngImage nullEntries = Png.CreateImage(1, 1, PngPixelFormat.Indexed8, new byte[] { 0 });
        nullEntries.Metadata.Palette.Entries = null!;
        PngImage nullEntry = Png.CreateImage(1, 1, PngPixelFormat.Indexed8, new byte[] { 0 });
        nullEntry.Metadata.Palette.Entries = new PngPaletteEntry[] { null! };
        PngImage tooManyForBitDepth = Png.CreateImage(1, 1, PngPixelFormat.Indexed2, new byte[] { 0 });
        tooManyForBitDepth.Metadata.Palette = CreatePalette(
            CreatePaletteEntry(0, 0, 0),
            CreatePaletteEntry(1, 1, 1),
            CreatePaletteEntry(2, 2, 2),
            CreatePaletteEntry(3, 3, 3),
            CreatePaletteEntry(4, 4, 4));

        Assert.Throws<InvalidDataException>(() => Png.EncodeToBytes(missingPalette, CreateIndexedOptions(PngBitDepth.Eight, PngInterlaceMethod.None)));
        Assert.Throws<ArgumentNullException>(() => Png.EncodeToBytes(nullMetadata, CreateIndexedOptions(PngBitDepth.Eight, PngInterlaceMethod.None)));
        Assert.Throws<ArgumentNullException>(() => Png.EncodeToBytes(nullPalette, CreateIndexedOptions(PngBitDepth.Eight, PngInterlaceMethod.None)));
        Assert.Throws<ArgumentNullException>(() => Png.EncodeToBytes(nullEntries, CreateIndexedOptions(PngBitDepth.Eight, PngInterlaceMethod.None)));
        Assert.Throws<InvalidDataException>(() => Png.EncodeToBytes(nullEntry, CreateIndexedOptions(PngBitDepth.Eight, PngInterlaceMethod.None)));
        Assert.Throws<InvalidDataException>(() => Png.EncodeToBytes(tooManyForBitDepth, CreateIndexedOptions(PngBitDepth.Two, PngInterlaceMethod.None)));
    }

    static void AssertImage(PngImage image, int width, int height, PngInterlaceMethod interlaceMethod, PngPixelFormat format, int stride, byte[] data)
    {
        AssertImage(image, width, height, interlaceMethod, format, PngColorType.TruecolorAlpha, PngBitDepth.Eight, stride, data);
    }

    static void AssertImage(PngImage image, int width, int height, PngInterlaceMethod interlaceMethod, PngPixelFormat format, PngColorType colorType, PngBitDepth bitDepth, int stride, byte[] data)
    {
        Assert.That(image.Info.Width, Is.EqualTo(width));
        Assert.That(image.Info.Height, Is.EqualTo(height));
        Assert.That(image.Info.ColorType, Is.EqualTo(colorType));
        Assert.That(image.Info.BitDepth, Is.EqualTo(bitDepth));
        Assert.That(image.Info.InterlaceMethod, Is.EqualTo(interlaceMethod));
        Assert.That(image.Info.PixelFormat, Is.EqualTo(format));
        Assert.That(image.Pixels.Format, Is.EqualTo(format));
        Assert.That(image.Pixels.Width, Is.EqualTo(width));
        Assert.That(image.Pixels.Height, Is.EqualTo(height));
        Assert.That(image.Pixels.Stride, Is.EqualTo(stride));
        Assert.That(image.Pixels.Data, Is.EqualTo(data));
    }

    static PngImage CreateImage(PngPixelBuffer pixels)
    {
        PngImage image = new PngImage();
        image.Pixels = pixels;
        return image;
    }

    static PngFrame CreateFrame(int width, int height, int xOffset, int yOffset, PngPixelBuffer pixels)
    {
        PngFrame frame = new PngFrame();
        frame.Width = width;
        frame.Height = height;
        frame.XOffset = xOffset;
        frame.YOffset = yOffset;
        frame.DelayDenominator = 100;
        frame.DisposeOperation = PngApngDisposeOperation.None;
        frame.BlendOperation = PngApngBlendOperation.Source;
        frame.Pixels = pixels;
        return frame;
    }

    static PngPixelBuffer CreateBuffer(PngPixelFormat format, int width, int height, int stride, byte[] data)
    {
        PngPixelBuffer buffer = new PngPixelBuffer();
        buffer.Format = format;
        buffer.Width = width;
        buffer.Height = height;
        buffer.Stride = stride;
        buffer.Data = data;
        return buffer;
    }

    static PngChunk CreateChunk(string type, byte[] data)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = type;
        chunk.Data = data;
        chunk.Length = (uint)data.Length;
        return chunk;
    }

    static PngEncoderOptions CreateFixedOptions(PngInterlaceMethod interlaceMethod)
    {
        PngEncoderOptions options = new PngEncoderOptions();
        options.ColorType = PngColorType.TruecolorAlpha;
        options.BitDepth = PngBitDepth.Eight;
        options.InterlaceMethod = interlaceMethod;
        options.UseFixedFilter = true;
        options.FixedFilterType = PngFilterType.None;
        options.UseAutomaticColorType = false;
        options.UseAutomaticBitDepth = false;
        return options;
    }

    static PngEncoderOptions CreateRgb8Options(PngInterlaceMethod interlaceMethod)
    {
        PngEncoderOptions options = CreateFixedOptions(interlaceMethod);
        options.ColorType = PngColorType.Truecolor;
        options.BitDepth = PngBitDepth.Eight;
        return options;
    }

    static PngEncoderOptions CreateGrayscaleAlpha8Options(PngInterlaceMethod interlaceMethod)
    {
        PngEncoderOptions options = CreateFixedOptions(interlaceMethod);
        options.ColorType = PngColorType.GrayscaleAlpha;
        options.BitDepth = PngBitDepth.Eight;
        return options;
    }

    static PngEncoderOptions CreateGrayscale8Options(PngInterlaceMethod interlaceMethod)
    {
        PngEncoderOptions options = CreateFixedOptions(interlaceMethod);
        options.ColorType = PngColorType.Grayscale;
        options.BitDepth = PngBitDepth.Eight;
        return options;
    }

    static PngEncoderOptions Create16BitOptions(PngColorType colorType, PngInterlaceMethod interlaceMethod)
    {
        PngEncoderOptions options = CreateFixedOptions(interlaceMethod);
        options.ColorType = colorType;
        options.BitDepth = PngBitDepth.Sixteen;
        return options;
    }

    static PngEncoderOptions CreateIndexedOptions(PngBitDepth bitDepth, PngInterlaceMethod interlaceMethod)
    {
        PngEncoderOptions options = CreateFixedOptions(interlaceMethod);
        options.ColorType = PngColorType.IndexedColor;
        options.BitDepth = bitDepth;
        return options;
    }

    static void Assert16BitRoundTrip(PngPixelFormat format, PngColorType colorType, PngInterlaceMethod interlaceMethod, int width, int height, int stride, byte[] pixels)
    {
        PngImage image = Png.CreateImage(width, height, format, pixels);

        byte[] encoded = Png.EncodeToBytes(image, Create16BitOptions(colorType, interlaceMethod));
        PngIdentifyResult identify = Png.IdentifyBytes(encoded);
        PngImage decoded = Png.DecodeBytes(encoded);

        Assert.That(identify.IsValid, Is.True);
        Assert.That(identify.Info.ColorType, Is.EqualTo(colorType));
        Assert.That(identify.Info.BitDepth, Is.EqualTo(PngBitDepth.Sixteen));
        AssertImage(decoded, width, height, interlaceMethod, format, colorType, PngBitDepth.Sixteen, stride, pixels);
    }

    static byte[] CreateBigEndianSamples(params ushort[] samples)
    {
        byte[] data = new byte[samples.Length * 2];
        int index = 0;
        while (index < samples.Length)
        {
            data[index * 2] = (byte)(samples[index] >> 8);
            data[index * 2 + 1] = (byte)(samples[index] & 0xFF);
            index += 1;
        }

        return data;
    }

    static PngPalette CreatePalette(params PngPaletteEntry[] entries)
    {
        PngPalette palette = new PngPalette();
        palette.Entries = entries;
        return palette;
    }

    static PngPaletteEntry CreatePaletteEntry(int red, int green, int blue)
    {
        PngPaletteEntry entry = new PngPaletteEntry();
        entry.Red = (byte)red;
        entry.Green = (byte)green;
        entry.Blue = (byte)blue;
        return entry;
    }

    static void AssertPaletteEntry(PngPaletteEntry entry, int red, int green, int blue)
    {
        Assert.That(entry.Red, Is.EqualTo(red));
        Assert.That(entry.Green, Is.EqualTo(green));
        Assert.That(entry.Blue, Is.EqualTo(blue));
    }

    static void AssertAutomaticColorType(PngPixelFormat format, PngColorType colorType, int width, int height, byte[] pixels)
    {
        PngImage image = Png.CreateImage(width, height, format, pixels);

        byte[] encoded = Png.EncodeToBytes(image);
        PngIdentifyResult identify = Png.IdentifyBytes(encoded);
        PngImage decoded = Png.DecodeBytes(encoded);

        Assert.That(identify.Info.ColorType, Is.EqualTo(colorType));
        Assert.That(identify.Info.BitDepth, Is.EqualTo(PngBitDepth.Eight));
        AssertImage(decoded, width, height, PngInterlaceMethod.None, format, colorType, PngBitDepth.Eight, pixels.Length / height, pixels);
    }

    static void AssertAutomaticFormat(PngPixelFormat format, PngColorType colorType, PngBitDepth bitDepth, int width, int height, int stride, byte[] pixels)
    {
        PngImage image = Png.CreateImage(width, height, format, pixels);

        byte[] encoded = Png.EncodeToBytes(image);
        PngIdentifyResult identify = Png.IdentifyBytes(encoded);
        PngImage decoded = Png.DecodeBytes(encoded);

        Assert.That(identify.Info.ColorType, Is.EqualTo(colorType));
        Assert.That(identify.Info.BitDepth, Is.EqualTo(bitDepth));
        AssertImage(decoded, width, height, PngInterlaceMethod.None, format, colorType, bitDepth, stride, pixels);
    }

    static bool HasChunk(PngChunk[] chunks, string chunkType)
    {
        int index = 0;
        while (index < chunks.Length)
        {
            if (chunks[index].Type == chunkType)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    static void AssertSplitIdatChunks(PngChunk[] chunks, int maxIdatChunkBytes, int firstIdatIndex)
    {
        int idatCount = CountChunks(chunks, PngConstants.ChunkIDAT);
        Assert.That(idatCount, Is.GreaterThan(1));
        Assert.That(chunks[firstIdatIndex].Type, Is.EqualTo(PngConstants.ChunkIDAT));
        Assert.That(chunks[chunks.Length - 1].Type, Is.EqualTo(PngConstants.ChunkIEND));

        int index = 0;
        bool seenIdat = false;
        bool endedIdat = false;
        while (index < chunks.Length)
        {
            if (chunks[index].Type == PngConstants.ChunkIDAT)
            {
                Assert.That(endedIdat, Is.False);
                Assert.That(index, Is.GreaterThanOrEqualTo(firstIdatIndex));
                Assert.That(chunks[index].Data, Has.Length.LessThanOrEqualTo(maxIdatChunkBytes));
                seenIdat = true;
            }
            else if (seenIdat)
            {
                endedIdat = true;
                Assert.That(chunks[index].Type, Is.EqualTo(PngConstants.ChunkIEND));
            }

            index += 1;
        }
    }

    static int CountChunks(PngChunk[] chunks, string chunkType)
    {
        int count = 0;
        int index = 0;
        while (index < chunks.Length)
        {
            if (chunks[index].Type == chunkType)
            {
                count += 1;
            }

            index += 1;
        }

        return count;
    }
}
