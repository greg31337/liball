using icc;

namespace icc.Tests;

public class EmbeddedProfileTagCodecTests
{
    [Test]
    public void Icc5TagDecodesClonesAndWritesCompleteEmbeddedProfile()
    {
        byte[] embeddedBytes = CreateProfile(0x05000000, IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb, IccConstants.EmbeddedProfileFlag);
        IccProfile embedded = ProfileCodec.ReadEnvelope(embeddedBytes, 0, 0, new IccReadOptions());
        byte[] payload = EmbeddedProfileTagCodec.SerializeEmbeddedProfileForContext(
            embedded, IccVersionKind.Version4, IccProfileClassSignatures.Display,
            IccColorSpaceSignatures.Rgb, 132);
        byte[] outerBytes = CreateOuterProfile(payload, IccTypeSignatures.EmbeddedProfile);
        IccReadOptions options = new IccReadOptions();
        options.DecodeTagsLazily = false;

        IccProfile outer = ProfileCodec.ReadEnvelope(outerBytes, 0, 1000, options);
        IccTagData data = outer.Tags[0].Data;
        IccTagData clone = ProfileClone.CloneTagDataStandalone(data);

        Assert.Multiple(() =>
        {
            Assert.That(outer.ReadResult.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(outer.Tags[0].DecodeResult.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(data.Kind, Is.EqualTo(IccTagDataKind.EmbeddedProfile));
            Assert.That(data.EmbeddedProfile, Is.Not.Null);
            Assert.That(data.EmbeddedProfile!.Header.Version.Kind, Is.EqualTo(IccVersionKind.Version5));
            Assert.That(data.EmbeddedProfile.Header.IsEmbedded, Is.True);
            Assert.That(data.EmbeddedProfile.Header.RequiresEmbeddedData, Is.False);
            Assert.That(data.EmbeddedProfile.Location.Offset, Is.EqualTo(1152));
            Assert.That(data.EmbeddedProfile.OriginalBytes, Is.EqualTo(embeddedBytes));
            Assert.That(payload.Take(8), Is.EqualTo(new byte[] { 0x49, 0x43, 0x43, 0x70, 0, 0, 0, 0 }));
            Assert.That(payload.Skip(8), Is.EqualTo(embeddedBytes));
            Assert.That(NestedTagDataSerializer.Serialize(data, 0, new HierarchicalWriteContext(4)), Is.EqualTo(payload));
            Assert.That(clone.EmbeddedProfile, Is.Not.SameAs(data.EmbeddedProfile));
            Assert.That(clone.EmbeddedProfile!.OriginalBytes, Is.Not.SameAs(data.EmbeddedProfile.OriginalBytes));
            Assert.That(clone.EmbeddedProfile.OriginalBytes, Is.EqualTo(embeddedBytes));
        });
    }

    [Test]
    public void ClassDeviceSpaceAndHeaderFlagsSelectRawIcc1Fallback()
    {
        TagDecodeState wrongClass = Decode(CreatePayload(CreateProfile(0x05000000, IccProfileClassSignatures.Output, IccColorSpaceSignatures.Rgb, 1)));
        TagDecodeState wrongDeviceSpace = Decode(CreatePayload(CreateProfile(0x05000000, IccProfileClassSignatures.Display, IccColorSpaceSignatures.Cmyk, 1)));
        TagDecodeState notEmbedded = Decode(CreatePayload(CreateProfile(0x05000000, IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb, 0)));
        TagDecodeState requiresData = Decode(CreatePayload(CreateProfile(0x05000000, IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb, 3)));

        Assert.Multiple(() =>
        {
            AssertFallback(wrongClass, "class");
            AssertFallback(wrongDeviceSpace, "device colour space");
            AssertFallback(notEmbedded, "embedded-profile flag");
            AssertFallback(requiresData, "use-independent-of-data");
        });
    }

    [Test]
    public void UnsupportedOrMalformedNestedProfilesSelectRawIcc1Fallback()
    {
        byte[] reservedPayload = CreatePayload(CreateProfile(0x05000000, IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb, 1));
        reservedPayload[7] = 1;
        TagDecodeState reserved = Decode(reservedPayload);
        TagDecodeState version4 = Decode(CreatePayload(CreateProfile(0x04400000, IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb, 1)));
        TagDecodeState malformedAlignment = Decode(CreatePayload(CreateProfileWithMissingAlignmentPadding()));
        byte[] textPayload = new byte[9];
        WriteUInt32(textPayload, 0, IccTypeSignatures.Text);
        textPayload[8] = 0;
        IccProfile wrongTypeOuter = ReadOuterProfile(textPayload, IccTypeSignatures.Text);

        Assert.Multiple(() =>
        {
            AssertInvalidFallback(reserved, "reserved bytes");
            Assert.That(version4.Result.Status, Is.EqualTo(IccOperationStatus.UnsupportedFeature));
            Assert.That(version4.Data.Kind, Is.EqualTo(IccTagDataKind.Raw));
            Assert.That(version4.Result.Message, Does.Contain("version 5").And.Contain("fallback"));
            AssertInvalidFallback(malformedAlignment, "alignment padding");
            Assert.That(wrongTypeOuter.Tags[0].DecodeResult.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(wrongTypeOuter.Tags[0].Data.Kind, Is.EqualTo(IccTagDataKind.Raw));
            Assert.That(wrongTypeOuter.Tags[0].DecodeResult.Message, Does.Contain("permits").And.Contain("ICCp"));
        });
    }

    [Test]
    public void NestedProfileLimitsSelectResourceLimitedIcc1Fallback()
    {
        byte[] payload = CreatePayload(CreateProfile(0x05000000, IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb, 1));
        TagDecodeState profileSize = Decode(payload);
        profileSize.MaximumProfileBytes = 131;
        TagCodecDispatcher.DecodeTagPayload(profileSize);
        TagDecodeState depth = Decode(payload);
        depth.MaximumNestingDepth = 1;
        depth.CurrentNestingDepth = 1;
        TagCodecDispatcher.DecodeTagPayload(depth);
        TagDecodeState tagCount = Decode(CreatePayload(CreateProfileWithOneTag()));
        tagCount.MaximumTagCount = 0;
        TagCodecDispatcher.DecodeTagPayload(tagCount);

        Assert.Multiple(() =>
        {
            AssertLimitFallback(profileSize, "configured maximum");
            AssertLimitFallback(depth, "nesting depth");
            AssertLimitFallback(tagCount, "tag count");
        });
    }

    [Test]
    public void WriterRejectsMissingBytesMismatchedContextAndInvalidEmbeddedFlags()
    {
        IccProfile valid = ProfileCodec.ReadEnvelope(
            CreateProfile(0x05000000, IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb, 1), 0, 0, new IccReadOptions());
        IccProfile missingBytes = ProfileClone.CloneProfile(valid);
        missingBytes.OriginalBytes = new byte[0];
        IccProfile missingFlag = ProfileCodec.ReadEnvelope(
            CreateProfile(0x05000000, IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb, 0), 0, 0, new IccReadOptions());
        IccProfile independent = ProfileCodec.ReadEnvelope(
            CreateProfile(0x05000000, IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb, 3), 0, 0, new IccReadOptions());
        IccProfile unsupportedClass = ProfileCodec.ReadEnvelope(
            CreateProfile(0x05000000, IccProfileClassSignatures.ColorEncodingSpace,
                IccColorSpaceSignatures.Rgb, 1), 0, 0, new IccReadOptions());

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileWriteException>(() => EmbeddedProfileTagCodec.SerializeEmbeddedProfileForContext(
                valid, IccVersionKind.Version4, IccProfileClassSignatures.Output,
                IccColorSpaceSignatures.Rgb, 132));
            Assert.Throws<IccProfileWriteException>(() => EmbeddedProfileTagCodec.SerializeEmbeddedProfileForContext(
                valid, IccVersionKind.Version4, IccProfileClassSignatures.Display,
                IccColorSpaceSignatures.Cmyk, 132));
            Assert.Throws<IccProfileWriteException>(() => EmbeddedProfileTagCodec.SerializeEmbeddedProfileForContext(
                valid, IccVersionKind.Version4, IccProfileClassSignatures.Display,
                IccColorSpaceSignatures.Rgb, 131));
            Assert.Throws<IccProfileWriteException>(() => EmbeddedProfileTagCodec.SerializeEmbeddedProfileForContext(
                valid, IccVersionKind.Version5, IccProfileClassSignatures.Display,
                IccColorSpaceSignatures.Rgb, 132));
            Assert.Throws<IccProfileWriteException>(() => EmbeddedProfileTagCodec.SerializeEmbeddedProfile(missingBytes));
            Assert.Throws<IccProfileWriteException>(() => EmbeddedProfileTagCodec.SerializeEmbeddedProfile(missingFlag));
            Assert.Throws<IccProfileWriteException>(() => EmbeddedProfileTagCodec.SerializeEmbeddedProfile(independent));
            Assert.Throws<IccProfileWriteException>(() => EmbeddedProfileTagCodec.SerializeEmbeddedProfile(unsupportedClass));
        });
    }

    private static void AssertFallback(TagDecodeState state, string messagePart)
    {
        Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.UnsupportedFeature));
        Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.Raw));
        Assert.That(state.Data.RawData.Bytes, Is.EqualTo(state.Source.ProfileBytes.ToArray()));
        Assert.That(state.Result.Message, Does.Contain(messagePart).And.Contain("fallback"));
    }

    private static void AssertInvalidFallback(TagDecodeState state, string messagePart)
    {
        Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
        Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.Raw));
        Assert.That(state.Result.Message, Does.Contain(messagePart).And.Contain("fallback"));
    }

    private static void AssertLimitFallback(TagDecodeState state, string messagePart)
    {
        Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.ResourceLimitExceeded));
        Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.Raw));
        Assert.That(state.Result.Message, Does.Contain(messagePart).And.Contain("fallback"));
    }

    private static TagDecodeState Decode(byte[] payload)
    {
        TagDecodeState state = new TagDecodeState(new TagDecodeSource(payload), 0, payload.Length, 500, IccVersionKind.Version4, true);
        state.HasProfileContext = true;
        state.TagSignature = IccTagSignatures.EmbeddedIccMaxProfile;
        state.ProfileClass = IccProfileClass.Display;
        state.ProfileClassSignature = IccProfileClassSignatures.Display;
        state.DataColorSpace = IccColorSpaceKind.Rgb;
        state.DataColorSpaceSignature = IccColorSpaceSignatures.Rgb;
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static IccProfile ReadOuterProfile(byte[] payload, uint typeSignature)
    {
        IccReadOptions options = new IccReadOptions();
        options.DecodeTagsLazily = false;
        return ProfileCodec.ReadEnvelope(CreateOuterProfile(payload, typeSignature), 0, 0, options);
    }

    private static byte[] CreatePayload(byte[] profile)
    {
        byte[] payload = new byte[8 + profile.Length];
        WriteUInt32(payload, 0, IccTypeSignatures.EmbeddedProfile);
        Array.Copy(profile, 0, payload, 8, profile.Length);
        return payload;
    }

    private static byte[] CreateProfile(uint version, uint profileClass, uint dataColorSpace, uint flags)
    {
        byte[] profile = new byte[132];
        WriteHeader(profile, version, profileClass, dataColorSpace, flags);
        return profile;
    }

    private static byte[] CreateProfileWithOneTag()
    {
        byte[] profile = new byte[152];
        WriteHeader(profile, 0x05000000, IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb, 1);
        WriteUInt32(profile, 128, 1);
        WriteUInt32(profile, 132, IccTagSignatures.Copyright);
        WriteUInt32(profile, 136, 144);
        WriteUInt32(profile, 140, 8);
        WriteUInt32(profile, 144, IccTypeSignatures.Text);
        return profile;
    }

    private static byte[] CreateProfileWithMissingAlignmentPadding()
    {
        byte[] profile = new byte[153];
        WriteHeader(profile, 0x05000000, IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb, 1);
        WriteUInt32(profile, 128, 1);
        WriteUInt32(profile, 132, IccTagSignatures.Copyright);
        WriteUInt32(profile, 136, 144);
        WriteUInt32(profile, 140, 9);
        WriteUInt32(profile, 144, IccTypeSignatures.Text);
        return profile;
    }

    private static void WriteHeader(byte[] profile, uint version, uint profileClass, uint dataColorSpace, uint flags)
    {
        WriteUInt32(profile, 0, (uint)profile.Length);
        WriteUInt32(profile, 8, version);
        WriteUInt32(profile, 12, profileClass);
        WriteUInt32(profile, 16, dataColorSpace);
        WriteUInt32(profile, 20, IccColorSpaceSignatures.Xyz);
        WriteUInt32(profile, 36, IccConstants.ProfileFileSignature);
        WriteUInt32(profile, 44, flags);
    }

    private static byte[] CreateOuterProfile(byte[] payload, uint typeSignature)
    {
        int declaredSize = (144 + payload.Length + 3) & -4;
        byte[] profile = new byte[declaredSize];
        WriteHeader(profile, 0x04400000, IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb, 0);
        WriteUInt32(profile, 128, 1);
        WriteUInt32(profile, 132, IccTagSignatures.EmbeddedIccMaxProfile);
        WriteUInt32(profile, 136, 144);
        WriteUInt32(profile, 140, (uint)payload.Length);
        WriteUInt32(payload, 0, typeSignature);
        Array.Copy(payload, 0, profile, 144, payload.Length);
        return profile;
    }

    private static void WriteUInt32(byte[] destination, int offset, uint value)
    {
        destination[offset] = (byte)(value >> 24);
        destination[offset + 1] = (byte)(value >> 16);
        destination[offset + 2] = (byte)(value >> 8);
        destination[offset + 3] = (byte)value;
    }
}
