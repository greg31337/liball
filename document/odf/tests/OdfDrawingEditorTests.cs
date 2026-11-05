using System.IO.Compression;
using System.Text;

namespace odf.Tests;

public class OdfDrawingEditorTests
{
    [Test]
    public void CreatesEditsCopiesRenamesAndRemovesDrawingGeometry()
    {
        OdfDocument document = ReadFlat(
            "<draw:page draw:name='Page'>"
            + "<draw:rect draw:name='Rectangle' draw:id='shape1' "
            + "svg:x='1cm' svg:y='2cm' svg:width='3cm' svg:height='4cm'/>"
            + "<draw:connector draw:name='Connector' "
            + "draw:start-shape='shape1' svg:x1='0cm' svg:y1='0cm' "
            + "svg:x2='5cm' svg:y2='5cm'/>"
            + "</draw:page>");
        IOdfDrawingEditor editor = new OdfDrawingEditor();
        OdfDrawingObject rectangle = editor.FindDrawingObject(
            document,
            "Rectangle")!;

        rectangle.Width = "8cm";
        rectangle.Geometry.CornerRadius = "0.5cm";
        editor.ApplyDrawingChanges(document);
        rectangle = editor.RenameDrawingIdentifier(
            document,
            rectangle,
            "renamed-shape");
        OdfDrawingObject page = editor.FindDrawingObject(document, "Page")!;
        OdfDrawingObject ellipse = editor.CreateDrawingObject(
            document,
            page,
            OdfDrawingKind.Ellipse,
            "Temporary");
        ellipse.Geometry.CenterX = "2cm";
        ellipse.Geometry.CenterY = "3cm";
        ellipse.Geometry.Radius = "1cm";
        editor.ApplyDrawingChanges(document);
        editor.RemoveDrawingObject(document, ellipse);
        page = editor.FindDrawingObject(document, "Page")!;
        rectangle = editor.FindDrawingObject(document, "Rectangle")!;
        OdfDrawingObject copy = editor.CopyDrawingObject(
            document,
            rectangle,
            page);

        OdfDocument reread = ReadFlatBytes(Write(document));
        OdfDrawingObject changed = new OdfDrawingEditor()
            .FindDrawingObject(reread, "Rectangle")!;
        OdfDrawingObject connector = new OdfDrawingEditor()
            .FindDrawingObject(reread, "Connector")!;

        Assert.Multiple(() =>
        {
            Assert.That(changed.Id, Is.EqualTo("renamed-shape"));
            Assert.That(changed.Width, Is.EqualTo("8cm"));
            Assert.That(changed.Geometry.CornerRadius, Is.EqualTo("0.5cm"));
            Assert.That(connector.Geometry.Routing.StartShapeId,
                Is.EqualTo("renamed-shape"));
            Assert.That(connector.Geometry.Routing.StartShape,
                Is.SameAs(changed));
            Assert.That(copy.Id, Is.EqualTo("renamed-shape-copy-2"));
            Assert.That(copy.Name, Is.EqualTo("Rectangle Copy 2"));
            Assert.That(editor.FindDrawingObject(document, "Temporary"),
                Is.Null);
            Assert.That(editor.GetDrawingObjects(document),
                Is.Not.SameAs(document.DrawingObjects));
        });
    }

    [Test]
    public void EditsAndExtractsImagesMediaEmbeddedObjectsAndThumbnails()
    {
        OdfDocument document = ReadPackage(CreateResourcePackage(1, true));
        IOdfDrawingEditor editor = new OdfDrawingEditor();
        OdfImage image = editor.GetImages(document).Single(item =>
            item.PackagePath == "Pictures/shared.png");

        OdfEditResult moved = editor.MovePackageResource(
            document,
            "Pictures/shared.png",
            "Pictures/moved.png");
        image = editor.GetImages(document).Single(item =>
            item.PackagePath == "Pictures/moved.png");
        OdfEditResult replaced = editor.ReplaceImageData(
            document,
            image,
            PngData(4),
            OdfMediaTypes.Png);
        image = editor.GetImages(document).Single(item =>
            item.PackagePath == "Pictures/moved.png");
        byte[] extractedImage = editor.ExtractImageData(document, image);
        OdfDrawingObject page = editor.FindDrawingObject(document, "Page")!;
        OdfImage added = editor.AddPackageImage(
            document,
            page,
            "Pictures/added.png",
            PngData(5),
            OdfMediaTypes.Png,
            "Added Image",
            false);
        OdfImage external = editor.SetExternalImage(
            document,
            added,
            "https://example.com/image.gif",
            OdfMediaTypes.Gif);
        OdfImageSourceKind externalKind = external.SourceKind;
        string externalHref = external.Href;
        added = editor.SetInlineImage(
            document,
            external,
            GifData(),
            OdfMediaTypes.Gif);
        OdfMediaResource media = editor.GetMediaResources(document).Single();
        media = editor.SetMediaReference(
            document,
            media,
            "https://example.com/video.mp4",
            "video/mp4");
        OdfEmbeddedObject embedded = editor.GetEmbeddedObjects(document)
            .Single(item =>
                item.SourceKind == OdfResourceSourceKind.Inline);
        embedded = editor.ReplaceEmbeddedObjectData(
            document,
            embedded,
            new byte[] { 8, 7, 6 });
        byte[] embeddedData = editor.ExtractEmbeddedObjectData(
            document,
            embedded);
        OdfThumbnail thumbnail = editor.GetThumbnails(document).Single();
        byte[] thumbnailData = editor.ExtractThumbnailData(thumbnail);
        string[] orphans = editor.GetOrphanedResourcePaths(document);
        OdfEditResult collected = editor.CollectDrawingGarbage(document);
        OdfDocument reread = ReadPackage(Write(document));

        Assert.Multiple(() =>
        {
            Assert.That(moved.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(replaced.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(extractedImage, Is.EqualTo(PngData(4)));
            Assert.That(externalKind,
                Is.EqualTo(OdfImageSourceKind.External));
            Assert.That(externalHref,
                Is.EqualTo("https://example.com/image.gif"));
            Assert.That(added.SourceKind,
                Is.EqualTo(OdfImageSourceKind.Base64));
            Assert.That(editor.ExtractImageData(document, added),
                Is.EqualTo(GifData()));
            Assert.That(media.SourceKind,
                Is.EqualTo(OdfResourceSourceKind.External));
            Assert.That(media.IsActiveContent, Is.False);
            Assert.That(embeddedData, Is.EqualTo(new byte[] { 8, 7, 6 }));
            Assert.That(thumbnailData, Is.EqualTo(PngData(7)));
            Assert.That(orphans, Does.Contain("Pictures/orphan.png"));
            Assert.That(orphans, Does.Contain("Pictures/added.png"));
            Assert.That(collected.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(reread.PackageEntries.Any(item =>
                item.Path == "Pictures/orphan.png"), Is.False);
            Assert.That(reread.PackageEntries.Any(item =>
                item.Path == "Pictures/added.png"), Is.False);
            Assert.That(reread.Images.Single(item =>
                item.PackagePath == "Pictures/moved.png").Data,
                Is.EqualTo(PngData(4)));
            Assert.That(reread.EmbeddedObjects.Single(item =>
                item.SourceKind == OdfResourceSourceKind.Inline).Data,
                Is.EqualTo(new byte[] { 8, 7, 6 }));
        });
    }

    [Test]
    public void ReadsEveryDrawingKindAndTypedDescriptionThroughContract()
    {
        OdfDocument document = ReadFlatBytes(
            Encoding.UTF8.GetBytes(ComprehensiveDrawingXml()));
        IOdfDrawingEditor editor = new OdfDrawingEditor();

        OdfDrawingObject[] drawings = editor.GetDrawingObjects(document);
        OdfDrawingObject rectangle = editor.FindDrawingObject(
            document,
            "Rectangle")!;
        OdfDrawingObject path = editor.FindDrawingObject(
            document,
            "Path")!;
        OdfDrawingObject frame = editor.FindDrawingObject(
            document,
            "Anchored Frame")!;
        OdfDrawingObject custom = editor.FindDrawingObject(
            document,
            "Custom")!;
        OdfDrawingObject scene = editor.FindDrawingObject(
            document,
            "Scene")!;
        OdfDrawingObject cube = editor.FindDrawingObject(
            document,
            "Cube")!;
        OdfDrawingObject image = drawings.Single(item =>
            item.Kind == OdfDrawingKind.Image);
        OdfDrawingObject media = drawings.Single(item =>
            item.Kind == OdfDrawingKind.Media);
        OdfDrawingObject embedded = drawings.First(item =>
            item.Kind == OdfDrawingKind.EmbeddedObject);
        OdfDrawingKind[] expectedKinds =
        [
            OdfDrawingKind.Page,
            OdfDrawingKind.Layer,
            OdfDrawingKind.Frame,
            OdfDrawingKind.Group,
            OdfDrawingKind.Rectangle,
            OdfDrawingKind.Ellipse,
            OdfDrawingKind.Line,
            OdfDrawingKind.Polyline,
            OdfDrawingKind.Polygon,
            OdfDrawingKind.RegularPolygon,
            OdfDrawingKind.Path,
            OdfDrawingKind.Connector,
            OdfDrawingKind.Measure,
            OdfDrawingKind.Caption,
            OdfDrawingKind.Control,
            OdfDrawingKind.TextBox,
            OdfDrawingKind.CustomShape,
            OdfDrawingKind.Scene3D,
            OdfDrawingKind.Cube,
            OdfDrawingKind.Sphere,
            OdfDrawingKind.Rotate,
            OdfDrawingKind.Extrude,
            OdfDrawingKind.Image,
            OdfDrawingKind.Media,
            OdfDrawingKind.EmbeddedObject
        ];

        Assert.Multiple(() =>
        {
            Assert.That(
                drawings.Select(item => item.Kind).Distinct(),
                Is.SupersetOf(expectedKinds));
            Assert.That(rectangle.Geometry.CornerRadius,
                Is.EqualTo("0.2cm"));
            Assert.That(rectangle.GraphicProperties.Fill.Mode,
                Is.EqualTo("solid"));
            Assert.That(rectangle.GraphicProperties.Fill.Color,
                Is.EqualTo("#112233"));
            Assert.That(path.Geometry.Path.IsValid, Is.True);
            Assert.That(path.Geometry.ViewBoxValue.Width, Is.EqualTo(10));
            Assert.That(path.Parent?.Transform.Expression,
                Is.EqualTo("translate(1cm 2cm) rotate(0.5)"));
            Assert.That(frame.Layer?.Name, Is.EqualTo("layout"));
            Assert.That(frame.Anchor.Kind, Is.EqualTo(OdfTextAnchorKind.Page));
            Assert.That(frame.Anchor.PageNumber, Is.EqualTo(2));
            Assert.That(frame.TextBox?.MinimumWidth, Is.EqualTo("2cm"));
            Assert.That(custom.Geometry.EnhancedGeometry?.EnhancedPath,
                Is.EqualTo("M 0 0 L 10 10 N"));
            Assert.That(scene.ThreeDimensional.Camera.Projection,
                Is.EqualTo(OdfThreeDimensionalProjection.Perspective));
            Assert.That(scene.ThreeDimensional.Lights.Single().IsEnabled,
                Is.True);
            Assert.That(cube.ThreeDimensional.Material.DiffuseColor,
                Is.EqualTo("#445566"));
            Assert.That(image.Image?.SourceKind,
                Is.EqualTo(OdfImageSourceKind.Base64));
            Assert.That(media.MediaResource?.SourceKind,
                Is.EqualTo(OdfResourceSourceKind.External));
            Assert.That(embedded.EmbeddedObject, Is.Not.Null);
            Assert.That(editor.GetImages(document), Is.Not.SameAs(
                document.Images));
            Assert.That(editor.GetMediaResources(document), Is.Not.SameAs(
                document.MediaResources));
            Assert.That(editor.GetEmbeddedObjects(document), Is.Not.SameAs(
                document.EmbeddedObjects));
            Assert.That(editor.GetCharts(document).Single().Kind,
                Is.EqualTo(OdfChartKind.Bar));
        });
    }

    [Test]
    public void ImportsDrawingWithCollisionSafeStyleAndResourceDependencies()
    {
        OdfDocument source = ReadPackage(CreateResourcePackage(1, false));
        OdfDocument target = ReadPackage(CreateResourcePackage(9, false));
        IOdfDrawingEditor editor = new OdfDrawingEditor();
        OdfDrawingObject sourceFrame = editor.FindDrawingObject(
            source,
            "Image Frame")!;
        OdfDrawingObject targetPage = editor.FindDrawingObject(
            target,
            "Page")!;

        OdfDrawingObject imported = editor.ImportDrawingObject(
            source,
            target,
            sourceFrame,
            targetPage);
        OdfImage importedImage = editor.GetImages(target).Single(item =>
            item.Drawing?.Parent == imported);
        OdfDocument reread = ReadPackage(Write(target));

        Assert.Multiple(() =>
        {
            Assert.That(imported.Name, Is.EqualTo("Image Frame Copy 2"));
            Assert.That(imported.StyleName, Is.Not.EqualTo("GraphicStyle"));
            Assert.That(importedImage.PackagePath,
                Is.EqualTo("Pictures/shared-copy-2.png"));
            Assert.That(importedImage.Data, Is.EqualTo(PngData(1)));
            Assert.That(target.Styles.Any(item =>
                item.Name == imported.StyleName
                && item.Family == OdfStyleFamily.Graphic), Is.True);
            Assert.That(reread.Images.Any(item =>
                item.PackagePath == "Pictures/shared-copy-2.png"), Is.True);
        });
    }

    static OdfDocument ReadFlat(string content)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:draw='" + OdfNamespaces.Drawing + "'"
            + " xmlns:svg='" + OdfNamespaces.Svg + "'"
            + " xmlns:xlink='" + OdfNamespaces.XLink + "'"
            + " office:version='1.4' office:mimetype='"
            + OdfMediaTypes.Graphics + "'>"
            + "<office:body><office:drawing>" + content
            + "</office:drawing></office:body></office:document>";
        return ReadFlatBytes(Encoding.UTF8.GetBytes(xml));
    }

    static OdfDocument ReadFlatBytes(byte[] data)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            data,
            "drawing.fodg",
            new OdfLoadOptions());
    }

    static OdfDocument ReadPackage(byte[] data)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            data,
            "drawing.odg",
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

    static byte[] CreateResourcePackage(byte marker, bool includeExtras)
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
                Encoding.UTF8.GetBytes(OdfMediaTypes.Graphics),
                CompressionLevel.NoCompression);
            WriteEntry(
                archive,
                OdfPartNames.Content,
                Encoding.UTF8.GetBytes(ResourceContentXml(includeExtras)),
                CompressionLevel.Optimal);
            WriteEntry(
                archive,
                OdfPartNames.Manifest,
                Encoding.UTF8.GetBytes(ResourceManifestXml(includeExtras)),
                CompressionLevel.Optimal);
            WriteEntry(
                archive,
                "Pictures/shared.png",
                PngData(marker),
                CompressionLevel.Optimal);
            if (includeExtras)
            {
                WriteEntry(
                    archive,
                    "Pictures/orphan.png",
                    PngData(2),
                    CompressionLevel.Optimal);
                WriteEntry(
                    archive,
                    OdfPartNames.Thumbnail,
                    PngData(7),
                    CompressionLevel.Optimal);
            }
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

    static string ResourceContentXml(bool includeExtras)
    {
        string extras = includeExtras
            ? "<draw:frame draw:name='Media Frame'>"
                + "<draw:plugin xlink:href='Media/video.mp4' "
                + "xlink:type='simple' draw:mime-type='video/mp4'/>"
                + "</draw:frame>"
                + "<draw:frame draw:name='OLE Frame'>"
                + "<draw:object-ole draw:class-id='ole'>"
                + "<office:binary-data>AQID</office:binary-data>"
                + "</draw:object-ole></draw:frame>"
            : "";
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document-content"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:draw='" + OdfNamespaces.Drawing + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:xlink='" + OdfNamespaces.XLink + "'"
            + " office:version='1.4'>"
            + "<office:automatic-styles>"
            + "<style:style style:name='GraphicStyle' "
            + "style:family='graphic'><style:graphic-properties "
            + "draw:fill='solid'/></style:style>"
            + "</office:automatic-styles>"
            + "<office:body><office:drawing>"
            + "<draw:page draw:name='Page'>"
            + "<draw:frame draw:name='Image Frame' "
            + "draw:style-name='GraphicStyle'>"
            + "<draw:image xlink:href='Pictures/shared.png' "
            + "xlink:type='simple' draw:mime-type='image/png'/>"
            + "</draw:frame>" + extras
            + "</draw:page></office:drawing></office:body>"
            + "</office:document-content>";
    }

    static string ResourceManifestXml(bool includeExtras)
    {
        string extras = includeExtras
            ? Entry("Pictures/orphan.png", OdfMediaTypes.Png)
                + Entry(OdfPartNames.Thumbnail, OdfMediaTypes.Png)
            : "";
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<manifest:manifest xmlns:manifest='"
            + OdfNamespaces.Manifest + "' manifest:version='1.4'>"
            + Entry("/", OdfMediaTypes.Graphics)
            + Entry(OdfPartNames.Content, OdfMediaTypes.Xml)
            + Entry("Pictures/shared.png", OdfMediaTypes.Png)
            + extras + "</manifest:manifest>";
    }

    static string Entry(string path, string mediaType)
    {
        return "<manifest:file-entry manifest:full-path='" + path
            + "' manifest:media-type='" + mediaType + "'/>";
    }

    static byte[] PngData(byte marker)
    {
        return [
            137, 80, 78, 71, 13, 10, 26, 10,
            marker, 0, 0, 0
        ];
    }

    static byte[] GifData()
    {
        return [71, 73, 70, 56, 57, 97, 1, 0, 1, 0];
    }

    static string ComprehensiveDrawingXml()
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:draw='" + OdfNamespaces.Drawing + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:svg='" + OdfNamespaces.Svg + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:fo='" + OdfNamespaces.Fo + "'"
            + " xmlns:xlink='" + OdfNamespaces.XLink + "'"
            + " xmlns:dr3d='" + OdfNamespaces.Dr3d + "'"
            + " xmlns:chart='" + OdfNamespaces.Chart + "'"
            + " office:version='1.4' office:mimetype='"
            + OdfMediaTypes.Graphics + "'>"
            + "<office:automatic-styles>"
            + "<style:style style:name='Graphic' style:family='graphic'>"
            + "<style:graphic-properties draw:fill='solid' "
            + "draw:fill-color='#112233'/></style:style>"
            + "<style:style style:name='ThreeD' style:family='graphic'>"
            + "<style:graphic-properties dr3d:diffuse-color='#445566' "
            + "dr3d:backface-culling='enabled'/></style:style>"
            + "</office:automatic-styles>"
            + "<office:body><office:drawing>"
            + "<draw:page draw:name='Page'>"
            + "<draw:layer-set><draw:layer draw:name='layout'/></draw:layer-set>"
            + "<draw:g draw:name='Group' "
            + "draw:transform='translate(1cm 2cm) rotate(0.5)'>"
            + "<draw:rect draw:name='Rectangle' draw:style-name='Graphic' "
            + "draw:corner-radius='0.2cm'/>"
            + "<draw:ellipse draw:name='Ellipse' svg:cx='2cm' svg:cy='2cm' "
            + "svg:rx='1cm' svg:ry='2cm'/>"
            + "<draw:line draw:name='Line' svg:x1='0cm' svg:y1='0cm' "
            + "svg:x2='1cm' svg:y2='1cm'/>"
            + "<draw:polyline draw:name='Polyline' draw:points='0,0 10,10' "
            + "svg:viewBox='0 0 10 10'/>"
            + "<draw:polygon draw:name='Polygon' "
            + "draw:points='0,0 10,0 10,10' svg:viewBox='0 0 10 10'/>"
            + "<draw:regular-polygon draw:name='Regular Polygon' "
            + "draw:corners='5' draw:concave='true'/>"
            + "<draw:path draw:name='Path' svg:d='M 0 0 L 10 10' "
            + "svg:viewBox='0 0 10 10'/>"
            + "<draw:connector draw:name='Connector' draw:type='line' "
            + "svg:x1='0cm' svg:y1='0cm' svg:x2='2cm' svg:y2='2cm'/>"
            + "<draw:measure draw:name='Measure' svg:x1='0cm' svg:y1='0cm' "
            + "svg:x2='3cm' svg:y2='0cm'/>"
            + "<draw:caption draw:name='Caption' draw:caption-point-x='1cm' "
            + "draw:caption-point-y='1cm'><text:p>Caption</text:p>"
            + "</draw:caption>"
            + "<draw:control draw:name='Control' draw:control='Control1'/>"
            + "<draw:custom-shape draw:name='Custom'>"
            + "<draw:enhanced-geometry draw:type='rectangle' "
            + "svg:viewBox='0 0 10 10' "
            + "draw:enhanced-path='M 0 0 L 10 10 N'/>"
            + "</draw:custom-shape>"
            + "</draw:g>"
            + "<draw:frame draw:name='Anchored Frame' draw:layer='layout' "
            + "text:anchor-type='page' text:anchor-page-number='2'>"
            + "<draw:text-box fo:min-width='2cm'><text:p>Text</text:p>"
            + "</draw:text-box></draw:frame>"
            + "<draw:frame draw:name='Image Frame'><draw:image>"
            + "<office:binary-data>R0lGODlhAQABAA==</office:binary-data>"
            + "</draw:image></draw:frame>"
            + "<draw:frame draw:name='Media Frame'><draw:plugin "
            + "xlink:href='https://example.com/video.mp4' xlink:type='simple' "
            + "draw:mime-type='video/mp4'/></draw:frame>"
            + "<draw:frame draw:name='OLE Frame'><draw:object-ole "
            + "draw:class-id='ole'><office:binary-data>AQID</office:binary-data>"
            + "</draw:object-ole></draw:frame>"
            + "<draw:frame draw:name='Chart Frame'><draw:object>"
            + "<office:document office:version='1.4' office:mimetype='"
            + OdfMediaTypes.Chart + "'><office:body><office:chart>"
            + "<chart:chart chart:class='chart:bar'><chart:plot-area/>"
            + "</chart:chart></office:chart></office:body></office:document>"
            + "</draw:object></draw:frame>"
            + "<dr3d:scene draw:name='Scene' dr3d:vrp='(1 2 3)' "
            + "dr3d:vpn='(0 0 1)' dr3d:vup='(0 1 0)' "
            + "dr3d:projection='perspective'>"
            + "<dr3d:light dr3d:direction='(0 -1 1)' dr3d:enabled='true'/>"
            + "<dr3d:cube draw:name='Cube' draw:style-name='ThreeD' "
            + "dr3d:min-edge='(-1 -1 -1)' dr3d:max-edge='(1 1 1)'/>"
            + "<dr3d:sphere draw:name='Sphere' dr3d:center='(0 0 0)' "
            + "dr3d:size='(2 2 2)'/>"
            + "<dr3d:rotate draw:name='Rotate' svg:viewBox='0 0 10 10' "
            + "svg:d='M0 0L10 10'/>"
            + "<dr3d:extrude draw:name='Extrude' svg:viewBox='0 0 10 10' "
            + "svg:d='M0 0L10 0L10 10z'/>"
            + "</dr3d:scene>"
            + "</draw:page></office:drawing></office:body>"
            + "</office:document>";
    }
}
