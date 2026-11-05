using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class CodingIndependentCodePointsTests
{
    [Test]
    public void Parse_ReadsCodePointsUsingOffset()
    {
        PngCodingIndependentCodePoints codePoints = PngCodingIndependentCodePointsParser.Parse(new byte[] { 99, 9, 16, 9, 1, 88 }, 1, 4);

        AssertCodePoints(codePoints);
    }

    [Test]
    public void Parse_AcceptsLimitedAndFullRangeFlags()
    {
        Assert.That(PngCodingIndependentCodePointsParser.Parse(new byte[] { 1, 1, 1, 0 }).VideoFullRangeFlag, Is.EqualTo(0));
        Assert.That(PngCodingIndependentCodePointsParser.Parse(new byte[] { 1, 1, 1, 1 }).VideoFullRangeFlag, Is.EqualTo(1));
    }

    [Test]
    public void Parse_ReadsChunkPayload()
    {
        PngChunk chunk = CreateChunk(PngConstants.ChunkCodingIndependentCodePoints, new byte[] { 9, 16, 9, 1 });

        PngCodingIndependentCodePoints codePoints = PngCodingIndependentCodePointsParser.Parse(chunk);

        AssertCodePoints(codePoints);
    }

    [Test]
    public void WriteData_WritesCodePoints()
    {
        PngCodingIndependentCodePoints codePoints = CreateCodePoints();

        byte[] data = PngCodingIndependentCodePointsWriter.WriteData(codePoints);

        Assert.That(data, Is.EqualTo(new byte[] { 9, 16, 9, 1 }));
    }

    [Test]
    public void WriteData_WritesAtOffset()
    {
        PngCodingIndependentCodePoints codePoints = CreateCodePoints();
        byte[] data = new byte[] { 99, 99, 99, 99, 99, 88 };

        PngCodingIndependentCodePointsWriter.WriteData(data, 1, codePoints);

        Assert.That(data, Is.EqualTo(new byte[] { 99, 9, 16, 9, 1, 88 }));
    }

    [Test]
    public void WriteChunk_CreatesKnownCicpChunk()
    {
        PngCodingIndependentCodePoints codePoints = CreateCodePoints();

        PngChunk chunk = PngCodingIndependentCodePointsWriter.WriteChunk(codePoints);

        Assert.That(chunk.Type, Is.EqualTo(PngConstants.ChunkCodingIndependentCodePoints));
        Assert.That(chunk.Data, Is.EqualTo(new byte[] { 9, 16, 9, 1 }));
        Assert.That(chunk.Length, Is.EqualTo(4));
        Assert.That(chunk.IsKnown, Is.True);
        Assert.That(chunk.IsCritical, Is.False);
        Assert.That(chunk.IsPublic, Is.True);
        Assert.That(chunk.IsReservedBitValid, Is.True);
        Assert.That(chunk.IsSafeToCopy, Is.False);
    }

    [Test]
    public void WriteChunk_RoundTripsThroughRawChunkWriter()
    {
        PngChunk chunk = PngCodingIndependentCodePointsWriter.WriteChunk(CreateCodePoints());

        byte[] rawChunk = PngRawChunkWriter.WriteToBytes(chunk);
        PngChunk readChunk = PngRawChunkReader.Read(rawChunk, 0);
        PngCodingIndependentCodePoints codePoints = PngCodingIndependentCodePointsParser.Parse(readChunk);

        AssertCodePoints(codePoints);
        Assert.That(readChunk.Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc(PngConstants.ChunkCodingIndependentCodePoints, readChunk.Data)));
    }

    [Test]
    public void Parser_RejectsInvalidInputs()
    {
        PngChunk nullType = new PngChunk();
        nullType.Type = null!;
        nullType.Data = new byte[] { 9, 16, 9, 1 };
        PngChunk nullData = new PngChunk();
        nullData.Type = PngConstants.ChunkCodingIndependentCodePoints;
        nullData.Data = null!;
        PngChunk wrongType = CreateChunk(PngConstants.ChunkGamma, new byte[] { 9, 16, 9, 1 });

        Assert.Throws<ArgumentNullException>(() => PngCodingIndependentCodePointsParser.Parse((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngCodingIndependentCodePointsParser.Parse(null!, 0, 4));
        Assert.Throws<ArgumentNullException>(() => PngCodingIndependentCodePointsParser.Parse((PngChunk)null!));
        Assert.Throws<ArgumentNullException>(() => PngCodingIndependentCodePointsParser.Parse(nullType));
        Assert.Throws<ArgumentNullException>(() => PngCodingIndependentCodePointsParser.Parse(nullData));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngCodingIndependentCodePointsParser.Parse(new byte[4], -1, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngCodingIndependentCodePointsParser.Parse(new byte[4], 5, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngCodingIndependentCodePointsParser.Parse(new byte[4], 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngCodingIndependentCodePointsParser.Parse(new byte[4], 1, 4));
        Assert.Throws<InvalidDataException>(() => PngCodingIndependentCodePointsParser.Parse(new byte[3]));
        Assert.Throws<InvalidDataException>(() => PngCodingIndependentCodePointsParser.Parse(new byte[5]));
        Assert.Throws<InvalidDataException>(() => PngCodingIndependentCodePointsParser.Parse(new byte[] { 9, 16, 9, 2 }));
        Assert.Throws<InvalidDataException>(() => PngCodingIndependentCodePointsParser.Parse(wrongType));
    }

    [Test]
    public void Writer_RejectsInvalidInputs()
    {
        PngCodingIndependentCodePoints codePoints = CreateCodePoints();
        PngCodingIndependentCodePoints invalidFlag = CreateCodePoints();
        invalidFlag.VideoFullRangeFlag = 2;

        Assert.Throws<ArgumentNullException>(() => PngCodingIndependentCodePointsWriter.WriteData(null!));
        Assert.Throws<ArgumentNullException>(() => PngCodingIndependentCodePointsWriter.WriteData(null!, 0, codePoints));
        Assert.Throws<ArgumentNullException>(() => PngCodingIndependentCodePointsWriter.WriteData(new byte[4], 0, null!));
        Assert.Throws<ArgumentNullException>(() => PngCodingIndependentCodePointsWriter.WriteChunk(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngCodingIndependentCodePointsWriter.WriteData(new byte[4], -1, codePoints));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngCodingIndependentCodePointsWriter.WriteData(new byte[4], 1, codePoints));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngCodingIndependentCodePointsWriter.WriteData(invalidFlag));
    }

    static void AssertCodePoints(PngCodingIndependentCodePoints codePoints)
    {
        Assert.That(codePoints.ColorPrimaries, Is.EqualTo(9));
        Assert.That(codePoints.TransferCharacteristics, Is.EqualTo(16));
        Assert.That(codePoints.MatrixCoefficients, Is.EqualTo(9));
        Assert.That(codePoints.VideoFullRangeFlag, Is.EqualTo(1));
    }

    static PngCodingIndependentCodePoints CreateCodePoints()
    {
        PngCodingIndependentCodePoints codePoints = new PngCodingIndependentCodePoints();
        codePoints.ColorPrimaries = 9;
        codePoints.TransferCharacteristics = 16;
        codePoints.MatrixCoefficients = 9;
        codePoints.VideoFullRangeFlag = 1;
        return codePoints;
    }

    static PngChunk CreateChunk(string type, byte[] data)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = type;
        chunk.Data = data;
        chunk.Length = (uint)data.Length;
        chunk.IsKnown = true;
        return chunk;
    }
}
