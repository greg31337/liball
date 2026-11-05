using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class LexerParameterTests
{
    [Test]
    public void Lexer_TokenizesQuestionParameters()
    {
        SqlToken[] tokens = SqlLexer.Tokenize("? ?123");

        AssertToken(tokens[0], SqlTokenKind.Parameter, "?");
        AssertToken(tokens[2], SqlTokenKind.Parameter, "?123");
        AssertToken(tokens[3], SqlTokenKind.EndOfFile, "");
    }

    [Test]
    public void Lexer_TokenizesNamedParameters()
    {
        SqlToken[] tokens = SqlLexer.Tokenize(":name @a_b $abc123");

        AssertToken(tokens[0], SqlTokenKind.Parameter, ":name");
        AssertToken(tokens[2], SqlTokenKind.Parameter, "@a_b");
        AssertToken(tokens[4], SqlTokenKind.Parameter, "$abc123");
        AssertToken(tokens[5], SqlTokenKind.EndOfFile, "");
    }

    [Test]
    public void Lexer_DoesNotTokenizeNamedParameterPrefixesWithoutNames()
    {
        SqlToken[] tokens = SqlLexer.Tokenize(": @ $");

        AssertToken(tokens[0], SqlTokenKind.Colon, ":");
        AssertToken(tokens[2], SqlTokenKind.At, "@");
        AssertToken(tokens[4], SqlTokenKind.Dollar, "$");
        AssertToken(tokens[5], SqlTokenKind.EndOfFile, "");
    }

    [Test]
    public void Lexer_ParameterFlagsDisableParameterScanning()
    {
        SqlDialectOptions options = GenericDialect.CreateOptions();

        options.AllowQuestionParameters = false;
        options.AllowColonParameters = false;
        options.AllowAtParameters = false;
        options.AllowDollarParameters = false;
        SqlToken[] tokens = SqlLexer.Tokenize("?1 :name @name $name", options);

        AssertToken(tokens[0], SqlTokenKind.Question, "?");
        AssertToken(tokens[1], SqlTokenKind.NumericLiteral, "1");
        AssertToken(tokens[3], SqlTokenKind.Colon, ":");
        AssertToken(tokens[4], SqlTokenKind.Identifier, "name");
        AssertToken(tokens[6], SqlTokenKind.At, "@");
        AssertToken(tokens[7], SqlTokenKind.Identifier, "name");
        AssertToken(tokens[9], SqlTokenKind.Dollar, "$");
        AssertToken(tokens[10], SqlTokenKind.Identifier, "name");
    }

    [Test]
    public void Lexer_DollarParameterWinsWhenDollarIdentifiersAreAlsoEnabled()
    {
        SqlDialectOptions options = GenericDialect.CreateOptions();

        options.AllowDollarInBareIdentifiers = true;
        options.AllowDollarParameters = true;
        SqlToken[] parameterTokens = SqlLexer.Tokenize("$name", options);

        options.AllowDollarParameters = false;
        SqlToken[] identifierTokens = SqlLexer.Tokenize("$name", options);

        AssertToken(parameterTokens[0], SqlTokenKind.Parameter, "$name");
        AssertToken(identifierTokens[0], SqlTokenKind.Identifier, "$name");
    }

    [Test]
    public void Lexer_DoubleColonStillTokenizesAsOperator()
    {
        SqlToken[] tokens = SqlLexer.Tokenize("::");

        AssertToken(tokens[0], SqlTokenKind.DoubleColon, "::");
        AssertToken(tokens[1], SqlTokenKind.EndOfFile, "");
    }

    static void AssertToken(SqlToken token, SqlTokenKind kind, string text)
    {
        Assert.That(token.Kind, Is.EqualTo(kind));
        Assert.That(token.Text, Is.EqualTo(text));
    }
}
