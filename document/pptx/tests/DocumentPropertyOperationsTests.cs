using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class DocumentPropertyOperationsTests
{
    [Test]
    public void CoreAndApplicationProperties_AllMetadataFieldsRoundTripAndPreserveUnknownNodes()
    {
        PptxDocument document = PptxDocument.CreatePresentation();
        PptxPackage package = document.GetPackage();
        package.ReplacePart(PptxPartNames.CoreProperties, PptxContentTypes.CoreProperties, Encoding.UTF8.GetBytes(CreateCorePropertiesXml()));
        package.ReplacePart(PptxPartNames.ExtendedProperties, PptxContentTypes.ExtendedProperties, Encoding.UTF8.GetBytes(CreateAppPropertiesXml()));
        PptxPresentation presentation = document.LoadPresentation();
        PptxPresentationOperations presentationOperations = new PptxPresentationOperations();
        PptxCoreProperties core = presentationOperations.GetCoreProperties(presentation);
        PptxAppProperties app = presentationOperations.GetAppProperties(presentation);

        core.Title = "Complete metadata";
        core.Subject = "Subject";
        core.Creator = "Author";
        core.Description = "Description";
        core.Keywords = "one;two";
        core.LastModifiedBy = "Editor";
        core.Revision = "7";
        core.Category = "Category";
        core.ContentStatus = "Final";
        core.Created = "2026-08-26T10:00:00Z";
        core.Modified = "2026-08-26T11:00:00Z";
        core.LastPrinted = "2026-08-26T12:00:00Z";
        core.Identifier = "deck-42";
        core.Language = "en-US";
        core.Version = "2.1";
        new PptxCorePropertiesOperations().Save(core);

        app.Application = "pptx tests";
        app.PresentationFormat = "Custom";
        app.Slides = 5;
        app.Notes = 2;
        app.HiddenSlides = 1;
        app.Template = "Template.potx";
        app.Manager = "Manager";
        app.Company = "Example Corp";
        app.AppVersion = "10.0";
        app.TotalTime = 90;
        app.Pages = 5;
        app.Words = 123;
        app.Characters = 456;
        app.Lines = 12;
        app.Paragraphs = 8;
        app.MultimediaClips = 3;
        app.CharactersWithSpaces = 500;
        app.DocumentSecurity = 4;
        app.ScaleCrop = true;
        app.LinksUpToDate = true;
        app.SharedDocument = true;
        app.HyperlinkBase = "https://example.test/base/";
        app.HyperlinksChanged = true;
        new PptxAppPropertiesOperations().Save(app);

        PptxDocument reopenedDocument = PptxDocument.OpenBytes(document.SaveBytes());
        PptxPresentation reopened = reopenedDocument.LoadPresentation();
        PptxCoreProperties actualCore = presentationOperations.GetCoreProperties(reopened);
        PptxAppProperties actualApp = presentationOperations.GetAppProperties(reopened);

        Assert.That(actualCore.Title, Is.EqualTo("Complete metadata"));
        Assert.That(actualCore.Creator, Is.EqualTo("Author"));
        Assert.That(actualCore.Revision, Is.EqualTo("7"));
        Assert.That(actualCore.Category, Is.EqualTo("Category"));
        Assert.That(actualCore.Keywords, Is.EqualTo("one;two"));
        Assert.That(actualCore.Created, Is.EqualTo("2026-08-26T10:00:00Z"));
        Assert.That(actualCore.Modified, Is.EqualTo("2026-08-26T11:00:00Z"));
        Assert.That(actualCore.LastPrinted, Is.EqualTo("2026-08-26T12:00:00Z"));
        Assert.That(actualCore.Identifier, Is.EqualTo("deck-42"));
        Assert.That(actualCore.Language, Is.EqualTo("en-US"));
        Assert.That(actualCore.Version, Is.EqualTo("2.1"));
        Assert.That(actualApp.Template, Is.EqualTo("Template.potx"));
        Assert.That(actualApp.Manager, Is.EqualTo("Manager"));
        Assert.That(actualApp.Company, Is.EqualTo("Example Corp"));
        Assert.That(actualApp.AppVersion, Is.EqualTo("10.0"));
        Assert.That(actualApp.TotalTime, Is.EqualTo(90));
        Assert.That(actualApp.Pages, Is.EqualTo(5));
        Assert.That(actualApp.Words, Is.EqualTo(123));
        Assert.That(actualApp.Characters, Is.EqualTo(456));
        Assert.That(actualApp.Lines, Is.EqualTo(12));
        Assert.That(actualApp.Paragraphs, Is.EqualTo(8));
        Assert.That(actualApp.MultimediaClips, Is.EqualTo(3));
        Assert.That(actualApp.CharactersWithSpaces, Is.EqualTo(500));
        Assert.That(actualApp.DocumentSecurity, Is.EqualTo(4));
        Assert.That(actualApp.ScaleCrop, Is.True);
        Assert.That(actualApp.LinksUpToDate, Is.True);
        Assert.That(actualApp.SharedDocument, Is.True);
        Assert.That(actualApp.HyperlinkBase, Is.EqualTo("https://example.test/base/"));
        Assert.That(actualApp.HyperlinksChanged, Is.True);
        Assert.That(Encoding.UTF8.GetString(reopenedDocument.GetPackage().GetPart(PptxPartNames.CoreProperties).Bytes), Does.Contain("preserve-core"));
        Assert.That(Encoding.UTF8.GetString(reopenedDocument.GetPackage().GetPart(PptxPartNames.ExtendedProperties).Bytes), Does.Contain("preserve-app"));
    }

    [Test]
    public void CustomProperties_TypedLifecycleRoundTripsAndPreservesUnknownNodes()
    {
        PptxDocument document = PptxDocument.CreatePresentation();
        PptxPackage package = document.GetPackage();
        package.AddPart(PptxPartNames.CustomProperties, PptxContentTypes.CustomProperties, Encoding.UTF8.GetBytes(CreateCustomPropertiesXml()));
        package.AddRelationship("", PptxRelationshipTypes.CustomProperties, "docProps/custom.xml", PptxRelationshipTargetMode.Internal);
        PptxCustomProperties custom = document.LoadPresentation().GetCustomProperties();
        PptxCustomPropertiesOperations operations = new PptxCustomPropertiesOperations();

        Assert.That(operations.Rename(custom, "Legacy", "Renamed"), Is.True);
        operations.SetLong(custom, "Large", 5000000000L);
        operations.SetDouble(custom, "Ratio", 1.25);
        operations.SetDateTime(custom, "Reviewed", "2026-08-26T12:30:00Z");
        operations.Save(custom);

        PptxDocument reopenedDocument = PptxDocument.OpenBytes(document.SaveBytes());
        PptxCustomProperties actual = reopenedDocument.LoadPresentation().GetCustomProperties();

        Assert.That(actual.GetValue("Renamed"), Is.EqualTo("old"));
        Assert.That(actual.Find("Large")!.ValueType, Is.EqualTo("i8"));
        Assert.That(actual.GetValue("Large"), Is.EqualTo("5000000000"));
        Assert.That(actual.Find("Ratio")!.ValueType, Is.EqualTo("r8"));
        Assert.That(actual.GetValue("Ratio"), Is.EqualTo("1.25"));
        Assert.That(actual.Find("Reviewed")!.ValueType, Is.EqualTo("filetime"));
        Assert.That(Encoding.UTF8.GetString(reopenedDocument.GetPackage().GetPart(PptxPartNames.CustomProperties).Bytes), Does.Contain("preserve-custom"));

        int removed = operations.Clear(actual);
        operations.Save(actual);
        PptxDocument clearedDocument = PptxDocument.OpenBytes(reopenedDocument.SaveBytes());
        Assert.That(removed, Is.EqualTo(4));
        Assert.That(clearedDocument.LoadPresentation().GetCustomProperties().Count(), Is.EqualTo(0));
        Assert.That(Encoding.UTF8.GetString(clearedDocument.GetPackage().GetPart(PptxPartNames.CustomProperties).Bytes), Does.Contain("preserve-custom"));
    }

    [Test]
    public void PresentationProperties_ShowModeAndScopeRoundTripAndPreserveUnknownNodes()
    {
        PptxDocument document = PptxDocument.CreatePresentation();
        document.GetPackage().ReplacePart(PptxPartNames.PresentationProperties, PptxContentTypes.PresentationProperties, Encoding.UTF8.GetBytes(CreatePresentationPropertiesXml()));
        PptxPresentation presentation = document.LoadPresentation();
        PptxPresentationProperties properties = new PptxPresentationOperations().GetPresentationProperties(presentation);

        Assert.That(properties.ShowMode, Is.EqualTo("browse"));
        Assert.That(properties.ShowAllSlides, Is.True);
        properties.Loop = true;
        properties.ShowAnimation = false;
        properties.ShowNarration = false;
        properties.UseTimings = false;
        properties.ShowMode = "kiosk";
        properties.ShowAllSlides = false;
        properties.SlideRangeStart = 2;
        properties.SlideRangeEnd = 4;
        new PptxPresentationPropertiesOperations().Save(properties);

        PptxDocument reopenedDocument = PptxDocument.OpenBytes(document.SaveBytes());
        PptxPresentationProperties actual = new PptxPresentationOperations().GetPresentationProperties(reopenedDocument.LoadPresentation());
        string xml = Encoding.UTF8.GetString(reopenedDocument.GetPackage().GetPart(PptxPartNames.PresentationProperties).Bytes);

        Assert.That(actual.Loop, Is.True);
        Assert.That(actual.ShowAnimation, Is.False);
        Assert.That(actual.ShowNarration, Is.False);
        Assert.That(actual.UseTimings, Is.False);
        Assert.That(actual.ShowMode, Is.EqualTo("kiosk"));
        Assert.That(actual.ShowAllSlides, Is.False);
        Assert.That(actual.SlideRangeStart, Is.EqualTo(2));
        Assert.That(actual.SlideRangeEnd, Is.EqualTo(4));
        Assert.That(xml, Does.Contain("preserve-show-properties"));
        Assert.That(xml, Does.Contain("p:penClr"));
    }

    [Test]
    public void Thumbnail_AddReadReplaceAndRemoveRoundTrip()
    {
        PptxPresentationOperations operations = new PptxPresentationOperations();
        PptxDocument document = PptxDocument.CreatePresentation();
        PptxPresentation presentation = document.LoadPresentation();
        byte[] png = CreatePngBytes(320, 180);

        Assert.That(operations.GetThumbnail(presentation), Is.Null);
        PptxThumbnail added = operations.AddThumbnail(presentation, png);
        Assert.That(added.PartName, Is.EqualTo("/docProps/thumbnail.png"));
        Assert.That(added.ContentType, Is.EqualTo(PptxContentTypes.Png));
        Assert.Throws<System.ArgumentException>(() => operations.AddThumbnail(presentation, png));

        PptxDocument withThumbnailDocument = PptxDocument.OpenBytes(document.SaveBytes());
        PptxPresentation withThumbnail = withThumbnailDocument.LoadPresentation();
        PptxThumbnail? read = operations.GetThumbnail(withThumbnail);
        Assert.That(read, Is.Not.Null);
        Assert.That(read!.Bytes, Is.EqualTo(png));
        Assert.That(read.Info.WidthPixels, Is.EqualTo(320));
        Assert.That(read.Info.HeightPixels, Is.EqualTo(180));

        byte[] gif = CreateGifBytes(40, 30);
        PptxThumbnail replaced = operations.ReplaceThumbnail(withThumbnail, gif);
        Assert.That(replaced.ContentType, Is.EqualTo(PptxContentTypes.Gif));
        Assert.That(replaced.PartName, Is.EqualTo("/docProps/thumbnail.gif"));
        Assert.That(withThumbnailDocument.GetPackage().HasPart("/docProps/thumbnail.png"), Is.False);

        PptxDocument replacedDocument = PptxDocument.OpenBytes(withThumbnailDocument.SaveBytes());
        PptxPresentation replacedPresentation = replacedDocument.LoadPresentation();
        Assert.That(operations.GetThumbnail(replacedPresentation)!.Bytes, Is.EqualTo(gif));
        Assert.That(operations.RemoveThumbnail(replacedPresentation), Is.True);
        Assert.That(operations.RemoveThumbnail(replacedPresentation), Is.False);

        PptxDocument removedDocument = PptxDocument.OpenBytes(replacedDocument.SaveBytes());
        Assert.That(operations.GetThumbnail(removedDocument.LoadPresentation()), Is.Null);
        Assert.That(removedDocument.GetPackage().RootRelationships.Relationships.Exists(item => item.Type == PptxRelationshipTypes.Thumbnail), Is.False);
        Assert.That(removedDocument.Validate().Succeeded(), Is.True, removedDocument.Validate().FormatDiagnostics());
    }

    static string CreateCorePropertiesXml()
    {
        return
            "<cp:coreProperties xmlns:cp=\"" + PptxNamespaces.CoreProperties + "\" xmlns:dc=\"" + PptxNamespaces.DublinCore + "\" xmlns:x=\"urn:test\">" +
            "<dc:title>Old</dc:title><x:extra>preserve-core</x:extra>" +
            "</cp:coreProperties>";
    }

    static string CreateAppPropertiesXml()
    {
        return
            "<Properties xmlns=\"" + PptxNamespaces.ExtendedProperties + "\" xmlns:x=\"urn:test\">" +
            "<Application>Old</Application><x:extra>preserve-app</x:extra>" +
            "</Properties>";
    }

    static string CreateCustomPropertiesXml()
    {
        return
            "<Properties xmlns=\"" + PptxNamespaces.CustomProperties + "\" xmlns:vt=\"" + PptxNamespaces.VariantTypes + "\" xmlns:x=\"urn:test\">" +
            "<x:extra>preserve-custom</x:extra>" +
            "<property fmtid=\"{D5CDD505-2E9C-101B-9397-08002B2CF9AE}\" pid=\"2\" name=\"Legacy\"><vt:lpwstr>old</vt:lpwstr></property>" +
            "</Properties>";
    }

    static string CreatePresentationPropertiesXml()
    {
        return
            "<p:presentationPr xmlns:p=\"" + PptxNamespaces.Presentation + "\" xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:x=\"urn:test\">" +
            "<p:showPr loop=\"0\" showAnimation=\"1\" showNarration=\"1\" useTimings=\"1\">" +
            "<p:browse/><p:sldAll/><p:penClr><a:srgbClr val=\"FF0000\"/></p:penClr><p:extLst><p:ext uri=\"preserve-show-properties\"><x:data/></p:ext></p:extLst>" +
            "</p:showPr></p:presentationPr>";
    }

    static byte[] CreatePngBytes(int width, int height)
    {
        byte[] bytes = new byte[24];
        bytes[0] = 137;
        bytes[1] = 80;
        bytes[2] = 78;
        bytes[3] = 71;
        bytes[4] = 13;
        bytes[5] = 10;
        bytes[6] = 26;
        bytes[7] = 10;
        bytes[12] = 73;
        bytes[13] = 72;
        bytes[14] = 68;
        bytes[15] = 82;
        WriteInt32Big(bytes, 16, width);
        WriteInt32Big(bytes, 20, height);
        return bytes;
    }

    static byte[] CreateGifBytes(int width, int height)
    {
        byte[] bytes = Encoding.ASCII.GetBytes("GIF89a0000");
        bytes[6] = (byte)(width & 255);
        bytes[7] = (byte)((width >> 8) & 255);
        bytes[8] = (byte)(height & 255);
        bytes[9] = (byte)((height >> 8) & 255);
        return bytes;
    }

    static void WriteInt32Big(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)((value >> 24) & 255);
        bytes[offset + 1] = (byte)((value >> 16) & 255);
        bytes[offset + 2] = (byte)((value >> 8) & 255);
        bytes[offset + 3] = (byte)(value & 255);
    }
}
