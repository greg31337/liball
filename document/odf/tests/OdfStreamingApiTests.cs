using System.IO.Compression;
using System.Text;

namespace odf.Tests;

public class OdfStreamingApiTests
{
    [Test]
    public void ReadsEveryXmlEventFromANonSeekableStream()
    {
        string xml = "<?setup ready?><root xmlns:t='urn:test' id='root'>"
            + "<t:item xml:space='preserve'>alpha <![CDATA[beta]]>"
            + "<!--note--><?inside value?></t:item>"
            + "<t:space xml:space='preserve'>  </t:space><empty/></root>";
        using NonSeekableInput input = new NonSeekableInput(
            Encoding.UTF8.GetBytes(xml));
        IOdfStreamingReader reader = new OdfStreamingReader();
        reader.OpenXmlStream(
            input,
            OdfPartNames.Content,
            new OdfLoadOptions { LeaveInputOpen = true });

        List<OdfStreamEvent> events = ReadAll(reader);
        OdfStreamEvent root = events.Single(item =>
            item.Kind == OdfStreamEventKind.ElementStart
            && item.Name?.LocalName == "root");
        OdfStreamEvent selected = events.Single(item =>
            item.Kind == OdfStreamEventKind.ElementStart
            && item.Name?.LocalName == "item");

        Assert.Multiple(() =>
        {
            Assert.That(events.Select(item => item.Kind).Distinct(),
                Is.EqualTo(new[]
                {
                    OdfStreamEventKind.ProcessingInstruction,
                    OdfStreamEventKind.ElementStart,
                    OdfStreamEventKind.Text,
                    OdfStreamEventKind.Comment,
                    OdfStreamEventKind.ElementEnd,
                    OdfStreamEventKind.EndOfStream
                }));
            Assert.That(events[0].Name?.LocalName, Is.EqualTo("setup"));
            Assert.That(events[0].Value, Is.EqualTo("ready"));
            Assert.That(root.Attributes.Single().Value, Is.EqualTo("root"));
            Assert.That(selected.Name?.NamespaceName, Is.EqualTo("urn:test"));
            Assert.That(selected.NamespaceScope.Single(item =>
                    item.Prefix == "t").NamespaceName,
                Is.EqualTo("urn:test"));
            Assert.That(events.Single(item =>
                    item.Kind == OdfStreamEventKind.Comment).Value,
                Is.EqualTo("note"));
            Assert.That(events.Single(item =>
                    item.Kind == OdfStreamEventKind.ProcessingInstruction
                    && item.Name?.LocalName == "inside").Value,
                Is.EqualTo("value"));
            Assert.That(events.Single(item =>
                    item.Kind == OdfStreamEventKind.ElementStart
                    && item.Name?.LocalName == "empty").IsEmptyElement,
                Is.True);
            Assert.That(events.Where(item =>
                    item.Kind == OdfStreamEventKind.Text)
                    .Any(item => item.IsSignificantWhitespace),
                Is.True);
            Assert.That(input.CanRead, Is.True);
        });
    }

    [Test]
    public void ProjectsOnlySelectedXmlSubtreesWithoutMaterializingADocument()
    {
        string xml = "<root xmlns:text='" + OdfNamespaces.Text + "'>"
            + "<text:p>First <text:span>selected</text:span></text:p>"
            + "<text:h>Ignored heading</text:h>"
            + "<text:p>Second</text:p></root>";
        using NonSeekableInput input = new NonSeekableInput(
            Encoding.UTF8.GetBytes(xml));
        IOdfStreamingReader reader = new OdfStreamingReader();
        reader.OpenXmlStream(
            input,
            OdfPartNames.Content,
            new OdfLoadOptions
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
            });

        List<OdfStreamEvent> events = ReadAll(reader);
        string projection = string.Concat(events
            .Where(item => item.Kind == OdfStreamEventKind.Text)
            .Select(item => item.Value));

        Assert.Multiple(() =>
        {
            Assert.That(events.Count(item =>
                    item.Kind == OdfStreamEventKind.ElementStart
                    && item.Name?.LocalName == "p"),
                Is.EqualTo(2));
            Assert.That(events.Any(item => item.Name?.LocalName == "root"),
                Is.False);
            Assert.That(events.Any(item => item.Name?.LocalName == "h"),
                Is.False);
            Assert.That(events.Any(item => item.Name?.LocalName == "span"),
                Is.True);
            Assert.That(projection, Is.EqualTo("First selectedSecond"));
            Assert.That(input.CanSeek, Is.False);
        });
    }

    [Test]
    public void HonorsCancellationThroughBothPublicReaderModes()
    {
        OdfCancellationState xmlCancellation = new OdfCancellationState();
        MemoryStream xmlInput = new MemoryStream(Encoding.UTF8.GetBytes(
            "<root><child/></root>"));
        IOdfStreamingReader xmlReader = new OdfStreamingReader();
        xmlReader.OpenXmlStream(
            xmlInput,
            "flat.xml",
            new OdfLoadOptions
            {
                LeaveInputOpen = true,
                Cancellation = xmlCancellation
            });
        xmlReader.ReadNextEvent();
        xmlCancellation.Reason = "cancel XML contract";
        xmlCancellation.IsCancellationRequested = true;

        OdfCancellationState packageCancellation =
            new OdfCancellationState();
        MemoryStream packageInput = new MemoryStream(CreateSourcePackage());
        IOdfStreamingReader packageReader = new OdfStreamingReader();
        packageReader.OpenPackageStream(
            packageInput,
            new OdfLoadOptions
            {
                LeaveInputOpen = true,
                Cancellation = packageCancellation
            });
        packageCancellation.Reason = "cancel package contract";
        packageCancellation.IsCancellationRequested = true;

        Assert.Multiple(() =>
        {
            Assert.That(() => xmlReader.ReadNextEvent(),
                Throws.TypeOf<OdfDocumentException>()
                    .With.Message.Contains("cancel XML contract"));
            Assert.That(() => packageReader.ReadNextEvent(),
                Throws.TypeOf<OdfPackageException>()
                    .With.Message.Contains("cancel package contract"));
            Assert.That(xmlInput.CanRead, Is.True);
            Assert.That(packageInput.CanRead, Is.True);
        });
        xmlInput.Dispose();
        packageInput.Dispose();
    }

    [Test]
    public void ReadsPackageEntriesAndSupportsReadAndSkipActions()
    {
        MemoryStream input = new MemoryStream(CreateSourcePackage());
        IOdfStreamingReader reader = new OdfStreamingReader();
        reader.OpenPackageStream(
            input,
            new OdfLoadOptions
            {
                LeaveInputOpen = true,
                StreamSelection = new OdfStreamSelection
                {
                    PartPaths =
                    [
                        OdfPartNames.Content,
                        "Pictures/image.bin"
                    ],
                    IncludeBinaryContent = true
                }
            });

        OdfStreamEvent contentStart = reader.ReadNextEvent();
        byte[] content = reader.ReadCurrentPackageEntryBytes();
        content[0] = (byte)'X';
        byte[] independentContent =
            reader.ReadCurrentPackageEntryBytes();
        OdfStreamEvent contentEnd = reader.ReadNextEvent();
        OdfStreamEvent imageStart = reader.ReadNextEvent();
        reader.SkipCurrentPackageEntry();
        OdfStreamEvent imageEnd = reader.ReadNextEvent();
        OdfStreamEvent finished = reader.ReadNextEvent();

        Assert.Multiple(() =>
        {
            Assert.That(contentStart.Kind,
                Is.EqualTo(OdfStreamEventKind.PackageEntryStart));
            Assert.That(contentStart.PartPath,
                Is.EqualTo(OdfPartNames.Content));
            Assert.That(Encoding.UTF8.GetString(independentContent),
                Is.EqualTo("<root>source</root>"));
            Assert.That(contentEnd.BinaryContent,
                Is.EqualTo(independentContent));
            Assert.That(imageStart.PartPath,
                Is.EqualTo("Pictures/image.bin"));
            Assert.That(imageEnd.WasSkipped, Is.True);
            Assert.That(imageEnd.BinaryContent, Is.Empty);
            Assert.That(finished.Kind,
                Is.EqualTo(OdfStreamEventKind.EndOfStream));
            Assert.That(input.CanRead, Is.True);
        });
        input.Dispose();
    }

    [Test]
    public void ReadsASelectedPackageEntryFromANonSeekableStream()
    {
        using NonSeekableInput input = new NonSeekableInput(
            CreateSourcePackage());
        IOdfStreamingReader reader = new OdfStreamingReader();
        reader.OpenPackageStream(
            input,
            new OdfLoadOptions
            {
                LeaveInputOpen = true,
                StreamSelection = new OdfStreamSelection
                {
                    PartPaths = [OdfPartNames.Content],
                    IncludeBinaryContent = true
                }
            });

        List<OdfStreamEvent> events = ReadAll(reader);

        Assert.Multiple(() =>
        {
            Assert.That(events.Select(item => item.Kind),
                Is.EqualTo(new[]
                {
                    OdfStreamEventKind.PackageEntryStart,
                    OdfStreamEventKind.PackageEntryEnd,
                    OdfStreamEventKind.EndOfStream
                }));
            Assert.That(events[0].PartPath,
                Is.EqualTo(OdfPartNames.Content));
            Assert.That(Encoding.UTF8.GetString(events[1].BinaryContent),
                Is.EqualTo("<root>source</root>"));
            Assert.That(events.Any(item =>
                    item.PartPath == "Pictures/image.bin"),
                Is.False);
            Assert.That(input.CanSeek, Is.False);
        });
    }

    [Test]
    public void ReadsXmlEventsAndSkipsTheCurrentSubtree()
    {
        MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(
            "<root><skip><nested>hidden</nested></skip>"
            + "<keep>visible</keep></root>"));
        IOdfStreamingReader reader = new OdfStreamingReader();
        reader.OpenXmlStream(
            input,
            OdfPartNames.Content,
            new OdfLoadOptions { LeaveInputOpen = false });

        OdfStreamEvent root = reader.ReadNextEvent();
        OdfStreamEvent skippedStart = reader.ReadNextEvent();
        reader.SkipCurrentXmlSubtree();
        OdfStreamEvent skippedEnd = reader.ReadNextEvent();
        OdfStreamEvent keptStart = reader.ReadNextEvent();
        OdfStreamEvent keptText = reader.ReadNextEvent();
        OdfStreamEvent keptEnd = reader.ReadNextEvent();
        OdfStreamEvent rootEnd = reader.ReadNextEvent();
        OdfStreamEvent finished = reader.ReadNextEvent();

        Assert.Multiple(() =>
        {
            Assert.That(root.Name?.LocalName, Is.EqualTo("root"));
            Assert.That(skippedStart.Name?.LocalName, Is.EqualTo("skip"));
            Assert.That(skippedEnd.Name?.LocalName, Is.EqualTo("skip"));
            Assert.That(skippedEnd.WasSkipped, Is.True);
            Assert.That(keptStart.Name?.LocalName, Is.EqualTo("keep"));
            Assert.That(keptText.Value, Is.EqualTo("visible"));
            Assert.That(keptEnd.Name?.LocalName, Is.EqualTo("keep"));
            Assert.That(rootEnd.Name?.LocalName, Is.EqualTo("root"));
            Assert.That(finished.Kind,
                Is.EqualTo(OdfStreamEventKind.EndOfStream));
            Assert.That(input.CanRead, Is.False);
        });
    }

    [Test]
    public void WritesXmlEventsThroughThePublicApi()
    {
        MemoryStream output = new MemoryStream();
        IOdfStreamingWriter writer = new OdfStreamingWriter();
        writer.OpenXmlStream(
            output,
            new OdfSaveOptions { LeaveOutputOpen = true });
        OdfStreamEvent root = StartElement(
            OdfNamespaces.Text,
            "p",
            "text",
            0);
        root.NamespaceDeclarations =
        [
            new OdfNamespaceDeclaration
            {
                Prefix = "text",
                NamespaceName = OdfNamespaces.Text
            }
        ];
        writer.WriteXmlEvent(root);
        writer.WriteXmlEvent(TextEvent("public streaming", 1));
        writer.WriteXmlEvent(EndElement(
            OdfNamespaces.Text,
            "p",
            "text",
            0));
        writer.CompleteStream();

        OdfNode[] nodes = OdfTreeBuilder.ReadBytes(
            output.ToArray(),
            OdfPartNames.Content,
            new OdfLoadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(nodes.Single(item =>
                item.Kind == OdfNodeKind.Element).Name?.LocalName,
                Is.EqualTo("p"));
            Assert.That(nodes.Single(item =>
                item.Kind == OdfNodeKind.Element).Children[0].Value,
                Is.EqualTo("public streaming"));
            Assert.That(output.CanWrite, Is.True);
        });
        output.Dispose();
    }

    [Test]
    public void WritesEveryXmlEventToAForwardOnlyDestination()
    {
        ForwardOnlyOutput output = new ForwardOnlyOutput();
        IOdfStreamingWriter writer = new OdfStreamingWriter();
        writer.OpenXmlStream(
            output,
            new OdfSaveOptions
            {
                LeaveOutputOpen = true,
                Deterministic = true
            });
        writer.WriteXmlEvent(ProcessingInstructionEvent(
            "setup", "ready", 0));
        OdfStreamEvent root = StartElement("urn:test", "root", "t", 0);
        root.NamespaceDeclarations =
        [
            new OdfNamespaceDeclaration
            {
                Prefix = "t",
                NamespaceName = "urn:test"
            }
        ];
        root.Attributes =
        [
            new OdfAttribute
            {
                Name = Name("", "id", ""),
                Value = "root-id"
            }
        ];
        writer.WriteXmlEvent(root);
        writer.WriteXmlEvent(TextEvent("text & value", 1));
        writer.WriteXmlEvent(CommentEvent("preserved comment", 1));
        writer.WriteXmlEvent(ProcessingInstructionEvent(
            "inside", "value", 1));
        OdfStreamEvent empty = StartElement("urn:test", "empty", "t", 1);
        empty.IsEmptyElement = true;
        writer.WriteXmlEvent(empty);
        writer.WriteXmlEvent(EndElement("urn:test", "empty", "t", 1));
        writer.WriteXmlEvent(EndElement("urn:test", "root", "t", 0));
        writer.WriteXmlEvent(new OdfStreamEvent
        {
            Kind = OdfStreamEventKind.EndOfStream
        });
        writer.CompleteStream();

        byte[] xml = output.GetBytes();
        using NonSeekableInput input = new NonSeekableInput(xml);
        IOdfStreamingReader reader = new OdfStreamingReader();
        reader.OpenXmlStream(
            input,
            "generated.xml",
            new OdfLoadOptions { LeaveInputOpen = true });
        List<OdfStreamEvent> events = ReadAll(reader);

        Assert.Multiple(() =>
        {
            Assert.That(events.First().Kind,
                Is.EqualTo(OdfStreamEventKind.ProcessingInstruction));
            Assert.That(events.Single(item =>
                    item.Kind == OdfStreamEventKind.ElementStart
                    && item.Name?.LocalName == "root")
                    .Attributes.Single().Value,
                Is.EqualTo("root-id"));
            Assert.That(events.Single(item =>
                    item.Kind == OdfStreamEventKind.Text).Value,
                Is.EqualTo("text & value"));
            Assert.That(events.Single(item =>
                    item.Kind == OdfStreamEventKind.Comment).Value,
                Is.EqualTo("preserved comment"));
            Assert.That(events.Any(item =>
                    item.Kind == OdfStreamEventKind.ProcessingInstruction
                    && item.Name?.LocalName == "inside"),
                Is.True);
            Assert.That(events.Last().Kind,
                Is.EqualTo(OdfStreamEventKind.EndOfStream));
            Assert.That(output.CanSeek, Is.False);
            Assert.That(output.CanWrite, Is.True);
        });
        output.Dispose();
    }

    [Test]
    public void WritesPackageEntriesAndFinalizesTheManifest()
    {
        MemoryStream output = new MemoryStream();
        IOdfStreamingWriter writer = new OdfStreamingWriter();
        writer.OpenPackageStream(
            output,
            OdfMediaTypes.Text,
            "1.4",
            new OdfSaveOptions
            {
                LeaveOutputOpen = true,
                Deterministic = true
            });
        Stream contentOutput = writer.BeginPackageEntry(
            OdfPartNames.Content,
            OdfMediaTypes.Xml,
            OdfCompressionMethod.Deflated);
        byte[] content = Encoding.UTF8.GetBytes("<root/>");
        contentOutput.Write(content, 0, content.Length);
        writer.EndPackageEntry();
        writer.BeginPackageEntry(
            "Pictures/image.bin",
            OdfMediaTypes.OctetStream,
            OdfCompressionMethod.Stored);
        writer.WritePackageContent([1, 2, 3], 0, 3);
        writer.WritePackageContent([4, 5], 0, 2);
        writer.EndPackageEntry();
        writer.CompleteStream();

        byte[] package = output.ToArray();
        ZipPackageInspection inspection =
            ZipPackageReader.InspectBytes(package);
        using MemoryStream packageInput = new MemoryStream(package);
        using ZipArchive archive = new ZipArchive(
            packageInput,
            ZipArchiveMode.Read,
            true);
        OdfManifest manifest = ManifestParser.ParseBytes(
            ReadEntry(archive, OdfPartNames.Manifest));

        Assert.Multiple(() =>
        {
            Assert.That(inspection.IsValid, Is.True);
            Assert.That(archive.Entries[0].FullName,
                Is.EqualTo(OdfPartNames.Mimetype));
            Assert.That(archive.Entries[^1].FullName,
                Is.EqualTo(OdfPartNames.Manifest));
            Assert.That(ReadEntry(archive, OdfPartNames.Content),
                Is.EqualTo(content));
            Assert.That(ReadEntry(archive, "Pictures/image.bin"),
                Is.EqualTo(new byte[] { 1, 2, 3, 4, 5 }));
            Assert.That(ManifestMediaType(
                manifest,
                OdfPartNames.Content), Is.EqualTo(OdfMediaTypes.Xml));
            Assert.That(ManifestMediaType(
                manifest,
                "Pictures/image.bin"),
                Is.EqualTo(OdfMediaTypes.OctetStream));
            Assert.That(output.CanWrite, Is.True);
        });
        output.Dispose();
    }

    [Test]
    public void WritesAPackageToAForwardOnlyDestination()
    {
        ForwardOnlyOutput output = new ForwardOnlyOutput();
        IOdfStreamingWriter writer = new OdfStreamingWriter();
        writer.OpenPackageStream(
            output,
            OdfMediaTypes.Text,
            "1.4",
            new OdfSaveOptions
            {
                LeaveOutputOpen = true,
                Deterministic = true
            });
        Stream content = writer.BeginPackageEntry(
            OdfPartNames.Content,
            OdfMediaTypes.Xml,
            OdfCompressionMethod.Deflated);
        byte[] contentBytes = Encoding.UTF8.GetBytes("<root>streamed</root>");
        content.Write(contentBytes, 0, contentBytes.Length);
        writer.EndPackageEntry();
        writer.BeginPackageEntry(
            "Pictures/data.bin",
            OdfMediaTypes.OctetStream,
            OdfCompressionMethod.Stored);
        byte[] binary = [0, 1, 2, 3, 4, 5];
        writer.WritePackageContent(binary, 1, 4);
        writer.EndPackageEntry();
        writer.CompleteStream();

        byte[] package = output.GetBytes();
        using MemoryStream packageInput = new MemoryStream(package);
        using ZipArchive archive = new ZipArchive(
            packageInput,
            ZipArchiveMode.Read,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(ZipPackageReader.InspectBytes(package).IsValid,
                Is.True);
            Assert.That(ReadEntry(archive, OdfPartNames.Content),
                Is.EqualTo(contentBytes));
            Assert.That(ReadEntry(archive, "Pictures/data.bin"),
                Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(archive.Entries[0].FullName,
                Is.EqualTo(OdfPartNames.Mimetype));
            Assert.That(archive.Entries[^1].FullName,
                Is.EqualTo(OdfPartNames.Manifest));
            Assert.That(output.CanSeek, Is.False);
            Assert.That(output.CanWrite, Is.True);
        });
        output.Dispose();
    }

    [Test]
    public void RequiresAnOpenModeAndRejectsReopeningInstances()
    {
        IOdfStreamingReader reader = new OdfStreamingReader();
        IOdfStreamingWriter writer = new OdfStreamingWriter();
        MemoryStream readerInput = new MemoryStream(
            Encoding.UTF8.GetBytes("<root/>"));
        MemoryStream writerOutput = new MemoryStream();

        Assert.Multiple(() =>
        {
            Assert.That(() => reader.ReadNextEvent(),
                Throws.TypeOf<OdfDocumentException>());
            Assert.That(() => reader.SkipCurrentPackageEntry(),
                Throws.TypeOf<OdfPackageException>());
            Assert.That(() => writer.CompleteStream(),
                Throws.TypeOf<OdfDocumentException>());
            Assert.That(() => writer.WriteXmlEvent(TextEvent("x", 0)),
                Throws.TypeOf<OdfXmlException>());
        });

        reader.OpenXmlStream(
            readerInput,
            "",
            new OdfLoadOptions { LeaveInputOpen = true });
        writer.OpenXmlStream(
            writerOutput,
            new OdfSaveOptions { LeaveOutputOpen = true });

        Assert.Multiple(() =>
        {
            Assert.That(() => reader.OpenPackageStream(
                    readerInput,
                    new OdfLoadOptions()),
                Throws.TypeOf<OdfDocumentException>());
            Assert.That(() => writer.OpenPackageStream(
                    writerOutput,
                    OdfMediaTypes.Text,
                    "1.4",
                    new OdfSaveOptions()),
                Throws.TypeOf<OdfDocumentException>());
            Assert.That(() => reader.ReadCurrentPackageEntryBytes(),
                Throws.TypeOf<OdfPackageException>());
            Assert.That(() => writer.BeginPackageEntry(
                    OdfPartNames.Content,
                    OdfMediaTypes.Xml,
                    OdfCompressionMethod.Deflated),
                Throws.TypeOf<OdfPackageException>());
        });

        reader.ReadNextEvent();
        reader.ReadNextEvent();
        reader.ReadNextEvent();
        writer.WriteXmlEvent(StartElement("", "root", "", 0));
        writer.WriteXmlEvent(EndElement("", "root", "", 0));
        writer.CompleteStream();
        readerInput.Dispose();
        writerOutput.Dispose();
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
                Encoding.UTF8.GetBytes(OdfMediaTypes.Text),
                CompressionLevel.NoCompression);
            WriteEntry(
                archive,
                OdfPartNames.Content,
                Encoding.UTF8.GetBytes("<root>source</root>"),
                CompressionLevel.Optimal);
            WriteEntry(
                archive,
                "Pictures/image.bin",
                [1, 2, 3, 4, 5],
                CompressionLevel.NoCompression);
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
        return manifest.Entries.Single(item =>
            item.FullPath == path).MediaType;
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

    static OdfStreamEvent CommentEvent(string value, int depth)
    {
        OdfStreamEvent result = new OdfStreamEvent();
        result.Kind = OdfStreamEventKind.Comment;
        result.Value = value;
        result.Depth = depth;
        return result;
    }

    static OdfStreamEvent ProcessingInstructionEvent(
        string target,
        string value,
        int depth)
    {
        OdfStreamEvent result = new OdfStreamEvent();
        result.Kind = OdfStreamEventKind.ProcessingInstruction;
        result.Name = Name("", target, "");
        result.Value = value;
        result.Depth = depth;
        return result;
    }

    static List<OdfStreamEvent> ReadAll(IOdfStreamingReader reader)
    {
        List<OdfStreamEvent> result = new List<OdfStreamEvent>();
        OdfStreamEvent item = reader.ReadNextEvent();
        result.Add(item);
        while (item.Kind != OdfStreamEventKind.EndOfStream)
        {
            item = reader.ReadNextEvent();
            result.Add(item);
        }
        return result;
    }

    static OdfQualifiedName Name(
        string namespaceName,
        string localName,
        string prefix)
    {
        OdfQualifiedName result = new OdfQualifiedName();
        result.NamespaceName = namespaceName;
        result.LocalName = localName;
        result.Prefix = prefix;
        return result;
    }

    class NonSeekableInput : Stream
    {
        readonly MemoryStream Source;

        internal NonSeekableInput(byte[] content)
        {
            Source = new MemoryStream(content);
        }

        public override bool CanRead => Source.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Source.Read(buffer, offset, count);
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
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Source.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    class ForwardOnlyOutput : Stream
    {
        readonly MemoryStream Destination = new MemoryStream();

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
            if (disposing)
            {
                Destination.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
