using icc;

namespace icc.Tests;

public class DeterministicSerializationTests
{
    [Test]
    public void CanonicalFloatsNormalizeSignedZeroWhilePreservationKeepsItsBits()
    {
        double negativeZero = BitConverter.Int64BitsToDouble(unchecked((long)0x8000000000000000));
        byte[] canonical16 = Icc2ArrayTagCodec.SerializeFloatingPointArrayForMode(IccTypeSignatures.Float16Array, new[] { negativeZero }, IccWriteMode.Canonical);
        byte[] canonical32 = Icc2ArrayTagCodec.SerializeFloatingPointArrayForMode(IccTypeSignatures.Float32Array, new[] { negativeZero }, IccWriteMode.Canonical);
        byte[] canonical64 = Icc2ArrayTagCodec.SerializeFloatingPointArrayForMode(IccTypeSignatures.Float64Array, new[] { negativeZero }, IccWriteMode.Canonical);
        byte[] preserved16 = Icc2ArrayTagCodec.SerializeFloatingPointArrayForMode(IccTypeSignatures.Float16Array, new[] { negativeZero }, IccWriteMode.Preservation);
        byte[] preserved32 = Icc2ArrayTagCodec.SerializeFloatingPointArrayForMode(IccTypeSignatures.Float32Array, new[] { negativeZero }, IccWriteMode.Preservation);
        byte[] preserved64 = Icc2ArrayTagCodec.SerializeFloatingPointArrayForMode(IccTypeSignatures.Float64Array, new[] { negativeZero }, IccWriteMode.Preservation);
        IccTag tag = FloatingPointTag(IccTypeSignatures.Float64Array, negativeZero);
        IccWriteOptions canonicalOptions = new IccWriteOptions();
        IccWriteOptions preservationOptions = new IccWriteOptions();
        preservationOptions.Mode = IccWriteMode.Preservation;
        byte[] writerCanonical = ProfileTagSerializer.SerializeTag(tag, new IccProfileHeader(), canonicalOptions);
        byte[] writerPreserved = ProfileTagSerializer.SerializeTag(tag, new IccProfileHeader(), preservationOptions);

        Assert.Multiple(() =>
        {
            Assert.That(canonical16.Skip(8), Is.All.Zero);
            Assert.That(canonical32.Skip(8), Is.All.Zero);
            Assert.That(canonical64.Skip(8), Is.All.Zero);
            Assert.That(preserved16.Skip(8), Is.EqualTo(new byte[] { 0x80, 0x00 }));
            Assert.That(preserved32.Skip(8), Is.EqualTo(new byte[] { 0x80, 0x00, 0x00, 0x00 }));
            Assert.That(preserved64.Skip(8), Is.EqualTo(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }));
            Assert.That(writerCanonical, Is.EqualTo(canonical64));
            Assert.That(writerPreserved, Is.EqualTo(preserved64));
            Assert.That(IccNumberCodec.EncodeS15Fixed16(0.5 / IccNumberCodec.S15Fixed16Scale), Is.EqualTo(1));
            Assert.That(IccNumberCodec.EncodeS15Fixed16(-0.5 / IccNumberCodec.S15Fixed16Scale), Is.EqualTo(-1));
        });
    }

    [Test]
    public void CanonicalTextUsesNfcAndTypeSpecificTerminatorRules()
    {
        IccTagData text = CreateText(IccTypeSignatures.Utf8, "Cafe\u0301");
        byte[] canonical = SerializeNested(text, IccWriteMode.Canonical);
        byte[] preserved = SerializeNested(text, IccWriteMode.Preservation);
        byte[] expectedCanonical = System.Text.Encoding.UTF8.GetBytes("Caf\u00e9");
        byte[] expectedPreserved = System.Text.Encoding.UTF8.GetBytes("Cafe\u0301");
        byte[] terminated = NestedTagDataSerializer.SerializeUtf8Data("Caf\u00e9");

        Assert.Multiple(() =>
        {
            Assert.That(canonical.Skip(8), Is.EqualTo(expectedCanonical));
            Assert.That(preserved.Skip(8), Is.EqualTo(expectedPreserved));
            Assert.That(canonical[^1], Is.Not.Zero);
            Assert.That(terminated.Skip(12), Is.EqualTo(new byte[] { 0x43, 0x61, 0x66, 0xc3, 0xa9, 0x00 }));
            Assert.Throws<IccProfileWriteException>(() => NestedTagDataSerializer.SerializeAsciiText("A\0B"));
            Assert.Throws<IccProfileWriteException>(() => NestedTagDataSerializer.SerializeUtf8Data("A\0B"));
        });
    }

    [Test]
    public void CanonicalLocalesDictionariesAndStructuresHaveValueBasedOrdering()
    {
        IccLocalizedString englishDecomposed = Localized("en", "US", "Cafe\u0301");
        IccLocalizedString englishComposed = Localized("en", "US", "Caf\u00e9");
        IccLocalizedString french = Localized("fr", "CA", "Couleur");
        byte[] localizedFirst = LocalizedUnicodeTagCodec.SerializeMultiLocalizedUnicodeForMode(new[] { french, englishDecomposed }, IccWriteMode.Canonical);
        byte[] localizedSecond = LocalizedUnicodeTagCodec.SerializeMultiLocalizedUnicodeForMode(new[] { englishComposed, french }, IccWriteMode.Canonical);
        byte[] localizedPreserved = LocalizedUnicodeTagCodec.SerializeMultiLocalizedUnicodeForMode(new[] { french, englishDecomposed }, IccWriteMode.Preservation);

        IccMetadataEntry zulu = Metadata("Zulu", "Cafe\u0301");
        IccMetadataEntry alpha = Metadata("Alpha", "One");
        byte[] dictionaryFirst = DictionaryTagCodec.SerializeDictionaryForMode(new[] { zulu, alpha }, IccWriteMode.Canonical);
        byte[] dictionarySecond = DictionaryTagCodec.SerializeDictionaryForMode(new[] { Metadata("Alpha", "One"), Metadata("Zulu", "Caf\u00e9") }, IccWriteMode.Canonical);
        byte[] dictionaryPreserved = DictionaryTagCodec.SerializeDictionaryForMode(new[] { zulu, alpha }, IccWriteMode.Preservation);

        IccStructureMember high = Member(0x7a7a7a7a, "high");
        IccStructureMember low = Member(0x61616161, "low");
        IccTagStructure firstStructure = Structure(new[] { high, low });
        IccTagStructure secondStructure = Structure(new[] { low, high });
        byte[] canonicalFirst = HierarchicalTagCodec.SerializeTagStructureForMode(firstStructure, 4, IccWriteMode.Canonical);
        byte[] canonicalSecond = HierarchicalTagCodec.SerializeTagStructureForMode(secondStructure, 4, IccWriteMode.Canonical);
        byte[] preserved = HierarchicalTagCodec.SerializeTagStructureForMode(firstStructure, 4, IccWriteMode.Preservation);

        Assert.Multiple(() =>
        {
            Assert.That(localizedFirst, Is.EqualTo(localizedSecond));
            Assert.That(ReadUInt16(localizedFirst, 16), Is.EqualTo(0x656e));
            Assert.That(ReadUInt16(localizedPreserved, 16), Is.EqualTo(0x6672));
            Assert.That(dictionaryFirst, Is.EqualTo(dictionarySecond));
            Assert.That(ReadUtf16(dictionaryFirst, (int)ReadUInt32(dictionaryFirst, 16), (int)ReadUInt32(dictionaryFirst, 20)), Is.EqualTo("Alpha"));
            Assert.That(ReadUtf16(dictionaryPreserved, (int)ReadUInt32(dictionaryPreserved, 16), (int)ReadUInt32(dictionaryPreserved, 20)), Is.EqualTo("Zulu"));
            Assert.That(ReadUInt32(canonicalFirst, 16), Is.EqualTo(0x61616161));
            Assert.That(canonicalFirst, Is.EqualTo(canonicalSecond));
            Assert.That(ReadUInt32(preserved, 16), Is.EqualTo(0x7a7a7a7a));
            Assert.That(preserved, Is.Not.EqualTo(canonicalFirst));
        });
    }

    private static byte[] SerializeNested(IccTagData data, IccWriteMode mode)
    {
        HierarchicalWriteContext context = new HierarchicalWriteContext(4);
        context.Mode = mode;
        return NestedTagDataSerializer.Serialize(data, 0, context);
    }

    private static IccTagData CreateText(uint typeSignature, string value)
    {
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.Text;
        data.Text = new IccTextData();
        data.Text.TypeSignature = typeSignature;
        data.Text.Text = value;
        return data;
    }

    private static IccTag FloatingPointTag(uint typeSignature, double value)
    {
        IccTag tag = new IccTag();
        tag.Signature = 0x74657374;
        tag.TableEntry.Signature = tag.Signature;
        tag.Data.Kind = IccTagDataKind.FloatingPointArray;
        tag.Data.RawData.HasTypeSignature = true;
        tag.Data.RawData.TypeSignature = typeSignature;
        tag.Data.FloatingPointValues = new[] { value };
        return tag;
    }

    private static IccLocalizedString Localized(string language, string country, string text)
    {
        IccLocalizedString value = new IccLocalizedString();
        value.Language = language;
        value.Country = country;
        value.Text = text;
        return value;
    }

    private static IccMetadataEntry Metadata(string name, string value)
    {
        IccMetadataEntry entry = new IccMetadataEntry();
        entry.Name = name;
        entry.Value = value;
        entry.HasValue = true;
        return entry;
    }

    private static IccStructureMember Member(uint signature, string text)
    {
        IccStructureMember member = new IccStructureMember();
        member.Signature = signature;
        member.Data = CreateText(IccTypeSignatures.Utf8, text);
        return member;
    }

    private static IccTagStructure Structure(IccStructureMember[] members)
    {
        IccTagStructure structure = new IccTagStructure();
        structure.StructureSignature = 0x74657374;
        structure.Members = members;
        return structure;
    }

    private static ushort ReadUInt16(byte[] source, int offset)
    {
        return (ushort)((source[offset] << 8) | source[offset + 1]);
    }

    private static uint ReadUInt32(byte[] source, int offset)
    {
        return ((uint)source[offset] << 24) | ((uint)source[offset + 1] << 16) | ((uint)source[offset + 2] << 8) | source[offset + 3];
    }

    private static string ReadUtf16(byte[] source, int offset, int length)
    {
        return System.Text.Encoding.BigEndianUnicode.GetString(source, offset, length);
    }
}
