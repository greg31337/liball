using System.Collections.Generic;
using System.IO;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class Cr2MetadataParserTests
{
    [TestCase(true)]
    [TestCase(false)]
    public void Parse_ReadsCr2HeaderAndDedicatedRawIfd(bool littleEndian)
    {
        byte[] bytes = CreateCr2(littleEndian, 16, 22, false);

        ImageMetadataDocument document = Parse(bytes, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Cr2));
        Assert.That(document.ByteLength, Is.EqualTo(bytes.Length));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.Header.ByteOrder").Value.Text, Is.EqualTo(littleEndian ? "LittleEndian" : "BigEndian"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.Profile.Signature").Value.Text, Is.EqualTo("CR"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.Profile.MajorVersion").Value.IntegerValue, Is.EqualTo(2));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.Profile.MinorVersion").Value.IntegerValue, Is.EqualTo(0));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.Profile.FirstIfdOffset").Value.UnsignedIntegerValue, Is.EqualTo(16));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.Profile.RawIfdOffset").Value.UnsignedIntegerValue, Is.EqualTo(22));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.CR2RawIFD.ImageWidth").Value.UnsignedIntegerValue, Is.EqualTo(6000));
        MetadataDirectory rawDirectory = FindDirectory(document, "CR2RawIFD") ?? throw new System.InvalidOperationException("Expected CR2 RAW IFD was not found.");
        Assert.That(rawDirectory.Family, Is.EqualTo(MetadataFamily.Raw));
        Assert.That(document.RawBlocks, Has.Count.EqualTo(1));
        Assert.That(document.RawBlocks[0].Offset, Is.EqualTo(0));
        Assert.That(document.RawBlocks[0].Length, Is.EqualTo(16));
    }

    [Test]
    public void Parse_DoesNotDuplicateRawIfdAlreadyReachedByTiffChain()
    {
        byte[] bytes = CreateCr2(true, 16, 22, true);

        ImageMetadataDocument document = Parse(bytes, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(CountDirectoriesAtOffset(document, 22), Is.EqualTo(1));
        Assert.That(FindDirectory(document, "IFD1"), Is.Not.Null);
        Assert.That(FindDirectory(document, "CR2RawIFD"), Is.Null);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD1.ImageWidth").Value.UnsignedIntegerValue, Is.EqualTo(6000));
    }

    [TestCase(12, "cr2.raw-ifd.offset.inside-header")]
    [TestCase(1000, "cr2.raw-ifd.offset.outside")]
    public void Parse_InvalidRawIfdOffsetReturnsDiagnostic(int rawIfdOffset, string code)
    {
        byte[] bytes = CreateCr2(true, 16, (uint)rawIfdOffset, false);

        ImageMetadataDocument document = Parse(bytes, new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, code), Is.True);
        Assert.That(FindDirectory(document, "CR2RawIFD"), Is.Null);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.Profile.RawIfdOffset").Value.UnsignedIntegerValue, Is.EqualTo((uint)rawIfdOffset));
    }

    [Test]
    public void Parse_FirstIfdInsideExtendedHeaderIsRejected()
    {
        byte[] bytes = CreateCr2(true, 8, 22, false);

        ImageMetadataDocument document = Parse(bytes, new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, "tiff.ifd.offset.inside.header"), Is.True);
        Assert.That(FindDirectory(document, "IFD0"), Is.Null);
        Assert.That(FindDirectory(document, "CR2RawIFD"), Is.Not.Null);
    }

    [Test]
    public void Parse_TruncatedHeaderReturnsPartialCr2Document()
    {
        byte[] bytes = new byte[] { 73, 73, 42, 0, 16, 0, 0, 0, 67, 82, 2 };

        ImageMetadataDocument document = Parse(bytes, new ImageMetadataReaderOptions());

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Cr2));
        Assert.That(HasDiagnostic(document, "cr2.header.truncated"), Is.True);
        Assert.That(document.Directories, Is.Empty);
    }

    [Test]
    public void Parse_InvalidSignatureAndUnexpectedVersionAreDiagnosed()
    {
        byte[] bytes = CreateCr2(true, 16, 22, false);
        bytes[8] = 88;
        bytes[10] = 3;

        ImageMetadataDocument document = Parse(bytes, new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, "cr2.signature.invalid"), Is.True);
        Assert.That(HasDiagnostic(document, "cr2.version.unexpected"), Is.True);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.Profile.Signature").Value.Text, Is.EqualTo("XR"));
        Assert.That(FindDirectory(document, "IFD0"), Is.Not.Null);
        Assert.That(FindDirectory(document, "CR2RawIFD"), Is.Null);
    }

    [Test]
    public void Parse_InvalidTiffMagicDoesNotTraverseIfds()
    {
        byte[] bytes = CreateCr2(true, 16, 22, false);
        bytes[2] = 41;

        ImageMetadataDocument document = Parse(bytes, new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, "cr2.magic.invalid"), Is.True);
        Assert.That(FindDirectory(document, "IFD0"), Is.Null);
        Assert.That(FindDirectory(document, "CR2RawIFD"), Is.Null);
    }

    [Test]
    public void Parse_HeaderRawBlockHonorsCaptureOptions()
    {
        byte[] bytes = CreateCr2(true, 16, 22, false);
        ImageMetadataReaderOptions capturedOptions = new ImageMetadataReaderOptions();
        capturedOptions.CaptureRawBlockData = true;
        ImageMetadataReaderOptions disabledOptions = new ImageMetadataReaderOptions();
        disabledOptions.CaptureRawBlocks = false;

        ImageMetadataDocument captured = Parse(bytes, capturedOptions);
        ImageMetadataDocument disabled = Parse(bytes, disabledOptions);

        Assert.That(captured.RawBlocks, Has.Count.EqualTo(1));
        Assert.That(captured.RawBlocks[0].Data, Is.EqualTo(Slice(bytes, 16)));
        Assert.That(disabled.RawBlocks, Is.Empty);
    }

    [Test]
    public void ImageReader_DetectsAndParsesCr2Wrapper()
    {
        byte[] bytes = CreateCr2(true, 16, 22, false);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Cr2));
        Assert.That(document.Format.DetectedByMagic, Is.True);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.Profile.HeaderVariant").Value.Text, Is.EqualTo("CR2"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.CR2RawIFD.ImageWidth"), Is.Not.Null);
    }

    [Test]
    public void KindFromExtension_MapsCr2Profile()
    {
        Assert.That(RawTiffProfileParser.KindFromExtension(".cr2"), Is.EqualTo(ImageFormatKind.Cr2));
        Assert.That(RawTiffProfileParser.KindFromExtension(".CR2"), Is.EqualTo(ImageFormatKind.Cr2));
    }

    public static byte[] CreateCr2(bool littleEndian, uint firstIfdOffset, uint rawIfdOffset, bool linkRawIfd)
    {
        List<byte> bytes = new List<byte>();
        bytes.Add(littleEndian ? (byte)73 : (byte)77);
        bytes.Add(littleEndian ? (byte)73 : (byte)77);
        AddUnsigned16(bytes, 42, littleEndian);
        AddUnsigned32(bytes, firstIfdOffset, littleEndian);
        bytes.Add(67);
        bytes.Add(82);
        bytes.Add(2);
        bytes.Add(0);
        AddUnsigned32(bytes, rawIfdOffset, littleEndian);

        AddUnsigned16(bytes, 0, littleEndian);
        AddUnsigned32(bytes, linkRawIfd ? rawIfdOffset : 0, littleEndian);

        AddUnsigned16(bytes, 1, littleEndian);
        AddUnsigned16(bytes, 256, littleEndian);
        AddUnsigned16(bytes, 4, littleEndian);
        AddUnsigned32(bytes, 1, littleEndian);
        AddUnsigned32(bytes, 6000, littleEndian);
        AddUnsigned32(bytes, 0, littleEndian);
        return bytes.ToArray();
    }

    public static ImageMetadataDocument Parse(byte[] bytes, ImageMetadataReaderOptions options)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return Cr2MetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.Cr2), options);
    }

    public static void AddUnsigned16(List<byte> bytes, int value, bool littleEndian)
    {
        if (littleEndian)
        {
            bytes.Add((byte)(value & 255));
            bytes.Add((byte)((value >> 8) & 255));
        }
        else
        {
            bytes.Add((byte)((value >> 8) & 255));
            bytes.Add((byte)(value & 255));
        }
    }

    public static void AddUnsigned32(List<byte> bytes, uint value, bool littleEndian)
    {
        if (littleEndian)
        {
            bytes.Add((byte)(value & 255));
            bytes.Add((byte)((value >> 8) & 255));
            bytes.Add((byte)((value >> 16) & 255));
            bytes.Add((byte)((value >> 24) & 255));
        }
        else
        {
            bytes.Add((byte)((value >> 24) & 255));
            bytes.Add((byte)((value >> 16) & 255));
            bytes.Add((byte)((value >> 8) & 255));
            bytes.Add((byte)(value & 255));
        }
    }

    public static MetadataDirectory? FindDirectory(ImageMetadataDocument document, string group)
    {
        foreach (MetadataDirectory directory in document.Directories)
        {
            if (directory.Group == group)
            {
                return directory;
            }
        }

        return null;
    }

    public static int CountDirectoriesAtOffset(ImageMetadataDocument document, long offset)
    {
        int count = 0;
        foreach (MetadataDirectory directory in document.Directories)
        {
            if (directory.Offset == offset)
            {
                count = count + 1;
            }
        }

        return count;
    }

    public static bool HasDiagnostic(ImageMetadataDocument document, string code)
    {
        foreach (MetadataDiagnostic diagnostic in document.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }

    public static byte[] Slice(byte[] bytes, int length)
    {
        byte[] result = new byte[length];
        int index = 0;
        while (index < length)
        {
            result[index] = bytes[index];
            index = index + 1;
        }

        return result;
    }
}
