using System;

namespace currencycodes.Tests;

public class CurrencyNormalizerContractTests
{
    [Test]
    public void NormalizationOperationsHaveCompleteUniqueNames()
    {
        string[] names = Array.ConvertAll(
            typeof(ICurrencyNormalizer).GetMethods(),
            method => method.Name);
        Array.Sort(names, StringComparer.Ordinal);

        Assert.That(names, Is.EqualTo(new[]
        {
            "NormalizeCurrencyAlphabeticCode",
            "NormalizeCurrencyNumericCode",
            "NormalizeTerritoryAlpha2Code",
            "NormalizeTerritoryAlpha3Code"
        }));
    }

    [Test]
    public void NormalizationOperationsReturnStructuredResultsForTextInput()
    {
        AssertSignature("NormalizeCurrencyAlphabeticCode");
        AssertSignature("NormalizeCurrencyNumericCode");
        AssertSignature("NormalizeTerritoryAlpha2Code");
        AssertSignature("NormalizeTerritoryAlpha3Code");
    }

    private static void AssertSignature(string name)
    {
        System.Reflection.MethodInfo? method = typeof(ICurrencyNormalizer).GetMethod(
            name,
            new[] { typeof(string) });

        Assert.That(method, Is.Not.Null, name);
        Assert.That(method?.ReturnType, Is.EqualTo(typeof(NormalizationResult)), name);
    }
}
