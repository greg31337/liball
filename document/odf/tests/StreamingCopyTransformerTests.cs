using System.IO.Compression;
using System.Text;

namespace odf.Tests;

public class StreamingCopyTransformerTests
{
    [Test]
    public void TransformsOnlySelectedSubtreesAndPreservesForeignEvents()
    {
        string xml = FeatureXml();
        OdfLoadOptions loadOptions = SelectedParagraphOptions();
        OdfSaveOptions saveOptions = new OdfSaveOptions
        {
            LeaveOutputOpen = true
        };
        using MemoryStream input = XmlInput(xml);
        using MemoryStream output = new MemoryStream();
        SelectedTextTransform transform = new SelectedTextTransform();

        StreamingCopyTransformer.CopyXml(
            input,
            output,
            OdfPartNames.Content,
            loadOptions,
            saveOptions,
            transform);

        string result = Encoding.UTF8.GetString(output.ToArray());
        OdfNode root = OdfTreeBuilder.ReadBytes(
            output.ToArray(),
            OdfPartNames.Content,
            new OdfLoadOptions()).Single(item =>
                item.Kind == OdfNodeKind.Element);
        OdfNode paragraph = Descendant(root, OdfNamespaces.Text, "p");
        OdfNode heading = Descendant(root, OdfNamespaces.Text, "h");
        OdfNode selectedForeign = Descendant(
            paragraph,
            "urn:example:foreign",
            "inside");
        OdfNode outsideForeign = Descendant(
            root,
            "urn:example:foreign",
            "outside");

        Assert.Multiple(() =>
        {
            Assert.That(NodeText(paragraph),
                Is.EqualTo("CHANGESELECTED FOREIGN"));
            Assert.That(NodeText(heading), Is.EqualTo("leave heading"));
            Assert.That(NodeText(selectedForeign),
                Is.EqualTo("SELECTED FOREIGN"));
            Assert.That(NodeText(outsideForeign), Is.EqualTo("outside text"));
            Assert.That(AttributeValue(
                paragraph,
                OdfNamespaces.Text,
                "style-name"), Is.EqualTo("Body"));
            Assert.That(AttributeValue(
                paragraph,
                "urn:example:foreign",
                "transformed"), Is.EqualTo("yes"));
            Assert.That(AttributeValue(
                selectedForeign,
                "urn:example:foreign",
                "flag"), Is.EqualTo("keep"));
            Assert.That(result, Does.Contain("<!--inside comment-->"));
            Assert.That(result, Does.Contain("<?keep instruction?>"));
            Assert.That(transform.EventCount, Is.GreaterThan(4));
            Assert.That(input.CanRead, Is.True);
            Assert.That(output.CanWrite, Is.True);
        });
    }

    [Test]
    public void CopiesUnselectedPackageEntriesAndFinalizesManifest()
    {
        byte[] originalPackage = CreatePackage();
        byte[] originalStyles;
        byte[] originalImage;
        using (MemoryStream originalInput = new MemoryStream(originalPackage))
        using (ZipArchive originalArchive = new ZipArchive(
            originalInput,
            ZipArchiveMode.Read,
            true))
        {
            originalStyles = ReadEntry(originalArchive, OdfPartNames.Styles);
            originalImage = ReadEntry(originalArchive, "Pictures/picture.bin");
        }
        MemoryStream input = new MemoryStream(originalPackage);
        MemoryStream output = new MemoryStream();
        OdfLoadOptions loadOptions = SelectedParagraphOptions();
        loadOptions.StreamSelection.PartPaths = [OdfPartNames.Content];

        StreamingCopyTransformer.CopyPackage(
            input,
            output,
            loadOptions,
            new OdfSaveOptions { LeaveOutputOpen = true },
            new SelectedTextTransform());

        byte[] transformedPackage = output.ToArray();
        ZipPackageInspection inspection =
            ZipPackageReader.InspectBytes(transformedPackage);
        using MemoryStream transformedInput =
            new MemoryStream(transformedPackage);
        using ZipArchive archive = new ZipArchive(
            transformedInput,
            ZipArchiveMode.Read,
            true);
        string content = Encoding.UTF8.GetString(
            ReadEntry(archive, OdfPartNames.Content));
        OdfManifest manifest = ManifestParser.ParseBytes(
            ReadEntry(archive, OdfPartNames.Manifest));

        Assert.Multiple(() =>
        {
            Assert.That(content, Does.Contain("CHANGE"));
            Assert.That(content, Does.Contain("leave heading"));
            Assert.That(content, Does.Contain("ext:outside"));
            Assert.That(ReadEntry(archive, OdfPartNames.Styles),
                Is.EqualTo(originalStyles));
            Assert.That(ReadEntry(archive, "Pictures/picture.bin"),
                Is.EqualTo(originalImage));
            Assert.That(
                inspection.IsValid,
                Is.True,
                string.Join(" | ", inspection.Diagnostics.Select(item =>
                    item.Code + ": " + item.Message)));
            Assert.That(inspection.Entries[0].Path,
                Is.EqualTo(OdfPartNames.Mimetype));
            Assert.That(ManifestMediaType(manifest, OdfPartNames.Root),
                Is.EqualTo(OdfMediaTypes.Text));
            Assert.That(ManifestMediaType(manifest, OdfPartNames.Content),
                Is.EqualTo(OdfMediaTypes.Xml));
            Assert.That(ManifestMediaType(manifest, OdfPartNames.Styles),
                Is.EqualTo(OdfMediaTypes.Xml));
            Assert.That(ManifestMediaType(
                manifest,
                "Pictures/picture.bin"), Is.EqualTo(OdfMediaTypes.OctetStream));
            Assert.That(input.CanRead, Is.True);
            Assert.That(output.CanWrite, Is.True);
        });
        input.Dispose();
        output.Dispose();
    }

    [Test]
    public void RejectsTransformsThatChangeEventKinds()
    {
        using MemoryStream input = XmlInput(FeatureXml());
        using MemoryStream output = new MemoryStream();

        Assert.That(() => StreamingCopyTransformer.CopyXml(
                input,
                output,
                OdfPartNames.Content,
                SelectedParagraphOptions(),
                new OdfSaveOptions { LeaveOutputOpen = true },
                new InvalidKindTransform()),
            Throws.TypeOf<OdfXmlException>());
    }

    static OdfLoadOptions SelectedParagraphOptions()
    {
        return new OdfLoadOptions
        {
            LeaveInputOpen = true,
            StreamSelection = new OdfStreamSelection
            {
                ElementNames =
                [
                    new OdfQualifiedName
                    {
                        NamespaceName = OdfNamespaces.Text,
                        LocalName = "p"
                    }
                ],
                SkipUnselectedSubtrees = false
            }
        };
    }

    static string FeatureXml()
    {
        return "<?keep instruction?><office:document-content xmlns:office='"
            + OdfNamespaces.Office + "' xmlns:text='" + OdfNamespaces.Text
            + "' xmlns:ext='urn:example:foreign'><office:body><office:text>"
            + "<text:p text:style-name='Body'>change"
            + "<ext:inside ext:flag='keep'>selected foreign</ext:inside>"
            + "<!--inside comment--></text:p>"
            + "<text:h>leave heading</text:h>"
            + "<ext:outside ext:data='keep'>outside text</ext:outside>"
            + "</office:text></office:body></office:document-content>";
    }

    static byte[] CreatePackage()
    {
        string manifest = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<manifest:manifest xmlns:manifest='" + OdfNamespaces.Manifest
            + "' manifest:version='1.4'>"
            + "<manifest:file-entry manifest:full-path='/' "
            + "manifest:media-type='" + OdfMediaTypes.Text + "' "
            + "manifest:version='1.4'/>"
            + "<manifest:file-entry manifest:full-path='content.xml' "
            + "manifest:media-type='text/xml'/>"
            + "<manifest:file-entry manifest:full-path='styles.xml' "
            + "manifest:media-type='text/xml'/>"
            + "<manifest:file-entry manifest:full-path='Pictures/picture.bin' "
            + "manifest:media-type='application/octet-stream'/>"
            + "</manifest:manifest>";
        byte[] image = new byte[200000];
        int index = 0;
        while (index < image.Length)
        {
            image[index] = (byte)(index % 251);
            index = index + 1;
        }
        using MemoryStream output = new MemoryStream();
        using (ZipArchive archive = new ZipArchive(
            output,
            ZipArchiveMode.Create,
            true))
        {
            WriteEntry(
                archive,
                OdfPartNames.Mimetype,
                Encoding.UTF8.GetBytes(OdfMediaTypes.Text),
                CompressionLevel.NoCompression);
            WriteEntry(
                archive,
                OdfPartNames.Content,
                Encoding.UTF8.GetBytes(FeatureXml()),
                CompressionLevel.Optimal);
            WriteEntry(
                archive,
                OdfPartNames.Styles,
                Encoding.UTF8.GetBytes("unselected malformed foreign XML"),
                CompressionLevel.Optimal);
            WriteEntry(
                archive,
                "Pictures/picture.bin",
                image,
                CompressionLevel.NoCompression);
            WriteEntry(
                archive,
                OdfPartNames.Manifest,
                Encoding.UTF8.GetBytes(manifest),
                CompressionLevel.Optimal);
        }
        return output.ToArray();
    }

    static void WriteEntry(
        ZipArchive archive,
        string path,
        byte[] content,
        CompressionLevel compression)
    {
        ZipArchiveEntry entry = archive.CreateEntry(path, compression);
        using Stream output = entry.Open();
        output.Write(content, 0, content.Length);
    }

    static byte[] ReadEntry(ZipArchive archive, string path)
    {
        ZipArchiveEntry entry = archive.GetEntry(path)!;
        using Stream input = entry.Open();
        using MemoryStream output = new MemoryStream();
        input.CopyTo(output);
        return output.ToArray();
    }

    static string ManifestMediaType(OdfManifest manifest, string path)
    {
        return manifest.Entries.Single(item => item.FullPath == path).MediaType;
    }

    static MemoryStream XmlInput(string xml)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(xml));
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
            if (ContainsDescendant(
                node.Children[index],
                namespaceName,
                localName))
            {
                return Descendant(
                    node.Children[index],
                    namespaceName,
                    localName);
            }
            index = index + 1;
        }
        throw new AssertionException("Expected XML descendant was not found.");
    }

    static bool ContainsDescendant(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        if (node.Name?.NamespaceName == namespaceName
            && node.Name.LocalName == localName)
        {
            return true;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            if (ContainsDescendant(
                node.Children[index],
                namespaceName,
                localName))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static string NodeText(OdfNode node)
    {
        StringBuilder result = new StringBuilder();
        AppendNodeText(result, node);
        return result.ToString();
    }

    static void AppendNodeText(StringBuilder result, OdfNode node)
    {
        if (node.Kind == OdfNodeKind.Text)
        {
            result.Append(node.Value);
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            AppendNodeText(result, node.Children[index]);
            index = index + 1;
        }
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

    class SelectedTextTransform : IStreamingXmlEventTransform
    {
        internal int EventCount;

        public OdfStreamEvent TransformXmlEvent(OdfStreamEvent streamEvent)
        {
            EventCount = EventCount + 1;
            if (streamEvent.Kind == OdfStreamEventKind.Text)
            {
                streamEvent.Value = streamEvent.Value.ToUpperInvariant();
            }
            if (streamEvent.Kind == OdfStreamEventKind.ElementStart
                && streamEvent.Name?.NamespaceName == OdfNamespaces.Text
                && streamEvent.Name.LocalName == "p")
            {
                OdfAttribute added = new OdfAttribute();
                added.Name.NamespaceName = "urn:example:foreign";
                added.Name.LocalName = "transformed";
                added.Name.Prefix = "ext";
                added.Value = "yes";
                streamEvent.Attributes = [.. streamEvent.Attributes, added];
            }
            return streamEvent;
        }
    }

    class InvalidKindTransform : IStreamingXmlEventTransform
    {
        public OdfStreamEvent TransformXmlEvent(OdfStreamEvent streamEvent)
        {
            streamEvent.Kind = OdfStreamEventKind.Comment;
            return streamEvent;
        }
    }
}
