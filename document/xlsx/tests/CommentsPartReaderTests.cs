using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class CommentsPartReaderTests
{
    [Test]
    public void ReadString_ParsesAuthorsCommentsPlainTextAndUnknowns()
    {
        string xml = "<comments xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:x=\"urn:x\" x:root=\"r\">" +
            "<authors><author>Ada</author><author>Bob</author></authors>" +
            "<commentList>" +
            "<comment ref=\"A1\" authorId=\"0\" shapeId=\"1024\" x:item=\"i\"><text><t>Hello </t><r><rPr><b /></rPr><t>World</t></r></text><x:future /></comment>" +
            "<comment ref=\"C3\" authorId=\"1\"><text><r><t>Second</t></r></text></comment>" +
            "</commentList>" +
            "<x:futureRoot />" +
            "</comments>";

        XlsxCommentsPart part = new XlsxCommentsPartReader().ReadString(xml);
        IXlsxCommentsPartOperations operations = new XlsxCommentsPartOperations();

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.NamespaceUri, Is.EqualTo(XlsxNamespaces.Spreadsheet));
        Assert.That(part.RawXml, Does.Contain("<comments"));
        Assert.That(part.UnknownRootAttributes.Count, Is.EqualTo(1));
        Assert.That(part.Authors.Count, Is.EqualTo(2));
        Assert.That(part.Authors[0], Is.EqualTo("Ada"));
        Assert.That(part.Comments.Count, Is.EqualTo(2));
        Assert.That(part.Comments[0].Reference, Is.EqualTo("A1"));
        Assert.That(part.Comments[0].AuthorId, Is.EqualTo(0));
        Assert.That(part.Comments[0].Author, Is.EqualTo("Ada"));
        Assert.That(part.Comments[0].ShapeId, Is.EqualTo(1024));
        Assert.That(part.Comments[0].Text, Is.EqualTo("Hello World"));
        Assert.That(part.Comments[0].TextRawXml, Does.Contain("<text"));
        Assert.That(part.Comments[0].UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(part.Comments[0].UnknownXml.Count, Is.EqualTo(1));
        Assert.That(operations.GetComment(part, "C3")!.Author, Is.EqualTo("Bob"));
        Assert.That(operations.GetComment(part, "C3")!.Text, Is.EqualTo("Second"));
        Assert.That(part.UnknownXml.Count, Is.EqualTo(1));
    }

    [Test]
    public void LegacyCommentDrawingReader_ParsesPositionsVisibilityAndAppliesToComments()
    {
        XlsxCommentsPart comments = new XlsxCommentsPartReader().ReadString(CreateCommentsXml());
        string vml = "<xml xmlns:v=\"urn:schemas-microsoft-com:vml\" xmlns:x=\"urn:schemas-microsoft-com:office:excel\">" +
            "<v:shape id=\"_x0000_s1025\" style=\"position:absolute;margin-left:60pt;margin-top:12pt;width:108pt;height:59.25pt;visibility:hidden\">" +
            "<x:ClientData ObjectType=\"Note\"><x:MoveWithCells /><x:SizeWithCells /><x:Anchor>1, 15, 0, 2, 3, 15, 4, 16</x:Anchor><x:Row>0</x:Row><x:Column>0</x:Column></x:ClientData>" +
            "</v:shape>" +
            "<v:shape id=\"_x0000_s1026\" style=\"position:absolute;margin-left:90pt;margin-top:18pt;width:120pt;height:60pt\">" +
            "<x:ClientData ObjectType=\"Note\"><x:Visible /><x:Anchor>3, 1, 2, 3, 5, 1, 6, 3</x:Anchor><x:Row>2</x:Row><x:Column>2</x:Column></x:ClientData>" +
            "</v:shape>" +
            "</xml>";

        XlsxLegacyCommentDrawingPart drawingPart = new XlsxLegacyCommentDrawingReader().ReadString(vml);
        new XlsxCommentsPartOperations().ApplyLegacyDrawing(comments, drawingPart);

        Assert.That(drawingPart.IsValid, Is.True);
        Assert.That(drawingPart.RawXml, Does.Contain("<xml"));
        Assert.That(drawingPart.Drawings.Count, Is.EqualTo(2));
        Assert.That(drawingPart.Drawings[0].Reference, Is.EqualTo("A1"));
        Assert.That(drawingPart.Drawings[0].Left, Is.EqualTo("60pt"));
        Assert.That(drawingPart.Drawings[0].Top, Is.EqualTo("12pt"));
        Assert.That(drawingPart.Drawings[0].Width, Is.EqualTo("108pt"));
        Assert.That(drawingPart.Drawings[0].Height, Is.EqualTo("59.25pt"));
        Assert.That(drawingPart.Drawings[0].HasVisible, Is.True);
        Assert.That(drawingPart.Drawings[0].Visible, Is.False);
        Assert.That(drawingPart.Drawings[0].MoveWithCells, Is.True);
        Assert.That(drawingPart.Drawings[0].SizeWithCells, Is.True);
        Assert.That(comments.GetComment("A1")!.HasPosition, Is.True);
        Assert.That(comments.GetComment("A1")!.Anchor, Is.EqualTo("1, 15, 0, 2, 3, 15, 4, 16"));
        Assert.That(comments.GetComment("A1")!.Visible, Is.False);
        Assert.That(comments.GetComment("C3")!.HasPosition, Is.True);
        Assert.That(comments.GetComment("C3")!.Visible, Is.True);
        Assert.That(comments.GetComment("C3")!.Anchor, Is.EqualTo("3, 1, 2, 3, 5, 1, 6, 3"));
    }

    [Test]
    public void Readers_ReportMissingRootsInvalidRootsAndInvalidValues()
    {
        string invalidCommentsXml = "<comments xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
            "<authors><author>Ada</author></authors>" +
            "<commentList><comment ref=\"XFE1\" authorId=\"4\" shapeId=\"bad\"><text><t>Bad</t></text></comment></commentList>" +
            "</comments>";
        string invalidVml = "<xml xmlns:x=\"urn:schemas-microsoft-com:office:excel\"><shape><x:ClientData><x:Row>bad</x:Row><x:Column>0</x:Column></x:ClientData></shape></xml>";

        XlsxCommentsPart missingComments = new XlsxCommentsPartReader().ReadString("");
        XlsxCommentsPart invalidRoot = new XlsxCommentsPartReader().ReadString("<bad />");
        XlsxCommentsPart invalidComments = new XlsxCommentsPartReader().ReadString(invalidCommentsXml);
        XlsxLegacyCommentDrawingPart missingVml = new XlsxLegacyCommentDrawingReader().ReadString("");
        XlsxLegacyCommentDrawingPart invalidVmlPart = new XlsxLegacyCommentDrawingReader().ReadString(invalidVml);

        Assert.That(missingComments.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingComments, "comments.missingRoot"), Is.True);
        Assert.That(invalidRoot.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidRoot, "comments.invalidRoot"), Is.True);
        Assert.That(invalidComments.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidComments, "a1.cellReference.columnRange"), Is.True);
        Assert.That(HasDiagnostic(invalidComments, "xml.unsignedInteger.invalid"), Is.True);
        Assert.That(HasDiagnostic(invalidComments, "comments.authorIdRange"), Is.True);
        Assert.That(missingVml.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingVml, "legacyCommentDrawing.missingRoot"), Is.True);
        Assert.That(invalidVmlPart.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidVmlPart, "xml.unsignedInteger.invalid"), Is.True);
    }

    static string CreateCommentsXml()
    {
        return "<comments xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
            "<authors><author>Ada</author><author>Bob</author></authors>" +
            "<commentList>" +
            "<comment ref=\"A1\" authorId=\"0\"><text><t>One</t></text></comment>" +
            "<comment ref=\"C3\" authorId=\"1\"><text><t>Two</t></text></comment>" +
            "</commentList>" +
            "</comments>";
    }

    static bool HasDiagnostic(XlsxCommentsPart part, string code)
    {
        foreach (XlsxDiagnostic diagnostic in part.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }

    static bool HasDiagnostic(XlsxLegacyCommentDrawingPart part, string code)
    {
        foreach (XlsxDiagnostic diagnostic in part.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
