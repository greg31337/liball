using System.Collections.Generic;
using System.IO;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class PngChunkTraverserTests
{
    [Test]
    public void Parse_ReadsPngChunksAndRecordsCrcState()
    {
        byte[] ihdr = new byte[]
        {
            0, 0, 0, 1,
            0, 0, 0, 1,
            8, 6, 0, 0, 0
        };

        ImageMetadataDocument document = ParsePng(CreatePng(new string[] { "IHDR", "tEXt", "IEND" }, new byte[][] { ihdr, Encoding.ASCII.GetBytes("k\0v"), new byte[0] }));

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Directories, Has.Count.EqualTo(4));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Png.Header.SignatureValid").Value.BooleanValue, Is.True);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Png.IHDR.Index").Value.IntegerValue, Is.EqualTo(0));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Png.IHDR.Length").Value.UnsignedIntegerValue, Is.EqualTo(13));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Png.IHDR.DataOffset").Value.IntegerValue, Is.EqualTo(16));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Png.IHDR.CrcOffset").Value.IntegerValue, Is.EqualTo(29));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Png.IHDR.CrcValid").Value.BooleanValue, Is.True);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Png.IHDR.Critical").Value.BooleanValue, Is.True);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Png.IHDR.Public").Value.BooleanValue, Is.True);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Png.IHDR.ReservedBitValid").Value.BooleanValue, Is.True);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Png.IHDR.SafeToCopy").Value.BooleanValue, Is.False);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Png.tEXt.Index").Value.IntegerValue, Is.EqualTo(1));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Png.tEXt.CrcValid").Value.BooleanValue, Is.True);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Png.IEND.Index").Value.IntegerValue, Is.EqualTo(2));
    }

    [Test]
    public void Parse_AddsDiagnosticForCrcMismatch()
    {
        byte[] png = CreatePng(new string[] { "IHDR", "tEXt", "IEND" }, new byte[][] { CreateIhdr(), Encoding.ASCII.GetBytes("bad"), new byte[0] });
        png[45] = (byte)(png[45] + 1);

        ImageMetadataDocument document = ParsePng(png);

        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("png.crc.mismatch"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.RecoverableError));
        Assert.That(document.Diagnostics[0].BlockType, Is.EqualTo("tEXt"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Png.tEXt.CrcValid").Value.BooleanValue, Is.False);
    }

    [Test]
    public void Parse_StopsWithDiagnosticForTruncatedChunkPayload()
    {
        List<byte> bytes = new List<byte>();
        AddSignature(bytes);
        AddUnsigned32(bytes, 5);
        AddAscii(bytes, "tEXt");
        bytes.Add(1);
        bytes.Add(2);

        ImageMetadataDocument document = ParsePng(bytes.ToArray());

        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("png.chunk.truncated"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.Truncated));
        Assert.That(document.Diagnostics[0].BlockType, Is.EqualTo("tEXt"));
    }

    [Test]
    public void Parse_ReportsInvalidChunkType()
    {
        ImageMetadataDocument document = ParsePng(CreatePng(new string[] { "ID1T", "IEND" }, new byte[][] { new byte[0], new byte[0] }));

        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("png.chunk.type.invalid"));
        Assert.That(document.Diagnostics[0].BlockType, Is.EqualTo("ID1T"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Png.ID1T.TypeValid").Value.BooleanValue, Is.False);
    }

    public static byte[] CreateIhdr()
    {
        return new byte[]
        {
            0, 0, 0, 1,
            0, 0, 0, 1,
            8, 6, 0, 0, 0
        };
    }

    public static byte[] CreatePng(string[] types, byte[][] payloads)
    {
        List<byte> bytes = new List<byte>();
        AddSignature(bytes);

        int index = 0;
        while (index < types.Length)
        {
            AddChunk(bytes, types[index], payloads[index]);
            index = index + 1;
        }

        return bytes.ToArray();
    }

    public static void AddSignature(List<byte> bytes)
    {
        bytes.Add(137);
        bytes.Add(80);
        bytes.Add(78);
        bytes.Add(71);
        bytes.Add(13);
        bytes.Add(10);
        bytes.Add(26);
        bytes.Add(10);
    }

    public static void AddChunk(List<byte> bytes, string type, byte[] data)
    {
        AddUnsigned32(bytes, (uint)data.Length);
        AddAscii(bytes, type);
        foreach (byte value in data)
        {
            bytes.Add(value);
        }

        uint crc = Crc32.ComputePngChunk(type, data, new List<MetadataDiagnostic>());
        AddUnsigned32(bytes, crc);
    }

    public static void AddAscii(List<byte> bytes, string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            bytes.Add((byte)text[index]);
            index = index + 1;
        }
    }

    public static void AddUnsigned32(List<byte> bytes, uint value)
    {
        bytes.Add((byte)((value >> 24) & 255));
        bytes.Add((byte)((value >> 16) & 255));
        bytes.Add((byte)((value >> 8) & 255));
        bytes.Add((byte)(value & 255));
    }

    public static ImageMetadataDocument ParsePng(byte[] bytes)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return PngMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.Png), new ImageMetadataReaderOptions());
    }
}
