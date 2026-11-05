using System.Text;
using System.Xml;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PptxMarkupCompatibilityPreservationTests
{
    public const string PowerPoint2010Namespace = "http://schemas.microsoft.com/office/powerpoint/2010/main";

    [Test]
    public void SaveBytes_PreservesMarkupCompatibilityAndExtensionListsInCleanParts()
    {
        string slideXml = CreateSlideXml();
        PptxPackage package = PptxPackage.Create();
        package.AddPart("/ppt/slides/slide1.xml", PptxContentTypes.Slide, Encoding.UTF8.GetBytes(slideXml));

        PptxPackage opened = PptxPackage.OpenBytes(package.SaveBytes());
        PptxPackage reopened = PptxPackage.OpenBytes(opened.SaveBytes());

        Assert.That(Encoding.UTF8.GetString(reopened.GetPart("/ppt/slides/slide1.xml").Bytes), Is.EqualTo(slideXml));
    }

    [Test]
    public void SaveBytes_PreservesMarkupCompatibilityAndExtensionListsAfterDirtyXmlSerialization()
    {
        PptxPackage package = PptxPackage.Create();
        package.AddPart("/ppt/slides/slide1.xml", PptxContentTypes.Slide, Encoding.UTF8.GetBytes(CreateSlideXml()));
        PptxPackage opened = PptxPackage.OpenBytes(package.SaveBytes());
        PptxPart slide = opened.GetPart("/ppt/slides/slide1.xml");
        XmlDocument document = slide.LoadXmlDocument();

        PptxXml.SetAttributeValue(document.DocumentElement!, "", "", "data-edit", "1");
        slide.MarkDirty();

        PptxPackage saved = PptxPackage.OpenBytes(opened.SaveBytes());
        string savedXml = Encoding.UTF8.GetString(saved.GetPart("/ppt/slides/slide1.xml").Bytes);
        XmlDocument savedDocument = PptxXml.LoadString(savedXml);
        XmlNamespaceManager manager = PptxXml.CreateNamespaceManager(savedDocument);
        manager.AddNamespace("p14", PowerPoint2010Namespace);

        Assert.That(savedXml, Does.Contain("mc:Ignorable=\"p14\""));
        Assert.That(savedXml, Does.Contain("xmlns:p14=\"" + PowerPoint2010Namespace + "\""));
        Assert.That(savedXml, Does.Contain("data-edit=\"1\""));
        Assert.That(savedDocument.SelectSingleNode("/p:sld/mc:AlternateContent/mc:Choice/p14:creationId", manager), Is.Not.Null);
        Assert.That(savedDocument.SelectSingleNode("/p:sld/mc:AlternateContent/mc:Fallback/p:empty", manager), Is.Not.Null);
        Assert.That(savedDocument.SelectSingleNode("/p:sld/p:extLst/p:ext/p14:slideGuide", manager), Is.Not.Null);
        Assert.That(savedDocument.SelectSingleNode("//a:extLst/a:ext/p14:shapeStyle", manager), Is.Not.Null);
    }

    [Test]
    public void WriteString_PreservesAlternateContentChoiceRequiresPrefixDeclaration()
    {
        XmlDocument document = PptxXml.LoadString(CreateSlideXml());
        string serialized = PptxXml.WriteString(document);
        XmlDocument reparsed = PptxXml.LoadString(serialized);
        XmlNamespaceManager manager = PptxXml.CreateNamespaceManager(reparsed);
        manager.AddNamespace("p14", PowerPoint2010Namespace);

        Assert.That(serialized, Does.Contain("<mc:AlternateContent"));
        Assert.That(serialized, Does.Contain("<mc:Choice Requires=\"p14\">"));
        Assert.That(serialized, Does.Contain("xmlns:p14=\"" + PowerPoint2010Namespace + "\""));
        Assert.That(reparsed.SelectSingleNode("/p:sld/mc:AlternateContent/mc:Choice/p14:creationId", manager), Is.Not.Null);
    }

    public static string CreateSlideXml()
    {
        return
            "<p:sld xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\" " +
            "xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" " +
            "xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\" " +
            "xmlns:p14=\"" + PowerPoint2010Namespace + "\" " +
            "mc:Ignorable=\"p14\">" +
            "<p:cSld>" +
            "<p:spTree>" +
            "<p:sp>" +
            "<p:nvSpPr />" +
            "<p:spPr>" +
            "<a:extLst>" +
            "<a:ext uri=\"{11111111-1111-1111-1111-111111111111}\">" +
            "<p14:shapeStyle val=\"preserve-shape\" />" +
            "</a:ext>" +
            "</a:extLst>" +
            "</p:spPr>" +
            "</p:sp>" +
            "</p:spTree>" +
            "</p:cSld>" +
            "<mc:AlternateContent>" +
            "<mc:Choice Requires=\"p14\">" +
            "<p14:creationId val=\"123456\" />" +
            "</mc:Choice>" +
            "<mc:Fallback>" +
            "<p:empty />" +
            "</mc:Fallback>" +
            "</mc:AlternateContent>" +
            "<p:extLst>" +
            "<p:ext uri=\"{22222222-2222-2222-2222-222222222222}\">" +
            "<p14:slideGuide val=\"preserve-slide\" />" +
            "</p:ext>" +
            "</p:extLst>" +
            "</p:sld>";
    }
}
