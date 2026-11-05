using ttf;

namespace ttf.Tests;

internal class PdfCidMappingArtifactTests
{
    [Test]
    public void SelectsIdentityForMatchingType2GlyphIdentifiers()
    {
        PdfCidAllocator allocator = PdfCidAllocator.Create(3);
        allocator.GetOrAddCid(1);
        allocator.GetOrAddCid(2);
        PdfCidAllocation allocation = allocator.BindSubsetGlyphs(
            new int[] { 0, 1, 2 },
            new int[] { 0, 1, 2 });
        PdfFontOptions options = PdfFontOptions.Create();
        options.SetArchivalProfile(PdfArchivalProfile.PdfA1);

        PdfCidMappingArtifact artifact =
            PdfCidMappingBuilder.BuildType2(allocation, options);
        PdfCidSystemInfo systemInfo = artifact.GetCidSystemInfo();

        Assert.Multiple(() =>
        {
            Assert.That(systemInfo.GetRegistry(), Is.EqualTo("Adobe"));
            Assert.That(systemInfo.GetOrdering(), Is.EqualTo("Identity"));
            Assert.That(systemInfo.GetSupplement(), Is.Zero);
            Assert.That(artifact.GetCidCount(), Is.EqualTo(3));
            Assert.That(artifact.HasCidToGidMap(), Is.True);
            Assert.That(artifact.UsesIdentityCidToGidMap(), Is.True);
            Assert.That(artifact.GetCidToGidMapName(),
                Is.EqualTo("Identity"));
            Assert.That(artifact.GetMappedGlyphId(2), Is.EqualTo(2));
            Assert.That(artifact.CopyCidToGidMap(), Is.Empty);
            Assert.That(artifact.HasCidSet(), Is.True);
            Assert.That(artifact.CopyCidSet(),
                Is.EqualTo(new byte[] { 0xE0 }));
        });
    }

    [Test]
    public void WritesNonIdentityType2MapInBigEndianCidOrder()
    {
        PdfCidAllocator allocator = PdfCidAllocator.Create(3);
        allocator.GetOrAddCid(1);
        allocator.GetOrAddCid(2);
        PdfCidAllocation allocation = allocator.BindSubsetGlyphs(
            new int[] { 0, 2, 1 },
            new int[] { 0, 2, 1 });

        PdfCidMappingArtifact artifact =
            PdfCidMappingBuilder.BuildType2(
                allocation,
                PdfFontOptions.Create());
        byte[] map = artifact.CopyCidToGidMap();
        map[3] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(artifact.HasCidToGidMap(), Is.True);
            Assert.That(artifact.UsesIdentityCidToGidMap(), Is.False);
            Assert.That(artifact.GetCidToGidMapName(), Is.Empty);
            Assert.That(artifact.CopyCidToGidMap(), Is.EqualTo(
                new byte[] { 0, 0, 0, 2, 0, 1 }));
            Assert.That(artifact.GetMappedGlyphId(1), Is.EqualTo(2));
            Assert.That(artifact.HasCidSet(), Is.False);
            Assert.That(artifact.CopyCidSet(), Is.Empty);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                artifact.GetMappedGlyphId(3));
        });
    }

    [Test]
    public void Type0OmitsMapAndBuildsSparsePdfA1CidSetFromCharset()
    {
        PdfCidAllocation allocation = PdfCidAllocator.Create(3)
            .BindSubsetGlyphs(
                new int[] { 0, -1, -1 },
                new int[] { 0 });
        PdfFontOptions options = PdfFontOptions.Create();
        options.SetArchivalProfile(PdfArchivalProfile.PdfA1);

        PdfCidMappingArtifact artifact =
            PdfCidMappingBuilder.BuildType0(
                allocation,
                new int[] { 0, 9, 2 },
                options);
        byte[] cidSet = artifact.CopyCidSet();
        cidSet[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(artifact.GetCidCount(), Is.EqualTo(1));
            Assert.That(artifact.HasCidToGidMap(), Is.False);
            Assert.That(artifact.UsesIdentityCidToGidMap(), Is.False);
            Assert.That(artifact.GetCidToGidMapName(), Is.Empty);
            Assert.That(artifact.CopyCidToGidMap(), Is.Empty);
            Assert.That(artifact.HasCidSet(), Is.True);
            Assert.That(artifact.CopyCidSet(),
                Is.EqualTo(new byte[] { 0xA0, 0x40 }));
            Assert.Throws<InvalidOperationException>(() =>
                artifact.GetMappedGlyphId(0));
        });
    }

    [Test]
    public void ProgramArtifactsExposeMappingsForTheirDescendantKinds()
    {
        FontInstance trueType = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        PdfFontOptions trueTypeOptions = PdfFontOptions.Create();
        trueTypeOptions.SetArchivalProfile(PdfArchivalProfile.PdfA1);
        PdfFontUsage trueTypeUsage = CreateUsage(
            trueType,
            trueTypeOptions);
        trueTypeUsage.AddGlyph(3, Array.Empty<int>());
        trueTypeUsage.AddGlyph(1, Array.Empty<int>());
        PdfCidFontType2Artifact type2 = PdfCidFontType2Builder.Build(
            trueTypeUsage,
            PdfCidAllocator.Create(trueTypeUsage));

        FontInstance cff = FontInstanceCffTests.CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            CffCharsetEncodingTests.BuildCidCff(false),
            4).CreateDefaultInstance();
        PdfFontOptions cffOptions = PdfFontOptions.Create();
        cffOptions.SetArchivalProfile(PdfArchivalProfile.PdfA1);
        PdfFontUsage cffUsage = CreateUsage(cff, cffOptions);
        cffUsage.AddGlyph(3, Array.Empty<int>());
        PdfCidFontType0Artifact type0 = PdfCidFontType0Builder.Build(
            cffUsage,
            PdfCidAllocator.Create(cffUsage));

        Assert.Multiple(() =>
        {
            Assert.That(type2.GetCidMapping().HasCidToGidMap(), Is.True);
            Assert.That(type2.GetCidMapping().CopyCidToGidMap(),
                Is.EqualTo(new byte[] { 0, 0, 0, 2, 0, 1 }));
            Assert.That(type2.GetCidMapping().CopyCidSet(),
                Is.EqualTo(new byte[] { 0xE0 }));
            Assert.That(type0.GetCidMapping().HasCidToGidMap(), Is.False);
            Assert.That(type0.GetCidMapping().CopyCidSet(),
                Is.EqualTo(new byte[] { 0xC0 }));
        });
    }

    [Test]
    public void OpenTypeArtifactUsesItsResolvedDescendantMappingKind()
    {
        FontInstance instance = HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
        PdfFontOptions options = PdfFontOptions.Create();
        options.SetFontKind(PdfFontKind.Type0OpenType);
        options.SetVersion(PdfVersion.Pdf1_6);
        PdfFontUsage usage = CreateUsage(instance, options);
        usage.AddGlyph(13, Array.Empty<int>());
        usage.AddGlyph(2, Array.Empty<int>());

        PdfOpenTypeFontArtifact artifact = PdfOpenTypeFontBuilder.Build(
            usage,
            PdfCidAllocator.Create(usage));

        Assert.Multiple(() =>
        {
            Assert.That(artifact.GetDescendantFontKind(),
                Is.EqualTo(PdfFontKind.Type0CidFontType2));
            Assert.That(artifact.GetCidMapping().HasCidToGidMap(), Is.True);
            Assert.That(artifact.GetCidMapping().CopyCidToGidMap(),
                Is.EqualTo(new byte[] { 0, 0, 0, 2, 0, 1 }));
        });
    }

    [Test]
    public void RejectsMissingAndInvalidMappingInputs()
    {
        PdfCidAllocation allocation = PdfCidAllocator.Create(1)
            .BindSubsetGlyphs(
                new int[] { 0 },
                new int[] { 0 });

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() =>
                PdfCidMappingBuilder.BuildType2(
                    null!,
                    PdfFontOptions.Create()));
            Assert.Throws<ArgumentNullException>(() =>
                PdfCidMappingBuilder.BuildType0(
                    allocation,
                    null!,
                    PdfFontOptions.Create()));
            Assert.Throws<ArgumentException>(() =>
                PdfCidMappingBuilder.BuildType0(
                    allocation,
                    Array.Empty<int>(),
                    PdfFontOptions.Create()));
            Assert.Throws<ArgumentException>(() =>
                PdfCidMappingBuilder.BuildType0(
                    allocation,
                    new int[] { 0, 0 },
                    PdfFontOptions.Create()));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PdfCidMappingBuilder.BuildType0(
                    allocation,
                    new int[] { 0, 65536 },
                    PdfFontOptions.Create()));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PdfCidMappingBuilder.CreateDenseCids(0));
        });
    }

    static PdfFontUsage CreateUsage(
        FontInstance instance,
        PdfFontOptions options)
    {
        return new PdfFontUsageAccumulator().GetOrAdd(instance, options);
    }
}
