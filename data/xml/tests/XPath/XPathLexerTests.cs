using xml;

namespace xml.Tests;

[TestFixture]
public class XPathLexerTests
{
    [Test]
    public void TokenizesEveryDelimitingTerminalWithLongestMatches()
    {
        string source = "! != # $ ( ) * *: + , - . .. / // : :* :: := < << <= = => > >= >> ? @ Q{urn:test} [ ] { | || }";

        XPathToken[] tokens = new XPathLexer(source, "https://example.test/expression.xpath").TokenizeXPath();

        Assert.That(tokens.Select(token => token.Kind), Is.EqualTo(new[]
        {
            XPathTokenKind.Bang,
            XPathTokenKind.NotEqual,
            XPathTokenKind.Hash,
            XPathTokenKind.Dollar,
            XPathTokenKind.LeftParenthesis,
            XPathTokenKind.RightParenthesis,
            XPathTokenKind.Star,
            XPathTokenKind.StarColon,
            XPathTokenKind.Plus,
            XPathTokenKind.Comma,
            XPathTokenKind.Minus,
            XPathTokenKind.Dot,
            XPathTokenKind.DotDot,
            XPathTokenKind.Slash,
            XPathTokenKind.SlashSlash,
            XPathTokenKind.Colon,
            XPathTokenKind.ColonStar,
            XPathTokenKind.ColonColon,
            XPathTokenKind.ColonEquals,
            XPathTokenKind.LessThan,
            XPathTokenKind.NodeBefore,
            XPathTokenKind.LessThanOrEqual,
            XPathTokenKind.Equals,
            XPathTokenKind.Arrow,
            XPathTokenKind.GreaterThan,
            XPathTokenKind.GreaterThanOrEqual,
            XPathTokenKind.NodeAfter,
            XPathTokenKind.Question,
            XPathTokenKind.At,
            XPathTokenKind.BracedUriLiteral,
            XPathTokenKind.LeftBracket,
            XPathTokenKind.RightBracket,
            XPathTokenKind.LeftBrace,
            XPathTokenKind.Pipe,
            XPathTokenKind.PipePipe,
            XPathTokenKind.RightBrace,
            XPathTokenKind.EndOfInput
        }));
        Assert.Multiple(() =>
        {
            Assert.That(tokens[1].Lexeme, Is.EqualTo("!="));
            Assert.That(tokens[12].Lexeme, Is.EqualTo(".."));
            Assert.That(tokens[29].Value, Is.EqualTo("urn:test"));
            Assert.That(tokens[29].NamespaceUri, Is.EqualTo("urn:test"));
            Assert.That(tokens[0].HasLeadingSeparator, Is.False);
            Assert.That(tokens[1].HasLeadingSeparator, Is.True);
        });
    }

    [Test]
    public void RecognizesEveryKeywordCaseSensitivelyWithoutReservingOtherNames()
    {
        string[] keywords =
        {
            "ancestor", "ancestor-or-self", "and", "array", "as", "attribute", "cast", "castable", "child", "comment",
            "descendant", "descendant-or-self", "div", "document-node", "element", "else", "empty-sequence", "eq", "every", "except",
            "following", "following-sibling", "for", "function", "ge", "gt", "idiv", "if", "in", "instance", "intersect", "is",
            "item", "le", "let", "lt", "map", "mod", "namespace", "namespace-node", "ne", "node", "of", "or", "parent",
            "preceding", "preceding-sibling", "processing-instruction", "return", "satisfies", "schema-attribute", "schema-element", "self",
            "some", "text", "then", "to", "treat", "union"
        };
        string source = string.Join(" ", keywords) + " For FOR custom";

        XPathToken[] tokens = new XPathLexer(source, null).TokenizeXPath();

        Assert.Multiple(() =>
        {
            Assert.That(tokens.Take(keywords.Length).All(token => token.Kind == XPathTokenKind.Keyword), Is.True);
            Assert.That(tokens.Take(keywords.Length).Select(token => token.Value), Is.EqualTo(keywords));
            Assert.That(tokens[keywords.Length].Kind, Is.EqualTo(XPathTokenKind.NcName));
            Assert.That(tokens[keywords.Length + 1].Kind, Is.EqualTo(XPathTokenKind.NcName));
            Assert.That(tokens[keywords.Length + 2].Kind, Is.EqualTo(XPathTokenKind.NcName));
            Assert.That(tokens[keywords.Length + 3].Kind, Is.EqualTo(XPathTokenKind.EndOfInput));
        });
    }

    [Test]
    public void TokenizesNumericLiteralsQNamesAndUriQualifiedNames()
    {
        string source = "0 42 1. .5 6.25 1e3 2.E-2 .5E+4 local π:名 Q{  urn:test  }thing Q{}empty";

        XPathToken[] tokens = new XPathLexer(source, null).TokenizeXPath();

        Assert.Multiple(() =>
        {
            Assert.That(tokens.Select(token => token.Kind), Is.EqualTo(new[]
            {
                XPathTokenKind.IntegerLiteral,
                XPathTokenKind.IntegerLiteral,
                XPathTokenKind.DecimalLiteral,
                XPathTokenKind.DecimalLiteral,
                XPathTokenKind.DecimalLiteral,
                XPathTokenKind.DoubleLiteral,
                XPathTokenKind.DoubleLiteral,
                XPathTokenKind.DoubleLiteral,
                XPathTokenKind.NcName,
                XPathTokenKind.QName,
                XPathTokenKind.UriQualifiedName,
                XPathTokenKind.UriQualifiedName,
                XPathTokenKind.EndOfInput
            }));
            Assert.That(tokens.Take(8).Select(token => token.Value), Is.EqualTo(new[] { "0", "42", "1.", ".5", "6.25", "1e3", "2.E-2", ".5E+4" }));
            Assert.That(tokens[8].LocalName, Is.EqualTo("local"));
            Assert.That(tokens[9].Prefix, Is.EqualTo("π"));
            Assert.That(tokens[9].LocalName, Is.EqualTo("名"));
            Assert.That(tokens[10].NamespaceUri, Is.EqualTo("urn:test"));
            Assert.That(tokens[10].LocalName, Is.EqualTo("thing"));
            Assert.That(tokens[10].Lexeme, Is.EqualTo("Q{  urn:test  }thing"));
            Assert.That(tokens[11].NamespaceUri, Is.EqualTo(""));
            Assert.That(tokens[11].LocalName, Is.EqualTo("empty"));
        });
    }

    [Test]
    public void DecodesDoubledQuotesWithoutExpandingEntityText()
    {
        string source = "'it''s' \"a\"\"b\" '&amp;'";

        XPathToken[] tokens = new XPathLexer(source, null).TokenizeXPath();

        Assert.Multiple(() =>
        {
            Assert.That(tokens.Take(3).All(token => token.Kind == XPathTokenKind.StringLiteral), Is.True);
            Assert.That(tokens.Take(3).Select(token => token.Value), Is.EqualTo(new[] { "it's", "a\"b", "&amp;" }));
            Assert.That(tokens[0].Lexeme, Is.EqualTo("'it''s'"));
            Assert.That(tokens[0].Quote, Is.EqualTo('\''));
            Assert.That(tokens[1].Quote, Is.EqualTo('"'));
        });
    }

    [Test]
    public void SkipsNestedCommentsAndTracksUtf16LinesColumnsAndSeparators()
    {
        string source = " \t(: outer\r\n(: inner :)\nend :) \nQ{urn:test}名+\n2";
        int nameOffset = source.IndexOf("Q{urn:test}名", StringComparison.Ordinal);

        XPathToken[] tokens = new XPathLexer(source, "urn:source").TokenizeXPath();
        XPathToken name = tokens[0];
        XPathToken plus = tokens[1];
        XPathToken number = tokens[2];

        Assert.Multiple(() =>
        {
            Assert.That(tokens.Select(token => token.Kind), Is.EqualTo(new[]
            {
                XPathTokenKind.UriQualifiedName,
                XPathTokenKind.Plus,
                XPathTokenKind.IntegerLiteral,
                XPathTokenKind.EndOfInput
            }));
            Assert.That(name.HasLeadingSeparator, Is.True);
            Assert.That(name.StartUtf16Offset, Is.EqualTo(nameOffset));
            Assert.That(name.Utf16Length, Is.EqualTo("Q{urn:test}名".Length));
            Assert.That(name.Range.Start!.SourceUri, Is.EqualTo("urn:source"));
            Assert.That(name.Range.Start.Line, Is.EqualTo(4));
            Assert.That(name.Range.Start.Column, Is.EqualTo(1));
            Assert.That(name.Range.End!.CharacterOffset, Is.EqualTo(nameOffset + "Q{urn:test}名".Length));
            Assert.That(name.Range.End.Line, Is.EqualTo(4));
            Assert.That(name.Range.End.Column, Is.EqualTo(13));
            Assert.That(plus.HasLeadingSeparator, Is.False);
            Assert.That(number.HasLeadingSeparator, Is.True);
            Assert.That(number.Range.Start!.Line, Is.EqualTo(5));
            Assert.That(number.Range.Start.Column, Is.EqualTo(1));
        });
    }

    [Test]
    public void TreatsCommentMarkersAsContentInsideExplicitLexicalTerminals()
    {
        string source = "'(:not a comment:)' Q{  (:uri:)  }name Q{uri}(: separator :)local";

        XPathToken[] tokens = new XPathLexer(source, null).TokenizeXPath();

        Assert.Multiple(() =>
        {
            Assert.That(tokens.Select(token => token.Kind), Is.EqualTo(new[]
            {
                XPathTokenKind.StringLiteral,
                XPathTokenKind.UriQualifiedName,
                XPathTokenKind.BracedUriLiteral,
                XPathTokenKind.NcName,
                XPathTokenKind.EndOfInput
            }));
            Assert.That(tokens[0].Value, Is.EqualTo("(:not a comment:)"));
            Assert.That(tokens[1].NamespaceUri, Is.EqualTo("(:uri:)"));
            Assert.That(tokens[1].LocalName, Is.EqualTo("name"));
            Assert.That(tokens[2].NamespaceUri, Is.EqualTo("uri"));
            Assert.That(tokens[3].Value, Is.EqualTo("local"));
            Assert.That(tokens[3].HasLeadingSeparator, Is.True);
        });
    }

    [Test]
    public void CountsSupplementaryNameCharactersAsOneSourceColumn()
    {
        XPathToken token = new XPathLexer("😀", null).ReadXPathToken();

        Assert.Multiple(() =>
        {
            Assert.That(token.Kind, Is.EqualTo(XPathTokenKind.NcName));
            Assert.That(token.StartUtf16Offset, Is.Zero);
            Assert.That(token.Utf16Length, Is.EqualTo(2));
            Assert.That(token.Range.Start!.Column, Is.EqualTo(1));
            Assert.That(token.Range.End!.CharacterOffset, Is.EqualTo(2));
            Assert.That(token.Range.End.Column, Is.EqualTo(2));
        });
    }

    [Test]
    public void AppliesSeparatorRulesAfterClassifyingBracedUriTerminals()
    {
        XPathToken[] tokens = new XPathLexer("1Q{}*", null).TokenizeXPath();

        Assert.That(tokens.Select(token => token.Kind), Is.EqualTo(new[]
        {
            XPathTokenKind.IntegerLiteral,
            XPathTokenKind.BracedUriLiteral,
            XPathTokenKind.Star,
            XPathTokenKind.EndOfInput
        }));
        Assert.Throws<XPathStaticException>(() => new XPathLexer("1Q{}local", null).TokenizeXPath());
    }

    [Test]
    public void CommentsActAsSeparatorsAtEveryNonDelimitingBoundary()
    {
        string source = "1(:a:)and(:b:)Q{urn:names}local(:c:)2.5(:d:)name";

        XPathToken[] tokens = new XPathLexer(source, null).TokenizeXPath();

        Assert.Multiple(() =>
        {
            Assert.That(tokens.Select(token => token.Kind), Is.EqualTo(new[]
            {
                XPathTokenKind.IntegerLiteral,
                XPathTokenKind.Keyword,
                XPathTokenKind.UriQualifiedName,
                XPathTokenKind.DecimalLiteral,
                XPathTokenKind.NcName,
                XPathTokenKind.EndOfInput
            }));
            Assert.That(tokens.Skip(1).Take(4).All(token => token.HasLeadingSeparator), Is.True);
            Assert.That(tokens[2].NamespaceUri, Is.EqualTo("urn:names"));
            Assert.That(tokens[2].LocalName, Is.EqualTo("local"));
        });
    }

    [TestCase("1e+", 1, 1, 2)]
    [TestCase("\nQ{bad{uri}name", 6, 2, 6)]
    [TestCase("1and", 1, 1, 2)]
    [TestCase("12..5", 3, 1, 4)]
    public void ReportsExactLexicalErrorLocations(string source, int offset, int line, int column)
    {
        XPathStaticException exception = Assert.Throws<XPathStaticException>(() => new XPathLexer(source, "urn:lexical-error").TokenizeXPath())!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Location!.CharacterOffset, Is.EqualTo(offset));
            Assert.That(exception.Location.Line, Is.EqualTo(line));
            Assert.That(exception.Location.Column, Is.EqualTo(column));
            Assert.That(exception.Location.SourceUri, Is.EqualTo("urn:lexical-error"));
            Assert.That(exception.Diagnostic!.Range!.Start, Is.SameAs(exception.Location));
            Assert.That(exception.Diagnostic.Range.End, Is.SameAs(exception.Location));
        });
    }

    [TestCase("(: unterminated")]
    [TestCase("'unterminated")]
    [TestCase("Q{unterminated")]
    [TestCase("Q{a{b}name")]
    [TestCase("1e+")]
    [TestCase("1and 2")]
    [TestCase("1..2")]
    [TestCase("\\")]
    [TestCase("\u0000")]
    public void ReportsMalformedLexicalInputAsXpSt0003(string source)
    {
        XPathStaticException exception = Assert.Throws<XPathStaticException>(() => new XPathLexer(source, "urn:bad").TokenizeXPath())!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0003));
            Assert.That(exception.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.XPathStatic));
            Assert.That(exception.Diagnostic.Severity, Is.EqualTo(XmlDiagnosticSeverity.Fatal));
            Assert.That(exception.Location, Is.Not.Null);
            Assert.That(exception.Location!.SourceUri, Is.EqualTo("urn:bad"));
            Assert.That(exception.Diagnostic.Range!.Start, Is.SameAs(exception.Location));
        });
    }

    [Test]
    public void RejectsUnpairedUtf16Surrogates()
    {
        string source = new string(new[] { '\uD800' });

        XPathStaticException exception = Assert.Throws<XPathStaticException>(() => new XPathLexer(source, null).TokenizeXPath())!;

        Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0003));
    }
}
