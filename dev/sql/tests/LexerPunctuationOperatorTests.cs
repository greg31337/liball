using System.Collections.Generic;
using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class LexerPunctuationOperatorTests
{
    [Test]
    public void Lexer_TokenizesCommonPunctuation()
    {
        SqlDialectOptions options = GenericDialect.CreateOptions();

        options.AllowQuestionParameters = false;
        options.AllowColonParameters = false;
        options.AllowAtParameters = false;
        options.AllowDollarParameters = false;
        SqlToken[] tokens = SqlLexer.Tokenize("(),.;:?@$[]", options);

        AssertToken(tokens[0], SqlTokenKind.OpenParen, "(");
        AssertToken(tokens[1], SqlTokenKind.CloseParen, ")");
        AssertToken(tokens[2], SqlTokenKind.Comma, ",");
        AssertToken(tokens[3], SqlTokenKind.Dot, ".");
        AssertToken(tokens[4], SqlTokenKind.Semicolon, ";");
        AssertToken(tokens[5], SqlTokenKind.Colon, ":");
        AssertToken(tokens[6], SqlTokenKind.Question, "?");
        AssertToken(tokens[7], SqlTokenKind.At, "@");
        AssertToken(tokens[8], SqlTokenKind.Dollar, "$");
        AssertToken(tokens[9], SqlTokenKind.OpenBracket, "[");
        AssertToken(tokens[10], SqlTokenKind.CloseBracket, "]");
        AssertToken(tokens[11], SqlTokenKind.EndOfFile, "");
    }

    [Test]
    public void Lexer_TokenizesSingleCharacterOperators()
    {
        SqlToken[] tokens = SqlLexer.Tokenize("+-*/%|&~^= < >");
        SqlToken[] significant = GetNonWhitespaceTokens(tokens);

        AssertToken(significant[0], SqlTokenKind.Plus, "+");
        AssertToken(significant[1], SqlTokenKind.Minus, "-");
        AssertToken(significant[2], SqlTokenKind.Star, "*");
        AssertToken(significant[3], SqlTokenKind.Slash, "/");
        AssertToken(significant[4], SqlTokenKind.Percent, "%");
        AssertToken(significant[5], SqlTokenKind.Pipe, "|");
        AssertToken(significant[6], SqlTokenKind.Ampersand, "&");
        AssertToken(significant[7], SqlTokenKind.Tilde, "~");
        AssertToken(significant[8], SqlTokenKind.Caret, "^");
        AssertToken(significant[9], SqlTokenKind.Equal, "=");
        AssertToken(significant[10], SqlTokenKind.Less, "<");
        AssertToken(significant[11], SqlTokenKind.Greater, ">");
        AssertToken(significant[12], SqlTokenKind.EndOfFile, "");
    }

    [Test]
    public void Lexer_TokenizesLongestMultiCharacterOperators()
    {
        SqlToken[] tokens = SqlLexer.Tokenize(":: || == != <= >= <> << >> -> ->>");
        SqlToken[] significant = GetNonWhitespaceTokens(tokens);

        AssertToken(significant[0], SqlTokenKind.DoubleColon, "::");
        AssertToken(significant[1], SqlTokenKind.DoublePipe, "||");
        AssertToken(significant[2], SqlTokenKind.DoubleEqual, "==");
        AssertToken(significant[3], SqlTokenKind.BangEqual, "!=");
        AssertToken(significant[4], SqlTokenKind.LessEqual, "<=");
        AssertToken(significant[5], SqlTokenKind.GreaterEqual, ">=");
        AssertToken(significant[6], SqlTokenKind.LessGreater, "<>");
        AssertToken(significant[7], SqlTokenKind.ShiftLeft, "<<");
        AssertToken(significant[8], SqlTokenKind.ShiftRight, ">>");
        AssertToken(significant[9], SqlTokenKind.Arrow, "->");
        AssertToken(significant[10], SqlTokenKind.DoubleArrow, "->>");
        AssertToken(significant[11], SqlTokenKind.EndOfFile, "");
    }

    [Test]
    public void Lexer_CommentsWinOverOperatorPrefixes()
    {
        SqlToken[] lineCommentTokens = SqlLexer.Tokenize("-- x");
        SqlToken[] blockCommentTokens = SqlLexer.Tokenize("/* x */");

        AssertToken(lineCommentTokens[0], SqlTokenKind.LineComment, "-- x");
        AssertToken(blockCommentTokens[0], SqlTokenKind.BlockComment, "/* x */");
    }

    static SqlToken[] GetNonWhitespaceTokens(SqlToken[] tokens)
    {
        List<SqlToken> filtered = new List<SqlToken>();
        int index = 0;

        while (index < tokens.Length)
        {
            if (tokens[index].Kind != SqlTokenKind.Whitespace)
            {
                filtered.Add(tokens[index]);
            }

            index = index + 1;
        }

        return filtered.ToArray();
    }

    static void AssertToken(SqlToken token, SqlTokenKind kind, string text)
    {
        Assert.That(token.Kind, Is.EqualTo(kind));
        Assert.That(token.Text, Is.EqualTo(text));
    }
}
