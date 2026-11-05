using System.IO;
using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxCommentCreationTests
{
    [Test]
    public void CreateComment_CreatesPartRelationshipAndPersistsComment()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();

        global::docx.DocxComment first = document.CreateComment("Alice", "AL", "First & <comment>");
        global::docx.DocxComment second = document.CreateCommentWithId("7", "Bob", "BO", "2026-08-03T12:00:00Z", "Explicit comment");
        global::docx.DocxRelationshipSet relationships = document.Package.FindRelationshipSet("/word/document.xml")!;
        global::docx.DocxRelationship relationship = relationships.FindById("rId1")!;
        string xml = document.Package.FindPart("/word/comments.xml")!.GetText();
        global::docx.DocxDocument reopened = global::docx.DocxDocument.OpenPackage(document.Package);

        Assert.That(first.Id, Is.EqualTo("1"));
        Assert.That(second.Id, Is.EqualTo("7"));
        Assert.That(first.Author, Is.EqualTo("Alice"));
        Assert.That(first.Initials, Is.EqualTo("AL"));
        Assert.That(second.Date, Is.EqualTo("2026-08-03T12:00:00Z"));
        Assert.That(first.GetBlock(0).Paragraph!.GetText(), Is.EqualTo("First & <comment>"));
        Assert.That(document.HasComments(), Is.True);
        Assert.That(document.CommentCount(), Is.EqualTo(2));
        Assert.That(document.CommentsPart!.PartName, Is.EqualTo("/word/comments.xml"));
        Assert.That(relationship.Type, Is.EqualTo(global::docx.DocxConstants.RelationshipTypeComments));
        Assert.That(relationship.Target, Is.EqualTo("comments.xml"));
        Assert.That(document.Package.FindPart("/word/_rels/document.xml.rels")!.GetText(), Does.Contain(global::docx.DocxConstants.RelationshipTypeComments));
        Assert.That(xml, Does.Contain("First &amp; &lt;comment&gt;"));
        Assert.That(xml, Does.Contain("Explicit comment"));
        Assert.That(reopened.CommentCount(), Is.EqualTo(2));
        Assert.That(reopened.FindCommentById("1")!.GetBlock(0).Paragraph!.GetText(), Is.EqualTo("First & <comment>"));
        Assert.That(reopened.FindCommentById("7")!.Author, Is.EqualTo("Bob"));
    }

    [Test]
    public void CreateComment_AppendsToExistingCommentsAndAllocatesNextPositiveId()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(CreatePackageWithComments());

        global::docx.DocxComment created = document.CreateComment("Carol", "CA", "Created comment");

        Assert.That(created.Id, Is.EqualTo("3"));
        Assert.That(document.CommentCount(), Is.EqualTo(2));
        Assert.That(document.Package.FindPart("/word/comments.xml")!.GetText(), Does.Contain("Existing"));
        Assert.That(document.Package.FindPart("/word/comments.xml")!.GetText(), Does.Contain("Created comment"));
    }

    [Test]
    public void CommentIdArithmetic_RejectsValuesOutsideTheSupportedRange()
    {
        global::docx.DocxComments comments = global::docx.DocxComments.Create();

        Assert.That(global::docx.DocxComments.IsNonNegativeInteger("2147483648"), Is.False);
        Assert.Throws<ArgumentException>(() => global::docx.DocxComments.ParseNonNegativeInteger("2147483648"));

        comments.CreateCommentWithId("2147483647", "Alice", "AL", "", "Last comment");

        Assert.Throws<InvalidOperationException>(() => comments.AllocateCommentId());
    }

    [Test]
    public void AddCommentRange_InsertsRangeMarkersAndReferenceRun()
    {
        global::docx.DocxComment comment = global::docx.DocxComment.Create("5", "Alice", "AL", "", "Comment body");
        global::docx.DocxParagraph paragraph = ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t>Target</w:t></w:r></w:p>");

        global::docx.DocxRun referenceRun = paragraph.AddCommentRange(comment);
        global::docx.DocxParagraph written = ParseParagraph(paragraph.WriteXml());

        Assert.That(referenceRun.CountUnknownElements("commentReference", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.EqualTo(1));
        Assert.That(paragraph.ChildCount(), Is.EqualTo(4));
        Assert.That(paragraph.RunCount(), Is.EqualTo(2));
        Assert.That(paragraph.GetChildXml(0), Does.Contain("commentRangeStart"));
        Assert.That(paragraph.GetChildXml(2), Does.Contain("commentRangeEnd"));
        Assert.That(paragraph.GetChildXml(3), Does.Contain("commentReference"));
        Assert.That(written.UnknownElementCount(), Is.EqualTo(2));
        Assert.That(written.CountUnknownElements("commentRangeStart", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.EqualTo(1));
        Assert.That(written.CountUnknownElements("commentRangeEnd", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.EqualTo(1));
        Assert.That(written.GetRun(1).CountUnknownElements("commentReference", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.EqualTo(1));
        Assert.That(written.WriteXml(), Does.Contain("w:id=\"5\""));
    }

    [Test]
    public void CommentCreationAndRangeInsertion_RejectInvalidArguments()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        global::docx.DocxComments comments = global::docx.DocxComments.Create();
        global::docx.DocxComment comment = new global::docx.DocxComment("");
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");
        global::docx.DocxXmlWriter writer = global::docx.DocxXmlWriter.Create();

        Assert.Throws<ArgumentNullException>(() => document.CreateComment(null!, "AL", "text"));
        Assert.Throws<ArgumentNullException>(() => document.CreateComment("Alice", null!, "text"));
        Assert.Throws<ArgumentNullException>(() => document.CreateComment("Alice", "AL", null!));
        Assert.Throws<ArgumentNullException>(() => document.CreateCommentWithId(null!, "Alice", "AL", "", "text"));
        Assert.Throws<ArgumentNullException>(() => document.CreateCommentWithId("1", null!, "AL", "", "text"));
        Assert.Throws<ArgumentNullException>(() => document.CreateCommentWithId("1", "Alice", null!, "", "text"));
        Assert.Throws<ArgumentNullException>(() => document.CreateCommentWithId("1", "Alice", "AL", null!, "text"));
        Assert.Throws<ArgumentNullException>(() => document.CreateCommentWithId("1", "Alice", "AL", "", null!));
        Assert.Throws<ArgumentException>(() => document.CreateCommentWithId("", "Alice", "AL", "", "text"));
        Assert.Throws<InvalidOperationException>(() => new global::docx.DocxDocument(global::docx.DocxPackage.Create()).WriteCommentsPart());

        Assert.Throws<ArgumentNullException>(() => comments.CreateComment(null!, "AL", "text"));
        Assert.Throws<ArgumentNullException>(() => comments.CreateComment("Alice", null!, "text"));
        Assert.Throws<ArgumentNullException>(() => comments.CreateComment("Alice", "AL", null!));
        Assert.Throws<ArgumentNullException>(() => comments.CreateCommentWithId(null!, "Alice", "AL", "", "text"));
        Assert.Throws<ArgumentNullException>(() => comments.CreateCommentWithId("1", null!, "AL", "", "text"));
        Assert.Throws<ArgumentNullException>(() => comments.CreateCommentWithId("1", "Alice", null!, "", "text"));
        Assert.Throws<ArgumentNullException>(() => comments.CreateCommentWithId("1", "Alice", "AL", null!, "text"));
        Assert.Throws<ArgumentNullException>(() => comments.CreateCommentWithId("1", "Alice", "AL", "", null!));
        Assert.Throws<ArgumentNullException>(() => comments.AddComment(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxComments.IsNonNegativeInteger(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxComments.ParseNonNegativeInteger(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxComment.Create(null!, "Alice", "AL", "", "text"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxComment.Create("1", null!, "AL", "", "text"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxComment.Create("1", "Alice", null!, "", "text"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxComment.Create("1", "Alice", "AL", null!, "text"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxComment.Create("1", "Alice", "AL", "", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxComment.CreateTextParagraph(null!));
        Assert.Throws<ArgumentNullException>(() => comment.AddParagraph(null!));
        Assert.Throws<ArgumentNullException>(() => comment.AddBlock(null!));
        Assert.Throws<ArgumentException>(() => comments.CreateCommentWithId("", "Alice", "AL", "", "text"));
        Assert.Throws<ArgumentException>(() => comments.AddComment(new global::docx.DocxComment("")));
        Assert.Throws<ArgumentException>(() => global::docx.DocxComments.ParseNonNegativeInteger("abc"));
        Assert.Throws<ArgumentException>(() => global::docx.DocxComment.Create("", "Alice", "AL", "", "text"));
        comments.CreateCommentWithId("1", "Alice", "AL", "", "First");
        Assert.Throws<InvalidOperationException>(() => comments.CreateCommentWithId("1", "Alice", "AL", "", "Duplicate"));

        Assert.Throws<ArgumentNullException>(() => paragraph.AddCommentRange((global::docx.DocxComment)null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.AddCommentRangeById((string)null!));
        Assert.Throws<ArgumentException>(() => paragraph.AddCommentRangeById(""));
        Assert.Throws<ArgumentException>(() => paragraph.AddCommentRange(new global::docx.DocxComment("")));
        Assert.Throws<ArgumentNullException>(() => paragraph.AddCommentReference((global::docx.DocxComment)null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.AddCommentReferenceById((string)null!));
        Assert.Throws<ArgumentException>(() => paragraph.AddCommentReferenceById(""));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxComment.WriteRangeMarkerTo(null!, "commentRangeStart", "1"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxComment.WriteRangeMarkerTo(writer, null!, "1"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxComment.WriteRangeMarkerTo(writer, "commentRangeStart", null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxComment.WriteRangeMarkerTo(writer, "", "1"));
        Assert.Throws<ArgumentException>(() => global::docx.DocxComment.WriteRangeMarkerTo(writer, "commentRangeStart", ""));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxComment.WriteReferenceTo(null!, "1"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxComment.WriteReferenceTo(writer, null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxComment.WriteReferenceTo(writer, ""));
    }

    [Test]
    public void CommentManagement_CreatesEnumeratesAndPreservesReviewMetadata()
    {
        global::docx.IDocxCommentManagement comments = new global::docx.DocxCommentManagement();
        global::docx.IDocxDocumentPersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.DocxDocument document = persistence.CreateDocument();
        global::docx.DocxComment first = comments.CreateCommentWithMetadata(
            document,
            "Alice",
            "AL",
            "2026-08-25T12:30:00Z",
            "Review this section");
        global::docx.DocxComment second = comments.CreateCommentWithId(
            document,
            "7",
            "Bob",
            "BO",
            "2026-08-25T13:00:00Z",
            "Second review");

        Assert.That(comments.GetComments(document), Is.EqualTo(new[] { first, second }));
        Assert.That(comments.FindComment(document, "1"), Is.SameAs(first));
        Assert.That(comments.GetCommentId(first), Is.EqualTo("1"));
        Assert.That(comments.GetCommentAuthor(first), Is.EqualTo("Alice"));
        Assert.That(comments.GetCommentInitials(first), Is.EqualTo("AL"));
        Assert.That(comments.GetCommentDate(first), Is.EqualTo("2026-08-25T12:30:00Z"));
        Assert.That(comments.GetCommentBlocks(first).Count, Is.EqualTo(1));

        global::docx.DocxDocument reopened = RoundTripDocument(persistence, document);
        global::docx.DocxComment parsed = comments.FindComment(reopened, "1")!;

        Assert.That(comments.GetComments(reopened).Count, Is.EqualTo(2));
        Assert.That(comments.GetCommentAuthor(parsed), Is.EqualTo("Alice"));
        Assert.That(comments.GetCommentInitials(parsed), Is.EqualTo("AL"));
        Assert.That(comments.GetCommentDate(parsed), Is.EqualTo("2026-08-25T12:30:00Z"));
        Assert.That(parsed.GetText(), Is.EqualTo("Review this section"));
    }

    [Test]
    public void CommentManagement_EditsContentAndReviewMetadataAcrossRoundTrip()
    {
        global::docx.IDocxCommentManagement comments = new global::docx.DocxCommentManagement();
        global::docx.IDocxDocumentPersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.IDocxContentFactory factory = new global::docx.DocxContentFactory();
        global::docx.DocxDocument document = persistence.CreateDocument();
        global::docx.DocxComment comment = comments.CreateComment(document, "Original", "OR", "Old text");

        comments.SetCommentReviewMetadata(document, comment, "Updated", "UP", "2026-08-25T14:00:00Z");
        comments.ReplaceCommentText(document, comment, "New text");
        comments.AppendCommentParagraph(document, comment, factory.CreateTextParagraph("Additional text"));

        Assert.That(comments.GetCommentBlocks(comment).Count, Is.EqualTo(2));
        Assert.That(comments.RemoveCommentBlock(document, comment, 0).GetParagraph()!.GetText(), Is.EqualTo("New text"));
        Assert.That(comment.GetText(), Is.EqualTo("Additional text"));

        global::docx.DocxDocument reopened = RoundTripDocument(persistence, document);
        global::docx.DocxComment parsed = comments.FindComment(reopened, "1")!;

        Assert.That(comments.GetCommentAuthor(parsed), Is.EqualTo("Updated"));
        Assert.That(comments.GetCommentInitials(parsed), Is.EqualTo("UP"));
        Assert.That(comments.GetCommentDate(parsed), Is.EqualTo("2026-08-25T14:00:00Z"));
        Assert.That(parsed.GetText(), Is.EqualTo("Additional text"));
    }

    [Test]
    public void CommentManagement_InsertsSelectedChildRangeAndReferenceAcrossRoundTrip()
    {
        global::docx.IDocxCommentManagement comments = new global::docx.DocxCommentManagement();
        global::docx.IDocxDocumentPersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.IDocxContentFactory factory = new global::docx.DocxContentFactory();
        global::docx.DocxDocument document = persistence.CreateDocument();
        global::docx.DocxComment comment = comments.CreateCommentWithMetadata(document, "Alice", "AL", "2026-08-25T12:30:00Z", "Review");
        global::docx.DocxParagraph paragraph = factory.CreateParagraph();

        paragraph.InsertRun(0, factory.CreateTextRun("First"));
        paragraph.InsertRun(1, factory.CreateTextRun("Second"));
        comments.InsertCommentRange(paragraph, 0, 2, comment);
        document.GetBody().AddBlock(factory.CreateParagraphBlock(paragraph));

        Assert.That(paragraph.ChildCount(), Is.EqualTo(5));
        Assert.That(comments.GetCommentRangeStartIds(paragraph), Is.EqualTo(new[] { "1" }));
        Assert.That(comments.GetCommentRangeEndIds(paragraph), Is.EqualTo(new[] { "1" }));
        Assert.That(comments.GetCommentReferenceIds(paragraph), Is.EqualTo(new[] { "1" }));
        Assert.That(paragraph.GetText(), Is.EqualTo("FirstSecond"));

        global::docx.DocxDocument reopened = RoundTripDocument(persistence, document);
        global::docx.DocxParagraph parsed = reopened.GetBody().GetParagraph(0);

        Assert.That(comments.GetCommentRangeStartIds(parsed), Is.EqualTo(new[] { "1" }));
        Assert.That(comments.GetCommentRangeEndIds(parsed), Is.EqualTo(new[] { "1" }));
        Assert.That(comments.GetCommentReferenceIds(parsed), Is.EqualTo(new[] { "1" }));
        Assert.That(parsed.GetText(), Is.EqualTo("FirstSecond"));
    }

    [Test]
    public void CommentManagement_RemovesAnchorsWithoutRemovingOtherRunContentAndDeletesDefinition()
    {
        global::docx.IDocxCommentManagement comments = new global::docx.DocxCommentManagement();
        global::docx.DocxDocument document = new global::docx.DocxPackagePersistence().CreateDocument();
        global::docx.DocxComment comment = comments.CreateComment(document, "Alice", "AL", "Review");
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph(
            "<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">"
            + "<w:commentRangeStart w:id=\"1\"/>"
            + "<w:r><w:t>Before</w:t><w:commentReference w:id=\"1\"/><w:t>After</w:t></w:r>"
            + "<w:commentRangeEnd w:id=\"1\"/>"
            + "<w:r><w:commentReference w:id=\"1\"/></w:r>"
            + "</w:p>");

        Assert.That(comments.RemoveCommentRangeMarkers(paragraph, "1"), Is.EqualTo(2));
        Assert.That(comments.RemoveCommentReferences(paragraph, "1"), Is.EqualTo(2));
        Assert.That(paragraph.ChildCount(), Is.EqualTo(1));
        Assert.That(paragraph.GetText(), Is.EqualTo("BeforeAfter"));
        Assert.That(comments.GetCommentRangeStartIds(paragraph), Is.Empty);
        Assert.That(comments.GetCommentRangeEndIds(paragraph), Is.Empty);
        Assert.That(comments.GetCommentReferenceIds(paragraph), Is.Empty);
        Assert.That(comments.DeleteComment(document, comments.GetCommentId(comment)), Is.True);
        Assert.That(comments.DeleteComment(document, "missing"), Is.False);
        Assert.That(comments.FindComment(document, "1"), Is.Null);
        Assert.That(comments.GetComments(document), Is.Empty);
    }

    static global::docx.DocxDocument RoundTripDocument(global::docx.IDocxDocumentPersistence persistence, global::docx.DocxDocument document)
    {
        MemoryStream stream = new MemoryStream();
        persistence.SaveDocument(document, stream, new global::docx.DocxSaveOptions());
        stream.Position = 0;
        return persistence.OpenDocument(stream, new global::docx.DocxLoadOptions());
    }

    static global::docx.DocxParagraph ParseParagraph(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("p", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxParagraph.Parse(reader);
    }

    static global::docx.DocxPackage CreatePackageWithComments()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxRelationshipSet rootRelationships = global::docx.DocxPackage.CreateRootRelationshipSet("/word/document.xml");
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        documentRelationships.CreateRelationshipWithId("rComments", global::docx.DocxConstants.RelationshipTypeComments, "comments.xml", false);

        package.ContentTypes.SetDefault("rels", global::docx.DocxConstants.ContentTypeRelationships);
        package.ContentTypes.SetDefault("xml", global::docx.DocxConstants.ContentTypeXml);
        package.AddXmlPartWithContent("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, global::docx.DocxDocument.CreateMinimalMainDocumentXml());
        package.AddXmlPartWithContent("/word/comments.xml", global::docx.DocxConstants.ContentTypeComments, "<w:comments xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:comment w:id=\"2\" w:author=\"Alice\"><w:p><w:r><w:t>Existing</w:t></w:r></w:p></w:comment></w:comments>");
        package.AddXmlPartWithContent("_rels/.rels", global::docx.DocxConstants.ContentTypeRelationships, rootRelationships.WriteXml());
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, documentRelationships.WriteXml());

        return package;
    }
}
