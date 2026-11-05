using ttf;

namespace ttf.Tests;

internal class PdfFontResourceTests
{
    [Test]
    public void FinalizesDefaultSubsetOnceAndRejectsEveryLaterAddition()
    {
        FontInstance instance = HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
        PdfFontOptions options = PdfFontOptions.Create();
        options.SetSubsetPrefixOverride("ABCDEF");
        PdfFontBuilder builder = new PdfFontBuilder(
            instance,
            options);
        options.SetFontKind(PdfFontKind.Type0OpenType);
        options.SetWritingMode(PdfWritingMode.Vertical);
        options.SetSubsetPrefixOverride("UVWXYZ");
        ShapedRun run = CreateRun(instance, 2, 0x41);
        builder.AddGlyph(13, new int[] { 0x43 });
        PdfEncodedRun encoded =
            builder.AddShapedRunWithBaselineShift(run, 1.5);

        PdfFontResource resource = builder.FinalizeFont();
        PdfFontOptions retained = resource.GetFontOptions();
        retained.SetFontKind(PdfFontKind.SimpleTrueType);
        retained.SetWritingMode(PdfWritingMode.Vertical);
        retained.SetSubsetPrefixOverride("GHIJKL");
        PdfFontOptions retainedAgain = resource.GetFontOptions();
        byte[] embedded = resource.CopyEmbeddedFont();
        byte[] changed = resource.CopyEmbeddedFont();
        changed[0] = 99;
        FontPackage reopened = FontLoader.Load(
            embedded,
            FontLoadOptions.CreateStrict());

        Assert.Multiple(() =>
        {
            Assert.That(encoded.GetCids(), Is.EqualTo(new int[] { 2 }));
            Assert.That(encoded.GetBaselineShift(), Is.EqualTo(1.5));
            Assert.That(builder.IsFinalized(), Is.True);
            Assert.That(builder.GetFinalizedResource(), Is.SameAs(resource));
            Assert.That(builder.GetCharacterCodeCount(), Is.EqualTo(3));
            Assert.That(resource.GetFontKind(),
                Is.EqualTo(PdfFontKind.Type0CidFontType2));
            Assert.That(retainedAgain.GetFontKind(),
                Is.EqualTo(PdfFontKind.Automatic));
            Assert.That(retainedAgain.GetWritingMode(),
                Is.EqualTo(PdfWritingMode.Horizontal));
            Assert.That(retainedAgain.GetSubsetPrefixOverride(),
                Is.EqualTo("ABCDEF"));
            Assert.That(resource.GetEmbeddedFontFileKind(),
                Is.EqualTo(PdfEmbeddedFontFileKind.FontFile2));
            Assert.That(resource.GetCharacterCodeCount(), Is.EqualTo(3));
            Assert.That(resource.GetBaseFontName(),
                Does.StartWith("ABCDEF+"));
            Assert.That(resource.GetCidFontType2Artifact()!
                .CopyCidToSourceGlyphIds(),
                Is.EqualTo(new int[] { 0, 13, 2 }));
            Assert.That(resource.GetToUnicodeMappings().GetMappingCount(),
                Is.EqualTo(2));
            Assert.That(resource.GetEmbeddingDecision()
                .GetEffectiveEmbeddingMode(),
                Is.EqualTo(FontSubsetEmbeddingMode.Subset));
            Assert.That(reopened.GetFace(0).GetMaxpTable()!.GetGlyphCount(),
                Is.EqualTo(3));
            Assert.That(resource.CopyEmbeddedFont()[0],
                Is.EqualTo(embedded[0]));
            Assert.Throws<InvalidOperationException>(() =>
                resource.GetCidFontType0Artifact());
            Assert.Throws<InvalidOperationException>(() =>
                resource.GetOpenTypeArtifact());
            Assert.Throws<InvalidOperationException>(() =>
                resource.GetSimpleEncoding());
            Assert.Throws<InvalidOperationException>(() =>
                builder.AddText("A"));
            Assert.Throws<InvalidOperationException>(() =>
                builder.AddGlyph(4, new int[] { 0x42 }));
            Assert.Throws<InvalidOperationException>(() =>
                builder.AddShapedRun(run));
            Assert.Throws<InvalidOperationException>(() =>
                builder.FinalizeFont());
        });
    }

    [Test]
    public void FullEmbeddingFinalizationPreservesTheCompleteProgram()
    {
        FontInstance instance = HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
        PdfFontOptions options = PdfFontOptions.Create();
        options.SetEmbeddingMode(PdfFontEmbeddingMode.FullFont);
        PdfFontBuilder builder = new PdfFontBuilder(instance, options);
        builder.AddGlyph(13, new int[] { 0x43 });

        PdfFontResource resource = builder.FinalizeFont();
        FontPackage reopened = FontLoader.Load(
            resource.CopyEmbeddedFont(),
            FontLoadOptions.CreateStrict());

        Assert.Multiple(() =>
        {
            Assert.That(reopened.GetFace(0).GetMaxpTable()!.GetGlyphCount(),
                Is.EqualTo(instance.GetGlyphCount()));
            Assert.That(resource.GetEmbeddingDecision()
                .GetEffectiveEmbeddingMode(),
                Is.EqualTo(FontSubsetEmbeddingMode.FullFont));
            Assert.That(resource.GetCharacterCodeCount(), Is.EqualTo(2));
        });
    }

    [Test]
    public void FinalizesExplicitOpenTypeAndSimpleTrueTypeResources()
    {
        FontInstance instance = HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
        PdfFontOptions openTypeOptions = PdfFontOptions.Create();
        openTypeOptions.SetFontKind(PdfFontKind.Type0OpenType);
        PdfFontBuilder openTypeBuilder = new PdfFontBuilder(
            instance,
            openTypeOptions);
        openTypeBuilder.AddGlyph(4, new int[] { 0x42 });
        PdfFontResource openType = openTypeBuilder.FinalizeFont();

        PdfFontOptions simpleOptions = PdfFontOptions.Create();
        simpleOptions.SetFontKind(PdfFontKind.SimpleTrueType);
        PdfFontBuilder simpleBuilder = new PdfFontBuilder(
            instance,
            simpleOptions);
        PdfEncodedRun simpleRun = simpleBuilder.AddShapedRun(
            CreateRun(instance, 5, 0x69));
        PdfFontResource simple = simpleBuilder.FinalizeFont();
        FontPackage reopenedSimple = FontLoader.Load(
            simple.CopyEmbeddedFont(),
            FontLoadOptions.CreateStrict());

        Assert.Multiple(() =>
        {
            Assert.That(openType.GetFontKind(),
                Is.EqualTo(PdfFontKind.Type0OpenType));
            Assert.That(openType.GetEmbeddedFontFileKind(),
                Is.EqualTo(PdfEmbeddedFontFileKind.FontFile3));
            Assert.That(openType.GetOpenTypeArtifact(), Is.Not.Null);
            Assert.That(simpleRun.GetCodeByteCount(), Is.EqualTo(1));
            Assert.That(simple.GetFontKind(),
                Is.EqualTo(PdfFontKind.SimpleTrueType));
            Assert.That(simple.GetEmbeddedFontFileKind(),
                Is.EqualTo(PdfEmbeddedFontFileKind.FontFile2));
            Assert.That(simple.GetSimpleEncoding()!.GetDifferenceCount(),
                Is.EqualTo(2));
            Assert.That(simple.GetToUnicodeMappings().GetCodeByteCount(),
                Is.EqualTo(1));
            Assert.That(reopenedSimple.GetFace(0).GetMaxpTable()!
                .GetGlyphCount(), Is.EqualTo(2));
            Assert.Throws<InvalidOperationException>(() =>
                simple.GetCidFontType2Artifact());
        });
    }

    [Test]
    public void AutomaticSelectionUsesCidFontType0ForCidKeyedCff()
    {
        FontInstance instance = FontInstanceCffTests.CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            CffCharsetEncodingTests.BuildCidCff(false),
            4).CreateDefaultInstance();
        PdfFontBuilder builder = new PdfFontBuilder(
            instance,
            PdfFontOptions.Create());
        builder.AddGlyph(3, new int[] { 0x43 });

        PdfFontResource resource = builder.FinalizeFont();

        Assert.Multiple(() =>
        {
            Assert.That(resource.GetFontKind(),
                Is.EqualTo(PdfFontKind.Type0CidFontType0));
            Assert.That(resource.GetEmbeddedFontFileKind(),
                Is.EqualTo(PdfEmbeddedFontFileKind.FontFile3));
            Assert.That(resource.GetCidFontType0Artifact(), Is.Not.Null);
            Assert.That(resource.CopyEmbeddedFont()[0], Is.EqualTo(1));
            Assert.That(resource.GetCharacterCodeCount(), Is.EqualTo(2));
        });
    }

    [Test]
    public void FailedFinalizationDoesNotCommitTheBuilder()
    {
        FontInstance instance = HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
        PdfFontOptions options = PdfFontOptions.Create();
        options.SetFontKind(PdfFontKind.SimpleTrueType);
        options.SetWritingMode(PdfWritingMode.Vertical);
        PdfFontBuilder builder = new PdfFontBuilder(instance, options);
        builder.AddGlyph(4, new int[] { 0x42 });

        Assert.Throws<InvalidOperationException>(() =>
            builder.FinalizeFont());

        Assert.Multiple(() =>
        {
            Assert.That(builder.IsFinalized(), Is.False);
            Assert.Throws<InvalidOperationException>(() =>
                builder.GetFinalizedResource());
            Assert.DoesNotThrow(() =>
                builder.AddGlyph(5, new int[] { 0x69 }));
            Assert.That(builder.GetCharacterCodeCount(), Is.EqualTo(3));
        });
    }

    [Test]
    public void RejectsNullBuilderInputs()
    {
        FontInstance instance = HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() =>
                new PdfFontBuilder(null!, PdfFontOptions.Create()));
            Assert.Throws<ArgumentNullException>(() =>
                new PdfFontBuilder(instance, null!));
        });
    }

    [Test]
    public void DeterministicKeysDeduplicateIndependentIdenticalResources()
    {
        FontInstance instance = HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
        PdfFontResource first = BuildResource(
            instance,
            PdfFontOptions.Create(),
            13,
            0x43);
        PdfFontResource second = BuildResource(
            instance,
            PdfFontOptions.Create(),
            13,
            0x43);
        PdfFontResourceCollection resources =
            new PdfFontResourceCollection();

        PdfFontResource firstRegistration = resources.GetOrAdd(first);
        PdfFontResource duplicateRegistration = resources.GetOrAdd(second);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.GetDeduplicationKey(),
                Does.Match("^PFR1-[0-9A-F]{64}$"));
            Assert.That(second.GetDeduplicationKey(),
                Is.EqualTo(first.GetDeduplicationKey()));
            Assert.That(second.CopyEmbeddedFont(),
                Is.EqualTo(first.CopyEmbeddedFont()));
            Assert.That(firstRegistration, Is.SameAs(first));
            Assert.That(duplicateRegistration, Is.SameAs(first));
            Assert.That(resources.GetCount(), Is.EqualTo(1));
            Assert.That(resources.GetResource(0), Is.SameAs(first));
            Assert.That(resources.Find(first.GetDeduplicationKey()),
                Is.SameAs(first));
            Assert.That(resources.Find("PFR1-missing"), Is.Null);
            Assert.Throws<ArgumentNullException>(() =>
                resources.GetOrAdd(null!));
            Assert.Throws<ArgumentNullException>(() =>
                resources.Find(null!));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                resources.GetResource(1));
        });
    }

    [Test]
    public void OneFaceKeepsWritingModesAndPoliciesAsDistinctResources()
    {
        FontInstance instance = HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
        PdfFontOptions horizontalOptions = PdfFontOptions.Create();
        PdfFontOptions verticalOptions = PdfFontOptions.Create();
        verticalOptions.SetWritingMode(PdfWritingMode.Vertical);
        PdfFontOptions reportOptions = PdfFontOptions.Create();
        reportOptions.SetRightsPolicy(PdfFontRightsPolicy.Report);

        PdfFontResource horizontal = BuildResource(
            instance,
            horizontalOptions,
            4,
            0x42);
        PdfFontResource vertical = BuildResource(
            instance,
            verticalOptions,
            4,
            0x42);
        PdfFontResource report = BuildResource(
            instance,
            reportOptions,
            4,
            0x42);
        PdfFontResourceCollection resources =
            new PdfFontResourceCollection();

        resources.GetOrAdd(horizontal);
        resources.GetOrAdd(vertical);
        resources.GetOrAdd(report);

        Assert.Multiple(() =>
        {
            Assert.That(vertical.GetDeduplicationKey(),
                Is.Not.EqualTo(horizontal.GetDeduplicationKey()));
            Assert.That(report.GetDeduplicationKey(),
                Is.Not.EqualTo(horizontal.GetDeduplicationKey()));
            Assert.That(vertical.GetCidFontType2Artifact()!
                .HasVerticalWidths(), Is.True);
            Assert.That(horizontal.GetCidFontType2Artifact()!
                .HasVerticalWidths(), Is.False);
            Assert.That(resources.GetCount(), Is.EqualTo(3));
            Assert.That(resources.GetResource(0), Is.SameAs(horizontal));
            Assert.That(resources.GetResource(1), Is.SameAs(vertical));
            Assert.That(resources.GetResource(2), Is.SameAs(report));
        });
    }

    static PdfFontResource BuildResource(
        FontInstance instance,
        PdfFontOptions options,
        int glyphId,
        int unicodeScalar)
    {
        PdfFontBuilder builder = new PdfFontBuilder(instance, options);
        builder.AddGlyph(glyphId, new int[] { unicodeScalar });
        return builder.FinalizeFont();
    }

    static ShapedRun CreateRun(
        FontInstance instance,
        int glyphId,
        int unicodeScalar)
    {
        double advance = instance.GetGlyphMetrics(glyphId)
            .GetAdvanceWidth();
        ShapedGlyph glyph = new ShapedGlyph(
            glyphId,
            0,
            0,
            1,
            new int[] { unicodeScalar },
            1,
            0,
            0,
            0,
            ShapedGlyphFlags.None,
            advance,
            0,
            0,
            0);
        ShapedCluster cluster = new ShapedCluster(
            0,
            1,
            new int[] { unicodeScalar },
            new int[] { 0 },
            0,
            0,
            false);
        return new ShapedRun(
            new ShapedGlyph[] { glyph },
            new ShapedCluster[] { cluster },
            new int[] { 0 },
            new int[] { 0 },
            new int[] { 0 },
            new int[] { 0 },
            1,
            ShapingDirection.LeftToRight);
    }
}
