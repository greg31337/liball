using ttf;

namespace ttf.Tests;

internal class PdfFontOptionBehaviorTests
{
    [Test]
    public void PropagatesRetentionAndPrefixChoicesToSubsetting()
    {
        PdfFontOptions options = PdfFontOptions.Create();
        options.SetSubsetPrefixOverride("ABCDEF");
        options.SetHintPolicy(FontSubsetHintPolicy.Retain);
        options.SetLayoutPolicy(FontSubsetLayoutPolicy.Retain);
        options.SetVerticalPolicy(FontSubsetVerticalPolicy.Retain);
        options.SetColorPolicy(FontSubsetColorPolicy.Retain);
        options.SetMetadataPolicy(FontSubsetMetadataPolicy.Minimal);
        FontSubsetOptions subsetOptions =
            PdfFontOptionResolver.CreateSubsetOptions(options);
        PdfFontBuilder builder = new PdfFontBuilder(
            FontInstanceTests.CreateFace().CreateDefaultInstance(),
            options);
        builder.AddGlyph(1, new int[] { 0x41 });

        PdfFontResource resource = builder.FinalizeFont();
        byte[] embeddedFont = resource.CopyEmbeddedFont();

        Assert.Multiple(() =>
        {
            Assert.That(subsetOptions.GetHintPolicy(),
                Is.EqualTo(FontSubsetHintPolicy.Retain));
            Assert.That(subsetOptions.GetLayoutPolicy(),
                Is.EqualTo(FontSubsetLayoutPolicy.Retain));
            Assert.That(subsetOptions.GetVerticalPolicy(),
                Is.EqualTo(FontSubsetVerticalPolicy.Retain));
            Assert.That(subsetOptions.GetColorPolicy(),
                Is.EqualTo(FontSubsetColorPolicy.Retain));
            Assert.That(subsetOptions.GetMetadataPolicy(),
                Is.EqualTo(FontSubsetMetadataPolicy.Minimal));
            Assert.That(subsetOptions.GetSubsetPrefixOverride(),
                Is.EqualTo("ABCDEF"));
            Assert.That(resource.GetBaseFontName(),
                Does.StartWith("ABCDEF+"));
            Assert.That(HasTable(embeddedFont, FontTags.Fpgm), Is.True);
            Assert.That(HasTable(embeddedFont, FontTags.Prep), Is.True);
            Assert.That(HasTable(embeddedFont, FontTags.Cvt), Is.True);
        });
    }

    [Test]
    public void PreservedCidAllocationUsesSourceGlyphIdentifiers()
    {
        PdfFontOptions options = PdfFontOptions.Create();
        options.SetCidAllocationMode(
            PdfCidAllocationMode.PreserveSourceGlyphIdentifiers);
        options.SetAccessibilityProfile(PdfAccessibilityProfile.PdfUa1);
        PdfFontBuilder builder = new PdfFontBuilder(
            FontInstanceTests.CreateFace().CreateDefaultInstance(),
            options);
        builder.AddGlyph(3, new int[] { 0x43 });

        PdfFontResource resource = builder.FinalizeFont();
        PdfCidFontType2Artifact artifact =
            resource.GetCidFontType2Artifact()!;

        Assert.Multiple(() =>
        {
            Assert.That(builder.GetCharacterCodeCount(), Is.EqualTo(4));
            Assert.That(artifact.CopyCidToSourceGlyphIds(),
                Is.EqualTo(new int[] { 0, -1, -1, 3 }));
            Assert.That(artifact.CopySourceGlyphToCids(),
                Is.EqualTo(new int[] { 0, -1, -1, 3 }));
            Assert.That(artifact.CopyCidToSubsetGlyphIds(),
                Is.EqualTo(new int[] { 0, 0, 0, 3 }));
            Assert.That(artifact.CopyOldToNewGlyphIds(),
                Is.EqualTo(new int[] { 0, -1, -1, 3 }));
            Assert.That(resource.GetCompatibilityReport()
                .GetMissingUnicodeMappingCount(), Is.Zero);
        });
    }

    [Test]
    public void StaticAndSimpleRestrictionsRejectIncompatibleRequests()
    {
        PdfFontOptions staticOnly = PdfFontOptions.Create();
        staticOnly.SetStaticInstancePolicy(
            PdfStaticInstancePolicy.RequireStaticSource);
        PdfFontOptions noSimple = PdfFontOptions.Create();
        noSimple.SetFontKind(PdfFontKind.SimpleTrueType);
        noSimple.SetSimpleFontPolicy(PdfSimpleFontPolicy.Disallow);

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(() =>
                new PdfFontBuilder(
                    FontInstanceTests.CreateVariableFace(true)
                        .CreateDefaultInstance(),
                    staticOnly));
            Assert.Throws<InvalidOperationException>(() =>
                new PdfFontBuilder(
                    FontInstanceTests.CreateFace().CreateDefaultInstance(),
                    noSimple));
        });
    }

    [Test]
    public void PreservedCidAllocationSupportsCidKeyedCff()
    {
        FontInstance instance = FontInstanceCffTests.CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            CffCharsetEncodingTests.BuildCidCff(false),
            4).CreateDefaultInstance();
        PdfFontOptions options = PdfFontOptions.Create();
        options.SetCidAllocationMode(
            PdfCidAllocationMode.PreserveSourceGlyphIdentifiers);
        PdfFontBuilder builder = new PdfFontBuilder(instance, options);
        builder.AddGlyph(3, new int[] { 0x43 });

        PdfCidFontType0Artifact artifact = builder.FinalizeFont()
            .GetCidFontType0Artifact()!;

        Assert.Multiple(() =>
        {
            Assert.That(artifact.CopyCidToSourceGlyphIds(),
                Is.EqualTo(new int[] { 0, -1, -1, 3 }));
            Assert.That(artifact.CopySourceGlyphToCids(),
                Is.EqualTo(new int[] { 0, -1, -1, 3 }));
            Assert.That(artifact.CopyOldToNewGlyphIds(),
                Is.EqualTo(new int[] { 0, -1, -1, 3 }));
        });
    }

    [Test]
    public void CompatibilityPoliciesCanReportOrRejectFindings()
    {
        FontInstance instance = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        PdfFontOptions versionOptions = PdfFontOptions.Create();
        versionOptions.SetVersion(PdfVersion.Pdf1_1);
        versionOptions.SetVersionCompatibilityPolicy(
            PdfVersionCompatibilityPolicy.Report);
        PdfFontBuilder versionBuilder = new PdfFontBuilder(
            instance,
            versionOptions);
        versionBuilder.AddGlyph(1, new int[] { 0x41 });
        PdfFontResource versionResource = versionBuilder.FinalizeFont();

        PdfFontOptions profileOptions = PdfFontOptions.Create();
        profileOptions.SetAccessibilityProfile(
            PdfAccessibilityProfile.PdfUa1);
        profileOptions.SetProfileCompatibilityPolicy(
            PdfProfileCompatibilityPolicy.Reject);
        PdfFontBuilder profileBuilder = new PdfFontBuilder(
            instance,
            profileOptions);
        profileBuilder.AddGlyph(1, new int[0]);

        Assert.Multiple(() =>
        {
            Assert.That(versionResource.GetCompatibilityReport()
                .GetMinimumPdfVersion(), Is.EqualTo(PdfVersion.Pdf1_2));
            Assert.That(versionResource.GetCompatibilityReport()
                .GetTargetPdfVersion(), Is.EqualTo(PdfVersion.Pdf1_1));
            Assert.That(versionResource.GetCompatibilityReport()
                .GetConcern(0).GetKind(),
                Is.EqualTo(PdfFontResourceConcernKind.PdfVersionTooLow));
            Assert.Throws<InvalidOperationException>(() =>
                profileBuilder.FinalizeFont());
            Assert.That(profileBuilder.IsFinalized(), Is.False);
        });
    }

    static bool HasTable(byte[] bytes, int tag)
    {
        FontData data = FontData.Create(bytes);
        SfntDirectory directory = SfntDirectoryParser.Parse(
            data.GetSlice(0, data.GetLength()));
        int index = 0;

        while (index < directory.GetTableCount())
        {
            if (directory.GetTableRecord(index).GetTag() == tag)
            {
                return true;
            }
            index += 1;
        }

        return false;
    }
}
