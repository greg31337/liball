using System;
using System.Collections.Generic;

namespace diameter.Tests;

public class AccountingDeliveryPolicyTests
{
    [Test]
    public void RealtimePoliciesChooseRetentionAndServiceGrantOnRejection()
    {
        PolicyAccountingRecordStore deliverStore =
            new PolicyAccountingRecordStore();
        PolicyAccountingRequestClient deliverRequests =
            new PolicyAccountingRequestClient();
        deliverRequests.Accept = false;
        DiameterAccountingClient deliverClient = CreatePolicyClient(
            deliverStore,
            deliverRequests);
        DiameterAccountingRequest deliver = CreatePolicyRequest(
            "deliver-and-grant;1");
        deliver.RealtimeRequired =
            DiameterAccountingRealtimeRequired.DeliverAndGrant;
        deliver.AllowFailover = false;
        deliver.AllowRetransmission = false;

        DiameterAccountingResult deliverResult =
            deliverClient.SendDiameterAccountingEvent(deliver);

        PolicyAccountingRecordStore storeStore =
            new PolicyAccountingRecordStore();
        PolicyAccountingRequestClient storeRequests =
            new PolicyAccountingRequestClient();
        storeRequests.Accept = false;
        DiameterAccountingClient storeClient = CreatePolicyClient(
            storeStore,
            storeRequests);
        DiameterAccountingRequest grantStore = CreatePolicyRequest(
            "grant-and-store;1");
        grantStore.RealtimeRequired =
            DiameterAccountingRealtimeRequired.GrantAndStore;
        DiameterAccountingResult storeResult =
            storeClient.SendDiameterAccountingEvent(grantStore);

        PolicyAccountingRecordStore loseStore =
            new PolicyAccountingRecordStore();
        PolicyAccountingRequestClient loseRequests =
            new PolicyAccountingRequestClient();
        loseRequests.Accept = false;
        DiameterAccountingClient loseClient = CreatePolicyClient(
            loseStore,
            loseRequests);
        DiameterAccountingRequest grantLose = CreatePolicyRequest(
            "grant-and-lose;1");
        grantLose.RealtimeRequired =
            DiameterAccountingRealtimeRequired.GrantAndLose;
        DiameterAccountingResult loseResult =
            loseClient.SendDiameterAccountingEvent(grantLose);

        Assert.Multiple(() =>
        {
            Assert.That(deliverResult.Stored, Is.True);
            Assert.That(deliverResult.ServiceGranted, Is.False);
            Assert.That(deliverResult.State,
                Is.EqualTo(DiameterAccountingState.PendingEvent));
            Assert.That(deliverStore.Records.Count, Is.EqualTo(1));
            Assert.That(deliverRequests.Requests[0].AllowFailover,
                Is.False);
            Assert.That(deliverRequests.Requests[0].AllowRetransmission,
                Is.False);
            Assert.That(storeResult.Stored, Is.True);
            Assert.That(storeResult.ServiceGranted, Is.True);
            Assert.That(storeStore.Records.Count, Is.EqualTo(1));
            Assert.That(loseResult.Stored, Is.False);
            Assert.That(loseResult.ServiceGranted, Is.True);
            Assert.That(loseResult.State,
                Is.EqualTo(DiameterAccountingState.Failed));
            Assert.That(loseStore.Records, Is.Empty);
        });
    }

    [Test]
    public void StorageFailurePreventsGrantAndStoreDelivery()
    {
        PolicyAccountingRecordStore store =
            new PolicyAccountingRecordStore();
        store.FailSaves = true;
        PolicyAccountingRequestClient requests =
            new PolicyAccountingRequestClient();
        DiameterAccountingClient client = CreatePolicyClient(
            store,
            requests);
        DiameterAccountingRequest request = CreatePolicyRequest(
            "store-failure;1");
        request.RealtimeRequired =
            DiameterAccountingRealtimeRequired.GrantAndStore;

        DiameterAccountingResult result =
            client.SendDiameterAccountingEvent(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Stored, Is.False);
            Assert.That(result.ServiceGranted, Is.False);
            Assert.That(result.TerminateService, Is.True);
            Assert.That(result.State,
                Is.EqualTo(DiameterAccountingState.Failed));
            Assert.That(result.Code,
                Is.EqualTo("accounting-store-failed"));
            Assert.That(requests.Requests, Is.Empty);
        });
    }

    [Test]
    public void TerminalDeliveryFailureRetainsAttemptMetadata()
    {
        PolicyAccountingRecordStore store =
            new PolicyAccountingRecordStore();
        PolicyAccountingRequestClient requests =
            new PolicyAccountingRequestClient();
        DiameterAccountingClient client = CreatePolicyClient(
            store,
            requests);
        DiameterAccountingRequest request = CreatePolicyRequest(
            "retained-failure;1");
        request.RealtimeRequired =
            DiameterAccountingRealtimeRequired.GrantAndStore;
        client.SendDiameterAccountingEvent(request);

        requests.FailAccountingRequest(0, 3, true);
        List<DiameterAccountingRecord> records =
            client.ReadDiameterAccountingRecords();

        Assert.Multiple(() =>
        {
            Assert.That(records.Count, Is.EqualTo(1));
            Assert.That(records[0].State,
                Is.EqualTo(DiameterAccountingState.PendingEvent));
            Assert.That(records[0].Durable, Is.True);
            Assert.That(records[0].NextAttemptPresent, Is.True);
            Assert.That(records[0].AttemptCount, Is.EqualTo(3));
            Assert.That(records[0].OutboundMessage!.Flags &
                DiameterMessageFlags.PotentiallyRetransmitted,
                Is.Not.EqualTo(DiameterMessageFlags.None));
            Assert.That(records[0].Code,
                Is.EqualTo("accounting-pending-retained"));
            Assert.That(store.Records.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void PendingInterimIsOverwrittenByNewerCumulativeRecord()
    {
        PolicyAccountingRecordStore store =
            new PolicyAccountingRecordStore();
        PolicyAccountingRequestClient requests =
            new PolicyAccountingRequestClient();
        DiameterAccountingClient client = CreatePolicyClient(
            store,
            requests);
        DiameterAccountingRequest start = CreatePolicyRequest(
            "interim-overwrite;1");
        start.InterimIntervalMilliseconds = 10000;
        client.SendDiameterAccountingStart(start);
        requests.CompleteAccountingRequest(0);

        DiameterAccountingRequest firstInterim = CreatePolicyRequest(
            "interim-overwrite;1");
        firstInterim.InterimIntervalMilliseconds = 10000;
        client.SendDiameterAccountingInterim(firstInterim);
        requests.FailAccountingRequest(1, 2, true);
        DiameterAccountingRequest cumulative = CreatePolicyRequest(
            "interim-overwrite;1");
        cumulative.InterimIntervalMilliseconds = 10000;
        DiameterAccountingResult replacement =
            client.SendDiameterAccountingInterim(cumulative);
        List<DiameterAccountingRecord> records =
            client.ReadDiameterAccountingRecords();

        Assert.Multiple(() =>
        {
            Assert.That(replacement.Request.RecordNumber, Is.EqualTo(1U));
            Assert.That(records.Count, Is.EqualTo(3));
            Assert.That(records[1].Superseded, Is.True);
            Assert.That(records[1].Durable, Is.False);
            Assert.That(records[1].State,
                Is.EqualTo(DiameterAccountingState.Closed));
            Assert.That(records[2].Request.RecordNumber, Is.EqualTo(1U));
            Assert.That(records[2].Durable, Is.True);
            Assert.That(store.Records.Count, Is.EqualTo(1));
            Assert.That(store.Records.ContainsKey(
                records[2].RecordIdentifier), Is.True);
        });
    }

    [Test]
    public void StopIsDurableUntilItsSuccessfulAnswer()
    {
        PolicyAccountingRecordStore store =
            new PolicyAccountingRecordStore();
        PolicyAccountingRequestClient requests =
            new PolicyAccountingRequestClient();
        DiameterAccountingClient client = CreatePolicyClient(
            store,
            requests);
        DiameterAccountingRequest start = CreatePolicyRequest(
            "durable-stop;1");
        start.RealtimeRequired =
            DiameterAccountingRealtimeRequired.GrantAndLose;
        client.SendDiameterAccountingStart(start);
        requests.CompleteAccountingRequest(0);
        DiameterAccountingRequest stop = CreatePolicyRequest(
            "durable-stop;1");
        stop.RealtimeRequired =
            DiameterAccountingRealtimeRequired.GrantAndLose;

        DiameterAccountingResult pending =
            client.SendDiameterAccountingStop(stop);
        int storedBeforeAnswer = store.Records.Count;
        requests.CompleteAccountingRequest(1);
        List<DiameterAccountingRecord> records =
            client.ReadDiameterAccountingRecords();

        Assert.Multiple(() =>
        {
            Assert.That(pending.Stored, Is.True);
            Assert.That(storedBeforeAnswer, Is.EqualTo(1));
            Assert.That(store.Records, Is.Empty);
            Assert.That(records[1].State,
                Is.EqualTo(DiameterAccountingState.Closed));
            Assert.That(records[1].Acknowledged, Is.True);
            Assert.That(records[1].Durable, Is.False);
        });
    }

    [Test]
    public void RetainedRecordIsRetransmittedAndClearedDuringRecovery()
    {
        PolicyAccountingRecordStore store =
            new PolicyAccountingRecordStore();
        PolicyAccountingRequestClient firstRequests =
            new PolicyAccountingRequestClient();
        DiameterAccountingClient firstClient = CreatePolicyClient(
            store,
            firstRequests);
        DiameterAccountingRequest request = CreatePolicyRequest(
            "recovery;1");
        request.RealtimeRequired =
            DiameterAccountingRealtimeRequired.GrantAndStore;
        firstClient.SendDiameterAccountingEvent(request);
        firstRequests.FailAccountingRequest(0, 1, false);

        PolicyAccountingRequestClient recoveredRequests =
            new PolicyAccountingRequestClient();
        DiameterAccountingClient recoveredClient = CreatePolicyClient(
            store,
            recoveredRequests);
        List<DiameterAccountingRecord> replaying =
            recoveredClient.ReadDiameterAccountingRecords();
        recoveredRequests.CompleteAccountingRequest(0);
        List<DiameterAccountingRecord> completed =
            recoveredClient.ReadDiameterAccountingRecords();

        Assert.Multiple(() =>
        {
            Assert.That(recoveredRequests.Requests.Count, Is.EqualTo(1));
            Assert.That(recoveredRequests.Requests[0].Request.Flags &
                DiameterMessageFlags.PotentiallyRetransmitted,
                Is.Not.EqualTo(DiameterMessageFlags.None));
            Assert.That(replaying[0].Request.SessionId,
                Is.EqualTo("recovery;1"));
            Assert.That(replaying[0].Request.RecordNumber, Is.Zero);
            Assert.That(replaying[0].AttemptCount, Is.EqualTo(2));
            Assert.That(completed[0].State,
                Is.EqualTo(DiameterAccountingState.Closed));
            Assert.That(completed[0].Acknowledged, Is.True);
            Assert.That(completed[0].Durable, Is.False);
            Assert.That(store.Records, Is.Empty);
        });
    }

    [Test]
    public void FailedInterimReplacementPreservesTheOlderDurableRecord()
    {
        PolicyAccountingRecordStore store =
            new PolicyAccountingRecordStore();
        PolicyAccountingRequestClient requests =
            new PolicyAccountingRequestClient();
        DiameterAccountingClient client = CreatePolicyClient(
            store,
            requests);
        DiameterAccountingRequest start = CreatePolicyRequest(
            "overwrite-store-failure;1");
        start.InterimIntervalMilliseconds = 10000;
        client.SendDiameterAccountingStart(start);
        requests.CompleteAccountingRequest(0);
        DiameterAccountingRequest interim = CreatePolicyRequest(
            "overwrite-store-failure;1");
        interim.InterimIntervalMilliseconds = 10000;
        client.SendDiameterAccountingInterim(interim);
        requests.FailAccountingRequest(1, 2, true);
        long retainedIdentifier =
            client.ReadDiameterAccountingRecords()[1].RecordIdentifier;
        store.FailSaves = true;
        DiameterAccountingRequest replacement = CreatePolicyRequest(
            "overwrite-store-failure;1");

        DiameterAccountingResult result =
            client.SendDiameterAccountingInterim(replacement);
        List<DiameterAccountingRecord> records =
            client.ReadDiameterAccountingRecords();

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Code,
                Is.EqualTo("accounting-store-failed"));
            Assert.That(requests.Requests, Has.Count.EqualTo(2));
            Assert.That(records[1].State,
                Is.EqualTo(DiameterAccountingState.PendingInterim));
            Assert.That(records[1].Superseded, Is.False);
            Assert.That(records[1].Durable, Is.True);
            Assert.That(records[2].State,
                Is.EqualTo(DiameterAccountingState.Failed));
            Assert.That(store.Records, Has.Count.EqualTo(1));
            Assert.That(store.Records.ContainsKey(retainedIdentifier),
                Is.True);
        });
    }

    [Test]
    public void RecoveryConflictDoesNotRetransmitUnversionedState()
    {
        PolicyAccountingRecordStore store =
            new PolicyAccountingRecordStore();
        PolicyAccountingRequestClient initialRequests =
            new PolicyAccountingRequestClient();
        DiameterAccountingClient initialClient = CreatePolicyClient(
            store,
            initialRequests);
        DiameterAccountingRequest request = CreatePolicyRequest(
            "recovery-conflict;1");
        initialClient.SendDiameterAccountingEvent(request);
        initialRequests.FailAccountingRequest(0, 2, true);
        store.FailSaves = true;
        PolicyAccountingRequestClient recoveredRequests =
            new PolicyAccountingRequestClient();

        DiameterAccountingClient recovered = CreatePolicyClient(
            store,
            recoveredRequests);
        DiameterAccountingRecord record =
            recovered.ReadDiameterAccountingRecords()[0];

        Assert.Multiple(() =>
        {
            Assert.That(recoveredRequests.Requests, Is.Empty);
            Assert.That(record.State,
                Is.EqualTo(DiameterAccountingState.Failed));
            Assert.That(record.Code,
                Is.EqualTo("accounting-recovery-conflict"));
            Assert.That(record.NextAttemptPresent, Is.True);
            Assert.That(record.Durable, Is.False);
            Assert.That(store.Records, Has.Count.EqualTo(1));
        });
    }

    private static DiameterAccountingClient CreatePolicyClient(
        PolicyAccountingRecordStore store,
        PolicyAccountingRequestClient requests)
    {
        return new DiameterAccountingClient(
            CreatePolicyIdentity("client.example"),
            CreatePolicyIdentity("example"),
            new DiameterBaseAvpFactory(),
            requests,
            store,
            new TestDiameterClock(
                new DateTimeOffset(
                    2026,
                    8,
                    29,
                    12,
                    0,
                    0,
                    TimeSpan.Zero),
                0));
    }

    private static DiameterAccountingRequest CreatePolicyRequest(
        string sessionId)
    {
        DiameterAccountingRequest result =
            new DiameterAccountingRequest();
        result.SessionId = sessionId;
        result.ApplicationId = DiameterApplicationIds.BaseAccounting;
        result.DestinationRealm =
            CreatePolicyIdentity("remote.example");
        result.PeerIdentity =
            CreatePolicyIdentity("peer.remote.example");
        return result;
    }

    private static DiameterIdentity CreatePolicyIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }
}

internal class PolicyAccountingRecordStore : IAccountingRecordStore
{
    internal Dictionary<long, DiameterAccountingRecord> Records =
        new Dictionary<long, DiameterAccountingRecord>();
    internal List<long> Order = new List<long>();
    internal bool FailSaves;

    public DiameterAccountingStoreResult LoadDiameterAccountingRecord(
        long recordIdentifier)
    {
        DiameterAccountingStoreResult result =
            new DiameterAccountingStoreResult();
        result.RecordIdentifier = recordIdentifier;
        if (Records.ContainsKey(recordIdentifier))
        {
            result.Found = true;
            result.Record = AccountingRecordCloner.CloneAccountingRecord(
                Records[recordIdentifier]);
        }

        return result;
    }

    public DiameterAccountingStoreResult SaveDiameterAccountingRecord(
        DiameterAccountingRecord? record,
        long expectedVersion)
    {
        DiameterAccountingStoreResult result =
            new DiameterAccountingStoreResult();
        if (record == null)
        {
            result.Code = "record-required";
            return result;
        }

        result.RecordIdentifier = record.RecordIdentifier;
        if (FailSaves)
        {
            result.Code = "store-failed";
            return result;
        }

        bool exists = Records.ContainsKey(record.RecordIdentifier);
        if ((exists &&
            Records[record.RecordIdentifier].Version != expectedVersion) ||
            (!exists && expectedVersion != 0))
        {
            result.Found = exists;
            result.Conflict = true;
            result.Code = "version-conflict";
            return result;
        }

        DiameterAccountingRecord saved =
            AccountingRecordCloner.CloneAccountingRecord(record);
        saved.Version = expectedVersion + 1;
        saved.Durable = true;
        Records[saved.RecordIdentifier] = saved;
        if (!exists)
        {
            Order.Add(saved.RecordIdentifier);
        }

        result.Found = exists;
        result.Saved = true;
        result.Record = AccountingRecordCloner.CloneAccountingRecord(saved);
        return result;
    }

    public DiameterAccountingStoreResult RemoveDiameterAccountingRecord(
        long recordIdentifier,
        long expectedVersion)
    {
        DiameterAccountingStoreResult result =
            new DiameterAccountingStoreResult();
        result.RecordIdentifier = recordIdentifier;
        if (!Records.ContainsKey(recordIdentifier))
        {
            return result;
        }

        result.Found = true;
        if (Records[recordIdentifier].Version != expectedVersion)
        {
            result.Conflict = true;
            return result;
        }

        Records.Remove(recordIdentifier);
        Order.Remove(recordIdentifier);
        result.Removed = true;
        return result;
    }

    public List<DiameterAccountingRecord>
        ReadStoredDiameterAccountingRecords()
    {
        List<DiameterAccountingRecord> result =
            new List<DiameterAccountingRecord>();
        int index = 0;
        while (index < Order.Count)
        {
            long identifier = Order[index];
            if (Records.ContainsKey(identifier))
            {
                result.Add(AccountingRecordCloner.CloneAccountingRecord(
                    Records[identifier]));
            }

            index = index + 1;
        }

        return result;
    }
}

internal class PolicyAccountingRequestClient : IDiameterRequestClient
{
    internal List<DiameterSendRequest> Requests =
        new List<DiameterSendRequest>();
    internal List<IDiameterAnswerHandler?> Handlers =
        new List<IDiameterAnswerHandler?>();
    internal List<DiameterPendingRequestSnapshot> Pending =
        new List<DiameterPendingRequestSnapshot>();
    internal bool Accept = true;
    internal long NextIdentifier = 301;

    public DiameterSendResult SendDiameterRequest(
        DiameterSendRequest? request,
        IDiameterAnswerHandler? handler)
    {
        if (request == null)
        {
            throw new ArgumentException(
                "A policy accounting request is required.");
        }

        Requests.Add(request);
        Handlers.Add(handler);
        DiameterSendResult result = new DiameterSendResult();
        if (!Accept)
        {
            result.Status = DiameterSendStatus.PeerUnavailable;
            result.Code = "peer-unavailable";
            return result;
        }

        DiameterPendingRequestSnapshot pending =
            new DiameterPendingRequestSnapshot();
        pending.RequestIdentifier = NextIdentifier;
        NextIdentifier = NextIdentifier + 1;
        pending.SendRequest = request;
        pending.OutboundMessage = request.Request;
        pending.PeerIdentity.Value = "peer.remote.example";
        pending.AttemptCount = 1;
        Pending.Add(pending);
        result.Status = DiameterSendStatus.Accepted;
        result.PendingRequest = pending;
        result.PeerIdentity = pending.PeerIdentity;
        result.Accepted = true;
        result.Delivered = true;
        result.Code = "request-delivered";
        return result;
    }

    public DiameterAnswerResult CancelDiameterRequest(
        long requestIdentifier)
    {
        DiameterAnswerResult result = new DiameterAnswerResult();
        result.RequestIdentifier = requestIdentifier;
        result.Cancelled = true;
        return result;
    }

    public List<DiameterPendingRequestSnapshot> ReadPendingDiameterRequests()
    {
        return Pending;
    }

    internal void FailAccountingRequest(
        int index,
        int attemptCount,
        bool retransmitted)
    {
        DiameterPendingRequestSnapshot pending = Pending[index];
        pending.AttemptCount = attemptCount;
        if (retransmitted)
        {
            pending.OutboundMessage.Flags =
                pending.OutboundMessage.Flags |
                DiameterMessageFlags.PotentiallyRetransmitted;
        }

        DiameterAnswerResult result = new DiameterAnswerResult();
        result.RequestIdentifier = pending.RequestIdentifier;
        result.PendingRequest = pending;
        result.PeerIdentity = pending.PeerIdentity;
        result.PeerFailed = true;
        result.Code = "peer-failed";
        IDiameterAnswerHandler? handler = Handlers[index];
        if (handler != null)
        {
            handler.HandleDiameterAnswer(result);
        }
    }

    internal void CompleteAccountingRequest(int index)
    {
        DiameterPendingRequestSnapshot pending = Pending[index];
        DiameterMessage request = Requests[index].Request;
        DiameterMessage answer = new DiameterMessage();
        answer.CommandCode = request.CommandCode;
        answer.ApplicationId = request.ApplicationId;
        answer.HopByHopIdentifier = request.HopByHopIdentifier;
        answer.EndToEndIdentifier = request.EndToEndIdentifier;
        answer.Avps.Add(ClonePolicyAccountingAvp(
            FindPolicyAccountingAvp(request, DiameterAvpCodes.SessionId)));
        answer.Avps.Add(CreatePolicyAccountingUnsigned32(
            DiameterAvpCodes.ResultCode,
            DiameterResultCodes.Success));
        answer.Avps.Add(ClonePolicyAccountingAvp(
            FindPolicyAccountingAvp(
                request,
                DiameterAvpCodes.AccountingRecordType)));
        answer.Avps.Add(ClonePolicyAccountingAvp(
            FindPolicyAccountingAvp(
                request,
                DiameterAvpCodes.AccountingRecordNumber)));
        DiameterAnswerResult result = new DiameterAnswerResult();
        result.RequestIdentifier = pending.RequestIdentifier;
        result.PendingRequest = pending;
        result.Answer = answer;
        result.PeerIdentity = pending.PeerIdentity;
        result.AnswerReceived = true;
        result.Correlated = true;
        result.Successful = true;
        result.ResultCode = DiameterResultCodes.Success;
        IDiameterAnswerHandler? handler = Handlers[index];
        if (handler != null)
        {
            handler.HandleDiameterAnswer(result);
        }
    }

    internal DiameterAvp FindPolicyAccountingAvp(
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
            "The policy accounting AVP is absent.");
    }

    internal DiameterAvp ClonePolicyAccountingAvp(DiameterAvp source)
    {
        return AccountingRecordCloner.CloneAccountingAvp(source);
    }

    internal DiameterAvp CreatePolicyAccountingUnsigned32(
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
}
