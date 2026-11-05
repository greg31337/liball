using ttf;

namespace ttf.Tests;

internal class PdfFontEncodingArtifactTests
{
    [Test]
    public void BuildsHorizontalAndVerticalIdentityMetadata()
    {
        PdfFontOptions horizontal = PdfFontOptions.Create();
        PdfFontOptions vertical = PdfFontOptions.Create();
        vertical.SetWritingMode(PdfWritingMode.Vertical);

        PdfType0EncodingMetadata horizontalMetadata =
            PdfFontEncodingBuilder.BuildType0(horizontal);
        PdfType0EncodingMetadata verticalMetadata =
            PdfFontEncodingBuilder.BuildType0(vertical);

        Assert.Multiple(() =>
        {
            Assert.That(horizontalMetadata.GetEncodingName(),
                Is.EqualTo("Identity-H"));
            Assert.That(horizontalMetadata.GetWritingMode(),
                Is.EqualTo(PdfWritingMode.Horizontal));
            Assert.That(horizontalMetadata.GetWMode(), Is.Zero);
            Assert.That(verticalMetadata.GetEncodingName(),
                Is.EqualTo("Identity-V"));
            Assert.That(verticalMetadata.GetWritingMode(),
                Is.EqualTo(PdfWritingMode.Vertical));
            Assert.That(verticalMetadata.GetWMode(), Is.EqualTo(1));
            Assert.That(verticalMetadata.GetCodeByteCount(), Is.EqualTo(2));
            Assert.That(verticalMetadata.GetCodeSpaceMinimum(), Is.Zero);
            Assert.That(verticalMetadata.GetCodeSpaceMaximum(),
                Is.EqualTo(65535));
        });
    }

    [Test]
    public void BuildsSimpleDifferencesInDocumentCodeOrder()
    {
        FontInstance instance = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        PdfFontOptions options = CreateSimpleOptions();
        PdfFontUsage usage = new PdfFontUsageAccumulator().GetOrAdd(
            instance,
            options);
        usage.AddGlyph(3, new int[] { 0x43 });
        usage.AddGlyph(1, new int[] { 0x41 });
        PdfCidAllocator allocator = PdfCidAllocator.Create(usage);

        PdfSimpleFontEncodingArtifact artifact =
            PdfFontEncodingBuilder.BuildSimple(usage, allocator);

        Assert.Multiple(() =>
        {
            Assert.That(artifact.GetBaseEncodingName(),
                Is.EqualTo("WinAnsiEncoding"));
            Assert.That(artifact.GetCodeByteCount(), Is.EqualTo(1));
            Assert.That(artifact.GetDifferenceCount(), Is.EqualTo(3));
            AssertDifference(artifact.GetDifference(0), 0, 0, ".notdef");
            AssertDifference(artifact.GetDifference(1), 1, 3, "gid0003");
            AssertDifference(artifact.GetDifference(2), 2, 1, "gid0001");
        });
    }

    [Test]
    public void IncludesDirectAndNewUsageAllocationsAndReturnsArrayCopies()
    {
        FontInstance instance = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        PdfFontUsage usage = new PdfFontUsageAccumulator().GetOrAdd(
            instance,
            CreateSimpleOptions());
        PdfCidAllocator allocator = PdfCidAllocator.Create(usage);
        allocator.GetOrAddCid(2);
        usage.AddGlyph(1, System.Array.Empty<int>());

        PdfSimpleFontEncodingArtifact artifact =
            PdfFontEncodingBuilder.BuildSimple(usage, allocator);
        PdfSimpleFontEncodingDifference?[] differences =
            artifact.GetDifferences();
        differences[0] = null;

        Assert.Multiple(() =>
        {
            Assert.That(artifact.GetDifferenceCount(), Is.EqualTo(3));
            AssertDifference(artifact.GetDifference(1), 1, 2, "gid0002");
            AssertDifference(artifact.GetDifference(2), 2, 1, "gid0001");
            Assert.That(artifact.GetDifferences()[0], Is.Not.Null);
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                artifact.GetDifference(3));
        });
    }

    [Test]
    public void RejectsIncompatibleIdentityAndSimpleSelections()
    {
        PdfFontOptions simple = CreateSimpleOptions();
        PdfFontOptions oldType0 = PdfFontOptions.Create();
        oldType0.SetVersion(PdfVersion.Pdf1_1);
        FontInstance trueType = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        PdfFontOptions automatic = PdfFontOptions.Create();
        PdfFontOptions vertical = CreateSimpleOptions();
        vertical.SetWritingMode(PdfWritingMode.Vertical);
        PdfFontOptions oldSimple = CreateSimpleOptions();
        oldSimple.SetVersion(PdfVersion.Pdf1_0);
        FontFace cffFace = FontInstanceCffTests.CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            CffCharsetEncodingTests.BuildCidCff(false),
            4);

        Assert.Multiple(() =>
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                PdfFontEncodingBuilder.BuildType0(null!));
            Assert.Throws<System.InvalidOperationException>(() =>
                PdfFontEncodingBuilder.BuildType0(simple));
            Assert.Throws<System.InvalidOperationException>(() =>
                PdfFontEncodingBuilder.BuildType0(oldType0));
            Assert.Throws<System.ArgumentNullException>(() =>
                PdfFontEncodingBuilder.BuildSimple(
                    null!,
                    PdfCidAllocator.Create(4)));
            Assert.Throws<System.InvalidOperationException>(() =>
                BuildSimple(trueType, automatic));
            Assert.Throws<System.InvalidOperationException>(() =>
                BuildSimple(trueType, vertical));
            Assert.Throws<System.InvalidOperationException>(() =>
                BuildSimple(trueType, oldSimple));
            Assert.Throws<System.InvalidOperationException>(() =>
                BuildSimple(cffFace.CreateDefaultInstance(), simple));
            Assert.Throws<System.ArgumentException>(() =>
                PdfFontEncodingBuilder.BuildSimple(
                    CreateUsage(trueType, simple),
                    PdfCidAllocator.Create(5)));
        });
    }

    static PdfSimpleFontEncodingArtifact BuildSimple(
        FontInstance instance,
        PdfFontOptions options)
    {
        PdfFontUsage usage = CreateUsage(instance, options);
        return PdfFontEncodingBuilder.BuildSimple(
            usage,
            PdfCidAllocator.Create(usage));
    }

    static PdfFontUsage CreateUsage(
        FontInstance instance,
        PdfFontOptions options)
    {
        return new PdfFontUsageAccumulator().GetOrAdd(instance, options);
    }

    static PdfFontOptions CreateSimpleOptions()
    {
        PdfFontOptions options = PdfFontOptions.Create();
        options.SetFontKind(PdfFontKind.SimpleTrueType);
        return options;
    }

    static void AssertDifference(
        PdfSimpleFontEncodingDifference difference,
        int code,
        int glyphId,
        string glyphName)
    {
        Assert.Multiple(() =>
        {
            Assert.That(difference.GetCharacterCode(), Is.EqualTo(code));
            Assert.That(difference.GetSourceGlyphId(), Is.EqualTo(glyphId));
            Assert.That(difference.GetGlyphName(), Is.EqualTo(glyphName));
        });
    }
}
