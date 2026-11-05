using System.IO;
using System.Text;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class InternationalTextTests
{
    [Test]
    public void Parse_ReadsUncompressedUtf8TextUsingOffsetAndIgnoresMethod()
    {
        byte[] payload = CreatePayload("Comment", 0, 99, "es", "Comentario", "Ni\u00F1o \u2603\n");
        byte[] data = WrapPayload(payload);

        PngInternationalText text = PngInternationalTextParser.Parse(data, 1, payload.Length);

        Assert.That(text.Keyword, Is.EqualTo("Comment"));
        Assert.That(text.CompressionFlag, Is.EqualTo(0));
        Assert.That(text.CompressionMethod, Is.EqualTo(99));
        Assert.That(text.LanguageTag, Is.EqualTo("es"));
        Assert.That(text.TranslatedKeyword, Is.EqualTo("Comentario"));
        Assert.That(text.Text, Is.EqualTo("Ni\u00F1o \u2603\n"));
        Assert.That(text.CompressedText, Is.Empty);
    }

    [Test]
    public void Parse_ReadsCompressedUtf8TextUsingOffset()
    {
        byte[] payload = CreatePayload("Comment", 1, 0, "ja", "\u30B3\u30E1\u30F3\u30C8", "\u96EA");
        byte[] data = WrapPayload(payload);

        PngInternationalText text = PngInternationalTextParser.Parse(data, 1, payload.Length);

        Assert.That(text.Keyword, Is.EqualTo("Comment"));
        Assert.That(text.CompressionFlag, Is.EqualTo(1));
        Assert.That(text.CompressionMethod, Is.EqualTo(0));
        Assert.That(text.LanguageTag, Is.EqualTo("ja"));
        Assert.That(text.TranslatedKeyword, Is.EqualTo("\u30B3\u30E1\u30F3\u30C8"));
        Assert.That(text.Text, Is.EqualTo("\u96EA"));
        Assert.That(text.CompressedText, Is.EqualTo(GetStoredText(payload)));
    }

    [Test]
    public void Parse_AllowsEmptyLanguageTranslatedKeywordAndText()
    {
        PngInternationalText text = PngInternationalTextParser.Parse(CreatePayload("Title", 0, 0, "", "", ""));

        Assert.That(text.Keyword, Is.EqualTo("Title"));
        Assert.That(text.LanguageTag, Is.EqualTo(""));
        Assert.That(text.TranslatedKeyword, Is.EqualTo(""));
        Assert.That(text.Text, Is.EqualTo(""));
    }

    [Test]
    public void Parse_ReadsChunkPayloadAndEnforcesDecompressedLimit()
    {
        PngChunk chunk = CreateChunk(PngConstants.ChunkInternationalText, CreatePayload("Comment", 1, 0, "en", "Comment", "12345"));
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxDecompressedBytes = 4;

        PngInternationalText text = PngInternationalTextParser.Parse(chunk);

        Assert.That(text.Text, Is.EqualTo("12345"));
        Assert.Throws<OverflowException>(() => PngInternationalTextParser.Parse(chunk, limits));
    }

    [Test]
    public void WriteData_WritesUncompressedUtf8Text()
    {
        PngInternationalText input = CreateText(0);

        byte[] data = PngInternationalTextWriter.WriteData(input);
        PngInternationalText parsed = PngInternationalTextParser.Parse(data);

        AssertInternationalText(parsed, 0);
        Assert.That(data[7], Is.EqualTo(0));
        Assert.That(data[8], Is.EqualTo(0));
        Assert.That(data[9], Is.EqualTo(0));
    }

    [Test]
    public void WriteData_WritesCompressedUtf8Text()
    {
        PngInternationalText input = CreateText(1);

        byte[] data = PngInternationalTextWriter.WriteData(input);
        PngInternationalText parsed = PngInternationalTextParser.Parse(data);

        AssertInternationalText(parsed, 1);
        Assert.That(data[7], Is.EqualTo(0));
        Assert.That(data[8], Is.EqualTo(1));
        Assert.That(data[9], Is.EqualTo(0));
        Assert.That(parsed.CompressedText, Is.Not.Empty);
    }

    [Test]
    public void WriteData_WritesAtOffset()
    {
        PngInternationalText input = CreateText(1);
        byte[] payload = PngInternationalTextWriter.WriteData(input);
        byte[] data = new byte[payload.Length + 2];
        data[0] = 99;
        data[data.Length - 1] = 88;

        PngInternationalTextWriter.WriteData(data, 1, input);
        PngInternationalText parsed = PngInternationalTextParser.Parse(data, 1, payload.Length);

        Assert.That(data[0], Is.EqualTo(99));
        Assert.That(data[data.Length - 1], Is.EqualTo(88));
        AssertInternationalText(parsed, 1);
    }

    [Test]
    public void WriteChunk_CreatesKnownInternationalTextChunk()
    {
        PngChunk chunk = PngInternationalTextWriter.WriteChunk(CreateText(0));

        Assert.That(chunk.Type, Is.EqualTo(PngConstants.ChunkInternationalText));
        Assert.That(chunk.Length, Is.EqualTo(chunk.Data.Length));
        Assert.That(chunk.IsKnown, Is.True);
        Assert.That(chunk.IsCritical, Is.False);
        Assert.That(chunk.IsPublic, Is.True);
        Assert.That(chunk.IsReservedBitValid, Is.True);
        Assert.That(chunk.IsSafeToCopy, Is.True);
        AssertInternationalText(PngInternationalTextParser.Parse(chunk), 0);
    }

    [Test]
    public void WriteChunk_RoundTripsThroughRawChunkWriter()
    {
        PngChunk chunk = PngInternationalTextWriter.WriteChunk(CreateText(1));

        byte[] rawChunk = PngRawChunkWriter.WriteToBytes(chunk);
        PngChunk readChunk = PngRawChunkReader.Read(rawChunk, 0);
        PngInternationalText text = PngInternationalTextParser.Parse(readChunk);

        AssertInternationalText(text, 1);
        Assert.That(readChunk.Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc(PngConstants.ChunkInternationalText, readChunk.Data)));
    }

    [Test]
    public void Parser_RejectsInvalidInputs()
    {
        byte[] valid = CreatePayload("Comment", 0, 0, "en", "Comment", "Text");
        PngChunk nullType = new PngChunk();
        nullType.Type = null!;
        nullType.Data = valid;
        PngChunk nullData = new PngChunk();
        nullData.Type = PngConstants.ChunkInternationalText;
        nullData.Data = null!;
        PngChunk wrongType = CreateChunk(PngConstants.ChunkText, valid);

        Assert.Throws<ArgumentNullException>(() => PngInternationalTextParser.Parse((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngInternationalTextParser.Parse(null!, 0, 1));
        Assert.Throws<ArgumentNullException>(() => PngInternationalTextParser.Parse(valid, null!));
        Assert.Throws<ArgumentNullException>(() => PngInternationalTextParser.Parse(valid, 0, valid.Length, null!));
        Assert.Throws<ArgumentNullException>(() => PngInternationalTextParser.Parse((PngChunk)null!));
        Assert.Throws<ArgumentNullException>(() => PngInternationalTextParser.Parse(nullType));
        Assert.Throws<ArgumentNullException>(() => PngInternationalTextParser.Parse(nullData));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngInternationalTextParser.Parse(valid, -1, valid.Length));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngInternationalTextParser.Parse(valid, valid.Length + 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngInternationalTextParser.Parse(valid, 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngInternationalTextParser.Parse(valid, 1, valid.Length));
        Assert.Throws<InvalidDataException>(() => PngInternationalTextParser.Parse(new byte[] { 65, 66 }));
        Assert.Throws<InvalidDataException>(() => PngInternationalTextParser.Parse(new byte[] { 0, 0, 0, 0, 0 }));
        Assert.Throws<InvalidDataException>(() => PngInternationalTextParser.Parse(CreateLongKeywordPayload()));
        Assert.Throws<InvalidDataException>(() => PngInternationalTextParser.Parse(new byte[] { 65, 0 }));
        Assert.Throws<InvalidDataException>(() => PngInternationalTextParser.Parse(new byte[] { 65, 0, 0 }));
        Assert.Throws<InvalidDataException>(() => PngInternationalTextParser.Parse(new byte[] { 65, 0, 2, 0, 0, 0 }));
        Assert.Throws<InvalidDataException>(() => PngInternationalTextParser.Parse(new byte[] { 65, 0, 1, 1, 0, 0 }));
        Assert.Throws<InvalidDataException>(() => PngInternationalTextParser.Parse(new byte[] { 65, 0, 0, 0, 65 }));
        Assert.Throws<InvalidDataException>(() => PngInternationalTextParser.Parse(new byte[] { 65, 0, 0, 0, 0, 65 }));
        Assert.Throws<InvalidDataException>(() => PngInternationalTextParser.Parse(CreatePayloadWithLanguageTagBytes(new byte[] { 0xFF })));
        Assert.Throws<InvalidDataException>(() => PngInternationalTextParser.Parse(CreatePayloadWithTextBytes(0, new byte[] { 65, 0, 66 })));
        Assert.Throws<InvalidDataException>(() => PngInternationalTextParser.Parse(CreatePayloadWithTextBytes(1, new byte[] { 1, 2, 3 })));
        Assert.Throws<DecoderFallbackException>(() => PngInternationalTextParser.Parse(CreatePayloadWithTranslatedKeywordBytes(new byte[] { 0xFF })));
        Assert.Throws<DecoderFallbackException>(() => PngInternationalTextParser.Parse(CreatePayloadWithTextBytes(0, new byte[] { 0xFF })));
        Assert.Throws<InvalidDataException>(() => PngInternationalTextParser.Parse(wrongType));
    }

    [Test]
    public void Writer_RejectsInvalidInputs()
    {
        PngInternationalText text = CreateText(0);
        PngInternationalText nullKeyword = CreateText(0);
        nullKeyword.Keyword = null!;
        PngInternationalText nullLanguage = CreateText(0);
        nullLanguage.LanguageTag = null!;
        PngInternationalText nullTranslated = CreateText(0);
        nullTranslated.TranslatedKeyword = null!;
        PngInternationalText nullText = CreateText(0);
        nullText.Text = null!;
        PngInternationalText emptyKeyword = CreateText(0);
        emptyKeyword.Keyword = "";
        PngInternationalText longKeyword = CreateText(0);
        longKeyword.Keyword = new string('A', 80);
        PngInternationalText nullInKeyword = CreateText(0);
        nullInKeyword.Keyword = "A\0B";
        PngInternationalText nonLatin1Keyword = CreateText(0);
        nonLatin1Keyword.Keyword = "A\u0100";
        PngInternationalText invalidFlag = CreateText(0);
        invalidFlag.CompressionFlag = 2;
        PngInternationalText invalidMethod = CreateText(0);
        invalidMethod.CompressionMethod = 1;
        PngInternationalText nullInLanguage = CreateText(0);
        nullInLanguage.LanguageTag = "en\0US";
        PngInternationalText nonAsciiLanguage = CreateText(0);
        nonAsciiLanguage.LanguageTag = "en-\u00F1";
        PngInternationalText nullInTranslated = CreateText(0);
        nullInTranslated.TranslatedKeyword = "A\0B";
        PngInternationalText nullInText = CreateText(0);
        nullInText.Text = "A\0B";
        byte[] payload = PngInternationalTextWriter.WriteData(text);

        Assert.Throws<ArgumentNullException>(() => PngInternationalTextWriter.WriteData(null!));
        Assert.Throws<ArgumentNullException>(() => PngInternationalTextWriter.WriteData(null!, new PngSecurityLimits()));
        Assert.Throws<ArgumentNullException>(() => PngInternationalTextWriter.WriteData(text, null!));
        Assert.Throws<ArgumentNullException>(() => PngInternationalTextWriter.WriteData(null!, 0, text));
        Assert.Throws<ArgumentNullException>(() => PngInternationalTextWriter.WriteData(new byte[payload.Length], 0, null!));
        Assert.Throws<ArgumentNullException>(() => PngInternationalTextWriter.WriteData(new byte[payload.Length], 0, text, null!));
        Assert.Throws<ArgumentNullException>(() => PngInternationalTextWriter.WriteChunk(null!));
        Assert.Throws<ArgumentNullException>(() => PngInternationalTextWriter.WriteChunk(text, null!));
        Assert.Throws<ArgumentNullException>(() => PngInternationalTextWriter.WriteData(nullKeyword));
        Assert.Throws<ArgumentNullException>(() => PngInternationalTextWriter.WriteData(nullLanguage));
        Assert.Throws<ArgumentNullException>(() => PngInternationalTextWriter.WriteData(nullTranslated));
        Assert.Throws<ArgumentNullException>(() => PngInternationalTextWriter.WriteData(nullText));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngInternationalTextWriter.WriteData(emptyKeyword));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngInternationalTextWriter.WriteData(longKeyword));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngInternationalTextWriter.WriteData(nullInKeyword));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngInternationalTextWriter.WriteData(nonLatin1Keyword));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngInternationalTextWriter.WriteData(invalidFlag));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngInternationalTextWriter.WriteData(invalidMethod));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngInternationalTextWriter.WriteData(nullInLanguage));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngInternationalTextWriter.WriteData(nonAsciiLanguage));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngInternationalTextWriter.WriteData(nullInTranslated));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngInternationalTextWriter.WriteData(nullInText));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngInternationalTextWriter.WriteData(new byte[payload.Length], -1, text));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngInternationalTextWriter.WriteData(new byte[payload.Length], 1, text));
    }

    static void AssertInternationalText(PngInternationalText text, int compressionFlag)
    {
        Assert.That(text.Keyword, Is.EqualTo("Comment"));
        Assert.That(text.CompressionFlag, Is.EqualTo(compressionFlag));
        Assert.That(text.CompressionMethod, Is.EqualTo(0));
        Assert.That(text.LanguageTag, Is.EqualTo("fr"));
        Assert.That(text.TranslatedKeyword, Is.EqualTo("Commentaire"));
        Assert.That(text.Text, Is.EqualTo("Cr\u00E8me \u2603"));
        if (compressionFlag == 1)
        {
            Assert.That(text.CompressedText, Is.Not.Empty);
        }
        else
        {
            Assert.That(text.CompressedText, Is.Empty);
        }
    }

    static PngInternationalText CreateText(int compressionFlag)
    {
        PngInternationalText text = new PngInternationalText();
        text.Keyword = "Comment";
        text.CompressionFlag = (byte)compressionFlag;
        text.CompressionMethod = PngInternationalTextParser.CompressionMethodDeflate;
        text.LanguageTag = "fr";
        text.TranslatedKeyword = "Commentaire";
        text.Text = "Cr\u00E8me \u2603";
        return text;
    }

    static byte[] CreatePayload(string keyword, int compressionFlag, int compressionMethod, string languageTag, string translatedKeyword, string text)
    {
        byte[] textBytes = EncodeUtf8(text);
        if (compressionFlag == 1)
        {
            textBytes = PngZlibCompressor.Compress(textBytes);
        }

        return CreatePayloadFromFields(EncodeLatin1(keyword), (byte)compressionFlag, (byte)compressionMethod, EncodeAscii(languageTag), EncodeUtf8(translatedKeyword), textBytes);
    }

    static byte[] CreatePayloadWithLanguageTagBytes(byte[] languageTag)
    {
        return CreatePayloadFromFields(EncodeLatin1("A"), 0, 0, languageTag, new byte[0], new byte[0]);
    }

    static byte[] CreatePayloadWithTranslatedKeywordBytes(byte[] translatedKeyword)
    {
        return CreatePayloadFromFields(EncodeLatin1("A"), 0, 0, new byte[0], translatedKeyword, new byte[0]);
    }

    static byte[] CreatePayloadWithTextBytes(int compressionFlag, byte[] text)
    {
        byte[] storedText = text;
        if (compressionFlag == 1 && text.Length != 3)
        {
            storedText = PngZlibCompressor.Compress(text);
        }

        return CreatePayloadFromFields(EncodeLatin1("A"), (byte)compressionFlag, 0, new byte[0], new byte[0], storedText);
    }

    static byte[] CreatePayloadFromFields(byte[] keyword, byte compressionFlag, byte compressionMethod, byte[] languageTag, byte[] translatedKeyword, byte[] text)
    {
        byte[] data = new byte[keyword.Length + 5 + languageTag.Length + translatedKeyword.Length + text.Length];
        int offset = 0;
        CopyBytes(keyword, 0, data, offset, keyword.Length);
        offset += keyword.Length;
        data[offset] = 0;
        offset += 1;
        data[offset] = compressionFlag;
        offset += 1;
        data[offset] = compressionMethod;
        offset += 1;
        CopyBytes(languageTag, 0, data, offset, languageTag.Length);
        offset += languageTag.Length;
        data[offset] = 0;
        offset += 1;
        CopyBytes(translatedKeyword, 0, data, offset, translatedKeyword.Length);
        offset += translatedKeyword.Length;
        data[offset] = 0;
        offset += 1;
        CopyBytes(text, 0, data, offset, text.Length);
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

    static byte[] GetStoredText(byte[] payload)
    {
        int offset = 0;
        while (payload[offset] != 0)
        {
            offset += 1;
        }

        offset += 3;
        while (payload[offset] != 0)
        {
            offset += 1;
        }

        offset += 1;
        while (payload[offset] != 0)
        {
            offset += 1;
        }

        offset += 1;
        byte[] storedText = new byte[payload.Length - offset];
        CopyBytes(payload, offset, storedText, 0, storedText.Length);
        return storedText;
    }

    static byte[] CreateLongKeywordPayload()
    {
        byte[] keyword = new byte[80];
        int index = 0;
        while (index < keyword.Length)
        {
            keyword[index] = 65;
            index += 1;
        }

        return CreatePayloadFromFields(keyword, 0, 0, new byte[0], new byte[0], new byte[0]);
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

    static byte[] EncodeAscii(string text)
    {
        return EncodeLatin1(text);
    }

    static byte[] EncodeUtf8(string text)
    {
        UTF8Encoding encoding = new UTF8Encoding(false, true);
        return encoding.GetBytes(text);
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
