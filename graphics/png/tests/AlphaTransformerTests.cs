using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class AlphaTransformerTests
{
    [Test]
    public void AddOpaqueAlpha_AddsAlphaToGrayscale8RowsUsingStride()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Grayscale8, 3, 2, 4, new byte[] { 7, 8, 9, 99, 10, 11, 12, 99 });

        PngPixelBuffer result = PngAlphaTransformer.AddOpaqueAlpha(buffer);

        AssertBuffer(result, PngPixelFormat.GrayscaleAlpha8, 3, 2, 6, new byte[]
        {
            7, 255, 8, 255, 9, 255,
            10, 255, 11, 255, 12, 255
        });
    }

    [Test]
    public void AddOpaqueAlpha_AddsAlphaToPackedGrayscaleAsGrayscaleAlpha8()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Grayscale2, 3, 1, 1, new byte[] { 0b00011000 });

        PngPixelBuffer result = PngAlphaTransformer.AddOpaqueAlpha(buffer);

        AssertBuffer(result, PngPixelFormat.GrayscaleAlpha8, 3, 1, 6, new byte[] { 0, 255, 85, 255, 170, 255 });
    }

    [Test]
    public void AddOpaqueAlpha_AddsAlphaToGrayscale16()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Grayscale16, 2, 1, 4, CreateBigEndianSamples(0x1234, 0x8001));

        PngPixelBuffer result = PngAlphaTransformer.AddOpaqueAlpha(buffer);

        AssertBuffer(result, PngPixelFormat.GrayscaleAlpha16, 2, 1, 8, new byte[] { 18, 52, 255, 255, 128, 1, 255, 255 });
    }

    [Test]
    public void AddOpaqueAlpha_AddsAlphaToRgb8RowsUsingStride()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgb8, 2, 2, 7, new byte[]
        {
            1, 2, 3, 4, 5, 6, 99,
            7, 8, 9, 10, 11, 12, 99
        });

        PngPixelBuffer result = PngAlphaTransformer.AddOpaqueAlpha(buffer);

        AssertBuffer(result, PngPixelFormat.Rgba8, 2, 2, 8, new byte[]
        {
            1, 2, 3, 255, 4, 5, 6, 255,
            7, 8, 9, 255, 10, 11, 12, 255
        });
    }

    [Test]
    public void AddOpaqueAlpha_AddsAlphaToRgb16()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgb16, 2, 1, 12, CreateBigEndianSamples(0x1234, 0x4567, 0x89AB, 0x0001, 0x00FF, 0xFF00));

        PngPixelBuffer result = PngAlphaTransformer.AddOpaqueAlpha(buffer);

        AssertBuffer(result, PngPixelFormat.Rgba16, 2, 1, 16, new byte[]
        {
            18, 52, 69, 103, 137, 171, 255, 255,
            0, 1, 0, 255, 255, 0, 255, 255
        });
    }

    [Test]
    public void AddOpaqueAlpha_ReturnsIndependentBuffer()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 1, 2, 3 });

        PngPixelBuffer result = PngAlphaTransformer.AddOpaqueAlpha(buffer);
        buffer.Data[0] = 99;

        AssertBuffer(result, PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 1, 2, 3, 255 });
    }

    [Test]
    public void AddOpaqueAlpha_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => PngAlphaTransformer.AddOpaqueAlpha(null!));
        Assert.Throws<ArgumentNullException>(() => PngAlphaTransformer.AddOpaqueAlpha(CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, null!)));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.AddOpaqueAlpha(CreateBuffer(PngPixelFormat.Indexed8, 1, 1, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.AddOpaqueAlpha(CreateBuffer(PngPixelFormat.GrayscaleAlpha8, 1, 1, 2, new byte[] { 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.AddOpaqueAlpha(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 0, 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.AddOpaqueAlpha(CreateBuffer(PngPixelFormat.Rgb8, 0, 1, 3, new byte[] { 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.AddOpaqueAlpha(CreateBuffer(PngPixelFormat.Rgb8, 1, 0, 3, new byte[] { 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.AddOpaqueAlpha(CreateBuffer(PngPixelFormat.Rgb8, 2, 1, 5, new byte[] { 0, 0, 0, 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.AddOpaqueAlpha(CreateBuffer(PngPixelFormat.Rgb8, 2, 2, 6, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 })));
    }

    [Test]
    public void StripAlpha_StripsGrayscaleAlpha8RowsUsingStride()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.GrayscaleAlpha8, 3, 2, 8, new byte[]
        {
            7, 1, 8, 2, 9, 3, 99, 99,
            10, 4, 11, 5, 12, 6, 99, 99
        });

        PngPixelBuffer result = PngAlphaTransformer.StripAlpha(buffer);

        AssertBuffer(result, PngPixelFormat.Grayscale8, 3, 2, 3, new byte[] { 7, 8, 9, 10, 11, 12 });
    }

    [Test]
    public void StripAlpha_StripsGrayscaleAlpha16()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.GrayscaleAlpha16, 2, 1, 8, new byte[]
        {
            18, 52, 0, 0,
            128, 1, 255, 255
        });

        PngPixelBuffer result = PngAlphaTransformer.StripAlpha(buffer);

        AssertBuffer(result, PngPixelFormat.Grayscale16, 2, 1, 4, new byte[] { 18, 52, 128, 1 });
    }

    [Test]
    public void StripAlpha_StripsRgba8RowsUsingStride()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 10, new byte[]
        {
            1, 2, 3, 10, 4, 5, 6, 20, 99, 99,
            7, 8, 9, 30, 10, 11, 12, 40, 99, 99
        });

        PngPixelBuffer result = PngAlphaTransformer.StripAlpha(buffer);

        AssertBuffer(result, PngPixelFormat.Rgb8, 2, 2, 6, new byte[]
        {
            1, 2, 3, 4, 5, 6,
            7, 8, 9, 10, 11, 12
        });
    }

    [Test]
    public void StripAlpha_StripsRgba16()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgba16, 2, 1, 16, new byte[]
        {
            18, 52, 69, 103, 137, 171, 0, 0,
            0, 1, 0, 255, 255, 0, 255, 255
        });

        PngPixelBuffer result = PngAlphaTransformer.StripAlpha(buffer);

        AssertBuffer(result, PngPixelFormat.Rgb16, 2, 1, 12, new byte[]
        {
            18, 52, 69, 103, 137, 171,
            0, 1, 0, 255, 255, 0
        });
    }

    [Test]
    public void StripAlpha_ReturnsIndependentBuffer()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 1, 2, 3, 4 });

        PngPixelBuffer result = PngAlphaTransformer.StripAlpha(buffer);
        buffer.Data[0] = 99;

        AssertBuffer(result, PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 1, 2, 3 });
    }

    [Test]
    public void StripAlpha_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => PngAlphaTransformer.StripAlpha(null!));
        Assert.Throws<ArgumentNullException>(() => PngAlphaTransformer.StripAlpha(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, null!)));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.StripAlpha(CreateBuffer(PngPixelFormat.Indexed8, 1, 1, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.StripAlpha(CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.StripAlpha(CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.StripAlpha(CreateBuffer(PngPixelFormat.Rgba8, 0, 1, 4, new byte[] { 0, 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.StripAlpha(CreateBuffer(PngPixelFormat.Rgba8, 1, 0, 4, new byte[] { 0, 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.StripAlpha(CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 7, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.StripAlpha(CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 8, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 })));
    }

    [Test]
    public void IsOpaque_DetectsOpaqueAlphaFormatsUsingStride()
    {
        PngPixelBuffer grayscale8 = CreateBuffer(PngPixelFormat.GrayscaleAlpha8, 2, 2, 6, new byte[]
        {
            1, 255, 2, 255, 99, 99,
            3, 255, 4, 255, 88, 88
        });
        PngPixelBuffer rgba8 = CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 10, new byte[] { 1, 2, 3, 255, 4, 5, 6, 255, 0, 0 });
        PngPixelBuffer grayscale16 = CreateBuffer(PngPixelFormat.GrayscaleAlpha16, 2, 1, 8, CreateBigEndianSamples(0x1234, 0xFFFF, 0x4567, 0xFFFF));
        PngPixelBuffer rgba16 = CreateBuffer(PngPixelFormat.Rgba16, 1, 1, 8, CreateBigEndianSamples(0x1111, 0x2222, 0x3333, 0xFFFF));

        Assert.That(PngAlphaTransformer.IsOpaque(grayscale8), Is.True);
        Assert.That(PngAlphaTransformer.IsOpaque(rgba8), Is.True);
        Assert.That(PngAlphaTransformer.IsOpaque(grayscale16), Is.True);
        Assert.That(PngAlphaTransformer.IsOpaque(rgba16), Is.True);
    }

    [Test]
    public void IsOpaque_ReturnsFalseWhenAnyAlphaSampleIsNotOpaque()
    {
        PngPixelBuffer grayscale8 = CreateBuffer(PngPixelFormat.GrayscaleAlpha8, 2, 1, 4, new byte[] { 1, 255, 2, 254 });
        PngPixelBuffer rgba16 = CreateBuffer(PngPixelFormat.Rgba16, 1, 1, 8, CreateBigEndianSamples(0x1111, 0x2222, 0x3333, 0xFFFE));

        Assert.That(PngAlphaTransformer.IsOpaque(grayscale8), Is.False);
        Assert.That(PngAlphaTransformer.IsOpaque(rgba16), Is.False);
    }

    [Test]
    public void StripAlphaIfOpaque_StripsOpaqueRgba8RowsUsingStride()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 10, new byte[]
        {
            1, 2, 3, 255, 4, 5, 6, 255, 99, 99,
            7, 8, 9, 255, 10, 11, 12, 255, 88, 88
        });

        PngPixelBuffer result = PngAlphaTransformer.StripAlphaIfOpaque(buffer);

        AssertBuffer(result, PngPixelFormat.Rgb8, 2, 2, 6, new byte[]
        {
            1, 2, 3, 4, 5, 6,
            7, 8, 9, 10, 11, 12
        });
    }

    [Test]
    public void StripAlphaIfOpaque_StripsOpaqueGrayscaleAlpha16()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.GrayscaleAlpha16, 2, 1, 8, CreateBigEndianSamples(0x1234, 0xFFFF, 0x8001, 0xFFFF));

        PngPixelBuffer result = PngAlphaTransformer.StripAlphaIfOpaque(buffer);

        AssertBuffer(result, PngPixelFormat.Grayscale16, 2, 1, 4, CreateBigEndianSamples(0x1234, 0x8001));
    }

    [Test]
    public void StripAlphaIfOpaque_ReturnsIndependentTightCopyWhenAlphaIsNotOpaque()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 10, new byte[] { 1, 2, 3, 255, 4, 5, 6, 128, 99, 99 });

        PngPixelBuffer result = PngAlphaTransformer.StripAlphaIfOpaque(buffer);
        buffer.Data[0] = 77;

        AssertBuffer(result, PngPixelFormat.Rgba8, 2, 1, 8, new byte[] { 1, 2, 3, 255, 4, 5, 6, 128 });
    }

    [Test]
    public void StripAlphaIfOpaque_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => PngAlphaTransformer.StripAlphaIfOpaque(null!));
        Assert.Throws<ArgumentNullException>(() => PngAlphaTransformer.StripAlphaIfOpaque(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, null!)));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.StripAlphaIfOpaque(CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.StripAlphaIfOpaque(CreateBuffer(PngPixelFormat.Rgba8, 0, 1, 4, new byte[] { 0, 0, 0, 255 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.StripAlphaIfOpaque(CreateBuffer(PngPixelFormat.Rgba8, 1, 0, 4, new byte[] { 0, 0, 0, 255 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.StripAlphaIfOpaque(CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 7, new byte[] { 0, 0, 0, 255, 0, 0, 0, 255 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.StripAlphaIfOpaque(CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 8, new byte[] { 0, 0, 0, 255, 0, 0, 0, 255, 0, 0, 0 })));
    }

    [Test]
    public void IsOpaque_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => PngAlphaTransformer.IsOpaque(null!));
        Assert.Throws<ArgumentNullException>(() => PngAlphaTransformer.IsOpaque(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, null!)));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.IsOpaque(CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.IsOpaque(CreateBuffer(PngPixelFormat.Rgba8, 0, 1, 4, new byte[] { 0, 0, 0, 255 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.IsOpaque(CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 7, new byte[] { 0, 0, 0, 255, 0, 0, 0, 255 })));
    }

    [Test]
    public void InvertAlpha_InvertsGrayscaleAlpha8RowsUsingStride()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.GrayscaleAlpha8, 3, 2, 8, new byte[]
        {
            7, 0, 8, 1, 9, 255, 99, 99,
            10, 10, 11, 128, 12, 200, 99, 99
        });

        PngPixelBuffer result = PngAlphaTransformer.InvertAlpha(buffer);

        AssertBuffer(result, PngPixelFormat.GrayscaleAlpha8, 3, 2, 6, new byte[]
        {
            7, 255, 8, 254, 9, 0,
            10, 245, 11, 127, 12, 55
        });
    }

    [Test]
    public void InvertAlpha_InvertsGrayscaleAlpha16()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.GrayscaleAlpha16, 2, 1, 8, new byte[]
        {
            18, 52, 0, 0,
            128, 1, 18, 52
        });

        PngPixelBuffer result = PngAlphaTransformer.InvertAlpha(buffer);

        AssertBuffer(result, PngPixelFormat.GrayscaleAlpha16, 2, 1, 8, new byte[]
        {
            18, 52, 255, 255,
            128, 1, 237, 203
        });
    }

    [Test]
    public void InvertAlpha_InvertsRgba8RowsUsingStride()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 10, new byte[]
        {
            1, 2, 3, 0, 4, 5, 6, 127, 99, 99,
            7, 8, 9, 128, 10, 11, 12, 255, 99, 99
        });

        PngPixelBuffer result = PngAlphaTransformer.InvertAlpha(buffer);

        AssertBuffer(result, PngPixelFormat.Rgba8, 2, 2, 8, new byte[]
        {
            1, 2, 3, 255, 4, 5, 6, 128,
            7, 8, 9, 127, 10, 11, 12, 0
        });
    }

    [Test]
    public void InvertAlpha_InvertsRgba16()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgba16, 2, 1, 16, new byte[]
        {
            18, 52, 69, 103, 137, 171, 0, 1,
            0, 1, 0, 255, 255, 0, 171, 205
        });

        PngPixelBuffer result = PngAlphaTransformer.InvertAlpha(buffer);

        AssertBuffer(result, PngPixelFormat.Rgba16, 2, 1, 16, new byte[]
        {
            18, 52, 69, 103, 137, 171, 255, 254,
            0, 1, 0, 255, 255, 0, 84, 50
        });
    }

    [Test]
    public void InvertAlpha_ReturnsIndependentBuffer()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 1, 2, 3, 4 });

        PngPixelBuffer result = PngAlphaTransformer.InvertAlpha(buffer);
        buffer.Data[3] = 99;

        AssertBuffer(result, PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 1, 2, 3, 251 });
    }

    [Test]
    public void InvertAlpha_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => PngAlphaTransformer.InvertAlpha(null!));
        Assert.Throws<ArgumentNullException>(() => PngAlphaTransformer.InvertAlpha(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, null!)));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.InvertAlpha(CreateBuffer(PngPixelFormat.Indexed8, 1, 1, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.InvertAlpha(CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.InvertAlpha(CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.InvertAlpha(CreateBuffer(PngPixelFormat.Rgba8, 0, 1, 4, new byte[] { 0, 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.InvertAlpha(CreateBuffer(PngPixelFormat.Rgba8, 1, 0, 4, new byte[] { 0, 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.InvertAlpha(CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 7, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.InvertAlpha(CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 8, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 })));
    }

    [Test]
    public void CompositeOverBackground_CompositesGrayscaleAlpha8RowsUsingStride()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.GrayscaleAlpha8, 3, 2, 8, new byte[]
        {
            200, 255, 50, 0, 200, 128, 99, 99,
            0, 255, 255, 0, 120, 64, 99, 99
        });
        PngBackgroundColor background = CreateBackground(100, 0, 0, 0);

        PngPixelBuffer result = PngAlphaTransformer.CompositeOverBackground(buffer, background);

        AssertBuffer(result, PngPixelFormat.Grayscale8, 3, 2, 3, new byte[] { 200, 100, 150, 0, 100, 105 });
    }

    [Test]
    public void CompositeOverBackground_CompositesRgba8AndRemovesAlpha()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgba8, 3, 1, 12, new byte[]
        {
            100, 120, 140, 128,
            1, 2, 3, 0,
            200, 210, 220, 255
        });
        PngBackgroundColor background = CreateBackground(0, 10, 20, 30);

        PngPixelBuffer result = PngAlphaTransformer.CompositeOverBackground(buffer, background);

        AssertBuffer(result, PngPixelFormat.Rgb8, 3, 1, 9, new byte[] { 55, 70, 85, 10, 20, 30, 200, 210, 220 });
    }

    [Test]
    public void CompositeOverBackground_CompositesGrayscaleAlpha16()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.GrayscaleAlpha16, 2, 1, 8, CreateBigEndianSamples(0x8000, 0xFFFF, 0x1234, 0x0000));
        PngBackgroundColor background = CreateBackground(0x4000, 0, 0, 0);

        PngPixelBuffer result = PngAlphaTransformer.CompositeOverBackground(buffer, background);

        AssertBuffer(result, PngPixelFormat.Grayscale16, 2, 1, 4, CreateBigEndianSamples(0x8000, 0x4000));
    }

    [Test]
    public void CompositeOverBackground_CompositesRgba16()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgba16, 2, 1, 16, CreateBigEndianSamples(0x8000, 0x9000, 0xA000, 0xFFFF, 0x1111, 0x2222, 0x3333, 0x0000));
        PngBackgroundColor background = CreateBackground(0, 0x1000, 0x2000, 0x3000);

        PngPixelBuffer result = PngAlphaTransformer.CompositeOverBackground(buffer, background);

        AssertBuffer(result, PngPixelFormat.Rgb16, 2, 1, 12, CreateBigEndianSamples(0x8000, 0x9000, 0xA000, 0x1000, 0x2000, 0x3000));
    }

    [Test]
    public void CompositeOverBackground_ReturnsIndependentBuffer()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 1, 2, 3, 255 });
        PngBackgroundColor background = CreateBackground(0, 10, 20, 30);

        PngPixelBuffer result = PngAlphaTransformer.CompositeOverBackground(buffer, background);
        buffer.Data[0] = 99;

        AssertBuffer(result, PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 1, 2, 3 });
    }

    [Test]
    public void CompositeOverBackground_RejectsInvalidInputs()
    {
        PngBackgroundColor valid = CreateBackground(10, 10, 20, 30);
        PngBackgroundColor grayTooLarge = CreateBackground(256, 0, 0, 0);
        PngBackgroundColor redTooLarge = CreateBackground(0, 256, 0, 0);

        Assert.Throws<ArgumentNullException>(() => PngAlphaTransformer.CompositeOverBackground(null!, valid));
        Assert.Throws<ArgumentNullException>(() => PngAlphaTransformer.CompositeOverBackground(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, null!), valid));
        Assert.Throws<ArgumentNullException>(() => PngAlphaTransformer.CompositeOverBackground(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 0, 0, 0, 0 }), null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.CompositeOverBackground(CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 0, 0, 0 }), valid));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.CompositeOverBackground(CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 0 }), valid));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.CompositeOverBackground(CreateBuffer(PngPixelFormat.Indexed8, 1, 1, 1, new byte[] { 0 }), valid));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.CompositeOverBackground(CreateBuffer(PngPixelFormat.Rgba8, 0, 1, 4, new byte[] { 0, 0, 0, 0 }), valid));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.CompositeOverBackground(CreateBuffer(PngPixelFormat.Rgba8, 1, 0, 4, new byte[] { 0, 0, 0, 0 }), valid));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.CompositeOverBackground(CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 7, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }), valid));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.CompositeOverBackground(CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 8, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }), valid));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.CompositeOverBackground(CreateBuffer(PngPixelFormat.GrayscaleAlpha8, 1, 1, 2, new byte[] { 0, 0 }), grayTooLarge));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.CompositeOverBackground(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 0, 0, 0, 0 }), redTooLarge));
    }

    [Test]
    public void PremultiplyAlpha_PremultipliesGrayscaleAlpha8RowsUsingStride()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.GrayscaleAlpha8, 3, 2, 8, new byte[]
        {
            200, 255, 50, 0, 200, 128, 99, 99,
            120, 64, 255, 128, 1, 128, 99, 99
        });

        PngPixelBuffer result = PngAlphaTransformer.PremultiplyAlpha(buffer);

        AssertBuffer(result, PngPixelFormat.GrayscaleAlpha8, 3, 2, 6, new byte[]
        {
            200, 255, 0, 0, 100, 128,
            30, 64, 128, 128, 1, 128
        });
    }

    [Test]
    public void PremultiplyAlpha_PremultipliesRgba8()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgba8, 3, 1, 12, new byte[]
        {
            100, 120, 140, 128,
            1, 2, 3, 0,
            200, 210, 220, 255
        });

        PngPixelBuffer result = PngAlphaTransformer.PremultiplyAlpha(buffer);

        AssertBuffer(result, PngPixelFormat.Rgba8, 3, 1, 12, new byte[]
        {
            50, 60, 70, 128,
            0, 0, 0, 0,
            200, 210, 220, 255
        });
    }

    [Test]
    public void PremultiplyAlpha_PremultipliesGrayscaleAlpha16()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.GrayscaleAlpha16, 3, 1, 12, CreateBigEndianSamples(0x8000, 0xFFFF, 0x1234, 0x0000, 0xFFFF, 0x8000));

        PngPixelBuffer result = PngAlphaTransformer.PremultiplyAlpha(buffer);

        AssertBuffer(result, PngPixelFormat.GrayscaleAlpha16, 3, 1, 12, CreateBigEndianSamples(0x8000, 0xFFFF, 0x0000, 0x0000, 0x8000, 0x8000));
    }

    [Test]
    public void PremultiplyAlpha_PremultipliesRgba16()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgba16, 2, 1, 16, CreateBigEndianSamples(0xFFFF, 0x8000, 0x4000, 0x8000, 0x1234, 0x5678, 0x9ABC, 0x0000));

        PngPixelBuffer result = PngAlphaTransformer.PremultiplyAlpha(buffer);

        AssertBuffer(result, PngPixelFormat.Rgba16, 2, 1, 16, CreateBigEndianSamples(0x8000, 0x4000, 0x2000, 0x8000, 0x0000, 0x0000, 0x0000, 0x0000));
    }

    [Test]
    public void PremultiplyAlpha_ReturnsIndependentBuffer()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 100, 120, 140, 128 });

        PngPixelBuffer result = PngAlphaTransformer.PremultiplyAlpha(buffer);
        buffer.Data[0] = 99;

        AssertBuffer(result, PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 50, 60, 70, 128 });
    }

    [Test]
    public void PremultiplyAlpha_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => PngAlphaTransformer.PremultiplyAlpha(null!));
        Assert.Throws<ArgumentNullException>(() => PngAlphaTransformer.PremultiplyAlpha(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, null!)));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.PremultiplyAlpha(CreateBuffer(PngPixelFormat.Indexed8, 1, 1, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.PremultiplyAlpha(CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.PremultiplyAlpha(CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.PremultiplyAlpha(CreateBuffer(PngPixelFormat.Rgba8, 0, 1, 4, new byte[] { 0, 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.PremultiplyAlpha(CreateBuffer(PngPixelFormat.Rgba8, 1, 0, 4, new byte[] { 0, 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.PremultiplyAlpha(CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 7, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.PremultiplyAlpha(CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 8, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 })));
    }

    [Test]
    public void UnpremultiplyAlpha_UnpremultipliesGrayscaleAlpha8RowsUsingStride()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.GrayscaleAlpha8, 3, 2, 8, new byte[]
        {
            200, 255, 0, 0, 50, 128, 99, 99,
            30, 64, 128, 128, 1, 128, 99, 99
        });

        PngPixelBuffer result = PngAlphaTransformer.UnpremultiplyAlpha(buffer);

        AssertBuffer(result, PngPixelFormat.GrayscaleAlpha8, 3, 2, 6, new byte[]
        {
            200, 255, 0, 0, 100, 128,
            120, 64, 255, 128, 2, 128
        });
    }

    [Test]
    public void UnpremultiplyAlpha_UnpremultipliesRgba8()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgba8, 3, 1, 12, new byte[]
        {
            50, 60, 64, 128,
            1, 2, 3, 0,
            200, 210, 220, 255
        });

        PngPixelBuffer result = PngAlphaTransformer.UnpremultiplyAlpha(buffer);

        AssertBuffer(result, PngPixelFormat.Rgba8, 3, 1, 12, new byte[]
        {
            100, 120, 128, 128,
            0, 0, 0, 0,
            200, 210, 220, 255
        });
    }

    [Test]
    public void UnpremultiplyAlpha_UnpremultipliesGrayscaleAlpha16()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.GrayscaleAlpha16, 3, 1, 12, CreateBigEndianSamples(0x8000, 0xFFFF, 0x0000, 0x0000, 0x8000, 0x8000));

        PngPixelBuffer result = PngAlphaTransformer.UnpremultiplyAlpha(buffer);

        AssertBuffer(result, PngPixelFormat.GrayscaleAlpha16, 3, 1, 12, CreateBigEndianSamples(0x8000, 0xFFFF, 0x0000, 0x0000, 0xFFFF, 0x8000));
    }

    [Test]
    public void UnpremultiplyAlpha_UnpremultipliesRgba16()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgba16, 2, 1, 16, CreateBigEndianSamples(0x8000, 0x4000, 0x2000, 0x8000, 0x1234, 0x5678, 0x9ABC, 0x0000));

        PngPixelBuffer result = PngAlphaTransformer.UnpremultiplyAlpha(buffer);

        AssertBuffer(result, PngPixelFormat.Rgba16, 2, 1, 16, CreateBigEndianSamples(0xFFFF, 0x8000, 0x4000, 0x8000, 0x0000, 0x0000, 0x0000, 0x0000));
    }

    [Test]
    public void UnpremultiplyAlpha_ReturnsIndependentBuffer()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 50, 60, 64, 128 });

        PngPixelBuffer result = PngAlphaTransformer.UnpremultiplyAlpha(buffer);
        buffer.Data[0] = 99;

        AssertBuffer(result, PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 100, 120, 128, 128 });
    }

    [Test]
    public void UnpremultiplyAlpha_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => PngAlphaTransformer.UnpremultiplyAlpha(null!));
        Assert.Throws<ArgumentNullException>(() => PngAlphaTransformer.UnpremultiplyAlpha(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, null!)));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.UnpremultiplyAlpha(CreateBuffer(PngPixelFormat.Indexed8, 1, 1, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.UnpremultiplyAlpha(CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.UnpremultiplyAlpha(CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.UnpremultiplyAlpha(CreateBuffer(PngPixelFormat.Rgba8, 0, 1, 4, new byte[] { 0, 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.UnpremultiplyAlpha(CreateBuffer(PngPixelFormat.Rgba8, 1, 0, 4, new byte[] { 0, 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.UnpremultiplyAlpha(CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 7, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.UnpremultiplyAlpha(CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 8, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 })));
    }

    [Test]
    public void ClearTransparentPixelColors_ClearsGrayscaleAlpha8RowsUsingStride()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.GrayscaleAlpha8, 3, 2, 8, new byte[]
        {
            7, 0, 8, 1, 9, 255, 99, 99,
            10, 0, 11, 128, 12, 0, 99, 99
        });

        PngPixelBuffer result = PngAlphaTransformer.ClearTransparentPixelColors(buffer);

        AssertBuffer(result, PngPixelFormat.GrayscaleAlpha8, 3, 2, 6, new byte[]
        {
            0, 0, 8, 1, 9, 255,
            0, 0, 11, 128, 0, 0
        });
    }

    [Test]
    public void ClearTransparentPixelColors_ClearsRgba8RowsUsingStride()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 10, new byte[]
        {
            1, 2, 3, 0, 4, 5, 6, 1, 99, 99,
            7, 8, 9, 255, 10, 11, 12, 0, 99, 99
        });

        PngPixelBuffer result = PngAlphaTransformer.ClearTransparentPixelColors(buffer);

        AssertBuffer(result, PngPixelFormat.Rgba8, 2, 2, 8, new byte[]
        {
            0, 0, 0, 0, 4, 5, 6, 1,
            7, 8, 9, 255, 0, 0, 0, 0
        });
    }

    [Test]
    public void ClearTransparentPixelColors_ClearsGrayscaleAlpha16()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.GrayscaleAlpha16, 3, 1, 12, CreateBigEndianSamples(0x1234, 0x0000, 0x8001, 0x0001, 0xFFFF, 0xFFFF));

        PngPixelBuffer result = PngAlphaTransformer.ClearTransparentPixelColors(buffer);

        AssertBuffer(result, PngPixelFormat.GrayscaleAlpha16, 3, 1, 12, CreateBigEndianSamples(0x0000, 0x0000, 0x8001, 0x0001, 0xFFFF, 0xFFFF));
    }

    [Test]
    public void ClearTransparentPixelColors_ClearsRgba16()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgba16, 2, 1, 16, CreateBigEndianSamples(0x1234, 0x4567, 0x89AB, 0x0000, 0x1111, 0x2222, 0x3333, 0x0001));

        PngPixelBuffer result = PngAlphaTransformer.ClearTransparentPixelColors(buffer);

        AssertBuffer(result, PngPixelFormat.Rgba16, 2, 1, 16, CreateBigEndianSamples(0x0000, 0x0000, 0x0000, 0x0000, 0x1111, 0x2222, 0x3333, 0x0001));
    }

    [Test]
    public void ClearTransparentPixelColors_ReturnsIndependentBuffer()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 1, 2, 3, 0 });

        PngPixelBuffer result = PngAlphaTransformer.ClearTransparentPixelColors(buffer);
        buffer.Data[0] = 99;

        AssertBuffer(result, PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 0, 0, 0, 0 });
    }

    [Test]
    public void ClearTransparentPixelColors_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => PngAlphaTransformer.ClearTransparentPixelColors(null!));
        Assert.Throws<ArgumentNullException>(() => PngAlphaTransformer.ClearTransparentPixelColors(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, null!)));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.ClearTransparentPixelColors(CreateBuffer(PngPixelFormat.Indexed8, 1, 1, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.ClearTransparentPixelColors(CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.ClearTransparentPixelColors(CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.ClearTransparentPixelColors(CreateBuffer(PngPixelFormat.Rgba8, 0, 1, 4, new byte[] { 0, 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.ClearTransparentPixelColors(CreateBuffer(PngPixelFormat.Rgba8, 1, 0, 4, new byte[] { 0, 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.ClearTransparentPixelColors(CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 7, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAlphaTransformer.ClearTransparentPixelColors(CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 8, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 })));
    }

    static void AssertBuffer(PngPixelBuffer result, PngPixelFormat format, int width, int height, int stride, byte[] data)
    {
        Assert.That(result.Format, Is.EqualTo(format));
        Assert.That(result.Width, Is.EqualTo(width));
        Assert.That(result.Height, Is.EqualTo(height));
        Assert.That(result.Stride, Is.EqualTo(stride));
        Assert.That(result.Data, Is.EqualTo(data));
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

    static PngBackgroundColor CreateBackground(int gray, int red, int green, int blue)
    {
        PngBackgroundColor background = new PngBackgroundColor();
        background.Gray = (ushort)gray;
        background.Red = (ushort)red;
        background.Green = (ushort)green;
        background.Blue = (ushort)blue;
        return background;
    }

    static byte[] CreateBigEndianSamples(params int[] samples)
    {
        byte[] data = new byte[samples.Length * 2];
        int index = 0;
        int offset = 0;
        while (index < samples.Length)
        {
            PngBinaryWriter.WriteUInt16(data, offset, (ushort)samples[index]);
            offset += 2;
            index += 1;
        }

        return data;
    }
}
