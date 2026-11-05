namespace json.Tests;

public class JsonStringExhaustiveTests
{
    [Test]
    public void LexerDecodesEveryShortEscapeEndToEnd()
    {
        (string Escape, char Value)[] cases =
        {
            ("\\\"", '"'),
            ("\\\\", '\\'),
            ("\\/", '/'),
            ("\\b", '\b'),
            ("\\f", '\f'),
            ("\\n", '\n'),
            ("\\r", '\r'),
            ("\\t", '\t')
        };

        foreach ((string escape, char value) in cases)
        {
            string raw = "\"" + escape + "\"";
            JsonToken token = LexString(System.Text.Encoding.ASCII.GetBytes(raw), JsonUnicodePolicy.UnicodeScalar, null, true);
            Assert.Multiple(() =>
            {
                Assert.That(token.StringValue, Is.EqualTo(value.ToString()), escape);
                Assert.That(token.RawUtf8, Is.EqualTo(System.Text.Encoding.ASCII.GetBytes(raw)), escape);
            });
        }
    }

    [Test]
    public void DecoderAcceptsEveryHexadecimalDigitCaseAtEveryPosition()
    {
        char[] digits = "0123456789ABCDEFabcdef".ToCharArray();

        for (int position = 0; position < 4; position++)
        {
            foreach (char digit in digits)
            {
                char[] spelling = { '0', '0', '0', '0' };
                spelling[position] = digit;
                string escape = "\\u" + new string(spelling);
                JsonToken token = LexString(System.Text.Encoding.ASCII.GetBytes("\"" + escape + "\""), JsonUnicodePolicy.UnicodeScalar, null, false);
                int digitValue = HexadecimalValue(digit);
                int expectedValue = digitValue << ((3 - position) * 4);
                string value = token.StringValue ?? throw new InvalidOperationException("Expected string value.");

                Assert.Multiple(() =>
                {
                    Assert.That(value.Length, Is.EqualTo(1), $"{escape}, position {position}");
                    Assert.That(value[0], Is.EqualTo((char)expectedValue), $"{escape}, position {position}");
                });
            }
        }
    }

    [Test]
    public void LexerCombinesSupplementaryPairBoundariesAcrossEveryInputSplit()
    {
        (string Pair, int Scalar)[] cases =
        {
            ("\\uD800\\uDC00", 0x10000),
            ("\\uD800\\uDFFF", 0x103FF),
            ("\\uDBFF\\uDC00", 0x10FC00),
            ("\\uDBFF\\uDFFF", 0x10FFFF),
            ("\\uD83D\\uDE00", 0x1F600)
        };

        foreach ((string pair, int scalar) in cases)
        {
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes("\"A" + pair + "Z\"");
            string expected = "A" + char.ConvertFromUtf32(scalar) + "Z";
            for (int split = 0; split <= bytes.Length; split++)
            {
                JsonReaderOptions options = new JsonReaderOptions();
                options.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
                options.CaptureRawTokens = true;
                JsonLexer lexer = new JsonLexer(options);
                lexer.FeedBytes(new ReadOnlySpan<byte>(bytes, 0, split));
                JsonReadStatus prefixStatus = lexer.ReadNextToken(false);
                JsonToken? token = null;
                if (prefixStatus == JsonReadStatus.TokenAvailable)
                {
                    token = lexer.GetCurrentToken();
                }
                else
                {
                    Assert.That(prefixStatus, Is.EqualTo(JsonReadStatus.NeedMoreData), $"{pair}, split {split}");
                }

                lexer.FeedBytes(new ReadOnlySpan<byte>(bytes, split, bytes.Length - split));
                if (token == null)
                {
                    Assert.That(lexer.ReadNextToken(true), Is.EqualTo(JsonReadStatus.TokenAvailable), $"{pair}, split {split}");
                    token = lexer.GetCurrentToken();
                }

                Assert.Multiple(() =>
                {
                    Assert.That(token?.StringValue, Is.EqualTo(expected), $"{pair}, split {split}");
                    Assert.That(token?.RawUtf8, Is.EqualTo(bytes), $"{pair}, split {split}");
                });
            }
        }
    }

    [Test]
    public void LoneSurrogatePoliciesCoverEveryOrderingShape()
    {
        (string Content, char[] CompatibleValue)[] cases =
        {
            ("\\uD800", new char[] { '\uD800' }),
            ("\\uDC00", new char[] { '\uDC00' }),
            ("\\uD800x", new char[] { '\uD800', 'x' }),
            ("\\uD800\\n", new char[] { '\uD800', '\n' }),
            ("\\uD800\\u0041", new char[] { '\uD800', 'A' }),
            ("\\uD800\\uD801", new char[] { '\uD800', '\uD801' }),
            ("\\uDC00\\uD800", new char[] { '\uDC00', '\uD800' }),
            ("\\uDC00\\uDC01", new char[] { '\uDC00', '\uDC01' })
        };

        foreach ((string content, char[] compatibleValue) in cases)
        {
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes("\"" + content + "\"");
            JsonToken compatible = LexString(bytes, JsonUnicodePolicy.SyntaxCompatible, null, false);
            Assert.That(compatible.StringValue?.ToCharArray(), Is.EqualTo(compatibleValue), content);

            JsonParseException exception = Assert.Throws<JsonParseException>(() =>
                LexString(bytes, JsonUnicodePolicy.UnicodeScalar, null, false))
                ?? throw new InvalidOperationException("Expected JsonParseException.");
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidSurrogatePair), content);
        }
    }

    [Test]
    public void LexerAcceptsEscapedNulButRejectsEmbeddedNulAndEveryRawControl()
    {
        JsonToken escapedNul = LexString(System.Text.Encoding.ASCII.GetBytes("\"\\u0000\""), JsonUnicodePolicy.UnicodeScalar, null, false);
        Assert.That(escapedNul.StringValue?.ToCharArray(), Is.EqualTo(new char[] { '\0' }));

        JsonParseException zeroEscape = Assert.Throws<JsonParseException>(() =>
            LexString(System.Text.Encoding.ASCII.GetBytes("\"\\0\""), JsonUnicodePolicy.UnicodeScalar, null, false))
            ?? throw new InvalidOperationException("Expected JsonParseException.");
        Assert.That(zeroEscape.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidEscape));

        for (int value = 0; value < 0x20; value++)
        {
            byte[] bytes = { 0x22, (byte)value, 0x22 };
            JsonParseException exception = Assert.Throws<JsonParseException>(() =>
                LexString(bytes, JsonUnicodePolicy.UnicodeScalar, null, false))
                ?? throw new InvalidOperationException("Expected JsonParseException.");
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidStringCharacter), $"0x{value:X2}");
        }
    }

    [Test]
    public void ScannerRejectsEveryUnsupportedAsciiEscapeName()
    {
        byte[] allowed = { 0x22, 0x5C, 0x2F, 0x62, 0x66, 0x6E, 0x72, 0x74, 0x75 };

        for (int value = 0; value <= 0x7F; value++)
        {
            if (allowed.Contains((byte)value))
            {
                continue;
            }

            byte[] bytes = { 0x22, 0x5C, (byte)value };
            JsonStringScanner scanner = new JsonStringScanner();
            JsonParseException exception = Assert.Throws<JsonParseException>(() =>
            {
                foreach (byte item in bytes)
                {
                    scanner.PushByte(item);
                }
            }) ?? throw new InvalidOperationException("Expected JsonParseException.");

            Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidEscape), $"0x{value:X2}");
        }
    }

    [Test]
    public void RawAndEscapedLineSeparatorsArePreservedWithoutNewlineSemantics()
    {
        string rawText = "\"A\u2028B\u2029C\"";
        string escapedText = "\"A\\u2028B\\u2029C\"";
        JsonToken raw = LexString(System.Text.Encoding.UTF8.GetBytes(rawText), JsonUnicodePolicy.UnicodeScalar, null, true);
        JsonToken escaped = LexString(System.Text.Encoding.ASCII.GetBytes(escapedText), JsonUnicodePolicy.UnicodeScalar, null, true);

        Assert.Multiple(() =>
        {
            Assert.That(raw.StringValue, Is.EqualTo("A\u2028B\u2029C"));
            Assert.That(escaped.StringValue, Is.EqualTo(raw.StringValue));
            Assert.That(raw.Range?.Start?.Line, Is.EqualTo(1));
            Assert.That(raw.Range?.End?.Line, Is.EqualTo(1));
            Assert.That(raw.Range?.End?.ScalarColumn, Is.EqualTo(8));
        });
    }

    [Test]
    public void LexerPreservesNormalizationDistinctionsForRawAndEscapedText()
    {
        JsonToken rawPrecomposed = LexString(System.Text.Encoding.UTF8.GetBytes("\"é\""), JsonUnicodePolicy.UnicodeScalar, null, false);
        JsonToken rawDecomposed = LexString(System.Text.Encoding.UTF8.GetBytes("\"e\u0301\""), JsonUnicodePolicy.UnicodeScalar, null, false);
        JsonToken escapedPrecomposed = LexString(System.Text.Encoding.ASCII.GetBytes("\"\\u00E9\""), JsonUnicodePolicy.UnicodeScalar, null, false);
        JsonToken escapedDecomposed = LexString(System.Text.Encoding.ASCII.GetBytes("\"\\u0065\\u0301\""), JsonUnicodePolicy.UnicodeScalar, null, false);

        Assert.Multiple(() =>
        {
            Assert.That(rawPrecomposed.StringValue, Is.EqualTo("é"));
            Assert.That(rawDecomposed.StringValue, Is.EqualTo("e\u0301"));
            Assert.That(escapedPrecomposed.StringValue, Is.EqualTo(rawPrecomposed.StringValue));
            Assert.That(escapedDecomposed.StringValue, Is.EqualTo(rawDecomposed.StringValue));
            Assert.That(rawPrecomposed.StringValue, Is.Not.EqualTo(rawDecomposed.StringValue));
            Assert.That(rawPrecomposed.StringValue?.Length, Is.EqualTo(1));
            Assert.That(rawDecomposed.StringValue?.Length, Is.EqualTo(2));
        });
    }

    [Test]
    public void IndependentStringSizeLimitsAcceptExactBoundariesAndRejectOneBeyond()
    {
        AssertStringLimitBoundary("\"abc\"", 5, 3, 3);
        AssertStringLimitBoundary("\"€\"", 5, 3, 1);
        AssertStringLimitBoundary("\"😀\"", 6, 4, 2);
        AssertStringLimitBoundary("\"\\u0041\"", 8, 1, 1);
        AssertStringLimitBoundary("\"\\uD83D\\uDE00\"", 14, 4, 2);

        AssertLimitFailure("\"abc\"", 4, 100, 100, JsonDiagnosticCode.TokenByteLimitExceeded);
        AssertLimitFailure("\"€\"", 100, 2, 100, JsonDiagnosticCode.DecodedStringByteLimitExceeded);
        AssertLimitFailure("\"😀\"", 100, 100, 1, JsonDiagnosticCode.DecodedStringCodeUnitLimitExceeded);
        AssertLimitFailure("\"\\uD83D\\uDE00\"", 100, 3, 100, JsonDiagnosticCode.DecodedStringByteLimitExceeded);
        AssertLimitFailure("\"\\uD83D\\uDE00\"", 100, 100, 1, JsonDiagnosticCode.DecodedStringCodeUnitLimitExceeded);
    }

    static JsonToken LexString(byte[] bytes, JsonUnicodePolicy policy, JsonLimits? limits, bool trackLocations)
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.UnicodePolicy = policy;
        options.Limits = limits;
        options.CaptureRawTokens = true;
        options.TrackLineInformation = trackLocations;
        options.TrackUnicodeScalarColumn = trackLocations;
        JsonLexer lexer = new JsonLexer(options);
        lexer.FeedBytes(bytes);
        Assert.That(lexer.ReadNextToken(true), Is.EqualTo(JsonReadStatus.TokenAvailable), Convert.ToHexString(bytes));
        JsonToken token = lexer.GetCurrentToken()
            ?? throw new InvalidOperationException("Expected string token.");
        Assert.That(token.Kind, Is.EqualTo(JsonTokenKind.String));
        Assert.That(lexer.ReadNextToken(true), Is.EqualTo(JsonReadStatus.StreamComplete));
        return token;
    }

    static int HexadecimalValue(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }

        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }

        return value - 'a' + 10;
    }

    static void AssertStringLimitBoundary(string text, int tokenBytes, int decodedBytes, int decodedCodeUnits)
    {
        JsonLimits limits = CreateLimits(tokenBytes, decodedBytes, decodedCodeUnits);
        JsonToken token = LexString(System.Text.Encoding.UTF8.GetBytes(text), JsonUnicodePolicy.UnicodeScalar, limits, false);
        Assert.That(token.StringValue, Is.Not.Null, text);
    }

    static void AssertLimitFailure(
        string text,
        int tokenBytes,
        int decodedBytes,
        int decodedCodeUnits,
        JsonDiagnosticCode expectedCode)
    {
        JsonLimits limits = CreateLimits(tokenBytes, decodedBytes, decodedCodeUnits);
        JsonParseException? parseException = null;
        JsonLimitException? limitException = null;
        try
        {
            LexString(System.Text.Encoding.UTF8.GetBytes(text), JsonUnicodePolicy.UnicodeScalar, limits, false);
        }
        catch (JsonLimitException exception)
        {
            limitException = exception;
        }
        catch (JsonParseException exception)
        {
            parseException = exception;
        }

        Assert.Multiple(() =>
        {
            Assert.That(parseException, Is.Null, text);
            Assert.That(limitException, Is.Not.Null, text);
            Assert.That(limitException?.Diagnostic.Code, Is.EqualTo(expectedCode), text);
        });
    }

    static JsonLimits CreateLimits(int tokenBytes, int decodedBytes, int decodedCodeUnits)
    {
        JsonLimits limits = new JsonLimits();
        limits.MaximumTokenBytes = tokenBytes;
        limits.MaximumDecodedStringBytes = decodedBytes;
        limits.MaximumDecodedStringCodeUnits = decodedCodeUnits;
        return limits;
    }
}
