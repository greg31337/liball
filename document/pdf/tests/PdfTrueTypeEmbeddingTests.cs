using System.Text;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfTrueTypeEmbeddingTests
{
    [Test]
    public void Canvas_EncodesGlyphIdsAndMeasuresLoadedTrueTypeMetrics()
    {
        PdfTrueTypeFont font = LoadFont(0, false);
        IPdfCanvas canvas = new PdfCanvas();

        canvas.DrawTextWithTrueTypeFont("AC", 10, 20, 12, font);
        double width = canvas.MeasureTrueTypeText("AC", 12, font);

        Assert.That(canvas.ToPdfString(), Is.EqualTo(
            "BT\n/" + font.ResourceName + " 12 Tf\n10 20 Td\n<00010003> Tj\nET\n"));
        Assert.That(width, Is.EqualTo(15.6).Within(0.0001));
        Assert.That(((PdfCanvas)canvas).TrueTypeFontUsages.Count, Is.EqualTo(1));
        Assert.That(((PdfCanvas)canvas).TrueTypeFontUsages[0].GlyphIndices, Is.EqualTo(new[] { 1, 3 }));
    }

    [Test]
    public void Canvas_ExplicitTextOperationsUseSelectedTrueTypeFont()
    {
        PdfTrueTypeFont font = LoadFont(0, false);
        IPdfCanvas canvas = new PdfCanvas();

        canvas.BeginText();
        canvas.SetTextTrueTypeFont(font, 9);
        canvas.ShowText("B");
        canvas.EndText();

        Assert.That(canvas.ToPdfString(), Is.EqualTo("BT\n/" + font.ResourceName + " 9 Tf\n<0002> Tj\nET\n"));
    }

    [Test]
    public void Canvas_RejectsMissingGlyphRestrictedEmbeddingAndOpenTypeCff()
    {
        PdfTrueTypeFont ordinary = LoadFont(0, false);
        PdfTrueTypeFont restricted = LoadFont(0x0002, false);
        PdfTrueTypeFont openType = LoadFont(0, true);
        IPdfCanvas canvas = new PdfCanvas();

        Assert.Throws<ArgumentException>(() => canvas.DrawTextWithTrueTypeFont("Z", 0, 0, 10, ordinary));
        Assert.Throws<InvalidOperationException>(() => canvas.DrawTextWithTrueTypeFont("A", 0, 0, 10, restricted));
        Assert.Throws<InvalidOperationException>(() => canvas.DrawTextWithTrueTypeFont("😀", 0, 0, 10, openType));
    }

    [Test]
    public void Subsetter_IncludesCompositeDependenciesAndDropsUnusedGlyphData()
    {
        PdfTrueTypeFont font = LoadFont(0, false);
        PdfTrueTypeFontUsage usage = new PdfTrueTypeFontUsage(font);
        usage.EncodeText("C");

        byte[] subset = PdfTrueTypeSubsetter.CreateFontProgram(usage);
        Dictionary<string, PdfFontTable> tables = PdfTrueTypeFontParser.ReadTableDirectory(subset);
        PdfFontTable head = tables["head"];
        PdfFontTable loca = tables["loca"];
        PdfFontTable glyf = tables["glyf"];
        int format = PdfTrueTypeFontParser.ReadInt16(subset, head.Offset + 50);
        int[] offsets = PdfTrueTypeSubsetter.ReadGlyphOffsets(subset, loca, glyf, font.GlyphCount, format);

        Assert.That(offsets, Is.EqualTo(new[] { 0, 0, 12, 12, 30 }));
        Assert.That(glyf.Length, Is.EqualTo(30));
        Assert.That(subset.Length, Is.LessThan(font.FontData.Length));
        Assert.That(PdfTrueTypeSubsetter.CalculateChecksum(subset), Is.EqualTo(0xB1B0AFBAU));
    }

    [Test]
    public void Subsetter_EmbedsCompleteFontWhenLicenseProhibitsSubsetting()
    {
        PdfTrueTypeFont font = LoadFont(0x0100, false);
        PdfTrueTypeFontUsage usage = new PdfTrueTypeFontUsage(font);
        usage.EncodeText("A");

        byte[] embedded = PdfTrueTypeSubsetter.CreateFontProgram(usage);

        Assert.That(font.EmbeddingAllowed, Is.True);
        Assert.That(font.SubsettingAllowed, Is.False);
        Assert.That(embedded, Is.EqualTo(font.FontData));
        Assert.That(embedded, Is.Not.SameAs(font.FontData));
        Assert.That(PdfTrueTypeSubsetter.CreateEmbeddedFontName(font), Is.EqualTo("TestSans-Regular"));
    }

    [Test]
    public void Writer_SerializesCidFontDescriptorWidthsFontFileAndToUnicodeMap()
    {
        PdfDocument document = new PdfDocument();
        PdfTrueTypeFont font = document.RegisterTrueTypeFont(TestSfntFont.Create(0x00010000U, 0, false));
        PdfPage page = document.AddPage(200, 100);
        page.Canvas.DrawTextWithTrueTypeFont("AC", 10, 20, 12, font);

        string pdf = Encoding.Latin1.GetString(document.ToBytes());
        string baseFontName = PdfTrueTypeSubsetter.CreateSubsetFontName(font);

        Assert.Multiple(() =>
        {
            Assert.That(pdf, Does.Contain("/Subtype /Type0 /BaseFont /" + baseFontName));
            Assert.That(pdf, Does.Contain("/Encoding /Identity-H"));
            Assert.That(pdf, Does.Contain("/Subtype /CIDFontType2 /BaseFont /" + baseFontName));
            Assert.That(pdf, Does.Contain("/CIDToGIDMap /Identity /DW 500 /W [1 [600] 3 [700] ]"));
            Assert.That(pdf, Does.Contain("/Type /FontDescriptor /FontName /" + baseFontName));
            Assert.That(pdf, Does.Contain("/FontBBox [-50 -200 900 800]"));
            Assert.That(pdf, Does.Contain("/Ascent 800 /Descent -200 /CapHeight 700"));
            Assert.That(pdf, Does.Contain("/FontFile2 "));
            Assert.That(pdf, Does.Contain("/Length1 "));
            Assert.That(pdf, Does.Contain("<0001> <0041>"));
            Assert.That(pdf, Does.Contain("<0003> <0043>"));
            Assert.That(pdf, Does.Contain("/" + font.ResourceName + " "));
            Assert.That(pdf, Does.Contain("<00010003> Tj"));
        });
    }

    [Test]
    public void Writer_DeduplicatesOneEmbeddedFontAcrossPagesAndForms()
    {
        PdfDocument document = new PdfDocument();
        PdfTrueTypeFont font = document.RegisterTrueTypeFont(TestSfntFont.Create(0x00010000U, 0, false));
        document.AddPage(100, 100).Canvas.DrawTextWithTrueTypeFont("A", 1, 2, 10, font);
        document.AddPage(100, 100).Canvas.DrawTextWithTrueTypeFont("B", 1, 2, 10, font);
        document.AddForm(20, 20).Canvas.DrawTextWithTrueTypeFont("C", 1, 2, 10, font);

        string pdf = Encoding.Latin1.GetString(document.ToBytes());

        Assert.That(CountOccurrences(pdf, "/Subtype /Type0"), Is.EqualTo(1));
        Assert.That(pdf, Does.Contain("/W [1 [600] 2 [700] 3 [700] ]"));
        Assert.That(pdf, Does.Contain("<0001> <0041>"));
        Assert.That(pdf, Does.Contain("<0002> <0042>"));
        Assert.That(pdf, Does.Contain("<0003> <0043>"));
    }

    [Test]
    public void Writer_ToUnicodeMapsSupplementaryUnicodeAsUtf16SurrogatePair()
    {
        PdfDocument document = new PdfDocument();
        PdfTrueTypeFont font = document.RegisterTrueTypeFont(TestSfntFont.Create(0x00010000U, 0, true));
        document.AddPage(100, 100).Canvas.DrawTextWithTrueTypeFont("😀", 1, 2, 10, font);

        string pdf = Encoding.Latin1.GetString(document.ToBytes());

        Assert.That(pdf, Does.Contain("<0003> <D83DDE00>"));
        Assert.That(pdf, Does.Contain("<0003> Tj"));
    }

    static PdfTrueTypeFont LoadFont(int fsType, bool openType)
    {
        uint signature = openType ? 0x4F54544FU : 0x00010000U;
        return new PdfTrueTypeFontLoader().Load(TestSfntFont.Create(signature, (ushort)fsType, openType));
    }

    static int CountOccurrences(string text, string value)
    {
        int count = 0;
        int index = 0;
        while (index >= 0)
        {
            index = text.IndexOf(value, index, StringComparison.Ordinal);
            if (index >= 0)
            {
                count++;
                index += value.Length;
            }
        }

        return count;
    }
}
