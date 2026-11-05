using ttf;

namespace ttf.Tests;

public class FontSubsetRightsTests
{
    [TestCase(2, FontSubsetEmbeddingMode.Subset)]
    [TestCase(2, FontSubsetEmbeddingMode.FullFont)]
    [TestCase(0x0100, FontSubsetEmbeddingMode.Subset)]
    [TestCase(0x0200, FontSubsetEmbeddingMode.Subset)]
    public void RejectPolicyStopsConflictingRequests(
        int flags,
        FontSubsetEmbeddingMode mode)
    {
        FontSubsetBuilder builder = CreateBuilder(
            CreateFace(flags, FontOutlineKind.TrueType),
            mode,
            FontSubsetRightsPolicy.Reject);

        Assert.Throws<FontEmbeddingException>(() => builder.Build());
    }

    [Test]
    public void ReportPolicyPreservesRequestAndReturnsEveryRestriction()
    {
        FontSubset subset = CreateBuilder(
            CreateFace(2 | 0x0100 | 0x0200, FontOutlineKind.TrueType),
            FontSubsetEmbeddingMode.Subset,
            FontSubsetRightsPolicy.Report).Build();
        FontSubsetEmbeddingDecision decision =
            subset.GetEmbeddingDecision();
        int[] glyphMap = subset.GetOldToNewGlyphMap();

        Assert.Multiple(() =>
        {
            Assert.That(decision.HasRightsInformation(), Is.True);
            Assert.That(decision.GetRights(), Is.Not.Null);
            Assert.That(decision.GetRights()!.AllowsEmbedding(), Is.False);
            Assert.That(decision.GetRequestedEmbeddingMode(),
                Is.EqualTo(FontSubsetEmbeddingMode.Subset));
            Assert.That(decision.GetEffectiveEmbeddingMode(),
                Is.EqualTo(FontSubsetEmbeddingMode.Subset));
            Assert.That(decision.GetPolicy(),
                Is.EqualTo(FontSubsetRightsPolicy.Report));
            Assert.That(decision.HasPolicyConflict(), Is.True);
            Assert.That(decision.WasConflictReported(), Is.True);
            Assert.That(decision.WasFullFontForced(), Is.False);
            Assert.That(decision.GetDiagnosticCount(), Is.EqualTo(3));
            Assert.That(decision.GetDiagnostic(0).GetCode(),
                Is.EqualTo(FontDiagnosticCode.EmbeddingRestricted));
            Assert.That(decision.GetDiagnostic(1).GetCode(),
                Is.EqualTo(FontDiagnosticCode.SubsettingRestricted));
            Assert.That(decision.GetDiagnostic(2).GetCode(),
                Is.EqualTo(FontDiagnosticCode.BitmapEmbeddingOnly));
            Assert.That(decision.GetDiagnostic(0).GetTableTag(),
                Is.EqualTo(FontTags.Os2));
            Assert.That(decision.GetDiagnostic(0).GetLogicalPath(),
                Is.EqualTo("subset.rights.fsType"));
            Assert.That(subset.GetDiagnostics(), Has.Length.EqualTo(3));
            Assert.That(glyphMap[0], Is.Zero);
            Assert.That(glyphMap[1], Is.EqualTo(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                decision.GetDiagnostic(3));
        });
    }

    [Test]
    public void FullFontPolicyTransparentlyForcesNoSubsettingRequest()
    {
        FontSubset subset = CreateBuilder(
            CreateFace(0x0100, FontOutlineKind.TrueType),
            FontSubsetEmbeddingMode.Subset,
            FontSubsetRightsPolicy.EmbedFullFont).Build();
        FontSubsetEmbeddingDecision decision =
            subset.GetEmbeddingDecision();
        int[] oldToNew = subset.GetOldToNewGlyphMap();
        byte[] fullFont = subset.CreateFullFontData();
        FontSubsetResult result = subset.CreateResult();

        Assert.Multiple(() =>
        {
            Assert.That(decision.GetRequestedEmbeddingMode(),
                Is.EqualTo(FontSubsetEmbeddingMode.Subset));
            Assert.That(decision.GetEffectiveEmbeddingMode(),
                Is.EqualTo(FontSubsetEmbeddingMode.FullFont));
            Assert.That(decision.HasPolicyConflict(), Is.True);
            Assert.That(decision.WasConflictReported(), Is.False);
            Assert.That(decision.WasFullFontForced(), Is.True);
            Assert.That(decision.GetDiagnosticCount(), Is.EqualTo(1));
            Assert.That(decision.GetDiagnostic(0).GetCode(),
                Is.EqualTo(FontDiagnosticCode.SubsettingRestricted));
            Assert.That(subset.CopyOptions().GetEmbeddingMode(),
                Is.EqualTo(FontSubsetEmbeddingMode.FullFont));
            Assert.That(result.GetEffectiveOptions().GetEmbeddingMode(),
                Is.EqualTo(FontSubsetEmbeddingMode.FullFont));
            Assert.That(result.GetPrefixReport().GetPrefix(), Is.Empty);
            Assert.That(oldToNew, Is.EqualTo(new int[] { 0, 1, 2, 3 }));
            Assert.That(FontChecksum.VerifyFont(fullFont), Is.True);
        });
    }

    [TestCase(2)]
    [TestCase(0x0200)]
    [TestCase(2 | 0x0100)]
    public void FullFontPolicyDoesNotOverrideUnresolvableRestrictions(
        int flags)
    {
        FontSubsetBuilder builder = CreateBuilder(
            CreateFace(flags, FontOutlineKind.TrueType),
            FontSubsetEmbeddingMode.Subset,
            FontSubsetRightsPolicy.EmbedFullFont);

        Assert.Throws<FontEmbeddingException>(() => builder.Build());
    }

    [Test]
    public void CompliantFullAndBitmapRequestsRetainRestrictionReports()
    {
        FontSubset full = CreateBuilder(
            CreateFace(0x0100, FontOutlineKind.TrueType),
            FontSubsetEmbeddingMode.FullFont,
            FontSubsetRightsPolicy.Reject).Build();
        FontSubset bitmap = CreateBuilder(
            CreateFace(0x0200, FontOutlineKind.BitmapOnly),
            FontSubsetEmbeddingMode.Subset,
            FontSubsetRightsPolicy.Reject).Build();

        Assert.Multiple(() =>
        {
            Assert.That(full.GetEmbeddingDecision().HasPolicyConflict(),
                Is.False);
            Assert.That(full.GetEmbeddingDecision().WasFullFontForced(),
                Is.False);
            Assert.That(full.GetEmbeddingDecision().GetDiagnosticCount(),
                Is.EqualTo(1));
            Assert.That(bitmap.GetEmbeddingDecision().HasPolicyConflict(),
                Is.False);
            Assert.That(bitmap.GetEmbeddingDecision().GetDiagnosticCount(),
                Is.EqualTo(1));
            Assert.That(bitmap.GetEmbeddingDecision().GetDiagnostic(0)
                .GetCode(), Is.EqualTo(
                    FontDiagnosticCode.BitmapEmbeddingOnly));
        });
    }

    [Test]
    public void MissingOs2RightsAreExplicitlyReportedAsUnavailable()
    {
        FontSubset subset = CreateBuilder(
            FontInstanceTests.CreateFace(),
            FontSubsetEmbeddingMode.Subset,
            FontSubsetRightsPolicy.Reject).Build();
        FontSubsetEmbeddingDecision decision =
            subset.GetEmbeddingDecision();

        Assert.Multiple(() =>
        {
            Assert.That(decision.HasRightsInformation(), Is.False);
            Assert.That(decision.GetRights(), Is.Null);
            Assert.That(decision.HasPolicyConflict(), Is.False);
            Assert.That(decision.GetDiagnosticCount(), Is.Zero);
            Assert.That(decision.GetRequestedEmbeddingMode(),
                Is.EqualTo(FontSubsetEmbeddingMode.Subset));
            Assert.That(decision.GetEffectiveEmbeddingMode(),
                Is.EqualTo(FontSubsetEmbeddingMode.Subset));
        });
    }

    [Test]
    public void Os2TooShortForFsTypeIsExplicitlyUnavailable()
    {
        FontSubset subset = CreateBuilder(
            CreateFaceWithOs2(new byte[9], FontOutlineKind.TrueType),
            FontSubsetEmbeddingMode.Subset,
            FontSubsetRightsPolicy.Reject).Build();

        Assert.Multiple(() =>
        {
            Assert.That(subset.GetEmbeddingDecision()
                .HasRightsInformation(), Is.False);
            Assert.That(subset.GetEmbeddingDecision()
                .HasPolicyConflict(), Is.False);
            Assert.That(subset.GetEmbeddingDecision()
                .GetDiagnosticCount(), Is.Zero);
        });
    }

    static FontSubsetBuilder CreateBuilder(
        FontFace face,
        FontSubsetEmbeddingMode mode,
        FontSubsetRightsPolicy policy)
    {
        FontSubsetOptions options = FontSubsetOptions.CreateStrict();
        options.SetEmbeddingMode(mode);
        options.SetRightsPolicy(policy);
        return new FontSubsetBuilder(
            face.CreateDefaultInstance(),
            options);
    }

    static FontFace CreateFace(int flags, FontOutlineKind outlineKind)
    {
        return CreateFaceWithOs2(BuildOs2(flags), outlineKind);
    }

    static FontFace CreateFaceWithOs2(
        byte[] os2,
        FontOutlineKind outlineKind)
    {
        FontFace sourceFace = FontInstanceTests.CreateFace();
        SfntTableStore source = sourceFace.GetTableStore();
        System.Collections.Generic.List<SfntTableData> tables =
            new System.Collections.Generic.List<SfntTableData>();
        int index = 0;

        while (index < source.GetTableCount())
        {
            SfntTableData table = source.GetTable(index);

            if (outlineKind != FontOutlineKind.BitmapOnly ||
                (table.GetTag() != FontTags.Glyf &&
                    table.GetTag() != FontTags.Loca))
            {
                tables.Add(table);
            }

            index += 1;
        }

        AddTable(tables, FontTags.Os2, os2);
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                tables.ToArray(),
                Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        return new FontFace(
            0,
            sourceFace.GetScalerType(),
            new SfntValidationResult(
                outlineKind,
                Array.Empty<FontDiagnostic>()),
            cache);
    }

    static byte[] BuildOs2(int flags)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(5)
            .WriteInt16(500)
            .WriteUInt16(400)
            .WriteUInt16(5)
            .WriteUInt16((ushort)flags)
            .WriteInt16(650)
            .WriteInt16(650)
            .WriteInt16(0)
            .WriteInt16(140)
            .WriteInt16(650)
            .WriteInt16(650)
            .WriteInt16(0)
            .WriteInt16(480)
            .WriteInt16(50)
            .WriteInt16(300)
            .WriteInt16(0);

        int index = 0;
        while (index < 10)
        {
            builder.WriteUInt8(0);
            index += 1;
        }

        return builder
            .WriteUInt32(1)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(0x54455354U)
            .WriteUInt16(0)
            .WriteUInt16(0x20)
            .WriteUInt16(0x7E)
            .WriteInt16(800)
            .WriteInt16(-200)
            .WriteInt16(0)
            .WriteUInt16(900)
            .WriteUInt16(250)
            .WriteUInt32(1)
            .WriteUInt32(0)
            .WriteInt16(450)
            .WriteInt16(700)
            .WriteUInt16(0)
            .WriteUInt16(0x20)
            .WriteUInt16(2)
            .WriteUInt16(0)
            .WriteUInt16(0xFFFF)
            .CopyBytes();
    }

    static void AddTable(
        System.Collections.Generic.List<SfntTableData> tables,
        int tag,
        byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        tables.Add(new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true));
    }
}
