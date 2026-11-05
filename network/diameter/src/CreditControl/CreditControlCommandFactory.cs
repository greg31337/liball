using System;
using System.Collections.Generic;
using System.Net;

namespace diameter;

internal class CreditControlCommandFactory
{
    internal DiameterBaseCommandFactory BaseFactory;
    internal DiameterBaseAvpFactory AvpFactory =
        new DiameterBaseAvpFactory();
    internal DiameterIdentity OriginHost = new DiameterIdentity();
    internal DiameterIdentity OriginRealm = new DiameterIdentity();
    internal uint OriginStateIdentifier;

    internal CreditControlCommandFactory(
        IDiameterIdentifierSource? identifierSource,
        DiameterIdentity? originHost,
        DiameterIdentity? originRealm)
    {
        DiameterValueResult normalizedHost =
            ValueCodecs.NormalizeDiameterIdentity(originHost);
        DiameterValueResult normalizedRealm =
            ValueCodecs.NormalizeDiameterRealm(originRealm);
        if (identifierSource == null ||
            normalizedHost.Status != DiameterCodecStatus.Success ||
            normalizedRealm.Status != DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "Credit-control command creation requires identifiers and local origin identities.");
        }

        BaseFactory = new DiameterBaseCommandFactory(identifierSource);
        OriginHost.Value = normalizedHost.Text;
        OriginRealm.Value = normalizedRealm.Text;
        OriginStateIdentifier =
            identifierSource.CreateOriginStateIdentifier();
    }

    internal DiameterMessage CreateCreditControlRequest(
        DiameterCreditControlRequest? request)
    {
        if (request == null)
        {
            throw new ArgumentException(
                "A credit-control request is required.");
        }

        request.ValidateCreditControlRequest();
        ValidateCreditControlRequestNumber(request);
        DiameterMessage result = BaseFactory.CreateRequestMessage(
            DiameterApplicationIds.CreditControl,
            DiameterCommandCodes.CreditControl,
            true);
        result.Avps.Add(AvpFactory.CreateSessionIdAvp(
            request.SessionId));
        result.Avps.Add(AvpFactory.CreateOriginHostAvp(OriginHost));
        result.Avps.Add(AvpFactory.CreateOriginRealmAvp(OriginRealm));
        result.Avps.Add(AvpFactory.CreateDestinationRealmAvp(
            request.DestinationRealm));
        result.Avps.Add(AvpFactory.CreateAuthApplicationIdAvp(
            DiameterApplicationIds.CreditControl));
        result.Avps.Add(AvpFactory.CreateTextAvp(
            DiameterAvpCodes.ServiceContextId,
            request.ServiceContextId));
        result.Avps.Add(BaseFactory.CreateEnumeratedAvp(
            DiameterAvpCodes.CcRequestType,
            (int)request.RequestType));
        result.Avps.Add(BaseFactory.CreateUnsigned32Avp(
            DiameterAvpCodes.CcRequestNumber,
            request.RequestNumber));
        AppendCreditControlRequestOptionals(result, request);
        AppendCreditControlRequestExtensions(result, request);
        return result;
    }

    internal DiameterMessage CreateCreditControlAnswer(
        DiameterMessage? request,
        DiameterCreditControlAnswer? answer)
    {
        if (request == null || answer == null)
        {
            throw new ArgumentException(
                "A source request and credit-control answer are required.");
        }

        answer.ValidateCreditControlAnswer();
        CreditControlCommandParser parser =
            new CreditControlCommandParser(new DiameterSystemClock());
        DiameterCreditControlRequest parsed =
            parser.ParseCreditControlRequest(request);
        if (answer.SessionId != parsed.SessionId ||
            answer.RequestType != parsed.RequestType ||
            answer.RequestNumber != parsed.RequestNumber)
        {
            throw new DiameterCreditControlException(
                "The credit-control answer does not correlate to its request.");
        }

        DiameterMessage result = BaseFactory.CreateAnswerMessage(
            request,
            true);
        result.Avps.Add(AvpFactory.CreateSessionIdAvp(
            answer.SessionId));
        result.Avps.Add(BaseFactory.CreateUnsigned32Avp(
            DiameterAvpCodes.ResultCode,
            answer.ResultCode));
        result.Avps.Add(AvpFactory.CreateOriginHostAvp(OriginHost));
        result.Avps.Add(AvpFactory.CreateOriginRealmAvp(OriginRealm));
        result.Avps.Add(AvpFactory.CreateAuthApplicationIdAvp(
            DiameterApplicationIds.CreditControl));
        result.Avps.Add(BaseFactory.CreateEnumeratedAvp(
            DiameterAvpCodes.CcRequestType,
            (int)answer.RequestType));
        result.Avps.Add(BaseFactory.CreateUnsigned32Avp(
            DiameterAvpCodes.CcRequestNumber,
            answer.RequestNumber));
        AppendCreditControlAnswerOptionals(result, answer);
        AppendCreditControlAnswerExtensions(result, answer);
        return result;
    }

    internal void ValidateCreditControlRequestNumber(
        DiameterCreditControlRequest request)
    {
        bool startsSequence =
            request.RequestType ==
                DiameterCreditControlRequestType.Initial ||
            request.RequestType ==
                DiameterCreditControlRequestType.Event;
        if ((startsSequence && request.RequestNumber != 0U) ||
            (!startsSequence && request.RequestNumber == 0U))
        {
            throw new DiameterCreditControlException(
                "INITIAL and EVENT use request number zero; UPDATE and TERMINATION require a later request number.");
        }
    }

    internal void AppendCreditControlRequestOptionals(
        DiameterMessage message,
        DiameterCreditControlRequest request)
    {
        CreditControlMultipleServiceState
            .ValidateCreditControlMultipleServiceKeys(
                request.MultipleServiceCredits);
        if (request.DestinationHost != null)
        {
            message.Avps.Add(AvpFactory.CreateDestinationHostAvp(
                request.DestinationHost));
        }

        if (request.UserName.Length > 0)
        {
            message.Avps.Add(AvpFactory.CreateTextAvp(
                DiameterAvpCodes.UserName,
                request.UserName));
        }

        if (request.CreditControlSubSessionIdPresent)
        {
            message.Avps.Add(CreditControlCommandCodec
                .CreateCreditControlUnsigned64Avp(
                    DiameterAvpCodes.CcSubSessionId,
                    request.CreditControlSubSessionId));
        }

        if (request.AccountingMultiSessionId.Length > 0)
        {
            message.Avps.Add(AvpFactory.CreateTextAvp(
                DiameterAvpCodes.AcctMultiSessionId,
                request.AccountingMultiSessionId));
        }

        message.Avps.Add(AvpFactory.CreateOriginStateIdAvp(
            OriginStateIdentifier));
        if (request.EventTimestampPresent)
        {
            message.Avps.Add(CreditControlCommandCodec
                .CreateCreditControlTimeAvp(
                    DiameterAvpCodes.EventTimestamp,
                    request.EventTimestamp));
        }

        int subscriptionIndex = 0;
        while (subscriptionIndex < request.SubscriptionIdentifiers.Count)
        {
            message.Avps.Add(CreditControlCommandCodec
                .CreateCreditControlSubscriptionAvp(
                    request.SubscriptionIdentifiers[subscriptionIndex]));
            subscriptionIndex = subscriptionIndex + 1;
        }

        int serviceIndex = 0;
        while (serviceIndex < request.ServiceIdentifiers.Count)
        {
            message.Avps.Add(BaseFactory.CreateUnsigned32Avp(
                DiameterAvpCodes.ServiceIdentifier,
                request.ServiceIdentifiers[serviceIndex]));
            serviceIndex = serviceIndex + 1;
        }

        if (request.TerminationCausePresent)
        {
            message.Avps.Add(BaseFactory.CreateEnumeratedAvp(
                DiameterAvpCodes.TerminationCause,
                (int)request.TerminationCause));
        }

        if (request.RequestedActionPresent)
        {
            message.Avps.Add(BaseFactory.CreateEnumeratedAvp(
                DiameterAvpCodes.RequestedAction,
                (int)request.RequestedAction));
        }

        if (request.MultipleServicesIndicatorPresent)
        {
            message.Avps.Add(BaseFactory.CreateEnumeratedAvp(
                DiameterAvpCodes.MultipleServicesIndicator,
                (int)request.MultipleServicesIndicator));
        }

        if (request.CorrelationId.Length > 0)
        {
            message.Avps.Add(AvpFactory.CreateOctetStringAvp(
                DiameterAvpCodes.CcCorrelationId,
                request.CorrelationId));
        }

        if (request.UserEquipmentInformation != null)
        {
            message.Avps.Add(CreditControlCommandCodec
                .CreateCreditControlEquipmentAvp(
                    request.UserEquipmentInformation));
        }

        if (request.RequestedServiceUnit != null)
        {
            message.Avps.Add(CreditControlCommandCodec
                .CreateCreditControlServiceUnitAvp(
                    DiameterAvpCodes.RequestedServiceUnit,
                    request.RequestedServiceUnit));
        }

        int usedIndex = 0;
        while (usedIndex < request.UsedServiceUnits.Count)
        {
            message.Avps.Add(CreditControlCommandCodec
                .CreateCreditControlServiceUnitAvp(
                    DiameterAvpCodes.UsedServiceUnit,
                    request.UsedServiceUnits[usedIndex]));
            usedIndex = usedIndex + 1;
        }

        int multipleIndex = 0;
        while (multipleIndex < request.MultipleServiceCredits.Count)
        {
            message.Avps.Add(CreditControlCommandCodec
                .CreateCreditControlMultipleServiceAvp(
                    request.MultipleServiceCredits[multipleIndex]));
            multipleIndex = multipleIndex + 1;
        }
    }

    internal void AppendCreditControlRequestExtensions(
        DiameterMessage message,
        DiameterCreditControlRequest request)
    {
        int parameterIndex = 0;
        while (parameterIndex < request.ServiceParameterAvps.Count)
        {
            message.Avps.Add(CreditControlCommandCodec
                .ValidateCreditControlServiceParameterAvp(
                    request.ServiceParameterAvps[parameterIndex]));
            parameterIndex = parameterIndex + 1;
        }

        int serviceIndex = 0;
        while (serviceIndex < request.ServiceAvps.Count)
        {
            DiameterAvp avp = request.ServiceAvps[serviceIndex];
            if (CreditControlCommandCodec
                .IsCreditControlCorrelationAvp(avp))
            {
                throw new DiameterCreditControlException(
                    "Service AVPs cannot replace credit-control correlation AVPs.");
            }

            message.Avps.Add(CreditControlCommandCodec
                .CloneCreditControlAvp(avp));
            serviceIndex = serviceIndex + 1;
        }
    }

    internal void AppendCreditControlAnswerOptionals(
        DiameterMessage message,
        DiameterCreditControlAnswer answer)
    {
        CreditControlMultipleServiceState
            .ValidateCreditControlMultipleServiceKeys(
                answer.MultipleServiceCredits);
        if (answer.UserName.Length > 0)
        {
            message.Avps.Add(AvpFactory.CreateTextAvp(
                DiameterAvpCodes.UserName,
                answer.UserName));
        }

        if (answer.SessionFailoverPresent)
        {
            message.Avps.Add(BaseFactory.CreateEnumeratedAvp(
                DiameterAvpCodes.CcSessionFailover,
                answer.SessionFailoverSupported ? 1 : 0));
        }

        if (answer.CreditControlSubSessionIdPresent)
        {
            message.Avps.Add(CreditControlCommandCodec
                .CreateCreditControlUnsigned64Avp(
                    DiameterAvpCodes.CcSubSessionId,
                    answer.CreditControlSubSessionId));
        }

        if (answer.AccountingMultiSessionId.Length > 0)
        {
            message.Avps.Add(AvpFactory.CreateTextAvp(
                DiameterAvpCodes.AcctMultiSessionId,
                answer.AccountingMultiSessionId));
        }

        message.Avps.Add(AvpFactory.CreateOriginStateIdAvp(
            OriginStateIdentifier));
        if (answer.EventTimestampPresent)
        {
            message.Avps.Add(CreditControlCommandCodec
                .CreateCreditControlTimeAvp(
                    DiameterAvpCodes.EventTimestamp,
                    answer.EventTimestamp));
        }

        if (answer.CheckBalanceResultPresent)
        {
            message.Avps.Add(BaseFactory.CreateEnumeratedAvp(
                DiameterAvpCodes.CheckBalanceResult,
                (int)answer.CheckBalanceResult));
        }

        if (answer.FailureHandlingPresent)
        {
            message.Avps.Add(BaseFactory.CreateEnumeratedAvp(
                DiameterAvpCodes.CreditControlFailureHandling,
                (int)answer.FailureHandling));
        }

        if (answer.DirectDebitFailureHandlingPresent)
        {
            message.Avps.Add(BaseFactory.CreateEnumeratedAvp(
                DiameterAvpCodes.DirectDebitingFailureHandling,
                (int)answer.DirectDebitFailureHandling));
        }

        if (answer.ValidityTimePresent)
        {
            uint seconds = (uint)(answer.ValidityTimeMilliseconds / 1000);
            message.Avps.Add(BaseFactory.CreateUnsigned32Avp(
                DiameterAvpCodes.ValidityTime,
                seconds));
        }

        int redirectIndex = 0;
        while (redirectIndex < answer.RedirectHosts.Count)
        {
            message.Avps.Add(AvpFactory.CreateRedirectHostAvp(
                answer.RedirectHosts[redirectIndex]));
            redirectIndex = redirectIndex + 1;
        }

        if (answer.RedirectHostUsagePresent)
        {
            message.Avps.Add(AvpFactory.CreateRedirectHostUsageAvp(
                answer.RedirectHostUsage));
        }

        if (answer.RedirectMaxCacheTimePresent)
        {
            uint seconds = (uint)
                (answer.RedirectMaxCacheTimeMilliseconds / 1000);
            message.Avps.Add(AvpFactory.CreateRedirectMaxCacheTimeAvp(
                seconds));
        }

        if (answer.GrantedServiceUnit != null)
        {
            message.Avps.Add(CreditControlCommandCodec
                .CreateCreditControlServiceUnitAvp(
                    DiameterAvpCodes.GrantedServiceUnit,
                answer.GrantedServiceUnit));
        }

        if (answer.CostInformation != null)
        {
            message.Avps.Add(CreditControlCommandCodec
                .CreateCreditControlCostInformationAvp(
                    answer.CostInformation));
        }

        if (answer.FinalUnitInstruction != null)
        {
            message.Avps.Add(CreditControlCommandCodec
                .CreateCreditControlFinalUnitAvp(
                    answer.FinalUnitInstruction));
        }

        if (answer.QosFinalUnitInstruction != null)
        {
            message.Avps.Add(CreditControlCommandCodec
                .CreateCreditControlFinalUnitAvp(
                    answer.QosFinalUnitInstruction));
        }

        int multipleIndex = 0;
        while (multipleIndex < answer.MultipleServiceCredits.Count)
        {
            message.Avps.Add(CreditControlCommandCodec
                .CreateCreditControlMultipleServiceAvp(
                    answer.MultipleServiceCredits[multipleIndex]));
            multipleIndex = multipleIndex + 1;
        }
    }

    internal void AppendCreditControlAnswerExtensions(
        DiameterMessage message,
        DiameterCreditControlAnswer answer)
    {
        int serviceIndex = 0;
        while (serviceIndex < answer.ServiceAvps.Count)
        {
            DiameterAvp avp = answer.ServiceAvps[serviceIndex];
            if (CreditControlCommandCodec
                .IsCreditControlCorrelationAvp(avp))
            {
                throw new DiameterCreditControlException(
                    "Service AVPs cannot replace credit-control correlation AVPs.");
            }

            message.Avps.Add(CreditControlCommandCodec
                .CloneCreditControlAvp(avp));
            serviceIndex = serviceIndex + 1;
        }

        int failedIndex = 0;
        while (failedIndex < answer.FailedAvps.Count)
        {
            message.Avps.Add(CreditControlCommandCodec
                .CloneCreditControlAvp(answer.FailedAvps[failedIndex]));
            failedIndex = failedIndex + 1;
        }
    }
}

internal static class CreditControlCommandCodec
{
    internal static DiameterAvp CreateCreditControlUnsigned64Avp(
        uint code,
        ulong value)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Flags = DiameterAvpFlags.Mandatory;
        result.Data = new byte[8];
        NetworkByteOrder.WriteUInt64(result.Data, value);
        return result;
    }

    internal static DiameterAvp CreateCreditControlTimeAvp(
        uint code,
        DateTimeOffset value)
    {
        byte[] data = new byte[4];
        DiameterValueResult encoded =
            new DiameterAvpCodec().EncodeTime(data, value);
        if (encoded.Status != DiameterCodecStatus.Success)
        {
            throw new DiameterCreditControlException(
                "The credit-control timestamp could not be encoded.");
        }

        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Flags = DiameterAvpFlags.Mandatory;
        result.Data = data;
        return result;
    }

    internal static DiameterAvp CreateCreditControlServiceUnitAvp(
        uint code,
        DiameterServiceUnit serviceUnit)
    {
        serviceUnit.ValidateCreditControlServiceUnit();
        if (code != DiameterAvpCodes.RequestedServiceUnit &&
            code != DiameterAvpCodes.GrantedServiceUnit &&
            code != DiameterAvpCodes.UsedServiceUnit)
        {
            throw new ArgumentOutOfRangeException("code");
        }

        List<DiameterAvp> children = new List<DiameterAvp>();
        if (code == DiameterAvpCodes.GrantedServiceUnit &&
            serviceUnit.TariffChangeTimePresent)
        {
            children.Add(CreateCreditControlTimeAvp(
                DiameterAvpCodes.TariffTimeChange,
                serviceUnit.TariffChangeTime));
        }

        if (code == DiameterAvpCodes.UsedServiceUnit &&
            serviceUnit.TariffChangeUsagePresent)
        {
            children.Add(CreateCreditControlInteger32Avp(
                DiameterAvpCodes.TariffChangeUsage,
                (int)serviceUnit.TariffChangeUsage));
        }

        if (serviceUnit.TimePresent)
        {
            children.Add(CreateCreditControlUnsigned32ValueAvp(
                DiameterAvpCodes.CcTime,
                serviceUnit.Time));
        }

        if (serviceUnit.Money != null)
        {
            children.Add(CreateCreditControlMoneyAvp(serviceUnit.Money));
        }

        if (serviceUnit.TotalOctetsPresent)
        {
            children.Add(CreateCreditControlUnsigned64Avp(
                DiameterAvpCodes.CcTotalOctets,
                serviceUnit.TotalOctets));
        }

        if (serviceUnit.InputOctetsPresent)
        {
            children.Add(CreateCreditControlUnsigned64Avp(
                DiameterAvpCodes.CcInputOctets,
                serviceUnit.InputOctets));
        }

        if (serviceUnit.OutputOctetsPresent)
        {
            children.Add(CreateCreditControlUnsigned64Avp(
                DiameterAvpCodes.CcOutputOctets,
                serviceUnit.OutputOctets));
        }

        if (serviceUnit.ServiceSpecificUnitsPresent)
        {
            children.Add(CreateCreditControlUnsigned64Avp(
                DiameterAvpCodes.CcServiceSpecificUnits,
                serviceUnit.ServiceSpecificUnits));
        }

        return CreateCreditControlGroupedAvp(code, children);
    }

    internal static DiameterAvp CreateCreditControlMoneyAvp(
        DiameterCostInformation money)
    {
        money.ValidateCreditControlCostInformation();
        if (money.CostUnit.Length > 0)
        {
            throw new DiameterCreditControlException(
                "CC-Money does not carry Cost-Unit text.");
        }

        List<DiameterAvp> children = new List<DiameterAvp>();
        children.Add(CreateCreditControlUnitValueAvp(
            money.ValueDigits,
            money.Exponent,
            money.ExponentPresent));
        if (money.CurrencyCodePresent)
        {
            children.Add(CreateCreditControlUnsigned32ValueAvp(
                DiameterAvpCodes.CurrencyCode,
                money.CurrencyCode));
        }

        return CreateCreditControlGroupedAvp(
            DiameterAvpCodes.CcMoney,
            children);
    }

    internal static DiameterAvp CreateCreditControlCostInformationAvp(
        DiameterCostInformation cost)
    {
        cost.ValidateCreditControlCostInformation();
        if (!cost.CurrencyCodePresent)
        {
            throw new DiameterCreditControlException(
                "Cost-Information requires Currency-Code.");
        }

        List<DiameterAvp> children = new List<DiameterAvp>();
        children.Add(CreateCreditControlUnitValueAvp(
            cost.ValueDigits,
            cost.Exponent,
            cost.ExponentPresent));
        children.Add(CreateCreditControlUnsigned32ValueAvp(
            DiameterAvpCodes.CurrencyCode,
            cost.CurrencyCode));
        if (cost.CostUnit.Length > 0)
        {
            children.Add(new DiameterBaseAvpFactory().CreateTextAvp(
                DiameterAvpCodes.CostUnit,
                cost.CostUnit));
        }

        return CreateCreditControlGroupedAvp(
            DiameterAvpCodes.CostInformation,
            children);
    }

    internal static DiameterAvp CreateCreditControlUnitValueAvp(
        long valueDigits,
        int exponent,
        bool exponentPresent)
    {
        List<DiameterAvp> children = new List<DiameterAvp>();
        children.Add(CreateCreditControlInteger64Avp(
            DiameterAvpCodes.ValueDigits,
            valueDigits));
        if (exponentPresent)
        {
            children.Add(CreateCreditControlInteger32Avp(
                DiameterAvpCodes.Exponent,
                exponent));
        }

        return CreateCreditControlGroupedAvp(
            DiameterAvpCodes.UnitValue,
            children);
    }

    internal static DiameterAvp CreateCreditControlMultipleServiceAvp(
        DiameterMultipleServiceCredit credit)
    {
        credit.ValidateCreditControlMultipleServiceCredit();
        List<DiameterAvp> children = new List<DiameterAvp>();
        if (credit.GrantedUnit != null)
        {
            children.Add(CreateCreditControlServiceUnitAvp(
                DiameterAvpCodes.GrantedServiceUnit,
                credit.GrantedUnit));
        }

        if (credit.RequestedUnit != null)
        {
            children.Add(CreateCreditControlServiceUnitAvp(
                DiameterAvpCodes.RequestedServiceUnit,
                credit.RequestedUnit));
        }

        int usedIndex = 0;
        while (usedIndex < credit.UsedUnits.Count)
        {
            children.Add(CreateCreditControlServiceUnitAvp(
                DiameterAvpCodes.UsedServiceUnit,
                credit.UsedUnits[usedIndex]));
            usedIndex = usedIndex + 1;
        }

        int serviceIndex = 0;
        while (serviceIndex < credit.ServiceIdentifiers.Count)
        {
            children.Add(CreateCreditControlUnsigned32ValueAvp(
                DiameterAvpCodes.ServiceIdentifier,
                credit.ServiceIdentifiers[serviceIndex]));
            serviceIndex = serviceIndex + 1;
        }

        if (credit.RatingGroupPresent)
        {
            children.Add(CreateCreditControlUnsigned32ValueAvp(
                DiameterAvpCodes.RatingGroup,
                credit.RatingGroup));
        }

        if (credit.TariffChangeUsagePresent)
        {
            children.Add(CreateCreditControlInteger32Avp(
                DiameterAvpCodes.TariffChangeUsage,
                (int)credit.TariffChangeUsage));
        }

        if (credit.ValidityTimePresent)
        {
            children.Add(CreateCreditControlUnsigned32ValueAvp(
                DiameterAvpCodes.ValidityTime,
                (uint)(credit.ValidityTimeMilliseconds / 1000)));
        }

        if (credit.FinalUnitInstruction != null)
        {
            children.Add(CreateCreditControlFinalUnitAvp(
                credit.FinalUnitInstruction));
        }

        if (credit.QosFinalUnitInstruction != null)
        {
            children.Add(CreateCreditControlFinalUnitAvp(
                credit.QosFinalUnitInstruction));
        }

        int poolIndex = 0;
        while (poolIndex < credit.PoolReferences.Count)
        {
            children.Add(CreateCreditControlPoolReferenceAvp(
                credit.PoolReferences[poolIndex]));
            poolIndex = poolIndex + 1;
        }

        if (credit.ResultCodePresent)
        {
            children.Add(CreateCreditControlUnsigned32ValueAvp(
                DiameterAvpCodes.ResultCode,
                credit.ResultCode));
        }

        return CreateCreditControlGroupedAvp(
            DiameterAvpCodes.MultipleServicesCreditControl,
            children);
    }

    internal static DiameterAvp CreateCreditControlPoolReferenceAvp(
        DiameterPoolReference reference)
    {
        reference.ValidateCreditControlPoolReference();
        List<DiameterAvp> children = new List<DiameterAvp>();
        children.Add(CreateCreditControlUnsigned32ValueAvp(
            DiameterAvpCodes.GsuPoolIdentifier,
            reference.PoolIdentifier));
        children.Add(CreateCreditControlInteger32Avp(
            DiameterAvpCodes.CcUnitType,
            (int)reference.UnitType));
        children.Add(CreateCreditControlUnitValueAvp(
            reference.ValueDigits,
            reference.Exponent,
            reference.ExponentPresent));
        return CreateCreditControlGroupedAvp(
            DiameterAvpCodes.GsuPoolReference,
            children);
    }

    internal static DiameterAvp CreateCreditControlFinalUnitAvp(
        DiameterFinalUnitInstruction instruction)
    {
        instruction.ValidateCreditControlFinalUnitInstruction();
        List<DiameterAvp> children = new List<DiameterAvp>();
        children.Add(CreateCreditControlInteger32Avp(
            DiameterAvpCodes.FinalUnitAction,
            (int)instruction.Action));
        int ruleIndex = 0;
        while (ruleIndex < instruction.RestrictionFilterRules.Count)
        {
            children.Add(CreateCreditControlFilterRuleAvp(
                instruction.Qos ? DiameterAvpCodes.FilterRule :
                    DiameterAvpCodes.RestrictionFilterRule,
                instruction.RestrictionFilterRules[ruleIndex],
                instruction.Qos));
            ruleIndex = ruleIndex + 1;
        }

        DiameterBaseAvpFactory avpFactory =
            new DiameterBaseAvpFactory();
        int filterIndex = 0;
        while (filterIndex < instruction.FilterIdentifiers.Count)
        {
            children.Add(avpFactory.CreateTextAvp(
                DiameterAvpCodes.FilterId,
                instruction.FilterIdentifiers[filterIndex]));
            filterIndex = filterIndex + 1;
        }

        if (instruction.Action == DiameterFinalUnitAction.Redirect)
        {
            children.Add(instruction.RedirectExtension ?
                CreateCreditControlRedirectExtensionAvp(instruction) :
                CreateCreditControlLegacyRedirectAvp(instruction));
        }

        DiameterAvp result = CreateCreditControlGroupedAvp(
            instruction.Qos ?
                DiameterAvpCodes.QosFinalUnitIndication :
                DiameterAvpCodes.FinalUnitIndication,
            children);
        if (instruction.Qos)
        {
            result.Flags = DiameterAvpFlags.None;
        }

        return result;
    }

    internal static DiameterAvp CreateCreditControlFilterRuleAvp(
        uint code,
        string value,
        bool qos)
    {
        DiameterValueResult validation = qos ?
            FilterRuleCodec.ValidateQosFilterRule(value) :
            FilterRuleCodec.ValidateIpFilterRule(value);
        if (validation.Status != DiameterCodecStatus.Success)
        {
            throw new DiameterCreditControlException(
                "A final-unit filter rule is invalid.");
        }

        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Flags = DiameterAvpFlags.Mandatory;
        result.Data = System.Text.Encoding.ASCII.GetBytes(value);
        return result;
    }

    internal static DiameterAvp CreateCreditControlLegacyRedirectAvp(
        DiameterFinalUnitInstruction instruction)
    {
        List<DiameterAvp> children = new List<DiameterAvp>();
        children.Add(CreateCreditControlInteger32Avp(
            DiameterAvpCodes.RedirectAddressType,
            (int)instruction.RedirectAddressType));
        children.Add(new DiameterBaseAvpFactory().CreateTextAvp(
            DiameterAvpCodes.RedirectServerAddress,
            instruction.RedirectAddress));
        return CreateCreditControlGroupedAvp(
            DiameterAvpCodes.RedirectServer,
            children);
    }

    internal static DiameterAvp CreateCreditControlRedirectExtensionAvp(
        DiameterFinalUnitInstruction instruction)
    {
        List<DiameterAvp> children = new List<DiameterAvp>();
        DiameterAvp child;
        if (instruction.RedirectAddress.StartsWith(
            "sip:",
            StringComparison.OrdinalIgnoreCase) ||
            instruction.RedirectAddress.StartsWith(
                "sips:",
                StringComparison.OrdinalIgnoreCase))
        {
            child = new DiameterBaseAvpFactory().CreateTextAvp(
                DiameterAvpCodes.RedirectAddressSipUri,
                instruction.RedirectAddress);
        }
        else if (instruction.RedirectAddress.StartsWith(
            "http://",
            StringComparison.OrdinalIgnoreCase) ||
            instruction.RedirectAddress.StartsWith(
                "https://",
                StringComparison.OrdinalIgnoreCase))
        {
            child = new DiameterBaseAvpFactory().CreateTextAvp(
                DiameterAvpCodes.RedirectAddressUrl,
                instruction.RedirectAddress);
        }
        else
        {
            IPAddress address = IPAddress.Parse(
                instruction.RedirectAddress);
            byte[] data = new byte[18];
            DiameterValueResult encoded = new DiameterAvpCodec()
                .EncodeAddress(data, address);
            if (encoded.Status != DiameterCodecStatus.Success)
            {
                throw new DiameterCreditControlException(
                    "The redirect IP address is invalid.");
            }

            child = new DiameterAvp();
            child.Code = DiameterAvpCodes.RedirectAddressIpAddress;
            child.Data = new byte[encoded.WrittenCount];
            Array.Copy(data, child.Data, encoded.WrittenCount);
        }

        child.Flags = DiameterAvpFlags.None;
        children.Add(child);
        DiameterAvp result = CreateCreditControlGroupedAvp(
            DiameterAvpCodes.RedirectServerExtension,
            children);
        result.Flags = DiameterAvpFlags.None;
        return result;
    }

    internal static DiameterAvp CreateCreditControlSubscriptionAvp(
        DiameterSubscriptionIdentifier subscription)
    {
        subscription.ValidateCreditControlSubscriptionIdentifier();
        List<DiameterAvp> children = new List<DiameterAvp>();
        if (!subscription.Extension)
        {
            children.Add(CreateCreditControlInteger32Avp(
                DiameterAvpCodes.SubscriptionIdType,
                (int)subscription.Type));
            children.Add(new DiameterBaseAvpFactory().CreateTextAvp(
                DiameterAvpCodes.SubscriptionIdData,
                subscription.Data));
            return CreateCreditControlGroupedAvp(
                DiameterAvpCodes.SubscriptionId,
                children);
        }

        DiameterAvp value = new DiameterBaseAvpFactory().CreateTextAvp(
            ReadCreditControlSubscriptionExtensionCode(subscription.Type),
            subscription.Data);
        value.Flags = DiameterAvpFlags.None;
        children.Add(value);
        DiameterAvp result = CreateCreditControlGroupedAvp(
            DiameterAvpCodes.SubscriptionIdExtension,
            children);
        result.Flags = DiameterAvpFlags.None;
        return result;
    }

    internal static uint ReadCreditControlSubscriptionExtensionCode(
        DiameterSubscriptionIdType type)
    {
        switch (type)
        {
            case DiameterSubscriptionIdType.EndUserE164:
                return DiameterAvpCodes.SubscriptionIdE164;
            case DiameterSubscriptionIdType.EndUserImsi:
                return DiameterAvpCodes.SubscriptionIdImsi;
            case DiameterSubscriptionIdType.EndUserSipUri:
                return DiameterAvpCodes.SubscriptionIdSipUri;
            case DiameterSubscriptionIdType.EndUserNai:
                return DiameterAvpCodes.SubscriptionIdNai;
            case DiameterSubscriptionIdType.EndUserPrivate:
                return DiameterAvpCodes.SubscriptionIdPrivate;
            default:
                throw new DiameterCreditControlException(
                    "The subscription identifier extension type is invalid.");
        }
    }

    internal static DiameterAvp CreateCreditControlEquipmentAvp(
        DiameterUserEquipmentInformation equipment)
    {
        equipment.ValidateCreditControlUserEquipmentInformation();
        List<DiameterAvp> children = new List<DiameterAvp>();
        if (!equipment.Extension)
        {
            DiameterAvp type = CreateCreditControlInteger32Avp(
                DiameterAvpCodes.UserEquipmentInfoType,
                (int)equipment.Type);
            type.Flags = DiameterAvpFlags.None;
            children.Add(type);
            DiameterAvp value = new DiameterAvp();
            value.Code = DiameterAvpCodes.UserEquipmentInfoValue;
            value.Data = CloneCreditControlBytes(equipment.Value);
            children.Add(value);
            DiameterAvp legacy = CreateCreditControlGroupedAvp(
                DiameterAvpCodes.UserEquipmentInfo,
                children);
            legacy.Flags = DiameterAvpFlags.None;
            return legacy;
        }

        DiameterAvp extensionValue = new DiameterAvp();
        extensionValue.Code = ReadCreditControlEquipmentExtensionCode(
            equipment.Type,
            equipment.Imei);
        extensionValue.Data = CloneCreditControlBytes(equipment.Value);
        children.Add(extensionValue);
        DiameterAvp result = CreateCreditControlGroupedAvp(
            DiameterAvpCodes.UserEquipmentInfoExtension,
            children);
        result.Flags = DiameterAvpFlags.None;
        return result;
    }

    internal static uint ReadCreditControlEquipmentExtensionCode(
        DiameterUserEquipmentInfoType type,
        bool imei)
    {
        if (type == DiameterUserEquipmentInfoType.Imeisv)
        {
            return imei ? DiameterAvpCodes.UserEquipmentInfoImei :
                DiameterAvpCodes.UserEquipmentInfoImeisv;
        }

        switch (type)
        {
            case DiameterUserEquipmentInfoType.Mac:
                return DiameterAvpCodes.UserEquipmentInfoMac;
            case DiameterUserEquipmentInfoType.Eui64:
                return DiameterAvpCodes.UserEquipmentInfoEui64;
            case DiameterUserEquipmentInfoType.ModifiedEui64:
                return DiameterAvpCodes.UserEquipmentInfoModifiedEui64;
            default:
                throw new DiameterCreditControlException(
                    "The user-equipment extension type is invalid.");
        }
    }

    internal static DiameterAvp ValidateCreditControlServiceParameterAvp(
        DiameterAvp parameter)
    {
        if (parameter == null || parameter.Data == null ||
            parameter.VendorId != DiameterVendorIds.Ietf ||
            parameter.Code != DiameterAvpCodes.ServiceParameterInfo)
        {
            throw new DiameterCreditControlException(
                "Service parameters must use Service-Parameter-Info.");
        }

        DiameterValueResult grouped = AvpDecoder.DecodeAvpSequence(
            parameter.Data);
        if (grouped.Status != DiameterCodecStatus.Success)
        {
            throw new DiameterCreditControlException(
                "Service-Parameter-Info is invalid.");
        }

        bool typePresent = false;
        bool valuePresent = false;
        DiameterAvpCodec codec = new DiameterAvpCodec();
        int index = 0;
        while (index < grouped.Avps.Count)
        {
            DiameterAvp child = grouped.Avps[index];
            if (child.VendorId == DiameterVendorIds.Ietf &&
                child.Code == DiameterAvpCodes.ServiceParameterType &&
                !typePresent)
            {
                if (codec.DecodeUnsigned32(child.Data).Status !=
                    DiameterCodecStatus.Success)
                {
                    throw new DiameterCreditControlException(
                        "Service-Parameter-Type is invalid.");
                }

                typePresent = true;
            }
            else if (child.VendorId == DiameterVendorIds.Ietf &&
                child.Code == DiameterAvpCodes.ServiceParameterValue &&
                !valuePresent)
            {
                valuePresent = true;
            }
            else
            {
                throw new DiameterCreditControlException(
                    "Service-Parameter-Info requires one type and one value.");
            }

            index = index + 1;
        }

        if (!typePresent || !valuePresent)
        {
            throw new DiameterCreditControlException(
                "Service-Parameter-Info requires one type and one value.");
        }

        return CloneCreditControlAvp(parameter);
    }

    internal static byte[] CloneCreditControlBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        Array.Copy(source, result, source.Length);
        return result;
    }

    internal static DiameterAvp CreateCreditControlUnsigned32ValueAvp(
        uint code,
        uint value)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Flags = DiameterAvpFlags.Mandatory;
        result.Data = new byte[4];
        NetworkByteOrder.WriteUInt32(result.Data, value);
        return result;
    }

    internal static DiameterAvp CreateCreditControlInteger32Avp(
        uint code,
        int value)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Flags = DiameterAvpFlags.Mandatory;
        result.Data = new byte[4];
        NetworkByteOrder.WriteInt32(result.Data, value);
        return result;
    }

    internal static DiameterAvp CreateCreditControlInteger64Avp(
        uint code,
        long value)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Flags = DiameterAvpFlags.Mandatory;
        result.Data = new byte[8];
        NetworkByteOrder.WriteInt64(result.Data, value);
        return result;
    }

    internal static DiameterAvp CreateCreditControlGroupedAvp(
        uint code,
        List<DiameterAvp> children)
    {
        DiameterLimitConfiguration limits =
            new DiameterLimitConfiguration();
        DiameterValueResult measurement =
            GroupedValueCodec.MeasureGroupedValue(
                children,
                null,
                limits);
        if (measurement.Status != DiameterCodecStatus.Success)
        {
            throw new DiameterCreditControlException(
                "The credit-control grouped AVP could not be measured.");
        }

        byte[] data = new byte[measurement.RequiredCount];
        DiameterValueResult encoded =
            GroupedValueCodec.EncodeGroupedValue(
                data,
                children,
                null,
                limits);
        if (encoded.Status != DiameterCodecStatus.Success)
        {
            throw new DiameterCreditControlException(
                "The credit-control grouped AVP could not be encoded.");
        }

        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Flags = DiameterAvpFlags.Mandatory;
        result.Data = data;
        return result;
    }

    internal static DiameterAvp CloneCreditControlAvp(DiameterAvp source)
    {
        if (source == null || source.Data == null)
        {
            throw new DiameterCreditControlException(
                "A credit-control AVP requires owned payload data.");
        }

        DiameterAvp result = new DiameterAvp();
        result.Code = source.Code;
        result.VendorId = source.VendorId;
        result.Flags = source.Flags;
        result.Data = new byte[source.Data.Length];
        Array.Copy(source.Data, result.Data, source.Data.Length);
        return result;
    }

    internal static DiameterMessage CloneCreditControlMessage(
        DiameterMessage source)
    {
        DiameterMessage result = new DiameterMessage();
        result.Version = source.Version;
        result.Flags = source.Flags;
        result.CommandCode = source.CommandCode;
        result.ApplicationId = source.ApplicationId;
        result.HopByHopIdentifier = source.HopByHopIdentifier;
        result.EndToEndIdentifier = source.EndToEndIdentifier;
        int index = 0;
        while (index < source.Avps.Count)
        {
            result.Avps.Add(CloneCreditControlAvp(source.Avps[index]));
            index = index + 1;
        }

        return result;
    }

    internal static bool IsCreditControlCorrelationAvp(DiameterAvp avp)
    {
        if (avp.VendorId != DiameterVendorIds.Ietf)
        {
            return false;
        }

        return avp.Code == DiameterAvpCodes.SessionId ||
            avp.Code == DiameterAvpCodes.ResultCode ||
            avp.Code == DiameterAvpCodes.OriginHost ||
            avp.Code == DiameterAvpCodes.OriginRealm ||
            avp.Code == DiameterAvpCodes.DestinationRealm ||
            avp.Code == DiameterAvpCodes.AuthApplicationId ||
            avp.Code == DiameterAvpCodes.ServiceContextId ||
            avp.Code == DiameterAvpCodes.CcRequestType ||
            avp.Code == DiameterAvpCodes.CcRequestNumber;
    }
}
