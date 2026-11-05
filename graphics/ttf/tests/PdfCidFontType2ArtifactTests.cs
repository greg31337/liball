using ttf;

namespace ttf.Tests;

internal class PdfCidFontType2ArtifactTests
{
    [Test]
    public void BuildsFontFile2WithIndependentCidAndSubsetGlyphMaps()
    {
        FontInstance instance = HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
        PdfFontUsage usage = new PdfFontUsageAccumulator().GetOrAdd(
            instance,
            PdfFontOptions.Create());
        usage.AddGlyph(13, new int[] { 0x43 });
        usage.AddGlyph(2, new int[] { 0x41 });
        PdfCidAllocator allocator = PdfCidAllocator.Create(usage);

        PdfCidFontType2Artifact artifact =
            PdfCidFontType2Builder.Build(usage, allocator);
        byte[] fontFile2 = artifact.CopyFontFile2();
        FontPackage reopened = FontLoader.Load(
            fontFile2,
            FontLoadOptions.CreateStrict());

        Assert.Multiple(() =>
        {
            Assert.That(FontChecksum.VerifyFont(fontFile2), Is.True);
            Assert.That(reopened.GetFace(0).GetOutlineKind(),
                Is.EqualTo(FontOutlineKind.TrueType));
            Assert.That(reopened.GetFace(0).GetMaxpTable()!.GetGlyphCount(),
                Is.EqualTo(3));
            Assert.That(artifact.GetFontKind(),
                Is.EqualTo(PdfFontKind.Type0CidFontType2));
            Assert.That(artifact.GetBaseFontName(),
                Does.Match("^[A-Z]{6}\\+"));
            Assert.That(artifact.GetCidCount(), Is.EqualTo(3));
            Assert.That(artifact.CopyCidToSourceGlyphIds(),
                Is.EqualTo(new int[] { 0, 13, 2 }));
            Assert.That(artifact.CopySourceGlyphToCids()[13],
                Is.EqualTo(1));
            Assert.That(artifact.CopySourceGlyphToCids()[2],
                Is.EqualTo(2));
            Assert.That(artifact.CopyCidToSubsetGlyphIds(),
                Is.EqualTo(new int[] { 0, 2, 1 }));
            Assert.That(artifact.CopySubsetGlyphToCids(),
                Is.EqualTo(new int[] { 0, 2, 1 }));
            Assert.That(artifact.CopyOldToNewGlyphIds()[13],
                Is.EqualTo(2));
            Assert.That(artifact.CopyOldToNewGlyphIds()[2],
                Is.EqualTo(1));
            Assert.That(artifact.CopyNewToOldGlyphIds(),
                Is.EqualTo(new int[] { 0, 2, 13 }));
            Assert.That(artifact.GetSourceGlyphId(1), Is.EqualTo(13));
            Assert.That(artifact.GetSubsetGlyphId(1), Is.EqualTo(2));
            Assert.That(artifact.FindCidForSourceGlyph(2), Is.EqualTo(2));
            Assert.That(artifact.FindCidForSubsetGlyph(1), Is.EqualTo(2));
            Assert.That(artifact.GetEmbeddingDecision()
                .GetEffectiveEmbeddingMode(),
                Is.EqualTo(FontSubsetEmbeddingMode.Subset));
        });
    }

    [Test]
    public void FullFontPreservesGidsWithoutEquatingThemToCids()
    {
        FontInstance instance = HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
        PdfFontOptions options = PdfFontOptions.Create();
        options.SetEmbeddingMode(PdfFontEmbeddingMode.FullFont);
        PdfFontUsage usage = new PdfFontUsageAccumulator().GetOrAdd(
            instance,
            options);
        PdfCidAllocator allocator = PdfCidAllocator.Create(usage);
        usage.AddGlyph(13, Array.Empty<int>());
        usage.AddGlyph(2, Array.Empty<int>());

        PdfCidFontType2Artifact artifact =
            PdfCidFontType2Builder.Build(usage, allocator);
        FontPackage reopened = FontLoader.Load(
            artifact.CopyFontFile2(),
            FontLoadOptions.CreateStrict());

        Assert.Multiple(() =>
        {
            Assert.That(reopened.GetFace(0).GetMaxpTable()!.GetGlyphCount(),
                Is.EqualTo(instance.GetGlyphCount()));
            Assert.That(artifact.CopyCidToSourceGlyphIds(),
                Is.EqualTo(new int[] { 0, 13, 2 }));
            Assert.That(artifact.CopyCidToSubsetGlyphIds(),
                Is.EqualTo(new int[] { 0, 13, 2 }));
            Assert.That(artifact.CopyOldToNewGlyphIds()[13],
                Is.EqualTo(13));
            Assert.That(artifact.GetEmbeddingDecision()
                .GetEffectiveEmbeddingMode(),
                Is.EqualTo(FontSubsetEmbeddingMode.FullFont));
        });
    }

    [Test]
    public void ReturnsDefensiveProgramAndMappingCopies()
    {
        FontInstance instance = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        PdfFontUsage usage = new PdfFontUsageAccumulator().GetOrAdd(
            instance,
            PdfFontOptions.Create());
        usage.AddGlyph(1, Array.Empty<int>());
        PdfCidFontType2Artifact artifact = PdfCidFontType2Builder.Build(
            usage,
            PdfCidAllocator.Create(usage));
        byte[] bytes = artifact.CopyFontFile2();
        int[] cidToGlyph = artifact.CopyCidToSubsetGlyphIds();
        bytes[0] = 99;
        cidToGlyph[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(artifact.CopyFontFile2()[0], Is.Not.EqualTo(99));
            Assert.That(artifact.CopyCidToSubsetGlyphIds()[0], Is.Zero);
        });
    }

    [Test]
    public void EmbedsEveryDirectlyAllocatedCidGlyph()
    {
        FontInstance instance = HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
        PdfFontUsage usage = new PdfFontUsageAccumulator().GetOrAdd(
            instance,
            PdfFontOptions.Create());
        PdfCidAllocator allocator = PdfCidAllocator.Create(usage);
        allocator.GetOrAddCid(13);

        PdfCidFontType2Artifact artifact =
            PdfCidFontType2Builder.Build(usage, allocator);
        FontPackage reopened = FontLoader.Load(
            artifact.CopyFontFile2(),
            FontLoadOptions.CreateStrict());

        Assert.Multiple(() =>
        {
            Assert.That(artifact.CopyCidToSourceGlyphIds(),
                Is.EqualTo(new int[] { 0, 13 }));
            Assert.That(artifact.CopyCidToSubsetGlyphIds(),
                Is.EqualTo(new int[] { 0, 1 }));
            Assert.That(reopened.GetFace(0).GetMaxpTable()!.GetGlyphCount(),
                Is.EqualTo(2));
        });
    }

    [Test]
    public void RejectsIncompatibleProgramsKindsVersionsAndAllocators()
    {
        FontInstance trueType = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        FontFace cffFace = FontInstanceCffTests.CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            CffCharsetEncodingTests.BuildCidCff(false),
            4);
        PdfFontOptions wrongKind = PdfFontOptions.Create();
        wrongKind.SetFontKind(PdfFontKind.Type0OpenType);
        PdfFontOptions oldVersion = PdfFontOptions.Create();
        oldVersion.SetVersion(PdfVersion.Pdf1_1);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() =>
                PdfCidFontType2Builder.Build(null!, PdfCidAllocator.Create(4)));
            Assert.Throws<InvalidOperationException>(() => Build(
                cffFace.CreateDefaultInstance(),
                PdfFontOptions.Create()));
            Assert.Throws<InvalidOperationException>(() => Build(
                trueType,
                wrongKind));
            Assert.Throws<InvalidOperationException>(() => Build(
                trueType,
                oldVersion));
            Assert.Throws<ArgumentException>(() =>
                PdfCidFontType2Builder.Build(
                    CreateUsage(trueType, PdfFontOptions.Create()),
                    PdfCidAllocator.Create(5)));
        });
    }

    static PdfCidFontType2Artifact Build(
        FontInstance instance,
        PdfFontOptions options)
    {
        PdfFontUsage usage = CreateUsage(instance, options);
        return PdfCidFontType2Builder.Build(
            usage,
            PdfCidAllocator.Create(usage));
    }

    static PdfFontUsage CreateUsage(
        FontInstance instance,
        PdfFontOptions options)
    {
        return new PdfFontUsageAccumulator().GetOrAdd(instance, options);
    }
}
