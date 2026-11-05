using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class LexerDiagnosticTests
{
    [Test]
    public void Lexer_DiagnosesInvalidCharacters()
    {
        SqlLexer lexer = SqlLexer.Create("#");

        lexer.LexTokens();

        AssertDiagnostic(lexer, 0, "SQLLEX001", 0, 1);
    }

    [Test]
    public void Lexer_DiagnosesUnterminatedBlockComment()
    {
        SqlLexer lexer = SqlLexer.Create("/* comment");

        lexer.LexTokens();

        AssertDiagnostic(lexer, 0, "SQLLEX002", 0, 10);
    }

    [Test]
    public void Lexer_DiagnosesUnterminatedQuotedTokens()
    {
        SqlDialectOptions options = SqliteDialect.CreateOptions();
        SqlLexer doubleQuotedLexer = SqlLexer.Create("\"name");
        SqlLexer stringLexer = SqlLexer.Create("'value");
        SqlLexer bracketLexer = SqlLexer.Create("[name", options);
        SqlLexer backtickLexer = SqlLexer.Create("`name", options);

        doubleQuotedLexer.LexTokens();
        stringLexer.LexTokens();
        bracketLexer.LexTokens();
        backtickLexer.LexTokens();

        AssertDiagnostic(doubleQuotedLexer, 0, "SQLLEX003", 0, 5);
        AssertDiagnostic(stringLexer, 0, "SQLLEX007", 0, 6);
        AssertDiagnostic(bracketLexer, 0, "SQLLEX005", 0, 5);
        AssertDiagnostic(backtickLexer, 0, "SQLLEX006", 0, 5);
    }

    [Test]
    public void Lexer_DiagnosesUnsupportedQuotedTokens()
    {
        SqlDialectOptions options = GenericDialect.CreateOptions();

        options.AllowDoubleQuotedIdentifiers = false;
        options.AllowDoubleQuotedStrings = false;
        options.AllowSingleQuotedStrings = false;
        SqlLexer lexer = SqlLexer.Create("\"name\" 'value'", options);

        lexer.LexTokens();

        AssertDiagnostic(lexer, 0, "SQLLEX004", 0, 6);
        AssertDiagnostic(lexer, 1, "SQLLEX008", 7, 7);
    }

    [Test]
    public void Lexer_DiagnosesMalformedBlobLiterals()
    {
        SqlDialectOptions options = SqliteDialect.CreateOptions();
        SqlLexer oddLengthLexer = SqlLexer.Create("X'ABC'", options);
        SqlLexer invalidHexLexer = SqlLexer.Create("X'GG'", options);

        oddLengthLexer.LexTokens();
        invalidHexLexer.LexTokens();

        AssertDiagnostic(oddLengthLexer, 0, "SQLLEX010", 0, 6);
        AssertDiagnostic(invalidHexLexer, 0, "SQLLEX010", 0, 5);
    }

    [Test]
    public void Lexer_DiagnosesUnterminatedBlobLiteral()
    {
        SqlDialectOptions options = SqliteDialect.CreateOptions();
        SqlLexer lexer = SqlLexer.Create("X'AB", options);

        lexer.LexTokens();

        AssertDiagnostic(lexer, 0, "SQLLEX009", 0, 4);
    }

    [Test]
    public void Lexer_DiagnosesMalformedHexInteger()
    {
        SqlDialectOptions options = SqliteDialect.CreateOptions();
        SqlLexer lexer = SqlLexer.Create("0x", options);

        lexer.LexTokens();

        AssertDiagnostic(lexer, 0, "SQLLEX011", 0, 2);
    }

    [Test]
    public void Lexer_DiagnosesMalformedExponent()
    {
        SqlLexer lexer = SqlLexer.Create("1e+");
        SqlToken[] tokens = lexer.LexTokens();

        AssertToken(tokens[0], SqlTokenKind.NumericLiteral, "1e+");
        AssertDiagnostic(lexer, 0, "SQLLEX012", 0, 3);
    }

    [Test]
    public void Lexer_PeekReturnsSentinelForExtremeOffsets()
    {
        SqlLexer lexer = SqlLexer.Create("ab");

        Assert.That(lexer.Peek(int.MinValue), Is.EqualTo('\0'));
        Assert.That(lexer.Peek(int.MaxValue), Is.EqualTo('\0'));
        lexer.Position = 1;
        Assert.That(lexer.Peek(int.MaxValue), Is.EqualTo('\0'));
    }

    static void AssertDiagnostic(SqlLexer lexer, int index, string code, int start, int length)
    {
        SqlDiagnostic diagnostic = lexer.Diagnostics.GetAt(index);

        Assert.That(diagnostic.Severity, Is.EqualTo(SqlDiagnosticSeverity.Error));
        Assert.That(diagnostic.Code, Is.EqualTo(code));
        Assert.That(diagnostic.Span.Start, Is.EqualTo(start));
        Assert.That(diagnostic.Span.Length, Is.EqualTo(length));
        Assert.That(diagnostic.GetLineNumber(), Is.EqualTo(1));
        Assert.That(diagnostic.GetColumnNumber(), Is.EqualTo(start + 1));
    }

    static void AssertToken(SqlToken token, SqlTokenKind kind, string text)
    {
        Assert.That(token.Kind, Is.EqualTo(kind));
        Assert.That(token.Text, Is.EqualTo(text));
    }
}
