using System.Text;
using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxContentTypesTests
{
    [Test]
    public void Parse_ReadsDefaultContentTypes()
    {
        global::docx.DocxContentTypes contentTypes = global::docx.DocxContentTypes.Parse(CreateContentTypesXml());

        Assert.That(contentTypes.DefaultCount(), Is.EqualTo(3));
        Assert.That(contentTypes.Defaults[0].Extension, Is.EqualTo("rels"));
        Assert.That(contentTypes.Defaults[0].ContentType, Is.EqualTo(global::docx.DocxConstants.ContentTypeRelationships));
        Assert.That(contentTypes.GetDefaultContentType("xml"), Is.EqualTo(global::docx.DocxConstants.ContentTypeXml));
        Assert.That(contentTypes.GetDefaultContentType(".png"), Is.EqualTo(global::docx.DocxConstants.ContentTypePng));
    }

    [Test]
    public void Parse_ReadsOverrideContentTypes()
    {
        global::docx.DocxContentTypes contentTypes = global::docx.DocxContentTypes.Parse(CreateContentTypesXml());

        Assert.That(contentTypes.OverrideCount(), Is.EqualTo(2));
        Assert.That(contentTypes.Overrides[0].PartName, Is.EqualTo("/word/document.xml"));
        Assert.That(contentTypes.Overrides[0].ContentType, Is.EqualTo(global::docx.DocxConstants.ContentTypeMainDocument));
        Assert.That(contentTypes.GetOverrideContentType("word/styles.xml"), Is.EqualTo(global::docx.DocxConstants.ContentTypeStyles));
        Assert.That(contentTypes.GetOverrideContentType("word/missing.xml"), Is.EqualTo(""));
    }

    [Test]
    public void Parse_StreamReadsDefaultsAndLeavesStreamOpen()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(CreateContentTypesXml());
        MemoryStream stream = new MemoryStream(bytes);

        global::docx.DocxContentTypes contentTypes = global::docx.DocxContentTypes.Parse(stream);

        Assert.That(contentTypes.GetDefaultContentType("rels"), Is.EqualTo(global::docx.DocxConstants.ContentTypeRelationships));
        Assert.That(stream.CanRead, Is.True);
    }

    [Test]
    public void FindDefault_MatchesExtensionsCaseInsensitively()
    {
        global::docx.DocxContentTypes contentTypes = new global::docx.DocxContentTypes();

        contentTypes.AddDefault(".XML", global::docx.DocxConstants.ContentTypeXml);

        global::docx.DocxContentTypeDefault? value = contentTypes.FindDefault("xml");

        Assert.That(value, Is.Not.Null);
        Assert.That(value!.ContentType, Is.EqualTo(global::docx.DocxConstants.ContentTypeXml));
    }

    [Test]
    public void GetContentType_PrefersOverrideThenFallsBackToDefaultExtension()
    {
        global::docx.DocxContentTypes contentTypes = global::docx.DocxContentTypes.Parse(CreateContentTypesXml());

        Assert.That(contentTypes.GetContentType("word/document.xml"), Is.EqualTo(global::docx.DocxConstants.ContentTypeMainDocument));
        Assert.That(contentTypes.GetContentType("/word/styles.xml"), Is.EqualTo(global::docx.DocxConstants.ContentTypeStyles));
        Assert.That(contentTypes.GetContentType("word/media/IMAGE.PNG"), Is.EqualTo(global::docx.DocxConstants.ContentTypePng));
        Assert.That(contentTypes.GetContentType("custom/item1.dat"), Is.EqualTo(""));
        Assert.That(contentTypes.GetContentType("custom/noextension"), Is.EqualTo(""));
    }

    [Test]
    public void WriteXml_RoundTripsDefaultsAndOverrides()
    {
        global::docx.DocxContentTypes contentTypes = global::docx.DocxContentTypes.Parse(CreateContentTypesXml());

        string xml = contentTypes.WriteXml();
        global::docx.DocxContentTypes roundTripped = global::docx.DocxContentTypes.Parse(xml);

        Assert.That(roundTripped.DefaultCount(), Is.EqualTo(contentTypes.DefaultCount()));
        Assert.That(roundTripped.OverrideCount(), Is.EqualTo(contentTypes.OverrideCount()));
        Assert.That(roundTripped.GetContentType("/word/document.xml"), Is.EqualTo(global::docx.DocxConstants.ContentTypeMainDocument));
        Assert.That(roundTripped.GetContentType("/word/media/image1.png"), Is.EqualTo(global::docx.DocxConstants.ContentTypePng));
    }

    [Test]
    public void WriteXml_DeterministicModeSortsDefaultsAndOverrides()
    {
        global::docx.DocxContentTypes contentTypes = new global::docx.DocxContentTypes();

        contentTypes.AddDefault("xml", global::docx.DocxConstants.ContentTypeXml);
        contentTypes.AddDefault("png", global::docx.DocxConstants.ContentTypePng);
        contentTypes.AddDefault("rels", global::docx.DocxConstants.ContentTypeRelationships);
        contentTypes.AddOverride("/word/styles.xml", global::docx.DocxConstants.ContentTypeStyles);
        contentTypes.AddOverride("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument);

        string xml = contentTypes.WriteDeterministicXml(true);

        Assert.That(xml.IndexOf("Extension=\"png\"", StringComparison.Ordinal), Is.LessThan(xml.IndexOf("Extension=\"rels\"", StringComparison.Ordinal)));
        Assert.That(xml.IndexOf("Extension=\"rels\"", StringComparison.Ordinal), Is.LessThan(xml.IndexOf("Extension=\"xml\"", StringComparison.Ordinal)));
        Assert.That(xml.IndexOf("PartName=\"/word/document.xml\"", StringComparison.Ordinal), Is.LessThan(xml.IndexOf("PartName=\"/word/styles.xml\"", StringComparison.Ordinal)));
    }

    [Test]
    public void GetExtensionFromPartName_ReturnsExtensionAfterLastPathSeparator()
    {
        Assert.That(global::docx.DocxContentTypes.GetExtensionFromPartName("/word/document.xml"), Is.EqualTo("xml"));
        Assert.That(global::docx.DocxContentTypes.GetExtensionFromPartName("word/media/image.final.png"), Is.EqualTo("png"));
        Assert.That(global::docx.DocxContentTypes.GetExtensionFromPartName("word/.hidden"), Is.EqualTo("hidden"));
        Assert.That(global::docx.DocxContentTypes.GetExtensionFromPartName("word/noextension"), Is.EqualTo(""));
    }

    [Test]
    public void Parse_RejectsDefaultElementsMissingRequiredAttributes()
    {
        Assert.Throws<InvalidDataException>(() => global::docx.DocxContentTypes.Parse(
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default ContentType=\"application/xml\" /></Types>"));

        Assert.Throws<InvalidDataException>(() => global::docx.DocxContentTypes.Parse(
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"xml\" /></Types>"));
    }

    [Test]
    public void Parse_RejectsOverrideElementsMissingRequiredAttributes()
    {
        Assert.Throws<InvalidDataException>(() => global::docx.DocxContentTypes.Parse(
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Override ContentType=\"application/xml\" /></Types>"));

        Assert.Throws<InvalidDataException>(() => global::docx.DocxContentTypes.Parse(
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Override PartName=\"/word/document.xml\" /></Types>"));
    }

    [Test]
    public void AddOverride_NormalizesPartNamesAndValidatesContentType()
    {
        global::docx.DocxContentTypes contentTypes = new global::docx.DocxContentTypes();

        contentTypes.AddOverride("word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument);

        global::docx.DocxContentTypeOverride? value = contentTypes.FindOverride("/word/document.xml");

        Assert.That(value, Is.Not.Null);
        Assert.That(value!.PartName, Is.EqualTo("/word/document.xml"));
        Assert.That(value.ContentType, Is.EqualTo(global::docx.DocxConstants.ContentTypeMainDocument));

        Assert.Throws<ArgumentNullException>(() => contentTypes.AddOverride(null!, global::docx.DocxConstants.ContentTypeMainDocument));
        Assert.Throws<ArgumentNullException>(() => contentTypes.AddOverride("word/document.xml", null!));
        Assert.Throws<ArgumentException>(() => contentTypes.AddOverride("word/document.xml", ""));
        Assert.Throws<ArgumentException>(() => contentTypes.AddOverride("/word/../document.xml", global::docx.DocxConstants.ContentTypeMainDocument));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxContentTypes.CompareDefaults(null!, new global::docx.DocxContentTypeDefault("xml", global::docx.DocxConstants.ContentTypeXml)));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxContentTypes.CompareDefaults(new global::docx.DocxContentTypeDefault("xml", global::docx.DocxConstants.ContentTypeXml), null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxContentTypes.CompareOverrides(null!, new global::docx.DocxContentTypeOverride("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument)));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxContentTypes.CompareOverrides(new global::docx.DocxContentTypeOverride("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument), null!));
    }

    [Test]
    public void SetDefault_AddsAndUpdatesDefaultContentTypes()
    {
        global::docx.DocxContentTypes contentTypes = new global::docx.DocxContentTypes();

        contentTypes.SetDefault(".xml", global::docx.DocxConstants.ContentTypeXml);
        contentTypes.SetDefault("XML", "application/custom+xml");

        Assert.That(contentTypes.DefaultCount(), Is.EqualTo(1));
        Assert.That(contentTypes.GetDefaultContentType("xml"), Is.EqualTo("application/custom+xml"));
        Assert.That(contentTypes.RemoveDefault("xml"), Is.True);
        Assert.That(contentTypes.RemoveDefault("xml"), Is.False);
        Assert.That(contentTypes.DefaultCount(), Is.EqualTo(0));
    }

    [Test]
    public void SetContentTypeForPart_AddsOverrideWhenNoMatchingDefaultExists()
    {
        global::docx.DocxContentTypes contentTypes = global::docx.DocxContentTypes.Parse(CreateContentTypesXml());

        contentTypes.SetContentTypeForPart("custom/item1.xml", "application/custom+xml");

        Assert.That(contentTypes.GetContentType("/custom/item1.xml"), Is.EqualTo("application/custom+xml"));
        Assert.That(contentTypes.OverrideCount(), Is.EqualTo(3));
    }

    [Test]
    public void SetContentTypeForPart_RemovesOverrideWhenDefaultCoversPart()
    {
        global::docx.DocxContentTypes contentTypes = global::docx.DocxContentTypes.Parse(CreateContentTypesXml());

        contentTypes.SetOverride("word/notes.xml", "application/custom+xml");

        Assert.That(contentTypes.OverrideCount(), Is.EqualTo(3));

        contentTypes.SetContentTypeForPart("word/notes.xml", global::docx.DocxConstants.ContentTypeXml);

        Assert.That(contentTypes.OverrideCount(), Is.EqualTo(2));
        Assert.That(contentTypes.GetContentType("word/notes.xml"), Is.EqualTo(global::docx.DocxConstants.ContentTypeXml));
    }

    [Test]
    public void SetOverride_UpdatesExistingOverrideWithoutAddingDuplicates()
    {
        global::docx.DocxContentTypes contentTypes = new global::docx.DocxContentTypes();

        contentTypes.SetOverride("word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument);
        contentTypes.SetOverride("/word/document.xml", global::docx.DocxConstants.ContentTypeMacroEnabledMainDocument);

        Assert.That(contentTypes.OverrideCount(), Is.EqualTo(1));
        Assert.That(contentTypes.GetOverrideContentType("word/document.xml"), Is.EqualTo(global::docx.DocxConstants.ContentTypeMacroEnabledMainDocument));
    }

    [Test]
    public void RemoveContentTypeForPart_RemovesPartSpecificOverrideOnly()
    {
        global::docx.DocxContentTypes contentTypes = global::docx.DocxContentTypes.Parse(CreateContentTypesXml());

        Assert.That(contentTypes.RemoveContentTypeForPart("word/document.xml"), Is.True);
        Assert.That(contentTypes.GetContentType("word/document.xml"), Is.EqualTo(global::docx.DocxConstants.ContentTypeXml));
        Assert.That(contentTypes.RemoveContentTypeForPart("word/document.xml"), Is.False);
        Assert.That(contentTypes.GetContentType("word/media/image1.png"), Is.EqualTo(global::docx.DocxConstants.ContentTypePng));
    }

    [Test]
    public void RemoveContentTypeForPart_DoesNotRemoveDefaultContentTypes()
    {
        global::docx.DocxContentTypes contentTypes = global::docx.DocxContentTypes.Parse(CreateContentTypesXml());

        Assert.That(contentTypes.RemoveContentTypeForPart("word/media/image1.png"), Is.False);
        Assert.That(contentTypes.GetContentType("word/media/image1.png"), Is.EqualTo(global::docx.DocxConstants.ContentTypePng));
        Assert.That(contentTypes.GetDefaultContentType("png"), Is.EqualTo(global::docx.DocxConstants.ContentTypePng));
    }

    [Test]
    public void OrderedContentTypes_ReturnCopiesWithoutMutatingCatalogOrder()
    {
        global::docx.DocxContentTypes contentTypes = new global::docx.DocxContentTypes();

        contentTypes.AddDefault("xml", global::docx.DocxConstants.ContentTypeXml);
        contentTypes.AddDefault("png", global::docx.DocxConstants.ContentTypePng);
        contentTypes.AddOverride("/word/styles.xml", global::docx.DocxConstants.ContentTypeStyles);
        contentTypes.AddOverride("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument);

        List<global::docx.DocxContentTypeDefault> orderedDefaults = contentTypes.GetOrderedDefaults(true);
        List<global::docx.DocxContentTypeOverride> orderedOverrides = contentTypes.GetOrderedOverrides(true);

        Assert.That(orderedDefaults[0].Extension, Is.EqualTo("png"));
        Assert.That(orderedDefaults[1].Extension, Is.EqualTo("xml"));
        Assert.That(orderedOverrides[0].PartName, Is.EqualTo("/word/document.xml"));
        Assert.That(orderedOverrides[1].PartName, Is.EqualTo("/word/styles.xml"));
        Assert.That(contentTypes.Defaults[0].Extension, Is.EqualTo("xml"));
        Assert.That(contentTypes.Overrides[0].PartName, Is.EqualTo("/word/styles.xml"));
    }

    [Test]
    public void ContentTypes_RejectNullAndInvalidInputs()
    {
        global::docx.DocxContentTypes contentTypes = new global::docx.DocxContentTypes();

        Assert.Throws<ArgumentNullException>(() => global::docx.DocxContentTypes.Parse((string)null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxContentTypes.Parse((Stream)null!));
        Assert.Throws<ArgumentNullException>(() => contentTypes.AddDefault(null!, global::docx.DocxConstants.ContentTypeXml));
        Assert.Throws<ArgumentNullException>(() => contentTypes.AddDefault("xml", null!));
        Assert.Throws<ArgumentException>(() => contentTypes.AddDefault("", global::docx.DocxConstants.ContentTypeXml));
        Assert.Throws<ArgumentException>(() => contentTypes.AddDefault("word/document.xml", global::docx.DocxConstants.ContentTypeXml));
        Assert.Throws<ArgumentException>(() => contentTypes.AddDefault("xml", ""));
        Assert.Throws<ArgumentNullException>(() => contentTypes.GetContentType(null!));
        Assert.Throws<ArgumentException>(() => contentTypes.GetContentType("/word/../document.xml"));
        Assert.Throws<ArgumentNullException>(() => contentTypes.SetContentTypeForPart(null!, global::docx.DocxConstants.ContentTypeXml));
        Assert.Throws<ArgumentNullException>(() => contentTypes.SetContentTypeForPart("word/document.xml", null!));
        Assert.Throws<ArgumentException>(() => contentTypes.SetContentTypeForPart("word/document.xml", ""));
        Assert.Throws<ArgumentException>(() => contentTypes.SetDefault("xml", ""));
        Assert.Throws<ArgumentException>(() => contentTypes.SetOverride("word/document.xml", ""));
    }

    static string CreateContentTypesXml()
    {
        return string.Join("\n", new string[]
        {
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">",
            "  <Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\" />",
            "  <Default Extension=\"xml\" ContentType=\"application/xml\" />",
            "  <Default Extension=\"png\" ContentType=\"image/png\" />",
            "  <Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\" />",
            "  <Override PartName=\"/word/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml\" />",
            "</Types>"
        });
    }
}
