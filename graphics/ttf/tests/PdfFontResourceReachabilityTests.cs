using ttf;

namespace ttf.Tests;

internal class PdfFontResourceReachabilityTests
{
    [Test]
    public void Type2ResourceDirectlyExposesEveryCompositeFontArtifact()
    {
        PdfFontOptions options = PdfFontOptions.Create();
        options.SetWritingMode(PdfWritingMode.Vertical);
        options.SetArchivalProfile(PdfArchivalProfile.PdfA1);
        PdfFontBuilder builder = new PdfFontBuilder(
            FontInstanceTests.CreateFace().CreateDefaultInstance(),
            options);
        builder.AddGlyph(3, new int[] { 0x43 });
        builder.AddGlyph(1, new int[] { 0x41 });

        PdfFontResource resource = builder.FinalizeFont();
        PdfCidFontType2Artifact artifact =
            resource.GetCidFontType2Artifact()!;
        int[] codeToCid = resource.CopyCharacterCodeToCids();
        int[] cidToCode = resource.CopyCidToCharacterCodes();
        codeToCid[0] = 99;
        cidToCode[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(resource.GetType0EncodingMetadata()
                .GetEncodingName(), Is.EqualTo("Identity-V"));
            Assert.That(resource.GetType0EncodingMetadata().GetWMode(),
                Is.EqualTo(1));
            Assert.That(resource.GetResourceNames(),
                Is.SameAs(artifact.GetResourceNames()));
            Assert.That(resource.GetCidMapping(),
                Is.SameAs(artifact.GetCidMapping()));
            Assert.That(resource.GetCidSystemInfo().GetRegistry(),
                Is.EqualTo("Adobe"));
            Assert.That(resource.GetCidSystemInfo().GetOrdering(),
                Is.EqualTo("Identity"));
            Assert.That(resource.HasCidToGidMap(), Is.True);
            Assert.That(resource.UsesIdentityCidToGidMap(), Is.False);
            Assert.That(resource.GetCidToGidMapName(), Is.Empty);
            Assert.That(resource.CopyCidToGidMap(), Is.Not.Empty);
            Assert.That(resource.HasCidSet(), Is.True);
            Assert.That(resource.CopyCidSet(), Is.Not.Empty);
            Assert.That(resource.GetWidths(),
                Is.SameAs(artifact.GetWidths()));
            Assert.That(resource.HasVerticalWidths(), Is.True);
            Assert.That(resource.GetVerticalWidths(),
                Is.SameAs(artifact.GetVerticalWidths()));
            Assert.That(resource.GetDescriptorFlags(),
                Is.SameAs(artifact.GetDescriptorFlags()));
            Assert.That(resource.GetDescriptorMetrics(),
                Is.SameAs(artifact.GetDescriptorMetrics()));
            Assert.That(resource.GetToUnicodeMappings(),
                Is.SameAs(artifact.GetToUnicodeMappings()));
            Assert.That(resource.GetSourceGlyphId(1), Is.EqualTo(3));
            Assert.That(resource.FindCidForSourceGlyph(3), Is.EqualTo(1));
            Assert.That(resource.GetSubsetGlyphId(1), Is.EqualTo(2));
            Assert.That(resource.FindCidForSubsetGlyph(2), Is.EqualTo(1));
            Assert.That(resource.CopyCidToSourceGlyphIds(),
                Is.EqualTo(artifact.CopyCidToSourceGlyphIds()));
            Assert.That(resource.CopySourceGlyphToCids(),
                Is.EqualTo(artifact.CopySourceGlyphToCids()));
            Assert.That(resource.CopyCidToSubsetGlyphIds(),
                Is.EqualTo(artifact.CopyCidToSubsetGlyphIds()));
            Assert.That(resource.CopySubsetGlyphToCids(),
                Is.EqualTo(artifact.CopySubsetGlyphToCids()));
            Assert.That(resource.CopyOldToNewGlyphIds(),
                Is.EqualTo(artifact.CopyOldToNewGlyphIds()));
            Assert.That(resource.CopyNewToOldGlyphIds(),
                Is.EqualTo(artifact.CopyNewToOldGlyphIds()));
            Assert.That(resource.GetCidForCharacterCode(1), Is.EqualTo(1));
            Assert.That(resource.FindCharacterCodeForCid(2), Is.EqualTo(2));
            Assert.That(resource.CopyCharacterCodeToCids(),
                Is.EqualTo(new int[] { 0, 1, 2 }));
            Assert.That(resource.CopyCidToCharacterCodes(),
                Is.EqualTo(new int[] { 0, 1, 2 }));
        });
    }

    [Test]
    public void Type0ResourceExposesCharsetMappingsWithoutCidToGidEntry()
    {
        FontInstance instance = FontInstanceCffTests.CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            CffCharsetEncodingTests.BuildCidCff(false),
            4).CreateDefaultInstance();
        PdfFontOptions options = PdfFontOptions.Create();
        options.SetArchivalProfile(PdfArchivalProfile.PdfA1);
        PdfFontBuilder builder = new PdfFontBuilder(instance, options);
        builder.AddGlyph(3, new int[] { 0x43 });

        PdfFontResource resource = builder.FinalizeFont();
        PdfCidFontType0Artifact artifact =
            resource.GetCidFontType0Artifact()!;

        Assert.Multiple(() =>
        {
            Assert.That(resource.GetType0EncodingMetadata()
                .GetEncodingName(), Is.EqualTo("Identity-H"));
            Assert.That(resource.GetResourceNames(),
                Is.SameAs(artifact.GetResourceNames()));
            Assert.That(resource.GetCidMapping(),
                Is.SameAs(artifact.GetCidMapping()));
            Assert.That(resource.HasCidToGidMap(), Is.False);
            Assert.That(resource.UsesIdentityCidToGidMap(), Is.False);
            Assert.That(resource.GetCidToGidMapName(), Is.Empty);
            Assert.That(resource.CopyCidToGidMap(), Is.Empty);
            Assert.That(resource.HasCidSet(), Is.True);
            Assert.That(resource.CopyCidSet(), Is.Not.Empty);
            Assert.That(resource.GetWidths(),
                Is.SameAs(artifact.GetWidths()));
            Assert.That(resource.GetDescriptorMetrics(),
                Is.SameAs(artifact.GetDescriptorMetrics()));
            Assert.That(resource.CopyCidToSourceGlyphIds(),
                Is.EqualTo(new int[] { 0, 3 }));
            Assert.That(resource.CopyOldToNewGlyphIds(),
                Is.EqualTo(artifact.CopyOldToNewGlyphIds()));
            Assert.That(resource.HasCffCharsetMappings(), Is.True);
            Assert.That(resource.GetCffCharsetCid(1),
                Is.EqualTo(artifact.GetCharsetCid(1)));
            Assert.That(resource.FindSubsetGlyphForCffCharsetCid(
                artifact.GetCharsetCid(1)), Is.EqualTo(1));
            Assert.That(resource.CopyCffCharsetCids(),
                Is.EqualTo(artifact.CopyCharsetCids()));
        });
    }

    [Test]
    public void OpenTypeAndSimpleResourcesExposeOnlyApplicableMetadata()
    {
        FontInstance instance = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        PdfFontOptions openTypeOptions = PdfFontOptions.Create();
        openTypeOptions.SetFontKind(PdfFontKind.Type0OpenType);
        PdfFontResource openType = Build(instance, openTypeOptions, 3);
        PdfOpenTypeFontArtifact openTypeArtifact =
            openType.GetOpenTypeArtifact()!;

        PdfFontOptions simpleOptions = PdfFontOptions.Create();
        simpleOptions.SetFontKind(PdfFontKind.SimpleTrueType);
        PdfFontResource simple = Build(instance, simpleOptions, 3);

        Assert.Multiple(() =>
        {
            Assert.That(openType.GetResourceNames(),
                Is.SameAs(openTypeArtifact.GetResourceNames()));
            Assert.That(openType.FindCidForSourceGlyph(3), Is.EqualTo(1));
            Assert.That(openType.FindCidForSubsetGlyph(1), Is.EqualTo(1));
            Assert.That(openType.GetWidths(),
                Is.SameAs(openTypeArtifact.GetWidths()));
            Assert.That(openType.HasCffCharsetMappings(), Is.False);
            Assert.That(simple.GetResourceNames().GetBaseFontName(),
                Is.EqualTo(simple.GetBaseFontName()));
            Assert.That(simple.GetWidths().GetCidCount(), Is.EqualTo(2));
            Assert.That(simple.GetDescriptorMetrics().GetFontFileKind(),
                Is.EqualTo(PdfEmbeddedFontFileKind.FontFile2));
            Assert.That(simple.CopyCidToSourceGlyphIds(),
                Is.EqualTo(new int[] { 0, 3 }));
            Assert.That(simple.CopyCharacterCodeToCids(),
                Is.EqualTo(new int[] { 0, 1 }));
            Assert.That(simple.HasVerticalWidths(), Is.False);
            Assert.That(simple.GetVerticalWidths(), Is.Null);
            Assert.That(simple.HasCffCharsetMappings(), Is.False);
            Assert.Throws<InvalidOperationException>(() =>
                simple.GetType0EncodingMetadata());
            Assert.Throws<InvalidOperationException>(() =>
                simple.GetCidMapping());
            Assert.Throws<InvalidOperationException>(() =>
                simple.GetCidSystemInfo());
            Assert.Throws<InvalidOperationException>(() =>
                simple.CopyCffCharsetCids());
        });
    }

    static PdfFontResource Build(
        FontInstance instance,
        PdfFontOptions options,
        int glyphId)
    {
        PdfFontBuilder builder = new PdfFontBuilder(instance, options);
        builder.AddGlyph(glyphId, new int[] { 0x43 });
        return builder.FinalizeFont();
    }
}
