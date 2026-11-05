using NUnit.Framework;
using unicode.Tools.UnicodeDataGenerator;

namespace unicode.Tests;

[TestFixture]
public class UnicodeNormalizationParserTests
{
    [Test]
    public void ParseNormalizationQuickCheckLine_ReadsRangeFormAndValue()
    {
        UnicodeNormalizationQuickCheckRecord record = UnicodeDataParser.ParseNormalizationQuickCheckLine("00C0..00C5 ; NFD_QC; N # L& [6]");

        Assert.That(record.StartCodePoint, Is.EqualTo(0x00C0));
        Assert.That(record.EndCodePoint, Is.EqualTo(0x00C5));
        Assert.That(record.Form, Is.EqualTo("NFD"));
        Assert.That(record.QuickCheckValue, Is.EqualTo("N"));
        Assert.That(record.Contains(0x00C3), Is.True);
        Assert.That(record.Contains(0x00C6), Is.False);
    }

    [Test]
    public void ReadNormalizationQuickChecks_FiltersOtherDerivedNormalizationProperties()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "DerivedNormalizationProps.sample.txt");
        File.WriteAllLines(
            path,
            new string[]
            {
                "# comment",
                "",
                "037A ; FC_NFKC; 0020 03B9",
                "0340..0341 ; NFC_QC; N",
                "0300..0301 ; NFC_QC; M",
                "00C0 ; NFD_QC; N",
                "0958 ; Full_Composition_Exclusion",
            });

        UnicodeNormalizationQuickCheckRecord[] records = UnicodeDataParser.ReadNormalizationQuickChecks(path);

        Assert.That(records, Has.Length.EqualTo(3));
        Assert.That(records[0].Form, Is.EqualTo("NFC"));
        Assert.That(records[0].QuickCheckValue, Is.EqualTo("N"));
        Assert.That(records[1].QuickCheckValue, Is.EqualTo("M"));
        Assert.That(records[2].Form, Is.EqualTo("NFD"));
    }

    [Test]
    public void ParseCompositionExclusionLine_ReadsSingleCodePointAndRanges()
    {
        UnicodeCompositionExclusionRecord single = UnicodeDataParser.ParseCompositionExclusionLine("0958 # DEVANAGARI LETTER QA");
        UnicodeCompositionExclusionRecord range = UnicodeDataParser.ParseCompositionExclusionLine("0958..095F # sample range");

        Assert.That(single.StartCodePoint, Is.EqualTo(0x0958));
        Assert.That(single.EndCodePoint, Is.EqualTo(0x0958));
        Assert.That(single.Contains(0x0958), Is.True);
        Assert.That(single.Contains(0x0959), Is.False);
        Assert.That(range.StartCodePoint, Is.EqualTo(0x0958));
        Assert.That(range.EndCodePoint, Is.EqualTo(0x095F));
    }

    [Test]
    public void ReadCompositionExclusions_StripsCommentsAndBlankLines()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "CompositionExclusions.sample.txt");
        File.WriteAllLines(
            path,
            new string[]
            {
                "# comment",
                "",
                "0958 # DEVANAGARI LETTER QA",
                "0F43 # TIBETAN LETTER GHA",
            });

        UnicodeCompositionExclusionRecord[] records = UnicodeDataParser.ReadCompositionExclusions(path);

        Assert.That(records, Has.Length.EqualTo(2));
        Assert.That(records[0].StartCodePoint, Is.EqualTo(0x0958));
        Assert.That(records[1].StartCodePoint, Is.EqualTo(0x0F43));
    }

    [Test]
    public void ReadDecompositionMappings_ReturnsCanonicalAndCompatibilityMappings()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "UnicodeData.decomposition.sample.txt");
        File.WriteAllLines(
            path,
            new string[]
            {
                "0041;LATIN CAPITAL LETTER A;Lu;0;L;;;;;N;;;;0061;",
                "00C0;LATIN CAPITAL LETTER A WITH GRAVE;Lu;0;L;0041 0300;;;;N;LATIN CAPITAL LETTER A GRAVE;;;00E0;",
                "00B2;SUPERSCRIPT TWO;No;0;EN;<super> 0032;;2;2;N;SUPERSCRIPT DIGIT TWO;;;;",
            });

        UnicodeDecompositionRecord[] records = UnicodeDataParser.ReadDecompositionMappings(path);

        Assert.That(records, Has.Length.EqualTo(2));
        Assert.That(records[0].CodePoint, Is.EqualTo(0x00C0));
        Assert.That(records[0].DecompositionType, Is.EqualTo(""));
        Assert.That(records[0].Mapping, Is.EqualTo(new int[] { 0x0041, 0x0300 }));
        Assert.That(records[0].IsCompatibility, Is.False);
        Assert.That(records[1].CodePoint, Is.EqualTo(0x00B2));
        Assert.That(records[1].DecompositionType, Is.EqualTo("super"));
        Assert.That(records[1].Mapping, Is.EqualTo(new int[] { 0x0032 }));
        Assert.That(records[1].IsCompatibility, Is.True);
    }

    [Test]
    public void NormalizationParsers_RejectMalformedRecords()
    {
        Assert.Throws<FormatException>(() => UnicodeDataParser.ParseNormalizationQuickCheckLine("00C0 ; NFC_QC"));
        Assert.Throws<FormatException>(() => UnicodeDataParser.ParseNormalizationQuickCheckLine("00C0 ; NFC; N"));
        Assert.Throws<FormatException>(() => UnicodeDataParser.ParseNormalizationQuickCheckLine("00C0 ; FOO_QC; N"));
        Assert.Throws<FormatException>(() => UnicodeDataParser.ParseNormalizationQuickCheckLine("00C0 ; NFC_QC; X"));
        Assert.Throws<FormatException>(() => UnicodeDataParser.ParseCompositionExclusionLine(""));
        Assert.Throws<ArgumentException>(() => new UnicodeCompositionExclusionRecord(2, 1));
        Assert.Throws<ArgumentException>(() => new UnicodeNormalizationQuickCheckRecord(2, 1, "NFC", "N"));
    }
}
