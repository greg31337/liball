using raw;
using System.Buffers.Binary;

namespace raw.Tests;

public class NikonMakerNoteCodecTests
{
    [TestCase("0100", 72, "RBGG", false)]
    [TestCase("0102", 10, "RGGB", false)]
    [TestCase("0103", 20, "RGBG", false)]
    [TestCase("0205", 18, "RGGB", true)]
    [TestCase("0210", 290, "RGGB", true)]
    [TestCase("0209", 294, "GRBG", true)]
    [TestCase("0211", 300, "GRBG", true)]
    [TestCase("0213", 294, "RGGB", true)]
    [TestCase("0216", 288, "GRBG", true)]
    [TestCase("0219", 128, "RGGB", true)]
    public void ReviewedColorBalanceVersionsDecodeWithoutDiscardingOriginalBytes(string colorVersion, int levelsPosition, string channelOrder, bool encrypted)
    {
        byte[] bytes = CreateNikonNote(RawByteOrder.LittleEndian, colorVersion, "0200");
        RawMakerNote note = RawMakerNote.Parse(bytes, 100, 42, "IFD0/Exif", "NIKON CORPORATION", 0, RawByteOrder.LittleEndian, new RawLimits());
        RawNikonMakerNoteTag? colorTag = note.FindNikonTag(0x0097);
        byte[] changedOriginal = (note.NikonColorBalance ?? throw new System.InvalidOperationException("A required value is unavailable.")).CopyOriginalBytes();
        changedOriginal[4] ^= 0xff;

        Assert.Multiple(() =>
        {
            Assert.That(note.Signature, Is.EqualTo("NikonEmbeddedTiff"));
            Assert.That(note.NikonVersion, Is.EqualTo("0200"));
            Assert.That(note.NikonColorBalance.Version, Is.EqualTo(colorVersion));
            Assert.That(note.NikonColorBalance.Encrypted, Is.EqualTo(encrypted));
            Assert.That(note.NikonColorBalance.DecryptionSupported, Is.True);
            Assert.That(note.NikonColorBalance.LevelsPosition, Is.EqualTo(levelsPosition));
            Assert.That(note.NikonColorBalance.ChannelOrder, Is.EqualTo(channelOrder));
            Assert.That(note.NikonColorBalance.Levels, Is.EqualTo(new ushort[] { 101, 202, 303, 404 }));
            Assert.That(note.NikonColorBalance.OriginalBytes, Is.EqualTo((colorTag ?? throw new System.InvalidOperationException("A required value is unavailable.")).CopyBytes()));
            Assert.That(note.NikonColorBalance.CopyOriginalBytes(), Is.Not.EqualTo(changedOriginal));
            Assert.That(note.NikonColorBalance.DecodedBytes.AsSpan(levelsPosition, 8).ToArray(), Is.Not.Empty);
        });

        if (encrypted)
        {
            Assert.That(note.NikonColorBalance.OriginalBytes, Is.Not.EqualTo(note.NikonColorBalance.DecodedBytes));
        }
        else
        {
            Assert.That(note.NikonColorBalance.OriginalBytes, Is.EqualTo(note.NikonColorBalance.DecodedBytes));
        }
    }

    [TestCase(RawByteOrder.LittleEndian)]
    [TestCase(RawByteOrder.BigEndian)]
    public void VersionedEncodingChangesOnlyReviewedStorageAndPreservesKeysEncryptedCaptureEditAndUnknownData(RawByteOrder byteOrder)
    {
        RawMakerNote original = RawMakerNote.Parse(CreateNikonNote(byteOrder, "0216", "0200"), 100, 42, "IFD0/Exif", "NIKON CORPORATION", 0, byteOrder, new RawLimits());
        RawNikonMakerNoteEdit[] edits =
        {
            new RawNikonMakerNoteEdit(0x0005, RawNikonMakerNoteVersion.Baseline, RawTagValue.CreateAscii("Cloudy")),
            new RawNikonMakerNoteEdit(0x0092, RawNikonMakerNoteVersion.Extended, RawTagValue.CreateInt16(new short[] { -6 }, byteOrder)),
            new RawNikonMakerNoteEdit(0x0095, RawNikonMakerNoteVersion.Extended, RawTagValue.CreateAscii("High ISO"))
        };

        RawMakerNote encoded = original.EncodeNikon(edits, new RawLimits());

        Assert.Multiple(() =>
        {
            Assert.That((original.FindNikonTag(0x0005) ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetText(), Is.EqualTo("Auto"));
            Assert.That((encoded.FindNikonTag(0x0005) ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetText(), Is.EqualTo("Cloudy"));
            Assert.That((encoded.FindNikonTag(0x0092) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetInt16Values().Single(), Is.EqualTo(-6));
            Assert.That((encoded.FindNikonTag(0x0095) ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetText(), Is.EqualTo("High ISO"));
            Assert.That((encoded.FindNikonTag(0x001d) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Protected, Is.True);
            Assert.That((encoded.FindNikonTag(0x00a7) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Protected, Is.True);
            Assert.That((encoded.FindNikonTag(0x0097) ?? throw new System.InvalidOperationException("A required value is unavailable.")).CopyBytes(), Is.EqualTo((original.FindNikonTag(0x0097) ?? throw new System.InvalidOperationException("A required value is unavailable.")).CopyBytes()));
            Assert.That((encoded.NikonColorBalance ?? throw new System.InvalidOperationException("A required value is unavailable.")).OriginalBytes, Is.EqualTo((original.NikonColorBalance ?? throw new System.InvalidOperationException("A required value is unavailable.")).OriginalBytes));
            Assert.That((encoded.FindNikonTag(0x0e01) ?? throw new System.InvalidOperationException("A required value is unavailable.")).CopyBytes(), Is.EqualTo((original.FindNikonTag(0x0e01) ?? throw new System.InvalidOperationException("A required value is unavailable.")).CopyBytes()));
            Assert.That((encoded.FindNikonTag(0x0e13) ?? throw new System.InvalidOperationException("A required value is unavailable.")).CopyBytes(), Is.EqualTo((original.FindNikonTag(0x0e13) ?? throw new System.InvalidOperationException("A required value is unavailable.")).CopyBytes()));
            Assert.That((encoded.FindNikonTag(0xbeef) ?? throw new System.InvalidOperationException("A required value is unavailable.")).CopyBytes(), Is.EqualTo((original.FindNikonTag(0xbeef) ?? throw new System.InvalidOperationException("A required value is unavailable.")).CopyBytes()));
            Assert.That(encoded.Validate(new RawLimits()).IsValid(), Is.True);
        });

        AssertOnlyEditedStorageChanged(original, encoded, new ushort[] { 0x0005, 0x0092, 0x0095 });
    }

    [Test]
    public void ProtectedUnknownVersionTypeSizeExistenceAndUniquenessAreRejected()
    {
        RawLimits limits = new RawLimits();
        RawMakerNote note = RawMakerNote.Parse(CreateNikonNote(RawByteOrder.LittleEndian, "0205", "0200"), 100, 42, "IFD0/Exif", "NIKON CORPORATION", 0, RawByteOrder.LittleEndian, limits);
        RawMakerNote oldVersion = RawMakerNote.Parse(CreateNikonNote(RawByteOrder.LittleEndian, "0103", "0100"), 100, 42, "IFD0/Exif", "NIKON CORPORATION", 0, RawByteOrder.LittleEndian, limits);
        RawNikonMakerNoteEdit protectedColor = new RawNikonMakerNoteEdit(0x0097, RawNikonMakerNoteVersion.Extended, RawTagValue.CreateUndefined((note.FindNikonTag(0x0097) ?? throw new System.InvalidOperationException("A required value is unavailable.")).CopyBytes()));
        RawNikonMakerNoteEdit protectedCapture = new RawNikonMakerNoteEdit(0x0e13, RawNikonMakerNoteVersion.Extended, RawTagValue.CreateUndefined((note.FindNikonTag(0x0e13) ?? throw new System.InvalidOperationException("A required value is unavailable.")).CopyBytes()));
        RawNikonMakerNoteEdit unknown = new RawNikonMakerNoteEdit(0xbeef, RawNikonMakerNoteVersion.Extended, RawTagValue.CreateUndefined(new byte[] { 1, 2, 3, 4, 5, 6, 7 }));
        RawNikonMakerNoteEdit missing = new RawNikonMakerNoteEdit(0x0004, RawNikonMakerNoteVersion.Baseline, RawTagValue.CreateAscii("Fine"));
        RawNikonMakerNoteEdit wrongType = new RawNikonMakerNoteEdit(0x0092, RawNikonMakerNoteVersion.Extended, RawTagValue.CreateUInt16(new ushort[] { 1 }, RawByteOrder.LittleEndian));
        RawNikonMakerNoteEdit wrongCount = new RawNikonMakerNoteEdit(0x0092, RawNikonMakerNoteVersion.Extended, RawTagValue.CreateInt16(new short[] { 1, 2 }, RawByteOrder.LittleEndian));
        RawNikonMakerNoteEdit tooLong = new RawNikonMakerNoteEdit(0x0005, RawNikonMakerNoteVersion.Baseline, RawTagValue.CreateAscii(new string('x', 16)));
        RawNikonMakerNoteEdit extended = new RawNikonMakerNoteEdit(0x0092, RawNikonMakerNoteVersion.Extended, RawTagValue.CreateInt16(new short[] { 1 }, RawByteOrder.LittleEndian));

        Assert.Multiple(() =>
        {
            Assert.That((Assert.Throws<RawWriteException>(() => note.EncodeNikon(new RawNikonMakerNoteEdit[] { protectedColor }, limits)) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.UnsupportedProfile));
            Assert.That((Assert.Throws<RawWriteException>(() => note.EncodeNikon(new RawNikonMakerNoteEdit[] { protectedCapture }, limits)) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.UnsupportedProfile));
            Assert.That((Assert.Throws<RawWriteException>(() => note.EncodeNikon(new RawNikonMakerNoteEdit[] { unknown }, limits)) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.UnsupportedProfile));
            Assert.That((Assert.Throws<RawWriteException>(() => note.EncodeNikon(new RawNikonMakerNoteEdit[] { missing }, limits)) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidMetadata));
            Assert.That((Assert.Throws<RawWriteException>(() => note.EncodeNikon(new RawNikonMakerNoteEdit[] { wrongType }, limits)) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidMetadata));
            Assert.That((Assert.Throws<RawWriteException>(() => note.EncodeNikon(new RawNikonMakerNoteEdit[] { wrongCount }, limits)) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidMetadata));
            Assert.That((Assert.Throws<RawWriteException>(() => note.EncodeNikon(new RawNikonMakerNoteEdit[] { tooLong }, limits)) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidMetadata));
            Assert.That((Assert.Throws<RawWriteException>(() => note.EncodeNikon(new RawNikonMakerNoteEdit[] { extended, extended }, limits)) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidMetadata));
            Assert.That((Assert.Throws<RawWriteException>(() => oldVersion.EncodeNikon(new RawNikonMakerNoteEdit[] { extended }, limits)) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.UnsupportedProfile));
        });
    }

    [Test]
    public void UnknownColorBalanceVersionRemainsAvailableWithoutGuessedDecoding()
    {
        RawMakerNote note = RawMakerNote.Parse(CreateNikonNote(RawByteOrder.LittleEndian, "0228", "0200"), 100, 42, "IFD0/Exif", "NIKON CORPORATION", 0, RawByteOrder.LittleEndian, new RawLimits());

        Assert.Multiple(() =>
        {
            Assert.That((note.NikonColorBalance ?? throw new System.InvalidOperationException("A required value is unavailable.")).Version, Is.EqualTo("0228"));
            Assert.That(note.NikonColorBalance.Encrypted, Is.True);
            Assert.That(note.NikonColorBalance.DecryptionSupported, Is.False);
            Assert.That(note.NikonColorBalance.DecodedBytes, Is.Empty);
            Assert.That(note.NikonColorBalance.OriginalBytes, Is.EqualTo((note.FindNikonTag(0x0097) ?? throw new System.InvalidOperationException("A required value is unavailable.")).CopyBytes()));
        });
    }

    static byte[] CreateNikonNote(RawByteOrder byteOrder, string colorVersion, string makerVersion)
    {
        const int entryCount = 11;
        NikonColorBalanceLayout layout = NikonMakerNoteCodec.ColorLayout(colorVersion);
        int colorLength = Math.Max(layout.LevelsPosition + 12, layout.DecryptStart + 16);
        if (colorLength < 32)
        {
            colorLength = 32;
        }
        int dataPosition = 18 + 2 + entryCount * 12 + 4;
        int whiteBalancePosition = dataPosition;
        int noiseReductionPosition = whiteBalancePosition + 16;
        int serialPosition = noiseReductionPosition + 16;
        int colorPosition = serialPosition + 12;
        int capturePosition = colorPosition + colorLength;
        int editPosition = capturePosition + 11;
        int unknownPosition = editPosition + 13;
        byte[] bytes = new byte[unknownPosition + 7];
        System.Text.Encoding.ASCII.GetBytes("Nikon\0").CopyTo(bytes, 0);
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
        WriteUInt16(bytes, 18, entryCount, byteOrder);
        WriteEntry(bytes, 0, 0x0001, RawTagType.Undefined, 4, 0, byteOrder);
        System.Text.Encoding.ASCII.GetBytes(makerVersion).CopyTo(bytes, 28);
        WriteEntry(bytes, 1, 0x0002, RawTagType.Short, 2, 0, byteOrder);
        WriteUInt16(bytes, 40, 0, byteOrder);
        WriteUInt16(bytes, 42, 200, byteOrder);
        WriteEntry(bytes, 2, 0x0005, RawTagType.Ascii, 16, (uint)(whiteBalancePosition - 10), byteOrder);
        WriteEntry(bytes, 3, 0x0092, RawTagType.SignedShort, 1, 0, byteOrder);
        WriteUInt16(bytes, 64, 3, byteOrder);
        WriteEntry(bytes, 4, 0x0095, RawTagType.Ascii, 16, (uint)(noiseReductionPosition - 10), byteOrder);
        WriteEntry(bytes, 5, 0x001d, RawTagType.Ascii, 12, (uint)(serialPosition - 10), byteOrder);
        WriteEntry(bytes, 6, 0x00a7, RawTagType.Long, 1, 0x01020304, byteOrder);
        WriteEntry(bytes, 7, 0x0097, RawTagType.Undefined, (uint)colorLength, (uint)(colorPosition - 10), byteOrder);
        WriteEntry(bytes, 8, 0x0e01, RawTagType.Undefined, 11, (uint)(capturePosition - 10), byteOrder);
        WriteEntry(bytes, 9, 0x0e13, RawTagType.Undefined, 13, (uint)(editPosition - 10), byteOrder);
        WriteEntry(bytes, 10, 0xbeef, RawTagType.Undefined, 7, (uint)(unknownPosition - 10), byteOrder);
        WriteUInt32(bytes, 18 + 2 + entryCount * 12, 0, byteOrder);
        WriteFixedAscii(bytes, whiteBalancePosition, 16, "Auto");
        WriteFixedAscii(bytes, noiseReductionPosition, 16, "Normal");
        WriteFixedAscii(bytes, serialPosition, 12, "1234567");
        byte[] color = new byte[colorLength];
        for (int index = 0; index < color.Length; index++)
        {
            color[index] = (byte)(0x40 + index % 97);
        }
        System.Text.Encoding.ASCII.GetBytes(colorVersion).CopyTo(color, 0);
        if (layout.Known)
        {
            WriteUInt16(color, layout.LevelsPosition, 101, byteOrder);
            WriteUInt16(color, layout.LevelsPosition + 2, 202, byteOrder);
            WriteUInt16(color, layout.LevelsPosition + 4, 303, byteOrder);
            WriteUInt16(color, layout.LevelsPosition + 6, 404, byteOrder);
            if (layout.Encrypted)
            {
                NikonMakerNoteCodec.Transform(color, layout.DecryptStart, 1234567, 0x01020304);
            }
        }
        color.CopyTo(bytes, colorPosition);
        new byte[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21 }.CopyTo(bytes, capturePosition);
        new byte[] { 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43 }.CopyTo(bytes, editPosition);
        new byte[] { 91, 92, 93, 94, 95, 96, 97 }.CopyTo(bytes, unknownPosition);
        return bytes;
    }

    static void AssertOnlyEditedStorageChanged(RawMakerNote original, RawMakerNote encoded, ushort[] editedIds)
    {
        bool[] editable = new bool[(original.RawBytes ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length];
        foreach (ushort id in editedIds)
        {
            RawNikonMakerNoteTag? tag = original.FindNikonTag(id);
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

    static void WriteEntry(byte[] bytes, int index, ushort tag, RawTagType type, uint count, uint value, RawByteOrder byteOrder)
    {
        int position = 20 + index * 12;
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
