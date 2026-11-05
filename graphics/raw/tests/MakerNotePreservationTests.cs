using raw;
using System.Buffers.Binary;
using System.Text;

namespace raw.Tests;

public class MakerNotePreservationTests
{
    [TestCase(RawByteOrder.LittleEndian)]
    [TestCase(RawByteOrder.BigEndian)]
    public void RecognizedCanonIfdRelocationPatchesOnlyProvenTiffOffsets(RawByteOrder byteOrder)
    {
        byte[] bytes = CreateCanonIfd(byteOrder, 118);
        RawMakerNote note = RawMakerNote.Parse(bytes, 100, 42, "IFD0/Exif", "Canon", 0, byteOrder, new RawLimits());

        RawMakerNoteWritePlan fixedPlan = note.PlanWrite(100, new RawLimits());
        RawMakerNoteWritePlan movedPlan = note.PlanWrite(200, new RawLimits());

        Assert.Multiple(() =>
        {
            Assert.That(note.Signature, Is.EqualTo("CanonIfd"));
            Assert.That(note.OffsetBase, Is.EqualTo(RawMakerNoteOffsetBase.TiffHeader));
            Assert.That(note.OffsetBaseAbsolute, Is.Zero);
            Assert.That(note.RelocationSupported, Is.True);
            Assert.That(note.OffsetReferences, Has.Length.EqualTo(1));
            Assert.That((note.OffsetReferences ?? throw new System.InvalidOperationException("A required value is unavailable."))[0].Position, Is.EqualTo(10));
            Assert.That(note.OffsetReferences[0].TargetPosition, Is.EqualTo(18));
            Assert.That(note.OffsetReferences[0].AdjustOnRelocation, Is.True);
            Assert.That(fixedPlan.CanWrite, Is.True);
            Assert.That(fixedPlan.Strategy, Is.EqualTo(RawMakerNoteWriteStrategy.FixedAnchor));
            Assert.That(fixedPlan.Bytes, Is.EqualTo(bytes));
            Assert.That(movedPlan.CanWrite, Is.True);
            Assert.That(movedPlan.Strategy, Is.EqualTo(RawMakerNoteWriteStrategy.PatchedRelocation));
            Assert.That(movedPlan.OffsetDelta, Is.EqualTo(100));
            Assert.That(ReadUInt32(movedPlan.Bytes, 10, byteOrder), Is.EqualTo(218));
            Assert.That(movedPlan.Bytes.AsSpan(0, 10).ToArray(), Is.EqualTo(bytes.AsSpan(0, 10).ToArray()));
            Assert.That(movedPlan.Bytes.AsSpan(14).ToArray(), Is.EqualTo(bytes.AsSpan(14).ToArray()));
            Assert.That(ReadUInt32(note.RawBytes, 10, byteOrder), Is.EqualTo(118));
            Assert.That(movedPlan.Changes.Single().Kind, Is.EqualTo(RawWriteChangeKind.Relocated));
        });
    }

    [TestCase(RawByteOrder.LittleEndian)]
    [TestCase(RawByteOrder.BigEndian)]
    public void NikonEmbeddedTiffRelocatesByteExactBecauseOffsetsAreSelfRelative(RawByteOrder byteOrder)
    {
        byte[] bytes = CreateNikonEmbeddedTiff(byteOrder);
        RawMakerNote note = RawMakerNote.Parse(bytes, 500, 74, "IFD0/Exif", "NIKON CORPORATION", 0, RawByteOrder.LittleEndian, new RawLimits());

        RawMakerNoteWritePlan plan = note.PlanWrite(900, new RawLimits());

        Assert.Multiple(() =>
        {
            Assert.That(note.Signature, Is.EqualTo("NikonEmbeddedTiff"));
            Assert.That(note.ByteOrder, Is.EqualTo(byteOrder));
            Assert.That(note.OffsetBase, Is.EqualTo(RawMakerNoteOffsetBase.EmbeddedTiffHeader));
            Assert.That(note.OffsetBaseAbsolute, Is.EqualTo(510));
            Assert.That(note.EmbeddedHeaderPosition, Is.EqualTo(10));
            Assert.That(note.OffsetReferences, Has.Length.EqualTo(1));
            Assert.That((note.OffsetReferences ?? throw new System.InvalidOperationException("A required value is unavailable."))[0].TargetPosition, Is.EqualTo(36));
            Assert.That(note.OffsetReferences[0].AdjustOnRelocation, Is.False);
            Assert.That(note.RelocationSupported, Is.True);
            Assert.That(plan.CanWrite, Is.True);
            Assert.That(plan.Strategy, Is.EqualTo(RawMakerNoteWriteStrategy.RelativeRelocation));
            Assert.That(plan.OffsetDelta, Is.EqualTo(400));
            Assert.That(plan.Bytes, Is.EqualTo(bytes));
            Assert.That(plan.Bytes, Is.Not.SameAs(note.RawBytes));
        });
    }

    [Test]
    public void UnknownAndMalformedVendorNotesRequireTheirOriginalAnchor()
    {
        byte[] unknownBytes = new byte[] { 9, 8, 7, 6, 5 };
        RawMakerNote unknown = RawMakerNote.Parse(unknownBytes, 300, 80, "IFD0/Exif", "Mystery Camera", 0, RawByteOrder.LittleEndian, new RawLimits());
        byte[] malformedCanonBytes = Encoding.ASCII.GetBytes("not a Canon IFD");
        RawMakerNote malformedCanon = RawMakerNote.Parse(malformedCanonBytes, 400, 90, "IFD0/Exif", "Canon", 0, RawByteOrder.LittleEndian, new RawLimits());

        RawMakerNoteWritePlan anchored = unknown.PlanWrite(300, new RawLimits());
        RawMakerNoteWritePlan moved = unknown.PlanWrite(301, new RawLimits());
        RawMakerNoteWritePlan malformedMoved = malformedCanon.PlanWrite(500, new RawLimits());

        Assert.Multiple(() =>
        {
            Assert.That(unknown.OffsetBase, Is.EqualTo(RawMakerNoteOffsetBase.Unknown));
            Assert.That(unknown.RelocationSupported, Is.False);
            Assert.That(anchored.CanWrite, Is.True);
            Assert.That(anchored.Strategy, Is.EqualTo(RawMakerNoteWriteStrategy.FixedAnchor));
            Assert.That(anchored.Bytes, Is.EqualTo(unknownBytes));
            Assert.That(moved.CanWrite, Is.False);
            Assert.That(moved.Strategy, Is.EqualTo(RawMakerNoteWriteStrategy.Reject));
            Assert.That(moved.Bytes, Is.Empty);
            Assert.That(moved.Diagnostics.Single().Severity, Is.EqualTo(RawSeverity.Error));
            Assert.That(malformedCanon.Signature, Is.EqualTo("Canon"));
            Assert.That(malformedCanon.RelocationSupported, Is.False);
            Assert.That(malformedMoved.CanWrite, Is.False);
        });
    }

    [TestCase(RawByteOrder.LittleEndian)]
    [TestCase(RawByteOrder.BigEndian)]
    public void MetadataOpenPreservesMakerNoteAndMapsItsExactPayload(RawByteOrder byteOrder)
    {
        byte[] makerBytes = new byte[] { 11, 22, 33, 44, 55, 66 };
        SyntheticTiffBuilder builder = new SyntheticTiffBuilder(byteOrder, false);
        SyntheticTiffIfd root = builder.AddIfd();
        SyntheticTiffIfd exif = builder.AddIfd();
        builder.AddField(root, 271, TiffFieldType.Ascii, 8, Encoding.ASCII.GetBytes("Mystery\0"));
        builder.AddPointer(root, 34665, TiffFieldType.Long, exif);
        builder.AddField(exif, 37500, TiffFieldType.Undefined, (ulong)makerBytes.Length, makerBytes);
        byte[] tiff = builder.Build();

        RawDocument document = RawCodec.OpenMetadata(new ReadOnlyMemory<byte>(tiff), null, new RawMetadataOptions());
        RawMakerNote note = document.Metadata.MakerNotes.Single();
        RawPayloadBlock payload = document.PayloadMap.Find(RawPayloadKind.MakerNote).Single();

        Assert.Multiple(() =>
        {
            Assert.That(note.Manufacturer, Is.EqualTo("Mystery"));
            Assert.That(note.IfdPath, Is.EqualTo("IFD0/Exif"));
            Assert.That(note.Offset, Is.EqualTo(payload.Offset));
            Assert.That(note.RawBytes, Is.EqualTo(makerBytes));
            Assert.That(payload.Path, Is.EqualTo("IFD0/Exif/MakerNote0"));
            Assert.That(payload.Decodable, Is.False);
            Assert.That(payload.PreserveByteExact, Is.True);
            Assert.That(document.ExtractPayload(payload), Is.EqualTo(makerBytes));
            Assert.That((document.Metadata.FindTag("MakerNote") ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.CopyBytes(), Is.EqualTo(makerBytes));
        });
    }

    [Test]
    public void MakerNoteSelectionAndResourceLimitsAreEnforced()
    {
        byte[] makerBytes = new byte[] { 1, 2, 3, 4, 5, 6 };
        SyntheticTiffBuilder builder = new SyntheticTiffBuilder(RawByteOrder.LittleEndian, false);
        SyntheticTiffIfd root = builder.AddIfd();
        builder.AddField(root, 37500, TiffFieldType.Undefined, (ulong)makerBytes.Length, makerBytes);
        RawMetadataOptions options = new RawMetadataOptions();
        options.ReadMakerNotes = false;

        RawDocument document = RawCodec.OpenMetadata(new ReadOnlyMemory<byte>(builder.Build()), null, options);
        RawLimits limits = new RawLimits();
        limits.MaxTagValueBytes = makerBytes.Length - 1;

        Assert.Multiple(() =>
        {
            Assert.That(document.Metadata.MakerNotes, Is.Empty);
            Assert.That(document.PayloadMap.Find(RawPayloadKind.MakerNote), Is.Empty);
            Assert.Throws<RawLimitException>(() => RawMakerNote.Parse(makerBytes, 20, 8, "IFD0", "Mystery", 0, RawByteOrder.LittleEndian, limits));
        });
    }

    [Test]
    public void MutatedOffsetEvidenceIsRejectedBeforePlanningAWrite()
    {
        RawMakerNote note = RawMakerNote.Parse(CreateCanonIfd(RawByteOrder.LittleEndian, 118), 100, 42, "IFD0/Exif", "Canon", 0, RawByteOrder.LittleEndian, new RawLimits());
        (note.OffsetReferences ?? throw new System.InvalidOperationException("A required value is unavailable."))[0].Value = 119;

        RawValidationReport validation = note.Validate(new RawLimits());
        RawMakerNoteWritePlan plan = note.PlanWrite(200, new RawLimits());

        Assert.Multiple(() =>
        {
            Assert.That(validation.IsValid(), Is.False);
            Assert.That(((validation.Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(message => message.Context == "MakerNote.OffsetReferences")), Is.True);
            Assert.That(plan.CanWrite, Is.False);
            Assert.That(plan.Strategy, Is.EqualTo(RawMakerNoteWriteStrategy.Reject));
        });
    }

    static byte[] CreateCanonIfd(RawByteOrder byteOrder, uint valueOffset)
    {
        byte[] bytes = new byte[26];
        WriteUInt16(bytes, 0, 1, byteOrder);
        WriteUInt16(bytes, 2, 1, byteOrder);
        WriteUInt16(bytes, 4, 7, byteOrder);
        WriteUInt32(bytes, 6, 8, byteOrder);
        WriteUInt32(bytes, 10, valueOffset, byteOrder);
        WriteUInt32(bytes, 14, 0, byteOrder);
        for (int index = 18; index < bytes.Length; index++)
        {
            bytes[index] = (byte)(index + 1);
        }
        return bytes;
    }

    static byte[] CreateNikonEmbeddedTiff(RawByteOrder byteOrder)
    {
        byte[] bytes = new byte[44];
        Encoding.ASCII.GetBytes("Nikon\0").CopyTo(bytes, 0);
        bytes[6] = 2;
        if (byteOrder == RawByteOrder.LittleEndian)
        {
            bytes[10] = (byte)'I';
            bytes[11] = (byte)'I';
        }
        else
        {
            bytes[10] = (byte)'M';
            bytes[11] = (byte)'M';
        }
        WriteUInt16(bytes, 12, 42, byteOrder);
        WriteUInt32(bytes, 14, 8, byteOrder);
        WriteUInt16(bytes, 18, 1, byteOrder);
        WriteUInt16(bytes, 20, 1, byteOrder);
        WriteUInt16(bytes, 22, 7, byteOrder);
        WriteUInt32(bytes, 24, 8, byteOrder);
        WriteUInt32(bytes, 28, 26, byteOrder);
        WriteUInt32(bytes, 32, 0, byteOrder);
        for (int index = 36; index < bytes.Length; index++)
        {
            bytes[index] = (byte)(255 - index);
        }
        return bytes;
    }

    static uint ReadUInt32(byte[]? bytes, int offset, RawByteOrder byteOrder)
    {
        if (byteOrder == RawByteOrder.LittleEndian)
        {
            return BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset, 4));
        }
        return BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, 4));
    }

    static void WriteUInt16(byte[] bytes, int offset, ushort value, RawByteOrder byteOrder)
    {
        if (byteOrder == RawByteOrder.LittleEndian)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset, 2), value);
        }
        else
        {
            BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(offset, 2), value);
        }
    }

    static void WriteUInt32(byte[] bytes, int offset, uint value, RawByteOrder byteOrder)
    {
        if (byteOrder == RawByteOrder.LittleEndian)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset, 4), value);
        }
        else
        {
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(offset, 4), value);
        }
    }
}
