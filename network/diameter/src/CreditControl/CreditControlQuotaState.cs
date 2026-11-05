using System;

namespace diameter;

internal static class CreditControlQuotaState
{
    internal static void ApplyCreditControlGrantPolicy(
        DiameterCreditControlSession session,
        DiameterCreditControlAnswer answer,
        DateTimeOffset now)
    {
        session.ValidityDeadlinePresent = false;
        session.TariffChangeTimePresent = false;
        session.NextInterrogationTimePresent = false;
        session.QuotaThresholdReached = false;
        session.QuotaExhausted = false;
        if (answer.ValidityTimePresent)
        {
            session.ValidityDeadline = now.AddMilliseconds(
                answer.ValidityTimeMilliseconds);
            session.ValidityDeadlinePresent = true;
            ScheduleCreditControlInterrogation(
                session,
                session.ValidityDeadline);
        }

        if (answer.GrantedServiceUnit != null)
        {
            ApplyCreditControlTariffSchedule(
                session,
                answer.GrantedServiceUnit);
        }

        int answerIndex = 0;
        while (answerIndex < answer.MultipleServiceCredits.Count)
        {
            DiameterMultipleServiceCredit incoming =
                answer.MultipleServiceCredits[answerIndex];
            int stateIndex = CreditControlMultipleServiceState
                .FindCreditControlMultipleServiceState(
                    session.MultipleServiceCredits,
                    incoming);
            if (stateIndex < 0)
            {
                throw new DiameterCreditControlException(
                    "The granted service group has no retained state.");
            }

            DiameterMultipleServiceCredit state =
                session.MultipleServiceCredits[stateIndex];
            if (incoming.ValidityTimePresent)
            {
                state.ValidityDeadline = now.AddMilliseconds(
                    incoming.ValidityTimeMilliseconds);
                state.ValidityDeadlinePresent = true;
            }
            else
            {
                state.ValidityDeadlinePresent = false;
            }

            answerIndex = answerIndex + 1;
        }

        int stateCreditIndex = 0;
        while (stateCreditIndex < session.MultipleServiceCredits.Count)
        {
            DiameterMultipleServiceCredit credit =
                session.MultipleServiceCredits[stateCreditIndex];
            if (credit.GrantedUnit != null)
            {
                ApplyCreditControlTariffSchedule(
                    session,
                    credit.GrantedUnit);
            }

            if (credit.ValidityDeadlinePresent)
            {
                ScheduleCreditControlInterrogation(
                    session,
                    credit.ValidityDeadline);
            }

            stateCreditIndex = stateCreditIndex + 1;
        }
    }

    internal static void ApplyCreditControlUsagePolicy(
        DiameterCreditControlSession session)
    {
        session.QuotaThresholdReached =
            IsCreditControlThresholdReached(
                session.GrantedServiceUnit,
                session.UsedServiceUnit,
                session.QuotaThreshold);
        session.QuotaExhausted = IsCreditControlQuotaExhausted(
            session.GrantedServiceUnit,
            session.UsedServiceUnit);
        int index = 0;
        while (index < session.MultipleServiceCredits.Count)
        {
            DiameterMultipleServiceCredit credit =
                session.MultipleServiceCredits[index];
            DiameterServiceUnit? used = null;
            if (credit.UsedUnits.Count > 0)
            {
                used = credit.UsedUnits[0];
            }

            if (IsCreditControlThresholdReached(
                credit.GrantedUnit,
                used,
                credit.QuotaThreshold))
            {
                session.QuotaThresholdReached = true;
            }

            if (IsCreditControlQuotaExhausted(
                credit.GrantedUnit,
                used))
            {
                session.QuotaExhausted = true;
            }

            index = index + 1;
        }
    }

    internal static void ValidateCreditControlFinalUsageReport(
        DiameterCreditControlSession session,
        DiameterCreditControlRequest request)
    {
        if (!session.FinalUsageReportRequired)
        {
            return;
        }

        if (session.GrantedServiceUnit != null &&
            request.UsedServiceUnits.Count == 0)
        {
            throw new DiameterCreditControlException(
                "Termination requires final Used-Service-Unit data.");
        }

        int index = 0;
        while (index < session.MultipleServiceCredits.Count)
        {
            DiameterMultipleServiceCredit state =
                session.MultipleServiceCredits[index];
            if (state.GrantedUnit != null)
            {
                int requestIndex = CreditControlMultipleServiceState
                    .FindCreditControlMultipleServiceState(
                        request.MultipleServiceCredits,
                        state);
                if (requestIndex < 0 || request.MultipleServiceCredits[
                    requestIndex].UsedUnits.Count == 0)
                {
                    throw new DiameterCreditControlException(
                        "Termination requires final usage for every granted service group.");
                }
            }

            index = index + 1;
        }
    }

    internal static bool IsCreditControlQuotaExhausted(
        DiameterServiceUnit? granted,
        DiameterServiceUnit? used)
    {
        if (granted == null || used == null)
        {
            return false;
        }

        return granted.TimePresent && used.TimePresent &&
            used.Time >= granted.Time ||
            CreditControlUnitArithmetic
                .IsCreditControlMoneyQuotaExhausted(
                    granted.Money,
                    used.Money) ||
            granted.TotalOctetsPresent && used.TotalOctetsPresent &&
            used.TotalOctets >= granted.TotalOctets ||
            granted.InputOctetsPresent && used.InputOctetsPresent &&
            used.InputOctets >= granted.InputOctets ||
            granted.OutputOctetsPresent && used.OutputOctetsPresent &&
            used.OutputOctets >= granted.OutputOctets ||
            granted.ServiceSpecificUnitsPresent &&
            used.ServiceSpecificUnitsPresent &&
            used.ServiceSpecificUnits >= granted.ServiceSpecificUnits;
    }

    internal static bool IsCreditControlThresholdReached(
        DiameterServiceUnit? granted,
        DiameterServiceUnit? used,
        DiameterServiceUnit? threshold)
    {
        if (granted == null || used == null || threshold == null)
        {
            return false;
        }

        return threshold.TimePresent && granted.TimePresent &&
            used.TimePresent &&
            (used.Time >= granted.Time ||
            granted.Time - used.Time <= threshold.Time) ||
            threshold.TotalOctetsPresent && granted.TotalOctetsPresent &&
            used.TotalOctetsPresent &&
            (used.TotalOctets >= granted.TotalOctets ||
            granted.TotalOctets - used.TotalOctets <=
                threshold.TotalOctets) ||
            threshold.InputOctetsPresent && granted.InputOctetsPresent &&
            used.InputOctetsPresent &&
            (used.InputOctets >= granted.InputOctets ||
            granted.InputOctets - used.InputOctets <=
                threshold.InputOctets) ||
            threshold.OutputOctetsPresent && granted.OutputOctetsPresent &&
            used.OutputOctetsPresent &&
            (used.OutputOctets >= granted.OutputOctets ||
            granted.OutputOctets - used.OutputOctets <=
                threshold.OutputOctets) ||
            threshold.ServiceSpecificUnitsPresent &&
            granted.ServiceSpecificUnitsPresent &&
            used.ServiceSpecificUnitsPresent &&
            (used.ServiceSpecificUnits >= granted.ServiceSpecificUnits ||
            granted.ServiceSpecificUnits - used.ServiceSpecificUnits <=
                threshold.ServiceSpecificUnits);
    }

    internal static void ApplyCreditControlTariffSchedule(
        DiameterCreditControlSession session,
        DiameterServiceUnit unit)
    {
        if (!unit.TariffChangeTimePresent)
        {
            return;
        }

        if (!session.TariffChangeTimePresent ||
            unit.TariffChangeTime < session.TariffChangeTime)
        {
            session.TariffChangeTime = unit.TariffChangeTime;
            session.TariffChangeTimePresent = true;
        }

        ScheduleCreditControlInterrogation(
            session,
            unit.TariffChangeTime);
    }

    internal static void ScheduleCreditControlInterrogation(
        DiameterCreditControlSession session,
        DateTimeOffset scheduledTime)
    {
        if (!session.NextInterrogationTimePresent ||
            scheduledTime < session.NextInterrogationTime)
        {
            session.NextInterrogationTime = scheduledTime;
            session.NextInterrogationTimePresent = true;
        }
    }

    internal static void AdvanceCreditControlInterrogationSchedule(
        DiameterCreditControlSession session,
        DateTimeOffset now)
    {
        session.NextInterrogationTimePresent = false;
        if (session.ValidityDeadlinePresent &&
            session.ValidityDeadline <= now)
        {
            session.ValidityDeadlinePresent = false;
        }

        if (session.ValidityDeadlinePresent)
        {
            ScheduleCreditControlInterrogation(
                session,
                session.ValidityDeadline);
        }

        if (session.TariffChangeTimePresent &&
            session.TariffChangeTime <= now)
        {
            session.TariffChangeTimePresent = false;
        }

        if (session.TariffChangeTimePresent)
        {
            ScheduleCreditControlInterrogation(
                session,
                session.TariffChangeTime);
        }

        int index = 0;
        while (index < session.MultipleServiceCredits.Count)
        {
            DiameterMultipleServiceCredit credit =
                session.MultipleServiceCredits[index];
            if (credit.ValidityDeadlinePresent &&
                credit.ValidityDeadline <= now)
            {
                credit.ValidityDeadlinePresent = false;
            }

            if (credit.ValidityDeadlinePresent)
            {
                ScheduleCreditControlInterrogation(
                    session,
                    credit.ValidityDeadline);
            }

            index = index + 1;
        }
    }
}
