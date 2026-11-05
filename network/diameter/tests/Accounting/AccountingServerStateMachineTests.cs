using System;
using System.Collections.Generic;

namespace diameter.Tests;

public class AccountingServerStateMachineTests
{
    [Test]
    public void ValidRequestIsOwnedHandedOffAndAnswered()
    {
        AccountingServerTestContext context = CreateServerContext();
        context.Handler.InterimIntervalMilliseconds = 15000;
        context.Handler.InterimIntervalPresent = true;
        context.Handler.RealtimeRequired =
            DiameterAccountingRealtimeRequired.GrantAndLose;
        context.Handler.RealtimeRequiredPresent = true;
        context.Handler.AddExtension = true;
        DiameterMessage request = CreateServerAccountingRequest(
            context,
            "server-event;1",
            DiameterAccountingRecordType.Event,
            0);
        AddServerAccountingText(
            request,
            DiameterAvpCodes.UserName,
            "alice@remote.example");
        AddServerAccountingUnsigned64(
            request,
            DiameterAvpCodes.AccountingSubSessionId,
            7);
        AddServerAccountingOctets(
            request,
            DiameterAvpCodes.AcctSessionId,
            new byte[] { 1, 2, 3 });
        AddServerAccountingText(
            request,
            DiameterAvpCodes.AcctMultiSessionId,
            "multi-event");
        AddServerAccountingUnsigned32(
            request,
            DiameterAvpCodes.AcctInterimInterval,
            10);
        AddServerAccountingUnsigned32(
            request,
            DiameterAvpCodes.AccountingRealtimeRequired,
            (uint)DiameterAccountingRealtimeRequired.GrantAndStore);
        AddServerAccountingTime(
            request,
            context.Clock.UtcTime.AddSeconds(-2));
        AddServerAccountingOctets(request, 900, new byte[] { 4, 5 });

        AccountingServerResult result =
            context.Machine.ProcessAccountingServerRequest(
                request,
                CreateServerIdentity("transport.remote.example"));
        request.Avps[0].Data[0] = 9;
        DiameterAccountingRequest handedOff = context.Handler.Requests[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.ApplicationInvoked, Is.True);
            Assert.That(result.Duplicate, Is.False);
            Assert.That(result.ResultCode,
                Is.EqualTo(DiameterResultCodes.Success));
            Assert.That(context.Handler.Requests.Count, Is.EqualTo(1));
            Assert.That(handedOff.SessionId, Is.EqualTo("server-event;1"));
            Assert.That(handedOff.RecordType,
                Is.EqualTo(DiameterAccountingRecordType.Event));
            Assert.That(handedOff.RecordNumber, Is.Zero);
            Assert.That(handedOff.AccountingSubSessionId,
                Is.EqualTo(7UL));
            Assert.That(handedOff.AccountingSessionId,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(handedOff.AccountingMultiSessionId,
                Is.EqualTo("multi-event"));
            Assert.That(handedOff.InterimIntervalMilliseconds,
                Is.EqualTo(10000));
            Assert.That(handedOff.EventTimestamp,
                Is.EqualTo(context.Clock.UtcTime.AddSeconds(-2)));
            Assert.That(handedOff.PeerIdentity!.Value,
                Is.EqualTo("transport.remote.example"));
            Assert.That(handedOff.ServiceAvps.Count, Is.EqualTo(1));
            Assert.That(handedOff.ServiceAvps[0].Code, Is.EqualTo(900U));
            Assert.That(result.Answer, Is.Not.Null);
            Assert.That(result.Answer!.HopByHopIdentifier,
                Is.EqualTo(request.HopByHopIdentifier));
            Assert.That(ReadServerAccountingUnsigned32(
                result.Answer,
                DiameterAvpCodes.ResultCode),
                Is.EqualTo(DiameterResultCodes.Success));
            Assert.That(ReadServerAccountingUnsigned64(
                result.Answer,
                DiameterAvpCodes.AccountingSubSessionId),
                Is.EqualTo(7UL));
            Assert.That(ReadServerAccountingUnsigned32(
                result.Answer,
                DiameterAvpCodes.AcctInterimInterval),
                Is.EqualTo(15U));
            Assert.That(ReadServerAccountingUnsigned32(
                result.Answer,
                DiameterAvpCodes.AccountingRealtimeRequired),
                Is.EqualTo(
                    (uint)DiameterAccountingRealtimeRequired.GrantAndLose));
            Assert.That(FindServerAccountingAvp(result.Answer, 901).Data,
                Is.EqualTo(new byte[] { 8, 9 }));
        });
    }

    [Test]
    public void StartInterimStopAreProcessedOnlyInOrder()
    {
        AccountingServerTestContext context = CreateServerContext();
        DiameterMessage unknownInterim = CreateServerAccountingRequest(
            context,
            "unknown-sequence;1",
            DiameterAccountingRecordType.Interim,
            1);
        AccountingServerResult unknown =
            context.Machine.ProcessAccountingServerRequest(
                unknownInterim,
                null);

        DiameterMessage start = CreateServerAccountingRequest(
            context,
            "ordered-sequence;1",
            DiameterAccountingRecordType.Start,
            0);
        AccountingServerResult started =
            context.Machine.ProcessAccountingServerRequest(start, null);
        DiameterMessage gap = CreateServerAccountingRequest(
            context,
            "ordered-sequence;1",
            DiameterAccountingRecordType.Interim,
            2);
        AccountingServerResult skipped =
            context.Machine.ProcessAccountingServerRequest(gap, null);
        DiameterMessage interim = CreateServerAccountingRequest(
            context,
            "ordered-sequence;1",
            DiameterAccountingRecordType.Interim,
            1);
        AccountingServerResult updated =
            context.Machine.ProcessAccountingServerRequest(interim, null);
        DiameterMessage stop = CreateServerAccountingRequest(
            context,
            "ordered-sequence;1",
            DiameterAccountingRecordType.Stop,
            2);
        AccountingServerResult stopped =
            context.Machine.ProcessAccountingServerRequest(stop, null);
        DiameterMessage afterStop = CreateServerAccountingRequest(
            context,
            "ordered-sequence;1",
            DiameterAccountingRecordType.Interim,
            3);
        AccountingServerResult closed =
            context.Machine.ProcessAccountingServerRequest(
                afterStop,
                null);

        Assert.Multiple(() =>
        {
            Assert.That(unknown.ResultCode,
                Is.EqualTo(DiameterResultCodes.UnknownSessionId));
            Assert.That(started.Accepted, Is.True);
            Assert.That(skipped.ResultCode,
                Is.EqualTo(DiameterResultCodes.InvalidAvpValue));
            Assert.That(updated.Accepted, Is.True);
            Assert.That(stopped.Accepted, Is.True);
            Assert.That(closed.ResultCode,
                Is.EqualTo(DiameterResultCodes.UnknownSessionId));
            Assert.That(context.Handler.Requests.Count, Is.EqualTo(3));
            Assert.That(context.Handler.Requests[0].RecordType,
                Is.EqualTo(DiameterAccountingRecordType.Start));
            Assert.That(context.Handler.Requests[1].RecordType,
                Is.EqualTo(DiameterAccountingRecordType.Interim));
            Assert.That(context.Handler.Requests[2].RecordType,
                Is.EqualTo(DiameterAccountingRecordType.Stop));
        });
    }

    [Test]
    public void ProtocolDuplicateReplaysCachedAnswerWithoutHandoff()
    {
        AccountingServerTestContext context = CreateServerContext();
        DiameterMessage request = CreateServerAccountingRequest(
            context,
            "protocol-duplicate;1",
            DiameterAccountingRecordType.Event,
            0);
        AccountingServerResult first =
            context.Machine.ProcessAccountingServerRequest(request, null);
        DiameterMessage retry = CloneServerAccountingMessage(request);
        retry.HopByHopIdentifier = request.HopByHopIdentifier + 100U;
        retry.Flags = retry.Flags |
            DiameterMessageFlags.PotentiallyRetransmitted;

        AccountingServerResult duplicate =
            context.Machine.ProcessAccountingServerRequest(retry, null);

        Assert.Multiple(() =>
        {
            Assert.That(first.Accepted, Is.True);
            Assert.That(duplicate.Duplicate, Is.True);
            Assert.That(duplicate.Replayed, Is.True);
            Assert.That(duplicate.ApplicationInvoked, Is.False);
            Assert.That(duplicate.Code,
                Is.EqualTo("accounting-protocol-duplicate"));
            Assert.That(duplicate.Answer!.HopByHopIdentifier,
                Is.EqualTo(retry.HopByHopIdentifier));
            Assert.That(duplicate.Answer.EndToEndIdentifier,
                Is.EqualTo(retry.EndToEndIdentifier));
            Assert.That(context.Handler.Requests.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void AccountingKeyDuplicateWithNewProtocolIdAlsoReplays()
    {
        AccountingServerTestContext context = CreateServerContext();
        DiameterMessage request = CreateServerAccountingRequest(
            context,
            "record-duplicate;1",
            DiameterAccountingRecordType.Event,
            0);
        AddServerAccountingOctets(request, 900, new byte[] { 1 });
        context.Machine.ProcessAccountingServerRequest(request, null);
        DiameterMessage retry = CloneServerAccountingMessage(request);
        retry.HopByHopIdentifier = request.HopByHopIdentifier + 1U;
        retry.EndToEndIdentifier = request.EndToEndIdentifier + 1U;
        retry.Flags = retry.Flags |
            DiameterMessageFlags.PotentiallyRetransmitted;

        AccountingServerResult duplicate =
            context.Machine.ProcessAccountingServerRequest(retry, null);
        DiameterMessage repeated = CloneServerAccountingMessage(retry);
        repeated.HopByHopIdentifier = retry.HopByHopIdentifier + 1U;
        AccountingServerResult protocolReplay =
            context.Machine.ProcessAccountingServerRequest(repeated, null);

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Duplicate, Is.True);
            Assert.That(duplicate.Replayed, Is.True);
            Assert.That(duplicate.Code,
                Is.EqualTo("accounting-record-duplicate"));
            Assert.That(duplicate.Answer!.EndToEndIdentifier,
                Is.EqualTo(retry.EndToEndIdentifier));
            Assert.That(protocolReplay.Code,
                Is.EqualTo("accounting-protocol-duplicate"));
            Assert.That(protocolReplay.Answer!.HopByHopIdentifier,
                Is.EqualTo(repeated.HopByHopIdentifier));
            Assert.That(context.Handler.Requests.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void ReusedAccountingKeyWithChangedDataIsRejected()
    {
        AccountingServerTestContext context = CreateServerContext();
        DiameterMessage request = CreateServerAccountingRequest(
            context,
            "record-collision;1",
            DiameterAccountingRecordType.Event,
            0);
        AddServerAccountingOctets(request, 900, new byte[] { 1 });
        context.Machine.ProcessAccountingServerRequest(request, null);
        DiameterMessage changed = CloneServerAccountingMessage(request);
        changed.HopByHopIdentifier = request.HopByHopIdentifier + 1U;
        changed.EndToEndIdentifier = request.EndToEndIdentifier + 1U;
        FindServerAccountingAvp(changed, 900).Data[0] = 2;

        AccountingServerResult collision =
            context.Machine.ProcessAccountingServerRequest(changed, null);

        Assert.Multiple(() =>
        {
            Assert.That(collision.Accepted, Is.False);
            Assert.That(collision.Duplicate, Is.True);
            Assert.That(collision.ResultCode,
                Is.EqualTo(DiameterResultCodes.UnableToComply));
            Assert.That(collision.Code,
                Is.EqualTo("accounting-record-key-collision"));
            Assert.That(ReadServerAccountingUnsigned32(
                collision.Answer!,
                DiameterAvpCodes.ResultCode),
                Is.EqualTo(DiameterResultCodes.UnableToComply));
            Assert.That(context.Handler.Requests.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void InvalidAvpsAreRejectedBeforeApplicationHandoff()
    {
        AccountingServerTestContext context = CreateServerContext();
        DiameterMessage invalidType = CreateServerAccountingRequest(
            context,
            "invalid-type;1",
            DiameterAccountingRecordType.Event,
            0);
        WriteServerAccountingUnsigned32(
            FindServerAccountingAvp(
                invalidType,
                DiameterAvpCodes.AccountingRecordType),
            99);
        AccountingServerResult invalid =
            context.Machine.ProcessAccountingServerRequest(
                invalidType,
                null);

        DiameterMessage repeatedNumber = CreateServerAccountingRequest(
            context,
            "repeated-number;1",
            DiameterAccountingRecordType.Event,
            0);
        repeatedNumber.Avps.Add(CloneServerAccountingAvp(
            FindServerAccountingAvp(
                repeatedNumber,
                DiameterAvpCodes.AccountingRecordNumber)));
        AccountingServerResult repeated =
            context.Machine.ProcessAccountingServerRequest(
                repeatedNumber,
                null);

        DiameterMessage missingSession = CreateServerAccountingRequest(
            context,
            "missing-session;1",
            DiameterAccountingRecordType.Event,
            0);
        RemoveServerAccountingAvp(
            missingSession,
            DiameterAvpCodes.SessionId);
        AccountingServerResult missing =
            context.Machine.ProcessAccountingServerRequest(
                missingSession,
                null);

        DiameterMessage wrongHeader = CreateServerAccountingRequest(
            context,
            "wrong-header;1",
            DiameterAccountingRecordType.Event,
            0);
        wrongHeader.CommandCode = DiameterCommandCodes.DeviceWatchdog;
        DiameterMessage invalidPeerRequest = CreateServerAccountingRequest(
            context,
            "invalid-peer;1",
            DiameterAccountingRecordType.Event,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(invalid.ResultCode,
                Is.EqualTo(DiameterResultCodes.InvalidAvpValue));
            Assert.That(invalid.Answer, Is.Not.Null);
            Assert.That(repeated.ResultCode,
                Is.EqualTo(DiameterResultCodes.AvpOccursTooManyTimes));
            Assert.That(missing.ResultCode,
                Is.EqualTo(DiameterResultCodes.MissingAvp));
            Assert.That(missing.Answer, Is.Null);
            Assert.Throws<ArgumentException>(() =>
                context.Machine.ProcessAccountingServerRequest(
                    wrongHeader,
                    null));
            Assert.Throws<ArgumentException>(() =>
                context.Machine.ProcessAccountingServerRequest(
                    invalidPeerRequest,
                    CreateServerIdentity("bad peer")));
            Assert.That(context.Handler.Requests, Is.Empty);
        });
    }

    private static AccountingServerTestContext CreateServerContext()
    {
        AccountingServerTestContext result =
            new AccountingServerTestContext();
        result.Clock = new FakeDiscoveryClock();
        result.Identifiers = new AccountingServerIdentifierSource();
        result.CommandFactory =
            new DiameterBaseCommandFactory(result.Identifiers);
        result.DuplicateStore = new DuplicateRequestStore(
            result.Clock,
            64,
            60000);
        result.Handler = new RecordingAccountingHandler();
        result.Machine = new AccountingServerStateMachine(
            CreateServerIdentity("server.example"),
            CreateServerIdentity("example"),
            result.CommandFactory,
            result.DuplicateStore,
            result.Handler,
            result.Clock);
        return result;
    }

    private static DiameterMessage CreateServerAccountingRequest(
        AccountingServerTestContext context,
        string sessionId,
        DiameterAccountingRecordType recordType,
        uint recordNumber)
    {
        DiameterBaseCommandOptions options =
            new DiameterBaseCommandOptions();
        options.OriginHost = CreateServerIdentity("client.remote.example");
        options.OriginRealm = CreateServerIdentity("remote.example");
        options.DestinationRealm = CreateServerIdentity("example");
        options.SessionId = sessionId;
        options.AccountingRecordType = recordType;
        options.AccountingRecordNumber = recordNumber;
        DiameterMessage result =
            context.CommandFactory.CreateAccountingRequest(options);
        result.Avps.Add(new DiameterBaseAvpFactory()
            .CreateAcctApplicationIdAvp(
                DiameterApplicationIds.BaseAccounting));
        return result;
    }

    private static DiameterIdentity CreateServerIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }

    private static void AddServerAccountingUnsigned32(
        DiameterMessage message,
        uint code,
        uint value)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = code;
        avp.Flags = DiameterAvpFlags.Mandatory;
        avp.Data = new byte[4];
        NetworkByteOrder.WriteUInt32(avp.Data, value);
        message.Avps.Add(avp);
    }

    private static void AddServerAccountingUnsigned64(
        DiameterMessage message,
        uint code,
        ulong value)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = code;
        avp.Flags = DiameterAvpFlags.Mandatory;
        avp.Data = new byte[8];
        NetworkByteOrder.WriteUInt64(avp.Data, value);
        message.Avps.Add(avp);
    }

    private static void AddServerAccountingText(
        DiameterMessage message,
        uint code,
        string value)
    {
        byte[] data = new byte[256];
        DiameterValueResult encoded = ValueCodecs.EncodeUtf8String(
            data,
            value);
        DiameterAvp avp = new DiameterAvp();
        avp.Code = code;
        avp.Flags = DiameterAvpFlags.Mandatory;
        avp.Data = new byte[encoded.WrittenCount];
        Array.Copy(data, avp.Data, encoded.WrittenCount);
        message.Avps.Add(avp);
    }

    private static void AddServerAccountingOctets(
        DiameterMessage message,
        uint code,
        byte[] value)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = code;
        avp.Flags = DiameterAvpFlags.Mandatory;
        avp.Data = new byte[value.Length];
        Array.Copy(value, avp.Data, value.Length);
        message.Avps.Add(avp);
    }

    private static void AddServerAccountingTime(
        DiameterMessage message,
        DateTimeOffset value)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = DiameterAvpCodes.EventTimestamp;
        avp.Flags = DiameterAvpFlags.Mandatory;
        avp.Data = new byte[4];
        ValueCodecs.EncodeTime(avp.Data, value);
        message.Avps.Add(avp);
    }

    private static DiameterAvp FindServerAccountingAvp(
        DiameterMessage message,
        uint code)
    {
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp.Code == code &&
                avp.VendorId == DiameterVendorIds.Ietf)
            {
                return avp;
            }

            index = index + 1;
        }

        throw new AssertionException(
            "The expected accounting AVP is absent.");
    }

    private static uint ReadServerAccountingUnsigned32(
        DiameterMessage message,
        uint code)
    {
        return NetworkByteOrder.ReadUInt32(
            FindServerAccountingAvp(message, code).Data);
    }

    private static ulong ReadServerAccountingUnsigned64(
        DiameterMessage message,
        uint code)
    {
        return NetworkByteOrder.ReadUInt64(
            FindServerAccountingAvp(message, code).Data);
    }

    private static void WriteServerAccountingUnsigned32(
        DiameterAvp avp,
        uint value)
    {
        NetworkByteOrder.WriteUInt32(avp.Data, value);
    }

    private static DiameterMessage CloneServerAccountingMessage(
        DiameterMessage source)
    {
        DiameterMessage? result =
            AccountingRecordCloner.CloneOptionalAccountingMessage(source);
        if (result == null)
        {
            throw new AssertionException(
                "The accounting request clone is absent.");
        }

        return result;
    }

    private static DiameterAvp CloneServerAccountingAvp(
        DiameterAvp source)
    {
        return AccountingRecordCloner.CloneAccountingAvp(source);
    }

    private static void RemoveServerAccountingAvp(
        DiameterMessage message,
        uint code)
    {
        int index = message.Avps.Count - 1;
        while (index >= 0)
        {
            if (message.Avps[index].Code == code &&
                message.Avps[index].VendorId == DiameterVendorIds.Ietf)
            {
                message.Avps.RemoveAt(index);
            }

            index = index - 1;
        }
    }
}

internal class AccountingServerTestContext
{
    internal FakeDiscoveryClock Clock = new FakeDiscoveryClock();
    internal AccountingServerIdentifierSource Identifiers =
        new AccountingServerIdentifierSource();
    internal DiameterBaseCommandFactory CommandFactory = null!;
    internal DuplicateRequestStore DuplicateStore = null!;
    internal RecordingAccountingHandler Handler = null!;
    internal AccountingServerStateMachine Machine = null!;
}

internal class RecordingAccountingHandler : IDiameterAccountingHandler
{
    internal List<DiameterAccountingRequest> Requests =
        new List<DiameterAccountingRequest>();
    internal uint ResultCode = DiameterResultCodes.Success;
    internal int InterimIntervalMilliseconds;
    internal bool InterimIntervalPresent;
    internal DiameterAccountingRealtimeRequired RealtimeRequired =
        DiameterAccountingRealtimeRequired.GrantAndStore;
    internal bool RealtimeRequiredPresent;
    internal bool AddExtension;

    public DiameterAccountingAnswer HandleDiameterAccountingRequest(
        DiameterAccountingRequest? request)
    {
        if (request == null)
        {
            throw new ArgumentException(
                "A recorded accounting request is required.");
        }

        Requests.Add(request);
        DiameterAccountingAnswer result =
            new DiameterAccountingAnswer();
        result.SessionId = request.SessionId;
        result.RecordType = request.RecordType;
        result.RecordNumber = request.RecordNumber;
        result.ResultCode = ResultCode;
        result.InterimIntervalMilliseconds =
            InterimIntervalMilliseconds;
        result.InterimIntervalPresent = InterimIntervalPresent;
        result.RealtimeRequired = RealtimeRequired;
        result.RealtimeRequiredPresent = RealtimeRequiredPresent;
        if (AddExtension)
        {
            DiameterAvp extension = new DiameterAvp();
            extension.Code = 901;
            extension.Data = new byte[] { 8, 9 };
            result.ServiceAvps.Add(extension);
        }

        return result;
    }
}

internal class AccountingServerIdentifierSource : IDiameterIdentifierSource
{
    internal uint HopByHopIdentifier = 100;
    internal uint EndToEndIdentifier = 200;

    public uint CreateHopByHopIdentifier()
    {
        HopByHopIdentifier = HopByHopIdentifier + 1U;
        return HopByHopIdentifier;
    }

    public uint CreateEndToEndIdentifier()
    {
        EndToEndIdentifier = EndToEndIdentifier + 1U;
        return EndToEndIdentifier;
    }

    public string CreateSessionIdentifier(
        DiameterIdentity originHost,
        string? correlationText)
    {
        return originHost.Value + ";accounting-server";
    }

    public uint CreateOriginStateIdentifier()
    {
        return 77U;
    }
}
