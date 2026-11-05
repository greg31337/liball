using icc;

namespace icc.Tests;

public class TiffEmbeddingTests
{
    [TestCase(false, false)]
    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    [TestCase(true, true)]
    public void ExtractsReplacesAndRemovesAcrossLayoutsAndByteOrders(
        bool bigTiff, bool littleEndian)
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        IIccEmbeddedProfileWriter writer = new IccEmbeddedProfileWriter();
        IccProfile first = CreateProfile(2032);
        IccProfile second = CreateProfile(2033);
        byte[] firstBytes = EncodeProfile(first);
        byte[] secondBytes = EncodeProfile(second);
        byte[] original = CreateTiff(bigTiff, littleEndian, false,
            new byte[][] { firstBytes });
        IccEmbeddedFormat format = bigTiff ? IccEmbeddedFormat.BigTiff
            : IccEmbeddedFormat.Tiff;

        IccEmbeddedProfileResult result = reader.EnumerateProfilesBytes(original,
            new IccEmbeddingOptions());
        byte[] replaced = writer.ReplaceProfileBytes(original, 0, second,
            format, new IccWriteOptions());
        IccProfile replacement = reader.ExtractProfilesBytes(replaced,
            new IccReadOptions()).Single();
        byte[] removed = writer.RemoveProfileBytes(replaced, 0, format);
        IccEmbeddedProfileResult removedResult = reader.EnumerateProfilesBytes(
            removed, new IccEmbeddingOptions());
        int originalEntry = FindTagEntry(original, bigTiff, littleEndian,
            34675);
        byte[] normalizedReplacement = replaced.Take(original.Length).ToArray();
        System.Array.Copy(original, originalEntry + 4, normalizedReplacement,
            originalEntry + 4, bigTiff ? 16 : 8);
        byte[] normalizedRemoval = removed.Take(replaced.Length).ToArray();
        int headerPointer = bigTiff ? 8 : 4;
        int pointerSize = bigTiff ? 8 : 4;
        System.Array.Copy(replaced, headerPointer, normalizedRemoval,
            headerPointer, pointerSize);

        Assert.Multiple(() =>
        {
            Assert.That(reader.DetectCarrierBytes(original), Is.EqualTo(format));
            Assert.That(result.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(result.Profiles, Has.Length.EqualTo(1));
            Assert.That(result.Profiles[0].Format, Is.EqualTo(format));
            Assert.That(result.Profiles[0].Index, Is.Zero);
            Assert.That(result.Profiles[0].Parts, Has.Length.EqualTo(1));
            Assert.That(result.Profiles[0].Profile.Location.Offset,
                Is.EqualTo(result.Profiles[0].Location.Offset));
            Assert.That(result.Profiles[0].Profile.OriginalBytes,
                Is.EqualTo(firstBytes));
            Assert.That(replacement.OriginalBytes, Is.EqualTo(secondBytes));
            Assert.That(normalizedReplacement, Is.EqualTo(original));
            Assert.That(normalizedRemoval, Is.EqualTo(replaced));
            Assert.That(ContainsSequence(replaced,
                new byte[] { 0xde, 0xad, 0xbe }), Is.True);
            Assert.That(removedResult.Result.Status,
                Is.EqualTo(IccOperationStatus.NotFound));
            Assert.That(ContainsSequence(removed,
                new byte[] { 0xde, 0xad, 0xbe }), Is.True);
            Assert.Throws<IccEmbeddingException>(() =>
                reader.ExtractProfilesBytes(removed, new IccReadOptions()));
        });
    }

    [Test]
    public void EnumeratesAndMutatesProfilesThroughAnIfdChain()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        IIccEmbeddedProfileWriter writer = new IccEmbeddedProfileWriter();
        byte[] first = EncodeProfile(CreateProfile(2034));
        byte[] second = EncodeProfile(CreateProfile(2035));
        IccProfile replacementProfile = CreateProfile(2036);
        byte[] replacement = EncodeProfile(replacementProfile);
        byte[] carrier = CreateTiff(false, true, false,
            new byte[][] { first, second });

        IccEmbeddedProfileResult initial = reader.EnumerateProfilesBytes(carrier,
            new IccEmbeddingOptions());
        byte[] replaced = writer.ReplaceProfileBytes(carrier, 1,
            replacementProfile, IccEmbeddedFormat.Tiff,
            new IccWriteOptions());
        IccProfile[] replacedProfiles = reader.ExtractProfilesBytes(replaced,
            new IccReadOptions());
        byte[] removedFirst = writer.RemoveProfileBytes(replaced, 0,
            IccEmbeddedFormat.Tiff);
        IccProfile[] remaining = reader.ExtractProfilesBytes(removedFirst,
            new IccReadOptions());
        byte[] removedSecond = writer.RemoveProfileBytes(replaced, 1,
            IccEmbeddedFormat.Tiff);
        IccProfile[] firstRemaining = reader.ExtractProfilesBytes(removedSecond,
            new IccReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(initial.Profiles, Has.Length.EqualTo(2));
            Assert.That(initial.Profiles.Select(value => value.Index),
                Is.EqualTo(new int[] { 0, 1 }));
            Assert.That(replacedProfiles[0].OriginalBytes, Is.EqualTo(first));
            Assert.That(replacedProfiles[1].OriginalBytes,
                Is.EqualTo(replacement));
            Assert.That(remaining, Has.Length.EqualTo(1));
            Assert.That(remaining[0].OriginalBytes, Is.EqualTo(replacement));
            Assert.That(firstRemaining, Has.Length.EqualTo(1));
            Assert.That(firstRemaining[0].OriginalBytes, Is.EqualTo(first));
            Assert.Throws<IccEmbeddingException>(() => writer.RemoveProfileBytes(
                removedFirst, 1, IccEmbeddedFormat.Tiff));
        });
    }

    [TestCase(false, true)]
    [TestCase(true, false)]
    public void DetectsDngAndInsertsProfilesWhilePreservingInlineFields(
        bool bigTiff, bool littleEndian)
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        IIccEmbeddedProfileWriter writer = new IccEmbeddedProfileWriter();
        byte[] original = CreateTiff(bigTiff, littleEndian, true,
            new byte[0][]);
        IccProfile profile = CreateProfile(2037);

        byte[] inserted = writer.InsertProfileBytes(original, profile,
            IccEmbeddedFormat.Dng, new IccWriteOptions());
        IccProfile extracted = reader.ExtractProfilesMemory(inserted,
            new IccReadOptions()).Single();
        byte[] removed = writer.RemoveProfileBytes(inserted, 0,
            IccEmbeddedFormat.Dng);
        byte[] normalizedInsertion = inserted.Take(original.Length).ToArray();
        int headerPointer = bigTiff ? 8 : 4;
        int pointerSize = bigTiff ? 8 : 4;
        System.Array.Copy(original, headerPointer, normalizedInsertion,
            headerPointer, pointerSize);

        Assert.Multiple(() =>
        {
            Assert.That(reader.DetectCarrierBytes(original),
                Is.EqualTo(IccEmbeddedFormat.Dng));
            Assert.That(reader.DetectCarrierMemory(inserted),
                Is.EqualTo(IccEmbeddedFormat.Dng));
            Assert.That(extracted.OriginalBytes, Is.EqualTo(EncodeProfile(profile)));
            Assert.That(normalizedInsertion, Is.EqualTo(original));
            Assert.That(ContainsSequence(inserted,
                new byte[] { 1, 6, 0, 0 }), Is.True);
            Assert.That(ContainsSequence(inserted,
                new byte[] { 0xde, 0xad, 0xbe }), Is.True);
            Assert.That(reader.EnumerateProfilesBytes(removed,
                new IccEmbeddingOptions()).Result.Status,
                Is.EqualTo(IccOperationStatus.NotFound));
            Assert.Throws<IccEmbeddingException>(() => writer.InsertProfileBytes(
                original, profile, bigTiff ? IccEmbeddedFormat.BigTiff
                    : IccEmbeddedFormat.Tiff, new IccWriteOptions()));
            Assert.That(writer.ProbeEmbeddingOperation(IccEmbeddedFormat.Dng,
                IccEmbeddedOperation.Insert).Status,
                Is.EqualTo(IccOperationStatus.Success));
        });
    }

    [Test]
    public void SupportsStreamMutationAndAbsoluteExtractionLocations()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        IIccEmbeddedProfileWriter writer = new IccEmbeddedProfileWriter();
        byte[] original = CreateTiff(false, false, false, new byte[0][]);
        IccProfile profile = CreateProfile(2038);
        using MemoryStream source = new MemoryStream(original);
        using MemoryStream destination = new MemoryStream();
        writer.InsertProfileStream(source, destination, profile,
            IccEmbeddedFormat.Tiff, new IccWriteOptions());
        byte[] inserted = destination.ToArray();
        byte[] prefixed = new byte[11 + inserted.Length];
        System.Array.Copy(inserted, 0, prefixed, 11, inserted.Length);
        using MemoryStream extractionStream = new MemoryStream(prefixed);
        extractionStream.Position = 11;
        IccEmbeddedProfileResult result = reader.EnumerateProfilesStream(
            extractionStream, new IccEmbeddingOptions());
        using MemoryStream removeSource = new MemoryStream(inserted);
        using MemoryStream removeDestination = new MemoryStream();
        writer.RemoveProfileStream(removeSource, removeDestination, 0,
            IccEmbeddedFormat.Tiff);

        Assert.Multiple(() =>
        {
            Assert.That(result.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(result.Profiles.Single().Location.Offset,
                Is.GreaterThan(11));
            Assert.That(result.Profiles.Single().Profile.Location.Offset,
                Is.EqualTo(result.Profiles.Single().Location.Offset));
            Assert.That(reader.EnumerateProfilesBytes(
                removeDestination.ToArray(), new IccEmbeddingOptions())
                .Result.Status, Is.EqualTo(IccOperationStatus.NotFound));
        });
    }

    [Test]
    public void RejectsBadTypesDuplicateTagsUncheckedRangesAndCyclicIfds()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        byte[] profile = EncodeProfile(CreateProfile(2039));
        byte[] valid = CreateTiff(false, true, false,
            new byte[][] { profile });
        int entry = FindTagEntry(valid, false, true, 34675);
        byte[] wrongType = valid.ToArray();
        WriteUnsigned(wrongType, entry + 2, 2, 1, true);
        byte[] duplicate = valid.ToArray();
        int privateEntry = FindTagEntry(duplicate, false, true, 65000);
        WriteUnsigned(duplicate, privateEntry, 2, 34675, true);
        byte[] outside = valid.ToArray();
        WriteUnsigned(outside, entry + 8, 4, uint.MaxValue, true);
        byte[] cycle = valid.ToArray();
        int nextPointer = FindNextPointer(cycle, false, true);
        WriteUnsigned(cycle, nextPointer, 4, 8, true);
        byte[] bigOverflow = CreateTiff(true, false, false,
            new byte[][] { profile });
        int bigEntry = FindTagEntry(bigOverflow, true, false, 34675);
        WriteUnsigned(bigOverflow, bigEntry + 2, 2, 12, false);
        WriteUnsigned(bigOverflow, bigEntry + 4, 8, ulong.MaxValue, false);
        byte[] inline = valid.ToArray();
        WriteUnsigned(inline, entry + 4, 4, 4, true);
        WriteUnsigned(inline, entry + 8, 4, 0, true);

        Assert.Multiple(() =>
        {
            AssertInvalid(reader, wrongType, "UNDEFINED");
            AssertInvalid(reader, duplicate, "duplicate");
            AssertInvalid(reader, outside, "outside");
            AssertInvalid(reader, cycle, "cycle");
            AssertInvalid(reader, bigOverflow, "overflowing");
            AssertInvalid(reader, inline, "profile is invalid");
        });
    }

    [Test]
    public void AppliesProfileAndCarrierResourceLimits()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        IIccEmbeddedProfileWriter writer = new IccEmbeddedProfileWriter();
        IccProfile profile = CreateProfile(2040);
        byte[] encoded = EncodeProfile(profile);
        byte[] carrier = CreateTiff(true, true, false,
            new byte[][] { encoded });
        IccEmbeddingOptions readOptions = new IccEmbeddingOptions();
        readOptions.ResourceLimits.MaximumProfileBytes = encoded.Length - 1;
        IccEmbeddedProfileResult limited = reader.EnumerateProfilesBytes(carrier,
            readOptions);
        byte[] empty = CreateTiff(true, true, false, new byte[0][]);
        IccWriteOptions writeOptions = new IccWriteOptions();
        writeOptions.ResourceLimits.MaximumCarrierBytes = empty.Length;

        Assert.Multiple(() =>
        {
            Assert.That(limited.Result.Status,
                Is.EqualTo(IccOperationStatus.ResourceLimitExceeded));
            Assert.Throws<IccEmbeddingException>(() => writer.InsertProfileBytes(
                empty, profile, IccEmbeddedFormat.BigTiff, writeOptions));
        });
    }

    private static void AssertInvalid(IIccEmbeddedProfileReader reader,
        byte[] carrier, string messagePart)
    {
        IccEmbeddedProfileResult result = reader.EnumerateProfilesBytes(carrier,
            new IccEmbeddingOptions());
        Assert.That(result.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
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

    private static byte[] CreateTiff(bool bigTiff, bool littleEndian,
        bool dng, byte[][] profiles)
    {
        int headerSize = bigTiff ? 16 : 8;
        int countSize = bigTiff ? 8 : 2;
        int entrySize = bigTiff ? 20 : 12;
        int offsetSize = bigTiff ? 8 : 4;
        int alignment = bigTiff ? 8 : 2;
        int directoryCount = profiles.Length == 0 ? 1 : profiles.Length;
        int[] entryCounts = new int[directoryCount];
        int[] directoryOffsets = new int[directoryCount];
        int cursor = headerSize;
        for (int index = 0; index < directoryCount; index++)
        {
            bool hasProfile = index < profiles.Length;
            entryCounts[index] = 2 + (hasProfile ? 1 : 0)
                + (dng && index == 0 ? 1 : 0);
            cursor = Align(cursor, alignment);
            directoryOffsets[index] = cursor;
            cursor += countSize + (entryCounts[index] * entrySize)
                + offsetSize;
        }
        int[] profileOffsets = new int[profiles.Length];
        for (int index = 0; index < profiles.Length; index++)
        {
            cursor = Align(cursor, alignment);
            profileOffsets[index] = cursor;
            cursor += profiles[index].Length;
        }

        byte[] result = new byte[cursor];
        result[0] = littleEndian ? (byte)0x49 : (byte)0x4d;
        result[1] = result[0];
        WriteUnsigned(result, 2, 2, bigTiff ? 43u : 42u, littleEndian);
        if (bigTiff)
        {
            WriteUnsigned(result, 4, 2, 8, littleEndian);
            WriteUnsigned(result, 6, 2, 0, littleEndian);
            WriteUnsigned(result, 8, 8, (ulong)directoryOffsets[0],
                littleEndian);
        }
        else
        {
            WriteUnsigned(result, 4, 4, (ulong)directoryOffsets[0],
                littleEndian);
        }

        for (int directoryIndex = 0; directoryIndex < directoryCount;
            directoryIndex++)
        {
            int directoryOffset = directoryOffsets[directoryIndex];
            WriteUnsigned(result, directoryOffset, countSize,
                (ulong)entryCounts[directoryIndex], littleEndian);
            int entryOffset = directoryOffset + countSize;
            WriteScalarEntry(result, entryOffset, 256, 4, 1, bigTiff,
                littleEndian);
            entryOffset += entrySize;
            if (directoryIndex < profiles.Length)
            {
                WriteOffsetEntry(result, entryOffset, 34675, 7,
                    profiles[directoryIndex].Length,
                    profileOffsets[directoryIndex], bigTiff, littleEndian);
                entryOffset += entrySize;
            }
            if (dng && directoryIndex == 0)
            {
                WriteInlineEntry(result, entryOffset, 50706, 1,
                    new byte[] { 1, 6, 0, 0 }, bigTiff, littleEndian);
                entryOffset += entrySize;
            }
            WriteInlineEntry(result, entryOffset, 65000, 1,
                new byte[] { 0xde, 0xad, 0xbe }, bigTiff, littleEndian);
            int nextPointer = directoryOffset + countSize
                + (entryCounts[directoryIndex] * entrySize);
            ulong next = directoryIndex + 1 < directoryCount
                ? (ulong)directoryOffsets[directoryIndex + 1] : 0;
            WriteUnsigned(result, nextPointer, offsetSize, next, littleEndian);
        }
        for (int index = 0; index < profiles.Length; index++)
        {
            System.Array.Copy(profiles[index], 0, result,
                profileOffsets[index], profiles[index].Length);
        }
        return result;
    }

    private static void WriteScalarEntry(byte[] bytes, int offset, int tag,
        int type, ulong value, bool bigTiff, bool littleEndian)
    {
        int valueOffset = offset + (bigTiff ? 12 : 8);
        WriteUnsigned(bytes, offset, 2, (ulong)tag, littleEndian);
        WriteUnsigned(bytes, offset + 2, 2, (ulong)type, littleEndian);
        WriteUnsigned(bytes, offset + 4, bigTiff ? 8 : 4, 1, littleEndian);
        WriteUnsigned(bytes, valueOffset, 4, value, littleEndian);
    }

    private static void WriteOffsetEntry(byte[] bytes, int offset, int tag,
        int type, int count, int valueOffset, bool bigTiff, bool littleEndian)
    {
        WriteUnsigned(bytes, offset, 2, (ulong)tag, littleEndian);
        WriteUnsigned(bytes, offset + 2, 2, (ulong)type, littleEndian);
        WriteUnsigned(bytes, offset + 4, bigTiff ? 8 : 4, (ulong)count,
            littleEndian);
        WriteUnsigned(bytes, offset + (bigTiff ? 12 : 8), bigTiff ? 8 : 4,
            (ulong)valueOffset, littleEndian);
    }

    private static void WriteInlineEntry(byte[] bytes, int offset, int tag,
        int type, byte[] values, bool bigTiff, bool littleEndian)
    {
        WriteUnsigned(bytes, offset, 2, (ulong)tag, littleEndian);
        WriteUnsigned(bytes, offset + 2, 2, (ulong)type, littleEndian);
        WriteUnsigned(bytes, offset + 4, bigTiff ? 8 : 4,
            (ulong)values.Length, littleEndian);
        System.Array.Copy(values, 0, bytes, offset + (bigTiff ? 12 : 8),
            values.Length);
    }

    private static int FindTagEntry(byte[] bytes, bool bigTiff,
        bool littleEndian, int tag)
    {
        int countSize = bigTiff ? 8 : 2;
        int entrySize = bigTiff ? 20 : 12;
        int directoryOffset = (int)ReadUnsigned(bytes, bigTiff ? 8 : 4,
            bigTiff ? 8 : 4, littleEndian);
        int count = (int)ReadUnsigned(bytes, directoryOffset, countSize,
            littleEndian);
        for (int index = 0; index < count; index++)
        {
            int entryOffset = directoryOffset + countSize + index * entrySize;
            if (ReadUnsigned(bytes, entryOffset, 2, littleEndian)
                == (ulong)tag)
            {
                return entryOffset;
            }
        }
        return -1;
    }

    private static int FindNextPointer(byte[] bytes, bool bigTiff,
        bool littleEndian)
    {
        int countSize = bigTiff ? 8 : 2;
        int entrySize = bigTiff ? 20 : 12;
        int directoryOffset = (int)ReadUnsigned(bytes, bigTiff ? 8 : 4,
            bigTiff ? 8 : 4, littleEndian);
        int count = (int)ReadUnsigned(bytes, directoryOffset, countSize,
            littleEndian);
        return directoryOffset + countSize + count * entrySize;
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

    private static int Align(int value, int alignment)
    {
        int remainder = value % alignment;
        return remainder == 0 ? value : value + alignment - remainder;
    }

    private static ulong ReadUnsigned(byte[] bytes, int offset, int size,
        bool littleEndian)
    {
        ulong value = 0;
        for (int index = 0; index < size; index++)
        {
            int source = littleEndian ? size - index - 1 : index;
            value = (value << 8) | bytes[offset + source];
        }
        return value;
    }

    private static void WriteUnsigned(byte[] bytes, int offset, int size,
        ulong value, bool littleEndian)
    {
        for (int index = 0; index < size; index++)
        {
            int destination = littleEndian ? index : size - index - 1;
            bytes[offset + destination] = (byte)value;
            value >>= 8;
        }
    }
}
