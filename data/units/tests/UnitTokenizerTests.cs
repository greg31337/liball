using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class UnitTokenizerTests
{
    [Test]
    public void UnitToken_StoresTokenMetadata()
    {
        UnitToken token = new UnitToken(UnitTokenKind.Identifier, "meter", 3);

        Assert.That(token.GetKind(), Is.EqualTo(UnitTokenKind.Identifier));
        Assert.That(token.GetText(), Is.EqualTo("meter"));
        Assert.That(token.GetPosition(), Is.EqualTo(3));
    }

    [Test]
    public void UnitToken_RejectsInvalidMetadata()
    {
        Assert.Throws<System.ArgumentNullException>(() => new UnitToken(UnitTokenKind.Identifier, null!, 0));
        Assert.Throws<System.ArgumentException>(() => new UnitToken(UnitTokenKind.Identifier, "meter", -1));
    }

    [Test]
    public void Tokenize_TokenizesMultiplicationDivisionAndPower()
    {
        UnitToken[] tokens = new UnitTokenizer().Tokenize("kg*m/s^2");

        AssertToken(tokens[0], UnitTokenKind.Identifier, "kg", 0);
        AssertToken(tokens[1], UnitTokenKind.Star, "*", 2);
        AssertToken(tokens[2], UnitTokenKind.Identifier, "m", 3);
        AssertToken(tokens[3], UnitTokenKind.Slash, "/", 4);
        AssertToken(tokens[4], UnitTokenKind.Identifier, "s", 5);
        AssertToken(tokens[5], UnitTokenKind.Caret, "^", 6);
        AssertToken(tokens[6], UnitTokenKind.Integer, "2", 7);
        AssertToken(tokens[7], UnitTokenKind.End, "", 8);
    }

    [Test]
    public void Tokenize_SkipsWhitespaceAndTokenizesParentheses()
    {
        UnitToken[] tokens = new UnitTokenizer().Tokenize(" ( N * m ) ");

        AssertToken(tokens[0], UnitTokenKind.OpenParenthesis, "(", 1);
        AssertToken(tokens[1], UnitTokenKind.Identifier, "N", 3);
        AssertToken(tokens[2], UnitTokenKind.Star, "*", 5);
        AssertToken(tokens[3], UnitTokenKind.Identifier, "m", 7);
        AssertToken(tokens[4], UnitTokenKind.CloseParenthesis, ")", 9);
        AssertToken(tokens[5], UnitTokenKind.End, "", 11);
    }

    [Test]
    public void Tokenize_SplitsCompactExponentFromIdentifier()
    {
        UnitToken[] tokens = new UnitTokenizer().Tokenize("m2/s2");

        AssertToken(tokens[0], UnitTokenKind.Identifier, "m", 0);
        AssertToken(tokens[1], UnitTokenKind.Integer, "2", 1);
        AssertToken(tokens[2], UnitTokenKind.Slash, "/", 2);
        AssertToken(tokens[3], UnitTokenKind.Identifier, "s", 3);
        AssertToken(tokens[4], UnitTokenKind.Integer, "2", 4);
        AssertToken(tokens[5], UnitTokenKind.End, "", 5);
    }

    [Test]
    public void Tokenize_TokenizesSignedIntegerExponents()
    {
        UnitToken[] negative = new UnitTokenizer().Tokenize("s^-2");
        UnitToken[] positive = new UnitTokenizer().Tokenize("m^+2");
        UnitToken[] suffix = new UnitTokenizer().Tokenize("s-1");

        AssertToken(negative[0], UnitTokenKind.Identifier, "s", 0);
        AssertToken(negative[1], UnitTokenKind.Caret, "^", 1);
        AssertToken(negative[2], UnitTokenKind.Integer, "-2", 2);
        AssertToken(negative[3], UnitTokenKind.End, "", 4);
        AssertToken(positive[2], UnitTokenKind.Integer, "+2", 2);
        AssertToken(suffix[0], UnitTokenKind.Identifier, "s", 0);
        AssertToken(suffix[1], UnitTokenKind.Integer, "-1", 1);
    }

    [Test]
    public void Tokenize_KeepsHyphenatedLookupTextTogether()
    {
        UnitToken[] tokens = new UnitTokenizer().Tokenize("pound-force/s");

        AssertToken(tokens[0], UnitTokenKind.Identifier, "pound-force", 0);
        AssertToken(tokens[1], UnitTokenKind.Slash, "/", 11);
        AssertToken(tokens[2], UnitTokenKind.Identifier, "s", 12);
    }

    [Test]
    public void Tokenize_TokenizesUnicodeIdentifierCharacters()
    {
        UnitToken[] tokens = new UnitTokenizer().Tokenize("\u03a9");

        AssertToken(tokens[0], UnitTokenKind.Identifier, "\u03a9", 0);
        AssertToken(tokens[1], UnitTokenKind.End, "", 1);
    }

    [Test]
    public void Tokenize_ReturnsUnknownTokensForUnexpectedCharacters()
    {
        UnitToken[] tokens = new UnitTokenizer().Tokenize("m$");

        AssertToken(tokens[0], UnitTokenKind.Identifier, "m", 0);
        AssertToken(tokens[1], UnitTokenKind.Unknown, "$", 1);
        AssertToken(tokens[2], UnitTokenKind.End, "", 2);
    }

    [Test]
    public void Tokenize_RejectsMissingText()
    {
        Assert.Throws<System.ArgumentNullException>(() => new UnitTokenizer().Tokenize(null!));
    }

    static void AssertToken(UnitToken token, UnitTokenKind kind, string text, int position)
    {
        Assert.That(token.GetKind(), Is.EqualTo(kind));
        Assert.That(token.GetText(), Is.EqualTo(text));
        Assert.That(token.GetPosition(), Is.EqualTo(position));
    }
}
