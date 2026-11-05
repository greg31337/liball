using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class TokenAndTriviaTests
{
    [Test]
    public void Trivia_StoresKindSpanAndOriginalText()
    {
        SqlTextSpan span = new SqlTextSpan(4, 2);
        SqlTrivia trivia = new SqlTrivia(SqlTriviaKind.Whitespace, span, "  ");

        Assert.That(trivia.Kind, Is.EqualTo(SqlTriviaKind.Whitespace));
        Assert.That(trivia.Span, Is.SameAs(span));
        Assert.That(trivia.Text, Is.EqualTo("  "));
        Assert.That(trivia.GetLength(), Is.EqualTo(2));
        Assert.That(trivia.IsEmpty(), Is.False);
    }

    [Test]
    public void Trivia_RejectsNullsAndSpanTextLengthMismatch()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlTrivia(SqlTriviaKind.Whitespace, null!, " "));
        Assert.Throws<ArgumentNullException>(() => new SqlTrivia(SqlTriviaKind.Whitespace, new SqlTextSpan(0, 1), null!));
        Assert.Throws<ArgumentException>(() => new SqlTrivia(SqlTriviaKind.Whitespace, new SqlTextSpan(0, 2), " "));
    }

    [Test]
    public void Trivia_AllowsEmptyText()
    {
        SqlTrivia trivia = new SqlTrivia(SqlTriviaKind.SkippedText, new SqlTextSpan(3, 0), "");

        Assert.That(trivia.IsEmpty(), Is.True);
        Assert.That(trivia.GetLength(), Is.EqualTo(0));
    }

    [Test]
    public void Token_StoresKindSpanAndOriginalText()
    {
        SqlTextSpan span = new SqlTextSpan(0, 6);
        SqlToken token = SqlToken.Create(SqlTokenKind.Keyword, span, "select");

        Assert.That(token.Kind, Is.EqualTo(SqlTokenKind.Keyword));
        Assert.That(token.Span, Is.SameAs(span));
        Assert.That(token.Text, Is.EqualTo("select"));
        Assert.That(token.GetLength(), Is.EqualTo(6));
        Assert.That(token.IsEmpty(), Is.False);
        Assert.That(token.HasLeadingTrivia(), Is.False);
        Assert.That(token.HasTrailingTrivia(), Is.False);
        Assert.That(token.GetLeadingTriviaCount(), Is.EqualTo(0));
        Assert.That(token.GetTrailingTriviaCount(), Is.EqualTo(0));
    }

    [Test]
    public void Token_StoresLeadingAndTrailingTriviaInOrder()
    {
        SqlTrivia leading = new SqlTrivia(SqlTriviaKind.LineComment, new SqlTextSpan(0, 5), "-- x\n");
        SqlTrivia trailingWhitespace = new SqlTrivia(SqlTriviaKind.Whitespace, new SqlTextSpan(11, 1), " ");
        SqlTrivia trailingComment = new SqlTrivia(SqlTriviaKind.BlockComment, new SqlTextSpan(12, 5), "/*x*/");
        SqlToken token = SqlToken.Create(
            SqlTokenKind.Keyword,
            new SqlTextSpan(5, 6),
            "select",
            new SqlTrivia[] { leading },
            new SqlTrivia[] { trailingWhitespace, trailingComment });

        Assert.That(token.HasLeadingTrivia(), Is.True);
        Assert.That(token.HasTrailingTrivia(), Is.True);
        Assert.That(token.GetLeadingTriviaCount(), Is.EqualTo(1));
        Assert.That(token.GetTrailingTriviaCount(), Is.EqualTo(2));
        Assert.That(token.GetLeadingTriviaAt(0), Is.SameAs(leading));
        Assert.That(token.GetTrailingTriviaAt(0), Is.SameAs(trailingWhitespace));
        Assert.That(token.GetTrailingTriviaAt(1), Is.SameAs(trailingComment));
    }

    [Test]
    public void Token_AllowsEmptyEndOfFileToken()
    {
        SqlToken token = SqlToken.Create(SqlTokenKind.EndOfFile, new SqlTextSpan(7, 0), "");

        Assert.That(token.IsEmpty(), Is.True);
        Assert.That(token.GetLength(), Is.EqualTo(0));
    }

    [Test]
    public void Token_RejectsNullsAndSpanTextLengthMismatch()
    {
        Assert.Throws<ArgumentNullException>(() => SqlToken.Create(SqlTokenKind.Identifier, null!, "x"));
        Assert.Throws<ArgumentNullException>(() => SqlToken.Create(SqlTokenKind.Identifier, new SqlTextSpan(0, 1), null!));
        Assert.Throws<ArgumentException>(() => SqlToken.Create(SqlTokenKind.Identifier, new SqlTextSpan(0, 2), "x"));
        Assert.Throws<ArgumentNullException>(() => SqlToken.Create(SqlTokenKind.Identifier, new SqlTextSpan(0, 1), "x", null!, new SqlTrivia[0]));
        Assert.Throws<ArgumentNullException>(() => SqlToken.Create(SqlTokenKind.Identifier, new SqlTextSpan(0, 1), "x", new SqlTrivia[0], null!));
        Assert.Throws<ArgumentNullException>(() => SqlToken.Create(SqlTokenKind.Identifier, new SqlTextSpan(0, 1), "x", new SqlTrivia[] { null! }, new SqlTrivia[0]));
        Assert.Throws<ArgumentNullException>(() => SqlToken.Create(SqlTokenKind.Identifier, new SqlTextSpan(0, 1), "x", new SqlTrivia[0], new SqlTrivia[] { null! }));
    }

    [Test]
    public void Token_LexicalDataAndTriviaIndexesValidateBoundaries()
    {
        SqlTrivia trivia = new SqlTrivia(SqlTriviaKind.Whitespace, new SqlTextSpan(0, 1), " ");
        SqlToken token = SqlToken.Create(SqlTokenKind.Identifier, new SqlTextSpan(1, 3), "abc", new SqlTrivia[] { trivia }, new SqlTrivia[] { trivia });

        token.SetLexicalData("decoded", SqlNameQuoteStyle.DoubleQuote, SqlStringQuoteStyle.None, SqlParameterStyle.None, "\"");

        Assert.That(token.Value, Is.EqualTo("decoded"));
        Assert.That(token.IdentifierQuoteStyle, Is.EqualTo(SqlNameQuoteStyle.DoubleQuote));
        Assert.That(token.QuoteDelimiter, Is.EqualTo("\""));
        Assert.Throws<ArgumentNullException>(() => token.SetLexicalData(null!, SqlNameQuoteStyle.None, SqlStringQuoteStyle.None, SqlParameterStyle.None, ""));
        Assert.Throws<ArgumentNullException>(() => token.SetLexicalData("value", SqlNameQuoteStyle.None, SqlStringQuoteStyle.None, SqlParameterStyle.None, null!));
        Assert.Throws<IndexOutOfRangeException>(() => token.GetLeadingTriviaAt(-1));
        Assert.Throws<IndexOutOfRangeException>(() => token.GetTrailingTriviaAt(1));
    }
}
