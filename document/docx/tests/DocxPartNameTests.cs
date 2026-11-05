using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxPartNameTests
{
    [Test]
    public void Normalize_AddsLeadingSlashAndNormalizesSeparators()
    {
        Assert.That(global::docx.DocxPartName.Normalize("word/document.xml"), Is.EqualTo("/word/document.xml"));
        Assert.That(global::docx.DocxPartName.Normalize("\\word\\styles.xml"), Is.EqualTo("/word/styles.xml"));
        Assert.That(global::docx.DocxPartName.Normalize("//word///numbering.xml"), Is.EqualTo("/word/numbering.xml"));
    }

    [Test]
    public void Constructor_StoresNormalizedFullName()
    {
        global::docx.DocxPartName partName = new global::docx.DocxPartName("word/document.xml");

        Assert.That(partName.FullName, Is.EqualTo("/word/document.xml"));
        Assert.That(partName.ToZipEntryName(), Is.EqualTo("word/document.xml"));
    }

    [Test]
    public void ToZipEntryName_RemovesPackageLeadingSlash()
    {
        Assert.That(global::docx.DocxPartName.ToZipEntryName("/word/document.xml"), Is.EqualTo("word/document.xml"));
        Assert.That(global::docx.DocxPartName.ToZipEntryName("docProps/core.xml"), Is.EqualTo("docProps/core.xml"));
    }

    [Test]
    public void IsValid_AcceptsNormalizedAndZipStylePartNames()
    {
        Assert.That(global::docx.DocxPartName.IsValid("/[Content_Types].xml"), Is.True);
        Assert.That(global::docx.DocxPartName.IsValid("_rels/.rels"), Is.True);
        Assert.That(global::docx.DocxPartName.IsValid("/word/media/image1.png"), Is.True);
    }

    [Test]
    public void IsValid_RejectsMalformedPartNames()
    {
        Assert.That(global::docx.DocxPartName.IsValid(null!), Is.False);
        Assert.That(global::docx.DocxPartName.IsValid(""), Is.False);
        Assert.That(global::docx.DocxPartName.IsValid("/"), Is.False);
        Assert.That(global::docx.DocxPartName.IsValid("/word/"), Is.False);
        Assert.That(global::docx.DocxPartName.IsValid("/word/./document.xml"), Is.False);
        Assert.That(global::docx.DocxPartName.IsValid("/word/../document.xml"), Is.False);
        Assert.That(global::docx.DocxPartName.IsValid("/word/document.xml?x=1"), Is.False);
        Assert.That(global::docx.DocxPartName.IsValid("/word/document.xml#bookmark"), Is.False);
        Assert.That(global::docx.DocxPartName.IsValid("http://example.com/document.xml"), Is.False);
        Assert.That(global::docx.DocxPartName.IsValid("/word/document name.xml"), Is.False);
    }

    [Test]
    public void Normalize_ThrowsForNullAndMalformedPartNames()
    {
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxPartName.Normalize(null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxPartName.Normalize(""));
        Assert.Throws<ArgumentException>(() => global::docx.DocxPartName.Normalize("/"));
        Assert.Throws<ArgumentException>(() => global::docx.DocxPartName.Normalize("/word/../document.xml"));
    }

    [Test]
    public void GetValidationError_ReturnsEmptyStringForValidNames()
    {
        Assert.That(global::docx.DocxPartName.GetValidationError("word/document.xml"), Is.EqualTo(""));
        Assert.That(global::docx.DocxPartName.GetValidationError(null!), Is.EqualTo("Part name is required."));
        Assert.That(global::docx.DocxPartName.GetValidationError("/word/document.xml#fragment"), Is.EqualTo("Part name cannot contain query or fragment characters."));
    }
}
