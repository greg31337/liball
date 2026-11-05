using ttf;

namespace ttf.Tests;

internal class PdfFontDescriptorMetricsTests
{
    [Test]
    public void DerivesAndNormalizesDescriptorMetricsFromFontTables()
    {
        FontInstance instance = CreateMetadataFace().CreateDefaultInstance();

        PdfFontDescriptorMetrics metrics =
            PdfFontDescriptorMetricsBuilder.Build(
                instance,
                PdfEmbeddedFontFileKind.FontFile2,
                null,
                321);

        Assert.Multiple(() =>
        {
            Assert.That(metrics.GetBoundingBoxXMinimum(), Is.EqualTo(-11));
            Assert.That(metrics.GetBoundingBoxYMinimum(), Is.EqualTo(-16));
            Assert.That(metrics.GetBoundingBoxXMaximum(), Is.EqualTo(501));
            Assert.That(metrics.GetBoundingBoxYMaximum(), Is.EqualTo(451));
            Assert.That(metrics.GetBoundingBoxSource(), Is.EqualTo(
                PdfFontDescriptorMetricSource.Head));
            Assert.That(metrics.GetItalicAngle(), Is.EqualTo(-12.5));
            Assert.That(metrics.GetItalicAngleSource(), Is.EqualTo(
                PdfFontDescriptorMetricSource.Post));
            Assert.That(metrics.GetAscent(), Is.EqualTo(800));
            Assert.That(metrics.GetDescent(), Is.EqualTo(-200));
            Assert.That(metrics.GetLineMetricSource(), Is.EqualTo(
                FontLineMetricSource.Typographic));
            Assert.That(metrics.GetLineMetricsSource(), Is.EqualTo(
                PdfFontDescriptorMetricSource.RecommendedLineMetrics));
            Assert.That(metrics.GetCapHeight(), Is.EqualTo(701));
            Assert.That(metrics.GetCapHeightSource(), Is.EqualTo(
                PdfFontDescriptorMetricSource.Os2));
            Assert.That(metrics.HasXHeight(), Is.True);
            Assert.That(metrics.GetXHeight(), Is.EqualTo(451));
            Assert.That(metrics.GetXHeightSource(), Is.EqualTo(
                PdfFontDescriptorMetricSource.Os2));
            Assert.That(metrics.GetMissingWidth(), Is.EqualTo(300));
            Assert.That(metrics.GetMissingWidthSource(), Is.EqualTo(
                PdfFontDescriptorMetricSource.NotdefAdvance));
            Assert.That(metrics.GetStemVertical(), Is.EqualTo(120));
            Assert.That(metrics.GetStemVerticalSource(), Is.EqualTo(
                PdfFontDescriptorMetricSource.Os2WeightClass));
        });

        Assert.Multiple(() =>
        {
            Assert.That(metrics.GetFontFileKind(), Is.EqualTo(
                PdfEmbeddedFontFileKind.FontFile2));
            Assert.That(metrics.GetFontFileKey(), Is.EqualTo("FontFile2"));
            Assert.That(metrics.HasFontFileSubtype(), Is.False);
            Assert.That(metrics.GetFontFileSubtype(), Is.Null);
            Assert.That(metrics.GetFontFileLength(), Is.EqualTo(321));
            Assert.That(metrics.HasLength1(), Is.True);
            Assert.That(metrics.GetLength1(), Is.EqualTo(321));
        });
    }

    [Test]
    public void UsesVisibleFallbacksWhenOptionalMetadataIsUnavailable()
    {
        FontInstance instance = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();

        PdfFontDescriptorMetrics metrics =
            PdfFontDescriptorMetricsBuilder.Build(
                instance,
                PdfEmbeddedFontFileKind.FontFile3,
                "OpenType",
                456);

        Assert.Multiple(() =>
        {
            Assert.That(metrics.GetItalicAngle(), Is.Zero);
            Assert.That(metrics.GetItalicAngleSource(), Is.EqualTo(
                PdfFontDescriptorMetricSource.Unavailable));
            Assert.That(metrics.GetCapHeight(), Is.EqualTo(750));
            Assert.That(metrics.GetCapHeightSource(), Is.EqualTo(
                PdfFontDescriptorMetricSource.AscentFallback));
            Assert.That(metrics.HasXHeight(), Is.False);
            Assert.That(metrics.GetXHeightSource(), Is.EqualTo(
                PdfFontDescriptorMetricSource.Unavailable));
            Assert.Throws<InvalidOperationException>(() =>
                metrics.GetXHeight());
            Assert.That(metrics.GetMissingWidth(), Is.EqualTo(600));
            Assert.That(metrics.GetStemVertical(), Is.EqualTo(80));
            Assert.That(metrics.GetStemVerticalSource(), Is.EqualTo(
                PdfFontDescriptorMetricSource.DefaultStemEstimate));
            Assert.That(metrics.GetFontFileKind(), Is.EqualTo(
                PdfEmbeddedFontFileKind.FontFile3));
            Assert.That(metrics.GetFontFileKey(), Is.EqualTo("FontFile3"));
            Assert.That(metrics.HasFontFileSubtype(), Is.True);
            Assert.That(metrics.GetFontFileSubtype(), Is.EqualTo("OpenType"));
            Assert.That(metrics.GetFontFileLength(), Is.EqualTo(456));
            Assert.That(metrics.HasLength1(), Is.False);
            Assert.Throws<InvalidOperationException>(() =>
                metrics.GetLength1());
        });
    }

    [Test]
    public void DerivesCapAndXHeightFromMappedGlyphBounds()
    {
        FontInstance instance = CreateCmapFallbackFace()
            .CreateDefaultInstance();

        PdfFontDescriptorMetrics metrics =
            PdfFontDescriptorMetricsBuilder.Build(
                instance,
                PdfEmbeddedFontFileKind.FontFile2,
                null,
                1);

        Assert.Multiple(() =>
        {
            Assert.That(metrics.GetCapHeight(), Is.EqualTo(9));
            Assert.That(metrics.GetCapHeightSource(), Is.EqualTo(
                PdfFontDescriptorMetricSource.CmapGlyphBounds));
            Assert.That(metrics.HasXHeight(), Is.True);
            Assert.That(metrics.GetXHeight(), Is.EqualTo(9));
            Assert.That(metrics.GetXHeightSource(), Is.EqualTo(
                PdfFontDescriptorMetricSource.CmapGlyphBounds));
        });
    }

    [Test]
    public void ArtifactsExposeMatchingProgramMetadata()
    {
        FontInstance trueType = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        PdfFontUsage type2Usage = CreateUsage(
            trueType,
            PdfFontOptions.Create());
        PdfCidFontType2Artifact type2 = PdfCidFontType2Builder.Build(
            type2Usage,
            PdfCidAllocator.Create(type2Usage));

        FontInstance cff = FontInstanceCffTests.CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            CffCharsetEncodingTests.BuildCidCff(false),
            4).CreateDefaultInstance();
        PdfFontUsage type0Usage = CreateUsage(cff, PdfFontOptions.Create());
        PdfCidFontType0Artifact type0 = PdfCidFontType0Builder.Build(
            type0Usage,
            PdfCidAllocator.Create(type0Usage));

        PdfFontOptions openTypeOptions = PdfFontOptions.Create();
        openTypeOptions.SetFontKind(PdfFontKind.Type0OpenType);
        openTypeOptions.SetVersion(PdfVersion.Pdf1_6);
        PdfFontUsage openTypeUsage = CreateUsage(
            trueType,
            openTypeOptions);
        PdfOpenTypeFontArtifact openType = PdfOpenTypeFontBuilder.Build(
            openTypeUsage,
            PdfCidAllocator.Create(openTypeUsage));

        Assert.Multiple(() =>
        {
            Assert.That(type2.GetDescriptorMetrics().GetFontFileLength(),
                Is.EqualTo(type2.CopyFontFile2().Length));
            Assert.That(type2.GetDescriptorMetrics().GetFontFileKind(),
                Is.EqualTo(PdfEmbeddedFontFileKind.FontFile2));
            Assert.That(type2.GetDescriptorMetrics().GetLength1(),
                Is.EqualTo(type2.CopyFontFile2().Length));
            Assert.That(type0.GetDescriptorMetrics().GetFontFileLength(),
                Is.EqualTo(type0.CopyFontFile3().Length));
            Assert.That(type0.GetDescriptorMetrics().GetFontFileSubtype(),
                Is.EqualTo("CIDFontType0C"));
            Assert.That(openType.GetDescriptorMetrics().GetFontFileLength(),
                Is.EqualTo(openType.CopyFontFile3().Length));
            Assert.That(openType.GetDescriptorMetrics().GetFontFileSubtype(),
                Is.EqualTo("OpenType"));
        });
    }

    [Test]
    public void UsesResolvedBoundsAndWeightForVariableInstances()
    {
        FontCoordinates coordinates = new FontCoordinates();
        coordinates.Set(
            (int)TestSfntEncoding.EncodeTag("wght"),
            0.5);
        FontInstance instance = FontInstanceTests.CreateVariableFace(false)
            .CreateInstance(coordinates);

        PdfFontDescriptorMetrics metrics =
            PdfFontDescriptorMetricsBuilder.Build(
                instance,
                PdfEmbeddedFontFileKind.FontFile2,
                null,
                1);

        Assert.Multiple(() =>
        {
            Assert.That(metrics.GetBoundingBoxSource(), Is.EqualTo(
                PdfFontDescriptorMetricSource.ResolvedGlyphBounds));
            Assert.That(metrics.GetBoundingBoxXMaximum(), Is.LessThan(1000));
            Assert.That(metrics.GetBoundingBoxYMaximum(), Is.LessThan(1000));
            Assert.That(metrics.GetStemVertical(), Is.EqualTo(50));
            Assert.That(metrics.GetStemVerticalSource(), Is.EqualTo(
                PdfFontDescriptorMetricSource.VariationWeightAxis));
        });
    }

    [Test]
    public void ValidatesBuilderInputsAndSignedMetricRounding()
    {
        FontInstance instance = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();

        Assert.Multiple(() =>
        {
            Assert.That(PdfFontDescriptorMetricsBuilder.ConvertMetric(
                1,
                2), Is.EqualTo(500));
            Assert.That(PdfFontDescriptorMetricsBuilder.ConvertMetric(
                -1,
                2), Is.EqualTo(-500));
            Assert.That(PdfFontDescriptorMetricsBuilder.ConvertMetric(
                1,
                2000), Is.EqualTo(1));
            Assert.That(PdfFontDescriptorMetricsBuilder.ConvertMetric(
                -1,
                2000), Is.EqualTo(-1));
            Assert.Throws<ArgumentNullException>(() =>
                PdfFontDescriptorMetricsBuilder.Build(
                    null!,
                    PdfEmbeddedFontFileKind.FontFile2,
                    null,
                    0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PdfFontDescriptorMetricsBuilder.Build(
                    instance,
                    (PdfEmbeddedFontFileKind)9,
                    null,
                    0));
            Assert.Throws<ArgumentException>(() =>
                PdfFontDescriptorMetricsBuilder.Build(
                    instance,
                    PdfEmbeddedFontFileKind.FontFile2,
                    "OpenType",
                    0));
            Assert.Throws<ArgumentException>(() =>
                PdfFontDescriptorMetricsBuilder.Build(
                    instance,
                    PdfEmbeddedFontFileKind.FontFile3,
                    null,
                    0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PdfFontDescriptorMetricsBuilder.Build(
                    instance,
                    PdfEmbeddedFontFileKind.FontFile2,
                    null,
                    -1));
        });
    }

    static PdfFontUsage CreateUsage(
        FontInstance instance,
        PdfFontOptions options)
    {
        return new PdfFontUsageAccumulator().GetOrAdd(instance, options);
    }

    static FontFace CreateMetadataFace()
    {
        FontFace source = FontInstanceTests.CreateFace();
        List<SfntTableData> tables = CopyTables(source);
        ReplaceHead(tables, 2000, -21, -31, 1001, 901);
        tables.Add(CreateTable(FontTags.Os2, BuildOs2()));
        tables.Add(CreateTable(FontTags.Post, BuildPost()));
        return CreateFace(source, tables);
    }

    static FontFace CreateCmapFallbackFace()
    {
        FontFace source = FontInstanceTests.CreateFace();
        List<SfntTableData> tables = CopyTables(source);
        tables.Add(CreateTable(FontTags.Cmap, BuildFallbackCmap()));
        return CreateFace(source, tables);
    }

    static List<SfntTableData> CopyTables(FontFace source)
    {
        List<SfntTableData> tables = new List<SfntTableData>();
        int index = 0;

        while (index < source.GetTableCount())
        {
            FontTableInfo table = source.GetTableInfo(index);
            tables.Add(CreateTable(table.GetTag(), table.CopyData()));
            index += 1;
        }

        return tables;
    }

    static void ReplaceHead(
        List<SfntTableData> tables,
        int unitsPerEm,
        int xMinimum,
        int yMinimum,
        int xMaximum,
        int yMaximum)
    {
        int index = 0;

        while (index < tables.Count)
        {
            if (tables[index].GetTag() == FontTags.Head)
            {
                byte[] bytes = tables[index].CopyRawData();
                WriteUInt16(bytes, 18, unitsPerEm);
                WriteInt16(bytes, 36, xMinimum);
                WriteInt16(bytes, 38, yMinimum);
                WriteInt16(bytes, 40, xMaximum);
                WriteInt16(bytes, 42, yMaximum);
                tables[index] = CreateTable(FontTags.Head, bytes);
                return;
            }

            index += 1;
        }

        Assert.Fail("The source face did not contain head.");
    }

    static byte[] BuildOs2()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(4).WriteInt16(500)
            .WriteUInt16(700).WriteUInt16(5).WriteUInt16(0)
            .WriteInt16(600).WriteInt16(600).WriteInt16(0).WriteInt16(100)
            .WriteInt16(600).WriteInt16(600).WriteInt16(0).WriteInt16(400)
            .WriteInt16(50).WriteInt16(250).WriteInt16(0)
            .WriteZeros(10)
            .WriteUInt32(1).WriteUInt32(0).WriteUInt32(0).WriteUInt32(0)
            .WriteUInt32(0x54455354U).WriteUInt16(1 << 7)
            .WriteUInt16(0x20).WriteUInt16(0x7E)
            .WriteInt16(1600).WriteInt16(-400).WriteInt16(40)
            .WriteUInt16(1800).WriteUInt16(500)
            .WriteUInt32(0).WriteUInt32(0)
            .WriteInt16(901).WriteInt16(1401)
            .WriteUInt16(0).WriteUInt16(0x20).WriteUInt16(2);
        return builder.CopyBytes();
    }

    static byte[] BuildPost()
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00030000U)
            .WriteInt32(-819200)
            .WriteInt16(-100)
            .WriteInt16(50)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .CopyBytes();
    }

    static byte[] BuildFallbackCmap()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt16(3)
            .WriteUInt16(10)
            .WriteUInt32(12)
            .WriteUInt16(12)
            .WriteUInt16(0)
            .WriteUInt32(40)
            .WriteUInt32(0)
            .WriteUInt32(2)
            .WriteUInt32(0x48)
            .WriteUInt32(0x48)
            .WriteUInt32(2)
            .WriteUInt32(0x78)
            .WriteUInt32(0x78)
            .WriteUInt32(2)
            .CopyBytes();
    }

    static FontFace CreateFace(
        FontFace source,
        List<SfntTableData> tables)
    {
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                tables.ToArray(),
                Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        return new FontFace(
            0,
            source.GetScalerType(),
            new SfntValidationResult(
                source.GetOutlineKind(),
                Array.Empty<FontDiagnostic>()),
            cache);
    }

    static SfntTableData CreateTable(int tag, byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        return new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true);
    }

    static void WriteUInt16(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
    }

    static void WriteInt16(byte[] bytes, int offset, int value)
    {
        WriteUInt16(bytes, offset, value & 0xFFFF);
    }
}
