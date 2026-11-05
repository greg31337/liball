namespace diameter;

internal static class CreditControlFinalUnitState
{
    internal static void ApplyCreditControlFinalUnitGrantPolicy(
        DiameterCreditControlSession session)
    {
        session.AppliedFinalUnitInstruction = null;
        session.GracefulTerminationRequired = false;
        if (HasCreditControlFinalUnitInstruction(session) &&
            session.Disposition != DiameterCreditControlDisposition.Denied &&
            session.Disposition != DiameterCreditControlDisposition.Failed)
        {
            session.Disposition =
                DiameterCreditControlDisposition.FinalUnit;
        }
    }

    internal static void ApplyCreditControlFinalUnitUsagePolicy(
        DiameterCreditControlSession session)
    {
        DiameterFinalUnitInstruction? applied = null;
        if (CreditControlQuotaState.IsCreditControlQuotaExhausted(
            session.GrantedServiceUnit,
            session.UsedServiceUnit))
        {
            applied = SelectCreditControlFinalUnitInstruction(
                applied,
                session.FinalUnitInstruction);
            applied = SelectCreditControlFinalUnitInstruction(
                applied,
                session.QosFinalUnitInstruction);
        }

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

            if (CreditControlQuotaState.IsCreditControlQuotaExhausted(
                credit.GrantedUnit,
                used))
            {
                applied = SelectCreditControlFinalUnitInstruction(
                    applied,
                    credit.FinalUnitInstruction);
                applied = SelectCreditControlFinalUnitInstruction(
                    applied,
                    credit.QosFinalUnitInstruction);
            }

            index = index + 1;
        }

        session.AppliedFinalUnitInstruction =
            CreditControlDataCloner.CloneOptionalCreditControlFinalUnit(
                applied);
        session.GracefulTerminationRequired = applied != null &&
            applied.Action == DiameterFinalUnitAction.Terminate;
    }

    internal static bool HasCreditControlFinalUnitInstruction(
        DiameterCreditControlSession session)
    {
        if (session.FinalUnitInstruction != null ||
            session.QosFinalUnitInstruction != null)
        {
            return true;
        }

        int index = 0;
        while (index < session.MultipleServiceCredits.Count)
        {
            if (session.MultipleServiceCredits[index]
                    .FinalUnitInstruction != null ||
                session.MultipleServiceCredits[index]
                    .QosFinalUnitInstruction != null)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static DiameterFinalUnitInstruction?
        SelectCreditControlFinalUnitInstruction(
            DiameterFinalUnitInstruction? current,
            DiameterFinalUnitInstruction? candidate)
    {
        if (candidate == null)
        {
            return current;
        }

        if (current == null ||
            candidate.Action == DiameterFinalUnitAction.Terminate &&
            current.Action != DiameterFinalUnitAction.Terminate)
        {
            return candidate;
        }

        return current;
    }

    internal static void PopulateCreditControlFinalUnitResult(
        DiameterCreditControlResult result,
        DiameterCreditControlSession session)
    {
        result.FinalUnit = HasCreditControlFinalUnitInstruction(session);
        result.AppliedFinalUnitInstruction = CreditControlDataCloner
            .CloneOptionalCreditControlFinalUnit(
                session.AppliedFinalUnitInstruction);
        result.GracefulTerminationRequired =
            session.GracefulTerminationRequired;
        if (session.GracefulTerminationRequired)
        {
            result.ContinueService = false;
            result.TerminateService = true;
        }
        else if (session.AppliedFinalUnitInstruction != null)
        {
            result.ContinueService =
                session.State != DiameterCreditControlState.Closed &&
                session.State != DiameterCreditControlState.Failed;
            result.TerminateService = false;
        }
    }
}
