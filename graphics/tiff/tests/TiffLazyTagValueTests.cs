using System.IO;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffLazyTagValueTests
{
    [Test]
    public void ReadOptions_DefaultLazyThresholdDetectsLargeExternalValues()
    {
        TiffReadOptions options = new TiffReadOptions();
        TiffIfdEntry smallExternal = CreateEntry(0, 128, 4);
        TiffIfdEntry largeExternal = CreateEntry(0, 4097, 4);
        TiffIfdEntry inline = CreateEntry(0, 4, 4);

        Assert.That(options.LazyValueByteThreshold, Is.EqualTo(4096));
        Assert.That(options.ShouldLoadTagValueLazily(smallExternal), Is.False);
        Assert.That(options.ShouldLoadTagValueLazily(largeExternal), Is.True);
        Assert.That(options.ShouldLoadTagValueLazily(inline), Is.False);
        Assert.That(options.ShouldLoadTagValueLazily(null!), Is.False);
    }

    [Test]
    public void ReadOptions_CopyPreservesLazyThreshold()
    {
        TiffReadOptions options = new TiffReadOptions();
        options.LazyValueByteThreshold = 1024;

        TiffReadOptions copy = options.Copy();

        Assert.That(copy.LazyValueByteThreshold, Is.EqualTo(1024));
    }

    [Test]
    public void ShouldUseLazyLoading_ReturnsFalseForNegativeThreshold()
    {
        TiffIfdEntry external = CreateEntry(0, 128, 4);

        Assert.That(TiffLazyTagValue.ShouldUseLazyLoading(external, -1), Is.False);
    }

    [Test]
    public void LazyTagValue_DoesNotMaterializeUntilBytesAreRequested()
    {
        TiffIfdEntry entry = CreateEntry(4, 4, 2);
        byte[] fileBytes = new byte[] { 0, 1, 2, 3, 10, 11, 12, 13 };
        TiffLazyTagValue lazy = new TiffLazyTagValue(entry, fileBytes, new TiffLimits());

        Assert.That(lazy.IsMaterialized(), Is.False);

        TiffTagValueLoadResult result = lazy.GetBytes();

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 10, 11, 12, 13 }));
        Assert.That(lazy.IsMaterialized(), Is.True);
    }

    [Test]
    public void LazyTagValue_CachesMaterializedMemoryResult()
    {
        TiffIfdEntry entry = CreateEntry(4, 4, 2);
        byte[] fileBytes = new byte[] { 0, 1, 2, 3, 10, 11, 12, 13 };
        TiffLazyTagValue lazy = new TiffLazyTagValue(entry, fileBytes, new TiffLimits());

        TiffTagValueLoadResult first = lazy.GetBytes();
        fileBytes[4] = 99;
        TiffTagValueLoadResult second = lazy.GetBytes();

        Assert.That(first.Bytes, Is.EqualTo(new byte[] { 10, 11, 12, 13 }));
        Assert.That(second.Bytes, Is.EqualTo(new byte[] { 10, 11, 12, 13 }));
    }

    [Test]
    public void LazyTagValue_ReturnsDefensiveCopiesOfCachedBytes()
    {
        TiffIfdEntry entry = CreateEntry(4, 4, 2);
        byte[] fileBytes = new byte[] { 0, 1, 2, 3, 10, 11, 12, 13 };
        TiffLazyTagValue lazy = new TiffLazyTagValue(entry, fileBytes, new TiffLimits());

        TiffTagValueLoadResult first = lazy.GetBytes();
        first.Bytes[0] = 99;
        TiffTagValueLoadResult second = lazy.GetBytes();

        Assert.That(second.Bytes, Is.EqualTo(new byte[] { 10, 11, 12, 13 }));
    }

    [Test]
    public void LazyTagValue_LoadsFromStreamAndRestoresPosition()
    {
        TiffIfdEntry entry = CreateEntry(4, 4, 2);
        MemoryStream stream = new MemoryStream(new byte[] { 0, 1, 2, 3, 10, 11, 12, 13 });
        stream.Position = 2;
        TiffLazyTagValue lazy = TiffLazyTagValue.CreateFromStream(entry, stream, new TiffLimits());

        TiffTagValueLoadResult result = lazy.GetBytes();

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 10, 11, 12, 13 }));
        Assert.That(stream.Position, Is.EqualTo(2));
        Assert.That(lazy.IsMaterialized(), Is.True);
    }

    [Test]
    public void LazyTagValue_LoadsInlineWithoutFileBytes()
    {
        TiffIfdEntry entry = CreateEntry(0, 4, 4);
        entry.ValueOffsetBytes = new byte[] { 1, 2, 3, 4 };
        TiffLazyTagValue lazy = new TiffLazyTagValue(entry, new byte[0], new TiffLimits());

        TiffTagValueLoadResult result = lazy.GetBytes();

        Assert.That(result.Success, Is.True);
        Assert.That(result.WasInline, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
    }

    [Test]
    public void LazyTagValue_CopiesEntryAtConstruction()
    {
        TiffIfdEntry entry = CreateEntry(4, 4, 2);
        byte[] fileBytes = new byte[] { 0, 1, 2, 3, 10, 11, 12, 13 };
        TiffLazyTagValue lazy = new TiffLazyTagValue(entry, fileBytes, new TiffLimits());
        entry.ValueOffset = 0;

        TiffTagValueLoadResult result = lazy.GetBytes();

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 10, 11, 12, 13 }));
    }

    [Test]
    public void ReadOptions_CreateLazyTagValueUsesOptionsLimits()
    {
        TiffReadOptions options = new TiffReadOptions();
        options.Limits.MaximumAllocationBytes = 3;
        TiffIfdEntry entry = CreateEntry(4, 4, 2);
        byte[] fileBytes = new byte[] { 0, 1, 2, 3, 10, 11, 12, 13 };

        TiffLazyTagValue lazy = options.CreateLazyTagValue(entry, fileBytes);
        TiffTagValueLoadResult result = lazy.GetBytes();

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.AllocationLimitExceeded), Is.True);
    }

    static TiffIfdEntry CreateEntry(ulong offset, long byteCount, int slotByteCount)
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = TiffTagId.StripOffsets;
        entry.DataType = TiffDataType.Byte;
        entry.Count = byteCount;
        entry.RawCount = (ulong)byteCount;
        entry.ValueOffset = offset;
        entry.ValueOffsetBytes = new byte[slotByteCount];
        entry.ValueByteCount = byteCount;
        entry.ValueSlotByteCount = slotByteCount;
        return entry;
    }
}
