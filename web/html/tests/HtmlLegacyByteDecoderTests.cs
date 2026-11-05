
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlLegacyByteDecoderTests
{
    [TestCase("cp866", 1073)]
    [TestCase("iso-8859-2", 260)]
    [TestCase("iso-8859-3", 294)]
    [TestCase("iso-8859-4", 260)]
    [TestCase("iso-8859-5", 1025)]
    [TestCase("iso-8859-6", -1)]
    [TestCase("iso-8859-7", 8216)]
    [TestCase("iso-8859-8", -1)]
    [TestCase("iso-8859-8-i", -1)]
    [TestCase("iso-8859-10", 260)]
    [TestCase("iso-8859-13", 8221)]
    [TestCase("iso-8859-14", 7682)]
    [TestCase("iso-8859-15", 161)]
    [TestCase("iso-8859-16", 260)]
    [TestCase("koi8-r", 9553)]
    [TestCase("koi8-u", 9553)]
    [TestCase("macintosh", 176)]
    [TestCase("windows-874", 3585)]
    [TestCase("windows-1250", 711)]
    [TestCase("windows-1251", 1038)]
    [TestCase("windows-1253", 901)]
    [TestCase("windows-1254", 161)]
    [TestCase("windows-1255", 161)]
    [TestCase("windows-1256", 1548)]
    [TestCase("windows-1257", -1)]
    [TestCase("windows-1258", 161)]
    [TestCase("x-mac-cyrillic", 176)]
    public void EveryRemainingSingleByteDecoderUsesItsWhatwgIndex(string label, int codePoint)
    {
        HtmlDecodedInput result = HtmlByteDecoder.Decode(new byte[] { 0x41, 0xA1 }, label);

        string expected = codePoint < 0 ? "A�" : "A" + char.ConvertFromUtf32(codePoint);
        Assert.That(result.IsSupported(), Is.True);
        Assert.That(result.GetString(), Is.EqualTo(expected));
        Assert.That(result.GetErrorCount(), Is.EqualTo(codePoint < 0 ? 1 : 0));
        if (codePoint < 0)
        {
            AssertError(result.GetErrors()[0], 1, 1);
        }
    }

    [Test]
    public void Gb18030DecodesTwoAndFourByteMappingsIncludingRangeBoundaries()
    {
        byte[] bytes =
        [
            0x81, 0x40,
            0x81, 0x30, 0x81, 0x30,
            0x81, 0x35, 0xF4, 0x37,
            0x90, 0x30, 0x81, 0x30,
            0xE3, 0x32, 0x9A, 0x35,
            0x84, 0x31, 0xA5, 0x30,
        ];

        HtmlDecodedInput result = HtmlByteDecoder.Decode(bytes, "gb18030");

        Assert.That(result.GetString(), Is.EqualTo("丂\u0080\uE7C7\U00010000\U0010FFFF�"));
        Assert.That(result.GetEncodingName(), Is.EqualTo("gb18030"));
        Assert.That(result.GetErrorCount(), Is.EqualTo(1));
        AssertError(result.GetErrors()[0], 18, 4);
        Assert.That(result.GetByteOffset(result.GetCharacterCount()), Is.EqualTo(bytes.Length));
    }

    [Test]
    public void GbkUsesTheGb18030DecoderAndRestoresMalformedAsciiBytes()
    {
        HtmlDecodedInput fourByte = HtmlByteDecoder.Decode(new byte[] { 0x81, 0x30, 0x81, 0x30 }, "gbk");
        HtmlDecodedInput malformed = HtmlByteDecoder.Decode(new byte[] { 0x81, 0x30, 0x20 }, "gbk");

        Assert.That(fourByte.GetString(), Is.EqualTo("\u0080"));
        Assert.That(fourByte.GetEncodingName(), Is.EqualTo("GBK"));
        Assert.That(malformed.GetString(), Is.EqualTo("�0 "));
        AssertError(malformed.GetErrors()[0], 0, 1);
    }

    [Test]
    public void Big5DecodesIndexAndSpecialTwoCodePointMappings()
    {
        HtmlDecodedInput result = HtmlByteDecoder.Decode(
            new byte[] { 0xA4, 0x40, 0x88, 0x62, 0x88, 0x64, 0x88, 0xA3, 0x88, 0xA5 }, "big5");

        Assert.That(result.GetString(), Is.EqualTo("一Ê\u0304Ê\u030Cê\u0304ê\u030C"));
        Assert.That(result.HadErrors(), Is.False);
        Assert.That(result.GetByteOffset(2), Is.EqualTo(2));
        Assert.That(result.GetCharacterOffset(3), Is.EqualTo(1));
    }

    [Test]
    public void Big5IndexCanEmitASupplementaryScalar()
    {
        HtmlDecodedInput result = HtmlByteDecoder.Decode(new byte[] { 0x87, 0x45 }, "big5");

        Assert.That(result.GetString(), Is.EqualTo("\U00027267"));
        Assert.That(result.GetCharacterCount(), Is.EqualTo(2));
        Assert.That(result.GetByteOffset(1), Is.Zero);
        Assert.That(result.GetByteOffset(2), Is.EqualTo(2));
    }

    [TestCase("big5", new byte[] { 0x81, 0x22 })]
    [TestCase("euc-jp", new byte[] { 0xA1, 0x22 })]
    [TestCase("shift_jis", new byte[] { 0x82, 0x22 })]
    [TestCase("euc-kr", new byte[] { 0x81, 0x22 })]
    public void MultibyteDecodersRestoreMalformedAsciiTrailingBytes(string label, byte[] bytes)
    {
        HtmlDecodedInput result = HtmlByteDecoder.Decode(bytes, label);

        Assert.That(result.GetString(), Is.EqualTo("�\""));
        AssertError(result.GetErrors()[0], 0, 1);
        Assert.That(result.GetCharacterOffset(1), Is.EqualTo(1));
    }

    [TestCase("gb18030", new byte[] { 0x81 }, 1)]
    [TestCase("big5", new byte[] { 0x81 }, 1)]
    [TestCase("euc-jp", new byte[] { 0x8F, 0xA1 }, 2)]
    [TestCase("shift_jis", new byte[] { 0x82 }, 1)]
    [TestCase("euc-kr", new byte[] { 0x81 }, 1)]
    public void IncompleteMultibyteSequencesBecomeOneReplacement(string label, byte[] bytes, int errorLength)
    {
        HtmlDecodedInput result = HtmlByteDecoder.Decode(bytes, label);

        Assert.That(result.GetString(), Is.EqualTo("�"));
        Assert.That(result.GetErrorCount(), Is.EqualTo(1));
        AssertError(result.GetErrors()[0], 0, errorLength);
    }

    [Test]
    public void EucJpDecodesJis0208KatakanaAndJis0212()
    {
        HtmlDecodedInput result = HtmlByteDecoder.Decode(
            new byte[] { 0xA1, 0xA1, 0x8E, 0xA1, 0x8F, 0xA2, 0xAF }, "euc-jp");

        Assert.That(result.GetString(), Is.EqualTo("\u3000\uFF61\u02D8"));
        Assert.That(result.HadErrors(), Is.False);
        Assert.That(result.GetByteOffset(3), Is.EqualTo(7));
    }

    [Test]
    public void ShiftJisDecodesJis0208KatakanaAndPrivateUseMappings()
    {
        HtmlDecodedInput result = HtmlByteDecoder.Decode(
            new byte[] { 0x81, 0x40, 0xA1, 0xF0, 0x40, 0x80 }, "windows-31j");

        Assert.That(result.GetString(), Is.EqualTo("\u3000\uFF61\uE000\u0080"));
        Assert.That(result.HadErrors(), Is.False);
    }

    [Test]
    public void EucKrDecodesIndexedHangul()
    {
        HtmlDecodedInput result = HtmlByteDecoder.Decode(new byte[] { 0xB0, 0xA1 }, "euc-kr");

        Assert.That(result.GetString(), Is.EqualTo("가"));
        Assert.That(result.HadErrors(), Is.False);
    }

    [Test]
    public void Iso2022JpDecodesAsciiRomanKatakanaAndJis0208States()
    {
        byte[] bytes =
        [
            0x1B, 0x28, 0x4A, 0x5C, 0x7E,
            0x1B, 0x28, 0x49, 0x21, 0x5F,
            0x1B, 0x24, 0x42, 0x21, 0x21,
            0x1B, 0x28, 0x42, 0x41,
        ];

        HtmlDecodedInput result = HtmlByteDecoder.Decode(bytes, "iso-2022-jp");

        Assert.That(result.GetString(), Is.EqualTo("¥‾\uFF61\uFF9F\u3000A"));
        Assert.That(result.HadErrors(), Is.False);
        Assert.That(result.GetByteOffset(0), Is.EqualTo(3));
        Assert.That(result.GetCharacterOffset(2), Is.Zero);
        Assert.That(result.GetCharacterOffset(bytes.Length), Is.EqualTo(result.GetCharacterCount()));
    }

    [Test]
    public void Iso2022JpReportsConsecutiveAndMalformedEscapeSequences()
    {
        HtmlDecodedInput consecutive = HtmlByteDecoder.Decode(
            new byte[] { 0x1B, 0x28, 0x42, 0x1B, 0x28, 0x42, 0x41 }, "iso-2022-jp");
        HtmlDecodedInput malformed = HtmlByteDecoder.Decode(
            new byte[] { 0x1B, 0x24, 0x20, 0x41 }, "iso-2022-jp");
        HtmlDecodedInput masked = HtmlByteDecoder.Decode(
            new byte[] { 0x1B, 0x24, 0x42, 0x21, 0x20 }, "iso-2022-jp");

        Assert.That(consecutive.GetString(), Is.EqualTo("�A"));
        AssertError(consecutive.GetErrors()[0], 3, 3);
        Assert.That(malformed.GetString(), Is.EqualTo("�$ A"));
        AssertError(malformed.GetErrors()[0], 0, 1);
        Assert.That(masked.GetString(), Is.EqualTo("�"));
        AssertError(masked.GetErrors()[0], 3, 2);
    }

    [Test]
    public void ReplacementDecoderReturnsOneErrorAndIgnoresRemainingInput()
    {
        HtmlDecodedInput result = HtmlByteDecoder.Decode(new byte[] { 0x41, 0x42, 0x43 }, "iso-2022-kr");

        Assert.That(result.GetEncodingName(), Is.EqualTo("replacement"));
        Assert.That(result.GetString(), Is.EqualTo("�"));
        Assert.That(result.GetErrorCount(), Is.EqualTo(1));
        AssertError(result.GetErrors()[0], 0, 1);
        Assert.That(result.GetByteOffset(1), Is.EqualTo(3));
        Assert.That(result.GetCharacterOffset(2), Is.EqualTo(1));
    }

    [Test]
    public void UserDefinedDecoderMapsNonAsciiBytesIntoThePrivateUseArea()
    {
        HtmlDecodedInput result = HtmlByteDecoder.Decode(new byte[] { 0x41, 0x80, 0xFF }, "x-user-defined");

        Assert.That(result.GetString(), Is.EqualTo("A\uF780\uF7FF"));
        Assert.That(result.HadErrors(), Is.False);
    }

    [Test]
    public void GeneratedIndexRangeLookupHandlesSpecialGapAndMaximumPointers()
    {
        Assert.That(WebEncodingIndexes.GetGb18030RangeCodePoint(0), Is.EqualTo(0x80));
        Assert.That(WebEncodingIndexes.GetGb18030RangeCodePoint(7457), Is.EqualTo(0xE7C7));
        Assert.That(WebEncodingIndexes.GetGb18030RangeCodePoint(39420), Is.EqualTo(-1));
        Assert.That(WebEncodingIndexes.GetGb18030RangeCodePoint(189000), Is.EqualTo(0x10000));
        Assert.That(WebEncodingIndexes.GetGb18030RangeCodePoint(1237575), Is.EqualTo(0x10FFFF));
        Assert.That(WebEncodingIndexes.GetGb18030RangeCodePoint(1237576), Is.EqualTo(-1));
    }

    static void AssertError(HtmlDecodingError error, int offset, int length)
    {
        Assert.That(error.GetByteOffset(), Is.EqualTo(offset));
        Assert.That(error.GetByteLength(), Is.EqualTo(length));
    }
}
