using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class TextFrameTests
{
    [Test]
    public void Shape_LoadsTextFrameParagraphsRunsFieldsBreaksTabsBulletsNumberingAndFormatting()
    {
        PptxPresentation presentation = LoadTextPresentation();
        PptxShape shape = presentation.GetSlide(0).GetShapeTree().Get(0);

        PptxTextFrame frame = shape.GetTextFrame();
        PptxParagraph first = frame.GetParagraph(0);
        PptxRun formatted = first.GetRun(0);
        PptxRun tab = first.GetRun(1);
        PptxRun lineBreak = first.GetRun(3);
        PptxRun field = first.GetRun(4);
        PptxParagraph numbered = frame.GetParagraph(1);
        PptxParagraph none = frame.GetParagraph(2);
        PptxParagraph picture = frame.GetParagraph(3);

        Assert.That(frame.Present, Is.True);
        Assert.That(frame.ParagraphCount(), Is.EqualTo(4));
        Assert.That(frame.HasText(), Is.True);
        Assert.That(frame.GetText(), Is.EqualTo("Hello\tWorld\n1\nSecond\nNo bullet\nPicture bullet"));
        Assert.That(frame.BodyPropertiesXml, Does.Contain("wrap=\"square\""));
        Assert.That(frame.ListStyleXml, Does.Contain("lstStyle"));
        Assert.That(first.Alignment, Is.EqualTo("ctr"));
        Assert.That(first.Level, Is.EqualTo(1));
        Assert.That(first.HasBullet, Is.True);
        Assert.That(first.BulletType, Is.EqualTo(PptxBullet.Character));
        Assert.That(first.BulletCharacter, Is.EqualTo("-"));
        Assert.That(first.RunCount(), Is.EqualTo(5));
        Assert.That(formatted.Type, Is.EqualTo(PptxRun.TextRun));
        Assert.That(formatted.Text, Is.EqualTo("Hello"));
        Assert.That(formatted.Font.Language, Is.EqualTo("en-US"));
        Assert.That(formatted.Font.SizePoints, Is.EqualTo(24.0));
        Assert.That(formatted.Font.Bold, Is.True);
        Assert.That(formatted.Font.Italic, Is.True);
        Assert.That(formatted.Font.Underline, Is.True);
        Assert.That(formatted.Font.Strike, Is.True);
        Assert.That(formatted.Font.Baseline, Is.EqualTo(30000));
        Assert.That(formatted.Font.Family, Is.EqualTo("Aptos"));
        Assert.That(formatted.Font.EastAsianFamily, Is.EqualTo("Yu Gothic"));
        Assert.That(formatted.Font.ComplexScriptFamily, Is.EqualTo("Arial"));
        Assert.That(formatted.Font.FillColor.Type, Is.EqualTo(PptxColor.Rgb));
        Assert.That(formatted.Font.FillColor.Value, Is.EqualTo("FF0000"));
        Assert.That(formatted.Font.FillColor.Alpha, Is.EqualTo(75000));
        Assert.That(formatted.HyperlinkRelationshipId, Is.EqualTo("rIdLink"));
        Assert.That(formatted.HyperlinkTarget, Is.EqualTo("https://example.com/run"));
        Assert.That(tab.Type, Is.EqualTo(PptxRun.Tab));
        Assert.That(tab.Text, Is.EqualTo("\t"));
        Assert.That(lineBreak.Type, Is.EqualTo(PptxRun.LineBreak));
        Assert.That(lineBreak.Text, Is.EqualTo("\n"));
        Assert.That(field.Type, Is.EqualTo(PptxRun.Field));
        Assert.That(field.FieldId, Is.EqualTo("fld1"));
        Assert.That(field.FieldType, Is.EqualTo("slidenum"));
        Assert.That(field.Text, Is.EqualTo("1"));
        Assert.That(numbered.BulletType, Is.EqualTo(PptxBullet.Number));
        Assert.That(numbered.NumberingType, Is.EqualTo("arabicPeriod"));
        Assert.That(numbered.NumberingStartAt, Is.EqualTo(3));
        Assert.That(none.HasBullet, Is.False);
        Assert.That(none.BulletType, Is.EqualTo(PptxBullet.None));
        Assert.That(picture.HasBullet, Is.True);
        Assert.That(picture.BulletType, Is.EqualTo(PptxBullet.Picture));
        Assert.That(formatted.Xml, Does.Contain("<a:t>Hello</a:t>"));
        Assert.That(formatted.PropertiesXml, Does.Contain("hlinkClick"));
    }

    [Test]
    public void Shape_TextFrameReportsMissingTextBody()
    {
        PptxPresentation presentation = LoadTextPresentation();
        PptxShape connector = presentation.GetSlide(0).GetShapeTree().Get(1);

        PptxTextFrame frame = connector.GetTextFrame();

        Assert.That(frame.Present, Is.False);
        Assert.That(frame.ParagraphCount(), Is.EqualTo(0));
        Assert.That(frame.HasText(), Is.False);
        Assert.That(frame.GetText(), Is.Empty);
    }

    static PptxPresentation LoadTextPresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(CreateTextSlideXml()));
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdLink", PptxRelationshipTypes.Hyperlink, "https://example.com/run", PptxRelationshipTargetMode.External);
        PptxDocument document = PptxDocument.OpenBytes(package.SaveBytes());
        return document.LoadPresentation();
    }

    static string CreateTextSlideXml()
    {
        return
            "<p:sld xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld>" +
            "<p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>" +
            CreateTextShapeXml() +
            CreateConnectorXml() +
            "</p:spTree>" +
            "</p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sld>";
    }

    static string CreateTextShapeXml()
    {
        return
            "<p:sp>" +
            "<p:nvSpPr><p:cNvPr id=\"2\" name=\"Text Shape\"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>" +
            "<p:spPr>" +
            "<a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"100\" cy=\"100\"/></a:xfrm>" +
            "<a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom>" +
            "</p:spPr>" +
            "<p:txBody>" +
            "<a:bodyPr wrap=\"square\"/>" +
            "<a:lstStyle/>" +
            CreateCharacterBulletParagraphXml() +
            CreateNumberedParagraphXml() +
            CreateNoBulletParagraphXml() +
            CreatePictureBulletParagraphXml() +
            "</p:txBody>" +
            "</p:sp>";
    }

    static string CreateCharacterBulletParagraphXml()
    {
        return
            "<a:p>" +
            "<a:pPr algn=\"ctr\" lvl=\"1\"><a:buChar char=\"-\"/></a:pPr>" +
            "<a:r>" +
            "<a:rPr lang=\"en-US\" sz=\"2400\" b=\"1\" i=\"1\" u=\"sng\" strike=\"sngStrike\" baseline=\"30000\">" +
            "<a:solidFill><a:srgbClr val=\"FF0000\"><a:alpha val=\"75000\"/></a:srgbClr></a:solidFill>" +
            "<a:latin typeface=\"Aptos\"/>" +
            "<a:ea typeface=\"Yu Gothic\"/>" +
            "<a:cs typeface=\"Arial\"/>" +
            "<a:hlinkClick r:id=\"rIdLink\"/>" +
            "</a:rPr>" +
            "<a:t>Hello</a:t>" +
            "</a:r>" +
            "<a:tab/>" +
            "<a:r><a:t>World</a:t></a:r>" +
            "<a:br><a:rPr lang=\"en-US\"/></a:br>" +
            "<a:fld id=\"fld1\" type=\"slidenum\"><a:rPr lang=\"en-US\"/><a:t>1</a:t></a:fld>" +
            "<a:endParaRPr lang=\"en-US\"/>" +
            "</a:p>";
    }

    static string CreateNumberedParagraphXml()
    {
        return
            "<a:p>" +
            "<a:pPr><a:buAutoNum type=\"arabicPeriod\" startAt=\"3\"/></a:pPr>" +
            "<a:r><a:t>Second</a:t></a:r>" +
            "</a:p>";
    }

    static string CreateNoBulletParagraphXml()
    {
        return
            "<a:p>" +
            "<a:pPr><a:buNone/></a:pPr>" +
            "<a:r><a:t>No bullet</a:t></a:r>" +
            "</a:p>";
    }

    static string CreatePictureBulletParagraphXml()
    {
        return
            "<a:p>" +
            "<a:pPr><a:buBlip><a:blip r:embed=\"rIdBullet\"/></a:buBlip></a:pPr>" +
            "<a:r><a:t>Picture bullet</a:t></a:r>" +
            "</a:p>";
    }

    static string CreateConnectorXml()
    {
        return
            "<p:cxnSp>" +
            "<p:nvCxnSpPr><p:cNvPr id=\"3\" name=\"Connector\"/><p:cNvCxnSpPr/><p:nvPr/></p:nvCxnSpPr>" +
            "<p:spPr>" +
            "<a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"100\" cy=\"100\"/></a:xfrm>" +
            "<a:prstGeom prst=\"line\"><a:avLst/></a:prstGeom>" +
            "</p:spPr>" +
            "</p:cxnSp>";
    }
}
