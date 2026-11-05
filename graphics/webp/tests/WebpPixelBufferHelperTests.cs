using webp;

namespace webp.Tests;

[TestFixture]
public class WebpPixelBufferHelperTests
{
    [Test]
    public void AllocateRgba_CreatesCompactBuffer()
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(3, 2);

        Assert.That(buffer.Width, Is.EqualTo(3));
        Assert.That(buffer.Height, Is.EqualTo(2));
        Assert.That(buffer.Stride, Is.EqualTo(12));
        Assert.That(buffer.Rgba, Has.Length.EqualTo(24));
        Assert.That(WebpPixelBufferHelper.ComputeMinimumRgbaStride(3), Is.EqualTo(12));
        Assert.That(WebpPixelBufferHelper.ComputeRequiredRgbaLength(3, 2, 12), Is.EqualTo(24));
    }

    [Test]
    public void AllocateRgba_CreatesPaddedBuffer()
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(2, 3, 12);

        Assert.That(buffer.Width, Is.EqualTo(2));
        Assert.That(buffer.Height, Is.EqualTo(3));
        Assert.That(buffer.Stride, Is.EqualTo(12));
        Assert.That(buffer.Rgba, Has.Length.EqualTo(36));
    }

    [Test]
    public void ValidateRgbaBuffer_AcceptsSufficientStrideAndLength()
    {
        WebpPixelBuffer buffer = new WebpPixelBuffer();
        buffer.Width = 2;
        buffer.Height = 2;
        buffer.Stride = 12;
        buffer.Rgba = new byte[24];

        Assert.DoesNotThrow(() => WebpPixelBufferHelper.ValidateRgbaBuffer(buffer));
        Assert.DoesNotThrow(() => WebpPixelBufferHelper.ValidateRgbaBuffer(buffer.Rgba, 2, 2, 12));
    }

    [Test]
    public void ValidateRgbaBuffer_RejectsInvalidShapeAndShortBuffers()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpPixelBufferHelper.AllocateRgba(0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpPixelBufferHelper.AllocateRgba(1, 0));
        Assert.Throws<ArgumentException>(() => WebpPixelBufferHelper.AllocateRgba(2, 2, 7));
        Assert.Throws<ArgumentException>(() => WebpPixelBufferHelper.ValidateRgbaBuffer(new byte[23], 2, 2, 12));
    }

    [Test]
    public void Copy_CreatesDeepCopyOfPixelBuffer()
    {
        WebpPixelBuffer source = WebpPixelBufferHelper.AllocateRgba(2, 1);
        source.Rgba[0] = 1;
        source.Rgba[1] = 2;
        source.Rgba[2] = 3;
        source.Rgba[3] = 4;

        WebpPixelBuffer copy = WebpPixelBufferHelper.Copy(source);
        source.Rgba[0] = 99;

        Assert.That(copy.Width, Is.EqualTo(2));
        Assert.That(copy.Height, Is.EqualTo(1));
        Assert.That(copy.Stride, Is.EqualTo(8));
        Assert.That(copy.Rgba[0], Is.EqualTo(1));
        Assert.That(copy.Rgba, Is.Not.SameAs(source.Rgba));
    }

    [Test]
    public void CopyRgba_CompactsRowsFromPaddedSource()
    {
        byte[] source = new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8, 99, 99, 99, 99,
            9, 10, 11, 12, 13, 14, 15, 16, 88, 88, 88, 88
        };

        byte[] copy = WebpPixelBufferHelper.CopyRgba(source, 2, 2, 12);

        Assert.That(copy, Is.EqualTo(new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8,
            9, 10, 11, 12, 13, 14, 15, 16
        }));
    }

    [Test]
    public void CopyRgbaTo_CopiesRowsIntoPaddedDestination()
    {
        byte[] source = new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8,
            9, 10, 11, 12, 13, 14, 15, 16
        };
        byte[] destination = new byte[24];

        WebpPixelBufferHelper.CopyRgbaTo(source, 2, 2, 8, destination, 12);

        Assert.That(destination, Is.EqualTo(new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8, 0, 0, 0, 0,
            9, 10, 11, 12, 13, 14, 15, 16, 0, 0, 0, 0
        }));
    }

    [Test]
    public void ToBgra_SwapsRedAndBlueChannelsAndCompactsRows()
    {
        byte[] rgba = new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8, 99, 99, 99, 99,
            9, 10, 11, 12, 13, 14, 15, 16, 88, 88, 88, 88
        };

        byte[] bgra = WebpPixelBufferHelper.ToBgra(rgba, 2, 2, 12);

        Assert.That(bgra, Is.EqualTo(new byte[]
        {
            3, 2, 1, 4, 7, 6, 5, 8,
            11, 10, 9, 12, 15, 14, 13, 16
        }));
    }

    [Test]
    public void FromBgra_SwapsBlueAndRedChannelsAndCompactsRows()
    {
        byte[] bgra = new byte[]
        {
            3, 2, 1, 4, 7, 6, 5, 8, 99, 99, 99, 99,
            11, 10, 9, 12, 15, 14, 13, 16, 88, 88, 88, 88
        };

        byte[] rgba = WebpPixelBufferHelper.FromBgra(bgra, 2, 2, 12);

        Assert.That(rgba, Is.EqualTo(new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8,
            9, 10, 11, 12, 13, 14, 15, 16
        }));
    }

    [Test]
    public void CopyRgbaToBgra_AndCopyBgraToRgba_WritePaddedDestinations()
    {
        byte[] rgba = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        byte[] bgra = new byte[12];
        byte[] roundTrip = new byte[12];

        WebpPixelBufferHelper.CopyRgbaToBgra(rgba, 2, 1, 8, bgra, 12);
        WebpPixelBufferHelper.CopyBgraToRgba(bgra, 2, 1, 12, roundTrip, 12);

        Assert.That(bgra, Is.EqualTo(new byte[] { 3, 2, 1, 4, 7, 6, 5, 8, 0, 0, 0, 0 }));
        Assert.That(roundTrip, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 0, 0, 0, 0 }));
    }
}
