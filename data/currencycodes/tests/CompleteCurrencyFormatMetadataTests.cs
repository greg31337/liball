using System.Collections.Generic;

namespace currencycodes.Tests;

public class CompleteCurrencyFormatMetadataTests
{
    [Test]
    public void EveryCurrencyMatchesIndependentIsoAndCldrFixtures()
    {
        Dictionary<string, ExpectedCurrencyFormatMetadata> expected =
            PinnedCurrencyFormatFixture.Load();
        ICurrencyCatalog catalog = new CurrencyCodeService();
        CurrencyInfo[] currencies = catalog.GetCurrencyCatalog(CurrencySet.All);
        HashSet<string> catalogCodes = new HashSet<string>(StringComparer.Ordinal);

        foreach (CurrencyInfo currency in currencies)
        {
            catalogCodes.Add(currency.AlphabeticCode);
        }

        Assert.Multiple(() =>
        {
            Assert.That(expected, Has.Count.EqualTo(326));
            Assert.That(catalogCodes, Is.EquivalentTo(expected.Keys));
            Assert.That(CountExplicitRules(expected), Is.EqualTo(74));
            Assert.That(CountInheritedRules(expected), Is.EqualTo(252));
            Assert.That(CountExplicitCashDigits(expected), Is.EqualTo(13));
            Assert.That(CountExplicitCashRounding(expected), Is.EqualTo(17));
        });

        string[] codes = expected.Keys.ToArray();
        Array.Sort(codes, StringComparer.Ordinal);
        foreach (string code in codes)
        {
            CurrencyFormatMetadata actual = catalog.GetCurrencyFormatMetadata(
                " \t" + code.ToLowerInvariant() + "\u00A0 ");
            AssertMetadata(actual, expected[code]);
        }
    }

    [Test]
    public void EveryPinnedIsoAndCldrDigitDifferenceRemainsSeparate()
    {
        Dictionary<string, ExpectedCurrencyFormatMetadata> expected =
            PinnedCurrencyFormatFixture.Load();
        ICurrencyCatalog catalog = new CurrencyCodeService();
        int differenceCount = 0;

        foreach (ExpectedCurrencyFormatMetadata item in expected.Values)
        {
            if (!item.HasIsoMinorUnit
                || item.IsoMinorUnitDigits == item.StandardFormatDigits)
            {
                continue;
            }

            differenceCount++;
            CurrencyFormatMetadata actual = catalog.GetCurrencyFormatMetadata(item.CurrencyCode);
            Assert.Multiple(() =>
            {
                Assert.That(
                    actual.IsoMinorUnitDigits,
                    Is.EqualTo(item.IsoMinorUnitDigits),
                    item.CurrencyCode);
                Assert.That(
                    actual.StandardFormatDigits,
                    Is.EqualTo(item.StandardFormatDigits),
                    item.CurrencyCode);
                Assert.That(
                    actual.IsoMinorUnitDigits,
                    Is.Not.EqualTo(actual.StandardFormatDigits),
                    item.CurrencyCode);
            });
        }

        Assert.That(differenceCount, Is.EqualTo(16));
    }

    [Test]
    public void CashRulesResolveFromTheirSpecifiedInheritanceLevel()
    {
        ICurrencyCatalog catalog = new CurrencyCodeService();
        CurrencyFormatMetadata defaultRule = catalog.GetCurrencyFormatMetadata("USD");
        CurrencyFormatMetadata explicitStandard = catalog.GetCurrencyFormatMetadata("AFN");
        CurrencyFormatMetadata explicitCashIncrement = catalog.GetCurrencyFormatMetadata("CHF");
        CurrencyFormatMetadata explicitCashDigits = catalog.GetCurrencyFormatMetadata("AMD");

        Assert.Multiple(() =>
        {
            AssertRule(defaultRule, 2, 0, 2, 0);
            AssertRule(explicitStandard, 0, 0, 0, 0);
            AssertRule(explicitCashIncrement, 2, 0, 2, 5);
            AssertRule(explicitCashDigits, 2, 0, 0, 0);
        });
    }

    private static void AssertMetadata(
        CurrencyFormatMetadata actual,
        ExpectedCurrencyFormatMetadata expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.CurrencyCode, Is.EqualTo(expected.CurrencyCode), expected.CurrencyCode);
            Assert.That(
                actual.HasIsoMinorUnit,
                Is.EqualTo(expected.HasIsoMinorUnit),
                expected.CurrencyCode);
            Assert.That(
                actual.IsoMinorUnitDigits,
                Is.EqualTo(expected.IsoMinorUnitDigits),
                expected.CurrencyCode);
            Assert.That(
                actual.IsoMinorUnitFactor,
                Is.EqualTo(expected.IsoMinorUnitFactor),
                expected.CurrencyCode);
            Assert.That(actual.HasStandardFormat, Is.True, expected.CurrencyCode);
            Assert.That(
                actual.StandardFormatDigits,
                Is.EqualTo(expected.StandardFormatDigits),
                expected.CurrencyCode);
            Assert.That(
                actual.StandardRoundingIncrement,
                Is.EqualTo(expected.StandardRoundingIncrement),
                expected.CurrencyCode);
            Assert.That(
                actual.StandardFormatFactor,
                Is.EqualTo(expected.StandardFormatFactor),
                expected.CurrencyCode);
            Assert.That(actual.HasCash, Is.True, expected.CurrencyCode);
            Assert.That(actual.CashDigits, Is.EqualTo(expected.CashDigits), expected.CurrencyCode);
            Assert.That(
                actual.CashRoundingIncrement,
                Is.EqualTo(expected.CashRoundingIncrement),
                expected.CurrencyCode);
            Assert.That(actual.CashFactor, Is.EqualTo(expected.CashFactor), expected.CurrencyCode);
            Assert.That(actual.SourceVersion, Is.EqualTo("48.2"), expected.CurrencyCode);
        });
    }

    private static void AssertRule(
        CurrencyFormatMetadata actual,
        int standardDigits,
        int standardRounding,
        int cashDigits,
        int cashRounding)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.StandardFormatDigits, Is.EqualTo(standardDigits));
            Assert.That(actual.StandardRoundingIncrement, Is.EqualTo(standardRounding));
            Assert.That(actual.CashDigits, Is.EqualTo(cashDigits));
            Assert.That(actual.CashRoundingIncrement, Is.EqualTo(cashRounding));
        });
    }

    private static int CountExplicitRules(
        Dictionary<string, ExpectedCurrencyFormatMetadata> expected)
    {
        return expected.Values.Count(item => item.CldrRuleIsExplicit);
    }

    private static int CountInheritedRules(
        Dictionary<string, ExpectedCurrencyFormatMetadata> expected)
    {
        return expected.Values.Count(item => !item.CldrRuleIsExplicit);
    }

    private static int CountExplicitCashDigits(
        Dictionary<string, ExpectedCurrencyFormatMetadata> expected)
    {
        return expected.Values.Count(item => item.CashDigitsIsExplicit);
    }

    private static int CountExplicitCashRounding(
        Dictionary<string, ExpectedCurrencyFormatMetadata> expected)
    {
        return expected.Values.Count(item => item.CashRoundingIsExplicit);
    }
}
