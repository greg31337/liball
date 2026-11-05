using NUnit.Framework;
using System.IO;

namespace docx.Tests;

[TestFixture]
public class DocxCommentsTests
{
    [Test]
    public void OpenPackage_DiscoversAndParsesCommentsPart()
    {
        global::docx.DocxPackage package = CreatePackageWithComments(CreateCommentsXml());

        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);
        global::docx.DocxComment firstComment = document.FindCommentById("1")!;

        Assert.That(package.HasCommentsPart(), Is.True);
        Assert.That(package.GetCommentsPartName(), Is.EqualTo("/word/comments.xml"));
        Assert.That(package.GetCommentsPart()!.PartName, Is.EqualTo("/word/comments.xml"));
        Assert.That(document.HasCommentsRoot, Is.True);
        Assert.That(document.HasComments(), Is.True);
        Assert.That(document.CommentsPart, Is.Not.Null);
        Assert.That(document.CommentCount(), Is.EqualTo(2));
        Assert.That(document.GetComments().CommentCount(), Is.EqualTo(2));
        Assert.That(document.GetComments().UnknownChildCount(), Is.EqualTo(1));
        Assert.That(document.GetComments().CountCommentsByAuthor("Alice"), Is.EqualTo(1));
        Assert.That(firstComment.Id, Is.EqualTo("1"));
        Assert.That(firstComment.Author, Is.EqualTo("Alice"));
        Assert.That(firstComment.Initials, Is.EqualTo("AL"));
        Assert.That(firstComment.Date, Is.EqualTo("2026-08-03T12:00:00Z"));
        Assert.That(firstComment.BlockCount(), Is.EqualTo(1));
        Assert.That(firstComment.ParagraphCount(), Is.EqualTo(1));
        Assert.That(firstComment.TableCount(), Is.EqualTo(0));
        Assert.That(firstComment.GetBlock(0).Paragraph!.GetText(), Is.EqualTo("Comment text"));
        Assert.That(document.FindCommentById("missing"), Is.Null);
    }

    [Test]
    public void OpenPackage_ReturnsEmptyCommentsWhenRelationshipIsMissing()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();

        Assert.That(document.Package.FindCommentsRelationship(), Is.Null);
        Assert.That(document.Package.GetCommentsPartName(), Is.EqualTo(""));
        Assert.That(document.Package.GetCommentsPart(), Is.Null);
        Assert.That(document.Package.HasCommentsPart(), Is.False);
        Assert.That(document.HasCommentsRoot, Is.False);
        Assert.That(document.HasComments(), Is.False);
        Assert.That(document.CommentCount(), Is.EqualTo(0));
    }

    [Test]
    public void CommentsParsing_PreservesRawXmlAndUnknownChildren()
    {
        global::docx.DocxComments comments = global::docx.DocxComments.ParseXml(CreateCommentsXml());
        global::docx.DocxComment comment = comments.FindCommentById("1")!;

        Assert.That(comments.GetXml(), Does.Contain("<w:comment"));
        Assert.That(comments.WriteXml(), Does.Contain("Comment text"));
        Assert.That(comments.UnknownChildCount(), Is.EqualTo(1));
        Assert.That(comment.GetXml(), Does.Contain("Comment text"));
        Assert.That(comment.WriteXml(), Does.Contain("Comment text"));
        Assert.That(comment.HasXml(), Is.True);
    }

    [Test]
    public void CommentsTextExtraction_ReadsParagraphAndTableBlocks()
    {
        global::docx.DocxComments comments = global::docx.DocxComments.ParseXml("<w:comments xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:comment w:id=\"4\" w:author=\"Alice\"><w:p><w:r><w:t>Intro</w:t></w:r></w:p><w:tbl><w:tr><w:tc><w:p><w:r><w:t>A</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t>B</w:t></w:r></w:p></w:tc></w:tr></w:tbl></w:comment></w:comments>");
        global::docx.DocxComment comment = comments.FindCommentById("4")!;
        global::docx.DocxTextOptions options = new global::docx.DocxTextOptions();

        options.ParagraphSeparator = "|";
        options.TableCellSeparator = ",";

        Assert.That(comment.BlockCount(), Is.EqualTo(2));
        Assert.That(comment.ParagraphCount(), Is.EqualTo(1));
        Assert.That(comment.TableCount(), Is.EqualTo(1));
        Assert.That(comment.GetTextWithOptions(options), Is.EqualTo("Intro|A,B"));
        Assert.That(comments.GetTextWithOptions(options), Is.EqualTo("Intro|A,B"));
    }

    [Test]
    public void CommentsParsing_RejectsInvalidArguments()
    {
        global::docx.DocxComments comments = global::docx.DocxComments.ParseXml(CreateCommentsXml());
        global::docx.DocxComment comment = comments.GetComment(0);
        global::docx.DocxXmlReader rootReader = global::docx.DocxXmlReader.Create("<root />");

        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxComments(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxComments.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxComments.ParseXml(null!));
        Assert.Throws<ArgumentNullException>(() => comments.FindCommentById(null!));
        Assert.Throws<ArgumentNullException>(() => comments.CountCommentsByAuthor(null!));
        Assert.Throws<ArgumentNullException>(() => comments.IsCommentBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => comments.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxComment(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxComment.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxComment.ParseXml(null!));
        Assert.Throws<ArgumentNullException>(() => comment.WriteTo(null!));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxComments.Parse(rootReader));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxComments.ParseXml("<root />"));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxComment.ParseXml("<root />"));
        Assert.Throws<InvalidDataException>(() => global::docx.DocxDocument.OpenPackage(CreatePackageWithComments("<w:notComments xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />")));
    }

    static global::docx.DocxPackage CreatePackageWithComments(string commentsXml)
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxRelationshipSet rootRelationships = global::docx.DocxPackage.CreateRootRelationshipSet("/word/document.xml");
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        documentRelationships.CreateRelationshipWithId("rComments", global::docx.DocxConstants.RelationshipTypeComments, "comments.xml", false);
        documentRelationships.CreateRelationshipWithId("rExternalComments", global::docx.DocxConstants.RelationshipTypeComments, "https://example.com/comments.xml", true);

        package.ContentTypes.SetDefault("rels", global::docx.DocxConstants.ContentTypeRelationships);
        package.ContentTypes.SetDefault("xml", global::docx.DocxConstants.ContentTypeXml);
        package.AddXmlPartWithContent("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, global::docx.DocxDocument.CreateMinimalMainDocumentXml());
        package.AddXmlPartWithContent("/word/comments.xml", global::docx.DocxConstants.ContentTypeComments, commentsXml);
        package.AddXmlPartWithContent("_rels/.rels", global::docx.DocxConstants.ContentTypeRelationships, rootRelationships.WriteXml());
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, documentRelationships.WriteXml());

        return package;
    }

    static string CreateCommentsXml()
    {
        return string.Join(
            "",
            new string[]
            {
                "<w:comments xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">",
                "<w:comment w:id=\"1\" w:author=\"Alice\" w:initials=\"AL\" w:date=\"2026-08-03T12:00:00Z\"><w:p><w:r><w:t>Comment text</w:t></w:r></w:p></w:comment>",
                "<w:comment w:id=\"2\" w:author=\"Bob\"><w:p><w:r><w:t>Second comment</w:t></w:r></w:p></w:comment>",
                "<w:custom />",
                "</w:comments>"
            });
    }
}
