using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class GeometryTransformerTests
{
    [Test]
    public void FlipVertical_FlipsRgba8RowsAndRemovesStridePadding()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgba8, 2, 3, 10, new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8, 90, 91,
            9, 10, 11, 12, 13, 14, 15, 16, 92, 93,
            17, 18, 19, 20, 21, 22, 23, 24, 94, 95
        });

        PngPixelBuffer result = PngGeometryTransformer.FlipVertical(buffer);

        AssertBuffer(result, PngPixelFormat.Rgba8, 2, 3, 8, new byte[]
        {
            17, 18, 19, 20, 21, 22, 23, 24,
            9, 10, 11, 12, 13, 14, 15, 16,
            1, 2, 3, 4, 5, 6, 7, 8
        });
    }

    [Test]
    public void FlipVertical_FlipsPackedIndexedRows()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Indexed1, 10, 3, 3, new byte[]
        {
            0b10101010, 0b11000000, 90,
            0b01010101, 0b00000000, 91,
            0b11110000, 0b10000000, 92
        });

        PngPixelBuffer result = PngGeometryTransformer.FlipVertical(buffer);

        AssertBuffer(result, PngPixelFormat.Indexed1, 10, 3, 2, new byte[]
        {
            0b11110000, 0b10000000,
            0b01010101, 0b00000000,
            0b10101010, 0b11000000
        });
    }

    [Test]
    public void FlipVertical_FlipsRgb16Rows()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgb16, 1, 2, 8, new byte[]
        {
            0x12, 0x34, 0x45, 0x67, 0x89, 0xAB, 90, 91,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 92, 93
        });

        PngPixelBuffer result = PngGeometryTransformer.FlipVertical(buffer);

        AssertBuffer(result, PngPixelFormat.Rgb16, 1, 2, 6, new byte[]
        {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06,
            0x12, 0x34, 0x45, 0x67, 0x89, 0xAB
        });
    }

    [Test]
    public void FlipVertical_ReturnsIndependentSingleRowBuffer()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Grayscale8, 3, 1, 5, new byte[] { 1, 2, 3, 90, 91 });

        PngPixelBuffer result = PngGeometryTransformer.FlipVertical(buffer);
        buffer.Data[0] = 99;

        AssertBuffer(result, PngPixelFormat.Grayscale8, 3, 1, 3, new byte[] { 1, 2, 3 });
    }

    [Test]
    public void FlipVertical_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => PngGeometryTransformer.FlipVertical(null!));
        Assert.Throws<ArgumentNullException>(() => PngGeometryTransformer.FlipVertical(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, null!)));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.FlipVertical(CreateBuffer(PngPixelFormat.Unknown, 1, 1, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.FlipVertical(CreateBuffer(PngPixelFormat.Rgba8, 0, 1, 4, new byte[] { 0, 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.FlipVertical(CreateBuffer(PngPixelFormat.Rgba8, 1, 0, 4, new byte[] { 0, 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.FlipVertical(CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 7, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.FlipVertical(CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 8, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 })));
    }

    [Test]
    public void Crop_CropsRgba8RectangleAndRemovesStridePadding()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgba8, 4, 3, 18, new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 90, 91,
            21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 92, 93,
            41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 94, 95
        });

        PngPixelBuffer result = PngGeometryTransformer.Crop(buffer, 1, 1, 2, 2);

        AssertBuffer(result, PngPixelFormat.Rgba8, 2, 2, 8, new byte[]
        {
            25, 26, 27, 28, 29, 30, 31, 32,
            45, 46, 47, 48, 49, 50, 51, 52
        });
    }

    [Test]
    public void Crop_CropsPackedIndexedRectangle()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Indexed2, 5, 3, 3, new byte[]
        {
            0b00011011, 0b00000000, 90,
            0b11100100, 0b11000000, 91,
            0b01011010, 0b11000000, 92
        });

        PngPixelBuffer result = PngGeometryTransformer.Crop(buffer, 1, 1, 3, 2);

        AssertBuffer(result, PngPixelFormat.Indexed2, 3, 2, 1, new byte[]
        {
            0b10010000,
            0b01101000
        });
    }

    [Test]
    public void Crop_CropsRgb16Rectangle()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgb16, 2, 2, 14, new byte[]
        {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 90, 91,
            0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 92, 93
        });

        PngPixelBuffer result = PngGeometryTransformer.Crop(buffer, 1, 0, 1, 2);

        AssertBuffer(result, PngPixelFormat.Rgb16, 1, 2, 6, new byte[]
        {
            0x11, 0x12, 0x13, 0x14, 0x15, 0x16,
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36
        });
    }

    [Test]
    public void Crop_ReturnsIndependentTightCopyForFullImage()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Grayscale8, 3, 2, 5, new byte[]
        {
            1, 2, 3, 90, 91,
            4, 5, 6, 92, 93
        });

        PngPixelBuffer result = PngGeometryTransformer.Crop(buffer, 0, 0, 3, 2);
        buffer.Data[0] = 99;

        AssertBuffer(result, PngPixelFormat.Grayscale8, 3, 2, 3, new byte[] { 1, 2, 3, 4, 5, 6 });
    }

    [Test]
    public void Crop_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => PngGeometryTransformer.Crop(null!, 0, 0, 1, 1));
        Assert.Throws<ArgumentNullException>(() => PngGeometryTransformer.Crop(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, null!), 0, 0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.Crop(CreateBuffer(PngPixelFormat.Unknown, 1, 1, 1, new byte[] { 0 }), 0, 0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.Crop(CreateBuffer(PngPixelFormat.Rgba8, 0, 1, 4, new byte[] { 0, 0, 0, 0 }), 0, 0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.Crop(CreateBuffer(PngPixelFormat.Rgba8, 1, 0, 4, new byte[] { 0, 0, 0, 0 }), 0, 0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.Crop(CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 7, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }), 0, 0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.Crop(CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 8, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }), 0, 0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.Crop(CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 8, new byte[16]), -1, 0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.Crop(CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 8, new byte[16]), 0, -1, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.Crop(CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 8, new byte[16]), 0, 0, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.Crop(CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 8, new byte[16]), 0, 0, 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.Crop(CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 8, new byte[16]), 1, 0, 2, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.Crop(CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 8, new byte[16]), 0, 1, 1, 2));
    }

    [Test]
    public void PadCanvas_PadsRgba8RowsAndRemovesStridePadding()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 10, new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8, 90, 91,
            9, 10, 11, 12, 13, 14, 15, 16, 92, 93
        });

        PngPixelBuffer result = PngGeometryTransformer.PadCanvas(buffer, 1, 1, 1, 1);

        AssertBuffer(result, PngPixelFormat.Rgba8, 4, 4, 16, new byte[]
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 0, 0, 0, 0,
            0, 0, 0, 0, 9, 10, 11, 12, 13, 14, 15, 16, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
        });
    }

    [Test]
    public void PadCanvas_PadsPackedIndexedRows()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Indexed2, 3, 2, 2, new byte[]
        {
            0b01101100, 90,
            0b11000100, 91
        });

        PngPixelBuffer result = PngGeometryTransformer.PadCanvas(buffer, 1, 1, 2, 0);

        AssertBuffer(result, PngPixelFormat.Indexed2, 6, 3, 2, new byte[]
        {
            0b00000000, 0b00000000,
            0b00011011, 0b00000000,
            0b00110001, 0b00000000
        });
    }

    [Test]
    public void PadCanvas_ReturnsIndependentTightCopyWithZeroPadding()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Grayscale8, 3, 1, 5, new byte[] { 1, 2, 3, 90, 91 });

        PngPixelBuffer result = PngGeometryTransformer.PadCanvas(buffer, 0, 0, 0, 0);
        buffer.Data[0] = 99;

        AssertBuffer(result, PngPixelFormat.Grayscale8, 3, 1, 3, new byte[] { 1, 2, 3 });
    }

    [Test]
    public void PadCanvas_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => PngGeometryTransformer.PadCanvas(null!, 0, 0, 0, 0));
        Assert.Throws<ArgumentNullException>(() => PngGeometryTransformer.PadCanvas(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, null!), 0, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.PadCanvas(CreateBuffer(PngPixelFormat.Unknown, 1, 1, 1, new byte[] { 0 }), 0, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.PadCanvas(CreateBuffer(PngPixelFormat.Rgba8, 0, 1, 4, new byte[] { 0, 0, 0, 0 }), 0, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.PadCanvas(CreateBuffer(PngPixelFormat.Rgba8, 1, 0, 4, new byte[] { 0, 0, 0, 0 }), 0, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.PadCanvas(CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 7, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }), 0, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.PadCanvas(CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 8, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }), 0, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.PadCanvas(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[4]), -1, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.PadCanvas(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[4]), 0, -1, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.PadCanvas(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[4]), 0, 0, -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.PadCanvas(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[4]), 0, 0, 0, -1));
        Assert.Throws<OverflowException>(() => PngGeometryTransformer.PadCanvas(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[4]), int.MaxValue, 0, 0, 0));
    }

    [Test]
    public void CopyRectangle_CopiesRgba8RectangleBetweenPaddedBuffers()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Rgba8, 4, 3, 18, new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 90, 91,
            21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 92, 93,
            41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 94, 95
        });
        PngPixelBuffer destination = CreateBuffer(PngPixelFormat.Rgba8, 4, 3, 18, new byte[]
        {
            100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 200, 201,
            101, 101, 101, 101, 101, 101, 101, 101, 101, 101, 101, 101, 101, 101, 101, 101, 202, 203,
            102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 204, 205
        });

        PngGeometryTransformer.CopyRectangle(source, 1, 1, destination, 1, 0, 2, 2);

        Assert.That(destination.Data, Is.EqualTo(new byte[]
        {
            100, 100, 100, 100, 25, 26, 27, 28, 29, 30, 31, 32, 100, 100, 100, 100, 200, 201,
            101, 101, 101, 101, 45, 46, 47, 48, 49, 50, 51, 52, 101, 101, 101, 101, 202, 203,
            102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 204, 205
        }));
    }

    [Test]
    public void CopyRectangle_OverwritesPackedIndexedSamples()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Indexed2, 5, 3, 3, new byte[]
        {
            0b00011011, 0b00000000, 90,
            0b11100100, 0b11000000, 91,
            0b01011010, 0b11000000, 92
        });
        PngPixelBuffer destination = CreateBuffer(PngPixelFormat.Indexed2, 6, 3, 2, new byte[]
        {
            0xFF, 0xFF,
            0xFF, 0xFF,
            0xFF, 0xFF
        });

        PngGeometryTransformer.CopyRectangle(source, 1, 1, destination, 2, 0, 3, 2);

        Assert.That(destination.Data, Is.EqualTo(new byte[]
        {
            0xF9, 0x3F,
            0xF6, 0xBF,
            0xFF, 0xFF
        }));
    }

    [Test]
    public void CopyRectangle_HandlesSameBufferOverlap()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Grayscale8, 4, 3, 4, new byte[]
        {
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12
        });

        PngGeometryTransformer.CopyRectangle(buffer, 0, 0, buffer, 1, 1, 3, 2);

        Assert.That(buffer.Data, Is.EqualTo(new byte[]
        {
            1, 2, 3, 4,
            5, 1, 2, 3,
            9, 5, 6, 7
        }));
    }

    [Test]
    public void CopyRectangle_RejectsInvalidInputs()
    {
        PngPixelBuffer validSource = CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 8, new byte[16]);
        PngPixelBuffer validDestination = CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 8, new byte[16]);

        Assert.Throws<ArgumentNullException>(() => PngGeometryTransformer.CopyRectangle(null!, 0, 0, validDestination, 0, 0, 1, 1));
        Assert.Throws<ArgumentNullException>(() => PngGeometryTransformer.CopyRectangle(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, null!), 0, 0, validDestination, 0, 0, 1, 1));
        Assert.Throws<ArgumentNullException>(() => PngGeometryTransformer.CopyRectangle(validSource, 0, 0, null!, 0, 0, 1, 1));
        Assert.Throws<ArgumentNullException>(() => PngGeometryTransformer.CopyRectangle(validSource, 0, 0, CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, null!), 0, 0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.CopyRectangle(CreateBuffer(PngPixelFormat.Unknown, 1, 1, 1, new byte[] { 0 }), 0, 0, validDestination, 0, 0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.CopyRectangle(validSource, 0, 0, CreateBuffer(PngPixelFormat.Rgb8, 2, 2, 6, new byte[12]), 0, 0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.CopyRectangle(CreateBuffer(PngPixelFormat.Rgba8, 0, 1, 4, new byte[4]), 0, 0, validDestination, 0, 0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.CopyRectangle(CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 7, new byte[8]), 0, 0, validDestination, 0, 0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.CopyRectangle(CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 8, new byte[11]), 0, 0, validDestination, 0, 0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.CopyRectangle(validSource, -1, 0, validDestination, 0, 0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.CopyRectangle(validSource, 0, -1, validDestination, 0, 0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.CopyRectangle(validSource, 0, 0, validDestination, -1, 0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.CopyRectangle(validSource, 0, 0, validDestination, 0, -1, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.CopyRectangle(validSource, 0, 0, validDestination, 0, 0, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.CopyRectangle(validSource, 0, 0, validDestination, 0, 0, 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.CopyRectangle(validSource, 1, 0, validDestination, 0, 0, 2, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.CopyRectangle(validSource, 0, 1, validDestination, 0, 0, 1, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.CopyRectangle(validSource, 0, 0, validDestination, 1, 0, 2, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.CopyRectangle(validSource, 0, 0, validDestination, 0, 1, 1, 2));
    }

    [Test]
    public void ResizeNearestNeighbor_UpscalesRgba8RowsUsingStride()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 10, new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8, 90, 91,
            9, 10, 11, 12, 13, 14, 15, 16, 92, 93
        });

        PngPixelBuffer result = PngGeometryTransformer.ResizeNearestNeighbor(buffer, 4, 4);

        AssertBuffer(result, PngPixelFormat.Rgba8, 4, 4, 16, new byte[]
        {
            1, 2, 3, 4, 1, 2, 3, 4, 5, 6, 7, 8, 5, 6, 7, 8,
            1, 2, 3, 4, 1, 2, 3, 4, 5, 6, 7, 8, 5, 6, 7, 8,
            9, 10, 11, 12, 9, 10, 11, 12, 13, 14, 15, 16, 13, 14, 15, 16,
            9, 10, 11, 12, 9, 10, 11, 12, 13, 14, 15, 16, 13, 14, 15, 16
        });
    }

    [Test]
    public void ResizeNearestNeighbor_DownscalesGrayscale8()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Grayscale8, 4, 4, 5, new byte[]
        {
            1, 2, 3, 4, 90,
            5, 6, 7, 8, 91,
            9, 10, 11, 12, 92,
            13, 14, 15, 16, 93
        });

        PngPixelBuffer result = PngGeometryTransformer.ResizeNearestNeighbor(buffer, 2, 2);

        AssertBuffer(result, PngPixelFormat.Grayscale8, 2, 2, 2, new byte[] { 1, 3, 9, 11 });
    }

    [Test]
    public void ResizeNearestNeighbor_ResizesPackedIndexedRows()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Indexed2, 2, 2, 1, new byte[]
        {
            0b01100000,
            0b11000000
        });

        PngPixelBuffer result = PngGeometryTransformer.ResizeNearestNeighbor(buffer, 4, 3);

        AssertBuffer(result, PngPixelFormat.Indexed2, 4, 3, 1, new byte[]
        {
            0b01011010,
            0b01011010,
            0b11110000
        });
    }

    [Test]
    public void ResizeNearestNeighbor_ReturnsIndependentTightCopyForSameSize()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Grayscale8, 3, 1, 5, new byte[] { 1, 2, 3, 90, 91 });

        PngPixelBuffer result = PngGeometryTransformer.ResizeNearestNeighbor(buffer, 3, 1);
        buffer.Data[0] = 99;

        AssertBuffer(result, PngPixelFormat.Grayscale8, 3, 1, 3, new byte[] { 1, 2, 3 });
    }

    [Test]
    public void ResizeNearestNeighbor_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => PngGeometryTransformer.ResizeNearestNeighbor(null!, 1, 1));
        Assert.Throws<ArgumentNullException>(() => PngGeometryTransformer.ResizeNearestNeighbor(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, null!), 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.ResizeNearestNeighbor(CreateBuffer(PngPixelFormat.Unknown, 1, 1, 1, new byte[] { 0 }), 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.ResizeNearestNeighbor(CreateBuffer(PngPixelFormat.Rgba8, 0, 1, 4, new byte[] { 0, 0, 0, 0 }), 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.ResizeNearestNeighbor(CreateBuffer(PngPixelFormat.Rgba8, 1, 0, 4, new byte[] { 0, 0, 0, 0 }), 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.ResizeNearestNeighbor(CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 7, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }), 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.ResizeNearestNeighbor(CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 8, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }), 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.ResizeNearestNeighbor(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[4]), 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.ResizeNearestNeighbor(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[4]), 1, 0));
    }

    [Test]
    public void ResizeBilinear_InterpolatesRgba8Channels()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 10, new byte[]
        {
            0, 10, 20, 30, 100, 110, 120, 130, 90, 91,
            150, 160, 170, 180, 250, 240, 230, 220, 92, 93
        });

        PngPixelBuffer result = PngGeometryTransformer.ResizeBilinear(buffer, 3, 3);

        AssertBuffer(result, PngPixelFormat.Rgba8, 3, 3, 12, new byte[]
        {
            0, 10, 20, 30, 50, 60, 70, 80, 100, 110, 120, 130,
            75, 85, 95, 105, 125, 130, 135, 140, 175, 175, 175, 175,
            150, 160, 170, 180, 200, 200, 200, 200, 250, 240, 230, 220
        });
    }

    [Test]
    public void ResizeBilinear_InterpolatesGrayscale16()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Grayscale16, 2, 2, 6, new byte[]
        {
            0, 0, 3, 232, 90, 91,
            7, 208, 11, 184, 92, 93
        });

        PngPixelBuffer result = PngGeometryTransformer.ResizeBilinear(buffer, 3, 3);

        AssertBuffer(result, PngPixelFormat.Grayscale16, 3, 3, 6, CreateBigEndianSamples(
            0, 500, 1000,
            1000, 1500, 2000,
            2000, 2500, 3000));
    }

    [Test]
    public void ResizeBilinear_ReturnsIndependentTightCopyForSameSize()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Grayscale8, 3, 1, 5, new byte[] { 1, 2, 3, 90, 91 });

        PngPixelBuffer result = PngGeometryTransformer.ResizeBilinear(buffer, 3, 1);
        buffer.Data[0] = 99;

        AssertBuffer(result, PngPixelFormat.Grayscale8, 3, 1, 3, new byte[] { 1, 2, 3 });
    }

    [Test]
    public void ResizeBilinear_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => PngGeometryTransformer.ResizeBilinear(null!, 1, 1));
        Assert.Throws<ArgumentNullException>(() => PngGeometryTransformer.ResizeBilinear(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, null!), 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.ResizeBilinear(CreateBuffer(PngPixelFormat.Unknown, 1, 1, 1, new byte[] { 0 }), 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.ResizeBilinear(CreateBuffer(PngPixelFormat.Indexed8, 1, 1, 1, new byte[] { 0 }), 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.ResizeBilinear(CreateBuffer(PngPixelFormat.Rgba8, 0, 1, 4, new byte[] { 0, 0, 0, 0 }), 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.ResizeBilinear(CreateBuffer(PngPixelFormat.Rgba8, 1, 0, 4, new byte[] { 0, 0, 0, 0 }), 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.ResizeBilinear(CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 7, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }), 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.ResizeBilinear(CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 8, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }), 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.ResizeBilinear(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[4]), 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGeometryTransformer.ResizeBilinear(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[4]), 1, 0));
    }

    static void AssertBuffer(PngPixelBuffer result, PngPixelFormat format, int width, int height, int stride, byte[] data)
    {
        Assert.That(result.Format, Is.EqualTo(format));
        Assert.That(result.Width, Is.EqualTo(width));
        Assert.That(result.Height, Is.EqualTo(height));
        Assert.That(result.Stride, Is.EqualTo(stride));
        Assert.That(result.Data, Is.EqualTo(data));
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
}
