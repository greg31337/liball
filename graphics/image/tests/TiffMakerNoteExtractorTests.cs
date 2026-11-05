using System.Collections.Generic;
using System.IO;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class TiffMakerNoteExtractorTests
{
    [Test]
    public void Parse_PreservesMakerNoteAsRawDirectoryProfileAndRawBlock()
    {
        byte[] payload = new byte[] { 78, 105, 107, 111, 110, 0, 1, 2, 3 };

        ImageMetadataDocument document = ParseTiff(CreateMakerNoteTiff(payload), new ImageMetadataReaderOptions());

        MetadataTag exifTag = RequiredMetadataQuery.FindFirst(document, "Exif.ExifIFD.MakerNote");
        MetadataDirectory makerNote = FindDirectory(document, MetadataFamily.MakerNote, "MakerNote0") ?? throw new System.InvalidOperationException("Expected maker-note directory was not found.");
        MetadataTag dataTag = RequiredMetadataQuery.FindFirst(document, "MakerNote.MakerNote0.Data");

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(exifTag, Is.Not.Null);
        Assert.That(exifTag.Value.Kind, Is.EqualTo(MetadataValueKind.ByteArray));
        Assert.That(exifTag.Value.Bytes, Is.EqualTo(payload));
        Assert.That(makerNote, Is.Not.Null);
        Assert.That(makerNote.Offset, Is.EqualTo(44));
        Assert.That(makerNote.Length, Is.EqualTo(payload.Length));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "MakerNote.MakerNote0.SourceFamily").Value.Text, Is.EqualTo("Exif"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "MakerNote.MakerNote0.SourceGroup").Value.Text, Is.EqualTo("ExifIFD"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "MakerNote.MakerNote0.SourceTag").Value.Text, Is.EqualTo("Exif.ExifIFD.MakerNote"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "MakerNote.MakerNote0.Offset").Value.IntegerValue, Is.EqualTo(44));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "MakerNote.MakerNote0.ByteLength").Value.IntegerValue, Is.EqualTo(payload.Length));
        Assert.That(dataTag.Value.Kind, Is.EqualTo(MetadataValueKind.ByteArray));
        Assert.That(dataTag.Value.Bytes, Is.EqualTo(payload));
        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Kind, Is.EqualTo(MetadataProfileKind.MakerNote));
        Assert.That(document.Profiles[0].Family, Is.EqualTo(MetadataFamily.MakerNote));
        Assert.That(document.Profiles[0].Key, Is.EqualTo("MakerNote.MakerNote0"));
        Assert.That(document.Profiles[0].Data, Is.EqualTo(payload));
        Assert.That(document.RawBlocks, Has.Count.EqualTo(1));
        Assert.That(document.RawBlocks[0].Family, Is.EqualTo(MetadataFamily.MakerNote));
        Assert.That(document.RawBlocks[0].BlockType, Is.EqualTo("MakerNote"));
        Assert.That(document.RawBlocks[0].Offset, Is.EqualTo(44));
        Assert.That(document.RawBlocks[0].Length, Is.EqualTo(payload.Length));
        Assert.That(document.RawBlocks[0].Data, Is.Empty);
    }

    [Test]
    public void Parse_CapturesMakerNoteRawBlockDataWhenEnabled()
    {
        byte[] payload = new byte[] { 67, 97, 110, 111, 110, 0, 9, 8, 7 };
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.CaptureRawBlockData = true;

        ImageMetadataDocument document = ParseTiff(CreateMakerNoteTiff(payload), options);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.RawBlocks, Has.Count.EqualTo(1));
        Assert.That(document.RawBlocks[0].Data, Is.EqualTo(payload));
    }

    [Test]
    public void Parse_DoesNotPreserveMakerNoteWhenOptionDisabled()
    {
        byte[] payload = new byte[] { 83, 111, 110, 121, 0, 1, 2, 3 };
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.PreserveMakerNotes = false;

        ImageMetadataDocument document = ParseTiff(CreateMakerNoteTiff(payload), options);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Exif.ExifIFD.MakerNote"), Is.Not.Null);
        Assert.That(FindDirectory(document, MetadataFamily.MakerNote, "MakerNote0"), Is.Null);
        Assert.That(document.Profiles, Is.Empty);
        Assert.That(document.RawBlocks, Is.Empty);
    }

    [Test]
    public void Parse_DoesNotDecodeMakerNoteByDefault()
    {
        ImageMetadataDocument document = ParseTiff(CreateMakerNoteTiffWithMake("Canon", CreateCanonMakerNotePayload()), new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "MakerNote.MakerNote0.Data"), Is.Not.Null);
        Assert.That(new ImageMetadataQuery().FindFirst(document, "MakerNote.MakerNote0.Vendor"), Is.Null);
        Assert.That(new ImageMetadataQuery().FindFirst(document, "MakerNote.MakerNote0.CameraSettings"), Is.Null);
    }

    [Test]
    public void Parse_DecodesCanonDirectMakerNoteWhenEnabled()
    {
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.DecodeMakerNotes = true;

        ImageMetadataDocument document = ParseTiff(CreateMakerNoteTiffWithMake("Canon", CreateCanonMakerNotePayload()), options);

        MetadataTag vendor = RequiredMetadataQuery.FindFirst(document, "MakerNote.MakerNote0.Vendor");
        MetadataTag format = RequiredMetadataQuery.FindFirst(document, "MakerNote.MakerNote0.Format");
        MetadataTag decodedCount = RequiredMetadataQuery.FindFirst(document, "MakerNote.MakerNote0.DecodedTagCount");
        MetadataTag cameraSettings = RequiredMetadataQuery.FindFirst(document, "MakerNote.MakerNote0.CameraSettings");
        MetadataTag firmware = RequiredMetadataQuery.FindFirst(document, "MakerNote.MakerNote0.FirmwareVersion");

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(vendor.Value.Text, Is.EqualTo("Canon"));
        Assert.That(format.Value.Text, Is.EqualTo("Canon IFD"));
        Assert.That(decodedCount.Value.IntegerValue, Is.EqualTo(2));
        Assert.That(cameraSettings.NumericId, Is.EqualTo(1));
        Assert.That(cameraSettings.Value.UnsignedIntegerValue, Is.EqualTo(3));
        Assert.That(firmware.NumericId, Is.EqualTo(7));
        Assert.That(firmware.Value.Text, Is.EqualTo("FW1"));
    }

    [Test]
    public void Parse_DecodesNikonType3MakerNoteWhenEnabled()
    {
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.DecodeMakerNotes = true;

        ImageMetadataDocument document = ParseTiff(CreateMakerNoteTiffWithMake("Nikon", CreateNikonType3MakerNotePayload()), options);

        MetadataTag vendor = RequiredMetadataQuery.FindFirst(document, "MakerNote.MakerNote0.Vendor");
        MetadataTag format = RequiredMetadataQuery.FindFirst(document, "MakerNote.MakerNote0.Format");
        MetadataTag decodedCount = RequiredMetadataQuery.FindFirst(document, "MakerNote.MakerNote0.DecodedTagCount");
        MetadataTag quality = RequiredMetadataQuery.FindFirst(document, "MakerNote.MakerNote0.Quality");

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(vendor.Value.Text, Is.EqualTo("Nikon"));
        Assert.That(format.Value.Text, Is.EqualTo("Nikon TIFF IFD"));
        Assert.That(decodedCount.Value.IntegerValue, Is.EqualTo(1));
        Assert.That(quality.NumericId, Is.EqualTo(4));
        Assert.That(quality.Value.Text, Is.EqualTo("FINE"));
    }

    public static byte[] CreateMakerNoteTiff(byte[] payload)
    {
        List<byte> bytes = new List<byte>();
        bytes.Add(73);
        bytes.Add(73);
        AddUnsigned16(bytes, 42);
        AddUnsigned32(bytes, 8);

        AddUnsigned16(bytes, 1);
        AddEntry(bytes, 34665, 4, 1, 26);
        AddUnsigned32(bytes, 0);

        AddUnsigned16(bytes, 1);
        AddEntry(bytes, 37500, 7, (uint)payload.Length, 44);
        AddUnsigned32(bytes, 0);

        foreach (byte value in payload)
        {
            bytes.Add(value);
        }

        return bytes.ToArray();
    }

    public static byte[] CreateMakerNoteTiffWithMake(string make, byte[] payload)
    {
        List<byte> bytes = new List<byte>();
        bytes.Add(73);
        bytes.Add(73);
        AddUnsigned16(bytes, 42);
        AddUnsigned32(bytes, 8);

        AddUnsigned16(bytes, 2);
        AddEntry(bytes, 271, 2, (uint)(make.Length + 1), 38);
        AddEntry(bytes, 34665, 4, 1, 44);
        AddUnsigned32(bytes, 0);
        AddAscii(bytes, make);

        AddUnsigned16(bytes, 1);
        AddEntry(bytes, 37500, 7, (uint)payload.Length, 62);
        AddUnsigned32(bytes, 0);

        foreach (byte value in payload)
        {
            bytes.Add(value);
        }

        return bytes.ToArray();
    }

    public static byte[] CreateCanonMakerNotePayload()
    {
        List<byte> bytes = new List<byte>();
        AddUnsigned16(bytes, 2);
        AddEntry(bytes, 1, 3, 1, 3);
        AddEntry(bytes, 7, 2, 5, 30);
        AddUnsigned32(bytes, 0);
        bytes.Add(70);
        bytes.Add(87);
        bytes.Add(49);
        bytes.Add(0);
        bytes.Add(0);
        return bytes.ToArray();
    }

    public static byte[] CreateNikonType3MakerNotePayload()
    {
        List<byte> bytes = new List<byte>();
        bytes.Add(78);
        bytes.Add(105);
        bytes.Add(107);
        bytes.Add(111);
        bytes.Add(110);
        bytes.Add(0);
        bytes.Add(2);
        bytes.Add(16);
        bytes.Add(0);
        bytes.Add(0);
        bytes.Add(73);
        bytes.Add(73);
        AddUnsigned16(bytes, 42);
        AddUnsigned32(bytes, 8);
        AddUnsigned16(bytes, 1);
        AddEntry(bytes, 4, 2, 5, 26);
        AddUnsigned32(bytes, 0);
        bytes.Add(70);
        bytes.Add(73);
        bytes.Add(78);
        bytes.Add(69);
        bytes.Add(0);
        return bytes.ToArray();
    }

    public static MetadataDirectory? FindDirectory(ImageMetadataDocument document, MetadataFamily family, string group)
    {
        foreach (MetadataDirectory directory in document.Directories)
        {
            if (directory.Family == family && directory.Group == group)
            {
                return directory;
            }
        }

        return null;
    }

    public static void AddEntry(List<byte> bytes, int tagId, int typeId, uint count, uint value)
    {
        AddUnsigned16(bytes, tagId);
        AddUnsigned16(bytes, typeId);
        AddUnsigned32(bytes, count);
        AddUnsigned32(bytes, value);
    }

    public static void AddAscii(List<byte> bytes, string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            bytes.Add((byte)text[index]);
            index = index + 1;
        }

        bytes.Add(0);
    }

    public static void AddUnsigned16(List<byte> bytes, int value)
    {
        bytes.Add((byte)(value & 255));
        bytes.Add((byte)((value >> 8) & 255));
    }

    public static void AddUnsigned32(List<byte> bytes, uint value)
    {
        bytes.Add((byte)(value & 255));
        bytes.Add((byte)((value >> 8) & 255));
        bytes.Add((byte)((value >> 16) & 255));
        bytes.Add((byte)((value >> 24) & 255));
    }

    public static ImageMetadataDocument ParseTiff(byte[] bytes, ImageMetadataReaderOptions options)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return TiffMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.Tiff), options);
    }
}
