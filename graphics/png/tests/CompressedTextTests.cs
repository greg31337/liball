using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class CompressedTextTests
{
    [Test]
    public void Parse_DecompressesLatin1TextUsingOffset()
    {
        byte[] payload = CreatePayload("Comment", "Jalape\u00F1o\n");
        byte[] data = WrapPayload(payload);

        PngCompressedText text = PngCompressedTextParser.Parse(data, 1, payload.Length);

        Assert.That(text.Keyword, Is.EqualTo("Comment"));
        Assert.That(text.CompressionMethod, Is.EqualTo(0));
        Assert.That(text.Text, Is.EqualTo("Jalape\u00F1o\n"));
        Assert.That(text.CompressedText, Is.EqualTo(GetCompressedText(payload)));
    }

    [Test]
    public void Parse_AllowsEmptyTextString()
    {
        PngCompressedText text = PngCompressedTextParser.Parse(CreatePayload("Title", ""));

        Assert.That(text.Keyword, Is.EqualTo("Title"));
        Assert.That(text.Text, Is.EqualTo(""));
        Assert.That(text.CompressedText, Is.Empty);
    }

    [Test]
    public void Parse_ReadsChunkPayload()
    {
        PngChunk chunk = CreateChunk(PngConstants.ChunkCompressedText, CreatePayload("Comment", "Compressed line"));

        PngCompressedText text = PngCompressedTextParser.Parse(chunk);

        Assert.That(text.Keyword, Is.EqualTo("Comment"));
        Assert.That(text.Text, Is.EqualTo("Compressed line"));
    }

    [Test]
    public void Parse_EnforcesDecompressedByteLimit()
    {
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxDecompressedBytes = 4;
        PngChunk chunk = CreateChunk(PngConstants.ChunkCompressedText, CreatePayload("Comment", "12345"));

        Assert.Throws<OverflowException>(() => PngCompressedTextParser.Parse(chunk, limits));
    }

    [Test]
    public void WriteData_CompressesLatin1Text()
    {
        PngCompressedText input = CreateText();

        byte[] data = PngCompressedTextWriter.WriteData(input);
        PngCompressedText parsed = PngCompressedTextParser.Parse(data);

        AssertCompressedText(parsed);
        Assert.That(data[7], Is.EqualTo(0));
        Assert.That(data[8], Is.EqualTo(0));
        Assert.That(parsed.CompressedText, Is.Not.Empty);
    }

    [Test]
    public void WriteData_WritesAtOffset()
    {
        PngCompressedText input = CreateText();
        byte[] payload = PngCompressedTextWriter.WriteData(input);
        byte[] data = new byte[payload.Length + 2];
        data[0] = 99;
        data[data.Length - 1] = 88;

        PngCompressedTextWriter.WriteData(data, 1, input);
        PngCompressedText parsed = PngCompressedTextParser.Parse(data, 1, payload.Length);

        Assert.That(data[0], Is.EqualTo(99));
        Assert.That(data[data.Length - 1], Is.EqualTo(88));
        AssertCompressedText(parsed);
    }

    [Test]
    public void WriteChunk_CreatesKnownCompressedTextChunk()
    {
        PngChunk chunk = PngCompressedTextWriter.WriteChunk(CreateText());

        Assert.That(chunk.Type, Is.EqualTo(PngConstants.ChunkCompressedText));
        Assert.That(chunk.Length, Is.EqualTo(chunk.Data.Length));
        Assert.That(chunk.IsKnown, Is.True);
        Assert.That(chunk.IsCritical, Is.False);
        Assert.That(chunk.IsPublic, Is.True);
        Assert.That(chunk.IsReservedBitValid, Is.True);
        Assert.That(chunk.IsSafeToCopy, Is.True);
        AssertCompressedText(PngCompressedTextParser.Parse(chunk));
    }

    [Test]
    public void WriteChunk_RoundTripsThroughRawChunkWriter()
    {
        PngChunk chunk = PngCompressedTextWriter.WriteChunk(CreateText());

        byte[] rawChunk = PngRawChunkWriter.WriteToBytes(chunk);
        PngChunk readChunk = PngRawChunkReader.Read(rawChunk, 0);
        PngCompressedText text = PngCompressedTextParser.Parse(readChunk);

        AssertCompressedText(text);
        Assert.That(readChunk.Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc(PngConstants.ChunkCompressedText, readChunk.Data)));
    }

    [Test]
    public void Parser_RejectsInvalidInputs()
    {
        PngChunk nullType = new PngChunk();
        nullType.Type = null!;
        nullType.Data = CreatePayload("Comment", "Text");
        PngChunk nullData = new PngChunk();
        nullData.Type = PngConstants.ChunkCompressedText;
        nullData.Data = null!;
        PngChunk wrongType = CreateChunk(PngConstants.ChunkText, CreatePayload("Comment", "Text"));
        byte[] valid = CreatePayload("Comment", "Text");

        Assert.Throws<ArgumentNullException>(() => PngCompressedTextParser.Parse((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngCompressedTextParser.Parse(null!, 0, 1));
        Assert.Throws<ArgumentNullException>(() => PngCompressedTextParser.Parse(valid, null!));
        Assert.Throws<ArgumentNullException>(() => PngCompressedTextParser.Parse(valid, 0, valid.Length, null!));
        Assert.Throws<ArgumentNullException>(() => PngCompressedTextParser.Parse((PngChunk)null!));
        Assert.Throws<ArgumentNullException>(() => PngCompressedTextParser.Parse(nullType));
        Assert.Throws<ArgumentNullException>(() => PngCompressedTextParser.Parse(nullData));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngCompressedTextParser.Parse(valid, -1, valid.Length));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngCompressedTextParser.Parse(valid, valid.Length + 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngCompressedTextParser.Parse(valid, 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngCompressedTextParser.Parse(valid, 1, valid.Length));
        Assert.Throws<InvalidDataException>(() => PngCompressedTextParser.Parse(new byte[] { 65, 66 }));
        Assert.Throws<InvalidDataException>(() => PngCompressedTextParser.Parse(new byte[] { 0, 0, 1, 2 }));
        Assert.Throws<InvalidDataException>(() => PngCompressedTextParser.Parse(CreateLongKeywordPayload()));
        Assert.Throws<InvalidDataException>(() => PngCompressedTextParser.Parse(new byte[] { 65, 0 }));
        Assert.Throws<InvalidDataException>(() => PngCompressedTextParser.Parse(new byte[] { 65, 0, 1, 1, 2, 3 }));
        Assert.Throws<InvalidDataException>(() => PngCompressedTextParser.Parse(new byte[] { 65, 0, 0, 1, 2, 3 }));
        Assert.Throws<InvalidDataException>(() => PngCompressedTextParser.Parse(CreatePayloadFromCompressedBytes("A", PngZlibCompressor.Compress(new byte[] { 65, 0, 66 }))));
        Assert.Throws<InvalidDataException>(() => PngCompressedTextParser.Parse(wrongType));
    }

    [Test]
    public void Writer_RejectsInvalidInputs()
    {
        PngCompressedText text = CreateText();
        PngCompressedText nullKeyword = CreateText();
        nullKeyword.Keyword = null!;
        PngCompressedText nullText = CreateText();
        nullText.Text = null!;
        PngCompressedText emptyKeyword = CreateText();
        emptyKeyword.Keyword = "";
        PngCompressedText longKeyword = CreateText();
        longKeyword.Keyword = new string('A', 80);
        PngCompressedText nullInKeyword = CreateText();
        nullInKeyword.Keyword = "A\0B";
        PngCompressedText nullInText = CreateText();
        nullInText.Text = "A\0B";
        PngCompressedText nonLatin1Keyword = CreateText();
        nonLatin1Keyword.Keyword = "A\u0100";
        PngCompressedText nonLatin1Text = CreateText();
        nonLatin1Text.Text = "A\u0100";
        PngCompressedText invalidMethod = CreateText();
        invalidMethod.CompressionMethod = 1;
        byte[] payload = PngCompressedTextWriter.WriteData(text);

        Assert.Throws<ArgumentNullException>(() => PngCompressedTextWriter.WriteData(null!));
        Assert.Throws<ArgumentNullException>(() => PngCompressedTextWriter.WriteData(null!, new PngSecurityLimits()));
        Assert.Throws<ArgumentNullException>(() => PngCompressedTextWriter.WriteData(text, null!));
        Assert.Throws<ArgumentNullException>(() => PngCompressedTextWriter.WriteData(null!, 0, text));
        Assert.Throws<ArgumentNullException>(() => PngCompressedTextWriter.WriteData(new byte[payload.Length], 0, null!));
        Assert.Throws<ArgumentNullException>(() => PngCompressedTextWriter.WriteData(new byte[payload.Length], 0, text, null!));
        Assert.Throws<ArgumentNullException>(() => PngCompressedTextWriter.WriteChunk(null!));
        Assert.Throws<ArgumentNullException>(() => PngCompressedTextWriter.WriteChunk(text, null!));
        Assert.Throws<ArgumentNullException>(() => PngCompressedTextWriter.WriteData(nullKeyword));
        Assert.Throws<ArgumentNullException>(() => PngCompressedTextWriter.WriteData(nullText));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngCompressedTextWriter.WriteData(emptyKeyword));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngCompressedTextWriter.WriteData(longKeyword));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngCompressedTextWriter.WriteData(nullInKeyword));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngCompressedTextWriter.WriteData(nullInText));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngCompressedTextWriter.WriteData(nonLatin1Keyword));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngCompressedTextWriter.WriteData(nonLatin1Text));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngCompressedTextWriter.WriteData(invalidMethod));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngCompressedTextWriter.WriteData(new byte[payload.Length], -1, text));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngCompressedTextWriter.WriteData(new byte[payload.Length], 1, text));
    }

    static void AssertCompressedText(PngCompressedText text)
    {
        Assert.That(text.Keyword, Is.EqualTo("Comment"));
        Assert.That(text.CompressionMethod, Is.EqualTo(0));
        Assert.That(text.Text, Is.EqualTo("Line\nCopyright \u00A9"));
        Assert.That(text.CompressedText, Is.Not.Empty);
    }

    static PngCompressedText CreateText()
    {
        PngCompressedText text = new PngCompressedText();
        text.Keyword = "Comment";
        text.CompressionMethod = PngCompressedTextParser.CompressionMethodDeflate;
        text.Text = "Line\nCopyright \u00A9";
        return text;
    }

    static byte[] CreatePayload(string keyword, string text)
    {
        return CreatePayloadFromCompressedBytes(keyword, PngZlibCompressor.Compress(EncodeLatin1(text)));
    }

    static byte[] CreatePayloadFromCompressedBytes(string keyword, byte[] compressed)
    {
        byte[] keywordBytes = EncodeLatin1(keyword);
        byte[] data = new byte[keywordBytes.Length + 2 + compressed.Length];
        CopyBytes(keywordBytes, 0, data, 0, keywordBytes.Length);
        data[keywordBytes.Length] = 0;
        data[keywordBytes.Length + 1] = 0;
        CopyBytes(compressed, 0, data, keywordBytes.Length + 2, compressed.Length);
        return data;
    }

    static byte[] WrapPayload(byte[] payload)
    {
        byte[] data = new byte[payload.Length + 2];
        data[0] = 99;
        CopyBytes(payload, 0, data, 1, payload.Length);
        data[data.Length - 1] = 88;
        return data;
    }

    static byte[] GetCompressedText(byte[] payload)
    {
        int offset = 0;
        while (payload[offset] != 0)
        {
            offset += 1;
        }

        offset += 2;
        byte[] compressed = new byte[payload.Length - offset];
        CopyBytes(payload, offset, compressed, 0, compressed.Length);
        return compressed;
    }

    static byte[] CreateLongKeywordPayload()
    {
        byte[] compressed = PngZlibCompressor.Compress(new byte[] { 65 });
        byte[] data = new byte[82 + compressed.Length];
        int index = 0;
        while (index < 80)
        {
            data[index] = 65;
            index += 1;
        }

        data[80] = 0;
        data[81] = 0;
        CopyBytes(compressed, 0, data, 82, compressed.Length);
        return data;
    }

    static byte[] EncodeLatin1(string text)
    {
        byte[] data = new byte[text.Length];
        int index = 0;
        while (index < text.Length)
        {
            data[index] = (byte)text[index];
            index += 1;
        }

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

    static void CopyBytes(byte[] source, int sourceOffset, byte[] destination, int destinationOffset, int length)
    {
        int index = 0;
        while (index < length)
        {
            destination[destinationOffset + index] = source[sourceOffset + index];
            index += 1;
        }
    }
}
