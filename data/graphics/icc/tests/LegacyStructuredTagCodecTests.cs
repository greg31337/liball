using icc;

namespace icc.Tests;

public class LegacyStructuredTagCodecTests
{
    [Test]
    public void CrdInfoRoundTripsMisalignedCountsAbsentNamesAndClonesIndependently()
    {
        IccCrdInfo source = new IccCrdInfo();
        source.ProductName = "P";
        source.RenderingIntentCrdNames = new[] { "Perceptual", "", "Saturation", "Absolute" };

        byte[] payload = LegacyStructuredTagCodec.SerializeCrdInfo(source);
        TagDecodeState state = Decode(payload, IccTagSignatures.CrdInfo, IccVersionKind.Version2, true);
        IccTagData clone = ProfileClone.CloneTagDataStandalone(state.Data);
        byte[] roundTrip = LegacyStructuredTagCodec.SerializeCrdInfo(state.Data.CrdInfo!);

        Assert.Multiple(() =>
        {
            Assert.That(payload, Has.Length.EqualTo(61));
            Assert.That(ReadUInt32(payload, 8), Is.EqualTo(2));
            Assert.That(ReadUInt32(payload, 14), Is.EqualTo(11));
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.CrdInfo));
            Assert.That(state.Data.CrdInfo!.ProductName, Is.EqualTo("P"));
            Assert.That(state.Data.CrdInfo.RenderingIntentCrdNames, Is.EqualTo(source.RenderingIntentCrdNames));
            Assert.That(roundTrip, Is.EqualTo(payload));
            Assert.That(clone.CrdInfo!.RenderingIntentCrdNames, Is.EqualTo(source.RenderingIntentCrdNames));
            Assert.That(clone.CrdInfo.RenderingIntentCrdNames, Is.Not.SameAs(state.Data.CrdInfo.RenderingIntentCrdNames));
        });
    }

    [Test]
    public void CrdInfoRejectsMalformedCountsTextAndUnexpectedBytes()
    {
        IccCrdInfo info = new IccCrdInfo();
        info.ProductName = "Product";
        info.RenderingIntentCrdNames[0] = "Intent";
        byte[] valid = LegacyStructuredTagCodec.SerializeCrdInfo(info);
        byte[] reserved = valid.ToArray();
        reserved[4] = 1;
        byte[] absentProduct = valid.ToArray();
        WriteUInt32(absentProduct, 8, 0);
        byte[] unterminated = valid.ToArray();
        unterminated[19] = (byte)'X';
        byte[] nonAscii = valid.ToArray();
        nonAscii[12] = 0xff;
        byte[] oversizedIntent = valid.ToArray();
        WriteUInt32(oversizedIntent, 20, uint.MaxValue);
        byte[] trailing = valid.Append((byte)0).ToArray();
        info.ProductName = "Bad\0Name";

        Assert.Multiple(() =>
        {
            AssertInvalid(Decode(reserved), "reserved bytes");
            AssertInvalid(Decode(absentProduct), "terminating NULL");
            AssertInvalid(Decode(unterminated), "end with a NULL");
            AssertInvalid(Decode(nonAscii), "7-bit ASCII");
            AssertInvalid(Decode(oversizedIntent), "cannot fit");
            AssertInvalid(Decode(trailing), "unexpected bytes");
            Assert.Throws<IccProfileWriteException>(() => LegacyStructuredTagCodec.SerializeCrdInfo(info));
        });
    }

    [Test]
    public void ScreeningRoundTripsFlagsFixedPointChannelsAndSpotFunctions()
    {
        IccScreening source = new IccScreening();
        source.UsesPrinterDefaultScreens = true;
        source.FrequencyIsLinesPerInch = true;
        source.Channels = new[]
        {
            CreateScreeningChannel(150.5, 15.0, IccScreeningSpotShape.Round),
            CreateScreeningChannel(75.25, -45.0, IccScreeningSpotShape.Square)
        };

        byte[] payload = LegacyStructuredTagCodec.SerializeScreening(source);
        TagDecodeState state = Decode(payload, IccTagSignatures.Screening, IccVersionKind.Version2, true);
        IccTagData clone = ProfileClone.CloneTagDataStandalone(state.Data);
        byte[] roundTrip = LegacyStructuredTagCodec.SerializeScreening(state.Data.Screening!);

        Assert.Multiple(() =>
        {
            Assert.That(payload, Has.Length.EqualTo(40));
            Assert.That(ReadUInt32(payload, 8), Is.EqualTo(3));
            Assert.That(ReadUInt32(payload, 12), Is.EqualTo(2));
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.Screening));
            Assert.That(state.Data.Screening!.UsesPrinterDefaultScreens, Is.True);
            Assert.That(state.Data.Screening.FrequencyIsLinesPerInch, Is.True);
            Assert.That(state.Data.Screening.Channels[0].Frequency, Is.EqualTo(150.5));
            Assert.That(state.Data.Screening.Channels[1].AngleDegrees, Is.EqualTo(-45.0));
            Assert.That(state.Data.Screening.Channels.Select(value => value.SpotShape), Is.EqualTo(new[] { IccScreeningSpotShape.Round, IccScreeningSpotShape.Square }));
            Assert.That(roundTrip, Is.EqualTo(payload));
            Assert.That(clone.Screening!.Channels, Is.Not.SameAs(state.Data.Screening.Channels));
            Assert.That(clone.Screening.Channels[0], Is.Not.SameAs(state.Data.Screening.Channels[0]));
        });
    }

    [Test]
    public void ScreeningRejectsReservedFlagsBadCountsLengthsAndSpotFunctions()
    {
        IccScreening screening = new IccScreening();
        screening.Channels = new[] { CreateScreeningChannel(100.0, 45.0, IccScreeningSpotShape.Line) };
        byte[] valid = LegacyStructuredTagCodec.SerializeScreening(screening);
        byte[] flags = valid.ToArray();
        WriteUInt32(flags, 8, 4);
        byte[] count = valid.ToArray();
        WriteUInt32(count, 12, 16);
        byte[] truncated = valid.Take(valid.Length - 1).ToArray();
        byte[] spot = valid.ToArray();
        WriteUInt32(spot, 24, 8);
        IccScreening tooMany = new IccScreening();
        tooMany.Channels = Enumerable.Range(0, 16).Select(value => CreateScreeningChannel(value, value, IccScreeningSpotShape.Unknown)).ToArray();
        screening.Channels[0].SpotShape = (IccScreeningSpotShape)8;

        Assert.Multiple(() =>
        {
            AssertInvalid(Decode(flags), "reserved bits");
            AssertInvalid(Decode(count), "more than 15");
            AssertInvalid(Decode(truncated), "exactly 28 bytes");
            AssertInvalid(Decode(spot), "undefined spot-shape");
            Assert.Throws<IccProfileWriteException>(() => LegacyStructuredTagCodec.SerializeScreening(tooMany));
            Assert.Throws<IccProfileWriteException>(() => LegacyStructuredTagCodec.SerializeScreening(screening));
        });
    }

    [Test]
    public void UcrBgRoundTripsOddSizedCurveCountsDescriptionAndClone()
    {
        IccUcrBg source = new IccUcrBg();
        source.UnderColorRemovalValues = new ushort[] { 0, 32768, 65535 };
        source.BlackGenerationValues = new ushort[] { 45 };
        source.Description = "GCR";

        byte[] payload = LegacyStructuredTagCodec.SerializeUcrBg(source);
        TagDecodeState state = Decode(payload, IccTagSignatures.UcrBg, IccVersionKind.Version2, true);
        IccTagData clone = ProfileClone.CloneTagDataStandalone(state.Data);
        byte[] roundTrip = LegacyStructuredTagCodec.SerializeUcrBg(state.Data.UcrBg!);

        Assert.Multiple(() =>
        {
            Assert.That(payload, Has.Length.EqualTo(28));
            Assert.That(ReadUInt32(payload, 8), Is.EqualTo(3));
            Assert.That(ReadUInt32(payload, 18), Is.EqualTo(1));
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.UcrBg));
            Assert.That(state.Data.UcrBg!.UnderColorRemovalValues, Is.EqualTo(source.UnderColorRemovalValues));
            Assert.That(state.Data.UcrBg.BlackGenerationValues, Is.EqualTo(source.BlackGenerationValues));
            Assert.That(state.Data.UcrBg.Description, Is.EqualTo("GCR"));
            Assert.That(roundTrip, Is.EqualTo(payload));
            Assert.That(clone.UcrBg!.UnderColorRemovalValues, Is.Not.SameAs(state.Data.UcrBg.UnderColorRemovalValues));
        });
    }

    [Test]
    public void UcrBgRejectsCountsPercentagesAndMalformedDescriptions()
    {
        IccUcrBg source = new IccUcrBg();
        source.UnderColorRemovalValues = new ushort[] { 50 };
        source.BlackGenerationValues = new ushort[] { 75 };
        source.Description = "Method";
        byte[] valid = LegacyStructuredTagCodec.SerializeUcrBg(source);
        byte[] ucrCount = valid.ToArray();
        WriteUInt32(ucrCount, 8, uint.MaxValue);
        byte[] bgCount = valid.ToArray();
        WriteUInt32(bgCount, 14, uint.MaxValue);
        byte[] percent = valid.ToArray();
        WriteUInt16(percent, 12, 101);
        byte[] unterminated = valid.ToArray();
        unterminated[unterminated.Length - 1] = (byte)'X';
        byte[] nonAscii = valid.ToArray();
        nonAscii[20] = 0xff;
        source.BlackGenerationValues[0] = 101;

        Assert.Multiple(() =>
        {
            AssertInvalid(Decode(ucrCount), "UCR count");
            AssertInvalid(Decode(bgCount), "black-generation count");
            AssertInvalid(Decode(percent), "percentage");
            AssertInvalid(Decode(unterminated), "NULL byte");
            AssertInvalid(Decode(nonAscii), "7-bit ASCII");
            Assert.Throws<IccProfileWriteException>(() => LegacyStructuredTagCodec.SerializeUcrBg(source));
        });
    }

    [Test]
    public void DeviceSettingsValidatesNestedStructuresAndRetainsKnownRawBytes()
    {
        byte[] payload = CreateDeviceSettingsPayload();
        TagDecodeState state = Decode(payload, IccTagSignatures.DeviceSettings, IccVersionKind.Version2, false);
        byte[] roundTrip = LegacyStructuredTagCodec.SerializeDeviceSettingsRaw(state.Data.RawData);
        byte[] badPlatform = payload.ToArray();
        WriteUInt32(badPlatform, 12, 0x62616421);
        byte[] unreferencedPlatformData = payload.ToArray();
        WriteUInt32(unreferencedPlatformData, 20, 0);
        byte[] badSettingCount = payload.ToArray();
        WriteUInt32(badSettingCount, 28, 3);
        byte[] badValueCount = payload.ToArray();
        WriteUInt32(badValueCount, 40, uint.MaxValue);
        byte[] zeroValueSize = payload.ToArray();
        WriteUInt32(zeroValueSize, 36, 0);
        byte[] overflowingValueProduct = payload.ToArray();
        WriteUInt32(overflowingValueProduct, 36, uint.MaxValue);
        WriteUInt32(overflowingValueProduct, 40, uint.MaxValue);
        byte[] trailing = payload.Append((byte)0).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Result.Message, Does.Contain("structurally validated").And.Contain("raw data"));
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.Raw));
            Assert.That(state.Data.RawData.TypeSignature, Is.EqualTo(IccTypeSignatures.DeviceSettings));
            Assert.That(state.Data.RawData.Bytes, Is.EqualTo(payload));
            Assert.That(roundTrip, Is.EqualTo(payload).And.Not.SameAs(state.Data.RawData.Bytes));
            AssertInvalid(Decode(badPlatform), "unregistered signature");
            AssertInvalid(Decode(unreferencedPlatformData), "unreferenced bytes");
            AssertInvalid(Decode(badSettingCount), "setting count");
            AssertInvalid(Decode(badValueCount), "values exceed");
            AssertInvalid(Decode(zeroValueSize), "zero byte size");
            AssertInvalid(Decode(overflowingValueProduct), "values exceed");
            AssertInvalid(Decode(trailing), "trailing or unreferenced");
            Assert.Throws<IccProfileWriteException>(() => LegacyStructuredTagCodec.SerializeDeviceSettingsRaw(new IccRawData { Bytes = badValueCount }));
        });
    }

    [TestCase(IccTagSignatures.CrdInfo, IccTypeSignatures.CrdInfo)]
    [TestCase(IccTagSignatures.NamedColor, IccTypeSignatures.NamedColor)]
    [TestCase(IccTagSignatures.PostScriptCrd0, IccTypeSignatures.Data)]
    [TestCase(IccTagSignatures.PostScriptCrd1, IccTypeSignatures.Data)]
    [TestCase(IccTagSignatures.PostScriptCrd2, IccTypeSignatures.Data)]
    [TestCase(IccTagSignatures.PostScriptCrd3, IccTypeSignatures.Data)]
    [TestCase(IccTagSignatures.PostScriptCsa, IccTypeSignatures.Data)]
    [TestCase(IccTagSignatures.PostScriptRenderingIntent, IccTypeSignatures.Data)]
    [TestCase(IccTagSignatures.ScreeningDescription, IccTypeSignatures.TextDescription)]
    [TestCase(IccTagSignatures.Screening, IccTypeSignatures.Screening)]
    [TestCase(IccTagSignatures.UcrBg, IccTypeSignatures.UcrBg)]
    [TestCase(IccTagSignatures.DeviceSettings, IccTypeSignatures.DeviceSettings)]
    public void DefinesEveryLegacyV2TagTypeMapping(uint tagSignature, uint typeSignature)
    {
        Assert.That(LegacyStructuredTagCodec.GetMappedType(tagSignature), Is.EqualTo(typeSignature));
    }

    [Test]
    public void DispatcherEnforcesLegacyMappingsAndVersionAvailability()
    {
        IccCrdInfo info = new IccCrdInfo();
        info.ProductName = "Product";
        byte[] crdPayload = LegacyStructuredTagCodec.SerializeCrdInfo(info);
        byte[] dataPayload = CreateDataPayload("CRD");
        TagDecodeState validPostScript = Decode(dataPayload, IccTagSignatures.PostScriptCrd0, IccVersionKind.Version2, true);
        TagDecodeState wrongType = Decode(crdPayload, IccTagSignatures.Screening, IccVersionKind.Version2, true);
        TagDecodeState wrongExclusiveTag = Decode(crdPayload, IccTagSignatures.Copyright, IccVersionKind.Version2, true);
        TagDecodeState version4 = Decode(crdPayload, IccTagSignatures.CrdInfo, IccVersionKind.Version4, true);

        Assert.Multiple(() =>
        {
            Assert.That(validPostScript.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(validPostScript.Data.Text!.Text, Is.EqualTo("CRD"));
            AssertInvalid(wrongType, "requires tag type 'scrn'");
            AssertInvalid(wrongExclusiveTag, "permitted only for tag 'crdi'");
            Assert.That(version4.Result.Status, Is.EqualTo(IccOperationStatus.UnsupportedFeature));
            Assert.That(TagCodecDispatcher.IsTypeSupportedByGeneration(IccTypeSignatures.DeviceSettings, IccVersionKind.Version2), Is.True);
            Assert.That(TagCodecDispatcher.IsTypeSupportedByGeneration(IccTypeSignatures.DeviceSettings, IccVersionKind.Version4), Is.False);
        });
    }

    private static IccScreeningChannel CreateScreeningChannel(double frequency, double angle, IccScreeningSpotShape spotShape)
    {
        IccScreeningChannel channel = new IccScreeningChannel();
        channel.Frequency = frequency;
        channel.AngleDegrees = angle;
        channel.SpotShape = spotShape;
        return channel;
    }

    private static byte[] CreateDeviceSettingsPayload()
    {
        byte[] payload = new byte[76];
        WriteUInt32(payload, 0, IccTypeSignatures.DeviceSettings);
        WriteUInt32(payload, 8, 1);
        WriteUInt32(payload, 12, IccConstants.MicrosoftPlatform);
        WriteUInt32(payload, 16, 64);
        WriteUInt32(payload, 20, 1);
        WriteUInt32(payload, 24, 52);
        WriteUInt32(payload, 28, 2);
        WriteUInt32(payload, 32, 0x72736c6e);
        WriteUInt32(payload, 36, 8);
        WriteUInt32(payload, 40, 2);
        WriteUInt32(payload, 44, 300);
        WriteUInt32(payload, 48, 600);
        WriteUInt32(payload, 52, 600);
        WriteUInt32(payload, 56, 1200);
        WriteUInt32(payload, 60, 0x6d747970);
        WriteUInt32(payload, 64, 4);
        WriteUInt32(payload, 68, 1);
        WriteUInt32(payload, 72, 3);
        return payload;
    }

    private static byte[] CreateDataPayload(string text)
    {
        byte[] payload = new byte[13 + text.Length];
        WriteUInt32(payload, 0, IccTypeSignatures.Data);
        int index = 0;
        while (index < text.Length)
        {
            payload[12 + index] = (byte)text[index];
            index = index + 1;
        }

        return payload;
    }

    private static TagDecodeState Decode(byte[] payload)
    {
        return Decode(payload, 0, IccVersionKind.Version2, true);
    }

    private static TagDecodeState Decode(byte[] payload, uint tagSignature, IccVersionKind version, bool preserve)
    {
        TagDecodeSource source = new TagDecodeSource(payload);
        TagDecodeState state = new TagDecodeState(source, 0, payload.Length, 0, version, preserve);
        state.HasProfileContext = tagSignature != 0;
        state.TagSignature = tagSignature;
        state.DataColorSpaceSignature = IccColorSpaceSignatures.Rgb;
        state.DataColorSpace = IccColorSpaceKind.Rgb;
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static void AssertInvalid(TagDecodeState state, string message)
    {
        Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
        Assert.That(state.Result.Message, Does.Contain(message));
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
