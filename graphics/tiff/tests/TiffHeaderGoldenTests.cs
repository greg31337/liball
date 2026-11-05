using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffHeaderGoldenTests
{
    [Test]
    public void ClassicLittleEndianGoldenHeader_ParsesAndWritesExactly()
    {
        byte[] golden = new byte[] { 0x49, 0x49, 0x2A, 0x00, 0x08, 0x00, 0x00, 0x00 };

        TiffHeaderReadResult readResult = TiffHeader.ReadClassic(golden);
        byte[] written = new byte[8];
        TiffDiagnostics writeDiagnostics = TiffHeader.WriteClassic(written, readResult.Header);

        Assert.That(readResult.Success, Is.True);
        Assert.That(readResult.Header.ByteOrder, Is.EqualTo(TiffByteOrder.LittleEndian));
        Assert.That(readResult.Header.FileKind, Is.EqualTo(TiffFileKind.Classic));
        Assert.That(readResult.Header.FirstIfdOffset, Is.EqualTo(8));
        Assert.That(writeDiagnostics.Count(), Is.EqualTo(0));
        Assert.That(written, Is.EqualTo(golden));
    }

    [Test]
    public void ClassicBigEndianGoldenHeader_ParsesAndWritesExactly()
    {
        byte[] golden = new byte[] { 0x4D, 0x4D, 0x00, 0x2A, 0x12, 0x34, 0x56, 0x78 };

        TiffHeaderReadResult readResult = TiffHeader.ReadClassic(golden);
        byte[] written = new byte[8];
        TiffDiagnostics writeDiagnostics = TiffHeader.WriteClassic(written, readResult.Header);

        Assert.That(readResult.Success, Is.True);
        Assert.That(readResult.Header.ByteOrder, Is.EqualTo(TiffByteOrder.BigEndian));
        Assert.That(readResult.Header.FileKind, Is.EqualTo(TiffFileKind.Classic));
        Assert.That(readResult.Header.FirstIfdOffset, Is.EqualTo(0x12345678UL));
        Assert.That(writeDiagnostics.Count(), Is.EqualTo(0));
        Assert.That(written, Is.EqualTo(golden));
    }

    [Test]
    public void BigTiffLittleEndianGoldenHeader_ParsesAndWritesExactly()
    {
        byte[] golden = new byte[]
        {
            0x49, 0x49, 0x2B, 0x00,
            0x08, 0x00, 0x00, 0x00,
            0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11
        };

        TiffHeaderReadResult readResult = TiffHeader.ReadBigTiff(golden);
        byte[] written = new byte[16];
        TiffDiagnostics writeDiagnostics = TiffHeader.WriteBigTiff(written, readResult.Header);

        Assert.That(readResult.Success, Is.True);
        Assert.That(readResult.Header.ByteOrder, Is.EqualTo(TiffByteOrder.LittleEndian));
        Assert.That(readResult.Header.FileKind, Is.EqualTo(TiffFileKind.BigTiff));
        Assert.That(readResult.Header.FirstIfdOffset, Is.EqualTo(0x1122334455667788UL));
        Assert.That(writeDiagnostics.Count(), Is.EqualTo(0));
        Assert.That(written, Is.EqualTo(golden));
    }

    [Test]
    public void BigTiffBigEndianGoldenHeader_ParsesAndWritesExactly()
    {
        byte[] golden = new byte[]
        {
            0x4D, 0x4D, 0x00, 0x2B,
            0x00, 0x08, 0x00, 0x00,
            0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88
        };

        TiffHeaderReadResult readResult = TiffHeader.ReadBigTiff(golden);
        byte[] written = new byte[16];
        TiffDiagnostics writeDiagnostics = TiffHeader.WriteBigTiff(written, readResult.Header);

        Assert.That(readResult.Success, Is.True);
        Assert.That(readResult.Header.ByteOrder, Is.EqualTo(TiffByteOrder.BigEndian));
        Assert.That(readResult.Header.FileKind, Is.EqualTo(TiffFileKind.BigTiff));
        Assert.That(readResult.Header.FirstIfdOffset, Is.EqualTo(0x1122334455667788UL));
        Assert.That(writeDiagnostics.Count(), Is.EqualTo(0));
        Assert.That(written, Is.EqualTo(golden));
    }

    [Test]
    public void HeaderDetectors_AcceptWrittenGoldenHeaders()
    {
        byte[] classic = new byte[8];
        byte[] bigTiff = new byte[16];

        TiffDiagnostics classicDiagnostics = TiffHeader.WriteClassic(classic, TiffByteOrder.LittleEndian, 8);
        TiffDiagnostics bigTiffDiagnostics = TiffHeader.WriteBigTiff(bigTiff, TiffByteOrder.LittleEndian, 16);

        Assert.That(classicDiagnostics.Count(), Is.EqualTo(0));
        Assert.That(bigTiffDiagnostics.Count(), Is.EqualTo(0));
        Assert.That(TiffHeader.IsClassicTiff(classic), Is.True);
        Assert.That(TiffHeader.IsBigTiff(bigTiff), Is.True);
        Assert.That(TiffHeader.IsBigTiff(classic), Is.False);
        Assert.That(TiffHeader.IsClassicTiff(bigTiff), Is.False);
    }

    [Test]
    public void CrossKindHeaderParsing_ReportsInvalidVersion()
    {
        byte[] classicPadded = new byte[]
        {
            0x49, 0x49, 0x2A, 0x00,
            0x08, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00
        };
        byte[] bigTiff = new byte[]
        {
            0x49, 0x49, 0x2B, 0x00,
            0x08, 0x00, 0x00, 0x00,
            0x10, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00
        };

        TiffHeaderReadResult classicAsBigTiff = TiffHeader.ReadBigTiff(classicPadded);
        TiffHeaderReadResult bigTiffAsClassic = TiffHeader.ReadClassic(bigTiff);

        Assert.That(classicAsBigTiff.Success, Is.False);
        Assert.That(classicAsBigTiff.Diagnostics.ContainsCode(TiffErrorCode.InvalidTiffVersion), Is.True);
        Assert.That(bigTiffAsClassic.Success, Is.False);
        Assert.That(bigTiffAsClassic.Diagnostics.ContainsCode(TiffErrorCode.InvalidTiffVersion), Is.True);
    }
}
