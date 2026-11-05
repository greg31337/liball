using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxHeaderTests
{
    [Test]
    public void HeaderParsing_ReadsParagraphAndTableBlocks()
    {
        global::docx.DocxHeader header = ParseHeader("<w:hdr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p marker=\"first\"><w:r><w:t>Header</w:t></w:r></w:p><w:tbl marker=\"table\"><w:tr><w:tc><w:p /></w:tc></w:tr></w:tbl><w:custom marker=\"ignored\" /><w:p marker=\"second\" /></w:hdr>");

        Assert.That(header.HasXml(), Is.True);
        Assert.That(header.BlockCount(), Is.EqualTo(3));
        Assert.That(header.ParagraphCount(), Is.EqualTo(2));
        Assert.That(header.TableCount(), Is.EqualTo(1));
        Assert.That(header.GetBlock(0).IsParagraph(), Is.True);
        Assert.That(header.GetBlock(1).IsTable(), Is.True);
        Assert.That(header.GetBlock(2).IsParagraph(), Is.True);
        Assert.That(header.GetBlock(0).GetXml(), Does.Contain("first"));
        Assert.That(header.GetBlock(1).GetXml(), Does.Contain("table"));
        Assert.That(header.GetBlock(2).GetXml(), Does.Contain("second"));
    }

    [Test]
    public void HeaderCreation_CreatesMinimalHeaderXml()
    {
        global::docx.DocxHeader header = global::docx.DocxHeader.Create();

        Assert.That(header.HasXml(), Is.True);
        Assert.That(header.GetXml(), Is.EqualTo("<w:hdr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />"));
        Assert.That(header.BlockCount(), Is.EqualTo(0));
        Assert.That(global::docx.DocxHeader.CreateMinimalXml(), Is.EqualTo(header.GetXml()));
    }

    [Test]
    public void HeaderTextExtraction_UsesConfiguredSeparators()
    {
        global::docx.DocxHeader header = ParseHeader("<w:hdr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p><w:r><w:t>Header</w:t></w:r></w:p><w:tbl><w:tr><w:tc><w:p><w:r><w:t>Left</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t>Right</w:t></w:r></w:p></w:tc></w:tr></w:tbl></w:hdr>");
        global::docx.DocxTextOptions options = new global::docx.DocxTextOptions();

        options.ParagraphSeparator = "|";
        options.TableCellSeparator = ",";

        Assert.That(header.GetTextWithOptions(options), Is.EqualTo("Header|Left,Right"));
        Assert.Throws<ArgumentNullException>(() => header.GetTextWithOptions(null!));
    }

    [Test]
    public void DocumentHeaderDiscovery_ParsesInternalHeaderRelationships()
    {
        global::docx.DocxPackage package = CreatePackageWithHeaders();

        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);

        Assert.That(document.HeaderCount(), Is.EqualTo(1));
        Assert.That(document.GetHeader(0).RelationshipId, Is.EqualTo("rHeader1"));
        Assert.That(document.GetHeader(0).PartName, Is.EqualTo("/word/header1.xml"));
        Assert.That(document.GetHeader(0).BlockCount(), Is.EqualTo(1));
        Assert.That(document.GetHeader(0).GetBlock(0).IsParagraph(), Is.True);
        Assert.That(document.GetHeader(0).GetBlock(0).GetXml(), Does.Contain("Header One"));
    }

    [Test]
    public void DocumentHeaderDiscovery_ResolvesNestedHeaderTargets()
    {
        global::docx.DocxPackage package = CreatePackageWithNestedHeaderTarget();

        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);

        Assert.That(document.HeaderCount(), Is.EqualTo(1));
        Assert.That(document.GetHeader(0).RelationshipId, Is.EqualTo("rHeaderNested"));
        Assert.That(document.GetHeader(0).PartName, Is.EqualTo("/word/headers/header2.xml"));
        Assert.That(document.GetHeader(0).GetText(), Is.EqualTo("Nested Header"));
    }

    [Test]
    public void HeaderParsing_RejectsInvalidArguments()
    {
        global::docx.DocxHeader header = new global::docx.DocxHeader("");
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create("<root />");
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        global::docx.DocxRelationshipSet relationships = new global::docx.DocxRelationshipSet("/word/document.xml");
        global::docx.DocxRelationship relationship = new global::docx.DocxRelationship("rId1", global::docx.DocxConstants.RelationshipTypeHeader, "header1.xml", "");

        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxHeader(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxHeader.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxHeader.WriteMinimal(null!));
        Assert.Throws<ArgumentNullException>(() => header.AddParsedBlock(null!));
        Assert.Throws<ArgumentNullException>(() => header.SetPartInfo(null!, "rId1"));
        Assert.Throws<ArgumentNullException>(() => header.SetPartInfo("/word/header1.xml", null!));
        Assert.Throws<ArgumentNullException>(() => document.AddHeaderFromRelationship(null!, relationship));
        Assert.Throws<ArgumentNullException>(() => document.AddHeaderFromRelationship(relationships, null!));

        Assert.That(reader.ReadToElement("root", ""), Is.True);
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxHeader.Parse(reader));
    }

    static global::docx.DocxHeader ParseHeader(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("hdr", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxHeader.Parse(reader);
    }

    static global::docx.DocxPackage CreatePackageWithHeaders()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxRelationshipSet rootRelationships = global::docx.DocxPackage.CreateRootRelationshipSet("/word/document.xml");
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        documentRelationships.CreateRelationshipWithId("rHeader1", global::docx.DocxConstants.RelationshipTypeHeader, "header1.xml", false);
        documentRelationships.CreateRelationshipWithId("rMissing", global::docx.DocxConstants.RelationshipTypeHeader, "missingHeader.xml", false);
        documentRelationships.CreateRelationshipWithId("rExternal", global::docx.DocxConstants.RelationshipTypeHeader, "https://example.com/header.xml", true);

        package.ContentTypes.SetDefault("rels", global::docx.DocxConstants.ContentTypeRelationships);
        package.ContentTypes.SetDefault("xml", global::docx.DocxConstants.ContentTypeXml);
        package.ContentTypes.SetOverride("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument);
        package.ContentTypes.SetOverride("/word/header1.xml", global::docx.DocxConstants.ContentTypeHeader);
        package.AddXmlPartWithContent("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, global::docx.DocxDocument.CreateMinimalMainDocumentXml());
        package.AddXmlPartWithContent("/word/header1.xml", global::docx.DocxConstants.ContentTypeHeader, "<w:hdr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p><w:r><w:t>Header One</w:t></w:r></w:p></w:hdr>");
        package.AddXmlPartWithContent("_rels/.rels", global::docx.DocxConstants.ContentTypeRelationships, rootRelationships.WriteXml());
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, documentRelationships.WriteXml());

        return package;
    }

    static global::docx.DocxPackage CreatePackageWithNestedHeaderTarget()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxRelationshipSet rootRelationships = global::docx.DocxPackage.CreateRootRelationshipSet("/word/document.xml");
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        documentRelationships.CreateRelationshipWithId("rHeaderNested", global::docx.DocxConstants.RelationshipTypeHeader, "headers/header2.xml", false);

        package.ContentTypes.SetDefault("rels", global::docx.DocxConstants.ContentTypeRelationships);
        package.ContentTypes.SetDefault("xml", global::docx.DocxConstants.ContentTypeXml);
        package.ContentTypes.SetOverride("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument);
        package.ContentTypes.SetOverride("/word/headers/header2.xml", global::docx.DocxConstants.ContentTypeHeader);
        package.AddXmlPartWithContent("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, global::docx.DocxDocument.CreateMinimalMainDocumentXml());
        package.AddXmlPartWithContent("/word/headers/header2.xml", global::docx.DocxConstants.ContentTypeHeader, "<w:hdr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p><w:r><w:t>Nested Header</w:t></w:r></w:p></w:hdr>");
        package.AddXmlPartWithContent("_rels/.rels", global::docx.DocxConstants.ContentTypeRelationships, rootRelationships.WriteXml());
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, documentRelationships.WriteXml());

        return package;
    }
}
