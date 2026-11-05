using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffRequiredTagValidatorTests
{
    [Test]
    public void ValidateEntries_ReturnsSuccessForCompleteStrippedImageDirectory()
    {
        TiffIfdEntry[] entries = CreateCommonEntries();
        entries = Append(entries, CreateEntry(TiffTagId.StripOffsets));
        entries = Append(entries, CreateEntry(TiffTagId.RowsPerStrip));
        entries = Append(entries, CreateEntry(TiffTagId.StripByteCounts));

        TiffRequiredTagValidationResult result = TiffRequiredTagValidator.ValidateEntries(entries);

        Assert.That(result.Success, Is.True);
        Assert.That(result.HasMissingRequiredTags, Is.False);
        Assert.That(result.MissingTagIds.Length, Is.EqualTo(0));
        Assert.That(result.Diagnostics.Count(), Is.EqualTo(0));
    }

    [Test]
    public void ValidateEntries_ReturnsSuccessForCompleteTiledImageDirectory()
    {
        TiffIfdEntry[] entries = CreateCommonEntries();
        entries = Append(entries, CreateEntry(TiffTagId.TileWidth));
        entries = Append(entries, CreateEntry(TiffTagId.TileLength));
        entries = Append(entries, CreateEntry(TiffTagId.TileOffsets));
        entries = Append(entries, CreateEntry(TiffTagId.TileByteCounts));

        TiffRequiredTagValidationResult result = TiffRequiredTagValidator.ValidateEntries(entries);

        Assert.That(result.Success, Is.True);
        Assert.That(result.HasMissingRequiredTags, Is.False);
        Assert.That(result.Diagnostics.Count(), Is.EqualTo(0));
    }

    [Test]
    public void ValidateEntries_ReportsMissingCommonAndStripTagsInStableOrder()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth),
            CreateEntry(TiffTagId.PhotometricInterpretation),
            CreateEntry(TiffTagId.StripOffsets)
        };

        TiffRequiredTagValidationResult result = TiffRequiredTagValidator.ValidateEntriesForImage(entries, 2);

        Assert.That(result.Success, Is.False);
        Assert.That(result.HasMissingRequiredTags, Is.True);
        Assert.That(result.MissingTagIds, Is.EqualTo(new int[] { TiffTagId.ImageLength, TiffTagId.BitsPerSample, TiffTagId.RowsPerStrip, TiffTagId.StripByteCounts }));
        Assert.That(result.Diagnostics.ErrorCount(), Is.EqualTo(4));
        Assert.That(result.Diagnostics.Get(0).Code, Is.EqualTo(TiffErrorCode.MissingRequiredTag));
        Assert.That(result.Diagnostics.Get(0).ImageIndex, Is.EqualTo(2));
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.ImageLength));
        Assert.That(result.Diagnostics.Get(3).TagId, Is.EqualTo(TiffTagId.StripByteCounts));
    }

    [Test]
    public void ValidateEntries_ReportsMissingTileTagsWhenDirectoryUsesTileStorage()
    {
        TiffIfdEntry[] entries = CreateCommonEntries();
        entries = Append(entries, CreateEntry(TiffTagId.TileOffsets));

        TiffRequiredTagValidationResult result = TiffRequiredTagValidator.ValidateEntries(entries);

        Assert.That(result.Success, Is.False);
        Assert.That(result.MissingTagIds, Is.EqualTo(new int[] { TiffTagId.TileWidth, TiffTagId.TileLength, TiffTagId.TileByteCounts }));
        Assert.That(result.Diagnostics.ErrorCount(), Is.EqualTo(3));
    }

    [Test]
    public void ValidateEntries_IgnoresNullEntries()
    {
        TiffIfdEntry[] entries = CreateCommonEntries();
        entries = Append(entries, null!);
        entries = Append(entries, CreateEntry(TiffTagId.StripOffsets));
        entries = Append(entries, CreateEntry(TiffTagId.RowsPerStrip));
        entries = Append(entries, CreateEntry(TiffTagId.StripByteCounts));

        TiffRequiredTagValidationResult result = TiffRequiredTagValidator.ValidateEntries(entries);

        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void ValidateEntries_NullArrayReportsAllStrippedImageTagsMissing()
    {
        TiffRequiredTagValidationResult result = TiffRequiredTagValidator.ValidateEntries(null!);

        Assert.That(result.Success, Is.False);
        Assert.That(result.MissingTagIds, Is.EqualTo(new int[] { TiffTagId.ImageWidth, TiffTagId.ImageLength, TiffTagId.BitsPerSample, TiffTagId.PhotometricInterpretation, TiffTagId.StripOffsets, TiffTagId.RowsPerStrip, TiffTagId.StripByteCounts }));
        Assert.That(result.Diagnostics.ErrorCount(), Is.EqualTo(7));
    }

    [Test]
    public void ValidateValues_ReportsMissingRequiredTagValues()
    {
        TiffTagValue[] values = new TiffTagValue[]
        {
            CreateValue(TiffTagId.ImageWidth),
            CreateValue(TiffTagId.ImageLength),
            CreateValue(TiffTagId.BitsPerSample),
            CreateValue(TiffTagId.PhotometricInterpretation),
            CreateValue(TiffTagId.StripOffsets),
            CreateValue(TiffTagId.RowsPerStrip)
        };

        TiffRequiredTagValidationResult result = TiffRequiredTagValidator.ValidateValuesForImage(values, 4);

        Assert.That(result.Success, Is.False);
        Assert.That(result.MissingTagIds, Is.EqualTo(new int[] { TiffTagId.StripByteCounts }));
        Assert.That(result.Diagnostics.Get(0).ImageIndex, Is.EqualTo(4));
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.StripByteCounts));
    }

    [Test]
    public void ResultCopiesMissingTagIds()
    {
        int[] missing = new int[] { TiffTagId.ImageWidth };

        TiffRequiredTagValidationResult result = TiffRequiredTagValidationResult.Create(missing, new TiffDiagnostics());
        missing[0] = TiffTagId.ImageLength;

        Assert.That(result.MissingTagIds, Is.EqualTo(new int[] { TiffTagId.ImageWidth }));
    }

    [Test]
    public void HelperMethods_ExposeRequiredSetsAndTagLookup()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth),
            CreateEntry(TiffTagId.StripOffsets)
        };

        int[] ids = TiffRequiredTagValidator.AppendTagId(new int[] { TiffTagId.ImageWidth }, TiffTagId.ImageLength);

        Assert.That(TiffRequiredTagValidator.GetCommonRequiredTagIds(), Is.EqualTo(new int[] { TiffTagId.ImageWidth, TiffTagId.ImageLength, TiffTagId.BitsPerSample, TiffTagId.PhotometricInterpretation }));
        Assert.That(TiffRequiredTagValidator.GetStripStorageRequiredTagIds(), Is.EqualTo(new int[] { TiffTagId.StripOffsets, TiffTagId.RowsPerStrip, TiffTagId.StripByteCounts }));
        Assert.That(TiffRequiredTagValidator.GetTileStorageRequiredTagIds(), Is.EqualTo(new int[] { TiffTagId.TileWidth, TiffTagId.TileLength, TiffTagId.TileOffsets, TiffTagId.TileByteCounts }));
        Assert.That(TiffRequiredTagValidator.HasEntryTag(entries, TiffTagId.ImageWidth), Is.True);
        Assert.That(TiffRequiredTagValidator.HasEntryTag(entries, TiffTagId.ImageLength), Is.False);
        Assert.That(TiffRequiredTagValidator.HasAnyEntryStripStorageTag(entries), Is.True);
        Assert.That(TiffRequiredTagValidator.ContainsTagId(ids, TiffTagId.ImageLength), Is.True);
        Assert.That(ids, Is.EqualTo(new int[] { TiffTagId.ImageWidth, TiffTagId.ImageLength }));
    }

    static TiffIfdEntry[] CreateCommonEntries()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth),
            CreateEntry(TiffTagId.ImageLength),
            CreateEntry(TiffTagId.BitsPerSample),
            CreateEntry(TiffTagId.PhotometricInterpretation)
        };
        return entries;
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

    static TiffTagValue CreateValue(int tagId)
    {
        return TiffTagValue.CreateUnsignedScalar(tagId, TiffDataType.Long, 1);
    }

    static TiffIfdEntry[] Append(TiffIfdEntry[] entries, TiffIfdEntry entry)
    {
        TiffIfdEntry[] copy = new TiffIfdEntry[entries.Length + 1];
        int index = 0;
        while (index < entries.Length)
        {
            copy[index] = entries[index];
            index = index + 1;
        }

        copy[copy.Length - 1] = entry;
        return copy;
    }
}
