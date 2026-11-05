using System.IO.Compression;
using System.Text;

namespace odf.Tests;

public class DrawingResourceManagerTests
{
    [Test]
    public void DetectsOrphansAndRemovesThemOnlyWhenExplicitlyRequested()
    {
        OdfDocument document = ReadPackage(CreateSourcePackage());

        Assert.That(
            document.OrphanedResourcePaths,
            Is.EqualTo(new[] { "Pictures/orphan.png" }));

        OdfDocument unchanged = ReadPackage(Write(document));
        Assert.That(
            unchanged.PackageEntries.Any(item =>
                item.Path == "Pictures/orphan.png"),
            Is.True);

        OdfEditResult result = DrawingResourceManager.CollectGarbage(
            document);
        OdfDocument collected = ReadPackage(Write(document));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(result.AffectedCount, Is.EqualTo(1));
            Assert.That(document.OrphanedResourcePaths, Is.Empty);
            Assert.That(collected.PackageEntries.Any(item =>
                item.Path == "Pictures/orphan.png"), Is.False);
            Assert.That(collected.Manifest?.Entries.Any(item =>
                item.FullPath == "Pictures/orphan.png"), Is.False);
            Assert.That(collected.PackageEntries.Any(item =>
                item.Path == "Object1/content.xml"), Is.True);
        });
    }

    [Test]
    public void MovesResourceAndUpdatesReferencesAndManifest()
    {
        OdfDocument document = ReadPackage(CreateSourcePackage());

        OdfEditResult result = DrawingResourceManager.MovePackageResource(
            document,
            "Pictures/shared.png",
            "Media/moved.png");
        OdfDocument reread = ReadPackage(Write(document));
        OdfImage image = reread.Images.Single(item =>
            item.PackagePath == "Media/moved.png");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(image.Href, Is.EqualTo("Media/moved.png"));
            Assert.That(image.PackagePath, Is.EqualTo("Media/moved.png"));
            Assert.That(reread.PackageEntries.Any(item =>
                item.Path == "Pictures/shared.png"), Is.False);
            Assert.That(reread.PackageEntries.Any(item =>
                item.Path == "Media/moved.png"), Is.True);
            Assert.That(reread.Manifest?.Entries.Any(item =>
                item.FullPath == "Media/moved.png"), Is.True);
        });
    }

    [Test]
    public void CopiesCrossDocumentStyleAndPackageDependencyClosure()
    {
        OdfDocument source = ReadPackage(CreateSourcePackage());
        OdfDocument target = ReadPackage(CreateTargetPackage());
        OdfDrawingObject sourceDrawing = source.DrawingObjects.Single(item =>
            item.Name == "Resources");
        OdfNode sourceNode = sourceDrawing.Node ?? new OdfNode();
        OdfNode targetNode = new OdfTreeEditor([]).CloneSubtree(sourceNode);

        string[] copied = DrawingResourceManager.CopyDrawingDependencies(
            source,
            target,
            sourceDrawing,
            targetNode,
            OdfPartNames.Content);
        string imageHref = DescendantAttribute(
            targetNode,
            OdfNamespaces.Drawing,
            "image",
            OdfNamespaces.XLink,
            "href");
        string objectHref = DescendantAttribute(
            targetNode,
            OdfNamespaces.Drawing,
            "object",
            OdfNamespaces.XLink,
            "href");
        string copiedStyleName = AttributeValue(
            targetNode,
            OdfNamespaces.Drawing,
            "style-name");

        Assert.Multiple(() =>
        {
            Assert.That(imageHref,
                Is.EqualTo("Pictures/shared-copy-2.png"));
            Assert.That(objectHref, Is.EqualTo("Object1"));
            Assert.That(copied, Does.Contain("Pictures/shared-copy-2.png"));
            Assert.That(copied, Does.Contain("Object1/content.xml"));
            Assert.That(copied, Does.Contain("Object1/Pictures/chart.png"));
            Assert.That(target.PackageEntries.Any(item =>
                item.Path == "Pictures/shared-copy-2.png"), Is.True);
            Assert.That(target.PackageEntries.Any(item =>
                item.Path == "Object1/Pictures/chart.png"), Is.True);
            Assert.That(copiedStyleName, Is.Not.EqualTo("GraphicStyle"));
            Assert.That(target.Styles.Any(item =>
                item.Name == copiedStyleName
                && item.Family == OdfStyleFamily.Graphic), Is.True);
        });
    }

    static OdfDocument ReadPackage(byte[] data)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            data,
            "resources.odg",
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

    static byte[] CreateSourcePackage()
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
                Encoding.UTF8.GetBytes(SourceContentXml()),
                CompressionLevel.Optimal);
            WriteEntry(
                archive,
                OdfPartNames.Manifest,
                Encoding.UTF8.GetBytes(SourceManifestXml()),
                CompressionLevel.Optimal);
            WriteEntry(
                archive,
                "Pictures/shared.png",
                PngData(1),
                CompressionLevel.Optimal);
            WriteEntry(
                archive,
                "Pictures/orphan.png",
                PngData(2),
                CompressionLevel.Optimal);
            archive.CreateEntry("Object1/", CompressionLevel.NoCompression);
            WriteEntry(
                archive,
                "Object1/mimetype",
                Encoding.UTF8.GetBytes(OdfMediaTypes.Graphics),
                CompressionLevel.NoCompression);
            WriteEntry(
                archive,
                "Object1/content.xml",
                Encoding.UTF8.GetBytes(EmbeddedContentXml()),
                CompressionLevel.Optimal);
            WriteEntry(
                archive,
                "Object1/Pictures/chart.png",
                PngData(3),
                CompressionLevel.Optimal);
        }
        return output.ToArray();
    }

    static byte[] CreateTargetPackage()
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
                Encoding.UTF8.GetBytes(TargetContentXml()),
                CompressionLevel.Optimal);
            WriteEntry(
                archive,
                OdfPartNames.Manifest,
                Encoding.UTF8.GetBytes(TargetManifestXml()),
                CompressionLevel.Optimal);
            WriteEntry(
                archive,
                "Pictures/shared.png",
                PngData(9),
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

    static string SourceContentXml()
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document-content"
            + Namespaces()
            + " office:version='1.4'>"
            + "<office:automatic-styles>"
            + "<style:style style:name='GraphicStyle' "
            + "style:family='graphic'><style:graphic-properties "
            + "draw:fill='solid'/></style:style>"
            + "</office:automatic-styles>"
            + "<office:body><office:drawing>"
            + "<draw:page draw:name='Page'>"
            + "<draw:frame draw:name='Resources' "
            + "draw:style-name='GraphicStyle'>"
            + "<draw:image xlink:href='Pictures/shared.png' "
            + "xlink:type='simple' draw:mime-type='image/png'/>"
            + "<draw:object xlink:href='./Object1' xlink:type='simple'/>"
            + "</draw:frame></draw:page>"
            + "</office:drawing></office:body>"
            + "</office:document-content>";
    }

    static string TargetContentXml()
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document-content"
            + Namespaces()
            + " office:version='1.4'>"
            + "<office:automatic-styles>"
            + "<style:style style:name='GraphicStyle' "
            + "style:family='graphic'><style:graphic-properties "
            + "draw:fill='none'/></style:style>"
            + "</office:automatic-styles>"
            + "<office:body><office:drawing>"
            + "<draw:page draw:name='Target'/></office:drawing></office:body>"
            + "</office:document-content>";
    }

    static string EmbeddedContentXml()
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document-content"
            + Namespaces()
            + " office:version='1.4'>"
            + "<office:body><office:drawing><draw:page draw:name='Chart'>"
            + "<draw:image xlink:href='Pictures/chart.png' "
            + "xlink:type='simple'/></draw:page>"
            + "</office:drawing></office:body></office:document-content>";
    }

    static string SourceManifestXml()
    {
        return ManifestStart()
            + ManifestEntry("/", OdfMediaTypes.Graphics)
            + ManifestEntry(OdfPartNames.Content, OdfMediaTypes.Xml)
            + ManifestEntry("Pictures/shared.png", OdfMediaTypes.Png)
            + ManifestEntry("Pictures/orphan.png", OdfMediaTypes.Png)
            + ManifestEntry("Object1/", OdfMediaTypes.Graphics)
            + ManifestEntry("Object1/mimetype", "")
            + ManifestEntry("Object1/content.xml", OdfMediaTypes.Xml)
            + ManifestEntry(
                "Object1/Pictures/chart.png",
                OdfMediaTypes.Png)
            + "</manifest:manifest>";
    }

    static string TargetManifestXml()
    {
        return ManifestStart()
            + ManifestEntry("/", OdfMediaTypes.Graphics)
            + ManifestEntry(OdfPartNames.Content, OdfMediaTypes.Xml)
            + ManifestEntry("Pictures/shared.png", OdfMediaTypes.Png)
            + "</manifest:manifest>";
    }

    static string ManifestStart()
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<manifest:manifest xmlns:manifest='"
            + OdfNamespaces.Manifest
            + "' manifest:version='1.4'>";
    }

    static string ManifestEntry(string path, string mediaType)
    {
        return "<manifest:file-entry manifest:full-path='" + path
            + "' manifest:media-type='" + mediaType + "'/>";
    }

    static string Namespaces()
    {
        return " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:draw='" + OdfNamespaces.Drawing + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:xlink='" + OdfNamespaces.XLink + "'";
    }

    static byte[] PngData(byte marker)
    {
        return [
            137, 80, 78, 71, 13, 10, 26, 10,
            marker, 0, 0, 0
        ];
    }

    static string DescendantAttribute(
        OdfNode node,
        string elementNamespace,
        string elementName,
        string attributeNamespace,
        string attributeName)
    {
        OdfNode element = Descendant(node, elementNamespace, elementName);
        return AttributeValue(
            element,
            attributeNamespace,
            attributeName);
    }

    static OdfNode Descendant(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        if (node.Name?.NamespaceName == namespaceName
            && node.Name.LocalName == localName)
        {
            return node;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode? result = OptionalDescendant(
                node.Children[index],
                namespaceName,
                localName);
            if (result != null)
            {
                return result;
            }
            index = index + 1;
        }
        throw new AssertionException("The expected descendant was not found.");
    }

    static OdfNode? OptionalDescendant(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        if (node.Name?.NamespaceName == namespaceName
            && node.Name.LocalName == localName)
        {
            return node;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode? result = OptionalDescendant(
                node.Children[index],
                namespaceName,
                localName);
            if (result != null)
            {
                return result;
            }
            index = index + 1;
        }
        return null;
    }

    static string AttributeValue(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        return node.Attributes.Single(item =>
            item.Name.NamespaceName == namespaceName
            && item.Name.LocalName == localName).Value;
    }
}
