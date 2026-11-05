using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxCorePropertiesTests
{
    [Test]
    public void CorePropertiesParsing_ReadsKnownFieldsAndUnknownChildren()
    {
        global::docx.DocxCoreProperties properties = ParseCoreProperties("<cp:coreProperties xmlns:cp=\"http://schemas.openxmlformats.org/package/2006/metadata/core-properties\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:dcterms=\"http://purl.org/dc/terms/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:x=\"urn:x\"><dc:title>Title</dc:title><dc:subject>Subject</dc:subject><dc:creator>Alice</dc:creator><cp:keywords>one two</cp:keywords><dc:description>Description</dc:description><cp:lastModifiedBy>Bob</cp:lastModifiedBy><cp:revision>7</cp:revision><dcterms:created xsi:type=\"dcterms:W3CDTF\">2026-08-03T12:00:00Z</dcterms:created><dcterms:modified xsi:type=\"dcterms:W3CDTF\">2026-08-04T12:00:00Z</dcterms:modified><cp:category>Category</cp:category><cp:contentStatus>Draft</cp:contentStatus><cp:version>1.0</cp:version><x:custom x:data=\"1\" /></cp:coreProperties>");

        Assert.That(properties.Title, Is.EqualTo("Title"));
        Assert.That(properties.Subject, Is.EqualTo("Subject"));
        Assert.That(properties.Creator, Is.EqualTo("Alice"));
        Assert.That(properties.Keywords, Is.EqualTo("one two"));
        Assert.That(properties.Description, Is.EqualTo("Description"));
        Assert.That(properties.LastModifiedBy, Is.EqualTo("Bob"));
        Assert.That(properties.Revision, Is.EqualTo("7"));
        Assert.That(properties.Created, Is.EqualTo("2026-08-03T12:00:00Z"));
        Assert.That(properties.Modified, Is.EqualTo("2026-08-04T12:00:00Z"));
        Assert.That(properties.Category, Is.EqualTo("Category"));
        Assert.That(properties.ContentStatus, Is.EqualTo("Draft"));
        Assert.That(properties.Version, Is.EqualTo("1.0"));
        Assert.That(properties.UnknownChildCount(), Is.EqualTo(1));
        Assert.That(properties.GetUnknownChildXml(0), Does.Contain("x:data=\"1\""));
    }

    [Test]
    public void CorePropertiesWriting_WritesKnownFieldsDatesAndUnknownChildren()
    {
        global::docx.DocxCoreProperties properties = global::docx.DocxCoreProperties.Create();

        properties.SetTitle("A <Title>");
        properties.SetSubject("Subject");
        properties.SetCreator("Alice");
        properties.SetKeywords("one two");
        properties.SetDescription("Description");
        properties.SetLastModifiedBy("Bob");
        properties.SetRevision("8");
        properties.SetCreated("2026-08-03T12:00:00Z");
        properties.SetModified("2026-08-04T12:00:00Z");
        properties.SetCategory("Category");
        properties.SetContentStatus("Final");
        properties.SetVersion("1.1");
        properties.AddUnknownChild(new global::docx.DocxXmlBuffer("<x:custom xmlns:x=\"urn:x\" />"));

        string xml = properties.WriteXml();
        global::docx.DocxCoreProperties parsed = ParseCoreProperties(xml);

        Assert.That(xml, Does.Contain("xmlns:cp=\"http://schemas.openxmlformats.org/package/2006/metadata/core-properties\""));
        Assert.That(xml, Does.Contain("xsi:type=\"dcterms:W3CDTF\""));
        Assert.That(xml, Does.Contain("A &lt;Title&gt;"));
        Assert.That(parsed.Title, Is.EqualTo("A <Title>"));
        Assert.That(parsed.Modified, Is.EqualTo("2026-08-04T12:00:00Z"));
        Assert.That(parsed.UnknownChildCount(), Is.EqualTo(1));
    }

    [Test]
    public void DocumentCoreProperties_ParseCreateWriteAndReopen()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxRelationshipSet relationshipSet = package.GetPackageRelationshipSet()!;
        relationshipSet.CreateRelationship(global::docx.DocxConstants.RelationshipTypeCoreProperties, "docProps/core.xml", false);
        package.ReplaceXmlPart("/_rels/.rels", global::docx.DocxConstants.ContentTypeRelationships, relationshipSet.WriteXml());
        package.AddXmlPartWithContent("/docProps/core.xml", global::docx.DocxConstants.ContentTypeCoreProperties, "<cp:coreProperties xmlns:cp=\"http://schemas.openxmlformats.org/package/2006/metadata/core-properties\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\"><dc:title>Existing</dc:title></cp:coreProperties>");

        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);

        Assert.That(document.HasCoreProperties(), Is.True);
        Assert.That(document.GetCoreProperties().Title, Is.EqualTo("Existing"));
        Assert.That(package.GetCorePropertiesPartName(), Is.EqualTo("/docProps/core.xml"));
        Assert.That(package.HasCorePropertiesPart(), Is.True);

        document.GetCoreProperties().SetTitle("Updated");
        document.WriteCorePropertiesPart();

        global::docx.DocxDocument reopened = global::docx.DocxDocument.OpenPackage(package);

        Assert.That(reopened.GetCoreProperties().Title, Is.EqualTo("Updated"));

        global::docx.DocxDocument created = global::docx.DocxDocument.Create();
        global::docx.DocxCoreProperties core = created.GetOrCreateCoreProperties();

        core.SetCreator("Creator");
        created.WriteCorePropertiesPart();

        Assert.That(created.HasCoreProperties(), Is.True);
        Assert.That(created.Package.GetCorePropertiesPartName(), Is.EqualTo("/docProps/core.xml"));
        Assert.That(created.Package.GetCorePropertiesPart()!.GetText(), Does.Contain("Creator"));
        Assert.That(created.Package.FindCorePropertiesRelationship(), Is.Not.Null);
    }

    [Test]
    public void CoreProperties_RejectInvalidArguments()
    {
        global::docx.DocxCoreProperties properties = global::docx.DocxCoreProperties.Create();
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create("<root />");
        global::docx.DocxXmlWriter writer = global::docx.DocxXmlWriter.Create();

        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxCoreProperties(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxCoreProperties.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxCoreProperties.ParseXml(null!));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxCoreProperties.ParseXml("<root />"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxCoreProperties.IsSupported(null!));
        Assert.Throws<ArgumentNullException>(() => properties.ReadKnownChild(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetTitle(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetSubject(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetCreator(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetKeywords(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetDescription(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetLastModifiedBy(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetRevision(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetCreated(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetModified(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetCategory(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetContentStatus(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetVersion(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxCoreProperties.ValidateValue(null!, ""));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxCoreProperties.ValidateValue("value", null!));
        Assert.Throws<ArgumentNullException>(() => properties.AddUnknownChild(null!));
        Assert.Throws<ArgumentNullException>(() => properties.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => properties.WriteChildrenTo(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxCoreProperties.WriteTextElement(null!, "dc", "title", global::docx.DocxConstants.NamespaceDublinCore, "value"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxCoreProperties.WriteTextElement(writer, "dc", "title", global::docx.DocxConstants.NamespaceDublinCore, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxCoreProperties.WriteDateElement(null!, "created", "value"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxCoreProperties.WriteDateElement(writer, null!, "value"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxCoreProperties.WriteDateElement(writer, "created", null!));
        Assert.Throws<InvalidOperationException>(() => document.WriteCorePropertiesPart());

        Assert.That(reader.ReadToElement("root", ""), Is.True);
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxCoreProperties.Parse(reader));
    }

    static global::docx.DocxCoreProperties ParseCoreProperties(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("coreProperties", global::docx.DocxConstants.NamespaceCoreProperties), Is.True);

        return global::docx.DocxCoreProperties.Parse(reader);
    }
}
