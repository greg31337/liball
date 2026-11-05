using System.Text;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfUnicodeMapTests
{
    [Test]
    public void EncodeTextString_KeepsAsciiLiteralStringsEscaped()
    {
        string encoded = PdfUnicodeMap.EncodeTextString("A (test) \\ value");

        Assert.That(encoded, Is.EqualTo("(A \\(test\\) \\\\ value)"));
        Assert.That(PdfUnicodeMap.NeedsUnicodeEncoding("Plain ASCII"), Is.False);
    }

    [Test]
    public void EncodeTextString_WritesUtf16HexForUnicodeText()
    {
        string encoded = PdfUnicodeMap.EncodeTextString("Cafe \u03c0");

        Assert.That(encoded, Is.EqualTo("<FEFF0043006100660065002003C0>"));
        Assert.That(PdfUnicodeMap.EncodeUtf16Hex("Hi"), Is.EqualTo("FEFF00480069"));
        Assert.That(PdfUnicodeMap.NeedsUnicodeEncoding("Cafe \u03c0"), Is.True);
    }

    [Test]
    public void Canvas_DrawTextUsesUtf16HexForUnicodeText()
    {
        PdfCanvas canvas = new PdfCanvas();

        canvas.DrawText("Cafe \u03c0", 10, 20, 12);

        string content = canvas.ToPdfString();
        Assert.That(content, Does.Contain("/F1 12 Tf"));
        Assert.That(content, Does.Contain("<FEFF0043006100660065002003C0> Tj"));
    }

    [Test]
    public void Save_WritesUnicodeMetadataAsUtf16Hex()
    {
        PdfDocument document = new PdfDocument();
        PdfMetadata metadata = new PdfMetadata();
        metadata.Title = "Cafe \u03c0";
        document.SetMetadata(metadata);
        document.AddPage(200, 100);

        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(pdf, Does.Contain("/Title <FEFF0043006100660065002003C0>"));
    }

    [Test]
    public void ToPdfCMap_WritesToUnicodeMappings()
    {
        PdfUnicodeMap map = new PdfUnicodeMap();

        map.Add(65, 65);
        map.Add(960, 960);

        string cmap = map.ToPdfCMap();
        Assert.That(cmap, Does.Contain("/CMapName /Adobe-Identity-UCS def"));
        Assert.That(cmap, Does.Contain("2 beginbfchar"));
        Assert.That(cmap, Does.Contain("<0041> <0041>"));
        Assert.That(cmap, Does.Contain("<03C0> <03C0>"));
        Assert.That(cmap, Does.Contain("endcmap"));
    }

    [Test]
    public void AddText_AddsDistinctUnicodeMappings()
    {
        PdfUnicodeMap map = new PdfUnicodeMap();

        map.AddText("ABA");

        Assert.That(map.Entries.Count, Is.EqualTo(2));
        Assert.That(map.Entries[0].CharacterCode, Is.EqualTo(65));
        Assert.That(map.Entries[0].UnicodeValue, Is.EqualTo(65));
        Assert.That(map.Entries[1].CharacterCode, Is.EqualTo(66));
        Assert.That(map.Entries[1].UnicodeValue, Is.EqualTo(66));
    }

    [Test]
    public void ToPdfCMap_SplitsLargeMappingSetsIntoValidBlocks()
    {
        PdfUnicodeMap map = new PdfUnicodeMap();
        for (int value = 1; value <= 205; value++)
        {
            map.Add(value, value);
        }

        string cmap = map.ToPdfCMap();

        Assert.That(CountOccurrences(cmap, "100 beginbfchar"), Is.EqualTo(2));
        Assert.That(cmap, Does.Contain("5 beginbfchar"));
        Assert.That(cmap, Does.Contain("<00CD> <00CD>"));
    }

    [Test]
    public void UnicodeMap_RejectsInvalidInputs()
    {
        PdfUnicodeMap map = new PdfUnicodeMap();

        Assert.Throws<ArgumentNullException>(() => PdfUnicodeMap.EncodeTextString(null!));
        Assert.Throws<ArgumentNullException>(() => PdfUnicodeMap.EncodeUtf16Hex(null!));
        Assert.Throws<ArgumentNullException>(() => PdfUnicodeMap.NeedsUnicodeEncoding(null!));
        Assert.Throws<ArgumentNullException>(() => map.AddText(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => map.Add(-1, 65));
        Assert.Throws<ArgumentOutOfRangeException>(() => map.Add(65536, 65));
        Assert.Throws<ArgumentOutOfRangeException>(() => map.Add(65, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => map.Add(65, 1114112));
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
