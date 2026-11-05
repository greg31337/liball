using ttf;

namespace ttf.Tests;

internal class PdfCidFontType0ArtifactTests
{
    [Test]
    public void BuildsRawCidFontType0CWithDocumentCidsInCharset()
    {
        FontInstance instance = CreateNonIdentityCidFace()
            .CreateDefaultInstance();
        PdfFontUsage usage = new PdfFontUsageAccumulator().GetOrAdd(
            instance,
            PdfFontOptions.Create());
        usage.AddGlyph(3, new int[] { 0x43 });
        usage.AddGlyph(1, new int[] { 0x41 });

        PdfCidFontType0Artifact artifact = PdfCidFontType0Builder.Build(
            usage,
            PdfCidAllocator.Create(usage));
        byte[] fontFile3 = artifact.CopyFontFile3();
        FontFace embeddedFace = FontInstanceCffTests.CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            fontFile3,
            3);
        SfntCffTable? cff = embeddedFace.GetCffTable();
        CffCidMetadata? cid = cff!.GetCidMetadata();

        Assert.Multiple(() =>
        {
            Assert.That(fontFile3[0], Is.EqualTo(1));
            Assert.That(artifact.GetFontFile3Subtype(),
                Is.EqualTo("CIDFontType0C"));
            Assert.That(artifact.GetFontKind(),
                Is.EqualTo(PdfFontKind.Type0CidFontType0));
            Assert.That(artifact.GetBaseFontName(),
                Does.Match("^[A-Z]{6}\\+"));
            Assert.That(artifact.GetRegistry(), Is.EqualTo("Adobe"));
            Assert.That(artifact.GetOrdering(), Is.EqualTo("Identity"));
            Assert.That(artifact.GetSupplement(), Is.Zero);
            Assert.That(cid!.GetRegistry(), Is.EqualTo("Adobe"));
            Assert.That(cid.GetOrdering(), Is.EqualTo("Identity"));
            Assert.That(cid.GetSupplement(), Is.Zero);
            Assert.That(cid.GetCidCount(), Is.EqualTo(3));
            Assert.That(artifact.GetCffCidCount(), Is.EqualTo(3));
            Assert.That(artifact.CopyCidToSourceGlyphIds(),
                Is.EqualTo(new int[] { 0, 3, 1 }));
            Assert.That(artifact.CopyCidToSubsetGlyphIds(),
                Is.EqualTo(new int[] { 0, 2, 1 }));
            Assert.That(artifact.CopyCharsetCids(),
                Is.EqualTo(new int[] { 0, 2, 1 }));
            Assert.That(cid.GetCharset().GetIdentifier(0), Is.Zero);
            Assert.That(cid.GetCharset().GetIdentifier(1), Is.EqualTo(2));
            Assert.That(cid.GetCharset().GetIdentifier(2), Is.EqualTo(1));
            Assert.That(artifact.GetSourceGlyphId(1), Is.EqualTo(3));
            Assert.That(artifact.GetSubsetGlyphId(1), Is.EqualTo(2));
            Assert.That(artifact.GetCharsetCid(2), Is.EqualTo(1));
            Assert.That(artifact.FindCidForSourceGlyph(1), Is.EqualTo(2));
            Assert.That(artifact.FindCidForSubsetGlyph(1), Is.EqualTo(2));
            Assert.That(artifact.FindSubsetGlyphForCharsetCid(1),
                Is.EqualTo(2));
            Assert.That(cid.GetFontDictionarySelect()
                .GetFontDictionaryIndex(1), Is.Zero);
            Assert.That(cid.GetFontDictionarySelect()
                .GetFontDictionaryIndex(2), Is.EqualTo(1));
        });
    }

    [Test]
    public void FullFontAssignsUnusedCharsetCidsWithoutChangingDocumentCids()
    {
        FontInstance instance = CreateCidFace().CreateDefaultInstance();
        PdfFontOptions options = PdfFontOptions.Create();
        options.SetEmbeddingMode(PdfFontEmbeddingMode.FullFont);
        PdfFontUsage usage = new PdfFontUsageAccumulator().GetOrAdd(
            instance,
            options);
        PdfCidAllocator allocator = PdfCidAllocator.Create(usage);
        usage.AddGlyph(3, Array.Empty<int>());
        usage.AddGlyph(1, Array.Empty<int>());

        PdfCidFontType0Artifact artifact =
            PdfCidFontType0Builder.Build(usage, allocator);
        FontFace embeddedFace = FontInstanceCffTests.CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            artifact.CopyFontFile3(),
            4);
        CffCidMetadata? cid = embeddedFace.GetCffTable()!.GetCidMetadata();

        Assert.Multiple(() =>
        {
            Assert.That(artifact.CopyCidToSourceGlyphIds(),
                Is.EqualTo(new int[] { 0, 3, 1 }));
            Assert.That(artifact.CopyCidToSubsetGlyphIds(),
                Is.EqualTo(new int[] { 0, 3, 1 }));
            Assert.That(artifact.CopySubsetGlyphToCids(),
                Is.EqualTo(new int[] { 0, 2, -1, 1 }));
            Assert.That(artifact.CopyCharsetCids(),
                Is.EqualTo(new int[] { 0, 2, 3, 1 }));
            Assert.That(cid!.GetCharset().GetIdentifier(2), Is.EqualTo(3));
            Assert.That(cid.GetCidCount(), Is.EqualTo(4));
            Assert.That(artifact.GetCffCidCount(), Is.EqualTo(4));
            Assert.That(artifact.GetEmbeddingDecision()
                .GetEffectiveEmbeddingMode(),
                Is.EqualTo(FontSubsetEmbeddingMode.FullFont));
        });
    }

    [Test]
    public void ReturnsDefensiveProgramCharsetAndMappingCopies()
    {
        FontInstance instance = CreateCidFace().CreateDefaultInstance();
        PdfFontUsage usage = new PdfFontUsageAccumulator().GetOrAdd(
            instance,
            PdfFontOptions.Create());
        usage.AddGlyph(1, Array.Empty<int>());
        PdfCidFontType0Artifact artifact = PdfCidFontType0Builder.Build(
            usage,
            PdfCidAllocator.Create(usage));
        byte[] bytes = artifact.CopyFontFile3();
        int[] charset = artifact.CopyCharsetCids();
        int[] mapping = artifact.CopyCidToSubsetGlyphIds();
        bytes[0] = 99;
        charset[0] = 99;
        mapping[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(artifact.CopyFontFile3()[0], Is.EqualTo(1));
            Assert.That(artifact.CopyCharsetCids()[0], Is.Zero);
            Assert.That(artifact.CopyCidToSubsetGlyphIds()[0], Is.Zero);
        });
    }

    [Test]
    public void RejectsIncompatibleProgramsKindsVersionsAndAllocators()
    {
        FontInstance cff = CreateCidFace().CreateDefaultInstance();
        FontInstance trueType = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        PdfFontOptions wrongKind = PdfFontOptions.Create();
        wrongKind.SetFontKind(PdfFontKind.Type0OpenType);
        PdfFontOptions oldVersion = PdfFontOptions.Create();
        oldVersion.SetVersion(PdfVersion.Pdf1_1);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() =>
                PdfCidFontType0Builder.Build(null!, PdfCidAllocator.Create(4)));
            Assert.Throws<InvalidOperationException>(() => Build(
                trueType,
                PdfFontOptions.Create()));
            Assert.Throws<InvalidOperationException>(() => Build(
                cff,
                wrongKind));
            Assert.Throws<InvalidOperationException>(() => Build(
                cff,
                oldVersion));
            Assert.Throws<ArgumentException>(() =>
                PdfCidFontType0Builder.Build(
                    CreateUsage(cff, PdfFontOptions.Create()),
                    PdfCidAllocator.Create(5)));
        });
    }

    static PdfCidFontType0Artifact Build(
        FontInstance instance,
        PdfFontOptions options)
    {
        PdfFontUsage usage = CreateUsage(instance, options);
        return PdfCidFontType0Builder.Build(
            usage,
            PdfCidAllocator.Create(usage));
    }

    static PdfFontUsage CreateUsage(
        FontInstance instance,
        PdfFontOptions options)
    {
        return new PdfFontUsageAccumulator().GetOrAdd(instance, options);
    }

    static FontFace CreateCidFace()
    {
        return FontInstanceCffTests.CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            CffCharsetEncodingTests.BuildCidCff(false),
            4);
    }

    static FontFace CreateNonIdentityCidFace()
    {
        byte[] cff = CffCharsetEncodingTests.BuildCidCff(false);
        ReplaceAscii(cff, "Adobe", "Other");
        ReplaceAscii(cff, "Identity", "Ordering");
        return FontInstanceCffTests.CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            cff,
            4);
    }

    static void ReplaceAscii(byte[] bytes, string source, string replacement)
    {
        Assert.That(replacement.Length, Is.EqualTo(source.Length));
        byte[] sourceBytes = System.Text.Encoding.ASCII.GetBytes(source);
        byte[] replacementBytes =
            System.Text.Encoding.ASCII.GetBytes(replacement);
        int offset = FindBytes(bytes, sourceBytes);
        Assert.That(offset, Is.GreaterThanOrEqualTo(0));
        Array.Copy(replacementBytes, 0, bytes, offset, replacementBytes.Length);
    }

    static int FindBytes(byte[] bytes, byte[] value)
    {
        int offset = 0;

        while (offset <= bytes.Length - value.Length)
        {
            int index = 0;

            while (index < value.Length &&
                bytes[offset + index] == value[index])
            {
                index += 1;
            }

            if (index == value.Length)
            {
                return offset;
            }

            offset += 1;
        }

        return -1;
    }
}
