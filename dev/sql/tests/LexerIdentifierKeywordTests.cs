using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class LexerIdentifierKeywordTests
{
    [Test]
    public void Lexer_TokenizesBareIdentifiers()
    {
        SqlToken[] tokens = SqlLexer.Tokenize("abc _x x1 a_b");

        AssertToken(tokens[0], SqlTokenKind.Identifier, "abc");
        AssertToken(tokens[1], SqlTokenKind.Whitespace, " ");
        AssertToken(tokens[2], SqlTokenKind.Identifier, "_x");
        AssertToken(tokens[3], SqlTokenKind.Whitespace, " ");
        AssertToken(tokens[4], SqlTokenKind.Identifier, "x1");
        AssertToken(tokens[5], SqlTokenKind.Whitespace, " ");
        AssertToken(tokens[6], SqlTokenKind.Identifier, "a_b");
        AssertToken(tokens[7], SqlTokenKind.EndOfFile, "");
    }

    [Test]
    public void Lexer_TokenizesKeywordsUsingDefaultDialectOptions()
    {
        SqlToken[] tokens = SqlLexer.Tokenize("select From where");

        AssertToken(tokens[0], SqlTokenKind.Keyword, "select");
        AssertToken(tokens[2], SqlTokenKind.Keyword, "From");
        AssertToken(tokens[4], SqlTokenKind.Keyword, "where");
    }

    [Test]
    public void Lexer_DoesNotTreatKeywordPrefixesAsKeywords()
    {
        SqlToken[] tokens = SqlLexer.Tokenize("select1 _from");

        AssertToken(tokens[0], SqlTokenKind.Identifier, "select1");
        AssertToken(tokens[2], SqlTokenKind.Identifier, "_from");
    }

    [Test]
    public void Lexer_KeywordRecognitionRespectsCaseSensitiveOptions()
    {
        SqlDialectOptions options = GenericDialect.CreateOptions();

        options.CaseInsensitiveKeywords = false;
        SqlToken[] tokens = SqlLexer.Tokenize("select SELECT", options);

        AssertToken(tokens[0], SqlTokenKind.Identifier, "select");
        AssertToken(tokens[2], SqlTokenKind.Keyword, "SELECT");
    }

    [Test]
    public void Lexer_DollarIdentifiersRequireDialectFlag()
    {
        SqlDialectOptions options = GenericDialect.CreateOptions();

        options.AllowDollarParameters = false;
        SqlToken[] defaultTokens = SqlLexer.Tokenize("$abc", options);
        options.AllowDollarInBareIdentifiers = true;
        SqlToken[] dollarIdentifierTokens = SqlLexer.Tokenize("$abc", options);

        AssertToken(defaultTokens[0], SqlTokenKind.Dollar, "$");
        AssertToken(defaultTokens[1], SqlTokenKind.Identifier, "abc");
        AssertToken(dollarIdentifierTokens[0], SqlTokenKind.Identifier, "$abc");
    }

    [Test]
    public void Lexer_RejectsNullOptions()
    {
        Assert.Throws<ArgumentNullException>(() => SqlLexer.Tokenize("select", null!));
        Assert.Throws<ArgumentNullException>(() => SqlLexer.Create("select", null!));
        Assert.Throws<ArgumentNullException>(() => SqlLexer.Create(SqlSourceText.Create("select"), null!));
    }

    static void AssertToken(SqlToken token, SqlTokenKind kind, string text)
    {
        Assert.That(token.Kind, Is.EqualTo(kind));
        Assert.That(token.Text, Is.EqualTo(text));
    }
}
