using ttf;

namespace ttf.Tests;

[NonParallelizable]
internal class PublicConcurrencyDeterminismTests
{
    [Test]
    public void SharedPublicOperationsRemainStableAcrossThreadsAndCultures()
    {
        FontLoader loader = new FontLoader();
        FontPackage package = loader.LoadFromBytes(
            System.Convert.FromBase64String(
                HarfBuzzLayoutDifferentialTests.OracleFontBase64),
            new FontLoadOptions());
        FontFace face = package.GetFace(0);
        FontInstance instance = face.CreateDefaultInstance();
        FontNameValue? familyName = face.GetNames().GetFamilyName();
        Assert.That(familyName, Is.Not.Null);
        FontRequest request = new FontRequest(
            familyName == null ? "Missing" : familyName.GetValue());
        FontSet fontSet = new FontSet();
        fontSet.Add(face);
        TextShapingOptions shapingOptions = new TextShapingOptions();
        TextLayoutOptions layoutOptions = new TextLayoutOptions();
        layoutOptions.SetAlignment(TextAlignment.Center);
        TextStyle textStyle = new TextStyle(12);
        TextLayoutStyle[] styles = new TextLayoutStyle[]
        {
            new TextLayoutStyle(0, 2, request, textStyle)
        };
        ColorGlyphOptions colorOptions = new ColorGlyphOptions();
        colorOptions.SetPreferredRepresentation(
            ColorGlyphRepresentation.MonochromeOutline);
        ColorGlyphResolver colorResolver = new ColorGlyphResolver();
        TextLayoutEngine layoutEngine = new TextLayoutEngine();
        FontSubsetOptions subsetOptions = new FontSubsetOptions();
        PdfFontOptions pdfOptions = new PdfFontOptions();
        int glyphId = face.GetCharacterMap().MapUnicode('A').GetGlyphId();
        int operationCount = 12;
        FontFace[] faces = new FontFace[operationCount];
        GlyphOutline[] outlines = new GlyphOutline[operationCount];
        int[][] shapedGlyphIds = new int[operationCount][];
        int[] lineCounts = new int[operationCount];
        double[] lineAdvances = new double[operationCount];
        ColorGlyphRepresentation[] representations =
            new ColorGlyphRepresentation[operationCount];
        int[][] subsetGlyphOrder = new int[operationCount][];
        byte[][] subsetBytes = new byte[operationCount][];
        int[][] pdfGlyphOrder = new int[operationCount][];
        byte[][] pdfBytes = new byte[operationCount][];
        string[] pdfKeys = new string[operationCount];
        FontDiagnostic[][] diagnostics = new FontDiagnostic[operationCount][];

        Parallel.For(0, operationCount, index =>
        {
            System.Globalization.CultureInfo originalCulture =
                System.Globalization.CultureInfo.CurrentCulture;
            System.Globalization.CultureInfo originalUiCulture =
                System.Globalization.CultureInfo.CurrentUICulture;
            string cultureName = index % 2 == 0 ? "tr-TR" : "ar-SA";
            System.Globalization.CultureInfo culture =
                System.Globalization.CultureInfo.GetCultureInfo(cultureName);

            try
            {
                System.Globalization.CultureInfo.CurrentCulture = culture;
                System.Globalization.CultureInfo.CurrentUICulture = culture;
                faces[index] = package.GetFace(0);
                outlines[index] = instance.GetGlyphOutline(glyphId);
                TextShapingResult shaped = instance.ShapeTextWithOptions(
                    "AV",
                    shapingOptions);
                shapedGlyphIds[index] = CopyGlyphIds(shaped.GetRun(0));
                PreparedTextResult prepared = layoutEngine.Prepare(
                    fontSet,
                    "AV",
                    styles,
                    layoutOptions);
                TextLayoutResult layout = prepared.Reflow(200);
                lineCounts[index] = layout.GetLineCount();
                lineAdvances[index] = layout.GetLineAdvance(0);
                representations[index] = colorResolver.ResolveGlyphWithOptions(
                    instance,
                    glyphId,
                    colorOptions).GetSelectedRepresentation();
                FontSubsetBuilder subset = new FontSubsetBuilder(
                    instance,
                    subsetOptions);
                if (index % 2 == 0)
                {
                    subset.AddCodePoint('A');
                    subset.AddCodePoint('V');
                }
                else
                {
                    subset.AddCodePoint('V');
                    subset.AddCodePoint('A');
                }
                FontSubsetResult subsetResult = subset.BuildResult();
                subsetGlyphOrder[index] = subsetResult.GetNewToOldGlyphMap();
                subsetBytes[index] = subsetResult.CopyFontBytes();
                PdfFontBuilder pdf = new PdfFontBuilder(instance, pdfOptions);
                pdf.AddGlyph(glyphId, new int[] { 'A' });
                PdfFontResource resource = pdf.FinalizeFont();
                PdfCidFontType2Artifact? type2 =
                    resource.GetCidFontType2Artifact();
                System.ArgumentNullException.ThrowIfNull(type2);
                pdfGlyphOrder[index] = type2.CopyCidToSourceGlyphIds();
                pdfBytes[index] = resource.CopyEmbeddedFont();
                pdfKeys[index] = resource.GetDeduplicationKey();
                diagnostics[index] = package.GetDiagnostics();
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture =
                    originalCulture;
                System.Globalization.CultureInfo.CurrentUICulture =
                    originalUiCulture;
            }
        });

        Assert.Multiple(() =>
        {
            Assert.That(faces, Has.All.SameAs(face));
            Assert.That(outlines, Has.All.SameAs(outlines[0]));
            Assert.That(shapedGlyphIds,
                Has.All.EqualTo(shapedGlyphIds[0]));
            Assert.That(lineCounts, Has.All.EqualTo(1));
            Assert.That(lineAdvances, Has.All.EqualTo(lineAdvances[0]));
            Assert.That(representations,
                Has.All.EqualTo(ColorGlyphRepresentation.MonochromeOutline));
            Assert.That(subsetGlyphOrder,
                Has.All.EqualTo(subsetGlyphOrder[0]));
            Assert.That(subsetBytes, Has.All.EqualTo(subsetBytes[0]));
            Assert.That(pdfGlyphOrder,
                Has.All.EqualTo(pdfGlyphOrder[0]));
            Assert.That(pdfBytes, Has.All.EqualTo(pdfBytes[0]));
            Assert.That(pdfKeys, Has.All.EqualTo(pdfKeys[0]));
            Assert.That(diagnostics, Has.All.EqualTo(diagnostics[0]));
        });
    }

    static int[] CopyGlyphIds(TextShapingRun run)
    {
        ShapedRun shaped = run.GetShapedRun();
        int[] result = new int[shaped.GetGlyphCount()];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = shaped.GetGlyph(index).GetGlyphId();
            index += 1;
        }
        return result;
    }
}
