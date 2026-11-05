using NUnit.Framework;
using System.Linq;
using System.Xml.Linq;

namespace docx.Tests;

[TestFixture]
public class DocxDrawingTests
{
    [Test]
    public void InlineImageDrawing_WritesExpectedDrawingMarkup()
    {
        global::docx.DocxImage image = global::docx.DocxImage.Create("/word/document.xml", "/word/media/image1.png", "rImage1", global::docx.DocxConstants.ContentTypePng, new byte[] { 1, 2 });

        global::docx.DocxDrawing drawing = global::docx.DocxDrawing.CreateInlineImage(image, 914400, 457200);
        string xml = drawing.WriteXml();
        XDocument document = XDocument.Parse(xml);
        XNamespace w = global::docx.DocxConstants.NamespaceWordprocessingMl;
        XNamespace wp = global::docx.DocxConstants.NamespaceWordprocessingDrawing;
        XNamespace a = global::docx.DocxConstants.NamespaceDrawingMl;
        XNamespace pic = global::docx.DocxConstants.NamespaceDrawingMlPicture;
        XNamespace r = global::docx.DocxConstants.NamespaceRelationships;
        XElement extent = document.Descendants(wp + "extent").Single();
        XElement docProperties = document.Descendants(wp + "docPr").Single();
        XElement graphicData = document.Descendants(a + "graphicData").Single();
        XElement blip = document.Descendants(a + "blip").Single();
        XElement picture = document.Descendants(pic + "pic").Single();
        XElement shapeExtent = document.Descendants(a + "ext").Single();

        Assert.That(drawing.RelationshipId, Is.EqualTo("rImage1"));
        Assert.That(drawing.Name, Is.EqualTo("image1.png"));
        Assert.That(drawing.WidthEmu, Is.EqualTo(914400));
        Assert.That(drawing.HeightEmu, Is.EqualTo(457200));
        Assert.That(document.Root!.Name, Is.EqualTo(w + "r"));
        Assert.That(document.Descendants(w + "drawing").Count(), Is.EqualTo(1));
        Assert.That(document.Descendants(wp + "inline").Count(), Is.EqualTo(1));
        Assert.That((string?)extent.Attribute("cx"), Is.EqualTo("914400"));
        Assert.That((string?)extent.Attribute("cy"), Is.EqualTo("457200"));
        Assert.That((string?)docProperties.Attribute("name"), Is.EqualTo("image1.png"));
        Assert.That((string?)docProperties.Attribute("descr"), Is.EqualTo(""));
        Assert.That((string?)graphicData.Attribute("uri"), Is.EqualTo(global::docx.DocxConstants.NamespaceDrawingMlPicture));
        Assert.That((string?)blip.Attribute(r + "embed"), Is.EqualTo("rImage1"));
        Assert.That(picture.Name, Is.EqualTo(pic + "pic"));
        Assert.That((string?)shapeExtent.Attribute("cx"), Is.EqualTo("914400"));
        Assert.That((string?)shapeExtent.Attribute("cy"), Is.EqualTo("457200"));
    }

    [Test]
    public void ParagraphAddInlineImage_AppendsDrawingRun()
    {
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");
        global::docx.DocxImage image = global::docx.DocxImage.Create("/word/document.xml", "/word/media/photo.jpg", "rPhoto", global::docx.DocxConstants.ContentTypeJpeg, new byte[] { 255, 216, 255, 224 });

        global::docx.DocxDrawing drawing = paragraph.AddInlineImage(image, 1828800, 914400);
        global::docx.DocxParagraph written = ParseParagraph(paragraph.WriteXml());

        Assert.That(drawing.RelationshipId, Is.EqualTo("rPhoto"));
        Assert.That(paragraph.ChildCount(), Is.EqualTo(1));
        Assert.That(paragraph.RunElementCount(), Is.EqualTo(1));
        Assert.That(paragraph.RunCount(), Is.EqualTo(1));
        Assert.That(written.ChildCount(), Is.EqualTo(1));
        Assert.That(written.RunElementCount(), Is.EqualTo(1));
        Assert.That(written.RunCount(), Is.EqualTo(1));
        Assert.That(written.GetRun(0).UnknownElementCount(), Is.EqualTo(1));
        Assert.That(written.GetRun(0).CountUnknownElements("drawing", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.EqualTo(1));
        Assert.That(written.WriteXml(), Does.Contain("r:embed=\"rPhoto\""));
        Assert.That(written.WriteXml(), Does.Contain("cx=\"1828800\""));
        Assert.That(written.WriteXml(), Does.Contain("cy=\"914400\""));
    }

    [Test]
    public void DocumentImageAndParagraphDrawing_RoundTripRelationshipAndInlineMarkup()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        byte[] png = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        global::docx.DocxImage image = document.AddImageToMainDocument(png, "");
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");

        global::docx.DocxDrawing drawing = paragraph.AddInlineImage(image, 914400, 914400);

        global::docx.DocxParagraph parsed = ParseParagraph(paragraph.WriteXml());
        global::docx.DocxRelationship relationship = document.Package.FindRelationshipSet("/word/document.xml")!.FindById(image.RelationshipId)!;

        Assert.That(drawing.RelationshipId, Is.EqualTo(image.RelationshipId));
        Assert.That(relationship.Target, Is.EqualTo("media/image1.png"));
        Assert.That(parsed.RunCount(), Is.EqualTo(1));
        Assert.That(parsed.GetRun(0).DrawingCount(), Is.EqualTo(1));
        Assert.That(parsed.GetRun(0).GetDrawing(0).IsInline(), Is.True);
        Assert.That(parsed.WriteXml(), Does.Contain("r:embed=\"" + image.RelationshipId + "\""));
        Assert.That(document.FindImage("/word/document.xml", image.RelationshipId), Is.SameAs(image));
    }

    [Test]
    public void InlineImageDrawing_ParseGeneratedXmlPreservesInlineKind()
    {
        global::docx.DocxDrawing drawing = global::docx.DocxDrawing.CreateInlineImage("rImage7", "photo.png", "Photo", 1000, 2000);

        global::docx.DocxRun run = ParseRun(drawing.WriteXml());
        global::docx.DocxDrawing parsed = run.GetDrawing(0);

        Assert.That(run.DrawingCount(), Is.EqualTo(1));
        Assert.That(parsed.IsInline(), Is.True);
        Assert.That(parsed.IsFloating(), Is.False);
        Assert.That(parsed.ContainsChart(), Is.False);
        Assert.That(parsed.ContainsDiagram(), Is.False);
        Assert.That(parsed.ContainsVml(), Is.False);
        Assert.That(parsed.ContainsEmbeddedObject(), Is.False);
        Assert.That(parsed.WriteXml(), Does.Contain("r:embed=\"rImage7\""));
        Assert.That(parsed.WriteXml(), Does.Contain("descr=\"Photo\""));
    }

    [Test]
    public void RunParsing_PreservesFloatingChartDiagramVmlAndEmbeddedObjects()
    {
        global::docx.DocxRun run = ParseRun(CreateRunWithPreservedDrawingsXml());

        Assert.That(run.ChildCount(), Is.EqualTo(4));
        Assert.That(run.UnknownElementCount(), Is.EqualTo(4));
        Assert.That(run.DrawingCount(), Is.EqualTo(4));
        Assert.That(run.GetDrawing(0).Kind, Is.EqualTo(global::docx.DocxDrawing.KindFloating));
        Assert.That(run.GetDrawing(0).IsFloating(), Is.True);
        Assert.That(run.GetDrawing(0).ContainsChart(), Is.True);
        Assert.That(run.GetDrawing(1).IsInline(), Is.True);
        Assert.That(run.GetDrawing(1).ContainsDiagram(), Is.True);
        Assert.That(run.GetDrawing(2).Kind, Is.EqualTo(global::docx.DocxDrawing.KindVml));
        Assert.That(run.GetDrawing(2).ContainsVml(), Is.True);
        Assert.That(run.GetDrawing(3).Kind, Is.EqualTo(global::docx.DocxDrawing.KindEmbeddedObject));
        Assert.That(run.GetDrawing(3).ContainsEmbeddedObject(), Is.True);
        Assert.That(run.CountDrawings(global::docx.DocxDrawing.KindFloating), Is.EqualTo(1));
        Assert.That(run.CountDrawings(global::docx.DocxDrawing.KindChart), Is.EqualTo(1));
        Assert.That(run.CountDrawings(global::docx.DocxDrawing.KindDiagram), Is.EqualTo(1));
        Assert.That(run.CountDrawings(global::docx.DocxDrawing.KindVml), Is.EqualTo(2));
        Assert.That(run.CountDrawings(global::docx.DocxDrawing.KindEmbeddedObject), Is.EqualTo(1));
        Assert.That(run.GetDrawing(0).WriteXml(), Does.Contain("<wp:anchor"));
        Assert.That(run.GetDrawing(0).WriteXml(), Does.Contain("<c:chart"));
        Assert.That(run.GetDrawing(2).WriteXml(), Does.Contain("<v:shape"));
        Assert.That(run.GetDrawing(3).WriteXml(), Does.Contain("<o:OLEObject"));

        global::docx.DocxRun written = ParseRun(run.WriteXml());

        Assert.That(written.DrawingCount(), Is.EqualTo(4));
        Assert.That(written.GetDrawing(0).IsFloating(), Is.True);
        Assert.That(written.GetDrawing(1).ContainsDiagram(), Is.True);
        Assert.That(written.GetDrawing(2).ContainsVml(), Is.True);
        Assert.That(written.GetDrawing(3).ContainsEmbeddedObject(), Is.True);
        Assert.That(written.WriteXml(), Does.Contain("r:id=\"rChart\""));
        Assert.That(written.WriteXml(), Does.Contain("r:id=\"rOle\""));
    }

    [Test]
    public void RunAddDrawing_AppendsPreservedDrawingAndUnknownElement()
    {
        global::docx.DocxRun run = new global::docx.DocxRun("");
        global::docx.DocxDrawing drawing = global::docx.DocxDrawing.ParseXml("<w:pict xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" xmlns:v=\"urn:schemas-microsoft-com:vml\"><v:shape id=\"vml1\" /></w:pict>");

        run.AddDrawing(drawing);

        global::docx.DocxRun written = ParseRun(run.WriteXml());

        Assert.That(run.ChildCount(), Is.EqualTo(1));
        Assert.That(run.DrawingCount(), Is.EqualTo(1));
        Assert.That(run.UnknownElementCount(), Is.EqualTo(1));
        Assert.That(written.DrawingCount(), Is.EqualTo(1));
        Assert.That(written.GetDrawing(0).ContainsVml(), Is.True);
        Assert.That(written.WriteXml(), Does.Contain("vml1"));
    }

    [Test]
    public void InlineImageDrawing_UsesFallbackNameWhenImagePartNameIsEmpty()
    {
        global::docx.DocxImage image = new global::docx.DocxImage();
        image.RelationshipId = "rImage1";

        global::docx.DocxDrawing drawing = global::docx.DocxDrawing.CreateInlineImage(image, 1, 1);

        Assert.That(drawing.Name, Is.EqualTo(global::docx.DocxDrawing.DefaultName));
        Assert.That(global::docx.DocxDrawing.BuildImageName(image), Is.EqualTo(global::docx.DocxDrawing.DefaultName));
    }

    [Test]
    public void InlineImageDrawing_RejectsInvalidArguments()
    {
        global::docx.DocxImage image = global::docx.DocxImage.Create("/word/document.xml", "/word/media/image1.png", "rImage1", global::docx.DocxConstants.ContentTypePng, new byte[] { 1 });
        global::docx.DocxDrawing drawing = global::docx.DocxDrawing.CreateInlineImage("rImage1", "Image", "", 1, 1);
        global::docx.DocxRun run = new global::docx.DocxRun("");
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create("<root />");

        Assert.Throws<ArgumentNullException>(() => global::docx.DocxDrawing.IsSupported(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxDrawing.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxDrawing.ParseXml(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxDrawing.CreateInlineImage((global::docx.DocxImage)null!, 1, 1));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxDrawing.CreateInlineImage(null!, "Image", "", 1, 1));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxDrawing.CreateInlineImage("rImage1", null!, "", 1, 1));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxDrawing.CreateInlineImage("rImage1", "Image", null!, 1, 1));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxDrawing.BuildImageName(null!));
        Assert.Throws<ArgumentNullException>(() => drawing.SetInlineImage(null!, "Image", "", 1, 1));
        Assert.Throws<ArgumentNullException>(() => drawing.SetInlineImage("rImage1", null!, "", 1, 1));
        Assert.Throws<ArgumentNullException>(() => drawing.SetInlineImage("rImage1", "Image", null!, 1, 1));
        Assert.Throws<ArgumentNullException>(() => drawing.SetPreservedXml(null!, "w", "drawing", global::docx.DocxConstants.NamespaceWordprocessingMl));
        Assert.Throws<ArgumentNullException>(() => drawing.SetPreservedXml("", null!, "drawing", global::docx.DocxConstants.NamespaceWordprocessingMl));
        Assert.Throws<ArgumentNullException>(() => drawing.SetPreservedXml("", "w", null!, global::docx.DocxConstants.NamespaceWordprocessingMl));
        Assert.Throws<ArgumentNullException>(() => drawing.SetPreservedXml("", "w", "drawing", null!));
        Assert.Throws<ArgumentNullException>(() => drawing.AnalyzeElement(null!, global::docx.DocxConstants.NamespaceWordprocessingMl));
        Assert.Throws<ArgumentNullException>(() => drawing.AnalyzeElement("drawing", null!));
        Assert.Throws<ArgumentNullException>(() => drawing.IsKind(null!));
        Assert.Throws<ArgumentNullException>(() => run.AddDrawing(null!));
        Assert.Throws<ArgumentNullException>(() => run.CountDrawings(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRun.DocxDrawingFromBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => drawing.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => drawing.WriteExtentTo(null!));
        Assert.Throws<ArgumentNullException>(() => drawing.WriteDocPropertiesTo(null!));
        Assert.Throws<ArgumentNullException>(() => drawing.WriteNonVisualGraphicFramePropertiesTo(null!));
        Assert.Throws<ArgumentNullException>(() => drawing.WriteGraphicTo(null!));
        Assert.Throws<ArgumentNullException>(() => drawing.WritePictureTo(null!));
        Assert.Throws<ArgumentNullException>(() => drawing.WriteNonVisualPicturePropertiesTo(null!));
        Assert.Throws<ArgumentNullException>(() => drawing.WriteBlipFillTo(null!));
        Assert.Throws<ArgumentNullException>(() => drawing.WriteShapePropertiesTo(null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxDrawing.CreateInlineImage("", "Image", "", 1, 1));
        Assert.Throws<ArgumentException>(() => global::docx.DocxDrawing.CreateInlineImage("rImage1", "", "", 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => global::docx.DocxDrawing.CreateInlineImage("rImage1", "Image", "", 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => global::docx.DocxDrawing.CreateInlineImage("rImage1", "Image", "", 1, 0));
        Assert.Throws<ArgumentException>(() => new global::docx.DocxDrawing().WriteXml());
        Assert.Throws<ArgumentException>(() => drawing.SetPreservedXml("", "w", "", global::docx.DocxConstants.NamespaceWordprocessingMl));
        Assert.Throws<ArgumentException>(() => drawing.IsKind(""));
        Assert.Throws<ArgumentException>(() => run.CountDrawings(""));
        Assert.Throws<ArgumentException>(() => global::docx.DocxDrawing.CreateInlineImage(global::docx.DocxImage.Create("/word/document.xml", "/word/media/image1.png", "", global::docx.DocxConstants.ContentTypePng, new byte[] { 1 }), 1, 1));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxDrawing.Parse(reader));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxDrawing.ParseXml("<root />"));
        Assert.DoesNotThrow(() => global::docx.DocxDrawing.CreateInlineImage(image, 1, 1));
    }

    static global::docx.DocxParagraph ParseParagraph(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("p", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxParagraph.Parse(reader);
    }

    static global::docx.DocxRun ParseRun(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("r", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxRun.Parse(reader);
    }

    static string CreateRunWithPreservedDrawingsXml()
    {
        return string.Join(
            "",
            new string[]
            {
                "<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" xmlns:wp=\"http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing\" xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" xmlns:c=\"http://schemas.openxmlformats.org/drawingml/2006/chart\" xmlns:dgm=\"http://schemas.openxmlformats.org/drawingml/2006/diagram\" xmlns:v=\"urn:schemas-microsoft-com:vml\" xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">",
                "<w:drawing><wp:anchor><a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/chart\"><c:chart r:id=\"rChart\" /></a:graphicData></a:graphic></wp:anchor></w:drawing>",
                "<w:drawing><wp:inline><a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/diagram\"><dgm:relIds r:dm=\"rDm\" /></a:graphicData></a:graphic></wp:inline></w:drawing>",
                "<w:pict><v:shape id=\"vml1\" /></w:pict>",
                "<w:object><v:shape id=\"oleShape\" /><o:OLEObject r:id=\"rOle\" /></w:object>",
                "</w:r>"
            });
    }
}
