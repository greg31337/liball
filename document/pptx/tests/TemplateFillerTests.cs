using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class TemplateFillerTests
{
    [Test]
    public void Presentation_FillTemplateReplacesDictionaryValuesAcrossPackageText()
    {
        PptxPresentation presentation = LoadTemplatePresentation();
        Dictionary<string, string> values = new Dictionary<string, string>();
        values.Add("ClientName", "Contoso");
        values.Add("Project", "Apollo");
        values.Add("Status", "Ready");

        presentation.SetSlideNotesText(0, "Notes for {{ClientName}} and {{Project}}");
        int count = presentation.FillTemplateFromDictionary(values);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        string extracted = reopened.ExtractText();

        Assert.That(count, Is.EqualTo(8));
        Assert.That(extracted, Does.Contain("Hello Contoso"));
        Assert.That(extracted, Does.Contain("Apollo Slide"));
        Assert.That(extracted, Does.Contain("Status Ready"));
        Assert.That(extracted, Does.Contain("Notes for Contoso and Apollo"));
        Assert.That(extracted, Does.Contain("Comment for Contoso"));
        Assert.That(extracted, Does.Contain("Chart Apollo"));
        Assert.That(extracted, Does.Contain("Ready Series"));
        Assert.That(extracted, Does.Not.Contain("{{"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Slide_FillTemplateUsesTemplateDataAndReplacesAcrossRuns()
    {
        PptxPresentation presentation = LoadRunTemplatePresentation();
        PptxTemplateData data = new PptxTemplateData();
        data.Add("Name", "Ada");
        data.Add("Name", "Grace");

        int count = presentation.GetSlide(0).FillTemplate(data);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxShape shape = reopened.GetSlide(0).GetShapeTree().Get(0);

        Assert.That(count, Is.EqualTo(1));
        Assert.That(data.Count(), Is.EqualTo(1));
        Assert.That(data.Contains(" Name "), Is.True);
        Assert.That(data.Get("Name"), Is.EqualTo("Grace"));
        Assert.That(shape.GetText(), Is.EqualTo("Hello Grace"));
        Assert.That(shape.GetTextFrame().GetParagraph(0).GetRun(0).Text, Is.EqualTo("Hello Grace"));
        Assert.That(shape.GetTextFrame().GetParagraph(0).GetRun(1).Text, Is.Empty);
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void FillTemplateListDataRequiresMatchingKeysAndValues()
    {
        PptxPresentation presentation = LoadTemplatePresentation();
        List<string> keys = new List<string>();
        List<string> values = new List<string>();
        keys.Add("ClientName");
        keys.Add("Project");
        values.Add("Contoso");

        Assert.That(
            delegate
            {
                presentation.FillTemplateFromLists(keys, values);
            },
            Throws.TypeOf<System.ArgumentException>());
    }

    [Test]
    public void TemplateDataRejectsBlankKeys()
    {
        PptxTemplateData data = new PptxTemplateData();

        Assert.That(
            delegate
            {
                data.Add(" ", "Ignored");
            },
            Throws.TypeOf<System.ArgumentException>());
    }

    static PptxPresentation LoadTemplatePresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(CreateTemplateSlideXml()));
        package.AddPart("/ppt/comments/comment1.xml", PptxContentTypes.Comments, Encoding.UTF8.GetBytes(CreateCommentsXml()));
        package.AddPart("/ppt/charts/chart1.xml", PptxContentTypes.Chart, Encoding.UTF8.GetBytes(CreateChartXml()));
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdComments", PptxRelationshipTypes.Comments, "../comments/comment1.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdChart", PptxRelationshipTypes.Chart, "../charts/chart1.xml", PptxRelationshipTargetMode.Internal);
        PptxDocument document = PptxDocument.OpenBytes(package.SaveBytes());
        return document.LoadPresentation();
    }

    static PptxPresentation LoadRunTemplatePresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(CreateRunTemplateSlideXml()));
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

    static string CreateTemplateSlideXml()
    {
        return
            "<p:sld xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:c=\"" + PptxNamespaces.Chart + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld>" +
            "<p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>" +
            CreateShapeXml("2", "Client", "Hello {{ClientName}}") +
            CreateShapeXml("3", "Project", "{{Project}} Slide") +
            CreateTableFrameXml() +
            CreateChartFrameXml() +
            "</p:spTree>" +
            "</p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sld>";
    }

    static string CreateRunTemplateSlideXml()
    {
        return
            "<p:sld xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld>" +
            "<p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>" +
            "<p:sp>" +
            "<p:nvSpPr><p:cNvPr id=\"2\" name=\"Run Template\"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>" +
            "<p:spPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"100\" cy=\"100\"/></a:xfrm><a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></p:spPr>" +
            "<p:txBody><a:bodyPr/><a:lstStyle/><a:p>" +
            "<a:r><a:rPr b=\"1\"/><a:t>Hello {{Na</a:t></a:r>" +
            "<a:r><a:rPr i=\"1\"/><a:t>me}}</a:t></a:r>" +
            "</a:p></p:txBody>" +
            "</p:sp>" +
            "</p:spTree>" +
            "</p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sld>";
    }

    static string CreateShapeXml(string id, string name, string text)
    {
        return
            "<p:sp>" +
            "<p:nvSpPr><p:cNvPr id=\"" + id + "\" name=\"" + name + "\"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>" +
            "<p:spPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"100\" cy=\"100\"/></a:xfrm><a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></p:spPr>" +
            "<p:txBody><a:bodyPr/><a:lstStyle/><a:p><a:r><a:t>" + text + "</a:t></a:r></a:p></p:txBody>" +
            "</p:sp>";
    }

    static string CreateTableFrameXml()
    {
        return
            "<p:graphicFrame>" +
            "<p:nvGraphicFramePr><p:cNvPr id=\"4\" name=\"Table\"/><p:cNvGraphicFramePr/><p:nvPr/></p:nvGraphicFramePr>" +
            "<p:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"400\" cy=\"200\"/></p:xfrm>" +
            "<a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/table\">" +
            "<a:tbl><a:tblPr/><a:tblGrid><a:gridCol w=\"200\"/></a:tblGrid>" +
            "<a:tr h=\"100\"><a:tc><a:txBody><a:bodyPr/><a:lstStyle/><a:p><a:r><a:t>Status {{Status}}</a:t></a:r></a:p></a:txBody><a:tcPr/></a:tc></a:tr>" +
            "</a:tbl>" +
            "</a:graphicData></a:graphic>" +
            "</p:graphicFrame>";
    }

    static string CreateChartFrameXml()
    {
        return
            "<p:graphicFrame>" +
            "<p:nvGraphicFramePr><p:cNvPr id=\"5\" name=\"Chart\"/><p:cNvGraphicFramePr/><p:nvPr/></p:nvGraphicFramePr>" +
            "<p:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"400\" cy=\"200\"/></p:xfrm>" +
            "<a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/chart\"><c:chart r:id=\"rIdChart\"/></a:graphicData></a:graphic>" +
            "</p:graphicFrame>";
    }

    static string CreateCommentsXml()
    {
        return
            "<p:cmLst xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cm authorId=\"0\" dt=\"2026-01-01T00:00:00Z\" idx=\"0\"><p:text>Comment for {{ClientName}}</p:text></p:cm>" +
            "</p:cmLst>";
    }

    static string CreateChartXml()
    {
        return
            "<c:chartSpace xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:c=\"" + PptxNamespaces.Chart + "\">" +
            "<c:chart>" +
            "<c:title><c:tx><c:rich><a:bodyPr/><a:lstStyle/><a:p><a:r><a:t>Chart {{Project}}</a:t></a:r></a:p></c:rich></c:tx></c:title>" +
            "<c:plotArea><c:barChart><c:ser><c:tx><c:strRef><c:strCache><c:pt idx=\"0\"><c:v>{{Status}} Series</c:v></c:pt></c:strCache></c:strRef></c:tx></c:ser></c:barChart></c:plotArea>" +
            "</c:chart>" +
            "</c:chartSpace>";
    }
}
