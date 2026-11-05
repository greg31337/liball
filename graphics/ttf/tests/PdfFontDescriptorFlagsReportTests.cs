using ttf;

namespace ttf.Tests;

internal class PdfFontDescriptorFlagsReportTests
{
    [Test]
    public void DerivesItalicForceBoldAndNonsymbolicFromOs2()
    {
        FontInstance instance = FontMatchingTests.CreateStaticFace(
            "Bold Italic",
            "Bold Italic",
            700,
            5,
            true,
            true,
            false).CreateDefaultInstance();

        PdfFontDescriptorFlagsReport report =
            PdfFontDescriptorFlagsBuilder.Build(instance);

        Assert.Multiple(() =>
        {
            Assert.That(report.GetFlags(), Is.EqualTo(
                (int)PdfFontDescriptorFlag.Nonsymbolic |
                (int)PdfFontDescriptorFlag.Italic |
                (int)PdfFontDescriptorFlag.ForceBold));
            Assert.That(report.GetInferenceCount(), Is.EqualTo(9));
            Assert.That(report.HasFlag(PdfFontDescriptorFlag.Symbolic),
                Is.False);
            Assert.That(report.HasFlag(PdfFontDescriptorFlag.Nonsymbolic),
                Is.True);
            Assert.That(report.HasFlag(PdfFontDescriptorFlag.Italic),
                Is.True);
            Assert.That(report.HasFlag(PdfFontDescriptorFlag.ForceBold),
                Is.True);
            Assert.That(report.FindInference(
                PdfFontDescriptorFlag.Italic).GetEvidence(),
                Is.EqualTo(PdfFontDescriptorFlagEvidence.Os2Selection));
            Assert.That(report.FindInference(
                PdfFontDescriptorFlag.ForceBold).GetEvidence(),
                Is.EqualTo(PdfFontDescriptorFlagEvidence.Multiple));
        });
    }

    [Test]
    public void CombinesPostOs2AndUppercaseCmapEvidence()
    {
        FontFace face = CreateClassifiedFace(
            false,
            true,
            true,
            1,
            true,
            true);

        PdfFontDescriptorFlagsReport report =
            PdfFontDescriptorFlagsBuilder.Build(
                face.CreateDefaultInstance());

        Assert.Multiple(() =>
        {
            Assert.That(report.HasFlag(PdfFontDescriptorFlag.FixedPitch),
                Is.True);
            Assert.That(report.HasFlag(PdfFontDescriptorFlag.Serif),
                Is.True);
            Assert.That(report.HasFlag(PdfFontDescriptorFlag.Symbolic),
                Is.False);
            Assert.That(report.HasFlag(PdfFontDescriptorFlag.Nonsymbolic),
                Is.True);
            Assert.That(report.HasFlag(PdfFontDescriptorFlag.Italic),
                Is.True);
            Assert.That(report.HasFlag(PdfFontDescriptorFlag.AllCap),
                Is.True);
            Assert.That(report.HasFlag(PdfFontDescriptorFlag.SmallCap),
                Is.False);
            Assert.That(report.FindInference(
                PdfFontDescriptorFlag.FixedPitch).GetEvidence(),
                Is.EqualTo(PdfFontDescriptorFlagEvidence.Multiple));
            Assert.That(report.FindInference(
                PdfFontDescriptorFlag.Serif).GetEvidence(),
                Is.EqualTo(PdfFontDescriptorFlagEvidence.Multiple));
            Assert.That(report.FindInference(
                PdfFontDescriptorFlag.AllCap).GetEvidence(),
                Is.EqualTo(
                    PdfFontDescriptorFlagEvidence.CmapLatinCoverage));
            Assert.That(report.FindInference(
                PdfFontDescriptorFlag.SmallCap).GetEvidence(),
                Is.EqualTo(
                    PdfFontDescriptorFlagEvidence.ConservativeDefault));
        });
    }

    [Test]
    public void SymbolCmapOverridesNonsymbolicAndSuppressesAllCap()
    {
        FontFace face = CreateClassifiedFace(
            true,
            false,
            false,
            10,
            false,
            false);

        PdfFontDescriptorFlagsReport report =
            PdfFontDescriptorFlagsBuilder.Build(
                face.CreateDefaultInstance());

        Assert.Multiple(() =>
        {
            Assert.That(report.HasFlag(PdfFontDescriptorFlag.Symbolic),
                Is.True);
            Assert.That(report.HasFlag(PdfFontDescriptorFlag.Nonsymbolic),
                Is.False);
            Assert.That(report.HasFlag(PdfFontDescriptorFlag.Script),
                Is.True);
            Assert.That(report.HasFlag(PdfFontDescriptorFlag.AllCap),
                Is.False);
            Assert.That(report.FindInference(
                PdfFontDescriptorFlag.Symbolic).GetEvidence(),
                Is.EqualTo(PdfFontDescriptorFlagEvidence.Multiple));
        });
    }

    [Test]
    public void MissingMetadataUsesVisibleConservativeInferences()
    {
        PdfFontDescriptorFlagsReport report =
            PdfFontDescriptorFlagsBuilder.Build(
                FontInstanceTests.CreateFace().CreateDefaultInstance());
        PdfFontDescriptorFlagInference fixedPitch = report.FindInference(
            PdfFontDescriptorFlag.FixedPitch);
        PdfFontDescriptorFlagInference nonsymbolic = report.FindInference(
            PdfFontDescriptorFlag.Nonsymbolic);

        Assert.Multiple(() =>
        {
            Assert.That(report.GetFlags(),
                Is.EqualTo((int)PdfFontDescriptorFlag.Nonsymbolic));
            Assert.That(fixedPitch.IsSet(), Is.False);
            Assert.That(fixedPitch.GetEvidence(),
                Is.EqualTo(PdfFontDescriptorFlagEvidence.Unavailable));
            Assert.That(nonsymbolic.IsSet(), Is.True);
            Assert.That(nonsymbolic.GetEvidence(),
                Is.EqualTo(PdfFontDescriptorFlagEvidence.Unavailable));
            Assert.That(nonsymbolic.GetExplanation(), Is.Not.Empty);
        });
    }

    [Test]
    public void ReturnsDefensiveInferenceArraysAndRejectsUnknownFlags()
    {
        PdfFontDescriptorFlagsReport report =
            PdfFontDescriptorFlagsBuilder.Build(
                FontInstanceTests.CreateFace().CreateDefaultInstance());
        PdfFontDescriptorFlagInference?[] inferences =
            report.GetInferences();
        inferences[0] = null;

        Assert.Multiple(() =>
        {
            Assert.That(report.GetInference(0), Is.Not.Null);
            Assert.That(report.GetInference(0).GetFlag(),
                Is.EqualTo(PdfFontDescriptorFlag.FixedPitch));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                report.GetInference(9));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                report.HasFlag((PdfFontDescriptorFlag)16));
            Assert.Throws<ArgumentNullException>(() =>
                PdfFontDescriptorFlagsBuilder.Build(null!));
        });
    }

    [Test]
    public void FontProgramArtifactsExposeDescriptorFlagReports()
    {
        FontInstance trueType = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        PdfFontUsage trueTypeUsage = CreateUsage(
            trueType,
            PdfFontOptions.Create());
        PdfCidFontType2Artifact type2 = PdfCidFontType2Builder.Build(
            trueTypeUsage,
            PdfCidAllocator.Create(trueTypeUsage));

        FontInstance cff = FontInstanceCffTests.CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            CffCharsetEncodingTests.BuildCidCff(false),
            4).CreateDefaultInstance();
        PdfFontUsage cffUsage = CreateUsage(cff, PdfFontOptions.Create());
        PdfCidFontType0Artifact type0 = PdfCidFontType0Builder.Build(
            cffUsage,
            PdfCidAllocator.Create(cffUsage));

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
            Assert.That(type2.GetDescriptorFlags().GetInferenceCount(),
                Is.EqualTo(9));
            Assert.That(type0.GetDescriptorFlags().GetInferenceCount(),
                Is.EqualTo(9));
            Assert.That(openType.GetDescriptorFlags().GetInferenceCount(),
                Is.EqualTo(9));
        });
    }

    static PdfFontUsage CreateUsage(
        FontInstance instance,
        PdfFontOptions options)
    {
        return new PdfFontUsageAccumulator().GetOrAdd(instance, options);
    }

    static FontFace CreateClassifiedFace(
        bool symbol,
        bool fixedPitch,
        bool italicAngle,
        int familyClass,
        bool panoseSerif,
        bool panoseMonospace)
    {
        byte[] cmap = BuildGroupedCmap(
            symbol ? 0 : 10,
            symbol ? 0xF041 : 0x41,
            symbol ? 0xF042 : 0x5A,
            1);
        FontFace source = FontMatchingTests.CreateStaticFaceWithCmap(
            "Descriptor Test",
            cmap,
            32);
        List<SfntTableData> tables = new List<SfntTableData>();
        int index = 0;

        while (index < source.GetTableCount())
        {
            FontTableInfo table = source.GetTableInfo(index);
            byte[] bytes = table.CopyData();

            if (table.GetTag() == FontTags.Os2)
            {
                bytes[30] = (byte)familyClass;
                bytes[31] = 0;
                bytes[32] = panoseSerif || panoseMonospace ?
                    (byte)2 : (byte)0;
                bytes[33] = panoseSerif ? (byte)2 : (byte)0;
                bytes[35] = panoseMonospace ? (byte)9 : (byte)0;
            }

            tables.Add(CreateTable(table.GetTag(), bytes));
            index += 1;
        }

        tables.Add(CreateTable(
            FontTags.Post,
            BuildPost(fixedPitch, italicAngle)));
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

    static byte[] BuildGroupedCmap(
        int encodingId,
        int start,
        int end,
        int startGlyphId)
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt16(3)
            .WriteUInt16((ushort)encodingId)
            .WriteUInt32(12)
            .WriteUInt16(12)
            .WriteUInt16(0)
            .WriteUInt32(28)
            .WriteUInt32(0)
            .WriteUInt32(1)
            .WriteUInt32((uint)start)
            .WriteUInt32((uint)end)
            .WriteUInt32((uint)startGlyphId)
            .CopyBytes();
    }

    static byte[] BuildPost(bool fixedPitch, bool italic)
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00030000U)
            .WriteInt32(italic ? -786432 : 0)
            .WriteInt16(-100)
            .WriteInt16(50)
            .WriteUInt32(fixedPitch ? 1U : 0U)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .CopyBytes();
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
}
