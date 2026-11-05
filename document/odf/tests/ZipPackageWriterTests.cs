using System.IO.Compression;
using System.Text;

namespace odf.Tests;

public class ZipPackageWriterTests
{
    [Test]
    public void WritesMimetypeFirstStoredAndGeneratesValidEntryCrcs()
    {
        ZipEntryIndex index = ZipEntryIndex.CreateBytes(CreateSourcePackage(
            [OdfPartNames.Content],
            [Encoding.UTF8.GetBytes("content")]));
        index.AddEntry(
            "stored.bin",
            [1, 2, 3, 4],
            OdfCompressionMethod.Stored);
        index.AddEntry(
            "deflated.txt",
            CreateVariedBytes(4096),
            OdfCompressionMethod.Deflated);
        index.AddEntry("Empty/", [], OdfCompressionMethod.Stored);

        byte[] output = ZipPackageWriter.WriteBytes(index, new OdfSaveOptions());
        ZipPackageInspection inspection = ZipPackageReader.InspectBytes(output);
        ZipEntryIndex written = ZipEntryIndex.CreateBytes(output);

        Assert.Multiple(() =>
        {
            Assert.That(inspection.IsValid, Is.True);
            Assert.That(inspection.Entries[0].Path, Is.EqualTo(OdfPartNames.Mimetype));
            Assert.That(inspection.Entries[0].IsFirstEntry, Is.True);
            Assert.That(inspection.Entries[0].CompressionMethod, Is.EqualTo(OdfCompressionMethod.Stored));
            Assert.That(inspection.Entries[0].LocalExtraFieldLength, Is.Zero);
            Assert.That(FindEntry(inspection, "stored.bin").CompressionMethod, Is.EqualTo(OdfCompressionMethod.Stored));
            Assert.That(FindEntry(inspection, "deflated.txt").CompressionMethod, Is.EqualTo(OdfCompressionMethod.Deflated));
            Assert.That(FindEntry(inspection, "Empty/").CompressionMethod, Is.EqualTo(OdfCompressionMethod.Stored));
            Assert.That(AllCrcsAreValid(inspection), Is.True);
            Assert.That(ReadEntryBytes(written, "stored.bin"), Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(ReadEntryBytes(written, "deflated.txt"), Is.EqualTo(CreateVariedBytes(4096)));
        });
    }

    [Test]
    public void DeterministicModeSortsEntriesFixesTimestampsAndIsByteStable()
    {
        ZipEntryIndex firstIndex = ZipEntryIndex.CreateBytes(CreateSourcePackage(
            ["z.txt", "a.txt"],
            [Encoding.UTF8.GetBytes("z"), Encoding.UTF8.GetBytes("a")]));
        ZipEntryIndex secondIndex = ZipEntryIndex.CreateBytes(CreateSourcePackage(
            ["a.txt", "z.txt"],
            [Encoding.UTF8.GetBytes("a"), Encoding.UTF8.GetBytes("z")]));
        OdfSaveOptions options = new OdfSaveOptions();
        options.Deterministic = true;

        byte[] first = ZipPackageWriter.WriteBytes(firstIndex, options);
        byte[] repeated = ZipPackageWriter.WriteBytes(firstIndex, options);
        byte[] reordered = ZipPackageWriter.WriteBytes(secondIndex, options);
        ZipPackageInspection inspection = ZipPackageReader.InspectBytes(first);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(repeated));
            Assert.That(first, Is.EqualTo(reordered));
            Assert.That(EntryPaths(inspection), Is.EqualTo(new[]
            {
                OdfPartNames.Mimetype,
                OdfPartNames.Manifest,
                "a.txt",
                "z.txt"
            }));
            Assert.That(AllTimestampsEqual(first, new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero)), Is.True);
        });
    }

    [Test]
    public void OrdinaryModePreservesSourceOrderingAfterRequiredMimetypePlacement()
    {
        ZipEntryIndex index = ZipEntryIndex.CreateBytes(CreateSourcePackage(
            ["z.txt", "a.txt"],
            [Encoding.UTF8.GetBytes("z"), Encoding.UTF8.GetBytes("a")]));

        byte[] output = ZipPackageWriter.WriteBytes(index, new OdfSaveOptions());
        ZipPackageInspection inspection = ZipPackageReader.InspectBytes(output);

        Assert.That(EntryPaths(inspection), Is.EqualTo(new[]
        {
            OdfPartNames.Mimetype,
            OdfPartNames.Manifest,
            "z.txt",
            "a.txt"
        }));
    }

    [Test]
    public void NonSeekableOutputReceivesACompleteConformingPackage()
    {
        ZipEntryIndex index = ZipEntryIndex.CreateBytes(CreateSourcePackage(
            [OdfPartNames.Content],
            [Encoding.UTF8.GetBytes("content")]));
        NonSeekableOutputStream output = new NonSeekableOutputStream();
        OdfSaveOptions options = new OdfSaveOptions();
        options.LeaveOutputOpen = true;

        ZipPackageWriter.WriteStream(index, output, options);
        ZipPackageInspection inspection = ZipPackageReader.InspectBytes(output.GetBytes());

        Assert.Multiple(() =>
        {
            Assert.That(output.WasDisposed, Is.False);
            Assert.That(output.CanWrite, Is.True);
            Assert.That(inspection.IsValid, Is.True);
            Assert.That(inspection.Entries[0].Path, Is.EqualTo(OdfPartNames.Mimetype));
            Assert.That(inspection.Entries[0].CompressionMethod, Is.EqualTo(OdfCompressionMethod.Stored));
            Assert.That(AllCrcsAreValid(inspection), Is.True);
        });
    }

    [Test]
    public void OrdinarySaveCopiesUntouchedRecordsAndRewritesChangedEntries()
    {
        byte[] source = CreateSourcePackage(
            ["changed.txt", "untouched.bin"],
            [Encoding.UTF8.GetBytes("before"), CreateVariedBytes(8192)]);
        byte[] originalUntouched = ReadLocalRecord(source, "untouched.bin");
        ZipEntryIndex index = ZipEntryIndex.CreateBytes(source);

        index.ReplaceEntry("changed.txt", Encoding.UTF8.GetBytes("after"));
        byte[] output = ZipPackageWriter.WriteBytes(index, new OdfSaveOptions());
        byte[] writtenUntouched = ReadLocalRecord(output, "untouched.bin");
        ZipPackageInspection inspection = ZipPackageReader.InspectBytes(output);
        ZipEntryIndex written = ZipEntryIndex.CreateBytes(output);

        Assert.Multiple(() =>
        {
            Assert.That(writtenUntouched, Is.EqualTo(originalUntouched));
            Assert.That(ReadEntryBytes(written, "changed.txt"), Is.EqualTo(Encoding.UTF8.GetBytes("after")));
            Assert.That(ReadEntryBytes(written, "untouched.bin"), Is.EqualTo(CreateVariedBytes(8192)));
            Assert.That(AllCrcsAreValid(inspection), Is.True);
        });
    }

    [Test]
    public void OrdinarySaveRetainsUntouchedUnsupportedCompressionRecords()
    {
        byte[] source = ChangeCompressionMethod(
            CreateSourcePackage(
                ["opaque.bin"],
                [CreateVariedBytes(1024)]),
            "opaque.bin",
            99);
        byte[] originalRecord = ReadLocalRecord(source, "opaque.bin");
        ZipEntryIndex index = ZipEntryIndex.CreateBytes(source);

        index.AddEntry(
            "new.txt",
            Encoding.UTF8.GetBytes("new"),
            OdfCompressionMethod.Deflated);
        byte[] output = ZipPackageWriter.WriteBytes(index, new OdfSaveOptions());
        ZipPackageInspection inspection = ZipPackageReader.InspectBytes(output);

        Assert.Multiple(() =>
        {
            Assert.That(ReadLocalRecord(output, "opaque.bin"), Is.EqualTo(originalRecord));
            Assert.That(FindEntry(inspection, "opaque.bin").CompressionMethodCode, Is.EqualTo(99));
            Assert.That(FindEntry(inspection, "opaque.bin").CompressionMethod, Is.EqualTo(OdfCompressionMethod.Unsupported));
            Assert.That(HasEntry(inspection, "new.txt"), Is.True);
        });
    }

    [Test]
    public void UnchangedOrdinarySaveIsByteForByteStable()
    {
        byte[] source = CreateSourcePackage(
            ["one.bin", "two.txt"],
            [CreateVariedBytes(2048), Encoding.UTF8.GetBytes("two")]);
        ZipEntryIndex index = ZipEntryIndex.CreateBytes(source);

        byte[] output = ZipPackageWriter.WriteBytes(index, new OdfSaveOptions());

        Assert.That(output, Is.EqualTo(source));
    }

    [Test]
    public void OutputOwnershipIsHonoredForSeekableAndNonSeekableStreams()
    {
        ZipEntryIndex index = ZipEntryIndex.CreateBytes(CreateSourcePackage(
            [OdfPartNames.Content],
            [Encoding.UTF8.GetBytes("content")]));
        OdfSaveOptions options = new OdfSaveOptions();
        options.LeaveOutputOpen = false;
        TrackingOutputStream seekable = new TrackingOutputStream();
        NonSeekableOutputStream nonSeekable = new NonSeekableOutputStream();

        ZipPackageWriter.WriteStream(index, seekable, options);
        ZipPackageWriter.WriteStream(index, nonSeekable, options);

        Assert.Multiple(() =>
        {
            Assert.That(seekable.WasDisposed, Is.True);
            Assert.That(nonSeekable.WasDisposed, Is.True);
        });
    }

    [Test]
    public void SynthesizesMissingMimetypeAndManifestInfrastructureWhenModelsRemain()
    {
        ZipEntryIndex missingMimetype = ZipEntryIndex.CreateBytes(
            CreateSourcePackageWithoutMimetype());
        ZipEntryIndex missingManifest = ZipEntryIndex.CreateBytes(CreateSourcePackage(
            [OdfPartNames.Content],
            [Encoding.UTF8.GetBytes("content")]));
        missingManifest.RemoveEntry(OdfPartNames.Manifest);

        byte[] withMimetype = ZipPackageWriter.WriteBytes(
            missingMimetype,
            new OdfSaveOptions());
        byte[] withManifest = ZipPackageWriter.WriteBytes(
            missingManifest,
            new OdfSaveOptions());
        ZipPackageInspection firstInspection = ZipPackageReader.InspectBytes(withMimetype);
        ZipPackageInspection secondInspection = ZipPackageReader.InspectBytes(withManifest);

        Assert.Multiple(() =>
        {
            Assert.That(firstInspection.Entries[0].Path, Is.EqualTo(OdfPartNames.Mimetype));
            Assert.That(firstInspection.MediaType, Is.EqualTo(OdfMediaTypes.Text));
            Assert.That(firstInspection.IsValid, Is.True);
            Assert.That(HasEntry(secondInspection, OdfPartNames.Manifest), Is.True);
            Assert.That(secondInspection.Manifest, Is.Not.Null);
            Assert.That(secondInspection.IsValid, Is.True);
        });
    }

    [Test]
    public void UsesSaveCompressionFallbackAndRejectsUnavailableContent()
    {
        ZipEntryIndex index = ZipEntryIndex.CreateBytes(CreateSourcePackage(
            ["fallback.bin", "unavailable.bin"],
            [new byte[] { 1 }, new byte[] { 2 }]));
        EditablePackageEntry fallback = FindEditable(index, "fallback.bin");
        fallback.Entry.CompressionMethod = OdfCompressionMethod.Unknown;
        EditablePackageEntry unavailable = FindEditable(index, "unavailable.bin");
        unavailable.IsContentAvailable = false;
        OdfSaveOptions options = new OdfSaveOptions();
        options.CompressionMethod = OdfCompressionMethod.Stored;

        Assert.Throws<OdfPackageException>(() => ZipPackageWriter.WriteBytes(index, options));

        index.RemoveEntry("unavailable.bin");
        byte[] output = ZipPackageWriter.WriteBytes(index, options);
        ZipPackageInspection inspection = ZipPackageReader.InspectBytes(output);

        Assert.That(
            FindEntry(inspection, "fallback.bin").CompressionMethod,
            Is.EqualTo(OdfCompressionMethod.Stored));
    }

    static byte[] CreateSourcePackage(string[] paths, byte[][] contents)
    {
        string manifest = ManifestStart()
            + FileEntry(OdfPartNames.Root, OdfMediaTypes.Text);
        int manifestIndex = 0;
        while (manifestIndex < paths.Length)
        {
            manifest = manifest + FileEntry(
                paths[manifestIndex],
                paths[manifestIndex].EndsWith(".xml", StringComparison.Ordinal)
                    ? OdfMediaTypes.Xml
                    : OdfMediaTypes.OctetStream);
            manifestIndex++;
        }
        manifest = manifest + "</m:manifest>";

        using MemoryStream output = new MemoryStream();
        using (ZipArchive archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            WriteEntry(
                archive,
                OdfPartNames.Mimetype,
                Encoding.UTF8.GetBytes(OdfMediaTypes.Text),
                CompressionLevel.NoCompression);
            WriteEntry(
                archive,
                OdfPartNames.Manifest,
                Encoding.UTF8.GetBytes(manifest),
                CompressionLevel.SmallestSize);
            int index = 0;
            while (index < paths.Length)
            {
                WriteEntry(
                    archive,
                    paths[index],
                    contents[index],
                    CompressionLevel.SmallestSize);
                index++;
            }
        }
        return output.ToArray();
    }

    static byte[] CreateSourcePackageWithoutMimetype()
    {
        string manifest = ManifestStart()
            + FileEntry(OdfPartNames.Root, OdfMediaTypes.Text)
            + FileEntry(OdfPartNames.Content, OdfMediaTypes.Xml)
            + "</m:manifest>";
        using MemoryStream output = new MemoryStream();
        using (ZipArchive archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            WriteEntry(
                archive,
                OdfPartNames.Manifest,
                Encoding.UTF8.GetBytes(manifest),
                CompressionLevel.SmallestSize);
            WriteEntry(
                archive,
                OdfPartNames.Content,
                Encoding.UTF8.GetBytes("content"),
                CompressionLevel.SmallestSize);
        }
        return output.ToArray();
    }

    static void WriteEntry(
        ZipArchive archive,
        string path,
        byte[] content,
        CompressionLevel compressionLevel)
    {
        ZipArchiveEntry entry = archive.CreateEntry(path, compressionLevel);
        using Stream output = entry.Open();
        output.Write(content, 0, content.Length);
    }

    static OdfPackageEntry FindEntry(
        ZipPackageInspection inspection,
        string path)
    {
        foreach (OdfPackageEntry entry in inspection.Entries)
        {
            if (entry.Path == path)
            {
                return entry;
            }
        }
        Assert.Fail($"Expected package entry '{path}'.");
        return new OdfPackageEntry();
    }

    static EditablePackageEntry FindEditable(ZipEntryIndex index, string path)
    {
        foreach (EditablePackageEntry entry in index.GetEditableEntries())
        {
            if (entry.Entry.Path == path)
            {
                return entry;
            }
        }
        Assert.Fail($"Expected editable package entry '{path}'.");
        return new EditablePackageEntry();
    }

    static bool HasEntry(ZipPackageInspection inspection, string path)
    {
        foreach (OdfPackageEntry entry in inspection.Entries)
        {
            if (entry.Path == path)
            {
                return true;
            }
        }
        return false;
    }

    static bool AllCrcsAreValid(ZipPackageInspection inspection)
    {
        foreach (OdfPackageEntry entry in inspection.Entries)
        {
            if (!entry.HasValidCrc)
            {
                return false;
            }
        }
        return true;
    }

    static string[] EntryPaths(ZipPackageInspection inspection)
    {
        string[] result = new string[inspection.Entries.Length];
        int index = 0;
        while (index < inspection.Entries.Length)
        {
            result[index] = inspection.Entries[index].Path;
            index++;
        }
        return result;
    }

    static byte[] ReadEntryBytes(ZipEntryIndex index, string path)
    {
        using Stream input = index.OpenEntry(path);
        using MemoryStream output = new MemoryStream();
        input.CopyTo(output);
        return output.ToArray();
    }

    static bool AllTimestampsEqual(byte[] package, DateTimeOffset expected)
    {
        using MemoryStream input = new MemoryStream(package);
        using ZipArchive archive = new ZipArchive(input, ZipArchiveMode.Read);
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            if (entry.LastWriteTime.DateTime != expected.DateTime)
            {
                return false;
            }
        }
        return true;
    }

    static byte[] CreateVariedBytes(int length)
    {
        byte[] result = new byte[length];
        uint state = 305419896;
        int index = 0;
        while (index < result.Length)
        {
            state = state * 1664525 + 1013904223;
            result[index] = (byte)(state >> 24);
            index++;
        }
        return result;
    }

    static byte[] ReadLocalRecord(byte[] package, string path)
    {
        ZipPackageInspection inspection = ZipPackageReader.InspectBytes(package);
        OdfPackageEntry entry = FindEntry(inspection, path);
        long end = inspection.DirectoryOffset;
        foreach (OdfPackageEntry candidate in inspection.Entries)
        {
            if (candidate.LocalHeaderOffset > entry.LocalHeaderOffset
                && candidate.LocalHeaderOffset < end)
            {
                end = candidate.LocalHeaderOffset;
            }
        }
        byte[] result = new byte[(int)(end - entry.LocalHeaderOffset)];
        Array.Copy(package, (int)entry.LocalHeaderOffset, result, 0, result.Length);
        return result;
    }

    static byte[] ChangeCompressionMethod(
        byte[] package,
        string path,
        ushort method)
    {
        byte[] result = new byte[package.Length];
        Array.Copy(package, result, package.Length);
        ZipPackageInspection inspection = ZipPackageReader.InspectBytes(package);
        OdfPackageEntry changed = FindEntry(inspection, path);
        WriteUInt16At(result, (int)changed.LocalHeaderOffset + 8, method);

        int cursor = (int)inspection.DirectoryOffset;
        foreach (OdfPackageEntry entry in inspection.Entries)
        {
            int recordLength = 46
                + ReadUInt16At(result, cursor + 28)
                + ReadUInt16At(result, cursor + 30)
                + ReadUInt16At(result, cursor + 32);
            if (entry.Path == path)
            {
                WriteUInt16At(result, cursor + 10, method);
                return result;
            }
            cursor = cursor + recordLength;
        }
        Assert.Fail($"Expected package entry '{path}'.");
        return result;
    }

    static ushort ReadUInt16At(byte[] data, int offset)
    {
        return (ushort)(data[offset] | data[offset + 1] << 8);
    }

    static void WriteUInt16At(byte[] data, int offset, ushort value)
    {
        data[offset] = (byte)(value & 255);
        data[offset + 1] = (byte)((value >> 8) & 255);
    }

    static string ManifestStart()
    {
        return "<m:manifest xmlns:m='"
            + OdfNamespaces.Manifest
            + "' m:version='1.4'>";
    }

    static string FileEntry(string path, string mediaType)
    {
        return "<m:file-entry m:full-path='"
            + path
            + "' m:media-type='"
            + mediaType
            + "'/>";
    }
}

public class TrackingOutputStream : MemoryStream
{
    public bool WasDisposed;

    protected override void Dispose(bool disposing)
    {
        WasDisposed = true;
        base.Dispose(disposing);
    }
}

public class NonSeekableOutputStream : Stream
{
    MemoryStream storage = new MemoryStream();

    public bool WasDisposed;

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => !WasDisposed;

    public override long Length => storage.Length;

    public override long Position
    {
        get => storage.Position;
        set => throw new NotSupportedException();
    }

    public byte[] GetBytes()
    {
        return storage.ToArray();
    }

    public override void Flush()
    {
        storage.Flush();
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
        storage.Write(buffer, offset, count);
    }

    protected override void Dispose(bool disposing)
    {
        WasDisposed = true;
        if (disposing)
        {
            storage.Dispose();
        }
        base.Dispose(disposing);
    }
}
