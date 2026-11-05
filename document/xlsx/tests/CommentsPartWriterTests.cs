using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class CommentsPartWriterTests
{
    [Test]
    public void WriteString_WritesConstructedCommentsPartAndRoundTrips()
    {
        XlsxCommentsPart part = new XlsxCommentsPart();
        part.Authors.Add("Ada");
        part.Authors.Add("Bob");
        part.Comments.Add(CreateComment("A1", 0, "Plain"));
        XlsxComment rich = CreateComment("C3", 1, "");
        rich.ShapeId = 1025;
        rich.HasShapeId = true;
        rich.TextRawXml = "<text><r><rPr><b /></rPr><t>Bold</t></r><t> tail</t></text>";
        part.Comments.Add(rich);

        XlsxCommentsPartWriteResult writeResult = new XlsxCommentsPartWriter().WriteString(part);
        XlsxCommentsPart roundTrip = new XlsxCommentsPartReader().ReadString(writeResult.Xml);

        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(writeResult.Xml, Does.Contain("<authors>"));
        Assert.That(writeResult.Xml, Does.Contain("<author>Ada</author>"));
        Assert.That(writeResult.Xml, Does.Contain("<comment ref=\"A1\" authorId=\"0\">"));
        Assert.That(writeResult.Xml, Does.Contain("<comment ref=\"C3\" authorId=\"1\" shapeId=\"1025\">"));
        Assert.That(writeResult.Xml, Does.Contain("<rPr><b /></rPr>"));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.Authors.Count, Is.EqualTo(2));
        Assert.That(roundTrip.Comments.Count, Is.EqualTo(2));
        Assert.That(roundTrip.GetComment("A1")!.Text, Is.EqualTo("Plain"));
        Assert.That(roundTrip.GetComment("C3")!.Text, Is.EqualTo("Bold tail"));
        Assert.That(roundTrip.GetComment("C3")!.ShapeId, Is.EqualTo(1025));
    }

    [Test]
    public void WriteString_PreservesLoadedCommentsRawXml()
    {
        string xml = "<comments xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:x=\"urn:x\" x:root=\"r\">" +
            "<authors><author>Ada</author></authors>" +
            "<commentList><comment ref=\"A1\" authorId=\"0\"><text><t>Loaded</t></text><x:future /></comment></commentList>" +
            "<x:futureRoot />" +
            "</comments>";
        XlsxCommentsPart part = new XlsxCommentsPartReader().ReadString(xml);

        XlsxCommentsPartWriteResult writeResult = new XlsxCommentsPartWriter().WriteString(part);

        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(writeResult.Xml, Is.EqualTo(xml));
    }

    [Test]
    public void WriteString_WritesConstructedLegacyDrawingAndRoundTrips()
    {
        XlsxLegacyCommentDrawingPart part = new XlsxLegacyCommentDrawingPart();
        XlsxLegacyCommentDrawing drawing = new XlsxLegacyCommentDrawing();
        drawing.ShapeId = "_x0000_s1025";
        drawing.Row = 0;
        drawing.HasRow = true;
        drawing.Column = 0;
        drawing.HasColumn = true;
        drawing.Anchor = "1, 15, 0, 2, 3, 15, 4, 16";
        drawing.Left = "60pt";
        drawing.Top = "12pt";
        drawing.Width = "108pt";
        drawing.Height = "59.25pt";
        drawing.Visible = false;
        drawing.HasVisible = true;
        drawing.MoveWithCells = true;
        drawing.SizeWithCells = true;
        part.Drawings.Add(drawing);

        XlsxLegacyCommentDrawingWriteResult writeResult = new XlsxLegacyCommentDrawingWriter().WriteString(part);
        XlsxLegacyCommentDrawingPart roundTrip = new XlsxLegacyCommentDrawingReader().ReadString(writeResult.Xml);

        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(writeResult.Xml, Does.Contain("<v:shape"));
        Assert.That(writeResult.Xml, Does.Contain("visibility:hidden"));
        Assert.That(writeResult.Xml, Does.Contain("<x:Anchor>1, 15, 0, 2, 3, 15, 4, 16</x:Anchor>"));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.Drawings.Count, Is.EqualTo(1));
        Assert.That(roundTrip.Drawings[0].Reference, Is.EqualTo("A1"));
        Assert.That(roundTrip.Drawings[0].Left, Is.EqualTo("60pt"));
        Assert.That(roundTrip.Drawings[0].Visible, Is.False);
        Assert.That(roundTrip.Drawings[0].MoveWithCells, Is.True);
        Assert.That(roundTrip.Drawings[0].SizeWithCells, Is.True);
    }

    [Test]
    public void WriteString_PreservesLoadedLegacyDrawingRawXml()
    {
        string xml = "<xml xmlns:v=\"urn:schemas-microsoft-com:vml\" xmlns:x=\"urn:schemas-microsoft-com:office:excel\">" +
            "<v:shape id=\"_x0000_s1025\" style=\"position:absolute;visibility:hidden\"><x:ClientData ObjectType=\"Note\"><x:Row>0</x:Row><x:Column>0</x:Column></x:ClientData></v:shape>" +
            "</xml>";
        XlsxLegacyCommentDrawingPart part = new XlsxLegacyCommentDrawingReader().ReadString(xml);

        XlsxLegacyCommentDrawingWriteResult writeResult = new XlsxLegacyCommentDrawingWriter().WriteString(part);

        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(writeResult.Xml, Is.EqualTo(xml));
    }

    [Test]
    public void WriteBytes_RejectsInvalidCommentsAndDrawingParts()
    {
        XlsxCommentsPart nullAuthors = new XlsxCommentsPart();
        nullAuthors.Authors = null!;
        XlsxCommentsPart nullComment = new XlsxCommentsPart();
        nullComment.Comments.Add(null!);
        XlsxCommentsPart badReference = new XlsxCommentsPart();
        badReference.Authors.Add("Ada");
        badReference.Comments.Add(CreateComment("XFE1", 0, "Bad"));
        XlsxCommentsPart badAuthor = new XlsxCommentsPart();
        badAuthor.Authors.Add("Ada");
        badAuthor.Comments.Add(CreateComment("A1", 2, "Bad"));
        XlsxLegacyCommentDrawingPart nullDrawings = new XlsxLegacyCommentDrawingPart();
        nullDrawings.Drawings = null!;
        XlsxLegacyCommentDrawingPart missingPosition = new XlsxLegacyCommentDrawingPart();
        missingPosition.Drawings.Add(new XlsxLegacyCommentDrawing());

        XlsxCommentsPartWriter commentsWriter = new XlsxCommentsPartWriter();
        XlsxLegacyCommentDrawingWriter drawingWriter = new XlsxLegacyCommentDrawingWriter();
        XlsxCommentsPartWriteResult nullAuthorsResult = commentsWriter.WriteBytes(nullAuthors);
        XlsxCommentsPartWriteResult nullCommentResult = commentsWriter.WriteBytes(nullComment);
        XlsxCommentsPartWriteResult badReferenceResult = commentsWriter.WriteBytes(badReference);
        XlsxCommentsPartWriteResult badAuthorResult = commentsWriter.WriteBytes(badAuthor);
        XlsxLegacyCommentDrawingWriteResult nullDrawingsResult = drawingWriter.WriteBytes(nullDrawings);
        XlsxLegacyCommentDrawingWriteResult missingPositionResult = drawingWriter.WriteBytes(missingPosition);

        Assert.That(commentsWriter.WriteBytes(null).IsValid, Is.False);
        Assert.That(HasDiagnostic(nullAuthorsResult, "commentsWriter.authorsNull"), Is.True);
        Assert.That(HasDiagnostic(nullCommentResult, "commentsWriter.commentNull"), Is.True);
        Assert.That(HasDiagnostic(badReferenceResult, "a1.cellReference.columnRange"), Is.True);
        Assert.That(HasDiagnostic(badAuthorResult, "commentsWriter.authorIdRange"), Is.True);
        Assert.That(drawingWriter.WriteBytes(null).IsValid, Is.False);
        Assert.That(HasDiagnostic(nullDrawingsResult, "legacyCommentDrawingWriter.drawingsNull"), Is.True);
        Assert.That(HasDiagnostic(missingPositionResult, "legacyCommentDrawingWriter.positionMissing"), Is.True);
    }

    static XlsxComment CreateComment(string reference, uint authorId, string text)
    {
        XlsxComment comment = new XlsxComment();
        comment.Reference = reference;
        comment.AuthorId = authorId;
        comment.HasAuthorId = true;
        comment.Text = text;
        return comment;
    }

    static bool HasDiagnostic(XlsxCommentsPartWriteResult result, string code)
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

    static bool HasDiagnostic(XlsxLegacyCommentDrawingWriteResult result, string code)
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
