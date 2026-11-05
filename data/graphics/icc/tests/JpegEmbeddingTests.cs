using icc;

namespace icc.Tests;

public class JpegEmbeddingTests
{
    [Test]
    public void InsertsExtractsAndRemovesProfileWhilePreservingEveryOtherByte()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        IIccEmbeddedProfileWriter writer = new IccEmbeddedProfileWriter();
        byte[] original = CreateJpeg();
        IccProfile profile = CreateProfile(2020);
        IccWriteOptions writeOptions = new IccWriteOptions();
        byte[] expectedProfile = new IccProfileWriter().WriteProfileBytes(
            profile, writeOptions);

        byte[] inserted = writer.InsertProfileBytes(original, profile,
            IccEmbeddedFormat.Jpeg, writeOptions);
        IccEmbeddedProfileResult result = reader.EnumerateProfilesBytes(inserted,
            new IccEmbeddingOptions());
        IccProfile extracted = reader.ExtractProfilesBytes(inserted,
            new IccReadOptions()).Single();
        byte[] removed = writer.RemoveProfileBytes(inserted, 0,
            IccEmbeddedFormat.Jpeg);

        Assert.Multiple(() =>
        {
            Assert.That(result.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(result.Options.Format, Is.EqualTo(IccEmbeddedFormat.Jpeg));
            Assert.That(result.Profiles, Has.Length.EqualTo(1));
            Assert.That(result.Profiles[0].Format, Is.EqualTo(IccEmbeddedFormat.Jpeg));
            Assert.That(result.Profiles[0].Index, Is.Zero);
            Assert.That(result.Profiles[0].Parts, Has.Length.EqualTo(1));
            Assert.That(result.Profiles[0].Location.Offset,
                Is.EqualTo(result.Profiles[0].Parts[0].Offset));
            Assert.That(result.Profiles[0].Profile.Location.Offset, Is.EqualTo(-1));
            Assert.That(result.Profiles[0].Profile.Location.Length,
                Is.EqualTo(expectedProfile.Length));
            Assert.That(extracted.OriginalBytes, Is.EqualTo(expectedProfile));
            Assert.That(removed, Is.EqualTo(original));
            Assert.That(writer.ProbeEmbeddingOperation(IccEmbeddedFormat.Jpeg,
                IccEmbeddedOperation.Replace).Status,
                Is.EqualTo(IccOperationStatus.Success));
            Assert.That(writer.ProbeEmbeddingOperation(IccEmbeddedFormat.Png,
                IccEmbeddedOperation.Insert).Status,
                Is.EqualTo(IccOperationStatus.Success));
        });
    }

    [Test]
    public void SegmentsLargeProfilesAndReplacementPreservesMarkersAndScanData()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        IIccEmbeddedProfileWriter writer = new IccEmbeddedProfileWriter();
        byte[] original = CreateJpeg();
        IccProfile large = CreateLargeProfile();
        IccWriteOptions writeOptions = new IccWriteOptions();
        byte[] expectedLarge = new IccProfileWriter().WriteProfileBytes(large,
            writeOptions);

        byte[] inserted = writer.InsertProfileBytes(original, large,
            IccEmbeddedFormat.Jpeg, writeOptions);
        IccEmbeddedProfileResult segmented = reader.EnumerateProfilesBytes(
            inserted, new IccEmbeddingOptions());
        IccProfile extracted = reader.ExtractProfilesBytes(inserted,
            new IccReadOptions()).Single();
        IccProfile replacement = CreateProfile(2021);
        byte[] replaced = writer.ReplaceProfileBytes(inserted, 0, replacement,
            IccEmbeddedFormat.Jpeg, writeOptions);
        byte[] replacementBytes = new IccProfileWriter().WriteProfileBytes(
            replacement, writeOptions);
        IccProfile extractedReplacement = reader.ExtractProfilesBytes(replaced,
            new IccReadOptions()).Single();
        byte[] restored = writer.RemoveProfileBytes(replaced, 0,
            IccEmbeddedFormat.Jpeg);

        Assert.Multiple(() =>
        {
            Assert.That(expectedLarge.Length, Is.GreaterThan(65519));
            Assert.That(segmented.Profiles.Single().Parts, Has.Length.EqualTo(2));
            Assert.That(extracted.OriginalBytes, Is.EqualTo(expectedLarge));
            Assert.That(extractedReplacement.OriginalBytes,
                Is.EqualTo(replacementBytes));
            Assert.That(restored, Is.EqualTo(original));
        });
    }

    [Test]
    public void ValidatesCountsAndReassemblesSegmentsInSequenceNumberOrder()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        IIccEmbeddedProfileWriter writer = new IccEmbeddedProfileWriter();
        byte[] valid = writer.InsertProfileBytes(CreateJpeg(),
            CreateLargeProfile(), IccEmbeddedFormat.Jpeg,
            new IccWriteOptions());
        int[] identifiers = FindIdentifiers(valid);
        IccEmbeddedProfileResult validResult = reader.EnumerateProfilesBytes(
            valid, new IccEmbeddingOptions());
        IccByteLocation firstPart = validResult.Profiles.Single().Parts[0];
        IccByteLocation secondPart = validResult.Profiles.Single().Parts[1];

        byte[] duplicate = valid.ToArray();
        duplicate[identifiers[1] + 12] = 1;
        byte[] outOfOrder = SwapAdjacentRanges(valid, (int)firstPart.Offset,
            (int)firstPart.Length, (int)secondPart.Length);
        byte[] inconsistent = valid.ToArray();
        inconsistent[identifiers[1] + 13] = 3;
        byte[] missing = RemoveRange(valid, (int)secondPart.Offset,
            (int)secondPart.Length);

        IccEmbeddedProfileResult duplicateResult = reader.EnumerateProfilesBytes(
            duplicate, new IccEmbeddingOptions());
        IccEmbeddedProfileResult orderResult = reader.EnumerateProfilesBytes(
            outOfOrder, new IccEmbeddingOptions());
        IccProfile reorderedProfile = reader.ExtractProfilesBytes(outOfOrder,
            new IccReadOptions()).Single();
        IccEmbeddedProfileResult countResult = reader.EnumerateProfilesBytes(
            inconsistent, new IccEmbeddingOptions());
        IccEmbeddedProfileResult missingResult = reader.EnumerateProfilesBytes(
            missing, new IccEmbeddingOptions());
        IccEmbeddingOptions limitedOptions = new IccEmbeddingOptions();
        limitedOptions.ResourceLimits.MaximumProfileBytes = 65519;
        IccEmbeddedProfileResult limitedResult = reader.EnumerateProfilesBytes(
            valid, limitedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(duplicateResult.Result.Status,
                Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(duplicateResult.Result.Message, Does.Contain("duplicate"));
            Assert.That(orderResult.Result.Status,
                Is.EqualTo(IccOperationStatus.Success));
            Assert.That(reorderedProfile.OriginalBytes,
                Is.EqualTo(validResult.Profiles.Single().Profile.OriginalBytes));
            Assert.That(countResult.Result.Status,
                Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(countResult.Result.Message, Does.Contain("inconsistent"));
            Assert.That(missingResult.Result.Status,
                Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(missingResult.Result.Message, Does.Contain("missing"));
            Assert.That(limitedResult.Result.Status,
                Is.EqualTo(IccOperationStatus.ResourceLimitExceeded));
            Assert.Throws<IccEmbeddingException>(() => reader.ExtractProfilesBytes(
                duplicate, new IccReadOptions()));
        });
    }

    [Test]
    public void StreamMutationAndFailureCasesHaveAtomicByteResults()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        IIccEmbeddedProfileWriter writer = new IccEmbeddedProfileWriter();
        byte[] original = CreateJpeg();
        IccProfile profile = CreateProfile(2022);
        IccProfile replacement = CreateProfile(2025);
        byte[] expectedInserted = writer.InsertProfileBytes(original, profile,
            IccEmbeddedFormat.Jpeg, new IccWriteOptions());
        byte[] expectedReplaced = writer.ReplaceProfileBytes(expectedInserted,
            0, replacement, IccEmbeddedFormat.Jpeg, new IccWriteOptions());
        byte[] expectedRemoved = writer.RemoveProfileBytes(expectedReplaced, 0,
            IccEmbeddedFormat.Jpeg);
        using MemoryStream source = new MemoryStream(original);
        using MemoryStream destination = PrefilledStream(
            expectedInserted.Length + 19);
        writer.InsertProfileStream(source, destination, profile,
            IccEmbeddedFormat.Jpeg, new IccWriteOptions());
        byte[] inserted = destination.ToArray();
        using MemoryStream replaceSource = new MemoryStream(inserted);
        using MemoryStream replaceDestination = PrefilledStream(
            expectedReplaced.Length + 23);
        writer.ReplaceProfileStream(replaceSource, replaceDestination, 0,
            replacement, IccEmbeddedFormat.Jpeg, new IccWriteOptions());
        byte[] replaced = replaceDestination.ToArray();
        using MemoryStream removeSource = new MemoryStream(replaced);
        using MemoryStream removeDestination = PrefilledStream(
            expectedRemoved.Length + 29);
        writer.RemoveProfileStream(removeSource, removeDestination, 0,
            IccEmbeddedFormat.Jpeg);

        Assert.Multiple(() =>
        {
            Assert.That(inserted, Is.EqualTo(expectedInserted));
            Assert.That(replaced, Is.EqualTo(expectedReplaced));
            Assert.That(reader.ExtractProfilesBytes(replaced,
                new IccReadOptions()).Single().Header.CreationDateTime.Year,
                Is.EqualTo(2025));
            Assert.That(removeDestination.ToArray(), Is.EqualTo(expectedRemoved));
            Assert.That(expectedRemoved, Is.EqualTo(original));
            Assert.Throws<IccEmbeddingException>(() => writer.InsertProfileBytes(
                inserted, profile, IccEmbeddedFormat.Jpeg,
                new IccWriteOptions()));
            Assert.Throws<IccEmbeddingException>(() => writer.ReplaceProfileBytes(
                original, 0, profile, IccEmbeddedFormat.Jpeg,
                new IccWriteOptions()));
            Assert.Throws<IccEmbeddingException>(() => writer.RemoveProfileBytes(
                inserted, 1, IccEmbeddedFormat.Jpeg));
            Assert.Throws<IccEmbeddingException>(() => writer.InsertProfileBytes(
                original, profile, IccEmbeddedFormat.Png,
                new IccWriteOptions()));
        });
    }

    [Test]
    public void DoesNotRawScanProfileBytesInsideAnUnrelatedJpegSegment()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        byte[] profileBytes = new IccProfileWriter().WriteProfileBytes(
            CreateProfile(2024), new IccWriteOptions());
        byte[] jpeg = CreateJpeg();
        byte[] carrier = Join(new byte[] { 0xff, 0xd8 },
            Segment(0xe1, profileBytes), jpeg.Skip(2).ToArray());

        IccEmbeddedProfileResult result = reader.EnumerateProfilesMemory(
            carrier, new IccEmbeddingOptions());
        IccEmbeddingException error = Assert.Throws<IccEmbeddingException>(
            () => reader.ExtractProfilesMemory(carrier,
                new IccReadOptions()))!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Result.Status,
                Is.EqualTo(IccOperationStatus.NotFound));
            Assert.That(result.Profiles, Is.Empty);
            Assert.That(result.Result.Message, Does.Contain("APP2"));
            Assert.That(error.Message, Does.Contain("APP2"));
        });
    }

    private static IccProfile CreateProfile(int year)
    {
        IccProfileBuildOptions options = new IccProfileBuildOptions();
        options.ProfileClass = IccProfileClass.ColorSpace;
        options.DataColorSpaceSignature = IccColorSpaceSignatures.Xyz;
        options.CreationDateTime.Year = year;
        return new IccProfileBuilder().CreateXyzIdentityProfile(options);
    }

    private static IccProfile CreateLargeProfile()
    {
        IIccProfileBuilder builder = new IccProfileBuilder();
        IccProfile profile = CreateProfile(2023);
        byte[] payload = new byte[70008];
        payload[0] = 0x70;
        payload[1] = 0x72;
        payload[2] = 0x76;
        payload[3] = 0x54;
        IccTag tag = new IccTag();
        tag.Signature = 0x70727631;
        tag.TableEntry.Signature = tag.Signature;
        tag.Data.Kind = IccTagDataKind.Raw;
        tag.Data.RawData.HasTypeSignature = true;
        tag.Data.RawData.TypeSignature = 0x70727654;
        tag.Data.RawData.Bytes = payload;
        return builder.SetProfileTag(profile, tag);
    }

    private static byte[] CreateJpeg()
    {
        byte[] soi = new byte[] { 0xff, 0xd8 };
        byte[] app0 = Segment(0xe0, new byte[] { 0x4a, 0x46, 0x49, 0x46 });
        byte[] unrelatedApp2 = Segment(0xe2,
            new byte[] { 0x4e, 0x4f, 0x54, 0x5f, 0x49, 0x43, 0x43 });
        byte[] comment = Segment(0xfe, new byte[] { 1, 2, 3 });
        byte[] scanHeader = Segment(0xda,
            new byte[] { 1, 1, 0, 0, 0, 0 });
        byte[] scanData = new byte[]
        {
            0x11, 0xff, 0x00, 0x22, 0xff, 0xd0, 0x33
        };
        byte[] eoiAndTrailing = new byte[] { 0xff, 0xd9, 0x55, 0x66 };
        return Join(soi, app0, unrelatedApp2, comment, scanHeader, scanData,
            eoiAndTrailing);
    }

    private static MemoryStream PrefilledStream(int length)
    {
        MemoryStream stream = new MemoryStream();
        stream.Write(Enumerable.Repeat((byte)0xa5, length).ToArray());
        stream.Position = 0;
        return stream;
    }

    private static byte[] Segment(byte marker, byte[] data)
    {
        byte[] segment = new byte[data.Length + 4];
        segment[0] = 0xff;
        segment[1] = marker;
        int length = data.Length + 2;
        segment[2] = (byte)(length >> 8);
        segment[3] = (byte)length;
        System.Array.Copy(data, 0, segment, 4, data.Length);
        return segment;
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

    private static int[] FindIdentifiers(byte[] bytes)
    {
        byte[] identifier = System.Text.Encoding.ASCII.GetBytes("ICC_PROFILE\0");
        List<int> offsets = new List<int>();
        for (int index = 0; index <= bytes.Length - identifier.Length; index++)
        {
            if (bytes.AsSpan(index, identifier.Length).SequenceEqual(identifier))
            {
                offsets.Add(index);
            }
        }
        return offsets.ToArray();
    }

    private static byte[] RemoveRange(byte[] source, int offset, int length)
    {
        byte[] result = new byte[source.Length - length];
        System.Array.Copy(source, 0, result, 0, offset);
        System.Array.Copy(source, offset + length, result, offset,
            source.Length - offset - length);
        return result;
    }

    private static byte[] SwapAdjacentRanges(byte[] source, int offset,
        int firstLength, int secondLength)
    {
        byte[] result = source.ToArray();
        System.Array.Copy(source, offset + firstLength, result, offset,
            secondLength);
        System.Array.Copy(source, offset, result, offset + secondLength,
            firstLength);
        return result;
    }
}
