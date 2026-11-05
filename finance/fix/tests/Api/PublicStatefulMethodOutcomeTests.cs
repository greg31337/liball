using System;
using System.IO;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class PublicStatefulMethodOutcomeTests
{
    [Test]
    public void SessionMethodsReturnExplicitResultsAcrossLifecycleStates()
    {
        FixSessionTests.SessionFixture fixture = new FixSessionTests.SessionFixture(false);
        IFixSession session = fixture.Session;

        SessionSnapshot initial = session.GetSessionSnapshot();
        SessionPollResult idlePoll = session.PollSession();
        SessionSendResult missing = session.SendMessage(null!);
        SessionPollResult invalidLogout = session.LogoutSession("not-started");
        SessionPollResult started = session.StartSession();
        SessionPollResult duplicateStart = session.StartSession();
        SessionSendResult inactiveSend = session.SendMessage(new FixMessage { MsgType = "D" });
        SessionPollResult activePoll = session.PollSession();
        SessionPollResult stopped = session.StopSession();
        SessionSnapshot final = session.GetSessionSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(initial.State, Is.EqualTo(SessionState.Stopped));
            Assert.That(idlePoll.State, Is.EqualTo(SessionState.Stopped));
            Assert.That(missing.Accepted, Is.False);
            Assert.That(missing.Code, Is.EqualTo("missing-send-message"));
            Assert.That(invalidLogout.State, Is.EqualTo(SessionState.Stopped));
            Assert.That(invalidLogout.Events, Has.Count.EqualTo(1));
            Assert.That(started.State, Is.EqualTo(SessionState.LogonPending));
            Assert.That(duplicateStart.Events.Exists(item => item.Code == "session-already-started"), Is.True);
            Assert.That(inactiveSend.Accepted, Is.False);
            Assert.That(inactiveSend.Code, Is.EqualTo("session-not-active"));
            Assert.That(activePoll.State, Is.EqualTo(SessionState.LogonPending));
            Assert.That(stopped.State, Is.EqualTo(SessionState.Stopped));
            Assert.That(final.State, Is.EqualTo(SessionState.Stopped));
        });
    }

    [Test]
    public void EngineMethodsTransitionRegistrationAndRejectMethodMisuseAtomically()
    {
        ISessionEngine engine = MultiSessionTests.CreateEngine();
        SessionSettings settings = MultiSessionTests.InactiveSettings("PUBLIC", "REMOTE", "CONTRACT");

        SessionPollResult added = engine.AddSession(settings);
        SessionSnapshot resolved = engine.ResolveSessionSnapshot(settings.Id);
        SessionPollResult polled = engine.PollSessions();
        SessionPollResult removed = engine.RemoveSession(settings.Id);
        SessionPollResult empty = engine.PollSessions();

        Assert.Multiple(() =>
        {
            Assert.That(added.State, Is.EqualTo(SessionState.Disconnected));
            Assert.That(resolved.State, Is.EqualTo(SessionState.Disconnected));
            Assert.That(polled.State, Is.EqualTo(SessionState.Disconnected));
            Assert.That(removed.State, Is.EqualTo(SessionState.Stopped));
            Assert.That(empty.State, Is.EqualTo(SessionState.Stopped));
            Assert.That(empty.Events, Is.Empty);
            Assert.Throws<FixSessionException>(() => engine.AddSession(null!));
            Assert.Throws<FixSessionException>(() => engine.ResolveSessionSnapshot(settings.Id));
            Assert.Throws<FixSessionException>(() => engine.RemoveSession(settings.Id));
        });
    }

    [Test]
    public void StoreMethodsPreserveBoundaryStateAndDetachedRecords()
    {
        IMessageStore store = new MemoryMessageStore(1);
        StoredMessage first = MessageStoreContractTests.Message(1, "D", 10);
        StoredMessage second = MessageStoreContractTests.Message(2, "F", 20);

        StoreRangeResult initial = store.RefreshStore();
        StoreRangeResult saved = store.SaveSequenceState(int.MaxValue, int.MaxValue);
        StoreRangeResult firstStored = store.StoreMessage(first);
        StoreRangeResult secondStored = store.StoreMessage(second);
        StoreRangeResult range = store.ReadMessageRange(1, 0);
        StoreRangeResult reset = store.ResetStore();

        Assert.Multiple(() =>
        {
            MessageStoreContractTests.AssertState(initial, 1, 1);
            MessageStoreContractTests.AssertState(saved, int.MaxValue, int.MaxValue);
            Assert.That(firstStored.Success, Is.True, firstStored.Code);
            Assert.That(secondStored.Success, Is.True, secondStored.Code);
            Assert.That(range.Messages.ConvertAll(message => message.SequenceNumber), Is.EqualTo(new[] { 2 }));
            Assert.That(range.HasGap, Is.True);
            MessageStoreContractTests.AssertState(reset, 1, 1);
            Assert.That(first.Bytes[0], Is.EqualTo(10));
            Assert.That(second.Bytes[0], Is.EqualTo(20));
        });
    }

    [Test]
    public void TransportMethodsExposeConnectedAndClosedBoundaryStates()
    {
        using MemoryStream stream = new MemoryStream();
        ISessionTransport transport = new StreamSessionTransport(stream);

        TransportWriteResult opened = transport.OpenTransport(new SessionEndpoint(), new SessionTlsSettings());
        TransportWriteResult emptyWrite = transport.WriteTransport(ReadOnlySpan<byte>.Empty);
        TransportWriteResult written = transport.WriteTransport([1, 2, 3]);
        TransportWriteResult closed = transport.CloseTransport();
        TransportReadResult read = transport.ReadTransport(new byte[1]);

        Assert.Multiple(() =>
        {
            Assert.That(opened.State, Is.EqualTo(TransportState.Connected));
            Assert.That(emptyWrite.State, Is.EqualTo(TransportState.Connected));
            Assert.That(emptyWrite.BytesWritten, Is.Zero);
            Assert.That(written.BytesWritten, Is.EqualTo(3));
            Assert.That(closed.State, Is.EqualTo(TransportState.Closed));
            Assert.That(read.State, Is.EqualTo(TransportState.Closed));
            Assert.That(read.ErrorCode, Is.EqualTo("transport-closed"));
            Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }
}
