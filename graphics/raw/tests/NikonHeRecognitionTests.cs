using raw;

namespace raw.Tests;

public class NikonHeRecognitionTests
{
    [TestCase(13, RawCompression.NikonHe, NikonHeRecognition.HeMessage)]
    [TestCase(14, RawCompression.NikonHeStar, NikonHeRecognition.HeStarMessage)]
    public void OpenRecognizesUnsupportedHeModesAndRetainsMetadataPreviewAndRawPayload(int makerCode, RawCompression compression, string message)
    {
        NikonHeFixture fixture = BuildNef((ushort)makerCode);

        RawDocument document = RawCodec.Open(fixture.Bytes, null, new RawOpenOptions());
        RawImageInfo? rawImage = (document.Images ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(image => image.Kind == RawImageKind.Raw);
        RawThumbnail preview = document.Thumbnails.Single();
        RawUnsupportedException? exception = Assert.Throws<RawUnsupportedException>(() => document.Decode(new RawDecodeOptions()));

        Assert.Multiple(() =>
        {
            Assert.That(document.Format, Is.EqualTo(RawFormat.Nef));
            Assert.That(document.ProfileName, Is.Empty);
            Assert.That(document.Metadata.Make, Is.EqualTo("NIKON CORPORATION"));
            Assert.That(document.Metadata.Model, Is.EqualTo("NIKON Z 8"));
            Assert.That((document.FormatDetails ?? throw new System.InvalidOperationException("A required value is unavailable.")).Nikon, Is.Not.Null);
            Assert.That((document.FormatDetails.Nikon ?? throw new System.InvalidOperationException("A required value is unavailable.")).NefCompressionCode, Is.EqualTo(makerCode));
            Assert.That(document.FormatDetails.Nikon.Compression, Is.EqualTo(compression));
            Assert.That(document.FormatDetails.Nikon.MakerNoteVersion, Is.EqualTo("0200"));
            Assert.That((document.FormatDetails.Nikon.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).Layout, Is.EqualTo(RawCfaLayout.Arbitrary));
            Assert.That(document.FormatDetails.Nikon.Cfa.Colors, Is.EqualTo(new RawCfaColor[] { RawCfaColor.Red, RawCfaColor.Green, RawCfaColor.Green, RawCfaColor.Blue }));
            Assert.That(rawImage.Compression, Is.EqualTo(compression));
            Assert.That((rawImage.Payload ?? throw new System.InvalidOperationException("A required value is unavailable.")).Compression, Is.EqualTo(compression));
            Assert.That(rawImage.Payload.Decodable, Is.False);
            Assert.That(rawImage.Payload.PreserveByteExact, Is.True);
            Assert.That(document.ExtractPayload(rawImage.Payload), Is.EqualTo(fixture.Raw));
            Assert.That(document.Capabilities.Recognized, Is.True);
            Assert.That(document.Capabilities.Inspected, Is.True);
            Assert.That(document.Capabilities.Decoded, Is.False);
            Assert.That(document.Capabilities.Rendered, Is.False);
            Assert.That(document.Capabilities.PreservingRewrite, Is.False);
            Assert.That(document.Capabilities.Encoded, Is.False);
            Assert.That(document.Capabilities.VendorConfirmed, Is.False);
            Assert.That(preview.Kind, Is.EqualTo(RawThumbnailKind.Rgb8));
            Assert.That(preview.EncodedBytes, Is.EqualTo(fixture.Preview));
            Assert.That(preview.Decode(new RawLimits(), CancellationToken.None).ByteSamples, Is.EqualTo(fixture.Preview));
            Assert.That(document.Diagnostics.Count(diagnostic => diagnostic.Code == RawDiagnosticCode.UnsupportedCodec), Is.EqualTo(1));
            Assert.That(document.Diagnostics.Single(diagnostic => diagnostic.Code == RawDiagnosticCode.UnsupportedCodec).Severity, Is.EqualTo(RawSeverity.Warning));
            Assert.That(document.Diagnostics.Single(diagnostic => diagnostic.Code == RawDiagnosticCode.UnsupportedCodec).Stage, Is.EqualTo(NikonHeRecognition.Stage));
            Assert.That(document.Diagnostics.Single(diagnostic => diagnostic.Code == RawDiagnosticCode.UnsupportedCodec).Message, Is.EqualTo(message));
            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.UnsupportedCodec));
            Assert.That(exception.Message, Is.EqualTo(message));
        });
    }

    [Test]
    public void HeRecognitionRequiresConsistentTiffAndMakerNoteEvidence()
    {
        Assert.Multiple(() =>
        {
            Assert.That(NikonHeRecognition.GetCompression(34713, 13), Is.EqualTo(RawCompression.NikonHe));
            Assert.That(NikonHeRecognition.GetCompression(34713, 14), Is.EqualTo(RawCompression.NikonHeStar));
            Assert.That(NikonHeRecognition.GetCompression(1, 13), Is.EqualTo(RawCompression.Unknown));
            Assert.That(NikonHeRecognition.GetCompression(34713, 3), Is.EqualTo(RawCompression.Unknown));
            Assert.That(NikonNefMetadataParser.ResolveCompression(34713, 13), Is.EqualTo(RawCompression.NikonHe));
            Assert.That(NikonNefMetadataParser.ResolveCompression(34713, 14), Is.EqualTo(RawCompression.NikonHeStar));
            Assert.That(NikonNefMetadataParser.ResolveCompression(34713, 3), Is.EqualTo(RawCompression.NikonLossless));
        });
    }

    internal static NikonHeFixture BuildNef(ushort makerCode)
    {
        byte[] preview = new byte[] { 10, 20, 30, 40, 50, 60 };
        byte[] raw = new byte[] { 1, 3, 5, 7, 9, 11, 13, 15 };
        byte[] makerNote = CreateMakerNote(makerCode);
        SyntheticTiffBuilder builder = new SyntheticTiffBuilder(RawByteOrder.LittleEndian, false);
        SyntheticTiffIfd root = builder.AddIfd();
        SyntheticTiffIfd rawIfd = builder.AddIfd();
        SyntheticTiffIfd exif = builder.AddIfd();
        builder.AddField(root, 271, TiffFieldType.Ascii, 18, System.Text.Encoding.ASCII.GetBytes("NIKON CORPORATION\0"));
        builder.AddField(root, 272, TiffFieldType.Ascii, 10, System.Text.Encoding.ASCII.GetBytes("NIKON Z 8\0"));
        builder.AddPointer(root, 330, TiffFieldType.Long, rawIfd);
        builder.AddPointer(root, 34665, TiffFieldType.Long, exif);
        SyntheticTiffEntry previewOffset = AddImage(builder, root, 2, 1, 1, 2, 8, 3, preview, out SyntheticTiffEntry previewData);
        SyntheticTiffEntry rawOffset = AddImage(builder, rawIfd, 4, 2, 34713, 32803, 14, 1, raw, out SyntheticTiffEntry rawData);
        builder.AddField(rawIfd, 33421, TiffFieldType.Short, 2, Shorts(new ushort[] { 2, 2 }));
        builder.AddField(rawIfd, 33422, TiffFieldType.Byte, 4, new byte[] { 0, 1, 1, 2 });
        builder.AddField(rawIfd, 50717, TiffFieldType.Short, 1, Shorts(new ushort[] { 16383 }));
        builder.AddField(exif, 37500, TiffFieldType.Undefined, (ulong)makerNote.Length, makerNote);
        byte[] bytes = builder.Build();
        PatchLong(bytes, root, previewOffset, (uint)previewData.ValueOffset);
        PatchLong(bytes, rawIfd, rawOffset, (uint)rawData.ValueOffset);
        return new NikonHeFixture(bytes, preview, raw);
    }

    static SyntheticTiffEntry AddImage(SyntheticTiffBuilder builder, SyntheticTiffIfd ifd, uint width, uint height, ushort compression, ushort photometric, ushort bits, ushort channels, byte[] payload, out SyntheticTiffEntry data)
    {
        AddLong(builder, ifd, 256, width);
        AddLong(builder, ifd, 257, height);
        AddShort(builder, ifd, 258, bits);
        AddShort(builder, ifd, 259, compression);
        AddShort(builder, ifd, 262, photometric);
        SyntheticTiffEntry offset = AddLong(builder, ifd, 273, 0);
        AddShort(builder, ifd, 277, channels);
        AddLong(builder, ifd, 278, height);
        AddLong(builder, ifd, 279, (uint)payload.Length);
        data = builder.AddField(ifd, 65000, TiffFieldType.Undefined, (ulong)payload.Length, payload);
        return offset;
    }

    static byte[] CreateMakerNote(ushort compression)
    {
        const int count = 2;
        byte[] bytes = new byte[18 + 2 + count * 12 + 4];
        System.Text.Encoding.ASCII.GetBytes("Nikon\0").CopyTo(bytes, 0);
        bytes[6] = 2;
        bytes[10] = (byte)'I';
        bytes[11] = (byte)'I';
        EndianWriter.WriteUInt16(bytes.AsSpan(12, 2), 42, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(bytes.AsSpan(14, 4), 8, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt16(bytes.AsSpan(18, 2), count, RawByteOrder.LittleEndian);
        WriteMakerEntry(bytes, 0, 0x0001, RawTagType.Undefined, 4, System.Text.Encoding.ASCII.GetBytes("0200"));
        WriteMakerEntry(bytes, 1, 0x0093, RawTagType.Short, 1, Shorts(new ushort[] { compression }));
        return bytes;
    }

    static void WriteMakerEntry(byte[] bytes, int index, ushort tag, RawTagType type, int count, byte[] value)
    {
        int position = 20 + index * 12;
        EndianWriter.WriteUInt16(bytes.AsSpan(position, 2), tag, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt16(bytes.AsSpan(position + 2, 2), (ushort)type, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(bytes.AsSpan(position + 4, 4), (uint)count, RawByteOrder.LittleEndian);
        value.CopyTo(bytes, position + 8);
    }

    static SyntheticTiffEntry AddLong(SyntheticTiffBuilder builder, SyntheticTiffIfd ifd, ushort tag, uint value)
    {
        byte[] bytes = new byte[4];
        EndianWriter.WriteUInt32(bytes, value, RawByteOrder.LittleEndian);
        return builder.AddField(ifd, tag, TiffFieldType.Long, 1, bytes);
    }

    static void AddShort(SyntheticTiffBuilder builder, SyntheticTiffIfd ifd, ushort tag, ushort value)
    {
        builder.AddField(ifd, tag, TiffFieldType.Short, 1, Shorts(new ushort[] { value }));
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
        int position = checked((int)ifd.Offset + 2 + entryIndex * 12 + 8);
        EndianWriter.WriteUInt32(bytes.AsSpan(position, 4), value, RawByteOrder.LittleEndian);
    }
}

internal class NikonHeFixture
{
    internal byte[] Bytes;
    internal byte[] Preview;
    internal byte[] Raw;

    internal NikonHeFixture(byte[] bytes, byte[] preview, byte[] raw)
    {
        Bytes = bytes;
        Preview = preview;
        Raw = raw;
    }
}
