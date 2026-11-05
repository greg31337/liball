using NUnit.Framework;
using System.IO;

namespace docx.Tests;

[TestFixture]
public class DocxFootnotesTests
{
    [Test]
    public void OpenPackage_DiscoversAndParsesFootnotesPart()
    {
        global::docx.DocxPackage package = CreatePackageWithFootnotes(CreateFootnotesXml());

        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);
        global::docx.DocxFootnote normalFootnote = document.FindFootnoteById("2")!;

        Assert.That(package.HasFootnotesPart(), Is.True);
        Assert.That(package.GetFootnotesPartName(), Is.EqualTo("/word/footnotes.xml"));
        Assert.That(package.GetFootnotesPart()!.PartName, Is.EqualTo("/word/footnotes.xml"));
        Assert.That(document.HasFootnotesRoot, Is.True);
        Assert.That(document.HasFootnotes(), Is.True);
        Assert.That(document.FootnotesPart, Is.Not.Null);
        Assert.That(document.FootnoteCount(), Is.EqualTo(2));
        Assert.That(document.GetFootnotes().FootnoteCount(), Is.EqualTo(2));
        Assert.That(document.GetFootnotes().UnknownChildCount(), Is.EqualTo(1));
        Assert.That(document.GetFootnotes().CountFootnotesByType("separator"), Is.EqualTo(1));
        Assert.That(document.GetFootnote(0).Id, Is.EqualTo("-1"));
        Assert.That(document.GetFootnote(0).Type, Is.EqualTo("separator"));
        Assert.That(document.GetFootnote(0).ParagraphCount(), Is.EqualTo(1));
        Assert.That(normalFootnote.Id, Is.EqualTo("2"));
        Assert.That(normalFootnote.Type, Is.EqualTo(""));
        Assert.That(normalFootnote.BlockCount(), Is.EqualTo(1));
        Assert.That(normalFootnote.ParagraphCount(), Is.EqualTo(1));
        Assert.That(normalFootnote.TableCount(), Is.EqualTo(0));
        Assert.That(normalFootnote.GetBlock(0).Paragraph!.GetText(), Is.EqualTo("Footnote text"));
        Assert.That(document.FindFootnoteById("missing"), Is.Null);
    }

    [Test]
    public void OpenPackage_ReturnsEmptyFootnotesWhenRelationshipIsMissing()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();

        Assert.That(document.Package.FindFootnotesRelationship(), Is.Null);
        Assert.That(document.Package.GetFootnotesPartName(), Is.EqualTo(""));
        Assert.That(document.Package.GetFootnotesPart(), Is.Null);
        Assert.That(document.Package.HasFootnotesPart(), Is.False);
        Assert.That(document.HasFootnotesRoot, Is.False);
        Assert.That(document.HasFootnotes(), Is.False);
        Assert.That(document.FootnoteCount(), Is.EqualTo(0));
    }

    [Test]
    public void FootnotesParsing_PreservesRawXmlAndUnknownChildren()
    {
        global::docx.DocxFootnotes footnotes = global::docx.DocxFootnotes.ParseXml(CreateFootnotesXml());
        global::docx.DocxFootnote footnote = footnotes.FindFootnoteById("2")!;

        Assert.That(footnotes.GetXml(), Does.Contain("<w:footnote"));
        Assert.That(footnotes.WriteXml(), Does.Contain("Footnote text"));
        Assert.That(footnotes.UnknownChildCount(), Is.EqualTo(1));
        Assert.That(footnote.GetXml(), Does.Contain("Footnote text"));
        Assert.That(footnote.WriteXml(), Does.Contain("Footnote text"));
        Assert.That(footnote.HasXml(), Is.True);
    }

    [Test]
    public void FootnotesTextExtraction_ReadsParagraphAndTableBlocks()
    {
        global::docx.DocxFootnotes footnotes = global::docx.DocxFootnotes.ParseXml("<w:footnotes xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:footnote w:id=\"4\"><w:p><w:r><w:t>Intro</w:t></w:r></w:p><w:tbl><w:tr><w:tc><w:p><w:r><w:t>A</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t>B</w:t></w:r></w:p></w:tc></w:tr></w:tbl></w:footnote></w:footnotes>");
        global::docx.DocxFootnote footnote = footnotes.FindFootnoteById("4")!;
        global::docx.DocxTextOptions options = new global::docx.DocxTextOptions();

        options.ParagraphSeparator = "|";
        options.TableCellSeparator = ",";

        Assert.That(footnote.BlockCount(), Is.EqualTo(2));
        Assert.That(footnote.ParagraphCount(), Is.EqualTo(1));
        Assert.That(footnote.TableCount(), Is.EqualTo(1));
        Assert.That(footnote.GetTextWithOptions(options), Is.EqualTo("Intro|A,B"));
        Assert.That(footnotes.GetTextWithOptions(options), Is.EqualTo("Intro|A,B"));
    }

    [Test]
    public void FootnotesParsing_RejectsInvalidArguments()
    {
        global::docx.DocxFootnotes footnotes = global::docx.DocxFootnotes.ParseXml(CreateFootnotesXml());
        global::docx.DocxFootnote footnote = footnotes.GetFootnote(0);
        global::docx.DocxXmlReader rootReader = global::docx.DocxXmlReader.Create("<root />");

        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxFootnotes(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxFootnotes.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxFootnotes.ParseXml(null!));
        Assert.Throws<ArgumentNullException>(() => footnotes.FindFootnoteById(null!));
        Assert.Throws<ArgumentNullException>(() => footnotes.CountFootnotesByType(null!));
        Assert.Throws<ArgumentNullException>(() => footnotes.IsFootnoteBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => footnotes.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxFootnote(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxFootnote.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxFootnote.ParseXml(null!));
        Assert.Throws<ArgumentNullException>(() => footnote.WriteTo(null!));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxFootnotes.Parse(rootReader));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxFootnotes.ParseXml("<root />"));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxFootnote.ParseXml("<root />"));
        Assert.Throws<InvalidDataException>(() => global::docx.DocxDocument.OpenPackage(CreatePackageWithFootnotes("<w:notFootnotes xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />")));
    }

    static global::docx.DocxPackage CreatePackageWithFootnotes(string footnotesXml)
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxRelationshipSet rootRelationships = global::docx.DocxPackage.CreateRootRelationshipSet("/word/document.xml");
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        documentRelationships.CreateRelationshipWithId("rFootnotes", global::docx.DocxConstants.RelationshipTypeFootnotes, "footnotes.xml", false);
        documentRelationships.CreateRelationshipWithId("rExternalFootnotes", global::docx.DocxConstants.RelationshipTypeFootnotes, "https://example.com/footnotes.xml", true);

        package.ContentTypes.SetDefault("rels", global::docx.DocxConstants.ContentTypeRelationships);
        package.ContentTypes.SetDefault("xml", global::docx.DocxConstants.ContentTypeXml);
        package.AddXmlPartWithContent("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, global::docx.DocxDocument.CreateMinimalMainDocumentXml());
        package.AddXmlPartWithContent("/word/footnotes.xml", global::docx.DocxConstants.ContentTypeFootnotes, footnotesXml);
        package.AddXmlPartWithContent("_rels/.rels", global::docx.DocxConstants.ContentTypeRelationships, rootRelationships.WriteXml());
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, documentRelationships.WriteXml());

        return package;
    }

    static string CreateFootnotesXml()
    {
        return string.Join(
            "",
            new string[]
            {
                "<w:footnotes xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">",
                "<w:footnote w:id=\"-1\" w:type=\"separator\"><w:p><w:r><w:t>Separator</w:t></w:r></w:p></w:footnote>",
                "<w:footnote w:id=\"2\"><w:p><w:r><w:t>Footnote text</w:t></w:r></w:p></w:footnote>",
                "<w:custom />",
                "</w:footnotes>"
            });
    }
}
