using ttf;

namespace ttf.Tests;

internal class HarfBuzzShapingModelCorpusTests
{
    [Test]
    public void ShapingModelsMatchPinnedHarfBuzzCorpus()
    {
        Assert.Multiple(() =>
        {
            AssertFace(
                HarfBuzzLayoutDifferentialTests.Load(),
                "default",
                "fi",
                TextShapingPlanKind.Default,
                new int[] { 6 },
                new int[] { 0 },
                new int[] { 900 },
                new int[] { 0 },
                new int[] { 0 });
            AssertCase(
                "arabic.ttf",
                "س",
                TextShapingPlanKind.Arabic,
                new int[] { 5 },
                new int[] { 0 },
                new int[] { 1209 },
                new int[] { 0 },
                new int[] { 0 });
            AssertCase(
                "hebrew.ttf",
                "ש",
                TextShapingPlanKind.Hebrew,
                new int[] { 5 },
                new int[] { 0 },
                new int[] { 730 },
                new int[] { 0 },
                new int[] { 0 });
            AssertCase(
                "indic.ttf",
                "क",
                TextShapingPlanKind.Indic,
                new int[] { 2 },
                new int[] { 0 },
                new int[] { 768 },
                new int[] { 0 },
                new int[] { 0 });
            AssertCase(
                "khmer.ttf",
                "ខ",
                TextShapingPlanKind.Khmer,
                new int[] { 1 },
                new int[] { 0 },
                new int[] { 635 },
                new int[] { 0 },
                new int[] { 0 });
            AssertCase(
                "myanmar.ttf",
                "မ",
                TextShapingPlanKind.Myanmar,
                new int[] { 2 },
                new int[] { 0 },
                new int[] { 676 },
                new int[] { 0 },
                new int[] { 0 });
            AssertCase(
                "thai.ttf",
                "ภ",
                TextShapingPlanKind.ThaiLao,
                new int[] { 1 },
                new int[] { 0 },
                new int[] { 603 },
                new int[] { 0 },
                new int[] { 0 });
            AssertCase(
                "hangul.ttf",
                "한",
                TextShapingPlanKind.Hangul,
                new int[] { 2 },
                new int[] { 0 },
                new int[] { 920 },
                new int[] { 0 },
                new int[] { 0 });
            AssertCase(
                "use.ttf",
                "ᰛ",
                TextShapingPlanKind.Universal,
                new int[] { 1 },
                new int[] { 0 },
                new int[] { 509 },
                new int[] { 0 },
                new int[] { 0 });
        });
    }

    static void AssertCase(
        string fontName,
        string text,
        TextShapingPlanKind kind,
        int[] glyphIds,
        int[] clusters,
        int[] advances,
        int[] xOffsets,
        int[] yOffsets)
    {
        FontFace face = LoadCorpusFace(fontName);
        AssertFace(
            face,
            fontName,
            text,
            kind,
            glyphIds,
            clusters,
            advances,
            xOffsets,
            yOffsets);
    }

    internal static FontFace LoadCorpusFace(string fontName)
    {
        byte[] archiveBytes = System.Convert.FromBase64String(
            HarfBuzzShapingModelCorpusData.ArchiveBase64);
        using (System.IO.MemoryStream archiveStream =
            new System.IO.MemoryStream(archiveBytes, false))
        using (System.IO.Compression.ZipArchive archive =
            new System.IO.Compression.ZipArchive(
                archiveStream,
                System.IO.Compression.ZipArchiveMode.Read))
        {
            System.IO.Compression.ZipArchiveEntry? entry = archive.GetEntry(
                fontName);
            if (entry == null)
            {
                throw new System.IO.InvalidDataException(
                    "The shaping corpus does not contain " + fontName + ".");
            }

            using (System.IO.Stream input = entry.Open())
            using (System.IO.MemoryStream fontBytes =
                new System.IO.MemoryStream())
            {
                input.CopyTo(fontBytes);
                return FontLoader.Load(
                    fontBytes.ToArray(),
                    FontLoadOptions.CreateStrict()).GetFace(0);
            }
        }
    }

    static void AssertFace(
        FontFace face,
        string name,
        string text,
        TextShapingPlanKind kind,
        int[] glyphIds,
        int[] clusters,
        int[] advances,
        int[] xOffsets,
        int[] yOffsets)
    {
        TextShapingResult result = face.CreateDefaultInstance().ShapeText(text);

        Assert.That(result.GetRunCount(), Is.EqualTo(1), name);
        TextShapingRun shapingRun = result.GetRun(0);
        ShapedRun run = shapingRun.GetShapedRun();
        Assert.That(shapingRun.GetPlanKind(), Is.EqualTo(kind), name);
        Assert.That(shapingRun.GetPlanStageCount(), Is.GreaterThan(0), name);
        Assert.That(run.GetGlyphCount(), Is.EqualTo(glyphIds.Length), name);
        int index = 0;
        while (index < glyphIds.Length)
        {
            ShapedGlyph glyph = run.GetGlyph(index);
            Assert.Multiple(() =>
            {
                Assert.That(glyph.GetGlyphId(), Is.EqualTo(glyphIds[index]),
                    name + " glyph " + index);
                Assert.That(glyph.GetUtf16Start(), Is.EqualTo(clusters[index]),
                    name + " cluster " + index);
                Assert.That(glyph.GetXAdvance(), Is.EqualTo(advances[index]),
                    name + " advance " + index);
                Assert.That(glyph.GetXOffset(), Is.EqualTo(xOffsets[index]),
                    name + " x offset " + index);
                Assert.That(glyph.GetYOffset(), Is.EqualTo(yOffsets[index]),
                    name + " y offset " + index);
            });
            index += 1;
        }
    }
}
