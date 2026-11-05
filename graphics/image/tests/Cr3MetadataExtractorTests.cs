using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class Cr3MetadataExtractorTests
{
    [Test]
    public void Parse_ExtractsCanonTiffFamiliesXmpAndPreviews()
    {
        byte[] bytes = BuildCompleteCr3();
        MemoryStream stream = new MemoryStream(bytes);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(stream);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Cr3));
        Assert.That(document.Format.ContainerBrand, Is.EqualTo("crx "));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmff.CMT1.Type").Value.Text, Is.EqualTo("CMT1"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.Profile.MetadataBoxCount").Value.IntegerValue, Is.EqualTo(7));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Make").Value.Text, Is.EqualTo("Canon"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Exif.ExifIFD.DateTimeOriginal").Value.Text, Is.EqualTo("2026:08:12 12:34:56"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "MakerNote.MakerNoteCanon.FirmwareVersion").Value.Text, Is.EqualTo("1.2.3"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gps.GPS.VersionID"), Is.Not.Null);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.CMT1Header.Magic").Value.IntegerValue, Is.EqualTo(42));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Thumbnail.CR3.THMB.Width").Value.IntegerValue, Is.EqualTo(160));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Thumbnail.CR3.THMB.Height").Value.IntegerValue, Is.EqualTo(120));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Thumbnail.CR3.PRVW.Width").Value.IntegerValue, Is.EqualTo(800));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Thumbnail.CR3.PRVW.Height").Value.IntegerValue, Is.EqualTo(600));
        Assert.That(document.Profiles, Has.Count.EqualTo(7));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Key == "Exif.CR3.CMT1" && profile.Data.Length > 8));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Key == "MakerNote.CR3.CMT3" && profile.Kind == MetadataProfileKind.MakerNote));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Key == "Xmp.CR3.uuid" && profile.Text.Contains("x:xmpmeta")));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Key == "Thumbnail.CR3.THMB" && profile.Data.SequenceEqual(new byte[] { 255, 216, 255, 217 })));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Key == "Thumbnail.CR3.PRVW" && profile.Data.SequenceEqual(new byte[] { 255, 216, 1, 2, 255, 217 })));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Key == "Thumbnail.CR3.THMB" && profile.Role == ImageMetadataRole.Thumbnail));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Key == "Thumbnail.CR3.PRVW" && profile.Role == ImageMetadataRole.Preview));
        Assert.That(document.RawBlocks, Has.Count.EqualTo(7));
        Assert.That(document.RawBlocks, Has.Some.Matches<MetadataRawBlock>(block => block.BlockType == "CMT4" && block.Offset > 0));
        Assert.That(document.Summary.CameraMake, Is.EqualTo("Canon"));
        Assert.That(stream.Position, Is.EqualTo(0));
    }

    [Test]
    public void Parse_InvalidCmtHeaderIsDiagnosedWithoutBlockingOtherCmtBoxes()
    {
        byte[] invalid = CreateBox("CMT1", new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        byte[] valid = CreateBox("CMT2", CreateTiff(36867, 2, Encoding.ASCII.GetBytes("2026:08:12 12:34:56\0")));
        byte[] bytes = BuildCr3(Join(invalid, valid));

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(HasDiagnostic(document, "cr3.cmt.tiff.header.invalid"), Is.True);
        Assert.That(new ImageMetadataQuery().FindFirst(document, "Tiff.IFD0.Make"), Is.Null);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Exif.ExifIFD.DateTimeOriginal"), Is.Not.Null);
        Assert.That(document.RawBlocks, Has.Count.EqualTo(2));
        Assert.That(document.Profiles, Has.Count.EqualTo(1));
    }

    [Test]
    public void Parse_DisabledMakerNotePreservationSkipsCmt3()
    {
        byte[] cmt1 = CreateBox("CMT1", CreateTiff(271, 2, Encoding.ASCII.GetBytes("Canon\0")));
        byte[] cmt3 = CreateBox("CMT3", CreateTiff(7, 2, Encoding.ASCII.GetBytes("1.2.3\0")));
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.PreserveMakerNotes = false;

        ImageMetadataDocument document = Parse(BuildCr3(Join(cmt1, cmt3)), options);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Make"), Is.Not.Null);
        Assert.That(new ImageMetadataQuery().FindFirst(document, "MakerNote.MakerNoteCanon.FirmwareVersion"), Is.Null);
        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.RawBlocks, Has.Count.EqualTo(1));
    }

    [Test]
    public void Parse_ProfileAndRawLimitsDoNotPreventTiffDecoding()
    {
        byte[] cmt1 = CreateBox("CMT1", CreateTiff(271, 2, Encoding.ASCII.GetBytes("Canon\0")));
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxProfileBytes = 4;
        options.MaxRawBlockBytes = 4;
        options.CaptureRawBlockData = true;

        ImageMetadataDocument document = Parse(BuildCr3(cmt1), options);

        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Make").Value.Text, Is.EqualTo("Canon"));
        Assert.That(HasDiagnostic(document, "cr3.cmt.profile.limit"), Is.True);
        Assert.That(HasDiagnostic(document, "cr3.raw.block.limit"), Is.True);
        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Data, Is.Empty);
        Assert.That(document.RawBlocks, Has.Count.EqualTo(1));
        Assert.That(document.RawBlocks[0].Data, Is.Empty);
    }

    [Test]
    public void BoxTraversal_OnlyTreatsKnownCanonUuidAsAContainer()
    {
        byte[] cmt1 = CreateBox("CMT1", CreateTiff(271, 2, Encoding.ASCII.GetBytes("Canon\0")));
        byte[] unknownUuid = CreateUuid("000102030405060708090a0b0c0d0e0f", cmt1);
        byte[] bytes = Join(CreateFtyp(), CreateBox("moov", unknownUuid));

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Cr3));
        Assert.That(new ImageMetadataQuery().FindFirst(document, "Bmff.CMT1.Type"), Is.Null);
        Assert.That(new ImageMetadataQuery().FindFirst(document, "Tiff.IFD0.Make"), Is.Null);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.Profile.MetadataBoxCount").Value.IntegerValue, Is.EqualTo(0));
    }

    [Test]
    public void ParseXmpFalseSkipsCr3XmpUuid()
    {
        byte[] xmp = Encoding.UTF8.GetBytes("<x:xmpmeta></x:xmpmeta>");
        byte[] bytes = Join(CreateFtyp(), CreateUuid(Cr3MetadataExtractor.XmpUuidHex, xmp));
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.ParseXmp = false;

        ImageMetadataDocument document = Parse(bytes, options);

        Assert.That(document.Profiles, Is.Empty);
        Assert.That(document.RawBlocks, Is.Empty);
    }

    public static byte[] BuildCompleteCr3()
    {
        byte[] cmt1 = CreateBox("CMT1", CreateTiff(271, 2, Encoding.ASCII.GetBytes("Canon\0")));
        byte[] cmt2 = CreateBox("CMT2", CreateTiff(36867, 2, Encoding.ASCII.GetBytes("2026:08:12 12:34:56\0")));
        byte[] cmt3 = CreateBox("CMT3", CreateTiff(7, 2, Encoding.ASCII.GetBytes("1.2.3\0")));
        byte[] cmt4 = CreateBox("CMT4", CreateTiff(0, 1, new byte[] { 2, 3, 0, 0 }));
        byte[] thumbnail = CreateThumbnailBox(160, 120, new byte[] { 255, 216, 255, 217 });
        byte[] canonUuid = CreateUuid(Cr3MetadataExtractor.CanonMetadataUuidHex, Join(cmt1, cmt2, cmt3, cmt4, thumbnail));
        byte[] moov = CreateBox("moov", canonUuid);
        byte[] xmp = CreateUuid(Cr3MetadataExtractor.XmpUuidHex, Encoding.UTF8.GetBytes("<x:xmpmeta></x:xmpmeta>"));
        byte[] preview = CreatePreviewUuid(800, 600, new byte[] { 255, 216, 1, 2, 255, 217 });
        return Join(CreateFtyp(), moov, xmp, preview);
    }

    public static byte[] BuildCr3(byte[] canonChildren)
    {
        byte[] uuid = CreateUuid(Cr3MetadataExtractor.CanonMetadataUuidHex, canonChildren);
        return Join(CreateFtyp(), CreateBox("moov", uuid));
    }

    public static byte[] CreateFtyp()
    {
        return CreateBox("ftyp", Join(Encoding.ASCII.GetBytes("crx "), new byte[] { 0, 0, 0, 1 }, Encoding.ASCII.GetBytes("crx "), Encoding.ASCII.GetBytes("isom")));
    }

    public static byte[] CreateTiff(int tagId, int typeId, byte[] value)
    {
        List<byte> bytes = new List<byte>();
        bytes.Add(73);
        bytes.Add(73);
        AddUnsigned16Little(bytes, 42);
        AddUnsigned32Little(bytes, 8);
        AddUnsigned16Little(bytes, 1);
        AddUnsigned16Little(bytes, tagId);
        AddUnsigned16Little(bytes, typeId);
        AddUnsigned32Little(bytes, (uint)value.Length);
        if (value.Length <= 4)
        {
            bytes.AddRange(value);
            while (bytes.Count < 22)
            {
                bytes.Add(0);
            }
        }
        else
        {
            AddUnsigned32Little(bytes, 26);
        }
        AddUnsigned32Little(bytes, 0);
        if (value.Length > 4)
        {
            bytes.AddRange(value);
        }
        return bytes.ToArray();
    }

    public static byte[] CreateThumbnailBox(int width, int height, byte[] image)
    {
        List<byte> payload = new List<byte>();
        AddUnsigned32(payload, 0);
        AddUnsigned16(payload, (uint)width);
        AddUnsigned16(payload, (uint)height);
        AddUnsigned32(payload, (uint)image.Length);
        AddUnsigned32(payload, 0);
        payload.AddRange(image);
        return CreateBox("THMB", payload.ToArray());
    }

    public static byte[] CreatePreviewUuid(int width, int height, byte[] image)
    {
        byte[] header = new byte[Cr3MetadataExtractor.PreviewHeaderLength];
        header[7] = 1;
        header[11] = (byte)(Cr3MetadataExtractor.PreviewHeaderLength + image.Length);
        header[12] = 80;
        header[13] = 82;
        header[14] = 86;
        header[15] = 87;
        header[20] = 0;
        header[21] = 1;
        WriteUnsigned16(header, 22, width);
        WriteUnsigned16(header, 24, height);
        header[26] = 0;
        header[27] = 1;
        WriteUnsigned32(header, 28, image.Length);
        return CreateUuid(Cr3MetadataExtractor.PreviewUuidHex, Join(header, image));
    }

    public static byte[] CreateUuid(string hex, byte[] payload)
    {
        return CreateBox("uuid", Join(HexBytes(hex), payload));
    }

    public static byte[] CreateBox(string type, byte[] payload)
    {
        List<byte> bytes = new List<byte>();
        AddUnsigned32(bytes, (uint)(8 + payload.Length));
        bytes.AddRange(Encoding.ASCII.GetBytes(type));
        bytes.AddRange(payload);
        return bytes.ToArray();
    }

    public static byte[] Join(params byte[][] values)
    {
        List<byte> bytes = new List<byte>();
        foreach (byte[] value in values)
        {
            bytes.AddRange(value);
        }
        return bytes.ToArray();
    }

    public static byte[] HexBytes(string hex)
    {
        byte[] bytes = new byte[hex.Length / 2];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = Convert.ToByte(hex.Substring(index * 2, 2), 16);
            index = index + 1;
        }
        return bytes;
    }

    public static void AddUnsigned16(List<byte> bytes, uint value)
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

    public static void AddUnsigned16Little(List<byte> bytes, int value)
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

    public static void WriteUnsigned16(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
    }

    public static void WriteUnsigned32(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }

    public static ImageMetadataDocument Parse(byte[] bytes, ImageMetadataReaderOptions options)
    {
        return new ImageMetadataReader().ReadStreamWithOptions(new MemoryStream(bytes), options);
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
}
