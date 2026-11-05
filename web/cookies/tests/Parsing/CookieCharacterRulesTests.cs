namespace cookies.Tests;

public class CookieCharacterRulesTests
{
    [Test]
    public void TokenClassificationMatchesTheAsciiTokenGrammar()
    {
        string punctuation = "!#$%&'*+-.^_`|~";
        for (int value = 0; value <= 127; value++)
        {
            char character = (char)value;
            bool expected = character >= 'A' && character <= 'Z' ||
                character >= 'a' && character <= 'z' ||
                character >= '0' && character <= '9' ||
                punctuation.Contains(character);

            Assert.That(CookieCharacterRules.IsTokenCharacter(character), Is.EqualTo(expected), $"ASCII {value}");
        }

        Assert.That(CookieCharacterRules.IsTokenCharacter('\u0080'), Is.False);
        Assert.That(CookieCharacterRules.IsTokenCharacter('\u00e9'), Is.False);
    }

    [Test]
    public void CookieOctetClassificationMatchesRfc6265Ranges()
    {
        for (int value = 0; value <= 127; value++)
        {
            bool expected = value == 0x21 ||
                value >= 0x23 && value <= 0x2b ||
                value >= 0x2d && value <= 0x3a ||
                value >= 0x3c && value <= 0x5b ||
                value >= 0x5d && value <= 0x7e;

            Assert.That(CookieCharacterRules.IsCookieOctet((char)value), Is.EqualTo(expected), $"ASCII {value}");
        }

        Assert.That(CookieCharacterRules.IsCookieOctet('\u0080'), Is.False);
    }

    [Test]
    public void AttributeValueClassificationExcludesControlsSemicolonAndNonAscii()
    {
        for (int value = 0; value <= 127; value++)
        {
            bool expected = value >= 0x20 && value <= 0x3a || value >= 0x3c && value <= 0x7e;
            Assert.That(CookieCharacterRules.IsAttributeValueOctet((char)value), Is.EqualTo(expected), $"ASCII {value}");
        }

        Assert.That(CookieCharacterRules.IsAttributeValueOctet('\u0080'), Is.False);
    }

    [Test]
    public void FoundationalAsciiClassificationsAreCultureIndependent()
    {
        Assert.That(CookieCharacterRules.IsAscii('A'), Is.True);
        Assert.That(CookieCharacterRules.IsAscii('\u007f'), Is.True);
        Assert.That(CookieCharacterRules.IsAscii('\u0080'), Is.False);
        Assert.That(CookieCharacterRules.IsAsciiAlpha('Z'), Is.True);
        Assert.That(CookieCharacterRules.IsAsciiAlpha('z'), Is.True);
        Assert.That(CookieCharacterRules.IsAsciiAlpha('\u00e9'), Is.False);
        Assert.That(CookieCharacterRules.IsAsciiDigit('0'), Is.True);
        Assert.That(CookieCharacterRules.IsAsciiDigit('/'), Is.False);
        Assert.That(CookieCharacterRules.IsAsciiWhitespace(' '), Is.True);
        Assert.That(CookieCharacterRules.IsAsciiWhitespace('\t'), Is.True);
        Assert.That(CookieCharacterRules.IsAsciiWhitespace('\r'), Is.False);
        Assert.That(CookieCharacterRules.IsControl('\0'), Is.True);
        Assert.That(CookieCharacterRules.IsControl('\u001f'), Is.True);
        Assert.That(CookieCharacterRules.IsControl(' '), Is.False);
        Assert.That(CookieCharacterRules.IsControl('\u007f'), Is.True);
        Assert.That(CookieCharacterRules.IsHeaderInjection('\0'), Is.True);
        Assert.That(CookieCharacterRules.IsHeaderInjection('\r'), Is.True);
        Assert.That(CookieCharacterRules.IsHeaderInjection('\n'), Is.True);
        Assert.That(CookieCharacterRules.IsHeaderInjection('\t'), Is.False);
    }
}
