using System.Globalization;

namespace currencycodes.Tests;

public class CurrencyNormalizationRulesTests
{
    [Test]
    public void AlphabeticOperationsTrimUnicodeWhitespaceAndUppercaseAscii()
    {
        string currencyInput = " \t\u2003uSd\u00A0\r\n";
        string alpha2Input = " \u2003tr ";
        string alpha3Input = " bGr\u00A0";

        NormalizationResult currency = CurrencyNormalizationRules.NormalizeCurrencyAlphabeticCode(
            currencyInput);
        NormalizationResult alpha2 = CurrencyNormalizationRules.NormalizeTerritoryAlpha2Code(alpha2Input);
        NormalizationResult alpha3 = CurrencyNormalizationRules.NormalizeTerritoryAlpha3Code(alpha3Input);

        Assert.Multiple(() =>
        {
            AssertSuccess(currency, currencyInput, "USD");
            AssertSuccess(alpha2, alpha2Input, "TR");
            AssertSuccess(alpha3, alpha3Input, "BGR");
        });
    }

    [TestCase("0", "000")]
    [TestCase("8", "008")]
    [TestCase("12", "012")]
    [TestCase("008", "008")]
    [TestCase("999", "999")]
    [TestCase(" \u20038\u00A0 ", "008")]
    public void NumericOperationAcceptsOneToThreeAsciiDigits(string input, string expected)
    {
        NormalizationResult result = CurrencyNormalizationRules.NormalizeCurrencyNumericCode(input);

        AssertSuccess(result, input, expected);
    }

    [Test]
    public void NullInputsReturnTheNullInputCategory()
    {
        NormalizationResult currency = CurrencyNormalizationRules.NormalizeCurrencyAlphabeticCode(null);
        NormalizationResult numeric = CurrencyNormalizationRules.NormalizeCurrencyNumericCode(null);
        NormalizationResult alpha2 = CurrencyNormalizationRules.NormalizeTerritoryAlpha2Code(null);
        NormalizationResult alpha3 = CurrencyNormalizationRules.NormalizeTerritoryAlpha3Code(null);

        Assert.Multiple(() =>
        {
            AssertFailure(currency, NormalizationError.NullInput, null);
            AssertFailure(numeric, NormalizationError.NullInput, null);
            AssertFailure(alpha2, NormalizationError.NullInput, null);
            AssertFailure(alpha3, NormalizationError.NullInput, null);
        });
    }

    [Test]
    public void EmptyAndWhitespaceInputsReturnTheEmptyInputCategory()
    {
        string whitespace = " \t\u2003\u00A0\r\n";
        NormalizationResult currency = CurrencyNormalizationRules.NormalizeCurrencyAlphabeticCode(
            string.Empty);
        NormalizationResult numeric = CurrencyNormalizationRules.NormalizeCurrencyNumericCode(whitespace);
        NormalizationResult alpha2 = CurrencyNormalizationRules.NormalizeTerritoryAlpha2Code(whitespace);
        NormalizationResult alpha3 = CurrencyNormalizationRules.NormalizeTerritoryAlpha3Code(string.Empty);

        Assert.Multiple(() =>
        {
            AssertFailure(currency, NormalizationError.EmptyInput, string.Empty);
            AssertFailure(numeric, NormalizationError.EmptyInput, whitespace);
            AssertFailure(alpha2, NormalizationError.EmptyInput, whitespace);
            AssertFailure(alpha3, NormalizationError.EmptyInput, string.Empty);
        });
    }

    [Test]
    public void InvalidWidthsReturnTheLengthCategoryBeforeCharacterValidation()
    {
        NormalizationResult currencyShort =
            CurrencyNormalizationRules.NormalizeCurrencyAlphabeticCode("US");
        NormalizationResult currencyLong =
            CurrencyNormalizationRules.NormalizeCurrencyAlphabeticCode("US1D");
        NormalizationResult numericLong = CurrencyNormalizationRules.NormalizeCurrencyNumericCode("12A4");
        NormalizationResult alpha2Long = CurrencyNormalizationRules.NormalizeTerritoryAlpha2Code("US1");
        NormalizationResult alpha3Short = CurrencyNormalizationRules.NormalizeTerritoryAlpha3Code("U1");

        Assert.Multiple(() =>
        {
            AssertFailure(currencyShort, NormalizationError.InvalidLength, "US");
            AssertFailure(currencyLong, NormalizationError.InvalidLength, "US1D");
            AssertFailure(numericLong, NormalizationError.InvalidLength, "12A4");
            AssertFailure(alpha2Long, NormalizationError.InvalidLength, "US1");
            AssertFailure(alpha3Short, NormalizationError.InvalidLength, "U1");
        });
    }

    [Test]
    public void InvalidCharactersRejectPunctuationWhitespaceAndNonAsciiText()
    {
        NormalizationResult currencyDigit =
            CurrencyNormalizationRules.NormalizeCurrencyAlphabeticCode("US1");
        NormalizationResult currencyUnicode =
            CurrencyNormalizationRules.NormalizeCurrencyAlphabeticCode("US\u00C9");
        NormalizationResult numericSign = CurrencyNormalizationRules.NormalizeCurrencyNumericCode("+12");
        NormalizationResult numericArabic = CurrencyNormalizationRules.NormalizeCurrencyNumericCode("\u0668");
        NormalizationResult numericFullWidth = CurrencyNormalizationRules.NormalizeCurrencyNumericCode("\uFF18");
        NormalizationResult alpha2Punctuation =
            CurrencyNormalizationRules.NormalizeTerritoryAlpha2Code("U-");
        NormalizationResult alpha3Space = CurrencyNormalizationRules.NormalizeTerritoryAlpha3Code("U D");
        NormalizationResult alpha3FullWidth =
            CurrencyNormalizationRules.NormalizeTerritoryAlpha3Code("\uFF35\uFF33\uFF24");

        Assert.Multiple(() =>
        {
            AssertFailure(currencyDigit, NormalizationError.InvalidCharacter, "US1");
            AssertFailure(currencyUnicode, NormalizationError.InvalidCharacter, "US\u00C9");
            AssertFailure(numericSign, NormalizationError.InvalidCharacter, "+12");
            AssertFailure(numericArabic, NormalizationError.InvalidCharacter, "\u0668");
            AssertFailure(numericFullWidth, NormalizationError.InvalidCharacter, "\uFF18");
            AssertFailure(alpha2Punctuation, NormalizationError.InvalidCharacter, "U-");
            AssertFailure(alpha3Space, NormalizationError.InvalidCharacter, "U D");
            AssertFailure(
                alpha3FullWidth,
                NormalizationError.InvalidCharacter,
                "\uFF35\uFF33\uFF24");
        });
    }

    [Test]
    public void WellFormedUnassignedValuesStillNormalizeSuccessfully()
    {
        NormalizationResult currency =
            CurrencyNormalizationRules.NormalizeCurrencyAlphabeticCode("aaa");
        NormalizationResult numeric = CurrencyNormalizationRules.NormalizeCurrencyNumericCode("999");
        NormalizationResult alpha2 = CurrencyNormalizationRules.NormalizeTerritoryAlpha2Code("aa");
        NormalizationResult alpha3 = CurrencyNormalizationRules.NormalizeTerritoryAlpha3Code("aaa");

        Assert.Multiple(() =>
        {
            AssertSuccess(currency, "aaa", "AAA");
            AssertSuccess(numeric, "999", "999");
            AssertSuccess(alpha2, "aa", "AA");
            AssertSuccess(alpha3, "aaa", "AAA");
        });
    }

    [Test]
    public void NormalizationIsIndependentOfCurrentCulture()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");

            NormalizationResult currency =
                CurrencyNormalizationRules.NormalizeCurrencyAlphabeticCode("try");
            NormalizationResult territory =
                CurrencyNormalizationRules.NormalizeTerritoryAlpha2Code("tr");

            Assert.Multiple(() =>
            {
                AssertSuccess(currency, "try", "TRY");
                AssertSuccess(territory, "tr", "TR");
            });
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Test]
    public void EveryCallReturnsAnIndependentResultObject()
    {
        NormalizationResult first =
            CurrencyNormalizationRules.NormalizeCurrencyAlphabeticCode("usd");
        NormalizationResult second =
            CurrencyNormalizationRules.NormalizeCurrencyAlphabeticCode("usd");
        NormalizationResult firstFailure = CurrencyNormalizationRules.NormalizeCurrencyNumericCode("+");
        NormalizationResult secondFailure = CurrencyNormalizationRules.NormalizeCurrencyNumericCode("+");

        first.NormalizedText = "changed";
        firstFailure.Error = NormalizationError.None;

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.NormalizedText, Is.EqualTo("USD"));
            Assert.That(secondFailure, Is.Not.SameAs(firstFailure));
            Assert.That(secondFailure.Error, Is.EqualTo(NormalizationError.InvalidCharacter));
        });
    }

    private static void AssertSuccess(
        NormalizationResult result,
        string original,
        string normalized)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(NormalizationError.None));
            Assert.That(result.OriginalText, Is.EqualTo(original));
            Assert.That(result.NormalizedText, Is.EqualTo(normalized));
        });
    }

    private static void AssertFailure(
        NormalizationResult result,
        NormalizationError error,
        string? original)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(error));
            Assert.That(result.OriginalText, Is.EqualTo(original));
            Assert.That(result.NormalizedText, Is.Empty);
        });
    }
}
