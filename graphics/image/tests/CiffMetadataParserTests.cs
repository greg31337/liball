using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class CiffMetadataParserTests
{
    [Test]
    public void Parse_TraversesNestedDirectoriesAndDecodesCiffValueTypes()
    {
        bool littleEndian = true;
        byte[] imageInfo = Join(
            Unsigned32(4000, littleEndian),
            Unsigned32(3000, littleEndian),
            Unsigned32(0, littleEndian),
            Unsigned32(90, littleEndian));
        byte[] camera = CreateDirectory(littleEndian,
            External(0x080a, Encoding.ASCII.GetBytes("Canon\0EOS 10D\0")),
            Inline(0x080b, Encoding.ASCII.GetBytes("1.2.3\0")),
            External(0x1810, imageInfo));
        byte[] root = CreateDirectory(littleEndian,
            External(0x2807, camera),
            Inline(0x100a, Unsigned16(1, littleEndian)),
            External(0x0810, Encoding.ASCII.GetBytes("Ada\0")));

        ImageMetadataDocument document = Parse(CreateCrw(littleEndian, 26, root), new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Crw));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.CIFF.Header.ByteOrder").Value.Text, Is.EqualTo("LittleEndian"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.Profile.Container").Value.Text, Is.EqualTo("CIFF"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.CIFF.Root.OwnerName").Value.Text, Is.EqualTo("Ada"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.CIFF.CameraObject.CanonFirmwareVersion").Value.Text, Is.EqualTo("1.2.3"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.CIFF.Root.TargetImageType").Value.UnsignedIntegerValues, Is.EqualTo(new ulong[] { 1, 0, 0, 0 }));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Make").Value.Text, Is.EqualTo("Canon"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Model").Value.Text, Is.EqualTo("EOS 10D"));
        Assert.That(document.Summary.CameraMake, Is.EqualTo("Canon"));
        Assert.That(document.Summary.CameraModel, Is.EqualTo("EOS 10D"));
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].Width, Is.EqualTo(4000));
        Assert.That(document.Frames[0].Height, Is.EqualTo(3000));
        MetadataDirectory rootDirectory = document.Directories.Single(item => item.Group == "Root");
        Assert.That(rootDirectory.Children, Has.Count.EqualTo(1));
        Assert.That(rootDirectory.Children[0].Group, Is.EqualTo("CameraObject"));
        Assert.That(rootDirectory.Children[0].Path, Is.EqualTo("CIFF/Root/CameraObject"));
        Assert.That(rootDirectory.Children[0].Tags, Has.Count.EqualTo(3));
    }

    [Test]
    public void Parse_HonorsBigEndianHeaderDirectoryAndValues()
    {
        bool littleEndian = false;
        byte[] root = CreateDirectory(littleEndian,
            External(0x0810, Encoding.ASCII.GetBytes("Grace\0")),
            External(0x1804, Unsigned32(0x12345678, littleEndian)));

        ImageMetadataDocument document = Parse(CreateCrw(littleEndian, 14, root), new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.CIFF.Header.ByteOrder").Value.Text, Is.EqualTo("BigEndian"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.CIFF.Root.OwnerName").Value.Text, Is.EqualTo("Grace"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.CIFF.Root.RecordID").Value.UnsignedIntegerValue, Is.EqualTo(0x12345678));
    }

    [Test]
    public void Parse_InvalidEntryRangeIsDiagnosedAndLaterEntriesRemainAvailable()
    {
        bool littleEndian = true;
        byte[] root = CreateDirectory(littleEndian,
            External(0x0815, Encoding.ASCII.GetBytes("RAW\0")),
            Inline(0x0810, Encoding.ASCII.GetBytes("Ada\0")));
        int tableOffset = (int)ReadUnsigned32(root, root.Length - 4, littleEndian);
        WriteUnsigned32(root, tableOffset + 4, 1000, littleEndian);

        ImageMetadataDocument document = Parse(CreateCrw(littleEndian, 14, root), new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, "ciff.entry.range.invalid"), Is.True);
        Assert.That(new ImageMetadataQuery().FindFirst(document, "Raw.CIFF.Root.CanonImageType"), Is.Null);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.CIFF.Root.OwnerName").Value.Text, Is.EqualTo("Ada"));
    }

    [Test]
    public void Parse_TruncatedDirectoryFooterReturnsHeaderAndDiagnostic()
    {
        byte[] bytes = CreateCrw(true, 14, new byte[] { 0, 0, 0 });

        ImageMetadataDocument document = Parse(bytes, new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, "ciff.directory.footer.truncated"), Is.True);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.CIFF.Header.Signature").Value.Text, Is.EqualTo("HEAPCCDR"));
        Assert.That(document.Directories.Any(item => item.Group == "Root"), Is.False);
    }

    [Test]
    public void Parse_NestingLimitStopsChildWithoutDiscardingRootEntries()
    {
        bool littleEndian = true;
        byte[] child = CreateDirectory(littleEndian, Inline(0x0810, Encoding.ASCII.GetBytes("Ada\0")));
        byte[] root = CreateDirectory(littleEndian,
            External(0x2807, child),
            Inline(0x0815, Encoding.ASCII.GetBytes("CRW\0")));
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxNestingDepth = 1;

        ImageMetadataDocument document = Parse(CreateCrw(littleEndian, 14, root), options);

        Assert.That(HasDiagnostic(document, "ciff.directory.depth.limit"), Is.True);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.CIFF.Root.CanonImageType").Value.Text, Is.EqualTo("CRW"));
        Assert.That(new ImageMetadataQuery().FindFirst(document, "Raw.CIFF.CameraObject.OwnerName"), Is.Null);
    }

    [Test]
    public void Parse_PreservesJpegThumbnailProfileAndHonorsRawCapture()
    {
        byte[] jpeg = new byte[] { 255, 216, 1, 2, 255, 217 };
        byte[] root = CreateDirectory(true, External(0x2008, jpeg));
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.CaptureRawBlockData = true;

        ImageMetadataDocument document = Parse(CreateCrw(true, 14, root), options);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Kind, Is.EqualTo(MetadataProfileKind.Thumbnail));
        Assert.That(document.Profiles[0].Key, Is.EqualTo("Thumbnail.CIFF.ThumbnailImage"));
        Assert.That(document.Profiles[0].Data, Is.EqualTo(jpeg));
        Assert.That(document.RawBlocks, Has.Some.Matches<MetadataRawBlock>(block => block.BlockType == "ThumbnailImage" && block.Data.SequenceEqual(jpeg)));
    }

    [Test]
    public void Parse_DoesNotBufferRawSensorPayloadAsTagData()
    {
        byte[] raw = new byte[1024];
        byte[] root = CreateDirectory(true, External(0x2005, raw));
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxSegmentBytes = 1;

        ImageMetadataDocument document = Parse(CreateCrw(true, 14, root), options);

        MetadataTag tag = RequiredMetadataQuery.FindFirst(document, "Raw.CIFF.Root.RawData");
        Assert.That(tag, Is.Not.Null);
        Assert.That(tag.Value.Bytes, Is.Empty);
        Assert.That(tag.RawValue, Is.Empty);
        Assert.That(tag.DisplayValue, Is.EqualTo("Binary data 1024 bytes"));
        Assert.That(HasDiagnostic(document, "ciff.value.limit"), Is.False);
    }

    [Test]
    public void Parse_TagCountLimitStopsDirectoryBeforeReadingEntries()
    {
        byte[] root = CreateDirectory(true,
            Inline(0x0810, Encoding.ASCII.GetBytes("Ada\0")),
            Inline(0x0815, Encoding.ASCII.GetBytes("CRW\0")));
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxTagCount = 1;

        ImageMetadataDocument document = Parse(CreateCrw(true, 14, root), options);

        Assert.That(HasDiagnostic(document, "ciff.directory.tag.count.limit"), Is.True);
        Assert.That(new ImageMetadataQuery().FindFirst(document, "Raw.CIFF.Root.OwnerName"), Is.Null);
    }

    public static ImageMetadataDocument Parse(byte[] bytes, ImageMetadataReaderOptions options)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return CiffMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.Crw), options);
    }

    public static byte[] CreateCrw(bool littleEndian, int heapOffset, byte[] root)
    {
        List<byte> bytes = new List<byte>();
        bytes.Add(littleEndian ? (byte)73 : (byte)77);
        bytes.Add(littleEndian ? (byte)73 : (byte)77);
        AddUnsigned32(bytes, (uint)heapOffset, littleEndian);
        bytes.AddRange(Encoding.ASCII.GetBytes("HEAPCCDR"));
        while (bytes.Count < heapOffset)
        {
            bytes.Add(0);
        }
        bytes.AddRange(root);
        return bytes.ToArray();
    }

    public static byte[] CreateDirectory(bool littleEndian, params CiffTestEntry[] entries)
    {
        List<byte> values = new List<byte>();
        List<byte[]> records = new List<byte[]>();
        foreach (CiffTestEntry entry in entries)
        {
            List<byte> record = new List<byte>();
            if (entry.Inline)
            {
                AddUnsigned16(record, (ushort)(entry.Tag | 0x4000), littleEndian);
                record.AddRange(entry.Data.Take(8));
                while (record.Count < 10)
                {
                    record.Add(0);
                }
            }
            else
            {
                int valueOffset = values.Count;
                values.AddRange(entry.Data);
                AddUnsigned16(record, entry.Tag, littleEndian);
                AddUnsigned32(record, (uint)entry.Data.Length, littleEndian);
                AddUnsigned32(record, (uint)valueOffset, littleEndian);
            }
            records.Add(record.ToArray());
        }

        int tableOffset = values.Count;
        AddUnsigned16(values, (ushort)records.Count, littleEndian);
        foreach (byte[] record in records)
        {
            values.AddRange(record);
        }
        AddUnsigned32(values, (uint)tableOffset, littleEndian);
        return values.ToArray();
    }

    public static CiffTestEntry External(ushort tag, byte[] data)
    {
        return new CiffTestEntry(tag, data, false);
    }

    public static CiffTestEntry Inline(ushort tag, byte[] data)
    {
        return new CiffTestEntry(tag, data, true);
    }

    public static byte[] Unsigned16(ushort value, bool littleEndian)
    {
        List<byte> bytes = new List<byte>();
        AddUnsigned16(bytes, value, littleEndian);
        return bytes.ToArray();
    }

    public static byte[] Unsigned32(uint value, bool littleEndian)
    {
        List<byte> bytes = new List<byte>();
        AddUnsigned32(bytes, value, littleEndian);
        return bytes.ToArray();
    }

    public static void AddUnsigned16(List<byte> bytes, ushort value, bool littleEndian)
    {
        if (littleEndian)
        {
            bytes.Add((byte)value);
            bytes.Add((byte)(value >> 8));
        }
        else
        {
            bytes.Add((byte)(value >> 8));
            bytes.Add((byte)value);
        }
    }

    public static void AddUnsigned32(List<byte> bytes, uint value, bool littleEndian)
    {
        if (littleEndian)
        {
            bytes.Add((byte)value);
            bytes.Add((byte)(value >> 8));
            bytes.Add((byte)(value >> 16));
            bytes.Add((byte)(value >> 24));
        }
        else
        {
            bytes.Add((byte)(value >> 24));
            bytes.Add((byte)(value >> 16));
            bytes.Add((byte)(value >> 8));
            bytes.Add((byte)value);
        }
    }

    public static uint ReadUnsigned32(byte[] bytes, int offset, bool littleEndian)
    {
        if (littleEndian)
        {
            return (uint)bytes[offset] | (uint)bytes[offset + 1] << 8 | (uint)bytes[offset + 2] << 16 | (uint)bytes[offset + 3] << 24;
        }
        return (uint)bytes[offset] << 24 | (uint)bytes[offset + 1] << 16 | (uint)bytes[offset + 2] << 8 | bytes[offset + 3];
    }

    public static void WriteUnsigned32(byte[] bytes, int offset, uint value, bool littleEndian)
    {
        byte[] encoded = Unsigned32(value, littleEndian);
        for (int index = 0; index < encoded.Length; index++)
        {
            bytes[offset + index] = encoded[index];
        }
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

public class CiffTestEntry
{
    public ushort Tag;
    public byte[] Data;
    public bool Inline;

    public CiffTestEntry(ushort tag, byte[] data, bool inline)
    {
        Tag = tag;
        Data = data;
        Inline = inline;
    }
}
