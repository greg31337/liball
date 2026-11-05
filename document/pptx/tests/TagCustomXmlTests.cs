using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class TagCustomXmlTests
{
    [Test]
    public void Slide_LoadsTagsAndPresentationCustomXmlParts()
    {
        PptxPresentation presentation = LoadTagCustomXmlPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        List<PptxTag> tags = slide.GetTags();
        List<PptxCustomXmlPart> customXmlParts = presentation.GetCustomXmlParts();
        PptxCustomXmlPart customXml = customXmlParts[0];

        Assert.That(tags.Count, Is.EqualTo(2));
        Assert.That(slide.TagCount(), Is.EqualTo(2));
        Assert.That(slide.GetTagValue("Reviewed"), Is.EqualTo("yes"));
        Assert.That(slide.GetTagValue("Owner"), Is.EqualTo("Docs"));
        Assert.That(slide.GetTagValue("Missing"), Is.EqualTo(""));
        Assert.That(tags[0].Name, Is.EqualTo("Reviewed"));
        Assert.That(tags[0].Value, Is.EqualTo("yes"));
        Assert.That(customXmlParts.Count, Is.EqualTo(1));
        Assert.That(customXml.RelationshipId, Is.EqualTo("rIdCustomXml"));
        Assert.That(customXml.PartName, Is.EqualTo("/customXml/item1.xml"));
        Assert.That(customXml.ContentType, Is.EqualTo(PptxContentTypes.Xml));
        Assert.That(customXml.Xml, Is.EqualTo(CreateCustomXml()));
    }

    [Test]
    public void TagAndCustomXmlPartsPreserveOnRoundTrip()
    {
        PptxPresentation presentation = LoadTagCustomXmlPresentation();
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxRelationship tagRelationship = FindRelationship(reopened.Package.GetRelationships(PptxPartNames.FirstSlide), PptxRelationshipTypes.Tags);
        PptxRelationship customRelationship = FindRelationship(reopened.Package.GetRelationships(PptxPartNames.Presentation), PptxRelationshipTypes.CustomXml);

        Assert.That(reopened.Package.GetPart("/ppt/tags/tag1.xml").GetBytes(), Is.EqualTo(Encoding.UTF8.GetBytes(CreateTagsXml())));
        Assert.That(reopened.Package.GetPart("/customXml/item1.xml").GetBytes(), Is.EqualTo(Encoding.UTF8.GetBytes(CreateCustomXml())));
        Assert.That(reopened.Package.GetPart(PptxPartNames.FirstSlide).GetBytes(), Is.EqualTo(Encoding.UTF8.GetBytes(CreateSlideXml())));
        Assert.That(tagRelationship.TargetPartName, Is.EqualTo("/ppt/tags/tag1.xml"));
        Assert.That(customRelationship.TargetPartName, Is.EqualTo("/customXml/item1.xml"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Slide_SetAndRemoveTagsAndAddCustomXmlRoundTrip()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        PptxPresentation presentation = document.LoadPresentation();
        PptxSlide slide = presentation.GetSlide(0);

        slide.SetTag("Owner", "Docs");
        slide.SetTag("State", "Draft");
        slide.SetTag("State", "Ready");
        slide.RemoveTag("Owner");
        PptxCustomXmlPart customXml = presentation.AddCustomXmlPart("<root><value>Initial</value></root>");
        customXml.SetXml("<root><value>Updated</value></root>");
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxSlide reopenedSlide = reopened.GetSlide(0);
        PptxCustomXmlPart reopenedCustomXml = reopened.GetCustomXmlParts()[0];
        PptxRelationship tagRelationship = FindRelationship(reopened.Package.GetRelationships(PptxPartNames.FirstSlide), PptxRelationshipTypes.Tags);
        PptxRelationship customRelationship = FindRelationship(reopened.Package.GetRelationships(PptxPartNames.Presentation), PptxRelationshipTypes.CustomXml);
        string slideXml = Encoding.UTF8.GetString(reopened.Package.GetPart(PptxPartNames.FirstSlide).GetBytes());

        Assert.That(reopenedSlide.TagCount(), Is.EqualTo(1));
        Assert.That(reopenedSlide.GetTagValue("State"), Is.EqualTo("Ready"));
        Assert.That(reopenedSlide.GetTagValue("Owner"), Is.EqualTo(""));
        Assert.That(tagRelationship.TargetPartName, Is.EqualTo("/ppt/tags/tag1.xml"));
        Assert.That(slideXml, Does.Contain("custDataLst"));
        Assert.That(slideXml, Does.Contain("tags"));
        Assert.That(reopenedCustomXml.PartName, Is.EqualTo("/customXml/item1.xml"));
        Assert.That(reopenedCustomXml.Xml, Is.EqualTo("<root><value>Updated</value></root>"));
        Assert.That(customRelationship.TargetPartName, Is.EqualTo("/customXml/item1.xml"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void CustomXml_BytesAndRelationshipsEditAndPartRemovalRoundTrip()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        PptxPresentation presentation = document.LoadPresentation();
        PptxCustomXmlPart customXml = presentation.AddCustomXmlPart("<root><value>Initial</value></root>");
        PptxCustomXmlPartOperations operations = new PptxCustomXmlPartOperations();
        byte[] replacement = Encoding.UTF8.GetBytes("<root><value>Bytes</value></root>");

        operations.SetXml(customXml, "<root><value>Text</value></root>");
        Assert.That(customXml.Xml, Is.EqualTo("<root><value>Text</value></root>"));
        operations.ReplaceBytes(customXml, replacement, "application/vnd.example+xml");
        PptxRelationship added = operations.AddRelationship(customXml, "http://example.com/relationships/metadata", "https://example.com/one", PptxRelationshipTargetMode.External);
        PptxRelationship edited = operations.SetRelationshipTarget(customXml, added.Id, "https://example.com/two", PptxRelationshipTargetMode.External);

        Assert.That(operations.GetBytes(customXml), Is.EqualTo(replacement));
        Assert.That(customXml.ContentType, Is.EqualTo("application/vnd.example+xml"));
        Assert.That(operations.GetRelationships(customXml).Count, Is.EqualTo(1));
        Assert.That(edited.Target, Is.EqualTo("https://example.com/two"));

        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxCustomXmlPart reopenedCustomXml = reopened.GetCustomXmlParts()[0];
        PptxRelationship reopenedRelationship = operations.GetRelationships(reopenedCustomXml)[0];
        string partName = reopenedCustomXml.PartName;

        Assert.That(operations.GetBytes(reopenedCustomXml), Is.EqualTo(replacement));
        Assert.That(reopenedRelationship.Target, Is.EqualTo("https://example.com/two"));

        operations.RemoveRelationship(reopenedCustomXml, reopenedRelationship.Id);
        Assert.That(operations.GetRelationships(reopenedCustomXml).Count, Is.EqualTo(0));
        Assert.That(operations.Remove(reopenedCustomXml), Is.True);
        Assert.That(operations.Remove(reopenedCustomXml), Is.False);

        PptxPresentation removed = SaveReopenAndLoad(reopened);

        Assert.That(removed.GetCustomXmlParts().Count, Is.EqualTo(0));
        Assert.That(removed.Package.HasPart(partName), Is.False);
        Assert.That(HasRelationship(removed.Package.GetRelationships(PptxPartNames.Presentation), PptxRelationshipTypes.CustomXml), Is.False);
        Assert.That(removed.Package.Validate().Succeeded(), Is.True, removed.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Tags_ClearPreservesExtensionsAndRemovePartCleansRelationshipMarker()
    {
        PptxPresentation presentation = LoadTagCustomXmlPresentation();
        PptxSlideOperations operations = new PptxSlideOperations();
        PptxSlide slide = presentation.GetSlide(0);

        operations.ClearTags(slide);
        PptxPresentation cleared = SaveReopenAndLoad(presentation);
        PptxSlide clearedSlide = cleared.GetSlide(0);
        string clearedTagsXml = Encoding.UTF8.GetString(cleared.Package.GetPart("/ppt/tags/tag1.xml").GetBytes());

        Assert.That(clearedSlide.TagCount(), Is.EqualTo(0));
        Assert.That(clearedTagsXml, Does.Contain("Preserved Tag Extension"));
        Assert.That(operations.RemoveTagPart(clearedSlide), Is.True);
        Assert.That(operations.RemoveTagPart(clearedSlide), Is.False);

        PptxPresentation removed = SaveReopenAndLoad(cleared);
        string slideXml = Encoding.UTF8.GetString(removed.Package.GetPart(PptxPartNames.FirstSlide).GetBytes());

        Assert.That(removed.Package.HasPart("/ppt/tags/tag1.xml"), Is.False);
        Assert.That(HasRelationship(removed.Package.GetRelationships(PptxPartNames.FirstSlide), PptxRelationshipTypes.Tags), Is.False);
        Assert.That(slideXml, Does.Not.Contain("custDataLst"));
        Assert.That(slideXml, Does.Not.Contain("rIdTags"));
        Assert.That(removed.Package.Validate().Succeeded(), Is.True, removed.Package.Validate().FormatDiagnostics());
    }

    static PptxPresentation LoadTagCustomXmlPresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(CreateSlideXml()));
        package.AddPart("/ppt/tags/tag1.xml", PptxContentTypes.Tags, Encoding.UTF8.GetBytes(CreateTagsXml()));
        package.AddPart("/customXml/item1.xml", PptxContentTypes.Xml, Encoding.UTF8.GetBytes(CreateCustomXml()));
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdTags", PptxRelationshipTypes.Tags, "../tags/tag1.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationshipWithId(PptxPartNames.Presentation, "rIdCustomXml", PptxRelationshipTypes.CustomXml, "../customXml/item1.xml", PptxRelationshipTargetMode.Internal);
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
            "<p:sld xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld>" +
            "<p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>" +
            "</p:spTree>" +
            "</p:cSld>" +
            "<p:custDataLst><p:tags r:id=\"rIdTags\"/></p:custDataLst>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sld>";
    }

    static string CreateTagsXml()
    {
        return
            "<p:tagLst xmlns:p=\"" + PptxNamespaces.Presentation + "\" xmlns:x=\"http://example.com/tags\">" +
            "<p:tag name=\"Reviewed\" val=\"yes\"/>" +
            "<p:tag name=\"Owner\" val=\"Docs\"/>" +
            "<p:extLst><p:ext uri=\"preserve\"><x:data>Preserved Tag Extension</x:data></p:ext></p:extLst>" +
            "</p:tagLst>";
    }

    static string CreateCustomXml()
    {
        return "<root xmlns=\"urn:example\"><value>One</value></root>";
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
