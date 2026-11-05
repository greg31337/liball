using System.Collections.Generic;
using System.IO;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class BmpHeaderParserTests
{
    [Test]
    public void Parse_BitmapInfoHeaderCreatesStructuralDirectoriesAndRanges()
    {
        byte[] bytes = CreateBmp(40);
        WriteSigned32(bytes, 18, 320);
        WriteSigned32(bytes, 22, 200);
        WriteUnsigned16(bytes, 26, 1);
        WriteUnsigned16(bytes, 28, 24);

        ImageMetadataDocument document = ParseBmp(bytes);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Bmp));
        Assert.That(document.ByteLength, Is.EqualTo(54));
        Assert.That(document.Directories, Has.Count.EqualTo(2));
        Assert.That(document.Directories[0].Family, Is.EqualTo(MetadataFamily.Bmp));
        Assert.That(document.Directories[0].Path, Is.EqualTo("BMP/FileHeader"));
        Assert.That(document.Directories[0].Offset, Is.EqualTo(0));
        Assert.That(document.Directories[0].Length, Is.EqualTo(14));
        Assert.That(document.Directories[1].Path, Is.EqualTo("BMP/DIBHeader"));
        Assert.That(document.Directories[1].Offset, Is.EqualTo(14));
        Assert.That(document.Directories[1].Length, Is.EqualTo(40));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.FileHeader.Signature").Value.Text, Is.EqualTo("BM"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.FileHeader.FileSize").Value.UnsignedIntegerValue, Is.EqualTo(54));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.FileHeader.PixelDataOffset").Value.UnsignedIntegerValue, Is.EqualTo(54));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.DIBHeader.HeaderSize").Value.UnsignedIntegerValue, Is.EqualTo(40));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.DIBHeader.HeaderType").Value.Text, Is.EqualTo("BITMAPINFOHEADER"));
        Assert.That(document.RawBlocks, Has.Count.EqualTo(2));
        Assert.That(document.RawBlocks[0].BlockType, Is.EqualTo("FileHeader"));
        Assert.That(document.RawBlocks[1].BlockType, Is.EqualTo("DIBHeader"));
    }

    [TestCase(12u, "BITMAPCOREHEADER")]
    [TestCase(16u, "OS22XBITMAPHEADER (Short)")]
    [TestCase(40u, "BITMAPINFOHEADER")]
    [TestCase(52u, "BITMAPV2INFOHEADER")]
    [TestCase(56u, "BITMAPV3INFOHEADER")]
    [TestCase(64u, "OS22XBITMAPHEADER")]
    [TestCase(108u, "BITMAPV4HEADER")]
    [TestCase(124u, "BITMAPV5HEADER")]
    public void Parse_RecognizesCommonDibHeaderVersions(uint headerSize, string expectedType)
    {
        byte[] bytes = CreateBmp((int)headerSize);

        ImageMetadataDocument document = ParseBmp(bytes);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Directories, Has.Count.EqualTo(2));
        Assert.That(document.Directories[1].Length, Is.EqualTo(headerSize));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.DIBHeader.HeaderType").Value.Text, Is.EqualTo(expectedType));
    }

    [Test]
    public void DecodeDibHeader_CoreHeaderUsesUnsignedDimensions()
    {
        byte[] data = new byte[12];
        WriteUnsigned32(data, 0, 12);
        WriteUnsigned16(data, 4, 50000);
        WriteUnsigned16(data, 6, 40000);
        WriteUnsigned16(data, 8, 1);
        WriteUnsigned16(data, 10, 8);

        BmpDibHeader header = BmpHeaderParser.DecodeDibHeader(data, BmpHeaderParser.HeaderType(12));

        Assert.That(header.Width, Is.EqualTo(50000));
        Assert.That(header.Height, Is.EqualTo(40000));
        Assert.That(header.Planes, Is.EqualTo(1));
        Assert.That(header.BitCount, Is.EqualTo(8));
    }

    [Test]
    public void DecodeDibHeader_V5DecodesExtendedHeaderFields()
    {
        byte[] data = new byte[124];
        WriteUnsigned32(data, 0, 124);
        WriteSigned32(data, 4, 640);
        WriteSigned32(data, 8, -480);
        WriteUnsigned16(data, 12, 1);
        WriteUnsigned16(data, 14, 32);
        WriteUnsigned32(data, 16, 3);
        WriteUnsigned32(data, 20, 1228800);
        WriteSigned32(data, 24, 3780);
        WriteSigned32(data, 28, 3780);
        WriteUnsigned32(data, 32, 16);
        WriteUnsigned32(data, 36, 8);
        WriteUnsigned32(data, 40, 0x00ff0000);
        WriteUnsigned32(data, 44, 0x0000ff00);
        WriteUnsigned32(data, 48, 0x000000ff);
        WriteUnsigned32(data, 52, 0xff000000);
        WriteUnsigned32(data, 56, 0x73524742);
        int endpoint = 0;
        while (endpoint < 36)
        {
            data[60 + endpoint] = (byte)(endpoint + 1);
            endpoint = endpoint + 1;
        }

        WriteUnsigned32(data, 96, 100);
        WriteUnsigned32(data, 100, 200);
        WriteUnsigned32(data, 104, 300);
        WriteUnsigned32(data, 108, 4);
        WriteUnsigned32(data, 112, 256);
        WriteUnsigned32(data, 116, 128);
        WriteUnsigned32(data, 120, 0);

        BmpDibHeader header = BmpHeaderParser.DecodeDibHeader(data, BmpHeaderParser.HeaderType(124));

        Assert.That(header.Width, Is.EqualTo(640));
        Assert.That(header.Height, Is.EqualTo(-480));
        Assert.That(header.Planes, Is.EqualTo(1));
        Assert.That(header.BitCount, Is.EqualTo(32));
        Assert.That(header.Compression, Is.EqualTo(3));
        Assert.That(header.ImageSize, Is.EqualTo(1228800));
        Assert.That(header.XPelsPerMeter, Is.EqualTo(3780));
        Assert.That(header.YPelsPerMeter, Is.EqualTo(3780));
        Assert.That(header.ColorsUsed, Is.EqualTo(16));
        Assert.That(header.ColorsImportant, Is.EqualTo(8));
        Assert.That(header.RedMask, Is.EqualTo(0x00ff0000));
        Assert.That(header.GreenMask, Is.EqualTo(0x0000ff00));
        Assert.That(header.BlueMask, Is.EqualTo(0x000000ff));
        Assert.That(header.AlphaMask, Is.EqualTo(0xff000000));
        Assert.That(header.ColorSpaceType, Is.EqualTo(0x73524742));
        Assert.That(header.ColorSpaceEndpoints, Has.Length.EqualTo(36));
        Assert.That(header.ColorSpaceEndpoints[0], Is.EqualTo(1));
        Assert.That(header.ColorSpaceEndpoints[35], Is.EqualTo(36));
        Assert.That(header.GammaRed, Is.EqualTo(100));
        Assert.That(header.GammaGreen, Is.EqualTo(200));
        Assert.That(header.GammaBlue, Is.EqualTo(300));
        Assert.That(header.Intent, Is.EqualTo(4));
        Assert.That(header.ProfileDataOffset, Is.EqualTo(256));
        Assert.That(header.ProfileSize, Is.EqualTo(128));
        Assert.That(header.Reserved, Is.Zero);
    }

    [Test]
    public void DecodeDibHeader_Os2HeaderUsesOs2ExtensionLayout()
    {
        byte[] data = new byte[64];
        WriteUnsigned32(data, 0, 64);
        WriteSigned32(data, 4, 100);
        WriteSigned32(data, 8, 80);
        WriteUnsigned16(data, 12, 1);
        WriteUnsigned16(data, 14, 8);
        WriteUnsigned16(data, 40, 2);
        WriteUnsigned16(data, 42, 0);
        WriteUnsigned16(data, 44, 3);
        WriteUnsigned16(data, 46, 4);
        WriteUnsigned32(data, 48, 5);
        WriteUnsigned32(data, 52, 6);
        WriteUnsigned32(data, 56, 7);
        WriteUnsigned32(data, 60, 8);

        BmpDibHeader header = BmpHeaderParser.DecodeDibHeader(data, BmpHeaderParser.HeaderType(64));

        Assert.That(header.Units, Is.EqualTo(2));
        Assert.That(header.Recording, Is.EqualTo(3));
        Assert.That(header.Rendering, Is.EqualTo(4));
        Assert.That(header.Size1, Is.EqualTo(5));
        Assert.That(header.Size2, Is.EqualTo(6));
        Assert.That(header.ColorEncoding, Is.EqualTo(7));
        Assert.That(header.Identifier, Is.EqualTo(8));
        Assert.That(header.RedMask, Is.Zero);
        Assert.That(header.AlphaMask, Is.Zero);
    }

    [Test]
    public void Parse_InvalidSignatureStopsBeforeDibHeader()
    {
        byte[] bytes = CreateBmp(40);
        bytes[0] = (byte)'Z';

        ImageMetadataDocument document = ParseBmp(bytes);

        Assert.That(document.Directories, Is.Empty);
        Assert.That(document.RawBlocks, Is.Empty);
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("bmp.signature.invalid"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.RecoverableError));
    }

    [Test]
    public void Parse_UnsupportedDibHeaderSizeAddsUnsupportedDiagnostic()
    {
        byte[] bytes = CreateBmp(32);

        ImageMetadataDocument document = ParseBmp(bytes);

        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(document.RawBlocks, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("bmp.dib-size.unsupported"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.Unsupported));
    }

    [Test]
    public void Parse_TruncatedDibHeaderReportsDeclaredRange()
    {
        byte[] bytes = CreateBmp(40);
        byte[] truncated = new byte[30];
        System.Array.Copy(bytes, truncated, truncated.Length);
        WriteUnsigned32(truncated, 2, (uint)truncated.Length);
        WriteUnsigned32(truncated, 10, (uint)truncated.Length);

        ImageMetadataDocument document = ParseBmp(truncated);

        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("bmp.dib-header.truncated"));
        Assert.That(document.Diagnostics[0].Offset, Is.EqualTo(14));
        Assert.That(document.Diagnostics[0].Length, Is.EqualTo(40));
    }

    [Test]
    public void Parse_PixelOffsetInsideHeadersAddsDiagnostic()
    {
        byte[] bytes = CreateBmp(40);
        WriteUnsigned32(bytes, 10, 20);

        ImageMetadataDocument document = ParseBmp(bytes);

        Assert.That(document.Directories, Has.Count.EqualTo(2));
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("bmp.pixel-offset.inside-header"));
    }

    [Test]
    public void Parse_RawCaptureCopiesHeaderBytesWhenEnabled()
    {
        byte[] bytes = CreateBmp(12);
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.CaptureRawBlockData = true;

        ImageMetadataDocument document = ParseBmp(bytes, options);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.RawBlocks, Has.Count.EqualTo(2));
        Assert.That(document.RawBlocks[0].Data, Is.EqualTo(Subarray(bytes, 0, 14)));
        Assert.That(document.RawBlocks[1].Data, Is.EqualTo(Subarray(bytes, 14, 12)));
    }

    [Test]
    public void Parse_DibHeaderOverSegmentLimitStopsSafely()
    {
        byte[] bytes = CreateBmp(40);
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxSegmentBytes = 20;

        ImageMetadataDocument document = ParseBmp(bytes, options);

        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("bmp.dib-header.limit"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.LimitExceeded));
    }

    public static ImageMetadataDocument ParseBmp(byte[] bytes)
    {
        return ParseBmp(bytes, new ImageMetadataReaderOptions());
    }

    public static ImageMetadataDocument ParseBmp(byte[] bytes, ImageMetadataReaderOptions options)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return BmpMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.Bmp), options);
    }

    public static byte[] CreateBmp(int dibHeaderSize)
    {
        byte[] bytes = new byte[14 + dibHeaderSize];
        bytes[0] = (byte)'B';
        bytes[1] = (byte)'M';
        WriteUnsigned32(bytes, 2, (uint)bytes.Length);
        WriteUnsigned32(bytes, 10, (uint)bytes.Length);
        WriteUnsigned32(bytes, 14, (uint)dibHeaderSize);

        if (dibHeaderSize == 12)
        {
            WriteUnsigned16(bytes, 18, 1);
            WriteUnsigned16(bytes, 20, 1);
            WriteUnsigned16(bytes, 22, 1);
            WriteUnsigned16(bytes, 24, 24);
        }
        else if (dibHeaderSize >= 16)
        {
            WriteSigned32(bytes, 18, 1);
            WriteSigned32(bytes, 22, 1);
            WriteUnsigned16(bytes, 26, 1);
            WriteUnsigned16(bytes, 28, 24);
        }

        return bytes;
    }

    public static byte[] Subarray(byte[] bytes, int offset, int length)
    {
        byte[] result = new byte[length];
        System.Array.Copy(bytes, offset, result, 0, length);
        return result;
    }

    public static void WriteUnsigned16(byte[] bytes, int offset, ushort value)
    {
        bytes[offset] = (byte)value;
        bytes[offset + 1] = (byte)(value >> 8);
    }

    public static void WriteUnsigned32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)value;
        bytes[offset + 1] = (byte)(value >> 8);
        bytes[offset + 2] = (byte)(value >> 16);
        bytes[offset + 3] = (byte)(value >> 24);
    }

    public static void WriteSigned32(byte[] bytes, int offset, int value)
    {
        WriteUnsigned32(bytes, offset, (uint)value);
    }
}
