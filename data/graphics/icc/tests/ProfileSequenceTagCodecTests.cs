using icc;

namespace icc.Tests;

public class ProfileSequenceTagCodecTests
{
    [Test]
    public void Version2ProfileSequenceDescriptionRoundTripsCompleteLegacyRecords()
    {
        IccProfileSequenceEntry entry = new IccProfileSequenceEntry();
        entry.DeviceManufacturerSignature = 0x41434d45;
        entry.DeviceModelSignature = 0x4d4f444c;
        entry.DeviceAttributes = 0x0102030405060708;
        entry.TechnologySignature = 0x696a6574;
        entry.ManufacturerDescription = CreateLegacyDescription("Acme", "Acme Unicode", 0x656e5553);
        entry.ModelDescription = CreateLegacyDescription("Model One", "Model One Unicode", 0x656e5553);

        byte[] payload = ProfileSequenceTagCodec.SerializeProfileSequenceDescription(new[] { entry }, IccVersionKind.Version2);
        TagDecodeState state = DecodePayload(payload, IccVersionKind.Version2, true);
        IccProfileSequenceEntry decoded = state.Data.ProfileSequence[0];
        int manufacturerSize = LegacyTagCodec.SerializeTextDescription(entry.ManufacturerDescription).Length;

        Assert.Multiple(() =>
        {
            Assert.That(ReadUInt32(payload, 32), Is.EqualTo(IccTypeSignatures.TextDescription));
            Assert.That(ReadUInt32(payload, 32 + manufacturerSize), Is.EqualTo(IccTypeSignatures.TextDescription));
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.ProfileSequence));
            Assert.That(state.Data.RawData.Bytes, Is.EqualTo(payload));
            Assert.That(decoded.DeviceManufacturerSignature, Is.EqualTo(0x41434d45));
            Assert.That(decoded.DeviceModelSignature, Is.EqualTo(0x4d4f444c));
            Assert.That(decoded.DeviceAttributes, Is.EqualTo(0x0102030405060708));
            Assert.That(decoded.TechnologySignature, Is.EqualTo(0x696a6574));
            Assert.That(decoded.ManufacturerDescription.AsciiText, Is.EqualTo("Acme"));
            Assert.That(decoded.ManufacturerDescription.UnicodeText, Is.EqualTo("Acme Unicode"));
            Assert.That(decoded.ModelDescription.Text, Is.EqualTo("Model One Unicode"));
            Assert.That(decoded.ManufacturerLocalizedDescriptions, Is.Empty);
            Assert.That(decoded.ModelLocalizedDescriptions, Is.Empty);
        });
    }

    [Test]
    public void Version4ProfileSequenceDescriptionKeepsEveryLocaleAndPlaceholders()
    {
        IccProfileSequenceEntry described = new IccProfileSequenceEntry();
        described.DeviceManufacturerSignature = 0x6d616e75;
        described.DeviceModelSignature = 0x6d6f646c;
        described.DeviceAttributes = 9;
        described.TechnologySignature = 0x64696769;
        described.ManufacturerLocalizedDescriptions = new[]
        {
            CreateLocalized("en", "US", "Maker"),
            CreateLocalized("fr", "FR", "Fabricant")
        };
        described.ModelLocalizedDescriptions = new[] { CreateLocalized("ja", "JP", "Model") };
        IccProfileSequenceEntry placeholder = new IccProfileSequenceEntry();

        byte[] payload = ProfileSequenceTagCodec.SerializeProfileSequenceDescription(new[] { described, placeholder }, IccVersionKind.Version4);
        TagDecodeState state = DecodePayload(payload, IccVersionKind.Version4, false);
        IccProfileSequenceEntry first = state.Data.ProfileSequence[0];
        IccProfileSequenceEntry second = state.Data.ProfileSequence[1];

        Assert.Multiple(() =>
        {
            Assert.That(ReadUInt32(payload, 32), Is.EqualTo(IccTypeSignatures.MultiLocalizedUnicode));
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.ProfileSequence, Has.Length.EqualTo(2));
            Assert.That(first.ManufacturerLocalizedDescriptions.Select(value => value.Text), Is.EqualTo(new[] { "Maker", "Fabricant" }));
            Assert.That(first.ManufacturerLocalizedDescriptions.Select(value => value.Language), Is.EqualTo(new[] { "en", "fr" }));
            Assert.That(first.ModelLocalizedDescriptions[0].Country, Is.EqualTo("JP"));
            Assert.That(first.ManufacturerDescription.TypeSignature, Is.EqualTo(IccTypeSignatures.MultiLocalizedUnicode));
            Assert.That(first.ManufacturerDescription.Text, Is.EqualTo("Maker"));
            Assert.That(first.ManufacturerDescription.Bytes, Is.Not.Empty);
            Assert.That(second.ManufacturerLocalizedDescriptions, Is.Empty);
            Assert.That(second.ModelLocalizedDescriptions, Is.Empty);
            Assert.That(second.ManufacturerDescription.Bytes, Has.Length.EqualTo(16));
        });
    }

    [Test]
    public void ProfileSequenceDescriptionRejectsWrongGenerationPaddingAndTrailingData()
    {
        IccProfileSequenceEntry entry = new IccProfileSequenceEntry();
        entry.ManufacturerLocalizedDescriptions = new[] { CreateLocalized("en", "US", "A") };
        entry.ModelLocalizedDescriptions = new[] { CreateLocalized("en", "US", "B") };
        byte[] valid = ProfileSequenceTagCodec.SerializeProfileSequenceDescription(new[] { entry }, IccVersionKind.Version4);
        byte[] wrongPadding = valid.ToArray();
        wrongPadding[62] = 1;
        byte[] trailing = valid.Append((byte)0).ToArray();

        TagDecodeState generationState = DecodePayload(valid, IccVersionKind.Version2, false);
        TagDecodeState paddingState = DecodePayload(wrongPadding, IccVersionKind.Version4, false);
        TagDecodeState trailingState = DecodePayload(trailing, IccVersionKind.Version4, false);

        Assert.Multiple(() =>
        {
            Assert.That(generationState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(generationState.Result.Message, Does.Contain("minimum-sized records"));
            Assert.That(paddingState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(paddingState.Result.Message, Does.Contain("zero alignment padding"));
            Assert.That(trailingState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(trailingState.Result.Message, Does.Contain("unexpected trailing bytes"));
        });
    }

    [Test]
    public void ProfileSequenceIdentifierRoundTripsProfileIdsAndSharesIdenticalStructures()
    {
        IccProfileSequenceIdentifier first = CreateIdentifier(1, "First");
        IccProfileSequenceIdentifier equivalent = CreateIdentifier(1, "First");
        IccProfileSequenceIdentifier third = CreateIdentifier(2, "Third");

        byte[] payload = ProfileSequenceTagCodec.SerializeProfileSequenceIdentifier(new[] { first, equivalent, third });
        TagDecodeState state = DecodePayload(payload, IccVersionKind.Version4, false);
        IccTagData clone = ProfileClone.CloneTagDataStandalone(state.Data);

        Assert.Multiple(() =>
        {
            Assert.That(ReadUInt32(payload, 12), Is.EqualTo(ReadUInt32(payload, 20)));
            Assert.That(ReadUInt32(payload, 16), Is.EqualTo(ReadUInt32(payload, 24)));
            Assert.That(ReadUInt32(payload, 28), Is.Not.EqualTo(ReadUInt32(payload, 12)));
            Assert.That(payload.Length % 4, Is.Zero);
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Result.Message, Does.Contain("1 shared position references"));
            Assert.That(state.Data.ProfileSequenceIdentifiers, Has.Length.EqualTo(3));
            Assert.That(state.Data.ProfileSequenceIdentifiers[0], Is.SameAs(state.Data.ProfileSequenceIdentifiers[1]));
            Assert.That(state.Data.ProfileSequenceIdentifiers[0].ProfileId.IsPresent, Is.True);
            Assert.That(state.Data.ProfileSequenceIdentifiers[0].ProfileId.Bytes[0], Is.EqualTo(1));
            Assert.That(state.Data.ProfileSequenceIdentifiers[2].Descriptions[0].Text, Is.EqualTo("Third"));
            Assert.That(clone.ProfileSequenceIdentifiers[0], Is.SameAs(clone.ProfileSequenceIdentifiers[1]));
            Assert.That(clone.ProfileSequenceIdentifiers[0], Is.Not.SameAs(state.Data.ProfileSequenceIdentifiers[0]));
        });
    }

    [Test]
    public void ProfileSequenceIdentifierRejectsPartialOverlapWrongDescriptionAndHiddenTrailingBytes()
    {
        byte[] valid = ProfileSequenceTagCodec.SerializeProfileSequenceIdentifier(new[] { CreateIdentifier(1, "One"), CreateIdentifier(2, "Two") });
        int firstOffset = (int)ReadUInt32(valid, 12);
        byte[] overlap = valid.ToArray();
        WriteUInt32(overlap, 20, (uint)(firstOffset + 4));
        byte[] wrongDescription = valid.ToArray();
        WriteUInt32(wrongDescription, firstOffset + 16, IccTypeSignatures.TextDescription);
        byte[] hiddenBytes = ProfileSequenceTagCodec.SerializeProfileSequenceIdentifier(new[] { CreateIdentifier(1, "One") });
        WriteUInt32(hiddenBytes, 16, ReadUInt32(hiddenBytes, 16) + 2);

        TagDecodeState overlapState = DecodePayload(overlap, IccVersionKind.Version4, false);
        TagDecodeState descriptionState = DecodePayload(wrongDescription, IccVersionKind.Version4, false);
        TagDecodeState hiddenState = DecodePayload(hiddenBytes, IccVersionKind.Version4, false);

        Assert.Multiple(() =>
        {
            Assert.That(overlapState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(overlapState.Result.Message, Does.Contain("overlap"));
            Assert.That(descriptionState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(descriptionState.Result.Message, Does.Contain("multiLocalizedUnicodeType"));
            Assert.That(hiddenState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(hiddenState.Result.Message, Does.Contain("does not exactly fill"));
        });
    }

    [Test]
    public void ProfileSequenceWritersRejectUnsupportedVersionsAndIncompleteIdentifiers()
    {
        IccProfileSequenceIdentifier invalid = CreateIdentifier(1, "Invalid");
        invalid.ProfileId.Bytes = new byte[15];

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileWriteException>(() => ProfileSequenceTagCodec.SerializeProfileSequenceDescription(new IccProfileSequenceEntry[0], IccVersionKind.Version5));
            Assert.Throws<IccProfileWriteException>(() => ProfileSequenceTagCodec.SerializeProfileSequenceIdentifier(new[] { invalid }));
        });
    }

    private static IccTextData CreateLegacyDescription(string asciiText, string unicodeText, uint languageCode)
    {
        IccTextData text = new IccTextData();
        text.Text = unicodeText;
        text.AsciiText = asciiText;
        text.UnicodeText = unicodeText;
        text.UnicodeLanguageCode = languageCode;
        return text;
    }

    private static IccLocalizedString CreateLocalized(string language, string country, string text)
    {
        IccLocalizedString localized = new IccLocalizedString();
        localized.Language = language;
        localized.Country = country;
        localized.Text = text;
        return localized;
    }

    private static IccProfileSequenceIdentifier CreateIdentifier(byte firstIdByte, string text)
    {
        IccProfileSequenceIdentifier identifier = new IccProfileSequenceIdentifier();
        identifier.ProfileId.Bytes[0] = firstIdByte;
        identifier.ProfileId.IsPresent = firstIdByte != 0;
        identifier.Descriptions = new[] { CreateLocalized("en", "US", text) };
        return identifier;
    }

    private static TagDecodeState DecodePayload(byte[] payload, IccVersionKind version, bool preserveOriginalBytes)
    {
        TagDecodeSource source = new TagDecodeSource(payload);
        TagDecodeState state = new TagDecodeState(source, 0, payload.Length, 0, version, preserveOriginalBytes);
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static uint ReadUInt32(byte[] source, int offset)
    {
        return ((uint)source[offset] << 24)
            | ((uint)source[offset + 1] << 16)
            | ((uint)source[offset + 2] << 8)
            | source[offset + 3];
    }

    private static void WriteUInt32(byte[] destination, int offset, uint value)
    {
        destination[offset] = (byte)(value >> 24);
        destination[offset + 1] = (byte)(value >> 16);
        destination[offset + 2] = (byte)(value >> 8);
        destination[offset + 3] = (byte)value;
    }
}
