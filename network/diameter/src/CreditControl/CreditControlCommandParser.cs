using System;

namespace diameter;

internal class CreditControlRequestParseState
{
    internal DiameterCreditControlRequest Request =
        new DiameterCreditControlRequest();
    internal bool SessionId;
    internal bool OriginHost;
    internal bool OriginRealm;
    internal bool DestinationRealm;
    internal bool AuthApplicationId;
    internal bool ServiceContextId;
    internal bool RequestType;
    internal bool RequestNumber;
}

internal class CreditControlAnswerParseState
{
    internal DiameterCreditControlAnswer Answer =
        new DiameterCreditControlAnswer();
    internal bool SessionId;
    internal bool ResultCode;
    internal bool OriginHost;
    internal bool OriginRealm;
    internal bool AuthApplicationId;
    internal bool RequestType;
    internal bool RequestNumber;
}

internal class CreditControlFinalUnitParseState
{
    internal DiameterFinalUnitInstruction Instruction =
        new DiameterFinalUnitInstruction();
    internal bool ActionPresent;
    internal bool RedirectPresent;
}

internal class CreditControlCommandParser
{
    internal IDiameterClock Clock;
    internal IDiameterAvpCodec Codec = new DiameterAvpCodec();

    internal CreditControlCommandParser(IDiameterClock? clock)
    {
        if (clock == null)
        {
            throw new ArgumentException(
                "Credit-control parsing requires a clock.");
        }

        Clock = clock;
    }

    internal DiameterCreditControlRequest ParseCreditControlRequest(
        DiameterMessage? message)
    {
        if (message == null)
        {
            throw new DiameterCreditControlException(
                "A Credit-Control-Request message is required.");
        }

        ValidateCreditControlRequestHeader(message);
        DiameterMessage input = message;
        CreditControlRequestParseState state =
            new CreditControlRequestParseState();
        int index = 0;
        while (index < input.Avps.Count)
        {
            ReadCreditControlRequestAvp(input.Avps[index], state);
            index = index + 1;
        }

        ValidateCreditControlRequestParseState(state);
        state.Request.ValidateCreditControlRequest();
        CreditControlMultipleServiceState
            .ValidateCreditControlMultipleServiceKeys(
                state.Request.MultipleServiceCredits);
        ValidateCreditControlParsedRequestNumber(state.Request);
        return state.Request;
    }

    internal DiameterCreditControlAnswer ParseCreditControlAnswer(
        DiameterMessage? message,
        DiameterCreditControlRequest? expectedRequest)
    {
        if (message == null)
        {
            throw new DiameterCreditControlException(
                "A Credit-Control-Answer message is required.");
        }

        ValidateCreditControlAnswerHeader(message);
        if (expectedRequest == null)
        {
            throw new DiameterCreditControlException(
                "An expected request is required to correlate a Credit-Control-Answer.");
        }

        expectedRequest.ValidateCreditControlRequest();
        ValidateCreditControlParsedRequestNumber(expectedRequest);
        DiameterMessage input = message;
        CreditControlAnswerParseState state =
            new CreditControlAnswerParseState();
        int index = 0;
        while (index < input.Avps.Count)
        {
            ReadCreditControlAnswerAvp(input.Avps[index], state);
            index = index + 1;
        }

        ValidateCreditControlAnswerParseState(state);
        if (state.Answer.SessionId != expectedRequest.SessionId ||
            state.Answer.RequestType != expectedRequest.RequestType ||
            state.Answer.RequestNumber != expectedRequest.RequestNumber)
        {
            throw new DiameterCreditControlException(
                "The Credit-Control-Answer correlation values do not match the request.");
        }

        state.Answer.Message =
            CreditControlCommandCodec.CloneCreditControlMessage(input);
        state.Answer.ValidateCreditControlAnswer();
        CreditControlMultipleServiceState
            .ValidateCreditControlMultipleServiceKeys(
                state.Answer.MultipleServiceCredits);
        return state.Answer;
    }

    internal void ValidateCreditControlRequestHeader(
        DiameterMessage? message)
    {
        if (message == null || message.Avps == null ||
            message.Version != DiameterProtocolConstants.Version ||
            message.CommandCode != DiameterCommandCodes.CreditControl ||
            message.ApplicationId != DiameterApplicationIds.CreditControl ||
            (message.Flags & DiameterMessageFlags.Request) ==
                DiameterMessageFlags.None ||
            (message.Flags & DiameterMessageFlags.Error) !=
                DiameterMessageFlags.None)
        {
            throw new DiameterCreditControlException(
                "A Credit-Control-Request must use command 272 and Application-Id 4.");
        }
    }

    internal void ValidateCreditControlAnswerHeader(
        DiameterMessage? message)
    {
        if (message == null || message.Avps == null ||
            message.Version != DiameterProtocolConstants.Version ||
            message.CommandCode != DiameterCommandCodes.CreditControl ||
            message.ApplicationId != DiameterApplicationIds.CreditControl ||
            (message.Flags & DiameterMessageFlags.Request) !=
                DiameterMessageFlags.None)
        {
            throw new DiameterCreditControlException(
                "A Credit-Control-Answer must use command 272 and Application-Id 4.");
        }
    }

    internal void ReadCreditControlRequestAvp(
        DiameterAvp avp,
        CreditControlRequestParseState state)
    {
        if (avp == null || avp.Data == null)
        {
            throw new DiameterCreditControlException(
                "Credit-control AVPs require owned payload data.");
        }

        if (avp.VendorId != DiameterVendorIds.Ietf)
        {
            state.Request.ServiceAvps.Add(
                CreditControlCommandCodec.CloneCreditControlAvp(avp));
            return;
        }

        if (avp.Code == DiameterAvpCodes.SessionId)
        {
            RequireCreditControlSingleAvp(state.SessionId, "Session-Id");
            state.SessionId = true;
            state.Request.SessionId = ReadCreditControlTextAvp(avp);
            return;
        }

        if (avp.Code == DiameterAvpCodes.OriginHost)
        {
            RequireCreditControlSingleAvp(state.OriginHost, "Origin-Host");
            state.OriginHost = true;
            ReadCreditControlIdentityAvp(avp);
            return;
        }

        if (avp.Code == DiameterAvpCodes.OriginRealm)
        {
            RequireCreditControlSingleAvp(state.OriginRealm, "Origin-Realm");
            state.OriginRealm = true;
            ReadCreditControlIdentityAvp(avp);
            return;
        }

        if (avp.Code == DiameterAvpCodes.DestinationRealm)
        {
            RequireCreditControlSingleAvp(
                state.DestinationRealm,
                "Destination-Realm");
            state.DestinationRealm = true;
            state.Request.DestinationRealm =
                ReadCreditControlIdentityAvp(avp);
            return;
        }

        if (avp.Code == DiameterAvpCodes.AuthApplicationId)
        {
            RequireCreditControlSingleAvp(
                state.AuthApplicationId,
                "Auth-Application-Id");
            state.AuthApplicationId = true;
            uint applicationId = ReadCreditControlUnsigned32Avp(avp);
            if (applicationId != DiameterApplicationIds.CreditControl)
            {
                throw new DiameterCreditControlException(
                    "Auth-Application-Id must identify Diameter credit control.");
            }

            return;
        }

        if (avp.Code == DiameterAvpCodes.ServiceContextId)
        {
            RequireCreditControlSingleAvp(
                state.ServiceContextId,
                "Service-Context-Id");
            state.ServiceContextId = true;
            state.Request.ServiceContextId =
                ReadCreditControlTextAvp(avp);
            return;
        }

        if (avp.Code == DiameterAvpCodes.CcRequestType)
        {
            RequireCreditControlSingleAvp(
                state.RequestType,
                "CC-Request-Type");
            state.RequestType = true;
            state.Request.RequestType =
                (DiameterCreditControlRequestType)
                ReadCreditControlInteger32Avp(avp);
            return;
        }

        if (avp.Code == DiameterAvpCodes.CcRequestNumber)
        {
            RequireCreditControlSingleAvp(
                state.RequestNumber,
                "CC-Request-Number");
            state.RequestNumber = true;
            state.Request.RequestNumber =
                ReadCreditControlUnsigned32Avp(avp);
            return;
        }

        ReadCreditControlOptionalRequestAvp(avp, state.Request);
    }

    internal void ReadCreditControlOptionalRequestAvp(
        DiameterAvp avp,
        DiameterCreditControlRequest request)
    {
        if (avp.Code == DiameterAvpCodes.DestinationHost)
        {
            if (request.DestinationHost != null)
            {
                throw new DiameterCreditControlException(
                    "Destination-Host occurs more than once.");
            }

            request.DestinationHost = ReadCreditControlIdentityAvp(avp);
        }
        else if (avp.Code == DiameterAvpCodes.UserName)
        {
            if (request.UserName.Length > 0)
            {
                throw new DiameterCreditControlException(
                    "User-Name occurs more than once.");
            }

            request.UserName = ReadCreditControlTextAvp(avp);
        }
        else if (avp.Code == DiameterAvpCodes.CcSubSessionId)
        {
            if (request.CreditControlSubSessionIdPresent)
            {
                throw new DiameterCreditControlException(
                    "CC-Sub-Session-Id occurs more than once.");
            }

            request.CreditControlSubSessionId =
                ReadCreditControlUnsigned64Avp(avp);
            request.CreditControlSubSessionIdPresent = true;
        }
        else if (avp.Code == DiameterAvpCodes.AcctMultiSessionId)
        {
            if (request.AccountingMultiSessionId.Length > 0)
            {
                throw new DiameterCreditControlException(
                    "Acct-Multi-Session-Id occurs more than once.");
            }

            request.AccountingMultiSessionId =
                ReadCreditControlTextAvp(avp);
        }
        else if (avp.Code == DiameterAvpCodes.OriginStateId)
        {
            if (request.OriginStateIdentifierPresent)
            {
                throw new DiameterCreditControlException(
                    "Origin-State-Id occurs more than once.");
            }

            request.OriginStateIdentifier =
                ReadCreditControlUnsigned32Avp(avp);
            request.OriginStateIdentifierPresent = true;
        }
        else if (avp.Code == DiameterAvpCodes.EventTimestamp)
        {
            if (request.EventTimestampPresent)
            {
                throw new DiameterCreditControlException(
                    "Event-Timestamp occurs more than once.");
            }

            request.EventTimestamp = ReadCreditControlTimeAvp(avp);
            request.EventTimestampPresent = true;
        }
        else if (avp.Code == DiameterAvpCodes.SubscriptionId ||
            avp.Code == DiameterAvpCodes.SubscriptionIdExtension)
        {
            request.SubscriptionIdentifiers.Add(
                ReadCreditControlSubscriptionAvp(avp));
        }
        else if (avp.Code == DiameterAvpCodes.UserEquipmentInfo ||
            avp.Code == DiameterAvpCodes.UserEquipmentInfoExtension)
        {
            if (request.UserEquipmentInformation != null)
            {
                throw new DiameterCreditControlException(
                    "User-Equipment-Info occurs more than once.");
            }

            request.UserEquipmentInformation =
                ReadCreditControlEquipmentAvp(avp);
        }
        else if (avp.Code == DiameterAvpCodes.ServiceIdentifier)
        {
            request.ServiceIdentifiers.Add(
                ReadCreditControlUnsigned32Avp(avp));
        }
        else if (avp.Code == DiameterAvpCodes.TerminationCause)
        {
            if (request.TerminationCausePresent)
            {
                throw new DiameterCreditControlException(
                    "Termination-Cause occurs more than once.");
            }

            request.TerminationCause = (DiameterTerminationCause)
                ReadCreditControlInteger32Avp(avp);
            request.TerminationCausePresent = true;
        }
        else if (avp.Code == DiameterAvpCodes.RequestedAction)
        {
            if (request.RequestedActionPresent)
            {
                throw new DiameterCreditControlException(
                    "Requested-Action occurs more than once.");
            }

            request.RequestedAction = (DiameterRequestedAction)
                ReadCreditControlInteger32Avp(avp);
            request.RequestedActionPresent = true;
        }
        else if (avp.Code == DiameterAvpCodes.MultipleServicesIndicator)
        {
            if (request.MultipleServicesIndicatorPresent)
            {
                throw new DiameterCreditControlException(
                    "Multiple-Services-Indicator occurs more than once.");
            }

            request.MultipleServicesIndicator =
                (DiameterMultipleServicesIndicator)
                ReadCreditControlInteger32Avp(avp);
            request.MultipleServicesIndicatorPresent = true;
        }
        else if (avp.Code == DiameterAvpCodes.CcCorrelationId)
        {
            if (request.CorrelationId.Length > 0)
            {
                throw new DiameterCreditControlException(
                    "CC-Correlation-Id occurs more than once.");
            }

            request.CorrelationId = ReadCreditControlOctetsAvp(avp);
        }
        else if (avp.Code == DiameterAvpCodes.RequestedServiceUnit)
        {
            if (request.RequestedServiceUnit != null)
            {
                throw new DiameterCreditControlException(
                    "Requested-Service-Unit occurs more than once.");
            }

            request.RequestedServiceUnit =
                ReadCreditControlServiceUnitAvp(avp);
        }
        else if (avp.Code == DiameterAvpCodes.UsedServiceUnit)
        {
            request.UsedServiceUnits.Add(
                ReadCreditControlServiceUnitAvp(avp));
        }
        else if (avp.Code ==
            DiameterAvpCodes.MultipleServicesCreditControl)
        {
            request.MultipleServiceCredits.Add(
                ReadCreditControlMultipleServiceAvp(avp));
        }
        else if (avp.Code == DiameterAvpCodes.ServiceParameterInfo)
        {
            request.ServiceParameterAvps.Add(
                CreditControlCommandCodec
                    .ValidateCreditControlServiceParameterAvp(avp));
        }
        else if (avp.Code != DiameterAvpCodes.ProxyInfo &&
            avp.Code != DiameterAvpCodes.RouteRecord)
        {
            request.ServiceAvps.Add(
                CreditControlCommandCodec.CloneCreditControlAvp(avp));
        }
    }

    internal void ReadCreditControlAnswerAvp(
        DiameterAvp avp,
        CreditControlAnswerParseState state)
    {
        if (avp == null || avp.Data == null)
        {
            throw new DiameterCreditControlException(
                "Credit-control AVPs require owned payload data.");
        }

        if (avp.VendorId != DiameterVendorIds.Ietf)
        {
            state.Answer.ServiceAvps.Add(
                CreditControlCommandCodec.CloneCreditControlAvp(avp));
            return;
        }

        if (avp.Code == DiameterAvpCodes.SessionId)
        {
            RequireCreditControlSingleAvp(state.SessionId, "Session-Id");
            state.SessionId = true;
            state.Answer.SessionId = ReadCreditControlTextAvp(avp);
            return;
        }

        if (avp.Code == DiameterAvpCodes.ResultCode)
        {
            RequireCreditControlSingleAvp(state.ResultCode, "Result-Code");
            state.ResultCode = true;
            state.Answer.ResultCode = ReadCreditControlUnsigned32Avp(avp);
            return;
        }

        if (avp.Code == DiameterAvpCodes.OriginHost)
        {
            RequireCreditControlSingleAvp(state.OriginHost, "Origin-Host");
            state.OriginHost = true;
            ReadCreditControlIdentityAvp(avp);
            return;
        }

        if (avp.Code == DiameterAvpCodes.OriginRealm)
        {
            RequireCreditControlSingleAvp(state.OriginRealm, "Origin-Realm");
            state.OriginRealm = true;
            ReadCreditControlIdentityAvp(avp);
            return;
        }

        if (avp.Code == DiameterAvpCodes.AuthApplicationId)
        {
            RequireCreditControlSingleAvp(
                state.AuthApplicationId,
                "Auth-Application-Id");
            state.AuthApplicationId = true;
            uint applicationId = ReadCreditControlUnsigned32Avp(avp);
            if (applicationId != DiameterApplicationIds.CreditControl)
            {
                throw new DiameterCreditControlException(
                    "Auth-Application-Id must identify Diameter credit control.");
            }

            return;
        }

        if (avp.Code == DiameterAvpCodes.CcRequestType)
        {
            RequireCreditControlSingleAvp(
                state.RequestType,
                "CC-Request-Type");
            state.RequestType = true;
            state.Answer.RequestType =
                (DiameterCreditControlRequestType)
                ReadCreditControlInteger32Avp(avp);
            return;
        }

        if (avp.Code == DiameterAvpCodes.CcRequestNumber)
        {
            RequireCreditControlSingleAvp(
                state.RequestNumber,
                "CC-Request-Number");
            state.RequestNumber = true;
            state.Answer.RequestNumber =
                ReadCreditControlUnsigned32Avp(avp);
            return;
        }

        ReadCreditControlOptionalAnswerAvp(avp, state.Answer);
    }

    internal void ReadCreditControlOptionalAnswerAvp(
        DiameterAvp avp,
        DiameterCreditControlAnswer answer)
    {
        if (avp.Code == DiameterAvpCodes.UserName)
        {
            if (answer.UserName.Length > 0)
            {
                throw new DiameterCreditControlException(
                    "User-Name occurs more than once.");
            }

            answer.UserName = ReadCreditControlTextAvp(avp);
        }
        else if (avp.Code == DiameterAvpCodes.CcSessionFailover)
        {
            if (answer.SessionFailoverPresent)
            {
                throw new DiameterCreditControlException(
                    "CC-Session-Failover occurs more than once.");
            }

            int value = ReadCreditControlInteger32Avp(avp);
            if (value < 0 || value > 1)
            {
                throw new DiameterCreditControlException(
                    "CC-Session-Failover has an invalid value.");
            }

            answer.SessionFailoverSupported = value == 1;
            answer.SessionFailoverPresent = true;
        }
        else if (avp.Code == DiameterAvpCodes.CcSubSessionId)
        {
            if (answer.CreditControlSubSessionIdPresent)
            {
                throw new DiameterCreditControlException(
                    "CC-Sub-Session-Id occurs more than once.");
            }

            answer.CreditControlSubSessionId =
                ReadCreditControlUnsigned64Avp(avp);
            answer.CreditControlSubSessionIdPresent = true;
        }
        else if (avp.Code == DiameterAvpCodes.AcctMultiSessionId)
        {
            if (answer.AccountingMultiSessionId.Length > 0)
            {
                throw new DiameterCreditControlException(
                    "Acct-Multi-Session-Id occurs more than once.");
            }

            answer.AccountingMultiSessionId =
                ReadCreditControlTextAvp(avp);
        }
        else if (avp.Code == DiameterAvpCodes.OriginStateId)
        {
            if (answer.OriginStateIdentifierPresent)
            {
                throw new DiameterCreditControlException(
                    "Origin-State-Id occurs more than once.");
            }

            answer.OriginStateIdentifier =
                ReadCreditControlUnsigned32Avp(avp);
            answer.OriginStateIdentifierPresent = true;
        }
        else if (avp.Code == DiameterAvpCodes.EventTimestamp)
        {
            if (answer.EventTimestampPresent)
            {
                throw new DiameterCreditControlException(
                    "Event-Timestamp occurs more than once.");
            }

            answer.EventTimestamp = ReadCreditControlTimeAvp(avp);
            answer.EventTimestampPresent = true;
        }
        else if (avp.Code == DiameterAvpCodes.CheckBalanceResult)
        {
            if (answer.CheckBalanceResultPresent)
            {
                throw new DiameterCreditControlException(
                    "Check-Balance-Result occurs more than once.");
            }

            answer.CheckBalanceResult = (DiameterCheckBalanceResult)
                ReadCreditControlInteger32Avp(avp);
            answer.CheckBalanceResultPresent = true;
        }
        else if (avp.Code == DiameterAvpCodes.CreditControlFailureHandling)
        {
            if (answer.FailureHandlingPresent)
            {
                throw new DiameterCreditControlException(
                    "Credit-Control-Failure-Handling occurs more than once.");
            }

            answer.FailureHandling =
                (DiameterCreditControlFailureHandling)
                ReadCreditControlInteger32Avp(avp);
            answer.FailureHandlingPresent = true;
        }
        else if (avp.Code ==
            DiameterAvpCodes.DirectDebitingFailureHandling)
        {
            if (answer.DirectDebitFailureHandlingPresent)
            {
                throw new DiameterCreditControlException(
                    "Direct-Debiting-Failure-Handling occurs more than once.");
            }

            answer.DirectDebitFailureHandling =
                (DiameterDirectDebitFailureHandling)
                ReadCreditControlInteger32Avp(avp);
            answer.DirectDebitFailureHandlingPresent = true;
        }
        else if (avp.Code == DiameterAvpCodes.ValidityTime)
        {
            if (answer.ValidityTimePresent)
            {
                throw new DiameterCreditControlException(
                    "Validity-Time occurs more than once.");
            }

            uint seconds = ReadCreditControlUnsigned32Avp(avp);
            if (seconds > (uint)(int.MaxValue / 1000))
            {
                throw new DiameterCreditControlException(
                    "Validity-Time exceeds the supported duration.");
            }

            answer.ValidityTimeMilliseconds = (int)seconds * 1000;
            answer.ValidityTimePresent = true;
        }
        else if (avp.Code == DiameterAvpCodes.RedirectHost)
        {
            DiameterValueResult decoded =
                Codec.DecodeDiameterUri(avp.Data);
            if (decoded.Status != DiameterCodecStatus.Success ||
                decoded.Uri == null || decoded.Uri.Protocol != "diameter")
            {
                throw new DiameterCreditControlException(
                    "Redirect-Host is not a valid Diameter URI.");
            }

            answer.RedirectHosts.Add(decoded.Uri);
        }
        else if (avp.Code == DiameterAvpCodes.RedirectHostUsage)
        {
            if (answer.RedirectHostUsagePresent)
            {
                throw new DiameterCreditControlException(
                    "Redirect-Host-Usage occurs more than once.");
            }

            int value = ReadCreditControlInteger32Avp(avp);
            if (value < (int)DiameterRedirectHostUsage.DoNotCache ||
                value > (int)DiameterRedirectHostUsage.AllUser)
            {
                throw new DiameterCreditControlException(
                    "Redirect-Host-Usage has an invalid value.");
            }

            answer.RedirectHostUsage =
                (DiameterRedirectHostUsage)value;
            answer.RedirectHostUsagePresent = true;
        }
        else if (avp.Code == DiameterAvpCodes.RedirectMaxCacheTime)
        {
            if (answer.RedirectMaxCacheTimePresent)
            {
                throw new DiameterCreditControlException(
                    "Redirect-Max-Cache-Time occurs more than once.");
            }

            uint seconds = ReadCreditControlUnsigned32Avp(avp);
            if (seconds > (uint)(int.MaxValue / 1000))
            {
                throw new DiameterCreditControlException(
                    "Redirect-Max-Cache-Time exceeds the supported duration.");
            }

            answer.RedirectMaxCacheTimeMilliseconds =
                (int)seconds * 1000;
            answer.RedirectMaxCacheTimePresent = true;
        }
        else if (avp.Code == DiameterAvpCodes.GrantedServiceUnit)
        {
            if (answer.GrantedServiceUnit != null)
            {
                throw new DiameterCreditControlException(
                    "Granted-Service-Unit occurs more than once.");
            }

            answer.GrantedServiceUnit =
                ReadCreditControlServiceUnitAvp(avp);
        }
        else if (avp.Code == DiameterAvpCodes.CostInformation)
        {
            if (answer.CostInformation != null)
            {
                throw new DiameterCreditControlException(
                    "Cost-Information occurs more than once.");
            }

            answer.CostInformation =
                ReadCreditControlCostInformationAvp(avp);
        }
        else if (avp.Code == DiameterAvpCodes.FinalUnitIndication)
        {
            if (answer.FinalUnitInstruction != null)
            {
                throw new DiameterCreditControlException(
                    "Final-Unit-Indication occurs more than once.");
            }

            answer.FinalUnitInstruction =
                ReadCreditControlFinalUnitAvp(avp, false);
        }
        else if (avp.Code == DiameterAvpCodes.QosFinalUnitIndication)
        {
            if (answer.QosFinalUnitInstruction != null)
            {
                throw new DiameterCreditControlException(
                    "QoS-Final-Unit-Indication occurs more than once.");
            }

            answer.QosFinalUnitInstruction =
                ReadCreditControlFinalUnitAvp(avp, true);
        }
        else if (avp.Code ==
            DiameterAvpCodes.MultipleServicesCreditControl)
        {
            answer.MultipleServiceCredits.Add(
                ReadCreditControlMultipleServiceAvp(avp));
        }
        else if (avp.Code == DiameterAvpCodes.FailedAvp)
        {
            answer.FailedAvps.Add(
                CreditControlCommandCodec.CloneCreditControlAvp(avp));
        }
        else if (avp.Code != DiameterAvpCodes.ProxyInfo &&
            avp.Code != DiameterAvpCodes.RouteRecord)
        {
            answer.ServiceAvps.Add(
                CreditControlCommandCodec.CloneCreditControlAvp(avp));
        }
    }

    internal void ValidateCreditControlRequestParseState(
        CreditControlRequestParseState state)
    {
        if (!state.SessionId || !state.OriginHost ||
            !state.OriginRealm || !state.DestinationRealm ||
            !state.AuthApplicationId || !state.ServiceContextId ||
            !state.RequestType || !state.RequestNumber)
        {
            throw new DiameterCreditControlException(
                "The Credit-Control-Request is missing a required correlation AVP.");
        }
    }

    internal void ValidateCreditControlAnswerParseState(
        CreditControlAnswerParseState state)
    {
        if (!state.SessionId || !state.ResultCode ||
            !state.OriginHost || !state.OriginRealm ||
            !state.AuthApplicationId || !state.RequestType ||
            !state.RequestNumber)
        {
            throw new DiameterCreditControlException(
                "The Credit-Control-Answer is missing a required correlation AVP.");
        }
    }

    internal void ValidateCreditControlParsedRequestNumber(
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
                "The CC-Request-Number does not match the CC-Request-Type.");
        }
    }

    internal void RequireCreditControlSingleAvp(
        bool alreadyPresent,
        string name)
    {
        if (alreadyPresent)
        {
            throw new DiameterCreditControlException(
                name + " occurs more than once.");
        }
    }

    internal string ReadCreditControlTextAvp(DiameterAvp avp)
    {
        DiameterValueResult decoded = Codec.DecodeUtf8String(avp.Data);
        if (decoded.Status != DiameterCodecStatus.Success ||
            decoded.Text.Length == 0)
        {
            throw new DiameterCreditControlException(
                "A credit-control text AVP is invalid.");
        }

        return decoded.Text;
    }

    internal DiameterIdentity ReadCreditControlIdentityAvp(DiameterAvp avp)
    {
        DiameterValueResult decoded =
            Codec.DecodeDiameterIdentity(avp.Data);
        if (decoded.Status != DiameterCodecStatus.Success ||
            decoded.Identity == null)
        {
            throw new DiameterCreditControlException(
                "A credit-control identity AVP is invalid.");
        }

        return decoded.Identity;
    }

    internal uint ReadCreditControlUnsigned32Avp(DiameterAvp avp)
    {
        DiameterValueResult decoded = Codec.DecodeUnsigned32(avp.Data);
        if (decoded.Status != DiameterCodecStatus.Success)
        {
            throw new DiameterCreditControlException(
                "A credit-control Unsigned32 AVP is invalid.");
        }

        return decoded.Unsigned32;
    }

    internal ulong ReadCreditControlUnsigned64Avp(DiameterAvp avp)
    {
        DiameterValueResult decoded = Codec.DecodeUnsigned64(avp.Data);
        if (decoded.Status != DiameterCodecStatus.Success)
        {
            throw new DiameterCreditControlException(
                "A credit-control Unsigned64 AVP is invalid.");
        }

        return decoded.Unsigned64;
    }

    internal long ReadCreditControlInteger64Avp(DiameterAvp avp)
    {
        DiameterValueResult decoded = Codec.DecodeInteger64(avp.Data);
        if (decoded.Status != DiameterCodecStatus.Success)
        {
            throw new DiameterCreditControlException(
                "A credit-control Integer64 AVP is invalid.");
        }

        return decoded.Integer64;
    }

    internal int ReadCreditControlInteger32Avp(DiameterAvp avp)
    {
        DiameterValueResult decoded = Codec.DecodeEnumerated(avp.Data);
        if (decoded.Status != DiameterCodecStatus.Success)
        {
            throw new DiameterCreditControlException(
                "A credit-control Enumerated AVP is invalid.");
        }

        return decoded.Integer32;
    }

    internal DateTimeOffset ReadCreditControlTimeAvp(DiameterAvp avp)
    {
        DiameterValueResult decoded = Codec.DecodeTime(
            avp.Data,
            Clock.ReadUtcTime());
        if (decoded.Status != DiameterCodecStatus.Success)
        {
            throw new DiameterCreditControlException(
                "A credit-control Time AVP is invalid.");
        }

        return decoded.Time;
    }

    internal byte[] ReadCreditControlOctetsAvp(DiameterAvp avp)
    {
        byte[] result = new byte[avp.Data.Length];
        Array.Copy(avp.Data, result, avp.Data.Length);
        return result;
    }

    internal DiameterSubscriptionIdentifier
        ReadCreditControlSubscriptionAvp(DiameterAvp avp)
    {
        DiameterValueResult grouped = AvpDecoder.DecodeAvpSequence(avp.Data);
        if (grouped.Status != DiameterCodecStatus.Success)
        {
            throw new DiameterCreditControlException(
                "A Subscription-Id grouped AVP is invalid.");
        }

        DiameterSubscriptionIdentifier result =
            new DiameterSubscriptionIdentifier();
        if (avp.Code == DiameterAvpCodes.SubscriptionIdExtension)
        {
            if (grouped.Avps.Count != 1 ||
                grouped.Avps[0].VendorId != DiameterVendorIds.Ietf)
            {
                throw new DiameterCreditControlException(
                    "Subscription-Id-Extension requires one identifier.");
            }

            DiameterAvp child = grouped.Avps[0];
            result.Type = ReadCreditControlSubscriptionExtensionType(
                child.Code);
            result.Data = ReadCreditControlTextAvp(child);
            result.Extension = true;
            result.ValidateCreditControlSubscriptionIdentifier();
            return result;
        }

        bool typePresent = false;
        bool dataPresent = false;
        int index = 0;
        while (index < grouped.Avps.Count)
        {
            DiameterAvp child = grouped.Avps[index];
            if (child.VendorId == DiameterVendorIds.Ietf &&
                child.Code == DiameterAvpCodes.SubscriptionIdType &&
                !typePresent)
            {
                result.Type = (DiameterSubscriptionIdType)
                    ReadCreditControlInteger32Avp(child);
                typePresent = true;
            }
            else if (child.VendorId == DiameterVendorIds.Ietf &&
                child.Code == DiameterAvpCodes.SubscriptionIdData &&
                !dataPresent)
            {
                result.Data = ReadCreditControlTextAvp(child);
                dataPresent = true;
            }
            else
            {
                throw new DiameterCreditControlException(
                    "Subscription-Id requires one type and one value.");
            }

            index = index + 1;
        }

        if (!typePresent || !dataPresent)
        {
            throw new DiameterCreditControlException(
                "Subscription-Id requires one type and one value.");
        }

        result.ValidateCreditControlSubscriptionIdentifier();
        return result;
    }

    internal DiameterSubscriptionIdType
        ReadCreditControlSubscriptionExtensionType(uint code)
    {
        switch (code)
        {
            case DiameterAvpCodes.SubscriptionIdE164:
                return DiameterSubscriptionIdType.EndUserE164;
            case DiameterAvpCodes.SubscriptionIdImsi:
                return DiameterSubscriptionIdType.EndUserImsi;
            case DiameterAvpCodes.SubscriptionIdSipUri:
                return DiameterSubscriptionIdType.EndUserSipUri;
            case DiameterAvpCodes.SubscriptionIdNai:
                return DiameterSubscriptionIdType.EndUserNai;
            case DiameterAvpCodes.SubscriptionIdPrivate:
                return DiameterSubscriptionIdType.EndUserPrivate;
            default:
                throw new DiameterCreditControlException(
                    "The subscription identifier extension type is invalid.");
        }
    }

    internal DiameterUserEquipmentInformation
        ReadCreditControlEquipmentAvp(DiameterAvp avp)
    {
        DiameterValueResult grouped = AvpDecoder.DecodeAvpSequence(avp.Data);
        if (grouped.Status != DiameterCodecStatus.Success)
        {
            throw new DiameterCreditControlException(
                "A User-Equipment-Info grouped AVP is invalid.");
        }

        DiameterUserEquipmentInformation result =
            new DiameterUserEquipmentInformation();
        if (avp.Code == DiameterAvpCodes.UserEquipmentInfoExtension)
        {
            if (grouped.Avps.Count != 1 ||
                grouped.Avps[0].VendorId != DiameterVendorIds.Ietf)
            {
                throw new DiameterCreditControlException(
                    "User-Equipment-Info-Extension requires one identifier.");
            }

            DiameterAvp child = grouped.Avps[0];
            ReadCreditControlEquipmentExtensionType(result, child.Code);
            result.Value = ReadCreditControlOctetsAvp(child);
            result.Extension = true;
            result.ValidateCreditControlUserEquipmentInformation();
            return result;
        }

        bool typePresent = false;
        bool valuePresent = false;
        int index = 0;
        while (index < grouped.Avps.Count)
        {
            DiameterAvp child = grouped.Avps[index];
            if (child.VendorId == DiameterVendorIds.Ietf &&
                child.Code == DiameterAvpCodes.UserEquipmentInfoType &&
                !typePresent)
            {
                result.Type = (DiameterUserEquipmentInfoType)
                    ReadCreditControlInteger32Avp(child);
                typePresent = true;
            }
            else if (child.VendorId == DiameterVendorIds.Ietf &&
                child.Code == DiameterAvpCodes.UserEquipmentInfoValue &&
                !valuePresent)
            {
                result.Value = ReadCreditControlOctetsAvp(child);
                valuePresent = true;
            }
            else
            {
                throw new DiameterCreditControlException(
                    "User-Equipment-Info requires one type and one value.");
            }

            index = index + 1;
        }

        if (!typePresent || !valuePresent)
        {
            throw new DiameterCreditControlException(
                "User-Equipment-Info requires one type and one value.");
        }

        result.ValidateCreditControlUserEquipmentInformation();
        return result;
    }

    internal void ReadCreditControlEquipmentExtensionType(
        DiameterUserEquipmentInformation equipment,
        uint code)
    {
        equipment.Imei = false;
        switch (code)
        {
            case DiameterAvpCodes.UserEquipmentInfoImeisv:
                equipment.Type = DiameterUserEquipmentInfoType.Imeisv;
                return;
            case DiameterAvpCodes.UserEquipmentInfoImei:
                equipment.Type = DiameterUserEquipmentInfoType.Imeisv;
                equipment.Imei = true;
                return;
            case DiameterAvpCodes.UserEquipmentInfoMac:
                equipment.Type = DiameterUserEquipmentInfoType.Mac;
                return;
            case DiameterAvpCodes.UserEquipmentInfoEui64:
                equipment.Type = DiameterUserEquipmentInfoType.Eui64;
                return;
            case DiameterAvpCodes.UserEquipmentInfoModifiedEui64:
                equipment.Type =
                    DiameterUserEquipmentInfoType.ModifiedEui64;
                return;
            default:
                throw new DiameterCreditControlException(
                    "The user-equipment extension type is invalid.");
        }
    }

    internal DiameterServiceUnit ReadCreditControlServiceUnitAvp(
        DiameterAvp avp)
    {
        DiameterValueResult grouped = AvpDecoder.DecodeAvpSequence(avp.Data);
        if (grouped.Status != DiameterCodecStatus.Success)
        {
            throw new DiameterCreditControlException(
                "A service-unit grouped AVP is invalid.");
        }

        DiameterServiceUnit result = new DiameterServiceUnit();
        int index = 0;
        while (index < grouped.Avps.Count)
        {
            DiameterAvp child = grouped.Avps[index];
            if (child.VendorId == DiameterVendorIds.Ietf)
            {
                ReadCreditControlServiceUnitChild(child, result, avp.Code);
            }

            index = index + 1;
        }

        result.ValidateCreditControlServiceUnit();
        return result;
    }

    internal void ReadCreditControlServiceUnitChild(
        DiameterAvp avp,
        DiameterServiceUnit serviceUnit,
        uint parentCode)
    {
        if (avp.Code == DiameterAvpCodes.TariffTimeChange)
        {
            if (parentCode != DiameterAvpCodes.GrantedServiceUnit)
            {
                throw new DiameterCreditControlException(
                    "Tariff-Time-Change is only valid in Granted-Service-Unit.");
            }

            RequireCreditControlSingleAvp(
                serviceUnit.TariffChangeTimePresent,
                "Tariff-Time-Change");
            serviceUnit.TariffChangeTime = ReadCreditControlTimeAvp(avp);
            serviceUnit.TariffChangeTimePresent = true;
        }
        else if (avp.Code == DiameterAvpCodes.TariffChangeUsage)
        {
            if (parentCode != DiameterAvpCodes.UsedServiceUnit)
            {
                throw new DiameterCreditControlException(
                    "Tariff-Change-Usage is only valid in Used-Service-Unit.");
            }

            RequireCreditControlSingleAvp(
                serviceUnit.TariffChangeUsagePresent,
                "Tariff-Change-Usage");
            serviceUnit.TariffChangeUsage =
                (DiameterTariffChangeUsage)
                ReadCreditControlInteger32Avp(avp);
            serviceUnit.TariffChangeUsagePresent = true;
        }
        else if (avp.Code == DiameterAvpCodes.CcTime)
        {
            RequireCreditControlSingleAvp(
                serviceUnit.TimePresent,
                "CC-Time");
            serviceUnit.Time = ReadCreditControlUnsigned32Avp(avp);
            serviceUnit.TimePresent = true;
        }
        else if (avp.Code == DiameterAvpCodes.CcMoney)
        {
            if (serviceUnit.Money != null)
            {
                throw new DiameterCreditControlException(
                    "CC-Money occurs more than once.");
            }

            serviceUnit.Money = ReadCreditControlMoneyAvp(avp);
        }
        else if (avp.Code == DiameterAvpCodes.CcTotalOctets)
        {
            RequireCreditControlSingleAvp(
                serviceUnit.TotalOctetsPresent,
                "CC-Total-Octets");
            serviceUnit.TotalOctets =
                ReadCreditControlUnsigned64Avp(avp);
            serviceUnit.TotalOctetsPresent = true;
        }
        else if (avp.Code == DiameterAvpCodes.CcInputOctets)
        {
            RequireCreditControlSingleAvp(
                serviceUnit.InputOctetsPresent,
                "CC-Input-Octets");
            serviceUnit.InputOctets =
                ReadCreditControlUnsigned64Avp(avp);
            serviceUnit.InputOctetsPresent = true;
        }
        else if (avp.Code == DiameterAvpCodes.CcOutputOctets)
        {
            RequireCreditControlSingleAvp(
                serviceUnit.OutputOctetsPresent,
                "CC-Output-Octets");
            serviceUnit.OutputOctets =
                ReadCreditControlUnsigned64Avp(avp);
            serviceUnit.OutputOctetsPresent = true;
        }
        else if (avp.Code == DiameterAvpCodes.CcServiceSpecificUnits)
        {
            RequireCreditControlSingleAvp(
                serviceUnit.ServiceSpecificUnitsPresent,
                "CC-Service-Specific-Units");
            serviceUnit.ServiceSpecificUnits =
                ReadCreditControlUnsigned64Avp(avp);
            serviceUnit.ServiceSpecificUnitsPresent = true;
        }
    }

    internal DiameterCostInformation ReadCreditControlMoneyAvp(
        DiameterAvp avp)
    {
        DiameterValueResult grouped = AvpDecoder.DecodeAvpSequence(avp.Data);
        if (grouped.Status != DiameterCodecStatus.Success)
        {
            throw new DiameterCreditControlException(
                "A CC-Money grouped AVP is invalid.");
        }

        DiameterCostInformation result = new DiameterCostInformation();
        bool unitValuePresent = false;
        int index = 0;
        while (index < grouped.Avps.Count)
        {
            DiameterAvp child = grouped.Avps[index];
            if (child.VendorId == DiameterVendorIds.Ietf &&
                child.Code == DiameterAvpCodes.UnitValue)
            {
                RequireCreditControlSingleAvp(
                    unitValuePresent,
                    "Unit-Value");
                unitValuePresent = true;
                ReadCreditControlUnitValueAvp(child, result);
            }
            else if (child.VendorId == DiameterVendorIds.Ietf &&
                child.Code == DiameterAvpCodes.CurrencyCode)
            {
                RequireCreditControlSingleAvp(
                    result.CurrencyCodePresent,
                    "Currency-Code");
                result.CurrencyCode =
                    ReadCreditControlUnsigned32Avp(child);
                result.CurrencyCodePresent = true;
            }

            index = index + 1;
        }

        if (!unitValuePresent)
        {
            throw new DiameterCreditControlException(
                "CC-Money requires Unit-Value.");
        }

        result.ValidateCreditControlCostInformation();
        return result;
    }

    internal DiameterCostInformation ReadCreditControlCostInformationAvp(
        DiameterAvp avp)
    {
        DiameterValueResult grouped = AvpDecoder.DecodeAvpSequence(avp.Data);
        if (grouped.Status != DiameterCodecStatus.Success)
        {
            throw new DiameterCreditControlException(
                "A Cost-Information grouped AVP is invalid.");
        }

        DiameterCostInformation result = new DiameterCostInformation();
        bool unitValuePresent = false;
        int index = 0;
        while (index < grouped.Avps.Count)
        {
            DiameterAvp child = grouped.Avps[index];
            if (child.VendorId == DiameterVendorIds.Ietf &&
                child.Code == DiameterAvpCodes.UnitValue)
            {
                RequireCreditControlSingleAvp(
                    unitValuePresent,
                    "Cost Unit-Value");
                unitValuePresent = true;
                ReadCreditControlUnitValueAvp(child, result);
            }
            else if (child.VendorId == DiameterVendorIds.Ietf &&
                child.Code == DiameterAvpCodes.CurrencyCode)
            {
                RequireCreditControlSingleAvp(
                    result.CurrencyCodePresent,
                    "Cost Currency-Code");
                result.CurrencyCode =
                    ReadCreditControlUnsigned32Avp(child);
                result.CurrencyCodePresent = true;
            }
            else if (child.VendorId == DiameterVendorIds.Ietf &&
                child.Code == DiameterAvpCodes.CostUnit)
            {
                RequireCreditControlSingleAvp(
                    result.CostUnit.Length > 0,
                    "Cost-Unit");
                result.CostUnit = ReadCreditControlTextAvp(child);
            }

            index = index + 1;
        }

        if (!unitValuePresent || !result.CurrencyCodePresent)
        {
            throw new DiameterCreditControlException(
                "Cost-Information requires Unit-Value and Currency-Code.");
        }

        result.ValidateCreditControlCostInformation();
        return result;
    }

    internal void ReadCreditControlUnitValueAvp(
        DiameterAvp avp,
        DiameterCostInformation value)
    {
        DiameterValueResult grouped = AvpDecoder.DecodeAvpSequence(avp.Data);
        if (grouped.Status != DiameterCodecStatus.Success)
        {
            throw new DiameterCreditControlException(
                "A Unit-Value grouped AVP is invalid.");
        }

        bool digitsPresent = false;
        int index = 0;
        while (index < grouped.Avps.Count)
        {
            DiameterAvp child = grouped.Avps[index];
            if (child.VendorId == DiameterVendorIds.Ietf &&
                child.Code == DiameterAvpCodes.ValueDigits)
            {
                RequireCreditControlSingleAvp(
                    digitsPresent,
                    "Value-Digits");
                digitsPresent = true;
                value.ValueDigits = ReadCreditControlInteger64Avp(child);
            }
            else if (child.VendorId == DiameterVendorIds.Ietf &&
                child.Code == DiameterAvpCodes.Exponent)
            {
                RequireCreditControlSingleAvp(
                    value.ExponentPresent,
                    "Exponent");
                value.Exponent = ReadCreditControlInteger32Avp(child);
                value.ExponentPresent = true;
            }

            index = index + 1;
        }

        if (!digitsPresent)
        {
            throw new DiameterCreditControlException(
                "Unit-Value requires Value-Digits.");
        }
    }

    internal DiameterMultipleServiceCredit
        ReadCreditControlMultipleServiceAvp(DiameterAvp avp)
    {
        DiameterValueResult grouped = AvpDecoder.DecodeAvpSequence(avp.Data);
        if (grouped.Status != DiameterCodecStatus.Success)
        {
            throw new DiameterCreditControlException(
                "A Multiple-Services-Credit-Control AVP is invalid.");
        }

        DiameterMultipleServiceCredit result =
            new DiameterMultipleServiceCredit();
        int index = 0;
        while (index < grouped.Avps.Count)
        {
            DiameterAvp child = grouped.Avps[index];
            if (child.VendorId == DiameterVendorIds.Ietf)
            {
                ReadCreditControlMultipleServiceChild(child, result);
            }

            index = index + 1;
        }

        result.ValidateCreditControlMultipleServiceCredit();
        return result;
    }

    internal void ReadCreditControlMultipleServiceChild(
        DiameterAvp avp,
        DiameterMultipleServiceCredit credit)
    {
        if (avp.Code == DiameterAvpCodes.GrantedServiceUnit)
        {
            if (credit.GrantedUnit != null)
            {
                throw new DiameterCreditControlException(
                    "Grouped Granted-Service-Unit occurs more than once.");
            }

            credit.GrantedUnit = ReadCreditControlServiceUnitAvp(avp);
        }
        else if (avp.Code == DiameterAvpCodes.RequestedServiceUnit)
        {
            if (credit.RequestedUnit != null)
            {
                throw new DiameterCreditControlException(
                    "Grouped Requested-Service-Unit occurs more than once.");
            }

            credit.RequestedUnit = ReadCreditControlServiceUnitAvp(avp);
        }
        else if (avp.Code == DiameterAvpCodes.UsedServiceUnit)
        {
            credit.UsedUnits.Add(ReadCreditControlServiceUnitAvp(avp));
        }
        else if (avp.Code == DiameterAvpCodes.ServiceIdentifier)
        {
            credit.ServiceIdentifiers.Add(
                ReadCreditControlUnsigned32Avp(avp));
        }
        else if (avp.Code == DiameterAvpCodes.RatingGroup)
        {
            RequireCreditControlSingleAvp(
                credit.RatingGroupPresent,
                "Rating-Group");
            credit.RatingGroup = ReadCreditControlUnsigned32Avp(avp);
            credit.RatingGroupPresent = true;
        }
        else if (avp.Code == DiameterAvpCodes.GsuPoolReference)
        {
            credit.PoolReferences.Add(
                ReadCreditControlPoolReferenceAvp(avp));
        }
        else if (avp.Code == DiameterAvpCodes.ResultCode)
        {
            RequireCreditControlSingleAvp(
                credit.ResultCodePresent,
                "Grouped Result-Code");
            credit.ResultCode = ReadCreditControlUnsigned32Avp(avp);
            credit.ResultCodePresent = true;
        }
        else if (avp.Code == DiameterAvpCodes.TariffChangeUsage)
        {
            RequireCreditControlSingleAvp(
                credit.TariffChangeUsagePresent,
                "Grouped Tariff-Change-Usage");
            credit.TariffChangeUsage =
                (DiameterTariffChangeUsage)
                ReadCreditControlInteger32Avp(avp);
            credit.TariffChangeUsagePresent = true;
        }
        else if (avp.Code == DiameterAvpCodes.ValidityTime)
        {
            RequireCreditControlSingleAvp(
                credit.ValidityTimePresent,
                "Grouped Validity-Time");
            uint seconds = ReadCreditControlUnsigned32Avp(avp);
            if (seconds == 0U || seconds > (uint)(int.MaxValue / 1000))
            {
                throw new DiameterCreditControlException(
                    "Grouped Validity-Time exceeds the supported duration.");
            }

            credit.ValidityTimeMilliseconds = (int)seconds * 1000;
            credit.ValidityTimePresent = true;
        }
        else if (avp.Code == DiameterAvpCodes.FinalUnitIndication)
        {
            if (credit.FinalUnitInstruction != null)
            {
                throw new DiameterCreditControlException(
                    "Grouped Final-Unit-Indication occurs more than once.");
            }

            credit.FinalUnitInstruction =
                ReadCreditControlFinalUnitAvp(avp, false);
        }
        else if (avp.Code == DiameterAvpCodes.QosFinalUnitIndication)
        {
            if (credit.QosFinalUnitInstruction != null)
            {
                throw new DiameterCreditControlException(
                    "Grouped QoS-Final-Unit-Indication occurs more than once.");
            }

            credit.QosFinalUnitInstruction =
                ReadCreditControlFinalUnitAvp(avp, true);
        }
    }

    internal DiameterFinalUnitInstruction ReadCreditControlFinalUnitAvp(
        DiameterAvp avp,
        bool qos)
    {
        DiameterValueResult grouped = AvpDecoder.DecodeAvpSequence(avp.Data);
        if (grouped.Status != DiameterCodecStatus.Success)
        {
            throw new DiameterCreditControlException(
                "A final-unit grouped AVP is invalid.");
        }

        CreditControlFinalUnitParseState state =
            new CreditControlFinalUnitParseState();
        state.Instruction.Qos = qos;
        int index = 0;
        while (index < grouped.Avps.Count)
        {
            DiameterAvp child = grouped.Avps[index];
            if (child.VendorId == DiameterVendorIds.Ietf)
            {
                ReadCreditControlFinalUnitChild(state, child, qos);
            }

            index = index + 1;
        }

        if (!state.ActionPresent)
        {
            throw new DiameterCreditControlException(
                "Final-Unit-Indication requires Final-Unit-Action.");
        }

        state.Instruction.ValidateCreditControlFinalUnitInstruction();
        return state.Instruction;
    }

    internal void ReadCreditControlFinalUnitChild(
        CreditControlFinalUnitParseState state,
        DiameterAvp avp,
        bool qos)
    {
        if (avp.Code == DiameterAvpCodes.FinalUnitAction)
        {
            RequireCreditControlSingleAvp(
                state.ActionPresent,
                "Final-Unit-Action");
            state.Instruction.Action = (DiameterFinalUnitAction)
                ReadCreditControlInteger32Avp(avp);
            state.ActionPresent = true;
        }
        else if (!qos &&
            avp.Code == DiameterAvpCodes.RestrictionFilterRule)
        {
            DiameterValueResult decoded = Codec.DecodeIpFilterRule(
                avp.Data);
            if (decoded.Status != DiameterCodecStatus.Success)
            {
                throw new DiameterCreditControlException(
                    "A Restriction-Filter-Rule is invalid.");
            }

            state.Instruction.RestrictionFilterRules.Add(decoded.Text);
        }
        else if (qos && avp.Code == DiameterAvpCodes.FilterRule)
        {
            DiameterValueResult decoded = Codec.DecodeQosFilterRule(
                avp.Data);
            if (decoded.Status != DiameterCodecStatus.Success)
            {
                throw new DiameterCreditControlException(
                    "A QoS Filter-Rule is invalid.");
            }

            state.Instruction.RestrictionFilterRules.Add(decoded.Text);
        }
        else if (avp.Code == DiameterAvpCodes.FilterId)
        {
            state.Instruction.FilterIdentifiers.Add(
                ReadCreditControlTextAvp(avp));
        }
        else if (!qos && avp.Code == DiameterAvpCodes.RedirectServer)
        {
            RequireCreditControlSingleAvp(
                state.RedirectPresent,
                "Redirect-Server");
            ReadCreditControlLegacyRedirectAvp(
                state.Instruction,
                avp);
            state.RedirectPresent = true;
        }
        else if (qos &&
            avp.Code == DiameterAvpCodes.RedirectServerExtension)
        {
            RequireCreditControlSingleAvp(
                state.RedirectPresent,
                "Redirect-Server-Extension");
            ReadCreditControlRedirectExtensionAvp(
                state.Instruction,
                avp);
            state.RedirectPresent = true;
        }
    }

    internal void ReadCreditControlLegacyRedirectAvp(
        DiameterFinalUnitInstruction instruction,
        DiameterAvp avp)
    {
        DiameterValueResult grouped = AvpDecoder.DecodeAvpSequence(avp.Data);
        if (grouped.Status != DiameterCodecStatus.Success)
        {
            throw new DiameterCreditControlException(
                "Redirect-Server is invalid.");
        }

        bool typePresent = false;
        bool addressPresent = false;
        int index = 0;
        while (index < grouped.Avps.Count)
        {
            DiameterAvp child = grouped.Avps[index];
            if (child.VendorId == DiameterVendorIds.Ietf &&
                child.Code == DiameterAvpCodes.RedirectAddressType)
            {
                RequireCreditControlSingleAvp(
                    typePresent,
                    "Redirect-Address-Type");
                instruction.RedirectAddressType =
                    (DiameterRedirectAddressType)
                    ReadCreditControlInteger32Avp(child);
                instruction.RedirectAddressTypePresent = true;
                typePresent = true;
            }
            else if (child.VendorId == DiameterVendorIds.Ietf &&
                child.Code == DiameterAvpCodes.RedirectServerAddress)
            {
                RequireCreditControlSingleAvp(
                    addressPresent,
                    "Redirect-Server-Address");
                instruction.RedirectAddress =
                    ReadCreditControlTextAvp(child);
                addressPresent = true;
            }

            index = index + 1;
        }

        if (!typePresent || !addressPresent)
        {
            throw new DiameterCreditControlException(
                "Redirect-Server requires an address and type.");
        }
    }

    internal void ReadCreditControlRedirectExtensionAvp(
        DiameterFinalUnitInstruction instruction,
        DiameterAvp avp)
    {
        DiameterValueResult grouped = AvpDecoder.DecodeAvpSequence(avp.Data);
        if (grouped.Status != DiameterCodecStatus.Success)
        {
            throw new DiameterCreditControlException(
                "Redirect-Server-Extension is invalid.");
        }

        int addressCount = 0;
        int index = 0;
        while (index < grouped.Avps.Count)
        {
            DiameterAvp child = grouped.Avps[index];
            if (child.VendorId == DiameterVendorIds.Ietf &&
                (child.Code ==
                    DiameterAvpCodes.RedirectAddressIpAddress ||
                child.Code == DiameterAvpCodes.RedirectAddressUrl ||
                child.Code == DiameterAvpCodes.RedirectAddressSipUri))
            {
                addressCount = addressCount + 1;
                if (addressCount > 1)
                {
                    throw new DiameterCreditControlException(
                        "Redirect-Server-Extension has multiple addresses.");
                }

                if (child.Code ==
                    DiameterAvpCodes.RedirectAddressIpAddress)
                {
                    DiameterValueResult decoded = Codec.DecodeAddress(
                        child.Data);
                    if (decoded.Status != DiameterCodecStatus.Success ||
                        decoded.Address == null)
                    {
                        throw new DiameterCreditControlException(
                            "The redirect IP address is invalid.");
                    }

                    instruction.RedirectAddress =
                        decoded.Address.ToString();
                }
                else
                {
                    instruction.RedirectAddress =
                        ReadCreditControlTextAvp(child);
                }
            }

            index = index + 1;
        }

        if (addressCount != 1)
        {
            throw new DiameterCreditControlException(
                "Redirect-Server-Extension requires one address.");
        }

        instruction.RedirectExtension = true;
    }

    internal DiameterPoolReference ReadCreditControlPoolReferenceAvp(
        DiameterAvp avp)
    {
        DiameterValueResult grouped = AvpDecoder.DecodeAvpSequence(avp.Data);
        if (grouped.Status != DiameterCodecStatus.Success)
        {
            throw new DiameterCreditControlException(
                "A G-S-U-Pool-Reference AVP is invalid.");
        }

        DiameterPoolReference result = new DiameterPoolReference();
        bool poolPresent = false;
        bool typePresent = false;
        bool valuePresent = false;
        int index = 0;
        while (index < grouped.Avps.Count)
        {
            DiameterAvp child = grouped.Avps[index];
            if (child.VendorId == DiameterVendorIds.Ietf &&
                child.Code == DiameterAvpCodes.GsuPoolIdentifier)
            {
                RequireCreditControlSingleAvp(
                    poolPresent,
                    "G-S-U-Pool-Identifier");
                poolPresent = true;
                result.PoolIdentifier =
                    ReadCreditControlUnsigned32Avp(child);
            }
            else if (child.VendorId == DiameterVendorIds.Ietf &&
                child.Code == DiameterAvpCodes.CcUnitType)
            {
                RequireCreditControlSingleAvp(
                    typePresent,
                    "CC-Unit-Type");
                typePresent = true;
                result.UnitType = (DiameterCreditControlUnitType)
                    ReadCreditControlInteger32Avp(child);
            }
            else if (child.VendorId == DiameterVendorIds.Ietf &&
                child.Code == DiameterAvpCodes.UnitValue)
            {
                RequireCreditControlSingleAvp(
                    valuePresent,
                    "Pool Unit-Value");
                valuePresent = true;
                DiameterCostInformation value =
                    new DiameterCostInformation();
                ReadCreditControlUnitValueAvp(child, value);
                result.ValueDigits = value.ValueDigits;
                result.Exponent = value.Exponent;
                result.ExponentPresent = value.ExponentPresent;
            }

            index = index + 1;
        }

        if (!poolPresent || !typePresent || !valuePresent)
        {
            throw new DiameterCreditControlException(
                "A pool reference requires identifier, unit type, and value.");
        }

        result.ValidateCreditControlPoolReference();
        return result;
    }
}
