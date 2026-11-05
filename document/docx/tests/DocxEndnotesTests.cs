using NUnit.Framework;
using System.IO;

namespace docx.Tests;

[TestFixture]
public class DocxEndnotesTests
{
    [Test]
    public void OpenPackage_DiscoversAndParsesEndnotesPart()
    {
        global::docx.DocxPackage package = CreatePackageWithEndnotes(CreateEndnotesXml());

        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);
        global::docx.DocxEndnote normalEndnote = document.FindEndnoteById("3")!;

        Assert.That(package.HasEndnotesPart(), Is.True);
        Assert.That(package.GetEndnotesPartName(), Is.EqualTo("/word/endnotes.xml"));
        Assert.That(package.GetEndnotesPart()!.PartName, Is.EqualTo("/word/endnotes.xml"));
        Assert.That(document.HasEndnotesRoot, Is.True);
        Assert.That(document.HasEndnotes(), Is.True);
        Assert.That(document.EndnotesPart, Is.Not.Null);
        Assert.That(document.EndnoteCount(), Is.EqualTo(2));
        Assert.That(document.GetEndnotes().EndnoteCount(), Is.EqualTo(2));
        Assert.That(document.GetEndnotes().UnknownChildCount(), Is.EqualTo(1));
        Assert.That(document.GetEndnotes().CountEndnotesByType("separator"), Is.EqualTo(1));
        Assert.That(document.GetEndnote(0).Id, Is.EqualTo("-1"));
        Assert.That(document.GetEndnote(0).Type, Is.EqualTo("separator"));
        Assert.That(document.GetEndnote(0).ParagraphCount(), Is.EqualTo(1));
        Assert.That(normalEndnote.Id, Is.EqualTo("3"));
        Assert.That(normalEndnote.Type, Is.EqualTo(""));
        Assert.That(normalEndnote.BlockCount(), Is.EqualTo(1));
        Assert.That(normalEndnote.ParagraphCount(), Is.EqualTo(1));
        Assert.That(normalEndnote.TableCount(), Is.EqualTo(0));
        Assert.That(normalEndnote.GetBlock(0).Paragraph!.GetText(), Is.EqualTo("Endnote text"));
        Assert.That(document.FindEndnoteById("missing"), Is.Null);
    }

    [Test]
    public void OpenPackage_ReturnsEmptyEndnotesWhenRelationshipIsMissing()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();

        Assert.That(document.Package.FindEndnotesRelationship(), Is.Null);
        Assert.That(document.Package.GetEndnotesPartName(), Is.EqualTo(""));
        Assert.That(document.Package.GetEndnotesPart(), Is.Null);
        Assert.That(document.Package.HasEndnotesPart(), Is.False);
        Assert.That(document.HasEndnotesRoot, Is.False);
        Assert.That(document.HasEndnotes(), Is.False);
        Assert.That(document.EndnoteCount(), Is.EqualTo(0));
    }

    [Test]
    public void EndnotesParsing_PreservesRawXmlAndUnknownChildren()
    {
        global::docx.DocxEndnotes endnotes = global::docx.DocxEndnotes.ParseXml(CreateEndnotesXml());
        global::docx.DocxEndnote endnote = endnotes.FindEndnoteById("3")!;

        Assert.That(endnotes.GetXml(), Does.Contain("<w:endnote"));
        Assert.That(endnotes.WriteXml(), Does.Contain("Endnote text"));
        Assert.That(endnotes.UnknownChildCount(), Is.EqualTo(1));
        Assert.That(endnote.GetXml(), Does.Contain("Endnote text"));
        Assert.That(endnote.WriteXml(), Does.Contain("Endnote text"));
        Assert.That(endnote.HasXml(), Is.True);
    }

    [Test]
    public void EndnotesTextExtraction_ReadsParagraphAndTableBlocks()
    {
        global::docx.DocxEndnotes endnotes = global::docx.DocxEndnotes.ParseXml("<w:endnotes xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:endnote w:id=\"5\"><w:p><w:r><w:t>Intro</w:t></w:r></w:p><w:tbl><w:tr><w:tc><w:p><w:r><w:t>A</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t>B</w:t></w:r></w:p></w:tc></w:tr></w:tbl></w:endnote></w:endnotes>");
        global::docx.DocxEndnote endnote = endnotes.FindEndnoteById("5")!;
        global::docx.DocxTextOptions options = new global::docx.DocxTextOptions();

        options.ParagraphSeparator = "|";
        options.TableCellSeparator = ",";

        Assert.That(endnote.BlockCount(), Is.EqualTo(2));
        Assert.That(endnote.ParagraphCount(), Is.EqualTo(1));
        Assert.That(endnote.TableCount(), Is.EqualTo(1));
        Assert.That(endnote.GetTextWithOptions(options), Is.EqualTo("Intro|A,B"));
        Assert.That(endnotes.GetTextWithOptions(options), Is.EqualTo("Intro|A,B"));
    }

    [Test]
    public void EndnotesParsing_RejectsInvalidArguments()
    {
        global::docx.DocxEndnotes endnotes = global::docx.DocxEndnotes.ParseXml(CreateEndnotesXml());
        global::docx.DocxEndnote endnote = endnotes.GetEndnote(0);
        global::docx.DocxXmlReader rootReader = global::docx.DocxXmlReader.Create("<root />");

        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxEndnotes(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxEndnotes.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxEndnotes.ParseXml(null!));
        Assert.Throws<ArgumentNullException>(() => endnotes.FindEndnoteById(null!));
        Assert.Throws<ArgumentNullException>(() => endnotes.CountEndnotesByType(null!));
        Assert.Throws<ArgumentNullException>(() => endnotes.IsEndnoteBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => endnotes.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxEndnote(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxEndnote.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxEndnote.ParseXml(null!));
        Assert.Throws<ArgumentNullException>(() => endnote.WriteTo(null!));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxEndnotes.Parse(rootReader));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxEndnotes.ParseXml("<root />"));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxEndnote.ParseXml("<root />"));
        Assert.Throws<InvalidDataException>(() => global::docx.DocxDocument.OpenPackage(CreatePackageWithEndnotes("<w:notEndnotes xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />")));
    }

    static global::docx.DocxPackage CreatePackageWithEndnotes(string endnotesXml)
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxRelationshipSet rootRelationships = global::docx.DocxPackage.CreateRootRelationshipSet("/word/document.xml");
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        documentRelationships.CreateRelationshipWithId("rEndnotes", global::docx.DocxConstants.RelationshipTypeEndnotes, "endnotes.xml", false);
        documentRelationships.CreateRelationshipWithId("rExternalEndnotes", global::docx.DocxConstants.RelationshipTypeEndnotes, "https://example.com/endnotes.xml", true);

        package.ContentTypes.SetDefault("rels", global::docx.DocxConstants.ContentTypeRelationships);
        package.ContentTypes.SetDefault("xml", global::docx.DocxConstants.ContentTypeXml);
        package.AddXmlPartWithContent("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, global::docx.DocxDocument.CreateMinimalMainDocumentXml());
        package.AddXmlPartWithContent("/word/endnotes.xml", global::docx.DocxConstants.ContentTypeEndnotes, endnotesXml);
        package.AddXmlPartWithContent("_rels/.rels", global::docx.DocxConstants.ContentTypeRelationships, rootRelationships.WriteXml());
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, documentRelationships.WriteXml());

        return package;
    }

    static string CreateEndnotesXml()
    {
        return string.Join(
            "",
            new string[]
            {
                "<w:endnotes xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">",
                "<w:endnote w:id=\"-1\" w:type=\"separator\"><w:p><w:r><w:t>Separator</w:t></w:r></w:p></w:endnote>",
                "<w:endnote w:id=\"3\"><w:p><w:r><w:t>Endnote text</w:t></w:r></w:p></w:endnote>",
                "<w:custom />",
                "</w:endnotes>"
            });
    }
}
