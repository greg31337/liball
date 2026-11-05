using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class TextOutlineExportTests
{
    [Test]
    public void Presentation_ExportTextUsesExistingFullTextExtraction()
    {
        PptxPresentation presentation = LoadOutlinePresentation();
        presentation.SetSlideNotesText(0, "Speaker Notes");

        string exported = presentation.ExportText();

        Assert.That(exported, Is.EqualTo(presentation.ExtractText()));
        Assert.That(exported, Does.Contain("Title"));
        Assert.That(exported, Does.Contain("First Bullet"));
        Assert.That(exported, Does.Contain("Speaker Notes"));
    }

    [Test]
    public void Presentation_ExportOutlineIncludesSlideHeadersLevelsAndBulletPrefixes()
    {
        PptxPresentation presentation = LoadOutlinePresentation();
        PptxSlide slide = presentation.GetSlide(0);

        string presentationOutline = presentation.ExportOutline();
        string slideOutline = slide.ExportOutline();
        List<PptxOutlineEntry> entries = presentation.GetOutlineEntries();

        Assert.That(
            presentationOutline,
            Is.EqualTo("Slide 1\nTitle\n  - First Bullet\n    3. Numbered Item\nPlain"));
        Assert.That(
            slideOutline,
            Is.EqualTo("Title\n  - First Bullet\n    3. Numbered Item\nPlain"));
        Assert.That(entries.Count, Is.EqualTo(4));
        Assert.That(entries[0].SlideIndex, Is.EqualTo(0));
        Assert.That(entries[0].ShapeId, Is.EqualTo(2));
        Assert.That(entries[0].ShapeName, Is.EqualTo("Outline Shape"));
        Assert.That(entries[1].Level, Is.EqualTo(1));
        Assert.That(entries[1].HasBullet, Is.True);
        Assert.That(entries[1].BulletType, Is.EqualTo(PptxBullet.Character));
        Assert.That(entries[1].BulletCharacter, Is.EqualTo("-"));
        Assert.That(entries[2].Level, Is.EqualTo(2));
        Assert.That(entries[2].BulletType, Is.EqualTo(PptxBullet.Number));
        Assert.That(entries[2].NumberingStartAt, Is.EqualTo(3));
        Assert.That(entries[3].HasBullet, Is.False);
        Assert.That(slide.GetOutlineEntries().Count, Is.EqualTo(4));
    }

    static PptxPresentation LoadOutlinePresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(CreateOutlineSlideXml()));
        PptxDocument document = PptxDocument.OpenBytes(package.SaveBytes());
        return document.LoadPresentation();
    }

    static string CreateOutlineSlideXml()
    {
        return
            "<p:sld xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld>" +
            "<p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>" +
            "<p:sp>" +
            "<p:nvSpPr><p:cNvPr id=\"2\" name=\"Outline Shape\"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>" +
            "<p:spPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"600\" cy=\"300\"/></a:xfrm><a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></p:spPr>" +
            "<p:txBody><a:bodyPr/><a:lstStyle/>" +
            "<a:p><a:r><a:t>Title</a:t></a:r></a:p>" +
            "<a:p><a:pPr lvl=\"1\"><a:buChar char=\"-\"/></a:pPr><a:r><a:t>First Bullet</a:t></a:r></a:p>" +
            "<a:p><a:pPr lvl=\"2\"><a:buAutoNum type=\"arabicPeriod\" startAt=\"3\"/></a:pPr><a:r><a:t>Numbered Item</a:t></a:r></a:p>" +
            "<a:p><a:pPr><a:buNone/></a:pPr><a:r><a:t>Plain</a:t></a:r></a:p>" +
            "</p:txBody>" +
            "</p:sp>" +
            "</p:spTree>" +
            "</p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sld>";
    }
}
