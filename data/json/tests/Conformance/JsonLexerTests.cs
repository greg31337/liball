namespace json.Tests;

public class JsonLexerTests
{
    [Test]
    public void LexerRecognizesWhitespaceStructuresAndEveryScalarToken()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.CaptureRawTokens = true;
        options.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
        JsonLexer lexer = new JsonLexer(options);
        string jsonText = " \t{\n\"a\":[true,false,null,-0.50E+2,\"x\\n\\u20AC\\uD83D\\uDE00\"]}\r";
        lexer.FeedBytes(System.Text.Encoding.UTF8.GetBytes(jsonText));

        List<JsonToken> tokens = new List<JsonToken>();
        List<byte> syntaxBytes = new List<byte>();
        while (true)
        {
            JsonReadStatus status = lexer.ReadNextToken(true);
            if (status == JsonReadStatus.StreamComplete)
            {
                break;
            }

            Assert.That(status, Is.EqualTo(JsonReadStatus.TokenAvailable));
            syntaxBytes.Add(lexer.GetCurrentSyntaxByte());
            Assert.That(lexer.GetCurrentRange(), Is.Not.Null);
            Assert.That(lexer.GetCurrentRawBytes(), Is.Not.Null);
            JsonToken? token = lexer.GetCurrentToken();
            if (token != null)
            {
                tokens.Add(token);
                Assert.That(token.RawUtf8, Is.EqualTo(lexer.GetCurrentRawBytes()));
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(syntaxBytes, Is.EqualTo(new byte[]
            {
                0x7B, 0x22, 0x3A, 0x5B, 0x74, 0x2C, 0x66, 0x2C, 0x6E,
                0x2C, 0x2D, 0x2C, 0x22, 0x5D, 0x7D
            }));
            Assert.That(tokens.Select(token => token.Kind), Is.EqualTo(new[]
            {
                JsonTokenKind.StartObject,
                JsonTokenKind.String,
                JsonTokenKind.StartArray,
                JsonTokenKind.Boolean,
                JsonTokenKind.Boolean,
                JsonTokenKind.Null,
                JsonTokenKind.Number,
                JsonTokenKind.String,
                JsonTokenKind.EndArray,
                JsonTokenKind.EndObject
            }));
            Assert.That(tokens[1].StringValue, Is.EqualTo("a"));
            Assert.That(tokens[3].BooleanValue, Is.True);
            Assert.That(tokens[4].BooleanValue, Is.False);
            Assert.That(tokens[6].Number?.Lexeme, Is.EqualTo("-0.50E+2"));
            Assert.That(tokens[6].Number?.Kind, Is.EqualTo(JsonNumberKind.FractionWithExponent));
            Assert.That(tokens[7].StringValue, Is.EqualTo("x\n€😀"));
            Assert.That(tokens[7].RawUtf8, Is.EqualTo(System.Text.Encoding.ASCII.GetBytes("\"x\\n\\u20AC\\uD83D\\uDE00\"")));
            Assert.That(lexer.GetAcceptedByteCount(), Is.EqualTo(System.Text.Encoding.UTF8.GetByteCount(jsonText)));
            Assert.That(lexer.GetConsumedByteCount(), Is.EqualTo(lexer.GetAcceptedByteCount()));
            Assert.That(lexer.GetBufferedByteCount(), Is.Zero);
        });
    }

    [Test]
    public void LexerResumesAcrossEveryInputSplitWithoutChangingTokens()
    {
        string jsonText = "[true,-12.5e+2,\"A😀\\n\",null]";
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(jsonText);
        List<string> expected = LexSignatures(bytes, bytes.Length);

        for (int split = 0; split <= bytes.Length; split++)
        {
            List<string> actual = LexSignatures(bytes, split);
            Assert.That(actual, Is.EqualTo(expected), $"split {split}");
        }
    }

    [Test]
    public void LexerAppliesConfiguredLoneSurrogatePolicyDuringStringDecoding()
    {
        JsonReaderOptions compatibleOptions = new JsonReaderOptions();
        compatibleOptions.UnicodePolicy = JsonUnicodePolicy.SyntaxCompatible;
        JsonLexer compatible = new JsonLexer(compatibleOptions);
        compatible.FeedBytes(System.Text.Encoding.ASCII.GetBytes("\"\\uD800x\\uDC00\""));

        Assert.That(compatible.ReadNextToken(true), Is.EqualTo(JsonReadStatus.TokenAvailable));
        string compatibleValue = compatible.GetCurrentToken()?.StringValue
            ?? throw new InvalidOperationException("Expected string token.");
        Assert.That(compatibleValue.ToCharArray(), Is.EqualTo(new char[] { '\uD800', 'x', '\uDC00' }));

        JsonReaderOptions scalarOptions = new JsonReaderOptions();
        scalarOptions.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
        JsonLexer scalar = new JsonLexer(scalarOptions);
        scalar.FeedBytes(System.Text.Encoding.ASCII.GetBytes("\"\\uD800x\""));

        JsonParseException exception = Assert.Throws<JsonParseException>(() => scalar.ReadNextToken(true))
            ?? throw new InvalidOperationException("Expected JsonParseException.");
        Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidSurrogatePair));
    }

    [Test]
    public void LexerAcceptsOnlyTheFourJsonWhitespaceBytes()
    {
        byte[] validWhitespace = { 0x20, 0x09, 0x0A, 0x0D };
        foreach (byte value in validWhitespace)
        {
            JsonLexer lexer = new JsonLexer(null);
            lexer.FeedBytes(new byte[] { value });
            Assert.That(lexer.ReadNextToken(true), Is.EqualTo(JsonReadStatus.StreamComplete), $"0x{value:X2}");
        }

        byte[] invalidWhitespace = { 0x00, 0x0B, 0x0C, 0x1F };
        foreach (byte value in invalidWhitespace)
        {
            JsonLexer lexer = new JsonLexer(null);
            lexer.FeedBytes(new byte[] { value });
            JsonParseException exception = Assert.Throws<JsonParseException>(() => lexer.ReadNextToken(true))
                ?? throw new InvalidOperationException("Expected JsonParseException.");
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidWhitespace), $"0x{value:X2}");
        }
    }

    [Test]
    public void LexerPreservesSpecificLiteralAndNumberFailures()
    {
        AssertLexerFailure("True ", JsonDiagnosticCode.InvalidLiteral);
        AssertLexerFailure("truE ", JsonDiagnosticCode.InvalidLiteral);
        AssertLexerFailure("truex ", JsonDiagnosticCode.InvalidLiteral);
        AssertLexerFailure("01 ", JsonDiagnosticCode.InvalidNumber);
        AssertLexerFailure("1e+ ", JsonDiagnosticCode.InvalidNumber);
        AssertLexerFailure("/comment", JsonDiagnosticCode.UnexpectedCharacter);
    }

    [Test]
    public void LexerEnforcesRawNumberAndDecodedStringLimitsAtBoundaries()
    {
        JsonLimits exactLimits = CreateLimits();
        exactLimits.MaximumTokenBytes = 5;
        exactLimits.MaximumNumberCharacters = 4;
        JsonReaderOptions exactOptions = new JsonReaderOptions();
        exactOptions.Limits = exactLimits;
        JsonLexer exact = new JsonLexer(exactOptions);
        exact.FeedBytes(System.Text.Encoding.ASCII.GetBytes("\"abc\" 1234 "));

        Assert.That(exact.ReadNextToken(false), Is.EqualTo(JsonReadStatus.TokenAvailable));
        Assert.That(exact.GetCurrentToken()?.StringValue, Is.EqualTo("abc"));
        Assert.That(exact.ReadNextToken(false), Is.EqualTo(JsonReadStatus.TokenAvailable));
        Assert.That(exact.GetCurrentToken()?.Number?.Lexeme, Is.EqualTo("1234"));

        AssertLimitFailure("\"abcd\"", 5, 20, 20, 20, JsonDiagnosticCode.TokenByteLimitExceeded);
        AssertLimitFailure("12345", 20, 4, 20, 20, JsonDiagnosticCode.NumberCharacterLimitExceeded);
        AssertLimitFailure("\"abc\"", 20, 20, 20, 2, JsonDiagnosticCode.DecodedStringCodeUnitLimitExceeded);
        AssertLimitFailure("\"€\"", 20, 20, 2, 20, JsonDiagnosticCode.DecodedStringByteLimitExceeded);
    }

    [Test]
    public void LexerTracksMultibyteTokenRangesAcrossCrLf()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.TrackLineInformation = true;
        options.TrackUnicodeScalarColumn = true;
        JsonLexer lexer = new JsonLexer(options);
        lexer.FeedBytes(System.Text.Encoding.UTF8.GetBytes(" \r\n\"€\""));

        Assert.That(lexer.ReadNextToken(true), Is.EqualTo(JsonReadStatus.TokenAvailable));
        JsonSourceRange range = lexer.GetCurrentToken()?.Range
            ?? throw new InvalidOperationException("Expected token range.");

        Assert.Multiple(() =>
        {
            AssertPosition(range.Start, 3, 2, 1, 1);
            AssertPosition(range.End, 8, 2, 6, 4);
            Assert.That(lexer.GetCurrentToken()?.StringValue, Is.EqualTo("€"));
        });
    }

    [Test]
    public void LexerRawCaptureCanBeDisabledAndResetClearsInputAndLocationState()
    {
        JsonLexer lexer = new JsonLexer(null);
        lexer.FeedBytes(System.Text.Encoding.ASCII.GetBytes("true "));

        Assert.That(lexer.ReadNextToken(false), Is.EqualTo(JsonReadStatus.TokenAvailable));
        Assert.Multiple(() =>
        {
            Assert.That(lexer.GetCurrentToken()?.RawUtf8, Is.Null);
            Assert.That(lexer.GetCurrentRawBytes(), Is.Null);
            Assert.That(lexer.GetAcceptedByteCount(), Is.EqualTo(5));
            Assert.That(lexer.GetConsumedByteCount(), Is.EqualTo(4));
            Assert.That(lexer.GetBufferedByteCount(), Is.EqualTo(1));
        });

        lexer.ResetLexer();
        Assert.Multiple(() =>
        {
            Assert.That(lexer.GetAcceptedByteCount(), Is.Zero);
            Assert.That(lexer.GetConsumedByteCount(), Is.Zero);
            Assert.That(lexer.GetBufferedByteCount(), Is.Zero);
            Assert.That(lexer.GetCurrentToken(), Is.Null);
            Assert.That(lexer.GetCurrentSyntaxByte(), Is.Zero);
        });

        lexer.FeedBytes(System.Text.Encoding.ASCII.GetBytes("[]"));
        Assert.That(lexer.ReadNextToken(false), Is.EqualTo(JsonReadStatus.TokenAvailable));
        JsonSourceRange firstRange = lexer.GetCurrentRange()
            ?? throw new InvalidOperationException("Expected range.");
        Assert.That(firstRange.Start?.ByteOffset, Is.Zero);
        Assert.That(lexer.GetCurrentToken()?.Kind, Is.EqualTo(JsonTokenKind.StartArray));
        Assert.That(lexer.ReadNextToken(true), Is.EqualTo(JsonReadStatus.TokenAvailable));
        Assert.That(lexer.GetCurrentToken()?.Kind, Is.EqualTo(JsonTokenKind.EndArray));
        Assert.That(lexer.ReadNextToken(true), Is.EqualTo(JsonReadStatus.StreamComplete));
    }

    static List<string> LexSignatures(byte[] bytes, int split)
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.CaptureRawTokens = true;
        options.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
        JsonLexer lexer = new JsonLexer(options);
        List<string> signatures = new List<string>();
        lexer.FeedBytes(new ReadOnlySpan<byte>(bytes, 0, split));
        CollectTokens(lexer, false, signatures);
        lexer.FeedBytes(new ReadOnlySpan<byte>(bytes, split, bytes.Length - split));
        CollectTokens(lexer, true, signatures);
        return signatures;
    }

    static void CollectTokens(JsonLexer lexer, bool finalInput, List<string> signatures)
    {
        while (true)
        {
            JsonReadStatus status = lexer.ReadNextToken(finalInput);
            if (status == JsonReadStatus.NeedMoreData || status == JsonReadStatus.StreamComplete)
            {
                return;
            }

            Assert.That(status, Is.EqualTo(JsonReadStatus.TokenAvailable));
            JsonToken? token = lexer.GetCurrentToken();
            string raw = Convert.ToHexString(lexer.GetCurrentRawBytes() ?? Array.Empty<byte>());
            if (token == null)
            {
                signatures.Add($"P:{lexer.GetCurrentSyntaxByte():X2}:{raw}");
            }
            else
            {
                signatures.Add($"T:{token.Kind}:{raw}:{token.StringValue}:{token.Number?.Lexeme}:{token.BooleanValue}");
            }
        }
    }

    static void AssertLexerFailure(string text, JsonDiagnosticCode expectedCode)
    {
        JsonLexer lexer = new JsonLexer(null);
        lexer.FeedBytes(System.Text.Encoding.ASCII.GetBytes(text));
        JsonParseException exception = Assert.Throws<JsonParseException>(() => lexer.ReadNextToken(true))
            ?? throw new InvalidOperationException($"Expected JsonParseException for {text}.");
        Assert.That(exception.Diagnostic.Code, Is.EqualTo(expectedCode), text);
    }

    static void AssertLimitFailure(
        string text,
        int maximumTokenBytes,
        int maximumNumberCharacters,
        int maximumDecodedStringBytes,
        int maximumDecodedStringCodeUnits,
        JsonDiagnosticCode expectedCode)
    {
        JsonLimits limits = CreateLimits();
        limits.MaximumTokenBytes = maximumTokenBytes;
        limits.MaximumNumberCharacters = maximumNumberCharacters;
        limits.MaximumDecodedStringBytes = maximumDecodedStringBytes;
        limits.MaximumDecodedStringCodeUnits = maximumDecodedStringCodeUnits;
        JsonReaderOptions options = new JsonReaderOptions();
        options.Limits = limits;
        JsonLexer lexer = new JsonLexer(options);
        lexer.FeedBytes(System.Text.Encoding.UTF8.GetBytes(text));

        JsonLimitException exception = Assert.Throws<JsonLimitException>(() => lexer.ReadNextToken(true))
            ?? throw new InvalidOperationException("Expected JsonLimitException.");
        Assert.That(exception.Diagnostic.Code, Is.EqualTo(expectedCode), text);
    }

    static JsonLimits CreateLimits()
    {
        JsonLimits limits = new JsonLimits();
        limits.MaximumInputBufferBytes = 1_024;
        return limits;
    }

    static void AssertPosition(JsonSourcePosition? position, long offset, int line, int byteColumn, int scalarColumn)
    {
        JsonSourcePosition actual = position ?? throw new InvalidOperationException("Expected position.");
        Assert.Multiple(() =>
        {
            Assert.That(actual.ByteOffset, Is.EqualTo(offset));
            Assert.That(actual.Line, Is.EqualTo(line));
            Assert.That(actual.ByteColumn, Is.EqualTo(byteColumn));
            Assert.That(actual.ScalarColumn, Is.EqualTo(scalarColumn));
        });
    }
}
