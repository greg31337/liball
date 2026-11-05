using ttf;

namespace ttf.Tests;

internal class PdfOpenTypeFontArtifactTests
{
    [Test]
    public void BuildsTrueTypeOpenTypeProgramWithCidFontType2Mappings()
    {
        FontInstance instance = HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
        PdfFontOptions options = OpenTypeOptions(PdfVersion.Pdf1_6);
        PdfFontUsage usage = new PdfFontUsageAccumulator().GetOrAdd(
            instance,
            options);
        usage.AddGlyph(13, Array.Empty<int>());
        usage.AddGlyph(2, Array.Empty<int>());

        PdfOpenTypeFontArtifact artifact = PdfOpenTypeFontBuilder.Build(
            usage,
            PdfCidAllocator.Create(usage));
        byte[] fontFile3 = artifact.CopyFontFile3();
        FontPackage reopened = FontLoader.Load(
            fontFile3,
            FontLoadOptions.CreateStrict());

        Assert.Multiple(() =>
        {
            Assert.That(FontChecksum.VerifyFont(fontFile3), Is.True);
            Assert.That(ReadUInt32(fontFile3, 0), Is.EqualTo(0x00010000U));
            Assert.That(HasTable(fontFile3, FontTags.Glyf), Is.True);
            Assert.That(HasTable(fontFile3, FontTags.Cmap), Is.True);
            Assert.That(HasTable(fontFile3, FontTags.Cff), Is.False);
            Assert.That(reopened.GetFace(0).GetOutlineKind(),
                Is.EqualTo(FontOutlineKind.TrueType));
            Assert.That(artifact.GetFontFile3Subtype(),
                Is.EqualTo("OpenType"));
            Assert.That(artifact.GetFontKind(),
                Is.EqualTo(PdfFontKind.Type0OpenType));
            Assert.That(artifact.GetDescendantFontKind(),
                Is.EqualTo(PdfFontKind.Type0CidFontType2));
            Assert.That(artifact.GetSourceOutlineKind(),
                Is.EqualTo(FontOutlineKind.TrueType));
            Assert.That(artifact.GetEmbeddedOutlineKind(),
                Is.EqualTo(FontOutlineKind.TrueType));
            Assert.That(artifact.UsesCffCharset(), Is.False);
            Assert.That(artifact.UsesDirectCidsAsGlyphIds(), Is.False);
            Assert.That(artifact.CopyCffCharsetCids(), Is.Empty);
            Assert.That(artifact.CopyCidToSourceGlyphIds(),
                Is.EqualTo(new int[] { 0, 13, 2 }));
            Assert.That(artifact.CopyCidToSubsetGlyphIds(),
                Is.EqualTo(new int[] { 0, 2, 1 }));
        });
    }

    [Test]
    public void BuildsCidKeyedCffOpenTypeProgramWithRewrittenCharset()
    {
        FontInstance instance = CreateCidFace().CreateDefaultInstance();
        PdfFontOptions options = OpenTypeOptions(PdfVersion.Pdf1_7);
        PdfFontUsage usage = new PdfFontUsageAccumulator().GetOrAdd(
            instance,
            options);
        usage.AddGlyph(3, Array.Empty<int>());
        usage.AddGlyph(1, Array.Empty<int>());

        PdfOpenTypeFontArtifact artifact = PdfOpenTypeFontBuilder.Build(
            usage,
            PdfCidAllocator.Create(usage));
        byte[] fontFile3 = artifact.CopyFontFile3();
        byte[] cffBytes = CopyTable(fontFile3, FontTags.Cff);
        FontFace cffFace = FontInstanceCffTests.CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            cffBytes,
            3);
        CffCidMetadata? cid = cffFace.GetCffTable()!.GetCidMetadata();

        Assert.Multiple(() =>
        {
            Assert.That(FontChecksum.VerifyFont(fontFile3), Is.True);
            Assert.That(ReadUInt32(fontFile3, 0), Is.EqualTo(0x4F54544FU));
            Assert.That(HasTable(fontFile3, FontTags.Cmap), Is.True);
            Assert.That(HasTable(fontFile3, FontTags.Cff2), Is.False);
            Assert.That(artifact.GetDescendantFontKind(),
                Is.EqualTo(PdfFontKind.Type0CidFontType0));
            Assert.That(artifact.UsesCffCharset(), Is.True);
            Assert.That(artifact.UsesDirectCidsAsGlyphIds(), Is.False);
            Assert.That(artifact.CopyCidToSubsetGlyphIds(),
                Is.EqualTo(new int[] { 0, 2, 1 }));
            Assert.That(artifact.CopyCffCharsetCids(),
                Is.EqualTo(new int[] { 0, 2, 1 }));
            Assert.That(cid!.GetCharset().GetIdentifier(1), Is.EqualTo(2));
            Assert.That(cid.GetCharset().GetIdentifier(2), Is.EqualTo(1));
            Assert.That(cid.GetRegistry(), Is.EqualTo("Adobe"));
            Assert.That(cid.GetOrdering(), Is.EqualTo("Identity"));
        });
    }

    [Test]
    public void MaterializesCff2AsNameKeyedStaticCffWithDirectCids()
    {
        FontInstance instance = CreateValidCff2Face()
            .CreateDefaultInstance();
        PdfFontUsage usage = new PdfFontUsageAccumulator().GetOrAdd(
            instance,
            OpenTypeOptions(PdfVersion.Pdf2_0));
        usage.AddGlyph(1, Array.Empty<int>());

        PdfOpenTypeFontArtifact artifact = PdfOpenTypeFontBuilder.Build(
            usage,
            PdfCidAllocator.Create(usage));
        byte[] fontFile3 = artifact.CopyFontFile3();
        FontPackage reopened = FontLoader.Load(
            fontFile3,
            FontLoadOptions.CreateStrict());

        Assert.Multiple(() =>
        {
            Assert.That(FontChecksum.VerifyFont(fontFile3), Is.True);
            Assert.That(HasTable(fontFile3, FontTags.Cff), Is.True);
            Assert.That(HasTable(fontFile3, FontTags.Cff2), Is.False);
            Assert.That(reopened.GetFace(0).GetOutlineKind(),
                Is.EqualTo(FontOutlineKind.Cff1));
            Assert.That(artifact.GetSourceOutlineKind(),
                Is.EqualTo(FontOutlineKind.Cff2));
            Assert.That(artifact.GetEmbeddedOutlineKind(),
                Is.EqualTo(FontOutlineKind.Cff1));
            Assert.That(artifact.GetDescendantFontKind(),
                Is.EqualTo(PdfFontKind.Type0CidFontType0));
            Assert.That(artifact.UsesCffCharset(), Is.False);
            Assert.That(artifact.UsesDirectCidsAsGlyphIds(), Is.True);
            Assert.That(artifact.CopyCidToSubsetGlyphIds(),
                Is.EqualTo(new int[] { 0, 1 }));
        });
    }

    [Test]
    public void RejectsPdfVersionsAndFontKindsThatDoNotPermitOpenType()
    {
        FontInstance instance = HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
        PdfFontOptions oldVersion = OpenTypeOptions(PdfVersion.Pdf1_5);
        PdfFontOptions wrongKind = PdfFontOptions.Create();
        wrongKind.SetFontKind(PdfFontKind.Type0CidFontType2);
        FontInstance nameKeyed = FontInstanceCffTests.CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            FontSubsetCffBuilderTests.BuildNonCidCff(),
            4).CreateDefaultInstance();
        PdfFontUsage nameKeyedUsage = CreateUsage(
            nameKeyed,
            OpenTypeOptions(PdfVersion.Pdf1_6));
        nameKeyedUsage.AddGlyph(3, Array.Empty<int>());
        nameKeyedUsage.AddGlyph(1, Array.Empty<int>());

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(() =>
                Build(instance, oldVersion));
            Assert.Throws<InvalidOperationException>(() =>
                Build(instance, wrongKind));
            Assert.Throws<ArgumentNullException>(() =>
                PdfOpenTypeFontBuilder.Build(null!, PdfCidAllocator.Create(4)));
            Assert.Throws<ArgumentException>(() =>
                PdfOpenTypeFontBuilder.Build(
                    CreateUsage(instance, OpenTypeOptions(PdfVersion.Pdf1_6)),
                    PdfCidAllocator.Create(5)));
            Assert.Throws<InvalidOperationException>(() =>
                PdfOpenTypeFontBuilder.Build(
                    nameKeyedUsage,
                    PdfCidAllocator.Create(nameKeyedUsage)));
        });
    }

    [Test]
    public void ReturnsDefensiveProgramAndMappingCopies()
    {
        FontInstance instance = HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
        PdfOpenTypeFontArtifact artifact = Build(
            instance,
            OpenTypeOptions(PdfVersion.Pdf1_6));
        byte[] bytes = artifact.CopyFontFile3();
        int[] mapping = artifact.CopyCidToSubsetGlyphIds();
        bytes[0] = 99;
        mapping[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(artifact.CopyFontFile3()[0], Is.Not.EqualTo(99));
            Assert.That(artifact.CopyCidToSubsetGlyphIds()[0], Is.Zero);
        });
    }

    static PdfOpenTypeFontArtifact Build(
        FontInstance instance,
        PdfFontOptions options)
    {
        PdfFontUsage usage = CreateUsage(instance, options);
        return PdfOpenTypeFontBuilder.Build(
            usage,
            PdfCidAllocator.Create(usage));
    }

    static PdfFontUsage CreateUsage(
        FontInstance instance,
        PdfFontOptions options)
    {
        return new PdfFontUsageAccumulator().GetOrAdd(instance, options);
    }

    static PdfFontOptions OpenTypeOptions(PdfVersion version)
    {
        PdfFontOptions options = PdfFontOptions.Create();
        options.SetFontKind(PdfFontKind.Type0OpenType);
        options.SetVersion(version);
        return options;
    }

    static FontFace CreateCidFace()
    {
        return FontInstanceCffTests.CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            CffCharsetEncodingTests.BuildCidCff(false),
            4);
    }

    static FontFace CreateValidCff2Face()
    {
        FontFace source = FontInstanceCffTests
            .CreateReopenableVariableOutlineFace();
        SfntTableStore store = source.GetTableStore();
        List<SfntTableData> tables = new List<SfntTableData>();
        int index = 0;

        while (index < store.GetTableCount())
        {
            SfntTableData table = store.GetTable(index);

            if (table.GetTag() == FontTags.Os2)
            {
                tables.Add(CreateTable(FontTags.Os2, BuildOs2()));
            }
            else
            {
                tables.Add(table);
            }

            index += 1;
        }

        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(tables.ToArray(), Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        return new FontFace(
            0,
            0x4F54544FU,
            new SfntValidationResult(
                FontOutlineKind.Cff2,
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

    static byte[] BuildOs2()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteInt16(500)
            .WriteUInt16(400)
            .WriteUInt16(5)
            .WriteUInt16(0)
            .WriteZeros(52)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0xFFFF)
            .WriteInt16(800)
            .WriteInt16(-200)
            .WriteInt16(0)
            .WriteUInt16(800)
            .WriteUInt16(200)
            .CopyBytes();
    }

    static bool HasTable(byte[] bytes, int tag)
    {
        return FindTableOffset(bytes, tag) >= 0;
    }

    static byte[] CopyTable(byte[] bytes, int tag)
    {
        int recordOffset = FindTableOffset(bytes, tag);

        if (recordOffset < 0)
        {
            throw new InvalidOperationException("The table is absent.");
        }

        int offset = System.Convert.ToInt32(
            ReadUInt32(bytes, recordOffset + 8));
        int length = System.Convert.ToInt32(
            ReadUInt32(bytes, recordOffset + 12));
        byte[] table = new byte[length];
        Array.Copy(bytes, offset, table, 0, length);
        return table;
    }

    static int FindTableOffset(byte[] bytes, int tag)
    {
        int tableCount = ReadUInt16(bytes, 4);
        int index = 0;

        while (index < tableCount)
        {
            int recordOffset = 12 + index * 16;

            if (ReadUInt32(bytes, recordOffset) == (uint)tag)
            {
                return recordOffset;
            }

            index += 1;
        }

        return -1;
    }

    static int ReadUInt16(byte[] bytes, int offset)
    {
        return bytes[offset] << 8 | bytes[offset + 1];
    }

    static uint ReadUInt32(byte[] bytes, int offset)
    {
        return (uint)bytes[offset] << 24 |
            (uint)bytes[offset + 1] << 16 |
            (uint)bytes[offset + 2] << 8 |
            bytes[offset + 3];
    }
}
