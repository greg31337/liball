using icc;

namespace icc.Tests;

public class RegisteredArrayProjectionCodecTests
{
    [Test]
    public void NamedColorArrayProjectsNamedRecordsAfterTintZero()
    {
        IccTagArray array = new IccTagArray
        {
            ArraySignature = IccArraySignatures.NamedColor,
            Values = new IccTagData[]
            {
                Structure(IccStructureSignatures.TintZero),
                NamedColor("Orange"),
                NamedColor("Blue")
            }
        };

        TagDecodeState state = DecodeArray(array, 0, IccColorSpaceSignatures.Cmyk, 0);
        IccTagData clone = ProfileClone.CloneTagDataStandalone(state.Data);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.TagArray!.Values[0].TagStructure!.StructureSignature, Is.EqualTo(IccStructureSignatures.TintZero));
            Assert.That(state.Data.NamedColors, Is.Not.Null);
            Assert.That(state.Data.NamedColors!.DeviceChannelCount, Is.EqualTo(4));
            Assert.That(state.Data.NamedColors.TintZero, Is.Not.Null);
            Assert.That(state.Data.NamedColors.Colors.Select(color => color.Name), Is.EqualTo(new[] { "Orange", "Blue" }));
            Assert.That(clone.NamedColors!.TintZero, Is.Not.Null.And.Not.SameAs(state.Data.NamedColors.TintZero));
        });
    }

    [Test]
    public void NamedColorArrayRejectsMissingTintZeroAndDuplicateNames()
    {
        IccTagArray missingTintZero = new IccTagArray
        {
            ArraySignature = IccArraySignatures.NamedColor,
            Values = new IccTagData[] { NamedColor("Orange"), NamedColor("Blue") }
        };
        IccTagArray duplicate = new IccTagArray
        {
            ArraySignature = IccArraySignatures.NamedColor,
            Values = new IccTagData[] { Structure(IccStructureSignatures.TintZero), NamedColor("Orange"), NamedColor("Orange") }
        };
        IccTagArray caseDistinct = new IccTagArray
        {
            ArraySignature = IccArraySignatures.NamedColor,
            Values = new IccTagData[] { Structure(IccStructureSignatures.TintZero), NamedColor("Orange"), NamedColor("orange") }
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileWriteException>(() => HierarchicalTagCodec.SerializeTagArray(missingTintZero));
            Assert.Throws<IccProfileWriteException>(() => HierarchicalTagCodec.SerializeTagArray(duplicate));
            Assert.DoesNotThrow(() => HierarchicalTagCodec.SerializeTagArray(caseDistinct));
        });
    }

    [Test]
    public void ProfileInfoArrayProjectsSequenceInArrayOrder()
    {
        IccTagArray array = new IccTagArray
        {
            ArraySignature = IccArraySignatures.ProfileInformation,
            Values = new IccTagData[]
            {
                ProfileInfo(0x41424344),
                ProfileInfo(0x45464748)
            }
        };

        TagDecodeState state = DecodeArray(array, 0, 0, 0);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.ProfileSequence, Has.Length.EqualTo(2));
            Assert.That(state.Data.ProfileSequence.Select(entry => entry.DeviceManufacturerSignature),
                Is.EqualTo(new uint[] { 0x41424344, 0x45464748 }));
        });
    }

    [Test]
    public void GenericStructureArrayRejectsNonStructureElements()
    {
        IccTagArray array = new IccTagArray
        {
            ArraySignature = IccArraySignatures.Generic,
            Values = new IccTagData[] { Text("not a structure") }
        };

        IccProfileWriteException? exception = Assert.Throws<IccProfileWriteException>(() => HierarchicalTagCodec.SerializeTagArray(array));

        Assert.That(exception!.Message, Does.Contain("tagStructType"));
    }

    [Test]
    public void MultiplexTypeArrayProjectsUniqueNamesAndHeaderOrder()
    {
        IccTagArray array = new IccTagArray
        {
            ArraySignature = IccArraySignatures.MultiplexType,
            Values = new IccTagData[] { Text("Gloss"), Text("Texture") }
        };

        TagDecodeState state = DecodeArray(array, IccTagSignatures.MultiplexTypeArray, 0, IccColorSpaceSignatures.MultiplexPrefix | 2);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.MultiplexChannels, Is.Not.Null);
            Assert.That(state.Data.MultiplexChannels!.Names, Is.EqualTo(new[] { "Gloss", "Texture" }));
            Assert.That(state.Data.MultiplexChannels.Order, Is.EqualTo(new[] { 0, 1 }));
        });
    }

    [Test]
    public void ProfileReaderSuppliesMaterialConnectionSpaceToMultiplexProjection()
    {
        IccTagArray array = new IccTagArray
        {
            ArraySignature = IccArraySignatures.MultiplexType,
            Values = new IccTagData[] { Text("Gloss"), Text("Texture") }
        };
        byte[] payload = HierarchicalTagCodec.SerializeTagArray(array);
        IccProfile profile = new IccProfileReader().ReadProfileBytes(CreateProfile(payload,
            IccTagSignatures.MultiplexTypeArray, IccColorSpaceSignatures.MultiplexPrefix | 2), new IccReadOptions());
        IccTagData data = TagDecoder.GetDecodedData(profile.Tags[0]);

        Assert.Multiple(() =>
        {
            Assert.That(profile.Tags[0].DecodeResult.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(data.MultiplexChannels, Is.Not.Null);
            Assert.That(data.MultiplexChannels!.Names, Is.EqualTo(new[] { "Gloss", "Texture" }));
        });
    }

    [Test]
    public void MultiplexTypeArrayRejectsDuplicateNamesAndMcsCountMismatch()
    {
        IccTagArray duplicate = new IccTagArray
        {
            ArraySignature = IccArraySignatures.MultiplexType,
            Values = new IccTagData[] { Text("Gloss"), Text("Gloss") }
        };
        IccTagArray twoNames = new IccTagArray
        {
            ArraySignature = IccArraySignatures.MultiplexType,
            Values = new IccTagData[] { Text("Gloss"), Text("Texture") }
        };

        TagDecodeState mismatch = DecodeArray(twoNames, IccTagSignatures.MultiplexTypeArray, 0, IccColorSpaceSignatures.MultiplexPrefix | 3);

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileWriteException>(() => HierarchicalTagCodec.SerializeTagArray(duplicate));
            Assert.That(mismatch.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(mismatch.Result.Message, Does.Contain("declares 3 channels"));
        });
    }

    [Test]
    public void MultiplexIntegerDefaultsNormalizeAndProjectInHeaderOrder()
    {
        byte[] payload = IntegerPayload(IccTypeSignatures.UInt8Array, 0, 128, 255);
        TagDecodeState state = DecodePayload(payload, IccTagSignatures.MultiplexDefaultValues,
            IccColorSpaceSignatures.MultiplexPrefix | 3);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.MultiplexChannels, Is.Not.Null);
            Assert.That(state.Data.MultiplexChannels!.DefaultValues,
                Is.EqualTo(new[] { 0.0, 128.0 / 255.0, 1.0 }).Within(0.0000001));
            Assert.That(state.Data.MultiplexChannels.Order, Is.EqualTo(new[] { 0, 1, 2 }));
        });
    }

    [Test]
    public void MultiplexDefaultsRejectFloat64AndHeaderCountMismatch()
    {
        byte[] float64 = Icc2ArrayTagCodec.SerializeFloatingPointArray(IccTypeSignatures.Float64Array, new[] { 0.5 });
        byte[] twoDefaults = IntegerPayload(IccTypeSignatures.UInt16Array, 0, ushort.MaxValue);

        TagDecodeState wrongType = DecodePayload(float64, IccTagSignatures.MultiplexDefaultValues,
            IccColorSpaceSignatures.MultiplexPrefix | 1);
        TagDecodeState wrongCount = DecodePayload(twoDefaults, IccTagSignatures.MultiplexDefaultValues,
            IccColorSpaceSignatures.MultiplexPrefix | 3);

        Assert.Multiple(() =>
        {
            Assert.That(wrongType.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(wrongType.Result.Message, Does.Contain("float16ArrayType or float32ArrayType"));
            Assert.That(wrongCount.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(wrongCount.Result.Message, Does.Contain("declares 3 channels"));
        });
    }

    [TestCase(IccTagSignatures.ReferenceName, "ISO 22028-1")]
    [TestCase(IccTagSignatures.ColorSpaceName, "sRGB")]
    public void AbbreviatedColorEncodingNamesProjectFromUtf8Tags(uint tagSignature, string name)
    {
        IccTextData text = new IccTextData { TypeSignature = IccTypeSignatures.Utf8, Text = name };
        byte[] payload = Icc2TextTagCodec.SerializeIcc2Text(text);
        TagDecodeState state = DecodePayload(payload, tagSignature, 0);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.Text!.Text, Is.EqualTo(name));
            Assert.That(state.Data.ColorEncodingParameters, Is.Not.Null);
            Assert.That(state.Data.ColorEncodingParameters!.Name, Is.EqualTo(name));
        });
    }

    private static TagDecodeState DecodeArray(IccTagArray array, uint tagSignature, uint dataColorSpace, uint mcsSignature)
    {
        return DecodePayload(HierarchicalTagCodec.SerializeTagArray(array), tagSignature, mcsSignature, dataColorSpace);
    }

    private static TagDecodeState DecodePayload(byte[] payload, uint tagSignature, uint mcsSignature, uint dataColorSpace = 0)
    {
        TagDecodeSource source = new TagDecodeSource(payload);
        TagDecodeState state = new TagDecodeState(source, 0, payload.Length, 0, IccVersionKind.Version5, true);
        state.TagSignature = tagSignature;
        state.DataColorSpaceSignature = dataColorSpace;
        if (mcsSignature != 0)
        {
            state.HasProfileContext = true;
            state.MaterialConnectionSpace = IccColorSpaceKind.Multiplex;
            state.MaterialConnectionSpaceSignature = mcsSignature;
        }

        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static IccTagData Structure(uint signature, params IccStructureMember[] members)
    {
        return new IccTagData
        {
            Kind = IccTagDataKind.TagStructure,
            TagStructure = new IccTagStructure { StructureSignature = signature, Members = members }
        };
    }

    private static IccTagData NamedColor(string name)
    {
        return Structure(IccStructureSignatures.NamedColor,
            new IccStructureMember { Signature = StructuredTagProjectionCodec.Name, Data = Text(name) });
    }

    private static IccTagData ProfileInfo(uint manufacturer)
    {
        IccTagData signature = new IccTagData { Kind = IccTagDataKind.Signature, Signature = manufacturer };
        return Structure(IccStructureSignatures.ProfileInformation,
            new IccStructureMember { Signature = StructuredTagProjectionCodec.ManufacturerSignature, Data = signature });
    }

    private static IccTagData Text(string value)
    {
        return new IccTagData
        {
            Kind = IccTagDataKind.Text,
            Text = new IccTextData { TypeSignature = IccTypeSignatures.Utf8, Text = value }
        };
    }

    private static byte[] IntegerPayload(uint typeSignature, params ushort[] values)
    {
        int width = typeSignature == IccTypeSignatures.UInt8Array ? 1 : 2;
        byte[] payload = new byte[8 + (values.Length * width)];
        WriteUInt32(payload, 0, typeSignature);
        int index = 0;
        while (index < values.Length)
        {
            int offset = 8 + (index * width);
            if (width == 1)
            {
                payload[offset] = (byte)values[index];
            }
            else
            {
                payload[offset] = (byte)(values[index] >> 8);
                payload[offset + 1] = (byte)values[index];
            }

            index++;
        }

        return payload;
    }

    private static byte[] CreateProfile(byte[] payload, uint tagSignature, uint mcsSignature)
    {
        int payloadEnd = 144 + payload.Length;
        int declaredSize = (payloadEnd + 3) & -4;
        byte[] profile = new byte[declaredSize];
        WriteUInt32(profile, 0, (uint)declaredSize);
        WriteUInt32(profile, 8, 0x05000000);
        WriteUInt32(profile, 12, IccProfileClassSignatures.MultiplexLink);
        WriteUInt32(profile, 16, IccColorSpaceSignatures.Rgb);
        WriteUInt32(profile, 36, IccConstants.ProfileFileSignature);
        WriteUInt32(profile, 116, mcsSignature);
        WriteUInt32(profile, 128, 1);
        WriteUInt32(profile, 132, tagSignature);
        WriteUInt32(profile, 136, 144);
        WriteUInt32(profile, 140, (uint)payload.Length);
        Array.Copy(payload, 0, profile, 144, payload.Length);
        return profile;
    }

    private static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }
}
