using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodePdfAccessibilityIso32000CompletionTests
{
    [TestCase(UnicodePdfTextContext.ActualText, "ActualText")]
    [TestCase(UnicodePdfTextContext.AltText, "Alt")]
    [TestCase(UnicodePdfTextContext.Bookmark, "Bookmark")]
    [TestCase(UnicodePdfTextContext.Outline, "Outline")]
    [TestCase(UnicodePdfTextContext.StructureElement, "Structure element")]
    [TestCase(UnicodePdfTextContext.Metadata, "Metadata")]
    public void EveryTextContext_NormalizesAndReportsItsContext(
        UnicodePdfTextContext context,
        string contextName)
    {
        UnicodePdfText pdfText = new UnicodePdfText();
        UnicodePdfDiagnostics diagnostics = new UnicodePdfDiagnostics();

        string prepared = pdfText.PrepareAccessibilityText("Cafe\u0301", context);
        UnicodePdfDiagnosticResult unprepared = diagnostics.CheckAccessibilityText("Cafe\u0301", context);
        UnicodePdfDiagnosticResult ready = diagnostics.CheckAccessibilityText(prepared, context);

        Assert.That(prepared, Is.EqualTo("Caf\u00E9"));
        Assert.That(pdfText.IsAccessibilityTextPrepared(prepared, context), Is.True);
        Assert.That(unprepared.IsValid, Is.False);
        Assert.That(unprepared.Status, Is.EqualTo(UnicodePdfDiagnosticStatus.NeedsAccessibilityTextPreparation));
        Assert.That(FindRule(unprepared, UnicodePdfDiagnosticRules.NeedsAccessibilityTextPreparationRule).Message, Does.Contain(contextName));
        Assert.That(ready.IsValid, Is.True);
        Assert.That(ready.Status, Is.EqualTo(UnicodePdfDiagnosticStatus.Valid));
        Assert.That(ready.Diagnostics, Is.Empty);
    }

    [TestCase(UnicodePdfTextContext.ActualText)]
    [TestCase(UnicodePdfTextContext.AltText)]
    [TestCase(UnicodePdfTextContext.Bookmark)]
    [TestCase(UnicodePdfTextContext.Outline)]
    [TestCase(UnicodePdfTextContext.StructureElement)]
    [TestCase(UnicodePdfTextContext.Metadata)]
    public void EveryTextContext_SupportsIso32000TextStringEncodingOutcomes(
        UnicodePdfTextContext context)
    {
        UnicodePdfText pdfText = new UnicodePdfText();
        UnicodePdfEncoding encoding = new UnicodePdfEncoding();
        UnicodePdfDiagnostics diagnostics = new UnicodePdfDiagnostics();
        string pdfDocText = pdfText.PrepareAccessibilityText("Cafe\u0301", context);
        string utf16Text = pdfText.PrepareAccessibilityText("A\u0100", context);

        UnicodePdfDiagnosticResult pdfDocResult = diagnostics.CheckAccessibilityText(pdfDocText, context);
        UnicodePdfDiagnosticResult utf16Result = diagnostics.CheckAccessibilityText(utf16Text, context);

        Assert.That(encoding.GetTextStringEncoding(pdfDocText), Is.EqualTo(UnicodePdfTextEncoding.PdfDocEncoding));
        Assert.That(encoding.EncodeTextString(pdfDocText), Is.EqualTo(new byte[] { 0x43, 0x61, 0x66, 0xE9 }));
        Assert.That(pdfDocResult.IsValid, Is.True);
        Assert.That(pdfDocResult.Status, Is.EqualTo(UnicodePdfDiagnosticStatus.Valid));
        Assert.That(encoding.GetTextStringEncoding(utf16Text), Is.EqualTo(UnicodePdfTextEncoding.Utf16BeWithBom));
        Assert.That(encoding.EncodeTextString(utf16Text), Is.EqualTo(new byte[] { 0xFE, 0xFF, 0x00, 0x41, 0x01, 0x00 }));
        Assert.That(utf16Result.IsValid, Is.True);
        Assert.That(utf16Result.Status, Is.EqualTo(UnicodePdfDiagnosticStatus.RequiresUtf16Be));
        Assert.That(utf16Result.Diagnostics, Has.Length.EqualTo(1));
        Assert.That(utf16Result.Diagnostics[0].Rule, Is.EqualTo(UnicodePdfDiagnosticRules.RequiresUtf16BeRule));
    }

    [Test]
    public void AccessibilityProfile_RejectsEveryProhibitedC0Control()
    {
        UnicodePdfText pdfText = new UnicodePdfText();
        UnicodePdfDiagnostics diagnostics = new UnicodePdfDiagnostics();
        int codePoint = 0;

        while (codePoint < 0x20)
        {
            string text = char.ConvertFromUtf32(codePoint);
            bool permitted = codePoint == 0x09 || codePoint == 0x0A || codePoint == 0x0D;
            UnicodePdfDiagnosticResult result = diagnostics.CheckAccessibilityText(
                text,
                UnicodePdfTextContext.Metadata);

            Assert.That(
                pdfText.ContainsOnlyValidAccessibilityScalars(text, UnicodePdfTextContext.Metadata),
                Is.EqualTo(permitted),
                "U+" + codePoint.ToString("X4"));
            if (permitted)
            {
                Assert.That(result.IsValid, Is.True, "U+" + codePoint.ToString("X4"));
            }
            else
            {
                Assert.That(result.IsValid, Is.False, "U+" + codePoint.ToString("X4"));
                Assert.That(result.Status, Is.EqualTo(UnicodePdfDiagnosticStatus.ProhibitedTextScalar));
                Assert.That(FindRule(result, UnicodePdfDiagnosticRules.ProhibitedTextScalarRule).CodePoint, Is.EqualTo(codePoint));
            }

            codePoint = codePoint + 1;
        }
    }

    [Test]
    public void AccessibilityDiagnostics_DistinguishMalformedAndExplicitReplacementText()
    {
        UnicodePdfDiagnostics diagnostics = new UnicodePdfDiagnostics();

        UnicodePdfDiagnosticResult malformed = diagnostics.CheckAccessibilityText(
            "A\uD800B",
            UnicodePdfTextContext.ActualText);
        UnicodePdfDiagnosticResult replacement = diagnostics.CheckAccessibilityText(
            "A\uFFFDB",
            UnicodePdfTextContext.ActualText);

        Assert.That(malformed.Status, Is.EqualTo(UnicodePdfDiagnosticStatus.InvalidTextScalar));
        Assert.That(malformed.Diagnostics, Has.Length.EqualTo(1));
        AssertDiagnosticLocation(
            malformed.Diagnostics[0],
            1,
            1,
            UnicodeScalar.ReplacementCharacter,
            UnicodePdfDiagnosticRules.InvalidTextScalarRule);
        Assert.That(replacement.Status, Is.EqualTo(UnicodePdfDiagnosticStatus.ReplacementTextScalar));
        AssertDiagnosticLocation(
            FindRule(replacement, UnicodePdfDiagnosticRules.ReplacementTextScalarRule),
            1,
            1,
            UnicodeScalar.ReplacementCharacter,
            UnicodePdfDiagnosticRules.ReplacementTextScalarRule);
        Assert.That(HasRule(replacement, UnicodePdfDiagnosticRules.InvalidTextScalarRule), Is.False);
    }

    [Test]
    public void PdfConformance_ValidatesExtractionMappingsAndMissingUnicodeData()
    {
        IUnicodePdfGlyphMap validMap = new SnapshotGlyphMap(
            "ExtractionFont",
            new UnicodePdfGlyphMapping[]
            {
                new UnicodePdfGlyphMapping(1, new int[] { 0x0041 }),
                new UnicodePdfGlyphMapping(2, new int[] { 0x0066, 0x0069 }),
                new UnicodePdfGlyphMapping(3, new int[] { 0x1F600 }),
            });
        UnicodePdfGlyphMapping missingScalars = new UnicodePdfGlyphMapping(4, new int[] { 0x0042 });
        missingScalars.UnicodeCodePoints = new int[] { };
        IUnicodePdfGlyphMap missingDataMap = new SnapshotGlyphMap(
            "MissingDataFont",
            new UnicodePdfGlyphMapping[] { missingScalars });
        IUnicodePdfGlyphMap absentDataMap = new SnapshotGlyphMap("AbsentDataFont", null);

        UnicodePdfDiagnosticResult valid = CheckMap(validMap, true);
        UnicodePdfDiagnosticResult missingData = CheckMap(missingDataMap, true);
        UnicodePdfDiagnosticResult absentData = CheckMap(absentDataMap, true);

        Assert.That(valid.IsValid, Is.True);
        Assert.That(valid.Diagnostics, Is.Empty);
        Assert.That(missingData.IsValid, Is.False);
        Assert.That(missingData.Status, Is.EqualTo(UnicodePdfDiagnosticStatus.InvalidUnicodeMap));
        Assert.That(FindRule(missingData, UnicodePdfDiagnosticRules.InvalidUnicodeMapRule).CharacterCode, Is.EqualTo(4));
        Assert.That(absentData.IsValid, Is.False);
        Assert.That(absentData.Status, Is.EqualTo(UnicodePdfDiagnosticStatus.InvalidUnicodeMap));
        Assert.That(HasRule(absentData, UnicodePdfDiagnosticRules.InvalidUnicodeMapRule), Is.True);
    }

    [Test]
    public void PdfConformance_DistinguishesAbsentOptionalRequiredAndEmptyMaps()
    {
        IUnicodePdfGlyphMap emptyMap = new SnapshotGlyphMap(
            "EmptyFont",
            new UnicodePdfGlyphMapping[] { });

        UnicodePdfDiagnosticResult absentOptional = CheckMap(null, false);
        UnicodePdfDiagnosticResult absentRequired = CheckMap(null, true);
        UnicodePdfDiagnosticResult emptyOptional = CheckMap(emptyMap, false);
        UnicodePdfDiagnosticResult emptyRequired = CheckMap(emptyMap, true);

        Assert.That(absentOptional.IsValid, Is.True);
        Assert.That(absentOptional.Diagnostics, Is.Empty);
        Assert.That(absentRequired.IsValid, Is.False);
        Assert.That(absentRequired.Status, Is.EqualTo(UnicodePdfDiagnosticStatus.MissingUnicodeMap));
        Assert.That(emptyOptional.IsValid, Is.False);
        Assert.That(emptyOptional.Status, Is.EqualTo(UnicodePdfDiagnosticStatus.MissingUnicodeMap));
        Assert.That(emptyRequired.IsValid, Is.False);
        Assert.That(emptyRequired.Status, Is.EqualTo(UnicodePdfDiagnosticStatus.MissingUnicodeMap));
    }

    [Test]
    public void PdfConformance_AggregatesDiagnosticsBySeverityInsteadOfEmissionOrder()
    {
        IUnicodePdfGlyphMap invalidMap = new SnapshotGlyphMap(
            "DuplicateFont",
            new UnicodePdfGlyphMapping[]
            {
                new UnicodePdfGlyphMapping(1, new int[] { 0x0041 }),
                new UnicodePdfGlyphMapping(1, new int[] { 0x0042 }),
            });
        IUnicodePdfGlyphMap validMap = new SnapshotGlyphMap(
            "ValidFont",
            new UnicodePdfGlyphMapping[]
            {
                new UnicodePdfGlyphMapping(1, new int[] { 0x0100 }),
            });

        AssertSeverity("\uD800", true, null, UnicodePdfDiagnosticStatus.InvalidTextScalar);
        AssertSeverity("\uFFFD", true, invalidMap, UnicodePdfDiagnosticStatus.ReplacementTextScalar);
        AssertSeverity("\u0001", true, invalidMap, UnicodePdfDiagnosticStatus.ProhibitedTextScalar);
        AssertSeverity("A\u0300", true, invalidMap, UnicodePdfDiagnosticStatus.InvalidUnicodeMap);
        AssertSeverity("\u0100", true, null, UnicodePdfDiagnosticStatus.MissingUnicodeMap);
        AssertSeverity("\u0100", true, validMap, UnicodePdfDiagnosticStatus.RequiresUtf16Be);
        AssertSeverity("Plain", false, null, UnicodePdfDiagnosticStatus.Valid);
    }

    [Test]
    public void EveryPublicPdfDiagnosticRule_IsEmittedByItsBehavior()
    {
        UnicodePdfDiagnostics diagnostics = new UnicodePdfDiagnostics();
        IUnicodePdfGlyphMap invalidMap = new SnapshotGlyphMap(
            "DuplicateFont",
            new UnicodePdfGlyphMapping[]
            {
                new UnicodePdfGlyphMapping(1, new int[] { 0x0041 }),
                new UnicodePdfGlyphMapping(1, new int[] { 0x0042 }),
            });

        UnicodePdfDiagnosticResult invalidScalar = diagnostics.CheckTextString("\uD800");
        UnicodePdfDiagnosticResult actualText = diagnostics.CheckTextString("A\u0300");
        UnicodePdfDiagnosticResult missingMap = CheckMap(null, true);
        UnicodePdfDiagnosticResult invalidMapResult = CheckMap(invalidMap, true);
        UnicodePdfDiagnosticResult accessibility = diagnostics.CheckAccessibilityText(
            "A\u0300",
            UnicodePdfTextContext.AltText);
        UnicodePdfDiagnosticResult prohibited = diagnostics.CheckAccessibilityText(
            "\u0001",
            UnicodePdfTextContext.Metadata);
        UnicodePdfDiagnosticResult replacement = diagnostics.CheckAccessibilityText(
            "\uFFFD",
            UnicodePdfTextContext.StructureElement);

        Assert.That(HasRule(invalidScalar, UnicodePdfDiagnosticRules.InvalidTextScalarRule), Is.True);
        Assert.That(HasRule(actualText, UnicodePdfDiagnosticRules.NeedsActualTextPreparationRule), Is.True);
        Assert.That(HasRule(actualText, UnicodePdfDiagnosticRules.RequiresUtf16BeRule), Is.True);
        Assert.That(HasRule(missingMap, UnicodePdfDiagnosticRules.MissingUnicodeMapRule), Is.True);
        Assert.That(HasRule(invalidMapResult, UnicodePdfDiagnosticRules.InvalidUnicodeMapRule), Is.True);
        Assert.That(HasRule(accessibility, UnicodePdfDiagnosticRules.NeedsAccessibilityTextPreparationRule), Is.True);
        Assert.That(HasRule(prohibited, UnicodePdfDiagnosticRules.ProhibitedTextScalarRule), Is.True);
        Assert.That(HasRule(replacement, UnicodePdfDiagnosticRules.ReplacementTextScalarRule), Is.True);
    }

    static UnicodePdfDiagnosticResult CheckMap(IUnicodePdfGlyphMap? glyphMap, bool required)
    {
        return new UnicodePdfDiagnostics().CheckPdfConformance(
            "Plain",
            UnicodePdfTextContext.ActualText,
            UnicodePdfConformance.PdfAAndPdfUa,
            required,
            glyphMap);
    }

    static void AssertSeverity(
        string text,
        bool required,
        IUnicodePdfGlyphMap? glyphMap,
        UnicodePdfDiagnosticStatus expected)
    {
        UnicodePdfDiagnosticResult result = new UnicodePdfDiagnostics().CheckPdfConformance(
            text,
            UnicodePdfTextContext.ActualText,
            UnicodePdfConformance.PdfAAndPdfUa,
            required,
            glyphMap);

        Assert.That(result.Status, Is.EqualTo(expected), text);
        Assert.That(result.IsValid, Is.EqualTo(expected == UnicodePdfDiagnosticStatus.Valid || expected == UnicodePdfDiagnosticStatus.RequiresUtf16Be));
    }

    static bool HasRule(UnicodePdfDiagnosticResult result, string rule)
    {
        int index = 0;
        while (index < result.Diagnostics.Length)
        {
            if (result.Diagnostics[index].Rule == rule)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static UnicodePdfDiagnostic FindRule(UnicodePdfDiagnosticResult result, string rule)
    {
        int index = 0;
        while (index < result.Diagnostics.Length)
        {
            if (result.Diagnostics[index].Rule == rule)
            {
                return result.Diagnostics[index];
            }

            index = index + 1;
        }

        Assert.Fail("Missing diagnostic rule " + rule);
        return result.Diagnostics[0];
    }

    static void AssertDiagnosticLocation(
        UnicodePdfDiagnostic diagnostic,
        int utf16Index,
        int utf16Length,
        int codePoint,
        string rule)
    {
        Assert.That(diagnostic.Utf16Index, Is.EqualTo(utf16Index));
        Assert.That(diagnostic.Utf16Length, Is.EqualTo(utf16Length));
        Assert.That(diagnostic.CodePoint, Is.EqualTo(codePoint));
        Assert.That(diagnostic.Rule, Is.EqualTo(rule));
    }

    class SnapshotGlyphMap : IUnicodePdfGlyphMap
    {
        string fontName;
        UnicodePdfGlyphMapping[]? mappings;

        public SnapshotGlyphMap(string fontName, UnicodePdfGlyphMapping[]? mappings)
        {
            this.fontName = fontName;
            this.mappings = mappings;
        }

        public string GetFontName()
        {
            return fontName;
        }

        public UnicodePdfGlyphMapping[]? GetMappings()
        {
            return mappings;
        }
    }
}
