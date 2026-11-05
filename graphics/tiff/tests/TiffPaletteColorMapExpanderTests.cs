using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffPaletteColorMapExpanderTests
{
    [Test]
    public void ExpandPaletteToRgb16_ExpandsIndicesUsingSeparateColorPlanes()
    {
        TiffImageInfo info = CreatePaletteInfo(2, new ushort[] { 0, 100, 200, 300, 10, 110, 210, 310, 20, 120, 220, 320 });
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        ushort[] result = TiffCodec.ExpandPaletteToRgb16(info, new double[] { 0, 2, 3 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new ushort[] { 0, 10, 20, 200, 210, 220, 300, 310, 320 }));
    }

    [Test]
    public void ExpandPaletteToRgb8_ScalesSixteenBitColorMapValues()
    {
        TiffImageInfo info = CreatePaletteInfo(1, new ushort[] { 0, 65535, 0, 32768, 0, 0 });
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ExpandPaletteToRgb8(info, new double[] { 1 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 255, 128, 0 }));
    }

    [Test]
    public void ExpandPaletteToRgb16_AllowsEmptyIndexInput()
    {
        TiffImageInfo info = CreatePaletteInfo(1, new ushort[] { 0, 65535, 0, 65535, 0, 65535 });
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        ushort[] result = TiffCodec.ExpandPaletteToRgb16(info, new double[0], diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new ushort[0]));
    }

    [Test]
    public void ExpandPaletteToRgb16_ReportsNonPalettePhotometric()
    {
        TiffImageInfo info = CreatePaletteInfo(1, new ushort[] { 0, 65535, 0, 65535, 0, 65535 });
        info.Photometric = TiffPhotometric.Rgb;
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        ushort[] result = TiffCodec.ExpandPaletteToRgb16(info, new double[] { 0 }, diagnostics);

        Assert.That(result, Is.EqualTo(new ushort[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.UnsupportedPhotometric), Is.True);
    }

    [Test]
    public void ExpandPaletteToRgb16_ReportsInvalidColorMapCount()
    {
        TiffImageInfo info = CreatePaletteInfo(2, new ushort[] { 0, 1, 2 });
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        ushort[] result = TiffCodec.ExpandPaletteToRgb16(info, new double[] { 0 }, diagnostics);

        Assert.That(result, Is.EqualTo(new ushort[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagCount), Is.True);
    }

    [Test]
    public void ExpandPaletteToRgb16_ReportsIndexOutsideColorMapRange()
    {
        TiffImageInfo info = CreatePaletteInfo(1, new ushort[] { 0, 65535, 0, 65535, 0, 65535 });
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        ushort[] result = TiffCodec.ExpandPaletteToRgb16(info, new double[] { 2 }, diagnostics);

        Assert.That(result, Is.EqualTo(new ushort[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void ExpandPaletteToRgb16_ReportsFractionalPaletteIndex()
    {
        TiffImageInfo info = CreatePaletteInfo(1, new ushort[] { 0, 65535, 0, 65535, 0, 65535 });
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        ushort[] result = TiffCodec.ExpandPaletteToRgb16(info, new double[] { 0.5d }, diagnostics);

        Assert.That(result, Is.EqualTo(new ushort[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    static TiffImageInfo CreatePaletteInfo(int bitsPerSample, ushort[] colorMap)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = 1;
        info.Length = 1;
        info.SamplesPerPixel = 1;
        info.BitsPerSample = new int[] { bitsPerSample };
        info.Photometric = TiffPhotometric.Palette;
        info.ColorMap = colorMap;
        return info;
    }
}
