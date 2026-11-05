using System;
using System.Text;
using NUnit.Framework;

namespace odf.Tests;

public class AnnotationDocumentModelTests
{
    [Test]
    public void LoadsAndUpdatesRangeContentMetadataAndPositioning()
    {
        OdfDocument document = ReadFlat(
            DocumentXml(
                "<text:p>Before <office:annotation office:name='note-1'"
                + " xml:id='annotation-id' loext:resolved='true'"
                + " office:display='true' svg:x='1cm' svg:y='2cm'"
                + " svg:width='3cm' svg:height='4cm'"
                + " draw:style-name='Comment' draw:text-style-name='CommentText'"
                + " text:anchor-type='char' text:anchor-page-number='2'"
                + " table:end-cell-address='Sheet1.C4' table:end-x='5mm'"
                + " table:end-y='6mm'>"
                + "<dc:creator>Alice</dc:creator>"
                + "<dc:date>2026-09-02T10:15:00Z</dc:date>"
                + "<meta:date-string>September 2</meta:date-string>"
                + "<meta:creator-initials>AL</meta:creator-initials>"
                + "<text:p>First body</text:p>"
                + "<text:list><text:list-item><text:p>Second body</text:p>"
                + "</text:list-item></text:list>"
                + "</office:annotation>selected <text:span>words</text:span>"
                + "<office:annotation-end office:name='note-1'/> after</text:p>"));

        Assert.That(document.Annotations, Has.Length.EqualTo(1));
        OdfAnnotation annotation = document.Annotations[0];
        Assert.Multiple(() =>
        {
            Assert.That(annotation.Name, Is.EqualTo("note-1"));
            Assert.That(annotation.XmlId, Is.EqualTo("annotation-id"));
            Assert.That(annotation.Creator, Is.EqualTo("Alice"));
            Assert.That(annotation.Date, Is.EqualTo("2026-09-02T10:15:00Z"));
            Assert.That(annotation.IsDateValid, Is.True);
            Assert.That(annotation.DateString, Is.EqualTo("September 2"));
            Assert.That(annotation.Initials, Is.EqualTo("AL"));
            Assert.That(annotation.Text, Is.EqualTo("First body\nSecond body"));
            Assert.That(annotation.TextBlocks, Has.Length.EqualTo(2));
            Assert.That(annotation.AnnotatedText, Is.EqualTo("selected words"));
            Assert.That(annotation.IsRange, Is.True);
            Assert.That(annotation.IsRangeResolved, Is.True);
            Assert.That(annotation.StartBlock, Is.SameAs(annotation.EndBlock));
            Assert.That(annotation.State, Is.EqualTo(OdfAnnotationState.Resolved));
            Assert.That(annotation.HasExplicitState, Is.True);
            Assert.That(annotation.X, Is.EqualTo("1cm"));
            Assert.That(annotation.Y, Is.EqualTo("2cm"));
            Assert.That(annotation.Width, Is.EqualTo("3cm"));
            Assert.That(annotation.Height, Is.EqualTo("4cm"));
            Assert.That(annotation.EndCellAddress, Is.EqualTo("Sheet1.C4"));
            Assert.That(annotation.EndX, Is.EqualTo("5mm"));
            Assert.That(annotation.EndY, Is.EqualTo("6mm"));
        });

        annotation.Name = "renamed";
        annotation.Creator = "Bob";
        annotation.Date = "2026-09-03";
        annotation.DateString = "September 3";
        annotation.Initials = "BO";
        annotation.Text = "Replacement body";
        annotation.State = OdfAnnotationState.Open;
        annotation.X = "7cm";
        annotation.Width = "8cm";
        OdfDocument roundTrip = WriteAndRead(document);
        OdfAnnotation updated = roundTrip.Annotations[0];

        Assert.Multiple(() =>
        {
            Assert.That(updated.Name, Is.EqualTo("renamed"));
            Assert.That(updated.EndNode, Is.Not.Null);
            Assert.That(updated.Creator, Is.EqualTo("Bob"));
            Assert.That(updated.Date, Is.EqualTo("2026-09-03"));
            Assert.That(updated.IsDateValid, Is.True);
            Assert.That(updated.DateString, Is.EqualTo("September 3"));
            Assert.That(updated.Initials, Is.EqualTo("BO"));
            Assert.That(updated.Text, Is.EqualTo("Replacement body"));
            Assert.That(updated.TextBlocks, Has.Length.EqualTo(1));
            Assert.That(updated.State, Is.EqualTo(OdfAnnotationState.Open));
            Assert.That(updated.X, Is.EqualTo("7cm"));
            Assert.That(updated.Width, Is.EqualTo("8cm"));
            Assert.That(updated.AnnotatedText, Is.EqualTo("selected words"));
        });
    }

    [Test]
    public void ResolvesReplyThreadsAndUpdatesParentNamesAfterRename()
    {
        OdfDocument document = ReadFlat(
            DocumentXml(
                "<text:p><office:annotation office:name='root'"
                + " loext:resolved='false'><dc:creator>Root</dc:creator>"
                + "<text:p>Question</text:p></office:annotation>"
                + "<office:annotation office:name='reply'"
                + " loext:parent-name='root' loext:resolved='true'>"
                + "<dc:creator>Reply</dc:creator>"
                + "<loext:sender-initials>RP</loext:sender-initials>"
                + "<text:p>Answer</text:p></office:annotation></text:p>"));

        OdfAnnotation root = document.Annotations[0];
        OdfAnnotation reply = document.Annotations[1];
        Assert.Multiple(() =>
        {
            Assert.That(root.Replies, Is.EqualTo(new[] { reply }));
            Assert.That(reply.Parent, Is.SameAs(root));
            Assert.That(reply.ParentName, Is.EqualTo("root"));
            Assert.That(reply.Initials, Is.EqualTo("RP"));
            Assert.That(root.State, Is.EqualTo(OdfAnnotationState.Open));
            Assert.That(reply.State, Is.EqualTo(OdfAnnotationState.Resolved));
            Assert.That(root.IsRange, Is.False);
            Assert.That(reply.IsRange, Is.False);
        });

        root.Name = "renamed-root";
        OdfDocument roundTrip = WriteAndRead(document);
        OdfAnnotation loadedRoot = roundTrip.Annotations[0];
        OdfAnnotation loadedReply = roundTrip.Annotations[1];
        Assert.Multiple(() =>
        {
            Assert.That(loadedRoot.Name, Is.EqualTo("renamed-root"));
            Assert.That(loadedReply.ParentName, Is.EqualTo("renamed-root"));
            Assert.That(loadedReply.Parent, Is.SameAs(loadedRoot));
            Assert.That(loadedReply.Initials, Is.EqualTo("RP"));
        });
    }

    [Test]
    public void DeletingAnnotationRemovesRangeEndAndDanglingReplyReference()
    {
        OdfDocument document = ReadFlat(
            DocumentXml(
                "<text:p><office:annotation office:name='root'>"
                + "<text:p>Question</text:p></office:annotation>selected"
                + "<office:annotation-end office:name='root'/>"
                + "<office:annotation office:name='reply'"
                + " loext:parent-name='root'><text:p>Answer</text:p>"
                + "</office:annotation></text:p>"));
        OdfAnnotation reply = document.Annotations[1];
        document.Annotations = [reply];

        byte[] bytes = new OdfDocumentWriter().WriteDocumentBytes(
            document, new OdfSaveOptions());
        string xml = Encoding.UTF8.GetString(bytes);
        OdfDocument roundTrip = new OdfDocumentReader().ReadDocumentBytes(
            bytes, "deleted.fodt", new OdfLoadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(roundTrip.Annotations, Has.Length.EqualTo(1));
            Assert.That(roundTrip.Annotations[0].Name, Is.EqualTo("reply"));
            Assert.That(roundTrip.Annotations[0].Parent, Is.Null);
            Assert.That(roundTrip.Annotations[0].ParentName, Is.Empty);
            Assert.That(xml, Does.Not.Contain("annotation-end"));
            Assert.That(xml, Does.Not.Contain("parent-name"));
            Assert.That(xml, Does.Not.Contain("Question"));
            Assert.That(xml, Does.Contain("Answer"));
        });
    }

    [Test]
    public void ReportsReversedAndDuplicateRangeEndpoints()
    {
        OdfDocument reversedDocument = ReadFlat(
            DocumentXml(
                "<text:p><office:annotation-end office:name='late'/>"
                + "text<office:annotation office:name='late'>"
                + "<text:p>Body</text:p></office:annotation></text:p>"));
        OdfAnnotation reversed = reversedDocument.Annotations[0];
        Assert.Multiple(() =>
        {
            Assert.That(reversed.IsRange, Is.True);
            Assert.That(reversed.IsReversed, Is.True);
            Assert.That(reversed.IsRangeResolved, Is.False);
            Assert.That(reversed.AnnotatedText, Is.Empty);
        });

        OdfDocument duplicateDocument = ReadFlat(
            DocumentXml(
                "<text:p><office:annotation office:name='duplicate'>"
                + "<text:p>Body</text:p></office:annotation>text"
                + "<office:annotation-end office:name='duplicate'/>"
                + "<office:annotation-end office:name='duplicate'/></text:p>"));
        OdfAnnotation duplicate = duplicateDocument.Annotations[0];
        Assert.Multiple(() =>
        {
            Assert.That(duplicate.IsRange, Is.True);
            Assert.That(duplicate.HasDuplicateEndpoint, Is.True);
            Assert.That(duplicate.IsRangeResolved, Is.False);
        });
    }

    static OdfDocument ReadFlat(string xml)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "annotations.fodt",
            new OdfLoadOptions());
    }

    static OdfDocument WriteAndRead(OdfDocument document)
    {
        byte[] bytes = new OdfDocumentWriter().WriteDocumentBytes(
            document, new OdfSaveOptions());
        return new OdfDocumentReader().ReadDocumentBytes(
            bytes, "annotations.fodt", new OdfLoadOptions());
    }

    static string DocumentXml(string body)
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document xmlns:office='" + OdfNamespaces.Office
            + "' xmlns:text='" + OdfNamespaces.Text
            + "' xmlns:dc='" + OdfNamespaces.DublinCore
            + "' xmlns:meta='" + OdfNamespaces.Metadata
            + "' xmlns:svg='" + OdfNamespaces.Svg
            + "' xmlns:draw='" + OdfNamespaces.Drawing
            + "' xmlns:table='" + OdfNamespaces.Table
            + "' xmlns:loext='" + OdfNamespaces.LibreOffice
            + "' office:version='1.4' office:mimetype='"
            + OdfMediaTypes.Text + "'><office:body><office:text>"
            + body + "</office:text></office:body></office:document>";
    }
}
