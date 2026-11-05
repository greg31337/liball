using System.Buffers.Binary;
using System.Text;
using ttf;

namespace ttf.Tests;

internal class PdfCoverageMatrixTests
{
    [Test]
    public void FinalizesTrueTypeCffAndPinnedVariableInstances()
    {
        FontInstance trueType = HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
        PdfFontResource trueTypeResource = BuildResource(
            trueType,
            4,
            new int[] { 0x42 });
        FontInstance cff = FontInstanceCffTests.CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            CffCharsetEncodingTests.BuildCidCff(false),
            4).CreateDefaultInstance();
        PdfFontResource cffResource = BuildResource(
            cff,
            3,
            new int[] { 0x43 });

        FontFace variableFace = VariableStaticInstanceDifferentialTests
            .GetVariableFontFace();
        FontCoordinates boldCoordinates = new FontCoordinates();
        boldCoordinates.Set(0x77676874, 700);
        PdfFontResource regularResource = BuildResource(
            variableFace.CreateDefaultInstance(),
            1,
            new int[] { 0x41 });
        PdfFontResource boldResource = BuildResource(
            variableFace.CreateInstance(boldCoordinates),
            1,
            new int[] { 0x41 });
        FontFace embeddedBold = FontLoader.Load(
            boldResource.CopyEmbeddedFont(),
            FontLoadOptions.CreateStrict()).GetFace(0);

        string trueTypePdf = Encoding.Latin1.GetString(
            MinimalPdfAssembler.Assemble(
                trueTypeResource,
                new byte[] { 0, 1 }));
        string cffPdf = Encoding.Latin1.GetString(
            MinimalPdfAssembler.Assemble(
                cffResource,
                new byte[] { 0, 1 }));

        Assert.Multiple(() =>
        {
            Assert.That(trueTypeResource.GetFontKind(),
                Is.EqualTo(PdfFontKind.Type0CidFontType2));
            Assert.That(trueTypePdf, Does.Contain("/CIDFontType2"));
            Assert.That(cffResource.GetFontKind(),
                Is.EqualTo(PdfFontKind.Type0CidFontType0));
            Assert.That(cffResource.CopyEmbeddedFont()[0], Is.EqualTo(1));
            Assert.That(cffPdf, Does.Contain("/CIDFontType0"));
            Assert.That(boldResource.GetDeduplicationKey(),
                Is.Not.EqualTo(regularResource.GetDeduplicationKey()));
            Assert.That(boldResource.GetCidFontType2Artifact()!
                .GetWidths().GetWidth(1), Is.EqualTo(672));
            Assert.That(embeddedBold.HasTable(FontTags.Fvar), Is.False);
            Assert.That(embeddedBold.HasTable(FontTags.Gvar), Is.False);
        });
    }

    [Test]
    public void KeepsFacesFromOneTrueTypeCollectionDistinct()
    {
        byte[] firstSfnt = HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance().CopyStaticSfntBytes();
        byte[] secondSfnt = FontToolsFreeTypeDifferentialTests
            .GetOracleFont();
        byte[] collectionBytes = BuildCollection(new byte[][]
        {
            firstSfnt,
            secondSfnt
        });
        FontPackage collection = FontLoader.Load(
            collectionBytes,
            FontLoadOptions.CreateCompatible());
        FontFace firstFace = collection.GetFace(0);
        FontFace secondFace = collection.GetFace(1);
        PdfFontResource firstResource = BuildResource(
            firstFace.CreateDefaultInstance(),
            4,
            new int[] { 0x42 });
        PdfFontResource secondResource = BuildResource(
            secondFace.CreateDefaultInstance(),
            2,
            new int[] { 0x41 });
        FontPackage firstEmbedded = FontLoader.Load(
            firstResource.CopyEmbeddedFont(),
            FontLoadOptions.CreateStrict());
        FontPackage secondEmbedded = FontLoader.Load(
            secondResource.CopyEmbeddedFont(),
            FontLoadOptions.CreateStrict());

        Assert.Multiple(() =>
        {
            Assert.That(collection.GetFaceCount(), Is.EqualTo(2));
            Assert.That(firstFace.GetNames().GetFamilyName()!.GetValue(),
                Is.Not.EqualTo(secondFace.GetNames()
                    .GetFamilyName()!.GetValue()));
            Assert.That(firstResource.GetDeduplicationKey(),
                Is.Not.EqualTo(secondResource.GetDeduplicationKey()));
            Assert.That(firstEmbedded.GetFaceCount(), Is.EqualTo(1));
            Assert.That(secondEmbedded.GetFaceCount(), Is.EqualTo(1));
            Assert.That(firstEmbedded.GetFace(0).GetMaxpTable()!
                .GetGlyphCount(), Is.EqualTo(2));
            Assert.That(secondEmbedded.GetFace(0).GetMaxpTable()!
                .GetGlyphCount(), Is.EqualTo(4));
            Assert.That(secondResource.GetCompatibilityReport()
                .GetEmbeddingRightsOutcome(), Is.EqualTo(
                    PdfFontEmbeddingRightsOutcome.FullFontForced));
        });
    }

    [Test]
    public void PreservesBmpSupplementaryAndCombiningMappings()
    {
        FontInstance instance = HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
        PdfFontBuilder builder = new PdfFontBuilder(
            instance,
            PdfFontOptions.Create());
        builder.AddGlyph(4, new int[] { 0x41 });
        builder.AddGlyph(5, new int[] { 0x1F600 });
        builder.AddGlyph(13, new int[] { 0x61, 0x0301 });

        PdfFontResource resource = builder.FinalizeFont();
        PdfToUnicodeMappingArtifact mappings =
            resource.GetToUnicodeMappings();
        string cmap = mappings.GetCMap().GetText();
        byte[] pdf = MinimalPdfAssembler.Assemble(
            resource,
            new byte[] { 0, 1, 0, 2, 0, 3 });

        Assert.Multiple(() =>
        {
            Assert.That(mappings.FindMapping(1)!
                .GetCMapUnicodeSequence(), Is.EqualTo(
                    new int[] { 0x41 }));
            Assert.That(mappings.FindMapping(2)!
                .GetCMapUnicodeSequence(), Is.EqualTo(
                    new int[] { 0x1F600 }));
            Assert.That(mappings.FindMapping(3)!
                .GetCMapUnicodeSequence(), Is.EqualTo(
                    new int[] { 0x61, 0x0301 }));
            Assert.That(cmap, Does.Contain("D83DDE00"));
            Assert.That(cmap, Does.Contain("00610301"));
            Assert.That(mappings.GetActualTextRecommendationCount(),
                Is.Zero);
            Assert.That(pdf, Is.Not.Empty);
        });
    }

    [Test]
    public void CreatesIndependentResourcesForResolvedFallbackFaces()
    {
        FontFace primary = HarfBuzzLayoutDifferentialTests.Load();
        FontFace fallback = FontLoader.Load(
            FontToolsFreeTypeDifferentialTests.GetOracleFont(),
            FontLoadOptions.CreateStrict()).GetFace(0);
        string primaryFamily = primary.GetNames()
            .GetFamilyName()!.GetValue();
        string fallbackFamily = fallback.GetNames()
            .GetFamilyName()!.GetValue();
        FontSet set = new FontSet();
        set.Add(primary);
        set.Add(fallback);
        set.AddFallbackFamily(fallbackFamily);

        FontFallbackList fallbacks = set.GetFallbacks(
            new FontRequest(primaryFamily),
            "Latn");
        PdfFontResource primaryResource = BuildResource(
            fallbacks.GetMatch(0).GetInstance(),
            4,
            new int[] { 0x42 });
        PdfFontResource fallbackResource = BuildResource(
            fallbacks.GetMatch(1).GetInstance(),
            2,
            new int[] { 0x41 });
        PdfFontResourceCollection resources =
            new PdfFontResourceCollection();
        resources.GetOrAdd(primaryResource);
        resources.GetOrAdd(fallbackResource);

        Assert.Multiple(() =>
        {
            Assert.That(fallbacks.GetCount(), Is.EqualTo(2));
            Assert.That(fallbacks.GetMatch(0).GetFace(),
                Is.SameAs(primary));
            Assert.That(fallbacks.GetMatch(1).GetFace(),
                Is.SameAs(fallback));
            Assert.That(resources.GetCount(), Is.EqualTo(2));
            Assert.That(primaryResource.GetDeduplicationKey(),
                Is.Not.EqualTo(fallbackResource.GetDeduplicationKey()));
            Assert.That(primaryResource.GetToUnicodeMappings()
                .FindMapping(1)!.GetCMapUnicodeSequence(),
                Is.EqualTo(new int[] { 0x42 }));
            Assert.That(fallbackResource.GetToUnicodeMappings()
                .FindMapping(1)!.GetCMapUnicodeSequence(),
                Is.EqualTo(new int[] { 0x41 }));
        });
    }

    [Test]
    public void EnforcesAndReportsEmbeddingRestrictionsByPolicy()
    {
        FontFace face = FontLoader.Load(
            FontToolsFreeTypeDifferentialTests.GetOracleFont(),
            FontLoadOptions.CreateStrict()).GetFace(0);
        FontInstance instance = face.CreateDefaultInstance();
        PdfFontResource forced = BuildResource(
            instance,
            2,
            new int[] { 0x41 });

        PdfFontOptions rejectOptions = PdfFontOptions.Create();
        rejectOptions.SetRightsPolicy(PdfFontRightsPolicy.Reject);
        PdfFontBuilder rejectedBuilder = new PdfFontBuilder(
            instance,
            rejectOptions);
        rejectedBuilder.AddGlyph(2, new int[] { 0x41 });

        PdfFontOptions reportOptions = PdfFontOptions.Create();
        reportOptions.SetRightsPolicy(PdfFontRightsPolicy.Report);
        reportOptions.SetArchivalProfile(PdfArchivalProfile.PdfA2);
        PdfFontResource reported = BuildResource(
            instance,
            reportOptions,
            2,
            new int[] { 0x41 });
        FontPackage forcedFont = FontLoader.Load(
            forced.CopyEmbeddedFont(),
            FontLoadOptions.CreateStrict());
        FontPackage reportedFont = FontLoader.Load(
            reported.CopyEmbeddedFont(),
            FontLoadOptions.CreateStrict());
        PdfFontResourceReport forcedReport =
            forced.GetCompatibilityReport();
        PdfFontResourceReport reportedReport =
            reported.GetCompatibilityReport();

        Assert.Multiple(() =>
        {
            Assert.That(face.GetEmbeddingRights()!.AllowsSubsetting(),
                Is.False);
            Assert.That(forcedReport.GetEmbeddingRightsOutcome(),
                Is.EqualTo(PdfFontEmbeddingRightsOutcome.FullFontForced));
            Assert.That(forced.GetEmbeddingDecision()
                .GetEffectiveEmbeddingMode(),
                Is.EqualTo(FontSubsetEmbeddingMode.FullFont));
            Assert.That(forcedFont.GetFace(0).GetMaxpTable()!
                .GetGlyphCount(), Is.EqualTo(4));
            Assert.Throws<FontEmbeddingException>(() =>
                rejectedBuilder.FinalizeFont());
            Assert.That(reportedReport.GetEmbeddingRightsOutcome(),
                Is.EqualTo(
                    PdfFontEmbeddingRightsOutcome.ConflictReported));
            Assert.That(reportedReport.HasPdfAEmbeddingConcerns(), Is.True);
            Assert.That(reportedReport.GetConcern(0).GetKind(),
                Is.EqualTo(PdfFontResourceConcernKind
                    .PdfAEmbeddingRightsConflict));
            Assert.That(reportedFont.GetFace(0).GetMaxpTable()!
                .GetGlyphCount(), Is.EqualTo(2));
        });
    }

    static PdfFontResource BuildResource(
        FontInstance instance,
        int glyphId,
        int[] unicodeSequence)
    {
        return BuildResource(
            instance,
            PdfFontOptions.Create(),
            glyphId,
            unicodeSequence);
    }

    static PdfFontResource BuildResource(
        FontInstance instance,
        PdfFontOptions options,
        int glyphId,
        int[] unicodeSequence)
    {
        PdfFontBuilder builder = new PdfFontBuilder(instance, options);
        builder.AddGlyph(glyphId, unicodeSequence);
        return builder.FinalizeFont();
    }

    static byte[] BuildCollection(byte[][] sfnts)
    {
        TtcFaceLayout[] layouts = new TtcFaceLayout[sfnts.Length];
        int directoryOffset = Align4(12 + sfnts.Length * 4);
        int faceIndex = 0;

        while (faceIndex < sfnts.Length)
        {
            int tableCount = ReadUInt16(sfnts[faceIndex], 4);
            layouts[faceIndex] = new TtcFaceLayout(
                sfnts[faceIndex],
                directoryOffset,
                tableCount);
            directoryOffset = Align4(
                directoryOffset + 12 + tableCount * 16);
            faceIndex += 1;
        }

        int tableOffset = directoryOffset;
        faceIndex = 0;
        while (faceIndex < layouts.Length)
        {
            TtcFaceLayout layout = layouts[faceIndex];
            int tableIndex = 0;
            while (tableIndex < layout.tableOffsets.Length)
            {
                int recordOffset = 12 + tableIndex * 16;
                int length = ReadInt32(layout.sfnt, recordOffset + 12);
                layout.tableOffsets[tableIndex] = tableOffset;
                tableOffset = Align4(tableOffset + length);
                tableIndex += 1;
            }

            faceIndex += 1;
        }

        byte[] collection = new byte[tableOffset];
        WriteInt32(collection, 0, 0x74746366);
        WriteInt32(collection, 4, 0x00010000);
        WriteInt32(collection, 8, layouts.Length);
        faceIndex = 0;
        while (faceIndex < layouts.Length)
        {
            TtcFaceLayout layout = layouts[faceIndex];
            WriteInt32(
                collection,
                12 + faceIndex * 4,
                layout.directoryOffset);
            Array.Copy(
                layout.sfnt,
                0,
                collection,
                layout.directoryOffset,
                12 + layout.tableOffsets.Length * 16);

            int tableIndex = 0;
            while (tableIndex < layout.tableOffsets.Length)
            {
                int sourceRecord = 12 + tableIndex * 16;
                int targetRecord = layout.directoryOffset + sourceRecord;
                int sourceOffset = ReadInt32(
                    layout.sfnt,
                    sourceRecord + 8);
                int length = ReadInt32(
                    layout.sfnt,
                    sourceRecord + 12);
                int targetOffset = layout.tableOffsets[tableIndex];
                WriteInt32(collection, targetRecord + 8, targetOffset);
                Array.Copy(
                    layout.sfnt,
                    sourceOffset,
                    collection,
                    targetOffset,
                    length);
                tableIndex += 1;
            }

            faceIndex += 1;
        }

        return collection;
    }

    static int Align4(int value)
    {
        return value + 3 & ~3;
    }

    static int ReadUInt16(byte[] bytes, int offset)
    {
        return BinaryPrimitives.ReadUInt16BigEndian(
            bytes.AsSpan(offset, 2));
    }

    static int ReadInt32(byte[] bytes, int offset)
    {
        return BinaryPrimitives.ReadInt32BigEndian(
            bytes.AsSpan(offset, 4));
    }

    static void WriteInt32(byte[] bytes, int offset, int value)
    {
        BinaryPrimitives.WriteInt32BigEndian(
            bytes.AsSpan(offset, 4),
            value);
    }

    sealed class TtcFaceLayout
    {
        internal byte[] sfnt;
        internal int directoryOffset;
        internal int[] tableOffsets;

        internal TtcFaceLayout(
            byte[] sfnt,
            int directoryOffset,
            int tableCount)
        {
            this.sfnt = sfnt;
            this.directoryOffset = directoryOffset;
            tableOffsets = new int[tableCount];
        }
    }
}
