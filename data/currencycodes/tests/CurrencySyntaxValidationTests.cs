using System.Globalization;

namespace currencycodes.Tests;

public class CurrencySyntaxValidationTests
{
    [Test]
    public void AlphabeticSyntaxTrimsAndNormalizesAsciiLetters()
    {
        CurrencyValidationResult result =
            CurrencyValidationRules.ValidateCurrencyAlphabeticSyntax(" \t\u2003uSd\u00A0\r\n");

        AssertValid(result, "USD");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \u2003 ")]
    [TestCase("US")]
    [TestCase("USDD")]
    [TestCase("US1")]
    [TestCase("U D")]
    [TestCase("US\u00C9")]
    [TestCase("\uFF35\uFF33\uFF24")]
    public void AlphabeticSyntaxRejectsMalformedText(string? code)
    {
        CurrencyValidationResult result =
            CurrencyValidationRules.ValidateCurrencyAlphabeticSyntax(code);

        AssertMalformed(result, "The currency alphabetic code is malformed.");
    }

    [Test]
    public void NumericSyntaxTrimsAndPreservesCanonicalLeadingZeroes()
    {
        CurrencyValidationResult result =
            CurrencyValidationRules.ValidateCurrencyNumericSyntax(" \t\u2003008\u00A0\r\n");

        AssertValid(result, "008");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \u2003 ")]
    [TestCase("8")]
    [TestCase("08")]
    [TestCase("0008")]
    [TestCase("+08")]
    [TestCase("-08")]
    [TestCase("0 8")]
    [TestCase("00A")]
    [TestCase("\u0660\u0660\u0668")]
    [TestCase("\uFF10\uFF10\uFF18")]
    public void NumericSyntaxRequiresExactlyThreeAsciiDigits(string? code)
    {
        CurrencyValidationResult result = CurrencyValidationRules.ValidateCurrencyNumericSyntax(code);

        AssertMalformed(result, "The currency numeric code is malformed.");
    }

    [Test]
    public void SyntaxValidationDoesNotRequireCatalogAssignment()
    {
        CurrencyValidationResult alphabetic =
            CurrencyValidationRules.ValidateCurrencyAlphabeticSyntax("zzz");
        CurrencyValidationResult numeric = CurrencyValidationRules.ValidateCurrencyNumericSyntax("000");

        Assert.Multiple(() =>
        {
            AssertValid(alphabetic, "ZZZ");
            AssertValid(numeric, "000");
        });
    }

    [Test]
    public void NumericValidationRemainsStrictWhenLenientNormalizationSucceeds()
    {
        NormalizationResult normalization = CurrencyNormalizationRules.NormalizeCurrencyNumericCode("8");
        CurrencyValidationResult validation = CurrencyValidationRules.ValidateCurrencyNumericSyntax("8");

        Assert.Multiple(() =>
        {
            Assert.That(normalization.Success, Is.True);
            Assert.That(normalization.NormalizedText, Is.EqualTo("008"));
            AssertMalformed(validation, "The currency numeric code is malformed.");
        });
    }

    [Test]
    public void SyntaxValidationIsIndependentOfCurrentCulture()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");

            CurrencyValidationResult alphabetic =
                CurrencyValidationRules.ValidateCurrencyAlphabeticSyntax("try");
            CurrencyValidationResult localizedNumeric =
                CurrencyValidationRules.ValidateCurrencyNumericSyntax("\u0660\u0660\u0668");

            Assert.Multiple(() =>
            {
                AssertValid(alphabetic, "TRY");
                AssertMalformed(localizedNumeric, "The currency numeric code is malformed.");
            });
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Test]
    public void SyntaxValidationReturnsIndependentResultObjects()
    {
        CurrencyValidationResult first =
            CurrencyValidationRules.ValidateCurrencyAlphabeticSyntax("usd");
        CurrencyValidationResult second =
            CurrencyValidationRules.ValidateCurrencyAlphabeticSyntax("usd");
        CurrencyValidationResult firstFailure =
            CurrencyValidationRules.ValidateCurrencyNumericSyntax("8");
        CurrencyValidationResult secondFailure =
            CurrencyValidationRules.ValidateCurrencyNumericSyntax("8");

        first.NormalizedText = "changed";
        firstFailure.Reason = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.NormalizedText, Is.EqualTo("USD"));
            Assert.That(secondFailure, Is.Not.SameAs(firstFailure));
            Assert.That(
                secondFailure.Reason,
                Is.EqualTo("The currency numeric code is malformed."));
        });
    }

    private static void AssertValid(CurrencyValidationResult result, string normalized)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsIndeterminate, Is.False);
            Assert.That(result.Error, Is.EqualTo(CurrencyValidationError.None));
            Assert.That(result.NormalizedText, Is.EqualTo(normalized));
            Assert.That(result.Reason, Is.Empty);
            Assert.That(result.Currency, Is.Null);
        });
    }

    private static void AssertMalformed(CurrencyValidationResult result, string reason)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IsIndeterminate, Is.False);
            Assert.That(result.Error, Is.EqualTo(CurrencyValidationError.MalformedInput));
            Assert.That(result.NormalizedText, Is.Empty);
            Assert.That(result.Reason, Is.EqualTo(reason));
            Assert.That(result.Currency, Is.Null);
        });
    }
}
