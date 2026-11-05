using System.Collections.Generic;

namespace diameter.Tests;

public class CreditControlContractTests
{
    [Test]
    public void CreditControlContractsCarryOperationsRatingsPersistenceAndEvents()
    {
        CreditControlContractProbe probe =
            new CreditControlContractProbe();
        DiameterCreditControlRequest request =
            new DiameterCreditControlRequest();
        DiameterCreditControlSession session =
            new DiameterCreditControlSession();
        DiameterCreditControlResult eventData =
            new DiameterCreditControlResult();
        IDiameterCreditControlClient client = probe;
        ICreditControlRequestHandler handler = probe;
        ICreditControlStore store = probe;
        ICreditControlEventHandler eventHandler = probe;

        DiameterCreditControlResult initial =
            client.BeginDiameterCreditControlSession(request);
        DiameterCreditControlResult update =
            client.UpdateDiameterCreditControlSession(request);
        DiameterCreditControlResult termination =
            client.TerminateDiameterCreditControlSession(request);
        DiameterCreditControlResult eventResult =
            client.ExecuteDiameterCreditControlEvent(request);
        DiameterCreditControlSession? found =
            client.FindDiameterCreditControlSession("session-1");
        List<DiameterCreditControlSession> clientSessions =
            client.ReadDiameterCreditControlSessions();
        DiameterCreditControlAnswer answer =
            handler.HandleDiameterCreditControlRequest(request);
        DiameterCreditControlSession? loaded =
            store.LoadDiameterCreditControlSession("session-2");
        DiameterCreditControlSession saved =
            store.SaveDiameterCreditControlSession(session, 4L);
        bool removed = store.RemoveDiameterCreditControlSession(
            "session-3",
            5L);
        List<DiameterCreditControlSession> storedSessions =
            store.ReadStoredDiameterCreditControlSessions();
        eventHandler.HandleDiameterCreditControlEvent(eventData);

        Assert.Multiple(() =>
        {
            Assert.That(initial, Is.SameAs(probe.Result));
            Assert.That(update, Is.SameAs(probe.Result));
            Assert.That(termination, Is.SameAs(probe.Result));
            Assert.That(eventResult, Is.SameAs(probe.Result));
            Assert.That(found, Is.SameAs(probe.Session));
            Assert.That(clientSessions, Is.SameAs(probe.Sessions));
            Assert.That(answer, Is.SameAs(probe.Answer));
            Assert.That(loaded, Is.SameAs(probe.Session));
            Assert.That(saved, Is.SameAs(probe.Session));
            Assert.That(removed, Is.True);
            Assert.That(storedSessions, Is.SameAs(probe.Sessions));
            Assert.That(probe.Request, Is.SameAs(request));
            Assert.That(probe.SuppliedSession, Is.SameAs(session));
            Assert.That(probe.EventData, Is.SameAs(eventData));
            Assert.That(probe.SessionId, Is.EqualTo("session-3"));
            Assert.That(probe.ExpectedVersion, Is.EqualTo(5L));
            Assert.That(probe.ClientOperationCount, Is.EqualTo(4));
            Assert.That(probe.EventCount, Is.EqualTo(1));
        });
    }
}

internal class CreditControlContractProbe :
    IDiameterCreditControlClient,
    ICreditControlRequestHandler,
    ICreditControlStore,
    ICreditControlEventHandler
{
    internal DiameterCreditControlResult Result =
        new DiameterCreditControlResult();
    internal DiameterCreditControlAnswer Answer =
        new DiameterCreditControlAnswer();
    internal DiameterCreditControlSession Session =
        new DiameterCreditControlSession();
    internal List<DiameterCreditControlSession> Sessions =
        new List<DiameterCreditControlSession>();
    internal DiameterCreditControlRequest? Request;
    internal DiameterCreditControlSession? SuppliedSession;
    internal DiameterCreditControlResult? EventData;
    internal string SessionId = string.Empty;
    internal long ExpectedVersion;
    internal int ClientOperationCount;
    internal int EventCount;

    public DiameterCreditControlResult BeginDiameterCreditControlSession(
        DiameterCreditControlRequest? request)
    {
        Request = request;
        ClientOperationCount = ClientOperationCount + 1;
        return Result;
    }

    public DiameterCreditControlResult UpdateDiameterCreditControlSession(
        DiameterCreditControlRequest? request)
    {
        Request = request;
        ClientOperationCount = ClientOperationCount + 1;
        return Result;
    }

    public DiameterCreditControlResult
        TerminateDiameterCreditControlSession(
            DiameterCreditControlRequest? request)
    {
        Request = request;
        ClientOperationCount = ClientOperationCount + 1;
        return Result;
    }

    public DiameterCreditControlResult ExecuteDiameterCreditControlEvent(
        DiameterCreditControlRequest? request)
    {
        Request = request;
        ClientOperationCount = ClientOperationCount + 1;
        return Result;
    }

    public DiameterCreditControlSession? FindDiameterCreditControlSession(
        string? sessionId)
    {
        SessionId = sessionId ?? string.Empty;
        return Session;
    }

    public List<DiameterCreditControlSession>
        ReadDiameterCreditControlSessions()
    {
        return Sessions;
    }

    public DiameterCreditControlAnswer HandleDiameterCreditControlRequest(
        DiameterCreditControlRequest? request)
    {
        Request = request;
        return Answer;
    }

    public DiameterCreditControlSession? LoadDiameterCreditControlSession(
        string? sessionId)
    {
        SessionId = sessionId ?? string.Empty;
        return Session;
    }

    public DiameterCreditControlSession SaveDiameterCreditControlSession(
        DiameterCreditControlSession? session,
        long expectedVersion)
    {
        SuppliedSession = session;
        ExpectedVersion = expectedVersion;
        return Session;
    }

    public bool RemoveDiameterCreditControlSession(
        string? sessionId,
        long expectedVersion)
    {
        SessionId = sessionId ?? string.Empty;
        ExpectedVersion = expectedVersion;
        return true;
    }

    public List<DiameterCreditControlSession>
        ReadStoredDiameterCreditControlSessions()
    {
        return Sessions;
    }

    public void HandleDiameterCreditControlEvent(
        DiameterCreditControlResult eventData)
    {
        EventData = eventData;
        EventCount = EventCount + 1;
    }
}
