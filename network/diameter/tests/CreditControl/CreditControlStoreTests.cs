using System;
using System.Collections.Generic;

namespace diameter.Tests;

public class CreditControlStoreTests
{
    [Test]
    public void InMemoryStoreOwnsSessionsAndEnforcesOptimisticVersions()
    {
        DiameterInMemoryCreditControlStore store =
            new DiameterInMemoryCreditControlStore();
        CreditControlClientStateMachine machine =
            new CreditControlClientStateMachine(store, CreateStoreClock());
        DiameterCreditControlRequest request =
            CreateStoreRequest("store-client.example;credit;1");
        machine.BeginCreditControlClientSession(request, 10);

        DiameterCreditControlSession loaded =
            store.LoadDiameterCreditControlSession(request.SessionId)!;
        loaded.Code = "store-updated";
        DiameterCreditControlSession updated =
            store.SaveDiameterCreditControlSession(loaded, 1);
        Assert.Throws<DiameterCreditControlException>(() =>
            store.SaveDiameterCreditControlSession(loaded, 1));
        loaded.Code = "mutated-after-save";
        loaded.CorrelationId[0] = 99;
        DiameterCreditControlSession retained =
            store.LoadDiameterCreditControlSession(request.SessionId)!;

        Assert.Multiple(() =>
        {
            Assert.That(updated.Version, Is.EqualTo(2));
            Assert.That(updated.Code, Is.EqualTo("store-updated"));
            Assert.That(retained.Version, Is.EqualTo(2));
            Assert.That(retained.Code, Is.EqualTo("store-updated"));
            Assert.That(retained.CorrelationId,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void InMemoryStorePreservesCreationOrderAndAtomicRemoval()
    {
        DiameterInMemoryCreditControlStore store =
            new DiameterInMemoryCreditControlStore();
        CreditControlClientStateMachine machine =
            new CreditControlClientStateMachine(store, CreateStoreClock());
        machine.BeginCreditControlClientSession(
            CreateStoreRequest("store-client.example;credit;20"),
            20);
        machine.BeginCreditControlClientSession(
            CreateStoreRequest("store-client.example;credit;21"),
            21);

        bool conflict = store.RemoveDiameterCreditControlSession(
            "store-client.example;credit;20",
            2);
        bool removed = store.RemoveDiameterCreditControlSession(
            "store-client.example;credit;20",
            1);
        bool missing = store.RemoveDiameterCreditControlSession(
            "store-client.example;credit;20",
            1);
        List<DiameterCreditControlSession> sessions =
            store.ReadStoredDiameterCreditControlSessions();
        sessions[0].SessionId = "mutated";

        Assert.Multiple(() =>
        {
            Assert.That(conflict, Is.False);
            Assert.That(removed, Is.True);
            Assert.That(missing, Is.False);
            Assert.That(sessions, Has.Count.EqualTo(1));
            Assert.That(store.ReadStoredDiameterCreditControlSessions()[0]
                .SessionId,
                Is.EqualTo("store-client.example;credit;21"));
        });
    }

    [Test]
    public void InMemoryStoreRejectsMissingInvalidAndExhaustedVersions()
    {
        DiameterInMemoryCreditControlStore store =
            new DiameterInMemoryCreditControlStore();
        DiameterCreditControlSession missing =
            new DiameterCreditControlSession();
        missing.SessionId = "missing.example;credit;1";
        missing.ServiceContextId = "example.store";
        missing.DestinationRealm = new DiameterIdentity();
        missing.DestinationRealm.Value = "remote.example";
        missing.CreatedAt = DateTimeOffset.FromUnixTimeSeconds(1000);
        missing.UpdatedAt = missing.CreatedAt;

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                store.SaveDiameterCreditControlSession(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                store.SaveDiameterCreditControlSession(missing, -1));
            Assert.Throws<DiameterCreditControlException>(() =>
                store.SaveDiameterCreditControlSession(missing, 1));
            Assert.That(
                store.RemoveDiameterCreditControlSession(
                    missing.SessionId,
                    0),
                Is.False);
        });

        missing.Version = long.MaxValue;
        store.Sessions.Add(missing);
        Assert.Throws<DiameterCreditControlException>(() =>
            store.SaveDiameterCreditControlSession(
                missing,
                long.MaxValue));
    }

    private static DiameterCreditControlRequest CreateStoreRequest(
        string sessionId)
    {
        DiameterCreditControlRequest result =
            new DiameterCreditControlRequest();
        result.SessionId = sessionId;
        result.DestinationRealm.Value = "remote.example";
        result.ServiceContextId = "example.store";
        result.RequestType = DiameterCreditControlRequestType.Initial;
        result.CorrelationId = new byte[] { 1, 2, 3 };
        return result;
    }

    private static TestDiameterClock CreateStoreClock()
    {
        return new TestDiameterClock(
            new DateTimeOffset(
                2026,
                8,
                29,
                12,
                0,
                0,
                TimeSpan.Zero),
            0);
    }
}
