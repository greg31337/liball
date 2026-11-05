using System;
using System.Collections.Generic;

namespace diameter;

internal static class AccountingRecordCloner
{
    internal static DiameterAccountingRequest CloneAccountingRequest(
        DiameterAccountingRequest source)
    {
        DiameterAccountingRequest result =
            new DiameterAccountingRequest();
        result.SessionId = source.SessionId;
        result.ApplicationId = source.ApplicationId;
        result.DestinationRealm = CloneAccountingIdentity(
            source.DestinationRealm);
        result.DestinationHost = CloneOptionalAccountingIdentity(
            source.DestinationHost);
        result.PeerIdentity = CloneOptionalAccountingIdentity(
            source.PeerIdentity);
        result.UserName = source.UserName;
        result.RecordType = source.RecordType;
        result.RecordNumber = source.RecordNumber;
        result.AccountingSubSessionId = source.AccountingSubSessionId;
        result.AccountingSubSessionIdPresent =
            source.AccountingSubSessionIdPresent;
        result.AccountingSessionId = CloneAccountingBytes(
            source.AccountingSessionId);
        result.AccountingMultiSessionId =
            source.AccountingMultiSessionId;
        result.InterimIntervalMilliseconds =
            source.InterimIntervalMilliseconds;
        result.RealtimeRequired = source.RealtimeRequired;
        result.EventTimestamp = source.EventTimestamp;
        result.EventTimestampPresent = source.EventTimestampPresent;
        result.TimeoutMilliseconds = source.TimeoutMilliseconds;
        result.AllowFailover = source.AllowFailover;
        result.AllowRetransmission = source.AllowRetransmission;
        result.RouteIdentifier = source.RouteIdentifier;
        result.CorrelationText = source.CorrelationText;
        result.ServiceAvps = CloneAccountingAvps(source.ServiceAvps);
        result.ApplicationState = CloneAccountingBytes(
            source.ApplicationState);
        return result;
    }

    internal static DiameterAccountingAnswer CloneAccountingAnswer(
        DiameterAccountingAnswer source)
    {
        DiameterAccountingAnswer result =
            new DiameterAccountingAnswer();
        result.SessionId = source.SessionId;
        result.RecordType = source.RecordType;
        result.RecordNumber = source.RecordNumber;
        result.ResultCode = source.ResultCode;
        result.AccountingSubSessionId = source.AccountingSubSessionId;
        result.AccountingSubSessionIdPresent =
            source.AccountingSubSessionIdPresent;
        result.InterimIntervalMilliseconds =
            source.InterimIntervalMilliseconds;
        result.InterimIntervalPresent = source.InterimIntervalPresent;
        result.RealtimeRequired = source.RealtimeRequired;
        result.RealtimeRequiredPresent = source.RealtimeRequiredPresent;
        result.ErrorMessage = source.ErrorMessage;
        result.ServiceAvps = CloneAccountingAvps(source.ServiceAvps);
        result.Message = CloneOptionalAccountingMessage(source.Message);
        return result;
    }

    internal static DiameterAccountingRecord CloneAccountingRecord(
        DiameterAccountingRecord source)
    {
        DiameterAccountingRecord result =
            new DiameterAccountingRecord();
        result.RecordIdentifier = source.RecordIdentifier;
        result.Version = source.Version;
        result.Request = CloneAccountingRequest(source.Request);
        result.State = source.State;
        result.OutboundMessage = CloneOptionalAccountingMessage(
            source.OutboundMessage);
        result.LastAnswer = CloneOptionalAccountingMessage(
            source.LastAnswer);
        result.PeerIdentity = CloneOptionalAccountingIdentity(
            source.PeerIdentity);
        result.RouteIdentifier = source.RouteIdentifier;
        result.CreatedAt = source.CreatedAt;
        result.UpdatedAt = source.UpdatedAt;
        result.LastAttemptAt = source.LastAttemptAt;
        result.NextAttemptAt = source.NextAttemptAt;
        result.NextAttemptPresent = source.NextAttemptPresent;
        result.NextInterimAt = source.NextInterimAt;
        result.NextInterimPresent = source.NextInterimPresent;
        result.AttemptCount = source.AttemptCount;
        result.Delivered = source.Delivered;
        result.Acknowledged = source.Acknowledged;
        result.Superseded = source.Superseded;
        result.Durable = source.Durable;
        result.ResultCode = source.ResultCode;
        result.Code = source.Code;
        result.Message = source.Message;
        return result;
    }

    internal static DiameterMessage? CloneOptionalAccountingMessage(
        DiameterMessage? source)
    {
        if (source == null)
        {
            return null;
        }

        DiameterMessage result = new DiameterMessage();
        result.Version = source.Version;
        result.Flags = source.Flags;
        result.CommandCode = source.CommandCode;
        result.ApplicationId = source.ApplicationId;
        result.HopByHopIdentifier = source.HopByHopIdentifier;
        result.EndToEndIdentifier = source.EndToEndIdentifier;
        result.Avps = CloneAccountingAvps(source.Avps);
        return result;
    }

    internal static List<DiameterAvp> CloneAccountingAvps(
        List<DiameterAvp> source)
    {
        List<DiameterAvp> result = new List<DiameterAvp>();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(CloneAccountingAvp(source[index]));
            index = index + 1;
        }

        return result;
    }

    internal static DiameterAvp CloneAccountingAvp(DiameterAvp source)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = source.Code;
        result.Flags = source.Flags;
        result.VendorId = source.VendorId;
        result.Data = CloneAccountingBytes(source.Data);
        return result;
    }

    internal static DiameterIdentity CloneAccountingIdentity(
        DiameterIdentity source)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = source.Value;
        return result;
    }

    internal static DiameterIdentity? CloneOptionalAccountingIdentity(
        DiameterIdentity? source)
    {
        if (source == null)
        {
            return null;
        }

        return CloneAccountingIdentity(source);
    }

    internal static byte[] CloneAccountingBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        Array.Copy(source, result, source.Length);
        return result;
    }
}
