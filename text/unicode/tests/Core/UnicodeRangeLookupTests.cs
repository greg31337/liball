using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeRangeLookupTests
{
    [Test]
    public void UnicodeRange_ValidatesBoundsAndContainsCodePoints()
    {
        UnicodeRange range = new UnicodeRange(10, 12);

        Assert.That(range.Start, Is.EqualTo(10));
        Assert.That(range.End, Is.EqualTo(12));
        Assert.That(range.Count(), Is.EqualTo(3));
        Assert.That(range.Contains(9), Is.False);
        Assert.That(range.Contains(10), Is.True);
        Assert.That(range.Contains(12), Is.True);
        Assert.That(range.Contains(13), Is.False);
        Assert.Throws<ArgumentException>(() => new UnicodeRange(12, 10));
    }

    [Test]
    public void UnicodeRangeSet_UsesBinarySearchForHitsAndMisses()
    {
        UnicodeRange[] ranges = UnicodeRange.FromArrays(
            new int[] { 0x0041, 0x0061, 0x0400 },
            new int[] { 0x005A, 0x007A, 0x04FF });
        UnicodeRangeSet set = new UnicodeRangeSet(ranges);

        Assert.That(set.GetRangeCount(), Is.EqualTo(3));
        Assert.That(ranges[0].Start, Is.EqualTo(0x0041));
        Assert.That(ranges[2].End, Is.EqualTo(0x04FF));
        Assert.That(set.Contains(0x0041), Is.True);
        Assert.That(set.Contains(0x005B), Is.False);
        Assert.That(set.Contains(0x0062), Is.True);
        Assert.That(set.Contains(0x0401), Is.True);
        Assert.That(UnicodeRangeSet.FindRangeIndexInRanges(set.GetRanges(), 0x0401), Is.EqualTo(2));
        Assert.That(UnicodeRangeSet.FindRangeIndexInRanges(ranges, 0x0030), Is.EqualTo(-1));
    }

    [Test]
    public void UnicodeRangeSet_ValidatesSortedNonOverlappingRanges()
    {
        UnicodeRange[] sorted = new UnicodeRange[]
        {
            new UnicodeRange(0, 2),
            new UnicodeRange(3, 4),
            new UnicodeRange(10, 20),
        };
        UnicodeRange[] overlapping = new UnicodeRange[]
        {
            new UnicodeRange(0, 2),
            new UnicodeRange(2, 4),
        };
        UnicodeRange[] unsorted = new UnicodeRange[]
        {
            new UnicodeRange(10, 20),
            new UnicodeRange(0, 2),
        };

        Assert.That(UnicodeRangeSet.AreRangesSortedAndNonOverlapping(sorted), Is.True);
        Assert.That(UnicodeRangeSet.AreRangesSortedAndNonOverlapping(new UnicodeRangeSet(sorted).GetRanges()), Is.True);
        Assert.That(UnicodeRangeSet.AreRangesSortedAndNonOverlapping(overlapping), Is.False);
        Assert.That(UnicodeRangeSet.AreRangesSortedAndNonOverlapping(unsorted), Is.False);
    }

    [Test]
    public void UnicodeTableLookup_ReturnsRangeValuesOrDefault()
    {
        UnicodeRangeValue[] ranges = UnicodeRangeValue.FromArrays(
            new int[] { 0x0030, 0x0041, 0x0061 },
            new int[] { 0x0039, 0x005A, 0x007A },
            new int[] { 1, 2, 3 });

        Assert.That(UnicodeTableLookup.AreRangeValuesSortedAndNonOverlapping(ranges), Is.True);
        Assert.That(ranges[0].Value, Is.EqualTo(1));
        Assert.That(UnicodeTableLookup.FindRangeValueIndex(ranges, 0x0042), Is.EqualTo(1));
        Assert.That(UnicodeTableLookup.LookupRangeValue(ranges, 0x0035, -1), Is.EqualTo(1));
        Assert.That(UnicodeTableLookup.LookupRangeValue(ranges, 0x005B, -1), Is.EqualTo(-1));
        Assert.That(UnicodeTableLookup.LookupRangeValue(ranges, 0x0062, -1), Is.EqualTo(3));
    }

    [Test]
    public void UnicodeTableLookup_TwoStageLookupReturnsValuesOrDefault()
    {
        int[] stage1 = new int[] { 0, -1, 16 };
        int[] stage2 = new int[]
        {
            0, 1, 2, 3, 4, 5, 6, 7,
            8, 9, 10, 11, 12, 13, 14, 15,
            100, 101, 102, 103, 104, 105, 106, 107,
            108, 109, 110, 111, 112, 113, 114, 115,
        };

        Assert.That(UnicodeTableLookup.LookupTwoStageValue(0x0005, stage1, stage2, 4, -1), Is.EqualTo(5));
        Assert.That(UnicodeTableLookup.LookupTwoStageValue(0x0015, stage1, stage2, 4, -1), Is.EqualTo(-1));
        Assert.That(UnicodeTableLookup.LookupTwoStageValue(0x0025, stage1, stage2, 4, -1), Is.EqualTo(105));
        Assert.That(UnicodeTableLookup.LookupTwoStageValue(0xD800, stage1, stage2, 4, -1), Is.EqualTo(-1));
        Assert.That(
            UnicodeTableLookup.LookupTwoStageValue(0x0001, new int[] { int.MaxValue }, stage2, 4, -1),
            Is.EqualTo(-1));
    }

    [Test]
    public void GeneratedRangeTables_ReturnCachedArraysForLookupReuse()
    {
        Assert.That(object.ReferenceEquals(UnicodeIdentifierTables.IdStartData, UnicodeIdentifierTables.IdStart()), Is.True);
        Assert.That(object.ReferenceEquals(UnicodeIdentifierTables.IdContinueData, UnicodeIdentifierTables.IdContinue()), Is.True);
        Assert.That(object.ReferenceEquals(UnicodeCategoryTables.GeneralCategoriesData, UnicodeCategoryTables.GeneralCategories()), Is.True);
        Assert.That(object.ReferenceEquals(UnicodePdfEncodingTables.PdfDocEncodingUnicodeToByteData, UnicodePdfEncodingTables.PdfDocEncodingUnicodeToByte()), Is.True);
        Assert.That(UnicodeRangeSet.AreRangesSortedAndNonOverlapping(UnicodeIdentifierTables.IdStart()), Is.True);
        Assert.That(UnicodeTableLookup.AreRangeValuesSortedAndNonOverlapping(UnicodeCategoryTables.GeneralCategories()), Is.True);
    }

    [Test]
    public void LookupHelpers_ThrowForNullTables()
    {
        Assert.Throws<ArgumentNullException>(() => new UnicodeRangeSet(null!));
        Assert.Throws<ArgumentNullException>(() => UnicodeRange.FromArrays(null!, new int[] { }));
        Assert.Throws<ArgumentNullException>(() => UnicodeRange.FromArrays(new int[] { }, null!));
        Assert.Throws<ArgumentException>(() => UnicodeRange.FromArrays(new int[] { 0 }, new int[] { }));
        Assert.Throws<ArgumentNullException>(() => UnicodeRangeSet.ContainsRanges(null!, 0));
        Assert.Throws<ArgumentNullException>(() => UnicodeRangeSet.AreRangesSortedAndNonOverlapping(null!));
        Assert.Throws<ArgumentNullException>(() => UnicodeRangeValue.FromArrays(null!, new int[] { }, new int[] { }));
        Assert.Throws<ArgumentNullException>(() => UnicodeRangeValue.FromArrays(new int[] { }, null!, new int[] { }));
        Assert.Throws<ArgumentNullException>(() => UnicodeRangeValue.FromArrays(new int[] { }, new int[] { }, null!));
        Assert.Throws<ArgumentException>(() => UnicodeRangeValue.FromArrays(new int[] { 0 }, new int[] { }, new int[] { 1 }));
        Assert.Throws<ArgumentException>(() => UnicodeRangeValue.FromArrays(new int[] { 0 }, new int[] { 0 }, new int[] { }));
        Assert.Throws<ArgumentNullException>(() => UnicodeTableLookup.LookupRangeValue(null!, 0, -1));
        Assert.Throws<ArgumentNullException>(() => UnicodeTableLookup.AreRangeValuesSortedAndNonOverlapping(null!));
        Assert.Throws<ArgumentNullException>(() => UnicodeTableLookup.LookupTwoStageValue(0, null!, new int[] { 0 }, 0, -1));
        Assert.Throws<ArgumentNullException>(() => UnicodeTableLookup.LookupTwoStageValue(0, new int[] { 0 }, null!, 0, -1));
    }
}
