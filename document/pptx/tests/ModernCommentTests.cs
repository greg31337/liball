using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class ModernCommentTests
{
    [Test]
    public void Slide_LoadsModernThreadedCommentInventory()
    {
        PptxPresentation presentation = LoadModernCommentPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        List<PptxModernComment> comments = slide.GetModernComments();
        PptxModernComment root = comments[0];
        PptxModernComment reply = comments[1];

        Assert.That(slide.HasModernComments(), Is.True);
        Assert.That(presentation.HasModernComments(), Is.True);
        Assert.That(presentation.GetModernComments().Count, Is.EqualTo(2));
        Assert.That(comments.Count, Is.EqualTo(2));
        Assert.That(root.RelationshipId, Is.EqualTo("rIdModern"));
        Assert.That(root.PartName, Is.EqualTo("/ppt/comments/comment1.xml"));
        Assert.That(root.Id, Is.EqualTo("{11111111-1111-1111-1111-111111111111}"));
        Assert.That(root.AuthorId, Is.EqualTo("{AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA}"));
        Assert.That(root.Created, Is.EqualTo("2026-03-01T10:20:30.000"));
        Assert.That(root.Status, Is.EqualTo("active"));
        Assert.That(root.Text, Is.EqualTo("Modern Root"));
        Assert.That(root.RawXml, Does.Contain("Preserved Modern Extension"));
        Assert.That(reply.Id, Is.EqualTo("{22222222-2222-2222-2222-222222222222}"));
        Assert.That(reply.Text, Is.EqualTo("Modern Reply"));
        Assert.That(reply.Status, Is.EqualTo("resolved"));
    }

    [Test]
    public void ModernCommentPartsPreserveOnRoundTripAndExtractText()
    {
        PptxPresentation presentation = LoadModernCommentPresentation();
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxRelationship relationship = FindRelationship(reopened.Package.GetRelationships(PptxPartNames.FirstSlide), PptxRelationshipTypes.ModernComments);

        Assert.That(reopened.Package.GetPart("/ppt/comments/comment1.xml").ContentType, Is.EqualTo(PptxContentTypes.ModernComments));
        Assert.That(reopened.Package.GetPart("/ppt/comments/comment1.xml").GetBytes(), Is.EqualTo(Encoding.UTF8.GetBytes(CreateModernCommentsXml())));
        Assert.That(reopened.Package.GetPart(PptxPartNames.FirstSlide).GetBytes(), Is.EqualTo(Encoding.UTF8.GetBytes(CreateSlideXml())));
        Assert.That(relationship.TargetPartName, Is.EqualTo("/ppt/comments/comment1.xml"));
        Assert.That(reopened.ExtractModernCommentsText(0), Is.EqualTo("Modern Root\nModern Reply"));
        Assert.That(reopened.ExtractText(), Does.Contain("Modern Root"));
        Assert.That(reopened.ExtractText(), Does.Contain("Modern Reply"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void PresentationWithoutModernCommentsReportsFalse()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        PptxPresentation presentation = document.LoadPresentation();

        Assert.That(presentation.HasModernComments(), Is.False);
        Assert.That(presentation.GetModernComments().Count, Is.EqualTo(0));
        Assert.That(presentation.GetSlide(0).HasModernComments(), Is.False);
    }

    static PptxPresentation LoadModernCommentPresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(CreateSlideXml()));
        package.AddPart("/ppt/comments/comment1.xml", PptxContentTypes.ModernComments, Encoding.UTF8.GetBytes(CreateModernCommentsXml()));
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdModern", PptxRelationshipTypes.ModernComments, "../comments/comment1.xml", PptxRelationshipTargetMode.Internal);
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

    static string CreateSlideXml()
    {
        return
            "<p:sld xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:p188=\"" + PptxNamespaces.ModernComments + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld>" +
            "<p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>" +
            "</p:spTree>" +
            "</p:cSld>" +
            "<p:extLst><p:ext uri=\"{6950BFC3-D8DA-4A85-94F7-54DA5524770B}\"><p188:commentRel r:id=\"rIdModern\"/></p:ext></p:extLst>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sld>";
    }

    static string CreateModernCommentsXml()
    {
        return
            "<p188:cmLst xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:p188=\"" + PptxNamespaces.ModernComments + "\" xmlns:x=\"http://example.com/modern-comments\">" +
            "<p188:cm id=\"{11111111-1111-1111-1111-111111111111}\" authorId=\"{AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA}\" created=\"2026-03-01T10:20:30.000\" status=\"active\">" +
            "<p188:txBody><a:bodyPr/><a:lstStyle/><a:p><a:r><a:t>Modern Root</a:t></a:r></a:p></p188:txBody>" +
            "<p188:replyLst>" +
            "<p188:cm id=\"{22222222-2222-2222-2222-222222222222}\" authorId=\"{BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB}\" created=\"2026-03-01T11:20:30.000\" status=\"resolved\">" +
            "<p188:txBody><a:bodyPr/><a:lstStyle/><a:p><a:r><a:t>Modern Reply</a:t></a:r></a:p></p188:txBody>" +
            "</p188:cm>" +
            "</p188:replyLst>" +
            "<p188:extLst><p188:ext uri=\"preserve\"><x:data>Preserved Modern Extension</x:data></p188:ext></p188:extLst>" +
            "</p188:cm>" +
            "</p188:cmLst>";
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
}
