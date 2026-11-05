using System.Text;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlStringParserTests
{
    [Test]
    public void DecodesEveryRfc9682TextEscapeExampleIdentically()
    {
        string expected = "Domino's 🁳 + ⌘";
        CddlToken braced = Read("\"D\\u{6f}mino's \\u{1F073} + \\u{2318}\"");
        CddlToken paired = Read("\"Domino's \\uD83C\\uDC73 + \\u2318\"");
        CddlToken direct = Read("\"Domino's 🁳 + ⌘\"");

        Assert.Multiple(() =>
        {
            AssertText(braced, expected);
            AssertText(paired, expected);
            AssertText(direct, expected);
        });
    }

    [Test]
    public void DecodesEveryRfc9682Utf8ByteStringExampleIdentically()
    {
        byte[] expected = Encoding.UTF8.GetBytes("Domino's 🁳 + ⌘");
        CddlToken braced = Read("'D\\u{6f}mino\\u{27}s \\u{1F073} + \\u{2318}'");
        CddlToken paired = Read("'Domino\\'s \\uD83C\\uDC73 + \\u2318'");
        CddlToken direct = Read("'Domino\\'s 🁳 + ⌘'");

        Assert.Multiple(() =>
        {
            AssertBytes(braced, CddlLiteralKind.Utf8ByteString, expected);
            AssertBytes(paired, CddlLiteralKind.Utf8ByteString, expected);
            AssertBytes(direct, CddlLiteralKind.Utf8ByteString, expected);
        });
    }

    [Test]
    public void DecodesJsonEscapesBracedZerosAndUnicodeScalarEndpoints()
    {
        CddlToken escapes = Read("\"\\\"\\/\\\\\\b\\f\\n\\r\\t\\u0041\\u{42}\"");
        CddlToken endpoints = Read("\"\\u{000000000}\\u{10FFFF}\"");

        Assert.Multiple(() =>
        {
            AssertText(escapes, "\"/\\\b\f\n\r\tAB");
            AssertText(endpoints, "\0" + char.ConvertFromUtf32(0x10ffff));
        });
    }

    [Test]
    public void DecodesEveryUnicodeEscapeGrammarAlternative()
    {
        CddlToken fixedNonSurrogate = Read(
            "\"\\u0000\\uD7ff\\ue000\\uFFFF\"");
        CddlToken fixedSurrogatePairs = Read(
            "\"\\uD800\\uDC00\\udbff\\udfff\"");
        CddlToken bracedWidths = Read(
            "\"\\u{1}\\u{12}\\u{123}\\u{1234}\\u{12345}"
            + "\\u{10FFFF}\\u{00000000000041}\"");

        Assert.Multiple(() =>
        {
            AssertText(fixedNonSurrogate,
                "\0\ud7ff\ue000\uffff");
            AssertText(fixedSurrogatePairs,
                char.ConvertFromUtf32(0x10000)
                + char.ConvertFromUtf32(0x10ffff));
            AssertText(bracedWidths,
                char.ConvertFromUtf32(0x1)
                + char.ConvertFromUtf32(0x12)
                + char.ConvertFromUtf32(0x123)
                + char.ConvertFromUtf32(0x1234)
                + char.ConvertFromUtf32(0x12345)
                + char.ConvertFromUtf32(0x10ffff)
                + "A");
        });
    }

    [Test]
    public void AcceptsEveryDirectTextAndByteCharacterRangeBoundary()
    {
        string nonAscii = "\u00a0\ud7ff\ue000\uffff"
            + char.ConvertFromUtf32(0x10fffd);
        CddlToken text = Read("\" !#[]~" + nonAscii + "\"");
        CddlToken bytes = Read("' !&()\"[]~" + nonAscii + "'");

        Assert.Multiple(() =>
        {
            AssertText(text, " !#[]~" + nonAscii);
            AssertBytes(
                bytes,
                CddlLiteralKind.Utf8ByteString,
                Encoding.UTF8.GetBytes(" !&()\"[]~" + nonAscii));
        });
    }

    [Test]
    public void DecodesCaseInsensitiveQualifiedByteStringTerminals()
    {
        CddlToken hexadecimal = Read("H'44 6F'");
        CddlToken base64 = Read("B64'Zm8'");

        Assert.Multiple(() =>
        {
            AssertBytes(
                hexadecimal,
                CddlLiteralKind.HexByteString,
                Encoding.ASCII.GetBytes("Do"));
            AssertBytes(
                base64,
                CddlLiteralKind.Base64UrlByteString,
                Encoding.ASCII.GetBytes("fo"));
        });
    }

    [TestCase("\"\\q\"", "CDDL_ESCAPE_INVALID")]
    [TestCase("\"\\u12\"", "CDDL_UNICODE_ESCAPE_INCOMPLETE")]
    [TestCase("\"\\u12G4\"", "CDDL_UNICODE_ESCAPE_INVALID")]
    [TestCase("\"\\u{}\"", "CDDL_UNICODE_ESCAPE_INVALID")]
    [TestCase("\"\\u{110000}\"", "CDDL_UNICODE_SCALAR_INVALID")]
    [TestCase("\"\\u{D800}\"", "CDDL_UNICODE_SCALAR_INVALID")]
    [TestCase("\"\\uD800\"", "CDDL_UNICODE_SCALAR_INVALID")]
    [TestCase("\"\\uDC00\"", "CDDL_UNICODE_SCALAR_INVALID")]
    [TestCase("\"\\uD800\\u0041\"", "CDDL_UNICODE_SCALAR_INVALID")]
    [TestCase("\"\\'\"", "CDDL_ESCAPE_INVALID")]
    public void RejectsMalformedAndNonScalarEscapes(string source, string errorCode)
    {
        CddlToken token = Read(source);

        AssertInvalid(token, errorCode);
    }

    [TestCase("\"\\u{\"", "CDDL_UNICODE_ESCAPE_INCOMPLETE")]
    [TestCase("\"\\u{0X41}\"", "CDDL_UNICODE_ESCAPE_INVALID")]
    [TestCase("\"\\u{000000110000}\"", "CDDL_UNICODE_SCALAR_INVALID")]
    [TestCase("\"\\udbff\\uDBFF\"", "CDDL_UNICODE_SCALAR_INVALID")]
    [TestCase("\"\\ud7ff\\udc00\"", "CDDL_UNICODE_SCALAR_INVALID")]
    public void RejectsMalformedUnicodeEscapeProductionBoundaries(
        string source,
        string errorCode)
    {
        AssertInvalid(Read(source), errorCode);
    }

    [Test]
    public void RejectsRawControlsSurrogatesAndNoncharactersButAllowsU10fffd()
    {
        CddlToken control = Read("\"a\t b\"");
        CddlToken delete = Read("\"a\u007fb\"");
        CddlToken c1 = Read("\"a\u0085b\"");
        CddlToken surrogate = Read("\"a\ud800b\"");
        CddlToken maximumRaw = Read(
            "\"" + char.ConvertFromUtf32(0x10fffd) + "\"");
        CddlToken noncharacter = Read(
            "\"" + char.ConvertFromUtf32(0x10fffe) + "\"");

        Assert.Multiple(() =>
        {
            AssertInvalid(control, "CDDL_TEXT_CHARACTER_INVALID");
            AssertInvalid(delete, "CDDL_TEXT_CHARACTER_INVALID");
            AssertInvalid(c1, "CDDL_TEXT_CHARACTER_INVALID");
            AssertInvalid(surrogate, "CDDL_UNICODE_SCALAR_INVALID");
            AssertText(maximumRaw, char.ConvertFromUtf32(0x10fffd));
            AssertInvalid(noncharacter, "CDDL_UNICODE_SCALAR_INVALID");
        });
    }

    [Test]
    public void RejectsEveryGapBetweenDirectCharacterRanges()
    {
        string[] invalidText =
        {
            "\"\u0000\"",
            "\"\u001f\"",
            "\"\u007f\"",
            "\"\u0080\"",
            "\"\u009f\"",
            "\"\ud800\"",
            "\"\udfff\"",
            "\"" + char.ConvertFromUtf32(0x10fffe) + "\""
        };
        string[] invalidBytes = invalidText
            .Select(source => "'" + source.Substring(1, source.Length - 2) + "'")
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(invalidText.Select(Read).Select(token => token.Kind),
                Is.All.EqualTo(CddlTokenKind.Invalid));
            Assert.That(invalidBytes.Select(Read).Select(token => token.Kind),
                Is.All.EqualTo(CddlTokenKind.Invalid));
        });
    }

    [Test]
    public void Utf8ByteStringsPreserveAllowedNewlinesAndEscapedApostrophes()
    {
        CddlToken token = Read("'a\\'b\ncd\r\nef'");
        byte[] expected = Encoding.UTF8.GetBytes("a'b\ncd\r\nef");
        CddlToken bareCarriageReturn = Read("'a\rb'");
        CddlToken tab = Read("'a\tb'");

        Assert.Multiple(() =>
        {
            AssertBytes(token, CddlLiteralKind.Utf8ByteString, expected);
            AssertInvalid(bareCarriageReturn, "CDDL_BYTE_CHARACTER_INVALID");
            AssertInvalid(tab, "CDDL_BYTE_CHARACTER_INVALID");
        });
    }

    [Test]
    public void DecodesHexBytesAfterRemovingWhitespaceCommentsAndEscapes()
    {
        CddlToken spaced = Read("h' 44 6f ; word\r\n 6d 69 6e 6f '");
        CddlToken escapedSpace = Read("h'4\\u{20}1'");
        CddlToken empty = Read("h''");

        Assert.Multiple(() =>
        {
            AssertBytes(
                spaced,
                CddlLiteralKind.HexByteString,
                Encoding.ASCII.GetBytes("Domino"));
            AssertBytes(
                escapedSpace,
                CddlLiteralKind.HexByteString,
                new byte[] { 0x41 });
            AssertBytes(empty, CddlLiteralKind.HexByteString, []);
        });
    }

    [TestCase("h'0'", "CDDL_HEX_LENGTH_INVALID")]
    [TestCase("h'0g'", "CDDL_HEX_CHARACTER_INVALID")]
    [TestCase("h'41;comment'", "CDDL_ENCODED_COMMENT_UNTERMINATED")]
    [TestCase("h'4\t1'", "CDDL_BYTE_CHARACTER_INVALID")]
    public void RejectsMalformedHexAndEncodedTrivia(string source, string errorCode)
    {
        AssertInvalid(Read(source), errorCode);
    }

    [TestCase("b64''", "")]
    [TestCase("b64'Zg'", "f")]
    [TestCase("b64'Zm8'", "fo")]
    [TestCase("b64'Zm9v'", "foo")]
    [TestCase("b64'Zm9vYg'", "foob")]
    [TestCase("b64'Zm9vYmE'", "fooba")]
    [TestCase("b64'Zm9vYmFy'", "foobar")]
    public void DecodesUnpaddedBase64Vectors(string source, string expected)
    {
        CddlToken token = Read(source);

        AssertBytes(
            token,
            CddlLiteralKind.Base64UrlByteString,
            Encoding.ASCII.GetBytes(expected));
    }

    [Test]
    public void AcceptsStandardAndUrlBase64AlphabetsWithEncodedTrivia()
    {
        CddlToken standard = Read("b64' /w '");
        CddlToken url = Read("b64' _w '");
        CddlToken commented = Read("b64' Zm8 ; two letters\n '");

        Assert.Multiple(() =>
        {
            AssertBytes(standard, CddlLiteralKind.Base64UrlByteString, [0xff]);
            AssertBytes(url, CddlLiteralKind.Base64UrlByteString, [0xff]);
            AssertBytes(
                commented,
                CddlLiteralKind.Base64UrlByteString,
                Encoding.ASCII.GetBytes("fo"));
        });
    }

    [TestCase("b64'A'", "CDDL_BASE64URL_LENGTH_INVALID")]
    [TestCase("b64'A*'", "CDDL_BASE64URL_CHARACTER_INVALID")]
    [TestCase("b64'+_'", "CDDL_BASE64URL_ALPHABET_MIXED")]
    [TestCase("b64'Zh'", "CDDL_BASE64URL_TRAILING_BITS")]
    [TestCase("b64'Zg='", "CDDL_BASE64URL_CHARACTER_INVALID")]
    public void RejectsMalformedBase64(string source, string errorCode)
    {
        AssertInvalid(Read(source), errorCode);
    }

    [Test]
    public void ReportsLiteralDiagnosticsAtTheExactSourceSubspan()
    {
        string source = "  \"ok\\u{110000}\"";
        CddlParseOptions options = new CddlParseOptions
        {
            SourceName = "literal.cddl"
        };
        CddlLexer lexer = new CddlLexer(source, options);
        CddlToken token = lexer.ReadToken();
        CddlDiagnostic diagnostic = lexer.Diagnostics[0];
        CddlLexer multilineLexer = new CddlLexer(
            "h'\n  0G\n'",
            new CddlParseOptions { SourceName = "bytes.cddl" });
        CddlToken multiline = multilineLexer.ReadToken();
        CddlDiagnostic multilineDiagnostic = multilineLexer.Diagnostics[0];

        Assert.Multiple(() =>
        {
            AssertInvalid(token, "CDDL_UNICODE_SCALAR_INVALID");
            Assert.That(token.Span.Start, Is.EqualTo(2));
            Assert.That(token.Span.Length, Is.EqualTo(source.Length - 2));
            Assert.That(diagnostic.Span.SourceName, Is.EqualTo("literal.cddl"));
            Assert.That(diagnostic.Span.Start, Is.EqualTo(source.IndexOf("\\u")));
            Assert.That(diagnostic.Span.StartLine, Is.EqualTo(1));
            Assert.That(diagnostic.Span.StartColumn,
                Is.EqualTo(source.IndexOf("\\u") + 1));
            AssertInvalid(multiline, "CDDL_HEX_CHARACTER_INVALID");
            Assert.That(multilineDiagnostic.Span.SourceName, Is.EqualTo("bytes.cddl"));
            Assert.That(multilineDiagnostic.Span.StartLine, Is.EqualTo(2));
            Assert.That(multilineDiagnostic.Span.StartColumn, Is.EqualTo(4));
            Assert.That(multilineDiagnostic.Span.Length, Is.EqualTo(1));
        });
    }

    private static CddlToken Read(string source)
    {
        CddlLexer lexer = new CddlLexer(source, new CddlParseOptions
        {
            SourceName = "literal.cddl"
        });
        return lexer.ReadToken();
    }

    private static void AssertText(CddlToken token, string expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(token.Kind, Is.EqualTo(CddlTokenKind.TextString));
            Assert.That(token.LiteralKind, Is.EqualTo(CddlLiteralKind.TextString));
            Assert.That(token.StringValue, Is.EqualTo(expected));
            Assert.That(token.ByteStringValue, Is.Null);
        });
    }

    private static void AssertBytes(
        CddlToken token,
        CddlLiteralKind kind,
        byte[] expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(token.Kind, Is.EqualTo(CddlTokenKind.ByteString));
            Assert.That(token.LiteralKind, Is.EqualTo(kind));
            Assert.That(token.ByteStringValue, Is.EqualTo(expected));
            Assert.That(token.StringValue, Is.Null);
        });
    }

    private static void AssertInvalid(CddlToken token, string errorCode)
    {
        Assert.Multiple(() =>
        {
            Assert.That(token.Kind, Is.EqualTo(CddlTokenKind.Invalid));
            Assert.That(token.ErrorCode, Is.EqualTo(errorCode));
            Assert.That(token.ErrorMessage, Is.Not.Empty);
        });
    }
}
