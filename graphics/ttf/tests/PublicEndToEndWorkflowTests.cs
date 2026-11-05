using System.Text;
using ttf;

namespace ttf.Tests;

internal class PublicEndToEndWorkflowTests
{
    [Test]
    public void InternationalFallbackWorkflowProducesConsumableFontsAndPdfs()
    {
        FontFace source = HarfBuzzLayoutDifferentialTests.Load();
        FontFace primary = LoadPublicFixture(
            source,
            "International Latin",
            new int[] { 0x20, 0x41, 0x56 });
        FontFace greek = LoadPublicFixture(
            source,
            "International Greek",
            new int[] { 0x0391, 0x0392 });
        FontFace arabic = LoadPublicFixture(
            source,
            "International Arabic",
            new int[] { 0x0627, 0x0628 });
        FontSet fontSet = new FontSet();
        fontSet.Add(primary);
        fontSet.Add(greek);
        fontSet.Add(arabic);
        fontSet.AddFallbackFamily(GetFamily(greek));
        fontSet.AddFallbackFamily(GetFamily(arabic));
        string text = "AV \u0391\u0392 \u0627\u0628";
        FontRequest request = new FontRequest(GetFamily(primary));

        TextShapingResult shaping = fontSet.ShapeText(text, request);
        TextLayoutResult layout = PrepareLayout(
            fontSet,
            text,
            request,
            TextWritingMode.Horizontal);
        System.Collections.Generic.Dictionary<
            FontInstance,
            FontSubsetBuilder> subsets =
                new System.Collections.Generic.Dictionary<
                    FontInstance,
                    FontSubsetBuilder>();
        System.Collections.Generic.Dictionary<
            FontInstance,
            PdfFontBuilder> pdfBuilders =
                new System.Collections.Generic.Dictionary<
                    FontInstance,
                    PdfFontBuilder>();
        PdfEncodedRun[] encodedRuns =
            new PdfEncodedRun[shaping.GetRunCount()];
        int runIndex = 0;

        while (runIndex < shaping.GetRunCount())
        {
            TextShapingRun run = shaping.GetRun(runIndex);
            FontInstance instance = run.GetFontInstance();
            FontSubsetBuilder subset = GetSubsetBuilder(subsets, instance);
            PdfFontBuilder pdf = GetPdfBuilder(pdfBuilders, instance, false);
            subset.AddResolvedRun(run);
            encodedRuns[runIndex] = pdf.AddResolvedRun(run);
            runIndex += 1;
        }

        AssertResolvedRunValidation(shaping);
        System.Collections.Generic.Dictionary<
            FontInstance,
            PdfFontResource> resources = FinalizeResources(pdfBuilders);
        ValidateSubsetOutputs(subsets);

        runIndex = 0;
        while (runIndex < shaping.GetRunCount())
        {
            TextShapingRun run = shaping.GetRun(runIndex);
            PdfFontResource resource = resources[run.GetFontInstance()];
            string expectedText = text.Substring(
                run.GetUtf16Start(),
                run.GetUtf16Length());
            byte[] pdf = MinimalPdfAssembler.Assemble(
                resource,
                encodedRuns[runIndex].GetCodes(),
                expectedText);
            Assert.That(ExtractActualText(pdf), Is.EqualTo(expectedText));
            Assert.That(IndependentSfntGlyphCount(
                resource.CopyEmbeddedFont()), Is.GreaterThan(0));
            Assert.That(Encoding.Latin1.GetString(pdf),
                Does.Contain("/ToUnicode"));
            runIndex += 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(shaping.GetDiagnosticCount(), Is.Zero);
            Assert.That(shaping.GetRunCount(), Is.GreaterThanOrEqualTo(3));
            Assert.That(DistinctInstanceCount(shaping), Is.EqualTo(3));
            Assert.That(HasScript(shaping, "Latn"), Is.True);
            Assert.That(HasScript(shaping, "Grek"), Is.True);
            Assert.That(HasScript(shaping, "Arab"), Is.True);
            Assert.That(layout.GetSourceText(), Is.EqualTo(text));
            Assert.That(layout.GetLineCount(), Is.EqualTo(1));
            Assert.That(layout.GetResolvedFontCount(), Is.EqualTo(3));
            Assert.That(resources.Count, Is.EqualTo(3));
        });
    }

    [Test]
    public void VerticalResolvedRunPreservesPositioningAndExtraction()
    {
        FontFace face = LoadPublicFixture(
            HarfBuzzLayoutDifferentialTests.Load(),
            "International Vertical",
            new int[] { 0x41, 0x56 });
        FontInstance instance = face.CreateDefaultInstance();
        TextShapingOptions shapingOptions = new TextShapingOptions();
        shapingOptions.SetDirection(TextShapingDirection.TopToBottom);
        TextShapingRun run = instance.ShapeTextWithOptions(
            "AV",
            shapingOptions).GetRun(0);
        PdfFontBuilder builder = GetPdfBuilder(
            new System.Collections.Generic.Dictionary<
                FontInstance,
                PdfFontBuilder>(),
            instance,
            true);
        PdfEncodedRun encoded = builder.AddResolvedRunWithBaselineShift(
            run,
            1.5);
        PdfFontResource resource = builder.FinalizeFont();
        FontSet set = new FontSet();
        set.Add(face);
        TextLayoutResult layout = PrepareLayout(
            set,
            "AV",
            new FontRequest(GetFamily(face)),
            TextWritingMode.VerticalTopToBottom);
        PositionedRunResult positioned = layout.GetVisualRun(0);
        byte[] pdf = MinimalPdfAssembler.Assemble(
            resource,
            encoded.GetCodes(),
            "AV");
        string pdfText = Encoding.Latin1.GetString(pdf);
        PdfCidFontType2Artifact? artifact =
            resource.GetCidFontType2Artifact();

        Assert.Multiple(() =>
        {
            Assert.That(encoded.GetWritingMode(),
                Is.EqualTo(PdfWritingMode.Vertical));
            Assert.That(encoded.GetYAdvance(), Is.Not.Zero);
            Assert.That(encoded.GetBaselineShift(), Is.EqualTo(1.5));
            Assert.That(layout.GetWritingMode(),
                Is.EqualTo(TextWritingMode.VerticalTopToBottom));
            Assert.That(positioned.GetDirection(),
                Is.EqualTo(ShapingDirection.TopToBottom));
            Assert.That(positioned.GetVisualGlyph(1).GetY(),
                Is.Not.EqualTo(positioned.GetVisualGlyph(0).GetY()));
            Assert.That(artifact, Is.Not.Null);
            Assert.That(artifact!.HasVerticalWidths(), Is.True);
            Assert.That(artifact.GetVerticalWidths(), Is.Not.Null);
            Assert.That(pdfText, Does.Contain("/Encoding /Identity-V"));
            Assert.That(pdfText, Does.Contain("/DW2 ["));
            Assert.That(pdfText, Does.Contain("/W2 ["));
            Assert.That(ExtractActualText(pdf), Is.EqualTo("AV"));
            Assert.That(IndependentSfntGlyphCount(
                resource.CopyEmbeddedFont()),
                Is.EqualTo(artifact.CopyNewToOldGlyphIds().Length));
        });
    }

    static FontFace LoadPublicFixture(
        FontFace source,
        string family,
        int[] codePoints)
    {
        FontFace fixture = FontClusterFallbackTests.Clone(
            source,
            family,
            codePoints);
        byte[] bytes = SfntFaceWriter.Write(
            fixture,
            FontLimits.CreateStrict());
        FontLoader loader = new FontLoader();
        return loader.LoadFromBytes(
            bytes,
            new FontLoadOptions()).GetFace(0);
    }

    static string GetFamily(FontFace face)
    {
        FontNameValue? family = face.GetNames().GetTypographicFamilyName();
        if (family == null)
        {
            family = face.GetNames().GetFamilyName();
        }
        System.ArgumentNullException.ThrowIfNull(family);
        return family.GetValue();
    }

    static TextLayoutResult PrepareLayout(
        FontSet set,
        string text,
        FontRequest request,
        TextWritingMode writingMode)
    {
        TextLayoutOptions options = new TextLayoutOptions();
        options.SetWritingMode(writingMode);
        TextLayoutStyle style = new TextLayoutStyle(
            0,
            text.Length,
            request,
            new TextStyle(12));
        return new TextLayoutEngine().Prepare(
            set,
            text,
            new TextLayoutStyle[] { style },
            options).Reflow(500);
    }

    static FontSubsetBuilder GetSubsetBuilder(
        System.Collections.Generic.Dictionary<
            FontInstance,
            FontSubsetBuilder> builders,
        FontInstance instance)
    {
        if (!builders.ContainsKey(instance))
        {
            builders.Add(
                instance,
                new FontSubsetBuilder(
                    instance,
                    new FontSubsetOptions()));
        }
        return builders[instance];
    }

    static PdfFontBuilder GetPdfBuilder(
        System.Collections.Generic.Dictionary<
            FontInstance,
            PdfFontBuilder> builders,
        FontInstance instance,
        bool vertical)
    {
        if (!builders.ContainsKey(instance))
        {
            PdfFontOptions options = new PdfFontOptions();
            if (vertical)
            {
                options.SetWritingMode(PdfWritingMode.Vertical);
            }
            builders.Add(instance, new PdfFontBuilder(instance, options));
        }
        return builders[instance];
    }

    static System.Collections.Generic.Dictionary<
        FontInstance,
        PdfFontResource> FinalizeResources(
            System.Collections.Generic.Dictionary<
                FontInstance,
                PdfFontBuilder> builders)
    {
        System.Collections.Generic.Dictionary<
            FontInstance,
            PdfFontResource> resources =
                new System.Collections.Generic.Dictionary<
                    FontInstance,
                    PdfFontResource>();
        foreach (System.Collections.Generic.KeyValuePair<
            FontInstance,
            PdfFontBuilder> pair in builders)
        {
            resources.Add(pair.Key, pair.Value.FinalizeFont());
        }
        return resources;
    }

    static void ValidateSubsetOutputs(
        System.Collections.Generic.Dictionary<
            FontInstance,
            FontSubsetBuilder> builders)
    {
        foreach (System.Collections.Generic.KeyValuePair<
            FontInstance,
            FontSubsetBuilder> pair in builders)
        {
            FontSubsetResult result = pair.Value.BuildResult();
            int independentGlyphCount = IndependentSfntGlyphCount(
                result.CopyFontBytes());
            Assert.Multiple(() =>
            {
                Assert.That(result.GetSourceInstance(), Is.SameAs(pair.Key));
                Assert.That(independentGlyphCount,
                    Is.EqualTo(result.GetNewToOldGlyphMap().Length));
                Assert.That(result.GetUnicodeMappings(), Is.Not.Empty);
            });
        }
    }

    static void AssertResolvedRunValidation(TextShapingResult shaping)
    {
        TextShapingRun first = shaping.GetRun(0);
        TextShapingRun? foreign = null;
        int index = 1;
        while (index < shaping.GetRunCount() && foreign == null)
        {
            TextShapingRun candidate = shaping.GetRun(index);
            if (!object.ReferenceEquals(
                candidate.GetFontInstance(),
                first.GetFontInstance()))
            {
                foreign = candidate;
            }
            index += 1;
        }
        System.ArgumentNullException.ThrowIfNull(foreign);
        FontSubsetBuilder subset = new FontSubsetBuilder(
            first.GetFontInstance(),
            new FontSubsetOptions());
        PdfFontBuilder pdf = new PdfFontBuilder(
            first.GetFontInstance(),
            new PdfFontOptions());

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() =>
                subset.AddResolvedRun(null!));
            Assert.Throws<ArgumentException>(() =>
                subset.AddResolvedRun(foreign));
            Assert.Throws<ArgumentNullException>(() =>
                pdf.AddResolvedRun(null!));
            Assert.Throws<ArgumentException>(() =>
                pdf.AddResolvedRun(foreign));
        });
    }

    static int DistinctInstanceCount(TextShapingResult shaping)
    {
        System.Collections.Generic.HashSet<FontInstance> instances =
            new System.Collections.Generic.HashSet<FontInstance>();
        int index = 0;
        while (index < shaping.GetRunCount())
        {
            instances.Add(shaping.GetRun(index).GetFontInstance());
            index += 1;
        }
        return instances.Count;
    }

    static bool HasScript(TextShapingResult shaping, string script)
    {
        int index = 0;
        while (index < shaping.GetRunCount())
        {
            if (shaping.GetRun(index).GetScript() == script)
            {
                return true;
            }
            index += 1;
        }
        return false;
    }

    static string ExtractActualText(byte[] pdf)
    {
        string text = Encoding.Latin1.GetString(pdf);
        string marker = "/ActualText <";
        int start = text.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
        {
            throw new AssertionException("The PDF has no ActualText marker.");
        }
        start += marker.Length;
        int end = text.IndexOf('>', start);
        if (end < 0)
        {
            throw new AssertionException("The PDF ActualText hex string is incomplete.");
        }
        byte[] bytes = System.Convert.FromHexString(
            text.Substring(start, end - start));
        if (bytes.Length < 2 || bytes[0] != 0xFE || bytes[1] != 0xFF)
        {
            throw new AssertionException("The PDF ActualText value has no UTF-16BE BOM.");
        }
        return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
    }

    static int IndependentSfntGlyphCount(byte[] font)
    {
        if (font.Length < 12)
        {
            throw new AssertionException("The embedded font header is truncated.");
        }
        uint scaler = ReadUInt32(font, 0);
        if (scaler != 0x00010000U && scaler != 0x4F54544FU)
        {
            throw new AssertionException("The embedded font scaler is unsupported.");
        }
        int tableCount = ReadUInt16(font, 4);
        if (tableCount <= 0 || 12 + tableCount * 16 > font.Length)
        {
            throw new AssertionException("The embedded table directory is invalid.");
        }
        int maxpOffset = -1;
        int maxpLength = 0;
        uint previousTag = 0;
        int index = 0;
        while (index < tableCount)
        {
            int record = 12 + index * 16;
            uint tag = ReadUInt32(font, record);
            uint checksum = ReadUInt32(font, record + 4);
            int offset = ReadInt32(font, record + 8);
            int length = ReadInt32(font, record + 12);
            if (index != 0 && tag <= previousTag)
            {
                throw new AssertionException("Embedded font tables are not in tag order.");
            }
            if (offset < 0 || length < 0 || offset > font.Length - length)
            {
                throw new AssertionException("An embedded font table is out of bounds.");
            }
            if (TableChecksum(font, offset, length, tag) != checksum)
            {
                throw new AssertionException("An embedded font table checksum is invalid.");
            }
            if (tag == 0x6D617870U)
            {
                maxpOffset = offset;
                maxpLength = length;
            }
            previousTag = tag;
            index += 1;
        }
        if (maxpOffset < 0 || maxpLength < 6)
        {
            throw new AssertionException("The embedded font has no complete maxp table.");
        }
        if (WholeFontChecksum(font) != 0xB1B0AFBAU)
        {
            throw new AssertionException("The embedded font checksum adjustment is invalid.");
        }
        return ReadUInt16(font, maxpOffset + 4);
    }

    static uint TableChecksum(
        byte[] font,
        int offset,
        int length,
        uint tag)
    {
        uint sum = 0;
        int index = 0;
        while (index < length)
        {
            uint value = 0;
            int byteIndex = 0;
            while (byteIndex < 4)
            {
                int tableIndex = index + byteIndex;
                byte current = tableIndex < length ?
                    font[offset + tableIndex] : (byte)0;
                if (tag == 0x68656164U &&
                    tableIndex >= 8 && tableIndex < 12)
                {
                    current = 0;
                }
                value = value << 8 | current;
                byteIndex += 1;
            }
            sum = unchecked(sum + value);
            index += 4;
        }
        return sum;
    }

    static uint WholeFontChecksum(byte[] font)
    {
        uint sum = 0;
        int index = 0;
        while (index < font.Length)
        {
            uint value = 0;
            int byteIndex = 0;
            while (byteIndex < 4)
            {
                int fontIndex = index + byteIndex;
                value = value << 8 |
                    (fontIndex < font.Length ? font[fontIndex] : (byte)0);
                byteIndex += 1;
            }
            sum = unchecked(sum + value);
            index += 4;
        }
        return sum;
    }

    static int ReadUInt16(byte[] value, int offset)
    {
        return value[offset] << 8 | value[offset + 1];
    }

    static int ReadInt32(byte[] value, int offset)
    {
        uint result = ReadUInt32(value, offset);
        if (result > int.MaxValue)
        {
            throw new AssertionException("An SFNT offset exceeds the test consumer range.");
        }
        return (int)result;
    }

    static uint ReadUInt32(byte[] value, int offset)
    {
        return (uint)value[offset] << 24 |
            (uint)value[offset + 1] << 16 |
            (uint)value[offset + 2] << 8 |
            value[offset + 3];
    }
}
