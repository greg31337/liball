using icc;

namespace icc.Tests;

public class DictionaryTagCodecTests
{
    [Test]
    public void DictionarySerializesDeterministicallyAndDecodesRegisteredAndUnknownKeys()
    {
        IccMetadataEntry vendor = CreateEntry("VendorKey", "shared");
        vendor.DisplayValues = new[] { CreateLocalized("en", "US", "Shared value") };
        IccMetadataEntry registered = CreateEntry("MediaColor", "shared");
        registered.DisplayNames = new[] { CreateLocalized("en", "US", "Media color") };
        registered.DisplayValues = new[] { CreateLocalized("en", "US", "Shared value") };

        byte[] payload = DictionaryTagCodec.SerializeDictionary(new[] { vendor, registered });
        TagDecodeState state = DecodePayload(payload, IccVersionKind.Version4, true);
        IccTagData clone = ProfileClone.CloneTagDataStandalone(state.Data);

        Assert.Multiple(() =>
        {
            Assert.That(ReadUInt32(payload, 12), Is.EqualTo(32));
            Assert.That(ReadUInt32(payload, 24), Is.EqualTo(ReadUInt32(payload, 56)));
            Assert.That(ReadUInt32(payload, 40), Is.EqualTo(ReadUInt32(payload, 72)));
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Result.Message, Does.Contain("1 names are registered").And.Contain("1 names are retained as unknown"));
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.Dictionary));
            Assert.That(state.Data.Metadata.Select(entry => entry.Name), Is.EqualTo(new[] { "MediaColor", "VendorKey" }));
            Assert.That(state.Data.Metadata[0].IsRegisteredName, Is.True);
            Assert.That(state.Data.Metadata[1].IsRegisteredName, Is.False);
            Assert.That(state.Data.Metadata[0].HasValue, Is.True);
            Assert.That(state.Data.Metadata[0].DisplayNames[0].Text, Is.EqualTo("Media color"));
            Assert.That(state.Data.Metadata[0].HasDisplayNameElement, Is.True);
            Assert.That(state.Data.Metadata[0].HasDisplayValueElement, Is.True);
            Assert.That(state.Data.Metadata[0].DisplayValues, Is.SameAs(state.Data.Metadata[1].DisplayValues));
            Assert.That(clone.Metadata[0].IsRegisteredName, Is.True);
            Assert.That(clone.Metadata[0].HasDisplayValueElement, Is.True);
        });
    }

    [TestCase(IccVersionKind.Version2)]
    [TestCase(IccVersionKind.Version4)]
    [TestCase(IccVersionKind.Version5)]
    public void DictionaryCodecIsAvailableInEverySupportedProfileGeneration(IccVersionKind version)
    {
        byte[] payload = DictionaryTagCodec.SerializeDictionary(new[] { CreateEntry("Custom", "Value") });

        TagDecodeState state = DecodePayload(payload, version, false);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.Metadata, Has.Length.EqualTo(1));
            Assert.That(state.Data.Metadata[0].Name, Is.EqualTo("Custom"));
        });
    }

    [Test]
    public void DictionaryWriterSelectsTheShortestLegalRecordForm()
    {
        IccMetadataEntry plain = CreateEntry("Plain", string.Empty);
        plain.HasValue = true;
        IccMetadataEntry named = CreateEntry("Named", "Value");
        named.DisplayNames = new[] { CreateLocalized("en", "US", "Name") };
        IccMetadataEntry valued = CreateEntry("Valued", "Value");
        valued.DisplayValues = new[] { CreateLocalized("en", "US", "Display value") };

        byte[] sixteen = DictionaryTagCodec.SerializeDictionary(new[] { plain });
        byte[] twentyFour = DictionaryTagCodec.SerializeDictionary(new[] { named });
        byte[] thirtyTwo = DictionaryTagCodec.SerializeDictionary(new[] { valued });

        Assert.Multiple(() =>
        {
            Assert.That(ReadUInt32(sixteen, 12), Is.EqualTo(16));
            Assert.That(ReadUInt32(sixteen, 24), Is.GreaterThan(0));
            Assert.That(ReadUInt32(sixteen, 28), Is.EqualTo(0));
            Assert.That(ReadUInt32(twentyFour, 12), Is.EqualTo(24));
            Assert.That(ReadUInt32(twentyFour, 32), Is.GreaterThan(0));
            Assert.That(ReadUInt32(thirtyTwo, 12), Is.EqualTo(32));
            Assert.That(ReadUInt32(thirtyTwo, 32), Is.EqualTo(0));
            Assert.That(ReadUInt32(thirtyTwo, 40), Is.GreaterThan(0));
            Assert.That(DecodePayload(sixteen, IccVersionKind.Version4, false).Data.Metadata[0].Value, Is.Empty);
            Assert.That(DecodePayload(sixteen, IccVersionKind.Version4, false).Data.Metadata[0].HasValue, Is.True);
        });
    }

    [Test]
    public void DictionaryPreservesUndefinedAndDefinedEmptyPositions()
    {
        IccMetadataEntry undefined = CreateEntry("Undefined", string.Empty);
        IccMetadataEntry emptyDisplay = CreateEntry("NoDisplay", "Value");
        emptyDisplay.HasDisplayNameElement = true;

        byte[] undefinedPayload = DictionaryTagCodec.SerializeDictionary(new[] { undefined });
        byte[] emptyDisplayPayload = DictionaryTagCodec.SerializeDictionary(new[] { emptyDisplay });
        TagDecodeState undefinedState = DecodePayload(undefinedPayload, IccVersionKind.Version4, false);
        TagDecodeState emptyDisplayState = DecodePayload(emptyDisplayPayload, IccVersionKind.Version4, false);

        Assert.Multiple(() =>
        {
            Assert.That(ReadUInt32(undefinedPayload, 24), Is.EqualTo(0));
            Assert.That(undefinedState.Data.Metadata[0].HasValue, Is.False);
            Assert.That(ReadUInt32(emptyDisplayPayload, 12), Is.EqualTo(24));
            Assert.That(ReadUInt32(emptyDisplayPayload, 32), Is.GreaterThan(0));
            Assert.That(ReadUInt32(emptyDisplayPayload, 36), Is.EqualTo(0));
            Assert.That(emptyDisplayState.Data.Metadata[0].HasDisplayNameElement, Is.True);
            Assert.That(emptyDisplayState.Data.Metadata[0].DisplayNames, Is.Empty);
        });
    }

    [Test]
    public void DictionaryRejectsInvalidPositionsEncodingsNamesAndDisplayElements()
    {
        byte[] valid = DictionaryTagCodec.SerializeDictionary(new[] { CreateEntry("Alpha", "A"), CreateEntry("Beta", "B") });
        byte[] badPosition = valid.ToArray();
        WriteUInt32(badPosition, 16, 16);
        byte[] oddNameLength = valid.ToArray();
        WriteUInt32(oddNameLength, 20, 3);
        byte[] duplicateName = valid.ToArray();
        WriteUInt32(duplicateName, 32, ReadUInt32(duplicateName, 16));
        WriteUInt32(duplicateName, 36, ReadUInt32(duplicateName, 20));
        byte[] invalidUtf16 = valid.ToArray();
        int firstNameOffset = (int)ReadUInt32(invalidUtf16, 16);
        invalidUtf16[firstNameOffset] = 0xd8;
        invalidUtf16[firstNameOffset + 1] = 0;
        byte[] wrongRecordSize = valid.ToArray();
        WriteUInt32(wrongRecordSize, 12, 20);

        IccMetadataEntry displayed = CreateEntry("Shown", "Value");
        displayed.DisplayNames = new[] { CreateLocalized("en", "US", "Shown") };
        byte[] badDisplay = DictionaryTagCodec.SerializeDictionary(new[] { displayed });
        int displayOffset = (int)ReadUInt32(badDisplay, 32);
        WriteUInt32(badDisplay, displayOffset, 0x62616421);

        TagDecodeState badPositionState = DecodePayload(badPosition, IccVersionKind.Version4, false);
        TagDecodeState oddLengthState = DecodePayload(oddNameLength, IccVersionKind.Version4, false);
        TagDecodeState duplicateState = DecodePayload(duplicateName, IccVersionKind.Version4, false);
        TagDecodeState invalidUtf16State = DecodePayload(invalidUtf16, IccVersionKind.Version4, true);
        TagDecodeState recordSizeState = DecodePayload(wrongRecordSize, IccVersionKind.Version4, false);
        TagDecodeState displayState = DecodePayload(badDisplay, IccVersionKind.Version4, false);

        Assert.Multiple(() =>
        {
            Assert.That(badPositionState.Result.Message, Does.Contain("non-empty, aligned UTF-16BE name"));
            Assert.That(oddLengthState.Result.Message, Does.Contain("non-empty, aligned UTF-16BE name"));
            Assert.That(duplicateState.Result.Message, Does.Contain("duplicates the name"));
            Assert.That(invalidUtf16State.Result.Message, Does.Contain("invalid UTF-16BE name"));
            Assert.That(invalidUtf16State.Data.RawData.Bytes, Is.EqualTo(invalidUtf16));
            Assert.That(recordSizeState.Result.Message, Does.Contain("16, 24, or 32"));
            Assert.That(displayState.Result.Message, Does.Contain("invalid multiLocalizedUnicodeType display-name"));
            Assert.That(new[] { badPositionState, oddLengthState, duplicateState, invalidUtf16State, recordSizeState, displayState }.Select(item => item.Result.Status), Is.All.EqualTo(IccOperationStatus.InvalidData));
        });
    }

    [Test]
    public void DictionaryWriterRejectsDuplicateNamesAndInvalidUnicode()
    {
        IccMetadataEntry first = CreateEntry("Same", "One");
        IccMetadataEntry second = CreateEntry("Same", "Two");
        IccMetadataEntry invalid = CreateEntry("Broken", "\ud800");

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileWriteException>(() => DictionaryTagCodec.SerializeDictionary(new[] { first, second }));
            Assert.Throws<IccProfileWriteException>(() => DictionaryTagCodec.SerializeDictionary(new[] { invalid }));
        });
    }

    [TestCase("MediaColor", true)]
    [TestCase("MDCV", true)]
    [TestCase("VendorPrivateKey", false)]
    [TestCase("mediacolor", false)]
    public void MetadataRegistryNamesAreRecognizedExactly(string name, bool expected)
    {
        Assert.That(DictionaryTagCodec.IsRegisteredMetadataName(name), Is.EqualTo(expected));
    }

    private static TagDecodeState DecodePayload(byte[] payload, IccVersionKind version, bool preserveOriginalBytes)
    {
        TagDecodeSource source = new TagDecodeSource(payload);
        TagDecodeState state = new TagDecodeState(source, 0, payload.Length, 0, version, preserveOriginalBytes);
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static IccMetadataEntry CreateEntry(string name, string value)
    {
        IccMetadataEntry entry = new IccMetadataEntry();
        entry.Name = name;
        entry.Value = value;
        return entry;
    }

    private static IccLocalizedString CreateLocalized(string language, string country, string text)
    {
        IccLocalizedString localized = new IccLocalizedString();
        localized.Language = language;
        localized.Country = country;
        localized.Text = text;
        return localized;
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
