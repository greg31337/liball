using System.Collections.Generic;

namespace diameter;

internal static class CreditControlRequestComparer
{
    internal static bool AreCreditControlRequestsEquivalent(
        DiameterCreditControlRequest first,
        DiameterCreditControlRequest second)
    {
        return first.SessionId == second.SessionId &&
            AreCreditControlIdentitiesEquivalent(
                first.DestinationRealm,
                second.DestinationRealm) &&
            AreOptionalCreditControlIdentitiesEquivalent(
                first.DestinationHost,
                second.DestinationHost) &&
            first.ServiceContextId == second.ServiceContextId &&
            first.UserName == second.UserName &&
            first.RequestType == second.RequestType &&
            first.RequestNumber == second.RequestNumber &&
            first.CreditControlSubSessionId ==
                second.CreditControlSubSessionId &&
            first.CreditControlSubSessionIdPresent ==
                second.CreditControlSubSessionIdPresent &&
            first.AccountingMultiSessionId ==
                second.AccountingMultiSessionId &&
            first.EventTimestamp == second.EventTimestamp &&
            first.EventTimestampPresent == second.EventTimestampPresent &&
            first.OriginStateIdentifier == second.OriginStateIdentifier &&
            first.OriginStateIdentifierPresent ==
                second.OriginStateIdentifierPresent &&
            AreCreditControlUnsigned32ListsEquivalent(
                first.ServiceIdentifiers,
                second.ServiceIdentifiers) &&
            first.TerminationCause == second.TerminationCause &&
            first.TerminationCausePresent ==
                second.TerminationCausePresent &&
            AreOptionalCreditControlUnitsEquivalent(
                first.RequestedServiceUnit,
                second.RequestedServiceUnit) &&
            AreOptionalCreditControlUnitsEquivalent(
                first.QuotaThreshold,
                second.QuotaThreshold) &&
            first.RequireFinalUsageReport ==
                second.RequireFinalUsageReport &&
            first.RequestedAction == second.RequestedAction &&
            first.RequestedActionPresent == second.RequestedActionPresent &&
            AreCreditControlUnitListsEquivalent(
                first.UsedServiceUnits,
                second.UsedServiceUnits) &&
            first.MultipleServicesIndicator ==
                second.MultipleServicesIndicator &&
            first.MultipleServicesIndicatorPresent ==
                second.MultipleServicesIndicatorPresent &&
            AreCreditControlMultipleCreditListsEquivalent(
                first.MultipleServiceCredits,
                second.MultipleServiceCredits) &&
            AreCreditControlAvpListsEquivalent(
                first.ServiceParameterAvps,
                second.ServiceParameterAvps) &&
            AreCreditControlBytesEquivalent(
                first.CorrelationId,
                second.CorrelationId) &&
            AreCreditControlSubscriptionListsEquivalent(
                first.SubscriptionIdentifiers,
                second.SubscriptionIdentifiers) &&
            AreOptionalCreditControlEquipmentEquivalent(
                first.UserEquipmentInformation,
                second.UserEquipmentInformation) &&
            AreCreditControlAvpListsEquivalent(
                first.ServiceAvps,
                second.ServiceAvps);
    }

    internal static bool AreCreditControlIdentitiesEquivalent(
        DiameterIdentity first,
        DiameterIdentity second)
    {
        return first.Value == second.Value;
    }

    internal static bool AreOptionalCreditControlIdentitiesEquivalent(
        DiameterIdentity? first,
        DiameterIdentity? second)
    {
        if (first == null || second == null)
        {
            return first == null && second == null;
        }

        return AreCreditControlIdentitiesEquivalent(first, second);
    }

    internal static bool AreCreditControlUnsigned32ListsEquivalent(
        List<uint> first,
        List<uint> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        int index = 0;
        while (index < first.Count)
        {
            if (first[index] != second[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool AreOptionalCreditControlUnitsEquivalent(
        DiameterServiceUnit? first,
        DiameterServiceUnit? second)
    {
        if (first == null || second == null)
        {
            return first == null && second == null;
        }

        return first.Time == second.Time &&
            first.TimePresent == second.TimePresent &&
            AreOptionalCreditControlCostsEquivalent(
                first.Money,
                second.Money) &&
            first.TotalOctets == second.TotalOctets &&
            first.TotalOctetsPresent == second.TotalOctetsPresent &&
            first.InputOctets == second.InputOctets &&
            first.InputOctetsPresent == second.InputOctetsPresent &&
            first.OutputOctets == second.OutputOctets &&
            first.OutputOctetsPresent == second.OutputOctetsPresent &&
            first.ServiceSpecificUnits == second.ServiceSpecificUnits &&
            first.ServiceSpecificUnitsPresent ==
                second.ServiceSpecificUnitsPresent &&
            first.TariffChangeTime == second.TariffChangeTime &&
            first.TariffChangeTimePresent ==
                second.TariffChangeTimePresent &&
            first.TariffChangeUsage == second.TariffChangeUsage &&
            first.TariffChangeUsagePresent ==
                second.TariffChangeUsagePresent;
    }

    internal static bool AreCreditControlUnitListsEquivalent(
        List<DiameterServiceUnit> first,
        List<DiameterServiceUnit> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        int index = 0;
        while (index < first.Count)
        {
            if (!AreOptionalCreditControlUnitsEquivalent(
                first[index],
                second[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool AreOptionalCreditControlCostsEquivalent(
        DiameterCostInformation? first,
        DiameterCostInformation? second)
    {
        if (first == null || second == null)
        {
            return first == null && second == null;
        }

        return first.ValueDigits == second.ValueDigits &&
            first.Exponent == second.Exponent &&
            first.ExponentPresent == second.ExponentPresent &&
            first.CurrencyCode == second.CurrencyCode &&
            first.CurrencyCodePresent == second.CurrencyCodePresent &&
            first.CostUnit == second.CostUnit;
    }

    internal static bool AreCreditControlMultipleCreditListsEquivalent(
        List<DiameterMultipleServiceCredit> first,
        List<DiameterMultipleServiceCredit> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        int index = 0;
        while (index < first.Count)
        {
            if (!AreCreditControlMultipleCreditsEquivalent(
                first[index],
                second[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool AreCreditControlMultipleCreditsEquivalent(
        DiameterMultipleServiceCredit first,
        DiameterMultipleServiceCredit second)
    {
        return AreOptionalCreditControlUnitsEquivalent(
                first.QuotaThreshold,
                second.QuotaThreshold) &&
            AreOptionalCreditControlUnitsEquivalent(
                first.RequestedUnit,
                second.RequestedUnit) &&
            AreOptionalCreditControlUnitsEquivalent(
                first.GrantedUnit,
                second.GrantedUnit) &&
            AreCreditControlUnitListsEquivalent(
                first.UsedUnits,
                second.UsedUnits) &&
            AreCreditControlUnsigned32ListsEquivalent(
                first.ServiceIdentifiers,
                second.ServiceIdentifiers) &&
            first.RatingGroup == second.RatingGroup &&
            first.RatingGroupPresent == second.RatingGroupPresent &&
            AreCreditControlPoolListsEquivalent(
                first.PoolReferences,
                second.PoolReferences) &&
            first.TariffChangeUsage == second.TariffChangeUsage &&
            first.TariffChangeUsagePresent ==
                second.TariffChangeUsagePresent &&
            first.ValidityTimeMilliseconds ==
                second.ValidityTimeMilliseconds &&
            first.ValidityTimePresent == second.ValidityTimePresent &&
            first.ResultCode == second.ResultCode &&
            first.ResultCodePresent == second.ResultCodePresent &&
            AreOptionalCreditControlFinalUnitsEquivalent(
                first.FinalUnitInstruction,
                second.FinalUnitInstruction) &&
            AreOptionalCreditControlFinalUnitsEquivalent(
                first.QosFinalUnitInstruction,
                second.QosFinalUnitInstruction) &&
            first.Disposition == second.Disposition;
    }

    internal static bool AreCreditControlPoolListsEquivalent(
        List<DiameterPoolReference> first,
        List<DiameterPoolReference> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        int index = 0;
        while (index < first.Count)
        {
            DiameterPoolReference firstItem = first[index];
            DiameterPoolReference secondItem = second[index];
            if (firstItem.PoolIdentifier != secondItem.PoolIdentifier ||
                firstItem.UnitType != secondItem.UnitType ||
                firstItem.ValueDigits != secondItem.ValueDigits ||
                firstItem.Exponent != secondItem.Exponent ||
                firstItem.ExponentPresent != secondItem.ExponentPresent)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool AreOptionalCreditControlFinalUnitsEquivalent(
        DiameterFinalUnitInstruction? first,
        DiameterFinalUnitInstruction? second)
    {
        if (first == null || second == null)
        {
            return first == null && second == null;
        }

        return first.Action == second.Action &&
            AreCreditControlTextListsEquivalent(
                first.RestrictionFilterRules,
                second.RestrictionFilterRules) &&
            AreCreditControlTextListsEquivalent(
                first.FilterIdentifiers,
                second.FilterIdentifiers) &&
            first.RedirectAddressType == second.RedirectAddressType &&
            first.RedirectAddressTypePresent ==
                second.RedirectAddressTypePresent &&
            first.RedirectAddress == second.RedirectAddress &&
            first.RedirectExtension == second.RedirectExtension &&
            first.Qos == second.Qos;
    }

    internal static bool AreCreditControlTextListsEquivalent(
        List<string> first,
        List<string> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        int index = 0;
        while (index < first.Count)
        {
            if (first[index] != second[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool AreCreditControlAvpListsEquivalent(
        List<DiameterAvp> first,
        List<DiameterAvp> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        int index = 0;
        while (index < first.Count)
        {
            DiameterAvp firstAvp = first[index];
            DiameterAvp secondAvp = second[index];
            if (firstAvp.Code != secondAvp.Code ||
                firstAvp.Flags != secondAvp.Flags ||
                firstAvp.VendorId != secondAvp.VendorId ||
                !AreCreditControlBytesEquivalent(
                    firstAvp.Data,
                    secondAvp.Data))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool AreCreditControlBytesEquivalent(
        byte[] first,
        byte[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }

        int index = 0;
        while (index < first.Length)
        {
            if (first[index] != second[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool AreCreditControlSubscriptionListsEquivalent(
        List<DiameterSubscriptionIdentifier> first,
        List<DiameterSubscriptionIdentifier> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        int index = 0;
        while (index < first.Count)
        {
            DiameterSubscriptionIdentifier firstItem = first[index];
            DiameterSubscriptionIdentifier secondItem = second[index];
            if (firstItem.Type != secondItem.Type ||
                firstItem.Data != secondItem.Data ||
                firstItem.Extension != secondItem.Extension)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool AreOptionalCreditControlEquipmentEquivalent(
        DiameterUserEquipmentInformation? first,
        DiameterUserEquipmentInformation? second)
    {
        if (first == null || second == null)
        {
            return first == null && second == null;
        }

        return first.Type == second.Type &&
            AreCreditControlBytesEquivalent(first.Value, second.Value) &&
            first.Extension == second.Extension &&
            first.Imei == second.Imei;
    }
}
