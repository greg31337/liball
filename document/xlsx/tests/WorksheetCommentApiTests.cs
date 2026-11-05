using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorksheetCommentApiTests
{
    [Test]
    public void AddComment_CreatesCellAuthorCommentAndLegacyDrawing()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();

        XlsxWorksheetCommentResult result = part.AddComment("C3", " Ada ", "Hello note");
        XlsxCommentsPartWriteResult commentsWrite = new XlsxCommentsPartWriter().WriteString(part.CommentsPart);
        XlsxLegacyCommentDrawingWriteResult drawingWrite = new XlsxLegacyCommentDrawingWriter().WriteString(part.LegacyCommentDrawingPart);
        XlsxCommentsPart commentsRoundTrip = new XlsxCommentsPartReader().ReadString(commentsWrite.Xml);
        XlsxLegacyCommentDrawingPart drawingRoundTrip = new XlsxLegacyCommentDrawingReader().ReadString(drawingWrite.Xml);
        commentsRoundTrip.ApplyLegacyDrawing(drawingRoundTrip);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Reference, Is.EqualTo("C3"));
        Assert.That(result.Row, Is.EqualTo(3));
        Assert.That(result.Column, Is.EqualTo(3));
        Assert.That(result.CreatedRow, Is.True);
        Assert.That(result.CreatedCell, Is.True);
        Assert.That(part.GetCell("C3"), Is.Not.Null);
        Assert.That(part.HasCommentsPart, Is.True);
        Assert.That(part.HasLegacyCommentDrawingPart, Is.True);
        Assert.That(part.CommentsPart.Authors.Count, Is.EqualTo(1));
        Assert.That(part.CommentsPart.Authors[0], Is.EqualTo("Ada"));
        Assert.That(part.CommentsPart.Comments.Count, Is.EqualTo(1));
        Assert.That(part.CommentsPart.GetComment("C3")!.Text, Is.EqualTo("Hello note"));
        Assert.That(part.CommentsPart.GetComment("C3")!.Author, Is.EqualTo("Ada"));
        Assert.That(part.CommentsPart.GetComment("C3")!.AuthorId, Is.EqualTo(0));
        Assert.That(part.CommentsPart.GetComment("C3")!.HasShapeId, Is.True);
        Assert.That(part.LegacyCommentDrawingPart.Drawings.Count, Is.EqualTo(1));
        Assert.That(part.LegacyCommentDrawingPart.Drawings[0].Reference, Is.EqualTo("C3"));
        Assert.That(part.LegacyCommentDrawingPart.Drawings[0].Row, Is.EqualTo(2));
        Assert.That(part.LegacyCommentDrawingPart.Drawings[0].Column, Is.EqualTo(2));
        Assert.That(part.LegacyCommentDrawingPart.Drawings[0].HasVisible, Is.True);
        Assert.That(part.LegacyCommentDrawingPart.Drawings[0].Visible, Is.False);
        Assert.That(commentsWrite.IsValid, Is.True);
        Assert.That(commentsWrite.Xml, Does.Contain("<author>Ada</author>"));
        Assert.That(commentsWrite.Xml, Does.Contain("<comment ref=\"C3\" authorId=\"0\""));
        Assert.That(commentsWrite.Xml, Does.Contain("<t>Hello note</t>"));
        Assert.That(drawingWrite.IsValid, Is.True);
        Assert.That(drawingWrite.Xml, Does.Contain("<x:Row>2</x:Row>"));
        Assert.That(drawingWrite.Xml, Does.Contain("<x:Column>2</x:Column>"));
        Assert.That(commentsRoundTrip.GetComment("C3")!.HasPosition, Is.True);
        Assert.That(commentsRoundTrip.GetComment("C3")!.Visible, Is.False);
    }

    [Test]
    public void AddComment_ReusesAuthorsAndReplacesExistingCellComment()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();

        XlsxWorksheetCommentResult first = part.AddComment("A1", "Ada", "First");
        XlsxWorksheetCommentResult second = part.AddComment("B2", "Ada", "Second");
        XlsxWorksheetCommentResult replacement = part.AddCommentWithVisibility("A1", "Bob", "Updated", true);

        Assert.That(first.IsValid, Is.True);
        Assert.That(first.CreatedAuthor, Is.True);
        Assert.That(second.IsValid, Is.True);
        Assert.That(second.CreatedAuthor, Is.False);
        Assert.That(replacement.IsValid, Is.True);
        Assert.That(replacement.CreatedAuthor, Is.True);
        Assert.That(replacement.ReplacedComment, Is.True);
        Assert.That(replacement.CreatedComment, Is.False);
        Assert.That(part.CommentsPart.Authors.Count, Is.EqualTo(2));
        Assert.That(part.CommentsPart.Comments.Count, Is.EqualTo(2));
        Assert.That(part.CommentsPart.GetComment("A1")!.Text, Is.EqualTo("Updated"));
        Assert.That(part.CommentsPart.GetComment("A1")!.Author, Is.EqualTo("Bob"));
        Assert.That(part.CommentsPart.GetComment("A1")!.AuthorId, Is.EqualTo(1));
        Assert.That(part.CommentsPart.GetComment("A1")!.Visible, Is.True);
        Assert.That(part.LegacyCommentDrawingPart.Drawings.Count, Is.EqualTo(2));
        Assert.That(part.LegacyCommentDrawingPart.Drawings[0].Reference, Is.EqualTo("A1"));
        Assert.That(part.LegacyCommentDrawingPart.Drawings[0].Visible, Is.True);
        Assert.That(part.LegacyCommentDrawingPart.Drawings[1].Reference, Is.EqualTo("B2"));
    }

    [Test]
    public void AddComment_ClearsLoadedRawXmlBeforeRegeneratingModifiedParts()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.CommentsPart = new XlsxCommentsPartReader().ReadString("<comments xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><authors><author>Ada</author></authors><commentList><comment ref=\"A1\" authorId=\"0\"><text><t>Loaded</t></text></comment></commentList></comments>");
        part.HasCommentsPart = true;
        part.LegacyCommentDrawingPart = new XlsxLegacyCommentDrawingReader().ReadString("<xml xmlns:v=\"urn:schemas-microsoft-com:vml\" xmlns:x=\"urn:schemas-microsoft-com:office:excel\"><v:shape id=\"_x0000_s1025\"><x:ClientData ObjectType=\"Note\"><x:Row>0</x:Row><x:Column>0</x:Column></x:ClientData></v:shape></xml>");
        part.HasLegacyCommentDrawingPart = true;

        XlsxWorksheetCommentResult result = part.AddComment("B2", "Bob", "New");
        XlsxCommentsPartWriteResult commentsWrite = new XlsxCommentsPartWriter().WriteString(part.CommentsPart);
        XlsxLegacyCommentDrawingWriteResult drawingWrite = new XlsxLegacyCommentDrawingWriter().WriteString(part.LegacyCommentDrawingPart);

        Assert.That(result.IsValid, Is.True);
        Assert.That(part.CommentsPart.RawXml, Is.EqualTo(""));
        Assert.That(part.LegacyCommentDrawingPart.RawXml, Is.EqualTo(""));
        Assert.That(commentsWrite.Xml, Does.Contain("<author>Bob</author>"));
        Assert.That(commentsWrite.Xml, Does.Contain("ref=\"B2\""));
        Assert.That(drawingWrite.Xml, Does.Contain("<x:Row>1</x:Row>"));
        Assert.That(drawingWrite.Xml, Does.Contain("<x:Column>1</x:Column>"));
    }

    [Test]
    public void AddComment_ReportsInvalidInputsAndNullLists()
    {
        XlsxWorksheetPart invalidReference = new XlsxWorksheetPart();
        XlsxWorksheetPart missingAuthor = new XlsxWorksheetPart();
        XlsxWorksheetPart missingText = new XlsxWorksheetPart();
        XlsxWorksheetPart nullAuthors = new XlsxWorksheetPart();
        XlsxWorksheetPart nullComments = new XlsxWorksheetPart();
        XlsxWorksheetPart nullDrawings = new XlsxWorksheetPart();

        nullAuthors.CommentsPart.Authors = null!;
        nullComments.CommentsPart.Comments = null!;
        nullDrawings.LegacyCommentDrawingPart.Drawings = null!;

        XlsxWorksheetCommentResult invalidReferenceResult = invalidReference.AddComment("XFE1", "Ada", "Bad");
        XlsxWorksheetCommentResult missingAuthorResult = missingAuthor.AddComment("A1", " ", "Bad");
        XlsxWorksheetCommentResult? missingTextResult = missingText.AddComment("A1", "Ada", null!);
        XlsxWorksheetCommentResult nullAuthorsResult = nullAuthors.AddComment("A1", "Ada", "Bad");
        XlsxWorksheetCommentResult nullCommentsResult = nullComments.AddComment("A1", "Ada", "Bad");
        XlsxWorksheetCommentResult nullDrawingsResult = nullDrawings.AddComment("A1", "Ada", "Bad");

        Assert.That(invalidReferenceResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidReferenceResult, "worksheet.commentReferenceInvalid"), Is.True);
        Assert.That(HasDiagnostic(invalidReferenceResult, "a1.cellReference.columnRange"), Is.True);
        Assert.That(missingAuthorResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingAuthorResult, "worksheet.commentAuthorMissing"), Is.True);
        Assert.That(missingTextResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingTextResult, "worksheet.commentTextMissing"), Is.True);
        Assert.That(nullAuthorsResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullAuthorsResult, "worksheet.commentAuthorsNull"), Is.True);
        Assert.That(nullCommentsResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullCommentsResult, "worksheet.commentsNull"), Is.True);
        Assert.That(nullDrawingsResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullDrawingsResult, "worksheet.commentDrawingsNull"), Is.True);
    }

    static bool HasDiagnostic(XlsxWorksheetCommentResult result, string code)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
