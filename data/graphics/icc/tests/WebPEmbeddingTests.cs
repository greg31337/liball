using icc;

namespace icc.Tests;

public class WebPEmbeddingTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void PromotesSimpleWebPAndMaintainsVp8xFlagAndDimensions(
        bool lossless)
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        IIccEmbeddedProfileWriter writer = new IccEmbeddedProfileWriter();
        byte[] image = lossless ? LosslessImage(17, 9)
            : LossyImage(17, 9);
        byte[] unknown = Chunk("UNKN", new byte[] { 7, 8, 9 });
        byte[] original = WebP(image, unknown);
        IccProfile profile = CreateProfile(2041);
        byte[] profileBytes = EncodeProfile(profile);

        byte[] inserted = writer.InsertProfileBytes(original, profile,
            IccEmbeddedFormat.WebP, new IccWriteOptions());
        IccEmbeddedProfileResult result = reader.EnumerateProfilesBytes(inserted,
            new IccEmbeddingOptions());
        IccProfile extracted = reader.ExtractProfilesMemory(inserted,
            new IccReadOptions()).Single();
        byte[] removed = writer.RemoveProfileBytes(inserted, 0,
            IccEmbeddedFormat.WebP);
        int addedLength = 18 + 8 + profileBytes.Length
            + (profileBytes.Length & 1);

        Assert.Multiple(() =>
        {
            Assert.That(result.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(result.Profiles, Has.Length.EqualTo(1));
            Assert.That(result.Profiles[0].Format,
                Is.EqualTo(IccEmbeddedFormat.WebP));
            Assert.That(result.Profiles[0].Index, Is.Zero);
            Assert.That(result.Profiles[0].Profile.Location.Offset,
                Is.EqualTo(result.Profiles[0].Location.Offset));
            Assert.That(extracted.OriginalBytes, Is.EqualTo(profileBytes));
            Assert.That(Ascii(inserted, 12, 4), Is.EqualTo("VP8X"));
            Assert.That(inserted[20] & 0x20, Is.EqualTo(0x20));
            Assert.That(ReadUInt24(inserted, 24), Is.EqualTo(16));
            Assert.That(ReadUInt24(inserted, 27), Is.EqualTo(8));
            Assert.That(inserted.Skip(12 + addedLength),
                Is.EqualTo(original.Skip(12)));
            Assert.That(ReadUInt32(inserted, 4),
                Is.EqualTo((uint)inserted.Length - 8));
            Assert.That(removed[20] & 0x20, Is.Zero);
            Assert.That(removed.Skip(30), Is.EqualTo(original.Skip(12)));
            Assert.That(reader.EnumerateProfilesBytes(removed,
                new IccEmbeddingOptions()).Result.Status,
                Is.EqualTo(IccOperationStatus.NotFound));
            Assert.That(removed[^1], Is.Zero);
        });
    }

    [Test]
    public void ReplacesAndRemovesProfileWhilePreservingUnknownChunksAndPadding()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        IIccEmbeddedProfileWriter writer = new IccEmbeddedProfileWriter();
        IccProfile first = CreateProfile(2042);
        IccProfile second = CreateProfile(2043);
        byte[] firstBytes = EncodeProfile(first);
        byte[] secondBytes = EncodeProfile(second);
        byte[] unknown = Chunk("zzZZ", new byte[] { 1, 3, 5, 7, 9 });
        byte[] original = WebP(ExtendedHeader(0x20, 13, 11),
            Chunk("ICCP", firstBytes), LosslessImage(13, 11), unknown);
        int originalProfileStart = FindChunk(original, "ICCP");
        int originalProfileEnd = ChunkEnd(original, originalProfileStart);

        byte[] replaced = writer.ReplaceProfileBytes(original, 0, second,
            IccEmbeddedFormat.WebP, new IccWriteOptions());
        int replacementStart = FindChunk(replaced, "ICCP");
        int replacementEnd = ChunkEnd(replaced, replacementStart);
        IccProfile replacement = reader.ExtractProfilesBytes(replaced,
            new IccReadOptions()).Single();
        byte[] removed = writer.RemoveProfileBytes(replaced, 0,
            IccEmbeddedFormat.WebP);

        Assert.Multiple(() =>
        {
            Assert.That(replacement.OriginalBytes, Is.EqualTo(secondBytes));
            Assert.That(replaced.Skip(12).Take(originalProfileStart - 12),
                Is.EqualTo(original.Skip(12).Take(originalProfileStart - 12)));
            Assert.That(replaced.Skip(replacementEnd),
                Is.EqualTo(original.Skip(originalProfileEnd)));
            Assert.That(ContainsSequence(replaced, unknown), Is.True);
            Assert.That(ContainsSequence(removed, unknown), Is.True);
            Assert.That(removed[20] & 0x20, Is.Zero);
            Assert.That(ReadUInt32(removed, 4),
                Is.EqualTo((uint)removed.Length - 8));
            Assert.That(reader.EnumerateProfilesBytes(removed,
                new IccEmbeddingOptions()).Result.Status,
                Is.EqualTo(IccOperationStatus.NotFound));
            Assert.That(writer.ProbeEmbeddingOperation(IccEmbeddedFormat.WebP,
                IccEmbeddedOperation.Replace).Status,
                Is.EqualTo(IccOperationStatus.Success));
        });
    }

    [Test]
    public void SupportsStreamMutationAndCarrierLimits()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        IIccEmbeddedProfileWriter writer = new IccEmbeddedProfileWriter();
        byte[] original = WebP(LosslessImage(2, 2));
        IccProfile profile = CreateProfile(2044);
        using MemoryStream source = new MemoryStream(original);
        using MemoryStream destination = new MemoryStream();
        writer.InsertProfileStream(source, destination, profile,
            IccEmbeddedFormat.WebP, new IccWriteOptions());
        byte[] inserted = destination.ToArray();
        using MemoryStream removeSource = new MemoryStream(inserted);
        using MemoryStream removeDestination = new MemoryStream();
        writer.RemoveProfileStream(removeSource, removeDestination, 0,
            IccEmbeddedFormat.WebP);
        IccWriteOptions limited = new IccWriteOptions();
        limited.ResourceLimits.MaximumCarrierBytes = original.Length;

        Assert.Multiple(() =>
        {
            Assert.That(reader.ExtractProfilesStream(new MemoryStream(inserted),
                new IccReadOptions()), Has.Length.EqualTo(1));
            Assert.That(reader.EnumerateProfilesBytes(
                removeDestination.ToArray(), new IccEmbeddingOptions())
                .Result.Status, Is.EqualTo(IccOperationStatus.NotFound));
            Assert.Throws<IccEmbeddingException>(() => writer.InsertProfileBytes(
                original, profile, IccEmbeddedFormat.WebP, limited));
            Assert.Throws<IccEmbeddingException>(() => writer.RemoveProfileBytes(
                inserted, 1, IccEmbeddedFormat.WebP));
        });
    }

    [Test]
    public void RejectsRiffSizeChunkBoundsPaddingAndOrderingFailures()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        byte[] profile = EncodeProfile(CreateProfile(2045));
        byte[] valid = WebP(ExtendedHeader(0x20, 3, 3),
            Chunk("ICCP", profile), LosslessImage(3, 3));
        byte[] wrongRiffSize = valid.ToArray();
        WriteUInt32(wrongRiffSize, 4, (uint)valid.Length - 10);
        byte[] truncatedChunk = valid.ToArray();
        int profileOffset = FindChunk(truncatedChunk, "ICCP");
        WriteUInt32(truncatedChunk, profileOffset + 4, uint.MaxValue);
        byte[] badPadding = WebP(LosslessImage(3, 3),
            Chunk("UNKN", new byte[] { 1 }));
        badPadding[^1] = 1;
        byte[] lateProfile = WebP(ExtendedHeader(0x20, 3, 3),
            LosslessImage(3, 3), Chunk("ICCP", profile));
        byte[] afterAlpha = WebP(ExtendedHeader(0x30, 3, 3),
            Chunk("ALPH", new byte[] { 0 }), Chunk("ICCP", profile),
            LossyImage(3, 3));
        byte[] duplicate = WebP(ExtendedHeader(0x20, 3, 3),
            Chunk("ICCP", profile), Chunk("ICCP", profile),
            LosslessImage(3, 3));
        byte[] headerAfterImage = WebP(LosslessImage(3, 3),
            ExtendedHeader(0, 3, 3));

        Assert.Multiple(() =>
        {
            AssertInvalid(reader, wrongRiffSize, "RIFF size");
            AssertStatus(reader, truncatedChunk,
                IccOperationStatus.ResourceLimitExceeded, "managed size");
            AssertInvalid(reader, badPadding, "padding");
            AssertInvalid(reader, lateProfile, "precede");
            AssertInvalid(reader, afterAlpha, "precede");
            AssertInvalid(reader, duplicate, "more than one");
            AssertInvalid(reader, headerAfterImage, "unique, first");
            Assert.Throws<IccEmbeddingException>(() =>
                reader.ExtractProfilesBytes(badPadding, new IccReadOptions()));
        });
    }

    [Test]
    public void RejectsVp8xFlagDisagreementAndInvalidProfiles()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        byte[] profile = EncodeProfile(CreateProfile(2046));
        byte[] missingFlag = WebP(ExtendedHeader(0, 5, 4),
            Chunk("ICCP", profile), LosslessImage(5, 4));
        byte[] missingChunk = WebP(ExtendedHeader(0x20, 5, 4),
            LosslessImage(5, 4));
        byte[] invalidProfile = WebP(ExtendedHeader(0x20, 5, 4),
            Chunk("ICCP", new byte[] { 1, 2, 3, 4 }),
            LosslessImage(5, 4));
        byte[] reserved = WebP(ExtendedHeader(0x21, 5, 4),
            Chunk("ICCP", profile), LosslessImage(5, 4));

        Assert.Multiple(() =>
        {
            AssertInvalid(reader, missingFlag, "disagree");
            AssertInvalid(reader, missingChunk, "disagree");
            AssertInvalid(reader, invalidProfile, "profile is invalid");
            AssertInvalid(reader, reserved, "reserved");
        });
    }

    [Test]
    public void AppliesProfileLimitsAndRejectsUnpromotableSimpleImages()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        IIccEmbeddedProfileWriter writer = new IccEmbeddedProfileWriter();
        IccProfile profile = CreateProfile(2047);
        byte[] profileBytes = EncodeProfile(profile);
        byte[] carrier = WebP(ExtendedHeader(0x20, 4, 4),
            Chunk("ICCP", profileBytes), LosslessImage(4, 4));
        IccEmbeddingOptions limited = new IccEmbeddingOptions();
        limited.ResourceLimits.MaximumProfileBytes = profileBytes.Length - 1;
        IccEmbeddedProfileResult limitResult = reader.EnumerateProfilesBytes(
            carrier, limited);
        byte[] invalidSimple = WebP(Chunk("VP8L",
            new byte[] { 0, 1, 2, 3, 4 }));

        Assert.Multiple(() =>
        {
            Assert.That(limitResult.Result.Status,
                Is.EqualTo(IccOperationStatus.ResourceLimitExceeded));
            Assert.Throws<IccEmbeddingException>(() => writer.InsertProfileBytes(
                invalidSimple, profile, IccEmbeddedFormat.WebP,
                new IccWriteOptions()));
        });
    }

    private static void AssertInvalid(IIccEmbeddedProfileReader reader,
        byte[] carrier, string messagePart)
    {
        AssertStatus(reader, carrier, IccOperationStatus.InvalidData,
            messagePart);
    }

    private static void AssertStatus(IIccEmbeddedProfileReader reader,
        byte[] carrier, IccOperationStatus status, string messagePart)
    {
        IccEmbeddedProfileResult result = reader.EnumerateProfilesBytes(carrier,
            new IccEmbeddingOptions());
        Assert.That(result.Result.Status, Is.EqualTo(status));
        Assert.That(result.Result.Message, Does.Contain(messagePart));
    }

    private static IccProfile CreateProfile(int year)
    {
        IccProfileBuildOptions options = new IccProfileBuildOptions();
        options.ProfileClass = IccProfileClass.ColorSpace;
        options.DataColorSpaceSignature = IccColorSpaceSignatures.Xyz;
        options.CreationDateTime.Year = year;
        return new IccProfileBuilder().CreateXyzIdentityProfile(options);
    }

    private static byte[] EncodeProfile(IccProfile profile)
    {
        return new IccProfileWriter().WriteProfileBytes(profile,
            new IccWriteOptions());
    }

    private static byte[] WebP(params byte[][] chunks)
    {
        int length = 12 + chunks.Sum(value => value.Length);
        byte[] result = new byte[length];
        System.Array.Copy(System.Text.Encoding.ASCII.GetBytes("RIFF"),
            result, 4);
        WriteUInt32(result, 4, (uint)length - 8);
        System.Array.Copy(System.Text.Encoding.ASCII.GetBytes("WEBP"), 0,
            result, 8, 4);
        int offset = 12;
        foreach (byte[] chunk in chunks)
        {
            System.Array.Copy(chunk, 0, result, offset, chunk.Length);
            offset += chunk.Length;
        }
        return result;
    }

    private static byte[] Chunk(string type, byte[] payload)
    {
        byte[] result = new byte[8 + payload.Length + (payload.Length & 1)];
        System.Array.Copy(System.Text.Encoding.ASCII.GetBytes(type), result, 4);
        WriteUInt32(result, 4, (uint)payload.Length);
        System.Array.Copy(payload, 0, result, 8, payload.Length);
        return result;
    }

    private static byte[] ExtendedHeader(byte flags, int width, int height)
    {
        byte[] payload = new byte[10];
        payload[0] = flags;
        WriteUInt24(payload, 4, width - 1);
        WriteUInt24(payload, 7, height - 1);
        return Chunk("VP8X", payload);
    }

    private static byte[] LosslessImage(int width, int height)
    {
        uint bits = (uint)(width - 1) | ((uint)(height - 1) << 14);
        byte[] payload = new byte[5];
        payload[0] = 0x2f;
        WriteUInt32(payload, 1, bits);
        return Chunk("VP8L", payload);
    }

    private static byte[] LossyImage(int width, int height)
    {
        byte[] payload = new byte[10];
        payload[3] = 0x9d;
        payload[4] = 0x01;
        payload[5] = 0x2a;
        payload[6] = (byte)width;
        payload[7] = (byte)(width >> 8);
        payload[8] = (byte)height;
        payload[9] = (byte)(height >> 8);
        return Chunk("VP8 ", payload);
    }

    private static int FindChunk(byte[] carrier, string type)
    {
        int offset = 12;
        while (offset <= carrier.Length - 8)
        {
            if (Ascii(carrier, offset, 4) == type)
            {
                return offset;
            }
            offset = ChunkEnd(carrier, offset);
        }
        return -1;
    }

    private static int ChunkEnd(byte[] carrier, int offset)
    {
        int length = (int)ReadUInt32(carrier, offset + 4);
        return offset + 8 + length + (length & 1);
    }

    private static bool ContainsSequence(byte[] bytes, byte[] sequence)
    {
        for (int offset = 0; offset <= bytes.Length - sequence.Length; offset++)
        {
            bool matches = true;
            for (int index = 0; index < sequence.Length; index++)
            {
                if (bytes[offset + index] != sequence[index])
                {
                    matches = false;
                }
            }
            if (matches)
            {
                return true;
            }
        }
        return false;
    }

    private static string Ascii(byte[] bytes, int offset, int length)
    {
        return System.Text.Encoding.ASCII.GetString(bytes, offset, length);
    }

    private static uint ReadUInt32(byte[] bytes, int offset)
    {
        return bytes[offset] | ((uint)bytes[offset + 1] << 8)
            | ((uint)bytes[offset + 2] << 16)
            | ((uint)bytes[offset + 3] << 24);
    }

    private static int ReadUInt24(byte[] bytes, int offset)
    {
        return bytes[offset] | (bytes[offset + 1] << 8)
            | (bytes[offset + 2] << 16);
    }

    private static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)value;
        bytes[offset + 1] = (byte)(value >> 8);
        bytes[offset + 2] = (byte)(value >> 16);
        bytes[offset + 3] = (byte)(value >> 24);
    }

    private static void WriteUInt24(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)value;
        bytes[offset + 1] = (byte)(value >> 8);
        bytes[offset + 2] = (byte)(value >> 16);
    }
}
