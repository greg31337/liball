using System;

namespace currencycodes.Tests;

public class CurrencyValidatorContractTests
{
    [Test]
    public void ValidationOperationsHaveCompleteUniqueNames()
    {
        string[] names = Array.ConvertAll(
            typeof(ICurrencyValidator).GetMethods(),
            method => method.Name);
        Array.Sort(names, StringComparer.Ordinal);

        Assert.That(names, Is.EqualTo(new[]
        {
            "ValidateCurrencyAlphabeticSyntax",
            "ValidateCurrencyAmount",
            "ValidateCurrencyAssignment",
            "ValidateCurrencyNumericSyntax",
            "ValidateCurrencyOnDate",
            "ValidateTerritoryAssignment",
            "ValidateTerritoryCurrencyAssociation"
        }));
    }

    [Test]
    public void ValidationOperationsExposePlannedSignatures()
    {
        AssertSignature(
            "ValidateCurrencyAlphabeticSyntax",
            typeof(CurrencyValidationResult),
            typeof(string));
        AssertSignature(
            "ValidateCurrencyNumericSyntax",
            typeof(CurrencyValidationResult),
            typeof(string));
        AssertSignature(
            "ValidateCurrencyAssignment",
            typeof(CurrencyValidationResult),
            typeof(string),
            typeof(CurrencySet));
        AssertSignature(
            "ValidateCurrencyOnDate",
            typeof(CurrencyValidationResult),
            typeof(string),
            typeof(DateOnly));
        AssertSignature(
            "ValidateTerritoryAssignment",
            typeof(CurrencyValidationResult),
            typeof(string));
        AssertSignature(
            "ValidateTerritoryCurrencyAssociation",
            typeof(CurrencyValidationResult),
            typeof(string),
            typeof(string),
            typeof(DateOnly),
            typeof(bool));
        AssertSignature(
            "ValidateCurrencyAmount",
            typeof(AmountValidationResult),
            typeof(decimal),
            typeof(string),
            typeof(AmountUsage));
    }

    private static void AssertSignature(string name, Type returnType, params Type[] parameterTypes)
    {
        System.Reflection.MethodInfo? method = typeof(ICurrencyValidator).GetMethod(
            name,
            parameterTypes);

        Assert.That(method, Is.Not.Null, name);
        Assert.That(method?.ReturnType, Is.EqualTo(returnType), name);
    }
}
