using System.IO.Compression;
using System.Text;

namespace odf.Tests;

public class StreamingEventWriterTests
{
    [Test]
    public void WritesLargeTextDocumentsIncrementallyFromEvents()
    {
        using MemoryStream output = new MemoryStream();
        XmlEventWriter writer = new XmlEventWriter(
            output,
            new OdfSaveOptions { LeaveOutputOpen = true });
        WriteDocumentPrefix(writer, "text");
        int index = 0;
        while (index < 500)
        {
            writer.WriteEvent(StartTextElement("p", 3));
            writer.WriteEvent(TextEvent("Paragraph " + index, 4));
            writer.WriteEvent(EndTextElement("p", 3));
            index = index + 1;
        }
        WriteDocumentSuffix(writer, "text");
        writer.WriteEvent(EndOfStreamEvent());

        using MemoryStream input = new MemoryStream(output.ToArray());
        string projected = StreamingTextProjector.ProjectXmlText(
            input,
            OdfPartNames.Content,
            new OdfLoadOptions { LeaveInputOpen = true });
        string[] paragraphs = projected.Split('\n');

        Assert.Multiple(() =>
        {
            Assert.That(paragraphs, Has.Length.EqualTo(500));
            Assert.That(paragraphs[0], Is.EqualTo("Paragraph 0"));
            Assert.That(paragraphs[499], Is.EqualTo("Paragraph 499"));
            Assert.That(output.CanWrite, Is.True);
        });
    }

    [Test]
    public void WritesLargeSparseSpreadsheetsWithoutExpandingRepeats()
    {
        using MemoryStream output = new MemoryStream();
        XmlEventWriter writer = new XmlEventWriter(
            output,
            new OdfSaveOptions { LeaveOutputOpen = true });
        WriteDocumentPrefix(writer, "spreadsheet");
        OdfStreamEvent table = StartTableElement("table", 3);
        table.Attributes =
        [
            Attribute(OdfNamespaces.Table, "name", "table", "Generated")
        ];
        writer.WriteEvent(table);
        int index = 0;
        while (index < 300)
        {
            OdfStreamEvent row = StartTableElement("table-row", 4);
            row.Attributes =
            [
                Attribute(
                    OdfNamespaces.Table,
                    "number-rows-repeated",
                    "table",
                    "1000")
            ];
            writer.WriteEvent(row);
            OdfStreamEvent cell = StartTableElement("table-cell", 5);
            cell.IsEmptyElement = true;
            cell.Attributes =
            [
                Attribute(
                    OdfNamespaces.Table,
                    "number-columns-repeated",
                    "table",
                    "500"),
                Attribute(
                    OdfNamespaces.Office,
                    "value-type",
                    "office",
                    "float"),
                Attribute(
                    OdfNamespaces.Office,
                    "value",
                    "office",
                    "7.5")
            ];
            writer.WriteEvent(cell);
            writer.WriteEvent(EndTableElement("table-cell", 5));
            writer.WriteEvent(EndTableElement("table-row", 4));
            index = index + 1;
        }
        writer.WriteEvent(EndTableElement("table", 3));
        WriteDocumentSuffix(writer, "spreadsheet");
        writer.CompleteXml();

        using MemoryStream input = new MemoryStream(output.ToArray());
        OdfTable tableResult =
            StreamingSpreadsheetProjector.ProjectXmlTables(
                input,
                OdfPartNames.Content,
                new OdfLoadOptions { LeaveInputOpen = true }).Single();

        Assert.Multiple(() =>
        {
            Assert.That(tableResult.Rows, Has.Length.EqualTo(300));
            Assert.That(tableResult.RowCount, Is.EqualTo(300000));
            Assert.That(tableResult.ColumnCount, Is.EqualTo(500));
            Assert.That(tableResult.Rows[299].StartIndex,
                Is.EqualTo(299000));
            Assert.That(tableResult.Rows[0].Cells, Has.Length.EqualTo(1));
            Assert.That(tableResult.Rows[0].Cells[0].RepeatCount,
                Is.EqualTo(500));
            Assert.That(tableResult.Rows[0].Cells[0].Value.NumericValue,
                Is.EqualTo(7.5));
        });
    }

    [Test]
    public void StreamsPackageEntriesAndFinalizesTheManifestLast()
    {
        ForwardOnlyPackageOutput output = new ForwardOnlyPackageOutput();
        OdfSaveOptions packageOptions = new OdfSaveOptions();
        packageOptions.LeaveOutputOpen = true;
        packageOptions.Deterministic = true;
        PackageEventWriter packageWriter = new PackageEventWriter(
            output,
            OdfMediaTypes.Text,
            "1.4",
            packageOptions);

        Stream contentOutput = packageWriter.BeginPackageEntry(
            OdfPartNames.Content,
            OdfMediaTypes.Xml,
            OdfCompressionMethod.Deflated);
        OdfSaveOptions xmlOptions = new OdfSaveOptions();
        xmlOptions.LeaveOutputOpen = true;
        XmlEventWriter xmlWriter = new XmlEventWriter(
            contentOutput,
            xmlOptions);
        WriteDocumentPrefix(xmlWriter, "text");
        xmlWriter.WriteEvent(StartTextElement("p", 3));
        xmlWriter.WriteEvent(TextEvent("Streamed package", 4));
        xmlWriter.WriteEvent(EndTextElement("p", 3));
        WriteDocumentSuffix(xmlWriter, "text");
        xmlWriter.CompleteXml();
        packageWriter.EndPackageEntry();

        packageWriter.BeginPackageEntry(
            "Pictures/image.bin",
            OdfMediaTypes.OctetStream,
            OdfCompressionMethod.Stored);
        byte[] firstChunk = [1, 2, 3];
        byte[] secondChunk = [4, 5];
        packageWriter.WritePackageContent(
            firstChunk,
            0,
            firstChunk.Length);
        packageWriter.WritePackageContent(
            secondChunk,
            0,
            secondChunk.Length);
        packageWriter.EndPackageEntry();
        packageWriter.CompletePackage();

        byte[] package = output.GetBytes();
        ZipPackageInspection inspection = ZipPackageReader.InspectBytes(package);
        using MemoryStream packageInput = new MemoryStream(package);
        using ZipArchive archive = new ZipArchive(
            packageInput,
            ZipArchiveMode.Read,
            true);
        string[] paths = archive.Entries.Select(item => item.FullName).ToArray();
        OdfManifest manifest = ManifestParser.ParseBytes(
            ReadArchiveEntry(archive, OdfPartNames.Manifest));

        Assert.Multiple(() =>
        {
            Assert.That(paths, Is.EqualTo(new[]
            {
                OdfPartNames.Mimetype,
                OdfPartNames.Content,
                "Pictures/image.bin",
                OdfPartNames.Manifest
            }));
            Assert.That(inspection.IsValid, Is.True);
            Assert.That(inspection.Entries[0].CompressionMethod,
                Is.EqualTo(OdfCompressionMethod.Stored));
            Assert.That(ReadArchiveEntry(
                archive,
                "Pictures/image.bin"), Is.EqualTo(new byte[] { 1, 2, 3, 4, 5 }));
            Assert.That(manifest.IsValid, Is.True);
            Assert.That(ManifestMediaType(manifest, OdfPartNames.Root),
                Is.EqualTo(OdfMediaTypes.Text));
            Assert.That(ManifestMediaType(manifest, OdfPartNames.Content),
                Is.EqualTo(OdfMediaTypes.Xml));
            Assert.That(ManifestMediaType(manifest, "Pictures/image.bin"),
                Is.EqualTo(OdfMediaTypes.OctetStream));
            Assert.That(output.WasDisposed, Is.False);
            Assert.That(output.CanWrite, Is.True);
        });
        output.Dispose();
    }

    [Test]
    public void RejectsMismatchedXmlEventsAndDuplicatePackagePaths()
    {
        using MemoryStream xmlOutput = new MemoryStream();
        XmlEventWriter xmlWriter = new XmlEventWriter(
            xmlOutput,
            new OdfSaveOptions { LeaveOutputOpen = true });
        xmlWriter.WriteEvent(StartTextElement("p", 0));

        using MemoryStream packageOutput = new MemoryStream();
        PackageEventWriter packageWriter = new PackageEventWriter(
            packageOutput,
            OdfMediaTypes.Text,
            "1.4",
            new OdfSaveOptions { LeaveOutputOpen = true });
        packageWriter.BeginPackageEntry(
            OdfPartNames.Content,
            OdfMediaTypes.Xml,
            OdfCompressionMethod.Deflated);
        packageWriter.EndPackageEntry();

        Assert.Multiple(() =>
        {
            Assert.That(() => xmlWriter.WriteEvent(
                    EndTextElement("h", 0)),
                Throws.TypeOf<OdfXmlException>());
            Assert.That(() => packageWriter.BeginPackageEntry(
                    OdfPartNames.Content,
                    OdfMediaTypes.Xml,
                    OdfCompressionMethod.Deflated),
                Throws.TypeOf<OdfPackageException>());
        });
        packageWriter.CompletePackage();
    }

    static void WriteDocumentPrefix(
        XmlEventWriter writer,
        string bodyName)
    {
        OdfStreamEvent root = StartOfficeElement("document-content", 0);
        root.NamespaceDeclarations =
        [
            Namespace("office", OdfNamespaces.Office),
            Namespace("text", OdfNamespaces.Text),
            Namespace("table", OdfNamespaces.Table)
        ];
        writer.WriteEvent(root);
        writer.WriteEvent(StartOfficeElement("body", 1));
        writer.WriteEvent(StartOfficeElement(bodyName, 2));
    }

    static void WriteDocumentSuffix(
        XmlEventWriter writer,
        string bodyName)
    {
        writer.WriteEvent(EndOfficeElement(bodyName, 2));
        writer.WriteEvent(EndOfficeElement("body", 1));
        writer.WriteEvent(EndOfficeElement("document-content", 0));
    }

    static OdfStreamEvent StartOfficeElement(string localName, int depth)
    {
        return StartElement(
            OdfNamespaces.Office,
            localName,
            "office",
            depth);
    }

    static OdfStreamEvent EndOfficeElement(string localName, int depth)
    {
        return EndElement(
            OdfNamespaces.Office,
            localName,
            "office",
            depth);
    }

    static OdfStreamEvent StartTextElement(string localName, int depth)
    {
        return StartElement(
            OdfNamespaces.Text,
            localName,
            "text",
            depth);
    }

    static OdfStreamEvent EndTextElement(string localName, int depth)
    {
        return EndElement(
            OdfNamespaces.Text,
            localName,
            "text",
            depth);
    }

    static OdfStreamEvent StartTableElement(string localName, int depth)
    {
        return StartElement(
            OdfNamespaces.Table,
            localName,
            "table",
            depth);
    }

    static OdfStreamEvent EndTableElement(string localName, int depth)
    {
        return EndElement(
            OdfNamespaces.Table,
            localName,
            "table",
            depth);
    }

    static OdfStreamEvent StartElement(
        string namespaceName,
        string localName,
        string prefix,
        int depth)
    {
        OdfStreamEvent result = new OdfStreamEvent();
        result.Kind = OdfStreamEventKind.ElementStart;
        result.Name = Name(namespaceName, localName, prefix);
        result.Depth = depth;
        return result;
    }

    static OdfStreamEvent EndElement(
        string namespaceName,
        string localName,
        string prefix,
        int depth)
    {
        OdfStreamEvent result = new OdfStreamEvent();
        result.Kind = OdfStreamEventKind.ElementEnd;
        result.Name = Name(namespaceName, localName, prefix);
        result.Depth = depth;
        return result;
    }

    static OdfStreamEvent TextEvent(string value, int depth)
    {
        OdfStreamEvent result = new OdfStreamEvent();
        result.Kind = OdfStreamEventKind.Text;
        result.Value = value;
        result.Depth = depth;
        return result;
    }

    static OdfStreamEvent EndOfStreamEvent()
    {
        OdfStreamEvent result = new OdfStreamEvent();
        result.Kind = OdfStreamEventKind.EndOfStream;
        return result;
    }

    static OdfQualifiedName Name(
        string namespaceName,
        string localName,
        string prefix)
    {
        return new OdfQualifiedName
        {
            NamespaceName = namespaceName,
            LocalName = localName,
            Prefix = prefix
        };
    }

    static OdfNamespaceDeclaration Namespace(
        string prefix,
        string namespaceName)
    {
        return new OdfNamespaceDeclaration
        {
            Prefix = prefix,
            NamespaceName = namespaceName
        };
    }

    static OdfAttribute Attribute(
        string namespaceName,
        string localName,
        string prefix,
        string value)
    {
        return new OdfAttribute
        {
            Name = Name(namespaceName, localName, prefix),
            Value = value
        };
    }

    static byte[] ReadArchiveEntry(ZipArchive archive, string path)
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

    class ForwardOnlyPackageOutput : Stream
    {
        MemoryStream Destination = new MemoryStream();

        internal bool WasDisposed;

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => Destination.CanWrite;
        public override long Length => Destination.Length;
        public override long Position
        {
            get => Destination.Position;
            set => throw new NotSupportedException();
        }

        internal byte[] GetBytes()
        {
            return Destination.ToArray();
        }

        public override void Flush()
        {
            Destination.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Destination.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            if (disposing)
            {
                Destination.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
