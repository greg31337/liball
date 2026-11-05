using raw;
using System.Buffers.Binary;

namespace raw.Tests;

public class CanonMakerNoteCodecTests
{
    [TestCase(RawByteOrder.LittleEndian)]
    [TestCase(RawByteOrder.BigEndian)]
    public void ReviewedAndUnknownMainIfdTagsAreDecodedWithExactValues(RawByteOrder byteOrder)
    {
        byte[] bytes = CreateCanonNote(byteOrder, 100);
        RawMakerNote note = RawMakerNote.Parse(bytes, 100, 42, "IFD0/Exif", "Canon", 0, byteOrder, new RawLimits());
        RawCanonMakerNoteTag? firmware = note.FindCanonTag(0x0007);
        RawCanonMakerNoteTag? model = note.FindCanonTag(0x0010);
        RawCanonMakerNoteTag? unknown = note.FindCanonTag(0xBEEF);

        byte[] unknownCopy = (unknown ?? throw new System.InvalidOperationException("A required value is unavailable.")).CopyBytes();
        unknownCopy[0] ^= 0xFF;

        Assert.Multiple(() =>
        {
            Assert.That(note.CanonTags, Has.Length.EqualTo(6));
            Assert.That((firmware ?? throw new System.InvalidOperationException("A required value is unavailable.")).Name, Is.EqualTo("CanonFirmwareVersion"));
            Assert.That(firmware.MinimumVersion, Is.EqualTo(RawCanonMakerNoteVersion.Baseline));
            Assert.That(firmware.GetText(), Is.EqualTo("Firmware 1.0"));
            Assert.That(firmware.Writable, Is.True);
            Assert.That((model ?? throw new System.InvalidOperationException("A required value is unavailable.")).Name, Is.EqualTo("ModelID"));
            Assert.That(model.MinimumVersion, Is.EqualTo(RawCanonMakerNoteVersion.Extended));
            Assert.That(model.Value.GetUInt32Values().Single(), Is.EqualTo(0x80000218U));
            Assert.That(unknown.Name, Is.EqualTo("Tag0xBEEF"));
            Assert.That(unknown.MinimumVersion, Is.EqualTo(RawCanonMakerNoteVersion.Unknown));
            Assert.That(unknown.Writable, Is.False);
            Assert.That(unknown.Value.Type, Is.EqualTo(RawTagType.Undefined));
            Assert.That(unknown.CopyBytes(), Is.EqualTo(new byte[] { 91, 92, 93, 94, 95, 96 }));
            Assert.That(unknown.CopyBytes(), Is.Not.EqualTo(unknownCopy));
        });
    }

    [TestCase(RawByteOrder.LittleEndian)]
    [TestCase(RawByteOrder.BigEndian)]
    public void VersionedEncodingReplacesReviewedTagsWithoutChangingLayoutOrUnknownBytes(RawByteOrder byteOrder)
    {
        byte[] bytes = CreateCanonNote(byteOrder, 100);
        RawMakerNote original = RawMakerNote.Parse(bytes, 100, 42, "IFD0/Exif", "Canon", 0, byteOrder, new RawLimits());
        RawCanonMakerNoteEdit[] edits =
        {
            new RawCanonMakerNoteEdit(0x0007, RawCanonMakerNoteVersion.Baseline, RawTagValue.CreateAscii("Firmware 2.5")),
            new RawCanonMakerNoteEdit(0x0008, RawCanonMakerNoteVersion.Baseline, RawTagValue.CreateUInt32(new uint[] { 7654321 }, byteOrder)),
            new RawCanonMakerNoteEdit(0x0009, RawCanonMakerNoteVersion.Baseline, RawTagValue.CreateAscii("Reviewed Owner")),
            new RawCanonMakerNoteEdit(0x0010, RawCanonMakerNoteVersion.Extended, RawTagValue.CreateUInt32(new uint[] { 0x80000421U }, byteOrder)),
            new RawCanonMakerNoteEdit(0x0095, RawCanonMakerNoteVersion.Extended, RawTagValue.CreateAscii("RF50mm F1.2 L USM"))
        };

        RawMakerNote encoded = original.EncodeCanon(edits, new RawLimits());

        Assert.Multiple(() =>
        {
            Assert.That((encoded.RawBytes ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length, Is.EqualTo((original.RawBytes ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length));
            Assert.That((original.FindCanonTag(0x0007) ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetText(), Is.EqualTo("Firmware 1.0"));
            Assert.That((encoded.FindCanonTag(0x0007) ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetText(), Is.EqualTo("Firmware 2.5"));
            Assert.That((encoded.FindCanonTag(0x0008) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetUInt32Values().Single(), Is.EqualTo(7654321));
            Assert.That((encoded.FindCanonTag(0x0009) ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetText(), Is.EqualTo("Reviewed Owner"));
            Assert.That((encoded.FindCanonTag(0x0009) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.RawBytes, Has.Length.EqualTo(32));
            Assert.That((encoded.FindCanonTag(0x0010) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetUInt32Values().Single(), Is.EqualTo(0x80000421U));
            Assert.That((encoded.FindCanonTag(0x0095) ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetText(), Is.EqualTo("RF50mm F1.2 L USM"));
            Assert.That((encoded.FindCanonTag(0xBEEF) ?? throw new System.InvalidOperationException("A required value is unavailable.")).CopyBytes(), Is.EqualTo((original.FindCanonTag(0xBEEF) ?? throw new System.InvalidOperationException("A required value is unavailable.")).CopyBytes()));
            Assert.That(encoded.RawBytes.AsSpan((original.FindCanonTag(0xBEEF) ?? throw new System.InvalidOperationException("A required value is unavailable.")).EntryPosition, 12).ToArray(), Is.EqualTo(original.RawBytes.AsSpan((original.FindCanonTag(0xBEEF) ?? throw new System.InvalidOperationException("A required value is unavailable.")).EntryPosition, 12).ToArray()));
            Assert.That(((encoded.OffsetReferences ?? throw new System.InvalidOperationException("A required value is unavailable.")).Select(reference => reference.Value) ?? throw new System.InvalidOperationException("A required value is unavailable.")), Is.EqualTo(((original.OffsetReferences ?? throw new System.InvalidOperationException("A required value is unavailable.")).Select(reference => reference.Value) ?? throw new System.InvalidOperationException("A required value is unavailable."))));
            Assert.That(encoded.Validate(new RawLimits()).IsValid(), Is.True);
        });

        AssertOnlyEditedStorageChanged(original, encoded, new ushort[] { 0x0007, 0x0008, 0x0009, 0x0010, 0x0095 });
    }

    [Test]
    public void SchemaVersionTypeSizeExistenceAndUniquenessAreEnforced()
    {
        RawMakerNote note = RawMakerNote.Parse(CreateCanonNote(RawByteOrder.LittleEndian, 100), 100, 42, "IFD0/Exif", "Canon", 0, RawByteOrder.LittleEndian, new RawLimits());
        RawCanonMakerNoteEdit modelUnderBaseline = new RawCanonMakerNoteEdit(0x0010, RawCanonMakerNoteVersion.Baseline, RawTagValue.CreateUInt32(new uint[] { 1 }, RawByteOrder.LittleEndian));
        RawCanonMakerNoteEdit unknown = new RawCanonMakerNoteEdit(0xBEEF, RawCanonMakerNoteVersion.Extended, RawTagValue.CreateUndefined(new byte[] { 1, 2, 3, 4, 5, 6 }));
        RawCanonMakerNoteEdit missing = new RawCanonMakerNoteEdit(0x000C, RawCanonMakerNoteVersion.Baseline, RawTagValue.CreateUInt32(new uint[] { 1 }, RawByteOrder.LittleEndian));
        RawCanonMakerNoteEdit wrongType = new RawCanonMakerNoteEdit(0x0008, RawCanonMakerNoteVersion.Baseline, RawTagValue.CreateAscii("1"));
        RawCanonMakerNoteEdit tooLong = new RawCanonMakerNoteEdit(0x0009, RawCanonMakerNoteVersion.Baseline, RawTagValue.CreateAscii(new string('x', 32)));
        RawCanonMakerNoteEdit duplicate = new RawCanonMakerNoteEdit(0x0008, RawCanonMakerNoteVersion.Baseline, RawTagValue.CreateUInt32(new uint[] { 2 }, RawByteOrder.LittleEndian));

        RawWriteException? versionError = Assert.Throws<RawWriteException>(() => note.EncodeCanon(new RawCanonMakerNoteEdit[] { modelUnderBaseline }, new RawLimits()));
        RawWriteException? unknownError = Assert.Throws<RawWriteException>(() => note.EncodeCanon(new RawCanonMakerNoteEdit[] { unknown }, new RawLimits()));
        RawWriteException? missingError = Assert.Throws<RawWriteException>(() => note.EncodeCanon(new RawCanonMakerNoteEdit[] { missing }, new RawLimits()));
        RawWriteException? typeError = Assert.Throws<RawWriteException>(() => note.EncodeCanon(new RawCanonMakerNoteEdit[] { wrongType }, new RawLimits()));
        RawWriteException? sizeError = Assert.Throws<RawWriteException>(() => note.EncodeCanon(new RawCanonMakerNoteEdit[] { tooLong }, new RawLimits()));
        RawWriteException? duplicateError = Assert.Throws<RawWriteException>(() => note.EncodeCanon(new RawCanonMakerNoteEdit[] { duplicate, duplicate }, new RawLimits()));

        Assert.Multiple(() =>
        {
            Assert.That((versionError ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.UnsupportedProfile));
            Assert.That((unknownError ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.UnsupportedProfile));
            Assert.That((missingError ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidMetadata));
            Assert.That((typeError ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidMetadata));
            Assert.That((sizeError ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidMetadata));
            Assert.That((duplicateError ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidMetadata));
        });
    }

    [Test]
    public void EncodedCanonNoteRetainsSafeRelocationEvidence()
    {
        RawMakerNote original = RawMakerNote.Parse(CreateCanonNote(RawByteOrder.LittleEndian, 100), 100, 42, "IFD0/Exif", "Canon", 0, RawByteOrder.LittleEndian, new RawLimits());
        RawCanonMakerNoteEdit edit = new RawCanonMakerNoteEdit(0x0008, RawCanonMakerNoteVersion.Baseline, RawTagValue.CreateUInt32(new uint[] { 42 }, RawByteOrder.LittleEndian));
        RawMakerNote encoded = original.EncodeCanon(new RawCanonMakerNoteEdit[] { edit }, new RawLimits());

        RawMakerNoteWritePlan plan = encoded.PlanWrite(500, new RawLimits());

        Assert.Multiple(() =>
        {
            Assert.That(plan.CanWrite, Is.True);
            Assert.That(plan.Strategy, Is.EqualTo(RawMakerNoteWriteStrategy.PatchedRelocation));
            Assert.That(plan.OffsetDelta, Is.EqualTo(400));
            Assert.That(ReadUInt32(plan.Bytes, (encoded.OffsetReferences ?? throw new System.InvalidOperationException("A required value is unavailable."))[0].Position, RawByteOrder.LittleEndian), Is.EqualTo(encoded.OffsetReferences[0].Value + 400));
            Assert.That((RawMakerNote.Parse(plan.Bytes, 500, 42, "IFD0/Exif", "Canon", 0, RawByteOrder.LittleEndian, new RawLimits()).FindCanonTag(0x0008) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetUInt32Values().Single(), Is.EqualTo(42));
        });
    }

    static byte[] CreateCanonNote(RawByteOrder byteOrder, long noteOffset)
    {
        const int count = 6;
        int dataPosition = 2 + count * 12 + 4;
        int firmwarePosition = dataPosition;
        int ownerPosition = firmwarePosition + 16;
        int lensPosition = ownerPosition + 32;
        int unknownPosition = lensPosition + 24;
        byte[] bytes = new byte[unknownPosition + 6];
        WriteUInt16(bytes, 0, count, byteOrder);
        WriteEntry(bytes, 0, 0x0007, RawTagType.Ascii, 16, (uint)(noteOffset + firmwarePosition), byteOrder);
        WriteEntry(bytes, 1, 0x0008, RawTagType.Long, 1, 1234567, byteOrder);
        WriteEntry(bytes, 2, 0x0009, RawTagType.Ascii, 32, (uint)(noteOffset + ownerPosition), byteOrder);
        WriteEntry(bytes, 3, 0x0010, RawTagType.Long, 1, 0x80000218U, byteOrder);
        WriteEntry(bytes, 4, 0x0095, RawTagType.Ascii, 24, (uint)(noteOffset + lensPosition), byteOrder);
        WriteEntry(bytes, 5, 0xBEEF, RawTagType.Undefined, 6, (uint)(noteOffset + unknownPosition), byteOrder);
        WriteUInt32(bytes, 2 + count * 12, 0, byteOrder);
        WriteFixedAscii(bytes, firmwarePosition, 16, "Firmware 1.0");
        WriteFixedAscii(bytes, ownerPosition, 32, "Original Owner");
        WriteFixedAscii(bytes, lensPosition, 24, "EF24-70mm f/2.8L II");
        new byte[] { 91, 92, 93, 94, 95, 96 }.CopyTo(bytes, unknownPosition);
        return bytes;
    }

    static void WriteEntry(byte[] bytes, int index, ushort tag, RawTagType type, uint count, uint value, RawByteOrder byteOrder)
    {
        int position = 2 + index * 12;
        WriteUInt16(bytes, position, tag, byteOrder);
        WriteUInt16(bytes, position + 2, (ushort)type, byteOrder);
        WriteUInt32(bytes, position + 4, count, byteOrder);
        WriteUInt32(bytes, position + 8, value, byteOrder);
    }

    static void WriteFixedAscii(byte[] bytes, int position, int length, string value)
    {
        byte[] encoded = System.Text.Encoding.ASCII.GetBytes(value);
        encoded.CopyTo(bytes, position);
        bytes[position + encoded.Length] = 0;
        for (int index = encoded.Length + 1; index < length; index++)
        {
            bytes[position + index] = (byte)(index % 2 == 0 ? 0 : 0x20);
        }
    }

    static void AssertOnlyEditedStorageChanged(RawMakerNote original, RawMakerNote encoded, ushort[] editedIds)
    {
        bool[] editable = new bool[(original.RawBytes ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length];
        foreach (ushort id in editedIds)
        {
            RawCanonMakerNoteTag? tag = original.FindCanonTag(id);
            for (int index = 0; index < (tag ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.RawBytes.Length; index++)
            {
                editable[tag.ValuePosition + index] = true;
            }
        }
        for (int index = 0; index < original.RawBytes.Length; index++)
        {
            if (!editable[index])
            {
                Assert.That((encoded.RawBytes ?? throw new System.InvalidOperationException("A required value is unavailable."))[index], Is.EqualTo(original.RawBytes[index]), "Byte " + index + " outside edited storage changed.");
            }
        }
    }

    static uint ReadUInt32(byte[] bytes, int offset, RawByteOrder byteOrder)
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
