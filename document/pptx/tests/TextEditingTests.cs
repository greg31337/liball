using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class TextEditingTests
{
    [Test]
    public void Slide_AddTextBoxAndShapeTextEditingRoundTrips()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        PptxShape shape = slide.AddTextBox("One\nTwo\tThree", new PptxBounds(10L, 20L, 300L, 120L));

        shape.AddTextParagraph("Four");
        shape.GetTextFrame().GetParagraph(2).SetText("Four\tFive");
        shape.GetTextFrame().GetParagraph(0).GetRun(0).SetText("Uno");
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxShape reopenedShape = reopened.GetSlide(0).GetShapeTree().Get(0);

        Assert.That(shape.IsTextBox(), Is.True);
        Assert.That(shape.GetText(), Is.EqualTo("Uno\nTwo\tThree\nFour\tFive"));
        Assert.That(reopenedShape.IsTextBox(), Is.True);
        Assert.That(reopenedShape.GetText(), Is.EqualTo("Uno\nTwo\tThree\nFour\tFive"));
        Assert.That(reopenedShape.GetTextFrame().GetParagraph(1).GetRun(1).Type, Is.EqualTo(PptxRun.Tab));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Slide_SetPlaceholderTextPreservesParagraphsAndTabs()
    {
        PptxPresentation presentation = LoadPlaceholderPresentation();
        PptxSlide slide = presentation.GetSlide(0);

        slide.SetPlaceholderText("title", "Title\nSubtitle\tTabbed");
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        Assert.That(slide.GetPlaceholderText("title"), Is.EqualTo("Title\nSubtitle\tTabbed"));
        Assert.That(reopened.GetSlide(0).GetPlaceholderText("title"), Is.EqualTo("Title\nSubtitle\tTabbed"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Slide_SetTableCellTextWritesCellTextBodies()
    {
        PptxPresentation presentation = LoadTablePresentation();
        PptxSlide slide = presentation.GetSlide(0);

        slide.SetTableCellText(1, 1, "Bottom\nRight\tCell");
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        Assert.That(slide.GetTableCellText(1, 1), Is.EqualTo("Bottom\nRight\tCell"));
        Assert.That(reopened.GetSlide(0).GetTableCellText(1, 1), Is.EqualTo("Bottom\nRight\tCell"));
        Assert.That(reopened.GetSlide(0).GetTableCellText(0, 0), Is.EqualTo("A1"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Presentation_SetSlideNotesTextCreatesNotesPartAndRoundTrips()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();

        presentation.SetSlideNotesText(0, "Speaker\nNotes\tHere");
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        Assert.That(presentation.Package.HasPart(PptxPartNames.FirstNotesSlide), Is.True);
        Assert.That(presentation.GetSlideNotesText(0), Is.EqualTo("Speaker\nNotes\tHere"));
        Assert.That(reopened.GetSlideNotesText(0), Is.EqualTo("Speaker\nNotes\tHere"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    static PptxPresentation LoadGeneratedPresentation()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        byte[] bytes = document.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        return reopened.LoadPresentation();
    }

    static PptxPresentation LoadPlaceholderPresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(CreatePlaceholderSlideXml()));
        PptxDocument document = PptxDocument.OpenBytes(package.SaveBytes());
        return document.LoadPresentation();
    }

    static PptxPresentation LoadTablePresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(CreateTableSlideXml()));
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

    static string CreatePlaceholderSlideXml()
    {
        return
            "<p:sld xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld>" +
            "<p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>" +
            "<p:sp>" +
            "<p:nvSpPr><p:cNvPr id=\"2\" name=\"Title\"/><p:cNvSpPr/><p:nvPr><p:ph type=\"title\" idx=\"1\"/></p:nvPr></p:nvSpPr>" +
            "<p:spPr/>" +
            "<p:txBody><a:bodyPr/><a:lstStyle/><a:p><a:r><a:t>Old</a:t></a:r></a:p></p:txBody>" +
            "</p:sp>" +
            "</p:spTree>" +
            "</p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sld>";
    }

    static string CreateTableSlideXml()
    {
        return
            "<p:sld xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld>" +
            "<p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>" +
            "<p:graphicFrame>" +
            "<p:nvGraphicFramePr><p:cNvPr id=\"2\" name=\"Table\"/><p:cNvGraphicFramePr/><p:nvPr/></p:nvGraphicFramePr>" +
            "<p:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"400\" cy=\"200\"/></p:xfrm>" +
            "<a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/table\">" +
            CreateTableXml() +
            "</a:graphicData></a:graphic>" +
            "</p:graphicFrame>" +
            "</p:spTree>" +
            "</p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sld>";
    }

    static string CreateTableXml()
    {
        return
            "<a:tbl>" +
            "<a:tblPr/>" +
            "<a:tblGrid><a:gridCol w=\"200\"/><a:gridCol w=\"200\"/></a:tblGrid>" +
            "<a:tr h=\"100\">" +
            CreateCellXml("A1") +
            CreateCellXml("A2") +
            "</a:tr>" +
            "<a:tr h=\"100\">" +
            CreateCellXml("B1") +
            CreateCellXml("B2") +
            "</a:tr>" +
            "</a:tbl>";
    }

    static string CreateCellXml(string text)
    {
        return
            "<a:tc>" +
            "<a:txBody><a:bodyPr/><a:lstStyle/><a:p><a:r><a:t>" + text + "</a:t></a:r></a:p></a:txBody>" +
            "<a:tcPr/>" +
            "</a:tc>";
    }
}
