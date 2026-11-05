namespace chart.Tests;

[TestFixture]
public class BmpEncoderTests
{
    [Test]
    public void Encode24Bit_WritesBmpHeadersAndBottomUpRowsWithPadding()
    {
        RawBitmap bitmap = CreateSampleBitmap();

        byte[] bytes = BmpEncoder.Encode(bitmap, 24, false);

        Assert.That(bytes[0], Is.EqualTo((byte)'B'));
        Assert.That(bytes[1], Is.EqualTo((byte)'M'));
        Assert.That(BmpEncoder.ReadInt32(bytes, 2), Is.EqualTo(70));
        Assert.That(BmpEncoder.ReadInt32(bytes, 10), Is.EqualTo(54));
        Assert.That(BmpEncoder.ReadInt32(bytes, 14), Is.EqualTo(40));
        Assert.That(BmpEncoder.ReadInt32(bytes, 18), Is.EqualTo(2));
        Assert.That(BmpEncoder.ReadInt32(bytes, 22), Is.EqualTo(2));
        Assert.That(BmpEncoder.ReadInt16(bytes, 26), Is.EqualTo(1));
        Assert.That(BmpEncoder.ReadInt16(bytes, 28), Is.EqualTo(24));
        Assert.That(BmpEncoder.ReadInt32(bytes, 34), Is.EqualTo(16));

        Assert.That(bytes[54], Is.EqualTo(255));
        Assert.That(bytes[55], Is.EqualTo(0));
        Assert.That(bytes[56], Is.EqualTo(0));
        Assert.That(bytes[57], Is.EqualTo(255));
        Assert.That(bytes[58], Is.EqualTo(255));
        Assert.That(bytes[59], Is.EqualTo(255));
        Assert.That(bytes[60], Is.EqualTo(0));
        Assert.That(bytes[61], Is.EqualTo(0));
        Assert.That(bytes[62], Is.EqualTo(0));
        Assert.That(bytes[63], Is.EqualTo(0));
        Assert.That(bytes[64], Is.EqualTo(255));
        Assert.That(bytes[65], Is.EqualTo(0));
        Assert.That(bytes[66], Is.EqualTo(255));
        Assert.That(bytes[67], Is.EqualTo(0));
        Assert.That(bytes[68], Is.EqualTo(0));
        Assert.That(bytes[69], Is.EqualTo(0));
    }

    [Test]
    public void Encode24Bit_TopDownRowsUseNegativeHeightAndSourceOrder()
    {
        RawBitmap bitmap = CreateSampleBitmap();

        byte[] bytes = BmpEncoder.Encode(bitmap, 24, true);

        Assert.That(BmpEncoder.ReadInt32(bytes, 22), Is.EqualTo(-2));
        Assert.That(bytes[54], Is.EqualTo(0));
        Assert.That(bytes[55], Is.EqualTo(0));
        Assert.That(bytes[56], Is.EqualTo(255));
        Assert.That(bytes[57], Is.EqualTo(0));
        Assert.That(bytes[58], Is.EqualTo(255));
        Assert.That(bytes[59], Is.EqualTo(0));
    }

    [Test]
    public void Encode32Bit_WritesBgraRowsWithAlpha()
    {
        RawBitmap bitmap = CreateSampleBitmap();

        byte[] bytes = BmpEncoder.Encode(bitmap, 32, true);

        Assert.That(BmpEncoder.ReadInt32(bytes, 2), Is.EqualTo(70));
        Assert.That(BmpEncoder.ReadInt16(bytes, 28), Is.EqualTo(32));
        Assert.That(BmpEncoder.ReadInt32(bytes, 34), Is.EqualTo(16));
        Assert.That(bytes[54], Is.EqualTo(0));
        Assert.That(bytes[55], Is.EqualTo(0));
        Assert.That(bytes[56], Is.EqualTo(255));
        Assert.That(bytes[57], Is.EqualTo(10));
        Assert.That(bytes[58], Is.EqualTo(0));
        Assert.That(bytes[59], Is.EqualTo(255));
        Assert.That(bytes[60], Is.EqualTo(0));
        Assert.That(bytes[61], Is.EqualTo(20));
    }

    [Test]
    public void Encode32Bit_BottomUpRowsPreserveAlphaAndHeaderFields()
    {
        RawBitmap bitmap = CreateSampleBitmap();

        byte[] bytes = BmpEncoder.Encode(bitmap, 32, false);

        Assert.That(BmpEncoder.ReadInt16(bytes, 6), Is.EqualTo(0));
        Assert.That(BmpEncoder.ReadInt16(bytes, 8), Is.EqualTo(0));
        Assert.That(BmpEncoder.ReadInt32(bytes, 30), Is.EqualTo(0));
        Assert.That(BmpEncoder.ReadInt32(bytes, 38), Is.EqualTo(0));
        Assert.That(BmpEncoder.ReadInt32(bytes, 42), Is.EqualTo(0));
        Assert.That(BmpEncoder.ReadInt32(bytes, 46), Is.EqualTo(0));
        Assert.That(BmpEncoder.ReadInt32(bytes, 50), Is.EqualTo(0));
        Assert.That(BmpEncoder.ReadInt32(bytes, 22), Is.EqualTo(2));
        AssertPixelBgra(bytes, 54, 0, 0, 255, 30);
        AssertPixelBgra(bytes, 58, 255, 255, 255, 40);
        AssertPixelBgra(bytes, 62, 255, 0, 0, 10);
        AssertPixelBgra(bytes, 66, 0, 255, 0, 20);
    }

    [Test]
    public void CalculateRowStride_AddsFourBytePaddingFor24BitRows()
    {
        Assert.That(BmpEncoder.CalculateRowStride(1, 24), Is.EqualTo(4));
        Assert.That(BmpEncoder.CalculateRowStride(2, 24), Is.EqualTo(8));
        Assert.That(BmpEncoder.CalculateRowStride(3, 24), Is.EqualTo(12));
        Assert.That(BmpEncoder.CalculateRowStride(2, 32), Is.EqualTo(8));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => BmpEncoder.CalculateRowStride(int.MaxValue, 24));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => BmpEncoder.CalculateRowStride(int.MaxValue, 32));
    }

    [Test]
    public void WriteFile_WritesEncodedBytes()
    {
        RawBitmap bitmap = CreateSampleBitmap();
        string path = System.IO.Path.Combine(TestContext.CurrentContext.WorkDirectory, "sample.bmp");

        BmpEncoder.WriteFile(bitmap, path, 24, true);

        byte[] bytes = System.IO.File.ReadAllBytes(path);

        Assert.That(bytes[0], Is.EqualTo((byte)'B'));
        Assert.That(bytes[1], Is.EqualTo((byte)'M'));
        Assert.That(BmpEncoder.ReadInt16(bytes, 28), Is.EqualTo(24));
    }

    [Test]
    public void BmpEncoder_ValidatesInputs()
    {
        RawBitmap bitmap = CreateSampleBitmap();

        Assert.Throws<System.ArgumentNullException>(() => BmpEncoder.Encode(null!, 24, true));
        Assert.Throws<System.ArgumentException>(() => BmpEncoder.Encode(bitmap, 8, true));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => BmpEncoder.CalculateRowStride(0, 24));
        Assert.Throws<System.ArgumentNullException>(() => BmpEncoder.WriteFile(bitmap, null!, 24, true));

        bitmap.Height = int.MaxValue;
        Assert.Throws<System.ArgumentOutOfRangeException>(() => BmpEncoder.Encode(bitmap, 24, true));
    }

    static RawBitmap CreateSampleBitmap()
    {
        RawBitmap bitmap = new RawBitmap(2, 2, ChartPixelFormat.Rgba32);
        bitmap.SetPixel(0, 0, new ChartColor(255, 0, 0, 10));
        bitmap.SetPixel(1, 0, new ChartColor(0, 255, 0, 20));
        bitmap.SetPixel(0, 1, new ChartColor(0, 0, 255, 30));
        bitmap.SetPixel(1, 1, new ChartColor(255, 255, 255, 40));
        return bitmap;
    }

    static void AssertPixelBgra(byte[] bytes, int offset, int r, int g, int b, int a)
    {
        Assert.That(bytes[offset], Is.EqualTo((byte)b));
        Assert.That(bytes[offset + 1], Is.EqualTo((byte)g));
        Assert.That(bytes[offset + 2], Is.EqualTo((byte)r));
        Assert.That(bytes[offset + 3], Is.EqualTo((byte)a));
    }
}
