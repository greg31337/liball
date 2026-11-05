using ttf;

namespace ttf.Tests;

internal class HarfBuzzLayoutDifferentialTests
{
    // This test-authored font was generated with fontTools 4.60.2 and has
    // SHA-256 977acd76ffbe6b956ecc6189964f8f49a8ef90d811c45ed3c60dcb3bfd91287e.
    // The expected glyph IDs, clusters, advances, and offsets below were
    // captured with hb-shape 14.3.0 using numeric glyph output.
    internal const string OracleFontBase64 =
        "AAEAAAANAIAAAwBQR0RFRgAaAAUAAAV4AAAAJEdQT1PscM+5AAAFnAAAAQBHU1VC+qnuhwAABpwAAABiT1MvMkfASAcAAAFYAAAA" +
        "YGNtYXALqAH3AAAB6AAAAHJnbHlmpLnEGwAAAnwAAAEeaGVhZC+JDOgAAADcAAAANmhoZWEF3gOOAAABFAAAACRobXR4EsAAAAAA" +
        "AbgAAAAubG9jYQHuAa0AAAJcAAAAHm1heHAAEAAGAAABOAAAACBuYW1lqSHe+gAAA5wAAAFxcG9zdHutxHEAAAUQAAAAaAABAAAA" +
        "AQAAMxaR018PPPUAAQPoAAAAAOaqZKYAAAAA5qpkpgAyAAAC7gK8AAAAAwACAAAAAAAAAAEAAAMg/zgAAAOEAAAAyAK8AAEAAAAA" +
        "AAAAAAAAAAAAAAAJAAEAAAAOAAQAAQAAAAAAAgAAAAAAAAAAAAAAAAAAAAAAAwJYAZAABQAEAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAALBAAAAAAAAAAAAAAAAPz8/PwAAACAEEQMg/zgAAAMgAMgAAAAAAAAAAAAAAAAAAAAgAAICWAAAASwAAAJY" +
        "AAACWAAAAlgAAAJYAAADhAAAAAAAAAJYAAAAAAAAAAAAAAAAAAAAAAACAAAAAwAAABQAAwABAAAAFAAEAF4AAAASABAAAwACACAA" +
        "QQBWAGYAawMBA5IEEf//AAAAIABBAFYAZgBpAwEDkQQQ////4f/B/63/ngAA/Qb8d/v6AAEAAAAAAAAAAAAKAAAAAAAAAAAABQAM" +
        "AA0AAAAAAAAAAAANABoAJwA0AEEAQQBOAFsAaAB1AIIAjwAAAAEAMgAAAcICvAADAAAzIREhMgGQ/nACvAAAAQAyAAABwgK8AAMA" +
        "ADMhESEyAZD+cAK8AAABADIAAAHCArwAAwAAMyERITIBkP5wArwAAAEAMgAAAcICvAADAAAzIREhMgGQ/nACvAAAAQAyAAAC7gK8" +
        "AAMAADMhESEyArz9RAK8AAABADIAAAHCArwAAwAAMyERITIBkP5wArwAAAEAMgAAAcICvAADAAAzIREhMgGQ/nACvAAAAQAyAAAB" +
        "wgK8AAMAADMhESEyAZD+cAK8AAABADIAAAHCArwAAwAAMyERITIBkP5wArwAAAEAMgAAAcICvAADAAAzIREhMgGQ/nACvAAAAQAy" +
        "AAABwgK8AAMAADMhESEyAZD+cAK8AAAAAAAACgB+AAEAAAAAAAEAFAAAAAEAAAAAAAIABwAUAAEAAAAAAAMAGgAbAAEAAAAAAAQA" +
        "HAA1AAEAAAAAAAYAGgAbAAMAAQQJAAEAKABRAAMAAQQJAAIADgB5AAMAAQQJAAMANACHAAMAAQQJAAQAOAC7AAMAAQQJAAYANACH" +
        "VDExOCBIYXJmQnV6eiBPcmFjbGVSZWd1bGFyVDExOEhhcmZCdXp6T3JhY2xlLVJlZ3VsYXJUMTE4IEhhcmZCdXp6IE9yYWNsZSBS" +
        "ZWd1bGFyAFQAMQAxADgAIABIAGEAcgBmAEIAdQB6AHoAIABPAHIAYQBjAGwAZQBSAGUAZwB1AGwAYQByAFQAMQAxADgASABhAHIA" +
        "ZgBCAHUAegB6AE8AcgBhAGMAbABlAC0AUgBlAGcAdQBsAGEAcgBUADEAMQA4ACAASABhAHIAZgBCAHUAegB6ACAATwByAGEAYwBs" +
        "AGUAIABSAGUAZwB1AGwAYQByAAAAAAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADgAAAAMAJAA5AEkATADAAQIBAwEE" +
        "AQUBBgBNAE4JYWN1dGVjb21iBUFscGhhBEJldGEJQWN5cmlsbGljCkJlY3lyaWxsaWMAAQAAAAwAAAAAAAAAAQACAAkAAQAAAAAA" +
        "AAAAAAMAAQAAAAEAAQAAAAoANABaAARERkxUABpjeXJsABpncmVrABpsYXRuABoABAAAAAD//wADAAAAAQACAANjdXJzABRrZXJu" +
        "ABptYXJrACAAAAABAAIAAAABAAAAAAABAAEAAwAIADIAfAACAAAAAQAIAAEAPAAEAAAAAwAQABYAHAABAAP/sAABAAn/ugABAAv/" +
        "xAAEAAAAAQAIAAEADAASAAEAHAAoAAEAAQAHAAEAAwACAAgACgABAAAABgABAAAB9AADAAgADgAUAAEBLAK8AAEBQALQAAEBVALk" +
        "AAMAAAABAAgAAQAOAAIAAAAWABwAAAABAAIADAANAAEB9AAyAAEAZACWAAEAAAAKADAAPgAEREZMVAAaY3lybAAaZ3JlawAabGF0" +
        "bgAaAAQAAAAA//8AAQAAAAFsaWdhAAgAAAABAAAAAQAEAAQAAAABAAgAAQASAAEACAABAAQABgACAAUAAQABAAQAAA==";

    static int Cursive = Tag("curs");
    static int Cyrillic = Tag("cyrl");
    static int Greek = Tag("grek");
    static int Kerning = Tag("kern");
    static int Latin = Tag("latn");
    static int Ligatures = Tag("liga");
    static int MarkPositioning = Tag("mark");

    [TestCase(0x0041, 0x0056, 520, 600, "latn")]
    [TestCase(0x0391, 0x0392, 530, 600, "grek")]
    [TestCase(0x0410, 0x0411, 540, 600, "cyrl")]
    public void KerningMatchesPinnedHarfBuzzAdvances(
        int firstScalar,
        int secondScalar,
        int firstAdvance,
        int secondAdvance,
        string script)
    {
        FontFace face = Load();
        int[] glyphIds = Map(face, firstScalar, secondScalar);
        SfntGposPositionBuffer buffer = CreatePositionBuffer(face, glyphIds);

        ApplyGposFeature(face, buffer, Tag(script), Kerning);

        Assert.Multiple(() =>
        {
            Assert.That(CopyGlyphIds(buffer), Is.EqualTo(glyphIds));
            Assert.That(buffer.GetXAdvance(0), Is.EqualTo(firstAdvance));
            Assert.That(buffer.GetXAdvance(1), Is.EqualTo(secondAdvance));
            Assert.That(buffer.GetXPlacement(0), Is.Zero);
            Assert.That(buffer.GetXPlacement(1), Is.Zero);
        });
    }

    [Test]
    public void LatinLigatureMatchesPinnedHarfBuzzGlyphAndCluster()
    {
        FontFace face = Load();
        int[] glyphIds = Map(face, 0x0066, 0x0069);
        SfntLayoutGlyphBuffer buffer = SfntLayoutGlyphBuffer.Create(glyphIds);

        ApplyGsubFeature(face, buffer, Latin, Ligatures);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetCount(), Is.EqualTo(1));
            Assert.That(buffer.GetGlyphId(0), Is.EqualTo(6));
            Assert.That(buffer.GetClusterStart(0), Is.Zero);
            Assert.That(buffer.GetClusterLength(0), Is.EqualTo(2));
        });
    }

    [TestCase(0x0041, -300, 200, "latn")]
    [TestCase(0x0391, -280, 220, "grek")]
    [TestCase(0x0410, -260, 240, "cyrl")]
    public void MarkAttachmentMatchesPinnedHarfBuzzOffsets(
        int baseScalar,
        int expectedX,
        int expectedY,
        string script)
    {
        FontFace face = Load();
        int[] glyphIds = Map(face, baseScalar, 0x0301);
        SfntGposPositionBuffer buffer = CreatePositionBuffer(face, glyphIds);

        ApplyGposFeature(face, buffer, Tag(script), MarkPositioning);
        buffer.ResolveAttachments(SfntLayoutTraversalDirection.Forward);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetXAdvance(0), Is.EqualTo(600));
            Assert.That(buffer.GetXAdvance(1), Is.Zero);
            Assert.That(buffer.GetXPlacement(1), Is.EqualTo(expectedX));
            Assert.That(buffer.GetYPlacement(1), Is.EqualTo(expectedY));
            Assert.That(buffer.GetAttachmentParent(1), Is.Zero);
        });
    }

    [Test]
    public void CursiveAttachmentMatchesPinnedHarfBuzzAdvancesAndOffsets()
    {
        FontFace face = Load();
        int[] glyphIds = Map(face, 0x006A, 0x006B);
        SfntGposPositionBuffer buffer = CreatePositionBuffer(face, glyphIds);

        ApplyGposFeature(face, buffer, Latin, Cursive);
        buffer.ResolveAttachments(SfntLayoutTraversalDirection.Forward);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetXAdvance(0), Is.EqualTo(500));
            Assert.That(buffer.GetXAdvance(1), Is.EqualTo(500));
            Assert.That(buffer.GetXPlacement(1), Is.EqualTo(-100));
            Assert.That(buffer.GetYPlacement(1), Is.EqualTo(-100));
            Assert.That(buffer.GetAttachmentParent(1), Is.Zero);
        });
    }

    [Test]
    public void FeatureRangeMatchesPinnedHarfBuzzPartialLigatureResult()
    {
        FontFace face = Load();
        SfntGsubTable? table = face.GetGsubTable();
        SfntLayoutFeaturePlan plan = SfntLayoutFeaturePlanBuilder.Build(
            new int[0],
            new int[] { Ligatures },
            new int[0],
            new SfntUserFeature[]
            {
                new SfntUserFeature(Ligatures, 0, 0, 2)
            },
            4);
        SfntLayoutSelection selection = plan.Select(
            table!.GetLayoutTable(),
            Latin,
            0);
        int lookupIndex = GetSingleLookup(selection);
        SfntLayoutGlyphBuffer enabledSuffix = SfntLayoutGlyphBuffer.Create(
            Map(face, 0x0066, 0x0069),
            new int[] { 2, 3 },
            new int[] { 1, 1 },
            new int[] { 1, 1 },
            new int[] { 0, 0 });

        SfntGsubExecutor.ApplyLookup(
            table,
            lookupIndex,
            enabledSuffix,
            face.GetGdefTable()!,
            SfntLayoutTraversalDirection.Forward,
            1);

        Assert.Multiple(() =>
        {
            SfntLayoutLookupSelection lookup = selection.GetLookup(0);
            Assert.That(lookup.GetValueAt(0), Is.Zero);
            Assert.That(lookup.GetValueAt(1), Is.Zero);
            Assert.That(lookup.GetValueAt(2), Is.EqualTo(1));
            Assert.That(lookup.GetValueAt(3), Is.EqualTo(1));
            Assert.That(enabledSuffix.GetCount(), Is.EqualTo(1));
            Assert.That(enabledSuffix.GetGlyphId(0), Is.EqualTo(6));
            Assert.That(enabledSuffix.GetClusterStart(0), Is.EqualTo(2));
            Assert.That(new int[] { 4, 5, enabledSuffix.GetGlyphId(0) },
                Is.EqualTo(new int[] { 4, 5, 6 }));
        });
    }

    static void ApplyGsubFeature(
        FontFace face,
        SfntLayoutGlyphBuffer buffer,
        int scriptTag,
        int featureTag)
    {
        SfntGsubTable? table = face.GetGsubTable();
        SfntLayoutSelection selection = SelectFeature(
            table!.GetLayoutTable(),
            scriptTag,
            featureTag,
            buffer.GetCount());
        SfntGsubExecutor.ApplyLookup(
            table,
            GetSingleLookup(selection),
            buffer,
            face.GetGdefTable()!,
            SfntLayoutTraversalDirection.Forward,
            1);
    }

    static void ApplyGposFeature(
        FontFace face,
        SfntGposPositionBuffer buffer,
        int scriptTag,
        int featureTag)
    {
        SfntGposTable? table = face.GetGposTable();
        SfntLayoutSelection selection = SelectFeature(
            table!.GetLayoutTable(),
            scriptTag,
            featureTag,
            buffer.GetCount());
        SfntGposExecutor.ApplyLookup(
            table,
            GetSingleLookup(selection),
            buffer,
            face.GetGdefTable()!,
            SfntLayoutTraversalDirection.Forward,
            new SfntGposAdjustmentContext(1000, 0, 0, null));
    }

    static SfntLayoutSelection SelectFeature(
        SfntLayoutTable table,
        int scriptTag,
        int featureTag,
        int textLength)
    {
        SfntLayoutFeaturePlan plan = SfntLayoutFeaturePlanBuilder.Build(
            new int[0],
            new int[] { featureTag },
            new int[0],
            new SfntUserFeature[0],
            textLength);
        SfntLayoutSelection selection = plan.Select(table, scriptTag, 0);
        Assert.That(selection.HasFeature(featureTag), Is.True);
        return selection;
    }

    static int GetSingleLookup(SfntLayoutSelection selection)
    {
        Assert.That(selection.GetLookupCount(), Is.EqualTo(1));
        return selection.GetLookup(0).GetLookupIndex();
    }

    static SfntGposPositionBuffer CreatePositionBuffer(
        FontFace face,
        int[] glyphIds)
    {
        SfntGposPositionBuffer buffer = SfntGposPositionBuffer.Create(glyphIds);
        int index = 0;

        while (index < glyphIds.Length)
        {
            buffer.SetXAdvance(
                index,
                face.GetDesignGlyphMetrics(glyphIds[index], 700)
                    .GetAdvanceWidth());
            index += 1;
        }

        return buffer;
    }

    static int[] Map(FontFace face, params int[] scalars)
    {
        int[] glyphIds = new int[scalars.Length];
        int index = 0;

        while (index < scalars.Length)
        {
            glyphIds[index] = face.GetCmapMapping()!.MapUnicode(scalars[index]);
            index += 1;
        }

        return glyphIds;
    }

    static int[] CopyGlyphIds(SfntGposPositionBuffer buffer)
    {
        int[] glyphIds = new int[buffer.GetCount()];
        int index = 0;

        while (index < glyphIds.Length)
        {
            glyphIds[index] = buffer.GetGlyphId(index);
            index += 1;
        }

        return glyphIds;
    }

    internal static FontFace Load()
    {
        return FontLoader.Load(
            System.Convert.FromBase64String(OracleFontBase64),
            FontLoadOptions.CreateStrict()).GetFace(0);
    }

    static int Tag(string value)
    {
        return (int)TestSfntEncoding.EncodeTag(value);
    }
}
