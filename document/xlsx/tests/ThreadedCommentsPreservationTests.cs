using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class ThreadedCommentsPreservationTests
{
    [Test]
    public void ReadString_ParsesThreadedCommentsAndPreservesRawXml()
    {
        XlsxThreadedCommentsPart part = new XlsxThreadedCommentsPartReader().ReadString(CreateThreadedCommentsXml());
        IXlsxThreadedCommentsPartOperations operations = new XlsxThreadedCommentsPartOperations();

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.NamespaceUri, Is.EqualTo(XlsxThreadedCommentsPartReader.DefaultNamespace));
        Assert.That(part.RawXml, Does.Contain("<ThreadedComments"));
        Assert.That(part.UnknownRootAttributes.Count, Is.EqualTo(1));
        Assert.That(part.Comments.Count, Is.EqualTo(2));
        Assert.That(part.Comments[0].Reference, Is.EqualTo("A1"));
        Assert.That(part.Comments[0].Id, Is.EqualTo("{11111111-1111-1111-1111-111111111111}"));
        Assert.That(part.Comments[0].PersonId, Is.EqualTo("{22222222-2222-2222-2222-222222222222}"));
        Assert.That(part.Comments[0].DateTimeText, Is.EqualTo("2026-08-09T12:34:56Z"));
        Assert.That(part.Comments[0].Text, Is.EqualTo("Hello threaded"));
        Assert.That(part.Comments[0].TextRawXml, Does.Contain("<text"));
        Assert.That(part.Comments[0].RawXml, Does.Contain("<threadedComment"));
        Assert.That(part.Comments[0].UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(part.Comments[0].UnknownXml.Count, Is.EqualTo(1));
        Assert.That(operations.GetComment(part, "C3")!.ParentId, Is.EqualTo("{11111111-1111-1111-1111-111111111111}"));
        Assert.That(operations.GetComment(part, "C3")!.Text, Is.EqualTo("Nested"));
        Assert.That(part.UnknownXml.Count, Is.EqualTo(1));
    }

    [Test]
    public void WriteString_PreservesLoadedThreadedCommentsRawXml()
    {
        string xml = CreateThreadedCommentsXml();
        XlsxThreadedCommentsPart part = new XlsxThreadedCommentsPartReader().ReadString(xml);

        XlsxThreadedCommentsPartWriteResult writeResult = new XlsxThreadedCommentsPartWriter().WriteString(part);

        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(writeResult.Xml, Is.EqualTo(xml));
    }

    [Test]
    public void WriteString_WritesConstructedThreadedCommentsAndRoundTrips()
    {
        XlsxThreadedCommentsPart part = new XlsxThreadedCommentsPart();
        part.Comments.Add(CreateComment("A1", "{11111111-1111-1111-1111-111111111111}", "{22222222-2222-2222-2222-222222222222}", "", "Plain"));
        XlsxThreadedComment reply = CreateComment("C3", "{33333333-3333-3333-3333-333333333333}", "{22222222-2222-2222-2222-222222222222}", "{11111111-1111-1111-1111-111111111111}", "");
        reply.TextRawXml = "<text><t>Rich</t></text>";
        part.Comments.Add(reply);

        XlsxThreadedCommentsPartWriteResult writeResult = new XlsxThreadedCommentsPartWriter().WriteString(part);
        XlsxThreadedCommentsPart roundTrip = new XlsxThreadedCommentsPartReader().ReadString(writeResult.Xml);

        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(writeResult.Xml, Does.Contain("<ThreadedComments"));
        Assert.That(writeResult.Xml, Does.Contain("<threadedComment ref=\"A1\""));
        Assert.That(writeResult.Xml, Does.Contain("personId=\"{22222222-2222-2222-2222-222222222222}\""));
        Assert.That(writeResult.Xml, Does.Contain("parentId=\"{11111111-1111-1111-1111-111111111111}\""));
        Assert.That(writeResult.Xml, Does.Contain("<text><t>Rich</t></text>"));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.Comments.Count, Is.EqualTo(2));
        Assert.That(roundTrip.GetComment("A1")!.Text, Is.EqualTo("Plain"));
        Assert.That(roundTrip.GetComment("C3")!.Text, Is.EqualTo("Rich"));
    }

    [Test]
    public void Writer_RejectsInvalidThreadedCommentsParts()
    {
        XlsxThreadedCommentsPart nullComments = new XlsxThreadedCommentsPart();
        nullComments.Comments = null!;
        XlsxThreadedCommentsPart nullComment = new XlsxThreadedCommentsPart();
        nullComment.Comments.Add(null!);
        XlsxThreadedCommentsPart missingReference = new XlsxThreadedCommentsPart();
        missingReference.Comments.Add(new XlsxThreadedComment());
        XlsxThreadedCommentsPart badReference = new XlsxThreadedCommentsPart();
        badReference.Comments.Add(CreateComment("XFE1", "{11111111-1111-1111-1111-111111111111}", "{22222222-2222-2222-2222-222222222222}", "", "Bad"));

        XlsxThreadedCommentsPartWriter writer = new XlsxThreadedCommentsPartWriter();
        XlsxThreadedCommentsPartWriteResult nullCommentsResult = writer.WriteBytes(nullComments);
        XlsxThreadedCommentsPartWriteResult nullCommentResult = writer.WriteBytes(nullComment);
        XlsxThreadedCommentsPartWriteResult missingReferenceResult = writer.WriteBytes(missingReference);
        XlsxThreadedCommentsPartWriteResult badReferenceResult = writer.WriteBytes(badReference);

        Assert.That(writer.WriteBytes(null).IsValid, Is.False);
        Assert.That(HasDiagnostic(nullCommentsResult, "threadedCommentsWriter.commentsNull"), Is.True);
        Assert.That(HasDiagnostic(nullCommentResult, "threadedCommentsWriter.commentNull"), Is.True);
        Assert.That(HasDiagnostic(missingReferenceResult, "threadedCommentsWriter.referenceMissing"), Is.True);
        Assert.That(HasDiagnostic(badReferenceResult, "a1.cellReference.columnRange"), Is.True);
    }

    [Test]
    public void Reader_ReportsMissingRootsInvalidRootsAndInvalidReferences()
    {
        string invalid = "<ThreadedComments xmlns=\"" + XlsxThreadedCommentsPartReader.DefaultNamespace + "\">" +
            "<threadedComment ref=\"XFE1\"><text>Bad</text></threadedComment>" +
            "</ThreadedComments>";

        XlsxThreadedCommentsPart missing = new XlsxThreadedCommentsPartReader().ReadString("");
        XlsxThreadedCommentsPart invalidRoot = new XlsxThreadedCommentsPartReader().ReadString("<bad />");
        XlsxThreadedCommentsPart invalidReference = new XlsxThreadedCommentsPartReader().ReadString(invalid);

        Assert.That(missing.IsValid, Is.False);
        Assert.That(HasDiagnostic(missing, "threadedComments.missingRoot"), Is.True);
        Assert.That(invalidRoot.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidRoot, "threadedComments.invalidRoot"), Is.True);
        Assert.That(invalidReference.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidReference, "a1.cellReference.columnRange"), Is.True);
    }

    static XlsxThreadedComment CreateComment(string reference, string id, string personId, string parentId, string text)
    {
        XlsxThreadedComment comment = new XlsxThreadedComment();
        comment.Reference = reference;
        comment.Id = id;
        comment.PersonId = personId;
        comment.ParentId = parentId;
        comment.DateTimeText = "2026-08-09T12:34:56Z";
        comment.Text = text;
        return comment;
    }

    static string CreateThreadedCommentsXml()
    {
        return "<ThreadedComments xmlns=\"" + XlsxThreadedCommentsPartReader.DefaultNamespace + "\" xmlns:x=\"urn:x\" x:root=\"r\">" +
            "<threadedComment ref=\"A1\" id=\"{11111111-1111-1111-1111-111111111111}\" personId=\"{22222222-2222-2222-2222-222222222222}\" dT=\"2026-08-09T12:34:56Z\" x:item=\"i\"><text>Hello threaded</text><x:future /></threadedComment>" +
            "<threadedComment ref=\"C3\" id=\"{33333333-3333-3333-3333-333333333333}\" personId=\"{22222222-2222-2222-2222-222222222222}\" parentId=\"{11111111-1111-1111-1111-111111111111}\"><text><t>Nested</t></text></threadedComment>" +
            "<x:futureRoot />" +
            "</ThreadedComments>";
    }

    static bool HasDiagnostic(XlsxThreadedCommentsPart part, string code)
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

    static bool HasDiagnostic(XlsxThreadedCommentsPartWriteResult result, string code)
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
