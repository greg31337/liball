using System.Text;
using ttf;

namespace ttf.Tests;

internal class ColorGlyphResolverTests
{
    [Test]
    public void ResolvesPaletteForegroundAndAllAvailableRepresentations()
    {
        FontInstance instance = CreateColorFace().CreateDefaultInstance();
        ColorGlyphOptions options = new ColorGlyphOptions();
        options.SetPaletteIndex(1);
        options.SetPreferredRepresentation(ColorGlyphRepresentation.Colr);
        options.SetForegroundColor(new ColorGlyphColor(90, 80, 70, 60));
        options.SetBitmapTargetSize(22, 23);

        ColorGlyph glyph = new ColorGlyphResolver().ResolveGlyphWithOptions(
            instance,
            5,
            options);
        options.SetPaletteIndex(0);
        options.SetPreferredRepresentation(ColorGlyphRepresentation.Svg);
        options.SetForegroundColor(new ColorGlyphColor(1, 2, 3, 4));
        options.ClearBitmapTargetSize();
        ColorGlyphOptions retained = glyph.GetResolutionOptions();
        retained.SetPaletteIndex(0);
        retained.SetPreferredRepresentation(ColorGlyphRepresentation.Sbix);
        retained.ClearBitmapTargetSize();
        ColorGlyphOptions retainedAgain = glyph.GetResolutionOptions();
        ColorPaint root = glyph.GetPaintGraph()!.GetRoot();
        ColorPaint palette = root.GetChild(0).GetChild(0);
        ColorPaint foreground = root.GetChild(1).GetChild(0);
        ColorBitmapStrike[] strikes = glyph.GetBitmapStrikes();

        Assert.Multiple(() =>
        {
            Assert.That(glyph.GetGlyphId(), Is.EqualTo(5));
            Assert.That(glyph.GetPaletteIndex(), Is.EqualTo(1));
            Assert.That(retainedAgain.GetPaletteIndex(), Is.EqualTo(1));
            Assert.That(retainedAgain.GetPreferredRepresentation(),
                Is.EqualTo(ColorGlyphRepresentation.Colr));
            Assert.That(retainedAgain.GetForegroundColor().GetRed(),
                Is.EqualTo(90));
            Assert.That(retainedAgain.GetTargetPixelsPerEmX(), Is.EqualTo(22));
            Assert.That(retainedAgain.GetTargetPixelsPerEmY(), Is.EqualTo(23));
            Assert.That(glyph.GetSelectedRepresentation(),
                Is.EqualTo(ColorGlyphRepresentation.Colr));
            Assert.That(glyph.GetPdfAppearanceGuidance(),
                Is.EqualTo(PdfColorGlyphAppearance.DrawVectorAppearance));
            Assert.That(glyph.GetPaintGraph()!.GetColrVersion(), Is.Zero);
            Assert.That(palette.GetKind(), Is.EqualTo(ColorPaintKind.PaletteColor));
            Assert.That(palette.GetPaletteEntryIndex(), Is.EqualTo(1));
            Assert.That(palette.GetColor().GetRed(), Is.EqualTo(27));
            Assert.That(palette.GetColor().GetGreen(), Is.EqualTo(26));
            Assert.That(palette.GetColor().GetBlue(), Is.EqualTo(25));
            Assert.That(palette.GetAlpha(), Is.EqualTo(1));
            Assert.That(foreground.GetKind(),
                Is.EqualTo(ColorPaintKind.ForegroundColor));
            Assert.That(foreground.GetColor().GetRed(), Is.EqualTo(90));
            Assert.That(foreground.GetColor().GetAlpha(), Is.EqualTo(60));
            Assert.That(glyph.GetSvgDocument(), Is.Not.Null);
            Assert.That(glyph.GetSvgDocument()!.GetStartGlyphId(), Is.EqualTo(5));
            Assert.That(glyph.GetSvgDocument()!.LoadXml().DocumentElement!.LocalName,
                Is.EqualTo("svg"));
            Assert.That(strikes, Has.Length.EqualTo(1));
            Assert.That(strikes[0].GetRepresentation(),
                Is.EqualTo(ColorGlyphRepresentation.Sbix));
            Assert.That(strikes[0].GetPixelsPerEmX(), Is.EqualTo(24));
            Assert.That(strikes[0].GetPixelsPerInch(), Is.EqualTo(144));
            Assert.That(strikes[0].ShouldDrawOutline(), Is.True);
            Assert.That(strikes[0].CopyData(), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(glyph.GetMonochromeOutline(), Is.Null);
        });
    }

    [Test]
    public void HonorsAvailablePreferencesAndFallsBackDeterministically()
    {
        FontInstance instance = CreateColorFace().CreateDefaultInstance();
        ColorGlyphOptions svgOptions = new ColorGlyphOptions();
        svgOptions.SetPreferredRepresentation(ColorGlyphRepresentation.Svg);
        ColorGlyphOptions sbixOptions = new ColorGlyphOptions();
        sbixOptions.SetPreferredRepresentation(ColorGlyphRepresentation.Sbix);
        ColorGlyphOptions unavailableOptions = new ColorGlyphOptions();
        unavailableOptions.SetPreferredRepresentation(ColorGlyphRepresentation.Cbdt);

        ColorGlyphResolver resolver = new ColorGlyphResolver();
        ColorGlyph svg = resolver.ResolveGlyphWithOptions(instance, 5, svgOptions);
        ColorGlyph sbix = resolver.ResolveGlyphWithOptions(instance, 5, sbixOptions);
        ColorGlyph unavailable = resolver.ResolveGlyphWithOptions(
            instance,
            5,
            unavailableOptions);

        Assert.Multiple(() =>
        {
            Assert.That(svg.GetSelectedRepresentation(),
                Is.EqualTo(ColorGlyphRepresentation.Svg));
            Assert.That(svg.GetPdfAppearanceGuidance(),
                Is.EqualTo(PdfColorGlyphAppearance.DrawSvgAppearance));
            Assert.That(sbix.GetSelectedRepresentation(),
                Is.EqualTo(ColorGlyphRepresentation.Sbix));
            Assert.That(sbix.GetSelectedBitmapStrike(), Is.Not.Null);
            Assert.That(sbix.GetPdfAppearanceGuidance(), Is.EqualTo(
                PdfColorGlyphAppearance.DrawImageAndOutlineAppearance));
            Assert.That(unavailable.GetSelectedRepresentation(),
                Is.EqualTo(ColorGlyphRepresentation.Colr));
        });
    }

    [Test]
    public void UsesTheOrdinaryOutlineAsTheFinalFallback()
    {
        FontInstance instance = FontInstanceTests.CreateFace().CreateDefaultInstance();

        ColorGlyph glyph = new ColorGlyphResolver().ResolveGlyph(instance, 1);

        Assert.Multiple(() =>
        {
            Assert.That(glyph.GetSelectedRepresentation(),
                Is.EqualTo(ColorGlyphRepresentation.MonochromeOutline));
            Assert.That(glyph.GetPdfAppearanceGuidance(),
                Is.EqualTo(PdfColorGlyphAppearance.UseTextGlyph));
            Assert.That(glyph.GetMonochromeOutline(),
                Is.SameAs(instance.GetGlyphOutline(1)));
            Assert.That(glyph.GetMonochromeFallbackGuidance(),
                Is.EqualTo(ColorMonochromeFallbackGuidance.Selected));
            Assert.That(glyph.GetPaintGraph(), Is.Null);
            Assert.That(glyph.GetSvgDocument(), Is.Null);
            Assert.That(glyph.GetBitmapStrikes(), Is.Empty);
        });
    }

    [Test]
    public void ValidatesOptionsPaletteAndGlyphInputs()
    {
        FontInstance instance = CreateColorFace().CreateDefaultInstance();
        ColorGlyphOptions badPalette = new ColorGlyphOptions();
        badPalette.SetPaletteIndex(2);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ColorGlyphColor(-1, 0, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ColorGlyphOptions().SetPaletteIndex(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ColorGlyphResolver().ResolveGlyphWithOptions(
                    instance,
                    5,
                    badPalette));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ColorGlyphResolver().ResolveGlyph(instance, 10));
            Assert.Throws<ArgumentNullException>(() =>
                new ColorGlyphResolver().ResolveGlyph(null!, 0));
            Assert.Throws<ArgumentNullException>(() =>
                new ColorGlyphResolver().ResolveGlyphWithOptions(
                    instance,
                    0,
                    null!));
        });
    }

    static FontFace CreateColorFace()
    {
        byte[] xml = Encoding.UTF8.GetBytes(
            "<svg xmlns=\"http://www.w3.org/2000/svg\"><g id=\"glyph5\"/></svg>");
        List<SfntTableData> tables = new List<SfntTableData>();
        tables.Add(CreateTable(FontTags.Maxp, BuildMaxp(10)));
        tables.Add(CreateTable(FontTags.Cpal, BuildCpal()));
        tables.Add(CreateTable(FontTags.Colr, BuildColr()));
        tables.Add(CreateTable(FontTags.Svg, BuildSvg(xml)));
        tables.Add(CreateTable(FontTags.Sbix, BuildSbix()));
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                tables.ToArray(),
                Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible);
        return new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.BitmapOnly,
                Array.Empty<FontDiagnostic>()),
            cache);
    }

    static SfntTableData CreateTable(int tag, byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        return new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true);
    }

    static byte[] BuildMaxp(int glyphCount)
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00005000U)
            .WriteUInt16((ushort)glyphCount)
            .CopyBytes();
    }

    static byte[] BuildCpal()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(3)
            .WriteUInt16(2)
            .WriteUInt16(6)
            .WriteUInt32(16)
            .WriteUInt16(0)
            .WriteUInt16(3)
            .WriteUInt8(1).WriteUInt8(2).WriteUInt8(3).WriteUInt8(255)
            .WriteUInt8(5).WriteUInt8(6).WriteUInt8(7).WriteUInt8(255)
            .WriteUInt8(9).WriteUInt8(10).WriteUInt8(11).WriteUInt8(255)
            .WriteUInt8(21).WriteUInt8(22).WriteUInt8(23).WriteUInt8(255)
            .WriteUInt8(25).WriteUInt8(26).WriteUInt8(27).WriteUInt8(255)
            .WriteUInt8(29).WriteUInt8(30).WriteUInt8(31).WriteUInt8(255)
            .CopyBytes();
    }

    static byte[] BuildColr()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt32(14)
            .WriteUInt32(20)
            .WriteUInt16(2)
            .WriteUInt16(5).WriteUInt16(0).WriteUInt16(2)
            .WriteUInt16(4).WriteUInt16(1)
            .WriteUInt16(6).WriteUInt16(0xFFFF)
            .CopyBytes();
    }

    static byte[] BuildSvg(byte[] document)
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt32(10)
            .WriteUInt32(0)
            .WriteUInt16(1)
            .WriteUInt16(5)
            .WriteUInt16(5)
            .WriteUInt32(14)
            .WriteUInt32((uint)document.Length)
            .WriteBytes(document)
            .CopyBytes();
    }

    static byte[] BuildSbix()
    {
        byte[][] glyphRecords = new byte[10][];
        glyphRecords[5] = new TestBinaryBuilder()
            .WriteInt16(-2)
            .WriteInt16(3)
            .WriteTag("png ")
            .WriteBytes(new byte[] { 1, 2, 3 })
            .CopyBytes();
        TestBinaryBuilder strike = new TestBinaryBuilder()
            .WriteUInt16(24)
            .WriteUInt16(144);
        int offsetsPosition = strike.GetLength();
        strike.WriteZeros((glyphRecords.Length + 1) * 4);
        int glyphId = 0;

        while (glyphId < glyphRecords.Length)
        {
            strike.PatchUInt32(
                offsetsPosition + glyphId * 4,
                (uint)strike.GetLength());

            if (glyphRecords[glyphId] != null)
            {
                strike.WriteBytes(glyphRecords[glyphId]);
            }

            glyphId += 1;
        }

        strike.PatchUInt32(
            offsetsPosition + glyphRecords.Length * 4,
            (uint)strike.GetLength());
        return new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(3)
            .WriteUInt32(1)
            .WriteUInt32(12)
            .WriteBytes(strike.CopyBytes())
            .CopyBytes();
    }
}
