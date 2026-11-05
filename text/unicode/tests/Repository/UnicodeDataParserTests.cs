using NUnit.Framework;
using unicode.Tools.UnicodeDataGenerator;

namespace unicode.Tests;

[TestFixture]
public class UnicodeDataParserTests
{
    [Test]
    public void ParseUnicodeDataLine_ReadsNamesCategoriesCombiningClassAndBidiClass()
    {
        UnicodeDataRecord record = UnicodeDataParser.ParseUnicodeDataLine("0301;COMBINING ACUTE ACCENT;Mn;230;NSM;;;;;N;NON-SPACING ACUTE ACCENT;;;;");

        Assert.That(record.CodePoint, Is.EqualTo(0x0301));
        Assert.That(record.Name, Is.EqualTo("COMBINING ACUTE ACCENT"));
        Assert.That(record.GeneralCategory, Is.EqualTo("Mn"));
        Assert.That(record.CombiningClass, Is.EqualTo(230));
        Assert.That(record.BidiClass, Is.EqualTo("NSM"));
        Assert.That(record.DecompositionType, Is.EqualTo(""));
        Assert.That(record.DecompositionMapping, Is.Empty);
    }

    [Test]
    public void ParseUnicodeDataLine_ReadsCanonicalDecompositionAndSimpleCaseMappings()
    {
        UnicodeDataRecord record = UnicodeDataParser.ParseUnicodeDataLine("00C0;LATIN CAPITAL LETTER A WITH GRAVE;Lu;0;L;0041 0300;;;;N;LATIN CAPITAL LETTER A GRAVE;;;00E0;");

        Assert.That(record.CodePoint, Is.EqualTo(0x00C0));
        Assert.That(record.DecompositionType, Is.EqualTo(""));
        Assert.That(record.DecompositionMapping, Is.EqualTo(new int[] { 0x0041, 0x0300 }));
        Assert.That(record.SimpleUppercaseMapping, Is.EqualTo(-1));
        Assert.That(record.SimpleLowercaseMapping, Is.EqualTo(0x00E0));
        Assert.That(record.SimpleTitlecaseMapping, Is.EqualTo(-1));
    }

    [Test]
    public void ParseUnicodeDataLine_ReadsCompatibilityDecompositionAndDigitValues()
    {
        UnicodeDataRecord record = UnicodeDataParser.ParseUnicodeDataLine("00B2;SUPERSCRIPT TWO;No;0;EN;<super> 0032;;2;2;N;SUPERSCRIPT DIGIT TWO;;;;");

        Assert.That(record.CodePoint, Is.EqualTo(0x00B2));
        Assert.That(record.DecompositionType, Is.EqualTo("super"));
        Assert.That(record.DecompositionMapping, Is.EqualTo(new int[] { 0x0032 }));
        Assert.That(record.DecimalDigitValue, Is.EqualTo(-1));
        Assert.That(record.DigitValue, Is.EqualTo(2));
        Assert.That(record.NumericValue.HasValue, Is.True);
        Assert.That(record.NumericValue.Text, Is.EqualTo("2"));
        Assert.That(record.NumericValue.Numerator, Is.EqualTo(2));
        Assert.That(record.NumericValue.Denominator, Is.EqualTo(1));
    }

    [Test]
    public void ParseUnicodeDataLine_ReadsFractionalNumericValues()
    {
        UnicodeDataRecord record = UnicodeDataParser.ParseUnicodeDataLine("00BD;VULGAR FRACTION ONE HALF;No;0;ON;<fraction> 0031 2044 0032;;;1/2;N;FRACTION ONE HALF;;;;");

        Assert.That(record.DecompositionType, Is.EqualTo("fraction"));
        Assert.That(record.DecompositionMapping, Is.EqualTo(new int[] { 0x0031, 0x2044, 0x0032 }));
        Assert.That(record.NumericValue.HasValue, Is.True);
        Assert.That(record.NumericValue.Text, Is.EqualTo("1/2"));
        Assert.That(record.NumericValue.Numerator, Is.EqualTo(1));
        Assert.That(record.NumericValue.Denominator, Is.EqualTo(2));
    }

    [Test]
    public void ParseUnicodeDataLine_ReadsUpperAndTitlecaseMappings()
    {
        UnicodeDataRecord record = UnicodeDataParser.ParseUnicodeDataLine("0061;LATIN SMALL LETTER A;Ll;0;L;;;;;N;;;0041;;0041");

        Assert.That(record.SimpleUppercaseMapping, Is.EqualTo(0x0041));
        Assert.That(record.SimpleLowercaseMapping, Is.EqualTo(-1));
        Assert.That(record.SimpleTitlecaseMapping, Is.EqualTo(0x0041));
    }

    [Test]
    public void ReadUnicodeData_StripsCommentsAndBlankLines()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "UnicodeData.sample.txt");
        File.WriteAllLines(
            path,
            new string[]
            {
                "# comment",
                "",
                "0041;LATIN CAPITAL LETTER A;Lu;0;L;;;;;N;;;;0061; # trailing comment",
                "0061;LATIN SMALL LETTER A;Ll;0;L;;;;;N;;;0041;;0041",
            });

        UnicodeDataRecord[] records = UnicodeDataParser.ReadUnicodeData(path);

        Assert.That(records, Has.Length.EqualTo(2));
        Assert.That(records[0].CodePoint, Is.EqualTo(0x0041));
        Assert.That(records[0].SimpleLowercaseMapping, Is.EqualTo(0x0061));
        Assert.That(records[1].CodePoint, Is.EqualTo(0x0061));
    }

    [Test]
    public void ParseUnicodeDataLine_RejectsMalformedFieldCount()
    {
        Assert.Throws<FormatException>(() => UnicodeDataParser.ParseUnicodeDataLine("0041;LATIN CAPITAL LETTER A"));
    }
}
