using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodePdfEncodingTableTests
{
    [Test]
    public void PdfDocEncodingToUnicode_ExposesAllByteMappings()
    {
        int[] table = UnicodePdfEncodingTables.PdfDocEncodingToUnicode();

        Assert.That(table, Has.Length.EqualTo(256));
        Assert.That(table[0x00], Is.EqualTo(0x0000));
        Assert.That(table[0x09], Is.EqualTo(0x0009));
        Assert.That(table[0x18], Is.EqualTo(0x02D8));
        Assert.That(table[0x41], Is.EqualTo(0x0041));
        Assert.That(table[0x7E], Is.EqualTo(0x007E));
        Assert.That(table[0x7F], Is.EqualTo(UnicodeScalar.ReplacementCharacter));
        Assert.That(table[0x80], Is.EqualTo(0x2022));
        Assert.That(table[0x85], Is.EqualTo(0x2013));
        Assert.That(table[0x8B], Is.EqualTo(0x2030));
        Assert.That(table[0x93], Is.EqualTo(0xFB01));
        Assert.That(table[0xA0], Is.EqualTo(0x20AC));
        Assert.That(table[0xAD], Is.EqualTo(UnicodeScalar.ReplacementCharacter));
        Assert.That(table[0xFF], Is.EqualTo(0x00FF));
    }

    [Test]
    public void PdfDocEncodingIdentityRanges_ExposeDirectUnicodeToByteRanges()
    {
        UnicodeRange[] ranges = UnicodePdfEncodingTables.PdfDocEncodingIdentityRanges();

        Assert.That(ranges, Has.Length.EqualTo(4));
        AssertRange(ranges[0], 0x0000, 0x0017);
        AssertRange(ranges[1], 0x0020, 0x007E);
        AssertRange(ranges[2], 0x00A1, 0x00AC);
        AssertRange(ranges[3], 0x00AE, 0x00FF);
        Assert.That(UnicodeRangeSet.ContainsRanges(ranges, 0x00AD), Is.False);
        Assert.That(UnicodeRangeSet.ContainsRanges(ranges, 0x007F), Is.False);
    }

    [Test]
    public void PdfDocEncodingUnicodeToByte_ExposesSortedSpecialMappings()
    {
        UnicodeRangeValue[] ranges = UnicodePdfEncodingTables.PdfDocEncodingUnicodeToByte();

        Assert.That(ranges, Has.Length.EqualTo(40));
        Assert.That(UnicodeTableLookup.AreRangeValuesSortedAndNonOverlapping(ranges), Is.True);
        Assert.That(UnicodeTableLookup.LookupRangeValue(ranges, 0x02D8, -1), Is.EqualTo(0x18));
        Assert.That(UnicodeTableLookup.LookupRangeValue(ranges, 0x2013, -1), Is.EqualTo(0x85));
        Assert.That(UnicodeTableLookup.LookupRangeValue(ranges, 0x2022, -1), Is.EqualTo(0x80));
        Assert.That(UnicodeTableLookup.LookupRangeValue(ranges, 0x20AC, -1), Is.EqualTo(0xA0));
        Assert.That(UnicodeTableLookup.LookupRangeValue(ranges, 0xFB01, -1), Is.EqualTo(0x93));
        Assert.That(UnicodeTableLookup.LookupRangeValue(ranges, UnicodeScalar.ReplacementCharacter, -1), Is.EqualTo(-1));
        Assert.That(UnicodeTableLookup.LookupRangeValue(ranges, 0x00AD, -1), Is.EqualTo(-1));
    }

    static void AssertRange(UnicodeRange range, int start, int end)
    {
        Assert.That(range.Start, Is.EqualTo(start));
        Assert.That(range.End, Is.EqualTo(end));
    }
}
