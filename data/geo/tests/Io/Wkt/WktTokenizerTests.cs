using System.Globalization;
using geo;

namespace geo.Tests;

public class WktTokenizerTests
{
    [Test]
    public void TokenizesKeywordsDimensionsNumbersAndPunctuationWithExactOffsets()
    {
        WktTokenizer tokenizer = Tokenizer("point ZM (1.5 -2E+1, .25 +3.)");

        AssertKeyword(tokenizer.ReadToken(), "POINT", 0, 5);
        AssertKeyword(tokenizer.ReadToken(), "ZM", 6, 2);
        AssertSimple(tokenizer.ReadToken(), WktTokenKind.LeftParenthesis, 9);
        AssertNumber(tokenizer.ReadToken(), "1.5", 1.5, 10, 3);
        AssertNumber(tokenizer.ReadToken(), "-2E+1", -20.0, 14, 5);
        AssertSimple(tokenizer.ReadToken(), WktTokenKind.Comma, 19);
        AssertNumber(tokenizer.ReadToken(), ".25", 0.25, 21, 3);
        AssertNumber(tokenizer.ReadToken(), "+3.", 3.0, 25, 3);
        AssertSimple(tokenizer.ReadToken(), WktTokenKind.RightParenthesis, 28);
        WktToken end = tokenizer.ReadToken();
        Assert.Multiple(() =>
        {
            Assert.That(end.Kind, Is.EqualTo(WktTokenKind.End));
            Assert.That(end.Offset, Is.EqualTo(29));
            Assert.That(tokenizer.Depth, Is.EqualTo(0));
        });
    }

    [Test]
    public void RecognizesEwktAndEmptyKeywordsWithoutMakingTypeDecisions()
    {
        WktTokenizer tokenizer = Tokenizer("  srid=4326;Triangle M EMPTY");

        AssertKeyword(tokenizer.ReadToken(), "SRID", 2, 4);
        AssertSimple(tokenizer.ReadToken(), WktTokenKind.Equals, 6);
        AssertNumber(tokenizer.ReadToken(), "4326", 4326.0, 7, 4);
        AssertSimple(tokenizer.ReadToken(), WktTokenKind.Semicolon, 11);
        AssertKeyword(tokenizer.ReadToken(), "TRIANGLE", 12, 8);
        AssertKeyword(tokenizer.ReadToken(), "M", 21, 1);
        AssertKeyword(tokenizer.ReadToken(), "EMPTY", 23, 5);
        Assert.That(tokenizer.ReadToken().Kind, Is.EqualTo(WktTokenKind.End));
    }

    [Test]
    [NonParallelizable]
    public void NumericParsingIsInvariantAndSupportsTheCompleteDecimalGrammar()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            WktTokenizer tokenizer = Tokenizer("(-0 +1 1. .5 6.02e23 -4E-2)");
            AssertSimple(tokenizer.ReadToken(), WktTokenKind.LeftParenthesis, 0);
            WktToken negativeZero = tokenizer.ReadToken();
            AssertNumber(negativeZero, "-0", -0.0, 1, 2);
            Assert.That(BitConverter.DoubleToInt64Bits(negativeZero.Number), Is.EqualTo(BitConverter.DoubleToInt64Bits(-0.0)));
            AssertNumber(tokenizer.ReadToken(), "+1", 1.0, 4, 2);
            AssertNumber(tokenizer.ReadToken(), "1.", 1.0, 7, 2);
            AssertNumber(tokenizer.ReadToken(), ".5", 0.5, 10, 2);
            AssertNumber(tokenizer.ReadToken(), "6.02e23", 6.02e23, 13, 7);
            AssertNumber(tokenizer.ReadToken(), "-4E-2", -0.04, 21, 5);
            AssertSimple(tokenizer.ReadToken(), WktTokenKind.RightParenthesis, 26);
            Assert.That(tokenizer.ReadToken().Kind, Is.EqualTo(WktTokenKind.End));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Test]
    public void ReportsMalformedAndOutOfRangeNumbersAtTheOffendingCharacter()
    {
        AssertTokenError("1e+", ParseErrorKind.InvalidNumber, 3, "exponent");
        AssertTokenError("1.2.3", ParseErrorKind.InvalidNumber, 3, "numeric token");
        AssertTokenError("12abc", ParseErrorKind.InvalidNumber, 2, "numeric token");
        AssertTokenError("1-2", ParseErrorKind.InvalidNumber, 1, "numeric token");
        AssertTokenError(".", ParseErrorKind.InvalidNumber, 0, "digit");
        AssertTokenError("1e999", ParseErrorKind.InvalidNumber, 0, "finite double");
    }

    [Test]
    public void EnforcesNestingAndTokenLengthBounds()
    {
        WktReadOptions shallowOptions = new WktReadOptions { MaximumNestingDepth = 1 };
        WktTokenizer shallow = new WktTokenizer("((1))", shallowOptions);
        AssertSimple(shallow.ReadToken(), WktTokenKind.LeftParenthesis, 0);
        AssertError(shallow.ReadToken(), ParseErrorKind.LimitExceeded, 1, "nesting depth");

        AssertTokenError(new string('A', WktTokenizer.MaximumKeywordLength + 1), ParseErrorKind.LimitExceeded,
            WktTokenizer.MaximumKeywordLength, "keyword");
        AssertTokenError(new string('1', WktTokenizer.MaximumNumberLength + 1), ParseErrorKind.LimitExceeded,
            WktTokenizer.MaximumNumberLength, "numeric token");

        WktTokenizer noCoordinates = new WktTokenizer("POINT EMPTY", new WktReadOptions { MaximumCoordinates = 0 });
        WktTokenizer noComponents = new WktTokenizer("POINT EMPTY", new WktReadOptions { MaximumComponents = 0 });
        WktTokenizer negativeDepth = new WktTokenizer("POINT EMPTY", new WktReadOptions { MaximumNestingDepth = -1 });
        AssertError(noCoordinates.ReadToken(), ParseErrorKind.LimitExceeded, 0, "coordinate count");
        AssertError(noComponents.ReadToken(), ParseErrorKind.LimitExceeded, 0, "component count");
        AssertError(negativeDepth.ReadToken(), ParseErrorKind.LimitExceeded, 0, "nesting depth");
    }

    [Test]
    public void ReportsUnexpectedCharactersClosuresAndUnclosedInputWithStableFailures()
    {
        AssertTokenError("@", ParseErrorKind.UnexpectedToken, 0, "Unexpected character");
        AssertTokenError(")", ParseErrorKind.UnexpectedToken, 0, "closing parenthesis");

        WktTokenizer unclosed = Tokenizer("POINT (1 2");
        AssertKeyword(unclosed.ReadToken(), "POINT", 0, 5);
        AssertSimple(unclosed.ReadToken(), WktTokenKind.LeftParenthesis, 6);
        AssertNumber(unclosed.ReadToken(), "1", 1.0, 7, 1);
        AssertNumber(unclosed.ReadToken(), "2", 2.0, 9, 1);
        WktToken failure = unclosed.ReadToken();
        AssertError(failure, ParseErrorKind.UnexpectedEnd, 10, "parentheses");
        WktToken repeated = unclosed.ReadToken();
        Assert.Multiple(() =>
        {
            Assert.That(repeated.Kind, Is.EqualTo(WktTokenKind.Invalid));
            Assert.That(repeated.Error, Is.SameAs(failure.Error));
        });
    }

    [Test]
    public void HandlesNullInputsAndReturnsRepeatableEndTokens()
    {
        WktTokenizer nullSource = new WktTokenizer(null!, new WktReadOptions());
        WktTokenizer nullOptions = new WktTokenizer("POINT EMPTY", null!);
        AssertError(nullSource.ReadToken(), ParseErrorKind.UnexpectedEnd, 0, "null");
        AssertError(nullOptions.ReadToken(), ParseErrorKind.LimitExceeded, 0, "options");

        WktTokenizer balanced = Tokenizer("(())");
        AssertSimple(balanced.ReadToken(), WktTokenKind.LeftParenthesis, 0);
        AssertSimple(balanced.ReadToken(), WktTokenKind.LeftParenthesis, 1);
        AssertSimple(balanced.ReadToken(), WktTokenKind.RightParenthesis, 2);
        AssertSimple(balanced.ReadToken(), WktTokenKind.RightParenthesis, 3);
        WktToken firstEnd = balanced.ReadToken();
        WktToken secondEnd = balanced.ReadToken();
        Assert.Multiple(() =>
        {
            Assert.That(firstEnd.Kind, Is.EqualTo(WktTokenKind.End));
            Assert.That(firstEnd.Offset, Is.EqualTo(4));
            Assert.That(secondEnd.Kind, Is.EqualTo(WktTokenKind.End));
            Assert.That(secondEnd.Offset, Is.EqualTo(4));
        });
    }

    private static WktTokenizer Tokenizer(string source)
    {
        return new WktTokenizer(source, new WktReadOptions());
    }

    private static void AssertTokenError(string source, ParseErrorKind kind, int offset, string messagePart)
    {
        AssertError(Tokenizer(source).ReadToken(), kind, offset, messagePart);
    }

    private static void AssertKeyword(WktToken token, string text, int offset, int length)
    {
        Assert.Multiple(() =>
        {
            Assert.That(token.Kind, Is.EqualTo(WktTokenKind.Keyword));
            Assert.That(token.Text, Is.EqualTo(text));
            Assert.That(token.Offset, Is.EqualTo(offset));
            Assert.That(token.Length, Is.EqualTo(length));
            Assert.That(token.Error, Is.Null);
        });
    }

    private static void AssertNumber(WktToken token, string text, double value, int offset, int length)
    {
        Assert.Multiple(() =>
        {
            Assert.That(token.Kind, Is.EqualTo(WktTokenKind.Number));
            Assert.That(token.Text, Is.EqualTo(text));
            Assert.That(token.Number, Is.EqualTo(value));
            Assert.That(token.Offset, Is.EqualTo(offset));
            Assert.That(token.Length, Is.EqualTo(length));
            Assert.That(token.Error, Is.Null);
        });
    }

    private static void AssertSimple(WktToken token, WktTokenKind kind, int offset)
    {
        Assert.Multiple(() =>
        {
            Assert.That(token.Kind, Is.EqualTo(kind));
            Assert.That(token.Offset, Is.EqualTo(offset));
            Assert.That(token.Length, Is.EqualTo(1));
            Assert.That(token.Error, Is.Null);
        });
    }

    private static void AssertError(WktToken token, ParseErrorKind kind, int offset, string messagePart)
    {
        Assert.Multiple(() =>
        {
            Assert.That(token.Kind, Is.EqualTo(WktTokenKind.Invalid));
            Assert.That(token.Error, Is.Not.Null);
            Assert.That(token.Error!.Kind, Is.EqualTo(kind));
            Assert.That(token.Error.Offset, Is.EqualTo(offset));
            Assert.That(token.Offset, Is.EqualTo(offset));
            Assert.That(token.Error.Message, Does.Contain(messagePart).IgnoreCase);
            Assert.That(token.Error.Message, Does.Contain($"offset {offset}"));
        });
    }
}
