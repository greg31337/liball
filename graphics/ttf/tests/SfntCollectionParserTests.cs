using System.Buffers.Binary;
using ttf;

namespace ttf.Tests;

public class SfntCollectionParserTests
{
    [Test]
    public void ParsesVersionOneFacesAndAbsoluteDirectoryOffsets()
    {
        byte[] bytes = BuildVersionOneCollection(true);

        SfntCollection collection = Parse(bytes);
        SfntCollectionFace first = collection.GetFace(0);
        SfntCollectionFace second = collection.GetFace(1);

        Assert.Multiple(() =>
        {
            Assert.That(collection.GetVersion(), Is.EqualTo(0x00010000U));
            Assert.That(collection.GetFaceCount(), Is.EqualTo(2));
            Assert.That(first.GetDirectoryOffset(), Is.EqualTo(20));
            Assert.That(first.GetDirectory().GetDirectoryOffset(), Is.EqualTo(20));
            Assert.That(first.GetDirectory().GetScalerType(), Is.EqualTo(0x00010000U));
            Assert.That(second.GetDirectoryOffset(), Is.EqualTo(48));
            Assert.That(second.GetDirectory().GetDirectoryOffset(), Is.EqualTo(48));
            Assert.That(second.GetDirectory().GetScalerType(), Is.EqualTo(0x4F54544FU));
            Assert.That(first.GetDirectory().GetTableRecord(0).GetOffset(), Is.EqualTo(76));
            Assert.That(second.GetDirectory().GetTableRecord(0).GetOffset(), Is.EqualTo(76));
        });
    }

    [Test]
    public void CanonicalizesTableRegionSharedAcrossFaces()
    {
        SfntCollection collection = Parse(BuildVersionOneCollection(true));
        SfntCollectionTableRegion first = collection.GetFace(0).GetTableRegion(0);
        SfntCollectionTableRegion second = collection.GetFace(1).GetTableRegion(0);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.SameAs(second));
            Assert.That(collection.GetTableRegionCount(), Is.EqualTo(1));
            Assert.That(collection.GetSharedTableRegionCount(), Is.EqualTo(1));
            Assert.That(collection.GetSharedTableRegion(0), Is.SameAs(first));
            Assert.That(first.GetFaceReferenceCount(), Is.EqualTo(2));
            Assert.That(first.GetOffset(), Is.EqualTo(76));
            Assert.That(first.GetLength(), Is.EqualTo(4));
            Assert.That(first.CopyData(), Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(first.GetDataSlice().GetAbsoluteOffset(), Is.EqualTo(76));
        });
    }

    [Test]
    public void KeepsUnsharedRegionsDistinctAndOrderedByOffset()
    {
        SfntCollection collection = Parse(BuildVersionOneCollection(false));

        Assert.Multiple(() =>
        {
            Assert.That(collection.GetTableRegionCount(), Is.EqualTo(2));
            Assert.That(collection.GetSharedTableRegionCount(), Is.EqualTo(0));
            Assert.That(collection.GetTableRegion(0).GetOffset(), Is.EqualTo(76));
            Assert.That(collection.GetTableRegion(1).GetOffset(), Is.EqualTo(80));
            Assert.That(collection.GetFace(0).GetTableRegion(0), Is.Not.SameAs(
                collection.GetFace(1).GetTableRegion(0)));
            Assert.That(collection.GetTableRegion(0).CopyData(), Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(collection.GetTableRegion(1).CopyData(), Is.EqualTo(new byte[] { 5, 6, 7, 8 }));
        });
    }

    [Test]
    public void VersionOneHasNoCollectionDsigMetadata()
    {
        SfntCollectionDsig dsig = Parse(BuildVersionOneCollection(true)).GetDsig();

        Assert.Multiple(() =>
        {
            Assert.That(dsig.IsPresent(), Is.False);
            Assert.That(dsig.GetTag(), Is.EqualTo(0));
            Assert.That(dsig.GetOffset(), Is.EqualTo(0));
            Assert.That(dsig.GetLength(), Is.EqualTo(0));
            Assert.Throws<InvalidOperationException>(() => dsig.CopyData());
        });
    }

    [Test]
    public void ParsesVersionTwoDsigMetadataAndData()
    {
        SfntCollection collection = Parse(BuildVersionTwoCollection(true));
        SfntCollectionDsig dsig = collection.GetDsig();

        Assert.Multiple(() =>
        {
            Assert.That(collection.GetVersion(), Is.EqualTo(0x00020000U));
            Assert.That(collection.GetFaceCount(), Is.EqualTo(1));
            Assert.That(collection.GetFace(0).GetDirectoryOffset(), Is.EqualTo(28));
            Assert.That(dsig.IsPresent(), Is.True);
            Assert.That(dsig.GetTag(), Is.EqualTo((uint)FontTags.Dsig));
            Assert.That(dsig.GetOffset(), Is.EqualTo(60));
            Assert.That(dsig.GetLength(), Is.EqualTo(5));
            Assert.That(dsig.CopyData(), Is.EqualTo(new byte[] { 9, 8, 7, 6, 5 }));
        });
    }

    [Test]
    public void ParsesVersionTwoNullDsigMetadata()
    {
        SfntCollectionDsig dsig = Parse(BuildVersionTwoCollection(false)).GetDsig();

        Assert.Multiple(() =>
        {
            Assert.That(dsig.IsPresent(), Is.False);
            Assert.That(dsig.GetTag(), Is.EqualTo(0));
            Assert.That(dsig.GetLength(), Is.EqualTo(0));
        });
    }

    [Test]
    public void SharedRegionCopiesAreIndependent()
    {
        SfntCollectionTableRegion region = Parse(
            BuildVersionOneCollection(true)).GetSharedTableRegion(0);
        byte[] first = region.CopyData();
        byte[] second = region.CopyData();
        first[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(first, Is.Not.SameAs(second));
        });
    }

    [TestCase(0x00000000U)]
    [TestCase(0x00010001U)]
    [TestCase(0x00030000U)]
    public void RejectsUnsupportedCollectionVersions(uint version)
    {
        byte[] bytes = BuildVersionOneCollection(true);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(4, 4), version);

        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    [Test]
    public void RejectsMissingCollectionSignature()
    {
        byte[] bytes = BuildVersionOneCollection(true);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(0, 4), 0x00010000U);

        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    [Test]
    public void RejectsZeroFaceCount()
    {
        byte[] bytes = BuildVersionOneCollection(true);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(8, 4), 0);

        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    [Test]
    public void RejectsFaceCountAboveConfiguredLimitBeforeOffsetAllocation()
    {
        byte[] bytes = new byte[12];
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(0, 4), 0x74746366U);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(4, 4), 0x00010000U);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(8, 4), 257);

        Assert.Throws<FontLimitException>(() => Parse(bytes, FontLoadOptions.CreateStrict()));
    }

    [Test]
    public void RejectsFaceTableCountAboveConfiguredLimit()
    {
        byte[] bytes = new byte[28];
        WriteCollectionHeader(bytes, 0x00010000U, new int[] { 16 });
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(16, 4), 0x00010000U);
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(20, 2), 257);

        Assert.Throws<FontLimitException>(() => Parse(bytes, FontLoadOptions.CreateStrict()));
    }

    [Test]
    public void RejectsEveryTruncationOfVersionOneHeader()
    {
        byte[] complete = BuildVersionOneCollection(true);

        for (int length = 0; length < 20; length += 1)
        {
            byte[] truncated = complete.AsSpan(0, length).ToArray();
            Assert.Throws<FontFormatException>(() => Parse(truncated), "Length {0}", length);
        }
    }

    [Test]
    public void RejectsTruncatedVersionTwoDsigFields()
    {
        byte[] complete = BuildVersionTwoCollection(true);

        for (int length = 16; length < 28; length += 1)
        {
            byte[] truncated = complete.AsSpan(0, length).ToArray();
            Assert.Throws<FontFormatException>(() => Parse(truncated), "Length {0}", length);
        }
    }

    [TestCase(16U)]
    [TestCase(21U)]
    [TestCase(4294967295U)]
    public void RejectsFaceOffsetsInsideHeaderUnalignedOrOutsideData(uint faceOffset)
    {
        byte[] bytes = BuildVersionOneCollection(true);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(12, 4), faceOffset);

        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    [Test]
    public void RejectsDuplicateFaceOffsets()
    {
        byte[] bytes = BuildVersionOneCollection(true);
        uint firstOffset = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(12, 4));
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(16, 4), firstOffset);

        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    [Test]
    public void RejectsUnsupportedFaceScalerType()
    {
        byte[] bytes = BuildVersionOneCollection(true);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(20, 4), 0xDEADBEEFU);

        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    [Test]
    public void RejectsTableRegionOutsideCollectionData()
    {
        byte[] bytes = BuildVersionOneCollection(true);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(20 + 20, 4), uint.MaxValue);

        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    [Test]
    public void RejectsConflictingLengthsForSharedOffset()
    {
        byte[] bytes = BuildVersionOneCollection(true);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(48 + 24, 4), 3);

        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    [Test]
    public void RejectsPartiallyOverlappingTablesAcrossFaces()
    {
        byte[] source = BuildVersionOneCollection(false);
        byte[] bytes = new byte[88];
        source.CopyTo(bytes, 0);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(20 + 24, 4), 8);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(48 + 24, 4), 8);

        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    [Test]
    public void RejectsTableOverlappingAnotherFaceDirectory()
    {
        byte[] bytes = BuildVersionOneCollection(false);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(20 + 20, 4), 48);

        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    [Test]
    public void RejectsOverlappingFaceDirectories()
    {
        byte[] bytes = BuildVersionOneCollection(true);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(16, 4), 24);
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(26, 2), 0);

        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    [Test]
    public void RejectsDsigOverlappingTableData()
    {
        byte[] bytes = BuildVersionTwoCollection(true);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(24, 4), 58);

        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    [Test]
    public void RejectsMalformedOrOutOfBoundsVersionTwoDsigMetadata()
    {
        byte[] wrongTag = BuildVersionTwoCollection(true);
        BinaryPrimitives.WriteUInt32BigEndian(wrongTag.AsSpan(16, 4), 0x42414420U);
        byte[] zeroLength = BuildVersionTwoCollection(true);
        BinaryPrimitives.WriteUInt32BigEndian(zeroLength.AsSpan(20, 4), 0);
        byte[] outOfBounds = BuildVersionTwoCollection(true);
        BinaryPrimitives.WriteUInt32BigEndian(outOfBounds.AsSpan(24, 4), uint.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(wrongTag));
            Assert.Throws<FontFormatException>(() => Parse(zeroLength));
            Assert.Throws<FontFormatException>(() => Parse(outOfBounds));
        });
    }

    [Test]
    public void IndexAccessRejectsValuesOutsideCollectionModels()
    {
        SfntCollection collection = Parse(BuildVersionOneCollection(true));
        SfntCollectionFace face = collection.GetFace(0);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => collection.GetFace(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => collection.GetFace(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => collection.GetTableRegion(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => collection.GetTableRegion(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => collection.GetSharedTableRegion(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => collection.GetSharedTableRegion(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => face.GetTableRegion(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => face.GetTableRegion(1));
        });
    }

    [Test]
    public void ParserRejectsNullArguments()
    {
        FontData data = FontData.Create(BuildVersionOneCollection(true));

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => SfntCollectionParser.Parse(
                null!,
                FontLoadOptions.CreateCompatible()));
            Assert.Throws<ArgumentNullException>(() => SfntCollectionParser.Parse(data, null!));
        });
    }

    [Test]
    public void ParserObservesCancellationBeforeReadingHeader()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();
        FontLoadOptions options = FontLoadOptions.CreateCompatible(source.Token);

        Assert.Throws<OperationCanceledException>(() => Parse(
            BuildVersionOneCollection(true),
            options));
    }

    private static SfntCollection Parse(byte[] bytes)
    {
        return Parse(bytes, FontLoadOptions.CreateCompatible());
    }

    private static SfntCollection Parse(byte[] bytes, FontLoadOptions options)
    {
        return SfntCollectionParser.Parse(FontData.Create(bytes), options);
    }

    private static byte[] BuildVersionOneCollection(bool sharedTable)
    {
        int firstFaceOffset = 20;
        int secondFaceOffset = 48;
        int firstTableOffset = 76;
        int secondTableOffset = sharedTable ? firstTableOffset : 80;
        int length = sharedTable ? 80 : 84;
        byte[] bytes = new byte[length];
        WriteCollectionHeader(bytes, 0x00010000U, new int[] { firstFaceOffset, secondFaceOffset });
        WriteFaceDirectory(bytes, firstFaceOffset, 0x00010000U, firstTableOffset, new byte[] { 1, 2, 3, 4 });
        WriteFaceDirectory(
            bytes,
            secondFaceOffset,
            0x4F54544FU,
            secondTableOffset,
            sharedTable ? new byte[] { 1, 2, 3, 4 } : new byte[] { 5, 6, 7, 8 });
        new byte[] { 1, 2, 3, 4 }.CopyTo(bytes, firstTableOffset);

        if (!sharedTable)
        {
            new byte[] { 5, 6, 7, 8 }.CopyTo(bytes, secondTableOffset);
        }

        return bytes;
    }

    private static byte[] BuildVersionTwoCollection(bool includeDsig)
    {
        int faceOffset = 28;
        int tableOffset = 56;
        int dsigOffset = 60;
        byte[] dsigData = new byte[] { 9, 8, 7, 6, 5 };
        byte[] bytes = new byte[includeDsig ? 65 : 60];
        WriteCollectionHeader(bytes, 0x00020000U, new int[] { faceOffset });

        if (includeDsig)
        {
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(16, 4), (uint)FontTags.Dsig);
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(20, 4), (uint)dsigData.Length);
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(24, 4), (uint)dsigOffset);
            dsigData.CopyTo(bytes, dsigOffset);
        }

        WriteFaceDirectory(bytes, faceOffset, 0x00010000U, tableOffset, new byte[] { 1, 2, 3, 4 });
        new byte[] { 1, 2, 3, 4 }.CopyTo(bytes, tableOffset);
        return bytes;
    }

    private static void WriteCollectionHeader(byte[] bytes, uint version, int[] faceOffsets)
    {
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(0, 4), 0x74746366U);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(4, 4), version);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(8, 4), (uint)faceOffsets.Length);

        for (int index = 0; index < faceOffsets.Length; index += 1)
        {
            BinaryPrimitives.WriteUInt32BigEndian(
                bytes.AsSpan(12 + index * 4, 4),
                (uint)faceOffsets[index]);
        }
    }

    private static void WriteFaceDirectory(
        byte[] bytes,
        int directoryOffset,
        uint scalerType,
        int tableOffset,
        byte[] tableData)
    {
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(directoryOffset, 4), scalerType);
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(directoryOffset + 4, 2), 1);
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(directoryOffset + 6, 2), 16);
        BinaryPrimitives.WriteUInt32BigEndian(
            bytes.AsSpan(directoryOffset + 12, 4),
            (uint)FontTags.Name);
        BinaryPrimitives.WriteUInt32BigEndian(
            bytes.AsSpan(directoryOffset + 16, 4),
            FontChecksum.CalculateTable(tableData));
        BinaryPrimitives.WriteUInt32BigEndian(
            bytes.AsSpan(directoryOffset + 20, 4),
            (uint)tableOffset);
        BinaryPrimitives.WriteUInt32BigEndian(
            bytes.AsSpan(directoryOffset + 24, 4),
            (uint)tableData.Length);
    }
}
