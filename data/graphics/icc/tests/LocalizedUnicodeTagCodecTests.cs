using icc;

namespace icc.Tests;

public class LocalizedUnicodeTagCodecTests
{
    [TestCase(IccVersionKind.Version4)]
    [TestCase(IccVersionKind.Version5)]
    public void MultiLocalizedUnicodeDecodesStableRecordsAndSharedStorage(IccVersionKind version)
    {
        byte[] payload = CreateSharedStringPayload();

        TagDecodeState state = DecodePayload(payload, version, true);
        IccTagData clone = ProfileClone.CloneTagDataStandalone(state.Data);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.LocalizedText));
            Assert.That(state.Data.LocalizedStrings, Has.Length.EqualTo(3));
            Assert.That(state.Data.LocalizedStrings[0].Language, Is.EqualTo("fr"));
            Assert.That(state.Data.LocalizedStrings[0].Country, Is.EqualTo("CA"));
            Assert.That(state.Data.LocalizedStrings[0].Text, Is.EqualTo("Couleur"));
            Assert.That(state.Data.LocalizedStrings[1].Language, Is.EqualTo("en"));
            Assert.That(state.Data.LocalizedStrings[1].Country, Is.EqualTo("US"));
            Assert.That(state.Data.LocalizedStrings[1].Text, Is.EqualTo("Color \U0001f600"));
            Assert.That(state.Data.LocalizedStrings[2].Country, Is.EqualTo("GB"));
            Assert.That(state.Data.LocalizedStrings[2].RawBytes, Is.SameAs(state.Data.LocalizedStrings[1].RawBytes));
            Assert.That(clone.LocalizedStrings[2].RawBytes, Is.SameAs(clone.LocalizedStrings[1].RawBytes));
            Assert.That(clone.LocalizedStrings[1].RawBytes, Is.Not.SameAs(state.Data.LocalizedStrings[1].RawBytes));
        });
    }

    [Test]
    public void LocalizedLookupUsesExactLanguageThenFirstRecordFallback()
    {
        TagDecodeState state = DecodePayload(CreateSharedStringPayload(), IccVersionKind.Version4, false);
        IccLocalizedString[] values = state.Data.LocalizedStrings;

        IccLocalizedString exact = LocalizedUnicodeTagCodec.FindLocalizedString(values, "EN", "gb")!;
        IccLocalizedString language = LocalizedUnicodeTagCodec.FindLocalizedString(values, "en", "AU")!;
        IccLocalizedString first = LocalizedUnicodeTagCodec.FindLocalizedString(values, "de", "DE")!;
        IccLocalizedString? empty = LocalizedUnicodeTagCodec.FindLocalizedString(new IccLocalizedString[0], "en", "US");

        Assert.Multiple(() =>
        {
            Assert.That(exact, Is.SameAs(values[2]));
            Assert.That(language, Is.SameAs(values[1]));
            Assert.That(first, Is.SameAs(values[0]));
            Assert.That(empty, Is.Null);
        });
    }

    [Test]
    public void CanonicalSerializationPreservesLocaleOrderAndSharesIdenticalStrings()
    {
        IccLocalizedString[] values = new IccLocalizedString[]
        {
            CreateLocalizedString("fr", "CA", "Couleur"),
            CreateLocalizedString("en", "US", "Color"),
            CreateLocalizedString("en", "GB", "Color")
        };

        byte[] payload = LocalizedUnicodeTagCodec.SerializeMultiLocalizedUnicode(values);
        TagDecodeState state = DecodePayload(payload, IccVersionKind.Version4, false);

        Assert.Multiple(() =>
        {
            Assert.That(ReadUInt32(payload, 8), Is.EqualTo(3));
            Assert.That(ReadUInt32(payload, 12), Is.EqualTo(12));
            Assert.That(ReadUInt32(payload, 36), Is.EqualTo(ReadUInt32(payload, 48)));
            Assert.That(state.Data.LocalizedStrings.Select(value => $"{value.Language}-{value.Country}"), Is.EqualTo(new[] { "fr-CA", "en-US", "en-GB" }));
            Assert.That(state.Data.LocalizedStrings[1].RawBytes, Is.SameAs(state.Data.LocalizedStrings[2].RawBytes));
        });
    }

    [Test]
    public void DuplicateLocalesAreReportedWithoutDiscardingTheirRecords()
    {
        IccLocalizedString[] values = new IccLocalizedString[]
        {
            CreateLocalizedString("en", "US", "First"),
            CreateLocalizedString("en", "US", "Second")
        };
        byte[] payload = LocalizedUnicodeTagCodec.SerializeMultiLocalizedUnicode(values);

        TagDecodeState state = DecodePayload(payload, IccVersionKind.Version4, false);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Result.Message, Does.Contain("1 duplicate locale record"));
            Assert.That(state.Data.LocalizedStrings.Select(value => value.Text), Is.EqualTo(new[] { "First", "Second" }));
        });
    }

    [Test]
    public void MultiLocalizedUnicodeRejectsInvalidUtf16AndRecordLocations()
    {
        byte[] invalidUtf16 = CreateSingleRecordPayload();
        invalidUtf16[28] = 0xd8;
        invalidUtf16[29] = 0x00;
        byte[] oddLength = CreateSingleRecordPayload();
        WriteUInt32(oddLength, 20, 1);
        byte[] tableReference = CreateSingleRecordPayload();
        WriteUInt32(tableReference, 24, 16);
        byte[] wrongRecordSize = CreateSingleRecordPayload();
        WriteUInt32(wrongRecordSize, 12, 16);

        TagDecodeState invalidUtf16State = DecodePayload(invalidUtf16, IccVersionKind.Version4, true);
        TagDecodeState oddLengthState = DecodePayload(oddLength, IccVersionKind.Version4, false);
        TagDecodeState tableReferenceState = DecodePayload(tableReference, IccVersionKind.Version4, false);
        TagDecodeState wrongRecordSizeState = DecodePayload(wrongRecordSize, IccVersionKind.Version4, false);

        Assert.Multiple(() =>
        {
            Assert.That(invalidUtf16State.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(invalidUtf16State.Result.Message, Does.Contain("invalid UTF-16BE"));
            Assert.That(invalidUtf16State.Data.RawData.Bytes, Is.EqualTo(invalidUtf16));
            Assert.That(oddLengthState.Result.Message, Does.Contain("odd UTF-16BE byte length"));
            Assert.That(tableReferenceState.Result.Message, Does.Contain("string-storage area"));
            Assert.That(wrongRecordSizeState.Result.Message, Does.Contain("record size shall be 12"));
        });
    }

    [Test]
    public void CanonicalSerializationRejectsInvalidLocalesAndSurrogates()
    {
        IccLocalizedString invalidLanguage = CreateLocalizedString("EN", "US", "Text");
        IccLocalizedString invalidCountry = CreateLocalizedString("en", "us", "Text");
        IccLocalizedString invalidUnicode = CreateLocalizedString("en", "US", "\ud800");

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileWriteException>(() => LocalizedUnicodeTagCodec.SerializeMultiLocalizedUnicode(new[] { invalidLanguage }));
            Assert.Throws<IccProfileWriteException>(() => LocalizedUnicodeTagCodec.SerializeMultiLocalizedUnicode(new[] { invalidCountry }));
            Assert.Throws<IccProfileWriteException>(() => LocalizedUnicodeTagCodec.SerializeMultiLocalizedUnicode(new[] { invalidUnicode }));
        });
    }

    private static TagDecodeState DecodePayload(byte[] payload, IccVersionKind version, bool preserveOriginalBytes)
    {
        TagDecodeSource source = new TagDecodeSource(payload);
        TagDecodeState state = new TagDecodeState(source, 0, payload.Length, 0, version, preserveOriginalBytes);
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static IccLocalizedString CreateLocalizedString(string language, string country, string text)
    {
        IccLocalizedString value = new IccLocalizedString();
        value.Language = language;
        value.Country = country;
        value.Text = text;
        return value;
    }

    private static byte[] CreateSharedStringPayload()
    {
        byte[] french = System.Text.Encoding.BigEndianUnicode.GetBytes("Couleur");
        byte[] english = System.Text.Encoding.BigEndianUnicode.GetBytes("Color \U0001f600");
        int tableEnd = 52;
        byte[] payload = new byte[tableEnd + french.Length + english.Length];
        WriteUInt32(payload, 0, IccTypeSignatures.MultiLocalizedUnicode);
        WriteUInt32(payload, 8, 3);
        WriteUInt32(payload, 12, 12);
        WriteRecord(payload, 16, "fr", "CA", french.Length, tableEnd);
        WriteRecord(payload, 28, "en", "US", english.Length, tableEnd + french.Length);
        WriteRecord(payload, 40, "en", "GB", english.Length, tableEnd + french.Length);
        System.Array.Copy(french, 0, payload, tableEnd, french.Length);
        System.Array.Copy(english, 0, payload, tableEnd + french.Length, english.Length);
        return payload;
    }

    private static byte[] CreateSingleRecordPayload()
    {
        byte[] payload = new byte[30];
        WriteUInt32(payload, 0, IccTypeSignatures.MultiLocalizedUnicode);
        WriteUInt32(payload, 8, 1);
        WriteUInt32(payload, 12, 12);
        WriteRecord(payload, 16, "en", "US", 2, 28);
        payload[29] = 65;
        return payload;
    }

    private static void WriteRecord(byte[] destination, int offset, string language, string country, int length, int stringOffset)
    {
        WriteUInt16(destination, offset, (ushort)((language[0] << 8) | language[1]));
        WriteUInt16(destination, offset + 2, (ushort)((country[0] << 8) | country[1]));
        WriteUInt32(destination, offset + 4, (uint)length);
        WriteUInt32(destination, offset + 8, (uint)stringOffset);
    }

    private static uint ReadUInt32(byte[] source, int offset)
    {
        return ((uint)source[offset] << 24)
            | ((uint)source[offset + 1] << 16)
            | ((uint)source[offset + 2] << 8)
            | source[offset + 3];
    }

    private static void WriteUInt16(byte[] destination, int offset, ushort value)
    {
        destination[offset] = (byte)(value >> 8);
        destination[offset + 1] = (byte)value;
    }

    private static void WriteUInt32(byte[] destination, int offset, uint value)
    {
        destination[offset] = (byte)(value >> 24);
        destination[offset + 1] = (byte)(value >> 16);
        destination[offset + 2] = (byte)(value >> 8);
        destination[offset + 3] = (byte)value;
    }
}
