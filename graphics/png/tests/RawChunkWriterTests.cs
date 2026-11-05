using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class RawChunkWriterTests
{
    [Test]
    public void WriteToBytes_EmitsLengthTypeDataAndGeneratedCrc()
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = PngConstants.ChunkText;
        chunk.Data = new byte[] { 65, 66, 67 };
        chunk.Crc = 0xFFFFFFFFu;

        byte[] bytes = PngRawChunkWriter.WriteToBytes(chunk);
        uint expectedCrc = PngCrc32.CalculateChunkCrc(PngConstants.ChunkText, chunk.Data);

        Assert.That(PngBinaryReader.ReadUInt32(bytes, 0), Is.EqualTo(3));
        Assert.That(bytes[4], Is.EqualTo((byte)116));
        Assert.That(bytes[5], Is.EqualTo((byte)69));
        Assert.That(bytes[6], Is.EqualTo((byte)88));
        Assert.That(bytes[7], Is.EqualTo((byte)116));
        Assert.That(new byte[] { bytes[8], bytes[9], bytes[10] }, Is.EqualTo(chunk.Data));
        Assert.That(PngBinaryReader.ReadUInt32(bytes, 11), Is.EqualTo(expectedCrc));

        Assert.That(chunk.Length, Is.EqualTo(3));
        Assert.That(chunk.Crc, Is.EqualTo(expectedCrc));
        Assert.That(chunk.Offset, Is.EqualTo(0));
        Assert.That(chunk.IsKnown, Is.True);
        Assert.That(chunk.IsCritical, Is.False);
        Assert.That(chunk.IsPublic, Is.True);
        Assert.That(chunk.IsReservedBitValid, Is.True);
        Assert.That(chunk.IsSafeToCopy, Is.True);
    }

    [Test]
    public void WriteToBytes_RoundTripsThroughRawChunkReader()
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = PngConstants.ChunkIEND;
        chunk.Data = new byte[0];

        byte[] bytes = PngRawChunkWriter.WriteToBytes(chunk);
        PngChunk readChunk = PngRawChunkReader.Read(bytes, 0);

        Assert.That(readChunk.Type, Is.EqualTo(PngConstants.ChunkIEND));
        Assert.That(readChunk.Length, Is.EqualTo(0));
        Assert.That(readChunk.Data, Is.Empty);
        Assert.That(readChunk.Crc, Is.EqualTo(0xAE426082u));
    }

    [Test]
    public void Write_StreamEmitsChunkAtCurrentPositionAndUpdatesOffset()
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = "vpAg";
        chunk.Data = new byte[] { 1, 2, 3, 4 };
        MemoryStream stream = new MemoryStream();
        stream.WriteByte(0xAA);
        stream.WriteByte(0xBB);

        PngRawChunkWriter.Write(stream, chunk);

        Assert.That(chunk.Offset, Is.EqualTo(2));
        Assert.That(stream.Position, Is.EqualTo(18));

        stream.Position = 2;
        PngChunk readChunk = PngRawChunkReader.Read(stream);
        Assert.That(readChunk.Type, Is.EqualTo("vpAg"));
        Assert.That(readChunk.Data, Is.EqualTo(chunk.Data));
        Assert.That(readChunk.Crc, Is.EqualTo(chunk.Crc));
    }

    [Test]
    public void Write_GeneratesCrcInsteadOfTrustingModelCrc()
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = PngConstants.ChunkIEND;
        chunk.Data = new byte[0];
        chunk.Crc = 0;

        byte[] bytes = PngRawChunkWriter.WriteToBytes(chunk);

        Assert.That(PngBinaryReader.ReadUInt32(bytes, 8), Is.EqualTo(0xAE426082u));
        Assert.That(chunk.Crc, Is.EqualTo(0xAE426082u));
    }

    [Test]
    public void Write_EnforcesIdatAndMetadataLimits()
    {
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxIdatBytes = 2;
        limits.MaxMetadataBytes = 2;
        PngChunk idat = new PngChunk();
        idat.Type = PngConstants.ChunkIDAT;
        idat.Data = new byte[] { 1, 2, 3 };
        PngChunk text = new PngChunk();
        text.Type = PngConstants.ChunkText;
        text.Data = new byte[] { 1, 2, 3 };

        Assert.Throws<OverflowException>(() => PngRawChunkWriter.WriteToBytes(idat, limits));
        Assert.Throws<OverflowException>(() => PngRawChunkWriter.WriteToBytes(text, limits));
    }

    [Test]
    public void Write_StreamEnforcesProvidedLimits()
    {
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxMetadataBytes = 2;
        PngChunk text = new PngChunk();
        text.Type = PngConstants.ChunkText;
        text.Data = new byte[] { 1, 2, 3 };

        Assert.Throws<OverflowException>(() => PngRawChunkWriter.Write(new MemoryStream(), text, limits));
    }

    [Test]
    public void WriteToBytes_UpdatesCriticalChunkFlags()
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = PngConstants.ChunkIDAT;
        chunk.Data = new byte[] { 1, 2, 3 };

        PngRawChunkWriter.WriteToBytes(chunk);

        Assert.That(chunk.Length, Is.EqualTo(3));
        Assert.That(chunk.IsKnown, Is.True);
        Assert.That(chunk.IsCritical, Is.True);
        Assert.That(chunk.IsPublic, Is.True);
        Assert.That(chunk.IsReservedBitValid, Is.True);
        Assert.That(chunk.IsSafeToCopy, Is.False);
    }

    [Test]
    public void Write_RejectsInvalidInputs()
    {
        PngChunk validChunk = new PngChunk();
        validChunk.Type = PngConstants.ChunkIEND;
        validChunk.Data = new byte[0];
        PngChunk invalidType = new PngChunk();
        invalidType.Type = "abca";
        invalidType.Data = new byte[0];
        PngChunk nullData = new PngChunk();
        nullData.Type = PngConstants.ChunkIEND;
        nullData.Data = null!;

        Assert.Throws<ArgumentNullException>(() => PngRawChunkWriter.WriteToBytes(null!));
        Assert.Throws<ArgumentNullException>(() => PngRawChunkWriter.WriteToBytes(validChunk, null!));
        Assert.Throws<ArgumentNullException>(() => PngRawChunkWriter.Write(null!, validChunk));
        Assert.Throws<ArgumentNullException>(() => PngRawChunkWriter.Write(new MemoryStream(), validChunk, null!));
        Assert.Throws<ArgumentNullException>(() => PngRawChunkWriter.WriteToBytes(nullData));
        Assert.Throws<InvalidDataException>(() => PngRawChunkWriter.WriteToBytes(invalidType));
    }
}
