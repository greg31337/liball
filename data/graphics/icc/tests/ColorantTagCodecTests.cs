using icc;

namespace icc.Tests;

public class ColorantTagCodecTests
{
    [Test]
    public void ColorantOrderTypeRoundTripsAChannelPermutation()
    {
        long[] order = new long[] { 2, 0, 1 };

        byte[] payload = ColorantOrderTagCodec.SerializeColorantOrder(order);
        TagDecodeState state = DecodePayload(payload, IccVersionKind.Version4, IccTagSignatures.ColorantOrder, IccProfileClass.Display, IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz, IccPcsKind.Xyz);
        TagDecodeState version5State = DecodePayload(payload, IccVersionKind.Version5, IccTagSignatures.ColorantOrder, IccProfileClass.Display, IccColorSpaceSignatures.ExtendedDevicePrefix | 3, IccColorSpaceSignatures.Xyz, IccPcsKind.Xyz);

        Assert.Multiple(() =>
        {
            Assert.That(payload, Is.EqualTo(new byte[] { 0x63, 0x6c, 0x72, 0x6f, 0, 0, 0, 0, 0, 0, 0, 3, 2, 0, 1 }));
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.IntegerArray));
            Assert.That(state.Data.IntegerValues, Is.EqualTo(order));
            Assert.That(version5State.Result.Status, Is.EqualTo(IccOperationStatus.Success));
        });
    }

    [Test]
    public void ColorantOrderTypeRejectsInvalidCountsSizesAndPermutations()
    {
        byte[] duplicate = ColorantOrderTagCodec.SerializeColorantOrder(new long[] { 0, 1, 2 });
        duplicate[14] = 1;
        byte[] outsideRange = duplicate.ToArray();
        outsideRange[14] = 3;
        byte[] zeroCount = duplicate.Take(12).ToArray();
        WriteUInt32(zeroCount, 8, 0);
        byte[] wrongSize = duplicate.Take(14).ToArray();

        TagDecodeState duplicateState = DecodePayloadWithoutContext(duplicate, IccVersionKind.Version4);
        TagDecodeState rangeState = DecodePayloadWithoutContext(outsideRange, IccVersionKind.Version4);
        TagDecodeState countState = DecodePayloadWithoutContext(zeroCount, IccVersionKind.Version4);
        TagDecodeState sizeState = DecodePayloadWithoutContext(wrongSize, IccVersionKind.Version4);

        Assert.Multiple(() =>
        {
            Assert.That(duplicateState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(duplicateState.Result.Message, Does.Contain("more than once"));
            Assert.That(rangeState.Result.Message, Does.Contain("outside the valid range"));
            Assert.That(countState.Result.Message, Does.Contain("between 1 and 15"));
            Assert.That(sizeState.Result.Message, Does.Contain("exactly 15 bytes"));
            Assert.Throws<IccProfileWriteException>(() => ColorantOrderTagCodec.SerializeColorantOrder(new long[0]));
            Assert.Throws<IccProfileWriteException>(() => ColorantOrderTagCodec.SerializeColorantOrder(Enumerable.Range(0, 16).Select(value => (long)value).ToArray()));
            Assert.Throws<IccProfileWriteException>(() => ColorantOrderTagCodec.SerializeColorantOrder(new long[] { 0, 0 }));
        });
    }

    [Test]
    public void ProfileDecodeChecksColorantOrderInputAndDeviceLinkOutputChannels()
    {
        byte[] twoChannels = ColorantOrderTagCodec.SerializeColorantOrder(new long[] { 1, 0 });
        byte[] fourChannels = ColorantOrderTagCodec.SerializeColorantOrder(new long[] { 3, 2, 1, 0 });
        byte[] inputMismatchProfile = CreateSingleTagProfile(IccTagSignatures.ColorantOrder, twoChannels, IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz);
        byte[] outputProfile = CreateSingleTagProfile(IccTagSignatures.ColorantOrderOut, fourChannels, IccProfileClassSignatures.DeviceLink, IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Cmyk);
        byte[] wrongClassProfile = CreateSingleTagProfile(IccTagSignatures.ColorantOrderOut, fourChannels, IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Cmyk);

        IccTag inputMismatch = ReadFirstTag(inputMismatchProfile);
        IccTag output = ReadFirstTag(outputProfile);
        IccTag wrongClass = ReadFirstTag(wrongClassProfile);

        Assert.Multiple(() =>
        {
            Assert.That(inputMismatch.DecodeResult.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(inputMismatch.DecodeResult.Message, Does.Contain("3 channels"));
            Assert.That(output.DecodeResult.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(output.Data.IntegerValues, Is.EqualTo(new long[] { 3, 2, 1, 0 }));
            Assert.That(wrongClass.DecodeResult.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(wrongClass.DecodeResult.Message, Does.Contain("DeviceLink"));
        });
    }

    [Test]
    public void ColorantTableTypeRoundTripsFixedNamesAndXyzCoordinates()
    {
        IccColorantSet set = CreateSet(IccColorSpaceSignatures.Gray, new[] { "Black" }, new[] { new[] { 0.5, 1.0, 1.5 } });

        byte[] payload = ColorantTableTagCodec.SerializeColorantTable(set, IccVersionKind.Version4, IccPcsKind.Xyz, false);
        TagDecodeState state = DecodePayload(payload, IccVersionKind.Version4, IccTagSignatures.ColorantTable, IccProfileClass.Display, IccColorSpaceSignatures.Gray, IccColorSpaceSignatures.Xyz, IccPcsKind.Xyz);

        Assert.Multiple(() =>
        {
            Assert.That(payload, Has.Length.EqualTo(50));
            Assert.That(payload.Skip(12).Take(6), Is.EqualTo(new byte[] { 66, 108, 97, 99, 107, 0 }));
            Assert.That(payload.Skip(18).Take(26), Is.All.Zero);
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.Colorants));
            Assert.That(state.Data.Colorants!.ColorSpaceSignature, Is.EqualTo(IccColorSpaceSignatures.Gray));
            Assert.That(state.Data.Colorants.Colorants[0].Name, Is.EqualTo("Black"));
            Assert.That(state.Data.Colorants.Colorants[0].PcsColor.ColorSpace, Is.EqualTo(IccColorSpaceKind.Xyz));
            Assert.That(state.Data.Colorants.Colorants[0].PcsColor.Components, Is.EqualTo(new[] { 0.5, 1.0, 1.5 }));
        });
    }

    [Test]
    public void ColorantTableTypeUsesGenerationSpecificLabEncoding()
    {
        IccColorantSet set = CreateSet(IccColorSpaceSignatures.Gray, new[] { "Neutral" }, new[] { new[] { 50.0, 0.0, 127.0 } });

        byte[] version2 = ColorantTableTagCodec.SerializeColorantTable(set, IccVersionKind.Version2, IccPcsKind.Lab, false);
        byte[] version4 = ColorantTableTagCodec.SerializeColorantTable(set, IccVersionKind.Version4, IccPcsKind.Lab, false);
        byte[] invalidVersion2 = version2.ToArray();
        invalidVersion2[44] = 0xff;
        invalidVersion2[45] = 0x01;
        TagDecodeState version2State = DecodePayload(version2, IccVersionKind.Version2, IccTagSignatures.ColorantTable, IccProfileClass.Display, IccColorSpaceSignatures.Gray, IccColorSpaceSignatures.Lab, IccPcsKind.Lab);
        TagDecodeState version4State = DecodePayload(version4, IccVersionKind.Version4, IccTagSignatures.ColorantTable, IccProfileClass.Display, IccColorSpaceSignatures.Gray, IccColorSpaceSignatures.Lab, IccPcsKind.Lab);
        TagDecodeState invalidVersion2State = DecodePayload(invalidVersion2, IccVersionKind.Version2, IccTagSignatures.ColorantTable, IccProfileClass.Display, IccColorSpaceSignatures.Gray, IccColorSpaceSignatures.Lab, IccPcsKind.Lab);

        Assert.Multiple(() =>
        {
            Assert.That(ReadUInt16(version2, 44), Is.EqualTo(0x7f80));
            Assert.That(ReadUInt16(version4, 44), Is.EqualTo(0x8000));
            Assert.That(ReadUInt16(version2, 46), Is.EqualTo(0x8000));
            Assert.That(ReadUInt16(version4, 46), Is.EqualTo(0x8080));
            Assert.That(version2State.Data.Colorants!.Colorants[0].PcsColor.Components[0], Is.EqualTo(50.0));
            Assert.That(version2State.Data.Colorants.Colorants[0].PcsColor.Components[2], Is.EqualTo(127.0));
            Assert.That(version4State.Data.Colorants!.Colorants[0].PcsColor.Components[0], Is.EqualTo(50.0).Within(0.001));
            Assert.That(version4State.Data.Colorants.Colorants[0].PcsColor.Components[2], Is.EqualTo(127.0).Within(0.001));
            Assert.That(invalidVersion2State.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(invalidVersion2State.Result.Message, Does.Contain("exceeds L*=100"));
        });
    }

    [Test]
    public void DeviceLinkOutputColorantTableUsesOutputSpaceAndPcsLab()
    {
        IccColorantSet outputSet = CreateSet(
            IccColorSpaceSignatures.Cmyk,
            new[] { "Cyan", "Magenta", "Yellow", "Black" },
            new[] { new[] { 55.0, -35.0, -45.0 }, new[] { 50.0, 75.0, -5.0 }, new[] { 90.0, -5.0, 90.0 }, new[] { 10.0, 0.0, 0.0 } });
        byte[] payload = ColorantTableTagCodec.SerializeColorantTable(outputSet, IccVersionKind.Version4, IccPcsKind.None, true);
        byte[] profileBytes = CreateSingleTagProfile(IccTagSignatures.ColorantTableOut, payload, IccProfileClassSignatures.DeviceLink, IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Cmyk);

        IccTag tag = ReadFirstTag(profileBytes);

        Assert.Multiple(() =>
        {
            Assert.That(tag.DecodeResult.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(tag.Data.Colorants!.ColorSpaceSignature, Is.EqualTo(IccColorSpaceSignatures.Cmyk));
            Assert.That(tag.Data.Colorants.Colorants, Has.Length.EqualTo(4));
            Assert.That(tag.Data.Colorants.Colorants[0].PcsColor.ColorSpace, Is.EqualTo(IccColorSpaceKind.Lab));
            Assert.That(tag.Data.Colorants.Colorants[0].PcsColor.Components[0], Is.EqualTo(55.0).Within(0.001));
        });
    }

    [Test]
    public void ColorantTableTypeReportsDuplicateNamesAndWriterRejectsThem()
    {
        IccColorantSet set = CreateSet(
            IccColorSpaceSignatures.Rgb,
            new[] { "Red", "Green", "Blue" },
            new[] { new[] { 0.5, 0.25, 0.125 }, new[] { 0.25, 0.5, 0.125 }, new[] { 0.125, 0.25, 0.5 } });
        byte[] payload = ColorantTableTagCodec.SerializeColorantTable(set, IccVersionKind.Version4, IccPcsKind.Xyz, false);
        Array.Copy(payload, 12, payload, 50, 32);

        TagDecodeState state = DecodePayload(payload, IccVersionKind.Version4, IccTagSignatures.ColorantTable, IccProfileClass.Display, IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz, IccPcsKind.Xyz);
        set.Colorants[1].Name = "Red";

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Result.Message, Does.Contain("1 duplicate colourant name"));
            Assert.That(state.Data.Colorants!.Colorants[0].Name, Is.EqualTo(state.Data.Colorants.Colorants[1].Name));
            Assert.Throws<IccProfileWriteException>(() => ColorantTableTagCodec.SerializeColorantTable(set, IccVersionKind.Version4, IccPcsKind.Xyz, false));
        });
    }

    [Test]
    public void ColorantTableTypeEnforcesNameAndChannelConstraints()
    {
        IccColorantSet set = CreateSet(IccColorSpaceSignatures.Gray, new[] { "Black" }, new[] { new[] { 0.5, 0.5, 0.5 } });
        byte[] valid = ColorantTableTagCodec.SerializeColorantTable(set, IccVersionKind.Version4, IccPcsKind.Xyz, false);
        byte[] notTerminated = valid.ToArray();
        Array.Fill(notTerminated, (byte)65, 12, 32);
        byte[] dirtyPadding = valid.ToArray();
        dirtyPadding[18] = 65;
        byte[] nonAscii = valid.ToArray();
        nonAscii[12] = 128;
        byte[] tooMany = valid.Take(12).ToArray();
        WriteUInt32(tooMany, 8, 16);

        TagDecodeState terminationState = DecodeTableWithoutProfileContext(notTerminated);
        TagDecodeState paddingState = DecodeTableWithoutProfileContext(dirtyPadding);
        TagDecodeState asciiState = DecodeTableWithoutProfileContext(nonAscii);
        TagDecodeState countState = DecodeTableWithoutProfileContext(tooMany);
        set.Colorants[0].Name = new string('A', 32);

        Assert.Multiple(() =>
        {
            Assert.That(terminationState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(terminationState.Result.Message, Does.Contain("null terminated"));
            Assert.That(paddingState.Result.Message, Does.Contain("zero-filled"));
            Assert.That(asciiState.Result.Message, Does.Contain("7-bit ASCII"));
            Assert.That(countState.Result.Message, Does.Contain("between 1 and 15"));
            Assert.Throws<IccProfileWriteException>(() => ColorantTableTagCodec.SerializeColorantTable(set, IccVersionKind.Version4, IccPcsKind.Xyz, false));
        });
    }

    private static IccColorantSet CreateSet(uint colorSpaceSignature, string[] names, double[][] coordinates)
    {
        IccColorantSet set = new IccColorantSet();
        set.ColorSpaceSignature = colorSpaceSignature;
        set.Colorants = new IccColorant[names.Length];
        int index = 0;
        while (index < names.Length)
        {
            IccColorant colorant = new IccColorant();
            colorant.Name = names[index];
            colorant.PcsColor.Components = coordinates[index];
            set.Colorants[index] = colorant;
            index++;
        }

        return set;
    }

    private static IccTag ReadFirstTag(byte[] profileBytes)
    {
        IccReadOptions options = new IccReadOptions();
        options.PreserveOriginalBytes = false;
        IccProfile profile = ProfileCodec.ReadEnvelope(profileBytes, 0, 0, options);
        TagDecoder.GetDecodedData(profile.Tags[0]);
        return profile.Tags[0];
    }

    private static byte[] CreateSingleTagProfile(uint tagSignature, byte[] payload, uint profileClass, uint dataColorSpace, uint connectionSpace)
    {
        int payloadOffset = 144;
        int profileSize = payloadOffset + ((payload.Length + 3) & ~3);
        byte[] profile = new byte[profileSize];
        WriteUInt32(profile, 0, (uint)profileSize);
        WriteUInt32(profile, 8, 0x04400000);
        WriteUInt32(profile, 12, profileClass);
        WriteUInt32(profile, 16, dataColorSpace);
        WriteUInt32(profile, 20, connectionSpace);
        WriteUInt32(profile, 36, IccConstants.ProfileFileSignature);
        WriteUInt32(profile, 128, 1);
        WriteUInt32(profile, 132, tagSignature);
        WriteUInt32(profile, 136, (uint)payloadOffset);
        WriteUInt32(profile, 140, (uint)payload.Length);
        Array.Copy(payload, 0, profile, payloadOffset, payload.Length);
        return profile;
    }

    private static TagDecodeState DecodePayload(byte[] payload, IccVersionKind version, uint tagSignature, IccProfileClass profileClass, uint dataColorSpace, uint connectionSpace, IccPcsKind pcsKind)
    {
        TagDecodeSource source = new TagDecodeSource(payload);
        TagDecodeState state = new TagDecodeState(source, 0, payload.Length, 0, version, false);
        state.HasProfileContext = true;
        state.TagSignature = tagSignature;
        state.ProfileClass = profileClass;
        state.DataColorSpaceSignature = dataColorSpace;
        state.ProfileConnectionSpaceSignature = connectionSpace;
        state.ProfileConnectionSpace = pcsKind;
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static TagDecodeState DecodePayloadWithoutContext(byte[] payload, IccVersionKind version)
    {
        TagDecodeSource source = new TagDecodeSource(payload);
        TagDecodeState state = new TagDecodeState(source, 0, payload.Length, 0, version, false);
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static TagDecodeState DecodeTableWithoutProfileContext(byte[] payload)
    {
        TagDecodeSource source = new TagDecodeSource(payload);
        TagDecodeState state = new TagDecodeState(source, 0, payload.Length, 0, IccVersionKind.Version4, false);
        state.ProfileConnectionSpace = IccPcsKind.Xyz;
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static ushort ReadUInt16(byte[] source, int offset)
    {
        return (ushort)(((uint)source[offset] << 8) | source[offset + 1]);
    }

    private static void WriteUInt32(byte[] destination, int offset, uint value)
    {
        destination[offset] = (byte)(value >> 24);
        destination[offset + 1] = (byte)(value >> 16);
        destination[offset + 2] = (byte)(value >> 8);
        destination[offset + 3] = (byte)value;
    }
}
