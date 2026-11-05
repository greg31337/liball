using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class RafMetadataParserTests
{
    [Test]
    public void Parse_ExtractsWrapperDirectoryJpegAndTiffMetadata()
    {
        byte[] jpeg = CreateJpegWithExif("FUJIFILM", 640, 480);
        byte[] rafDirectory = CreateRafDirectory(new RafTestEntry(0x0100, Join(Unsigned16(3000), Unsigned16(4000))));
        byte[] tiff = CreateTiff(305, "RAF TIFF");
        byte[] bytes = CreateRaf(jpeg, rafDirectory, tiff, new byte[0], new byte[0]);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Raf));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.RAF.Header.CameraModel").Value.Text, Is.EqualTo("FUJIFILM X-T5"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.RAF.Header.DirectoryVersion").Value.Text, Is.EqualTo("0103"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.Profile.HeaderVariant").Value.Text, Is.EqualTo("FUJIFILMCCD-RAW"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.RAF.RAF.RawImageFullSize").Value.UnsignedIntegerValues, Is.EqualTo(new ulong[] { 4000, 3000 }));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Make").Value.Text, Is.EqualTo("FUJIFILM"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Software").Value.Text, Is.EqualTo("RAF TIFF"));
        Assert.That(document.Summary.CameraMake, Is.EqualTo("FUJIFILM"));
        Assert.That(document.Summary.Width, Is.EqualTo(4000));
        Assert.That(document.Summary.Height, Is.EqualTo(3000));
        Assert.That(document.Frames, Has.Count.EqualTo(2));
        Assert.That(document.Frames[1].Width, Is.EqualTo(640));
        Assert.That(document.Frames[1].Height, Is.EqualTo(480));
        Assert.That(document.Frames[1].Role, Is.EqualTo(ImageMetadataRole.Preview));
        Assert.That(document.Frames[1].Directories[0].Path, Does.StartWith("RAF/Preview/JPEG/"));
        Assert.That(document.Frames[1].Directories[0].FrameIndex, Is.EqualTo(1));
        Assert.That(document.Directories.Any(item => item.Path.StartsWith("RAF/FujiIFD/TIFF/")), Is.True);
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(item => item.Key == "Thumbnail.RAF.Preview" && item.Data.SequenceEqual(jpeg)));
        MetadataProfile preview = document.Profiles.Find(item => item.Key == "Thumbnail.RAF.Preview") ?? throw new System.InvalidOperationException("Expected RAF preview profile was not found.");
        Assert.That(preview.Role, Is.EqualTo(ImageMetadataRole.Preview));
        Assert.That(preview.SourceFrameIndex, Is.EqualTo(1));
        Assert.That(document.RawBlocks, Has.Some.Matches<MetadataRawBlock>(item => item.BlockType == "JPEG"));
        Assert.That(document.RawBlocks, Has.Some.Matches<MetadataRawBlock>(item => item.BlockType == "FujiIFD"));
    }

    [Test]
    public void Parse_DiscoversSecondaryRafAndTiffRanges()
    {
        byte[] jpeg = CreateJpeg(80, 60);
        byte[] firstDirectory = CreateRafDirectory(new RafTestEntry(0x0111, Join(Unsigned16(2000), Unsigned16(3000))));
        byte[] firstTiff = CreateTiff(305, "First");
        byte[] secondDirectory = CreateRafDirectory(new RafTestEntry(0x0117, Unsigned32(1)));
        byte[] secondTiff = CreateTiff(315, "Second Artist");
        byte[] bytes = CreateRaf(jpeg, firstDirectory, firstTiff, secondDirectory, secondTiff);

        ImageMetadataDocument document = Parse(bytes, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.RAF.RAF1.RawZoomActive").Value.UnsignedIntegerValue, Is.EqualTo(1));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Software").Value.Text, Is.EqualTo("First"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Artist").Value.Text, Is.EqualTo("Second Artist"));
        Assert.That(document.Directories.Count(item => item.Group == "Header" && item.Family == MetadataFamily.Tiff), Is.EqualTo(2));
    }

    [Test]
    public void Parse_InvalidPreviewRangeDoesNotBlockValidTiff()
    {
        byte[] tiff = CreateTiff(305, "Still Parsed");
        byte[] bytes = CreateRaf(new byte[0], new byte[0], tiff, new byte[0], new byte[0]);
        WriteUnsigned32(bytes, RafMetadataParser.PreviewDescriptorOffset, (uint)(bytes.Length + 20));
        WriteUnsigned32(bytes, RafMetadataParser.PreviewDescriptorOffset + 4, 100);

        ImageMetadataDocument document = Parse(bytes, new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, "raf.preview.range.invalid"), Is.True);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Software").Value.Text, Is.EqualTo("Still Parsed"));
    }

    [Test]
    public void Parse_NonTiffFujiRangeIsIgnoredForOlderRafLayouts()
    {
        byte[] bytes = CreateRaf(CreateJpeg(40, 30), new byte[0], new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, new byte[0], new byte[0]);

        ImageMetadataDocument document = Parse(bytes, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Directories.Any(item => item.Family == MetadataFamily.Tiff), Is.False);
    }

    [Test]
    public void Parse_TruncatedHeaderReportsDiagnosticWithoutThrowing()
    {
        byte[] bytes = Encoding.ASCII.GetBytes("FUJIFILMCCD-RAW ");

        ImageMetadataDocument document = Parse(bytes, new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, "raf.header.truncated"), Is.True);
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Raf));
        Assert.That(document.Directories, Is.Empty);
    }

    [Test]
    public void Parse_HeaderWithoutPayloadNormalizesCameraIdentity()
    {
        ImageMetadataDocument document = Parse(RawMetadataDispatcherTests.RafHeader(), new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Summary.CameraMake, Is.EqualTo("FUJIFILM"));
        Assert.That(document.Summary.CameraModel, Is.EqualTo("FUJIFILM X-T5"));
    }

    [Test]
    public void Parse_MalformedRafDirectoryValueIsDiagnosed()
    {
        byte[] directory = CreateRafDirectory(new RafTestEntry(0x0100, Join(Unsigned16(10), Unsigned16(20))));
        WriteUnsigned16(directory, 6, 1000);
        byte[] bytes = CreateRaf(new byte[0], directory, new byte[0], new byte[0], new byte[0]);

        ImageMetadataDocument document = Parse(bytes, new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, "raf.directory.value.truncated"), Is.True);
        Assert.That(document.Directories.Any(item => item.Group == "RAF"), Is.True);
    }

    [Test]
    public void Parse_PreviewProfileAndRawCaptureRespectLimits()
    {
        byte[] jpeg = CreateJpeg(80, 60);
        byte[] bytes = CreateRaf(jpeg, new byte[0], new byte[0], new byte[0], new byte[0]);
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxProfileBytes = 4;
        options.MaxRawBlockBytes = 4;
        options.CaptureRawBlockData = true;

        ImageMetadataDocument document = Parse(bytes, options);

        MetadataProfile preview = document.Profiles.Single(item => item.Key == "Thumbnail.RAF.Preview");
        Assert.That(preview.Data, Is.Empty);
        Assert.That(HasDiagnostic(document, "raf.preview.profile.limit"), Is.True);
        Assert.That(HasDiagnostic(document, "raf.raw.block.limit"), Is.True);
        Assert.That(document.Frames, Has.Count.EqualTo(1));
    }

    [Test]
    public void Parse_DisabledRawCaptureStillParsesEmbeddedMetadata()
    {
        byte[] bytes = CreateRaf(CreateJpegWithExif("FUJIFILM", 32, 24), new byte[0], CreateTiff(305, "Parsed"), new byte[0], new byte[0]);
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.CaptureRawBlocks = false;

        ImageMetadataDocument document = Parse(bytes, options);

        Assert.That(document.RawBlocks, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Make").Value.Text, Is.EqualTo("FUJIFILM"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Software").Value.Text, Is.EqualTo("Parsed"));
    }

    public static ImageMetadataDocument Parse(byte[] bytes, ImageMetadataReaderOptions options)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return RafMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.Raf), options);
    }

    public static byte[] CreateRaf(byte[] jpeg, byte[] rafDirectory, byte[] tiff, byte[] secondDirectory, byte[] secondTiff)
    {
        byte[] header = new byte[RafMetadataParser.MaximumHeaderLength];
        Encoding.ASCII.GetBytes("FUJIFILMCCD-RAW ").CopyTo(header, 0);
        Encoding.ASCII.GetBytes("0201").CopyTo(header, 16);
        Encoding.ASCII.GetBytes("TEST0001").CopyTo(header, 20);
        Encoding.ASCII.GetBytes("FUJIFILM X-T5").CopyTo(header, 28);
        Encoding.ASCII.GetBytes("0103").CopyTo(header, 60);
        List<byte> bytes = new List<byte>(header);
        int jpegOffset = Append(bytes, jpeg);
        int rafOffset = Append(bytes, rafDirectory);
        int tiffOffset = Append(bytes, tiff);
        int secondDirectoryOffset = Append(bytes, secondDirectory);
        int secondTiffOffset = Append(bytes, secondTiff);
        WriteDescriptor(bytes, RafMetadataParser.PreviewDescriptorOffset, jpegOffset, jpeg.Length);
        WriteDescriptor(bytes, RafMetadataParser.RafDescriptorOffset, rafOffset, rafDirectory.Length);
        WriteDescriptor(bytes, RafMetadataParser.FujiIfdDescriptorOffset, tiffOffset, tiff.Length);
        WriteDescriptor(bytes, RafMetadataParser.RafSecondDescriptorOffset, secondDirectoryOffset, secondDirectory.Length);
        WriteDescriptor(bytes, RafMetadataParser.FujiIfdSecondDescriptorOffset, secondTiffOffset, secondTiff.Length);
        return bytes.ToArray();
    }

    public static int Append(List<byte> bytes, byte[] value)
    {
        if (value.Length == 0)
        {
            return 0;
        }
        int offset = bytes.Count;
        bytes.AddRange(value);
        return offset;
    }

    public static byte[] CreateRafDirectory(params RafTestEntry[] entries)
    {
        List<byte> bytes = new List<byte>();
        AddUnsigned32(bytes, (uint)entries.Length);
        foreach (RafTestEntry entry in entries)
        {
            AddUnsigned16(bytes, entry.Tag);
            AddUnsigned16(bytes, (ushort)entry.Value.Length);
            bytes.AddRange(entry.Value);
        }
        return bytes.ToArray();
    }

    public static byte[] CreateJpegWithExif(string make, int width, int height)
    {
        byte[] exif = Join(Encoding.ASCII.GetBytes("Exif\0\0"), CreateTiff(271, make));
        List<byte> bytes = new List<byte>();
        bytes.Add(255);
        bytes.Add(216);
        bytes.Add(255);
        bytes.Add(225);
        AddUnsigned16(bytes, (ushort)(exif.Length + 2));
        bytes.AddRange(exif);
        AddSof(bytes, width, height);
        bytes.Add(255);
        bytes.Add(217);
        return bytes.ToArray();
    }

    public static byte[] CreateJpeg(int width, int height)
    {
        List<byte> bytes = new List<byte>();
        bytes.Add(255);
        bytes.Add(216);
        AddSof(bytes, width, height);
        bytes.Add(255);
        bytes.Add(217);
        return bytes.ToArray();
    }

    public static void AddSof(List<byte> bytes, int width, int height)
    {
        bytes.Add(255);
        bytes.Add(192);
        AddUnsigned16(bytes, 11);
        bytes.Add(8);
        AddUnsigned16(bytes, (ushort)height);
        AddUnsigned16(bytes, (ushort)width);
        bytes.Add(1);
        bytes.Add(1);
        bytes.Add(17);
        bytes.Add(0);
    }

    public static byte[] CreateTiff(ushort tagId, string text)
    {
        byte[] value = Encoding.ASCII.GetBytes(text + "\0");
        List<byte> bytes = new List<byte>();
        bytes.Add(73);
        bytes.Add(73);
        bytes.Add(42);
        bytes.Add(0);
        AddUnsigned32Little(bytes, 8);
        AddUnsigned16Little(bytes, 1);
        AddUnsigned16Little(bytes, tagId);
        AddUnsigned16Little(bytes, 2);
        AddUnsigned32Little(bytes, (uint)value.Length);
        AddUnsigned32Little(bytes, 26);
        AddUnsigned32Little(bytes, 0);
        bytes.AddRange(value);
        return bytes.ToArray();
    }

    public static void WriteDescriptor(List<byte> bytes, int offset, int valueOffset, int valueLength)
    {
        WriteUnsigned32(bytes, offset, (uint)valueOffset);
        WriteUnsigned32(bytes, offset + 4, (uint)valueLength);
    }

    public static void WriteUnsigned32(List<byte> bytes, int offset, uint value)
    {
        byte[] encoded = Unsigned32(value);
        for (int index = 0; index < encoded.Length; index++)
        {
            bytes[offset + index] = encoded[index];
        }
    }

    public static void WriteUnsigned32(byte[] bytes, int offset, uint value)
    {
        byte[] encoded = Unsigned32(value);
        for (int index = 0; index < encoded.Length; index++)
        {
            bytes[offset + index] = encoded[index];
        }
    }

    public static void WriteUnsigned16(byte[] bytes, int offset, ushort value)
    {
        bytes[offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
    }

    public static byte[] Unsigned16(ushort value)
    {
        return new byte[] { (byte)(value >> 8), (byte)value };
    }

    public static byte[] Unsigned32(uint value)
    {
        return new byte[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value };
    }

    public static void AddUnsigned16(List<byte> bytes, ushort value)
    {
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)value);
    }

    public static void AddUnsigned32(List<byte> bytes, uint value)
    {
        bytes.Add((byte)(value >> 24));
        bytes.Add((byte)(value >> 16));
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)value);
    }

    public static void AddUnsigned16Little(List<byte> bytes, ushort value)
    {
        bytes.Add((byte)value);
        bytes.Add((byte)(value >> 8));
    }

    public static void AddUnsigned32Little(List<byte> bytes, uint value)
    {
        bytes.Add((byte)value);
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)(value >> 16));
        bytes.Add((byte)(value >> 24));
    }

    public static byte[] Join(params byte[][] arrays)
    {
        List<byte> bytes = new List<byte>();
        foreach (byte[] array in arrays)
        {
            bytes.AddRange(array);
        }
        return bytes.ToArray();
    }

    public static bool HasDiagnostic(ImageMetadataDocument document, string code)
    {
        return document.Diagnostics.Any(item => item.Code == code);
    }
}

public class RafTestEntry
{
    public ushort Tag;
    public byte[] Value;

    public RafTestEntry(ushort tag, byte[] value)
    {
        Tag = tag;
        Value = value;
    }
}
