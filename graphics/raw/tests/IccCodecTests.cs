using raw;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace raw.Tests;

public class IccCodecTests
{
    [Test]
    public void ProfileParsingPreservesBytesAndReportsHeaderAndIdentity()
    {
        byte[] bytes = CreateProfile();
        RawIccProfile profile = RawIccProfile.Parse(bytes, new RawLimits());
        byte[] changedCopy = profile.CopyBytes();
        changedCopy[0] ^= 0xFF;

        Assert.Multiple(() =>
        {
            Assert.That((profile.Header ?? throw new System.InvalidOperationException("A required value is unavailable.")).ProfileSize, Is.EqualTo(bytes.Length));
            Assert.That(profile.Header.VersionMajor, Is.EqualTo(4));
            Assert.That(profile.Header.VersionMinor, Is.EqualTo(4));
            Assert.That(profile.Header.VersionBugFix, Is.Zero);
            Assert.That(profile.Header.DeviceClass, Is.EqualTo("mntr"));
            Assert.That(profile.Header.DataColorSpace, Is.EqualTo("RGB "));
            Assert.That(profile.Header.ProfileConnectionSpace, Is.EqualTo("XYZ "));
            Assert.That(profile.Header.CreatedValid, Is.True);
            Assert.That(profile.Header.CreatedYear, Is.EqualTo(2026));
            Assert.That(profile.Header.RenderingIntent, Is.EqualTo(1));
            Assert.That(profile.Description, Is.EqualTo("Synthetic RGB"));
            Assert.That(profile.HasEmbeddedProfileId, Is.True);
            Assert.That(profile.ProfileIdMatches, Is.True);
            Assert.That(profile.EmbeddedProfileId, Is.EqualTo(profile.CalculatedProfileId));
            Assert.That(profile.EmbeddedProfileIdHex, Has.Length.EqualTo(32));
            Assert.That(profile.Identity, Is.EqualTo("Synthetic RGB [" + profile.CalculatedProfileIdHex + "]"));
            Assert.That(profile.TagDataContiguous, Is.True);
            Assert.That(profile.Tags, Has.Length.EqualTo(8));
            Assert.That((profile.GetTag("desc") ?? throw new System.InvalidOperationException("A required value is unavailable.")).TypeSignature, Is.EqualTo("mluc"));
            Assert.That((profile.GetTag("desc") ?? throw new System.InvalidOperationException("A required value is unavailable.")).CopyBytes(), Is.EqualTo((profile.GetTag("desc") ?? throw new System.InvalidOperationException("A required value is unavailable.")).RawBytes));
            Assert.That(profile.RawBytes, Is.EqualTo(bytes));
            Assert.That(profile.RawBytes, Is.Not.EqualTo(changedCopy));
            Assert.That(profile.ToTagValue().CopyBytes(), Is.EqualTo(bytes));
            Assert.That(profile.Validate().IsValid(), Is.True);
        });
    }

    [Test]
    public void MatrixWhitePointAndAllTrcFormsAreExtracted()
    {
        RawIccProfile profile = RawIccProfile.Parse(CreateProfile(), new RawLimits());
        double[] expectedMatrix =
        {
            0.43607, 0.38506, 0.14308,
            0.22250, 0.71688, 0.06062,
            0.01393, 0.09710, 0.71417
        };

        Assert.Multiple(() =>
        {
            Assert.That(profile.HasMatrixTrc(), Is.True);
            Assert.That(profile.Matrix, Is.EqualTo(expectedMatrix).Within(0.00002));
            Assert.That(profile.WhitePoint, Is.EqualTo(new double[] { 0.9642, 1.0, 0.8249 }).Within(0.00002));
            Assert.That((profile.RedTrc ?? throw new System.InvalidOperationException("A required value is unavailable.")).Kind, Is.EqualTo(RawIccCurveKind.Gamma));
            Assert.That(profile.RedTrc.Gamma, Is.EqualTo(2.2).Within(0.004));
            Assert.That((profile.GreenTrc ?? throw new System.InvalidOperationException("A required value is unavailable.")).Kind, Is.EqualTo(RawIccCurveKind.Sampled));
            Assert.That(profile.GreenTrc.Samples, Is.EqualTo(new ushort[] { 0, 32768, 65535 }));
            Assert.That((profile.BlueTrc ?? throw new System.InvalidOperationException("A required value is unavailable.")).Kind, Is.EqualTo(RawIccCurveKind.Parametric));
            Assert.That(profile.BlueTrc.FunctionType, Is.EqualTo(4));
            Assert.That(profile.BlueTrc.Parameters, Is.EqualTo(new double[] { 2.4, 0.94786, 0.05214, 0.0774, 0.04045, 0.0, 0.0 }).Within(0.00002));
        });
    }

    [Test]
    public void MalformedHeaderTableCurveAndLimitsAreRejected()
    {
        byte[] badSignature = CreateProfile();
        badSignature[36] = (byte)'x';
        byte[] badRange = CreateProfile();
        WriteUInt32(badRange, 136, (uint)(badRange.Length + 4));
        byte[] duplicateTag = CreateProfile();
        duplicateTag.AsSpan(132, 4).CopyTo(duplicateTag.AsSpan(144, 4));
        byte[] badCurve = CreateProfile();
        int redCurveOffset = FindTagOffset(badCurve, "rTRC");
        WriteUInt32(badCurve, redCurveOffset + 8, 2);
        RawLimits limits = new RawLimits();
        limits.MaxTagValueBytes = CreateProfile().Length - 1;

        Assert.Multiple(() =>
        {
            Assert.Throws<RawFormatException>(() => RawIccProfile.Parse(badSignature, new RawLimits()));
            Assert.Throws<RawFormatException>(() => RawIccProfile.Parse(badRange, new RawLimits()));
            Assert.Throws<RawFormatException>(() => RawIccProfile.Parse(duplicateTag, new RawLimits()));
            Assert.Throws<RawFormatException>(() => RawIccProfile.Parse(badCurve, new RawLimits()));
            Assert.Throws<RawLimitException>(() => RawIccProfile.Parse(CreateProfile(), limits));
            Assert.Throws<RawFormatException>(() => RawIccProfile.Parse(new byte[131], new RawLimits()));
        });
    }

    [Test]
    public void MissingOrMismatchedProfileIdUsesCalculatedIdentity()
    {
        byte[] missing = CreateProfile();
        Array.Clear(missing, 84, 16);
        byte[] mismatched = CreateProfile();
        mismatched[84] ^= 0x80;

        RawIccProfile missingProfile = RawIccProfile.Parse(missing, new RawLimits());
        RawIccProfile mismatchedProfile = RawIccProfile.Parse(mismatched, new RawLimits());

        Assert.Multiple(() =>
        {
            Assert.That(missingProfile.HasEmbeddedProfileId, Is.False);
            Assert.That(missingProfile.ProfileIdMatches, Is.False);
            Assert.That(missingProfile.EmbeddedProfileIdHex, Is.Empty);
            Assert.That(missingProfile.CalculatedProfileIdHex, Has.Length.EqualTo(32));
            Assert.That(mismatchedProfile.HasEmbeddedProfileId, Is.True);
            Assert.That(mismatchedProfile.ProfileIdMatches, Is.False);
            Assert.That(mismatchedProfile.CalculatedProfileIdHex, Is.EqualTo(missingProfile.CalculatedProfileIdHex));
            Assert.That(mismatchedProfile.Validate().IsValid(), Is.True);
            Assert.That(mismatchedProfile.Validate().HasWarnings(), Is.True);
        });
    }

    [TestCase(RawByteOrder.LittleEndian)]
    [TestCase(RawByteOrder.BigEndian)]
    public void MetadataOpenDiscoversTiffIccAndMapsExactPayload(RawByteOrder byteOrder)
    {
        byte[] profileBytes = CreateProfile();
        SyntheticTiffBuilder builder = new SyntheticTiffBuilder(byteOrder, false);
        SyntheticTiffIfd root = builder.AddIfd();
        builder.AddField(root, 34675, TiffFieldType.Undefined, (ulong)profileBytes.Length, profileBytes);
        byte[] tiff = builder.Build();

        RawDocument document = RawCodec.OpenMetadata(new ReadOnlyMemory<byte>(tiff), null, new RawOpenOptions());
        RawIccProfile profile = document.Metadata.IccProfiles.Single();
        RawPayloadBlock payload = document.PayloadMap.Find(RawPayloadKind.IccProfile).Single();

        Assert.Multiple(() =>
        {
            Assert.That(profile.IfdPath, Is.EqualTo("IFD0"));
            Assert.That(profile.Offset, Is.EqualTo(payload.Offset));
            Assert.That(profile.Description, Is.EqualTo("Synthetic RGB"));
            Assert.That(document.ExtractPayload(payload), Is.EqualTo(profileBytes));
            Assert.That((document.Metadata.FindTag("ICCProfile") ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.CopyBytes(), Is.EqualTo(profileBytes));
        });

        RawDocument previewOnly = RawCodec.OpenPreviews(new ReadOnlyMemory<byte>(tiff), null, new RawOpenOptions());
        Assert.Multiple(() =>
        {
            Assert.That(previewOnly.Metadata.IccProfiles, Is.Empty);
            Assert.That(previewOnly.PayloadMap.Find(RawPayloadKind.IccProfile), Is.Empty);
        });
    }

    internal static byte[] CreateProfile()
    {
        List<(string Signature, byte[] Data)> tags = new List<(string Signature, byte[] Data)>
        {
            ("desc", CreateMluc("Synthetic RGB")),
            ("wtpt", CreateXyz(0.9642, 1.0, 0.8249)),
            ("rXYZ", CreateXyz(0.43607, 0.22250, 0.01393)),
            ("gXYZ", CreateXyz(0.38506, 0.71688, 0.09710)),
            ("bXYZ", CreateXyz(0.14308, 0.06062, 0.71417)),
            ("rTRC", CreateGammaCurve(2.2)),
            ("gTRC", CreateSampledCurve(new ushort[] { 0, 32768, 65535 })),
            ("bTRC", CreateParametricCurve(4, new double[] { 2.4, 0.94786, 0.05214, 0.0774, 0.04045, 0.0, 0.0 }))
        };
        int tableEnd = 132 + tags.Count * 12;
        int position = tableEnd;
        List<int> offsets = new List<int>();
        foreach ((string Signature, byte[] Data) tag in tags)
        {
            offsets.Add(position);
            position = AlignFour(position + tag.Data.Length);
        }
        byte[] profile = new byte[position];
        WriteUInt32(profile, 0, (uint)profile.Length);
        WriteSignature(profile, 4, "TEST");
        profile[8] = 4;
        profile[9] = 0x40;
        WriteSignature(profile, 12, "mntr");
        WriteSignature(profile, 16, "RGB ");
        WriteSignature(profile, 20, "XYZ ");
        WriteUInt16(profile, 24, 2026);
        WriteUInt16(profile, 26, 8);
        WriteUInt16(profile, 28, 19);
        WriteUInt16(profile, 30, 14);
        WriteUInt16(profile, 32, 30);
        WriteUInt16(profile, 34, 45);
        WriteSignature(profile, 36, "acsp");
        WriteSignature(profile, 40, "APPL");
        WriteUInt32(profile, 44, 1);
        WriteSignature(profile, 48, "TEST");
        WriteSignature(profile, 52, "RGB ");
        WriteUInt32(profile, 64, 1);
        WriteFixed(profile, 68, 0.9642);
        WriteFixed(profile, 72, 1.0);
        WriteFixed(profile, 76, 0.8249);
        WriteSignature(profile, 80, "TEST");
        WriteUInt32(profile, 128, (uint)tags.Count);
        for (int index = 0; index < tags.Count; index++)
        {
            int entryOffset = 132 + index * 12;
            WriteSignature(profile, entryOffset, tags[index].Signature);
            WriteUInt32(profile, entryOffset + 4, (uint)offsets[index]);
            WriteUInt32(profile, entryOffset + 8, (uint)tags[index].Data.Length);
            tags[index].Data.CopyTo(profile, offsets[index]);
        }
        byte[] idBytes = CalculateProfileId(profile);
        idBytes.CopyTo(profile, 84);
        return profile;
    }

    static byte[] CreateMluc(string text)
    {
        byte[] encoded = new UnicodeEncoding(true, false).GetBytes(text);
        byte[] data = new byte[28 + encoded.Length];
        WriteSignature(data, 0, "mluc");
        WriteUInt32(data, 8, 1);
        WriteUInt32(data, 12, 12);
        data[16] = (byte)'e';
        data[17] = (byte)'n';
        data[18] = (byte)'U';
        data[19] = (byte)'S';
        WriteUInt32(data, 20, (uint)encoded.Length);
        WriteUInt32(data, 24, 28);
        encoded.CopyTo(data, 28);
        return data;
    }

    static byte[] CreateXyz(double x, double y, double z)
    {
        byte[] data = new byte[20];
        WriteSignature(data, 0, "XYZ ");
        WriteFixed(data, 8, x);
        WriteFixed(data, 12, y);
        WriteFixed(data, 16, z);
        return data;
    }

    static byte[] CreateGammaCurve(double gamma)
    {
        byte[] data = new byte[14];
        WriteSignature(data, 0, "curv");
        WriteUInt32(data, 8, 1);
        WriteUInt16(data, 12, (ushort)Math.Round(gamma * 256.0));
        return data;
    }

    static byte[] CreateSampledCurve(ushort[] samples)
    {
        byte[] data = new byte[12 + samples.Length * 2];
        WriteSignature(data, 0, "curv");
        WriteUInt32(data, 8, (uint)samples.Length);
        for (int index = 0; index < samples.Length; index++)
        {
            WriteUInt16(data, 12 + index * 2, samples[index]);
        }
        return data;
    }

    static byte[] CreateParametricCurve(ushort functionType, double[] parameters)
    {
        byte[] data = new byte[12 + parameters.Length * 4];
        WriteSignature(data, 0, "para");
        WriteUInt16(data, 8, functionType);
        for (int index = 0; index < parameters.Length; index++)
        {
            WriteFixed(data, 12 + index * 4, parameters[index]);
        }
        return data;
    }

    static int FindTagOffset(byte[] profile, string signature)
    {
        int count = (int)BinaryPrimitives.ReadUInt32BigEndian(profile.AsSpan(128, 4));
        for (int index = 0; index < count; index++)
        {
            int entryOffset = 132 + index * 12;
            if (Encoding.ASCII.GetString(profile, entryOffset, 4) == signature)
            {
                return (int)BinaryPrimitives.ReadUInt32BigEndian(profile.AsSpan(entryOffset + 4, 4));
            }
        }
        throw new InvalidOperationException("Tag not found.");
    }

    static byte[] CalculateProfileId(byte[] profile)
    {
        byte[] input = profile.ToArray();
        Array.Clear(input, 44, 4);
        Array.Clear(input, 64, 4);
        Array.Clear(input, 84, 16);
        return MD5.HashData(input);
    }

    static int AlignFour(int value)
    {
        return (value + 3) & ~3;
    }

    static void WriteSignature(byte[] bytes, int offset, string signature)
    {
        Encoding.ASCII.GetBytes(signature).CopyTo(bytes, offset);
    }

    static void WriteUInt16(byte[] bytes, int offset, ushort value)
    {
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(offset, 2), value);
    }

    static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(offset, 4), value);
    }

    static void WriteFixed(byte[] bytes, int offset, double value)
    {
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(offset, 4), checked((int)Math.Round(value * 65536.0)));
    }
}
