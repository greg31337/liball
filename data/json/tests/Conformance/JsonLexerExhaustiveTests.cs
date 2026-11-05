namespace json.Tests;

public class JsonLexerExhaustiveTests
{
    [Test]
    public void LexerAcceptsOnlyTheExactLowercaseLiteralAcrossEveryCasingPermutation()
    {
        string[] literals = { "true", "false", "null" };

        foreach (string literal in literals)
        {
            int permutationCount = 1 << literal.Length;
            for (int mask = 0; mask < permutationCount; mask++)
            {
                char[] characters = literal.ToCharArray();
                for (int index = 0; index < characters.Length; index++)
                {
                    if ((mask & (1 << index)) != 0)
                    {
                        characters[index] = char.ToUpperInvariant(characters[index]);
                    }
                }

                string candidate = new string(characters);
                JsonLexer lexer = new JsonLexer(null);
                lexer.FeedBytes(System.Text.Encoding.ASCII.GetBytes(candidate + " "));
                if (mask == 0)
                {
                    Assert.That(lexer.ReadNextToken(false), Is.EqualTo(JsonReadStatus.TokenAvailable), candidate);
                    JsonToken token = lexer.GetCurrentToken()
                        ?? throw new InvalidOperationException("Expected literal token.");
                    Assert.That(token.Kind, Is.EqualTo(literal == "null" ? JsonTokenKind.Null : JsonTokenKind.Boolean), candidate);
                }
                else
                {
                    JsonParseException exception = Assert.Throws<JsonParseException>(() => lexer.ReadNextToken(false))
                        ?? throw new InvalidOperationException("Expected JsonParseException.");
                    Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidLiteral), candidate);
                }
            }
        }
    }

    [Test]
    public void LexerAcceptsExactlyFourWhitespaceBytesAndRejectsAllOtherAsciiControls()
    {
        byte[] legal =
        {
            JsonSyntaxBytes.HorizontalTab,
            JsonSyntaxBytes.LineFeed,
            JsonSyntaxBytes.CarriageReturn,
            JsonSyntaxBytes.Space
        };
        byte[] legalSequence = legal.Concat(legal.Reverse()).Concat(System.Text.Encoding.ASCII.GetBytes("null")).ToArray();
        JsonLexer legalLexer = new JsonLexer(null);
        legalLexer.FeedBytes(legalSequence);

        Assert.That(legalLexer.ReadNextToken(true), Is.EqualTo(JsonReadStatus.TokenAvailable));
        Assert.That(legalLexer.GetCurrentToken()?.Kind, Is.EqualTo(JsonTokenKind.Null));

        for (int value = 0; value <= 0x20; value++)
        {
            if (legal.Contains((byte)value))
            {
                continue;
            }

            JsonLexer lexer = new JsonLexer(null);
            lexer.FeedBytes(new byte[] { (byte)value });
            JsonParseException exception = Assert.Throws<JsonParseException>(() => lexer.ReadNextToken(true))
                ?? throw new InvalidOperationException("Expected JsonParseException.");
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidWhitespace), $"0x{value:X2}");
        }
    }

    [Test]
    public void LexerRejectsRepresentativeNonJsonUnicodeWhitespace()
    {
        int[] whitespaceScalars =
        {
            0x0085,
            0x00A0,
            0x1680,
            0x2000,
            0x2001,
            0x2002,
            0x2003,
            0x2004,
            0x2005,
            0x2006,
            0x2007,
            0x2008,
            0x2009,
            0x200A,
            0x2028,
            0x2029,
            0x202F,
            0x205F,
            0x3000,
            0xFEFF
        };

        foreach (int scalar in whitespaceScalars)
        {
            JsonLexer lexer = new JsonLexer(null);
            lexer.FeedBytes(System.Text.Encoding.UTF8.GetBytes(char.ConvertFromUtf32(scalar)));
            Assert.That(() => lexer.ReadNextToken(true), Throws.TypeOf<JsonParseException>(), $"U+{scalar:X4}");
        }
    }

    [Test]
    public void RawCaptureOwnsEveryLexemeIndependentlyOfFeedsAndLaterTokens()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.CaptureRawTokens = true;
        JsonLexer lexer = new JsonLexer(options);
        byte[] input = System.Text.Encoding.UTF8.GetBytes("\"x\":true,null,12,\"x\"");
        byte[] expectedInput = (byte[])input.Clone();
        lexer.FeedBytes(input);
        Array.Fill(input, (byte)0x78);

        List<byte[]> captures = new List<byte[]>();
        while (true)
        {
            JsonReadStatus status = lexer.ReadNextToken(true);
            if (status == JsonReadStatus.StreamComplete)
            {
                break;
            }

            Assert.That(status, Is.EqualTo(JsonReadStatus.TokenAvailable));
            byte[] raw = lexer.GetCurrentRawBytes()
                ?? throw new InvalidOperationException("Expected raw capture.");
            captures.Add(raw);
        }

        byte[][] expectedCaptures =
        {
            System.Text.Encoding.ASCII.GetBytes("\"x\""),
            new byte[] { JsonSyntaxBytes.NameSeparator },
            System.Text.Encoding.ASCII.GetBytes("true"),
            new byte[] { JsonSyntaxBytes.ValueSeparator },
            System.Text.Encoding.ASCII.GetBytes("null"),
            new byte[] { JsonSyntaxBytes.ValueSeparator },
            System.Text.Encoding.ASCII.GetBytes("12"),
            new byte[] { JsonSyntaxBytes.ValueSeparator },
            System.Text.Encoding.ASCII.GetBytes("\"x\"")
        };

        Assert.Multiple(() =>
        {
            Assert.That(captures, Has.Count.EqualTo(expectedCaptures.Length));
            Assert.That(captures, Is.EqualTo(expectedCaptures));
            Assert.That(captures[0], Is.Not.SameAs(captures[8]));
            Assert.That(expectedInput, Is.EqualTo(System.Text.Encoding.UTF8.GetBytes("\"x\":true,null,12,\"x\"")));
        });

        captures[0][1] = 0x79;
        Assert.That(captures[8], Is.EqualTo(System.Text.Encoding.ASCII.GetBytes("\"x\"")));
    }

    [Test]
    public void NestedLexemesKeepAbsoluteDepthIndependentRanges()
    {
        string text = "{\r\n\"a\":[0,{\"b\":\"€\"}]}";
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);
        JsonReaderOptions options = new JsonReaderOptions();
        options.TrackLineInformation = true;
        options.TrackUnicodeScalarColumn = true;
        options.CaptureRawTokens = true;
        JsonLexer lexer = new JsonLexer(options);
        lexer.FeedBytes(bytes);

        int[] expectedStarts = { 0, 3, 6, 7, 8, 9, 10, 11, 14, 15, 20, 21, 22 };
        int[] expectedEnds = { 1, 6, 7, 8, 9, 10, 11, 14, 15, 20, 21, 22, 23 };
        List<JsonToken> tokens = new List<JsonToken>();

        for (int index = 0; index < expectedStarts.Length; index++)
        {
            Assert.That(lexer.ReadNextToken(true), Is.EqualTo(JsonReadStatus.TokenAvailable), $"lexeme {index}");
            JsonSourceRange range = lexer.GetCurrentRange()
                ?? throw new InvalidOperationException("Expected source range.");
            Assert.Multiple(() =>
            {
                Assert.That(range.Start?.ByteOffset, Is.EqualTo(expectedStarts[index]), $"lexeme {index} start");
                Assert.That(range.End?.ByteOffset, Is.EqualTo(expectedEnds[index]), $"lexeme {index} end");
            });

            JsonToken? token = lexer.GetCurrentToken();
            if (token != null)
            {
                tokens.Add(token);
                Assert.That(token.Depth, Is.Zero, $"lexeme {index}");
            }
        }

        Assert.That(lexer.ReadNextToken(true), Is.EqualTo(JsonReadStatus.StreamComplete));
        JsonToken euro = tokens.Single(token => token.StringValue == "€");
        Assert.Multiple(() =>
        {
            AssertPosition(euro.Range?.Start, 15, 2, 13, 13);
            AssertPosition(euro.Range?.End, 20, 2, 18, 16);
            Assert.That(tokens, Is.All.Matches<JsonToken>(token => token.Depth == 0));
        });
    }

    [Test]
    public void ByteAtATimeAndPatternedChunksMatchOneShotLexing()
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(" \r\n{\"a\":[true,-0.1E+2,\"€\\n😀\",null]}");
        List<string> expected = LexWithChunks(bytes, new int[] { bytes.Length });
        List<string> byteAtATime = LexWithChunks(bytes, Enumerable.Repeat(1, bytes.Length).ToArray());
        List<string> patterned = LexWithChunks(bytes, new int[] { 1, 2, 3, 4, 1, 3, 2, 5, 1, 1, 4, 2, 3, 6, 1, 2, 4, 8 });

        Assert.Multiple(() =>
        {
            Assert.That(byteAtATime, Is.EqualTo(expected));
            Assert.That(patterned, Is.EqualTo(expected));
        });
    }

    [Test]
    public void RawTokenAndNumberLimitsAcceptExactLengthsAndRejectOneBeyond()
    {
        (string Text, int TokenLength)[] exactCases =
        {
            ("true", 4),
            ("false", 5),
            ("null", 4),
            ("\"€\"", 5),
            ("-1.2e+3", 7),
            ("{", 1)
        };

        foreach ((string text, int tokenLength) in exactCases)
        {
            JsonLexer exact = CreateLimitedLexer(tokenLength, 100);
            exact.FeedBytes(System.Text.Encoding.UTF8.GetBytes(text));
            Assert.That(exact.ReadNextToken(true), Is.EqualTo(JsonReadStatus.TokenAvailable), text);

            if (tokenLength > 1)
            {
                JsonLexer over = CreateLimitedLexer(tokenLength - 1, 100);
                over.FeedBytes(System.Text.Encoding.UTF8.GetBytes(text));
                JsonLimitException exception = Assert.Throws<JsonLimitException>(() => over.ReadNextToken(true))
                    ?? throw new InvalidOperationException("Expected JsonLimitException.");
                Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.TokenByteLimitExceeded), text);
            }
        }

        JsonLexer exactNumber = CreateLimitedLexer(100, 7);
        exactNumber.FeedBytes(System.Text.Encoding.ASCII.GetBytes("-1.2e+3"));
        Assert.That(exactNumber.ReadNextToken(true), Is.EqualTo(JsonReadStatus.TokenAvailable));

        JsonLexer longNumber = CreateLimitedLexer(100, 6);
        longNumber.FeedBytes(System.Text.Encoding.ASCII.GetBytes("-1.2e+3"));
        JsonLimitException numberException = Assert.Throws<JsonLimitException>(() => longNumber.ReadNextToken(true))
            ?? throw new InvalidOperationException("Expected JsonLimitException.");
        Assert.That(numberException.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.NumberCharacterLimitExceeded));
    }

    [Test]
    public void TokenLimitStatePersistsAcrossChunkBoundaries()
    {
        JsonLexer lexer = CreateLimitedLexer(5, 100);
        lexer.FeedBytes(System.Text.Encoding.ASCII.GetBytes("\"ab"));
        Assert.That(lexer.ReadNextToken(false), Is.EqualTo(JsonReadStatus.NeedMoreData));
        lexer.FeedBytes(System.Text.Encoding.ASCII.GetBytes("cd\""));

        JsonLimitException exception = Assert.Throws<JsonLimitException>(() => lexer.ReadNextToken(true))
            ?? throw new InvalidOperationException("Expected JsonLimitException.");
        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.TokenByteLimitExceeded));
            Assert.That(lexer.GetConsumedByteCount(), Is.EqualTo(5));
            Assert.That(lexer.GetBufferedByteCount(), Is.EqualTo(1));
        });
    }

    static List<string> LexWithChunks(byte[] bytes, int[] chunkSizes)
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.CaptureRawTokens = true;
        options.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
        JsonLexer lexer = new JsonLexer(options);
        List<string> signatures = new List<string>();
        int offset = 0;
        int chunkIndex = 0;
        while (offset < bytes.Length)
        {
            int requested = chunkSizes[chunkIndex % chunkSizes.Length];
            int length = Math.Min(requested, bytes.Length - offset);
            lexer.FeedBytes(new ReadOnlySpan<byte>(bytes, offset, length));
            offset += length;
            chunkIndex++;
            CollectAvailable(lexer, false, signatures);
        }

        CollectAvailable(lexer, true, signatures);
        return signatures;
    }

    static void CollectAvailable(JsonLexer lexer, bool finalInput, List<string> signatures)
    {
        while (true)
        {
            JsonReadStatus status = lexer.ReadNextToken(finalInput);
            if (status == JsonReadStatus.NeedMoreData || status == JsonReadStatus.StreamComplete)
            {
                return;
            }

            Assert.That(status, Is.EqualTo(JsonReadStatus.TokenAvailable));
            string raw = Convert.ToHexString(lexer.GetCurrentRawBytes() ?? Array.Empty<byte>());
            JsonToken? token = lexer.GetCurrentToken();
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

    static JsonLexer CreateLimitedLexer(int maximumTokenBytes, int maximumNumberCharacters)
    {
        JsonLimits limits = new JsonLimits();
        limits.MaximumTokenBytes = maximumTokenBytes;
        limits.MaximumNumberCharacters = maximumNumberCharacters;
        JsonReaderOptions options = new JsonReaderOptions();
        options.Limits = limits;
        return new JsonLexer(options);
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
