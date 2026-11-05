using System.IO.Compression;
using System.Text;

namespace odf.Tests;

public class PackageEventReaderTests
{
    [Test]
    public void SelectsEntriesAndReadsThemForwardOnly()
    {
        byte[] package = CreatePackage();
        using NonSeekableReadStream input =
            new NonSeekableReadStream(package);
        OdfLoadOptions options = new OdfLoadOptions
        {
            LeaveInputOpen = true,
            StreamSelection = new OdfStreamSelection
            {
                PartPaths = [OdfPartNames.Content],
                IncludeBinaryContent = true
            }
        };
        PackageEventReader reader = new PackageEventReader(input, options);

        Assert.That(input.BytesRead, Is.Zero);
        OdfStreamEvent start = reader.ReadNextEvent();
        long bytesAtStart = input.BytesRead;
        OdfStreamEvent end = reader.ReadNextEvent();
        OdfStreamEvent finished = reader.ReadNextEvent();

        Assert.Multiple(() =>
        {
            Assert.That(start.Kind,
                Is.EqualTo(OdfStreamEventKind.PackageEntryStart));
            Assert.That(start.PartPath, Is.EqualTo(OdfPartNames.Content));
            Assert.That(start.PackageEntry?.CompressionMethod,
                Is.EqualTo(OdfCompressionMethod.Deflated));
            Assert.That(start.Location.ByteOffset, Is.GreaterThanOrEqualTo(0));
            Assert.That(bytesAtStart, Is.LessThan(package.LongLength));
            Assert.That(end.Kind,
                Is.EqualTo(OdfStreamEventKind.PackageEntryEnd));
            Assert.That(Encoding.UTF8.GetString(end.BinaryContent),
                Is.EqualTo("<office:document-content/>"));
            Assert.That(end.WasSkipped, Is.False);
            Assert.That(finished.Kind,
                Is.EqualTo(OdfStreamEventKind.EndOfStream));
            Assert.That(input.CanRead, Is.True);
        });
    }

    [Test]
    public void ExplicitlySkipsTheCurrentEntryWithoutInflatingIt()
    {
        byte[] package = CreatePackage();
        using MemoryStream input = new MemoryStream(package);
        PackageEventReader reader = new PackageEventReader(
            input,
            new OdfLoadOptions
            {
                LeaveInputOpen = true,
                StreamSelection = new OdfStreamSelection
                {
                    PartPaths = ["Pictures/image.bin"],
                    IncludeBinaryContent = true
                }
            });

        OdfStreamEvent start = reader.ReadNextEvent();
        reader.SkipCurrentEntry();
        OdfStreamEvent end = reader.ReadNextEvent();
        OdfStreamEvent finished = reader.ReadNextEvent();

        Assert.Multiple(() =>
        {
            Assert.That(start.PartPath, Is.EqualTo("Pictures/image.bin"));
            Assert.That(end.Kind,
                Is.EqualTo(OdfStreamEventKind.PackageEntryEnd));
            Assert.That(end.WasSkipped, Is.True);
            Assert.That(end.BinaryContent, Is.Empty);
            Assert.That(finished.Kind,
                Is.EqualTo(OdfStreamEventKind.EndOfStream));
            Assert.That(() => reader.SkipCurrentEntry(),
                Throws.TypeOf<OdfPackageException>());
        });
    }

    [Test]
    public void ExposesIndependentCurrentEntryByteSnapshots()
    {
        byte[] package = CreatePackage();
        using MemoryStream input = new MemoryStream(package);
        PackageEventReader reader = new PackageEventReader(
            input,
            new OdfLoadOptions
            {
                LeaveInputOpen = true,
                StreamSelection = new OdfStreamSelection
                {
                    PartPaths = [OdfPartNames.Mimetype]
                }
            });

        reader.ReadNextEvent();
        byte[] first = reader.ReadCurrentEntryBytes();
        first[0] = (byte)'X';
        byte[] second = reader.ReadCurrentEntryBytes();
        OdfStreamEvent end = reader.ReadNextEvent();

        Assert.Multiple(() =>
        {
            Assert.That(Encoding.UTF8.GetString(second),
                Is.EqualTo(OdfMediaTypes.Text));
            Assert.That(end.BinaryContent, Is.Empty);
            Assert.That(end.PackageEntry?.HasValidCrc, Is.True);
        });
    }

    [Test]
    public void EnforcesEntryCountAndExpandedSizeLimits()
    {
        byte[] package = CreatePackage();
        using MemoryStream sizeInput = new MemoryStream(package);
        PackageEventReader sizeReader = new PackageEventReader(
            sizeInput,
            new OdfLoadOptions
            {
                LeaveInputOpen = true,
                SecurityLimits = new OdfSecurityLimits
                {
                    MaximumSingleEntryBytes = 8
                }
            });
        using MemoryStream countInput = new MemoryStream(package);
        PackageEventReader countReader = new PackageEventReader(
            countInput,
            new OdfLoadOptions
            {
                LeaveInputOpen = true,
                SecurityLimits = new OdfSecurityLimits
                {
                    MaximumPackageEntries = 1
                }
            });

        Assert.Multiple(() =>
        {
            Assert.That(() => sizeReader.ReadNextEvent(),
                Throws.TypeOf<OdfPackageException>());
            Assert.That(() =>
            {
                countReader.ReadNextEvent();
                countReader.ReadNextEvent();
                countReader.ReadNextEvent();
            }, Throws.TypeOf<OdfPackageException>());
        });
    }

    [Test]
    public void HonorsInputOwnershipAndCancellation()
    {
        byte[] package = CreatePackage();
        MemoryStream owned = new MemoryStream(package);
        PackageEventReader ownedReader = new PackageEventReader(
            owned,
            new OdfLoadOptions
            {
                LeaveInputOpen = false,
                StreamSelection = new OdfStreamSelection
                {
                    IncludePackageEntryEvents = false
                }
            });
        OdfStreamEvent finished = ownedReader.ReadNextEvent();

        MemoryStream callerOwned = new MemoryStream(package);
        OdfCancellationState cancellation = new OdfCancellationState();
        PackageEventReader cancelledReader = new PackageEventReader(
            callerOwned,
            new OdfLoadOptions
            {
                LeaveInputOpen = true,
                Cancellation = cancellation
            });
        cancellation.Reason = "test stop";
        cancellation.IsCancellationRequested = true;

        Assert.Multiple(() =>
        {
            Assert.That(finished.Kind,
                Is.EqualTo(OdfStreamEventKind.EndOfStream));
            Assert.That(owned.CanRead, Is.False);
            Assert.That(() => cancelledReader.ReadNextEvent(),
                Throws.TypeOf<OdfPackageException>()
                    .With.Message.Contains("test stop"));
            Assert.That(callerOwned.CanRead, Is.True);
        });
        callerOwned.Dispose();
    }

    [Test]
    public void ReadsDataDescriptorEntriesFromANonSeekablePackage()
    {
        byte[] package = CreateDescriptorPackage();
        using NonSeekableReadStream input =
            new NonSeekableReadStream(package);
        PackageEventReader reader = new PackageEventReader(
            input,
            new OdfLoadOptions
            {
                LeaveInputOpen = true,
                StreamSelection = new OdfStreamSelection
                {
                    IncludeBinaryContent = true
                }
            });

        OdfStreamEvent start = reader.ReadNextEvent();
        OdfStreamEvent end = reader.ReadNextEvent();
        OdfStreamEvent finished = reader.ReadNextEvent();

        Assert.Multiple(() =>
        {
            Assert.That(start.PackageEntry?.UsesDataDescriptor, Is.True);
            Assert.That(start.PartPath, Is.EqualTo("streamed.bin"));
            Assert.That(Encoding.UTF8.GetString(end.BinaryContent),
                Is.EqualTo("descriptor payload"));
            Assert.That(end.PackageEntry?.ExpandedSize,
                Is.EqualTo("descriptor payload".Length));
            Assert.That(end.PackageEntry?.HasValidCrc, Is.True);
            Assert.That(finished.Kind,
                Is.EqualTo(OdfStreamEventKind.EndOfStream));
        });
    }

    static byte[] CreatePackage()
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
                "<office:document-content/>",
                CompressionLevel.Optimal);
            WriteEntry(
                archive,
                "Pictures/image.bin",
                "binary image payload",
                CompressionLevel.Optimal);
        }
        return output.ToArray();
    }

    static byte[] CreateDescriptorPackage()
    {
        using ForwardWriteStream output = new ForwardWriteStream();
        using (ZipArchive archive = new ZipArchive(
            output,
            ZipArchiveMode.Create,
            true))
        {
            WriteEntry(
                archive,
                "streamed.bin",
                "descriptor payload",
                CompressionLevel.Optimal);
        }
        return output.GetBytes();
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

    class NonSeekableReadStream : Stream
    {
        readonly MemoryStream Source;

        internal NonSeekableReadStream(byte[] data)
        {
            Source = new MemoryStream(data);
        }

        internal long BytesRead;

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
            int read = Source.Read(buffer, offset, count);
            BytesRead += read;
            return read;
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

    class ForwardWriteStream : Stream
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
