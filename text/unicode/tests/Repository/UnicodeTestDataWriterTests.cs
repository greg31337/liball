using NUnit.Framework;
using unicode.Tools.UnicodeDataGenerator;

namespace unicode.Tests;

[TestFixture]
public class UnicodeTestDataWriterTests
{
    [Test]
    public void WritePropertyRegressionFixture_GeneratesBroadPropertyRows()
    {
        string root = CreateSampleDataDirectory();
        string outputPath = Path.Combine(root, "PropertyRegression.tsv");

        UnicodeTestDataWriter.WritePropertyRegressionFixture(root, outputPath);

        string[] lines = File.ReadAllLines(outputPath);
        Assert.That(lines[0], Is.EqualTo("CodePoint\tName\tGeneralCategory\tCombiningClass\tBidiClass\tScript\tBlock\tEastAsianWidth\tNumericValue\tBinaryProperties"));
        Assert.That(lines, Does.Contain("0009\t\tCn\t0\tL\tUnknown\tBasic Latin\tNa\t\tWhite_Space"));
        Assert.That(lines, Does.Contain("0041\tLATIN CAPITAL LETTER A\tLu\t0\tL\tLatin\tBasic Latin\tNa\t\tUppercase|Alphabetic"));
        Assert.That(lines, Does.Contain("00BD\tVULGAR FRACTION ONE HALF\tNo\t0\tON\tUnknown\tLatin-1 Supplement\tN\t1/2\t"));
        Assert.That(lines, Does.Contain("0301\tCOMBINING ACUTE ACCENT\tMn\t230\tNSM\tInherited\tCombining Diacritical Marks\tA\t\t"));
        Assert.That(lines, Does.Contain("1F600\tGRINNING FACE\tSo\t0\tON\tCommon\tEmoticons\tW\t\tEmoji"));
    }

    [Test]
    public void WritePropertyRegressionFixture_AddsRangeMiddleAndEndSamples()
    {
        string root = CreateSampleDataDirectory();
        string outputPath = Path.Combine(root, "PropertyRegression.tsv");

        UnicodeTestDataWriter.WritePropertyRegressionFixture(root, outputPath);

        string[] lines = File.ReadAllLines(outputPath);
        Assert.That(lines, Does.Contain("004D\t\tCn\t0\tL\tLatin\tBasic Latin\tNa\t\tUppercase|Alphabetic"));
        Assert.That(lines, Does.Contain("005A\t\tCn\t0\tL\tLatin\tBasic Latin\tNa\t\tUppercase|Alphabetic"));
        Assert.That(lines, Does.Contain("007A\t\tCn\t0\tL\tLatin\tBasic Latin\tNa\t\tLowercase|Alphabetic"));
    }

    [Test]
    public void Program_WritePropertyFixtureCommandWritesFixture()
    {
        string root = CreateSampleDataDirectory();
        string outputPath = Path.Combine(root, "CliPropertyRegression.tsv");

        int exitCode = Program.Main(new string[] { "write-property-fixture", root, outputPath });

        Assert.That(exitCode, Is.EqualTo(0));
        Assert.That(File.Exists(outputPath), Is.True);
        Assert.That(File.ReadAllText(outputPath), Does.Contain("LATIN CAPITAL LETTER A"));
    }

    static string CreateSampleDataDirectory()
    {
        string root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "PropertyRegression", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllLines(
            Path.Combine(root, "UnicodeData.txt"),
            new string[]
            {
                "0030;DIGIT ZERO;Nd;0;EN;;0;0;0;N;;;;;",
                "0041;LATIN CAPITAL LETTER A;Lu;0;L;;;;;N;;;;0061;",
                "00BD;VULGAR FRACTION ONE HALF;No;0;ON;<fraction> 0031 2044 0032;;;1/2;N;FRACTION ONE HALF;;;;",
                "0301;COMBINING ACUTE ACCENT;Mn;230;NSM;;;;;N;NON-SPACING ACUTE;;;;",
                "1F600;GRINNING FACE;So;0;ON;;;;;N;;;;;",
            });
        File.WriteAllLines(
            Path.Combine(root, "PropList.txt"),
            new string[]
            {
                "0009..000D ; White_Space",
                "1F600 ; Emoji",
            });
        File.WriteAllLines(
            Path.Combine(root, "DerivedCoreProperties.txt"),
            new string[]
            {
                "0041..005A ; Uppercase",
                "0061..007A ; Lowercase",
                "0041..005A ; Alphabetic",
                "0061..007A ; Alphabetic",
            });
        File.WriteAllLines(
            Path.Combine(root, "Scripts.txt"),
            new string[]
            {
                "0030..0039 ; Common",
                "0041..005A ; Latin",
                "0061..007A ; Latin",
                "0300..036F ; Inherited",
                "1F600 ; Common",
            });
        File.WriteAllLines(
            Path.Combine(root, "Blocks.txt"),
            new string[]
            {
                "0000..007F ; Basic Latin",
                "0080..00FF ; Latin-1 Supplement",
                "0300..036F ; Combining Diacritical Marks",
                "1F600..1F64F ; Emoticons",
            });
        File.WriteAllLines(
            Path.Combine(root, "EastAsianWidth.txt"),
            new string[]
            {
                "0000..007F ; Na",
                "0300..036F ; A",
                "1F600..1F64F ; W",
            });

        return root;
    }
}
