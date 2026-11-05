using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class TextCompressionLimitTests
{
    [Test]
    public void CompressedText_ParseAndWriteHonorExactDecompressedLimit()
    {
        PngCompressedText text = CreateCompressedText("Comment", "12345");
        PngChunk chunk = PngCompressedTextWriter.WriteChunk(text);
        PngSecurityLimits exact = new PngSecurityLimits();
        exact.MaxDecompressedBytes = 5;
        PngSecurityLimits tooSmall = new PngSecurityLimits();
        tooSmall.MaxDecompressedBytes = 4;

        PngCompressedText parsed = PngCompressedTextParser.Parse(chunk, exact);

        Assert.That(parsed.Text, Is.EqualTo("12345"));
        Assert.That(PngCompressedTextWriter.WriteData(text, exact), Is.Not.Empty);
        Assert.Throws<OverflowException>(() => PngCompressedTextParser.Parse(chunk, tooSmall));
        Assert.Throws<OverflowException>(() => PngCompressedTextWriter.WriteData(text, tooSmall));
    }

    [Test]
    public void InternationalText_ParseAndWriteHonorExactCompressedTextLimit()
    {
        PngInternationalText text = CreateInternationalText("Comment", "12345", PngInternationalTextParser.CompressionFlagCompressed);
        PngChunk chunk = PngInternationalTextWriter.WriteChunk(text);
        PngSecurityLimits exact = new PngSecurityLimits();
        exact.MaxDecompressedBytes = 5;
        PngSecurityLimits tooSmall = new PngSecurityLimits();
        tooSmall.MaxDecompressedBytes = 4;

        PngInternationalText parsed = PngInternationalTextParser.Parse(chunk, exact);

        Assert.That(parsed.Text, Is.EqualTo("12345"));
        Assert.That(PngInternationalTextWriter.WriteData(text, exact), Is.Not.Empty);
        Assert.Throws<OverflowException>(() => PngInternationalTextParser.Parse(chunk, tooSmall));
        Assert.Throws<OverflowException>(() => PngInternationalTextWriter.WriteData(text, tooSmall));
    }

    [Test]
    public void Identify_TextLimitsCountValidTextChunkPayloadsAcrossAllTextTypes()
    {
        PngChunk text = PngTextWriter.WriteChunk(CreateText("Title", "A"));
        PngChunk compressed = PngCompressedTextWriter.WriteChunk(CreateCompressedText("Comment", "Zip"));
        PngChunk international = PngInternationalTextWriter.WriteChunk(CreateInternationalText("Description", "Utf8", PngInternationalTextParser.CompressionFlagUncompressed));
        long textBytes = 8;
        byte[] png = CreatePng(text, compressed, international);
        PngDecoderOptions exact = new PngDecoderOptions();
        exact.Limits.MaxTextBytes = textBytes;
        exact.Limits.MaxTextEntries = 3;
        PngDecoderOptions tooFewBytes = new PngDecoderOptions();
        tooFewBytes.Limits.MaxTextBytes = textBytes - 1;
        PngDecoderOptions tooFewEntries = new PngDecoderOptions();
        tooFewEntries.Limits.MaxTextEntries = 2;

        PngIdentifyResult result = PngIdentifier.Identify(png, 0, exact);

        Assert.That(result.Metadata.TextBytes, Is.EqualTo(textBytes));
        Assert.That(result.Metadata.TextEntryCount, Is.EqualTo(3));
        Assert.Throws<OverflowException>(() => PngIdentifier.Identify(png, 0, tooFewBytes));
        Assert.Throws<OverflowException>(() => PngIdentifier.Identify(png, 0, tooFewEntries));
    }

    static PngText CreateText(string keyword, string value)
    {
        PngText text = new PngText();
        text.Keyword = keyword;
        text.Text = value;
        return text;
    }

    static PngCompressedText CreateCompressedText(string keyword, string value)
    {
        PngCompressedText text = new PngCompressedText();
        text.Keyword = keyword;
        text.CompressionMethod = PngCompressedTextParser.CompressionMethodDeflate;
        text.Text = value;
        return text;
    }

    static PngInternationalText CreateInternationalText(string keyword, string value, byte compressionFlag)
    {
        PngInternationalText text = new PngInternationalText();
        text.Keyword = keyword;
        text.CompressionFlag = compressionFlag;
        text.CompressionMethod = PngInternationalTextParser.CompressionMethodDeflate;
        text.LanguageTag = "en";
        text.TranslatedKeyword = keyword;
        text.Text = value;
        return text;
    }

    static byte[] CreatePng(params PngChunk[] textChunks)
    {
        byte[][] chunks = new byte[textChunks.Length + 2][];
        chunks[0] = PngRawChunkWriter.WriteToBytes(PngIhdrWriter.WriteChunk(CreateIhdr()));
        int index = 0;
        while (index < textChunks.Length)
        {
            chunks[index + 1] = PngRawChunkWriter.WriteToBytes(textChunks[index]);
            index += 1;
        }

        chunks[chunks.Length - 1] = PngRawChunkWriter.WriteToBytes(PngIendWriter.WriteChunk());
        return JoinWithSignature(chunks);
    }

    static byte[] JoinWithSignature(byte[][] chunks)
    {
        int length = PngConstants.SignatureLength;
        int index = 0;
        while (index < chunks.Length)
        {
            length += chunks[index].Length;
            index += 1;
        }

        byte[] png = new byte[length];
        png[0] = PngConstants.SignatureByte0;
        png[1] = PngConstants.SignatureByte1;
        png[2] = PngConstants.SignatureByte2;
        png[3] = PngConstants.SignatureByte3;
        png[4] = PngConstants.SignatureByte4;
        png[5] = PngConstants.SignatureByte5;
        png[6] = PngConstants.SignatureByte6;
        png[7] = PngConstants.SignatureByte7;

        int offset = PngConstants.SignatureLength;
        index = 0;
        while (index < chunks.Length)
        {
            CopyBytes(chunks[index], 0, png, offset, chunks[index].Length);
            offset += chunks[index].Length;
            index += 1;
        }

        return png;
    }

    static PngIhdr CreateIhdr()
    {
        PngIhdr ihdr = new PngIhdr();
        ihdr.Width = 1;
        ihdr.Height = 1;
        ihdr.ColorType = PngColorType.Truecolor;
        ihdr.BitDepth = PngBitDepth.Eight;
        ihdr.CompressionMethod = PngCompressionMethod.Deflate;
        ihdr.FilterMethod = PngFilterMethod.Adaptive;
        ihdr.InterlaceMethod = PngInterlaceMethod.None;
        return ihdr;
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
