using raw;

namespace raw.Tests;

public class NefParserTests
{
    [Test]
    public void OpenUsesNikonEvidenceAndDiscoversEveryNefImageAndPrivateBlock()
    {
        NefFixture fixture = BuildNef();

        RawDocument document = RawCodec.Open(fixture.Bytes, null, new RawOpenOptions());
        RawImageInfo? primary = (document.Images ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(image => image.Kind == RawImageKind.Raw);
        RawImageInfo secondary = document.Images.Single(image => image.Kind == RawImageKind.Auxiliary);
        RawPayloadBlock capture = document.PayloadMap.Find(RawPayloadKind.Metadata).Single(block => block.Path.EndsWith("/NikonCaptureData", StringComparison.Ordinal));
        RawPayloadBlock edit = document.PayloadMap.Find(RawPayloadKind.Metadata).Single(block => block.Path.EndsWith("/NikonCaptureEditVersions", StringComparison.Ordinal));

        Assert.Multiple(() =>
        {
            Assert.That(document.Format, Is.EqualTo(RawFormat.Nef));
            Assert.That(document.Metadata.Make, Is.EqualTo("NIKON CORPORATION"));
            Assert.That(document.Metadata.Model, Is.EqualTo("NIKON Z 8"));
            Assert.That(document.Metadata.MakerNotes.Single().Signature, Is.EqualTo("NikonEmbeddedTiff"));
            Assert.That(document.Images.Count(image => image.Kind == RawImageKind.Preview), Is.EqualTo(1));
            Assert.That(document.Images.Count(image => image.Kind == RawImageKind.Thumbnail), Is.EqualTo(1));
            Assert.That((primary.Payload ?? throw new System.InvalidOperationException("A required value is unavailable.")).Kind, Is.EqualTo(RawPayloadKind.RawImage));
            Assert.That(primary.Payload.PlaneIndex, Is.Zero);
            Assert.That(primary.Payload.Compression, Is.EqualTo(RawCompression.Unknown));
            Assert.That(primary.Payload.Decodable, Is.False);
            Assert.That((secondary.Payload ?? throw new System.InvalidOperationException("A required value is unavailable.")).Kind, Is.EqualTo(RawPayloadKind.RawImage));
            Assert.That(secondary.Payload.PlaneIndex, Is.EqualTo(1));
            Assert.That(document.PayloadMap.Find(RawPayloadKind.RawImage), Has.Length.EqualTo(2));
            Assert.That(document.PayloadMap.Find(RawPayloadKind.Preview), Has.Length.EqualTo(1));
            Assert.That(document.PayloadMap.Find(RawPayloadKind.Thumbnail), Has.Length.EqualTo(1));
            Assert.That(document.PayloadMap.Find(RawPayloadKind.MakerNote), Has.Length.EqualTo(1));
            Assert.That(document.Thumbnails, Has.Length.EqualTo(2));
            Assert.That(document.ExtractPayload(primary.Payload), Is.EqualTo(fixture.Raw));
            Assert.That(document.ExtractPayload(secondary.Payload), Is.EqualTo(fixture.Secondary));
            Assert.That(document.ExtractPayload(capture), Is.EqualTo(fixture.Capture));
            Assert.That(document.ExtractPayload(edit), Is.EqualTo(fixture.Edit));
            Assert.That(capture.Decodable, Is.False);
            Assert.That(capture.PreserveByteExact, Is.True);
            Assert.That(document.PayloadMap.Validate().IsValid(), Is.True);
        });
    }

    [Test]
    public void PreviewOnlyNefOpenDoesNotMisclassifyDisplayImagesAsRaw()
    {
        NefFixture fixture = BuildNef();

        RawDocument document = RawCodec.OpenPreviews(fixture.Bytes, null, new RawOpenOptions());

        Assert.Multiple(() =>
        {
            Assert.That(document.Format, Is.EqualTo(RawFormat.Nef));
            Assert.That(((document.Images ?? throw new System.InvalidOperationException("A required value is unavailable.")).Count(image => image.Kind == RawImageKind.Raw)), Is.EqualTo(1));
            Assert.That(document.Images.Count(image => image.Kind == RawImageKind.Auxiliary), Is.EqualTo(1));
            Assert.That(document.Images.Count(image => image.Kind == RawImageKind.Preview), Is.EqualTo(1));
            Assert.That(document.Images.Count(image => image.Kind == RawImageKind.Thumbnail), Is.EqualTo(1));
            Assert.That(document.Thumbnails.Select(thumbnail => thumbnail.EncodedBytes), Is.EqualTo(new byte[][] { fixture.Preview, fixture.Thumbnail }));
            Assert.That(document.PayloadMap.Find(RawPayloadKind.Metadata), Is.Empty);
            Assert.That(document.PayloadMap.Find(RawPayloadKind.MakerNote), Is.Empty);
        });
    }

    internal static NefFixture BuildNef()
    {
        byte[] preview = new byte[] { 10, 20, 21, 22, 23 };
        byte[] thumbnail = new byte[] { 30, 31, 32, 33, 34 };
        byte[] raw = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        byte[] secondary = new byte[] { 9, 10, 11, 12, 13, 14, 15, 16 };
        byte[] capture = new byte[] { 41, 42, 43, 44, 45 };
        byte[] edit = new byte[] { 51, 52, 53, 54, 55, 56 };
        byte[] makerNote = CreateNikonMakerNote(capture, edit);
        SyntheticTiffBuilder builder = new SyntheticTiffBuilder(RawByteOrder.LittleEndian, false);
        SyntheticTiffIfd root = builder.AddIfd();
        SyntheticTiffIfd thumbnailIfd = builder.AddIfd();
        SyntheticTiffIfd rawIfd = builder.AddIfd();
        SyntheticTiffIfd secondaryIfd = builder.AddIfd();
        SyntheticTiffIfd exif = builder.AddIfd();
        builder.SetNextIfd(root, thumbnailIfd);
        builder.AddField(root, 271, TiffFieldType.Ascii, 18, System.Text.Encoding.ASCII.GetBytes("NIKON CORPORATION\0"));
        builder.AddField(root, 272, TiffFieldType.Ascii, 10, System.Text.Encoding.ASCII.GetBytes("NIKON Z 8\0"));
        builder.AddPointer(root, 330, TiffFieldType.Long, rawIfd, secondaryIfd);
        builder.AddPointer(root, 34665, TiffFieldType.Long, exif);
        SyntheticTiffEntry previewOffset = AddImage(builder, root, 5, 1, 1, 1, 8, preview, out SyntheticTiffEntry previewData);
        SyntheticTiffEntry thumbnailOffset = AddImage(builder, thumbnailIfd, 5, 1, 1, 1, 8, thumbnail, out SyntheticTiffEntry thumbnailData);
        AddLong(builder, thumbnailIfd, 254, 1);
        SyntheticTiffEntry rawOffset = AddImage(builder, rawIfd, 4, 2, 34713, 32803, 14, raw, out SyntheticTiffEntry rawData);
        AddCfa(builder, rawIfd);
        SyntheticTiffEntry secondaryOffset = AddImage(builder, secondaryIfd, 4, 2, 1, 32803, 14, secondary, out SyntheticTiffEntry secondaryData);
        AddCfa(builder, secondaryIfd);
        builder.AddField(exif, 37500, TiffFieldType.Undefined, (ulong)makerNote.Length, makerNote);

        byte[] bytes = builder.Build();
        PatchLong(bytes, root, previewOffset, (uint)previewData.ValueOffset);
        PatchLong(bytes, thumbnailIfd, thumbnailOffset, (uint)thumbnailData.ValueOffset);
        PatchLong(bytes, rawIfd, rawOffset, (uint)rawData.ValueOffset);
        PatchLong(bytes, secondaryIfd, secondaryOffset, (uint)secondaryData.ValueOffset);
        return new NefFixture(bytes, preview, thumbnail, raw, secondary, capture, edit);
    }

    static SyntheticTiffEntry AddImage(SyntheticTiffBuilder builder, SyntheticTiffIfd ifd, uint width, uint height, ushort compression, ushort photometric, ushort bits, byte[] payload, out SyntheticTiffEntry data)
    {
        AddLong(builder, ifd, 256, width);
        AddLong(builder, ifd, 257, height);
        AddShort(builder, ifd, 258, bits);
        AddShort(builder, ifd, 259, compression);
        AddShort(builder, ifd, 262, photometric);
        SyntheticTiffEntry offset = AddLong(builder, ifd, 273, 0);
        AddShort(builder, ifd, 277, 1);
        AddLong(builder, ifd, 278, height);
        AddLong(builder, ifd, 279, (uint)payload.Length);
        data = builder.AddField(ifd, 65000, TiffFieldType.Undefined, (ulong)payload.Length, payload);
        return offset;
    }

    static void AddCfa(SyntheticTiffBuilder builder, SyntheticTiffIfd ifd)
    {
        builder.AddField(ifd, 33421, TiffFieldType.Short, 2, Shorts(new ushort[] { 2, 2 }));
        builder.AddField(ifd, 33422, TiffFieldType.Byte, 4, new byte[] { 0, 1, 1, 2 });
    }

    static byte[] CreateNikonMakerNote(byte[] capture, byte[] edit)
    {
        const int entryCount = 2;
        const int ifdPosition = 18;
        int dataPosition = ifdPosition + 2 + entryCount * 12 + 4;
        byte[] bytes = new byte[dataPosition + capture.Length + edit.Length];
        System.Text.Encoding.ASCII.GetBytes("Nikon\0").CopyTo(bytes, 0);
        bytes[6] = 2;
        bytes[10] = (byte)'I';
        bytes[11] = (byte)'I';
        EndianWriter.WriteUInt16(bytes.AsSpan(12, 2), 42, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(bytes.AsSpan(14, 4), 8, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt16(bytes.AsSpan(ifdPosition, 2), entryCount, RawByteOrder.LittleEndian);
        WriteMakerEntry(bytes, 0, 0x0e01, capture.Length, dataPosition);
        WriteMakerEntry(bytes, 1, 0x0e13, edit.Length, dataPosition + capture.Length);
        capture.CopyTo(bytes, dataPosition);
        edit.CopyTo(bytes, dataPosition + capture.Length);
        return bytes;
    }

    static void WriteMakerEntry(byte[] bytes, int index, ushort tag, int length, int valuePosition)
    {
        int position = 20 + index * 12;
        EndianWriter.WriteUInt16(bytes.AsSpan(position, 2), tag, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt16(bytes.AsSpan(position + 2, 2), (ushort)RawTagType.Undefined, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(bytes.AsSpan(position + 4, 4), (uint)length, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(bytes.AsSpan(position + 8, 4), (uint)(valuePosition - 10), RawByteOrder.LittleEndian);
    }

    static SyntheticTiffEntry AddLong(SyntheticTiffBuilder builder, SyntheticTiffIfd ifd, ushort tag, uint value)
    {
        byte[] bytes = new byte[4];
        EndianWriter.WriteUInt32(bytes, value, RawByteOrder.LittleEndian);
        return builder.AddField(ifd, tag, TiffFieldType.Long, 1, bytes);
    }

    static void AddShort(SyntheticTiffBuilder builder, SyntheticTiffIfd ifd, ushort tag, ushort value)
    {
        byte[] bytes = new byte[2];
        EndianWriter.WriteUInt16(bytes, value, RawByteOrder.LittleEndian);
        builder.AddField(ifd, tag, TiffFieldType.Short, 1, bytes);
    }

    static byte[] Shorts(ushort[] values)
    {
        byte[] bytes = new byte[values.Length * 2];
        for (int index = 0; index < values.Length; index++)
        {
            EndianWriter.WriteUInt16(bytes.AsSpan(index * 2, 2), values[index], RawByteOrder.LittleEndian);
        }
        return bytes;
    }

    static void PatchLong(byte[] bytes, SyntheticTiffIfd ifd, SyntheticTiffEntry entry, uint value)
    {
        int entryIndex = ifd.Entries.IndexOf(entry);
        int slotOffset = checked((int)ifd.Offset + 2 + entryIndex * 12 + 8);
        EndianWriter.WriteUInt32(bytes.AsSpan(slotOffset, 4), value, RawByteOrder.LittleEndian);
    }
}

internal class NefFixture
{
    internal byte[] Bytes;
    internal byte[] Preview;
    internal byte[] Thumbnail;
    internal byte[] Raw;
    internal byte[] Secondary;
    internal byte[] Capture;
    internal byte[] Edit;

    internal NefFixture(byte[] bytes, byte[] preview, byte[] thumbnail, byte[] raw, byte[] secondary, byte[] capture, byte[] edit)
    {
        Bytes = bytes;
        Preview = preview;
        Thumbnail = thumbnail;
        Raw = raw;
        Secondary = secondary;
        Capture = capture;
        Edit = edit;
    }
}
