using System.IO.Compression;
using System.Text;

namespace odf.Tests;

public class StreamingNodeMaterializerTests
{
    [Test]
    public void MaterializesSelectedSubtreesAsIndependentCompleteTrees()
    {
        string textNamespace =
            "urn:oasis:names:tc:opendocument:xmlns:text:1.0";
        string officeNamespace =
            "urn:oasis:names:tc:opendocument:xmlns:office:1.0";
        string xml = "<office:root xmlns:office='" + officeNamespace
            + "' xmlns:text='" + textNamespace + "' xml:id='outer'>"
            + "<text:wrapper xml:space='preserve'>"
            + "<text:p text:style-name='Body'> \n "
            + "<text:span xml:id='inner'>value</text:span>"
            + "<!--note--><?go yes?></text:p>"
            + "<text:p>second</text:p>"
            + "</text:wrapper></office:root>";
        OdfLoadOptions options = new OdfLoadOptions
        {
            LeaveInputOpen = true,
            StreamSelection = new OdfStreamSelection
            {
                ElementNames =
                [
                    new OdfQualifiedName
                    {
                        NamespaceName = textNamespace,
                        LocalName = "p"
                    }
                ],
                IncludeTextEvents = false,
                IncludeCommentEvents = false,
                IncludeProcessingInstructionEvents = false,
                SkipUnselectedSubtrees = false
            }
        };
        using MemoryStream input = new MemoryStream(
            Encoding.UTF8.GetBytes(xml));

        OdfNode[] nodes =
            StreamingNodeMaterializer.MaterializeSelectedSubtrees(
                input,
                OdfPartNames.Content,
                options);

        OdfNode first = nodes[0];
        OdfNode whitespace = first.Children[0];
        OdfNode span = first.Children[1];
        Assert.Multiple(() =>
        {
            Assert.That(nodes, Has.Length.EqualTo(2));
            Assert.That(nodes[0].Name?.LocalName, Is.EqualTo("p"));
            Assert.That(nodes[1].Value, Is.Empty);
            Assert.That(first.Parent, Is.Null);
            Assert.That(first.Location.PartPath,
                Is.EqualTo(OdfPartNames.Content));
            Assert.That(first.Location.XmlId, Is.EqualTo("outer"));
            Assert.That(first.Attributes.Single().Location.XmlId,
                Is.EqualTo("outer"));
            Assert.That(NamespaceValue(first, "office"),
                Is.EqualTo(officeNamespace));
            Assert.That(NamespaceValue(first, "text"),
                Is.EqualTo(textNamespace));
            Assert.That(first.NamespaceDeclarations.Any(item =>
                item.Prefix == "xml"), Is.False);
            Assert.That(whitespace.Kind, Is.EqualTo(OdfNodeKind.Text));
            Assert.That(whitespace.IsSignificantWhitespace, Is.True);
            Assert.That(span.Parent, Is.SameAs(first));
            Assert.That(span.XmlId, Is.EqualTo("inner"));
            Assert.That(span.Children.Single().Location.XmlId,
                Is.EqualTo("inner"));
            Assert.That(first.Children.Any(item =>
                item.Kind == OdfNodeKind.Comment
                && item.Value == "note"), Is.True);
            Assert.That(first.Children.Any(item =>
                item.Kind == OdfNodeKind.ProcessingInstruction
                && item.Name?.LocalName == "go"
                && item.Value == "yes"), Is.True);
            Assert.That(nodes[1].Children.Single().Value,
                Is.EqualTo("second"));
            Assert.That(input.CanRead, Is.True);
        });
    }

    [Test]
    public void LoadsOnlySelectedXmlPackageParts()
    {
        byte[] package = CreatePackage();
        OdfLoadOptions options = new OdfLoadOptions
        {
            LeaveInputOpen = true,
            StreamSelection = new OdfStreamSelection
            {
                PartPaths = [OdfPartNames.Content]
            }
        };
        using MemoryStream input = new MemoryStream(package);

        OdfNode[] nodes =
            StreamingNodeMaterializer.LoadSelectedPackageParts(
                input,
                options);

        Assert.Multiple(() =>
        {
            Assert.That(nodes, Has.Length.EqualTo(1));
            Assert.That(nodes[0].Name?.LocalName,
                Is.EqualTo("document-content"));
            Assert.That(nodes[0].Location.PartPath,
                Is.EqualTo(OdfPartNames.Content));
            Assert.That(nodes[0].Children.Single().Name?.LocalName,
                Is.EqualTo("body"));
            Assert.That(input.CanRead, Is.True);
        });
    }

    [Test]
    public void AppliesElementSelectionInsideSelectedPackageParts()
    {
        byte[] package = CreatePackage();
        string officeNamespace =
            "urn:oasis:names:tc:opendocument:xmlns:office:1.0";
        OdfLoadOptions options = new OdfLoadOptions
        {
            LeaveInputOpen = true,
            StreamSelection = new OdfStreamSelection
            {
                PartPaths = [OdfPartNames.Content],
                ElementNames =
                [
                    new OdfQualifiedName
                    {
                        NamespaceName = officeNamespace,
                        LocalName = "body"
                    }
                ],
                SkipUnselectedSubtrees = false
            }
        };
        using MemoryStream input = new MemoryStream(package);

        OdfNode[] nodes =
            StreamingNodeMaterializer.LoadSelectedPackageParts(
                input,
                options);

        Assert.Multiple(() =>
        {
            Assert.That(nodes, Has.Length.EqualTo(1));
            Assert.That(nodes[0].Name?.LocalName, Is.EqualTo("body"));
            Assert.That(NamespaceValue(nodes[0], "office"),
                Is.EqualTo(officeNamespace));
            Assert.That(nodes[0].Parent, Is.Null);
        });
    }

    static string NamespaceValue(OdfNode node, string prefix)
    {
        return node.NamespaceDeclarations.Single(item =>
            item.Prefix == prefix).NamespaceName;
    }

    static byte[] CreatePackage()
    {
        string officeNamespace =
            "urn:oasis:names:tc:opendocument:xmlns:office:1.0";
        using MemoryStream output = new MemoryStream();
        using (ZipArchive archive = new ZipArchive(
            output,
            ZipArchiveMode.Create,
            true))
        {
            WriteEntry(
                archive,
                OdfPartNames.Mimetype,
                OdfMediaTypes.Text,
                CompressionLevel.NoCompression);
            WriteEntry(
                archive,
                OdfPartNames.Styles,
                "this unrelated selected-looking part is not XML",
                CompressionLevel.Optimal);
            WriteEntry(
                archive,
                OdfPartNames.Content,
                "<office:document-content xmlns:office='"
                    + officeNamespace + "'><office:body/>"
                    + "</office:document-content>",
                CompressionLevel.Optimal);
            WriteEntry(
                archive,
                "Pictures/image.bin",
                "unrelated binary content",
                CompressionLevel.Optimal);
        }
        return output.ToArray();
    }

    static void WriteEntry(
        ZipArchive archive,
        string path,
        string value,
        CompressionLevel compression)
    {
        ZipArchiveEntry entry = archive.CreateEntry(path, compression);
        using Stream stream = entry.Open();
        byte[] data = Encoding.UTF8.GetBytes(value);
        stream.Write(data, 0, data.Length);
    }
}
