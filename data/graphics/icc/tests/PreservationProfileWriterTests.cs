using icc;

namespace icc.Tests;

public class PreservationProfileWriterTests
{
    [Test]
    public void PreservationRetainsPrivateBytesOriginalOrderSharingAndPadding()
    {
        byte[] original = CreateSharedPrivateProfile();
        IccProfile profile = ReadProfile(original, true);
        profile.Tags = new IccTag[] { profile.Tags[2], profile.Tags[0], profile.Tags[1] };
        IccWriteOptions options = new IccWriteOptions();
        options.Mode = IccWriteMode.Preservation;
        options.ComputeProfileId = false;

        byte[] output = new IccProfileWriter().WriteProfileBytes(profile, options);
        IccProfile roundTrip = ReadProfile(output, true);

        Assert.Multiple(() =>
        {
            Assert.That(output, Is.EqualTo(original));
            Assert.That(ReadUInt32(output, 132), Is.EqualTo(0x7a303031));
            Assert.That(ReadUInt32(output, 144), Is.EqualTo(0x61303032));
            Assert.That(ReadUInt32(output, 156), Is.EqualTo(0x6d303033));
            Assert.That(ReadUInt32(output, 136), Is.EqualTo(ReadUInt32(output, 160)));
            Assert.That(output[178], Is.EqualTo(0xa5));
            Assert.That(output[179], Is.EqualTo(0x5a));
            Assert.That(roundTrip.TagTableEntries[0].SharedDataId, Is.Zero);
            Assert.That(roundTrip.TagTableEntries[2].SharedDataId, Is.Zero);
        });
    }

    [Test]
    public void PreservationKeepsLegalOriginalEncodingUntilDecodedDataChanges()
    {
        byte[] original = CreateNonCanonicalLocalizedProfile();
        IccProfile profile = ReadProfile(original, false);
        IccWriteOptions preservation = new IccWriteOptions();
        preservation.Mode = IccWriteMode.Preservation;
        preservation.ComputeProfileId = false;

        byte[] unchanged = new IccProfileWriter().WriteProfileBytes(profile, preservation);
        byte[] canonical = new IccProfileWriter().WriteProfileBytes(profile, new IccWriteOptions());
        profile.Tags[0].Data.LocalizedStrings[0].Text = "C";
        byte[] edited = new IccProfileWriter().WriteProfileBytes(profile, preservation);
        IccProfile editedProfile = ReadProfile(edited, false);

        Assert.Multiple(() =>
        {
            Assert.That(profile.Tags[0].DecodeResult.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(unchanged, Is.EqualTo(original));
            Assert.That(canonical, Is.Not.EqualTo(original));
            Assert.That(original[185], Is.EqualTo((byte)'B'));
            Assert.That(original[187], Is.EqualTo((byte)'A'));
            Assert.That(canonical[185], Is.EqualTo((byte)'A'));
            Assert.That(canonical[187], Is.EqualTo((byte)'B'));
            Assert.That(edited, Is.Not.EqualTo(original));
            Assert.That(editedProfile.Tags[0].Data.LocalizedStrings[0].Text, Is.EqualTo("C"));
            Assert.That(editedProfile.Tags[0].Data.LocalizedStrings[1].Text, Is.EqualTo("B"));
        });
    }

    [Test]
    public void PreservationSplitsAnEditedMemberOfAnOriginalSharedPayload()
    {
        byte[] original = CreateSharedPrivateProfile();
        IccProfile profile = ReadProfile(original, true);
        byte[] changedPayload = CreatePrivatePayload(0x11, 0x22);
        profile.Tags[2].Data.Kind = IccTagDataKind.Raw;
        profile.Tags[2].Data.RawData.Bytes = changedPayload;
        profile.Tags[2].Data.RawData.HasTypeSignature = true;
        profile.Tags[2].Data.RawData.TypeSignature = ReadUInt32(changedPayload, 0);
        IccWriteOptions options = new IccWriteOptions();
        options.Mode = IccWriteMode.Preservation;
        options.ComputeProfileId = false;

        byte[] output = new IccProfileWriter().WriteProfileBytes(profile, options);
        IccProfile roundTrip = ReadProfile(output, true);

        Assert.Multiple(() =>
        {
            Assert.That(output, Has.Length.EqualTo(204));
            Assert.That(ReadUInt32(output, 136), Is.EqualTo(168));
            Assert.That(ReadUInt32(output, 148), Is.EqualTo(180));
            Assert.That(ReadUInt32(output, 160), Is.EqualTo(192));
            Assert.That(output.Skip(168).Take(10), Is.EqualTo(original.Skip(168).Take(10)));
            Assert.That(output.Skip(192).Take(10), Is.EqualTo(changedPayload));
            Assert.That(roundTrip.TagTableEntries[0].SharedDataId, Is.EqualTo(-1));
            Assert.That(roundTrip.TagTableEntries[2].SharedDataId, Is.EqualTo(-1));
        });
    }

    private static IccProfile ReadProfile(byte[] bytes, bool lazy)
    {
        IccReadOptions options = new IccReadOptions();
        options.Mode = IccReadMode.Recovery;
        options.DecodeTagsLazily = lazy;
        return ProfileCodec.ReadEnvelope(bytes, 0, 0, options);
    }

    private static byte[] CreateSharedPrivateProfile()
    {
        byte[] profile = new byte[192];
        WriteHeader(profile);
        WriteUInt32(profile, 128, 3);
        WriteTableEntry(profile, 132, 0x7a303031, 168, 10);
        WriteTableEntry(profile, 144, 0x61303032, 180, 12);
        WriteTableEntry(profile, 156, 0x6d303033, 168, 10);
        byte[] privatePayload = CreatePrivatePayload(0xaa, 0xbb);
        System.Array.Copy(privatePayload, 0, profile, 168, privatePayload.Length);
        profile[178] = 0xa5;
        profile[179] = 0x5a;
        byte[] signaturePayload = CreateSignaturePayload(9);
        System.Array.Copy(signaturePayload, 0, profile, 180, signaturePayload.Length);
        return profile;
    }

    private static byte[] CreateNonCanonicalLocalizedProfile()
    {
        byte[] profile = new byte[188];
        WriteHeader(profile);
        WriteUInt32(profile, 128, 1);
        WriteTableEntry(profile, 132, IccTagSignatures.Copyright, 144, 44);
        int payloadOffset = 144;
        WriteUInt32(profile, payloadOffset, IccTypeSignatures.MultiLocalizedUnicode);
        WriteUInt32(profile, payloadOffset + 8, 2);
        WriteUInt32(profile, payloadOffset + 12, 12);
        WriteLocaleRecord(profile, payloadOffset + 16, "en", "US", 2, 42);
        WriteLocaleRecord(profile, payloadOffset + 28, "fr", "CA", 2, 40);
        profile[payloadOffset + 40] = 0;
        profile[payloadOffset + 41] = (byte)'B';
        profile[payloadOffset + 42] = 0;
        profile[payloadOffset + 43] = (byte)'A';
        return profile;
    }

    private static byte[] CreatePrivatePayload(byte first, byte second)
    {
        byte[] payload = new byte[10];
        WriteUInt32(payload, 0, 0x70726976);
        payload[8] = first;
        payload[9] = second;
        return payload;
    }

    private static byte[] CreateSignaturePayload(uint value)
    {
        byte[] payload = new byte[12];
        WriteUInt32(payload, 0, IccTypeSignatures.Signature);
        WriteUInt32(payload, 8, value);
        return payload;
    }

    private static void WriteHeader(byte[] profile)
    {
        WriteUInt32(profile, 0, (uint)profile.Length);
        WriteUInt32(profile, 8, 0x04400000);
        WriteUInt32(profile, 12, IccProfileClassSignatures.Display);
        WriteUInt32(profile, 16, IccColorSpaceSignatures.Rgb);
        WriteUInt32(profile, 20, IccColorSpaceSignatures.Xyz);
        WriteUInt32(profile, 36, IccConstants.ProfileFileSignature);
    }

    private static void WriteTableEntry(byte[] profile, int offset, uint signature, uint payloadOffset, uint payloadSize)
    {
        WriteUInt32(profile, offset, signature);
        WriteUInt32(profile, offset + 4, payloadOffset);
        WriteUInt32(profile, offset + 8, payloadSize);
    }

    private static void WriteLocaleRecord(byte[] profile, int offset, string language, string country, uint byteLength, uint payloadOffset)
    {
        WriteUInt16(profile, offset, (ushort)((language[0] << 8) | language[1]));
        WriteUInt16(profile, offset + 2, (ushort)((country[0] << 8) | country[1]));
        WriteUInt32(profile, offset + 4, byteLength);
        WriteUInt32(profile, offset + 8, payloadOffset);
    }

    private static uint ReadUInt32(byte[] source, int offset)
    {
        return ((uint)source[offset] << 24) | ((uint)source[offset + 1] << 16) | ((uint)source[offset + 2] << 8) | source[offset + 3];
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
