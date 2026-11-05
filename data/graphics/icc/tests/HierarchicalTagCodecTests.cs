using icc;

namespace icc.Tests;

public class HierarchicalTagCodecTests
{
    [Test]
    public void TagStructureRoundTripsTypedAndUnknownMembersInDeclaredOrder()
    {
        IccTagStructure structure = new IccTagStructure
        {
            StructureSignature = 0x64656d6f,
            Members = new IccStructureMember[]
            {
                CreateMember(0x6e616d65, CreateTextData("Colour \U0001f600")),
                CreateMember(0x76616c73, CreateFloatData(new double[] { 1.5, -2.0 })),
                CreateMember(0x66757472, CreateUnknownData())
            }
        };

        byte[] payload = HierarchicalTagCodec.SerializeTagStructure(structure);
        TagDecodeState state = DecodePayload(payload, true);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Result.Message, Does.Contain("retained 1 unknown"));
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.TagStructure));
            Assert.That(state.Data.TagStructure!.StructureSignature, Is.EqualTo(0x64656d6f));
            Assert.That(state.Data.TagStructure.Members.Select(member => member.Signature), Is.EqualTo(new uint[] { 0x6e616d65, 0x76616c73, 0x66757472 }));
            Assert.That(state.Data.TagStructure.Members[0].Data.Text!.Text, Is.EqualTo("Colour \U0001f600"));
            Assert.That(state.Data.TagStructure.Members[1].Data.FloatingPointValues, Is.EqualTo(new double[] { 1.5, -2.0 }));
            Assert.That(state.Data.TagStructure.Members[2].Data.Kind, Is.EqualTo(IccTagDataKind.Raw));
            Assert.That(state.Data.TagStructure.Members[2].Data.RawData.Bytes, Is.EqualTo(CreateUnknownData().RawData.Bytes));
        });
    }

    [Test]
    public void SharedMemberPositionsDecodeAndSerializeAsSharedObjectIdentity()
    {
        IccTagData shared = CreateTextData("shared");
        IccTagStructure structure = new IccTagStructure
        {
            StructureSignature = 0x73687264,
            Members = new IccStructureMember[]
            {
                CreateMember(0x6f6e6520, shared),
                CreateMember(0x74776f20, shared)
            }
        };

        byte[] first = HierarchicalTagCodec.SerializeTagStructure(structure);
        TagDecodeState state = DecodePayload(first, true);
        byte[] second = HierarchicalTagCodec.SerializeTagStructure(state.Data.TagStructure!);

        Assert.Multiple(() =>
        {
            Assert.That(ReadUInt32(first, 20), Is.EqualTo(ReadUInt32(first, 32)));
            Assert.That(ReadUInt32(first, 24), Is.EqualTo(ReadUInt32(first, 36)));
            Assert.That(state.Data.TagStructure!.Members[0].Data, Is.SameAs(state.Data.TagStructure.Members[1].Data));
            Assert.That(second, Is.EqualTo(first));
        });
    }

    [Test]
    public void CanonicalAndPreservationModesRoundTripNestedSharingAndTheirOwnOrdering()
    {
        IccTagData shared = CreateTextData("Cafe\u0301");
        IccTagStructure structure = new IccTagStructure
        {
            StructureSignature = 0x6d6f6465,
            Members = new IccStructureMember[]
            {
                CreateMember(0x7a7a7a7a, shared),
                CreateMember(0x61616161, shared)
            }
        };

        byte[] preserved = HierarchicalTagCodec.SerializeTagStructureForMode(
            structure, 4, IccWriteMode.Preservation);
        byte[] canonical = HierarchicalTagCodec.SerializeTagStructureForMode(
            structure, 4, IccWriteMode.Canonical);
        TagDecodeState preservedState = DecodePayload(preserved, true);
        TagDecodeState canonicalState = DecodePayload(canonical, true);
        IccTagStructure preservedStructure = preservedState.Data.TagStructure!;
        IccTagStructure canonicalStructure = canonicalState.Data.TagStructure!;

        Assert.Multiple(() =>
        {
            Assert.That(preservedState.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(canonicalState.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(preservedStructure.Members.Select(member => member.Signature),
                Is.EqualTo(new uint[] { 0x7a7a7a7a, 0x61616161 }));
            Assert.That(canonicalStructure.Members.Select(member => member.Signature),
                Is.EqualTo(new uint[] { 0x61616161, 0x7a7a7a7a }));
            Assert.That(preservedStructure.Members[0].Data,
                Is.SameAs(preservedStructure.Members[1].Data));
            Assert.That(canonicalStructure.Members[0].Data,
                Is.SameAs(canonicalStructure.Members[1].Data));
            Assert.That(preservedStructure.Members[0].Data.Text!.Text, Is.EqualTo("Cafe\u0301"));
            Assert.That(canonicalStructure.Members[0].Data.Text!.Text, Is.EqualTo("Caf\u00e9"));
            Assert.That(HierarchicalTagCodec.SerializeTagStructureForMode(
                preservedStructure, 4, IccWriteMode.Preservation), Is.EqualTo(preserved));
            Assert.That(HierarchicalTagCodec.SerializeTagStructureForMode(
                canonicalStructure, 4, IccWriteMode.Canonical), Is.EqualTo(canonical));
        });
    }

    [Test]
    public void TagArrayRoundTripsHomogeneousNestedStructuresAndPreservesSharing()
    {
        IccTagData first = CreateStructureData(0x6974656d, "first");
        IccTagData second = CreateStructureData(0x6974656d, "second");
        IccTagArray array = new IccTagArray
        {
            ArraySignature = IccArraySignatures.Generic,
            Values = new IccTagData[] { first, second, first }
        };

        byte[] payload = HierarchicalTagCodec.SerializeTagArray(array);
        TagDecodeState state = DecodePayload(payload, true);
        IccTagArray decoded = state.Data.TagArray!;

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(decoded.ArraySignature, Is.EqualTo(IccArraySignatures.Generic));
            Assert.That(decoded.Values, Has.Length.EqualTo(3));
            Assert.That(decoded.Values[0].TagStructure!.StructureSignature, Is.EqualTo(0x6974656d));
            Assert.That(decoded.Values[1].TagStructure!.Members[0].Data.Text!.Text, Is.EqualTo("second"));
            Assert.That(decoded.Values[0], Is.SameAs(decoded.Values[2]));
            Assert.That(ReadUInt32(payload, 16), Is.EqualTo(ReadUInt32(payload, 32)));
            Assert.That(HierarchicalTagCodec.SerializeTagArray(decoded), Is.EqualTo(payload));
        });
    }

    [Test]
    public void CanonicalLayoutAlignsUniqueElementsAndUsesZeroPadding()
    {
        IccTagStructure structure = new IccTagStructure
        {
            StructureSignature = 0x6c61796f,
            Members = new IccStructureMember[]
            {
                CreateMember(0x6f6e6520, CreateTextData("A")),
                CreateMember(0x74776f20, CreateTextData("BC"))
            }
        };

        byte[] payload = HierarchicalTagCodec.SerializeTagStructure(structure);

        Assert.Multiple(() =>
        {
            Assert.That(ReadUInt32(payload, 20), Is.EqualTo(40));
            Assert.That(ReadUInt32(payload, 24), Is.EqualTo(9));
            Assert.That(ReadUInt32(payload, 32), Is.EqualTo(52));
            Assert.That(ReadUInt32(payload, 36), Is.EqualTo(10));
            Assert.That(payload[49], Is.Zero);
            Assert.That(payload[50], Is.Zero);
            Assert.That(payload[51], Is.Zero);
            Assert.That(payload, Has.Length.EqualTo(62));
        });
    }

    [Test]
    public void StructureDecoderRejectsDuplicateMembersBadPositionsOverlapAndPadding()
    {
        IccTagStructure structure = new IccTagStructure
        {
            StructureSignature = 0x62616421,
            Members = new IccStructureMember[]
            {
                CreateMember(0x6f6e6520, CreateTextData("A")),
                CreateMember(0x74776f20, CreateTextData("BC"))
            }
        };
        byte[] valid = HierarchicalTagCodec.SerializeTagStructure(structure);
        byte[] duplicate = (byte[])valid.Clone();
        WriteUInt32(duplicate, 28, ReadUInt32(duplicate, 16));
        byte[] unaligned = (byte[])valid.Clone();
        WriteUInt32(unaligned, 20, 41);
        byte[] overlap = (byte[])valid.Clone();
        WriteUInt32(overlap, 32, 44);
        byte[] padding = (byte[])valid.Clone();
        padding[50] = 1;

        TagDecodeState duplicateState = DecodePayload(duplicate, true);
        TagDecodeState unalignedState = DecodePayload(unaligned, true);
        TagDecodeState overlapState = DecodePayload(overlap, true);
        TagDecodeState paddingState = DecodePayload(padding, true);

        Assert.Multiple(() =>
        {
            Assert.That(duplicateState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(duplicateState.Result.Message, Does.Contain("duplicated"));
            Assert.That(unalignedState.Result.Message, Does.Contain("unaligned"));
            Assert.That(overlapState.Result.Message, Does.Contain("overlap"));
            Assert.That(paddingState.Result.Message, Does.Contain("zero padding"));
        });
    }

    [Test]
    public void ArrayDecoderAndWriterRejectHeterogeneousElementTypesAndStructureIdentifiers()
    {
        IccTagArray mixedTypes = new IccTagArray
        {
            ArraySignature = 0,
            Values = new IccTagData[] { CreateTextData("text"), CreateFloatData(new double[] { 1.0 }) }
        };
        IccTagArray mixedStructures = new IccTagArray
        {
            ArraySignature = IccArraySignatures.Generic,
            Values = new IccTagData[] { CreateStructureData(0x6f6e6520, "one"), CreateStructureData(0x74776f20, "two") }
        };
        IccTagArray valid = new IccTagArray
        {
            ArraySignature = 0,
            Values = new IccTagData[] { CreateTextData("one"), CreateTextData("two") }
        };
        byte[] invalidDecodedTypes = HierarchicalTagCodec.SerializeTagArray(valid);
        int secondOffset = (int)ReadUInt32(invalidDecodedTypes, 24);
        WriteUInt32(invalidDecodedTypes, secondOffset, IccTypeSignatures.UInt8Array);
        TagDecodeState state = DecodePayload(invalidDecodedTypes, true);

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileWriteException>(() => HierarchicalTagCodec.SerializeTagArray(mixedTypes));
            Assert.Throws<IccProfileWriteException>(() => HierarchicalTagCodec.SerializeTagArray(mixedStructures));
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(state.Result.Message, Does.Contain("same tag type signature"));
        });
    }

    [Test]
    public void NestedInvalidPayloadMakesContainerInvalidWhileFutureTypeRemainsRaw()
    {
        IccTagStructure invalid = new IccTagStructure
        {
            Members = new IccStructureMember[] { CreateMember(0x74657874, CreateTextData("valid")) }
        };
        byte[] invalidPayload = HierarchicalTagCodec.SerializeTagStructure(invalid);
        int textOffset = (int)ReadUInt32(invalidPayload, 20);
        invalidPayload[textOffset + 8] = 0xc0;
        IccTagStructure unknown = new IccTagStructure
        {
            Members = new IccStructureMember[] { CreateMember(0x66757472, CreateUnknownData()) }
        };

        TagDecodeState invalidState = DecodePayload(invalidPayload, true);
        TagDecodeState unknownState = DecodePayload(HierarchicalTagCodec.SerializeTagStructure(unknown), false);

        Assert.Multiple(() =>
        {
            Assert.That(invalidState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(invalidState.Result.Message, Does.Contain("member 0").And.Contain("invalid UTF-8"));
            Assert.That(unknownState.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(unknownState.Data.TagStructure!.Members[0].Data.Kind, Is.EqualTo(IccTagDataKind.Raw));
            Assert.That(unknownState.Data.TagStructure.Members[0].Data.RawData.Bytes, Is.Not.Empty);
        });
    }

    [Test]
    public void NestingLimitIsAppliedDuringDirectProfileAndCanonicalOperations()
    {
        IccTagData inner = CreateStructureData(0x696e6e72, "inside");
        IccTagStructure outer = new IccTagStructure
        {
            StructureSignature = 0x6f757472,
            Members = new IccStructureMember[] { CreateMember(0x6368696c, inner) }
        };
        byte[] payload = HierarchicalTagCodec.SerializeTagStructureWithLimit(outer, 2);
        TagDecodeState limited = CreateDecodeState(payload, true);
        limited.MaximumNestingDepth = 1;
        TagCodecDispatcher.DecodeTagPayload(limited);
        IccReadOptions options = new IccReadOptions();
        options.ResourceLimits.MaximumNestingDepth = 1;
        IccProfile profile = new IccProfileReader().ReadProfileBytes(CreateProfileWithPayload(payload), options);
        IccTagData profileData = TagDecoder.GetDecodedData(profile.Tags[0]);

        Assert.Multiple(() =>
        {
            Assert.That(limited.Result.Status, Is.EqualTo(IccOperationStatus.ResourceLimitExceeded));
            Assert.That(limited.Result.Message, Does.Contain("nesting depth of 1"));
            Assert.That(profile.Tags[0].DecodeResult.Status, Is.EqualTo(IccOperationStatus.ResourceLimitExceeded));
            Assert.That(profileData.Kind, Is.EqualTo(IccTagDataKind.Raw));
            Assert.Throws<IccProfileWriteException>(() => HierarchicalTagCodec.SerializeTagStructureWithLimit(outer, 1));
        });
    }

    [Test]
    public void WriterRejectsDuplicateMemberSignaturesCyclesAndRawSignatureMismatch()
    {
        IccTagData shared = CreateTextData("value");
        IccTagStructure duplicate = new IccTagStructure
        {
            Members = new IccStructureMember[]
            {
                CreateMember(0x73616d65, shared),
                CreateMember(0x73616d65, shared)
            }
        };
        IccTagData cycleData = new IccTagData { Kind = IccTagDataKind.TagStructure };
        cycleData.TagStructure = new IccTagStructure { Members = new IccStructureMember[] { CreateMember(0x73656c66, cycleData) } };
        IccTagStructure cycle = new IccTagStructure { Members = new IccStructureMember[] { CreateMember(0x726f6f74, cycleData) } };
        IccTagData mismatched = CreateUnknownData();
        mismatched.RawData.TypeSignature = 0x62616421;
        IccTagStructure rawMismatch = new IccTagStructure { Members = new IccStructureMember[] { CreateMember(0x72617720, mismatched) } };

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileWriteException>(() => HierarchicalTagCodec.SerializeTagStructure(duplicate));
            Assert.Throws<IccProfileWriteException>(() => HierarchicalTagCodec.SerializeTagStructure(cycle));
            Assert.Throws<IccProfileWriteException>(() => HierarchicalTagCodec.SerializeTagStructure(rawMismatch));
        });
    }

    private static IccTagData CreateTextData(string text)
    {
        return new IccTagData
        {
            Kind = IccTagDataKind.Text,
            Text = new IccTextData { TypeSignature = IccTypeSignatures.Utf8, Text = text }
        };
    }

    private static IccTagData CreateFloatData(double[] values)
    {
        IccTagData data = new IccTagData
        {
            Kind = IccTagDataKind.FloatingPointArray,
            FloatingPointValues = values
        };
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = IccTypeSignatures.Float32Array;
        return data;
    }

    private static IccTagData CreateUnknownData()
    {
        byte[] bytes = new byte[] { 0x71, 0x71, 0x71, 0x71, 0, 0, 0, 0, 1, 2, 3, 4 };
        IccTagData data = new IccTagData { Kind = IccTagDataKind.Raw };
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = 0x71717171;
        data.RawData.Bytes = bytes;
        return data;
    }

    private static IccTagData CreateStructureData(uint structureSignature, string text)
    {
        IccTagData data = new IccTagData { Kind = IccTagDataKind.TagStructure };
        data.TagStructure = new IccTagStructure
        {
            StructureSignature = structureSignature,
            Members = new IccStructureMember[] { CreateMember(0x6e616d65, CreateTextData(text)) }
        };
        return data;
    }

    private static IccStructureMember CreateMember(uint signature, IccTagData data)
    {
        return new IccStructureMember { Signature = signature, Data = data };
    }

    private static TagDecodeState DecodePayload(byte[] payload, bool preserveOriginalBytes)
    {
        TagDecodeState state = CreateDecodeState(payload, preserveOriginalBytes);
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static TagDecodeState CreateDecodeState(byte[] payload, bool preserveOriginalBytes)
    {
        return new TagDecodeState(new TagDecodeSource(payload), 0, payload.Length, 700, IccVersionKind.Version5, preserveOriginalBytes);
    }

    private static byte[] CreateProfileWithPayload(byte[] payload)
    {
        int payloadEnd = 144 + payload.Length;
        int declaredSize = (payloadEnd + 3) & -4;
        byte[] profile = new byte[declaredSize];
        WriteUInt32(profile, 0, (uint)declaredSize);
        WriteUInt32(profile, 8, 0x05000000);
        WriteUInt32(profile, 12, IccProfileClassSignatures.Display);
        WriteUInt32(profile, 16, IccColorSpaceSignatures.Rgb);
        WriteUInt32(profile, 20, IccColorSpaceSignatures.Xyz);
        WriteUInt32(profile, 36, IccConstants.ProfileFileSignature);
        WriteUInt32(profile, 128, 1);
        WriteUInt32(profile, 132, IccTagSignatures.Metadata);
        WriteUInt32(profile, 136, 144);
        WriteUInt32(profile, 140, (uint)payload.Length);
        Array.Copy(payload, 0, profile, 144, payload.Length);
        return profile;
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
