using System.IO;
using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxNoteCreationTests
{
    [Test]
    public void CreateFootnote_CreatesPartRelationshipAndPersistsNote()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();

        global::docx.DocxFootnote first = document.CreateFootnote("First & <footnote>");
        global::docx.DocxFootnote second = document.CreateFootnoteWithId("7", "Explicit footnote");
        global::docx.DocxRelationshipSet relationships = document.Package.FindRelationshipSet("/word/document.xml")!;
        global::docx.DocxRelationship relationship = relationships.FindById("rId1")!;
        string xml = document.Package.FindPart("/word/footnotes.xml")!.GetText();
        global::docx.DocxDocument reopened = global::docx.DocxDocument.OpenPackage(document.Package);

        Assert.That(first.Id, Is.EqualTo("1"));
        Assert.That(second.Id, Is.EqualTo("7"));
        Assert.That(first.GetBlock(0).Paragraph!.GetText(), Is.EqualTo("First & <footnote>"));
        Assert.That(document.HasFootnotes(), Is.True);
        Assert.That(document.FootnoteCount(), Is.EqualTo(2));
        Assert.That(document.FootnotesPart!.PartName, Is.EqualTo("/word/footnotes.xml"));
        Assert.That(relationship.Type, Is.EqualTo(global::docx.DocxConstants.RelationshipTypeFootnotes));
        Assert.That(relationship.Target, Is.EqualTo("footnotes.xml"));
        Assert.That(document.Package.FindPart("/word/_rels/document.xml.rels")!.GetText(), Does.Contain(global::docx.DocxConstants.RelationshipTypeFootnotes));
        Assert.That(xml, Does.Contain("First &amp; &lt;footnote&gt;"));
        Assert.That(xml, Does.Contain("Explicit footnote"));
        Assert.That(reopened.FootnoteCount(), Is.EqualTo(2));
        Assert.That(reopened.FindFootnoteById("1")!.GetBlock(0).Paragraph!.GetText(), Is.EqualTo("First & <footnote>"));
        Assert.That(reopened.FindFootnoteById("7")!.GetBlock(0).Paragraph!.GetText(), Is.EqualTo("Explicit footnote"));
    }

    [Test]
    public void CreateFootnote_AppendsToExistingFootnotesAndAllocatesNextPositiveId()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(CreatePackageWithFootnotes());

        global::docx.DocxFootnote created = document.CreateFootnote("Created footnote");

        Assert.That(created.Id, Is.EqualTo("3"));
        Assert.That(document.FootnoteCount(), Is.EqualTo(3));
        Assert.That(document.Package.FindPart("/word/footnotes.xml")!.GetText(), Does.Contain("Existing"));
        Assert.That(document.Package.FindPart("/word/footnotes.xml")!.GetText(), Does.Contain("Created footnote"));
    }

    [Test]
    public void CreateEndnote_CreatesPartRelationshipAndPersistsNote()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();

        global::docx.DocxEndnote first = document.CreateEndnote("First & <endnote>");
        global::docx.DocxEndnote second = document.CreateEndnoteWithId("8", "Explicit endnote");
        global::docx.DocxRelationshipSet relationships = document.Package.FindRelationshipSet("/word/document.xml")!;
        global::docx.DocxRelationship relationship = relationships.FindById("rId1")!;
        string xml = document.Package.FindPart("/word/endnotes.xml")!.GetText();
        global::docx.DocxDocument reopened = global::docx.DocxDocument.OpenPackage(document.Package);

        Assert.That(first.Id, Is.EqualTo("1"));
        Assert.That(second.Id, Is.EqualTo("8"));
        Assert.That(first.GetBlock(0).Paragraph!.GetText(), Is.EqualTo("First & <endnote>"));
        Assert.That(document.HasEndnotes(), Is.True);
        Assert.That(document.EndnoteCount(), Is.EqualTo(2));
        Assert.That(document.EndnotesPart!.PartName, Is.EqualTo("/word/endnotes.xml"));
        Assert.That(relationship.Type, Is.EqualTo(global::docx.DocxConstants.RelationshipTypeEndnotes));
        Assert.That(relationship.Target, Is.EqualTo("endnotes.xml"));
        Assert.That(document.Package.FindPart("/word/_rels/document.xml.rels")!.GetText(), Does.Contain(global::docx.DocxConstants.RelationshipTypeEndnotes));
        Assert.That(xml, Does.Contain("First &amp; &lt;endnote&gt;"));
        Assert.That(xml, Does.Contain("Explicit endnote"));
        Assert.That(reopened.EndnoteCount(), Is.EqualTo(2));
        Assert.That(reopened.FindEndnoteById("1")!.GetBlock(0).Paragraph!.GetText(), Is.EqualTo("First & <endnote>"));
        Assert.That(reopened.FindEndnoteById("8")!.GetBlock(0).Paragraph!.GetText(), Is.EqualTo("Explicit endnote"));
    }

    [Test]
    public void CreateEndnote_AppendsToExistingEndnotesAndAllocatesNextPositiveId()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(CreatePackageWithEndnotes());

        global::docx.DocxEndnote created = document.CreateEndnote("Created endnote");

        Assert.That(created.Id, Is.EqualTo("4"));
        Assert.That(document.EndnoteCount(), Is.EqualTo(3));
        Assert.That(document.Package.FindPart("/word/endnotes.xml")!.GetText(), Does.Contain("Existing"));
        Assert.That(document.Package.FindPart("/word/endnotes.xml")!.GetText(), Does.Contain("Created endnote"));
    }

    [Test]
    public void NoteCreation_RejectsInvalidArguments()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        global::docx.DocxFootnotes footnotes = global::docx.DocxFootnotes.Create();
        global::docx.DocxEndnotes endnotes = global::docx.DocxEndnotes.Create();
        global::docx.DocxFootnote footnote = new global::docx.DocxFootnote("");
        global::docx.DocxEndnote endnote = new global::docx.DocxEndnote("");

        Assert.Throws<ArgumentNullException>(() => document.CreateFootnote(null!));
        Assert.Throws<ArgumentNullException>(() => document.CreateFootnoteWithId(null!, "text"));
        Assert.Throws<ArgumentNullException>(() => document.CreateFootnoteWithId("1", null!));
        Assert.Throws<ArgumentNullException>(() => document.CreateEndnote(null!));
        Assert.Throws<ArgumentNullException>(() => document.CreateEndnoteWithId(null!, "text"));
        Assert.Throws<ArgumentNullException>(() => document.CreateEndnoteWithId("1", null!));
        Assert.Throws<ArgumentNullException>(() => document.AllocateNotesPartName(null!));
        Assert.Throws<InvalidOperationException>(() => new global::docx.DocxDocument(global::docx.DocxPackage.Create()).WriteFootnotesPart());
        Assert.Throws<InvalidOperationException>(() => new global::docx.DocxDocument(global::docx.DocxPackage.Create()).WriteEndnotesPart());
        Assert.Throws<ArgumentException>(() => document.CreateFootnoteWithId("", "text"));
        Assert.Throws<ArgumentException>(() => document.CreateEndnoteWithId("", "text"));
        Assert.Throws<ArgumentException>(() => document.AllocateNotesPartName(""));

        Assert.Throws<ArgumentNullException>(() => footnotes.CreateFootnote(null!));
        Assert.Throws<ArgumentNullException>(() => footnotes.CreateFootnoteWithId(null!, "text"));
        Assert.Throws<ArgumentNullException>(() => footnotes.CreateFootnoteWithId("1", null!));
        Assert.Throws<ArgumentNullException>(() => footnotes.AddFootnote(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxFootnotes.IsNonNegativeInteger(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxFootnotes.ParseNonNegativeInteger(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxFootnote.Create(null!, "text"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxFootnote.Create("1", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxFootnote.CreateTextParagraph(null!));
        Assert.Throws<ArgumentNullException>(() => footnote.AddParagraph(null!));
        Assert.Throws<ArgumentNullException>(() => footnote.AddBlock(null!));
        Assert.Throws<ArgumentException>(() => footnotes.CreateFootnoteWithId("", "text"));
        Assert.Throws<ArgumentException>(() => footnotes.AddFootnote(new global::docx.DocxFootnote("")));
        Assert.Throws<ArgumentException>(() => global::docx.DocxFootnotes.ParseNonNegativeInteger("abc"));
        Assert.Throws<ArgumentException>(() => global::docx.DocxFootnote.Create("", "text"));
        footnotes.CreateFootnoteWithId("1", "First");
        Assert.Throws<InvalidOperationException>(() => footnotes.CreateFootnoteWithId("1", "Duplicate"));

        Assert.Throws<ArgumentNullException>(() => endnotes.CreateEndnote(null!));
        Assert.Throws<ArgumentNullException>(() => endnotes.CreateEndnoteWithId(null!, "text"));
        Assert.Throws<ArgumentNullException>(() => endnotes.CreateEndnoteWithId("1", null!));
        Assert.Throws<ArgumentNullException>(() => endnotes.AddEndnote(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxEndnotes.IsNonNegativeInteger(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxEndnotes.ParseNonNegativeInteger(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxEndnote.Create(null!, "text"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxEndnote.Create("1", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxEndnote.CreateTextParagraph(null!));
        Assert.Throws<ArgumentNullException>(() => endnote.AddParagraph(null!));
        Assert.Throws<ArgumentNullException>(() => endnote.AddBlock(null!));
        Assert.Throws<ArgumentException>(() => endnotes.CreateEndnoteWithId("", "text"));
        Assert.Throws<ArgumentException>(() => endnotes.AddEndnote(new global::docx.DocxEndnote("")));
        Assert.Throws<ArgumentException>(() => global::docx.DocxEndnotes.ParseNonNegativeInteger("abc"));
        Assert.Throws<ArgumentException>(() => global::docx.DocxEndnote.Create("", "text"));
        endnotes.CreateEndnoteWithId("1", "First");
        Assert.Throws<InvalidOperationException>(() => endnotes.CreateEndnoteWithId("1", "Duplicate"));
    }

    [Test]
    public void NoteIdArithmetic_RejectsValuesOutsideTheSupportedRange()
    {
        global::docx.DocxFootnotes footnotes = global::docx.DocxFootnotes.Create();
        global::docx.DocxEndnotes endnotes = global::docx.DocxEndnotes.Create();

        Assert.That(global::docx.DocxFootnotes.IsNonNegativeInteger("2147483648"), Is.False);
        Assert.That(global::docx.DocxEndnotes.IsNonNegativeInteger("99999999999999999999"), Is.False);
        Assert.Throws<ArgumentException>(() => global::docx.DocxFootnotes.ParseNonNegativeInteger("2147483648"));
        Assert.Throws<ArgumentException>(() => global::docx.DocxEndnotes.ParseNonNegativeInteger("99999999999999999999"));

        footnotes.CreateFootnoteWithId("2147483647", "Last footnote");
        endnotes.CreateEndnoteWithId("2147483647", "Last endnote");

        Assert.Throws<InvalidOperationException>(() => footnotes.AllocateFootnoteId());
        Assert.Throws<InvalidOperationException>(() => endnotes.AllocateEndnoteId());
    }

    [Test]
    public void NoteManagement_CreatesEnumeratesFindsAndReferencesNotesAcrossRoundTrip()
    {
        global::docx.IDocxNoteManagement notes = new global::docx.DocxNoteManagement();
        global::docx.IDocxDocumentPersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.IDocxContentFactory factory = new global::docx.DocxContentFactory();
        global::docx.DocxDocument document = persistence.CreateDocument();
        global::docx.DocxFootnote footnote = notes.CreateFootnote(document, "Footnote body");
        global::docx.DocxEndnote endnote = notes.CreateEndnoteWithId(document, "8", "Endnote body");
        global::docx.DocxParagraph paragraph = factory.CreateTextParagraph("Text");

        notes.InsertFootnoteReference(paragraph, 0, footnote);
        notes.InsertEndnoteReference(paragraph, 2, endnote);
        document.GetBody().AddBlock(factory.CreateParagraphBlock(paragraph));

        Assert.That(notes.GetFootnotes(document), Is.EqualTo(new[] { footnote }));
        Assert.That(notes.GetEndnotes(document), Is.EqualTo(new[] { endnote }));
        Assert.That(notes.FindFootnote(document, "1"), Is.SameAs(footnote));
        Assert.That(notes.FindEndnote(document, "8"), Is.SameAs(endnote));
        Assert.That(notes.GetFootnoteId(footnote), Is.EqualTo("1"));
        Assert.That(notes.GetEndnoteId(endnote), Is.EqualTo("8"));
        Assert.That(notes.GetFootnoteReferenceIds(paragraph), Is.EqualTo(new[] { "1" }));
        Assert.That(notes.GetEndnoteReferenceIds(paragraph), Is.EqualTo(new[] { "8" }));

        global::docx.DocxDocument reopened = RoundTripDocument(persistence, document);
        global::docx.DocxParagraph parsed = reopened.GetBody().GetParagraph(0);

        Assert.That(notes.FindFootnote(reopened, "1")!.GetText(), Is.EqualTo("Footnote body"));
        Assert.That(notes.FindEndnote(reopened, "8")!.GetText(), Is.EqualTo("Endnote body"));
        Assert.That(notes.GetFootnoteReferenceIds(parsed), Is.EqualTo(new[] { "1" }));
        Assert.That(notes.GetEndnoteReferenceIds(parsed), Is.EqualTo(new[] { "8" }));
        Assert.That(parsed.GetText(), Is.EqualTo("Text"));
    }

    [Test]
    public void NoteManagement_EditsStoryBlocksAndPersistsChanges()
    {
        global::docx.IDocxNoteManagement notes = new global::docx.DocxNoteManagement();
        global::docx.IDocxDocumentPersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.IDocxContentFactory factory = new global::docx.DocxContentFactory();
        global::docx.DocxDocument document = persistence.CreateDocument();
        global::docx.DocxFootnote footnote = notes.CreateFootnote(document, "Old footnote");
        global::docx.DocxEndnote endnote = notes.CreateEndnote(document, "Old endnote");

        notes.ReplaceFootnoteText(document, footnote, "New footnote");
        notes.AppendFootnoteParagraph(document, footnote, factory.CreateTextParagraph("More footnote"));
        notes.ReplaceEndnoteText(document, endnote, "New endnote");
        notes.AppendEndnoteParagraph(document, endnote, factory.CreateTextParagraph("More endnote"));

        Assert.That(notes.GetFootnoteBlocks(footnote).Count, Is.EqualTo(2));
        Assert.That(notes.GetEndnoteBlocks(endnote).Count, Is.EqualTo(2));
        Assert.That(notes.RemoveFootnoteBlock(document, footnote, 0).GetParagraph()!.GetText(), Is.EqualTo("New footnote"));
        Assert.That(notes.RemoveEndnoteBlock(document, endnote, 1).GetParagraph()!.GetText(), Is.EqualTo("More endnote"));
        Assert.That(footnote.GetText(), Is.EqualTo("More footnote"));
        Assert.That(endnote.GetText(), Is.EqualTo("New endnote"));

        global::docx.DocxDocument reopened = RoundTripDocument(persistence, document);
        Assert.That(notes.FindFootnote(reopened, "1")!.GetText(), Is.EqualTo("More footnote"));
        Assert.That(notes.FindEndnote(reopened, "1")!.GetText(), Is.EqualTo("New endnote"));
    }

    [Test]
    public void NoteManagement_RemovesReferencesWithoutRemovingOtherRunContent()
    {
        global::docx.IDocxNoteManagement notes = new global::docx.DocxNoteManagement();
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph(
            "<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">"
            + "<w:r><w:t>Before</w:t><w:footnoteReference w:id=\"4\"/><w:t>Middle</w:t><w:endnoteReference w:id=\"5\"/><w:t>After</w:t></w:r>"
            + "<w:r><w:footnoteReference w:id=\"4\"/></w:r>"
            + "</w:p>");

        Assert.That(notes.GetFootnoteReferenceIds(paragraph), Is.EqualTo(new[] { "4", "4" }));
        Assert.That(notes.GetEndnoteReferenceIds(paragraph), Is.EqualTo(new[] { "5" }));
        Assert.That(notes.RemoveFootnoteReferences(paragraph, "4"), Is.EqualTo(2));
        Assert.That(paragraph.RunCount(), Is.EqualTo(1));
        Assert.That(paragraph.GetText(), Is.EqualTo("BeforeMiddleAfter"));
        Assert.That(notes.GetFootnoteReferenceIds(paragraph), Is.Empty);
        Assert.That(notes.GetEndnoteReferenceIds(paragraph), Is.EqualTo(new[] { "5" }));
        Assert.That(notes.RemoveEndnoteReferences(paragraph, "5"), Is.EqualTo(1));
        Assert.That(paragraph.GetText(), Is.EqualTo("BeforeMiddleAfter"));
    }

    [Test]
    public void NoteManagement_DeletesDefinitionsAndExposesSeparatorStoryTypes()
    {
        global::docx.IDocxNoteManagement notes = new global::docx.DocxNoteManagement();
        global::docx.DocxDocument footnoteDocument = global::docx.DocxDocument.OpenPackage(CreatePackageWithFootnotes());
        global::docx.DocxDocument endnoteDocument = global::docx.DocxDocument.OpenPackage(CreatePackageWithEndnotes());

        Assert.That(notes.GetFootnotes(footnoteDocument).Count, Is.EqualTo(2));
        Assert.That(notes.GetEndnotes(endnoteDocument).Count, Is.EqualTo(2));
        Assert.That(notes.GetFootnoteType(notes.GetFootnotes(footnoteDocument)[0]), Is.EqualTo("separator"));
        Assert.That(notes.GetEndnoteType(notes.GetEndnotes(endnoteDocument)[0]), Is.EqualTo("separator"));
        Assert.That(notes.DeleteFootnote(footnoteDocument, "2"), Is.True);
        Assert.That(notes.DeleteEndnote(endnoteDocument, "3"), Is.True);
        Assert.That(notes.DeleteFootnote(footnoteDocument, "missing"), Is.False);
        Assert.That(notes.DeleteEndnote(endnoteDocument, "missing"), Is.False);
        Assert.That(notes.FindFootnote(footnoteDocument, "2"), Is.Null);
        Assert.That(notes.FindEndnote(endnoteDocument, "3"), Is.Null);
        Assert.That(notes.GetFootnotes(footnoteDocument).Count, Is.EqualTo(1));
        Assert.That(notes.GetEndnotes(endnoteDocument).Count, Is.EqualTo(1));
    }

    static global::docx.DocxDocument RoundTripDocument(global::docx.IDocxDocumentPersistence persistence, global::docx.DocxDocument document)
    {
        MemoryStream stream = new MemoryStream();
        persistence.SaveDocument(document, stream, new global::docx.DocxSaveOptions());
        stream.Position = 0;
        return persistence.OpenDocument(stream, new global::docx.DocxLoadOptions());
    }

    static global::docx.DocxPackage CreatePackageWithFootnotes()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxRelationshipSet rootRelationships = global::docx.DocxPackage.CreateRootRelationshipSet("/word/document.xml");
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        documentRelationships.CreateRelationshipWithId("rFootnotes", global::docx.DocxConstants.RelationshipTypeFootnotes, "footnotes.xml", false);

        package.ContentTypes.SetDefault("rels", global::docx.DocxConstants.ContentTypeRelationships);
        package.ContentTypes.SetDefault("xml", global::docx.DocxConstants.ContentTypeXml);
        package.AddXmlPartWithContent("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, global::docx.DocxDocument.CreateMinimalMainDocumentXml());
        package.AddXmlPartWithContent("/word/footnotes.xml", global::docx.DocxConstants.ContentTypeFootnotes, "<w:footnotes xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:footnote w:id=\"-1\" w:type=\"separator\"><w:p><w:r><w:t>Separator</w:t></w:r></w:p></w:footnote><w:footnote w:id=\"2\"><w:p><w:r><w:t>Existing</w:t></w:r></w:p></w:footnote></w:footnotes>");
        package.AddXmlPartWithContent("_rels/.rels", global::docx.DocxConstants.ContentTypeRelationships, rootRelationships.WriteXml());
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, documentRelationships.WriteXml());

        return package;
    }

    static global::docx.DocxPackage CreatePackageWithEndnotes()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxRelationshipSet rootRelationships = global::docx.DocxPackage.CreateRootRelationshipSet("/word/document.xml");
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        documentRelationships.CreateRelationshipWithId("rEndnotes", global::docx.DocxConstants.RelationshipTypeEndnotes, "endnotes.xml", false);

        package.ContentTypes.SetDefault("rels", global::docx.DocxConstants.ContentTypeRelationships);
        package.ContentTypes.SetDefault("xml", global::docx.DocxConstants.ContentTypeXml);
        package.AddXmlPartWithContent("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, global::docx.DocxDocument.CreateMinimalMainDocumentXml());
        package.AddXmlPartWithContent("/word/endnotes.xml", global::docx.DocxConstants.ContentTypeEndnotes, "<w:endnotes xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:endnote w:id=\"-1\" w:type=\"separator\"><w:p><w:r><w:t>Separator</w:t></w:r></w:p></w:endnote><w:endnote w:id=\"3\"><w:p><w:r><w:t>Existing</w:t></w:r></w:p></w:endnote></w:endnotes>");
        package.AddXmlPartWithContent("_rels/.rels", global::docx.DocxConstants.ContentTypeRelationships, rootRelationships.WriteXml());
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, documentRelationships.WriteXml());

        return package;
    }
}
