using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class TextTests
{
    [Test]
    public void Parse_ReadsLatin1KeywordAndTextUsingOffset()
    {
        PngText text = PngTextParser.Parse(new byte[]
        {
            99,
            65, 117, 116, 104, 111, 114,
            0,
            74, 97, 108, 97, 112, 101, 0xF1, 111, 10,
            88
        }, 1, 16);

        Assert.That(text.Keyword, Is.EqualTo("Author"));
        Assert.That(text.Text, Is.EqualTo("Jalape\u00F1o\n"));
    }

    [Test]
    public void Parse_AllowsEmptyTextString()
    {
        PngText text = PngTextParser.Parse(new byte[] { 84, 105, 116, 108, 101, 0 });

        Assert.That(text.Keyword, Is.EqualTo("Title"));
        Assert.That(text.Text, Is.EqualTo(""));
    }

    [Test]
    public void Parse_ReadsChunkPayload()
    {
        PngChunk chunk = CreateChunk(PngConstants.ChunkText, CreatePayload());

        PngText text = PngTextParser.Parse(chunk);

        AssertText(text);
    }

    [Test]
    public void WriteData_WritesKeywordSeparatorAndLatin1Text()
    {
        byte[] data = PngTextWriter.WriteData(CreateText());

        Assert.That(data, Is.EqualTo(CreatePayload()));
    }

    [Test]
    public void WriteData_WritesAtOffset()
    {
        PngText text = CreateText();
        byte[] data = new byte[26];
        data[0] = 99;
        data[25] = 88;

        PngTextWriter.WriteData(data, 1, text);

        Assert.That(data, Is.EqualTo(new byte[]
        {
            99,
            67, 111, 109, 109, 101, 110, 116,
            0,
            76, 105, 110, 101, 10, 67, 111, 112, 121, 114, 105, 103, 104, 116, 32, 0xA9,
            88
        }));
    }

    [Test]
    public void WriteChunk_CreatesKnownTextChunk()
    {
        PngChunk chunk = PngTextWriter.WriteChunk(CreateText());

        Assert.That(chunk.Type, Is.EqualTo(PngConstants.ChunkText));
        Assert.That(chunk.Data, Is.EqualTo(CreatePayload()));
        Assert.That(chunk.Length, Is.EqualTo(CreatePayload().Length));
        Assert.That(chunk.IsKnown, Is.True);
        Assert.That(chunk.IsCritical, Is.False);
        Assert.That(chunk.IsPublic, Is.True);
        Assert.That(chunk.IsReservedBitValid, Is.True);
        Assert.That(chunk.IsSafeToCopy, Is.True);
    }

    [Test]
    public void WriteChunk_RoundTripsThroughRawChunkWriter()
    {
        PngChunk chunk = PngTextWriter.WriteChunk(CreateText());

        byte[] rawChunk = PngRawChunkWriter.WriteToBytes(chunk);
        PngChunk readChunk = PngRawChunkReader.Read(rawChunk, 0);
        PngText text = PngTextParser.Parse(readChunk);

        AssertText(text);
        Assert.That(readChunk.Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc(PngConstants.ChunkText, readChunk.Data)));
    }

    [Test]
    public void Parser_RejectsInvalidInputs()
    {
        PngChunk nullType = new PngChunk();
        nullType.Type = null!;
        nullType.Data = CreatePayload();
        PngChunk nullData = new PngChunk();
        nullData.Type = PngConstants.ChunkText;
        nullData.Data = null!;
        PngChunk wrongType = CreateChunk(PngConstants.ChunkGamma, CreatePayload());

        Assert.Throws<ArgumentNullException>(() => PngTextParser.Parse((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngTextParser.Parse(null!, 0, 1));
        Assert.Throws<ArgumentNullException>(() => PngTextParser.Parse((PngChunk)null!));
        Assert.Throws<ArgumentNullException>(() => PngTextParser.Parse(nullType));
        Assert.Throws<ArgumentNullException>(() => PngTextParser.Parse(nullData));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTextParser.Parse(new byte[] { 65, 0 }, -1, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTextParser.Parse(new byte[] { 65, 0 }, 3, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTextParser.Parse(new byte[] { 65, 0 }, 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTextParser.Parse(new byte[] { 65, 0 }, 1, 2));
        Assert.Throws<InvalidDataException>(() => PngTextParser.Parse(new byte[] { 65, 66 }));
        Assert.Throws<InvalidDataException>(() => PngTextParser.Parse(new byte[] { 0, 65 }));
        Assert.Throws<InvalidDataException>(() => PngTextParser.Parse(CreateLongKeywordPayload()));
        Assert.Throws<InvalidDataException>(() => PngTextParser.Parse(new byte[] { 65, 0, 66, 0, 67 }));
        Assert.Throws<InvalidDataException>(() => PngTextParser.Parse(wrongType));
    }

    [Test]
    public void Writer_RejectsInvalidInputs()
    {
        PngText text = CreateText();
        PngText nullKeyword = CreateText();
        nullKeyword.Keyword = null!;
        PngText nullText = CreateText();
        nullText.Text = null!;
        PngText emptyKeyword = CreateText();
        emptyKeyword.Keyword = "";
        PngText longKeyword = CreateText();
        longKeyword.Keyword = new string('A', 80);
        PngText nullInKeyword = CreateText();
        nullInKeyword.Keyword = "A\0B";
        PngText nullInText = CreateText();
        nullInText.Text = "A\0B";
        PngText nonLatin1Keyword = CreateText();
        nonLatin1Keyword.Keyword = "A\u0100";
        PngText nonLatin1Text = CreateText();
        nonLatin1Text.Text = "A\u0100";

        Assert.Throws<ArgumentNullException>(() => PngTextWriter.WriteData(null!));
        Assert.Throws<ArgumentNullException>(() => PngTextWriter.WriteData(null!, 0, text));
        Assert.Throws<ArgumentNullException>(() => PngTextWriter.WriteData(new byte[25], 0, null!));
        Assert.Throws<ArgumentNullException>(() => PngTextWriter.WriteChunk(null!));
        Assert.Throws<ArgumentNullException>(() => PngTextWriter.WriteData(nullKeyword));
        Assert.Throws<ArgumentNullException>(() => PngTextWriter.WriteData(nullText));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTextWriter.WriteData(emptyKeyword));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTextWriter.WriteData(longKeyword));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTextWriter.WriteData(nullInKeyword));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTextWriter.WriteData(nullInText));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTextWriter.WriteData(nonLatin1Keyword));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTextWriter.WriteData(nonLatin1Text));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTextWriter.WriteData(new byte[25], -1, text));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTextWriter.WriteData(new byte[24], 1, text));
    }

    static void AssertText(PngText text)
    {
        Assert.That(text.Keyword, Is.EqualTo("Comment"));
        Assert.That(text.Text, Is.EqualTo("Line\nCopyright \u00A9"));
    }

    static PngText CreateText()
    {
        PngText text = new PngText();
        text.Keyword = "Comment";
        text.Text = "Line\nCopyright \u00A9";
        return text;
    }

    static byte[] CreatePayload()
    {
        return new byte[]
        {
            67, 111, 109, 109, 101, 110, 116,
            0,
            76, 105, 110, 101, 10, 67, 111, 112, 121, 114, 105, 103, 104, 116, 32, 0xA9
        };
    }

    static byte[] CreateLongKeywordPayload()
    {
        byte[] data = new byte[81];
        int index = 0;
        while (index < 80)
        {
            data[index] = 65;
            index += 1;
        }

        data[80] = 0;
        return data;
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
