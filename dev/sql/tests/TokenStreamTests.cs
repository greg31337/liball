using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class TokenStreamTests
{
    [Test]
    public void PublicContract_ExposesDistinctCursorOperations()
    {
        ISqlTokenStream stream = new SqlTokenStream(CreateTokens());

        Assert.That(stream.GetPosition(), Is.EqualTo(0));
        Assert.That(stream.IsAtEnd(), Is.False);
        AssertToken(stream.Peek(), SqlTokenKind.Keyword, "select");
        AssertToken(stream.LookAhead(1), SqlTokenKind.Identifier, "value");
        AssertToken(stream.LookAhead(int.MaxValue), SqlTokenKind.EndOfFile, "");
        Assert.That(stream.Match(SqlTokenKind.Identifier), Is.False);
        Assert.That(stream.Match(SqlTokenKind.Keyword), Is.True);
        AssertToken(stream.Consume(), SqlTokenKind.Identifier, "value");
        Assert.That(stream.IsAtEnd(), Is.True);
        Assert.That(stream.GetPosition(), Is.EqualTo(2));
    }

    [Test]
    public void PublicContract_ExpectReportsAccessibleDiagnostics()
    {
        ISqlTokenStream stream = new SqlTokenStream(CreateTokens());

        SqlToken token = stream.Expect(SqlTokenKind.NumericLiteral);

        AssertToken(token, SqlTokenKind.Keyword, "select");
        Assert.That(stream.GetPosition(), Is.EqualTo(1));
        Assert.That(stream.GetDiagnostics().Diagnostics, Has.Count.EqualTo(1));
        Assert.That(stream.GetDiagnostics().Diagnostics[0].Code, Is.EqualTo("SQLSTREAM001"));
    }

    [Test]
    public void PublicStream_SnapshotsTokensAndValidatesLookaheadAndEndToken()
    {
        SqlToken[] tokens = CreateTokens();
        ISqlTokenStream stream = new SqlTokenStream(tokens);
        tokens[0] = SqlToken.Create(SqlTokenKind.NumericLiteral, new SqlTextSpan(0, 1), "1");

        AssertToken(stream.Peek(), SqlTokenKind.Keyword, "select");
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.LookAhead(-1));
        Assert.Throws<ArgumentException>(() => new SqlTokenStream(new SqlToken[]
        {
            SqlToken.Create(SqlTokenKind.Identifier, new SqlTextSpan(0, 1), "x")
        }));
    }

    [Test]
    public void TokenStream_PeekReadsWithoutAdvancing()
    {
        SqlTokenStream stream = SqlTokenStream.Create(CreateTokens());

        AssertToken(stream.Peek(), SqlTokenKind.Keyword, "select");
        AssertToken(stream.PeekAtOffset(1), SqlTokenKind.Identifier, "value");
        Assert.That(stream.GetPosition(), Is.EqualTo(0));
    }

    [Test]
    public void TokenStream_PeekClampsExtremeOffsets()
    {
        SqlTokenStream stream = SqlTokenStream.Create(CreateTokens());

        AssertToken(stream.PeekAtOffset(int.MinValue), SqlTokenKind.Keyword, "select");
        AssertToken(stream.PeekAtOffset(int.MaxValue), SqlTokenKind.EndOfFile, "");
        stream.Consume();
        AssertToken(stream.PeekAtOffset(int.MinValue), SqlTokenKind.Keyword, "select");
        AssertToken(stream.PeekAtOffset(int.MaxValue), SqlTokenKind.EndOfFile, "");
    }

    [Test]
    public void TokenStream_ConsumeAdvancesUntilEndOfFile()
    {
        SqlTokenStream stream = SqlTokenStream.Create(CreateTokens());

        AssertToken(stream.Consume(), SqlTokenKind.Keyword, "select");
        Assert.That(stream.GetPosition(), Is.EqualTo(1));
        AssertToken(stream.Consume(), SqlTokenKind.Identifier, "value");
        Assert.That(stream.GetPosition(), Is.EqualTo(2));
        Assert.That(stream.IsAtEnd(), Is.True);
        AssertToken(stream.Consume(), SqlTokenKind.EndOfFile, "");
        Assert.That(stream.GetPosition(), Is.EqualTo(2));
    }

    [Test]
    public void TokenStream_MatchConsumesOnlyMatchingKind()
    {
        SqlTokenStream stream = SqlTokenStream.Create(CreateTokens());

        Assert.That(stream.Match(SqlTokenKind.Identifier), Is.False);
        Assert.That(stream.GetPosition(), Is.EqualTo(0));
        Assert.That(stream.Match(SqlTokenKind.Keyword), Is.True);
        Assert.That(stream.GetPosition(), Is.EqualTo(1));
    }

    [Test]
    public void TokenStream_ExpectConsumesMatchingKindWithoutDiagnostic()
    {
        SqlTokenStream stream = SqlTokenStream.Create(CreateTokens());

        AssertToken(stream.Expect(SqlTokenKind.Keyword), SqlTokenKind.Keyword, "select");
        Assert.That(stream.GetPosition(), Is.EqualTo(1));
        Assert.That(stream.Diagnostics.GetCount(), Is.EqualTo(0));
    }

    [Test]
    public void TokenStream_ExpectReportsAndConsumesUnexpectedToken()
    {
        SqlDiagnosticBag diagnostics = new SqlDiagnosticBag();
        SqlTokenStream stream = SqlTokenStream.Create(CreateTokens(), diagnostics);

        AssertToken(stream.Expect(SqlTokenKind.NumericLiteral), SqlTokenKind.Keyword, "select");
        Assert.That(stream.GetPosition(), Is.EqualTo(1));
        Assert.That(diagnostics.GetCount(), Is.EqualTo(1));
        Assert.That(diagnostics.GetAt(0).Code, Is.EqualTo("SQLSTREAM001"));
        Assert.That(diagnostics.GetAt(0).Span.Start, Is.EqualTo(0));
    }

    [Test]
    public void TokenStream_ExpectReportsEndOfFileWithoutAdvancingPastIt()
    {
        SqlToken eof = SqlToken.Create(SqlTokenKind.EndOfFile, new SqlTextSpan(0, 0), "");
        SqlTokenStream stream = SqlTokenStream.Create(new SqlToken[] { eof });

        AssertToken(stream.Expect(SqlTokenKind.Identifier), SqlTokenKind.EndOfFile, "");
        Assert.That(stream.GetPosition(), Is.EqualTo(0));
        Assert.That(stream.Diagnostics.GetCount(), Is.EqualTo(1));
    }

    [Test]
    public void TokenStream_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => SqlTokenStream.Create(null!));
        Assert.Throws<ArgumentNullException>(() => new SqlTokenStream(null!));
        Assert.Throws<ArgumentException>(() => SqlTokenStream.Create(new SqlToken[0]));
        Assert.Throws<ArgumentException>(() => new SqlTokenStream(new SqlToken[0]));
        Assert.Throws<ArgumentNullException>(() => SqlTokenStream.Create(new SqlToken[] { null! }));
        Assert.Throws<ArgumentNullException>(() => new SqlTokenStream(new SqlToken[] { null! }));
        Assert.Throws<ArgumentNullException>(() => SqlTokenStream.Create(CreateTokens(), null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SqlTokenStream(CreateTokens()).LookAhead(-1));
    }

    static SqlToken[] CreateTokens()
    {
        return new SqlToken[]
        {
            SqlToken.Create(SqlTokenKind.Keyword, new SqlTextSpan(0, 6), "select"),
            SqlToken.Create(SqlTokenKind.Identifier, new SqlTextSpan(7, 5), "value"),
            SqlToken.Create(SqlTokenKind.EndOfFile, new SqlTextSpan(12, 0), "")
        };
    }

    static void AssertToken(SqlToken token, SqlTokenKind kind, string text)
    {
        Assert.That(token.Kind, Is.EqualTo(kind));
        Assert.That(token.Text, Is.EqualTo(text));
    }
}
