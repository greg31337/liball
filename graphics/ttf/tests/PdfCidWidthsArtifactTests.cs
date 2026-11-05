using ttf;

namespace ttf.Tests;

internal class PdfCidWidthsArtifactTests
{
    [Test]
    public void ConvertsAdvancesWithDefinedNearestHalfUpRounding()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PdfCidWidthsBuilder.ConvertAdvance(1024, 2048),
                Is.EqualTo(500));
            Assert.That(PdfCidWidthsBuilder.ConvertAdvance(2047, 2048),
                Is.EqualTo(1000));
            Assert.That(PdfCidWidthsBuilder.ConvertAdvance(1, 2000),
                Is.EqualTo(1));
            Assert.That(PdfCidWidthsBuilder.ConvertAdvance(1, 2001),
                Is.Zero);
            Assert.That(PdfCidWidthsBuilder.ConvertAdvance(2, 3),
                Is.EqualTo(667));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PdfCidWidthsBuilder.ConvertAdvance(-1, 1000));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PdfCidWidthsBuilder.ConvertAdvance(1, 0));
            Assert.Throws<FontLimitException>(() =>
                PdfCidWidthsBuilder.ConvertAdvance(int.MaxValue, 1));
        });
    }

    [Test]
    public void BuildsWidthsFromSourceGlyphsInAllocatedCidOrder()
    {
        FontInstance instance = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        PdfCidAllocator allocator = PdfCidAllocator.Create(4);
        allocator.GetOrAddCid(1);
        allocator.GetOrAddCid(2);
        allocator.GetOrAddCid(3);
        PdfCidAllocation allocation = allocator.BindSubsetGlyphs(
            new int[] { 0, 1, 2, 3 },
            new int[] { 0, 1, 2, 3 });

        PdfCidWidthsArtifact artifact = PdfCidWidthsBuilder.Build(
            instance,
            allocation);
        PdfCidWidthEntry entry = artifact.GetEntry(0);

        Assert.Multiple(() =>
        {
            Assert.That(artifact.GetCidCount(), Is.EqualTo(4));
            Assert.That(artifact.CopyWidths(),
                Is.EqualTo(new int[] { 600, 800, 800, 1000 }));
            Assert.That(artifact.GetWidth(1), Is.EqualTo(800));
            Assert.That(artifact.GetDefaultWidth(), Is.EqualTo(800));
            Assert.That(artifact.GetEntryCount(), Is.EqualTo(2));
            Assert.That(entry.GetKind(),
                Is.EqualTo(PdfCidWidthEntryKind.Array));
            Assert.That(entry.GetFirstCid(), Is.Zero);
            Assert.That(entry.GetLastCid(), Is.Zero);
            Assert.That(entry.CopyArrayWidths(),
                Is.EqualTo(new int[] { 600 }));
        });
    }

    [Test]
    public void CompactsRepeatedExceptionsAsRangesAndOthersAsArrays()
    {
        PdfCidWidthsArtifact artifact =
            PdfCidWidthsBuilder.BuildFromWidths(new int[]
            {
                500, 700, 700, 600, 800, 900, 900, 1000, 800, 800
            });

        Assert.Multiple(() =>
        {
            Assert.That(artifact.GetDefaultWidth(), Is.EqualTo(800));
            Assert.That(artifact.GetEntryCount(), Is.EqualTo(5));
            AssertArrayEntry(artifact.GetEntry(0), 0, new int[] { 500 });
            AssertRangeEntry(artifact.GetEntry(1), 1, 2, 700);
            AssertArrayEntry(artifact.GetEntry(2), 3, new int[] { 600 });
            AssertRangeEntry(artifact.GetEntry(3), 5, 6, 900);
            AssertArrayEntry(artifact.GetEntry(4), 7,
                new int[] { 1000 });
        });
    }

    [Test]
    public void DefaultWidthTiesPreferPdfDefaultThenSmallerWidth()
    {
        PdfCidWidthsArtifact pdfDefault =
            PdfCidWidthsBuilder.BuildFromWidths(
                new int[] { 700, 1000 });
        PdfCidWidthsArtifact smaller =
            PdfCidWidthsBuilder.BuildFromWidths(
                new int[] { 800, 700 });

        Assert.Multiple(() =>
        {
            Assert.That(pdfDefault.GetDefaultWidth(), Is.EqualTo(1000));
            Assert.That(smaller.GetDefaultWidth(), Is.EqualTo(700));
        });
    }

    [Test]
    public void ReturnsDefensiveArraysAndRejectsInvalidAccess()
    {
        PdfCidWidthsArtifact artifact =
            PdfCidWidthsBuilder.BuildFromWidths(
                new int[] { 500, 600, 1000, 1000 });
        int[] widths = artifact.CopyWidths();
        PdfCidWidthEntry?[] entries = artifact.GetEntries();
        int[] arrayWidths = artifact.GetEntry(0).CopyArrayWidths();
        widths[0] = 99;
        entries[0] = null;
        arrayWidths[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(artifact.GetWidth(0), Is.EqualTo(500));
            Assert.That(artifact.GetEntry(0), Is.Not.Null);
            Assert.That(artifact.GetEntry(0).CopyArrayWidths()[0],
                Is.EqualTo(500));
            Assert.Throws<InvalidOperationException>(() =>
                PdfCidWidthsBuilder.BuildFromWidths(
                    new int[] { 700, 700, 1000 })
                    .GetEntry(0).GetRangeWidth());
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                artifact.GetWidth(4));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                artifact.GetEntry(-1));
            Assert.Throws<ArgumentNullException>(() =>
                PdfCidWidthsBuilder.BuildFromWidths(null!));
            Assert.Throws<ArgumentException>(() =>
                PdfCidWidthsBuilder.BuildFromWidths(Array.Empty<int>()));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PdfCidWidthsBuilder.BuildFromWidths(new int[] { -1 }));
        });
    }

    [Test]
    public void FontProgramArtifactsExposeTheirHorizontalWidths()
    {
        FontInstance trueType = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        PdfFontUsage trueTypeUsage = CreateUsage(
            trueType,
            PdfFontOptions.Create());
        trueTypeUsage.AddGlyph(1, Array.Empty<int>());
        PdfCidFontType2Artifact type2 = PdfCidFontType2Builder.Build(
            trueTypeUsage,
            PdfCidAllocator.Create(trueTypeUsage));

        FontInstance cff = FontInstanceCffTests.CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            CffCharsetEncodingTests.BuildCidCff(false),
            4).CreateDefaultInstance();
        PdfFontUsage cffUsage = CreateUsage(cff, PdfFontOptions.Create());
        cffUsage.AddGlyph(1, Array.Empty<int>());
        PdfCidFontType0Artifact type0 = PdfCidFontType0Builder.Build(
            cffUsage,
            PdfCidAllocator.Create(cffUsage));

        PdfFontOptions openTypeOptions = PdfFontOptions.Create();
        openTypeOptions.SetFontKind(PdfFontKind.Type0OpenType);
        openTypeOptions.SetVersion(PdfVersion.Pdf1_6);
        PdfFontUsage openTypeUsage = CreateUsage(
            trueType,
            openTypeOptions);
        openTypeUsage.AddGlyph(2, Array.Empty<int>());
        PdfOpenTypeFontArtifact openType = PdfOpenTypeFontBuilder.Build(
            openTypeUsage,
            PdfCidAllocator.Create(openTypeUsage));

        Assert.Multiple(() =>
        {
            Assert.That(type2.GetWidths().CopyWidths(),
                Is.EqualTo(new int[] { 600, 800 }));
            Assert.That(type0.GetWidths().CopyWidths(),
                Is.EqualTo(new int[] { 600, 700 }));
            Assert.That(openType.GetWidths().CopyWidths(),
                Is.EqualTo(new int[] { 600, 800 }));
        });
    }

    [Test]
    public void RejectsMismatchedFontAndAllocation()
    {
        PdfCidAllocation allocation = PdfCidAllocator.Create(5)
            .BindSubsetGlyphs(
                new int[] { 0, -1, -1, -1, -1 },
                new int[] { 0 });

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() =>
                PdfCidWidthsBuilder.Build(null!, allocation));
            Assert.Throws<ArgumentException>(() =>
                PdfCidWidthsBuilder.Build(
                    FontInstanceTests.CreateFace().CreateDefaultInstance(),
                    allocation));
        });
    }

    static PdfFontUsage CreateUsage(
        FontInstance instance,
        PdfFontOptions options)
    {
        return new PdfFontUsageAccumulator().GetOrAdd(instance, options);
    }

    static void AssertRangeEntry(
        PdfCidWidthEntry entry,
        int firstCid,
        int lastCid,
        int width)
    {
        Assert.Multiple(() =>
        {
            Assert.That(entry.GetKind(),
                Is.EqualTo(PdfCidWidthEntryKind.Range));
            Assert.That(entry.GetFirstCid(), Is.EqualTo(firstCid));
            Assert.That(entry.GetLastCid(), Is.EqualTo(lastCid));
            Assert.That(entry.GetRangeWidth(), Is.EqualTo(width));
            Assert.That(entry.CopyArrayWidths(), Is.Empty);
        });
    }

    static void AssertArrayEntry(
        PdfCidWidthEntry entry,
        int firstCid,
        int[] widths)
    {
        Assert.Multiple(() =>
        {
            Assert.That(entry.GetKind(),
                Is.EqualTo(PdfCidWidthEntryKind.Array));
            Assert.That(entry.GetFirstCid(), Is.EqualTo(firstCid));
            Assert.That(entry.GetLastCid(),
                Is.EqualTo(firstCid + widths.Length - 1));
            Assert.That(entry.CopyArrayWidths(), Is.EqualTo(widths));
        });
    }
}
