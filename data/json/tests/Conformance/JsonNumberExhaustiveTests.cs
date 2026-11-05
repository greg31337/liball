namespace json.Tests;

public class JsonNumberExhaustiveTests
{
    [Test]
    public void LexerAcceptsGeneratedValidGrammarFormsWithExactMetadata()
    {
        HashSet<string> cases = new HashSet<string>
        {
            "0",
            "-0",
            "10",
            "-10",
            "1234567890",
            "-9876543210",
            "0.0",
            "-0.000",
            "12.3400",
            "0e0",
            "-0E+00",
            "9e-999",
            "0.0e0",
            "-12.3400E+005"
        };

        for (int digit = 0; digit <= 9; digit++)
        {
            cases.Add("1" + digit);
            cases.Add("-1" + digit);
            cases.Add("1." + digit);
            cases.Add("-1." + digit);
            cases.Add("1e" + digit);
            cases.Add("1E+" + digit);
            cases.Add("1e-" + digit);
            cases.Add("1.0e" + digit);
            cases.Add("1.0E+" + digit);
            cases.Add("1.0e-" + digit);
        }

        for (int digit = 1; digit <= 9; digit++)
        {
            cases.Add(digit.ToString());
            cases.Add("-" + digit);
        }

        foreach (string text in cases)
        {
            JsonToken token = LexNumber(text);
            JsonNumber number = token.Number ?? throw new InvalidOperationException("Expected number payload.");
            bool hasFraction = text.Contains('.');
            bool hasExponent = text.Contains('e') || text.Contains('E');
            JsonNumberKind expectedKind = GetExpectedKind(hasFraction, hasExponent);

            Assert.Multiple(() =>
            {
                Assert.That(number.Lexeme, Is.EqualTo(text), text);
                Assert.That(number.Kind, Is.EqualTo(expectedKind), text);
                Assert.That(number.IsNegative, Is.EqualTo(text.StartsWith('-')), text);
                Assert.That(number.HasFraction, Is.EqualTo(hasFraction), text);
                Assert.That(number.HasExponent, Is.EqualTo(hasExponent), text);
                Assert.That(token.RawUtf8, Is.EqualTo(System.Text.Encoding.ASCII.GetBytes(text)), text);
            });
        }
    }

    [Test]
    public void ScannerRejectsEveryInvalidSignAndLeadingZeroFamily()
    {
        string[] cases =
        {
            "+0",
            "+1",
            "-",
            "--0",
            "-+0",
            "+-0",
            "++0",
            "- 0",
            "00",
            "01",
            "09",
            "-00",
            "-01",
            "00.0",
            "01e0",
            "0x0",
            "0X0",
            "0_0"
        };

        foreach (string text in cases)
        {
            AssertScannerInvalid(text);
        }
    }

    [Test]
    public void ScannerRejectsEveryInvalidFractionFamily()
    {
        string[] cases =
        {
            ".0",
            "-.0",
            "+.0",
            "1.",
            "-1.",
            "1..0",
            "1.-0",
            "1.+0",
            "1.e0",
            "1. e0",
            "1,0",
            "1_0",
            "1.0.0",
            "1.0-1",
            "1.0+1"
        };

        foreach (string text in cases)
        {
            AssertScannerInvalid(text);
        }
    }

    [Test]
    public void ScannerRejectsEveryInvalidExponentFamily()
    {
        string[] cases =
        {
            "e0",
            "E0",
            "-e0",
            "1e",
            "1E",
            "1e+",
            "1e-",
            "1e++0",
            "1e+-0",
            "1e-+0",
            "1e--0",
            "1ee0",
            "1eE0",
            "1e.0",
            "1e 0",
            "1e_0",
            "1e0.0",
            "1e0e0",
            "1e0+1",
            "1e0-1"
        };

        foreach (string text in cases)
        {
            AssertScannerInvalid(text);
        }
    }

    [Test]
    public void LexerStopsBeforeEveryLegalDelimiterWithoutConsumingIt()
    {
        byte[] delimiters =
        {
            JsonSyntaxBytes.Space,
            JsonSyntaxBytes.HorizontalTab,
            JsonSyntaxBytes.LineFeed,
            JsonSyntaxBytes.CarriageReturn,
            JsonSyntaxBytes.ValueSeparator,
            JsonSyntaxBytes.EndArray,
            JsonSyntaxBytes.EndObject,
            JsonSyntaxBytes.NameSeparator,
            JsonSyntaxBytes.BeginArray,
            JsonSyntaxBytes.BeginObject
        };

        foreach (byte delimiter in delimiters)
        {
            byte[] numberBytes = System.Text.Encoding.ASCII.GetBytes("-12.5e+2");
            byte[] input = numberBytes.Concat(new byte[] { delimiter }).ToArray();
            JsonReaderOptions options = new JsonReaderOptions();
            options.CaptureRawTokens = true;
            JsonLexer lexer = new JsonLexer(options);
            lexer.FeedBytes(input);

            Assert.That(lexer.ReadNextToken(false), Is.EqualTo(JsonReadStatus.TokenAvailable), $"0x{delimiter:X2}");
            Assert.Multiple(() =>
            {
                Assert.That(lexer.GetCurrentToken()?.Number?.Lexeme, Is.EqualTo("-12.5e+2"), $"0x{delimiter:X2}");
                Assert.That(lexer.GetCurrentRawBytes(), Is.EqualTo(numberBytes), $"0x{delimiter:X2}");
                Assert.That(lexer.GetConsumedByteCount(), Is.EqualTo(numberBytes.Length), $"0x{delimiter:X2}");
                Assert.That(lexer.GetBufferedByteCount(), Is.EqualTo(1), $"0x{delimiter:X2}");
            });

            JsonReadStatus delimiterStatus = lexer.ReadNextToken(true);
            if (delimiter == JsonSyntaxBytes.Space || delimiter == JsonSyntaxBytes.HorizontalTab ||
                delimiter == JsonSyntaxBytes.LineFeed || delimiter == JsonSyntaxBytes.CarriageReturn)
            {
                Assert.That(delimiterStatus, Is.EqualTo(JsonReadStatus.StreamComplete), $"0x{delimiter:X2}");
            }
            else
            {
                Assert.Multiple(() =>
                {
                    Assert.That(delimiterStatus, Is.EqualTo(JsonReadStatus.TokenAvailable), $"0x{delimiter:X2}");
                    Assert.That(lexer.GetCurrentSyntaxByte(), Is.EqualTo(delimiter), $"0x{delimiter:X2}");
                });
            }
        }
    }

    [Test]
    public void LexerRejectsEveryNonDelimiterContinuationAfterAnAcceptingNumber()
    {
        byte[] continuations =
        {
            0x22,
            0x27,
            0x2B,
            0x2D,
            0x2E,
            0x2F,
            0x3B,
            0x3D,
            0x5C,
            0x5F,
            0x61,
            0x65,
            0x74
        };

        foreach (byte continuation in continuations)
        {
            byte[] input = System.Text.Encoding.ASCII.GetBytes("12").Concat(new byte[] { continuation }).ToArray();
            JsonLexer lexer = new JsonLexer(null);
            lexer.FeedBytes(input);
            JsonParseException exception = Assert.Throws<JsonParseException>(() => lexer.ReadNextToken(true))
                ?? throw new InvalidOperationException("Expected JsonParseException.");
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidNumber), $"0x{continuation:X2}");
        }
    }

    [Test]
    public void LexerRejectsEveryTruncatedStateAtFinalInputAndBeforeDelimiters()
    {
        string[] incompleteCases = { "-", "0.", "1.", "0e", "1e", "1E", "1e+", "1e-", "-1.0E+" };
        byte[] delimiters = { 0x20, 0x2C, 0x5D, 0x7D };

        foreach (string text in incompleteCases)
        {
            AssertLexerInvalidNumber(System.Text.Encoding.ASCII.GetBytes(text), text + " at EOF");
            foreach (byte delimiter in delimiters)
            {
                byte[] input = System.Text.Encoding.ASCII.GetBytes(text).Concat(new byte[] { delimiter }).ToArray();
                AssertLexerInvalidNumber(input, $"{text} before 0x{delimiter:X2}");
            }
        }
    }

    [Test]
    public void LexerResumesAComplexNumberAtEveryByteSplit()
    {
        string text = "-1234567890.0012300E+00045";
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(text);

        for (int split = 0; split <= bytes.Length; split++)
        {
            JsonReaderOptions options = new JsonReaderOptions();
            options.CaptureRawTokens = true;
            JsonLexer lexer = new JsonLexer(options);
            lexer.FeedBytes(new ReadOnlySpan<byte>(bytes, 0, split));
            Assert.That(lexer.ReadNextToken(false), Is.EqualTo(JsonReadStatus.NeedMoreData), $"split {split}");

            lexer.FeedBytes(new ReadOnlySpan<byte>(bytes, split, bytes.Length - split));
            Assert.That(lexer.ReadNextToken(true), Is.EqualTo(JsonReadStatus.TokenAvailable), $"split {split}");
            JsonNumber number = lexer.GetCurrentToken()?.Number
                ?? throw new InvalidOperationException("Expected number payload.");
            Assert.Multiple(() =>
            {
                Assert.That(number.Lexeme, Is.EqualTo(text), $"split {split}");
                Assert.That(number.Kind, Is.EqualTo(JsonNumberKind.FractionWithExponent), $"split {split}");
                Assert.That(lexer.GetCurrentRawBytes(), Is.EqualTo(bytes), $"split {split}");
                Assert.That(lexer.GetConsumedByteCount(), Is.EqualTo(bytes.Length), $"split {split}");
            });
        }
    }

    [Test]
    public void NegativeZeroClassificationUsesOnlySignificandDigits()
    {
        string[] negativeZeroCases = { "-0", "-0.0", "-0.000", "-0e0", "-0E+999", "-0.000e-999" };
        string[] nonZeroCases = { "0", "0.0", "-1", "-10e-1", "-0.1", "-0.0001e999" };

        foreach (string text in negativeZeroCases)
        {
            Assert.That(LexNumber(text).Number?.IsNegativeZero, Is.True, text);
        }

        foreach (string text in nonZeroCases)
        {
            Assert.That(LexNumber(text).Number?.IsNegativeZero, Is.False, text);
        }
    }

    static JsonToken LexNumber(string text)
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.CaptureRawTokens = true;
        JsonLexer lexer = new JsonLexer(options);
        lexer.FeedBytes(System.Text.Encoding.ASCII.GetBytes(text));
        Assert.That(lexer.ReadNextToken(true), Is.EqualTo(JsonReadStatus.TokenAvailable), text);
        JsonToken token = lexer.GetCurrentToken()
            ?? throw new InvalidOperationException("Expected number token.");
        Assert.That(token.Kind, Is.EqualTo(JsonTokenKind.Number), text);
        Assert.That(lexer.ReadNextToken(true), Is.EqualTo(JsonReadStatus.StreamComplete), text);
        return token;
    }

    static JsonNumberKind GetExpectedKind(bool fraction, bool exponent)
    {
        if (fraction && exponent)
        {
            return JsonNumberKind.FractionWithExponent;
        }

        if (fraction)
        {
            return JsonNumberKind.Fraction;
        }

        if (exponent)
        {
            return JsonNumberKind.Exponent;
        }

        return JsonNumberKind.Integer;
    }

    static void AssertScannerInvalid(string text)
    {
        JsonNumberScanner scanner = new JsonNumberScanner();
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(text);
        JsonParseException? caught = null;
        foreach (byte value in bytes)
        {
            if (caught == null)
            {
                caught = CatchScannerFailure(scanner, value);
            }
        }

        if (caught == null)
        {
            caught = Assert.Throws<JsonParseException>(() => scanner.CompleteNumber());
        }

        JsonParseException exception = caught
            ?? throw new InvalidOperationException($"Expected JsonParseException for {text}.");
        Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidNumber), text);
    }

    static JsonParseException? CatchScannerFailure(JsonNumberScanner scanner, byte value)
    {
        try
        {
            scanner.PushByte(value);
            return null;
        }
        catch (JsonParseException exception)
        {
            return exception;
        }
    }

    static void AssertLexerInvalidNumber(byte[] bytes, string description)
    {
        JsonLexer lexer = new JsonLexer(null);
        lexer.FeedBytes(bytes);
        JsonParseException exception = Assert.Throws<JsonParseException>(() => lexer.ReadNextToken(true))
            ?? throw new InvalidOperationException($"Expected JsonParseException for {description}.");
        Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidNumber), description);
    }
}
