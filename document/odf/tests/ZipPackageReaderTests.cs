using System.IO.Compression;
using System.Text;

namespace odf.Tests;

public class ZipPackageReaderTests
{
    [Test]
    public void InspectBytesReadsStoredDeflatedAndUtf8Entries()
    {
        byte[] archive = CreateStandardArchive();

        ZipPackageInspection inspection = ZipPackageReader.InspectBytes(archive);

        Assert.Multiple(() =>
        {
            Assert.That(inspection.IsValid, Is.True);
            Assert.That(inspection.Diagnostics, Is.Empty);
            Assert.That(inspection.Entries, Has.Length.EqualTo(3));
            Assert.That(inspection.Entries[0].Path, Is.EqualTo("mimetype"));
            Assert.That(inspection.Entries[0].CompressionMethod, Is.EqualTo(OdfCompressionMethod.Stored));
            Assert.That(inspection.Entries[0].CompressionMethodCode, Is.Zero);
            Assert.That(inspection.Entries[0].HasValidCrc, Is.True);
            Assert.That(inspection.Entries[1].Path, Is.EqualTo("content.xml"));
            Assert.That(inspection.Entries[1].CompressionMethod, Is.EqualTo(OdfCompressionMethod.Deflated));
            Assert.That(inspection.Entries[1].CompressionMethodCode, Is.EqualTo(8));
            Assert.That(inspection.Entries[1].HasValidCrc, Is.True);
            Assert.That(inspection.Entries[2].Path, Is.EqualTo("Pictures/café.txt"));
            Assert.That(inspection.Entries[2].UsesUtf8Name, Is.True);
            Assert.That(inspection.Entries[2].HasValidCrc, Is.True);
        });
    }

    [Test]
    public void InspectPathAndStreamReadTheSamePackageWithoutClosingTheInput()
    {
        byte[] archive = CreateStandardArchive();
        string path = Path.GetTempFileName();
        File.WriteAllBytes(path, archive);
        try
        {
            ZipPackageInspection pathInspection = ZipPackageReader.InspectPath(path);
            using (MemoryStream stream = new MemoryStream(archive))
            {
                ZipPackageInspection streamInspection = ZipPackageReader.InspectStream(stream);

                Assert.Multiple(() =>
                {
                    Assert.That(pathInspection.IsValid, Is.True);
                    Assert.That(streamInspection.IsValid, Is.True);
                    Assert.That(pathInspection.Entries, Has.Length.EqualTo(3));
                    Assert.That(streamInspection.Entries, Has.Length.EqualTo(3));
                    Assert.That(stream.CanRead, Is.True);
                    Assert.That(stream.Position, Is.EqualTo(stream.Length));
                });
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void InspectBytesReportsDuplicateEntryNames()
    {
        byte[] archive = CreateDuplicateArchive();

        ZipPackageInspection inspection = ZipPackageReader.InspectBytes(archive);

        Assert.Multiple(() =>
        {
            Assert.That(inspection.IsValid, Is.False);
            Assert.That(inspection.Entries, Has.Length.EqualTo(2));
            Assert.That(HasDiagnostic(inspection, OdfDiagnosticCodes.DuplicateEntry), Is.True);
            Assert.That(GetDiagnostic(inspection, OdfDiagnosticCodes.DuplicateEntry).Location.PartPath, Is.EqualTo("same.txt"));
        });
    }

    [Test]
    public void InspectBytesReportsMalformedUtf8EntryNames()
    {
        byte[] archive = CreateSingleStoredArchive("plain.txt", "data");
        int localOffset = FindSignature(archive, 67324752, 0);
        int centralOffset = FindSignature(archive, 33639248, localOffset + 4);
        WriteUInt16(archive, localOffset + 6, 2048);
        WriteUInt16(archive, centralOffset + 8, 2048);
        archive[localOffset + 30] = 255;
        archive[centralOffset + 46] = 255;

        ZipPackageInspection inspection = ZipPackageReader.InspectBytes(archive);

        Assert.Multiple(() =>
        {
            Assert.That(inspection.IsValid, Is.False);
            Assert.That(inspection.Entries, Has.Length.EqualTo(1));
            Assert.That(inspection.Entries[0].UsesUtf8Name, Is.True);
            Assert.That(HasDiagnostic(inspection, OdfDiagnosticCodes.InvalidEntryPath), Is.True);
        });
    }

    [Test]
    public void InspectBytesReportsCrcMismatch()
    {
        byte[] archive = CreateSingleStoredArchive("content.xml", "content");
        int centralOffset = FindSignature(archive, 33639248, 0);
        archive[centralOffset + 16] = (byte)(archive[centralOffset + 16] ^ 255);

        ZipPackageInspection inspection = ZipPackageReader.InspectBytes(archive);

        Assert.Multiple(() =>
        {
            Assert.That(inspection.IsValid, Is.False);
            Assert.That(inspection.Entries[0].HasValidCrc, Is.False);
            Assert.That(HasDiagnostic(inspection, OdfDiagnosticCodes.CrcMismatch), Is.True);
        });
    }

    [Test]
    public void InspectBytesReportsUnsupportedCompressionWithoutExpandingIt()
    {
        byte[] archive = CreateSingleStoredArchive("content.xml", "content");
        int localOffset = FindSignature(archive, 67324752, 0);
        int centralOffset = FindSignature(archive, 33639248, localOffset + 4);
        WriteUInt16(archive, localOffset + 8, 99);
        WriteUInt16(archive, centralOffset + 10, 99);

        ZipPackageInspection inspection = ZipPackageReader.InspectBytes(archive);

        Assert.Multiple(() =>
        {
            Assert.That(inspection.IsValid, Is.False);
            Assert.That(inspection.Entries[0].CompressionMethod, Is.EqualTo(OdfCompressionMethod.Unsupported));
            Assert.That(inspection.Entries[0].CompressionMethodCode, Is.EqualTo(99));
            Assert.That(inspection.Entries[0].HasValidCrc, Is.False);
            Assert.That(HasDiagnostic(inspection, OdfDiagnosticCodes.UnsupportedCompression), Is.True);
        });
    }

    [Test]
    public void InspectBytesReadsValidZip64EntryMetadata()
    {
        byte[] archive = CreateZip64EntryArchive();

        ZipPackageInspection inspection = ZipPackageReader.InspectBytes(archive);

        Assert.Multiple(() =>
        {
            Assert.That(inspection.IsValid, Is.False);
            Assert.That(inspection.UsesZip64, Is.True);
            Assert.That(HasDiagnostic(inspection, OdfDiagnosticCodes.MissingMimetype), Is.True);
            Assert.That(inspection.Entries, Has.Length.EqualTo(1));
            Assert.That(inspection.Entries[0].UsesZip64, Is.True);
            Assert.That(inspection.Entries[0].CompressedSize, Is.EqualTo(3));
            Assert.That(inspection.Entries[0].ExpandedSize, Is.EqualTo(3));
            Assert.That(inspection.Entries[0].LocalHeaderOffset, Is.Zero);
            Assert.That(inspection.Entries[0].HasValidCrc, Is.True);
        });
    }

    [Test]
    public void InspectBytesReportsMissingZip64ExtraValues()
    {
        byte[] archive = CreateSingleStoredArchive("content.xml", "content");
        int centralOffset = FindSignature(archive, 33639248, 0);
        WriteUInt32(archive, centralOffset + 24, uint.MaxValue);

        ZipPackageInspection inspection = ZipPackageReader.InspectBytes(archive);

        Assert.Multiple(() =>
        {
            Assert.That(inspection.IsValid, Is.False);
            Assert.That(inspection.Entries, Is.Empty);
            Assert.That(HasDiagnostic(inspection, OdfDiagnosticCodes.InvalidPackage), Is.True);
            Assert.That(GetDiagnostic(inspection, OdfDiagnosticCodes.InvalidPackage).Message, Does.Contain("ZIP64"));
        });
    }

    [Test]
    public void InspectBytesReportsMissingCentralDirectory()
    {
        ZipPackageInspection inspection = ZipPackageReader.InspectBytes([1, 2, 3, 4]);

        Assert.Multiple(() =>
        {
            Assert.That(inspection.IsValid, Is.False);
            Assert.That(inspection.Entries, Is.Empty);
            Assert.That(GetDiagnostic(inspection, OdfDiagnosticCodes.InvalidPackage).Severity, Is.EqualTo(OdfValidationSeverity.Fatal));
        });
    }

    static byte[] CreateStandardArchive()
    {
        using (MemoryStream stream = new MemoryStream())
        {
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                WriteEntry(archive, "mimetype", OdfMediaTypes.Text, CompressionLevel.NoCompression);
                WriteEntry(archive, "content.xml", "<document>ordinary package content</document>", CompressionLevel.SmallestSize);
                WriteEntry(archive, "Pictures/café.txt", "image", CompressionLevel.NoCompression);
            }
            return stream.ToArray();
        }
    }

    static byte[] CreateDuplicateArchive()
    {
        using (MemoryStream stream = new MemoryStream())
        {
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                WriteEntry(archive, "same.txt", "first", CompressionLevel.NoCompression);
                WriteEntry(archive, "same.txt", "second", CompressionLevel.NoCompression);
            }
            return stream.ToArray();
        }
    }

    static byte[] CreateSingleStoredArchive(string path, string content)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                WriteEntry(archive, path, content, CompressionLevel.NoCompression);
            }
            return stream.ToArray();
        }
    }

    static void WriteEntry(ZipArchive archive, string path, string content, CompressionLevel compressionLevel)
    {
        ZipArchiveEntry entry = archive.CreateEntry(path, compressionLevel);
        using (Stream output = entry.Open())
        using (StreamWriter writer = new StreamWriter(output, new UTF8Encoding(false)))
        {
            writer.Write(content);
        }
    }

    static byte[] CreateZip64EntryArchive()
    {
        byte[] name = Encoding.UTF8.GetBytes("zip64.txt");
        byte[] content = Encoding.UTF8.GetBytes("zip");
        uint crc = ComputeCrc(content);
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            writer.Write(67324752U);
            writer.Write((ushort)45);
            writer.Write((ushort)2048);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write(crc);
            writer.Write(uint.MaxValue);
            writer.Write(uint.MaxValue);
            writer.Write((ushort)name.Length);
            writer.Write((ushort)20);
            writer.Write(name);
            writer.Write((ushort)1);
            writer.Write((ushort)16);
            writer.Write((ulong)content.Length);
            writer.Write((ulong)content.Length);
            writer.Write(content);

            long directoryOffset = stream.Position;
            writer.Write(33639248U);
            writer.Write((ushort)45);
            writer.Write((ushort)45);
            writer.Write((ushort)2048);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write(crc);
            writer.Write(uint.MaxValue);
            writer.Write(uint.MaxValue);
            writer.Write((ushort)name.Length);
            writer.Write((ushort)28);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write(0U);
            writer.Write(uint.MaxValue);
            writer.Write(name);
            writer.Write((ushort)1);
            writer.Write((ushort)24);
            writer.Write((ulong)content.Length);
            writer.Write((ulong)content.Length);
            writer.Write(0UL);
            long directorySize = stream.Position - directoryOffset;

            writer.Write(101010256U);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)1);
            writer.Write((uint)directorySize);
            writer.Write((uint)directoryOffset);
            writer.Write((ushort)0);
            writer.Flush();
            return stream.ToArray();
        }
    }

    static uint ComputeCrc(byte[] data)
    {
        uint crc = uint.MaxValue;
        foreach (byte value in data)
        {
            crc = crc ^ value;
            for (int bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 3988292384U : crc >> 1;
            }
        }
        return ~crc;
    }

    static bool HasDiagnostic(ZipPackageInspection inspection, string code)
    {
        foreach (OdfDiagnostic diagnostic in inspection.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }
        return false;
    }

    static OdfDiagnostic GetDiagnostic(ZipPackageInspection inspection, string code)
    {
        foreach (OdfDiagnostic diagnostic in inspection.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return diagnostic;
            }
        }
        Assert.Fail($"Expected diagnostic {code}.");
        return new OdfDiagnostic();
    }

    static int FindSignature(byte[] data, uint signature, int start)
    {
        int offset = start;
        while (offset <= data.Length - 4)
        {
            if (ReadUInt32(data, offset) == signature)
            {
                return offset;
            }
            offset++;
        }
        Assert.Fail($"Signature {signature} was not found.");
        return -1;
    }

    static uint ReadUInt32(byte[] data, int offset)
    {
        return (uint)data[offset]
            | ((uint)data[offset + 1] << 8)
            | ((uint)data[offset + 2] << 16)
            | ((uint)data[offset + 3] << 24);
    }

    static void WriteUInt16(byte[] data, int offset, ushort value)
    {
        data[offset] = (byte)value;
        data[offset + 1] = (byte)(value >> 8);
    }

    static void WriteUInt32(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)value;
        data[offset + 1] = (byte)(value >> 8);
        data[offset + 2] = (byte)(value >> 16);
        data[offset + 3] = (byte)(value >> 24);
    }
}
