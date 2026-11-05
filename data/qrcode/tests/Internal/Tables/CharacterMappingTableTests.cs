using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class CharacterMappingTableTests
{
    [Test]
    public void ShiftJisStandardMappingsCoverSingleByteAndQrKanjiCharacters()
    {
        Assert.That(ShiftJisTable.StandardMappingCount, Is.EqualTo(7037));
        Assert.That(ShiftJisTable.GetStandardUnicode(0x20), Is.EqualTo(0x20));
        Assert.That(ShiftJisTable.GetStandardUnicode(0x5C), Is.EqualTo(0x00A5));
        Assert.That(ShiftJisTable.GetStandardUnicode(0x8140), Is.EqualTo(0x3000));
        Assert.That(ShiftJisTable.GetStandardUnicode(0x82A0), Is.EqualTo(0x3042));
        Assert.That(ShiftJisTable.GetStandardUnicode(0x8ABF), Is.EqualTo(0x6F22));
        Assert.That(ShiftJisTable.GetStandardUnicode(0x8E9A), Is.EqualTo(0x5B57));
        Assert.That(ShiftJisTable.GetStandardUnicode(0x8740), Is.EqualTo(ShiftJisTable.NotMapped));
    }

    [Test]
    public void ShiftJisVendorMappingsRemainSeparateAndCanOverrideStandardMappings()
    {
        Assert.That(ShiftJisTable.VendorMappingCount, Is.EqualTo(854));
        Assert.That(ShiftJisTable.GetStandardUnicode(0x8160), Is.EqualTo(0x301C));
        Assert.That(ShiftJisTable.GetVendorUnicode(0x8160), Is.EqualTo(0xFF5E));
        Assert.That(ShiftJisTable.GetUnicode(0x8160, false), Is.EqualTo(0x301C));
        Assert.That(ShiftJisTable.GetUnicode(0x8160, true), Is.EqualTo(0xFF5E));
        Assert.That(ShiftJisTable.IsVendorOverride(0x8160), Is.True);

        Assert.That(ShiftJisTable.GetVendorUnicode(0x8740), Is.EqualTo(0x2460));
        Assert.That(ShiftJisTable.GetUnicode(0x8740, false), Is.EqualTo(ShiftJisTable.NotMapped));
        Assert.That(ShiftJisTable.GetUnicode(0x8740, true), Is.EqualTo(0x2460));
        Assert.That(ShiftJisTable.GetEncodedValue(0x2460, false), Is.EqualTo(ShiftJisTable.NotMapped));
        Assert.That(ShiftJisTable.GetEncodedValue(0x2460, true), Is.EqualTo(0x8740));
        Assert.That(ShiftJisTable.IsVendorOverride(0x8740), Is.False);
    }

    [Test]
    public void ShiftJisMappingsAreOrderedSeparatedAndReversible()
    {
        AssertStandardShiftJisMappings();
        AssertVendorShiftJisMappings();
    }

    [Test]
    public void Gb2312StandardMappingsUseEucEncodedValues()
    {
        Assert.That(Gb2312Table.StandardMappingCount, Is.EqualTo(7445));
        Assert.That(Gb2312Table.GetStandardUnicode(0xA1A1), Is.EqualTo(0x3000));
        Assert.That(Gb2312Table.GetStandardUnicode(0xB0A1), Is.EqualTo(0x554A));
        Assert.That(Gb2312Table.GetStandardUnicode(0xCEC4), Is.EqualTo(0x6587));
        Assert.That(Gb2312Table.GetStandardUnicode(0xD6D0), Is.EqualTo(0x4E2D));
        Assert.That(Gb2312Table.GetStandardUnicode(0xF7FE), Is.EqualTo(0x9F44));
        Assert.That(Gb2312Table.GetStandardUnicode(0x2121), Is.EqualTo(Gb2312Table.NotMapped));
    }

    [Test]
    public void Gb2312VendorMappingsRemainSeparateAndCanOverrideStandardMappings()
    {
        Assert.That(Gb2312Table.VendorMappingCount, Is.EqualTo(14348));
        Assert.That(Gb2312Table.GetStandardUnicode(0xA1A4), Is.EqualTo(0x30FB));
        Assert.That(Gb2312Table.GetVendorUnicode(0xA1A4), Is.EqualTo(0x00B7));
        Assert.That(Gb2312Table.GetUnicode(0xA1A4, false), Is.EqualTo(0x30FB));
        Assert.That(Gb2312Table.GetUnicode(0xA1A4, true), Is.EqualTo(0x00B7));
        Assert.That(Gb2312Table.IsVendorOverride(0xA1A4), Is.True);

        Assert.That(Gb2312Table.GetVendorUnicode(0x8140), Is.EqualTo(0x4E02));
        Assert.That(Gb2312Table.GetUnicode(0x8140, false), Is.EqualTo(Gb2312Table.NotMapped));
        Assert.That(Gb2312Table.GetUnicode(0x8140, true), Is.EqualTo(0x4E02));
        Assert.That(Gb2312Table.GetEncodedValue(0x4E02, false), Is.EqualTo(Gb2312Table.NotMapped));
        Assert.That(Gb2312Table.GetEncodedValue(0x4E02, true), Is.EqualTo(0x8140));
        Assert.That(Gb2312Table.IsVendorOverride(0x8140), Is.False);
    }

    [Test]
    public void Gb2312MappingsAreOrderedSeparatedAndReversible()
    {
        AssertStandardGb2312Mappings();
        AssertVendorGb2312Mappings();
    }

    [Test]
    public void MissingMappingsAndInvalidIndexesAreRejectedDeterministically()
    {
        Assert.That(ShiftJisTable.GetStandardUnicode(-1), Is.EqualTo(ShiftJisTable.NotMapped));
        Assert.That(ShiftJisTable.GetVendorEncodedValue(0x10FFFF), Is.EqualTo(ShiftJisTable.NotMapped));
        Assert.That(Gb2312Table.GetStandardEncodedValue(0x10FFFF), Is.EqualTo(Gb2312Table.NotMapped));
        Assert.That(Gb2312Table.GetVendorUnicode(0x10000), Is.EqualTo(Gb2312Table.NotMapped));

        Assert.That(() => ShiftJisTable.GetStandardEncodedValueAt(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => ShiftJisTable.GetVendorUnicodeAt(ShiftJisTable.VendorMappingCount), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Gb2312Table.GetStandardUnicodeAt(Gb2312Table.StandardMappingCount), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Gb2312Table.GetVendorEncodedValueAt(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    private static void AssertStandardShiftJisMappings()
    {
        int previousCode = -1;
        for (int index = 0; index < ShiftJisTable.StandardMappingCount; index++)
        {
            int code = ShiftJisTable.GetStandardEncodedValueAt(index);
            int unicodeValue = ShiftJisTable.GetStandardUnicodeAt(index);
            Assert.That(code, Is.GreaterThan(previousCode));
            Assert.That(ShiftJisTable.GetStandardUnicode(code), Is.EqualTo(unicodeValue));
            int reverseCode = ShiftJisTable.GetStandardEncodedValue(unicodeValue);
            Assert.That(reverseCode, Is.Not.EqualTo(ShiftJisTable.NotMapped));
            Assert.That(ShiftJisTable.GetStandardUnicode(reverseCode), Is.EqualTo(unicodeValue));
            previousCode = code;
        }
    }

    private static void AssertVendorShiftJisMappings()
    {
        int previousCode = -1;
        for (int index = 0; index < ShiftJisTable.VendorMappingCount; index++)
        {
            int code = ShiftJisTable.GetVendorEncodedValueAt(index);
            int unicodeValue = ShiftJisTable.GetVendorUnicodeAt(index);
            Assert.That(code, Is.GreaterThan(previousCode));
            Assert.That(ShiftJisTable.GetVendorUnicode(code), Is.EqualTo(unicodeValue));
            Assert.That(ShiftJisTable.GetStandardUnicode(code), Is.Not.EqualTo(unicodeValue));
            int reverseCode = ShiftJisTable.GetVendorEncodedValue(unicodeValue);
            Assert.That(reverseCode, Is.Not.EqualTo(ShiftJisTable.NotMapped));
            Assert.That(ShiftJisTable.GetVendorUnicode(reverseCode), Is.EqualTo(unicodeValue));
            previousCode = code;
        }
    }

    private static void AssertStandardGb2312Mappings()
    {
        int previousCode = -1;
        for (int index = 0; index < Gb2312Table.StandardMappingCount; index++)
        {
            int code = Gb2312Table.GetStandardEncodedValueAt(index);
            int unicodeValue = Gb2312Table.GetStandardUnicodeAt(index);
            Assert.That(code, Is.GreaterThan(previousCode));
            Assert.That(Gb2312Table.GetStandardUnicode(code), Is.EqualTo(unicodeValue));
            int reverseCode = Gb2312Table.GetStandardEncodedValue(unicodeValue);
            Assert.That(reverseCode, Is.Not.EqualTo(Gb2312Table.NotMapped));
            Assert.That(Gb2312Table.GetStandardUnicode(reverseCode), Is.EqualTo(unicodeValue));
            previousCode = code;
        }
    }

    private static void AssertVendorGb2312Mappings()
    {
        int previousCode = -1;
        for (int index = 0; index < Gb2312Table.VendorMappingCount; index++)
        {
            int code = Gb2312Table.GetVendorEncodedValueAt(index);
            int unicodeValue = Gb2312Table.GetVendorUnicodeAt(index);
            Assert.That(code, Is.GreaterThan(previousCode));
            Assert.That(Gb2312Table.GetVendorUnicode(code), Is.EqualTo(unicodeValue));
            Assert.That(Gb2312Table.GetStandardUnicode(code), Is.Not.EqualTo(unicodeValue));
            int reverseCode = Gb2312Table.GetVendorEncodedValue(unicodeValue);
            Assert.That(reverseCode, Is.Not.EqualTo(Gb2312Table.NotMapped));
            Assert.That(Gb2312Table.GetVendorUnicode(reverseCode), Is.EqualTo(unicodeValue));
            previousCode = code;
        }
    }
}
