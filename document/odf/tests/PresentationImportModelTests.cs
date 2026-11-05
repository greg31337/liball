using System.IO.Compression;
using System.Text;

namespace odf.Tests;

public class PresentationImportModelTests
{
    [Test]
    public void ImportsSlideWithCollisionSafeTransitiveDefinitions()
    {
        OdfDocument source = ReadFlat(FlatPresentation(true));
        OdfDocument target = ReadFlat(FlatPresentation(false));

        OdfSlide imported = PresentationImportModel.ImportSlide(
            source,
            target,
            source.Slides[0],
            1);
        OdfDrawingObject shape = imported.DrawingObjects.Single(item =>
            item.Name == "Source Shape");
        OdfStyle shapeStyle = target.Styles.Single(item =>
            item.Name == shape.StyleName
            && item.Family == OdfStyleFamily.Graphic);
        OdfStyle notesStyle = target.Styles.Single(item =>
            item.Name == "NotesParagraph Copy 2"
            && item.Family == OdfStyleFamily.Paragraph);

        Assert.Multiple(() =>
        {
            Assert.That(target.Slides, Has.Length.EqualTo(2));
            Assert.That(imported.Name, Is.EqualTo("First Copy 2"));
            Assert.That(imported.MasterPageName,
                Is.EqualTo("Master Copy 2"));
            Assert.That(imported.MasterPage?.PageLayoutName,
                Is.EqualTo("PageLayout Copy 2"));
            Assert.That(imported.PresentationPageLayoutName,
                Is.EqualTo("Layout Copy 2"));
            Assert.That(imported.HeaderName,
                Is.EqualTo("Header Copy 2"));
            Assert.That(imported.StyleName,
                Is.EqualTo("PageStyle Copy 2"));
            Assert.That(shape.StyleName,
                Is.EqualTo("ShapeStyle Copy 2"));
            Assert.That(shapeStyle.ParentStyleName,
                Is.EqualTo("Parent Copy 2"));
            Assert.That(shapeStyle.ParentStyle?.Name,
                Is.EqualTo("Parent Copy 2"));
            Assert.That(notesStyle.MasterPageName,
                Is.EqualTo("Master Copy 2"));
            Assert.That(imported.Notes?.TextBlocks.Single().Text,
                Is.EqualTo("Imported notes"));
            Assert.That(imported.Header?.Text,
                Is.EqualTo("Source Header"));
        });

        OdfDocument reread = ReadFlatBytes(Write(target));
        OdfSlide roundTrip = reread.Slides[1];
        Assert.Multiple(() =>
        {
            Assert.That(roundTrip.MasterPage?.PageLayout?.Name,
                Is.EqualTo("PageLayout Copy 2"));
            Assert.That(roundTrip.PresentationPageLayout?.Name,
                Is.EqualTo("Layout Copy 2"));
            Assert.That(roundTrip.Header?.Text,
                Is.EqualTo("Source Header"));
            Assert.That(roundTrip.Notes?.TextBlocks.Single().Text,
                Is.EqualTo("Imported notes"));
        });
    }

    [Test]
    public void MergesPresentationWithSharedPackageAndHandoutClosure()
    {
        OdfDocument source = ReadPackage(CreatePresentationPackage(true));
        OdfDocument target = ReadPackage(CreatePresentationPackage(false));

        OdfSlide[] imported = PresentationImportModel.MergePresentation(
            source,
            target,
            1);
        OdfImage[] importedImages = target.Images.Where(item =>
            item.Drawing?.Page == imported[0].Drawing
            || item.Drawing?.Page == imported[1].Drawing).ToArray();
        OdfEmbeddedObject importedObject = target.EmbeddedObjects.Single(
            item => item.Drawing?.Page == imported[0].Drawing);

        Assert.Multiple(() =>
        {
            Assert.That(imported, Has.Length.EqualTo(2));
            Assert.That(target.Slides.Select(item => item.Name),
                Is.EqualTo(new[] { "Existing", "Source One", "Source Two" }));
            Assert.That(imported.Select(item => item.MasterPageName),
                Is.EqualTo(new[] { "Master Copy 2", "Master Copy 2" }));
            Assert.That(imported[0].MasterPage,
                Is.SameAs(imported[1].MasterPage));
            Assert.That(target.MasterPages.Count(item =>
                item.Name == "Master Copy 2"), Is.EqualTo(1));
            Assert.That(target.StyleResources.Count(item =>
                item.Kind == OdfStyleResourceKind.PresentationLayout
                && item.Name == "Layout Copy 2"), Is.EqualTo(1));
            Assert.That(target.PresentationHandout?.PresentationPageLayoutName,
                Is.EqualTo("Layout Copy 2"));
            Assert.That(target.PresentationHandout?.HeaderName,
                Is.EqualTo("Header Copy 2"));
            Assert.That(imported[0].Notes?.TextBlocks.Single().Text,
                Is.EqualTo("Source notes one"));
            Assert.That(imported[1].Notes?.TextBlocks.Single().Text,
                Is.EqualTo("Source notes two"));
            Assert.That(importedImages, Has.Length.EqualTo(2));
            Assert.That(importedImages.Select(item => item.PackagePath),
                Is.All.EqualTo(importedImages[0].PackagePath));
            Assert.That(importedImages[0].PackagePath,
                Is.Not.EqualTo("Pictures/shared.png"));
            Assert.That(importedImages[0].Data, Is.EqualTo(PngData(1)));
            Assert.That(importedObject.PackagePath,
                Is.Not.EqualTo("Object1"));
            Assert.That(importedObject.PackageEntryExists, Is.True);
            Assert.That(importedObject.Chart, Is.Not.Null);
            Assert.That(target.PackageEntries.Any(item =>
                item.Path == importedObject.PackagePath.TrimEnd('/')
                    + "/content.xml"),
                Is.True);
        });

        OdfDocument reread = ReadPackage(Write(target));
        OdfSlide rereadOne = reread.Slides[1];
        OdfSlide rereadTwo = reread.Slides[2];
        OdfImage[] rereadImages = reread.Images.Where(item =>
            item.Drawing?.Page == rereadOne.Drawing
            || item.Drawing?.Page == rereadTwo.Drawing).ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(reread.Slides.Select(item => item.Name),
                Is.EqualTo(new[] { "Existing", "Source One", "Source Two" }));
            Assert.That(rereadOne.MasterPage,
                Is.SameAs(rereadTwo.MasterPage));
            Assert.That(reread.PresentationHandout?.Header?.Text,
                Is.EqualTo("Source Header"));
            Assert.That(rereadImages, Has.Length.EqualTo(2));
            Assert.That(rereadImages[0].Data, Is.EqualTo(PngData(1)));
            Assert.That(reread.EmbeddedObjects.Any(item =>
                item.Drawing?.Page == rereadOne.Drawing
                && item.Chart != null), Is.True);
        });
    }

    static OdfDocument ReadFlat(string xml)
    {
        return ReadFlatBytes(Encoding.UTF8.GetBytes(xml));
    }

    static OdfDocument ReadFlatBytes(byte[] data)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            data,
            "presentation.fodp",
            new OdfLoadOptions());
    }

    static OdfDocument ReadPackage(byte[] data)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            data,
            "presentation.odp",
            new OdfLoadOptions());
    }

    static byte[] Write(OdfDocument document)
    {
        return new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
    }

    static string FlatPresentation(bool source)
    {
        string marker = source ? "source" : "target";
        string shapeName = source ? "Source Shape" : "Target Shape";
        string notes = source ? "Imported notes" : "Target notes";
        string header = source ? "Source Header" : "Target Header";
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document" + PresentationNamespaces()
            + " office:version='1.4' office:mimetype='"
            + OdfMediaTypes.Presentation + "'>"
            + "<office:styles>"
            + "<style:style style:name='Parent' style:family='graphic'>"
            + "<style:graphic-properties draw:fill='solid' "
            + "draw:fill-color='#" + (source ? "112233" : "abcdef")
            + "'/></style:style>"
            + "<style:style style:name='ShapeStyle' style:family='graphic' "
            + "style:parent-style-name='Parent'><style:graphic-properties "
            + "draw:opacity='" + (source ? "80%" : "50%")
            + "'/></style:style>"
            + "<style:style style:name='PageStyle' "
            + "style:family='drawing-page'><style:drawing-page-properties "
            + "presentation:background-visible='true'/></style:style>"
            + "<style:style style:name='NotesParagraph' "
            + "style:family='paragraph' style:master-page-name='Master'>"
            + "<style:text-properties "
            + "fo:font-weight='bold'/></style:style>"
            + "<style:presentation-page-layout style:name='Layout'>"
            + "<presentation:placeholder presentation:object='title'/>"
            + "</style:presentation-page-layout>"
            + "</office:styles>"
            + "<office:automatic-styles>"
            + "<style:page-layout style:name='PageLayout'>"
            + "<style:page-layout-properties fo:page-width='28cm'/>"
            + "</style:page-layout>"
            + "</office:automatic-styles>"
            + "<office:master-styles>"
            + "<style:master-page style:name='Master' "
            + "style:page-layout-name='PageLayout' "
            + "draw:style-name='PageStyle' "
            + "presentation:presentation-page-layout-name='Layout' "
            + "presentation:use-header-name='Header'>"
            + "<draw:rect draw:name='Master " + marker + "' "
            + "draw:style-name='ShapeStyle'/></style:master-page>"
            + "</office:master-styles>"
            + "<office:body><office:presentation>"
            + "<presentation:header-decl presentation:name='Header'>"
            + header + "</presentation:header-decl>"
            + "<draw:page draw:name='First' draw:id='page-" + marker + "' "
            + "draw:master-page-name='Master' draw:style-name='PageStyle' "
            + "presentation:presentation-page-layout-name='Layout' "
            + "presentation:use-header-name='Header'>"
            + "<draw:rect draw:name='" + shapeName + "' "
            + "draw:id='shape-" + marker + "' "
            + "draw:style-name='ShapeStyle'/>"
            + "<presentation:notes><draw:frame draw:name='Notes "
            + marker + "'><draw:text-box><text:p "
            + "text:style-name='NotesParagraph'>" + notes
            + "</text:p></draw:text-box></draw:frame>"
            + "</presentation:notes></draw:page>"
            + "</office:presentation></office:body></office:document>";
    }

    static string PresentationNamespaces()
    {
        return " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:draw='" + OdfNamespaces.Drawing + "'"
            + " xmlns:presentation='" + OdfNamespaces.Presentation + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:fo='" + OdfNamespaces.Fo + "'";
    }

    static byte[] CreatePresentationPackage(bool source)
    {
        using MemoryStream output = new MemoryStream();
        using (ZipArchive archive = new ZipArchive(
            output,
            ZipArchiveMode.Create,
            true))
        {
            WriteEntry(
                archive,
                OdfPartNames.Mimetype,
                Encoding.UTF8.GetBytes(OdfMediaTypes.Presentation),
                CompressionLevel.NoCompression);
            WriteEntry(
                archive,
                OdfPartNames.Content,
                Encoding.UTF8.GetBytes(PackageContent(source)),
                CompressionLevel.Optimal);
            WriteEntry(
                archive,
                OdfPartNames.Styles,
                Encoding.UTF8.GetBytes(PackageStyles(source)),
                CompressionLevel.Optimal);
            WriteEntry(
                archive,
                OdfPartNames.Manifest,
                Encoding.UTF8.GetBytes(PackageManifest()),
                CompressionLevel.Optimal);
            WriteEntry(
                archive,
                "Pictures/shared.png",
                PngData(source ? (byte)1 : (byte)9),
                CompressionLevel.Optimal);
            WriteEntry(
                archive,
                "Object1/",
                [],
                CompressionLevel.NoCompression);
            WriteEntry(
                archive,
                "Object1/content.xml",
                Encoding.UTF8.GetBytes(ChartContent(source)),
                CompressionLevel.Optimal);
            WriteEntry(
                archive,
                "Object1/styles.xml",
                Encoding.UTF8.GetBytes(ChartStyles(source)),
                CompressionLevel.Optimal);
        }
        return output.ToArray();
    }

    static void WriteEntry(
        ZipArchive archive,
        string path,
        byte[] data,
        CompressionLevel compression)
    {
        ZipArchiveEntry entry = archive.CreateEntry(path, compression);
        using Stream stream = entry.Open();
        stream.Write(data, 0, data.Length);
    }

    static string PackageContent(bool source)
    {
        string pages = source
            ? PackageSlide("Source One", "source-one", "Source notes one", true)
                + PackageSlide("Source Two", "source-two", "Source notes two", false)
            : PackageSlide("Existing", "existing", "Target notes", false);
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document-content" + PackageNamespaces()
            + " office:version='1.4'>"
            + "<office:body><office:presentation>"
            + "<presentation:header-decl presentation:name='Header'>"
            + (source ? "Source Header" : "Target Header")
            + "</presentation:header-decl>" + pages
            + "</office:presentation></office:body>"
            + "</office:document-content>";
    }

    static string PackageSlide(
        string name,
        string identifier,
        string notes,
        bool chart)
    {
        string chartObject = chart
            ? "<draw:frame draw:name='Chart Frame'><draw:object "
                + "draw:name='Chart Object' xlink:href='./Object1' "
                + "xlink:type='simple'/></draw:frame>"
            : "";
        return "<draw:page draw:name='" + name + "' draw:id='" + identifier
            + "' draw:master-page-name='Master' draw:style-name='PageStyle' "
            + "presentation:presentation-page-layout-name='Layout' "
            + "presentation:use-header-name='Header'>"
            + "<draw:frame draw:name='Image " + identifier + "' "
            + "draw:style-name='ShapeStyle'><draw:image "
            + "xlink:href='Pictures/shared.png' xlink:type='simple' "
            + "draw:mime-type='image/png'/></draw:frame>"
            + chartObject
            + "<presentation:notes><draw:frame draw:name='Notes "
            + identifier + "'><draw:text-box><text:p>" + notes
            + "</text:p></draw:text-box></draw:frame></presentation:notes>"
            + "</draw:page>";
    }

    static string PackageStyles(bool source)
    {
        string handout = source
            ? "<presentation:handout-master "
                + "presentation:presentation-page-layout-name='Layout' "
                + "presentation:use-header-name='Header'>"
                + "<draw:frame draw:name='Source Handout' "
                + "draw:style-name='ShapeStyle'/></presentation:handout-master>"
            : "";
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document-styles" + PackageNamespaces()
            + " office:version='1.4'>"
            + "<office:styles>"
            + "<style:style style:name='ShapeStyle' style:family='graphic'>"
            + "<style:graphic-properties draw:fill-color='#"
            + (source ? "123456" : "abcdef") + "'/></style:style>"
            + "<style:style style:name='PageStyle' "
            + "style:family='drawing-page'/>"
            + "<style:presentation-page-layout style:name='Layout'>"
            + "<presentation:placeholder presentation:object='title'/>"
            + "</style:presentation-page-layout>"
            + "</office:styles>"
            + "<office:automatic-styles>"
            + "<style:page-layout style:name='PageLayout'>"
            + "<style:page-layout-properties fo:page-width='28cm'/>"
            + "</style:page-layout></office:automatic-styles>"
            + "<office:master-styles>"
            + "<style:master-page style:name='Master' "
            + "style:page-layout-name='PageLayout' "
            + "presentation:presentation-page-layout-name='Layout' "
            + "presentation:use-header-name='Header'>"
            + "<draw:rect draw:name='Master Shape' "
            + "draw:style-name='ShapeStyle'/></style:master-page>"
            + handout + "</office:master-styles>"
            + "</office:document-styles>";
    }

    static string PackageManifest()
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<manifest:manifest xmlns:manifest='"
            + OdfNamespaces.Manifest + "' manifest:version='1.4'>"
            + ManifestEntry("/", OdfMediaTypes.Presentation)
            + ManifestEntry(OdfPartNames.Content, OdfMediaTypes.Xml)
            + ManifestEntry(OdfPartNames.Styles, OdfMediaTypes.Xml)
            + ManifestEntry("Pictures/shared.png", OdfMediaTypes.Png)
            + ManifestEntry("Object1/", OdfMediaTypes.Chart)
            + ManifestEntry("Object1/content.xml", OdfMediaTypes.Xml)
            + ManifestEntry("Object1/styles.xml", OdfMediaTypes.Xml)
            + "</manifest:manifest>";
    }

    static string ManifestEntry(string path, string mediaType)
    {
        return "<manifest:file-entry manifest:full-path='" + path
            + "' manifest:media-type='" + mediaType + "'/>";
    }

    static string ChartContent(bool source)
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document-content xmlns:office='"
            + OdfNamespaces.Office + "' xmlns:chart='"
            + OdfNamespaces.Chart + "' office:version='1.4'>"
            + "<office:body><office:chart><chart:chart chart:class='chart:bar' "
            + "chart:style-name='" + (source ? "SourceChart" : "TargetChart")
            + "'/></office:chart></office:body></office:document-content>";
    }

    static string ChartStyles(bool source)
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document-styles xmlns:office='"
            + OdfNamespaces.Office + "' xmlns:style='"
            + OdfNamespaces.Style + "' office:version='1.4'>"
            + "<office:styles><style:style style:name='"
            + (source ? "SourceChart" : "TargetChart")
            + "' style:family='chart'/></office:styles>"
            + "</office:document-styles>";
    }

    static string PackageNamespaces()
    {
        return " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:draw='" + OdfNamespaces.Drawing + "'"
            + " xmlns:presentation='" + OdfNamespaces.Presentation + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:xlink='" + OdfNamespaces.XLink + "'"
            + " xmlns:fo='" + OdfNamespaces.Fo + "'";
    }

    static byte[] PngData(byte marker)
    {
        return [
            137, 80, 78, 71, 13, 10, 26, 10,
            marker, 0, 0, 0
        ];
    }
}
