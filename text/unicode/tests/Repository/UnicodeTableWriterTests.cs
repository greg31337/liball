using NUnit.Framework;
using unicode.Tools.UnicodeDataGenerator;

namespace unicode.Tests;

[TestFixture]
public class UnicodeTableWriterTests
{
    [Test]
    public void WriteCoreTables_GeneratesExpectedTableFilesAndContent()
    {
        string root = CreateSampleDataDirectory();
        string outputDirectory = Path.Combine(root, "Generated");

        UnicodeTableWriter.WriteCoreTables(root, outputDirectory);

        string categoryTables = ReadGenerated(outputDirectory, "UnicodeCategoryTables.cs");
        string propertyTables = ReadGenerated(outputDirectory, "UnicodePropertyTables.cs");
        string identifierTables = ReadGenerated(outputDirectory, "UnicodeIdentifierTables.cs");
        string scriptTables = ReadGenerated(outputDirectory, "UnicodeScriptTables.cs");
        string widthTables = ReadGenerated(outputDirectory, "UnicodeWidthTables.cs");
        string bidiTables = ReadGenerated(outputDirectory, "UnicodeBidiTables.cs");
        string numericTables = ReadGenerated(outputDirectory, "UnicodeNumericTables.cs");
        string nameTables = ReadGenerated(outputDirectory, "UnicodeNameTables.cs");
        string normalizationTables = ReadGenerated(outputDirectory, "UnicodeNormalizationTables.cs");
        string caseTables = ReadGenerated(outputDirectory, "UnicodeCaseTables.cs");
        string boundaryTables = ReadGenerated(outputDirectory, "UnicodeBoundaryTables.cs");
        string securityTables = ReadGenerated(outputDirectory, "UnicodeSecurityTables.cs");
        string pdfEncodingTables = ReadGenerated(outputDirectory, "UnicodePdfEncodingTables.cs");

        Assert.That(categoryTables, Does.Contain("internal static class UnicodeCategoryTables"));
        Assert.That(categoryTables, Does.Contain("GeneralCategories"));
        Assert.That(categoryTables, Does.Contain("\"Lu\""));
        Assert.That(categoryTables, Does.Contain("internal static readonly UnicodeRangeValue[] GeneralCategoriesData"));
        Assert.That(categoryTables, Does.Contain("UnicodeRangeValue.FromArrays"));
        Assert.That(categoryTables, Does.Contain("new int[] { 48, 65, 97, 192, 189 }"));
        Assert.That(categoryTables, Does.Contain("return GeneralCategoriesData;"));

        Assert.That(propertyTables, Does.Contain("BinaryProperties"));
        Assert.That(propertyTables, Does.Contain("\"White_Space\""));
        Assert.That(propertyTables, Does.Contain("\"Uppercase\""));

        Assert.That(identifierTables, Does.Contain("internal static class UnicodeIdentifierTables"));
        Assert.That(identifierTables, Does.Contain("IdStart"));
        Assert.That(identifierTables, Does.Contain("IdContinue"));
        Assert.That(identifierTables, Does.Contain("XidStart"));
        Assert.That(identifierTables, Does.Contain("XidContinue"));
        Assert.That(identifierTables, Does.Contain("PatternSyntax"));
        Assert.That(identifierTables, Does.Contain("PatternWhiteSpace"));
        Assert.That(identifierTables, Does.Contain("\"ID_Start\""));
        Assert.That(identifierTables, Does.Contain("\"Pattern_White_Space\""));
        Assert.That(identifierTables, Does.Contain("internal static readonly UnicodeRange[] IdStartData"));
        Assert.That(identifierTables, Does.Contain("UnicodeRange.FromArrays"));
        Assert.That(identifierTables, Does.Contain("new int[] { 65, 97 }"));
        Assert.That(identifierTables, Does.Contain("new int[] { 48, 65, 95, 97, 768 }"));
        Assert.That(identifierTables, Does.Contain("return IdStartData;"));

        Assert.That(scriptTables, Does.Contain("ScriptNames"));
        Assert.That(scriptTables, Does.Contain("\"Latin\""));

        Assert.That(widthTables, Does.Contain("EastAsianWidthNames"));
        Assert.That(widthTables, Does.Contain("\"Na\""));

        Assert.That(bidiTables, Does.Contain("BidiClassNames"));
        Assert.That(bidiTables, Does.Contain("\"EN\""));
        Assert.That(bidiTables, Does.Contain("BidiMirrors"));
        Assert.That(bidiTables, Does.Contain("new int[] { 40 }"));
        Assert.That(bidiTables, Does.Contain("BidiBracketTypeNames"));
        Assert.That(bidiTables, Does.Contain("\"o\""));

        Assert.That(numericTables, Does.Contain("NumericCodePoints"));
        Assert.That(numericTables, Does.Contain("189"));
        Assert.That(numericTables, Does.Contain("NumericDenominators"));
        Assert.That(numericTables, Does.Contain("2"));

        Assert.That(nameTables, Does.Contain("NameCodePoints"));
        Assert.That(nameTables, Does.Contain("\"LATIN CAPITAL LETTER A\""));
        Assert.That(nameTables, Does.Contain("\"VULGAR FRACTION ONE HALF\""));

        Assert.That(normalizationTables, Does.Contain("internal static class UnicodeNormalizationTables"));
        Assert.That(normalizationTables, Does.Contain("DecompositionCodePoints"));
        Assert.That(normalizationTables, Does.Contain("DecompositionMappingCodePoints"));
        Assert.That(normalizationTables, Does.Contain("\"fraction\""));
        Assert.That(normalizationTables, Does.Contain("CompositionExclusions"));
        Assert.That(normalizationTables, Does.Contain("new int[] { 2392 }"));
        Assert.That(normalizationTables, Does.Contain("QuickCheckStarts"));
        Assert.That(normalizationTables, Does.Contain("\"NFC\""));
        Assert.That(normalizationTables, Does.Contain("\"M\""));

        Assert.That(caseTables, Does.Contain("internal static class UnicodeCaseTables"));
        Assert.That(caseTables, Does.Contain("SimpleUppercaseCodePoints"));
        Assert.That(caseTables, Does.Contain("SimpleLowercaseMappings"));
        Assert.That(caseTables, Does.Contain("SpecialCaseCodePoints"));
        Assert.That(caseTables, Does.Contain("SpecialConditionNames"));
        Assert.That(caseTables, Does.Contain("\"Final_Sigma\""));
        Assert.That(caseTables, Does.Contain("\"More_Above\""));
        Assert.That(caseTables, Does.Contain("CaseFoldCodePoints"));
        Assert.That(caseTables, Does.Contain("CaseFoldStatusNames"));
        Assert.That(caseTables, Does.Contain("\"F\""));

        Assert.That(boundaryTables, Does.Contain("internal static class UnicodeBoundaryTables"));
        Assert.That(boundaryTables, Does.Contain("GraphemeBreaks"));
        Assert.That(boundaryTables, Does.Contain("\"Regional_Indicator\""));
        Assert.That(boundaryTables, Does.Contain("WordBreakNames"));
        Assert.That(boundaryTables, Does.Contain("\"ALetter\""));
        Assert.That(boundaryTables, Does.Contain("SentenceBreakNames"));
        Assert.That(boundaryTables, Does.Contain("\"ATerm\""));
        Assert.That(boundaryTables, Does.Contain("LineBreakNames"));
        Assert.That(boundaryTables, Does.Contain("\"SP\""));
        Assert.That(boundaryTables, Does.Contain("IndicConjunctBreakNames"));
        Assert.That(boundaryTables, Does.Contain("\"Linker\""));
        Assert.That(boundaryTables, Does.Contain("HangulSyllableTypeNames"));
        Assert.That(boundaryTables, Does.Contain("\"LVT\""));
        Assert.That(boundaryTables, Does.Contain("EmojiBoundaryPropertyNames"));
        Assert.That(boundaryTables, Does.Contain("\"Extended_Pictographic\""));

        Assert.That(securityTables, Does.Contain("internal static class UnicodeSecurityTables"));
        Assert.That(securityTables, Does.Contain("IdentifierStatuses"));
        Assert.That(securityTables, Does.Contain("AllowedIdentifierStatus"));
        Assert.That(securityTables, Does.Contain("ConfusableSourceCodePoints"));
        Assert.That(securityTables, Does.Contain("ConfusableTargetCodePoints"));
        Assert.That(securityTables, Does.Contain("IdentifierTypeNames"));
        Assert.That(securityTables, Does.Contain("\"Allowed\""));
        Assert.That(securityTables, Does.Contain("\"Recommended\""));
        Assert.That(securityTables, Does.Contain("\"Not_NFKC\""));
        Assert.That(securityTables, Does.Contain("new int[] { 48, 65, 880, 1536, 19968, 44032 }"));

        Assert.That(pdfEncodingTables, Does.Contain("internal static class UnicodePdfEncodingTables"));
        Assert.That(pdfEncodingTables, Does.Contain("PdfDocEncodingToUnicode"));
        Assert.That(pdfEncodingTables, Does.Contain("PdfDocEncodingIdentityRanges"));
        Assert.That(pdfEncodingTables, Does.Contain("PdfDocEncodingUnicodeToByte"));
        Assert.That(pdfEncodingTables, Does.Contain("65533"));
        Assert.That(pdfEncodingTables, Does.Contain("8364"));
        Assert.That(pdfEncodingTables, Does.Contain("UnicodeRangeValue.FromArrays"));
        Assert.That(pdfEncodingTables, Does.Contain("internal static readonly UnicodeRangeValue[] PdfDocEncodingUnicodeToByteData"));
        Assert.That(pdfEncodingTables, Does.Contain("160, 146, 138, 147, 148"));
    }

    [Test]
    public void WriteCoreTables_GeneratesSimplifiedRuntimeSyntax()
    {
        string root = CreateSampleDataDirectory();
        string outputDirectory = Path.Combine(root, "Generated");
        UnicodeTableWriter.WriteCoreTables(root, outputDirectory);
    }

    [Test]
    public void CheckedInGeneratedRangeTables_UseCompactCachedArraySource()
    {
        string root = FindRepositoryRoot();
        string generatedDirectory = Path.Combine(root, "src", "Generated");
        string[] files = Directory.GetFiles(generatedDirectory, "*.cs");
        int compactRangeTableCount = 0;
        int index = 0;

        while (index < files.Length)
        {
            string source = File.ReadAllText(files[index]);
            if (source.Contains("UnicodeRange.FromArrays") || source.Contains("UnicodeRangeValue.FromArrays"))
            {
                compactRangeTableCount = compactRangeTableCount + 1;
                Assert.That(source, Does.Contain("Data = UnicodeRange"));
                Assert.That(source, Does.Not.Contain("return UnicodeRange.FromArrays"));
                Assert.That(source, Does.Not.Contain("return UnicodeRangeValue.FromArrays"));
                Assert.That(source, Does.Not.Contain("new UnicodeRange("));
                Assert.That(source, Does.Not.Contain("new UnicodeRangeValue("));
            }

            index = index + 1;
        }

        Assert.That(compactRangeTableCount, Is.GreaterThan(5));
    }

    static string CreateSampleDataDirectory()
    {
        string root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "TableWriter", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "auxiliary"));
        Directory.CreateDirectory(Path.Combine(root, "emoji"));
        Directory.CreateDirectory(Path.Combine(root, "security"));
        File.WriteAllLines(
            Path.Combine(root, "UnicodeData.txt"),
            new string[]
            {
                "0030;DIGIT ZERO;Nd;0;EN;;0;0;0;N;;;;;",
                "0041;LATIN CAPITAL LETTER A;Lu;0;L;;;;;N;;;;0061;",
                "0061;LATIN SMALL LETTER A;Ll;0;L;;;;;N;;;0041;;0041",
                "00C0;LATIN CAPITAL LETTER A WITH GRAVE;Lu;0;L;0041 0300;;;;N;LATIN CAPITAL LETTER A GRAVE;;;00E0;",
                "00BD;VULGAR FRACTION ONE HALF;No;0;ON;<fraction> 0031 2044 0032;;;1/2;N;FRACTION ONE HALF;;;;",
            });
        File.WriteAllLines(
            Path.Combine(root, "PropList.txt"),
            new string[]
            {
                "0009..000D ; White_Space",
                "0021..0023 ; Pattern_Syntax",
                "0028..0029 ; Pattern_Syntax",
                "0009..000D ; Pattern_White_Space",
                "0020 ; Pattern_White_Space",
            });
        File.WriteAllLines(
            Path.Combine(root, "DerivedCoreProperties.txt"),
            new string[]
            {
                "0041..005A ; Uppercase",
                "0041..005A ; ID_Start",
                "0061..007A ; ID_Start",
                "0030..0039 ; ID_Continue",
                "0041..005A ; ID_Continue",
                "005F ; ID_Continue",
                "0061..007A ; ID_Continue",
                "0300..0301 ; ID_Continue",
                "0041..005A ; XID_Start",
                "0061..007A ; XID_Start",
                "0030..0039 ; XID_Continue",
                "0041..005A ; XID_Continue",
                "005F ; XID_Continue",
                "0061..007A ; XID_Continue",
                "0300..0301 ; XID_Continue",
                "094D ; InCB; Linker",
                "0915..0939 ; InCB; Consonant",
                "0300..0301 ; InCB; Extend",
            });
        File.WriteAllLines(
            Path.Combine(root, "Scripts.txt"),
            new string[]
            {
                "0041..005A ; Latin",
            });
        File.WriteAllLines(
            Path.Combine(root, "EastAsianWidth.txt"),
            new string[]
            {
                "0041 ; Na",
            });
        File.WriteAllLines(
            Path.Combine(root, "BidiMirroring.txt"),
            new string[]
            {
                "0028; 0029",
            });
        File.WriteAllLines(
            Path.Combine(root, "BidiBrackets.txt"),
            new string[]
            {
                "0028; 0029; o",
            });
        File.WriteAllLines(
            Path.Combine(root, "CompositionExclusions.txt"),
            new string[]
            {
                "0958 # DEVANAGARI LETTER QA",
            });
        File.WriteAllLines(
            Path.Combine(root, "DerivedNormalizationProps.txt"),
            new string[]
            {
                "037A ; FC_NFKC; 0020 03B9",
                "0340..0341 ; NFC_QC; N",
                "0300..0301 ; NFC_QC; M",
                "00C0 ; NFD_QC; N",
            });
        File.WriteAllLines(
            Path.Combine(root, "SpecialCasing.txt"),
            new string[]
            {
                "00DF; 00DF; 0053 0073; 0053 0053;",
                "0049; 0069 0307; 0049; 0049; lt More_Above;",
                "03A3; 03C2; 03A3; 03A3; Final_Sigma;",
            });
        File.WriteAllLines(
            Path.Combine(root, "CaseFolding.txt"),
            new string[]
            {
                "0041; C; 0061;",
                "00DF; F; 0073 0073;",
                "0049; T; 0131;",
            });
        File.WriteAllLines(
            Path.Combine(root, "auxiliary", "GraphemeBreakProperty.txt"),
            new string[]
            {
                "000D ; CR",
                "0300..0301 ; Extend",
                "1F1E6..1F1FF ; Regional_Indicator",
            });
        File.WriteAllLines(
            Path.Combine(root, "auxiliary", "WordBreakProperty.txt"),
            new string[]
            {
                "0030..0039 ; Numeric",
                "0041..005A ; ALetter",
                "0027 ; Single_Quote",
                "1F1E6..1F1FF ; Regional_Indicator",
            });
        File.WriteAllLines(
            Path.Combine(root, "auxiliary", "SentenceBreakProperty.txt"),
            new string[]
            {
                "002E ; ATerm",
                "0020 ; Sp",
                "0041..005A ; Upper",
                "0061..007A ; Lower",
            });
        File.WriteAllLines(
            Path.Combine(root, "LineBreak.txt"),
            new string[]
            {
                "0020 ; SP",
                "0021 ; EX",
                "3400..4DBF ; ID",
            });
        File.WriteAllLines(
            Path.Combine(root, "HangulSyllableType.txt"),
            new string[]
            {
                "1100..115F ; L",
                "1160..11A7 ; V",
                "11A8..11FF ; T",
                "AC00 ; LV",
                "AC01 ; LVT",
            });
        File.WriteAllLines(
            Path.Combine(root, "emoji", "emoji-data.txt"),
            new string[]
            {
                "200D ; Emoji_Component",
                "1F3FB..1F3FF ; Emoji_Modifier",
                "1F600 ; Extended_Pictographic",
            });
        File.WriteAllLines(
            Path.Combine(root, "security", "confusables.txt"),
            new string[]
            {
                "0041 ; 0061 ; SL",
                "0391 ; 0041 ; MA",
                "006D 0300 ; 0072 006E ; ML",
            });
        File.WriteAllLines(
            Path.Combine(root, "security", "IdentifierStatus.txt"),
            new string[]
            {
                "0030..003A ; Allowed",
                "0041..005A ; Allowed",
                "0370..0373 ; Allowed",
                "0600..0601 ; Allowed",
                "4E00 ; Allowed",
                "AC00 ; Allowed",
                "1F600 ; Restricted",
            });
        File.WriteAllLines(
            Path.Combine(root, "security", "IdentifierType.txt"),
            new string[]
            {
                "0030..0039 ; Recommended",
                "0041..005A ; Recommended",
                "2460..2468 ; Technical Not_XID Not_NFKC",
            });

        return root;
    }

    static string FindRepositoryRoot()
    {
        string directory = TestContext.CurrentContext.TestDirectory;

        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "unicode.slnx")) && Directory.Exists(Path.Combine(directory, "src")))
            {
                return directory;
            }

            DirectoryInfo? parent = Directory.GetParent(directory);
            if (parent == null)
            {
                break;
            }

            directory = parent.FullName;
        }

        return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
    }

    static string ReadGenerated(string outputDirectory, string fileName)
    {
        string path = Path.Combine(outputDirectory, fileName);
        Assert.That(File.Exists(path), Is.True, fileName);
        return File.ReadAllText(path);
    }
}
