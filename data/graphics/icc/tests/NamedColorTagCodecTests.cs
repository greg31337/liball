using icc;

namespace icc.Tests;

public class NamedColorTagCodecTests
{
    [Test]
    public void NamedColor2TypeRoundTripsLegacyLabAndOptionalDeviceCoordinates()
    {
        IccNamedColorSet set = CreateSet("Book ", " 100%", 4, 0x12340000);
        IccNamedColor color = CreateColor("Orange", new[] { 50.0, 10.0, -20.0 }, new[] { 0.0, 1.0 / 3.0, 2.0 / 3.0, 1.0 });
        set.Colors = new[] { color };

        byte[] payload = NamedColorTagCodec.SerializeNamedColor2(set, IccVersionKind.Version4, IccPcsKind.Lab, IccColorSpaceSignatures.Cmyk);
        TagDecodeState state = DecodePayload(payload, IccVersionKind.Version4, IccColorSpaceSignatures.Cmyk, IccColorSpaceSignatures.Lab, IccPcsKind.Lab);

        Assert.Multiple(() =>
        {
            Assert.That(payload, Has.Length.EqualTo(130));
            Assert.That(ReadUInt32(payload, 8), Is.EqualTo(0x12340000));
            Assert.That(ReadUInt16(payload, 116), Is.EqualTo(0x7f80));
            Assert.That(ReadUInt16(payload, 122), Is.Zero);
            Assert.That(ReadUInt16(payload, 124), Is.EqualTo(21845));
            Assert.That(ReadUInt16(payload, 126), Is.EqualTo(43690));
            Assert.That(ReadUInt16(payload, 128), Is.EqualTo(65535));
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.NamedColors));
            Assert.That(state.Data.NamedColors!.VendorFlags, Is.EqualTo(0x12340000));
            Assert.That(state.Data.NamedColors.Prefix, Is.EqualTo("Book "));
            Assert.That(state.Data.NamedColors.Suffix, Is.EqualTo(" 100%"));
            Assert.That(state.Data.NamedColors.DeviceChannelCount, Is.EqualTo(4));
            Assert.That(state.Data.NamedColors.Colors[0].Name, Is.EqualTo("Orange"));
            Assert.That(state.Data.NamedColors.Colors[0].Description.Name, Is.EqualTo("Book Orange 100%"));
            Assert.That(state.Data.NamedColors.Colors[0].Description.Lab!.A, Is.EqualTo(10.0));
            Assert.That(state.Data.NamedColors.Colors[0].Description.Xyz, Is.Not.Null);
            Assert.That(state.Data.NamedColors.Colors[0].Description.XyY, Is.Not.Null);
            Assert.That(state.Data.NamedColors.Colors[0].Description.Lch, Is.Not.Null);
            Assert.That(state.Data.NamedColors.Colors[0].Description.Lch!.C, Is.EqualTo(System.Math.Sqrt(500.0)).Within(0.0001));
            Assert.That(state.Data.NamedColors.Colors[0].DeviceColor.Components, Is.EqualTo(new[] { 0.0, 1.0 / 3.0, 2.0 / 3.0, 1.0 }));
        });
    }

    [Test]
    public void NamedColor2TypePreparesD50LabDataFromXyzForNearestMatching()
    {
        IccNamedColorSet set = CreateSet("", "", 0, 7);
        IccNamedColor color = CreateColor(
            "Paper",
            new[] { IccStandardColorimetry.D50X, IccStandardColorimetry.D50Y, IccStandardColorimetry.D50Z },
            new double[0]);
        set.Colors = new[] { color };

        byte[] payload = NamedColorTagCodec.SerializeNamedColor2(set, IccVersionKind.Version2, IccPcsKind.Xyz, IccColorSpaceSignatures.Cmyk);
        TagDecodeState state = DecodePayload(payload, IccVersionKind.Version2, IccColorSpaceSignatures.Cmyk, IccColorSpaceSignatures.Xyz, IccPcsKind.Xyz);
        IccNamedColor decoded = state.Data.NamedColors!.Colors[0];

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(decoded.DeviceColor.Components, Is.Empty);
            Assert.That(decoded.Description.Xyz, Is.Not.Null);
            Assert.That(decoded.Description.Lab, Is.Not.Null);
            Assert.That(decoded.Description.XyY, Is.Not.Null);
            Assert.That(decoded.Description.Lch, Is.Not.Null);
            Assert.That(decoded.Description.Lab!.L, Is.EqualTo(100.0).Within(0.01));
            Assert.That(decoded.Description.Lab.A, Is.EqualTo(0.0).Within(0.02));
            Assert.That(decoded.Description.Lab.B, Is.EqualTo(0.0).Within(0.02));
        });
    }

    [Test]
    public void NamedColor2TypeKeepsExactCaseSensitiveNamesAndReportsDuplicates()
    {
        IccNamedColorSet set = CreateSet("P", "S", 0, 0);
        set.Colors = new[]
        {
            CreateColor("Spot", new[] { 25.0, 0.0, 0.0 }, new double[0]),
            CreateColor("spot", new[] { 50.0, 0.0, 0.0 }, new double[0]),
            CreateColor("Spot", new[] { 75.0, 0.0, 0.0 }, new double[0])
        };

        byte[] payload = NamedColorTagCodec.SerializeNamedColor2(set, IccVersionKind.Version4, IccPcsKind.Lab, IccColorSpaceSignatures.Rgb);
        TagDecodeState state = DecodePayload(payload, IccVersionKind.Version4, IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Lab, IccPcsKind.Lab);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Result.Message, Does.Contain("1 duplicate exact names"));
            Assert.That(state.Data.NamedColors!.Colors.Select(value => value.Name), Is.EqualTo(new[] { "Spot", "spot", "Spot" }));
            Assert.That(state.Data.NamedColors.Colors.Select(value => value.Description.Name), Is.EqualTo(new[] { "PSpotS", "PspotS", "PSpotS" }));
        });
    }

    [Test]
    public void NamedColor2TypeEnforcesFixedNamesSizesPcsAndChannelConsistency()
    {
        IccNamedColorSet set = CreateSet("", "", 3, 0);
        set.Colors = new[] { CreateColor("Red", new[] { 50.0, 0.0, 0.0 }, new[] { 1.0, 0.0, 0.0 }) };
        byte[] valid = NamedColorTagCodec.SerializeNamedColor2(set, IccVersionKind.Version4, IccPcsKind.Lab, IccColorSpaceSignatures.Rgb);
        byte[] badName = valid.ToArray();
        Array.Fill(badName, (byte)65, 20, 32);
        byte[] badSize = valid.Append((byte)0).ToArray();
        byte[] badLab = valid.ToArray();
        badLab[116] = 0xff;
        badLab[117] = 0x01;
        byte[] badChannels = valid.ToArray();
        WriteUInt32(badChannels, 16, 4);

        TagDecodeState nameState = DecodePayload(badName, IccVersionKind.Version4, IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Lab, IccPcsKind.Lab);
        TagDecodeState sizeState = DecodePayload(badSize, IccVersionKind.Version4, IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Lab, IccPcsKind.Lab);
        TagDecodeState labState = DecodePayload(badLab, IccVersionKind.Version4, IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Lab, IccPcsKind.Lab);
        TagDecodeState channelState = DecodePayload(badChannels, IccVersionKind.Version4, IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Lab, IccPcsKind.Lab);
        set.Prefix = new string('A', 32);

        Assert.Multiple(() =>
        {
            Assert.That(nameState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(nameState.Result.Message, Does.Contain("null-terminated"));
            Assert.That(sizeState.Result.Message, Does.Contain("exactly"));
            Assert.That(labState.Result.Message, Does.Contain("exceeds L*=100"));
            Assert.That(channelState.Result.Message, Does.Contain("does not match"));
            Assert.Throws<IccProfileWriteException>(() => NamedColorTagCodec.SerializeNamedColor2(set, IccVersionKind.Version4, IccPcsKind.Lab, IccColorSpaceSignatures.Rgb));
            Assert.Throws<IccProfileWriteException>(() => NamedColorTagCodec.SerializeNamedColor2(CreateSet("", "", 4, 0), IccVersionKind.Version4, IccPcsKind.Lab, IccColorSpaceSignatures.Rgb));
        });
    }

    [Test]
    public void LegacyNamedColorTypeRoundTripsVariableNamesAndNormalizedTints()
    {
        IccNamedColorSet set = CreateSet("Pre-", "-Post", 3, 0xabcd0000);
        IccNamedColor color = CreateColor("Green", new double[0], new double[0]);
        color.Tints = new[] { 0.0, 128.0 / 255.0, 1.0 };
        set.Colors = new[] { color };

        byte[] payload = NamedColorTagCodec.SerializeNamedColor(set, IccColorSpaceSignatures.Rgb);
        TagDecodeState state = DecodePayload(payload, IccVersionKind.Version2, IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz, IccPcsKind.Xyz);
        IccNamedColor decoded = state.Data.NamedColors!.Colors[0];

        Assert.Multiple(() =>
        {
            Assert.That(payload, Has.Length.EqualTo(36));
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.NamedColors.VendorFlags, Is.EqualTo(0xabcd0000));
            Assert.That(state.Data.NamedColors.DeviceChannelCount, Is.EqualTo(3));
            Assert.That(decoded.Name, Is.EqualTo("Green"));
            Assert.That(decoded.Description.Name, Is.EqualTo("Pre-Green-Post"));
            Assert.That(decoded.Tints, Is.EqualTo(new[] { 0.0, 128.0 / 255.0, 1.0 }));
            Assert.That(decoded.DeviceColor.Components, Is.EqualTo(decoded.Tints).And.Not.SameAs(decoded.Tints));
            Assert.That(decoded.PcsColor.Components, Is.Empty);
        });
    }

    [Test]
    public void LegacyNamedColorTypeRequiresContextTerminatorsExactSizeAndValidTints()
    {
        IccNamedColorSet set = CreateSet("", "", 1, 0);
        IccNamedColor color = CreateColor("Gray", new double[0], new double[0]);
        color.Tints = new[] { 0.5 };
        set.Colors = new[] { color };
        byte[] valid = NamedColorTagCodec.SerializeNamedColor(set, IccColorSpaceSignatures.Gray);
        byte[] trailing = valid.Append((byte)0).ToArray();
        byte[] unterminated = new byte[49];
        WriteUInt32(unterminated, 0, IccTypeSignatures.NamedColor);
        Array.Fill(unterminated, (byte)65, 16, 32);
        unterminated[48] = 0;

        TagDecodeState noContext = DecodePayloadWithoutContext(valid, IccVersionKind.Version2);
        TagDecodeState trailingState = DecodePayload(trailing, IccVersionKind.Version2, IccColorSpaceSignatures.Gray, IccColorSpaceSignatures.Xyz, IccPcsKind.Xyz);
        TagDecodeState nameState = DecodePayload(unterminated, IccVersionKind.Version2, IccColorSpaceSignatures.Gray, IccColorSpaceSignatures.Xyz, IccPcsKind.Xyz);
        color.Tints[0] = 1.1;

        Assert.Multiple(() =>
        {
            Assert.That(noContext.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(noContext.Result.Message, Does.Contain("profile data colour-space"));
            Assert.That(trailingState.Result.Message, Does.Contain("unexpected bytes"));
            Assert.That(nameState.Result.Message, Does.Contain("prefix"));
            Assert.Throws<IccProfileWriteException>(() => NamedColorTagCodec.SerializeNamedColor(set, IccColorSpaceSignatures.Gray));
            Assert.Throws<IccProfileWriteException>(() => NamedColorTagCodec.SerializeNamedColor(CreateSet("", "", 2, 0), IccColorSpaceSignatures.Gray));
        });
    }

    [Test]
    public void NamedColorDataClonePreservesFlagsAndDoesNotAliasPreparedCoordinates()
    {
        IccNamedColorSet set = CreateSet("", "", 0, 0xf0000000);
        set.Colors = new[] { CreateColor("White", new[] { 100.0, 0.0, 0.0 }, new double[0]) };
        byte[] payload = NamedColorTagCodec.SerializeNamedColor2(set, IccVersionKind.Version4, IccPcsKind.Lab, IccColorSpaceSignatures.Cmyk);
        TagDecodeState state = DecodePayload(payload, IccVersionKind.Version4, IccColorSpaceSignatures.Cmyk, IccColorSpaceSignatures.Lab, IccPcsKind.Lab);

        IccTagData clone = ProfileClone.CloneTagDataStandalone(state.Data);
        clone.NamedColors!.Colors[0].PcsColor.Components[0] = 25.0;

        Assert.Multiple(() =>
        {
            Assert.That(clone.NamedColors.VendorFlags, Is.EqualTo(0xf0000000));
            Assert.That(clone.NamedColors.Colors[0].PcsColor.Components, Is.Not.SameAs(state.Data.NamedColors!.Colors[0].PcsColor.Components));
            Assert.That(state.Data.NamedColors.Colors[0].PcsColor.Components[0], Is.EqualTo(100.0));
        });
    }

    private static IccNamedColorSet CreateSet(string prefix, string suffix, int deviceChannelCount, uint vendorFlags)
    {
        IccNamedColorSet set = new IccNamedColorSet();
        set.Prefix = prefix;
        set.Suffix = suffix;
        set.DeviceChannelCount = deviceChannelCount;
        set.VendorFlags = vendorFlags;
        return set;
    }

    private static IccNamedColor CreateColor(string name, double[] pcs, double[] device)
    {
        IccNamedColor color = new IccNamedColor();
        color.Name = name;
        color.PcsColor.Components = pcs;
        color.DeviceColor.Components = device;
        return color;
    }

    private static TagDecodeState DecodePayload(byte[] payload, IccVersionKind version, uint dataColorSpace, uint connectionSpace, IccPcsKind pcsKind)
    {
        TagDecodeSource source = new TagDecodeSource(payload);
        TagDecodeState state = new TagDecodeState(source, 0, payload.Length, 0, version, false);
        state.HasProfileContext = true;
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

    private static ushort ReadUInt16(byte[] source, int offset)
    {
        return (ushort)(((uint)source[offset] << 8) | source[offset + 1]);
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
