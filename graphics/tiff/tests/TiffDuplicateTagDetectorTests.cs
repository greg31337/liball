using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffDuplicateTagDetectorTests
{
    [Test]
    public void DetectEntries_ReturnsSuccessForUniqueTags()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth),
            CreateEntry(TiffTagId.ImageLength),
            CreateEntry(TiffTagId.Compression)
        };

        TiffDuplicateTagResult result = TiffDuplicateTagDetector.DetectEntries(entries);

        Assert.That(result.Success, Is.True);
        Assert.That(result.HasDuplicates, Is.False);
        Assert.That(result.DuplicateTagIds.Length, Is.EqualTo(0));
        Assert.That(result.Diagnostics.Count(), Is.EqualTo(0));
    }

    [Test]
    public void DetectEntries_ReportsDuplicateTagOnce()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth),
            CreateEntry(TiffTagId.ImageLength),
            CreateEntry(TiffTagId.ImageWidth),
            CreateEntry(TiffTagId.ImageWidth)
        };

        TiffDuplicateTagResult result = TiffDuplicateTagDetector.DetectEntries(entries);

        Assert.That(result.Success, Is.False);
        Assert.That(result.HasDuplicates, Is.True);
        Assert.That(result.DuplicateTagIds, Is.EqualTo(new int[] { TiffTagId.ImageWidth }));
        Assert.That(result.Diagnostics.ErrorCount(), Is.EqualTo(1));
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.ImageWidth));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.DuplicateTag), Is.True);
    }

    [Test]
    public void DetectEntries_ReportsMultipleDuplicateTagIdsInEncounterOrder()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth),
            CreateEntry(TiffTagId.ImageLength),
            CreateEntry(TiffTagId.Compression),
            CreateEntry(TiffTagId.ImageLength),
            CreateEntry(TiffTagId.ImageWidth)
        };

        TiffDuplicateTagResult result = TiffDuplicateTagDetector.DetectEntries(entries);

        Assert.That(result.Success, Is.False);
        Assert.That(result.DuplicateTagIds, Is.EqualTo(new int[] { TiffTagId.ImageLength, TiffTagId.ImageWidth }));
        Assert.That(result.Diagnostics.ErrorCount(), Is.EqualTo(2));
    }

    [Test]
    public void DetectEntries_IgnoresNullEntries()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth),
            null!,
            CreateEntry(TiffTagId.ImageLength)
        };

        TiffDuplicateTagResult result = TiffDuplicateTagDetector.DetectEntries(entries);

        Assert.That(result.Success, Is.True);
        Assert.That(result.HasDuplicates, Is.False);
    }

    [Test]
    public void DetectEntries_NullArrayIsSuccessful()
    {
        TiffDuplicateTagResult result = TiffDuplicateTagDetector.DetectEntries(null!);

        Assert.That(result.Success, Is.True);
        Assert.That(result.HasDuplicates, Is.False);
    }

    [Test]
    public void DetectValues_ReportsDuplicateTagValues()
    {
        TiffTagValue[] values = new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 100),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, 200),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 300)
        };

        TiffDuplicateTagResult result = TiffDuplicateTagDetector.DetectValues(values);

        Assert.That(result.Success, Is.False);
        Assert.That(result.DuplicateTagIds, Is.EqualTo(new int[] { TiffTagId.ImageWidth }));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.DuplicateTag), Is.True);
    }

    [Test]
    public void ResultCopiesDuplicateTagIds()
    {
        int[] duplicates = new int[] { TiffTagId.ImageWidth };

        TiffDuplicateTagResult result = TiffDuplicateTagResult.Create(duplicates, new TiffDiagnostics());
        duplicates[0] = TiffTagId.ImageLength;

        Assert.That(result.DuplicateTagIds, Is.EqualTo(new int[] { TiffTagId.ImageWidth }));
    }

    [Test]
    public void HelperMethods_DetectPriorTagsAndAppendIds()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth),
            CreateEntry(TiffTagId.ImageLength)
        };

        int[] ids = TiffDuplicateTagDetector.AppendTagId(new int[] { TiffTagId.ImageWidth }, TiffTagId.ImageLength);

        Assert.That(TiffDuplicateTagDetector.AppearsEarlier(entries, 1, TiffTagId.ImageWidth), Is.True);
        Assert.That(TiffDuplicateTagDetector.AppearsEarlier(entries, 1, TiffTagId.Compression), Is.False);
        Assert.That(TiffDuplicateTagDetector.ContainsTagId(ids, TiffTagId.ImageLength), Is.True);
        Assert.That(ids, Is.EqualTo(new int[] { TiffTagId.ImageWidth, TiffTagId.ImageLength }));
    }

    static TiffIfdEntry CreateEntry(int tagId)
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = tagId;
        entry.DataType = TiffDataType.Long;
        entry.Count = 1;
        entry.RawCount = 1;
        entry.ValueByteCount = 4;
        entry.ValueSlotByteCount = 4;
        return entry;
    }
}
