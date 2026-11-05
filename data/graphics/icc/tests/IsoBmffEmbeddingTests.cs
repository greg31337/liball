using icc;

namespace icc.Tests;

public class IsoBmffEmbeddingTests
{
    [Test]
    public void EnumeratesAvifProfAndRestrictedProfilesWithExactLocations()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        byte[] first = EncodeProfile(CreateProfile(2060, string.Empty));
        byte[] second = EncodeProfile(CreateProfile(2061,
            "restricted profile"));
        byte[] hidden = EncodeProfile(CreateProfile(2062,
            "opaque media payload"));
        byte[] unknown = Box("zzZZ", new byte[] { 9, 7, 5, 3, 1 });
        byte[] properties = Box("iprp", Box("ipco",
            ColourBox("prof", first, false),
            ColourBox("nclx", new byte[] { 0, 1, 0, 13, 0, 6, 0x80 },
                false), unknown,
            ColourBox("rICC", second, true)),
            FullBox("ipma", 0, new byte[4]));
        byte[] meta = Meta(properties);
        byte[] media = Box("mdat", Join(new byte[] { 4, 3, 2, 1 }, hidden));
        byte[] carrier = IsoFile("avif", false, meta, media);

        IccEmbeddedProfileResult result = reader.EnumerateProfilesBytes(
            carrier, new IccEmbeddingOptions());
        IccProfile[] extracted = reader.ExtractProfilesMemory(carrier,
            new IccReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Result.Status,
                Is.EqualTo(IccOperationStatus.Success));
            Assert.That(result.Profiles, Has.Length.EqualTo(2));
            Assert.That(result.Profiles.Select(value => value.Format),
                Is.EqualTo(new[]
                {
                    IccEmbeddedFormat.Avif, IccEmbeddedFormat.Avif
                }));
            Assert.That(result.Profiles.Select(value => value.Index),
                Is.EqualTo(new[] { 0, 1 }));
            Assert.That(result.Profiles[0].Location.Offset,
                Is.EqualTo(FindSequence(carrier, first)));
            Assert.That(result.Profiles[1].Location.Offset,
                Is.EqualTo(FindSequence(carrier, second)));
            Assert.That(result.Profiles[1].Parts.Single().Length,
                Is.EqualTo(second.Length));
            Assert.That(result.Profiles[1].Profile.Location.Offset,
                Is.EqualTo(FindSequence(carrier, second)));
            Assert.That(extracted.Select(value => value.OriginalBytes),
                Is.EqualTo(new[] { first, second }));
        });
    }

    [Test]
    public void ReplacesAvifPropertyAndRepairsBoxesAndItemLocationOffset()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        IIccEmbeddedProfileWriter writer = new IccEmbeddedProfileWriter();
        IccProfile oldProfile = CreateProfile(2063, string.Empty);
        IccProfile replacement = CreateProfile(2064,
            new string('A', 191));
        byte[] oldBytes = EncodeProfile(oldProfile);
        byte[] replacementBytes = EncodeProfile(replacement);
        byte[] unknown = Box("uuid", new byte[] { 8, 6, 4, 2 });
        byte[] mediaPayload = new byte[]
        {
            0x81, 0x22, 0x43, 0x64, 0x85, 0xa6
        };
        byte[] fileType = FileType("avif", false);
        byte[] placeholderIloc = ItemLocation(0, mediaPayload.Length);
        byte[] oldColour = ColourBox("prof", oldBytes, true);
        byte[] oldContainer = Box("ipco", unknown, oldColour,
            Box("free", new byte[] { 1, 2, 3 }));
        byte[] oldProperties = ExtendedBox("iprp", oldContainer,
            FullBox("ipma", 0, new byte[4]));
        byte[] placeholderMeta = Meta(placeholderIloc, oldProperties);
        int oldMediaOffset = fileType.Length + placeholderMeta.Length + 8;
        byte[] itemLocation = ItemLocation(oldMediaOffset,
            mediaPayload.Length);
        byte[] meta = Meta(itemLocation, oldProperties);
        byte[] media = Box("mdat", mediaPayload);
        byte[] original = Join(fileType, meta, media);
        int metaStart = fileType.Length;
        int itemLocationStart = metaStart + 12;
        int propertiesStart = itemLocationStart + itemLocation.Length;
        int containerStart = propertiesStart + 16;
        int colourStart = containerStart + 8 + unknown.Length;

        byte[] replaced = writer.ReplaceProfileBytes(original, 0,
            replacement, IccEmbeddedFormat.Avif,
            new IccWriteOptions());
        int difference = replacementBytes.Length - oldBytes.Length;
        IccProfile extracted = reader.ExtractProfilesBytes(replaced,
            new IccReadOptions()).Single();

        Assert.Multiple(() =>
        {
            Assert.That(difference, Is.Not.Zero);
            Assert.That(extracted.OriginalBytes,
                Is.EqualTo(replacementBytes));
            Assert.That(ReadUInt32(replaced, metaStart),
                Is.EqualTo((uint)(meta.Length + difference)));
            Assert.That(ReadUInt64(replaced, propertiesStart + 8),
                Is.EqualTo(oldProperties.Length + difference));
            Assert.That(ReadUInt32(replaced, containerStart),
                Is.EqualTo((uint)(oldContainer.Length + difference)));
            Assert.That(ReadUInt32(replaced, colourStart), Is.EqualTo(1));
            Assert.That(ReadUInt64(replaced, colourStart + 8),
                Is.EqualTo(oldColour.Length + difference));
            Assert.That(Ascii(replaced, colourStart + 16, 4),
                Is.EqualTo("prof"));
            Assert.That(ReadUInt32(replaced, itemLocationStart + 26),
                Is.EqualTo((uint)(oldMediaOffset + difference)));
            Assert.That(replaced.Skip(oldMediaOffset - 8 + difference),
                Is.EqualTo(original.Skip(oldMediaOffset - 8)));
            Assert.That(ContainsSequence(replaced, unknown), Is.True);
        });
    }

    [Test]
    public void ReadsExtendedHeifFileTypeAndUsesAbsoluteStreamOffsets()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        IIccEmbeddedProfileWriter writer = new IccEmbeddedProfileWriter();
        byte[] profile = EncodeProfile(CreateProfile(2065, string.Empty));
        byte[] file = IsoFile("heic", true, Meta(Box("iprp",
            Box("ipco", ColourBox("rICC", profile, false)))));
        byte[] prefixed = Join(new byte[] { 7, 7, 7, 7, 7, 7 }, file);
        using MemoryStream stream = new MemoryStream(prefixed);
        stream.Position = 6;

        IccEmbeddedProfileResult result = reader.EnumerateProfilesStream(
            stream, new IccEmbeddingOptions());
        IccProfile replacement = CreateProfile(2071,
            "HEIF stream replacement");
        byte[] replacementBytes = EncodeProfile(replacement);
        using MemoryStream source = new MemoryStream(file);
        using MemoryStream destination = new MemoryStream();
        writer.ReplaceProfileStream(source, destination, 0, replacement,
            IccEmbeddedFormat.Heif, new IccWriteOptions());
        IccProfile replacedProfile = reader.ExtractProfilesBytes(
            destination.ToArray(), new IccReadOptions()).Single();

        Assert.Multiple(() =>
        {
            Assert.That(reader.DetectCarrierBytes(file),
                Is.EqualTo(IccEmbeddedFormat.Heif));
            Assert.That(result.Result.Status,
                Is.EqualTo(IccOperationStatus.Success));
            Assert.That(result.Profiles.Single().Format,
                Is.EqualTo(IccEmbeddedFormat.Heif));
            Assert.That(result.Profiles.Single().Location.Offset,
                Is.EqualTo(6 + FindSequence(file, profile)));
            Assert.That(result.Profiles.Single().Profile.OriginalBytes,
                Is.EqualTo(profile));
            Assert.That(replacedProfile.OriginalBytes,
                Is.EqualTo(replacementBytes));
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ReplacesSequenceSampleEntryAndRepairsChunkOffset(
        bool largeOffset)
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        IIccEmbeddedProfileWriter writer = new IccEmbeddedProfileWriter();
        IccProfile oldProfile = CreateProfile(2066, string.Empty);
        IccProfile replacement = CreateProfile(2067,
            new string('S', 149));
        byte[] oldBytes = EncodeProfile(oldProfile);
        byte[] replacementBytes = EncodeProfile(replacement);
        byte[] fileType = FileType("avis", false);
        byte[] oldColour = ColourBox("rICC", oldBytes, false);
        byte[] sampleEntry = Box("av01", new byte[78],
            Box("av1C", new byte[] { 0x81, 0, 0, 0 }), oldColour,
            Box("pasp", new byte[8]));
        byte[] placeholderSampleTable = Box("stbl",
            SampleDescriptionBox(sampleEntry),
            ChunkOffsetBox(0, largeOffset));
        byte[] placeholderMovie = ExtendedBox("moov", Box("trak",
            Box("mdia", Box("minf", placeholderSampleTable))));
        int oldMediaOffset = fileType.Length + placeholderMovie.Length + 8;
        byte[] sampleTable = Box("stbl", SampleDescriptionBox(sampleEntry),
            ChunkOffsetBox(oldMediaOffset, largeOffset));
        byte[] movie = ExtendedBox("moov", Box("trak",
            Box("mdia", Box("minf", sampleTable))));
        byte[] media = Box("mdat", new byte[]
        {
            0x12, 0x34, 0x56, 0x78, 0x9a
        });
        byte[] original = Join(fileType, movie, media);
        string chunkType = largeOffset ? "co64" : "stco";
        int oldChunkBox = FindAscii(original, chunkType) - 4;

        byte[] replaced = writer.ReplaceProfileBytes(original, 0,
            replacement, IccEmbeddedFormat.Avif,
            new IccWriteOptions());
        int difference = replacementBytes.Length - oldBytes.Length;
        int newChunkBox = FindAscii(replaced, chunkType) - 4;
        long changedChunkOffset = largeOffset
            ? ReadUInt64(replaced, newChunkBox + 16)
            : ReadUInt32(replaced, newChunkBox + 16);
        IccProfile extracted = reader.ExtractProfilesBytes(replaced,
            new IccReadOptions()).Single();

        Assert.Multiple(() =>
        {
            Assert.That(extracted.OriginalBytes,
                Is.EqualTo(replacementBytes));
            Assert.That(ReadUInt64(replaced, fileType.Length + 8),
                Is.EqualTo(movie.Length + difference));
            Assert.That(newChunkBox,
                Is.EqualTo(oldChunkBox + difference));
            Assert.That(changedChunkOffset,
                Is.EqualTo(oldMediaOffset + difference));
            Assert.That(replaced.Skip(oldMediaOffset - 8 + difference),
                Is.EqualTo(original.Skip(oldMediaOffset - 8)));
        });
    }

    [Test]
    public void IgnoresNonIccColourAndOpaqueMediaProfiles()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        byte[] hidden = EncodeProfile(CreateProfile(2068, string.Empty));
        byte[] carrier = IsoFile("avif", false,
            Meta(Box("iprp", Box("ipco", ColourBox("nclx",
                new byte[] { 0, 1, 0, 13, 0, 6, 0x80 }, false)))),
            Box("mdat", hidden));

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
    public void RejectsMalformedNestedBoxesProfilesTablesAndLimits()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        byte[] profile = EncodeProfile(CreateProfile(2069, string.Empty));
        byte[] shortMeta = IsoFile("avif", false,
            Box("meta", new byte[] { 0, 0, 0 }));
        byte[] shortColourBox = Box("colr", new byte[] { 1, 2, 3 });
        byte[] shortColour = IsoFile("avif", false,
            Meta(Box("iprp", Box("ipco", shortColourBox))));
        byte[] invalidProfile = IsoFile("avif", false,
            Meta(Box("iprp", Box("ipco",
                ColourBox("prof", new byte[] { 1, 2, 3 }, false)))));
        byte[] beyondParent = IsoFile("avif", false,
            Meta(Box("iprp", Box("ipco",
                InvalidLengthBox("colr", uint.MaxValue)))));
        byte[] badItemLocation = IsoFile("avif", false,
            Meta(FullBox("iloc", 0, new byte[] { 0 })));
        byte[] sampleEntry = Box("av01", new byte[78]);
        byte[] badDescription = IsoFile("avis", false,
            Box("moov", Box("trak", Box("mdia", Box("minf",
                Box("stbl", FullBox("stsd", 0,
                    Join(UInt32Bytes(2), sampleEntry))))))));
        byte[] nested = IsoFile("avif", false,
            Meta(Box("iprp", Box("ipco",
                ColourBox("prof", profile, false)))));
        IccEmbeddingOptions depthLimited = new IccEmbeddingOptions();
        depthLimited.ResourceLimits.MaximumNestingDepth = 2;
        IccEmbeddingOptions profileLimited = new IccEmbeddingOptions();
        profileLimited.ResourceLimits.MaximumProfileBytes = profile.Length - 1;

        Assert.Multiple(() =>
        {
            AssertStatus(reader, shortMeta, IccOperationStatus.InvalidData,
                "FullBox header");
            AssertStatus(reader, shortColour,
                IccOperationStatus.InvalidData, "colour type");
            AssertStatus(reader, invalidProfile,
                IccOperationStatus.InvalidData, "payload is invalid");
            AssertStatus(reader, beyondParent,
                IccOperationStatus.ResourceLimitExceeded, "beyond");
            AssertStatus(reader, badItemLocation,
                IccOperationStatus.InvalidData, "shorter than its header");
            AssertStatus(reader, badDescription,
                IccOperationStatus.InvalidData, "truncated box header");
            Assert.That(reader.EnumerateProfilesBytes(nested, depthLimited)
                .Result.Status,
                Is.EqualTo(IccOperationStatus.ResourceLimitExceeded));
            Assert.That(reader.EnumerateProfilesBytes(nested, profileLimited)
                .Result.Status,
                Is.EqualTo(IccOperationStatus.ResourceLimitExceeded));
        });
    }

    [Test]
    public void ExposesReplacementOnlyForAvifAndHeif()
    {
        IIccEmbeddedProfileWriter writer = new IccEmbeddedProfileWriter();
        IccProfile profile = CreateProfile(2070, string.Empty);
        byte[] avif = IsoFile("avif", false, Meta(Box("iprp",
            Box("ipco", ColourBox("prof", EncodeProfile(profile),
                false)))));

        Assert.Multiple(() =>
        {
            Assert.That(writer.ProbeEmbeddingOperation(
                IccEmbeddedFormat.Avif,
                IccEmbeddedOperation.Replace).Status,
                Is.EqualTo(IccOperationStatus.Success));
            Assert.That(writer.ProbeEmbeddingOperation(
                IccEmbeddedFormat.Heif,
                IccEmbeddedOperation.Replace).Status,
                Is.EqualTo(IccOperationStatus.Success));
            Assert.That(writer.ProbeEmbeddingOperation(
                IccEmbeddedFormat.Avif,
                IccEmbeddedOperation.Insert).Status,
                Is.EqualTo(IccOperationStatus.UnsupportedFeature));
            Assert.That(writer.ProbeEmbeddingOperation(
                IccEmbeddedFormat.Heif,
                IccEmbeddedOperation.Remove).Status,
                Is.EqualTo(IccOperationStatus.UnsupportedFeature));
            Assert.Throws<IccEmbeddingException>(() =>
                writer.InsertProfileBytes(avif, profile,
                    IccEmbeddedFormat.Avif, new IccWriteOptions()));
            Assert.Throws<IccEmbeddingException>(() =>
                writer.RemoveProfileBytes(avif, 0,
                    IccEmbeddedFormat.Avif));
            Assert.Throws<IccEmbeddingException>(() =>
                writer.ReplaceProfileBytes(avif, 0, profile,
                    IccEmbeddedFormat.Heif, new IccWriteOptions()));
            Assert.Throws<IccEmbeddingException>(() =>
                writer.ReplaceProfileBytes(avif, 1, profile,
                    IccEmbeddedFormat.Avif, new IccWriteOptions()));
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

    private static byte[] IsoFile(string brand, bool extendedFileType,
        params byte[][] boxes)
    {
        return Join(FileType(brand, extendedFileType), Join(boxes));
    }

    private static byte[] FileType(string brand, bool extended)
    {
        byte[] brandBytes = System.Text.Encoding.ASCII.GetBytes(brand);
        if (extended)
        {
            return ExtendedBox("ftyp", brandBytes, new byte[4], brandBytes);
        }
        return Box("ftyp", brandBytes, new byte[4], brandBytes);
    }

    private static byte[] Meta(params byte[][] children)
    {
        return FullBox("meta", 0, Join(children));
    }

    private static byte[] ColourBox(string colourType, byte[] payload,
        bool extended)
    {
        byte[] type = System.Text.Encoding.ASCII.GetBytes(colourType);
        if (extended)
        {
            return ExtendedBox("colr", type, payload);
        }
        return Box("colr", type, payload);
    }

    private static byte[] ItemLocation(int offset, int length)
    {
        byte[] fields = new byte[18];
        fields[0] = 0x44;
        fields[1] = 0x40;
        WriteUInt16(fields, 2, 1);
        WriteUInt16(fields, 4, 1);
        WriteUInt16(fields, 6, 0);
        WriteUInt32(fields, 8, 0);
        WriteUInt16(fields, 12, 1);
        WriteUInt32(fields, 14, (uint)offset);
        byte[] extentLength = UInt32Bytes((uint)length);
        return FullBox("iloc", 0, fields, extentLength);
    }

    private static byte[] SampleDescriptionBox(byte[] sampleEntry)
    {
        return FullBox("stsd", 0, UInt32Bytes(1), sampleEntry);
    }

    private static byte[] ChunkOffsetBox(int offset, bool large)
    {
        if (large)
        {
            byte[] encodedOffset = new byte[8];
            WriteUInt64(encodedOffset, 0, offset);
            return FullBox("co64", 0, UInt32Bytes(1), encodedOffset);
        }
        return FullBox("stco", 0, UInt32Bytes(1),
            UInt32Bytes((uint)offset));
    }

    private static byte[] FullBox(string type, int version,
        params byte[][] parts)
    {
        byte[] fullHeader = new byte[4];
        fullHeader[0] = (byte)version;
        return Box(type, fullHeader, Join(parts));
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

    private static byte[] UInt32Bytes(uint value)
    {
        byte[] result = new byte[4];
        WriteUInt32(result, 0, value);
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

    private static int FindAscii(byte[] bytes, string text)
    {
        return FindSequence(bytes, System.Text.Encoding.ASCII.GetBytes(text));
    }

    private static bool ContainsSequence(byte[] bytes, byte[] sequence)
    {
        return FindSequence(bytes, sequence) >= 0;
    }

    private static string Ascii(byte[] bytes, int offset, int length)
    {
        return System.Text.Encoding.ASCII.GetString(bytes, offset, length);
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

    private static void WriteUInt16(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
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
