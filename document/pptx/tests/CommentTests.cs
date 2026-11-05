using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class CommentTests
{
    [Test]
    public void Presentation_LoadsCommentAuthorsAndLegacyComments()
    {
        PptxPresentation presentation = LoadCommentPresentation();
        List<PptxCommentAuthor> authors = presentation.GetCommentAuthors();
        List<PptxComment> comments = presentation.GetSlide(0).GetComments();
        PptxCommentAuthor author = authors[0];
        PptxComment comment = comments[0];

        Assert.That(authors.Count, Is.EqualTo(1));
        Assert.That(author.Id, Is.EqualTo(0));
        Assert.That(author.Name, Is.EqualTo("Ada Lovelace"));
        Assert.That(author.Initials, Is.EqualTo("AL"));
        Assert.That(author.LastIndex, Is.EqualTo(4));
        Assert.That(author.ColorIndex, Is.EqualTo(2));
        Assert.That(comments.Count, Is.EqualTo(1));
        Assert.That(comment.AuthorId, Is.EqualTo(0));
        Assert.That(comment.Index, Is.EqualTo(4));
        Assert.That(comment.DateTime, Is.EqualTo("2026-01-01T00:00:00Z"));
        Assert.That(comment.Text, Is.EqualTo("Existing Comment"));
        Assert.That(comment.X, Is.EqualTo(11L));
        Assert.That(comment.Y, Is.EqualTo(22L));
        Assert.That(presentation.ExtractCommentsText(0), Is.EqualTo("Existing Comment"));
    }

    [Test]
    public void LegacyCommentPartsPreserveUnknownXmlOnRoundTripAndEdit()
    {
        PptxPresentation presentation = LoadCommentPresentation();
        PptxPresentation preserved = SaveReopenAndLoad(presentation);

        Assert.That(preserved.Package.GetPart("/ppt/comments/comment1.xml").GetBytes(), Is.EqualTo(Encoding.UTF8.GetBytes(CreateCommentsXml())));

        PptxCommentAuthor author = preserved.GetCommentAuthors()[0];
        PptxComment comment = preserved.GetSlide(0).GetComments()[0];
        author.SetName("Grace Hopper");
        author.SetInitials("GH");
        comment.SetText("Updated Comment");
        comment.SetPosition(33L, 44L);
        PptxPresentation reopened = SaveReopenAndLoad(preserved);
        PptxCommentAuthor reopenedAuthor = reopened.GetCommentAuthors()[0];
        PptxComment reopenedComment = reopened.GetSlide(0).GetComments()[0];

        Assert.That(reopenedAuthor.Name, Is.EqualTo("Grace Hopper"));
        Assert.That(reopenedAuthor.Initials, Is.EqualTo("GH"));
        Assert.That(reopenedComment.Text, Is.EqualTo("Updated Comment"));
        Assert.That(reopenedComment.X, Is.EqualTo(33L));
        Assert.That(reopenedComment.Y, Is.EqualTo(44L));
        Assert.That(Encoding.UTF8.GetString(reopened.Package.GetPart("/ppt/comments/comment1.xml").GetBytes()), Does.Contain("Preserved Comment Extension"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Slide_AddCommentCreatesAuthorAndCommentParts()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        PptxPresentation presentation = document.LoadPresentation();
        PptxCommentAuthor author = presentation.AddCommentAuthor("Alan Turing", "AT");
        PptxComment comment = presentation.GetSlide(0).AddCommentWithDetails(author.Id, "New Comment", "2026-02-03T04:05:06Z", 100L, 200L);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxCommentAuthor reopenedAuthor = reopened.GetCommentAuthors()[0];
        PptxComment reopenedComment = reopened.GetSlide(0).GetComments()[0];
        PptxRelationship authorRelationship = FindRelationship(reopened.Package.GetRelationships(PptxPartNames.Presentation), PptxRelationshipTypes.CommentAuthors);
        PptxRelationship commentsRelationship = FindRelationship(reopened.Package.GetRelationships(PptxPartNames.FirstSlide), PptxRelationshipTypes.Comments);

        Assert.That(comment.Index, Is.EqualTo(0));
        Assert.That(reopenedAuthor.Name, Is.EqualTo("Alan Turing"));
        Assert.That(reopenedAuthor.Initials, Is.EqualTo("AT"));
        Assert.That(reopenedAuthor.LastIndex, Is.EqualTo(0));
        Assert.That(reopenedComment.AuthorId, Is.EqualTo(author.Id));
        Assert.That(reopenedComment.Text, Is.EqualTo("New Comment"));
        Assert.That(reopenedComment.DateTime, Is.EqualTo("2026-02-03T04:05:06Z"));
        Assert.That(reopenedComment.X, Is.EqualTo(100L));
        Assert.That(reopenedComment.Y, Is.EqualTo(200L));
        Assert.That(authorRelationship.TargetPartName, Is.EqualTo("/ppt/commentAuthors.xml"));
        Assert.That(commentsRelationship.TargetPartName, Is.EqualTo("/ppt/comments/comment1.xml"));
        Assert.That(reopened.ExtractCommentsText(0), Is.EqualTo("New Comment"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void CommentOperations_LargeCoordinatesRoundTripAsLongValues()
    {
        PptxPresentation presentation = PptxDocument.CreateMinimumPresentation().LoadPresentation();
        PptxCommentAuthor author = presentation.AddCommentAuthor("Large Position", "LP");
        PptxComment comment = presentation.GetSlide(0).AddComment(author.Id, "Positioned comment");
        PptxCommentOperations operations = new PptxCommentOperations();

        operations.SetPosition(comment, 3000000000L, -3000000001L);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxComment actual = reopened.GetSlide(0).GetComments()[0];

        Assert.That(actual.X, Is.EqualTo(3000000000L));
        Assert.That(actual.Y, Is.EqualTo(-3000000001L));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Comment_DeleteRemovesLegacyComment()
    {
        PptxPresentation presentation = LoadCommentPresentation();
        PptxComment comment = presentation.GetSlide(0).GetComments()[0];

        comment.Delete();
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        Assert.That(reopened.GetSlide(0).GetComments().Count, Is.EqualTo(0));
        Assert.That(reopened.ExtractCommentsText(0), Is.EqualTo(""));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void CommentAuthor_ColorAndRemovalLifecycleProtectsReferencesAndCleansEmptyPart()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        PptxPresentation presentation = document.LoadPresentation();
        PptxCommentAuthor referenced = presentation.AddCommentAuthor("Referenced", "R");
        PptxCommentAuthor unused = presentation.AddCommentAuthor("Unused", "U");
        PptxComment comment = presentation.GetSlide(0).AddComment(referenced.Id, "Keep the author");
        PptxCommentAuthorOperations operations = new PptxCommentAuthorOperations();

        operations.SetColorIndex(referenced, 7);

        Assert.That(referenced.ColorIndex, Is.EqualTo(7));
        Assert.That(() => operations.Remove(referenced), Throws.ArgumentException);
        Assert.That(operations.Remove(unused), Is.True);
        Assert.That(presentation.GetCommentAuthors().Count, Is.EqualTo(1));

        comment.Delete();
        Assert.That(operations.Remove(referenced), Is.True);
        Assert.That(operations.Remove(referenced), Is.False);

        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        Assert.That(reopened.GetCommentAuthors().Count, Is.EqualTo(0));
        Assert.That(reopened.Package.HasPart("/ppt/commentAuthors.xml"), Is.False);
        Assert.That(HasRelationship(reopened.Package.GetRelationships(PptxPartNames.Presentation), PptxRelationshipTypes.CommentAuthors), Is.False);
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    static PptxPresentation LoadCommentPresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.AddPart("/ppt/commentAuthors.xml", PptxContentTypes.CommentAuthors, Encoding.UTF8.GetBytes(CreateCommentAuthorsXml()));
        package.AddPart("/ppt/comments/comment1.xml", PptxContentTypes.Comments, Encoding.UTF8.GetBytes(CreateCommentsXml()));
        package.AddRelationshipWithId(PptxPartNames.Presentation, "rIdAuthors", PptxRelationshipTypes.CommentAuthors, "commentAuthors.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdComments", PptxRelationshipTypes.Comments, "../comments/comment1.xml", PptxRelationshipTargetMode.Internal);
        PptxDocument document = PptxDocument.OpenBytes(package.SaveBytes());
        return document.LoadPresentation();
    }

    static PptxPresentation SaveReopenAndLoad(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        byte[] bytes = document.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        return reopened.LoadPresentation();
    }

    static string CreateCommentAuthorsXml()
    {
        return
            "<p:cmAuthorLst xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cmAuthor id=\"0\" name=\"Ada Lovelace\" initials=\"AL\" lastIdx=\"4\" clrIdx=\"2\"/>" +
            "</p:cmAuthorLst>";
    }

    static string CreateCommentsXml()
    {
        return
            "<p:cmLst xmlns:p=\"" + PptxNamespaces.Presentation + "\" xmlns:x=\"http://example.com/comments\">" +
            "<p:cm authorId=\"0\" dt=\"2026-01-01T00:00:00Z\" idx=\"4\">" +
            "<p:pos x=\"11\" y=\"22\"/>" +
            "<p:text>Existing Comment</p:text>" +
            "<p:extLst><p:ext uri=\"preserve\"><x:unknown>Preserved Comment Extension</x:unknown></p:ext></p:extLst>" +
            "</p:cm>" +
            "</p:cmLst>";
    }

    static PptxRelationship FindRelationship(PptxRelationshipCollection relationships, string type)
    {
        for (int index = 0; index < relationships.Relationships.Count; index++)
        {
            if (relationships.Relationships[index].Type == type)
            {
                return relationships.Relationships[index];
            }
        }

        throw new System.ArgumentException("Relationship was not found.");
    }

    static bool HasRelationship(PptxRelationshipCollection relationships, string type)
    {
        for (int index = 0; index < relationships.Relationships.Count; index++)
        {
            if (relationships.Relationships[index].Type == type)
            {
                return true;
            }
        }

        return false;
    }
}
