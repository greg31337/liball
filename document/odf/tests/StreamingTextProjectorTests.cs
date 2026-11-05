using System.IO.Compression;
using System.Text;

namespace odf.Tests;

public class StreamingTextProjectorTests
{
    [Test]
    public void ProjectsDocumentNotesAndCommentsWithOdfTextSemantics()
    {
        string xml = DocumentStart()
            + "<office:body><office:text>\n"
            + "<text:h>Title</text:h>"
            + "<text:p>Alpha<text:s text:c='3'/>Beta<text:tab/>Tab"
            + "<text:line-break/>Line<text:soft-page-break/>"
            + "<text:ruby><text:ruby-base>base</text:ruby-base>"
            + "<text:ruby-text>reading</text:ruby-text></text:ruby></text:p>"
            + "<text:p>Before<text:note><text:note-citation>1"
            + "</text:note-citation><text:note-body><text:p>Footnote"
            + "</text:p></text:note-body></text:note>After"
            + "<office:annotation><dc:creator>Alice</dc:creator>"
            + "<dc:date>2026-09-02</dc:date><text:p>Comment</text:p>"
            + "</office:annotation></text:p>"
            + "</office:text></office:body></office:document-content>";
        OdfLoadOptions options = new OdfLoadOptions
        {
            LeaveInputOpen = true,
            StreamSelection = new OdfStreamSelection
            {
                IncludeElementEvents = false,
                IncludeTextEvents = false
            }
        };
        using MemoryStream input = XmlInput(xml);

        string projected = StreamingTextProjector.ProjectXmlText(
            input,
            OdfPartNames.Content,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(projected, Is.EqualTo(
                "Title\nAlpha   Beta\tTab\nLinebase"
                + "\nBefore1\nFootnote\nAfter\nComment"));
            Assert.That(projected, Does.Not.Contain("reading"));
            Assert.That(projected, Does.Not.Contain("Alice"));
            Assert.That(projected, Does.Not.Contain("2026-09-02"));
            Assert.That(input.CanRead, Is.True);
        });
    }

    [Test]
    public void ProjectsHeaderFooterAndPresentationNoteContainers()
    {
        string xml = "<office:document-styles"
            + NamespaceDeclarations()
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:presentation='" + OdfNamespaces.Presentation + "'>"
            + "<office:styles><style:style><text:p>Ignored style text"
            + "</text:p></style:style></office:styles>"
            + "<office:master-styles><style:master-page>"
            + "<style:header><text:p>Header</text:p></style:header>"
            + "<style:footer-left><text:p>Footer left</text:p>"
            + "</style:footer-left></style:master-page></office:master-styles>"
            + "<presentation:notes><text:p>Slide note</text:p>"
            + "</presentation:notes></office:document-styles>";
        using MemoryStream input = XmlInput(xml);

        string projected = StreamingTextProjector.ProjectXmlText(
            input,
            OdfPartNames.Styles,
            new OdfLoadOptions { LeaveInputOpen = true });

        Assert.That(projected,
            Is.EqualTo("Header\nFooter left\nSlide note"));
    }

    [Test]
    public void ProjectsOnlyCallerSelectedTextContainers()
    {
        string xml = "<office:document-content"
            + NamespaceDeclarations()
            + " xmlns:draw='" + OdfNamespaces.Drawing + "'>"
            + "<office:body><office:text><text:p>Ignored body</text:p>"
            + "<draw:frame><draw:text-box>Direct one</draw:text-box>"
            + "</draw:frame><draw:text-box><text:p>Box"
            + "<text:s text:c='2'/>two</text:p></draw:text-box>"
            + "</office:text></office:body></office:document-content>";
        OdfLoadOptions options = new OdfLoadOptions
        {
            LeaveInputOpen = true,
            StreamSelection = new OdfStreamSelection
            {
                ElementNames =
                [
                    new OdfQualifiedName
                    {
                        NamespaceName = OdfNamespaces.Drawing,
                        LocalName = "text-box"
                    }
                ],
                SkipUnselectedSubtrees = false
            }
        };
        using MemoryStream input = XmlInput(xml);

        string projected = StreamingTextProjector.ProjectXmlText(
            input,
            OdfPartNames.Content,
            options);

        Assert.That(projected, Is.EqualTo("Direct one\nBox  two"));
    }

    [Test]
    public void ProjectsSelectedPackagePartsWithoutParsingUnrelatedXml()
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

        string projected = StreamingTextProjector.ProjectPackageText(
            input,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(projected, Is.EqualTo("Package text"));
            Assert.That(input.CanRead, Is.True);
        });
    }

    [Test]
    public void BoundsExpandedSpaceRunsByTheXmlTextLimit()
    {
        string xml = DocumentStart()
            + "<office:body><office:text><text:p><text:s text:c='20'/>"
            + "</text:p></office:text></office:body>"
            + "</office:document-content>";
        OdfLoadOptions options = new OdfLoadOptions
        {
            LeaveInputOpen = true,
            SecurityLimits = new OdfSecurityLimits
            {
                MaximumXmlTextLength = 4
            }
        };
        using MemoryStream input = XmlInput(xml);

        string projected = StreamingTextProjector.ProjectXmlText(
            input,
            OdfPartNames.Content,
            options);

        Assert.That(projected, Is.EqualTo("    "));
    }

    static byte[] CreatePackage()
    {
        string content = DocumentStart()
            + "<office:body><office:text><text:p>Package text</text:p>"
            + "</office:text></office:body></office:document-content>";
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
                "unrelated malformed XML",
                CompressionLevel.Optimal);
            WriteEntry(
                archive,
                OdfPartNames.Content,
                content,
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

    static MemoryStream XmlInput(string xml)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(xml));
    }

    static string DocumentStart()
    {
        return "<office:document-content" + NamespaceDeclarations() + ">";
    }

    static string NamespaceDeclarations()
    {
        return " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:dc='" + OdfNamespaces.DublinCore + "'";
    }
}
