using System;

namespace diameter;

internal static class CreditControlUnitArithmetic
{
    internal static DiameterServiceUnit AddCreditControlServiceUnits(
        DiameterServiceUnit? current,
        DiameterServiceUnit? addition)
    {
        DiameterServiceUnit result = new DiameterServiceUnit();
        if (current != null)
        {
            DiameterServiceUnit? cloned = CreditControlDataCloner
                .CloneOptionalCreditControlServiceUnit(current);
            if (cloned == null)
            {
                throw new DiameterCreditControlException(
                    "The cumulative service unit could not be created.");
            }

            result = cloned;
        }

        if (addition == null)
        {
            return result;
        }

        addition.ValidateCreditControlServiceUnit();
        AddCreditControlTime(result, addition);
        AddCreditControlMoney(result, addition);
        AddCreditControlTotalOctets(result, addition);
        AddCreditControlInputOctets(result, addition);
        AddCreditControlOutputOctets(result, addition);
        AddCreditControlSpecificUnits(result, addition);
        result.ValidateCreditControlServiceUnit();
        return result;
    }

    internal static DiameterCreditControlDisposition
        ReadCreditControlGrantDisposition(
            DiameterServiceUnit? requested,
            DiameterServiceUnit? granted,
            DiameterCreditControlDisposition declared)
    {
        if (declared != DiameterCreditControlDisposition.Granted ||
            requested == null)
        {
            return declared;
        }

        if (granted == null ||
            requested.TimePresent &&
            (!granted.TimePresent || granted.Time < requested.Time) ||
            requested.TotalOctetsPresent &&
            (!granted.TotalOctetsPresent ||
            granted.TotalOctets < requested.TotalOctets) ||
            requested.InputOctetsPresent &&
            (!granted.InputOctetsPresent ||
            granted.InputOctets < requested.InputOctets) ||
            requested.OutputOctetsPresent &&
            (!granted.OutputOctetsPresent ||
            granted.OutputOctets < requested.OutputOctets) ||
            requested.ServiceSpecificUnitsPresent &&
            (!granted.ServiceSpecificUnitsPresent ||
            granted.ServiceSpecificUnits <
                requested.ServiceSpecificUnits) ||
            IsCreditControlMoneyGrantPartial(
                requested.Money,
                granted.Money))
        {
            return DiameterCreditControlDisposition.PartiallyGranted;
        }

        return declared;
    }

    internal static void AddCreditControlTime(
        DiameterServiceUnit result,
        DiameterServiceUnit addition)
    {
        if (!addition.TimePresent)
        {
            return;
        }

        if (result.TimePresent &&
            uint.MaxValue - result.Time < addition.Time)
        {
            throw new DiameterCreditControlException(
                "Cumulative CC-Time exceeds Unsigned32.");
        }

        result.Time = result.TimePresent ?
            result.Time + addition.Time : addition.Time;
        result.TimePresent = true;
    }

    internal static void AddCreditControlMoney(
        DiameterServiceUnit result,
        DiameterServiceUnit addition)
    {
        if (addition.Money == null)
        {
            return;
        }

        if (result.Money == null)
        {
            result.Money = CreditControlDataCloner
                .CloneOptionalCreditControlCost(addition.Money);
            return;
        }

        DiameterCostInformation current = result.Money;
        DiameterCostInformation added = addition.Money;
        int currentExponent = current.ExponentPresent ?
            current.Exponent : 0;
        int addedExponent = added.ExponentPresent ? added.Exponent : 0;
        if (current.CurrencyCodePresent != added.CurrencyCodePresent ||
            current.CurrencyCodePresent &&
            current.CurrencyCode != added.CurrencyCode ||
            current.CostUnit != added.CostUnit)
        {
            throw new DiameterCreditControlException(
                "Cumulative CC-Money requires matching currency and cost unit.");
        }

        if (added.ValueDigits == 0L)
        {
            return;
        }

        if (current.ValueDigits == 0L)
        {
            current.ValueDigits = added.ValueDigits;
            current.Exponent = added.Exponent;
            current.ExponentPresent = added.ExponentPresent;
            return;
        }

        int resultExponent = currentExponent < addedExponent ?
            currentExponent : addedExponent;
        long currentPlaces = (long)currentExponent - resultExponent;
        long addedPlaces = (long)addedExponent - resultExponent;
        long currentDigits = ScaleCreditControlValueDigits(
            current.ValueDigits,
            currentPlaces);
        long addedDigits = ScaleCreditControlValueDigits(
            added.ValueDigits,
            addedPlaces);
        if (addedDigits > 0L &&
            currentDigits > long.MaxValue - addedDigits ||
            addedDigits < 0L &&
            currentDigits < long.MinValue - addedDigits)
        {
            throw new DiameterCreditControlException(
                "Cumulative CC-Money value digits exceed Integer64.");
        }

        current.ValueDigits = currentDigits + addedDigits;
        current.Exponent = resultExponent;
        current.ExponentPresent = resultExponent != 0 ||
            current.ExponentPresent || added.ExponentPresent;
    }

    internal static void AddCreditControlTotalOctets(
        DiameterServiceUnit result,
        DiameterServiceUnit addition)
    {
        if (!addition.TotalOctetsPresent)
        {
            return;
        }

        result.TotalOctets = AddCreditControlUnsigned64(
            result.TotalOctets,
            result.TotalOctetsPresent,
            addition.TotalOctets,
            "CC-Total-Octets");
        result.TotalOctetsPresent = true;
    }

    internal static void AddCreditControlInputOctets(
        DiameterServiceUnit result,
        DiameterServiceUnit addition)
    {
        if (!addition.InputOctetsPresent)
        {
            return;
        }

        result.InputOctets = AddCreditControlUnsigned64(
            result.InputOctets,
            result.InputOctetsPresent,
            addition.InputOctets,
            "CC-Input-Octets");
        result.InputOctetsPresent = true;
    }

    internal static void AddCreditControlOutputOctets(
        DiameterServiceUnit result,
        DiameterServiceUnit addition)
    {
        if (!addition.OutputOctetsPresent)
        {
            return;
        }

        result.OutputOctets = AddCreditControlUnsigned64(
            result.OutputOctets,
            result.OutputOctetsPresent,
            addition.OutputOctets,
            "CC-Output-Octets");
        result.OutputOctetsPresent = true;
    }

    internal static void AddCreditControlSpecificUnits(
        DiameterServiceUnit result,
        DiameterServiceUnit addition)
    {
        if (!addition.ServiceSpecificUnitsPresent)
        {
            return;
        }

        result.ServiceSpecificUnits = AddCreditControlUnsigned64(
            result.ServiceSpecificUnits,
            result.ServiceSpecificUnitsPresent,
            addition.ServiceSpecificUnits,
            "CC-Service-Specific-Units");
        result.ServiceSpecificUnitsPresent = true;
    }

    internal static ulong AddCreditControlUnsigned64(
        ulong current,
        bool currentPresent,
        ulong addition,
        string name)
    {
        if (!currentPresent)
        {
            return addition;
        }

        if (ulong.MaxValue - current < addition)
        {
            throw new DiameterCreditControlException(
                "Cumulative " + name + " exceeds Unsigned64.");
        }

        return current + addition;
    }

    internal static bool IsCreditControlMoneyGrantPartial(
        DiameterCostInformation? requested,
        DiameterCostInformation? granted)
    {
        if (requested == null)
        {
            return false;
        }

        if (granted == null)
        {
            return true;
        }

        int requestedExponent = requested.ExponentPresent ?
            requested.Exponent : 0;
        int grantedExponent = granted.ExponentPresent ?
            granted.Exponent : 0;
        return requested.CurrencyCodePresent != granted.CurrencyCodePresent ||
            requested.CurrencyCodePresent &&
            requested.CurrencyCode != granted.CurrencyCode ||
            requested.CostUnit != granted.CostUnit ||
            CompareCreditControlUnitValues(
                granted.ValueDigits,
                grantedExponent,
                requested.ValueDigits,
                requestedExponent) < 0;
    }

    internal static bool IsCreditControlMoneyQuotaExhausted(
        DiameterCostInformation? granted,
        DiameterCostInformation? used)
    {
        if (granted == null || used == null ||
            granted.CurrencyCodePresent != used.CurrencyCodePresent ||
            granted.CurrencyCodePresent &&
            granted.CurrencyCode != used.CurrencyCode ||
            granted.CostUnit != used.CostUnit)
        {
            return false;
        }

        int grantedExponent = granted.ExponentPresent ?
            granted.Exponent : 0;
        int usedExponent = used.ExponentPresent ?
            used.Exponent : 0;
        return CompareCreditControlUnitValues(
            used.ValueDigits,
            usedExponent,
            granted.ValueDigits,
            grantedExponent) >= 0;
    }

    internal static long ScaleCreditControlValueDigits(
        long valueDigits,
        long decimalPlaces)
    {
        if (decimalPlaces < 0L)
        {
            throw new DiameterCreditControlException(
                "A decimal value cannot be scaled by negative places.");
        }

        if (valueDigits == 0L || decimalPlaces == 0L)
        {
            return valueDigits;
        }

        if (decimalPlaces > 18L)
        {
            throw new DiameterCreditControlException(
                "Scaled decimal value digits exceed Integer64.");
        }

        long result = valueDigits;
        long index = 0L;
        while (index < decimalPlaces)
        {
            if (result > long.MaxValue / 10L ||
                result < long.MinValue / 10L)
            {
                throw new DiameterCreditControlException(
                    "Scaled decimal value digits exceed Integer64.");
            }

            result = result * 10L;
            index = index + 1L;
        }

        return result;
    }

    internal static int CompareCreditControlUnitValues(
        long firstValueDigits,
        int firstExponent,
        long secondValueDigits,
        int secondExponent)
    {
        if (firstValueDigits == 0L || secondValueDigits == 0L)
        {
            if (firstValueDigits == secondValueDigits)
            {
                return 0;
            }

            return firstValueDigits < secondValueDigits ? -1 : 1;
        }

        bool firstNegative = firstValueDigits < 0L;
        bool secondNegative = secondValueDigits < 0L;
        if (firstNegative != secondNegative)
        {
            return firstNegative ? -1 : 1;
        }

        string firstText = ReadCreditControlMagnitude(firstValueDigits)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);
        string secondText = ReadCreditControlMagnitude(secondValueDigits)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);
        long firstOrder = (long)firstText.Length + firstExponent;
        long secondOrder = (long)secondText.Length + secondExponent;
        int magnitudeComparison = 0;
        if (firstOrder < secondOrder)
        {
            magnitudeComparison = -1;
        }
        else if (firstOrder > secondOrder)
        {
            magnitudeComparison = 1;
        }
        else
        {
            int length = firstText.Length > secondText.Length ?
                firstText.Length : secondText.Length;
            int index = 0;
            while (index < length && magnitudeComparison == 0)
            {
                char firstDigit = index < firstText.Length ?
                    firstText[index] : '0';
                char secondDigit = index < secondText.Length ?
                    secondText[index] : '0';
                if (firstDigit < secondDigit)
                {
                    magnitudeComparison = -1;
                }
                else if (firstDigit > secondDigit)
                {
                    magnitudeComparison = 1;
                }

                index = index + 1;
            }
        }

        return firstNegative ? -magnitudeComparison :
            magnitudeComparison;
    }

    internal static bool AreCreditControlUnitValuesEqual(
        long firstValueDigits,
        int firstExponent,
        long secondValueDigits,
        int secondExponent)
    {
        return CompareCreditControlUnitValues(
            firstValueDigits,
            firstExponent,
            secondValueDigits,
            secondExponent) == 0;
    }

    internal static ulong ReadCreditControlMagnitude(long valueDigits)
    {
        if (valueDigits >= 0L)
        {
            return (ulong)valueDigits;
        }

        return (ulong)(-(valueDigits + 1L)) + 1UL;
    }
}
