namespace ttf.Tests;

internal class FontPublicResultsTests
{
    [Test]
    public void DescriptionAggregatesCachedResultsAndLocalizedNames()
    {
        FontFace face = CreateCompleteFace(true);
        FontDescription first = face.GetDescription();
        FontDescription second = face.GetDescription();
        FontDescription french = face.GetLocalizedDescription("fr-FR");

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.SameAs(first));
            Assert.That(first.GetFaceIndex(), Is.Zero);
            Assert.That(first.GetScalerType(), Is.EqualTo(0x4F54544FU));
            Assert.That(first.GetOutlineKind(), Is.EqualTo(FontOutlineKind.Cff1));
            Assert.That(first.GetNames(), Is.SameAs(face.GetNames()));
            Assert.That(first.GetClassification(), Is.SameAs(face.GetClassification()));
            Assert.That(first.GetCoverage(), Is.SameAs(face.GetCoverage()));
            Assert.That(first.GetMetrics(), Is.SameAs(face.GetMetrics()));
            Assert.That(first.GetEmbeddingRights(), Is.SameAs(face.GetEmbeddingRights()));
            Assert.That(first.GetNames().GetFamilyName()!.GetValue(), Is.EqualTo("Example Family"));
            Assert.That(first.GetNames().GetFamilyName()!.GetMatchKind(),
                Is.EqualTo(FontNameMatchKind.EnglishFallback));
            Assert.That(french.GetNames().GetFamilyName()!.GetValue(), Is.EqualTo("Famille Exemple"));
            Assert.That(french.GetNames().GetFamilyName()!.GetLanguageTag(), Is.EqualTo("fr-FR"));
            Assert.That(french.GetNames().GetFamilyName()!.GetMatchKind(),
                Is.EqualTo(FontNameMatchKind.ExactLanguage));
            Assert.That(first.GetNames().GetFullName()!.GetValue(), Is.EqualTo("Example Bold Italic"));
            Assert.That(first.GetNames().GetDescription()!.GetValue(), Is.EqualTo("Inspection font"));
            Assert.That(first.GetNames().GetLicenseDescription()!.GetValue(), Is.EqualTo("Test license"));
            Assert.That(first.GetNames().GetSampleText()!.GetValue(), Is.EqualTo("Sample text"));
            Assert.That(first.GetNames().GetPostScriptName()!.GetValue(), Is.EqualTo("Face / Name"));
            Assert.That(first.GetNames().GetPdfPostScriptName(), Is.EqualTo("Face-Name"));
            Assert.That(first.GetNames().GetTrademark(), Is.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() => first.GetNames().GetName(-1));
            Assert.Throws<ArgumentNullException>(
                () => face.GetLocalizedNames(null!));
            Assert.Throws<ArgumentException>(
                () => face.GetLocalizedDescription("en--US"));
        });
    }

    [Test]
    public void ClassificationExposesStyleFamilyRangesAndOpticalSizes()
    {
        FontClassification result = CreateCompleteFace(true).GetClassification();

        Assert.Multiple(() =>
        {
            Assert.That(result.HasOs2Classification(), Is.True);
            Assert.That(result.GetWeightClass(), Is.EqualTo(700));
            Assert.That(result.GetWidthClass(), Is.EqualTo(5));
            Assert.That(result.GetFamilyClass(), Is.EqualTo(1));
            Assert.That(result.GetFamilySubclass(), Is.EqualTo(2));
            Assert.That(result.CopyPanose(), Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }));
            Assert.That(result.GetUnicodeRange(0), Is.EqualTo(1U));
            Assert.That(result.HasUnicodeRange(0), Is.True);
            Assert.That(result.HasCodePageRanges(), Is.True);
            Assert.That(result.GetCodePageRange(0), Is.EqualTo(1U));
            Assert.That(result.HasCodePageRange(0), Is.True);
            Assert.That(result.GetVendorId(), Is.EqualTo(0x54455354U));
            Assert.That(result.GetSelectionFlags(), Is.EqualTo(0x21));
            Assert.That(result.HasSelectionFlag(5), Is.True);
            Assert.That(result.IsItalic(), Is.True);
            Assert.That(result.IsBold(), Is.True);
            Assert.That(result.IsOblique(), Is.False);
            Assert.That(result.IsMonospace(), Is.True);
            Assert.That(result.IsSerif(), Is.True);
            Assert.That(result.IsScript(), Is.False);
            Assert.That(result.IsSymbolic(), Is.False);
            Assert.That(result.HasOpticalPointSizes(), Is.True);
            Assert.That(result.GetLowerOpticalPointSize(), Is.EqualTo(8.0));
            Assert.That(result.GetUpperOpticalPointSize(), Is.EqualTo(36.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => result.GetUnicodeRange(4));
        });

        byte[] copy = result.CopyPanose();
        copy[0] = 99;
        Assert.That(result.CopyPanose()[0], Is.EqualTo(1));
    }

    [Test]
    public void CoverageKeepsCompactRangesAndSupportsBinaryQueries()
    {
        FontCoverage coverage = CreateCompleteFace(true).GetCoverage();
        FontCoverageRange range = coverage.GetRange(0);

        Assert.Multiple(() =>
        {
            Assert.That(coverage.GetRangeCount(), Is.EqualTo(1));
            Assert.That(coverage.GetCoveredValueCount(), Is.EqualTo(3));
            Assert.That(coverage.IsSymbolMapping(), Is.False);
            Assert.That(range.GetStartUnicodeValue(), Is.EqualTo(0x41));
            Assert.That(range.GetEndUnicodeValue(), Is.EqualTo(0x43));
            Assert.That(range.Contains(0x42), Is.True);
            Assert.That(coverage.Covers(0x41), Is.True);
            Assert.That(coverage.Covers(0x44), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => coverage.GetRange(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => coverage.Covers(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => coverage.Covers(0xD800));
            Assert.Throws<ArgumentOutOfRangeException>(() => coverage.Covers(0x110000));
            Assert.Throws<ArgumentOutOfRangeException>(() => range.Contains(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => range.Contains(0xDFFF));
            Assert.Throws<ArgumentOutOfRangeException>(() => range.Contains(0x110000));
        });
    }

    [Test]
    public void MetricsExposeValidatedUnscaledSourcesWithoutSelectingPolicy()
    {
        FontMetrics metrics = CreateCompleteFace(true).GetMetrics();
        FontBounds? bounds = metrics.GetGlobalBounds();

        Assert.Multiple(() =>
        {
            Assert.That(metrics.GetUnitsPerEm(), Is.EqualTo(1000));
            Assert.That(metrics.GetGlyphCount(), Is.EqualTo(5));
            Assert.That(bounds!.GetXMin(), Is.EqualTo(-50));
            Assert.That(bounds.GetYMin(), Is.EqualTo(-200));
            Assert.That(bounds.GetXMax(), Is.EqualTo(900));
            Assert.That(bounds.GetYMax(), Is.EqualTo(800));
            Assert.That(bounds.GetWidth(), Is.EqualTo(950));
            Assert.That(bounds.GetHeight(), Is.EqualTo(1000));
            Assert.That(metrics.HasHorizontalMetrics(), Is.True);
            Assert.That(metrics.GetHorizontalAscender(), Is.EqualTo(750));
            Assert.That(metrics.GetHorizontalDescender(), Is.EqualTo(-250));
            Assert.That(metrics.GetHorizontalLineGap(), Is.EqualTo(50));
            Assert.That(metrics.GetMaximumAdvanceWidth(), Is.EqualTo(900));
            Assert.That(metrics.GetHorizontalCaretSlopeRise(), Is.EqualTo(1));
            Assert.That(metrics.GetHorizontalCaretSlopeRun(), Is.Zero);
            Assert.That(metrics.HasVerticalMetrics(), Is.False);
            Assert.Throws<InvalidOperationException>(() => metrics.GetVerticalAscender());
            Assert.That(metrics.HasOs2Metrics(), Is.True);
            Assert.That(metrics.GetTypographicAscender(), Is.EqualTo(800));
            Assert.That(metrics.GetTypographicDescender(), Is.EqualTo(-200));
            Assert.That(metrics.GetTypographicLineGap(), Is.EqualTo(20));
            Assert.That(metrics.GetWindowsAscent(), Is.EqualTo(900));
            Assert.That(metrics.GetWindowsDescent(), Is.EqualTo(250));
            Assert.That(metrics.GetXHeight(), Is.EqualTo(450));
            Assert.That(metrics.GetCapHeight(), Is.EqualTo(700));
            Assert.That(metrics.GetUnderlinePosition(), Is.EqualTo(-100));
            Assert.That(metrics.GetUnderlineThickness(), Is.EqualTo(50));
            Assert.That(metrics.GetStrikeoutPosition(), Is.EqualTo(300));
            Assert.That(metrics.GetStrikeoutThickness(), Is.EqualTo(50));
        });
    }

    [Test]
    public void EmbeddingRightsExposeRestrictionsAndAreCached()
    {
        FontFace face = CreateCompleteFace(true);
        FontEmbeddingRights? rights = face.GetEmbeddingRights();

        Assert.Multiple(() =>
        {
            Assert.That(face.GetEmbeddingRights(), Is.SameAs(rights));
            Assert.That(rights!.GetTableVersion(), Is.EqualTo(5));
            Assert.That(rights.GetRawFlags(), Is.EqualTo(0x0104));
            Assert.That(rights.GetPermissionFlags(), Is.EqualTo(4));
            Assert.That(rights.GetPermission(), Is.EqualTo(FontEmbeddingPermission.PreviewAndPrint));
            Assert.That(rights.DeclaresPreviewAndPrintPermission(), Is.True);
            Assert.That(rights.GetDeclaredPermissionCount(), Is.EqualTo(1));
            Assert.That(rights.UsesLegacyLeastRestrictivePrecedence(), Is.False);
            Assert.That(rights.GetIgnoredFlags(), Is.Zero);
            Assert.That(rights.AllowsEmbedding(), Is.True);
            Assert.That(rights.AllowsPermanentInstallation(), Is.False);
            Assert.That(rights.AllowsDocumentEditing(), Is.False);
            Assert.That(rights.RequiresReadOnlyDocuments(), Is.True);
            Assert.That(rights.RequiresTemporaryLoading(), Is.True);
            Assert.That(rights.AllowsSubsetting(), Is.False);
            Assert.That(rights.RequiresFullEmbedding(), Is.True);
            Assert.That(rights.AllowsOutlineEmbedding(), Is.True);
            Assert.That(rights.AllowsBitmapEmbedding(), Is.True);
            Assert.That(rights.RequiresBitmapOnlyEmbedding(), Is.False);
            Assert.That(rights.GetRecommendedAction(), Is.EqualTo(FontEmbeddingAction.EmbedFullFont));
            Assert.That(rights.GetRestrictionCount(), Is.EqualTo(1));
            Assert.That(rights.GetRestrictionCode(0), Is.EqualTo(FontDiagnosticCode.SubsettingRestricted));
            Assert.Throws<ArgumentOutOfRangeException>(() => rights.GetRestrictionCode(1));
        });
    }

    [Test]
    public void TableResultsExposeClassificationAndCopyIndependentRawBytes()
    {
        FontFace face = CreateCompleteFace(true);
        FontTableInfo? head = face.FindTableInfo(FontTags.Head);
        int privateTag = (int)TestSfntEncoding.EncodeTag("ZZZZ");
        FontTableInfo? unknown = face.FindTableInfo(privateTag);

        Assert.Multiple(() =>
        {
            Assert.That(face.GetTableInfo(0).GetTag(), Is.EqualTo(FontTags.Head));
            Assert.That(head!.GetKind(), Is.EqualTo(FontTableKind.Known));
            Assert.That(head.GetLength(), Is.EqualTo(54));
            Assert.That(head.HasRawData(), Is.True);
            Assert.That(head.GetChecksum(), Is.EqualTo(FontChecksum.CalculateTable(BuildHead())));
            Assert.That(unknown!.GetKind(), Is.EqualTo(FontTableKind.Unknown));
            Assert.That(unknown.CopyData(), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(face.FindTableInfo((int)TestSfntEncoding.EncodeTag("NONE")), Is.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() => face.GetTableInfo(face.GetTableCount()));
        });

        byte[] firstCopy = unknown!.CopyData();
        firstCopy[0] = 99;
        Assert.That(unknown.CopyData()[0], Is.EqualTo(1));

        FontFace withoutRaw = CreateCompleteFace(false);
        FontTableInfo? unavailable = withoutRaw.FindTableInfo(privateTag);
        Assert.Multiple(() =>
        {
            Assert.That(unavailable!.HasRawData(), Is.False);
            Assert.Throws<InvalidOperationException>(() => unavailable.CopyData());
        });
    }

    [Test]
    public void MissingOptionalTablesProduceExplicitEmptyResults()
    {
        FontFace face = CreateSparseFace();
        FontDescription description = face.GetDescription();

        Assert.Multiple(() =>
        {
            Assert.That(description.GetNames().GetFamilyName(), Is.Null);
            Assert.That(description.GetClassification().HasOs2Classification(), Is.False);
            Assert.That(description.GetCoverage().GetRangeCount(), Is.Zero);
            Assert.That(description.GetMetrics().GetUnitsPerEm(), Is.EqualTo(1000));
            Assert.That(description.GetMetrics().HasHorizontalMetrics(), Is.False);
            Assert.That(description.GetMetrics().HasOs2Metrics(), Is.False);
            Assert.That(description.GetMetrics().HasUnderlineMetrics(), Is.False);
            Assert.That(description.GetEmbeddingRights(), Is.Null);
            Assert.Throws<InvalidOperationException>(() => description.GetMetrics().GetHorizontalAscender());
            Assert.Throws<InvalidOperationException>(() => description.GetMetrics().GetTypographicAscender());
            Assert.Throws<InvalidOperationException>(() => description.GetClassification().GetUnicodeRange(0));
        });
    }

    [Test]
    public void SelectsRecommendedAndExplicitLineMetricSources()
    {
        FontFace typographic = CreateMetricFace(true, true, true, true);
        FontFace windows = CreateMetricFace(false, true, true, true);
        FontFace hhea = CreateMetricFace(false, false, true, true);
        FontDesignMetrics recommendedTypographic = typographic.GetDesignMetrics(
            FontMetricSelectionPolicy.OpenTypeRecommended);
        FontDesignMetrics recommendedWindows = windows.GetDesignMetrics(
            FontMetricSelectionPolicy.OpenTypeRecommended);
        FontDesignMetrics recommendedHhea = hhea.GetDesignMetrics(
            FontMetricSelectionPolicy.OpenTypeRecommended);

        Assert.Multiple(() =>
        {
            Assert.That(recommendedTypographic.GetLineMetricSource(),
                Is.EqualTo(FontLineMetricSource.Typographic));
            Assert.That(recommendedTypographic.GetAscender(), Is.EqualTo(800));
            Assert.That(recommendedTypographic.GetDescender(), Is.EqualTo(-200));
            Assert.That(recommendedTypographic.GetLineGap(), Is.EqualTo(20));
            Assert.That(recommendedTypographic.GetLineHeight(), Is.EqualTo(1020));
            Assert.That(recommendedWindows.GetLineMetricSource(),
                Is.EqualTo(FontLineMetricSource.Windows));
            Assert.That(recommendedWindows.GetAscender(), Is.EqualTo(900));
            Assert.That(recommendedWindows.GetDescender(), Is.EqualTo(-250));
            Assert.That(recommendedWindows.GetLineGap(), Is.Zero);
            Assert.That(recommendedHhea.GetLineMetricSource(),
                Is.EqualTo(FontLineMetricSource.HorizontalHeader));
            Assert.That(recommendedHhea.GetAscender(), Is.EqualTo(750));
            Assert.That(recommendedHhea.GetDescender(), Is.EqualTo(-250));
            Assert.That(recommendedHhea.GetLineGap(), Is.EqualTo(50));
            Assert.That(windows.GetDesignMetrics(FontMetricSelectionPolicy.Typographic)
                .GetLineMetricSource(), Is.EqualTo(FontLineMetricSource.Typographic));
            Assert.That(windows.GetDesignMetrics(FontMetricSelectionPolicy.Windows)
                .GetLineMetricSource(), Is.EqualTo(FontLineMetricSource.Windows));
            Assert.That(windows.GetDesignMetrics(FontMetricSelectionPolicy.HorizontalHeader)
                .GetLineMetricSource(), Is.EqualTo(FontLineMetricSource.HorizontalHeader));
            Assert.Throws<FontFormatException>(() => hhea.GetDesignMetrics(
                FontMetricSelectionPolicy.Typographic));
            Assert.Throws<FontFormatException>(() => hhea.GetDesignMetrics(
                FontMetricSelectionPolicy.Windows));
            Assert.Throws<ArgumentOutOfRangeException>(() => hhea.GetDesignMetrics(
                (FontMetricSelectionPolicy)99));
            Assert.Throws<FontFormatException>(() => CreateSparseFace().GetDesignMetrics(
                FontMetricSelectionPolicy.OpenTypeRecommended));
            Assert.Throws<FontFormatException>(() => CreateSparseFace().GetDesignMetrics(
                FontMetricSelectionPolicy.HorizontalHeader));
        });
    }

    [Test]
    public void ScalesSelectedGlobalCoordinatesAndKeepsCaretSlopeUnscaled()
    {
        FontDesignMetrics design = CreateMetricFace(true, true, true, true)
            .GetDesignMetrics(FontMetricSelectionPolicy.OpenTypeRecommended);
        FontScaledMetrics scaled = design.Scale(20);
        ScaledFontBounds bounds = scaled.GetGlobalBounds();

        Assert.Multiple(() =>
        {
            Assert.That(scaled.GetDesignMetrics(), Is.SameAs(design));
            Assert.That(scaled.GetPointSize(), Is.EqualTo(20));
            Assert.That(scaled.GetAscender(), Is.EqualTo(16));
            Assert.That(scaled.GetDescender(), Is.EqualTo(-4));
            Assert.That(scaled.GetLineGap(), Is.EqualTo(0.4));
            Assert.That(scaled.GetLineHeight(), Is.EqualTo(20.4));
            Assert.That(bounds.GetXMin(), Is.EqualTo(-1));
            Assert.That(bounds.GetYMin(), Is.EqualTo(-4));
            Assert.That(bounds.GetXMax(), Is.EqualTo(18));
            Assert.That(bounds.GetYMax(), Is.EqualTo(16));
            Assert.That(bounds.GetWidth(), Is.EqualTo(19));
            Assert.That(bounds.GetHeight(), Is.EqualTo(20));
            Assert.That(scaled.GetCapHeight(), Is.EqualTo(14));
            Assert.That(scaled.GetXHeight(), Is.EqualTo(9));
            Assert.That(scaled.GetUnderlinePosition(), Is.EqualTo(-2));
            Assert.That(scaled.GetUnderlineThickness(), Is.EqualTo(1));
            Assert.That(scaled.GetStrikeoutPosition(), Is.EqualTo(6));
            Assert.That(scaled.GetStrikeoutThickness(), Is.EqualTo(1));
            Assert.That(scaled.GetCaretSlopeRise(), Is.EqualTo(1));
            Assert.That(scaled.GetCaretSlopeRun(), Is.Zero);
            Assert.That(scaled.GetCaretOffset(), Is.EqualTo(0.06));
            Assert.That(scaled.HasVerticalMetrics(), Is.True);
            Assert.That(scaled.GetVerticalAscender(), Is.EqualTo(10));
            Assert.That(scaled.GetVerticalDescender(), Is.EqualTo(-10));
            Assert.That(scaled.GetVerticalLineGap(), Is.EqualTo(0.8));
            Assert.That(scaled.GetVerticalLineHeight(), Is.EqualTo(20.8));
            Assert.That(scaled.GetVerticalCaretSlopeRise(), Is.EqualTo(-2));
            Assert.That(scaled.GetVerticalCaretSlopeRun(), Is.EqualTo(1));
            Assert.That(scaled.GetVerticalCaretOffset(), Is.EqualTo(0.06));
            Assert.That(CreateMetricFace(true, true, true, true).GetScaledMetrics(
                FontMetricSelectionPolicy.Windows, 0).GetLineHeight(), Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => design.Scale(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => design.Scale(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => design.Scale(double.PositiveInfinity));
        });
    }

    [Test]
    public void ResolvesAndScalesPerGlyphHorizontalAndVerticalMetrics()
    {
        FontFace face = CreateMetricFace(true, true, true, true);
        GlyphMetrics glyph = face.GetDesignGlyphMetrics(1, 700);
        ScaledGlyphMetrics scaled = face.GetScaledGlyphMetrics(1, 700, 20);

        Assert.Multiple(() =>
        {
            Assert.That(glyph.GetGlyphId(), Is.EqualTo(1));
            Assert.That(glyph.GetAdvanceWidth(), Is.EqualTo(900));
            Assert.That(glyph.GetLeftSideBearing(), Is.EqualTo(20));
            Assert.That(glyph.GetAdvanceHeight(), Is.EqualTo(1000));
            Assert.That(glyph.GetTopSideBearing(), Is.EqualTo(200));
            Assert.That(glyph.GetVerticalOriginY(), Is.EqualTo(900));
            Assert.That(glyph.UsesSynthesizedVerticalAdvance(), Is.False);
            Assert.That(glyph.UsesVorgOrigin(), Is.True);
            Assert.That(scaled.GetAdvanceWidth(), Is.EqualTo(18));
            Assert.That(scaled.GetLeftSideBearing(), Is.EqualTo(0.4));
            Assert.That(scaled.GetAdvanceHeight(), Is.EqualTo(20));
            Assert.That(scaled.GetTopSideBearing(), Is.EqualTo(4));
            Assert.That(scaled.GetVerticalOriginY(), Is.EqualTo(18));
            Assert.That(scaled.GetDesignMetrics().GetGlyphId(), Is.EqualTo(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => face.GetDesignGlyphMetrics(-1, 700));
            Assert.Throws<ArgumentOutOfRangeException>(() => face.GetDesignGlyphMetrics(5, 700));
            Assert.Throws<ArgumentOutOfRangeException>(() => face.GetScaledGlyphMetrics(1, 700, -1));
        });
    }

    [Test]
    public void UsesVmtxOriginsAndDocumentedVerticalFallbacks()
    {
        FontFace vmtxFace = CreateMetricFace(true, true, true, false);
        GlyphMetrics fromVmtx = vmtxFace.GetDesignGlyphMetrics(3, 700);
        FontFace fallbackFace = CreateMetricFace(true, true, false, false);
        GlyphMetrics fallback = fallbackFace.GetDesignGlyphMetrics(3, 700);
        FontDesignMetrics fallbackGlobal = fallbackFace.GetDesignMetrics(
            FontMetricSelectionPolicy.OpenTypeRecommended);

        Assert.Multiple(() =>
        {
            Assert.That(fromVmtx.GetAdvanceHeight(), Is.EqualTo(1000));
            Assert.That(fromVmtx.GetTopSideBearing(), Is.EqualTo(40));
            Assert.That(fromVmtx.GetVerticalOriginY(), Is.EqualTo(740));
            Assert.That(fromVmtx.UsesVorgOrigin(), Is.False);
            Assert.That(fromVmtx.UsesSynthesizedVerticalAdvance(), Is.False);
            Assert.That(fallback.GetAdvanceHeight(), Is.EqualTo(1000));
            Assert.That(fallback.GetTopSideBearing(), Is.EqualTo(100));
            Assert.That(fallback.GetVerticalOriginY(), Is.EqualTo(800));
            Assert.That(fallback.UsesSynthesizedVerticalAdvance(), Is.True);
            Assert.That(fallback.UsesVorgOrigin(), Is.False);
            Assert.That(fallbackGlobal.HasVerticalMetrics(), Is.False);
            Assert.Throws<InvalidOperationException>(() => fallbackGlobal.GetVerticalAscender());
            Assert.Throws<FontFormatException>(() => CreateSparseFace()
                .GetDesignGlyphMetrics(0, 700));
        });
    }

    [TestCase(FontMetricSelectionPolicy.OpenTypeRecommended, 0)]
    [TestCase(FontMetricSelectionPolicy.Typographic, 1)]
    [TestCase(FontMetricSelectionPolicy.Windows, 2)]
    [TestCase(FontMetricSelectionPolicy.HorizontalHeader, 3)]
    public void MetricSelectionPolicyValuesAreStable(
        FontMetricSelectionPolicy value,
        int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }

    [TestCase(FontNameMatchKind.ExactLanguage, 0)]
    [TestCase(FontNameMatchKind.ParentLanguage, 1)]
    [TestCase(FontNameMatchKind.EnglishFallback, 2)]
    [TestCase(FontNameMatchKind.FirstValidRecord, 3)]
    public void NameMatchKindValuesAreStable(FontNameMatchKind value, int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }

    static FontFace CreateCompleteFace(bool preserveRaw)
    {
        int privateTag = (int)TestSfntEncoding.EncodeTag("ZZZZ");
        SfntTableData[] tables = new SfntTableData[]
        {
            CreateTable(FontTags.Head, BuildHead(), SfntTableClassification.Known, preserveRaw),
            CreateTable(FontTags.Maxp, BuildMaxp(), SfntTableClassification.Known, preserveRaw),
            CreateTable(FontTags.Hhea, BuildHhea(), SfntTableClassification.Known, preserveRaw),
            CreateTable(FontTags.Os2, BuildOs2(), SfntTableClassification.Known, preserveRaw),
            CreateTable(FontTags.Post, BuildPost(), SfntTableClassification.Known, preserveRaw),
            CreateTable(FontTags.Name, BuildName(), SfntTableClassification.Known, preserveRaw),
            CreateTable(FontTags.Cmap, BuildCmap(), SfntTableClassification.Known, preserveRaw),
            CreateTable(privateTag, new byte[] { 1, 2, 3 }, SfntTableClassification.Unknown, preserveRaw)
        };
        return CreateFace(tables);
    }

    static FontFace CreateMetricFace(
        bool useTypographicMetrics,
        bool includeOs2,
        bool includeVerticalMetrics,
        bool includeVorg)
    {
        List<SfntTableData> tables = new List<SfntTableData>();
        tables.Add(CreateTable(FontTags.Head, BuildHead(), SfntTableClassification.Known, true));
        tables.Add(CreateTable(FontTags.Maxp, BuildMaxp(), SfntTableClassification.Known, true));
        tables.Add(CreateTable(FontTags.Hhea, BuildMetricHhea(), SfntTableClassification.Known, true));
        tables.Add(CreateTable(FontTags.Hmtx, BuildHmtx(), SfntTableClassification.Known, true));
        tables.Add(CreateTable(FontTags.Post, BuildPost(), SfntTableClassification.Known, true));
        if (includeOs2)
        {
            tables.Add(CreateTable(
                FontTags.Os2,
                BuildMetricOs2(useTypographicMetrics),
                SfntTableClassification.Known,
                true));
        }
        if (includeVerticalMetrics)
        {
            tables.Add(CreateTable(FontTags.Vhea, BuildVhea(), SfntTableClassification.Known, true));
            tables.Add(CreateTable(FontTags.Vmtx, BuildVmtx(), SfntTableClassification.Known, true));
        }
        if (includeVorg)
        {
            tables.Add(CreateTable(FontTags.Vorg, BuildVorg(), SfntTableClassification.Known, true));
        }
        return CreateFace(tables.ToArray());
    }

    static FontFace CreateSparseFace()
    {
        return CreateFace(new SfntTableData[]
        {
            CreateTable(FontTags.Head, BuildPlainHead(), SfntTableClassification.Known, true),
            CreateTable(FontTags.Maxp, BuildMaxp(), SfntTableClassification.Known, true)
        });
    }

    static FontFace CreateFace(SfntTableData[] tables)
    {
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(tables, Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        return new FontFace(
            0,
            0x4F54544FU,
            new SfntValidationResult(FontOutlineKind.Cff1, Array.Empty<FontDiagnostic>()),
            cache);
    }

    static SfntTableData CreateTable(
        int tag,
        byte[] bytes,
        SfntTableClassification classification,
        bool preserveRaw)
    {
        FontData data = FontData.Create(bytes);
        return new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            classification,
            preserveRaw);
    }

    static byte[] BuildHead()
    {
        byte[] bytes = BuildPlainHead();
        bytes[44] = 0;
        bytes[45] = 3;
        return bytes;
    }

    static byte[] BuildPlainHead()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0)
            .WriteUInt32(0x00010000U).WriteUInt32(0)
            .WriteUInt32(0x5F0F3CF5U).WriteUInt16(0).WriteUInt16(1000)
            .WriteInt64(0).WriteInt64(0)
            .WriteInt16(-50).WriteInt16(-200).WriteInt16(900).WriteInt16(800)
            .WriteUInt16(0).WriteUInt16(8).WriteInt16(2).WriteInt16(0).WriteInt16(0)
            .CopyBytes();
    }

    static byte[] BuildMaxp()
    {
        return new TestBinaryBuilder().WriteUInt32(0x00005000U).WriteUInt16(5).CopyBytes();
    }

    static byte[] BuildHhea()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0)
            .WriteInt16(750).WriteInt16(-250).WriteInt16(50)
            .WriteUInt16(900).WriteInt16(-50).WriteInt16(-40).WriteInt16(880)
            .WriteInt16(1).WriteInt16(0).WriteInt16(0)
            .WriteInt16(0).WriteInt16(0).WriteInt16(0).WriteInt16(0)
            .WriteInt16(0).WriteUInt16(2)
            .CopyBytes();
    }

    static byte[] BuildMetricHhea()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0)
            .WriteInt16(750).WriteInt16(-250).WriteInt16(50)
            .WriteUInt16(900).WriteInt16(-50).WriteInt16(-40).WriteInt16(880)
            .WriteInt16(1).WriteInt16(0).WriteInt16(3)
            .WriteInt16(0).WriteInt16(0).WriteInt16(0).WriteInt16(0)
            .WriteInt16(0).WriteUInt16(2)
            .CopyBytes();
    }

    static byte[] BuildHmtx()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(600).WriteInt16(-10)
            .WriteUInt16(900).WriteInt16(20)
            .WriteInt16(30).WriteInt16(-40).WriteInt16(50)
            .CopyBytes();
    }

    static byte[] BuildVhea()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0x1000)
            .WriteInt16(500).WriteInt16(-500).WriteInt16(40)
            .WriteUInt16(1000).WriteInt16(-20).WriteInt16(-30).WriteInt16(950)
            .WriteInt16(-2).WriteInt16(1).WriteInt16(3)
            .WriteInt16(0).WriteInt16(0).WriteInt16(0).WriteInt16(0)
            .WriteInt16(0).WriteUInt16(2)
            .CopyBytes();
    }

    static byte[] BuildVmtx()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(800).WriteInt16(10)
            .WriteUInt16(1000).WriteInt16(20)
            .WriteInt16(30).WriteInt16(40).WriteInt16(50)
            .CopyBytes();
    }

    static byte[] BuildVorg()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0).WriteInt16(880).WriteUInt16(1)
            .WriteUInt16(1).WriteInt16(900)
            .CopyBytes();
    }

    static byte[] BuildMetricOs2(bool useTypographicMetrics)
    {
        byte[] bytes = BuildOs2();
        int selection = useTypographicMetrics ? 0x00A1 : 0x0021;
        bytes[62] = (byte)(selection >> 8);
        bytes[63] = (byte)selection;
        return bytes;
    }

    static byte[] BuildOs2()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(5).WriteInt16(500).WriteUInt16(700).WriteUInt16(5)
            .WriteUInt16(0x0104)
            .WriteInt16(650).WriteInt16(650).WriteInt16(0).WriteInt16(140)
            .WriteInt16(650).WriteInt16(650).WriteInt16(0).WriteInt16(480)
            .WriteInt16(50).WriteInt16(300).WriteInt16(0x0102);
        int index = 1;
        while (index <= 10)
        {
            builder.WriteUInt8((byte)index);
            index += 1;
        }
        return builder
            .WriteUInt32(1).WriteUInt32(0).WriteUInt32(0).WriteUInt32(0)
            .WriteUInt32(0x54455354U).WriteUInt16(0x21)
            .WriteUInt16(0x20).WriteUInt16(0x7E)
            .WriteInt16(800).WriteInt16(-200).WriteInt16(20)
            .WriteUInt16(900).WriteUInt16(250)
            .WriteUInt32(1).WriteUInt32(0)
            .WriteInt16(450).WriteInt16(700)
            .WriteUInt16(0).WriteUInt16(0x20).WriteUInt16(2)
            .WriteUInt16(160).WriteUInt16(720)
            .CopyBytes();
    }

    static byte[] BuildPost()
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00030000U).WriteInt32(0)
            .WriteInt16(-100).WriteInt16(50).WriteUInt32(1)
            .WriteUInt32(0).WriteUInt32(0).WriteUInt32(0).WriteUInt32(0)
            .CopyBytes();
    }

    static byte[] BuildCmap()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0).WriteUInt16(1)
            .WriteUInt16(3).WriteUInt16(10).WriteUInt32(12)
            .WriteUInt16(12).WriteUInt16(0).WriteUInt32(28).WriteUInt32(0).WriteUInt32(1)
            .WriteUInt32(0x41).WriteUInt32(0x43).WriteUInt32(1)
            .CopyBytes();
    }

    static byte[] BuildName()
    {
        int[] languageIds = new int[] { 0x0409, 0x0409, 0x0409, 0x0409, 0x0409, 0x0409, 0x040C };
        int[] nameIds = new int[] { 1, 4, 6, 10, 13, 19, 1 };
        string[] values = new string[]
        {
            "Example Family",
            "Example Bold Italic",
            "Face / Name",
            "Inspection font",
            "Test license",
            "Sample text",
            "Famille Exemple"
        };
        byte[][] encoded = new byte[values.Length][];
        int storageOffset = 6 + values.Length * 12;
        int stringOffset = 0;
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(0).WriteUInt16((ushort)values.Length).WriteUInt16((ushort)storageOffset);
        int index = 0;
        while (index < values.Length)
        {
            encoded[index] = System.Text.Encoding.BigEndianUnicode.GetBytes(values[index]);
            builder.WriteUInt16(3).WriteUInt16(1)
                .WriteUInt16((ushort)languageIds[index])
                .WriteUInt16((ushort)nameIds[index])
                .WriteUInt16((ushort)encoded[index].Length)
                .WriteUInt16((ushort)stringOffset);
            stringOffset += encoded[index].Length;
            index += 1;
        }
        index = 0;
        while (index < encoded.Length)
        {
            builder.WriteBytes(encoded[index]);
            index += 1;
        }
        return builder.CopyBytes();
    }
}
