using System.Globalization;

namespace currencycodes.Tests;

public class CompleteNormalizationTests
{
    [Test]
    public void EveryAsciiLetterInBothCasesNormalizesForEveryAlphabeticWidth()
    {
        ICurrencyNormalizer normalizer = new CurrencyCodeService();

        for (char uppercase = 'A'; uppercase <= 'Z'; uppercase++)
        {
            char lowercase = (char)(uppercase + 32);
            string upperCurrency = new string(uppercase, 3);
            string lowerCurrency = new string(lowercase, 3);
            string upperAlpha2 = new string(uppercase, 2);
            string lowerAlpha2 = new string(lowercase, 2);

            AssertSuccess(
                normalizer.NormalizeCurrencyAlphabeticCode(upperCurrency),
                upperCurrency,
                upperCurrency);
            AssertSuccess(
                normalizer.NormalizeCurrencyAlphabeticCode(lowerCurrency),
                lowerCurrency,
                upperCurrency);
            AssertSuccess(
                normalizer.NormalizeTerritoryAlpha2Code(upperAlpha2),
                upperAlpha2,
                upperAlpha2);
            AssertSuccess(
                normalizer.NormalizeTerritoryAlpha2Code(lowerAlpha2),
                lowerAlpha2,
                upperAlpha2);
            AssertSuccess(
                normalizer.NormalizeTerritoryAlpha3Code(upperCurrency),
                upperCurrency,
                upperCurrency);
            AssertSuccess(
                normalizer.NormalizeTerritoryAlpha3Code(lowerCurrency),
                lowerCurrency,
                upperCurrency);
        }
    }

    [Test]
    public void EveryAsciiDigitNormalizesAtOneTwoAndThreeCharacterWidths()
    {
        ICurrencyNormalizer normalizer = new CurrencyCodeService();

        for (char digit = '0'; digit <= '9'; digit++)
        {
            string oneDigit = digit.ToString();
            string twoDigits = "0" + digit;
            string threeDigits = "00" + digit;

            AssertSuccess(
                normalizer.NormalizeCurrencyNumericCode(oneDigit),
                oneDigit,
                threeDigits);
            AssertSuccess(
                normalizer.NormalizeCurrencyNumericCode(twoDigits),
                twoDigits,
                threeDigits);
            AssertSuccess(
                normalizer.NormalizeCurrencyNumericCode(threeDigits),
                threeDigits,
                threeDigits);
        }

        AssertSuccess(
            normalizer.NormalizeCurrencyNumericCode("120"),
            "120",
            "120");
        AssertSuccess(
            normalizer.NormalizeCurrencyNumericCode("007"),
            "007",
            "007");
        AssertSuccess(
            normalizer.NormalizeCurrencyNumericCode("999"),
            "999",
            "999");
    }

    [Test]
    public void EveryNormalizerTrimsSupportedSurroundingUnicodeWhitespace()
    {
        ICurrencyNormalizer normalizer = new CurrencyCodeService();
        string[] boundaries = new[]
        {
            " ",
            "\t",
            "\r\n",
            "\u00A0",
            "\u1680",
            "\u2003",
            "\u202F",
            "\u205F",
            "\u3000"
        };

        for (int index = 0; index < boundaries.Length; index++)
        {
            string boundary = boundaries[index];
            string currency = boundary + "uSd" + boundary;
            string numeric = boundary + "7" + boundary;
            string alpha2 = boundary + "gB" + boundary;
            string alpha3 = boundary + "gBr" + boundary;

            AssertSuccess(
                normalizer.NormalizeCurrencyAlphabeticCode(currency),
                currency,
                "USD");
            AssertSuccess(
                normalizer.NormalizeCurrencyNumericCode(numeric),
                numeric,
                "007");
            AssertSuccess(
                normalizer.NormalizeTerritoryAlpha2Code(alpha2),
                alpha2,
                "GB");
            AssertSuccess(
                normalizer.NormalizeTerritoryAlpha3Code(alpha3),
                alpha3,
                "GBR");
        }
    }

    [Test]
    public void NullEmptyAndWhitespaceOnlyInputsHaveDistinctFailures()
    {
        ICurrencyNormalizer normalizer = new CurrencyCodeService();
        string whitespace = " \t\r\n\u00A0\u2003 ";

        Assert.Multiple(() =>
        {
            AssertFailure(
                normalizer.NormalizeCurrencyAlphabeticCode(null),
                NormalizationError.NullInput,
                null);
            AssertFailure(
                normalizer.NormalizeCurrencyNumericCode(null),
                NormalizationError.NullInput,
                null);
            AssertFailure(
                normalizer.NormalizeTerritoryAlpha2Code(null),
                NormalizationError.NullInput,
                null);
            AssertFailure(
                normalizer.NormalizeTerritoryAlpha3Code(null),
                NormalizationError.NullInput,
                null);
            AssertFailure(
                normalizer.NormalizeCurrencyAlphabeticCode(string.Empty),
                NormalizationError.EmptyInput,
                string.Empty);
            AssertFailure(
                normalizer.NormalizeCurrencyNumericCode(string.Empty),
                NormalizationError.EmptyInput,
                string.Empty);
            AssertFailure(
                normalizer.NormalizeTerritoryAlpha2Code(whitespace),
                NormalizationError.EmptyInput,
                whitespace);
            AssertFailure(
                normalizer.NormalizeTerritoryAlpha3Code(whitespace),
                NormalizationError.EmptyInput,
                whitespace);
        });
    }

    [Test]
    public void OverlengthAndUnderlengthInputsReturnInvalidLengthBeforeCharacterChecks()
    {
        ICurrencyNormalizer normalizer = new CurrencyCodeService();
        string[] currencyLengths = new[] { "U", "US", "US1D", "USDXX" };
        string[] numericLengths = new[] { "1234", "12345", "1A34" };
        string[] alpha2Lengths = new[] { "U", "U1D", "USXX" };
        string[] alpha3Lengths = new[] { "U", "US", "US1D", "USDXX" };

        AssertFailures(
            currencyLengths,
            normalizer.NormalizeCurrencyAlphabeticCode,
            NormalizationError.InvalidLength);
        AssertFailures(
            numericLengths,
            normalizer.NormalizeCurrencyNumericCode,
            NormalizationError.InvalidLength);
        AssertFailures(
            alpha2Lengths,
            normalizer.NormalizeTerritoryAlpha2Code,
            NormalizationError.InvalidLength);
        AssertFailures(
            alpha3Lengths,
            normalizer.NormalizeTerritoryAlpha3Code,
            NormalizationError.InvalidLength);
    }

    [Test]
    public void SymbolsPunctuationEmbeddedWhitespaceAndUnicodeLettersAreRejected()
    {
        ICurrencyNormalizer normalizer = new CurrencyCodeService();
        string[] currencyInputs = new[]
        {
            "U1D", "U-D", "U.D", "U D", "$SD", "U_D", "UŚD", "İSD", "ＵＳＤ"
        };
        string[] alpha2Inputs = new[]
        {
            "U1", "U-", "U.", "$S", "U_", "UŚ", "İS", "ＵＳ"
        };
        string[] alpha3Inputs = new[]
        {
            "G1R", "G-R", "G.R", "G R", "$BR", "G_R", "GΒR", "İBR", "ＧＢＲ"
        };
        string[] numericInputs = new[]
        {
            "+1", "-1", "1.2", "1 2", "1,2", "1_2", "١", "۱۲", "१२३", "１２３", "²"
        };

        AssertFailures(
            currencyInputs,
            normalizer.NormalizeCurrencyAlphabeticCode,
            NormalizationError.InvalidCharacter);
        AssertFailures(
            alpha2Inputs,
            normalizer.NormalizeTerritoryAlpha2Code,
            NormalizationError.InvalidCharacter);
        AssertFailures(
            alpha3Inputs,
            normalizer.NormalizeTerritoryAlpha3Code,
            NormalizationError.InvalidCharacter);
        AssertFailures(
            numericInputs,
            normalizer.NormalizeCurrencyNumericCode,
            NormalizationError.InvalidCharacter);
    }

    [Test]
    public void TurkishCurrentCultureDoesNotAffectAnyNormalizer()
    {
        ICurrencyNormalizer normalizer = new CurrencyCodeService();
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");

            Assert.Multiple(() =>
            {
                AssertSuccess(
                    normalizer.NormalizeCurrencyAlphabeticCode("iIi"),
                    "iIi",
                    "III");
                AssertSuccess(
                    normalizer.NormalizeCurrencyNumericCode("8"),
                    "8",
                    "008");
                AssertSuccess(
                    normalizer.NormalizeTerritoryAlpha2Code("ir"),
                    "ir",
                    "IR");
                AssertSuccess(
                    normalizer.NormalizeTerritoryAlpha3Code("irq"),
                    "irq",
                    "IRQ");
                AssertFailure(
                    normalizer.NormalizeCurrencyAlphabeticCode("İIR"),
                    NormalizationError.InvalidCharacter,
                    "İIR");
            });
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    private static void AssertFailures(
        string[] inputs,
        Func<string?, NormalizationResult> normalize,
        NormalizationError error)
    {
        for (int index = 0; index < inputs.Length; index++)
        {
            AssertFailure(normalize(inputs[index]), error, inputs[index]);
        }
    }

    private static void AssertSuccess(
        NormalizationResult result,
        string original,
        string normalized)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, original);
            Assert.That(result.Error, Is.EqualTo(NormalizationError.None), original);
            Assert.That(result.OriginalText, Is.EqualTo(original), original);
            Assert.That(result.NormalizedText, Is.EqualTo(normalized), original);
        });
    }

    private static void AssertFailure(
        NormalizationResult result,
        NormalizationError error,
        string? original)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False, original);
            Assert.That(result.Error, Is.EqualTo(error), original);
            Assert.That(result.OriginalText, Is.EqualTo(original), original);
            Assert.That(result.NormalizedText, Is.Empty, original);
        });
    }
}
