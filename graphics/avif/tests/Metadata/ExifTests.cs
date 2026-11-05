namespace avif.Tests;

public class ExifTests
{
    [TestCase(true)]
    [TestCase(false)]
    public void ParsesEndianAwareIfdsOrientationAndUnknownTags(
        bool littleEndian)
    {
        byte[] data = BuildExif(littleEndian, true);
        AvifExif exif = AvifExif.Create(data);
        int unknown = FindTag(exif, 51105);
        int comment = FindTag(exif, 37510);

        Assert.Multiple(() =>
        {
            Assert.That(exif.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(exif.GetTiffHeaderOffset(), Is.EqualTo(10));
            Assert.That(exif.IsLittleEndian(), Is.EqualTo(littleEndian));
            Assert.That(exif.GetIfdCount(), Is.EqualTo(3));
            Assert.That(exif.GetIfdDepth(0), Is.Zero);
            Assert.That(exif.GetIfdDepth(1), Is.EqualTo(1));
            Assert.That(exif.GetIfdDepth(2), Is.Zero);
            Assert.That(exif.GetTagCount(), Is.EqualTo(4));
            Assert.That(exif.GetOrientation(), Is.EqualTo(6));
            Assert.That(exif.GetTagType(unknown), Is.EqualTo(7));
            Assert.That(exif.GetTagValueCount(unknown), Is.EqualTo(6));
            Assert.That(exif.GetTagIfdIndex(unknown), Is.Zero);
            Assert.That(exif.GetTagData(unknown),
                Is.EqualTo(new byte[] { 9, 8, 7, 6, 5, 4 }));
            Assert.That(exif.GetTagData(comment),
                Is.EqualTo(new byte[] { 104, 101, 108, 108, 111, 0 }));
            Assert.That(exif.GetData(), Is.EqualTo(data));
            Assert.That(exif.GetTagIdentifier(-1), Is.Zero);
            Assert.That(exif.GetTagIfdIndex(99), Is.EqualTo(-1));
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public void UpdatesExistingOrientationWithoutChangingOtherBytes(
        bool littleEndian)
    {
        byte[] original = BuildExif(littleEndian, true);
        AvifExif exif = AvifExif.Create(original);
        int unknown = FindTag(exif, 51105);
        byte[] unknownBefore = exif.GetTagData(unknown);

        AvifResultCode result = exif.SetOrientation(8);
        byte[] updated = exif.GetData();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(AvifResultCode.Success));
            Assert.That(exif.GetOrientation(), Is.EqualTo(8));
            Assert.That(updated.Length, Is.EqualTo(original.Length));
            Assert.That(exif.GetTagData(FindTag(exif, 51105)),
                Is.EqualTo(unknownBefore));
            Assert.That(CountDifferences(original, updated), Is.EqualTo(1));
            Assert.That(exif.SetOrientation(0),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(exif.SetOrientation(9),
                Is.EqualTo(AvifResultCode.InvalidArgument));
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public void AddsOrientationByAppendingReplacementRootIfd(
        bool littleEndian)
    {
        byte[] original = BuildExif(littleEndian, false);
        AvifExif exif = AvifExif.Create(original);
        byte[] unknownBefore = exif.GetTagData(FindTag(exif, 51105));

        AvifResultCode result = exif.SetOrientation(5);
        int firstLength = exif.GetData().Length;
        AvifResultCode second = exif.SetOrientation(2);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(AvifResultCode.Success));
            Assert.That(second, Is.EqualTo(AvifResultCode.Success));
            Assert.That(exif.GetOrientation(), Is.EqualTo(2));
            Assert.That(exif.GetData().Length, Is.EqualTo(firstLength));
            Assert.That(firstLength, Is.GreaterThan(original.Length));
            Assert.That(exif.GetTagCount(), Is.EqualTo(4));
            Assert.That(exif.GetTagData(FindTag(exif, 51105)),
                Is.EqualTo(unknownBefore));
            Assert.That(exif.GetData().AsSpan(4, 6).ToArray(),
                Is.EqualTo(original.AsSpan(4, 6).ToArray()));
        });
    }

    [Test]
    public void AddsOrientationToTiffWithoutAnInitialIfd()
    {
        byte[] data = BuildEmptyExif();
        AvifExif exif = AvifExif.Create(data);

        AvifResultCode result = exif.SetOrientation(3);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(AvifResultCode.Success));
            Assert.That(exif.GetIfdCount(), Is.EqualTo(1));
            Assert.That(exif.GetTagCount(), Is.EqualTo(1));
            Assert.That(exif.GetOrientation(), Is.EqualTo(3));
        });
    }

    [Test]
    public void SharedIfdReferencesAreAcceptedButCyclesAreRejected()
    {
        byte[] shared = BuildSharedIfdExif();
        byte[] cycle = BuildExif(true, true);
        WriteUInt32(cycle, 52, 8, true);

        AvifExif sharedExif = AvifExif.Create(shared);
        AvifExif cyclicExif = AvifExif.Create(cycle);

        Assert.Multiple(() =>
        {
            Assert.That(sharedExif.GetCode(),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(sharedExif.GetIfdCount(), Is.EqualTo(2));
            Assert.That(cyclicExif.GetCode(),
                Is.EqualTo(AvifResultCode.InvalidMetadata));
        });
    }

    [Test]
    public void InvalidExifStateExposesNoPartialMetadataAndCannotBeMutated()
    {
        AvifExif exif = AvifExif.Create(new byte[11]);

        Assert.Multiple(() =>
        {
            Assert.That(exif.GetCode(),
                Is.EqualTo(AvifResultCode.InvalidMetadata));
            Assert.That(exif.GetData(), Is.Empty);
            Assert.That(exif.GetTiffHeaderOffset(), Is.Zero);
            Assert.That(exif.IsLittleEndian(), Is.False);
            Assert.That(exif.GetIfdCount(), Is.Zero);
            Assert.That(exif.GetIfdDepth(0), Is.EqualTo(-1));
            Assert.That(exif.GetTagCount(), Is.Zero);
            Assert.That(exif.GetTagIdentifier(0), Is.Zero);
            Assert.That(exif.GetTagType(0), Is.Zero);
            Assert.That(exif.GetTagValueCount(0), Is.Zero);
            Assert.That(exif.GetTagIfdIndex(0), Is.EqualTo(-1));
            Assert.That(exif.GetTagData(0), Is.Empty);
            Assert.That(exif.GetOrientation(), Is.Zero);
            Assert.That(exif.SetOrientation(1),
                Is.EqualTo(AvifResultCode.InvalidMetadata));
        });
    }

    [Test]
    public void RejectsMalformedHeaderIfdTypesValuesAndOrientation()
    {
        byte[] badDisplacement = BuildExif(true, true);
        WriteUInt32BigEndian(badDisplacement, 0, uint.MaxValue);
        byte[] badByteOrder = BuildExif(true, true);
        badByteOrder[10] = 0;
        byte[] badMagic = BuildExif(true, true);
        WriteUInt16(badMagic, 12, 43, true);
        byte[] badFirstIfd = BuildExif(true, true);
        WriteUInt32(badFirstIfd, 14, 4, true);
        byte[] truncatedIfd = BuildExif(true, true);
        WriteUInt16(truncatedIfd, 18, ushort.MaxValue, true);
        byte[] badType = BuildExif(true, true);
        WriteUInt16(badType, 22, 0, true);
        byte[] badCount = BuildExif(true, true);
        WriteUInt32(badCount, 24, 0, true);
        byte[] badValueOffset = BuildExif(true, true);
        WriteUInt32(badValueOffset, 40, uint.MaxValue, true);
        byte[] unterminatedAscii = BuildExif(true, true);
        unterminatedAscii[101] = 1;
        byte[] badOrientation = BuildExif(true, true);
        WriteUInt16(badOrientation, 28, 0, true);
        byte[] zeroDenominator = BuildRationalExif(0);

        Assert.Multiple(() =>
        {
            AssertInvalid(badDisplacement);
            AssertInvalid(badByteOrder);
            AssertInvalid(badMagic);
            AssertInvalid(badFirstIfd);
            AssertInvalid(truncatedIfd);
            AssertInvalid(badType);
            AssertInvalid(badCount);
            AssertInvalid(badValueOffset);
            AssertInvalid(unterminatedAscii);
            AssertInvalid(badOrientation);
            AssertInvalid(zeroDenominator);
            AssertInvalid(new byte[11]);
        });
    }

    [Test]
    public void EnforcesExifMetadataTemporaryAndRecursionLimits()
    {
        byte[] data = BuildExif(true, true);
        AvifSecurityLimits exifLimit = new AvifSecurityLimits();
        exifLimit.MaxExifBytes = data.Length - 1;
        AvifSecurityLimits metadataLimit = new AvifSecurityLimits();
        metadataLimit.MaxMetadataBytes = data.Length - 1;
        AvifSecurityLimits temporaryLimit = new AvifSecurityLimits();
        temporaryLimit.MaxTemporaryBytes = 1;
        AvifSecurityLimits recursionLimit = new AvifSecurityLimits();
        recursionLimit.MaxRecursionDepth = 1;

        Assert.Multiple(() =>
        {
            Assert.That(AvifExif.Create(data, exifLimit).GetCode(),
                Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(AvifExif.Create(data, metadataLimit).GetCode(),
                Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(AvifExif.Create(data, temporaryLimit).GetCode(),
                Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(AvifExif.Create(data, recursionLimit).GetCode(),
                Is.EqualTo(AvifResultCode.LimitExceeded));
        });
    }

    [Test]
    public void NextIfdChainsHonorRecursionLimitsWithoutChangingIfdDepths()
    {
        byte[] data = BuildNextIfdChain(4);
        AvifSecurityLimits limited = new AvifSecurityLimits();
        limited.MaxRecursionDepth = 3;
        AvifSecurityLimits allowed = new AvifSecurityLimits();
        allowed.MaxRecursionDepth = 4;

        AvifExif rejected = AvifExif.Create(data, limited);
        AvifExif parsed = AvifExif.Create(data, allowed);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.GetCode(),
                Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(parsed.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed.GetIfdCount(), Is.EqualTo(4));
            Assert.That(parsed.GetIfdDepth(0), Is.Zero);
            Assert.That(parsed.GetIfdDepth(1), Is.Zero);
            Assert.That(parsed.GetIfdDepth(2), Is.Zero);
            Assert.That(parsed.GetIfdDepth(3), Is.Zero);
        });
    }

    [Test]
    public void OrientationAppendHonorsRetainedMetadataLimits()
    {
        byte[] data = BuildExif(true, false);
        AvifSecurityLimits limits = new AvifSecurityLimits();
        limits.MaxExifBytes = data.Length;
        AvifExif exif = AvifExif.Create(data, limits);

        Assert.Multiple(() =>
        {
            Assert.That(exif.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(exif.SetOrientation(1),
                Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(exif.GetOrientation(), Is.Zero);
            Assert.That(exif.GetData(), Is.EqualTo(data));
        });
    }

    static byte[] BuildExif(bool littleEndian, bool orientation)
    {
        byte[] data = new byte[116];
        WriteUInt32BigEndian(data, 0, 6);
        new byte[] { 69, 120, 105, 102, 0, 0 }.CopyTo(data, 4);
        int tiff = 10;
        data[tiff] = littleEndian ? (byte)73 : (byte)77;
        data[tiff + 1] = data[tiff];
        WriteUInt16(data, tiff + 2, 42, littleEndian);
        WriteUInt32(data, tiff + 4, 8, littleEndian);
        int root = tiff + 8;
        int count = orientation ? 3 : 2;
        WriteUInt16(data, root, (ushort)count, littleEndian);
        int entry = root + 2;
        if (orientation)
        {
            WriteEntry(data, entry, 274, 3, 1, 6, littleEndian);
            entry += 12;
        }
        WriteEntry(data, entry, 51105, 7, 6, 80, littleEndian);
        entry += 12;
        WriteEntry(data, entry, 34665, 4, 1, 50, littleEndian);
        WriteUInt32(data, root + 2 + count * 12, 100, littleEndian);

        int child = tiff + 50;
        WriteUInt16(data, child, 1, littleEndian);
        WriteEntry(data, child + 2, 37510, 2, 6, 86, littleEndian);
        WriteUInt32(data, child + 14, 0, littleEndian);
        new byte[] { 9, 8, 7, 6, 5, 4 }.CopyTo(data, tiff + 80);
        new byte[] { 104, 101, 108, 108, 111, 0 }.CopyTo(data, tiff + 86);
        int next = tiff + 100;
        WriteUInt16(data, next, 0, littleEndian);
        WriteUInt32(data, next + 2, 0, littleEndian);
        return data;
    }

    static byte[] BuildEmptyExif()
    {
        byte[] data = new byte[18];
        WriteUInt32BigEndian(data, 0, 0);
        data[4] = 73;
        data[5] = 73;
        WriteUInt16(data, 6, 42, true);
        WriteUInt32(data, 8, 0, true);
        return data;
    }

    static byte[] BuildNextIfdChain(int count)
    {
        byte[] data = new byte[12 + count * 6];
        WriteUInt32BigEndian(data, 0, 0);
        data[4] = 73;
        data[5] = 73;
        WriteUInt16(data, 6, 42, true);
        WriteUInt32(data, 8, 8, true);
        int index = 0;
        while (index < count)
        {
            int relativeOffset = 8 + index * 6;
            int absoluteOffset = 4 + relativeOffset;
            WriteUInt16(data, absoluteOffset, 0, true);
            uint nextOffset = index + 1 < count
                ? (uint)(relativeOffset + 6) : 0;
            WriteUInt32(data, absoluteOffset + 2, nextOffset, true);
            index++;
        }
        return data;
    }

    static byte[] BuildSharedIfdExif()
    {
        byte[] data = new byte[80];
        WriteUInt32BigEndian(data, 0, 0);
        data[4] = 73;
        data[5] = 73;
        WriteUInt16(data, 6, 42, true);
        WriteUInt32(data, 8, 8, true);
        WriteUInt16(data, 12, 2, true);
        WriteEntry(data, 14, 34665, 4, 1, 40, true);
        WriteEntry(data, 26, 34853, 4, 1, 40, true);
        WriteUInt32(data, 38, 0, true);
        WriteUInt16(data, 44, 0, true);
        WriteUInt32(data, 46, 0, true);
        return data;
    }

    static byte[] BuildRationalExif(uint denominator)
    {
        byte[] data = new byte[50];
        WriteUInt32BigEndian(data, 0, 0);
        data[4] = 73;
        data[5] = 73;
        WriteUInt16(data, 6, 42, true);
        WriteUInt32(data, 8, 8, true);
        WriteUInt16(data, 12, 1, true);
        WriteEntry(data, 14, 282, 5, 1, 30, true);
        WriteUInt32(data, 26, 0, true);
        WriteUInt32(data, 34, 72, true);
        WriteUInt32(data, 38, denominator, true);
        return data;
    }

    static void WriteEntry(
        byte[] data,
        int offset,
        ushort identifier,
        ushort type,
        uint count,
        uint value,
        bool littleEndian)
    {
        WriteUInt16(data, offset, identifier, littleEndian);
        WriteUInt16(data, offset + 2, type, littleEndian);
        WriteUInt32(data, offset + 4, count, littleEndian);
        if (type == 3 && count == 1)
        {
            WriteUInt16(data, offset + 8, (ushort)value, littleEndian);
        }
        else
        {
            WriteUInt32(data, offset + 8, value, littleEndian);
        }
    }

    static int FindTag(AvifExif exif, int identifier)
    {
        for (int index = 0; index < exif.GetTagCount(); index++)
        {
            if (exif.GetTagIdentifier(index) == identifier)
            {
                return index;
            }
        }
        return -1;
    }

    static int CountDifferences(byte[] first, byte[] second)
    {
        int count = 0;
        for (int index = 0; index < first.Length; index++)
        {
            if (first[index] != second[index])
            {
                count++;
            }
        }
        return count;
    }

    static void AssertInvalid(byte[] data)
    {
        Assert.That(AvifExif.Create(data).GetCode(),
            Is.EqualTo(AvifResultCode.InvalidMetadata));
    }

    static void WriteUInt16(
        byte[] data,
        int offset,
        ushort value,
        bool littleEndian)
    {
        if (littleEndian)
        {
            data[offset] = (byte)value;
            data[offset + 1] = (byte)(value >> 8);
        }
        else
        {
            data[offset] = (byte)(value >> 8);
            data[offset + 1] = (byte)value;
        }
    }

    static void WriteUInt32(
        byte[] data,
        int offset,
        uint value,
        bool littleEndian)
    {
        if (littleEndian)
        {
            data[offset] = (byte)value;
            data[offset + 1] = (byte)(value >> 8);
            data[offset + 2] = (byte)(value >> 16);
            data[offset + 3] = (byte)(value >> 24);
        }
        else
        {
            WriteUInt32BigEndian(data, offset, value);
        }
    }

    static void WriteUInt32BigEndian(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)(value >> 24);
        data[offset + 1] = (byte)(value >> 16);
        data[offset + 2] = (byte)(value >> 8);
        data[offset + 3] = (byte)value;
    }
}
