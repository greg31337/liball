using System;
using System.Collections.Generic;

namespace diameter.Tests;

public class CreditControlClientStateMachineTests
{
    [Test]
    public void ClientSessionPersistsMonotonicInitialUpdateAndTermination()
    {
        DateTimeOffset initialTime =
            new DateTimeOffset(2026, 8, 29, 12, 0, 0, TimeSpan.Zero);
        TestDiameterClock clock = new TestDiameterClock(initialTime, 0);
        TestCreditControlStore store = new TestCreditControlStore();
        CreditControlClientStateMachine machine =
            new CreditControlClientStateMachine(store, clock);
        DiameterCreditControlRequest initial = CreateClientRequest(
            DiameterCreditControlRequestType.Initial);
        initial.ApplicationState = new byte[] { 1, 2 };

        DiameterCreditControlResult pendingInitial =
            machine.BeginCreditControlClientSession(initial, 101);
        initial.UserName = "changed@example.net";
        initial.ApplicationState[0] = 9;
        DiameterCreditControlSession? persistedInitial =
            store.LoadDiameterCreditControlSession(
                "client.example;credit;1");

        Assert.Multiple(() =>
        {
            Assert.That(pendingInitial.State,
                Is.EqualTo(DiameterCreditControlState.PendingInitial));
            Assert.That(pendingInitial.Request.RequestNumber, Is.Zero);
            Assert.That(pendingInitial.Session!.NextRequestNumber,
                Is.EqualTo(1U));
            Assert.That(pendingInitial.Session.PendingRequestIdentifier,
                Is.EqualTo(101));
            Assert.That(pendingInitial.Session.TransactionDeadline,
                Is.EqualTo(initialTime.AddSeconds(2)));
            Assert.That(persistedInitial!.UserName,
                Is.EqualTo("alice@remote.example"));
            Assert.That(persistedInitial.ApplicationState[0], Is.EqualTo(1));
            Assert.That(persistedInitial.Version, Is.EqualTo(1));
        });

        clock.UtcTime = initialTime.AddMilliseconds(100);
        DiameterCreditControlResult opened =
            machine.ProcessCreditControlClientAnswer(
                101,
                CreateClientAnswer(
                    DiameterCreditControlRequestType.Initial,
                    0U,
                    DiameterResultCodes.Success));
        CreditControlClientStateMachine recovered =
            new CreditControlClientStateMachine(store, clock);
        DiameterCreditControlRequest update = CreateClientRequest(
            DiameterCreditControlRequestType.Update);
        update.RequestNumber = 900U;
        DiameterCreditControlResult pendingUpdate =
            recovered.UpdateCreditControlClientSession(update, 102);

        clock.UtcTime = initialTime.AddMilliseconds(200);
        DiameterCreditControlResult updated =
            recovered.ProcessCreditControlClientAnswer(
                102,
                CreateClientAnswer(
                    DiameterCreditControlRequestType.Update,
                    1U,
                    DiameterResultCodes.Success));
        DiameterCreditControlRequest termination = CreateClientRequest(
            DiameterCreditControlRequestType.Termination);
        termination.RequestNumber = 800U;
        DiameterCreditControlResult pendingTermination =
            recovered.TerminateCreditControlClientSession(
                termination,
                103);
        DiameterCreditControlResult closed =
            recovered.ProcessCreditControlClientAnswer(
                103,
                CreateClientAnswer(
                    DiameterCreditControlRequestType.Termination,
                    2U,
                    DiameterResultCodes.Success));

        Assert.Multiple(() =>
        {
            Assert.That(opened.State,
                Is.EqualTo(DiameterCreditControlState.Open));
            Assert.That(opened.ContinueService, Is.True);
            Assert.That(pendingUpdate.Request.RequestNumber, Is.EqualTo(1U));
            Assert.That(pendingUpdate.Session!.NextRequestNumber,
                Is.EqualTo(2U));
            Assert.That(pendingUpdate.Session.PendingRequestType,
                Is.EqualTo(DiameterCreditControlRequestType.Update));
            Assert.That(updated.State,
                Is.EqualTo(DiameterCreditControlState.Open));
            Assert.That(pendingTermination.Request.RequestNumber,
                Is.EqualTo(2U));
            Assert.That(pendingTermination.Session!.NextRequestNumber,
                Is.EqualTo(3U));
            Assert.That(closed.State,
                Is.EqualTo(DiameterCreditControlState.Closed));
            Assert.That(closed.Successful, Is.True);
            Assert.That(closed.TerminateService, Is.True);
            Assert.That(
                store.LoadDiameterCreditControlSession(
                    "client.example;credit;1"),
                Is.Null);
        });
    }

    [Test]
    public void TxExpiryRecoversPersistedPendingSessionAndCleansItUp()
    {
        DateTimeOffset initialTime =
            new DateTimeOffset(2026, 8, 29, 13, 0, 0, TimeSpan.Zero);
        TestDiameterClock clock = new TestDiameterClock(initialTime, 0);
        TestCreditControlStore store = new TestCreditControlStore();
        CreditControlClientStateMachine machine =
            new CreditControlClientStateMachine(store, clock);
        DiameterCreditControlRequest request = CreateClientRequest(
            DiameterCreditControlRequestType.Initial);
        request.TimeoutMilliseconds = 1000;
        machine.BeginCreditControlClientSession(request, 201);

        clock.UtcTime = initialTime.AddMilliseconds(1001);
        CreditControlClientStateMachine recovered =
            new CreditControlClientStateMachine(store, clock);
        List<DiameterCreditControlResult> expired =
            recovered.ExpireCreditControlClientTransactions();
        DiameterCreditControlSession expiredSession = expired[0].Session!;

        Assert.Multiple(() =>
        {
            Assert.That(expired, Has.Count.EqualTo(1));
            Assert.That(expired[0].State,
                Is.EqualTo(DiameterCreditControlState.Failed));
            Assert.That(expired[0].TerminateService, Is.True);
            Assert.That(expired[0].Code,
                Is.EqualTo("credit-control-tx-expired"));
            Assert.That(expiredSession.PendingRequestPresent, Is.False);
            Assert.That(expiredSession.PendingRequestIdentifier, Is.Zero);
            Assert.That(
                recovered.FindCreditControlClientSession(request.SessionId),
                Is.Null);
            Assert.That(
                recovered.ExpireCreditControlClientTransactions(),
                Is.Empty);
        });
    }

    [Test]
    public void PendingInterrogationRejectsConcurrentAndMismatchedAnswers()
    {
        DateTimeOffset initialTime =
            new DateTimeOffset(2026, 8, 29, 14, 0, 0, TimeSpan.Zero);
        TestDiameterClock clock = new TestDiameterClock(initialTime, 0);
        TestCreditControlStore store = new TestCreditControlStore();
        CreditControlClientStateMachine machine =
            new CreditControlClientStateMachine(store, clock);
        DiameterCreditControlRequest request = CreateClientRequest(
            DiameterCreditControlRequestType.Initial);
        machine.BeginCreditControlClientSession(request, 301);

        DiameterCreditControlAnswer answer = CreateClientAnswer(
            DiameterCreditControlRequestType.Initial,
            0U,
            DiameterResultCodes.Success);

        Assert.Multiple(() =>
        {
            Assert.Throws<DiameterCreditControlException>(() =>
                machine.BeginCreditControlClientSession(request, 302));
            Assert.Throws<DiameterCreditControlException>(() =>
                machine.ProcessCreditControlClientAnswer(999, answer));
            Assert.That(
                machine.FindCreditControlClientSession(request.SessionId)!
                    .PendingRequestIdentifier,
                Is.EqualTo(301));
        });
    }

    [Test]
    public void RejectedInitialAnswerReturnsFailureAndRemovesSession()
    {
        DateTimeOffset initialTime =
            new DateTimeOffset(2026, 8, 29, 15, 0, 0, TimeSpan.Zero);
        TestDiameterClock clock = new TestDiameterClock(initialTime, 0);
        TestCreditControlStore store = new TestCreditControlStore();
        CreditControlClientStateMachine machine =
            new CreditControlClientStateMachine(store, clock);
        DiameterCreditControlRequest request = CreateClientRequest(
            DiameterCreditControlRequestType.Initial);
        machine.BeginCreditControlClientSession(request, 401);

        DiameterCreditControlResult failed =
            machine.ProcessCreditControlClientAnswer(
                401,
                CreateClientAnswer(
                    DiameterCreditControlRequestType.Initial,
                    0U,
                    DiameterResultCodes.CreditLimitReached));

        Assert.Multiple(() =>
        {
            Assert.That(failed.State,
                Is.EqualTo(DiameterCreditControlState.Failed));
            Assert.That(failed.AnswerReceived, Is.True);
            Assert.That(failed.Successful, Is.False);
            Assert.That(failed.TerminateService, Is.True);
            Assert.That(failed.ResultCode,
                Is.EqualTo(DiameterResultCodes.CreditLimitReached));
            Assert.That(
                store.LoadDiameterCreditControlSession(request.SessionId),
                Is.Null);
        });
    }

    private static DiameterCreditControlRequest CreateClientRequest(
        DiameterCreditControlRequestType requestType)
    {
        DiameterCreditControlRequest result =
            new DiameterCreditControlRequest();
        result.SessionId = "client.example;credit;1";
        result.DestinationRealm.Value = "remote.example";
        result.DestinationHost = new DiameterIdentity();
        result.DestinationHost.Value = "credit.remote.example";
        result.PeerIdentity = new DiameterIdentity();
        result.PeerIdentity.Value = "peer.remote.example";
        result.ServiceContextId = "example.service";
        result.UserName = "alice@remote.example";
        result.RequestType = requestType;
        result.TimeoutMilliseconds = 2000;
        result.RouteIdentifier = "credit-route";
        result.CorrelationText = "credit-operation";
        if (requestType == DiameterCreditControlRequestType.Termination)
        {
            result.TerminationCause = DiameterTerminationCause.Logout;
            result.TerminationCausePresent = true;
        }

        return result;
    }

    private static DiameterCreditControlAnswer CreateClientAnswer(
        DiameterCreditControlRequestType requestType,
        uint requestNumber,
        uint resultCode)
    {
        DiameterCreditControlAnswer result =
            new DiameterCreditControlAnswer();
        result.SessionId = "client.example;credit;1";
        result.RequestType = requestType;
        result.RequestNumber = requestNumber;
        result.ResultCode = resultCode;
        result.Disposition = resultCode >= 2000U && resultCode < 3000U ?
            DiameterCreditControlDisposition.Granted :
            DiameterCreditControlDisposition.Failed;
        return result;
    }
}

internal class TestCreditControlStore : ICreditControlStore
{
    internal List<DiameterCreditControlSession> Sessions =
        new List<DiameterCreditControlSession>();

    public DiameterCreditControlSession? LoadDiameterCreditControlSession(
        string? sessionId)
    {
        int index = FindCreditControlSessionIndex(sessionId);
        if (index < 0)
        {
            return null;
        }

        return CreditControlDataCloner.CloneCreditControlSession(
            Sessions[index]);
    }

    public DiameterCreditControlSession SaveDiameterCreditControlSession(
        DiameterCreditControlSession? session,
        long expectedVersion)
    {
        if (session == null)
        {
            throw new ArgumentException("A session is required.");
        }

        session.ValidateCreditControlSession();
        int index = FindCreditControlSessionIndex(session.SessionId);
        if (index < 0)
        {
            if (expectedVersion != 0)
            {
                throw new DiameterCreditControlException(
                    "The expected session version does not exist.");
            }

            DiameterCreditControlSession created =
                CreditControlDataCloner.CloneCreditControlSession(session);
            created.Version = 1;
            Sessions.Add(created);
            return CreditControlDataCloner.CloneCreditControlSession(created);
        }

        if (Sessions[index].Version != expectedVersion)
        {
            throw new DiameterCreditControlException(
                "The expected session version changed.");
        }

        DiameterCreditControlSession updated =
            CreditControlDataCloner.CloneCreditControlSession(session);
        updated.Version = expectedVersion + 1;
        Sessions[index] = updated;
        return CreditControlDataCloner.CloneCreditControlSession(updated);
    }

    public bool RemoveDiameterCreditControlSession(
        string? sessionId,
        long expectedVersion)
    {
        int index = FindCreditControlSessionIndex(sessionId);
        if (index < 0 || Sessions[index].Version != expectedVersion)
        {
            return false;
        }

        Sessions.RemoveAt(index);
        return true;
    }

    public List<DiameterCreditControlSession>
        ReadStoredDiameterCreditControlSessions()
    {
        List<DiameterCreditControlSession> result =
            new List<DiameterCreditControlSession>();
        int index = 0;
        while (index < Sessions.Count)
        {
            result.Add(CreditControlDataCloner.CloneCreditControlSession(
                Sessions[index]));
            index = index + 1;
        }

        return result;
    }

    internal int FindCreditControlSessionIndex(string? sessionId)
    {
        int index = 0;
        while (index < Sessions.Count)
        {
            if (Sessions[index].SessionId == sessionId)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }
}
