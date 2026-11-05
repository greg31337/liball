using ttf;

namespace ttf.Tests;

internal class FontSubsetColorClosureTests
{
    [Test]
    public void ColorPolicyControlsWhetherClosureIsComputed()
    {
        FontInstance instance = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        FontSubsetOptions drop = FontSubsetOptions.Create();
        FontSubsetOptions retain = FontSubsetOptions.Create();
        retain.SetColorPolicy(FontSubsetColorPolicy.Retain);

        FontSubset droppedSubset = new FontSubsetBuilder(instance, drop)
            .Build();
        FontSubset retainedSubset = new FontSubsetBuilder(instance, retain)
            .Build();
        FontSubsetClosureReport droppedReport =
            droppedSubset.CreateResult().GetClosureReport();
        FontSubsetClosureReport retainedReport =
            retainedSubset.CreateResult().GetClosureReport();

        Assert.Multiple(() =>
        {
            Assert.That(droppedReport.IsColorApplicable(), Is.False);
            Assert.That(retainedReport.IsColorApplicable(), Is.True);
            Assert.That(retainedReport.GetColrBaseGlyphIds(), Is.Empty);
            Assert.That(retainedReport.GetSvgDocumentIndices(), Is.Empty);
        });
    }

    [Test]
    public void ComputesColrLayerGlyphAndPaletteClosure()
    {
        SfntCpalTable cpal = CreateCpal();
        SfntColrLayerRecord[] layers =
        {
            new SfntColrLayerRecord(2, 1),
            new SfntColrLayerRecord(3, 0xFFFF)
        };
        SfntColrTable colr = SfntColrTable.Create(
            0,
            4,
            new SfntColrBaseGlyphRecord[]
            {
                new SfntColrBaseGlyphRecord(1, 0, 2, layers)
            },
            layers,
            cpal);

        FontSubsetColorClosure closure =
            FontSubsetColorClosureBuilder.CreateForTables(
                colr,
                cpal,
                null,
                null,
                null,
                null,
                FontLimits.Create(),
                Array.Empty<double>(),
                new int[] { 1 },
                new int[] { 1 },
                20);

        Assert.Multiple(() =>
        {
            Assert.That(closure.CopyGlyphIds(),
                Is.EqualTo(new int[] { 1, 2, 3 }));
            Assert.That(closure.CopyColrBaseGlyphIds(),
                Is.EqualTo(new int[] { 1 }));
            Assert.That(closure.CopyColrLayerIndices(),
                Is.EqualTo(new int[] { 0, 1 }));
            Assert.That(closure.CopyColrPaintOffsets(), Is.Empty);
            Assert.That(closure.CopyPaletteIndices(),
                Is.EqualTo(new int[] { 1 }));
            Assert.That(closure.CopyPaletteEntryIndices(),
                Is.EqualTo(new int[] { 1 }));
        });
    }

    [Test]
    public void ComputesSvgRangeSbixDuplicateAndBitmapComponentClosure()
    {
        SfntSvgTable svg = CreateSvg(4, 6, 8);
        SfntSbixGlyphData[] sbixGlyphs =
            new SfntSbixGlyphData[8];
        sbixGlyphs[4] = new SfntSbixGlyphData(
            4,
            0,
            0,
            Tag("dupe"),
            SfntSbixGraphicKind.Duplicate,
            Array.Empty<byte>(),
            7);
        sbixGlyphs[7] = new SfntSbixGlyphData(
            7,
            0,
            0,
            Tag("png "),
            SfntSbixGraphicKind.Png,
            new byte[] { 1 },
            -1);
        SfntSbixTable sbix = new SfntSbixTable(
            0,
            new SfntSbixStrike[]
            {
                new SfntSbixStrike(16, 72, 10, sbixGlyphs)
            });
        SfntBitmapGlyphData[] bitmapGlyphs =
            new SfntBitmapGlyphData[8];
        bitmapGlyphs[3] = BitmapGlyph(
            3,
            Array.Empty<SfntBitmapComponent>());
        bitmapGlyphs[6] = BitmapGlyph(
            6,
            new SfntBitmapComponent[]
            {
                new SfntBitmapComponent(3, 0, 0)
            });
        SfntBitmapLocationTable cbdt = new SfntBitmapLocationTable(
            3,
            true,
            new SfntBitmapStrike[]
            {
                new SfntBitmapStrike(
                    true,
                    3,
                    6,
                    16,
                    16,
                    32,
                    true,
                    null!,
                    null!,
                    bitmapGlyphs,
                    10)
            });

        FontSubsetColorClosure closure =
            FontSubsetColorClosureBuilder.CreateForTables(
                null,
                null,
                svg,
                sbix,
                cbdt,
                cbdt,
                FontLimits.Create(),
                Array.Empty<double>(),
                new int[] { 5 },
                Array.Empty<int>(),
                20);
        FontSubsetClosureReport report = new FontSubsetClosureReport(
            Array.Empty<int>(),
            new FontSubsetLayoutClosure(
                false,
                false,
                new int[] { 5 },
                Array.Empty<int>(),
                Array.Empty<int>(),
                Array.Empty<int>(),
                Array.Empty<int>(),
                Array.Empty<int>()),
            closure,
            new FontSubsetCompositeClosure(
                closure.CopyGlyphIds(),
                Array.Empty<FontDiagnostic>()),
            new FontSubsetCffClosure(
                false,
                Array.Empty<int>(),
                Array.Empty<int>(),
                Array.Empty<FontSubsetCffLocalSubroutineClosure>(),
                Array.Empty<FontDiagnostic>()));
        int[] copiedSvgDocuments = report.GetSvgDocumentIndices();
        copiedSvgDocuments[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(closure.CopyGlyphIds(),
                Is.EqualTo(new int[] { 3, 4, 5, 6, 7 }));
            Assert.That(closure.CopySvgDocumentIndices(),
                Is.EqualTo(new int[] { 0 }));
            Assert.That(closure.CopySbixGlyphIds(),
                Is.EqualTo(new int[] { 4, 7 }));
            Assert.That(closure.CopyCbdtGlyphIds(),
                Is.EqualTo(new int[] { 3, 6 }));
            Assert.That(closure.CopyEbdtGlyphIds(),
                Is.EqualTo(new int[] { 3, 6 }));
            Assert.That(report.IsColorApplicable(), Is.True);
            Assert.That(report.GetColorGlyphIds(),
                Is.EqualTo(new int[] { 3, 4, 5, 6, 7 }));
            Assert.That(report.GetSvgDocumentIndices(),
                Is.EqualTo(new int[] { 0 }));
            Assert.That(report.GetSbixGlyphIds(),
                Is.EqualTo(new int[] { 4, 7 }));
            Assert.That(report.GetCbdtGlyphIds(),
                Is.EqualTo(new int[] { 3, 6 }));
            Assert.That(report.GetEbdtGlyphIds(),
                Is.EqualTo(new int[] { 3, 6 }));
        });
    }

    [Test]
    public void RecordsRetainedColrVersionOnePaintOffsets()
    {
        SfntCpalTable cpal = CreateCpal();
        SfntColrVariationData variation =
            new SfntColrVariationData(null, null);
        SfntColrVariableValue alpha = new SfntColrVariableValue(
            16384,
            16384,
            0,
            1,
            true,
            true,
            -1,
            variation);
        SfntColrPaint paint = new SfntColrPaint(
            100,
            2,
            true,
            false,
            0,
            0,
            0,
            2,
            -1,
            0,
            SfntColrCompositeMode.Clear,
            Array.Empty<int>(),
            new SfntColrVariableValue[] { alpha },
            null,
            null);
        SfntColrTable colr = SfntColrTable.Create(
            1,
            2,
            Array.Empty<SfntColrBaseGlyphRecord>(),
            Array.Empty<SfntColrLayerRecord>(),
            cpal,
            new SfntColrBaseGlyphPaintRecord[]
            {
                new SfntColrBaseGlyphPaintRecord(1, 100, paint)
            },
            new SfntColrLayerList(
                0,
                Array.Empty<SfntColrPaint>()),
            null,
            new SfntColrPaint[] { paint },
            variation);

        FontSubsetColorClosure closure =
            FontSubsetColorClosureBuilder.CreateForTables(
                colr,
                cpal,
                null,
                null,
                null,
                null,
                FontLimits.Create(),
                Array.Empty<double>(),
                new int[] { 1 },
                Array.Empty<int>(),
                20);

        Assert.Multiple(() =>
        {
            Assert.That(closure.CopyColrBaseGlyphIds(),
                Is.EqualTo(new int[] { 1 }));
            Assert.That(closure.CopyColrPaintOffsets(),
                Is.EqualTo(new int[] { 100 }));
            Assert.That(closure.CopyPaletteEntryIndices(),
                Is.EqualTo(new int[] { 2 }));
        });
    }

    [Test]
    public void SvgClosureConservativelyRetainsEveryPaletteEntry()
    {
        FontSubsetColorClosure closure =
            FontSubsetColorClosureBuilder.CreateForTables(
                null,
                CreateCpal(),
                CreateSvg(1, 1, 2),
                null,
                null,
                null,
                FontLimits.Create(),
                Array.Empty<double>(),
                new int[] { 1 },
                Array.Empty<int>(),
                20);

        Assert.Multiple(() =>
        {
            Assert.That(closure.CopyPaletteIndices(),
                Is.EqualTo(new int[] { 0 }));
            Assert.That(closure.CopyPaletteEntryIndices(),
                Is.EqualTo(new int[] { 0, 1, 2 }));
        });
    }

    [Test]
    public void EnforcesColorGlyphClosureLimit()
    {
        Assert.Throws<FontLimitException>(() =>
            FontSubsetColorClosureBuilder.CreateForTables(
                null,
                null,
                CreateSvg(1, 3, 4),
                null,
                null,
                null,
                FontLimits.Create(),
                Array.Empty<double>(),
                new int[] { 2 },
                Array.Empty<int>(),
                2));
    }

    static SfntCpalTable CreateCpal()
    {
        SfntCpalColor[] colors =
        {
            new SfntCpalColor(0, 0, 0, 255),
            new SfntCpalColor(1, 1, 1, 255),
            new SfntCpalColor(2, 2, 2, 255),
            new SfntCpalColor(3, 3, 3, 255),
            new SfntCpalColor(4, 4, 4, 255),
            new SfntCpalColor(5, 5, 5, 255)
        };
        SfntCpalPalette[] palettes =
        {
            new SfntCpalPalette(
                0, 0, 3, colors, false, 0, false, 0, null),
            new SfntCpalPalette(
                1, 3, 3, colors, false, 0, false, 0, null)
        };
        return new SfntCpalTable(
            0,
            colors,
            palettes,
            3,
            false,
            false,
            false,
            new int[] { 0xFFFF, 0xFFFF, 0xFFFF },
            new SfntLocalizedName[3]);
    }

    static SfntSvgTable CreateSvg(
        int startGlyphId,
        int endGlyphId,
        int glyphCount)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(
            "<svg xmlns=\"http://www.w3.org/2000/svg\"/>");
        SfntSvgDocumentData data = new SfntSvgDocumentData(
            bytes,
            bytes,
            false);
        return new SfntSvgTable(
            glyphCount,
            new SfntSvgDocument[]
            {
                new SfntSvgDocument(
                    startGlyphId,
                    endGlyphId,
                    0,
                    data)
            });
    }

    static SfntBitmapGlyphData BitmapGlyph(
        int glyphId,
        SfntBitmapComponent[] components)
    {
        return new SfntBitmapGlyphData(
            glyphId,
            components.Length == 0 ? 17 : 8,
            true,
            false,
            0,
            1,
            null,
            new byte[] { 1 },
            components);
    }

    static int Tag(string value)
    {
        return (int)TestSfntEncoding.EncodeTag(value);
    }
}
