using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PptxUriTests
{
    [Test]
    public void NormalizePartName_AddsLeadingSlashAndNormalizesSeparators()
    {
        Assert.That(PptxUriCore.NormalizePartName("ppt\\slides\\slide1.xml"), Is.EqualTo("/ppt/slides/slide1.xml"));
        Assert.That(PptxUriCore.NormalizePartName("/ppt/theme/theme1.xml"), Is.EqualTo("/ppt/theme/theme1.xml"));
        Assert.That(PptxUriCore.ToZipEntryName("/ppt/slides/slide1.xml"), Is.EqualTo("ppt/slides/slide1.xml"));
    }

    [Test]
    public void ValidatePartName_RejectsUnsafeNames()
    {
        Assert.That(PptxUriCore.IsValidPartName(""), Is.False);
        Assert.That(PptxUriCore.IsValidPartName("/"), Is.False);
        Assert.That(PptxUriCore.IsValidPartName("/ppt//slide.xml"), Is.False);
        Assert.That(PptxUriCore.IsValidPartName("/ppt/../slide.xml"), Is.False);
        Assert.That(PptxUriCore.IsValidPartName("/ppt/%2e%2e/slide.xml"), Is.False);
        Assert.That(PptxUriCore.IsValidPartName("/ppt/media/image%2F1.png"), Is.False);
        Assert.That(PptxUriCore.IsValidPartName("file:///ppt/presentation.xml"), Is.False);
        Assert.That(PptxUriCore.IsValidPartName("/ppt/slides/slide 1.xml"), Is.False);
    }

    [Test]
    public void NormalizePartName_ThrowsWithDiagnosticsForInvalidName()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => PptxUriCore.NormalizePartName("/ppt/../slide.xml"))!;

        Assert.That(exception.Message, Does.Contain("URI010"));
        Assert.That(exception.Message, Does.Contain("dot segments"));
    }

    [Test]
    public void IsExternalTarget_DetectsSchemesAndNetworkReferences()
    {
        Assert.That(PptxUriCore.IsExternalTarget("https://example.com/deck.pptx"), Is.True);
        Assert.That(PptxUriCore.IsExternalTarget("mailto:user@example.com"), Is.True);
        Assert.That(PptxUriCore.IsExternalTarget("//cdn.example.com/image.png"), Is.True);
        Assert.That(PptxUriCore.IsExternalTarget("../media/image1.png"), Is.False);
        Assert.That(PptxUriCore.IsExternalTarget("slides/slide1.xml"), Is.False);
    }

    [Test]
    public void NormalizeInternalRelationshipTarget_ResolvesRelativeTargetsAgainstSourcePart()
    {
        Assert.That(
            PptxUriCore.NormalizeInternalRelationshipTarget("/ppt/presentation.xml", "slides/slide1.xml"),
            Is.EqualTo("/ppt/slides/slide1.xml"));

        Assert.That(
            PptxUriCore.NormalizeInternalRelationshipTarget("/ppt/slides/slide1.xml", "../slideLayouts/slideLayout1.xml"),
            Is.EqualTo("/ppt/slideLayouts/slideLayout1.xml"));

        Assert.That(
            PptxUriCore.NormalizeInternalRelationshipTarget("/ppt/slides/slide1.xml", "/ppt/theme/theme1.xml"),
            Is.EqualTo("/ppt/theme/theme1.xml"));
    }

    [Test]
    public void NormalizeInternalRelationshipTarget_PreservesFragments()
    {
        Assert.That(
            PptxUriCore.NormalizeInternalRelationshipTarget("/ppt/presentation.xml", "slides/slide1.xml#shape-1"),
            Is.EqualTo("/ppt/slides/slide1.xml#shape-1"));

        Assert.That(
            PptxUriCore.NormalizeInternalRelationshipTarget("/ppt/presentation.xml", "#presentation"),
            Is.EqualTo("/ppt/presentation.xml#presentation"));
    }

    [Test]
    public void ValidateInternalRelationshipTarget_RejectsExternalAndEscapingTargets()
    {
        Assert.That(
            PptxUriCore.IsValidInternalRelationshipTarget("/ppt/presentation.xml", "https://example.com/image.png"),
            Is.False);

        Assert.That(
            PptxUriCore.IsValidInternalRelationshipTarget("/ppt/presentation.xml", "../../evil.xml"),
            Is.False);

        Assert.That(
            PptxUriCore.IsValidInternalRelationshipTarget("/ppt/presentation.xml", "slides/slide%2f1.xml"),
            Is.False);

        Assert.That(
            PptxUriCore.IsValidInternalRelationshipTarget("/ppt/presentation.xml", "slides//slide1.xml"),
            Is.False);

        Assert.That(
            PptxUriCore.IsValidInternalRelationshipTarget("/ppt/presentation.xml", "slides/"),
            Is.False);
    }
}
