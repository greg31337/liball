using ttf;

namespace ttf.Tests;

internal class PdfCidVerticalWidthsArtifactTests
{
    [Test]
    public void ConvertsVerticalVectorsWithSignedHalfAwayRounding()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                PdfCidVerticalWidthsBuilder.ConvertDisplacement(2001, 2000),
                Is.EqualTo(-1001));
            Assert.That(
                PdfCidVerticalWidthsBuilder.ConvertOriginX(1, 1000),
                Is.EqualTo(1));
            Assert.That(
                PdfCidVerticalWidthsBuilder.ConvertOriginY(1761, 2000),
                Is.EqualTo(881));
            Assert.That(
                PdfCidVerticalWidthsBuilder.ConvertOriginY(-1, 2000),
                Is.EqualTo(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PdfCidVerticalWidthsBuilder.ConvertDisplacement(-1, 1000));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PdfCidVerticalWidthsBuilder.ConvertOriginX(-1, 1000));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PdfCidVerticalWidthsBuilder.ConvertOriginY(1, 0));
            Assert.Throws<FontLimitException>(() =>
                PdfCidVerticalWidthsBuilder.ConvertOriginY(int.MaxValue, 1));
        });
    }

    [Test]
    public void BuildsDw2AndCompactsW2RangesAndArrays()
    {
        PdfCidVerticalMetric defaultOne = Metric(-1000, 300, 880);
        PdfCidVerticalMetric repeated = Metric(-900, 320, 900);
        PdfCidVerticalMetric single = Metric(-800, 400, 850);
        PdfCidVerticalMetric defaultTwo = Metric(-1000, 500, 880);
        PdfCidVerticalMetric firstArray = Metric(-1100, 250, 700);
        PdfCidVerticalMetric secondArray = Metric(-1200, 260, 710);
        PdfCidVerticalWidthsArtifact artifact =
            PdfCidVerticalWidthsBuilder.BuildFromMetrics(
                new PdfCidVerticalMetric[]
                {
                    defaultOne,
                    repeated,
                    repeated,
                    single,
                    defaultTwo,
                    firstArray,
                    secondArray,
                    defaultOne
                });

        Assert.Multiple(() =>
        {
            Assert.That(artifact.GetDefaultOriginY(), Is.EqualTo(880));
            Assert.That(artifact.GetDefaultDisplacementY(),
                Is.EqualTo(-1000));
            Assert.That(artifact.GetCidCount(), Is.EqualTo(8));
            Assert.That(artifact.GetMetric(4), Is.SameAs(defaultTwo));
            Assert.That(artifact.GetEntryCount(), Is.EqualTo(3));
            AssertRangeEntry(artifact.GetEntry(0), 1, 2, repeated);
            AssertArrayEntry(artifact.GetEntry(1), 3,
                new PdfCidVerticalMetric[] { single });
            AssertArrayEntry(artifact.GetEntry(2), 5,
                new PdfCidVerticalMetric[] { firstArray, secondArray });
        });
    }

    [Test]
    public void DefaultTiePrefersIntrinsicDw2ThenLowerValues()
    {
        PdfCidVerticalWidthsArtifact intrinsic =
            PdfCidVerticalWidthsBuilder.BuildFromMetrics(
                new PdfCidVerticalMetric[]
                {
                    Metric(-900, 300, 900),
                    Metric(-1000, 500, 880)
                });
        PdfCidVerticalWidthsArtifact lower =
            PdfCidVerticalWidthsBuilder.BuildFromMetrics(
                new PdfCidVerticalMetric[]
                {
                    Metric(-900, 300, 900),
                    Metric(-1100, 300, 800)
                });

        Assert.Multiple(() =>
        {
            Assert.That(intrinsic.GetDefaultOriginY(), Is.EqualTo(880));
            Assert.That(intrinsic.GetDefaultDisplacementY(),
                Is.EqualTo(-1000));
            Assert.That(lower.GetDefaultOriginY(), Is.EqualTo(800));
            Assert.That(lower.GetDefaultDisplacementY(),
                Is.EqualTo(-1100));
        });
    }

    [Test]
    public void BuildsResolvedMetricsInAllocatedSourceGlyphOrder()
    {
        FontInstance instance = FontInstanceTests.CreateVariableFace(true)
            .CreateDefaultInstance();
        PdfCidAllocator allocator = PdfCidAllocator.Create(4);
        allocator.GetOrAddCid(3);
        allocator.GetOrAddCid(1);
        PdfCidAllocation allocation = allocator.BindSubsetGlyphs(
            new int[] { 0, 2, -1, 1 },
            new int[] { 0, 3, 1 });

        PdfCidVerticalWidthsArtifact artifact =
            PdfCidVerticalWidthsBuilder.Build(instance, allocation);
        int[] sourceGlyphIds = new int[] { 0, 3, 1 };
        int cid = 0;

        while (cid < sourceGlyphIds.Length)
        {
            GlyphMetrics source = instance.GetGlyphMetrics(
                sourceGlyphIds[cid]);
            PdfCidVerticalMetric actual = artifact.GetMetric(cid);

            Assert.Multiple(() =>
            {
                Assert.That(actual.GetDisplacementY(), Is.EqualTo(
                    PdfCidVerticalWidthsBuilder.ConvertDisplacement(
                        source.GetAdvanceHeight(),
                        1000)));
                Assert.That(actual.GetOriginX(), Is.EqualTo(
                    PdfCidVerticalWidthsBuilder.ConvertOriginX(
                        source.GetAdvanceWidth(),
                        1000)));
                Assert.That(actual.GetOriginY(), Is.EqualTo(
                    PdfCidVerticalWidthsBuilder.ConvertOriginY(
                        source.GetVerticalOriginY(),
                        1000)));
            });
            cid += 1;
        }
    }

    [Test]
    public void FontArtifactsExposeVerticalWidthsOnlyForVerticalWriting()
    {
        FontInstance trueType = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        PdfCidFontType2Artifact horizontal = BuildType2(
            trueType,
            PdfFontOptions.Create());
        PdfFontOptions verticalOptions = VerticalOptions();
        PdfCidFontType2Artifact type2 = BuildType2(
            trueType,
            verticalOptions);

        FontInstance cff = FontInstanceCffTests.CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            CffCharsetEncodingTests.BuildCidCff(false),
            4).CreateDefaultInstance();
        PdfFontUsage cffUsage = CreateUsage(cff, VerticalOptions());
        cffUsage.AddGlyph(1, Array.Empty<int>());
        PdfCidFontType0Artifact type0 = PdfCidFontType0Builder.Build(
            cffUsage,
            PdfCidAllocator.Create(cffUsage));

        PdfFontOptions openTypeOptions = VerticalOptions();
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
            Assert.That(horizontal.HasVerticalWidths(), Is.False);
            Assert.That(horizontal.GetVerticalWidths(), Is.Null);
            Assert.That(type2.HasVerticalWidths(), Is.True);
            Assert.That(type2.GetVerticalWidths()!.GetCidCount(),
                Is.EqualTo(type2.GetCidCount()));
            Assert.That(type0.HasVerticalWidths(), Is.True);
            Assert.That(type0.GetVerticalWidths()!.GetCidCount(),
                Is.EqualTo(type0.GetCidCount()));
            Assert.That(openType.HasVerticalWidths(), Is.True);
            Assert.That(openType.GetVerticalWidths()!.GetCidCount(),
                Is.EqualTo(openType.GetCidCount()));
        });
    }

    [Test]
    public void ReturnsDefensiveArraysAndRejectsInvalidAccess()
    {
        PdfCidVerticalMetric first = Metric(-900, 300, 900);
        PdfCidVerticalMetric second = Metric(-1000, 400, 880);
        PdfCidVerticalWidthsArtifact artifact =
            PdfCidVerticalWidthsBuilder.BuildFromMetrics(
                new PdfCidVerticalMetric[] { first, second });
        PdfCidVerticalMetric?[] metrics = artifact.GetMetrics();
        PdfCidVerticalWidthEntry?[] entries = artifact.GetEntries();
        metrics[0] = null;
        entries[0] = null;

        Assert.Multiple(() =>
        {
            Assert.That(artifact.GetMetric(0), Is.SameAs(first));
            Assert.That(artifact.GetEntry(0), Is.Not.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                artifact.GetMetric(2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                artifact.GetEntry(-1));
            Assert.Throws<ArgumentNullException>(() =>
                PdfCidVerticalWidthsBuilder.BuildFromMetrics(null!));
            Assert.Throws<ArgumentException>(() =>
                PdfCidVerticalWidthsBuilder.BuildFromMetrics(
                    Array.Empty<PdfCidVerticalMetric>()));
            Assert.Throws<ArgumentException>(() =>
                PdfCidVerticalWidthsBuilder.BuildFromMetrics(
                    new PdfCidVerticalMetric[] { null! }));
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
                PdfCidVerticalWidthsBuilder.Build(null!, allocation));
            Assert.Throws<ArgumentException>(() =>
                PdfCidVerticalWidthsBuilder.Build(
                    FontInstanceTests.CreateFace().CreateDefaultInstance(),
                    allocation));
        });
    }

    static PdfCidFontType2Artifact BuildType2(
        FontInstance instance,
        PdfFontOptions options)
    {
        PdfFontUsage usage = CreateUsage(instance, options);
        usage.AddGlyph(1, Array.Empty<int>());
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

    static PdfFontOptions VerticalOptions()
    {
        PdfFontOptions options = PdfFontOptions.Create();
        options.SetWritingMode(PdfWritingMode.Vertical);
        return options;
    }

    static PdfCidVerticalMetric Metric(
        int displacementY,
        int originX,
        int originY)
    {
        return new PdfCidVerticalMetric(
            displacementY,
            originX,
            originY);
    }

    static void AssertRangeEntry(
        PdfCidVerticalWidthEntry entry,
        int firstCid,
        int lastCid,
        PdfCidVerticalMetric metric)
    {
        Assert.Multiple(() =>
        {
            Assert.That(entry.GetKind(),
                Is.EqualTo(PdfCidWidthEntryKind.Range));
            Assert.That(entry.GetFirstCid(), Is.EqualTo(firstCid));
            Assert.That(entry.GetLastCid(), Is.EqualTo(lastCid));
            Assert.That(entry.GetRangeMetric(), Is.SameAs(metric));
            Assert.That(entry.GetArrayMetricCount(), Is.Zero);
            Assert.Throws<InvalidOperationException>(() =>
                entry.GetArrayMetric(0));
        });
    }

    static void AssertArrayEntry(
        PdfCidVerticalWidthEntry entry,
        int firstCid,
        PdfCidVerticalMetric[] metrics)
    {
        Assert.Multiple(() =>
        {
            Assert.That(entry.GetKind(),
                Is.EqualTo(PdfCidWidthEntryKind.Array));
            Assert.That(entry.GetFirstCid(), Is.EqualTo(firstCid));
            Assert.That(entry.GetLastCid(),
                Is.EqualTo(firstCid + metrics.Length - 1));
            Assert.That(entry.GetArrayMetricCount(),
                Is.EqualTo(metrics.Length));
            Assert.That(entry.GetArrayMetrics(), Is.EqualTo(metrics));
            Assert.That(entry.GetArrayMetric(0), Is.SameAs(metrics[0]));
            Assert.Throws<InvalidOperationException>(() =>
                entry.GetRangeMetric());
        });
    }
}
