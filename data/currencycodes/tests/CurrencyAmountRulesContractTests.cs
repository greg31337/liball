using System;

namespace currencycodes.Tests;

public class CurrencyAmountRulesContractTests
{
    [Test]
    public void AmountRuleOperationsHaveCompleteUniqueNames()
    {
        string[] names = Array.ConvertAll(
            typeof(ICurrencyAmountRules).GetMethods(),
            method => method.Name);
        Array.Sort(names, StringComparer.Ordinal);

        Assert.That(names, Is.EqualTo(new[]
        {
            "ConvertCurrencyMajorToMinor",
            "ConvertCurrencyMinorToMajor",
            "FormatInvariantCurrencyAmount",
            "ParseInvariantCurrencyAmount",
            "RoundCurrencyAmount"
        }));
    }

    [Test]
    public void AmountRuleOperationsExposePlannedSignatures()
    {
        AssertSignature(
            "RoundCurrencyAmount",
            typeof(decimal),
            typeof(decimal),
            typeof(string),
            typeof(AmountUsage),
            typeof(MidpointRounding));
        AssertSignature(
            "ConvertCurrencyMajorToMinor",
            typeof(MinorUnitConversionResult),
            typeof(decimal),
            typeof(string),
            typeof(AmountUsage));
        AssertSignature(
            "ConvertCurrencyMinorToMajor",
            typeof(MinorUnitConversionResult),
            typeof(long),
            typeof(string),
            typeof(AmountUsage));
        AssertSignature(
            "FormatInvariantCurrencyAmount",
            typeof(AmountTextResult),
            typeof(decimal),
            typeof(string),
            typeof(AmountUsage),
            typeof(MidpointRounding));
        AssertSignature(
            "ParseInvariantCurrencyAmount",
            typeof(AmountTextResult),
            typeof(string),
            typeof(AmountUsage));
    }

    private static void AssertSignature(string name, Type returnType, params Type[] parameterTypes)
    {
        System.Reflection.MethodInfo? method = typeof(ICurrencyAmountRules).GetMethod(
            name,
            parameterTypes);

        Assert.That(method, Is.Not.Null, name);
        Assert.That(method?.ReturnType, Is.EqualTo(returnType), name);
    }
}
