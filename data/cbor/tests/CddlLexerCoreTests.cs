using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlLexerCoreTests
{
    [Test]
    public void TokenizesEveryPunctuationAndMultiCharacterOperator()
    {
        CddlLexer lexer = Lexer(
            "= /= //= / // ... .. . ( ) { } [ ] < > , : => ^ ? * + ~ & #",
            false);
        CddlToken[] tokens = ReadAll(lexer);

        Assert.That(tokens.Select(token => token.Kind), Is.EqualTo(new CddlTokenKind[]
        {
            CddlTokenKind.Equal,
            CddlTokenKind.TypeExtensionAssign,
            CddlTokenKind.GroupExtensionAssign,
            CddlTokenKind.Slash,
            CddlTokenKind.GroupChoice,
            CddlTokenKind.RangeExclusive,
            CddlTokenKind.RangeInclusive,
            CddlTokenKind.Dot,
            CddlTokenKind.LeftParenthesis,
            CddlTokenKind.RightParenthesis,
            CddlTokenKind.LeftBrace,
            CddlTokenKind.RightBrace,
            CddlTokenKind.LeftBracket,
            CddlTokenKind.RightBracket,
            CddlTokenKind.LessThan,
            CddlTokenKind.GreaterThan,
            CddlTokenKind.Comma,
            CddlTokenKind.Colon,
            CddlTokenKind.Arrow,
            CddlTokenKind.Cut,
            CddlTokenKind.Optional,
            CddlTokenKind.Asterisk,
            CddlTokenKind.Plus,
            CddlTokenKind.Unwrap,
            CddlTokenKind.GroupEnumeration,
            CddlTokenKind.Hash,
            CddlTokenKind.EndOfFile
        }));
    }

    [Test]
    public void ReadsMaximalIdentifiersWithoutStealingAdjacentRangeOperators()
    {
        CddlLexer lexer = Lexer(
            "alpha @root _name $socket $$plug a-b.c a--b..9 name... 4",
            false);
        CddlToken[] tokens = ReadAll(lexer);

        Assert.Multiple(() =>
        {
            Assert.That(tokens.Take(8).Select(token => token.Kind),
                Is.All.EqualTo(CddlTokenKind.Identifier));
            Assert.That(tokens.Take(8).Select(token => token.Text), Is.EqualTo(new string[]
            {
                "alpha", "@root", "_name", "$socket", "$$plug", "a-b.c",
                "a--b..9", "name"
            }));
            Assert.That(tokens[8].Kind, Is.EqualTo(CddlTokenKind.RangeExclusive));
            Assert.That(tokens[8].Text, Is.EqualTo("..."));
            Assert.That(tokens[9].Kind, Is.EqualTo(CddlTokenKind.Number));
            Assert.That(tokens[9].Text, Is.EqualTo("4"));
        });
    }

    [Test]
    public void CoversEveryIdentifierStartAndContinuationProduction()
    {
        CddlLexer lexer = Lexer(
            "A z @ _ $ a0 a-b a.b a--..$9 hname b64name",
            false);
        CddlToken[] tokens = ReadAll(lexer);

        Assert.Multiple(() =>
        {
            Assert.That(tokens.Take(11).Select(token => token.Kind),
                Is.All.EqualTo(CddlTokenKind.Identifier));
            Assert.That(tokens.Take(11).Select(token => token.Text),
                Is.EqualTo(new string[]
                {
                    "A", "z", "@", "_", "$", "a0", "a-b", "a.b",
                    "a--..$9", "hname", "b64name"
                }));
            Assert.That(tokens[11].Kind, Is.EqualTo(CddlTokenKind.EndOfFile));
        });
    }

    [Test]
    public void RecognizesCaseInsensitiveByteQualifiersOnlyAtQuoteBoundaries()
    {
        CddlLexer lexer = Lexer(
            "h'00' H'01' b64'AA' B64'AQ' hname B64name",
            false);
        CddlToken[] tokens = ReadAll(lexer);

        Assert.Multiple(() =>
        {
            Assert.That(tokens.Select(token => token.Kind),
                Is.EqualTo(new CddlTokenKind[]
                {
                    CddlTokenKind.ByteString,
                    CddlTokenKind.ByteString,
                    CddlTokenKind.ByteString,
                    CddlTokenKind.ByteString,
                    CddlTokenKind.Identifier,
                    CddlTokenKind.Identifier,
                    CddlTokenKind.EndOfFile
                }));
            Assert.That(tokens[1].ByteStringValue, Is.EqualTo(new byte[] { 1 }));
            Assert.That(tokens[3].ByteStringValue, Is.EqualTo(new byte[] { 1 }));
            Assert.That(tokens[4].Text, Is.EqualTo("hname"));
            Assert.That(tokens[5].Text, Is.EqualTo("B64name"));
        });
    }

    [Test]
    public void SeparatesMajorTypeDigitsFromTheirOptionalHeadNumbers()
    {
        CddlToken[] tokens = ReadAll(Lexer(
            "#6.24 #7.<tag-number> #0.31 # 6.24",
            false));

        Assert.Multiple(() =>
        {
            Assert.That(tokens.Select(token => token.Kind),
                Is.EqualTo(new CddlTokenKind[]
                {
                    CddlTokenKind.Hash,
                    CddlTokenKind.Number,
                    CddlTokenKind.Dot,
                    CddlTokenKind.Number,
                    CddlTokenKind.Hash,
                    CddlTokenKind.Number,
                    CddlTokenKind.Dot,
                    CddlTokenKind.LessThan,
                    CddlTokenKind.Identifier,
                    CddlTokenKind.GreaterThan,
                    CddlTokenKind.Hash,
                    CddlTokenKind.Number,
                    CddlTokenKind.Dot,
                    CddlTokenKind.Number,
                    CddlTokenKind.Hash,
                    CddlTokenKind.Number,
                    CddlTokenKind.EndOfFile
                }));
            Assert.That(tokens[1].Text, Is.EqualTo("6"));
            Assert.That(tokens[3].Text, Is.EqualTo("24"));
            Assert.That(tokens[11].Text, Is.EqualTo("0"));
            Assert.That(tokens[13].Text, Is.EqualTo("31"));
            Assert.That(tokens[15].Text, Is.EqualTo("6.24"));
        });
    }

    [Test]
    public void KeepsQuotedAndNumericFormsWholeForDedicatedLiteralParsers()
    {
        CddlLexer lexer = Lexer(
            "\"a=>b\" 'x,y' h'00 ff' b64'YWJj' 0x1.fp2 -12.5e+2 1..2",
            false);
        CddlToken[] tokens = ReadAll(lexer);

        Assert.Multiple(() =>
        {
            Assert.That(tokens.Select(token => token.Kind), Is.EqualTo(new CddlTokenKind[]
            {
                CddlTokenKind.TextString,
                CddlTokenKind.ByteString,
                CddlTokenKind.ByteString,
                CddlTokenKind.ByteString,
                CddlTokenKind.Number,
                CddlTokenKind.Number,
                CddlTokenKind.Number,
                CddlTokenKind.RangeInclusive,
                CddlTokenKind.Number,
                CddlTokenKind.EndOfFile
            }));
            Assert.That(tokens.Take(6).Select(token => token.Text), Is.EqualTo(new string[]
            {
                "\"a=>b\"", "'x,y'", "h'00 ff'", "b64'YWJj'",
                "0x1.fp2", "-12.5e+2"
            }));
            Assert.That(tokens[6].Text, Is.EqualTo("1"));
            Assert.That(tokens[7].Text, Is.EqualTo(".."));
            Assert.That(tokens[8].Text, Is.EqualTo("2"));
        });
    }

    [Test]
    public void RetainsWhitespaceAndUnicodeCommentsWithExactSpansWhenRequested()
    {
        string source = "  ; ascii 😀\r\n \nroot";
        CddlLexer lexer = Lexer(source, true);
        CddlToken root = lexer.ReadToken();
        CddlToken end = lexer.ReadToken();

        Assert.Multiple(() =>
        {
            Assert.That(root.Kind, Is.EqualTo(CddlTokenKind.Identifier));
            Assert.That(root.Text, Is.EqualTo("root"));
            Assert.That(root.LeadingTrivia, Has.Count.EqualTo(3));
            Assert.That(root.LeadingTrivia.Select(item => item.Kind),
                Is.EqualTo(new CddlTriviaKind[]
                {
                    CddlTriviaKind.Whitespace,
                    CddlTriviaKind.Comment,
                    CddlTriviaKind.Whitespace
                }));
            Assert.That(root.LeadingTrivia[0].Text, Is.EqualTo("  "));
            Assert.That(root.LeadingTrivia[1].Text, Is.EqualTo("; ascii 😀\r\n"));
            Assert.That(root.LeadingTrivia[1].Span.StartLine, Is.EqualTo(1));
            Assert.That(root.LeadingTrivia[1].Span.StartColumn, Is.EqualTo(3));
            Assert.That(root.LeadingTrivia[1].Span.EndLine, Is.EqualTo(2));
            Assert.That(root.LeadingTrivia[1].Span.EndColumn, Is.EqualTo(1));
            Assert.That(root.Span.SourceName, Is.EqualTo("schema.cddl"));
            Assert.That(root.Span.Start, Is.EqualTo(source.IndexOf("root")));
            Assert.That(root.Span.Length, Is.EqualTo(4));
            Assert.That(root.Span.StartLine, Is.EqualTo(3));
            Assert.That(root.Span.StartColumn, Is.EqualTo(1));
            Assert.That(root.Span.EndLine, Is.EqualTo(3));
            Assert.That(root.Span.EndColumn, Is.EqualTo(5));
            Assert.That(end.Kind, Is.EqualTo(CddlTokenKind.EndOfFile));
        });
    }

    [Test]
    public void AcceptsEveryCommentScalarRangeAndNewlineForm()
    {
        string maximum = char.ConvertFromUtf32(0x10fffd);
        string source = "; ~\u00a0\ud7ff\ue000\uffff" + maximum
            + "\n;\r\nroot";
        CddlLexer lexer = Lexer(source, true);
        CddlToken root = lexer.ReadToken();

        Assert.Multiple(() =>
        {
            Assert.That(root.Kind, Is.EqualTo(CddlTokenKind.Identifier));
            Assert.That(root.LeadingTrivia, Has.Count.EqualTo(2));
            Assert.That(root.LeadingTrivia[0].Kind,
                Is.EqualTo(CddlTriviaKind.Comment));
            Assert.That(root.LeadingTrivia[0].Text, Does.EndWith("\n"));
            Assert.That(root.LeadingTrivia[1].Text, Is.EqualTo(";\r\n"));
            Assert.That(root.Span.StartLine, Is.EqualTo(3));
            Assert.That(root.Span.StartColumn, Is.EqualTo(1));
        });
    }

    [Test]
    public void DiscardsTriviaWhenRetentionIsDisabled()
    {
        CddlLexer lexer = Lexer(" ; comment\n root", false);
        CddlToken root = lexer.ReadToken();

        Assert.Multiple(() =>
        {
            Assert.That(root.Kind, Is.EqualTo(CddlTokenKind.Identifier));
            Assert.That(root.Text, Is.EqualTo("root"));
            Assert.That(root.LeadingTrivia, Is.Empty);
            Assert.That(root.Span.StartLine, Is.EqualTo(2));
            Assert.That(root.Span.StartColumn, Is.EqualTo(2));
        });
    }

    [Test]
    public void TracksUtf16OffsetsAndLineColumnsAcrossLfAndCrlf()
    {
        CddlLexer lexer = Lexer("a\n  b\r\nc", true);
        CddlToken[] tokens = ReadAll(lexer);

        Assert.Multiple(() =>
        {
            AssertSpan(tokens[0], 0, 1, 1, 1, 1, 2);
            AssertSpan(tokens[1], 4, 1, 2, 3, 2, 4);
            AssertSpan(tokens[2], 7, 1, 3, 1, 3, 2);
            Assert.That(tokens[1].LeadingTrivia[0].Text, Is.EqualTo("\n  "));
            Assert.That(tokens[2].LeadingTrivia[0].Text, Is.EqualTo("\r\n"));
        });
    }

    [Test]
    public void RejectsWhitespaceAndCommentsOutsideTheUpdatedRfcGrammar()
    {
        CddlLexer invalidWhitespace = Lexer("\troot", false);
        CddlLexer bareCarriageReturn = Lexer("\rroot", false);
        CddlLexer invalidComment = Lexer(";bad\tcomment\n", false);
        CddlLexer invalidC1Comment = Lexer(";bad\u0085comment\n", false);
        CddlLexer invalidNoncharacterComment = Lexer(
            ";bad" + char.ConvertFromUtf32(0x10ffff) + "comment\n",
            false);
        CddlLexer unterminatedComment = Lexer("; no newline", false);

        CddlToken whitespace = invalidWhitespace.ReadToken();
        CddlToken carriageReturn = bareCarriageReturn.ReadToken();
        CddlToken comment = invalidComment.ReadToken();
        CddlToken c1Comment = invalidC1Comment.ReadToken();
        CddlToken noncharacterComment = invalidNoncharacterComment.ReadToken();
        CddlToken unterminated = unterminatedComment.ReadToken();

        Assert.Multiple(() =>
        {
            AssertInvalid(whitespace, "CDDL_LEX_WHITESPACE_INVALID");
            AssertInvalid(carriageReturn, "CDDL_LEX_WHITESPACE_INVALID");
            AssertInvalid(comment, "CDDL_LEX_COMMENT_CHARACTER_INVALID");
            AssertInvalid(c1Comment, "CDDL_LEX_COMMENT_CHARACTER_INVALID");
            AssertInvalid(noncharacterComment, "CDDL_LEX_COMMENT_CHARACTER_INVALID");
            AssertInvalid(unterminated, "CDDL_LEX_COMMENT_UNTERMINATED");
            Assert.That(invalidComment.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(invalidComment.Diagnostics[0].Span.SourceName,
                Is.EqualTo("schema.cddl"));
        });
    }

    [Test]
    public void InvalidCharactersAdvanceByOneUnicodeScalarAndReportSurrogatesPrecisely()
    {
        CddlLexer punctuation = Lexer("!root", false);
        CddlLexer scalar = Lexer("😀root", false);
        CddlLexer surrogate = Lexer("\ud800root", false);

        CddlToken punctuationError = punctuation.ReadToken();
        CddlToken punctuationRoot = punctuation.ReadToken();
        CddlToken scalarError = scalar.ReadToken();
        CddlToken scalarRoot = scalar.ReadToken();
        CddlToken surrogateError = surrogate.ReadToken();
        CddlToken surrogateRoot = surrogate.ReadToken();

        Assert.Multiple(() =>
        {
            AssertInvalid(punctuationError, "CDDL_LEX_CHARACTER_INVALID");
            Assert.That(punctuationRoot.Text, Is.EqualTo("root"));
            AssertInvalid(scalarError, "CDDL_LEX_CHARACTER_INVALID");
            Assert.That(scalarError.Span.Length, Is.EqualTo(2));
            Assert.That(scalarRoot.Span.Start, Is.EqualTo(2));
            AssertInvalid(surrogateError, "CDDL_LEX_SURROGATE_INVALID");
            Assert.That(surrogateError.Span.Length, Is.EqualTo(1));
            Assert.That(surrogateRoot.Span.Start, Is.EqualTo(1));
        });
    }

    [Test]
    public void BoundsCollectedDiagnosticsWithoutStoppingTokenProgress()
    {
        CddlParseOptions options = new CddlParseOptions
        {
            SourceName = "bounded.cddl",
            MaxDiagnostics = 1
        };
        CddlLexer lexer = new CddlLexer("!\troot", options);

        CddlToken first = lexer.ReadToken();
        CddlToken second = lexer.ReadToken();
        CddlToken root = lexer.ReadToken();

        Assert.Multiple(() =>
        {
            Assert.That(first.Kind, Is.EqualTo(CddlTokenKind.Invalid));
            Assert.That(second.Kind, Is.EqualTo(CddlTokenKind.Invalid));
            Assert.That(root.Text, Is.EqualTo("root"));
            Assert.That(lexer.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(lexer.Diagnostics[0].Code,
                Is.EqualTo("CDDL_LEX_CHARACTER_INVALID"));
        });
    }

    private static CddlLexer Lexer(string source, bool retainTrivia)
    {
        return new CddlLexer(source, new CddlParseOptions
        {
            SourceName = "schema.cddl",
            RetainTrivia = retainTrivia
        });
    }

    private static CddlToken[] ReadAll(CddlLexer lexer)
    {
        List<CddlToken> tokens = new List<CddlToken>();
        bool complete = false;
        while (!complete)
        {
            CddlToken token = lexer.ReadToken();
            tokens.Add(token);
            complete = token.Kind == CddlTokenKind.EndOfFile;
        }
        return tokens.ToArray();
    }

    private static void AssertSpan(
        CddlToken token,
        int start,
        int length,
        int startLine,
        int startColumn,
        int endLine,
        int endColumn)
    {
        Assert.Multiple(() =>
        {
            Assert.That(token.Span.Start, Is.EqualTo(start));
            Assert.That(token.Span.Length, Is.EqualTo(length));
            Assert.That(token.Span.StartLine, Is.EqualTo(startLine));
            Assert.That(token.Span.StartColumn, Is.EqualTo(startColumn));
            Assert.That(token.Span.EndLine, Is.EqualTo(endLine));
            Assert.That(token.Span.EndColumn, Is.EqualTo(endColumn));
        });
    }

    private static void AssertInvalid(CddlToken token, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(token.Kind, Is.EqualTo(CddlTokenKind.Invalid));
            Assert.That(token.ErrorCode, Is.EqualTo(code));
            Assert.That(token.ErrorMessage, Is.Not.Empty);
        });
    }
}
