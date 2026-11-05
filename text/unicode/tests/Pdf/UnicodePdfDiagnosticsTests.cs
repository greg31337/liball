using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodePdfDiagnosticsTests
{
    [Test]
    public void CheckTextString_AcceptsPreparedPdfDocEncodedText()
    {
        UnicodePdfDiagnosticResult result = new UnicodePdfDiagnostics().CheckTextString("Plain text");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Status, Is.EqualTo(UnicodePdfDiagnosticStatus.Valid));
        Assert.That(result.Diagnostics, Is.Empty);
    }

    [Test]
    public void CheckTextString_ReportsUtf16BeRequirementAsAdvisoryDiagnostic()
    {
        UnicodePdfDiagnosticResult result = new UnicodePdfDiagnostics().CheckTextString("A\u0100");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Status, Is.EqualTo(UnicodePdfDiagnosticStatus.RequiresUtf16Be));
        Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
        AssertDiagnostic(
            result.Diagnostics[0],
            1,
            1,
            0x0100,
            -1,
            -1,
            UnicodePdfDiagnosticStatus.RequiresUtf16Be,
            UnicodePdfDiagnostics.RequiresUtf16BeRule);
    }

    [Test]
    public void CheckTextString_RejectsInvalidUtf16Input()
    {
        UnicodePdfDiagnosticResult result = new UnicodePdfDiagnostics().CheckTextString("\uD800A");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Status, Is.EqualTo(UnicodePdfDiagnosticStatus.InvalidTextScalar));
        Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
        AssertDiagnostic(
            result.Diagnostics[0],
            0,
            1,
            UnicodeScalar.ReplacementCharacter,
            -1,
            -1,
            UnicodePdfDiagnosticStatus.InvalidTextScalar,
            UnicodePdfDiagnostics.InvalidTextScalarRule);
    }

    [Test]
    public void CheckTextString_RejectsUnpreparedActualTextAndStillReportsEncoding()
    {
        UnicodePdfDiagnosticResult result = new UnicodePdfDiagnostics().CheckTextString("A\u0300");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Status, Is.EqualTo(UnicodePdfDiagnosticStatus.NeedsActualTextPreparation));
        Assert.That(result.Diagnostics, Has.Length.EqualTo(2));
        AssertDiagnostic(
            result.Diagnostics[0],
            0,
            2,
            -1,
            -1,
            -1,
            UnicodePdfDiagnosticStatus.NeedsActualTextPreparation,
            UnicodePdfDiagnostics.NeedsActualTextPreparationRule);
        AssertDiagnostic(
            result.Diagnostics[1],
            1,
            1,
            0x0300,
            -1,
            -1,
            UnicodePdfDiagnosticStatus.RequiresUtf16Be,
            UnicodePdfDiagnostics.RequiresUtf16BeRule);
    }

    [Test]
    public void CheckFontUnicodeMapping_AcceptsValidToUnicodeMap()
    {
        UnicodePdfGlyphMap glyphMap = new UnicodePdfGlyphMap(
            "F1",
            new UnicodePdfGlyphMapping[]
            {
                new UnicodePdfGlyphMapping(1, new int[] { 0x0041 }),
                new UnicodePdfGlyphMapping(2, new int[] { 0x0042 }),
            });

        UnicodePdfDiagnosticResult result = new UnicodePdfDiagnostics().CheckFontUnicodeMapping(glyphMap);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Status, Is.EqualTo(UnicodePdfDiagnosticStatus.Valid));
        Assert.That(result.Diagnostics, Is.Empty);
    }

    [Test]
    public void CheckFontUnicodeMapping_ReportsMissingUnicodeMaps()
    {
        UnicodePdfDiagnosticResult nullMapResult = new UnicodePdfDiagnostics().CheckFontUnicodeMapping(null);
        UnicodePdfDiagnosticResult emptyMapResult = new UnicodePdfDiagnostics().CheckFontUnicodeMapping(
            new UnicodePdfGlyphMap("F1", new UnicodePdfGlyphMapping[] { }));

        Assert.That(nullMapResult.IsValid, Is.False);
        Assert.That(nullMapResult.Status, Is.EqualTo(UnicodePdfDiagnosticStatus.MissingUnicodeMap));
        AssertDiagnostic(
            nullMapResult.Diagnostics[0],
            -1,
            -1,
            -1,
            -1,
            -1,
            UnicodePdfDiagnosticStatus.MissingUnicodeMap,
            UnicodePdfDiagnostics.MissingUnicodeMapRule);

        Assert.That(emptyMapResult.IsValid, Is.False);
        Assert.That(emptyMapResult.Status, Is.EqualTo(UnicodePdfDiagnosticStatus.MissingUnicodeMap));
        Assert.That(emptyMapResult.Diagnostics[0].Rule, Is.EqualTo(UnicodePdfDiagnostics.MissingUnicodeMapRule));
    }

    [Test]
    public void CheckFontUnicodeMapping_ReportsInvalidUnicodeMaps()
    {
        UnicodePdfGlyphMap glyphMap = new UnicodePdfGlyphMap(
            "F1",
            new UnicodePdfGlyphMapping[]
            {
                new UnicodePdfGlyphMapping(1, new int[] { 0x0041 }),
                new UnicodePdfGlyphMapping(1, new int[] { 0x0042 }),
            });

        UnicodePdfDiagnosticResult result = new UnicodePdfDiagnostics().CheckFontUnicodeMapping(glyphMap);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Status, Is.EqualTo(UnicodePdfDiagnosticStatus.InvalidUnicodeMap));
        AssertDiagnostic(
            result.Diagnostics[0],
            -1,
            -1,
            -1,
            1,
            1,
            UnicodePdfDiagnosticStatus.InvalidUnicodeMap,
            UnicodePdfDiagnostics.InvalidUnicodeMapRule);
    }

    [Test]
    public void PdfDiagnosticHelpers_RejectInvalidArguments()
    {
        System.Collections.Generic.List<UnicodePdfDiagnostic> diagnostics = new System.Collections.Generic.List<UnicodePdfDiagnostic>();
        diagnostics.Add(null!);

        Assert.Throws<ArgumentNullException>(() => new UnicodePdfDiagnostics().CheckTextString(null!));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfDiagnostics.AddTextStringDiagnostics(null!, new System.Collections.Generic.List<UnicodePdfDiagnostic>()));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfDiagnostics.AddTextStringDiagnostics("A", null!));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfDiagnostics.AddInvalidTextScalarDiagnostics(null!, new System.Collections.Generic.List<UnicodePdfDiagnostic>()));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfDiagnostics.AddInvalidTextScalarDiagnostics(new UnicodeScalarRecord[] { }, null!));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfDiagnostics.AddActualTextPreparationDiagnostic(null!, new System.Collections.Generic.List<UnicodePdfDiagnostic>()));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfDiagnostics.AddActualTextPreparationDiagnostic("A", null!));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfDiagnostics.AddTextStringEncodingDiagnostic(null!, new System.Collections.Generic.List<UnicodePdfDiagnostic>()));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfDiagnostics.AddTextStringEncodingDiagnostic(new UnicodeScalarRecord[] { }, null!));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfDiagnostics.FindFirstScalarRequiringUtf16Be(null!));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfDiagnostics.AddFontUnicodeMappingDiagnostics(new UnicodePdfGlyphMap("F1", new UnicodePdfGlyphMapping[] { }), null!));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfDiagnostics.AddCMapValidationDiagnostic(null!, new System.Collections.Generic.List<UnicodePdfDiagnostic>()));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfDiagnostics.AddCMapValidationDiagnostic(new UnicodePdfCMapDiagnostic(0, 0, 0, UnicodePdfCMapStatus.Valid, "rule", "message"), null!));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfDiagnostics.CreateDiagnosticResult(null!));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfDiagnostics.GetDiagnosticStatus(null!));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfDiagnostics.HasHardFailures(null!));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfDiagnostics.HasHardFailures(diagnostics));
        Assert.Throws<ArgumentNullException>(() => new UnicodePdfDiagnosticResult(true, UnicodePdfDiagnosticStatus.Valid, null!));
        Assert.Throws<ArgumentException>(() => new UnicodePdfDiagnostic(0, 0, 0, 0, 0, UnicodePdfDiagnosticStatus.Valid, "", "message"));
        Assert.Throws<ArgumentException>(() => new UnicodePdfDiagnostic(0, 0, 0, 0, 0, UnicodePdfDiagnosticStatus.Valid, "rule", ""));
    }

    static void AssertDiagnostic(
        UnicodePdfDiagnostic diagnostic,
        int utf16Index,
        int utf16Length,
        int codePoint,
        int mappingIndex,
        int characterCode,
        UnicodePdfDiagnosticStatus status,
        string rule)
    {
        Assert.That(diagnostic.Utf16Index, Is.EqualTo(utf16Index));
        Assert.That(diagnostic.Utf16Length, Is.EqualTo(utf16Length));
        Assert.That(diagnostic.CodePoint, Is.EqualTo(codePoint));
        Assert.That(diagnostic.MappingIndex, Is.EqualTo(mappingIndex));
        Assert.That(diagnostic.CharacterCode, Is.EqualTo(characterCode));
        Assert.That(diagnostic.Status, Is.EqualTo(status));
        Assert.That(diagnostic.Rule, Is.EqualTo(rule));
        Assert.That(diagnostic.Message, Is.Not.Empty);
    }
}
