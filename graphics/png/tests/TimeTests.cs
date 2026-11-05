using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class TimeTests
{
    [Test]
    public void Parse_ReadsTimestampUsingOffset()
    {
        PngTime time = PngTimeParser.Parse(new byte[] { 99, 0x07, 0xE8, 8, 4, 23, 59, 60, 88 }, 1, 7);

        AssertTime(time);
    }

    [Test]
    public void Parse_AllowsLeapSecond()
    {
        PngTime time = PngTimeParser.Parse(new byte[] { 0x07, 0xE8, 1, 1, 0, 0, 60 });

        Assert.That(time.Second, Is.EqualTo(60));
    }

    [Test]
    public void Parse_ReadsChunkPayload()
    {
        PngChunk chunk = CreateChunk(PngConstants.ChunkTime, CreatePayload());

        PngTime time = PngTimeParser.Parse(chunk);

        AssertTime(time);
    }

    [Test]
    public void WriteData_WritesTimestamp()
    {
        byte[] data = PngTimeWriter.WriteData(CreateTime());

        Assert.That(data, Is.EqualTo(CreatePayload()));
    }

    [Test]
    public void WriteData_WritesAtOffset()
    {
        byte[] data = new byte[] { 99, 99, 99, 99, 99, 99, 99, 99, 88 };

        PngTimeWriter.WriteData(data, 1, CreateTime());

        Assert.That(data, Is.EqualTo(new byte[] { 99, 0x07, 0xE8, 8, 4, 23, 59, 60, 88 }));
    }

    [Test]
    public void WriteChunk_CreatesKnownTimeChunk()
    {
        PngChunk chunk = PngTimeWriter.WriteChunk(CreateTime());

        Assert.That(chunk.Type, Is.EqualTo(PngConstants.ChunkTime));
        Assert.That(chunk.Data, Is.EqualTo(CreatePayload()));
        Assert.That(chunk.Length, Is.EqualTo(7));
        Assert.That(chunk.IsKnown, Is.True);
        Assert.That(chunk.IsCritical, Is.False);
        Assert.That(chunk.IsPublic, Is.True);
        Assert.That(chunk.IsReservedBitValid, Is.True);
        Assert.That(chunk.IsSafeToCopy, Is.False);
    }

    [Test]
    public void WriteChunk_RoundTripsThroughRawChunkWriter()
    {
        PngChunk chunk = PngTimeWriter.WriteChunk(CreateTime());

        byte[] rawChunk = PngRawChunkWriter.WriteToBytes(chunk);
        PngChunk readChunk = PngRawChunkReader.Read(rawChunk, 0);
        PngTime time = PngTimeParser.Parse(readChunk);

        AssertTime(time);
        Assert.That(readChunk.Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc(PngConstants.ChunkTime, readChunk.Data)));
    }

    [Test]
    public void Parser_RejectsInvalidInputs()
    {
        PngChunk nullType = new PngChunk();
        nullType.Type = null!;
        nullType.Data = CreatePayload();
        PngChunk nullData = new PngChunk();
        nullData.Type = PngConstants.ChunkTime;
        nullData.Data = null!;
        PngChunk wrongType = CreateChunk(PngConstants.ChunkGamma, CreatePayload());

        Assert.Throws<ArgumentNullException>(() => PngTimeParser.Parse((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngTimeParser.Parse(null!, 0, 7));
        Assert.Throws<ArgumentNullException>(() => PngTimeParser.Parse((PngChunk)null!));
        Assert.Throws<ArgumentNullException>(() => PngTimeParser.Parse(nullType));
        Assert.Throws<ArgumentNullException>(() => PngTimeParser.Parse(nullData));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTimeParser.Parse(new byte[7], -1, 7));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTimeParser.Parse(new byte[7], 8, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTimeParser.Parse(new byte[7], 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTimeParser.Parse(new byte[7], 1, 7));
        Assert.Throws<InvalidDataException>(() => PngTimeParser.Parse(new byte[6]));
        Assert.Throws<InvalidDataException>(() => PngTimeParser.Parse(new byte[8]));
        Assert.Throws<InvalidDataException>(() => PngTimeParser.Parse(new byte[] { 0x07, 0xE8, 0, 1, 0, 0, 0 }));
        Assert.Throws<InvalidDataException>(() => PngTimeParser.Parse(new byte[] { 0x07, 0xE8, 13, 1, 0, 0, 0 }));
        Assert.Throws<InvalidDataException>(() => PngTimeParser.Parse(new byte[] { 0x07, 0xE8, 1, 0, 0, 0, 0 }));
        Assert.Throws<InvalidDataException>(() => PngTimeParser.Parse(new byte[] { 0x07, 0xE8, 1, 32, 0, 0, 0 }));
        Assert.Throws<InvalidDataException>(() => PngTimeParser.Parse(new byte[] { 0x07, 0xE8, 1, 1, 24, 0, 0 }));
        Assert.Throws<InvalidDataException>(() => PngTimeParser.Parse(new byte[] { 0x07, 0xE8, 1, 1, 0, 60, 0 }));
        Assert.Throws<InvalidDataException>(() => PngTimeParser.Parse(new byte[] { 0x07, 0xE8, 1, 1, 0, 0, 61 }));
        Assert.Throws<InvalidDataException>(() => PngTimeParser.Parse(wrongType));
    }

    [Test]
    public void Writer_RejectsInvalidInputs()
    {
        PngTime time = CreateTime();
        PngTime badMonthLow = CreateTime();
        badMonthLow.Month = 0;
        PngTime badMonthHigh = CreateTime();
        badMonthHigh.Month = 13;
        PngTime badDayLow = CreateTime();
        badDayLow.Day = 0;
        PngTime badDayHigh = CreateTime();
        badDayHigh.Day = 32;
        PngTime badHour = CreateTime();
        badHour.Hour = 24;
        PngTime badMinute = CreateTime();
        badMinute.Minute = 60;
        PngTime badSecond = CreateTime();
        badSecond.Second = 61;

        Assert.Throws<ArgumentNullException>(() => PngTimeWriter.WriteData(null!));
        Assert.Throws<ArgumentNullException>(() => PngTimeWriter.WriteData(null!, 0, time));
        Assert.Throws<ArgumentNullException>(() => PngTimeWriter.WriteData(new byte[7], 0, null!));
        Assert.Throws<ArgumentNullException>(() => PngTimeWriter.WriteChunk(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTimeWriter.WriteData(new byte[7], -1, time));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTimeWriter.WriteData(new byte[7], 1, time));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTimeWriter.WriteData(badMonthLow));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTimeWriter.WriteData(badMonthHigh));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTimeWriter.WriteData(badDayLow));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTimeWriter.WriteData(badDayHigh));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTimeWriter.WriteData(badHour));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTimeWriter.WriteData(badMinute));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTimeWriter.WriteData(badSecond));
    }

    static PngTime CreateTime()
    {
        PngTime time = new PngTime();
        time.Year = 2024;
        time.Month = 8;
        time.Day = 4;
        time.Hour = 23;
        time.Minute = 59;
        time.Second = 60;
        return time;
    }

    static byte[] CreatePayload()
    {
        return new byte[] { 0x07, 0xE8, 8, 4, 23, 59, 60 };
    }

    static void AssertTime(PngTime time)
    {
        Assert.That(time.Year, Is.EqualTo(2024));
        Assert.That(time.Month, Is.EqualTo(8));
        Assert.That(time.Day, Is.EqualTo(4));
        Assert.That(time.Hour, Is.EqualTo(23));
        Assert.That(time.Minute, Is.EqualTo(59));
        Assert.That(time.Second, Is.EqualTo(60));
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
