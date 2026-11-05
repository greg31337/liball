using System;
using System.Collections.Generic;

namespace diameter.Tests;

public class CreditControlMonetaryArithmeticTests
{
    [Test]
    public void MoneyAccumulationAlignsDecimalExponentsExactly()
    {
        DiameterServiceUnit first = CreateMoneyUnit(125L, -2, 840U);
        DiameterServiceUnit second = CreateMoneyUnit(75L, -1, 840U);

        DiameterServiceUnit result = CreditControlUnitArithmetic
            .AddCreditControlServiceUnits(first, second);

        Assert.Multiple(() =>
        {
            Assert.That(result.Money!.ValueDigits, Is.EqualTo(875L));
            Assert.That(result.Money.Exponent, Is.EqualTo(-2));
            Assert.That(result.Money.ExponentPresent, Is.True);
            Assert.That(result.Money.CurrencyCode, Is.EqualTo(840U));
            Assert.That(first.Money!.ValueDigits, Is.EqualTo(125L));
        });
    }

    [Test]
    public void GrantDecisionComparesEquivalentDecimalRepresentations()
    {
        DiameterServiceUnit requested =
            CreateMoneyUnit(125L, -2, 978U);
        DiameterServiceUnit equivalent =
            CreateMoneyUnit(1250L, -3, 978U);
        DiameterServiceUnit partial =
            CreateMoneyUnit(1249L, -3, 978U);

        DiameterCreditControlDisposition complete =
            CreditControlUnitArithmetic.ReadCreditControlGrantDisposition(
                requested,
                equivalent,
                DiameterCreditControlDisposition.Granted);
        DiameterCreditControlDisposition incomplete =
            CreditControlUnitArithmetic.ReadCreditControlGrantDisposition(
                requested,
                partial,
                DiameterCreditControlDisposition.Granted);

        Assert.Multiple(() =>
        {
            Assert.That(complete,
                Is.EqualTo(DiameterCreditControlDisposition.Granted));
            Assert.That(incomplete,
                Is.EqualTo(
                    DiameterCreditControlDisposition.PartiallyGranted));
        });
    }

    [Test]
    public void DecimalComparisonHandlesExtremeExponentsAndSignedLimits()
    {
        int largeComparison = CreditControlUnitArithmetic
            .CompareCreditControlUnitValues(
                1L,
                int.MaxValue,
                long.MaxValue,
                int.MaxValue - 18);
        int negativeEquality = CreditControlUnitArithmetic
            .CompareCreditControlUnitValues(-10L, -1, -1L, 0);
        int signedLimitComparison = CreditControlUnitArithmetic
            .CompareCreditControlUnitValues(
                long.MinValue,
                int.MinValue,
                long.MaxValue,
                int.MinValue);

        Assert.Multiple(() =>
        {
            Assert.That(largeComparison, Is.LessThan(0));
            Assert.That(negativeEquality, Is.Zero);
            Assert.That(signedLimitComparison, Is.LessThan(0));
        });
    }

    [Test]
    public void DecimalAccumulationRejectsScalingOverflowBeforeMutation()
    {
        DiameterServiceUnit maximum =
            CreateMoneyUnit(long.MaxValue, 0, 840U);
        DiameterServiceUnit fraction =
            CreateMoneyUnit(1L, -1, 840U);

        Assert.Multiple(() =>
        {
            Assert.Throws<DiameterCreditControlException>(() =>
                CreditControlUnitArithmetic.AddCreditControlServiceUnits(
                    maximum,
                    fraction));
            Assert.That(maximum.Money!.ValueDigits,
                Is.EqualTo(long.MaxValue));
            Assert.That(maximum.Money.Exponent, Is.Zero);
        });
    }

    [Test]
    public void MoneyAccumulationRequiresMatchingCurrencyMetadata()
    {
        DiameterServiceUnit dollars = CreateMoneyUnit(100L, -2, 840U);
        DiameterServiceUnit euros = CreateMoneyUnit(100L, -2, 978U);

        Assert.Throws<DiameterCreditControlException>(() =>
            CreditControlUnitArithmetic.AddCreditControlServiceUnits(
                dollars,
                euros));
    }

    [Test]
    public void EquivalentPoolConversionValuesShareOnePoolState()
    {
        List<DiameterPoolReference> pools =
            new List<DiameterPoolReference>();
        DiameterPoolReference first = CreatePoolReference(10L, -1);
        DiameterPoolReference equivalent = CreatePoolReference(1L, 0);

        CreditControlMultipleServiceState.AddCreditControlPoolState(
            pools,
            first);
        CreditControlMultipleServiceState.AddCreditControlPoolState(
            pools,
            equivalent);

        Assert.That(pools, Has.Count.EqualTo(1));
    }

    private static DiameterServiceUnit CreateMoneyUnit(
        long valueDigits,
        int exponent,
        uint currencyCode)
    {
        DiameterCostInformation money =
            new DiameterCostInformation();
        money.ValueDigits = valueDigits;
        money.Exponent = exponent;
        money.ExponentPresent = true;
        money.CurrencyCode = currencyCode;
        money.CurrencyCodePresent = true;
        DiameterServiceUnit result = new DiameterServiceUnit();
        result.Money = money;
        return result;
    }

    private static DiameterPoolReference CreatePoolReference(
        long valueDigits,
        int exponent)
    {
        DiameterPoolReference result = new DiameterPoolReference();
        result.PoolIdentifier = 77U;
        result.UnitType = DiameterCreditControlUnitType.Time;
        result.ValueDigits = valueDigits;
        result.Exponent = exponent;
        result.ExponentPresent = true;
        return result;
    }
}
