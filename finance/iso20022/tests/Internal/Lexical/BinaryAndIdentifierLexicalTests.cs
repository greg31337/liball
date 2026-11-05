using iso20022;

namespace iso20022.Tests;

public class BinaryAndIdentifierLexicalTests
{
    [TestCase("", "")]
    [TestCase("TQ==", "4D")]
    [TestCase("TWE=", "4D61")]
    [TestCase("TWFu", "4D616E")]
    [TestCase(" T W\r\nFu ", "4D616E")]
    public void Base64ParserDecodesValidFormsAndFormatterCanonicalizes(string text, string expectedHex)
    {
        IsoBinaryValue? result = BinaryLexical.ParseBase64(text.AsSpan());

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result?.Encoding, Is.EqualTo(BinaryEncodingKind.Base64));
            Assert.That(result?.LexicalValue, Is.EqualTo(text));
            Assert.That(BinaryLexical.FormatHexadecimal(result!.Value), Is.EqualTo(expectedHex));
            Assert.That(BinaryLexical.FormatBase64(result.Value), Is.EqualTo(RemoveWhitespace(text)));
        });
    }

    [TestCase("A")]
    [TestCase("AAA")]
    [TestCase("====")]
    [TestCase("TQ=A")]
    [TestCase("TQ==AAAA")]
    [TestCase("TR==")]
    [TestCase("TWF=")]
    [TestCase("TW$u")]
    public void Base64ParserRejectsInvalidAlphabetPaddingAndPadBits(string text)
    {
        Assert.That(BinaryLexical.ParseBase64(text.AsSpan()), Is.Null);
    }

    [TestCase("", "")]
    [TestCase("00", "00")]
    [TestCase("4d616E", "4D616E")]
    [TestCase("FFFFFFFF", "FFFFFFFF")]
    public void HexadecimalParserAcceptsBothCasesAndFormatterUsesUppercase(string text, string formatted)
    {
        IsoBinaryValue? result = BinaryLexical.ParseHexadecimal(text.AsSpan());

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result?.Encoding, Is.EqualTo(BinaryEncodingKind.Hexadecimal));
            Assert.That(result?.LexicalValue, Is.EqualTo(text));
            Assert.That(BinaryLexical.FormatHexadecimal(result!.Value), Is.EqualTo(formatted));
        });
    }

    [TestCase("0")]
    [TestCase("GG")]
    [TestCase("00 11")]
    [TestCase("0x01")]
    public void HexadecimalParserRejectsOddLengthAndNonHexadecimalForms(string text)
    {
        Assert.That(BinaryLexical.ParseHexadecimal(text.AsSpan()), Is.Null);
    }

    [TestCase("pacs.008.001.10")]
    [TestCase("head.001.001.04")]
    [TestCase("nvlp.002.001.02")]
    public void MessageIdentifierParserPreservesAllFourComponents(string text)
    {
        MessageIdentifier? result = MessageIdentifierLexical.ParseMessageIdentifier(text.AsSpan());

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result?.BusinessArea, Is.EqualTo(text.Substring(0, 4)));
            Assert.That(result?.MessageFunctionality, Is.EqualTo(text.Substring(5, 3)));
            Assert.That(result?.Variant, Is.EqualTo(text.Substring(9, 3)));
            Assert.That(result?.Version, Is.EqualTo(text.Substring(13, 2)));
            Assert.That(result?.Value, Is.EqualTo(text));
            Assert.That(MessageIdentifierLexical.FormatMessageIdentifier(result!), Is.EqualTo(text));
        });
    }

    [TestCase("")]
    [TestCase("PACS.008.001.10")]
    [TestCase("pac5.008.001.10")]
    [TestCase("pacs.08.001.10")]
    [TestCase("pacs.008.01.10")]
    [TestCase("pacs.008.001.1")]
    [TestCase("pacs-008-001-10")]
    [TestCase("pacs.008.001.1A")]
    public void MessageIdentifierParserRejectsMalformedForms(string text)
    {
        Assert.That(MessageIdentifierLexical.ParseMessageIdentifier(text.AsSpan()), Is.Null);
    }

    internal static string RemoveWhitespace(string text)
    {
        return text.Replace(" ", string.Empty).Replace("\t", string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty);
    }
}
