using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodePdfTextSupportIntegrationTests
{
    [Test]
    public void PdfDocEncodingTextString_CanBeEscapedAsPdfLiteralString()
    {
        byte[] bytes = new UnicodePdfEncoding().EncodeTextString("(A\u20AC)\n");

        Assert.That(bytes, Is.EqualTo(new byte[] { 0x28, 0x41, 0xA0, 0x29, 0x0A }));
        Assert.That(new UnicodePdfEncoding().GetTextStringEncoding("(A\u20AC)\n"), Is.EqualTo(UnicodePdfTextEncoding.PdfDocEncoding));
        Assert.That(new UnicodePdfEncoding().DecodeTextString(bytes), Is.EqualTo("(A\u20AC)\n"));
        Assert.That(new UnicodePdfText().EscapeLiteralString(bytes), Is.EqualTo("\\(A\\240\\)\\n"));
        Assert.That(new UnicodePdfText().ToHexString(bytes), Is.EqualTo("2841A0290A"));
    }

    [Test]
    public void Utf16BeTextString_RoundTripsSupplementaryAndBmpScalars()
    {
        string text = "A\U0001F600\u0100";

        byte[] bytes = new UnicodePdfEncoding().EncodeTextString(text);

        Assert.That(new UnicodePdfEncoding().GetTextStringEncoding(text), Is.EqualTo(UnicodePdfTextEncoding.Utf16BeWithBom));
        Assert.That(bytes, Is.EqualTo(new byte[]
        {
            0xFE,
            0xFF,
            0x00,
            0x41,
            0xD8,
            0x3D,
            0xDE,
            0x00,
            0x01,
            0x00,
        }));
        Assert.That(new UnicodePdfEncoding().DecodeTextString(bytes), Is.EqualTo(text));
        Assert.That(new UnicodePdfText().ToHexString(bytes), Is.EqualTo("FEFF0041D83DDE000100"));
    }

    [Test]
    public void ActualTextPreparation_NormalizesThenEncodesAsPdfTextString()
    {
        string prepared = new UnicodePdfText().PrepareActualText("A\u0300 \uD800");
        UnicodePdfDiagnosticResult diagnosticResult = new UnicodePdfDiagnostics().CheckTextString(prepared);
        byte[] bytes = new UnicodePdfEncoding().EncodeTextString(prepared);

        Assert.That(prepared, Is.EqualTo("\u00C0 \uFFFD"));
        Assert.That(new UnicodePdfText().IsActualTextPrepared(prepared), Is.True);
        Assert.That(diagnosticResult.IsValid, Is.True);
        Assert.That(diagnosticResult.Status, Is.EqualTo(UnicodePdfDiagnosticStatus.RequiresUtf16Be));
        Assert.That(new UnicodePdfEncoding().DecodeTextString(bytes), Is.EqualTo(prepared));
    }

    [Test]
    public void ToUnicodeCMapGeneration_ProducesSearchableGlyphMappings()
    {
        UnicodePdfGlyphMap glyphMap = new UnicodePdfGlyphMap(
            "Subset F1",
            new UnicodePdfGlyphMapping[]
            {
                new UnicodePdfGlyphMapping(0x0020, new int[] { 0x0041 }),
                new UnicodePdfGlyphMapping(0x0021, new int[] { 0x0042 }),
                new UnicodePdfGlyphMapping(0x0030, new int[] { 0x0066, 0x0069 }),
                new UnicodePdfGlyphMapping(0x0100, new int[] { 0x1F600 }),
            });

        UnicodePdfCMapValidationResult validationResult = new UnicodePdfCMap().ValidateToUnicodeMap(glyphMap);
        UnicodePdfDiagnosticResult diagnosticResult = new UnicodePdfDiagnostics().CheckFontUnicodeMapping(glyphMap);
        string cmap = new UnicodePdfCMap().CreateToUnicodeMap(glyphMap);

        Assert.That(validationResult.IsValid, Is.True);
        Assert.That(diagnosticResult.IsValid, Is.True);
        Assert.That(cmap, Does.Contain("/CMapName /Subset#20F1-ToUnicode def\n"));
        Assert.That(cmap, Does.Contain("1 beginbfrange\n<0020> <0021> <0041>\nendbfrange\n"));
        Assert.That(cmap, Does.Contain("2 beginbfchar\n<0030> <00660069>\n<0100> <D83DDE00>\nendbfchar\n"));
    }

    [Test]
    public void ToUnicodeCMapGeneration_RejectsReplacementCharacterMappings()
    {
        UnicodePdfGlyphMap glyphMap = new UnicodePdfGlyphMap(
            "F1",
            new UnicodePdfGlyphMapping[]
            {
                new UnicodePdfGlyphMapping(0x01, new int[] { UnicodeScalar.ReplacementCharacter }),
            });

        UnicodePdfCMapValidationResult validationResult = new UnicodePdfCMap().ValidateToUnicodeMap(glyphMap);
        UnicodePdfDiagnosticResult diagnosticResult = new UnicodePdfDiagnostics().CheckFontUnicodeMapping(glyphMap);

        Assert.That(validationResult.IsValid, Is.False);
        Assert.That(validationResult.Status, Is.EqualTo(UnicodePdfCMapStatus.ReplacementCharacter));
        Assert.That(diagnosticResult.IsValid, Is.False);
        Assert.That(diagnosticResult.Status, Is.EqualTo(UnicodePdfDiagnosticStatus.InvalidUnicodeMap));
        Assert.Throws<ArgumentException>(() => new UnicodePdfCMap().CreateToUnicodeMap(glyphMap));
    }
}
