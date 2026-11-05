using System.Reflection;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class IendTests
{
    [Test]
    public void PngIend_UsesInternalFieldsOnly()
    {
        BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        Assert.That(typeof(PngIend).IsNotPublic, Is.True);
        Assert.That(typeof(PngIend).GetProperties(flags), Is.Empty);
        Assert.That(typeof(PngIend).GetEvents(flags), Is.Empty);
        Assert.That(typeof(PngIend).GetMethods(flags), Is.Empty);
        Assert.That(typeof(PngIend).GetFields(flags), Has.Length.EqualTo(1));
    }

    [Test]
    public void Parse_AcceptsEmptyPayload()
    {
        PngIend iend = PngIendParser.Parse(new byte[0]);

        Assert.That(iend.IsEnd, Is.True);
    }

    [Test]
    public void Parse_AcceptsZeroLengthPayloadAtOffset()
    {
        byte[] data = new byte[] { 1, 2, 3 };

        PngIend iend = PngIendParser.Parse(data, 2, 0);

        Assert.That(iend.IsEnd, Is.True);
    }

    [Test]
    public void Parse_AcceptsIendChunk()
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = PngConstants.ChunkIEND;
        chunk.Data = new byte[0];

        PngIend iend = PngIendParser.Parse(chunk);

        Assert.That(iend.IsEnd, Is.True);
    }

    [Test]
    public void WriteData_ReturnsEmptyPayload()
    {
        byte[] data = PngIendWriter.WriteData();

        Assert.That(data, Is.Not.Null);
        Assert.That(data, Is.Empty);
    }

    [Test]
    public void WriteData_AtOffsetLeavesArrayUnchanged()
    {
        byte[] data = new byte[] { 9, 8, 7 };

        PngIendWriter.WriteData(data, 3);

        Assert.That(data, Is.EqualTo(new byte[] { 9, 8, 7 }));
    }

    [Test]
    public void WriteChunk_CreatesIendChunkModel()
    {
        PngChunk chunk = PngIendWriter.WriteChunk();

        Assert.That(chunk.Type, Is.EqualTo(PngConstants.ChunkIEND));
        Assert.That(chunk.Data, Is.Empty);
        Assert.That(chunk.Length, Is.EqualTo(0));
        Assert.That(chunk.IsKnown, Is.True);
        Assert.That(chunk.IsCritical, Is.True);
        Assert.That(chunk.IsPublic, Is.True);
        Assert.That(chunk.IsReservedBitValid, Is.True);
        Assert.That(chunk.IsSafeToCopy, Is.False);
    }

    [Test]
    public void WriteChunk_RoundTripsThroughRawChunkReaderAndParser()
    {
        PngChunk chunk = PngIendWriter.WriteChunk();
        byte[] rawChunk = PngRawChunkWriter.WriteToBytes(chunk);

        PngChunk readChunk = PngRawChunkReader.Read(rawChunk, 0);
        PngIend readIend = PngIendParser.Parse(readChunk);

        Assert.That(readChunk.Type, Is.EqualTo(PngConstants.ChunkIEND));
        Assert.That(readChunk.Length, Is.EqualTo(0));
        Assert.That(readChunk.Crc, Is.EqualTo(0xAE426082u));
        Assert.That(readIend.IsEnd, Is.True);
    }

    [Test]
    public void WriteChunk_RoundTripsThroughDatastreamWriterAndChunkListReader()
    {
        PngChunk chunk = PngIendWriter.WriteChunk();

        byte[] datastream = PngChunkDatastreamWriter.WriteToBytes(new PngChunk[] { chunk });
        PngChunkListResult result = PngChunkListReader.Read(datastream, PngConstants.SignatureLength);

        Assert.That(PngSignature.HasSignature(datastream), Is.True);
        Assert.That(result.FoundIend, Is.True);
        Assert.That(result.Chunks, Has.Length.EqualTo(1));
        Assert.That(result.Chunks[0].Type, Is.EqualTo(PngConstants.ChunkIEND));
        Assert.That(result.Chunks[0].Length, Is.EqualTo(0));
        Assert.That(result.Chunks[0].Crc, Is.EqualTo(0xAE426082u));
        Assert.That(result.TrailingBytes, Is.Empty);
    }

    [Test]
    public void Parse_RejectsInvalidInputs()
    {
        PngChunk wrongType = new PngChunk();
        wrongType.Type = PngConstants.ChunkIHDR;
        wrongType.Data = new byte[0];
        PngChunk nullType = new PngChunk();
        nullType.Type = null!;
        nullType.Data = new byte[0];
        PngChunk nullData = new PngChunk();
        nullData.Type = PngConstants.ChunkIEND;
        nullData.Data = null!;

        Assert.Throws<ArgumentNullException>(() => PngIendParser.Parse((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngIendParser.Parse(null!, 0, 0));
        Assert.Throws<ArgumentNullException>(() => PngIendParser.Parse((PngChunk)null!));
        Assert.Throws<ArgumentNullException>(() => PngIendParser.Parse(nullType));
        Assert.Throws<ArgumentNullException>(() => PngIendParser.Parse(nullData));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngIendParser.Parse(new byte[0], -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngIendParser.Parse(new byte[0], 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngIendParser.Parse(new byte[1], 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngIendParser.Parse(new byte[1], 1, 1));
        Assert.Throws<InvalidDataException>(() => PngIendParser.Parse(new byte[] { 1 }));
        Assert.Throws<InvalidDataException>(() => PngIendParser.Parse(new byte[] { 1 }, 0, 1));
        Assert.Throws<InvalidDataException>(() => PngIendParser.Parse(wrongType));
    }

    [Test]
    public void WriteData_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => PngIendWriter.WriteData(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngIendWriter.WriteData(new byte[0], -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngIendWriter.WriteData(new byte[0], 1));
    }
}
