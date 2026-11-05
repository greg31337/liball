using System.Globalization;
using System.Text;
using ttf;

namespace ttf.Tests;

internal class MinimalPdfAssemblerTests
{
    [Test]
    public void PublicArtifactsAssembleDeterministicCrossReferencedPdfs()
    {
        FontInstance trueType = HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
        FontInstance cff = FontInstanceCffTests.CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            CffCharsetEncodingTests.BuildCidCff(false),
            4).CreateDefaultInstance();

        PdfFontOptions verticalOptions = PdfFontOptions.Create();
        verticalOptions.SetWritingMode(PdfWritingMode.Vertical);
        PdfFontOptions openTypeOptions = PdfFontOptions.Create();
        openTypeOptions.SetFontKind(PdfFontKind.Type0OpenType);
        openTypeOptions.SetVersion(PdfVersion.Pdf1_6);
        PdfFontOptions simpleOptions = PdfFontOptions.Create();
        simpleOptions.SetFontKind(PdfFontKind.SimpleTrueType);

        PdfFontResource[] resources = new PdfFontResource[]
        {
            BuildResource(trueType, PdfFontOptions.Create(), 4),
            BuildResource(trueType, verticalOptions, 4),
            BuildResource(cff, PdfFontOptions.Create(), 3),
            BuildResource(trueType, openTypeOptions, 4),
            BuildResource(trueType, simpleOptions, 4)
        };
        byte[][] codes = new byte[][]
        {
            new byte[] { 0, 1 },
            new byte[] { 0, 1 },
            new byte[] { 0, 1 },
            new byte[] { 0, 1 },
            new byte[] { 1 }
        };
        string[] requiredMarkers = new string[]
        {
            "/Subtype /CIDFontType2",
            "/Encoding /Identity-V",
            "/Subtype /CIDFontType0",
            "/Subtype /OpenType",
            "/Subtype /TrueType"
        };
        int index = 0;

        while (index < resources.Length)
        {
            byte[] first = MinimalPdfAssembler.Assemble(
                resources[index],
                codes[index]);
            byte[] second = MinimalPdfAssembler.Assemble(
                resources[index],
                codes[index]);
            string text = Encoding.Latin1.GetString(first);

            Assert.Multiple(() =>
            {
                Assert.That(second, Is.EqualTo(first));
                Assert.That(text, Does.StartWith("%PDF-1.7\n%"));
                Assert.That(text, Does.Contain("/Type /Catalog"));
                Assert.That(text, Does.Contain("/Type /Page"));
                Assert.That(text, Does.Contain("/ToUnicode"));
                Assert.That(text, Does.Contain(requiredMarkers[index]));
                Assert.That(text, Does.EndWith("%%EOF\n"));
                Assert.That(
                    Contains(first, resources[index].CopyEmbeddedFont()),
                    Is.True);
            });
            ValidateCrossReferences(first);
            index += 1;
        }

        string vertical = Encoding.Latin1.GetString(
            MinimalPdfAssembler.Assemble(resources[1], codes[1]));

        Assert.Multiple(() =>
        {
            Assert.That(vertical, Does.Contain("/DW2 ["));
            Assert.That(vertical, Does.Contain("/W2 ["));
        });
    }

    static PdfFontResource BuildResource(
        FontInstance instance,
        PdfFontOptions options,
        int glyphId)
    {
        PdfFontBuilder builder = new PdfFontBuilder(instance, options);
        builder.AddGlyph(glyphId, new int[] { 0x41 });
        return builder.FinalizeFont();
    }

    static void ValidateCrossReferences(byte[] pdf)
    {
        string text = Encoding.Latin1.GetString(pdf);
        int startMarker = text.LastIndexOf(
            "startxref\n",
            StringComparison.Ordinal);
        int offsetStart = startMarker + "startxref\n".Length;
        int offsetEnd = text.IndexOf('\n', offsetStart);
        int xrefOffset = int.Parse(
            text.Substring(offsetStart, offsetEnd - offsetStart),
            CultureInfo.InvariantCulture);

        Assert.That(text.Substring(xrefOffset), Does.StartWith("xref\n"));
        int xrefEnd = text.IndexOf("trailer\n", xrefOffset,
            StringComparison.Ordinal);
        string[] lines = text.Substring(
            xrefOffset,
            xrefEnd - xrefOffset).Split('\n');
        string[] header = lines[1].Split(' ');
        int objectCount = int.Parse(header[1], CultureInfo.InvariantCulture);
        int objectNumber = 1;

        while (objectNumber < objectCount)
        {
            int objectOffset = int.Parse(
                lines[2 + objectNumber].Substring(0, 10),
                CultureInfo.InvariantCulture);
            Assert.That(
                text.Substring(objectOffset),
                Does.StartWith(objectNumber.ToString(
                    CultureInfo.InvariantCulture) + " 0 obj\n"));
            objectNumber += 1;
        }
    }

    static bool Contains(byte[] value, byte[] expected)
    {
        int start = 0;

        while (start <= value.Length - expected.Length)
        {
            int index = 0;

            while (index < expected.Length &&
                value[start + index] == expected[index])
            {
                index += 1;
            }

            if (index == expected.Length)
            {
                return true;
            }

            start += 1;
        }

        return false;
    }
}
