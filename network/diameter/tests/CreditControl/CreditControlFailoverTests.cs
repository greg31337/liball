using System;
using System.Collections.Generic;

namespace diameter.Tests;

public class CreditControlFailoverTests
{
    [Test]
    public void NegotiatedFailoverRetainsAffinityRejectsDuplicateWorkAndFailsBack()
    {
        DateTimeOffset now =
            new DateTimeOffset(2026, 8, 29, 20, 0, 0, TimeSpan.Zero);
        TestDiameterClock clock = new TestDiameterClock(now, 0);
        TestCreditControlStore store = new TestCreditControlStore();
        CreditControlClientStateMachine machine =
            new CreditControlClientStateMachine(store, clock);
        DiameterCreditControlRequest initial = CreateFailoverRequest(
            DiameterCreditControlRequestType.Initial);
        machine.BeginCreditControlClientSession(initial, 101);
        DiameterCreditControlAnswer initialAnswer = CreateFailoverAnswer(
            DiameterCreditControlRequestType.Initial,
            0U,
            DiameterResultCodes.Success);
        initialAnswer.SessionFailoverPresent = true;
        initialAnswer.SessionFailoverSupported = true;
        machine.ProcessCreditControlClientAnswer(101, initialAnswer);

        machine.UpdateCreditControlClientSession(
            CreateFailoverRequest(DiameterCreditControlRequestType.Update),
            102);
        DiameterCreditControlResult failed =
            machine.ProcessCreditControlClientAnswer(
                102,
                CreateFailoverAnswer(
                    DiameterCreditControlRequestType.Update,
                    1U,
                    DiameterResultCodes.TooBusy));
        DiameterCreditControlAnswer success = CreateFailoverAnswer(
            DiameterCreditControlRequestType.Update,
            1U,
            DiameterResultCodes.Success);
        DiameterCreditControlResult recovered =
            machine.ProcessCreditControlClientAnswer(102, success);
        long completedVersion = recovered.Session!.Version;
        DiameterCreditControlResult duplicate =
            machine.ProcessCreditControlClientAnswer(102, success);

        DiameterCreditControlResult sticky =
            machine.UpdateCreditControlClientSession(
                CreateFailoverRequest(
                    DiameterCreditControlRequestType.Update),
                103);
        machine.ProcessCreditControlClientAnswer(
            103,
            CreateFailoverAnswer(
                DiameterCreditControlRequestType.Update,
                2U,
                DiameterResultCodes.Success));
        DiameterCreditControlSession primary =
            machine.FailbackCreditControlClientSession(
                initial.SessionId);
        DiameterCreditControlResult failedBack =
            machine.UpdateCreditControlClientSession(
                CreateFailoverRequest(
                    DiameterCreditControlRequestType.Update),
                104);

        Assert.Multiple(() =>
        {
            Assert.That(failed.RetryRequired, Is.True);
            Assert.That(failed.FailoverAttempted, Is.True);
            Assert.That(failed.Retransmission, Is.True);
            Assert.That(failed.Request.RequestNumber, Is.EqualTo(1U));
            Assert.That(failed.Request.RouteIdentifier,
                Is.EqualTo("alternate-route"));
            Assert.That(failed.Request.DestinationHost!.Value,
                Is.EqualTo("secondary.credit.example"));
            Assert.That(failed.Session!.PendingRequestIdentifier,
                Is.EqualTo(102));
            Assert.That(failed.Session.AttemptCount, Is.EqualTo(2));
            Assert.That(failed.Session.FailoverActive, Is.True);
            Assert.That(failed.Session.PrimaryServer, Is.Not.Null);
            Assert.That(failed.Session.PrimaryServer!.RouteIdentifier,
                Is.EqualTo("primary-route"));
            Assert.That(failed.Session.AttemptedServers,
                Has.Count.EqualTo(2));
            Assert.That(failed.Session.LastOperationResult, Is.Not.Null);
            Assert.That(failed.Session.LastOperationResult!.RetryRequired,
                Is.True);
            Assert.That(recovered.State,
                Is.EqualTo(DiameterCreditControlState.Open));
            Assert.That(duplicate.Duplicate, Is.True);
            Assert.That(duplicate.Retransmission, Is.True);
            Assert.That(duplicate.Session!.Version,
                Is.EqualTo(completedVersion));
            Assert.That(sticky.Request.RouteIdentifier,
                Is.EqualTo("alternate-route"));
            Assert.That(primary.FailoverActive, Is.False);
            Assert.That(primary.RouteIdentifier,
                Is.EqualTo("primary-route"));
            Assert.That(failedBack.Request.RouteIdentifier,
                Is.EqualTo("primary-route"));
            Assert.That(failedBack.Request.DestinationHost!.Value,
                Is.EqualTo("primary.credit.example"));
        });
    }

    [Test]
    public void ServerDecliningSessionFailoverMakesUpdateFailureTerminal()
    {
        TestDiameterClock clock = new TestDiameterClock(
            new DateTimeOffset(
                2026,
                8,
                29,
                21,
                0,
                0,
                TimeSpan.Zero),
            0);
        TestCreditControlStore store = new TestCreditControlStore();
        CreditControlClientStateMachine machine =
            new CreditControlClientStateMachine(store, clock);
        DiameterCreditControlRequest initial = CreateFailoverRequest(
            DiameterCreditControlRequestType.Initial);
        machine.BeginCreditControlClientSession(initial, 201);
        DiameterCreditControlAnswer initialAnswer = CreateFailoverAnswer(
            DiameterCreditControlRequestType.Initial,
            0U,
            DiameterResultCodes.Success);
        initialAnswer.SessionFailoverPresent = true;
        initialAnswer.SessionFailoverSupported = false;
        machine.ProcessCreditControlClientAnswer(201, initialAnswer);
        machine.UpdateCreditControlClientSession(
            CreateFailoverRequest(DiameterCreditControlRequestType.Update),
            202);

        DiameterCreditControlResult failed =
            machine.ProcessCreditControlClientAnswer(
                202,
                CreateFailoverAnswer(
                    DiameterCreditControlRequestType.Update,
                    1U,
                    DiameterResultCodes.TooBusy));

        Assert.Multiple(() =>
        {
            Assert.That(failed.RetryRequired, Is.False);
            Assert.That(failed.FailoverAttempted, Is.False);
            Assert.That(failed.TerminateService, Is.True);
            Assert.That(
                machine.FindCreditControlClientSession(initial.SessionId),
                Is.Null);
        });
    }

    [Test]
    public void PersistedPendingRequestRecoversWithSameIdentityOnAlternate()
    {
        DateTimeOffset now =
            new DateTimeOffset(2026, 8, 29, 22, 0, 0, TimeSpan.Zero);
        TestDiameterClock clock = new TestDiameterClock(now, 0);
        TestCreditControlStore store = new TestCreditControlStore();
        CreditControlClientStateMachine machine =
            new CreditControlClientStateMachine(store, clock);
        DiameterCreditControlRequest initial = CreateFailoverRequest(
            DiameterCreditControlRequestType.Initial);
        machine.BeginCreditControlClientSession(initial, 301);

        clock.UtcTime = now.AddMilliseconds(500);
        CreditControlClientStateMachine restarted =
            new CreditControlClientStateMachine(store, clock);
        List<DiameterCreditControlResult> recovered =
            restarted.RecoverCreditControlClientTransactions();
        DiameterCreditControlSession recoveredSession =
            recovered[0].Session!;

        Assert.Multiple(() =>
        {
            Assert.That(recovered, Has.Count.EqualTo(1));
            Assert.That(recovered[0].RetryRequired, Is.True);
            Assert.That(recovered[0].Retransmission, Is.True);
            Assert.That(recovered[0].FailoverAttempted, Is.True);
            Assert.That(recovered[0].Delivered, Is.False);
            Assert.That(recovered[0].Request.RequestNumber, Is.Zero);
            Assert.That(recovered[0].Request.RouteIdentifier,
                Is.EqualTo("alternate-route"));
            Assert.That(recoveredSession.PendingRequestIdentifier,
                Is.EqualTo(301));
            Assert.That(recoveredSession.AttemptCount, Is.EqualTo(2));
            Assert.That(recoveredSession.TransactionDeadline,
                Is.EqualTo(clock.UtcTime.AddSeconds(2)));
        });
    }

    private static DiameterCreditControlRequest CreateFailoverRequest(
        DiameterCreditControlRequestType requestType)
    {
        DiameterCreditControlRequest result =
            new DiameterCreditControlRequest();
        result.SessionId = "failover-client.example;credit;1";
        result.DestinationRealm.Value = "credit.example";
        result.DestinationHost = new DiameterIdentity();
        result.DestinationHost.Value = "primary.credit.example";
        result.PeerIdentity = new DiameterIdentity();
        result.PeerIdentity.Value = "primary-peer.example";
        result.ServiceContextId = "failover.service";
        result.UserName = "subscriber@credit.example";
        result.RequestType = requestType;
        result.TimeoutMilliseconds = 2000;
        result.AllowFailover = true;
        result.FailureHandling =
            DiameterCreditControlFailureHandling.RetryAndTerminate;
        result.RouteIdentifier = "primary-route";
        DiameterCreditControlServer alternate =
            new DiameterCreditControlServer();
        alternate.DestinationHost = new DiameterIdentity();
        alternate.DestinationHost.Value =
            "secondary.credit.example";
        alternate.PeerIdentity = new DiameterIdentity();
        alternate.PeerIdentity.Value = "secondary-peer.example";
        alternate.RouteIdentifier = "alternate-route";
        result.AlternateServers.Add(alternate);
        return result;
    }

    private static DiameterCreditControlAnswer CreateFailoverAnswer(
        DiameterCreditControlRequestType requestType,
        uint requestNumber,
        uint resultCode)
    {
        DiameterCreditControlAnswer result =
            new DiameterCreditControlAnswer();
        result.SessionId = "failover-client.example;credit;1";
        result.RequestType = requestType;
        result.RequestNumber = requestNumber;
        result.ResultCode = resultCode;
        result.Disposition = resultCode >= 2000U && resultCode < 3000U ?
            DiameterCreditControlDisposition.Granted :
            DiameterCreditControlDisposition.Failed;
        return result;
    }
}
