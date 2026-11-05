using System.Text;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfUnicodeTextLayoutTests
{
    [Test]
    public void FallbackChain_UsesFirstFontContainingUnicodeAndDeduplicatesIdentity()
    {
        PdfTrueTypeFont latin = CreateLayoutFont("Latin", new[] { 32, 45, 65, 66 }, new[] { 250, 300, 500, 600 });
        PdfTrueTypeFont emoji = CreateLayoutFont("Emoji", new[] { 0x1F600 }, new[] { 1000 });
        IPdfFontFallbackChain fonts = new PdfFontFallbackChain();

        fonts.AddFont(latin);
        fonts.AddFont(emoji);
        fonts.AddFont(latin);

        Assert.That(fonts.GetFontCount(), Is.EqualTo(2));
        Assert.That(fonts.GetFont(0), Is.SameAs(latin));
        Assert.That(fonts.FindFontForUnicode(65), Is.SameAs(latin));
        Assert.That(fonts.FindFontForUnicode(0x1F600), Is.SameAs(emoji));
        Assert.Throws<ArgumentException>(() => fonts.FindFontForUnicode(90));
        Assert.Throws<ArgumentOutOfRangeException>(() => fonts.GetFont(2));
    }

    [Test]
    public void FallbackChain_RejectsValuesThatAreNotUnicodeScalars()
    {
        PdfFontFallbackChain fonts = CreateChain(CreateLayoutFont("Latin", new[] { 65 }, new[] { 500 }));

        Assert.Throws<ArgumentOutOfRangeException>(() => fonts.FindFontForUnicode(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => fonts.FindFontForUnicode(0xD800));
        Assert.Throws<ArgumentOutOfRangeException>(() => fonts.FindFontForUnicode(0xDFFF));
        Assert.Throws<ArgumentOutOfRangeException>(() => fonts.FindFontForUnicode(0x110000));
    }

    [Test]
    public void Layout_SelectsFallbackRunsAndMeasuresTheirActualMetrics()
    {
        PdfTrueTypeFont latin = CreateLayoutFont("Latin", new[] { 65, 66 }, new[] { 500, 600 });
        PdfTrueTypeFont emoji = CreateLayoutFont("Emoji", new[] { 0x1F600 }, new[] { 1000 });
        PdfFontFallbackChain fonts = CreateChain(latin, emoji);
        PdfUnicodeTextOptions options = CreateOptions(100, 10);

        PdfUnicodeTextResult result = new PdfUnicodeTextLayout().Layout("A😀B", fonts, options);

        Assert.That(result.Lines.Length, Is.EqualTo(1));
        Assert.That(result.Lines[0].Runs.Length, Is.EqualTo(3));
        Assert.That(result.Lines[0].Runs[0].Font, Is.SameAs(latin));
        Assert.That(result.Lines[0].Runs[1].Font, Is.SameAs(emoji));
        Assert.That(result.Lines[0].Runs[2].Font, Is.SameAs(latin));
        Assert.That(result.Lines[0].Width, Is.EqualTo(21).Within(0.0001));
        Assert.That(result.Lines[0].Text, Is.EqualTo("A😀B"));
    }

    [Test]
    public void Layout_NeverSplitsExtendedGraphemeClusters()
    {
        PdfTrueTypeFont font = CreateLayoutFont("Marks", new[] { 45, 101, 120, 0x0301 }, new[] { 300, 500, 500, 0 });
        PdfUnicodeTextOptions options = CreateOptions(5, 10);

        PdfUnicodeTextResult result = new PdfUnicodeTextLayout().Layout("e\u0301x", CreateChain(font), options);

        Assert.That(result.Lines.Select(line => line.Text), Is.EqualTo(new[] { "e\u0301", "x" }));
        Assert.That(result.Lines[0].Width, Is.EqualTo(5).Within(0.0001));
    }

    [Test]
    public void Layout_PreservesGraphemeBoundariesAcrossSpanEdges()
    {
        PdfTrueTypeFont font = CreateLayoutFont("Marks", new[] { 101, 120, 0x0301 }, new[] { 500, 500, 0 });
        PdfUnicodeTextSpan baseLetter = new PdfUnicodeTextSpan("e");
        PdfUnicodeTextSpan combiningMark = new PdfUnicodeTextSpan("\u0301");
        PdfUnicodeTextSpan following = new PdfUnicodeTextSpan("x");

        PdfUnicodeTextResult result = new PdfUnicodeTextLayout().LayoutSpans(
            new[] { baseLetter, combiningMark, following }, CreateChain(font), CreateOptions(5, 10));

        Assert.That(result.Lines.Select(line => line.Text), Is.EqualTo(new[] { "e\u0301", "x" }));
    }

    [Test]
    public void Layout_AppliesCommonUax14CjkAndPunctuationBreakRules()
    {
        PdfTrueTypeFont font = CreateLayoutFont("Cjk", new[] { 0x3002, 0x4F60, 0x597D }, new[] { 1000, 1000, 1000 });
        PdfUnicodeTextOptions options = CreateOptions(20, 10);

        PdfUnicodeTextResult result = new PdfUnicodeTextLayout().Layout("你好。", CreateChain(font), options);

        Assert.That(result.Lines.Select(line => line.Text), Is.EqualTo(new[] { "你", "好。" }));
        Assert.That(result.Lines[1].Width, Is.EqualTo(20).Within(0.0001));
    }

    [Test]
    public void Layout_HandlesMandatoryBreaksAndBlankLines()
    {
        PdfTrueTypeFont font = CreateLayoutFont("Latin", new[] { 65, 66 }, new[] { 500, 500 });

        PdfUnicodeTextResult result = new PdfUnicodeTextLayout().Layout("A\n\nB", CreateChain(font), CreateOptions(100, 10));

        Assert.That(result.Lines.Select(line => line.Text), Is.EqualTo(new[] { "A", string.Empty, "B" }));
        Assert.That(result.Lines[1].Height, Is.EqualTo(12).Within(0.0001));
    }

    [Test]
    public void Layout_ShowsHyphenOnlyWhenBreakingAtSoftHyphen()
    {
        PdfTrueTypeFont font = CreateLatinWordFont();
        PdfUnicodeTextLayout layout = new PdfUnicodeTextLayout();

        PdfUnicodeTextResult wrapped = layout.Layout("co\u00ADoperate", CreateChain(font), CreateOptions(25, 10));
        PdfUnicodeTextResult unwrapped = layout.Layout("co\u00ADoperate", CreateChain(font), CreateOptions(100, 10));

        Assert.That(wrapped.Lines[0].Text, Is.EqualTo("co-"));
        Assert.That(wrapped.Lines.Select(line => line.Text), Is.EqualTo(new[] { "co-", "opera", "te" }));
        Assert.That(unwrapped.Lines[0].Text, Is.EqualTo("cooperate"));
    }

    [Test]
    public void Layout_UsesCallerSuppliedHyphenationPoints()
    {
        PdfUnicodeTextSpan span = new PdfUnicodeTextSpan("cooperate");
        span.HyphenationPoints = new[] { 2 };

        PdfUnicodeTextResult result = new PdfUnicodeTextLayout().LayoutSpans(
            new[] { span }, CreateChain(CreateLatinWordFont()), CreateOptions(25, 10));

        Assert.That(result.Lines[0].Text, Is.EqualTo("co-"));
    }

    [Test]
    public void Layout_ReordersMixedBidirectionalTextByEmbeddingLevels()
    {
        PdfTrueTypeFont font = CreateLayoutFont("Bidi", new[] { 32, 65, 66, 0x05D0, 0x05D1 }, new[] { 250, 500, 500, 500, 500 });
        PdfUnicodeTextLayout layout = new PdfUnicodeTextLayout();
        PdfUnicodeTextOptions automatic = CreateOptions(100, 10);
        PdfUnicodeTextOptions rightToLeft = CreateOptions(100, 10);
        rightToLeft.Direction = PdfTextDirection.RightToLeft;

        PdfUnicodeTextResult ltrResult = layout.Layout("AB אב", CreateChain(font), automatic);
        PdfUnicodeTextResult rtlResult = layout.Layout("אב AB", CreateChain(font), rightToLeft);

        Assert.That(ltrResult.Lines[0].Text, Is.EqualTo("AB בא"));
        Assert.That(ltrResult.Lines[0].Runs.Any(run => run.RightToLeft), Is.True);
        Assert.That(rtlResult.Lines[0].Text, Is.EqualTo("AB בא"));
    }

    [Test]
    public void Layout_AppliesSuperscriptSubscriptAndFootnoteMetrics()
    {
        PdfTrueTypeFont font = CreateLayoutFont("Scripts", new[] { 49, 65, 66, 67 }, new[] { 500, 500, 500, 500 });
        PdfUnicodeTextSpan normal = new PdfUnicodeTextSpan("A");
        PdfUnicodeTextSpan superscript = new PdfUnicodeTextSpan("B");
        superscript.Script = PdfTextScript.Superscript;
        PdfUnicodeTextSpan subscript = new PdfUnicodeTextSpan("C");
        subscript.Script = PdfTextScript.Subscript;
        PdfUnicodeTextSpan footnote = new PdfUnicodeTextSpan("1");
        footnote.FootnoteMarker = true;

        PdfUnicodeTextResult result = new PdfUnicodeTextLayout().LayoutSpans(
            new[] { normal, superscript, subscript, footnote }, CreateChain(font), CreateOptions(100, 10));

        Assert.That(result.Lines[0].Runs.Length, Is.EqualTo(4));
        Assert.That(result.Lines[0].Runs[0].FontSize, Is.EqualTo(10));
        Assert.That(result.Lines[0].Runs[1].FontSize, Is.EqualTo(6.5).Within(0.0001));
        Assert.That(result.Lines[0].Runs[1].BaselineShift, Is.EqualTo(3.5).Within(0.0001));
        Assert.That(result.Lines[0].Runs[2].BaselineShift, Is.EqualTo(-2).Within(0.0001));
        Assert.That(result.Lines[0].Runs[3].FootnoteMarker, Is.True);
        Assert.That(result.Lines[0].Height, Is.GreaterThan(10));
    }

    [Test]
    public void Draw_EmitsEachFallbackRunWithItsEmbeddedFont()
    {
        PdfDocument document = new PdfDocument();
        PdfTrueTypeFont latin = document.RegisterTrueTypeFont(TestSfntFont.Create(0x00010000U, 0, false));
        PdfTrueTypeFont emoji = document.RegisterTrueTypeFont(TestSfntFont.Create(0x00010000U, 0, true));
        PdfFontFallbackChain fonts = CreateChain(latin, emoji);
        PdfUnicodeTextLayout layout = new PdfUnicodeTextLayout();
        PdfUnicodeTextResult result = layout.Layout("A😀C", fonts, CreateOptions(100, 10));
        PdfPage page = document.AddPage(200, 100);

        layout.Draw(page.Canvas, result, 10, 50);
        string pdf = Encoding.Latin1.GetString(document.ToBytes());

        Assert.That(CountOccurrences(pdf, "/Subtype /Type0"), Is.EqualTo(2));
        Assert.That(pdf, Does.Contain("<0001> Tj"));
        Assert.That(pdf, Does.Contain("<0003> Tj"));
        Assert.That(page.Canvas.TrueTypeFontUsages.Count, Is.EqualTo(2));
    }

    [Test]
    public void Layout_RejectsMissingClusterFontsAndInvalidOptions()
    {
        PdfTrueTypeFont font = CreateLayoutFont("Latin", new[] { 65 }, new[] { 500 });
        PdfFontFallbackChain fonts = CreateChain(font);
        PdfUnicodeTextOptions options = CreateOptions(100, 10);
        PdfUnicodeTextLayout layout = new PdfUnicodeTextLayout();

        Assert.Throws<ArgumentException>(() => layout.Layout("Z", fonts, options));
        options.MaxWidth = 0;
        Assert.Throws<ArgumentOutOfRangeException>(() => layout.Layout("A", fonts, options));
        Assert.Throws<ArgumentNullException>(() => layout.Layout(null!, fonts, options));
    }

    [Test]
    public void Layout_RejectsHyphenationPointsInsideGraphemeClusters()
    {
        PdfTrueTypeFont font = CreateLayoutFont("Marks", new[] { 101, 0x0301 }, new[] { 500, 0 });
        PdfUnicodeTextSpan span = new PdfUnicodeTextSpan("e\u0301");
        span.HyphenationPoints = new[] { 1 };

        Assert.Throws<ArgumentException>(() => new PdfUnicodeTextLayout().LayoutSpans(
            new[] { span }, CreateChain(font), CreateOptions(100, 10)));
    }

    static PdfUnicodeTextOptions CreateOptions(double maxWidth, double fontSize)
    {
        PdfUnicodeTextOptions options = new PdfUnicodeTextOptions();
        options.MaxWidth = maxWidth;
        options.FontSize = fontSize;
        return options;
    }

    static PdfFontFallbackChain CreateChain(params PdfTrueTypeFont[] fonts)
    {
        PdfFontFallbackChain chain = new PdfFontFallbackChain();
        foreach (PdfTrueTypeFont font in fonts)
        {
            chain.AddFont(font);
        }

        return chain;
    }

    static PdfTrueTypeFont CreateLatinWordFont()
    {
        int[] codePoints = "-aceiooprtv".Select(character => (int)character).Distinct().OrderBy(value => value).ToArray();
        int[] widths = codePoints.Select(value => 500).ToArray();
        return CreateLayoutFont("Words", codePoints, widths);
    }

    static PdfTrueTypeFont CreateLayoutFont(string name, int[] codePoints, int[] widths)
    {
        int[] glyphIndices = new int[codePoints.Length];
        int[] advanceWidths = new int[codePoints.Length + 1];
        advanceWidths[0] = 500;
        for (int index = 0; index < codePoints.Length; index++)
        {
            glyphIndices[index] = index + 1;
            advanceWidths[index + 1] = widths[index];
        }

        return new PdfTrueTypeFont(
            name.PadRight(64, '0'),
            "TT" + name,
            name,
            "Regular",
            name + " Regular",
            name + "-Regular",
            false,
            1000,
            0,
            -200,
            1000,
            800,
            800,
            -200,
            0,
            700,
            450,
            advanceWidths.Length,
            advanceWidths,
            codePoints,
            glyphIndices,
            3,
            10,
            12,
            PdfFontEmbeddingPermission.Installable,
            true,
            true,
            new byte[0]);
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
