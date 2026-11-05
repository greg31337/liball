using System;
using System.Collections.Generic;

namespace diameter;

internal static class CreditControlMultipleServiceState
{
    internal static void ApplyCreditControlMultipleServiceRequest(
        DiameterCreditControlSession session,
        DiameterCreditControlRequest request)
    {
        ValidateCreditControlMultipleServiceRequestSupport(
            session,
            request);
        if (request.MultipleServicesIndicatorPresent)
        {
            session.MultipleServicesIndicator =
                request.MultipleServicesIndicator;
            session.MultipleServicesIndicatorPresent = true;
        }

        int index = 0;
        while (index < request.MultipleServiceCredits.Count)
        {
            DiameterMultipleServiceCredit incoming =
                request.MultipleServiceCredits[index];
            int stateIndex = FindCreditControlMultipleServiceState(
                session.MultipleServiceCredits,
                incoming);
            DiameterMultipleServiceCredit state;
            if (stateIndex < 0)
            {
                state = CreateCreditControlMultipleServiceState(incoming);
                session.MultipleServiceCredits.Add(state);
            }
            else
            {
                state = session.MultipleServiceCredits[stateIndex];
            }

            state.RequestedUnit = CreditControlDataCloner
                .CloneOptionalCreditControlServiceUnit(
                    incoming.RequestedUnit);
            if (incoming.QuotaThreshold != null)
            {
                state.QuotaThreshold = CreditControlDataCloner
                    .CloneOptionalCreditControlServiceUnit(
                        incoming.QuotaThreshold);
            }

            AccumulateCreditControlMultipleServiceUsage(state, incoming);
            index = index + 1;
        }
    }

    internal static void ValidateCreditControlMultipleServiceRequestSupport(
        DiameterCreditControlSession? session,
        DiameterCreditControlRequest request)
    {
        ValidateCreditControlMultipleServiceKeys(
            request.MultipleServiceCredits);
        bool supported = request.MultipleServicesIndicatorPresent &&
            request.MultipleServicesIndicator ==
                DiameterMultipleServicesIndicator.Supported;
        if (!request.MultipleServicesIndicatorPresent && session != null)
        {
            supported = session.MultipleServicesIndicatorPresent &&
                session.MultipleServicesIndicator ==
                    DiameterMultipleServicesIndicator.Supported;
        }

        if (request.MultipleServiceCredits.Count > 0 && !supported)
        {
            throw new DiameterCreditControlException(
                "Multiple-service credit requires the supported indicator.");
        }
    }

    internal static DiameterCreditControlDisposition
        ApplyCreditControlMultipleServiceAnswer(
            DiameterCreditControlSession session,
            List<DiameterMultipleServiceCredit> answers,
            DiameterCreditControlDisposition currentDisposition)
    {
        ValidateCreditControlMultipleServiceKeys(answers);
        DiameterCreditControlDisposition disposition = currentDisposition;
        int index = 0;
        while (index < answers.Count)
        {
            DiameterMultipleServiceCredit incoming = answers[index];
            int stateIndex = FindCreditControlMultipleServiceState(
                session.MultipleServiceCredits,
                incoming);
            DiameterMultipleServiceCredit state;
            if (stateIndex < 0)
            {
                state = CreateCreditControlMultipleServiceState(incoming);
                session.MultipleServiceCredits.Add(state);
            }
            else
            {
                state = session.MultipleServiceCredits[stateIndex];
            }

            state.GrantedUnit = CreditControlDataCloner
                .CloneOptionalCreditControlServiceUnit(
                    incoming.GrantedUnit);
            state.PoolReferences = CreditControlDataCloner
                .CloneCreditControlPoolReferences(
                    incoming.PoolReferences);
            state.ResultCode = incoming.ResultCode;
            state.ResultCodePresent = incoming.ResultCodePresent;
            state.TariffChangeUsage = incoming.TariffChangeUsage;
            state.TariffChangeUsagePresent =
                incoming.TariffChangeUsagePresent;
            state.ValidityTimeMilliseconds =
                incoming.ValidityTimeMilliseconds;
            state.ValidityTimePresent = incoming.ValidityTimePresent;
            state.FinalUnitInstruction = CreditControlDataCloner
                .CloneOptionalCreditControlFinalUnit(
                    incoming.FinalUnitInstruction);
            state.QosFinalUnitInstruction = CreditControlDataCloner
                .CloneOptionalCreditControlFinalUnit(
                    incoming.QosFinalUnitInstruction);
            state.Disposition = CreditControlUnitArithmetic
                .ReadCreditControlGrantDisposition(
                    state.RequestedUnit,
                    state.GrantedUnit,
                    incoming.Disposition);
            if (state.ResultCodePresent &&
                (state.ResultCode < 2000U || state.ResultCode >= 3000U))
            {
                state.Disposition =
                    DiameterCreditControlDisposition.Failed;
            }

            if ((state.FinalUnitInstruction != null ||
                state.QosFinalUnitInstruction != null) &&
                state.Disposition !=
                    DiameterCreditControlDisposition.Denied &&
                state.Disposition !=
                    DiameterCreditControlDisposition.Failed)
            {
                state.Disposition =
                    DiameterCreditControlDisposition.FinalUnit;
            }

            incoming.Disposition = state.Disposition;

            disposition = SelectCreditControlMultipleServiceDisposition(
                disposition,
                state.Disposition);
            index = index + 1;
        }

        RebuildCreditControlPoolState(session);
        return disposition;
    }

    internal static void ValidateCreditControlMultipleServiceKeys(
        List<DiameterMultipleServiceCredit> credits)
    {
        int index = 0;
        while (index < credits.Count)
        {
            int otherIndex = index + 1;
            while (otherIndex < credits.Count)
            {
                if (AreCreditControlMultipleServiceKeysEqual(
                    credits[index],
                    credits[otherIndex]))
                {
                    throw new DiameterCreditControlException(
                        "Multiple-service credit keys must be unique within an interrogation.");
                }

                otherIndex = otherIndex + 1;
            }

            index = index + 1;
        }
    }

    internal static int FindCreditControlMultipleServiceState(
        List<DiameterMultipleServiceCredit> states,
        DiameterMultipleServiceCredit key)
    {
        int index = 0;
        while (index < states.Count)
        {
            if (AreCreditControlMultipleServiceKeysEqual(
                states[index],
                key))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static bool AreCreditControlMultipleServiceKeysEqual(
        DiameterMultipleServiceCredit first,
        DiameterMultipleServiceCredit second)
    {
        return first.RatingGroupPresent == second.RatingGroupPresent &&
            (!first.RatingGroupPresent ||
            first.RatingGroup == second.RatingGroup) &&
            CreditControlRequestComparer
                .AreCreditControlUnsigned32ListsEquivalent(
                    first.ServiceIdentifiers,
                    second.ServiceIdentifiers);
    }

    internal static DiameterMultipleServiceCredit
        CreateCreditControlMultipleServiceState(
            DiameterMultipleServiceCredit source)
    {
        DiameterMultipleServiceCredit result =
            new DiameterMultipleServiceCredit();
        result.ServiceIdentifiers = CreditControlDataCloner
            .CloneCreditControlUnsigned32Values(
                source.ServiceIdentifiers);
        result.RatingGroup = source.RatingGroup;
        result.RatingGroupPresent = source.RatingGroupPresent;
        return result;
    }

    internal static void AccumulateCreditControlMultipleServiceUsage(
        DiameterMultipleServiceCredit state,
        DiameterMultipleServiceCredit incoming)
    {
        DiameterServiceUnit? cumulative = null;
        if (state.UsedUnits.Count > 0)
        {
            cumulative = state.UsedUnits[0];
        }

        int index = 0;
        while (index < incoming.UsedUnits.Count)
        {
            cumulative = CreditControlUnitArithmetic
                .AddCreditControlServiceUnits(
                    cumulative,
                    incoming.UsedUnits[index]);
            index = index + 1;
        }

        if (cumulative != null)
        {
            state.UsedUnits.Clear();
            state.UsedUnits.Add(cumulative);
        }
    }

    internal static DiameterCreditControlDisposition
        SelectCreditControlMultipleServiceDisposition(
            DiameterCreditControlDisposition current,
            DiameterCreditControlDisposition candidate)
    {
        if (candidate == DiameterCreditControlDisposition.Failed ||
            current == DiameterCreditControlDisposition.Failed)
        {
            return DiameterCreditControlDisposition.Failed;
        }

        if (candidate == DiameterCreditControlDisposition.Denied ||
            current == DiameterCreditControlDisposition.Denied)
        {
            return DiameterCreditControlDisposition.Denied;
        }

        if (candidate == DiameterCreditControlDisposition.FinalUnit ||
            current == DiameterCreditControlDisposition.FinalUnit)
        {
            return DiameterCreditControlDisposition.FinalUnit;
        }

        if (candidate ==
                DiameterCreditControlDisposition.PartiallyGranted ||
            current == DiameterCreditControlDisposition.PartiallyGranted)
        {
            return DiameterCreditControlDisposition.PartiallyGranted;
        }

        return DiameterCreditControlDisposition.Granted;
    }

    internal static void RebuildCreditControlPoolState(
        DiameterCreditControlSession session)
    {
        List<DiameterPoolReference> pools =
            new List<DiameterPoolReference>();
        int creditIndex = 0;
        while (creditIndex < session.MultipleServiceCredits.Count)
        {
            DiameterMultipleServiceCredit credit =
                session.MultipleServiceCredits[creditIndex];
            int poolIndex = 0;
            while (poolIndex < credit.PoolReferences.Count)
            {
                AddCreditControlPoolState(
                    pools,
                    credit.PoolReferences[poolIndex]);
                poolIndex = poolIndex + 1;
            }

            creditIndex = creditIndex + 1;
        }

        session.PoolReferences = pools;
    }

    internal static void AddCreditControlPoolState(
        List<DiameterPoolReference> pools,
        DiameterPoolReference incoming)
    {
        int index = 0;
        while (index < pools.Count)
        {
            DiameterPoolReference current = pools[index];
            if (current.PoolIdentifier == incoming.PoolIdentifier &&
                current.UnitType == incoming.UnitType)
            {
                int currentExponent = current.ExponentPresent ?
                    current.Exponent : 0;
                int incomingExponent = incoming.ExponentPresent ?
                    incoming.Exponent : 0;
                if (!CreditControlUnitArithmetic
                    .AreCreditControlUnitValuesEqual(
                        current.ValueDigits,
                        currentExponent,
                        incoming.ValueDigits,
                        incomingExponent))
                {
                    throw new DiameterCreditControlException(
                        "A pooled unit has conflicting conversion values.");
                }

                return;
            }

            index = index + 1;
        }

        List<DiameterPoolReference> one =
            new List<DiameterPoolReference>();
        one.Add(incoming);
        pools.Add(CreditControlDataCloner
            .CloneCreditControlPoolReferences(one)[0]);
    }
}
