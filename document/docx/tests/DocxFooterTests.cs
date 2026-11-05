using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxFooterTests
{
    [Test]
    public void FooterParsing_ReadsParagraphAndTableBlocks()
    {
        global::docx.DocxFooter footer = ParseFooter("<w:ftr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p marker=\"first\"><w:r><w:t>Footer</w:t></w:r></w:p><w:tbl marker=\"table\"><w:tr><w:tc><w:p /></w:tc></w:tr></w:tbl><w:custom marker=\"ignored\" /><w:p marker=\"second\" /></w:ftr>");

        Assert.That(footer.HasXml(), Is.True);
        Assert.That(footer.BlockCount(), Is.EqualTo(3));
        Assert.That(footer.ParagraphCount(), Is.EqualTo(2));
        Assert.That(footer.TableCount(), Is.EqualTo(1));
        Assert.That(footer.GetBlock(0).IsParagraph(), Is.True);
        Assert.That(footer.GetBlock(1).IsTable(), Is.True);
        Assert.That(footer.GetBlock(2).IsParagraph(), Is.True);
        Assert.That(footer.GetBlock(0).GetXml(), Does.Contain("first"));
        Assert.That(footer.GetBlock(1).GetXml(), Does.Contain("table"));
        Assert.That(footer.GetBlock(2).GetXml(), Does.Contain("second"));
    }

    [Test]
    public void FooterCreation_CreatesMinimalFooterXml()
    {
        global::docx.DocxFooter footer = global::docx.DocxFooter.Create();

        Assert.That(footer.HasXml(), Is.True);
        Assert.That(footer.GetXml(), Is.EqualTo("<w:ftr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />"));
        Assert.That(footer.BlockCount(), Is.EqualTo(0));
        Assert.That(global::docx.DocxFooter.CreateMinimalXml(), Is.EqualTo(footer.GetXml()));
    }

    [Test]
    public void FooterTextExtraction_UsesConfiguredSeparators()
    {
        global::docx.DocxFooter footer = ParseFooter("<w:ftr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p><w:r><w:t>Footer</w:t></w:r></w:p><w:tbl><w:tr><w:tc><w:p><w:r><w:t>Left</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t>Right</w:t></w:r></w:p></w:tc></w:tr></w:tbl></w:ftr>");
        global::docx.DocxTextOptions options = new global::docx.DocxTextOptions();

        options.ParagraphSeparator = "|";
        options.TableCellSeparator = ",";

        Assert.That(footer.GetTextWithOptions(options), Is.EqualTo("Footer|Left,Right"));
        Assert.Throws<ArgumentNullException>(() => footer.GetTextWithOptions(null!));
    }

    [Test]
    public void DocumentFooterDiscovery_ParsesInternalFooterRelationships()
    {
        global::docx.DocxPackage package = CreatePackageWithFooters();

        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);

        Assert.That(document.FooterCount(), Is.EqualTo(1));
        Assert.That(document.GetFooter(0).RelationshipId, Is.EqualTo("rFooter1"));
        Assert.That(document.GetFooter(0).PartName, Is.EqualTo("/word/footer1.xml"));
        Assert.That(document.GetFooter(0).BlockCount(), Is.EqualTo(1));
        Assert.That(document.GetFooter(0).GetBlock(0).IsParagraph(), Is.True);
        Assert.That(document.GetFooter(0).GetBlock(0).GetXml(), Does.Contain("Footer One"));
    }

    [Test]
    public void DocumentFooterDiscovery_ResolvesNestedFooterTargets()
    {
        global::docx.DocxPackage package = CreatePackageWithNestedFooterTarget();

        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);

        Assert.That(document.FooterCount(), Is.EqualTo(1));
        Assert.That(document.GetFooter(0).RelationshipId, Is.EqualTo("rFooterNested"));
        Assert.That(document.GetFooter(0).PartName, Is.EqualTo("/word/footers/footer2.xml"));
        Assert.That(document.GetFooter(0).GetText(), Is.EqualTo("Nested Footer"));
    }

    [Test]
    public void FooterParsing_RejectsInvalidArguments()
    {
        global::docx.DocxFooter footer = new global::docx.DocxFooter("");
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create("<root />");
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        global::docx.DocxRelationshipSet relationships = new global::docx.DocxRelationshipSet("/word/document.xml");
        global::docx.DocxRelationship relationship = new global::docx.DocxRelationship("rId1", global::docx.DocxConstants.RelationshipTypeFooter, "footer1.xml", "");

        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxFooter(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxFooter.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxFooter.WriteMinimal(null!));
        Assert.Throws<ArgumentNullException>(() => footer.AddParsedBlock(null!));
        Assert.Throws<ArgumentNullException>(() => footer.SetPartInfo(null!, "rId1"));
        Assert.Throws<ArgumentNullException>(() => footer.SetPartInfo("/word/footer1.xml", null!));
        Assert.Throws<ArgumentNullException>(() => document.AddFooterFromRelationship(null!, relationship));
        Assert.Throws<ArgumentNullException>(() => document.AddFooterFromRelationship(relationships, null!));

        Assert.That(reader.ReadToElement("root", ""), Is.True);
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxFooter.Parse(reader));
    }

    static global::docx.DocxFooter ParseFooter(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("ftr", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxFooter.Parse(reader);
    }

    static global::docx.DocxPackage CreatePackageWithFooters()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxRelationshipSet rootRelationships = global::docx.DocxPackage.CreateRootRelationshipSet("/word/document.xml");
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        documentRelationships.CreateRelationshipWithId("rFooter1", global::docx.DocxConstants.RelationshipTypeFooter, "footer1.xml", false);
        documentRelationships.CreateRelationshipWithId("rMissing", global::docx.DocxConstants.RelationshipTypeFooter, "missingFooter.xml", false);
        documentRelationships.CreateRelationshipWithId("rExternal", global::docx.DocxConstants.RelationshipTypeFooter, "https://example.com/footer.xml", true);

        package.ContentTypes.SetDefault("rels", global::docx.DocxConstants.ContentTypeRelationships);
        package.ContentTypes.SetDefault("xml", global::docx.DocxConstants.ContentTypeXml);
        package.ContentTypes.SetOverride("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument);
        package.ContentTypes.SetOverride("/word/footer1.xml", global::docx.DocxConstants.ContentTypeFooter);
        package.AddXmlPartWithContent("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, global::docx.DocxDocument.CreateMinimalMainDocumentXml());
        package.AddXmlPartWithContent("/word/footer1.xml", global::docx.DocxConstants.ContentTypeFooter, "<w:ftr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p><w:r><w:t>Footer One</w:t></w:r></w:p></w:ftr>");
        package.AddXmlPartWithContent("_rels/.rels", global::docx.DocxConstants.ContentTypeRelationships, rootRelationships.WriteXml());
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, documentRelationships.WriteXml());

        return package;
    }

    static global::docx.DocxPackage CreatePackageWithNestedFooterTarget()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxRelationshipSet rootRelationships = global::docx.DocxPackage.CreateRootRelationshipSet("/word/document.xml");
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        documentRelationships.CreateRelationshipWithId("rFooterNested", global::docx.DocxConstants.RelationshipTypeFooter, "footers/footer2.xml", false);

        package.ContentTypes.SetDefault("rels", global::docx.DocxConstants.ContentTypeRelationships);
        package.ContentTypes.SetDefault("xml", global::docx.DocxConstants.ContentTypeXml);
        package.ContentTypes.SetOverride("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument);
        package.ContentTypes.SetOverride("/word/footers/footer2.xml", global::docx.DocxConstants.ContentTypeFooter);
        package.AddXmlPartWithContent("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, global::docx.DocxDocument.CreateMinimalMainDocumentXml());
        package.AddXmlPartWithContent("/word/footers/footer2.xml", global::docx.DocxConstants.ContentTypeFooter, "<w:ftr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p><w:r><w:t>Nested Footer</w:t></w:r></w:p></w:ftr>");
        package.AddXmlPartWithContent("_rels/.rels", global::docx.DocxConstants.ContentTypeRelationships, rootRelationships.WriteXml());
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, documentRelationships.WriteXml());

        return package;
    }
}
