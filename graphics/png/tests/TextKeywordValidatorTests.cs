using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class TextKeywordValidatorTests
{
    [Test]
    public void IsValid_AcceptsRegisteredAndLatin1Keywords()
    {
        Assert.That(PngTextKeywordValidator.IsValid("Title"), Is.True);
        Assert.That(PngTextKeywordValidator.IsValid("Creation Time"), Is.True);
        Assert.That(PngTextKeywordValidator.IsValid("XML:com.adobe.xmp"), Is.True);
        Assert.That(PngTextKeywordValidator.IsValid("Latin \u00A1\u00FF"), Is.True);
        Assert.That(PngTextKeywordValidator.IsValid(new string('A', 79)), Is.True);
    }

    [Test]
    public void IsValid_RejectsInvalidKeywordStrings()
    {
        Assert.That(PngTextKeywordValidator.IsValid(""), Is.False);
        Assert.That(PngTextKeywordValidator.IsValid(new string('A', 80)), Is.False);
        Assert.That(PngTextKeywordValidator.IsValid(" Bad"), Is.False);
        Assert.That(PngTextKeywordValidator.IsValid("Bad "), Is.False);
        Assert.That(PngTextKeywordValidator.IsValid("Bad  Key"), Is.False);
        Assert.That(PngTextKeywordValidator.IsValid("Bad\u00A0Key"), Is.False);
        Assert.That(PngTextKeywordValidator.IsValid("Bad\0Key"), Is.False);
        Assert.That(PngTextKeywordValidator.IsValid("Bad\u001FKey"), Is.False);
        Assert.That(PngTextKeywordValidator.IsValid("Bad\u007FKey"), Is.False);
        Assert.That(PngTextKeywordValidator.IsValid("Bad\u0080Key"), Is.False);
        Assert.That(PngTextKeywordValidator.IsValid("Bad\u0100Key"), Is.False);
    }

    [Test]
    public void IsValidBytes_ValidatesOffsetLengthAndLatin1Ranges()
    {
        Assert.That(PngTextKeywordValidator.IsValidBytes(new byte[] { 99, 65, 32, 0xA1, 88 }, 1, 3), Is.True);
        Assert.That(PngTextKeywordValidator.IsValidBytes(new byte[] { 32, 65 }, 0, 2), Is.False);
        Assert.That(PngTextKeywordValidator.IsValidBytes(new byte[] { 65, 32 }, 0, 2), Is.False);
        Assert.That(PngTextKeywordValidator.IsValidBytes(new byte[] { 65, 32, 32, 66 }, 0, 4), Is.False);
        Assert.That(PngTextKeywordValidator.IsValidBytes(new byte[] { 65, 0xA0, 66 }, 0, 3), Is.False);
        Assert.That(PngTextKeywordValidator.IsValidBytes(new byte[] { 65, 0xFF }, 0, 2), Is.True);
    }

    [Test]
    public void Validate_RejectsNullAndInvalidRanges()
    {
        Assert.Throws<ArgumentNullException>(() => PngTextKeywordValidator.IsValid(null!));
        Assert.Throws<ArgumentNullException>(() => PngTextKeywordValidator.IsValidBytes(null!, 0, 0));
        Assert.Throws<ArgumentNullException>(() => PngTextKeywordValidator.Validate(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTextKeywordValidator.Validate(" Bad"));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTextKeywordValidator.IsValidBytes(new byte[] { 65 }, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTextKeywordValidator.IsValidBytes(new byte[] { 65 }, 2, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTextKeywordValidator.IsValidBytes(new byte[] { 65 }, 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTextKeywordValidator.IsValidBytes(new byte[] { 65 }, 1, 1));
    }

    [Test]
    public void TextChunkParsers_RejectInvalidKeywordRules()
    {
        Assert.Throws<InvalidDataException>(() => PngTextParser.Parse(CreateTextPayload(" Bad")));
        Assert.Throws<InvalidDataException>(() => PngCompressedTextParser.Parse(CreateCompressedTextPayload("Bad  Key")));
        Assert.Throws<InvalidDataException>(() => PngInternationalTextParser.Parse(CreateInternationalTextPayload("Bad\u00A0Key")));
    }

    [Test]
    public void TextChunkWriters_RejectInvalidKeywordRules()
    {
        PngText text = new PngText();
        text.Keyword = "Bad ";
        text.Text = "Text";
        PngCompressedText compressed = new PngCompressedText();
        compressed.Keyword = "Bad  Key";
        compressed.CompressionMethod = PngCompressedTextParser.CompressionMethodDeflate;
        compressed.Text = "Text";
        PngInternationalText international = new PngInternationalText();
        international.Keyword = "\u00A0Bad";
        international.CompressionFlag = PngInternationalTextParser.CompressionFlagUncompressed;
        international.CompressionMethod = PngInternationalTextParser.CompressionMethodDeflate;
        international.LanguageTag = "";
        international.TranslatedKeyword = "";
        international.Text = "Text";

        Assert.Throws<ArgumentOutOfRangeException>(() => PngTextWriter.WriteData(text));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngCompressedTextWriter.WriteData(compressed));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngInternationalTextWriter.WriteData(international));
    }

    static byte[] CreateTextPayload(string keyword)
    {
        byte[] keywordBytes = EncodeLatin1(keyword);
        byte[] data = new byte[keywordBytes.Length + 2];
        CopyBytes(keywordBytes, 0, data, 0, keywordBytes.Length);
        data[keywordBytes.Length] = 0;
        data[keywordBytes.Length + 1] = 65;
        return data;
    }

    static byte[] CreateCompressedTextPayload(string keyword)
    {
        byte[] keywordBytes = EncodeLatin1(keyword);
        byte[] compressed = PngZlibCompressor.Compress(new byte[] { 65 });
        byte[] data = new byte[keywordBytes.Length + 2 + compressed.Length];
        CopyBytes(keywordBytes, 0, data, 0, keywordBytes.Length);
        data[keywordBytes.Length] = 0;
        data[keywordBytes.Length + 1] = 0;
        CopyBytes(compressed, 0, data, keywordBytes.Length + 2, compressed.Length);
        return data;
    }

    static byte[] CreateInternationalTextPayload(string keyword)
    {
        byte[] keywordBytes = EncodeLatin1(keyword);
        byte[] data = new byte[keywordBytes.Length + 5];
        CopyBytes(keywordBytes, 0, data, 0, keywordBytes.Length);
        data[keywordBytes.Length] = 0;
        data[keywordBytes.Length + 1] = 0;
        data[keywordBytes.Length + 2] = 0;
        data[keywordBytes.Length + 3] = 0;
        data[keywordBytes.Length + 4] = 0;
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
