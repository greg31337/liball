using System;
using System.Collections.Generic;

namespace diameter;

internal static class CreditControlDataCloner
{
    internal static DiameterCreditControlRequest CloneCreditControlRequest(
        DiameterCreditControlRequest source)
    {
        DiameterCreditControlRequest result =
            new DiameterCreditControlRequest();
        result.SessionId = source.SessionId;
        result.DestinationRealm = CloneCreditControlIdentity(
            source.DestinationRealm);
        result.DestinationHost = CloneOptionalCreditControlIdentity(
            source.DestinationHost);
        result.PeerIdentity = CloneOptionalCreditControlIdentity(
            source.PeerIdentity);
        result.ServiceContextId = source.ServiceContextId;
        result.UserName = source.UserName;
        result.RequestType = source.RequestType;
        result.RequestNumber = source.RequestNumber;
        result.CreditControlSubSessionId =
            source.CreditControlSubSessionId;
        result.CreditControlSubSessionIdPresent =
            source.CreditControlSubSessionIdPresent;
        result.AccountingMultiSessionId = source.AccountingMultiSessionId;
        result.EventTimestamp = source.EventTimestamp;
        result.EventTimestampPresent = source.EventTimestampPresent;
        result.OriginStateIdentifier = source.OriginStateIdentifier;
        result.OriginStateIdentifierPresent =
            source.OriginStateIdentifierPresent;
        result.ServiceIdentifiers = CloneCreditControlUnsigned32Values(
            source.ServiceIdentifiers);
        result.TerminationCause = source.TerminationCause;
        result.TerminationCausePresent = source.TerminationCausePresent;
        result.RequestedServiceUnit = CloneOptionalCreditControlServiceUnit(
            source.RequestedServiceUnit);
        result.QuotaThreshold = CloneOptionalCreditControlServiceUnit(
            source.QuotaThreshold);
        result.RequireFinalUsageReport = source.RequireFinalUsageReport;
        result.RequestedAction = source.RequestedAction;
        result.RequestedActionPresent = source.RequestedActionPresent;
        result.UsedServiceUnits = CloneCreditControlServiceUnits(
            source.UsedServiceUnits);
        result.MultipleServicesIndicator = source.MultipleServicesIndicator;
        result.MultipleServicesIndicatorPresent =
            source.MultipleServicesIndicatorPresent;
        result.MultipleServiceCredits = CloneCreditControlMultipleCredits(
            source.MultipleServiceCredits);
        result.ServiceParameterAvps = CloneCreditControlAvps(
            source.ServiceParameterAvps);
        result.CorrelationId = CloneCreditControlBytes(source.CorrelationId);
        result.SubscriptionIdentifiers =
            CloneCreditControlSubscriptionIdentifiers(
                source.SubscriptionIdentifiers);
        result.UserEquipmentInformation =
            CloneOptionalCreditControlEquipment(
                source.UserEquipmentInformation);
        result.ServiceAvps = CloneCreditControlAvps(source.ServiceAvps);
        result.TimeoutMilliseconds = source.TimeoutMilliseconds;
        result.AllowFailover = source.AllowFailover;
        result.AlternateServers = CloneCreditControlServers(
            source.AlternateServers);
        result.FailureHandling = source.FailureHandling;
        result.DirectDebitFailureHandling =
            source.DirectDebitFailureHandling;
        result.RouteIdentifier = source.RouteIdentifier;
        result.CorrelationText = source.CorrelationText;
        result.ApplicationState = CloneCreditControlBytes(
            source.ApplicationState);
        return result;
    }

    internal static DiameterCreditControlAnswer CloneCreditControlAnswer(
        DiameterCreditControlAnswer source)
    {
        DiameterCreditControlAnswer result =
            new DiameterCreditControlAnswer();
        result.SessionId = source.SessionId;
        result.RequestType = source.RequestType;
        result.RequestNumber = source.RequestNumber;
        result.ResultCode = source.ResultCode;
        result.UserName = source.UserName;
        result.SessionFailoverSupported = source.SessionFailoverSupported;
        result.SessionFailoverPresent = source.SessionFailoverPresent;
        result.CreditControlSubSessionId =
            source.CreditControlSubSessionId;
        result.CreditControlSubSessionIdPresent =
            source.CreditControlSubSessionIdPresent;
        result.AccountingMultiSessionId = source.AccountingMultiSessionId;
        result.EventTimestamp = source.EventTimestamp;
        result.EventTimestampPresent = source.EventTimestampPresent;
        result.OriginStateIdentifier = source.OriginStateIdentifier;
        result.OriginStateIdentifierPresent =
            source.OriginStateIdentifierPresent;
        result.GrantedServiceUnit = CloneOptionalCreditControlServiceUnit(
            source.GrantedServiceUnit);
        result.MultipleServiceCredits = CloneCreditControlMultipleCredits(
            source.MultipleServiceCredits);
        result.CostInformation = CloneOptionalCreditControlCost(
            source.CostInformation);
        result.FinalUnitInstruction = CloneOptionalCreditControlFinalUnit(
            source.FinalUnitInstruction);
        result.QosFinalUnitInstruction = CloneOptionalCreditControlFinalUnit(
            source.QosFinalUnitInstruction);
        result.CheckBalanceResult = source.CheckBalanceResult;
        result.CheckBalanceResultPresent = source.CheckBalanceResultPresent;
        result.FailureHandling = source.FailureHandling;
        result.FailureHandlingPresent = source.FailureHandlingPresent;
        result.DirectDebitFailureHandling =
            source.DirectDebitFailureHandling;
        result.DirectDebitFailureHandlingPresent =
            source.DirectDebitFailureHandlingPresent;
        result.ValidityTimeMilliseconds = source.ValidityTimeMilliseconds;
        result.ValidityTimePresent = source.ValidityTimePresent;
        result.RedirectHosts = CloneCreditControlUris(source.RedirectHosts);
        result.RedirectHostUsage = source.RedirectHostUsage;
        result.RedirectHostUsagePresent = source.RedirectHostUsagePresent;
        result.RedirectMaxCacheTimeMilliseconds =
            source.RedirectMaxCacheTimeMilliseconds;
        result.RedirectMaxCacheTimePresent =
            source.RedirectMaxCacheTimePresent;
        result.Disposition = source.Disposition;
        result.Message = CloneOptionalCreditControlMessage(source.Message);
        result.ServiceAvps = CloneCreditControlAvps(source.ServiceAvps);
        result.FailedAvps = CloneCreditControlAvps(source.FailedAvps);
        return result;
    }

    internal static DiameterCreditControlSession CloneCreditControlSession(
        DiameterCreditControlSession source)
    {
        DiameterCreditControlSession result =
            new DiameterCreditControlSession();
        result.SessionId = source.SessionId;
        result.Version = source.Version;
        result.Role = source.Role;
        result.State = source.State;
        result.ServiceContextId = source.ServiceContextId;
        result.DestinationRealm = CloneOptionalCreditControlIdentity(
            source.DestinationRealm);
        result.DestinationHost = CloneOptionalCreditControlIdentity(
            source.DestinationHost);
        result.PeerIdentity = CloneOptionalCreditControlIdentity(
            source.PeerIdentity);
        result.UserName = source.UserName;
        result.NextRequestNumber = source.NextRequestNumber;
        result.PendingRequestType = source.PendingRequestType;
        result.PendingRequestNumber = source.PendingRequestNumber;
        result.PendingRequestPresent = source.PendingRequestPresent;
        result.PendingRequestIdentifier = source.PendingRequestIdentifier;
        result.LastCompletedRequestIdentifier =
            source.LastCompletedRequestIdentifier;
        result.CreditControlSubSessionId =
            source.CreditControlSubSessionId;
        result.CreditControlSubSessionIdPresent =
            source.CreditControlSubSessionIdPresent;
        result.AccountingMultiSessionId = source.AccountingMultiSessionId;
        result.CorrelationId = CloneCreditControlBytes(source.CorrelationId);
        result.RequestedServiceUnit = CloneOptionalCreditControlServiceUnit(
            source.RequestedServiceUnit);
        result.GrantedServiceUnit = CloneOptionalCreditControlServiceUnit(
            source.GrantedServiceUnit);
        result.UsedServiceUnit = CloneOptionalCreditControlServiceUnit(
            source.UsedServiceUnit);
        result.QuotaThreshold = CloneOptionalCreditControlServiceUnit(
            source.QuotaThreshold);
        result.MultipleServiceCredits = CloneCreditControlMultipleCredits(
            source.MultipleServiceCredits);
        result.MultipleServicesIndicator =
            source.MultipleServicesIndicator;
        result.MultipleServicesIndicatorPresent =
            source.MultipleServicesIndicatorPresent;
        result.PoolReferences = CloneCreditControlPoolReferences(
            source.PoolReferences);
        result.FinalUnitInstruction = CloneOptionalCreditControlFinalUnit(
            source.FinalUnitInstruction);
        result.QosFinalUnitInstruction =
            CloneOptionalCreditControlFinalUnit(
                source.QosFinalUnitInstruction);
        result.AppliedFinalUnitInstruction =
            CloneOptionalCreditControlFinalUnit(
                source.AppliedFinalUnitInstruction);
        result.GracefulTerminationRequired =
            source.GracefulTerminationRequired;
        result.CostInformation = CloneOptionalCreditControlCost(
            source.CostInformation);
        result.SubscriptionIdentifiers =
            CloneCreditControlSubscriptionIdentifiers(
                source.SubscriptionIdentifiers);
        result.UserEquipmentInformation =
            CloneOptionalCreditControlEquipment(
                source.UserEquipmentInformation);
        result.FailureHandling = source.FailureHandling;
        result.DirectDebitFailureHandling =
            source.DirectDebitFailureHandling;
        result.SessionFailoverSupported = source.SessionFailoverSupported;
        result.PrimaryServer = CloneOptionalCreditControlServer(
            source.PrimaryServer);
        result.AlternateServers = CloneCreditControlServers(
            source.AlternateServers);
        result.AttemptedServers = CloneCreditControlTexts(
            source.AttemptedServers);
        result.FailoverActive = source.FailoverActive;
        result.Disposition = source.Disposition;
        result.RouteIdentifier = source.RouteIdentifier;
        result.PeerOriginStateIdentifier =
            source.PeerOriginStateIdentifier;
        result.PeerOriginStateIdentifierPresent =
            source.PeerOriginStateIdentifierPresent;
        result.CreatedAt = source.CreatedAt;
        result.UpdatedAt = source.UpdatedAt;
        result.TransactionDeadline = source.TransactionDeadline;
        result.TransactionDeadlinePresent =
            source.TransactionDeadlinePresent;
        result.ValidityDeadline = source.ValidityDeadline;
        result.ValidityDeadlinePresent = source.ValidityDeadlinePresent;
        result.TariffChangeTime = source.TariffChangeTime;
        result.TariffChangeTimePresent = source.TariffChangeTimePresent;
        result.NextInterrogationTime = source.NextInterrogationTime;
        result.NextInterrogationTimePresent =
            source.NextInterrogationTimePresent;
        result.QuotaThresholdReached = source.QuotaThresholdReached;
        result.QuotaExhausted = source.QuotaExhausted;
        result.FinalUsageReportRequired =
            source.FinalUsageReportRequired;
        result.ReauthorizationRequested =
            source.ReauthorizationRequested;
        result.ReAuthRequestType = source.ReAuthRequestType;
        result.LastRequest = CloneOptionalCreditControlRequest(
            source.LastRequest);
        result.LastAnswer = CloneOptionalCreditControlAnswer(
            source.LastAnswer);
        result.LastOperationResult = CloneOptionalCreditControlResult(
            source.LastOperationResult);
        result.AttemptCount = source.AttemptCount;
        result.ApplicationState = CloneCreditControlBytes(
            source.ApplicationState);
        result.Code = source.Code;
        result.Message = source.Message;
        return result;
    }

    internal static DiameterCreditControlResult CloneCreditControlResult(
        DiameterCreditControlResult source)
    {
        DiameterCreditControlResult result =
            CloneCreditControlResultWithoutSession(source);
        if (source.Session != null)
        {
            result.Session = CloneCreditControlSession(source.Session);
        }

        return result;
    }

    internal static DiameterCreditControlResult
        CloneCreditControlResultWithoutSession(
            DiameterCreditControlResult source)
    {
        DiameterCreditControlResult result =
            new DiameterCreditControlResult();
        result.Request = CloneCreditControlRequest(source.Request);
        result.Answer = CloneOptionalCreditControlAnswer(source.Answer);
        result.State = source.State;
        result.Disposition = source.Disposition;
        result.Accepted = source.Accepted;
        result.Delivered = source.Delivered;
        result.AnswerReceived = source.AnswerReceived;
        result.Successful = source.Successful;
        result.Duplicate = source.Duplicate;
        result.Retransmission = source.Retransmission;
        result.FailoverAttempted = source.FailoverAttempted;
        result.RetryRequired = source.RetryRequired;
        result.BufferRequest = source.BufferRequest;
        result.ContinueService = source.ContinueService;
        result.TerminateService = source.TerminateService;
        result.QuotaExhausted = source.QuotaExhausted;
        result.QuotaThresholdReached = source.QuotaThresholdReached;
        result.FinalUnit = source.FinalUnit;
        result.AppliedFinalUnitInstruction =
            CloneOptionalCreditControlFinalUnit(
                source.AppliedFinalUnitInstruction);
        result.GracefulTerminationRequired =
            source.GracefulTerminationRequired;
        result.ResultCode = source.ResultCode;
        result.Code = source.Code;
        result.Message = source.Message;
        return result;
    }

    internal static DiameterCreditControlRequest?
        CloneOptionalCreditControlRequest(
            DiameterCreditControlRequest? source)
    {
        if (source == null)
        {
            return null;
        }

        return CloneCreditControlRequest(source);
    }

    internal static DiameterCreditControlAnswer?
        CloneOptionalCreditControlAnswer(
            DiameterCreditControlAnswer? source)
    {
        if (source == null)
        {
            return null;
        }

        return CloneCreditControlAnswer(source);
    }

    internal static DiameterCreditControlResult?
        CloneOptionalCreditControlResult(
            DiameterCreditControlResult? source)
    {
        if (source == null)
        {
            return null;
        }

        return CloneCreditControlResultWithoutSession(source);
    }

    internal static DiameterIdentity CloneCreditControlIdentity(
        DiameterIdentity source)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = source.Value;
        return result;
    }

    internal static DiameterIdentity? CloneOptionalCreditControlIdentity(
        DiameterIdentity? source)
    {
        if (source == null)
        {
            return null;
        }

        return CloneCreditControlIdentity(source);
    }

    internal static DiameterCreditControlServer CloneCreditControlServer(
        DiameterCreditControlServer source)
    {
        DiameterCreditControlServer result =
            new DiameterCreditControlServer();
        result.DestinationHost = CloneOptionalCreditControlIdentity(
            source.DestinationHost);
        result.PeerIdentity = CloneOptionalCreditControlIdentity(
            source.PeerIdentity);
        result.RouteIdentifier = source.RouteIdentifier;
        return result;
    }

    internal static DiameterCreditControlServer?
        CloneOptionalCreditControlServer(
            DiameterCreditControlServer? source)
    {
        if (source == null)
        {
            return null;
        }

        return CloneCreditControlServer(source);
    }

    internal static List<DiameterCreditControlServer>
        CloneCreditControlServers(
            List<DiameterCreditControlServer> source)
    {
        List<DiameterCreditControlServer> result =
            new List<DiameterCreditControlServer>();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(CloneCreditControlServer(source[index]));
            index = index + 1;
        }

        return result;
    }

    internal static DiameterMessage? CloneOptionalCreditControlMessage(
        DiameterMessage? source)
    {
        if (source == null)
        {
            return null;
        }

        return CreditControlCommandCodec.CloneCreditControlMessage(source);
    }

    internal static List<DiameterAvp> CloneCreditControlAvps(
        List<DiameterAvp> source)
    {
        List<DiameterAvp> result = new List<DiameterAvp>();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(CreditControlCommandCodec.CloneCreditControlAvp(
                source[index]));
            index = index + 1;
        }

        return result;
    }

    internal static byte[] CloneCreditControlBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        Array.Copy(source, result, source.Length);
        return result;
    }

    internal static List<uint> CloneCreditControlUnsigned32Values(
        List<uint> source)
    {
        List<uint> result = new List<uint>();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(source[index]);
            index = index + 1;
        }

        return result;
    }

    internal static DiameterServiceUnit? CloneOptionalCreditControlServiceUnit(
        DiameterServiceUnit? source)
    {
        if (source == null)
        {
            return null;
        }

        DiameterServiceUnit result = new DiameterServiceUnit();
        result.Time = source.Time;
        result.TimePresent = source.TimePresent;
        result.Money = CloneOptionalCreditControlCost(source.Money);
        result.TotalOctets = source.TotalOctets;
        result.TotalOctetsPresent = source.TotalOctetsPresent;
        result.InputOctets = source.InputOctets;
        result.InputOctetsPresent = source.InputOctetsPresent;
        result.OutputOctets = source.OutputOctets;
        result.OutputOctetsPresent = source.OutputOctetsPresent;
        result.ServiceSpecificUnits = source.ServiceSpecificUnits;
        result.ServiceSpecificUnitsPresent =
            source.ServiceSpecificUnitsPresent;
        result.TariffChangeTime = source.TariffChangeTime;
        result.TariffChangeTimePresent = source.TariffChangeTimePresent;
        result.TariffChangeUsage = source.TariffChangeUsage;
        result.TariffChangeUsagePresent = source.TariffChangeUsagePresent;
        return result;
    }

    internal static List<DiameterServiceUnit> CloneCreditControlServiceUnits(
        List<DiameterServiceUnit> source)
    {
        List<DiameterServiceUnit> result = new List<DiameterServiceUnit>();
        int index = 0;
        while (index < source.Count)
        {
            DiameterServiceUnit? unit =
                CloneOptionalCreditControlServiceUnit(source[index]);
            if (unit != null)
            {
                result.Add(unit);
            }

            index = index + 1;
        }

        return result;
    }

    internal static DiameterCostInformation? CloneOptionalCreditControlCost(
        DiameterCostInformation? source)
    {
        if (source == null)
        {
            return null;
        }

        DiameterCostInformation result = new DiameterCostInformation();
        result.ValueDigits = source.ValueDigits;
        result.Exponent = source.Exponent;
        result.ExponentPresent = source.ExponentPresent;
        result.CurrencyCode = source.CurrencyCode;
        result.CurrencyCodePresent = source.CurrencyCodePresent;
        result.CostUnit = source.CostUnit;
        return result;
    }

    internal static List<DiameterMultipleServiceCredit>
        CloneCreditControlMultipleCredits(
            List<DiameterMultipleServiceCredit> source)
    {
        List<DiameterMultipleServiceCredit> result =
            new List<DiameterMultipleServiceCredit>();
        int index = 0;
        while (index < source.Count)
        {
            DiameterMultipleServiceCredit item = source[index];
            DiameterMultipleServiceCredit copy =
                new DiameterMultipleServiceCredit();
            copy.QuotaThreshold =
                CloneOptionalCreditControlServiceUnit(
                    item.QuotaThreshold);
            copy.ServiceIdentifiers = CloneCreditControlUnsigned32Values(
                item.ServiceIdentifiers);
            copy.RatingGroup = item.RatingGroup;
            copy.RatingGroupPresent = item.RatingGroupPresent;
            copy.RequestedUnit =
                CloneOptionalCreditControlServiceUnit(
                    item.RequestedUnit);
            copy.GrantedUnit =
                CloneOptionalCreditControlServiceUnit(
                    item.GrantedUnit);
            copy.UsedUnits = CloneCreditControlServiceUnits(
                item.UsedUnits);
            copy.PoolReferences = CloneCreditControlPoolReferences(
                item.PoolReferences);
            copy.ValidityTimeMilliseconds =
                item.ValidityTimeMilliseconds;
            copy.ValidityTimePresent = item.ValidityTimePresent;
            copy.ValidityDeadline = item.ValidityDeadline;
            copy.ValidityDeadlinePresent =
                item.ValidityDeadlinePresent;
            copy.FinalUnitInstruction =
                CloneOptionalCreditControlFinalUnit(
                    item.FinalUnitInstruction);
            copy.QosFinalUnitInstruction =
                CloneOptionalCreditControlFinalUnit(
                    item.QosFinalUnitInstruction);
            copy.TariffChangeUsage = item.TariffChangeUsage;
            copy.TariffChangeUsagePresent =
                item.TariffChangeUsagePresent;
            copy.ResultCode = item.ResultCode;
            copy.ResultCodePresent = item.ResultCodePresent;
            copy.Disposition = item.Disposition;
            result.Add(copy);
            index = index + 1;
        }

        return result;
    }

    internal static List<DiameterPoolReference> CloneCreditControlPoolReferences(
        List<DiameterPoolReference> source)
    {
        List<DiameterPoolReference> result =
            new List<DiameterPoolReference>();
        int index = 0;
        while (index < source.Count)
        {
            DiameterPoolReference item = source[index];
            DiameterPoolReference copy = new DiameterPoolReference();
            copy.PoolIdentifier = item.PoolIdentifier;
            copy.UnitType = item.UnitType;
            copy.ValueDigits = item.ValueDigits;
            copy.Exponent = item.Exponent;
            copy.ExponentPresent = item.ExponentPresent;
            result.Add(copy);
            index = index + 1;
        }

        return result;
    }

    internal static DiameterFinalUnitInstruction?
        CloneOptionalCreditControlFinalUnit(
            DiameterFinalUnitInstruction? source)
    {
        if (source == null)
        {
            return null;
        }

        DiameterFinalUnitInstruction result =
            new DiameterFinalUnitInstruction();
        result.Action = source.Action;
        result.RestrictionFilterRules = CloneCreditControlTexts(
            source.RestrictionFilterRules);
        result.FilterIdentifiers = CloneCreditControlTexts(
            source.FilterIdentifiers);
        result.RedirectAddressType = source.RedirectAddressType;
        result.RedirectAddressTypePresent =
            source.RedirectAddressTypePresent;
        result.RedirectAddress = source.RedirectAddress;
        result.RedirectExtension = source.RedirectExtension;
        result.Qos = source.Qos;
        return result;
    }

    internal static List<string> CloneCreditControlTexts(List<string> source)
    {
        List<string> result = new List<string>();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(source[index]);
            index = index + 1;
        }

        return result;
    }

    internal static List<DiameterSubscriptionIdentifier>
        CloneCreditControlSubscriptionIdentifiers(
            List<DiameterSubscriptionIdentifier> source)
    {
        List<DiameterSubscriptionIdentifier> result =
            new List<DiameterSubscriptionIdentifier>();
        int index = 0;
        while (index < source.Count)
        {
            DiameterSubscriptionIdentifier item = source[index];
            DiameterSubscriptionIdentifier copy =
                new DiameterSubscriptionIdentifier();
            copy.Type = item.Type;
            copy.Data = item.Data;
            copy.Extension = item.Extension;
            result.Add(copy);
            index = index + 1;
        }

        return result;
    }

    internal static DiameterUserEquipmentInformation?
        CloneOptionalCreditControlEquipment(
            DiameterUserEquipmentInformation? source)
    {
        if (source == null)
        {
            return null;
        }

        DiameterUserEquipmentInformation result =
            new DiameterUserEquipmentInformation();
        result.Type = source.Type;
        result.Value = CloneCreditControlBytes(source.Value);
        result.Extension = source.Extension;
        result.Imei = source.Imei;
        return result;
    }

    internal static List<DiameterUri> CloneCreditControlUris(
        List<DiameterUri> source)
    {
        List<DiameterUri> result = new List<DiameterUri>();
        int index = 0;
        while (index < source.Count)
        {
            DiameterUri item = source[index];
            DiameterUri copy = new DiameterUri();
            copy.Scheme = item.Scheme;
            copy.Host = CloneCreditControlIdentity(item.Host);
            copy.Port = item.Port;
            copy.Protocol = item.Protocol;
            copy.Transport = item.Transport;
            result.Add(copy);
            index = index + 1;
        }

        return result;
    }
}
