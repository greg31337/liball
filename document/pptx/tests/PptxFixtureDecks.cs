using System.Collections.Generic;
using System.Text;

namespace pptx.Tests;

public class PptxFixtureDecks
{
    public const string PowerPoint = "powerpoint";
    public const string LibreOffice = "libreoffice";
    public const string GoogleSlides = "google-slides";
    public const string Keynote = "keynote";
    public const string LibraryGenerated = "library-generated";

    public static List<PptxFixtureDeck> CreateAll()
    {
        List<PptxFixtureDeck> decks = new List<PptxFixtureDeck>();
        decks.Add(CreatePowerPoint());
        decks.Add(CreateLibreOffice());
        decks.Add(CreateGoogleSlides());
        decks.Add(CreateKeynote());
        decks.Add(CreateLibraryGenerated());
        return decks;
    }

    public static PptxFixtureDeck Create(string family)
    {
        if (family == PowerPoint)
        {
            return CreatePowerPoint();
        }

        if (family == LibreOffice)
        {
            return CreateLibreOffice();
        }

        if (family == GoogleSlides)
        {
            return CreateGoogleSlides();
        }

        if (family == Keynote)
        {
            return CreateKeynote();
        }

        if (family == LibraryGenerated)
        {
            return CreateLibraryGenerated();
        }

        throw new System.ArgumentException("Unknown fixture family.");
    }

    public static PptxFixtureDeck CreatePowerPoint()
    {
        return CreateDeck(PowerPoint, "PowerPoint", "Microsoft PowerPoint", "PowerPoint-authored fixture", "p14", CreatePowerPointExtensionXml());
    }

    public static PptxFixtureDeck CreateLibreOffice()
    {
        return CreateDeck(LibreOffice, "LibreOffice", "LibreOffice", "LibreOffice Impress export fixture", "loext", CreateLibreOfficeExtensionXml());
    }

    public static PptxFixtureDeck CreateGoogleSlides()
    {
        return CreateDeck(GoogleSlides, "Google Slides", "Google", "Google Slides PPTX export fixture", "gs", CreateGoogleSlidesExtensionXml());
    }

    public static PptxFixtureDeck CreateKeynote()
    {
        return CreateDeck(Keynote, "Keynote", "Keynote", "Apple Keynote PPTX export fixture", "key", CreateKeynoteExtensionXml());
    }

    public static PptxFixtureDeck CreateLibraryGenerated()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        PptxPresentation presentation = document.LoadPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        slide.AddTextBox("library-generated fixture deck", new PptxBounds(914400L, 914400L, 5486400L, 914400L));
        slide.AddTextBox("fixture-marker:library-generated", new PptxBounds(914400L, 2057400L, 5486400L, 685800L));
        ApplyProperties(presentation, LibraryGenerated, "Library Generated", "pptx", "library-generated fixture");
        return new PptxFixtureDeck(LibraryGenerated, "Library Generated", "pptx", "library-generated fixture", presentation.Package.SaveBytes());
    }

    public static PptxFixtureDeck CreateDeck(string family, string displayName, string application, string source, string prefix, string extensionXml)
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        PptxPresentation presentation = document.LoadPresentation();
        ApplyProperties(presentation, family, displayName, application, source);
        presentation.Package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(CreateSlideXml(family, displayName, prefix, extensionXml)));
        return new PptxFixtureDeck(family, displayName, application, source, presentation.Package.SaveBytes());
    }

    public static void ApplyProperties(PptxPresentation presentation, string family, string displayName, string application, string source)
    {
        PptxCoreProperties core = presentation.GetCoreProperties();
        core.Title = displayName + " compatibility fixture";
        core.Creator = application;
        core.LastModifiedBy = application;
        core.Category = "compatibility-fixture";
        core.Description = source;
        core.Save();

        PptxAppProperties app = presentation.GetAppProperties();
        app.Application = application;
        app.PresentationFormat = "On-screen Show (16:9)";
        app.Slides = 1;
        app.Notes = 0;
        app.HiddenSlides = 0;
        app.Save();

        PptxCustomProperties custom = presentation.GetCustomProperties();
        custom.SetString("FixtureFamily", family);
        custom.SetString("FixtureSource", source);
        custom.Save();
    }

    public static string CreateSlideXml(string family, string displayName, string prefix, string extensionXml)
    {
        return
            "<p:sld xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\" xmlns:mc=\"" + PptxNamespaces.MarkupCompatibility + "\" xmlns:" + prefix + "=\"http://example.com/pptx/fixtures/" + family + "\" mc:Ignorable=\"" + prefix + "\">" +
            "<p:cSld name=\"" + EscapeXml(displayName) + " fixture\">" +
            "<p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>" +
            CreateTitleShapeXml(displayName) +
            CreateBodyShapeXml(family, displayName) +
            CreateFamilyMarkerShapeXml(family) +
            "</p:spTree>" +
            extensionXml +
            "</p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sld>";
    }

    public static string CreateTitleShapeXml(string displayName)
    {
        return
            "<p:sp>" +
            "<p:nvSpPr><p:cNvPr id=\"2\" name=\"Title\"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>" +
            "<p:spPr><a:xfrm><a:off x=\"914400\" y=\"685800\"/><a:ext cx=\"7315200\" cy=\"685800\"/></a:xfrm><a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></p:spPr>" +
            "<p:txBody><a:bodyPr/><a:lstStyle/><a:p><a:r><a:t>" + EscapeXml(displayName) + " fixture deck</a:t></a:r></a:p></p:txBody>" +
            "</p:sp>";
    }

    public static string CreateBodyShapeXml(string family, string displayName)
    {
        return
            "<p:sp>" +
            "<p:nvSpPr><p:cNvPr id=\"3\" name=\"Compatibility Body\"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>" +
            "<p:spPr><a:xfrm><a:off x=\"914400\" y=\"1600200\"/><a:ext cx=\"7315200\" cy=\"1371600\"/></a:xfrm><a:prstGeom prst=\"roundRect\"><a:avLst/></a:prstGeom></p:spPr>" +
            "<p:txBody><a:bodyPr wrap=\"square\"/><a:lstStyle/><a:p><a:r><a:t>" + EscapeXml(displayName) + " exported text</a:t></a:r></a:p><a:p><a:r><a:t>family:" + EscapeXml(family) + "</a:t></a:r></a:p></p:txBody>" +
            "</p:sp>";
    }

    public static string CreateFamilyMarkerShapeXml(string family)
    {
        return
            "<p:sp>" +
            "<p:nvSpPr><p:cNvPr id=\"4\" name=\"Fixture Marker\" descr=\"" + EscapeXml(family) + "\"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>" +
            "<p:spPr><a:xfrm><a:off x=\"914400\" y=\"3429000\"/><a:ext cx=\"2743200\" cy=\"685800\"/></a:xfrm><a:prstGeom prst=\"ellipse\"><a:avLst/></a:prstGeom></p:spPr>" +
            "<p:txBody><a:bodyPr/><a:lstStyle/><a:p><a:r><a:t>fixture-marker:" + EscapeXml(family) + "</a:t></a:r></a:p></p:txBody>" +
            "</p:sp>";
    }

    public static string CreatePowerPointExtensionXml()
    {
        return "<p:extLst><p:ext uri=\"{BB962C8B-B14F-4D97-AF65-F5344CB8AC3E}\"><p14:compatExt xmlns:p14=\"http://example.com/pptx/fixtures/powerpoint\" val=\"powerpoint\"/></p:ext></p:extLst>";
    }

    public static string CreateLibreOfficeExtensionXml()
    {
        return "<p:extLst><p:ext uri=\"{A6DD0C12-4BF7-4B97-A7B5-9F401B28F72D}\"><loext:compatExt xmlns:loext=\"http://example.com/pptx/fixtures/libreoffice\" val=\"libreoffice\"/></p:ext></p:extLst>";
    }

    public static string CreateGoogleSlidesExtensionXml()
    {
        return "<p:extLst><p:ext uri=\"{41D87294-7D16-4B78-8ED2-B3A9B1A1338F}\"><gs:compatExt xmlns:gs=\"http://example.com/pptx/fixtures/google-slides\" val=\"google-slides\"/></p:ext></p:extLst>";
    }

    public static string CreateKeynoteExtensionXml()
    {
        return "<p:extLst><p:ext uri=\"{EB0FD41F-1817-49AB-99E4-DF7B7E5A0175}\"><key:compatExt xmlns:key=\"http://example.com/pptx/fixtures/keynote\" val=\"keynote\"/></p:ext></p:extLst>";
    }

    public static string EscapeXml(string value)
    {
        string clean = PptxDiagnostic.CleanText(value);
        StringBuilder builder = new StringBuilder();
        for (int index = 0; index < clean.Length; index++)
        {
            if (clean[index] == '&')
            {
                builder.Append("&amp;");
            }
            else if (clean[index] == '<')
            {
                builder.Append("&lt;");
            }
            else if (clean[index] == '>')
            {
                builder.Append("&gt;");
            }
            else if (clean[index] == '"')
            {
                builder.Append("&quot;");
            }
            else if (clean[index] == '\'')
            {
                builder.Append("&apos;");
            }
            else
            {
                builder.Append(clean[index]);
            }
        }

        return builder.ToString();
    }
}
