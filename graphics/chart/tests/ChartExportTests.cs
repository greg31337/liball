namespace chart.Tests;

[TestFixture]
public class ChartExportTests
{
    [Test]
    public void ToRawBitmap_ClonesBitmapMetadataAndBuffer()
    {
        RawBitmap bitmap = CreateSampleBitmap();

        RawBitmap copy = ChartExportOperations.ToRawBitmap(bitmap);
        byte[] rawBytes = ChartExportOperations.ToRawBytes(bitmap);

        Assert.That(copy.Width, Is.EqualTo(bitmap.Width));
        Assert.That(copy.Height, Is.EqualTo(bitmap.Height));
        Assert.That(copy.Stride, Is.EqualTo(bitmap.Stride));
        Assert.That(copy.PixelFormat, Is.EqualTo(bitmap.PixelFormat));
        Assert.That(copy.Buffer, Is.EqualTo(bitmap.Buffer));
        Assert.That(object.ReferenceEquals(copy.Buffer, bitmap.Buffer), Is.False);
        Assert.That(rawBytes, Is.EqualTo(bitmap.Buffer));

        bitmap.SetPixel(0, 0, ChartColor.White());

        Assert.That(copy.GetPixel(0, 0).EqualsColor(new ChartColor(10, 20, 30, 40)), Is.True);
    }

    [Test]
    public void BmpHelpers_ReturnAndSaveEncodedBytes()
    {
        RawBitmap bitmap = CreateSampleBitmap();
        string path = System.IO.Path.Combine(TestContext.CurrentContext.WorkDirectory, "chart-export.bmp");

        byte[] defaultBytes = ChartExportOperations.ToBmpBytes(bitmap);
        byte[] explicitBytes = ChartExportOperations.ToBmpBytes(bitmap, 24, false);
        ChartExportOperations.SaveBmp(bitmap, path, 24, false);

        Assert.That(defaultBytes, Is.EqualTo(BmpEncoder.Encode(bitmap, 32, true)));
        Assert.That(explicitBytes, Is.EqualTo(BmpEncoder.Encode(bitmap, 24, false)));
        Assert.That(System.IO.File.ReadAllBytes(path), Is.EqualTo(explicitBytes));
    }

    [Test]
    public void PngHelpers_ReturnAndSaveEncodedBytes()
    {
        RawBitmap bitmap = CreateSampleBitmap();
        string path = System.IO.Path.Combine(TestContext.CurrentContext.WorkDirectory, "chart-export.png");

        byte[] bytes = ChartExportOperations.ToPngBytes(bitmap);
        ChartExportOperations.SavePng(bitmap, path);

        Assert.That(bytes, Is.EqualTo(PngEncoder.Encode(bitmap)));
        Assert.That(System.IO.File.ReadAllBytes(path), Is.EqualTo(bytes));
        Assert.That(new byte[] { bytes[0], bytes[1], bytes[2], bytes[3], bytes[4], bytes[5], bytes[6], bytes[7] }, Is.EqualTo(PngEncoder.Signature()));
    }

    [Test]
    public void SvgHelpers_ReturnStringStreamBytesAndFile()
    {
        SvgWriter writer = new SvgWriter(12, 8);
        writer.AddTitleDescription("Export", "");

        string svg = ChartExportOperations.ToSvg(writer);
        byte[] bytes = ChartExportOperations.ToSvgBytes(svg);
        System.IO.MemoryStream stream = ChartExportOperations.ToSvgStream(svg);
        System.IO.MemoryStream writeStream = new System.IO.MemoryStream();
        string path = System.IO.Path.Combine(TestContext.CurrentContext.WorkDirectory, "chart-export.svg");

        ChartExportOperations.WriteSvg(svg, writeStream);
        ChartExportOperations.SaveSvg(svg, path);

        Assert.That(svg, Is.EqualTo("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"12\" height=\"8\" viewBox=\"0 0 12 8\"><title>Export</title></svg>"));
        Assert.That(bytes, Is.EqualTo(System.Text.Encoding.UTF8.GetBytes(svg)));
        Assert.That(stream.Position, Is.EqualTo(0));
        Assert.That(System.Text.Encoding.UTF8.GetString(stream.ToArray()), Is.EqualTo(svg));
        Assert.That(System.Text.Encoding.UTF8.GetString(writeStream.ToArray()), Is.EqualTo(svg));
        Assert.That(System.IO.File.ReadAllText(path), Is.EqualTo(svg));
        Assert.That(ChartExportOperations.ToSvg(3, 2), Is.EqualTo("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"3\" height=\"2\" viewBox=\"0 0 3 2\"></svg>"));
    }

    [Test]
    public void ChartExport_ValidatesInputs()
    {
        RawBitmap bitmap = CreateSampleBitmap();

        Assert.Throws<System.ArgumentNullException>(() => ChartExportOperations.ToRawBitmap(null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartExportOperations.ToRawBytes(null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartExportOperations.ToBmpBytes(null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartExportOperations.SaveBmp(bitmap, null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartExportOperations.ToPngBytes(null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartExportOperations.SavePng(bitmap, null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartExportOperations.ToSvg(null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartExportOperations.ToSvgBytes((string)null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartExportOperations.ToSvgStream((string)null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartExportOperations.WriteSvg("<svg/>", null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartExportOperations.SaveSvg("<svg/>", null!));
    }

    static RawBitmap CreateSampleBitmap()
    {
        RawBitmap bitmap = new RawBitmap(2, 1, ChartPixelFormat.Rgba32);
        bitmap.SetPixel(0, 0, new ChartColor(10, 20, 30, 40));
        bitmap.SetPixel(1, 0, new ChartColor(50, 60, 70, 80));
        return bitmap;
    }
}
