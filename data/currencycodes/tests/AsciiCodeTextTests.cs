using System;
using System.Globalization;

namespace currencycodes.Tests;

public class AsciiCodeTextTests
{
    [Test]
    public void TrimWhitespaceRemovesOnlySurroundingUnicodeWhitespace()
    {
        string unchanged = "USD";

        Assert.Multiple(() =>
        {
            Assert.That(AsciiCodeText.TrimWhitespace(" \t\u2003USD\u00A0\r\n"), Is.EqualTo("USD"));
            Assert.That(AsciiCodeText.TrimWhitespace("  U S D  "), Is.EqualTo("U S D"));
            Assert.That(AsciiCodeText.TrimWhitespace("\u2003\u00A0"), Is.Empty);
            Assert.That(AsciiCodeText.TrimWhitespace(string.Empty), Is.Empty);
            Assert.That(AsciiCodeText.TrimWhitespace(unchanged), Is.SameAs(unchanged));
        });
    }

    [Test]
    public void IsAsciiLetterTextAcceptsOnlyNonemptyAsciiLetters()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AsciiCodeText.IsAsciiLetterText("USD"), Is.True);
            Assert.That(AsciiCodeText.IsAsciiLetterText("uSd"), Is.True);
            Assert.That(AsciiCodeText.IsAsciiLetterText(string.Empty), Is.False);
            Assert.That(AsciiCodeText.IsAsciiLetterText("US1"), Is.False);
            Assert.That(AsciiCodeText.IsAsciiLetterText("US D"), Is.False);
            Assert.That(AsciiCodeText.IsAsciiLetterText("USÉ"), Is.False);
            Assert.That(AsciiCodeText.IsAsciiLetterText("ＵＳＤ"), Is.False);
        });
    }

    [Test]
    public void IsAsciiDigitTextRejectsLocalizedDigitsAndFormatting()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AsciiCodeText.IsAsciiDigitText("008"), Is.True);
            Assert.That(AsciiCodeText.IsAsciiDigitText("0"), Is.True);
            Assert.That(AsciiCodeText.IsAsciiDigitText(string.Empty), Is.False);
            Assert.That(AsciiCodeText.IsAsciiDigitText(" 8"), Is.False);
            Assert.That(AsciiCodeText.IsAsciiDigitText("+8"), Is.False);
            Assert.That(AsciiCodeText.IsAsciiDigitText("8.0"), Is.False);
            Assert.That(AsciiCodeText.IsAsciiDigitText("٨"), Is.False);
            Assert.That(AsciiCodeText.IsAsciiDigitText("８"), Is.False);
        });
    }

    [Test]
    public void UppercaseAsciiIsIndependentOfCurrentCulture()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            string unchanged = "USD";

            Assert.Multiple(() =>
            {
                Assert.That(AsciiCodeText.UppercaseAscii("usd"), Is.EqualTo("USD"));
                Assert.That(AsciiCodeText.UppercaseAscii("iıI"), Is.EqualTo("IıI"));
                Assert.That(AsciiCodeText.UppercaseAscii(string.Empty), Is.Empty);
                Assert.That(AsciiCodeText.UppercaseAscii(unchanged), Is.SameAs(unchanged));
            });
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Test]
    public void PadNumericCodeProducesCanonicalThreeCharacterWidth()
    {
        string canonical = "008";

        Assert.Multiple(() =>
        {
            Assert.That(AsciiCodeText.PadNumericCode("8"), Is.EqualTo("008"));
            Assert.That(AsciiCodeText.PadNumericCode("12"), Is.EqualTo("012"));
            Assert.That(AsciiCodeText.PadNumericCode(string.Empty), Is.EqualTo("000"));
            Assert.That(AsciiCodeText.PadNumericCode(canonical), Is.SameAs(canonical));
            Assert.That(AsciiCodeText.PadNumericCode("1234"), Is.EqualTo("1234"));
        });
    }
}
