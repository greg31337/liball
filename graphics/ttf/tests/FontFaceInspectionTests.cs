using System.Buffers.Binary;
using ttf;

namespace ttf.Tests;

internal class FontFaceInspectionTests
{
    [Test]
    public void ExposesVersionSupportChecksumAlignmentAndRawPreservation()
    {
        byte[] source = BuildCompleteFont();
        FontFace compatible = new FontLoader()
            .LoadFromBytes(source, FontLoadOptions.CreateCompatible())
            .GetFace(0);
        FontFace strict = new FontLoader()
            .LoadFromBytes(source, FontLoadOptions.CreateStrict())
            .GetFace(0);
        FontTableInfo head = compatible.FindTableInfo(FontTags.Head)!;
        FontTableInfo baseTable = compatible.FindTableInfo(FontTags.Base)!;
        FontTableInfo strictBase = strict.FindTableInfo(FontTags.Base)!;
        FontTableInfo glyf = compatible.FindTableInfo(FontTags.Glyf)!;
        FontTableInfo unsupported = compatible.FindTableInfo(
            (int)TestSfntEncoding.EncodeTag("morx"))!;
        FontTableInfo unknown = compatible.FindTableInfo(
            (int)TestSfntEncoding.EncodeTag("ZZZZ"))!;

        Assert.Multiple(() =>
        {
            Assert.That(compatible.IsStructurallyValid(), Is.True);
            Assert.That(compatible.GetChecksumValidationState(),
                Is.EqualTo(FontChecksumValidationState.Valid));
            Assert.That(head.HasVersion(), Is.True);
            Assert.That(head.GetMajorVersion(), Is.EqualTo(1));
            Assert.That(head.GetMinorVersion(), Is.Zero);
            Assert.That(head.GetSupportState(), Is.EqualTo(FontTableSupportState.Supported));
            Assert.That(head.GetChecksumValidationState(),
                Is.EqualTo(FontChecksumValidationState.Valid));
            Assert.That(head.GetAlignmentState(), Is.EqualTo(FontTableAlignmentState.Valid));
            Assert.That(head.HasRawData(), Is.True);
            Assert.That(baseTable.HasVersion(), Is.True);
            Assert.That(baseTable.GetMajorVersion(), Is.EqualTo(1));
            Assert.That(baseTable.GetMinorVersion(), Is.EqualTo(2));
            Assert.That(baseTable.GetSupportState(),
                Is.EqualTo(FontTableSupportState.CompatibleExtension));
            Assert.That(strictBase.GetSupportState(), Is.EqualTo(FontTableSupportState.RawOnly));
            Assert.That(glyf.HasVersion(), Is.False);
            Assert.That(glyf.GetSupportState(), Is.EqualTo(FontTableSupportState.Supported));
            Assert.Throws<InvalidOperationException>(() => glyf.GetMajorVersion());
            Assert.That(unsupported.GetKind(), Is.EqualTo(FontTableKind.Unsupported));
            Assert.That(unsupported.GetSupportState(),
                Is.EqualTo(FontTableSupportState.Unsupported));
            Assert.That(unknown.GetKind(), Is.EqualTo(FontTableKind.Unknown));
            Assert.That(unknown.HasVersion(), Is.False);
            Assert.That(unknown.GetSupportState(), Is.EqualTo(FontTableSupportState.Unknown));
        });
    }

    [Test]
    public void ReportsChecksumMismatchAndEmissionRebuildsValidDeterministicBytes()
    {
        byte[] source = BuildCompleteFont();
        int nameOffset = GetTableOffset(source, FontTags.Name);
        source[nameOffset] ^= 0x7F;
        FontFace face = new FontLoader()
            .LoadFromBytes(source, FontLoadOptions.CreateCompatible())
            .GetFace(0);
        FontTableInfo name = face.FindTableInfo(FontTags.Name)!;
        byte[] first = face.CopyStandaloneSfntBytes();
        byte[] second = face.CopyStandaloneSfntBytes();
        first[0] = 0xFF;

        Assert.Multiple(() =>
        {
            Assert.That(face.IsStructurallyValid(), Is.True);
            Assert.That(face.GetChecksumValidationState(),
                Is.EqualTo(FontChecksumValidationState.Invalid));
            Assert.That(name.GetChecksumValidationState(),
                Is.EqualTo(FontChecksumValidationState.Invalid));
            Assert.That(name.GetAlignmentState(), Is.EqualTo(FontTableAlignmentState.Valid));
            Assert.That(second[0], Is.EqualTo(0));
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(face.CopyStandaloneSfntBytes(), Is.EqualTo(second));
            Assert.That(FontChecksum.VerifyFont(second), Is.True);
            Assert.That(FontChecksum.VerifyTable(
                CopyTable(second, FontTags.Name),
                GetTableChecksum(second, FontTags.Name)), Is.True);
        });
    }

    [Test]
    public void ReportsNonzeroAndMissingPaddingAndCanonicalizesValidFaceOutput()
    {
        byte[] nonzero = BuildCompleteFont();
        int unknownOffset = GetTableOffset(
            nonzero,
            (int)TestSfntEncoding.EncodeTag("ZZZZ"));
        nonzero[unknownOffset + 1] = 9;
        FontFace compatible = new FontLoader()
            .LoadFromBytes(nonzero, FontLoadOptions.CreateCompatible())
            .GetFace(0);
        FontTableInfo nonzeroTable = compatible.FindTableInfo(
            (int)TestSfntEncoding.EncodeTag("ZZZZ"))!;
        byte[] canonical = compatible.CopyStandaloneSfntBytes();
        int canonicalOffset = GetTableOffset(
            canonical,
            (int)TestSfntEncoding.EncodeTag("ZZZZ"));

        byte[] missing = new TinySfntBuilder()
            .AddTable("name", new byte[] { 1 })
            .Build();
        Array.Resize(ref missing, missing.Length - 3);
        FontFace inspectOnly = new FontLoader()
            .LoadFromBytes(missing, FontLoadOptions.CreateInspectOnly())
            .GetFace(0);
        FontTableInfo missingTable = inspectOnly.FindTableInfo(FontTags.Name)!;

        Assert.Multiple(() =>
        {
            Assert.That(nonzeroTable.GetAlignmentState(),
                Is.EqualTo(FontTableAlignmentState.NonZeroPadding));
            Assert.That(canonical[canonicalOffset + 1], Is.Zero);
            Assert.That(FontChecksum.VerifyFont(canonical), Is.True);
            Assert.That(missingTable.GetAlignmentState(),
                Is.EqualTo(FontTableAlignmentState.MissingPadding));
            Assert.That(inspectOnly.IsStructurallyValid(), Is.False);
            Assert.Throws<InvalidOperationException>(() =>
                inspectOnly.CopyStandaloneSfntBytes());
        });
    }

    [Test]
    public void ReportsMisalignedOffsetsAndEmitsWithoutRawPreservation()
    {
        byte[] misaligned = new TinySfntBuilder()
            .AddTable("name", new byte[] { 1, 2 })
            .Build();
        int recordOffset = FindRecordOffset(misaligned, FontTags.Name);
        int originalOffset = GetTableOffset(misaligned, FontTags.Name);
        BinaryPrimitives.WriteUInt32BigEndian(
            misaligned.AsSpan(recordOffset + 8, 4),
            (uint)(originalOffset + 1));
        FontFace invalid = new FontLoader()
            .LoadFromBytes(misaligned, FontLoadOptions.CreateInspectOnly())
            .GetFace(0);

        FontFace withoutRaw = new FontLoader()
            .LoadFromBytes(BuildCompleteFont(), WithoutRawPreservation())
            .GetFace(0);
        FontTableInfo head = withoutRaw.FindTableInfo(FontTags.Head)!;
        byte[] output = withoutRaw.CopyStandaloneSfntBytes();

        Assert.Multiple(() =>
        {
            Assert.That(invalid.FindTableInfo(FontTags.Name)!.GetAlignmentState(),
                Is.EqualTo(FontTableAlignmentState.MisalignedOffset));
            Assert.That(invalid.IsStructurallyValid(), Is.False);
            Assert.That(head.HasRawData(), Is.False);
            Assert.Throws<InvalidOperationException>(() => head.CopyData());
            Assert.That(FontChecksum.VerifyFont(output), Is.True);
            Assert.That(withoutRaw.CopyStandaloneSfntBytes(), Is.EqualTo(output));
        });
    }

    static FontLoadOptions WithoutRawPreservation()
    {
        return FontLoadOptions.Create(
            FontLoadMode.Compatible,
            FontInputOwnership.Copy,
            false,
            FontRepairPolicy.Safe,
            FontChecksumPolicy.ReportMismatch,
            FontLimits.CreateCompatible(),
            CancellationToken.None);
    }

    static byte[] BuildCompleteFont()
    {
        return new TinySfntBuilder()
            .AddTable("BASE", new byte[] { 0, 1, 0, 2 })
            .AddTable("ZZZZ", new byte[] { 7 })
            .AddTable("cmap", new byte[4])
            .AddTable("glyf", new byte[4])
            .AddTable("head", new byte[] { 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 })
            .AddTable("hhea", new byte[] { 0, 1, 0, 0 })
            .AddTable("hmtx", new byte[4])
            .AddTable("loca", new byte[4])
            .AddTable("maxp", new byte[] { 0, 1, 0, 0 })
            .AddTable("morx", new byte[] { 1 })
            .AddTable("name", new byte[4])
            .AddTable("OS/2", new byte[4])
            .AddTable("post", new byte[] { 0, 3, 0, 0 })
            .Build();
    }

    static int FindRecordOffset(byte[] font, int tag)
    {
        int tableCount = BinaryPrimitives.ReadUInt16BigEndian(font.AsSpan(4, 2));
        int index = 0;

        while (index < tableCount)
        {
            int recordOffset = 12 + index * 16;
            int recordTag = BinaryPrimitives.ReadInt32BigEndian(
                font.AsSpan(recordOffset, 4));

            if (recordTag == tag)
            {
                return recordOffset;
            }

            index += 1;
        }

        throw new InvalidOperationException("The requested test table is absent.");
    }

    static int GetTableOffset(byte[] font, int tag)
    {
        int recordOffset = FindRecordOffset(font, tag);
        return (int)BinaryPrimitives.ReadUInt32BigEndian(
            font.AsSpan(recordOffset + 8, 4));
    }

    static uint GetTableChecksum(byte[] font, int tag)
    {
        int recordOffset = FindRecordOffset(font, tag);
        return BinaryPrimitives.ReadUInt32BigEndian(
            font.AsSpan(recordOffset + 4, 4));
    }

    static byte[] CopyTable(byte[] font, int tag)
    {
        int recordOffset = FindRecordOffset(font, tag);
        int offset = (int)BinaryPrimitives.ReadUInt32BigEndian(
            font.AsSpan(recordOffset + 8, 4));
        int length = (int)BinaryPrimitives.ReadUInt32BigEndian(
            font.AsSpan(recordOffset + 12, 4));
        return font.AsSpan(offset, length).ToArray();
    }
}
