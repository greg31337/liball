using System.IO.Compression;
using System.Text;

namespace odf.Tests;

public class OdfReaderWriterTests
{
    [Test]
    public void XmlReaderAndWriterSupportBytesWithStandardPartMapping()
    {
        IOdfXmlReader reader = new OdfXmlReader();
        IOdfXmlWriter writer = new OdfXmlWriter();
        byte[] source = Encoding.UTF8.GetBytes(ContentXml("before", "1.4"));

        OdfPart part = reader.ReadXmlBytes(
            source,
            OdfPartNames.Content,
            new OdfLoadOptions());
        TextNode(part).Value = "after";
        OdfSaveOptions saveOptions = new OdfSaveOptions
        {
            TargetVersion = OdfVersion.Version13,
            Deterministic = true
        };
        byte[] written = writer.WriteXmlBytes(part, saveOptions);
        OdfPart reread = reader.ReadXmlBytes(
            written,
            OdfPartNames.Content,
            new OdfLoadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(part.RootName, Is.EqualTo("document-content"));
            Assert.That(part.Path, Is.EqualTo(OdfPartNames.Content));
            Assert.That(part.Body, Is.Not.Null);
            Assert.That(TextNode(reread).Value, Is.EqualTo("after"));
            Assert.That(reread.VersionInfo?.Version, Is.EqualTo(OdfVersion.Version13));
            Assert.That(Encoding.UTF8.GetString(written), Does.Contain("office:version=\"1.3\""));
        });
    }

    [Test]
    public void XmlPathAndStreamOperationsHonorOwnership()
    {
        IOdfXmlReader reader = new OdfXmlReader();
        IOdfXmlWriter writer = new OdfXmlWriter();
        byte[] source = Encoding.UTF8.GetBytes(FlatXml("stream", "1.4", ""));
        TrackingInputStream input = new TrackingInputStream(source);
        OdfLoadOptions loadOptions = new OdfLoadOptions
        {
            LeaveInputOpen = false
        };
        OdfPart fromStream = reader.ReadXmlStream(input, "", loadOptions);
        TrackingOutputStream output = new TrackingOutputStream();
        OdfSaveOptions streamSaveOptions = new OdfSaveOptions
        {
            LeaveOutputOpen = false
        };
        writer.WriteXmlStream(fromStream, output, streamSaveOptions);

        string inputPath = Path.GetTempFileName();
        string outputPath = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(inputPath, source);
            OdfPart fromPath = reader.ReadXmlPath(
                inputPath,
                "",
                new OdfLoadOptions());
            writer.WriteXmlPath(fromPath, outputPath, new OdfSaveOptions());
            OdfPart pathRoundTrip = reader.ReadXmlPath(
                outputPath,
                "",
                new OdfLoadOptions());

            Assert.Multiple(() =>
            {
                Assert.That(input.WasDisposed, Is.True);
                Assert.That(output.WasDisposed, Is.True);
                Assert.That(fromStream.IsFlatDocument, Is.True);
                Assert.That(pathRoundTrip.IsFlatDocument, Is.True);
                Assert.That(TextNode(pathRoundTrip).Value, Is.EqualTo("stream"));
            });
        }
        finally
        {
            File.Delete(inputPath);
            File.Delete(outputPath);
        }
    }

    [Test]
    public void DocumentReaderAndWriterRoundTripEditableFlatDocuments()
    {
        IOdfDocumentReader reader = new OdfDocumentReader();
        IOdfDocumentWriter writer = new OdfDocumentWriter();
        byte[] source = Encoding.UTF8.GetBytes(
            FlatXml("before", "1.4", "<ext:retained/>")
        );

        OdfDocument document = reader.ReadDocumentBytes(
            source,
            "report.fodt",
            new OdfLoadOptions());
        TextNode(document.Parts[0]).Value = "after";
        byte[] written = writer.WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                PreserveUnknownContent = true,
                IndentXml = true
            });
        OdfDocument reread = reader.ReadDocumentBytes(
            written,
            "roundtrip.fodt",
            new OdfLoadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(document.PackageKind, Is.EqualTo(OdfPackageKind.Flat));
            Assert.That(document.Kind, Is.EqualTo(OdfDocumentKind.Text));
            Assert.That(document.MediaType, Is.EqualTo(OdfMediaTypes.Text));
            Assert.That(document.PassthroughNodes, Has.Length.EqualTo(1));
            Assert.That(TextNode(reread.Parts[0]).Value, Is.EqualTo("after"));
            Assert.That(reread.PassthroughNodes, Has.Length.EqualTo(1));
            Assert.That(document.SaveOptions.IndentXml, Is.True);
        });
    }

    [Test]
    public void DocumentPathAndStreamOperationsHonorOwnership()
    {
        IOdfDocumentReader reader = new OdfDocumentReader();
        IOdfDocumentWriter writer = new OdfDocumentWriter();
        byte[] source = Encoding.UTF8.GetBytes(FlatXml("owned", "1.4", ""));
        TrackingInputStream input = new TrackingInputStream(source);
        OdfDocument document = reader.ReadDocumentStream(
            input,
            "owned.fodt",
            new OdfLoadOptions
            {
                LeaveInputOpen = false
            });
        TrackingOutputStream output = new TrackingOutputStream();
        writer.WriteDocumentStream(
            document,
            output,
            new OdfSaveOptions
            {
                LeaveOutputOpen = false
            });

        string inputPath = Path.GetTempFileName();
        string outputPath = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(inputPath, source);
            OdfDocument fromPath = reader.ReadDocumentPath(
                inputPath,
                new OdfLoadOptions());
            writer.WriteDocumentPath(
                fromPath,
                outputPath,
                new OdfSaveOptions());
            OdfDocument pathRoundTrip = reader.ReadDocumentPath(
                outputPath,
                new OdfLoadOptions());

            Assert.Multiple(() =>
            {
                Assert.That(input.WasDisposed, Is.True);
                Assert.That(output.WasDisposed, Is.True);
                Assert.That(TextNode(pathRoundTrip.Parts[0]).Value, Is.EqualTo("owned"));
            });
        }
        finally
        {
            File.Delete(inputPath);
            File.Delete(outputPath);
        }
    }

    [Test]
    public void PackageDocumentRoundTripUpdatesXmlAndPreservesResources()
    {
        byte[] source = CreateDocumentPackage();
        IOdfDocumentReader reader = new OdfDocumentReader();
        IOdfDocumentWriter writer = new OdfDocumentWriter();
        OdfDocument document = reader.ReadDocumentBytes(
            source,
            "document.odt",
            new OdfLoadOptions());
        TextNode(document.Parts[0]).Value = "after";

        byte[] written = writer.WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
        OdfDocument reread = reader.ReadDocumentBytes(
            written,
            "written.odt",
            new OdfLoadOptions());
        IOdfPackageReader packageReader = new OdfPackageReader();
        OdfPackage package = packageReader.ReadPackageBytes(
            written,
            new OdfLoadOptions());
        using Stream resource = packageReader.OpenPackageEntry(
            package,
            "Pictures/resource.bin");

        Assert.Multiple(() =>
        {
            Assert.That(document.PackageKind, Is.EqualTo(OdfPackageKind.Package));
            Assert.That(document.Parts.Select(part => part.Path), Is.EqualTo(new[]
            {
                OdfPartNames.Content,
                OdfPartNames.Styles,
                OdfPartNames.Metadata,
                OdfPartNames.Settings
            }));
            Assert.That(document.Kind, Is.EqualTo(OdfDocumentKind.Text));
            Assert.That(TextNode(reread.Parts[0]).Value, Is.EqualTo("after"));
            Assert.That(ReadAll(resource), Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(package.IsValid, Is.True);
            Assert.That(package.Entries.Any(entry => entry.Path == "Pictures/resource.bin"), Is.True);
        });
    }

    [Test]
    public void NonMaterializedDocumentsUseOriginalOrPackageCopyThrough()
    {
        IOdfDocumentReader reader = new OdfDocumentReader();
        IOdfDocumentWriter writer = new OdfDocumentWriter();
        byte[] flatSource = Encoding.UTF8.GetBytes(
            FlatXml("flat", "1.4", "<ext:retained/>")
        );
        OdfLoadOptions shallowOptions = new OdfLoadOptions
        {
            MaterializeXmlTrees = false
        };
        OdfDocument flat = reader.ReadDocumentBytes(
            flatSource,
            "shallow.fodt",
            shallowOptions);
        byte[] flatWritten = writer.WriteDocumentBytes(
            flat,
            new OdfSaveOptions());

        byte[] packageSource = CreateDocumentPackage();
        OdfDocument package = reader.ReadDocumentBytes(
            packageSource,
            "shallow.odt",
            shallowOptions);
        byte[] packageWritten = writer.WriteDocumentBytes(
            package,
            new OdfSaveOptions());
        OdfDocument packageReread = reader.ReadDocumentBytes(
            packageWritten,
            "shallow-written.odt",
            new OdfLoadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(flat.Nodes, Is.Empty);
            Assert.That(flat.Parts, Is.Empty);
            Assert.That(flatWritten, Is.EqualTo(flatSource));
            Assert.That(package.Nodes, Is.Empty);
            Assert.That(package.Parts, Is.Empty);
            Assert.That(packageReread.Parts, Has.Length.EqualTo(4));
            Assert.That(TextNode(packageReread.Parts[0]).Value, Is.EqualTo("before"));
        });
    }

    [Test]
    public void LoadAndSaveUnknownContentOptionsAreAppliedWithoutDroppingFutureContent()
    {
        IOdfDocumentReader reader = new OdfDocumentReader();
        IOdfDocumentWriter writer = new OdfDocumentWriter();
        OdfLoadOptions discardKnownOptions = new OdfLoadOptions
        {
            PreserveUnknownContent = false
        };
        OdfDocument known = reader.ReadDocumentBytes(
            Encoding.UTF8.GetBytes(FlatXml("known", "1.4", "<ext:drop/>")),
            "known.fodt",
            discardKnownOptions);
        OdfDocument future = reader.ReadDocumentBytes(
            Encoding.UTF8.GetBytes(FlatXml("future", "2.0", "<ext:keep/>")),
            "future.fodt",
            discardKnownOptions);

        byte[] futureWritten = writer.WriteDocumentBytes(
            future,
            new OdfSaveOptions
            {
                PreserveUnknownContent = false
            });

        Assert.Multiple(() =>
        {
            Assert.That(known.PassthroughNodes, Is.Empty);
            Assert.That(known.Parts[0].Root?.Children.Any(
                node => node.Name?.LocalName == "drop"), Is.False);
            Assert.That(future.IsFutureVersion, Is.True);
            Assert.That(future.PassthroughNodes, Has.Length.EqualTo(1));
            Assert.That(Encoding.UTF8.GetString(futureWritten), Does.Contain("keep"));
            Assert.That(Encoding.UTF8.GetString(futureWritten), Does.Contain("2.0"));
        });
    }

    [Test]
    public void ReadersAndWritersRejectCancelledOperations()
    {
        IOdfXmlReader xmlReader = new OdfXmlReader();
        IOdfDocumentReader documentReader = new OdfDocumentReader();
        OdfLoadOptions cancelledLoad = new OdfLoadOptions();
        cancelledLoad.Cancellation.IsCancellationRequested = true;
        OdfPart part = new OdfXmlReader().ReadXmlBytes(
            Encoding.UTF8.GetBytes(ContentXml("value", "1.4")),
            OdfPartNames.Content,
            new OdfLoadOptions());
        OdfSaveOptions cancelledSave = new OdfSaveOptions();
        cancelledSave.Cancellation.IsCancellationRequested = true;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => xmlReader.ReadXmlBytes(
                    Encoding.UTF8.GetBytes(ContentXml("value", "1.4")),
                    OdfPartNames.Content,
                    cancelledLoad),
                Throws.TypeOf<OdfXmlException>());
            Assert.That(
                () => documentReader.ReadDocumentBytes(
                    Encoding.UTF8.GetBytes(FlatXml("value", "1.4", "")),
                    "cancelled.fodt",
                    cancelledLoad),
                Throws.TypeOf<OdfDocumentException>());
            Assert.That(
                () => new OdfXmlWriter().WriteXmlBytes(part, cancelledSave),
                Throws.TypeOf<OdfXmlException>());
        });
    }

    private static OdfNode TextNode(OdfPart part)
    {
        OdfNode body = part.Body ?? throw new InvalidOperationException();
        OdfNode bodyKind = body.Children.Single(
            node => node.Kind == OdfNodeKind.Element);
        OdfNode paragraph = bodyKind.Children.Single(
            node => node.Kind == OdfNodeKind.Element);
        return paragraph.Children.Single(
            node => node.Kind == OdfNodeKind.Text
                && !string.IsNullOrWhiteSpace(node.Value));
    }

    private static string ContentXml(string value, string version)
    {
        return "<office:document-content xmlns:office='"
            + OdfNamespaces.Office
            + "' xmlns:text='"
            + OdfNamespaces.Text
            + "' office:version='"
            + version
            + "'><office:body><office:text><text:p>"
            + value
            + "</text:p></office:text></office:body></office:document-content>";
    }

    private static string FlatXml(
        string value,
        string version,
        string extra)
    {
        return "<office:document xmlns:office='"
            + OdfNamespaces.Office
            + "' xmlns:text='"
            + OdfNamespaces.Text
            + "' xmlns:ext='urn:example:extension' office:version='"
            + version
            + "' office:mimetype='"
            + OdfMediaTypes.Text
            + "'>"
            + extra
            + "<office:body><office:text><text:p>"
            + value
            + "</text:p></office:text></office:body></office:document>";
    }

    private static byte[] CreateDocumentPackage()
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
                OdfMediaTypes.Text,
                CompressionLevel.NoCompression);
            WriteEntry(
                archive,
                OdfPartNames.Content,
                ContentXml("before", "1.4"),
                CompressionLevel.SmallestSize);
            WriteEntry(
                archive,
                OdfPartNames.Styles,
                PartXml("document-styles", "<office:styles/>", "1.4"),
                CompressionLevel.SmallestSize);
            WriteEntry(
                archive,
                OdfPartNames.Metadata,
                PartXml("document-meta", "<office:meta/>", "1.4"),
                CompressionLevel.SmallestSize);
            WriteEntry(
                archive,
                OdfPartNames.Settings,
                PartXml("document-settings", "<office:settings/>", "1.4"),
                CompressionLevel.SmallestSize);
            WriteEntry(
                archive,
                "Pictures/resource.bin",
                new byte[] { 1, 2, 3, 4 },
                CompressionLevel.SmallestSize);
            WriteEntry(
                archive,
                OdfPartNames.Manifest,
                ManifestXml(),
                CompressionLevel.SmallestSize);
        }
        return output.ToArray();
    }

    private static string PartXml(
        string rootName,
        string children,
        string version)
    {
        return "<office:"
            + rootName
            + " xmlns:office='"
            + OdfNamespaces.Office
            + "' office:version='"
            + version
            + "'>"
            + children
            + "</office:"
            + rootName
            + ">";
    }

    private static string ManifestXml()
    {
        return "<manifest:manifest xmlns:manifest='"
            + OdfNamespaces.Manifest
            + "' manifest:version='1.4'>"
            + ManifestEntry(OdfPartNames.Root, OdfMediaTypes.Text)
            + ManifestEntry(OdfPartNames.Content, OdfMediaTypes.Xml)
            + ManifestEntry(OdfPartNames.Styles, OdfMediaTypes.Xml)
            + ManifestEntry(OdfPartNames.Metadata, OdfMediaTypes.Xml)
            + ManifestEntry(OdfPartNames.Settings, OdfMediaTypes.Xml)
            + ManifestEntry("Pictures/resource.bin", OdfMediaTypes.OctetStream)
            + "</manifest:manifest>";
    }

    private static string ManifestEntry(string path, string mediaType)
    {
        return "<manifest:file-entry manifest:full-path='"
            + path
            + "' manifest:media-type='"
            + mediaType
            + "'/>";
    }

    private static void WriteEntry(
        ZipArchive archive,
        string path,
        string content,
        CompressionLevel compression)
    {
        WriteEntry(
            archive,
            path,
            Encoding.UTF8.GetBytes(content),
            compression);
    }

    private static void WriteEntry(
        ZipArchive archive,
        string path,
        byte[] content,
        CompressionLevel compression)
    {
        ZipArchiveEntry entry = archive.CreateEntry(path, compression);
        using Stream output = entry.Open();
        output.Write(content, 0, content.Length);
    }

    private static byte[] ReadAll(Stream input)
    {
        using MemoryStream output = new MemoryStream();
        input.CopyTo(output);
        return output.ToArray();
    }
}
