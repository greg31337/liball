using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class TokenKindTests
{
    [Test]
    public void TokenKind_CoreValuesAreStable()
    {
        Assert.That((int)SqlTokenKind.None, Is.EqualTo(0));
        Assert.That((int)SqlTokenKind.EndOfFile, Is.EqualTo(1));
        Assert.That((int)SqlTokenKind.BadToken, Is.EqualTo(2));
        Assert.That((int)SqlTokenKind.Whitespace, Is.EqualTo(10));
        Assert.That((int)SqlTokenKind.Identifier, Is.EqualTo(20));
        Assert.That((int)SqlTokenKind.OpenParen, Is.EqualTo(40));
        Assert.That((int)SqlTokenKind.Plus, Is.EqualTo(70));
        Assert.That((int)SqlTokenKind.Equal, Is.EqualTo(90));
    }

    [Test]
    public void TokenKind_IncludesCommonTokenCategories()
    {
        Assert.That(SqlTokenKind.LineComment, Is.Not.EqualTo(SqlTokenKind.BlockComment));
        Assert.That(SqlTokenKind.Identifier, Is.Not.EqualTo(SqlTokenKind.Keyword));
        Assert.That(SqlTokenKind.NumericLiteral, Is.Not.EqualTo(SqlTokenKind.StringLiteral));
        Assert.That(SqlTokenKind.BlobLiteral, Is.Not.EqualTo(SqlTokenKind.Parameter));
    }

    [Test]
    public void TokenKind_IncludesCommonPunctuation()
    {
        Assert.That(SqlTokenKind.OpenParen, Is.Not.EqualTo(SqlTokenKind.CloseParen));
        Assert.That(SqlTokenKind.OpenBracket, Is.Not.EqualTo(SqlTokenKind.CloseBracket));
        Assert.That(SqlTokenKind.Comma, Is.Not.EqualTo(SqlTokenKind.Dot));
        Assert.That(SqlTokenKind.Semicolon, Is.Not.EqualTo(SqlTokenKind.Colon));
        Assert.That(SqlTokenKind.Question, Is.Not.EqualTo(SqlTokenKind.At));
        Assert.That(SqlTokenKind.Dollar, Is.Not.EqualTo(SqlTokenKind.At));
    }

    [Test]
    public void TokenKind_IncludesCommonOperators()
    {
        Assert.That(SqlTokenKind.Plus, Is.Not.EqualTo(SqlTokenKind.Minus));
        Assert.That(SqlTokenKind.Star, Is.Not.EqualTo(SqlTokenKind.Slash));
        Assert.That(SqlTokenKind.Percent, Is.Not.EqualTo(SqlTokenKind.Pipe));
        Assert.That(SqlTokenKind.DoublePipe, Is.Not.EqualTo(SqlTokenKind.Pipe));
        Assert.That(SqlTokenKind.Equal, Is.Not.EqualTo(SqlTokenKind.DoubleEqual));
        Assert.That(SqlTokenKind.BangEqual, Is.Not.EqualTo(SqlTokenKind.LessGreater));
        Assert.That(SqlTokenKind.LessEqual, Is.Not.EqualTo(SqlTokenKind.GreaterEqual));
        Assert.That(SqlTokenKind.ShiftLeft, Is.Not.EqualTo(SqlTokenKind.ShiftRight));
        Assert.That(SqlTokenKind.Arrow, Is.Not.EqualTo(SqlTokenKind.DoubleArrow));
    }
}
