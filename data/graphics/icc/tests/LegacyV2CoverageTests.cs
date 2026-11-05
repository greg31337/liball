using icc;

namespace icc.Tests;

public class LegacyV2CoverageTests
{
    [TestCaseSource(nameof(LegacyMappedPayloads))]
    public void EveryMappedLegacyTagDecodesItsRequiredV2Type(uint tagSignature,
        byte[] payload, IccTagDataKind expectedKind)
    {
        TagDecodeState state = Decode(payload, tagSignature,
            IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz,
            IccPcsKind.Xyz, true);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.Kind, Is.EqualTo(expectedKind));
            Assert.That(state.Data.RawData.TypeSignature,
                Is.EqualTo(LegacyStructuredTagCodec.GetMappedType(tagSignature)));
        });
    }

    [Test]
    public void V2NamedColor2UsesLegacyLabAndXyzEndpointEncodings()
    {
        byte[] labPayload = NamedColorTagCodec.SerializeNamedColor2(
            CreateNamedColor2Set(new[] { 100.0, -128.0, 127.0 }),
            IccVersionKind.Version2, IccPcsKind.Lab,
            IccColorSpaceSignatures.Rgb);
        byte[] xyzPayload = NamedColorTagCodec.SerializeNamedColor2(
            CreateNamedColor2Set(new[] { 0.0, 1.0, 65535.0 / 32768.0 }),
            IccVersionKind.Version2, IccPcsKind.Xyz,
            IccColorSpaceSignatures.Rgb);

        TagDecodeState lab = Decode(labPayload, IccTagSignatures.NamedColor2,
            IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Lab,
            IccPcsKind.Lab, true);
        TagDecodeState xyz = Decode(xyzPayload, IccTagSignatures.NamedColor2,
            IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz,
            IccPcsKind.Xyz, true);

        Assert.Multiple(() =>
        {
            Assert.That(labPayload.Skip(116).Take(6),
                Is.EqualTo(new byte[] { 0xff, 0x00, 0x00, 0x00, 0xff, 0x00 }));
            Assert.That(xyzPayload.Skip(116).Take(6),
                Is.EqualTo(new byte[] { 0x00, 0x00, 0x80, 0x00, 0xff, 0xff }));
            Assert.That(lab.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(lab.Data.NamedColors!.Colors[0].PcsColor.Components,
                Is.EqualTo(new[] { 100.0, -128.0, 127.0 }));
            Assert.That(xyz.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(xyz.Data.NamedColors!.Colors[0].PcsColor.Components,
                Is.EqualTo(new[] { 0.0, 1.0, 65535.0 / 32768.0 }));
        });
    }

    [Test]
    public void TolerantTextDescriptionPreservesOriginalUntilDecodedTextIsEdited()
    {
        byte[] quirky = CreateTextDescription("Legacy", "Localized", "Mac");
        IndependentBigEndian.WriteUInt32(quirky, 8, 6);
        TagDecodeState state = Decode(quirky,
            IccTagSignatures.ScreeningDescription,
            IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz,
            IccPcsKind.Xyz, true);

        byte[] unchanged = LegacyTagCodec.SerializeTextDescription(state.Data.Text!);
        state.Data.Text!.AsciiText = "Edited";
        byte[] edited = LegacyTagCodec.SerializeTextDescription(state.Data.Text);
        TagDecodeState roundTrip = Decode(edited,
            IccTagSignatures.ScreeningDescription,
            IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz,
            IccPcsKind.Xyz, true);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Result.Message, Does.Contain("tolerating inconsistent"));
            Assert.That(unchanged, Is.EqualTo(quirky));
            Assert.That(edited, Is.Not.EqualTo(quirky));
            Assert.That(roundTrip.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(roundTrip.Data.Text!.AsciiText, Is.EqualTo("Edited"));
            Assert.That(roundTrip.Data.Text.UnicodeText, Is.EqualTo("Localized"));
        });
    }

    [TestCaseSource(nameof(V2RequiredTagModels))]
    public void EveryV2ProfileModelReportsItsExactRequiredTagMatrix(
        IccProfileClass profileClass, uint dataColorSpace, uint connectionSpace,
        uint[] presentTags, uint[] expectedMissingTags)
    {
        IccProfile profile = CreateV2Profile(profileClass, dataColorSpace,
            connectionSpace, presentTags);

        string[] actual = Icc1ConformanceValidationCore.Validate(profile)
            .Where(issue => issue.RuleId == IccValidationRuleIds.RequiredTag)
            .Select(issue => issue.Path)
            .ToArray();
        string[] expected = expectedMissingTags
            .Select(signature => $"Tags['{FourCcCodec.Format(signature)}']")
            .ToArray();

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void UnknownAndPrivateV2PayloadsRoundTripByteForByte()
    {
        byte[] unknownPayload = IccBinaryFixture.CreateTagPayload(0x756e6b6e, 11);
        unknownPayload[8] = 0xde;
        unknownPayload[9] = 0xad;
        unknownPayload[10] = 0xbe;
        byte[] privatePayload = IccBinaryFixture.CreateTagPayload(0x70726976, 13);
        privatePayload[8] = 1;
        privatePayload[9] = 3;
        privatePayload[10] = 3;
        privatePayload[11] = 7;
        privatePayload[12] = 9;
        byte[] original = IccBinaryFixture.CreateProfile(0x02100000,
            IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb,
            IccColorSpaceSignatures.Xyz, 0,
            new IccBinaryTagFixture(0x7a7a7a31, unknownPayload),
            new IccBinaryTagFixture(0x70727631, privatePayload));

        IccReadOptions readOptions = new IccReadOptions();
        readOptions.Mode = IccReadMode.Recovery;
        readOptions.ValidationMode = IccValidationMode.Tolerant;
        readOptions.DecodeTagsLazily = false;
        IccProfile profile = ProfileCodec.ReadEnvelope(original, 0, 0, readOptions);
        IccWriteOptions writeOptions = new IccWriteOptions();
        writeOptions.Mode = IccWriteMode.Preservation;
        writeOptions.ComputeProfileId = false;
        byte[] output = new IccProfileWriter().WriteProfileBytes(profile, writeOptions);

        Assert.Multiple(() =>
        {
            Assert.That(profile.Header.Version.Kind, Is.EqualTo(IccVersionKind.Version2));
            Assert.That(profile.Tags.Select(tag => tag.Data.Kind),
                Is.EqualTo(new[] { IccTagDataKind.Raw, IccTagDataKind.Raw }));
            Assert.That(profile.Tags.Select(tag => tag.DecodeResult.Status),
                Is.EqualTo(new[]
                {
                    IccOperationStatus.UnsupportedFeature,
                    IccOperationStatus.UnsupportedFeature
                }));
            Assert.That(profile.Tags[0].Data.RawData.Bytes, Is.EqualTo(unknownPayload));
            Assert.That(profile.Tags[1].Data.RawData.Bytes, Is.EqualTo(privatePayload));
            Assert.That(output, Is.EqualTo(original));
        });
    }

    private static System.Collections.IEnumerable LegacyMappedPayloads()
    {
        IccCrdInfo crd = new IccCrdInfo();
        crd.ProductName = "Product";
        byte[] crdPayload = LegacyStructuredTagCodec.SerializeCrdInfo(crd);

        IccNamedColorSet oldNamed = new IccNamedColorSet();
        oldNamed.DeviceChannelCount = 3;
        IccNamedColor oldColor = new IccNamedColor();
        oldColor.Name = "Ink";
        oldColor.Tints = new[] { 0.0, 0.5, 1.0 };
        oldNamed.Colors = new[] { oldColor };
        byte[] oldNamedPayload = NamedColorTagCodec.SerializeNamedColor(oldNamed,
            IccColorSpaceSignatures.Rgb);

        IccTextData text = new IccTextData();
        text.Text = "Screen";
        text.AsciiText = "Screen";
        byte[] descriptionPayload = LegacyTagCodec.SerializeTextDescription(text);

        IccScreening screening = new IccScreening();
        IccScreeningChannel channel = new IccScreeningChannel();
        channel.Frequency = 100.0;
        channel.AngleDegrees = 45.0;
        channel.SpotShape = IccScreeningSpotShape.Round;
        screening.Channels = new[] { channel };
        byte[] screeningPayload = LegacyStructuredTagCodec.SerializeScreening(screening);

        IccUcrBg ucrBg = new IccUcrBg();
        ucrBg.UnderColorRemovalValues = new ushort[] { 25, 75 };
        ucrBg.BlackGenerationValues = new ushort[] { 100 };
        ucrBg.Description = "Method";
        byte[] ucrBgPayload = LegacyStructuredTagCodec.SerializeUcrBg(ucrBg);
        byte[] dataPayload = CreateAsciiData("PostScript");
        byte[] devicePayload = CreateDeviceSettingsPayload();

        yield return Case(IccTagSignatures.CrdInfo, crdPayload,
            IccTagDataKind.CrdInfo, "crdi");
        yield return Case(IccTagSignatures.NamedColor, oldNamedPayload,
            IccTagDataKind.NamedColors, "ncol");
        yield return Case(IccTagSignatures.PostScriptCrd0, dataPayload,
            IccTagDataKind.Text, "psd0");
        yield return Case(IccTagSignatures.PostScriptCrd1, dataPayload,
            IccTagDataKind.Text, "psd1");
        yield return Case(IccTagSignatures.PostScriptCrd2, dataPayload,
            IccTagDataKind.Text, "psd2");
        yield return Case(IccTagSignatures.PostScriptCrd3, dataPayload,
            IccTagDataKind.Text, "psd3");
        yield return Case(IccTagSignatures.PostScriptCsa, dataPayload,
            IccTagDataKind.Text, "ps2s");
        yield return Case(IccTagSignatures.PostScriptRenderingIntent,
            dataPayload, IccTagDataKind.Text, "ps2i");
        yield return Case(IccTagSignatures.ScreeningDescription,
            descriptionPayload, IccTagDataKind.Text, "scrd");
        yield return Case(IccTagSignatures.Screening, screeningPayload,
            IccTagDataKind.Screening, "scrn");
        yield return Case(IccTagSignatures.UcrBg, ucrBgPayload,
            IccTagDataKind.UcrBg, "bfd");
        yield return Case(IccTagSignatures.DeviceSettings, devicePayload,
            IccTagDataKind.Raw, "devs");
    }

    private static TestCaseData Case(uint tagSignature, byte[] payload,
        IccTagDataKind expectedKind, string name)
    {
        return new TestCaseData(tagSignature, payload, expectedKind).SetName(
            $"EveryMappedLegacyTagDecodesItsRequiredV2Type_{name}");
    }

    private static System.Collections.IEnumerable V2RequiredTagModels()
    {
        uint[] common = new uint[]
        {
            IccTagSignatures.ProfileDescription,
            IccTagSignatures.Copyright,
            IccTagSignatures.MediaWhitePoint
        };
        uint[] commonLink = new uint[]
        {
            IccTagSignatures.ProfileDescription,
            IccTagSignatures.Copyright
        };
        uint[] matrixAfterRed = new uint[]
        {
            IccTagSignatures.GreenMatrixColumn,
            IccTagSignatures.BlueMatrixColumn,
            IccTagSignatures.RedTrc,
            IccTagSignatures.GreenTrc,
            IccTagSignatures.BlueTrc
        };
        uint[] output = new uint[]
        {
            IccTagSignatures.AToB0,
            IccTagSignatures.AToB1,
            IccTagSignatures.AToB2,
            IccTagSignatures.BToA0,
            IccTagSignatures.BToA1,
            IccTagSignatures.BToA2,
            IccTagSignatures.Gamut
        };

        yield return Model(IccProfileClass.Input, IccColorSpaceSignatures.Rgb,
            IccColorSpaceSignatures.Xyz, new uint[0], Join(common,
                IccTagSignatures.AToB0), "InputLut");
        yield return Model(IccProfileClass.Display, IccColorSpaceSignatures.Rgb,
            IccColorSpaceSignatures.Xyz, new uint[0], Join(common,
                IccTagSignatures.AToB0, IccTagSignatures.BToA0), "DisplayLut");
        yield return Model(IccProfileClass.Input, IccColorSpaceSignatures.Gray,
            IccColorSpaceSignatures.Xyz, new uint[0], Join(common,
                IccTagSignatures.GrayTrc), "InputGray");
        yield return Model(IccProfileClass.Display, IccColorSpaceSignatures.Gray,
            IccColorSpaceSignatures.Xyz, new uint[0], Join(common,
                IccTagSignatures.GrayTrc), "DisplayGray");
        yield return Model(IccProfileClass.Output, IccColorSpaceSignatures.Gray,
            IccColorSpaceSignatures.Xyz, new uint[0], Join(common,
                IccTagSignatures.GrayTrc), "OutputGray");
        yield return Model(IccProfileClass.Output, IccColorSpaceSignatures.Cmyk,
            IccColorSpaceSignatures.Lab, new uint[0], Join(common, output),
            "OutputLut");
        yield return Model(IccProfileClass.Output, IccColorSpaceSignatures.FiveColor,
            IccColorSpaceSignatures.Lab, new uint[0], Join(Join(common, output),
                IccTagSignatures.ColorantTable), "OutputXclr");
        yield return Model(IccProfileClass.DeviceLink,
            IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Cmyk,
            new uint[0], Join(commonLink,
                IccTagSignatures.ProfileSequenceDescription,
                IccTagSignatures.AToB0), "DeviceLink");
        yield return Model(IccProfileClass.DeviceLink,
            IccColorSpaceSignatures.FiveColor,
            IccColorSpaceSignatures.FourColor, new uint[0], Join(commonLink,
                IccTagSignatures.ProfileSequenceDescription,
                IccTagSignatures.AToB0, IccTagSignatures.ColorantTable,
                IccTagSignatures.ColorantTableOut), "DeviceLinkXclr");
        yield return Model(IccProfileClass.ColorSpace,
            IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Lab,
            new uint[0], Join(common, IccTagSignatures.AToB0,
                IccTagSignatures.BToA0), "ColorSpaceLut");
        yield return Model(IccProfileClass.Abstract,
            IccColorSpaceSignatures.Lab, IccColorSpaceSignatures.Lab,
            new uint[0], Join(common, IccTagSignatures.AToB0), "Abstract");
        yield return Model(IccProfileClass.NamedColor,
            IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Lab,
            new uint[0], Join(common, IccTagSignatures.NamedColor2), "NamedColor");
        yield return Model(IccProfileClass.Input, IccColorSpaceSignatures.Rgb,
            IccColorSpaceSignatures.Xyz,
            new[] { IccTagSignatures.RedMatrixColumn },
            Join(common, matrixAfterRed), "InputMatrix");
        yield return Model(IccProfileClass.ColorSpace,
            IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz,
            new[] { IccTagSignatures.RedMatrixColumn },
            Join(common, matrixAfterRed), "ColorSpaceMatrix");
    }

    private static TestCaseData Model(IccProfileClass profileClass,
        uint dataColorSpace, uint connectionSpace, uint[] present,
        uint[] expected, string name)
    {
        return new TestCaseData(profileClass, dataColorSpace, connectionSpace,
            present, expected).SetName(
                $"EveryV2ProfileModelReportsItsExactRequiredTagMatrix_{name}");
    }

    private static uint[] Join(uint[] first, params uint[] second)
    {
        uint[] result = new uint[first.Length + second.Length];
        Array.Copy(first, result, first.Length);
        Array.Copy(second, 0, result, first.Length, second.Length);
        return result;
    }

    private static IccProfile CreateV2Profile(IccProfileClass profileClass,
        uint dataColorSpace, uint connectionSpace, uint[] signatures)
    {
        IccProfile profile = new IccProfile();
        profile.Header.Version = HeaderCodec.DecodeVersion(0x02100000);
        profile.Header.ProfileClass = profileClass;
        profile.Header.DataColorSpaceSignature = dataColorSpace;
        profile.Header.ProfileConnectionSpaceSignature = connectionSpace;
        profile.Tags = signatures.Select(signature =>
        {
            IccTag tag = new IccTag();
            tag.Signature = signature;
            tag.TableEntry.Signature = signature;
            tag.TableEntry.Size = 8;
            tag.Data.RawData.HasTypeSignature = true;
            tag.Data.RawData.TypeSignature = IccTypeSignatures.Xyz;
            return tag;
        }).ToArray();
        return profile;
    }

    private static IccNamedColorSet CreateNamedColor2Set(double[] components)
    {
        IccNamedColor color = new IccNamedColor();
        color.Name = "Edge";
        color.PcsColor.Components = components;
        color.DeviceColor.Components = new double[0];
        IccNamedColorSet set = new IccNamedColorSet();
        set.DeviceChannelCount = 0;
        set.Colors = new[] { color };
        return set;
    }

    private static TagDecodeState Decode(byte[] payload, uint tagSignature,
        uint dataColorSpace, uint connectionSpace, IccPcsKind pcsKind,
        bool preserve)
    {
        TagDecodeSource source = new TagDecodeSource(payload);
        TagDecodeState state = new TagDecodeState(source, 0, payload.Length,
            0, IccVersionKind.Version2, preserve);
        state.HasProfileContext = true;
        state.TagSignature = tagSignature;
        state.ProfileClass = IccProfileClass.Display;
        state.DataColorSpaceSignature = dataColorSpace;
        state.DataColorSpace = HeaderCodec.DecodeColorSpace(dataColorSpace);
        state.ProfileConnectionSpaceSignature = connectionSpace;
        state.ProfileConnectionSpace = pcsKind;
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static byte[] CreateAsciiData(string text)
    {
        byte[] payload = new byte[13 + text.Length];
        IndependentBigEndian.WriteUInt32(payload, 0, IccTypeSignatures.Data);
        int index = 0;
        while (index < text.Length)
        {
            payload[12 + index] = (byte)text[index];
            index = index + 1;
        }
        return payload;
    }

    private static byte[] CreateDeviceSettingsPayload()
    {
        byte[] payload = new byte[76];
        IndependentBigEndian.WriteUInt32(payload, 0,
            IccTypeSignatures.DeviceSettings);
        IndependentBigEndian.WriteUInt32(payload, 8, 1);
        IndependentBigEndian.WriteUInt32(payload, 12,
            IccConstants.MicrosoftPlatform);
        IndependentBigEndian.WriteUInt32(payload, 16, 64);
        IndependentBigEndian.WriteUInt32(payload, 20, 1);
        IndependentBigEndian.WriteUInt32(payload, 24, 52);
        IndependentBigEndian.WriteUInt32(payload, 28, 2);
        IndependentBigEndian.WriteUInt32(payload, 32, 0x72736c6e);
        IndependentBigEndian.WriteUInt32(payload, 36, 8);
        IndependentBigEndian.WriteUInt32(payload, 40, 2);
        IndependentBigEndian.WriteUInt32(payload, 44, 300);
        IndependentBigEndian.WriteUInt32(payload, 48, 600);
        IndependentBigEndian.WriteUInt32(payload, 52, 600);
        IndependentBigEndian.WriteUInt32(payload, 56, 1200);
        IndependentBigEndian.WriteUInt32(payload, 60, 0x6d747970);
        IndependentBigEndian.WriteUInt32(payload, 64, 4);
        IndependentBigEndian.WriteUInt32(payload, 68, 1);
        IndependentBigEndian.WriteUInt32(payload, 72, 3);
        return payload;
    }

    private static byte[] CreateTextDescription(string ascii, string unicode,
        string script)
    {
        int asciiCount = ascii.Length + 1;
        int unicodeCount = unicode.Length + 1;
        byte[] payload = new byte[90 + asciiCount + (unicodeCount * 2)];
        IndependentBigEndian.WriteUInt32(payload, 0,
            IccTypeSignatures.TextDescription);
        IndependentBigEndian.WriteUInt32(payload, 8, (uint)asciiCount);
        int index = 0;
        while (index < ascii.Length)
        {
            payload[12 + index] = (byte)ascii[index];
            index = index + 1;
        }
        int unicodeOffset = 12 + asciiCount;
        IndependentBigEndian.WriteUInt32(payload, unicodeOffset, 0x656e5553);
        IndependentBigEndian.WriteUInt32(payload, unicodeOffset + 4,
            (uint)unicodeCount);
        index = 0;
        while (index < unicode.Length)
        {
            IndependentBigEndian.WriteUInt16(payload,
                unicodeOffset + 8 + (index * 2), unicode[index]);
            index = index + 1;
        }
        int scriptOffset = payload.Length - 70;
        IndependentBigEndian.WriteUInt16(payload, scriptOffset, 0);
        payload[scriptOffset + 2] = (byte)(script.Length + 1);
        index = 0;
        while (index < script.Length)
        {
            payload[scriptOffset + 3 + index] = (byte)script[index];
            index = index + 1;
        }
        return payload;
    }
}
