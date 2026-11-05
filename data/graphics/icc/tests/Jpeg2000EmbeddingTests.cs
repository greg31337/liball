using icc;

namespace icc.Tests;

public class Jpeg2000EmbeddingTests
{
    [Test]
    public void EnumeratesAndExtractsJp2MethodTwoWithoutScanningCodestream()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        byte[] profile = EncodeProfile(CreateProfile(2050, string.Empty));
        byte[] hiddenProfile = EncodeProfile(CreateProfile(2051,
            "hidden in opaque codestream"));
        byte[] colour = ColourBox(2, 0, 0, profile, false);
        byte[] header = Box("jp2h", Box("ihdr", new byte[14]), colour,
            Box("zzZZ", new byte[] { 9, 7, 5 }));
        byte[] codestream = Box("jp2c", Join(
            new byte[] { 0xff, 0x4f, 1, 2, 3 }, hiddenProfile,
            new byte[] { 0xff, 0xd9 }));
        byte[] carrier = Jpeg2000("jp2 ", header, codestream);
        int profileOffset = FindSequence(carrier, profile);

        IccEmbeddedProfileResult result = reader.EnumerateProfilesBytes(
            carrier, new IccEmbeddingOptions());
        IccProfile[] extracted = reader.ExtractProfilesMemory(carrier,
            new IccReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Result.Status,
                Is.EqualTo(IccOperationStatus.Success));
            Assert.That(result.Profiles, Has.Length.EqualTo(1));
            Assert.That(result.Profiles[0].Format,
                Is.EqualTo(IccEmbeddedFormat.Jpeg2000));
            Assert.That(result.Profiles[0].Index, Is.Zero);
            Assert.That(result.Profiles[0].Location.Offset,
                Is.EqualTo(profileOffset));
            Assert.That(result.Profiles[0].Location.Length,
                Is.EqualTo(profile.Length));
            Assert.That(result.Profiles[0].Parts.Single().Offset,
                Is.EqualTo(profileOffset));
            Assert.That(result.Profiles[0].Profile.Location.Offset,
                Is.EqualTo(profileOffset));
            Assert.That(extracted, Has.Length.EqualTo(1));
            Assert.That(extracted[0].OriginalBytes, Is.EqualTo(profile));
        });
    }

    [Test]
    public void ReplacesIndexedJpxMethodThreeAndRepairsExtendedAncestors()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        IIccEmbeddedProfileWriter writer = new IccEmbeddedProfileWriter();
        IccProfile first = CreateProfile(2052, string.Empty);
        IccProfile oldSecond = CreateProfile(2053, string.Empty);
        IccProfile replacement = CreateProfile(2054,
            new string('R', 173));
        byte[] firstBytes = EncodeProfile(first);
        byte[] oldSecondBytes = EncodeProfile(oldSecond);
        byte[] replacementBytes = EncodeProfile(replacement);
        byte[] jp2Header = Box("jp2h",
            ColourBox(2, 0, 0, firstBytes, false));
        byte[] oldColour = ColourBox(3, 7, 1, oldSecondBytes, true);
        byte[] oldGroup = ExtendedBox("cgrp", oldColour);
        byte[] oldLayer = ExtendedBox("jplh", oldGroup,
            Box("lbl ", new byte[] { 4, 5, 6 }));
        byte[] unknown = Box("uuid", new byte[] { 8, 6, 4, 2 });
        byte[] codestream = Box("jp2c",
            new byte[] { 0xff, 0x4f, 0xaa, 0xbb, 0xff, 0xd9 });
        byte[] original = Jpeg2000("jpx ", jp2Header, unknown, oldLayer,
            codestream);
        int oldLayerStart = 12 + FileType("jpx ").Length
            + jp2Header.Length + unknown.Length;
        int oldGroupStart = oldLayerStart + 16;
        int oldColourStart = oldGroupStart + 16;
        int oldCodestreamStart = oldLayerStart + oldLayer.Length;

        byte[] replaced = writer.ReplaceProfileBytes(original, 1,
            replacement, IccEmbeddedFormat.Jpeg2000,
            new IccWriteOptions());
        int difference = replacementBytes.Length - oldSecondBytes.Length;
        IccEmbeddedProfileResult result = reader.EnumerateProfilesBytes(
            replaced, new IccEmbeddingOptions());
        IccProfile[] extracted = reader.ExtractProfilesBytes(replaced,
            new IccReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(difference, Is.Not.Zero);
            Assert.That(result.Profiles, Has.Length.EqualTo(2));
            Assert.That(extracted[0].OriginalBytes, Is.EqualTo(firstBytes));
            Assert.That(extracted[1].OriginalBytes,
                Is.EqualTo(replacementBytes));
            Assert.That(ReadUInt64(replaced, oldLayerStart + 8),
                Is.EqualTo(oldLayer.Length + difference));
            Assert.That(ReadUInt64(replaced, oldGroupStart + 8),
                Is.EqualTo(oldGroup.Length + difference));
            Assert.That(ReadUInt32(replaced, oldColourStart), Is.EqualTo(1));
            Assert.That(ReadUInt64(replaced, oldColourStart + 8),
                Is.EqualTo(oldColour.Length + difference));
            Assert.That(replaced[oldColourStart + 16], Is.EqualTo(3));
            Assert.That(replaced[oldColourStart + 17], Is.EqualTo(7));
            Assert.That(replaced[oldColourStart + 18], Is.EqualTo(1));
            Assert.That(replaced.Skip(oldCodestreamStart + difference),
                Is.EqualTo(original.Skip(oldCodestreamStart)));
            Assert.That(ContainsSequence(replaced, unknown), Is.True);
            Assert.That(writer.ProbeEmbeddingOperation(
                IccEmbeddedFormat.Jpeg2000,
                IccEmbeddedOperation.Replace).Status,
                Is.EqualTo(IccOperationStatus.Success));
        });
    }

    [Test]
    public void SupportsToEndColourBoxesAndStreamReplacementOffsets()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        IIccEmbeddedProfileWriter writer = new IccEmbeddedProfileWriter();
        IccProfile oldProfile = CreateProfile(2055, string.Empty);
        IccProfile newProfile = CreateProfile(2056, "larger replacement");
        byte[] oldBytes = EncodeProfile(oldProfile);
        byte[] newBytes = EncodeProfile(newProfile);
        byte[] colour = ColourBox(2, 3, 4, oldBytes, false);
        WriteUInt32(colour, 0, 0);
        byte[] header = Box("jp2h", colour);
        byte[] original = Jpeg2000("jp2 ", header);
        byte[] prefixed = Join(new byte[] { 1, 2, 3, 4, 5 }, original);
        using MemoryStream source = new MemoryStream(prefixed);
        source.Position = 5;
        using MemoryStream destination = new MemoryStream();

        writer.ReplaceProfileStream(source, destination, 0, newProfile,
            IccEmbeddedFormat.Jpeg2000, new IccWriteOptions());
        byte[] replaced = destination.ToArray();
        using MemoryStream readStream = new MemoryStream(Join(
            new byte[] { 6, 7, 8 }, replaced));
        readStream.Position = 3;
        IccEmbeddedProfileResult result = reader.EnumerateProfilesStream(
            readStream, new IccEmbeddingOptions());

        Assert.Multiple(() =>
        {
            Assert.That(ReadUInt32(replaced,
                12 + FileType("jp2 ").Length + 8), Is.Zero);
            Assert.That(ReadUInt32(replaced,
                12 + FileType("jp2 ").Length),
                Is.EqualTo((uint)(header.Length + newBytes.Length
                    - oldBytes.Length)));
            Assert.That(result.Profiles.Single().Location.Offset,
                Is.EqualTo(3 + FindSequence(replaced, newBytes)));
            Assert.That(result.Profiles.Single().Profile.OriginalBytes,
                Is.EqualTo(newBytes));
        });
    }

    [Test]
    public void IgnoresMethodOneAndIccLookingCodestreamBytes()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        byte[] hidden = EncodeProfile(CreateProfile(2057, string.Empty));
        byte[] methodOne = Box("colr",
            new byte[] { 1, 0, 0, 0, 0, 0, 16 });
        byte[] carrier = Jpeg2000("jp2 ", Box("jp2h", methodOne),
            Box("jp2c", hidden));

        IccEmbeddedProfileResult result = reader.EnumerateProfilesBytes(
            carrier, new IccEmbeddingOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Result.Status,
                Is.EqualTo(IccOperationStatus.NotFound));
            Assert.That(result.Profiles, Is.Empty);
            Assert.Throws<IccEmbeddingException>(() =>
                reader.ExtractProfilesBytes(carrier,
                    new IccReadOptions()));
        });
    }

    [Test]
    public void RejectsMalformedBoxesWrongMethodAndResourceLimits()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        byte[] profile = EncodeProfile(CreateProfile(2058, string.Empty));
        byte[] shortColour = Jpeg2000("jp2 ", Box("jp2h",
            Box("colr", new byte[] { 2, 0 })));
        byte[] methodThreeJp2 = Jpeg2000("jp2 ", Box("jp2h",
            ColourBox(3, 0, 0, profile, false)));
        byte[] beyondParent = Jpeg2000("jp2 ", Box("jp2h",
            InvalidLengthBox("colr", uint.MaxValue)));
        byte[] shortExtended = Jpeg2000("jp2 ", Box("jp2h",
            InvalidExtendedBox("colr", 12)));
        byte[] truncatedHeader = Join(Jpeg2000("jp2 ",
            Box("jp2h", ColourBox(2, 0, 0, profile, false))),
            new byte[] { 0, 0, 0, 8 });
        byte[] nested = Jpeg2000("jpx ", ExtendedBox("jplh",
            ExtendedBox("cgrp", ColourBox(3, 0, 0, profile, false))));
        IccEmbeddingOptions depthLimited = new IccEmbeddingOptions();
        depthLimited.ResourceLimits.MaximumNestingDepth = 1;
        IccEmbeddingOptions profileLimited = new IccEmbeddingOptions();
        profileLimited.ResourceLimits.MaximumProfileBytes = profile.Length - 1;

        Assert.Multiple(() =>
        {
            AssertStatus(reader, shortColour,
                IccOperationStatus.InvalidData, "method header");
            AssertStatus(reader, methodThreeJp2,
                IccOperationStatus.InvalidData, "requires JPX");
            AssertStatus(reader, beyondParent,
                IccOperationStatus.ResourceLimitExceeded, "beyond");
            AssertStatus(reader, shortExtended,
                IccOperationStatus.InvalidData, "shorter than its header");
            AssertStatus(reader, truncatedHeader,
                IccOperationStatus.InvalidData, "truncated box header");
            Assert.That(reader.EnumerateProfilesBytes(nested, depthLimited)
                .Result.Status,
                Is.EqualTo(IccOperationStatus.ResourceLimitExceeded));
            Assert.That(reader.EnumerateProfilesBytes(Jpeg2000("jp2 ",
                Box("jp2h", ColourBox(2, 0, 0, profile, false))),
                profileLimited).Result.Status,
                Is.EqualTo(IccOperationStatus.ResourceLimitExceeded));
        });
    }

    [Test]
    public void ExposesReplacementOnlyMutationSupport()
    {
        IIccEmbeddedProfileWriter writer = new IccEmbeddedProfileWriter();
        IccProfile profile = CreateProfile(2059, string.Empty);
        byte[] carrier = Jpeg2000("jp2 ", Box("jp2h",
            ColourBox(2, 0, 0, EncodeProfile(profile), false)));

        Assert.Multiple(() =>
        {
            Assert.That(writer.ProbeEmbeddingOperation(
                IccEmbeddedFormat.Jpeg2000,
                IccEmbeddedOperation.Insert).Status,
                Is.EqualTo(IccOperationStatus.UnsupportedFeature));
            Assert.That(writer.ProbeEmbeddingOperation(
                IccEmbeddedFormat.Jpeg2000,
                IccEmbeddedOperation.Remove).Status,
                Is.EqualTo(IccOperationStatus.UnsupportedFeature));
            Assert.Throws<IccEmbeddingException>(() =>
                writer.InsertProfileBytes(carrier, profile,
                    IccEmbeddedFormat.Jpeg2000,
                    new IccWriteOptions()));
            Assert.Throws<IccEmbeddingException>(() =>
                writer.RemoveProfileBytes(carrier, 0,
                    IccEmbeddedFormat.Jpeg2000));
            Assert.Throws<IccEmbeddingException>(() =>
                writer.ReplaceProfileBytes(carrier, 1, profile,
                    IccEmbeddedFormat.Jpeg2000,
                    new IccWriteOptions()));
        });
    }

    private static void AssertStatus(IIccEmbeddedProfileReader reader,
        byte[] carrier, IccOperationStatus status, string messagePart)
    {
        IccEmbeddedProfileResult result = reader.EnumerateProfilesBytes(
            carrier, new IccEmbeddingOptions());
        Assert.That(result.Result.Status, Is.EqualTo(status));
        Assert.That(result.Result.Message, Does.Contain(messagePart));
    }

    private static IccProfile CreateProfile(int year, string copyright)
    {
        IccProfileBuildOptions options = new IccProfileBuildOptions();
        options.ProfileClass = IccProfileClass.ColorSpace;
        options.DataColorSpaceSignature = IccColorSpaceSignatures.Xyz;
        options.CreationDateTime.Year = year;
        options.Copyright.Text = copyright;
        return new IccProfileBuilder().CreateXyzIdentityProfile(options);
    }

    private static byte[] EncodeProfile(IccProfile profile)
    {
        return new IccProfileWriter().WriteProfileBytes(profile,
            new IccWriteOptions());
    }

    private static byte[] Jpeg2000(string brand, params byte[][] boxes)
    {
        return Join(Box("jP  ", new byte[] { 0x0d, 0x0a, 0x87, 0x0a }),
            FileType(brand), Join(boxes));
    }

    private static byte[] FileType(string brand)
    {
        return Box("ftyp", System.Text.Encoding.ASCII.GetBytes(brand),
            new byte[4], System.Text.Encoding.ASCII.GetBytes(brand));
    }

    private static byte[] ColourBox(int method, int precedence,
        int approximation, byte[] profile, bool extended)
    {
        byte[] methodHeader = new byte[]
        {
            (byte)method, (byte)precedence, (byte)approximation
        };
        if (extended)
        {
            return ExtendedBox("colr", methodHeader, profile);
        }
        return Box("colr", methodHeader, profile);
    }

    private static byte[] Box(string type, params byte[][] parts)
    {
        byte[] payload = Join(parts);
        byte[] result = new byte[8 + payload.Length];
        WriteUInt32(result, 0, (uint)result.Length);
        System.Array.Copy(System.Text.Encoding.ASCII.GetBytes(type), 0,
            result, 4, 4);
        System.Array.Copy(payload, 0, result, 8, payload.Length);
        return result;
    }

    private static byte[] ExtendedBox(string type, params byte[][] parts)
    {
        byte[] payload = Join(parts);
        byte[] result = new byte[16 + payload.Length];
        WriteUInt32(result, 0, 1);
        System.Array.Copy(System.Text.Encoding.ASCII.GetBytes(type), 0,
            result, 4, 4);
        WriteUInt64(result, 8, result.Length);
        System.Array.Copy(payload, 0, result, 16, payload.Length);
        return result;
    }

    private static byte[] InvalidLengthBox(string type, uint length)
    {
        byte[] result = new byte[8];
        WriteUInt32(result, 0, length);
        System.Array.Copy(System.Text.Encoding.ASCII.GetBytes(type), 0,
            result, 4, 4);
        return result;
    }

    private static byte[] InvalidExtendedBox(string type, int length)
    {
        byte[] result = new byte[16];
        WriteUInt32(result, 0, 1);
        System.Array.Copy(System.Text.Encoding.ASCII.GetBytes(type), 0,
            result, 4, 4);
        WriteUInt64(result, 8, length);
        return result;
    }

    private static byte[] Join(params byte[][] values)
    {
        byte[] result = new byte[values.Sum(value => value.Length)];
        int offset = 0;
        foreach (byte[] value in values)
        {
            System.Array.Copy(value, 0, result, offset, value.Length);
            offset += value.Length;
        }
        return result;
    }

    private static int FindSequence(byte[] bytes, byte[] sequence)
    {
        for (int offset = 0; offset <= bytes.Length - sequence.Length;
            offset++)
        {
            if (bytes.AsSpan(offset, sequence.Length).SequenceEqual(sequence))
            {
                return offset;
            }
        }
        return -1;
    }

    private static bool ContainsSequence(byte[] bytes, byte[] sequence)
    {
        return FindSequence(bytes, sequence) >= 0;
    }

    private static uint ReadUInt32(byte[] bytes, int offset)
    {
        return ((uint)bytes[offset] << 24)
            | ((uint)bytes[offset + 1] << 16)
            | ((uint)bytes[offset + 2] << 8)
            | bytes[offset + 3];
    }

    private static long ReadUInt64(byte[] bytes, int offset)
    {
        return ((long)ReadUInt32(bytes, offset) << 32)
            | ReadUInt32(bytes, offset + 4);
    }

    private static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }

    private static void WriteUInt64(byte[] bytes, int offset, long value)
    {
        WriteUInt32(bytes, offset, (uint)(value >> 32));
        WriteUInt32(bytes, offset + 4, (uint)value);
    }
}
