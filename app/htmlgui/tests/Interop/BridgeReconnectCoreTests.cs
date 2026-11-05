using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class BridgeReconnectCoreTests
{
    [Test]
    public void ReconnectsFromLastAcknowledgedRevisionWithDetachedBatchesAndHandoff()
    {
        UpdateBatchCore updates = UpdateHistory(4, 0, 2);
        BridgeSession original = Session("transport-1", UiTransportState.Ready);
        BridgeReconnectCore reconnect = new BridgeReconnectCore(original, updates);
        UiHydrationManifest handoff = Handoff();
        UiPreservationState focus = FocusState();

        BridgeReconnectResult captured = reconnect.CaptureBridgeHandoff(handoff, focus);
        handoff.State[0].Value.StringValue = "mutated source";
        focus.FocusedNodeId = "mutated-source";
        BridgeReconnectResult started = reconnect.BeginBridgeReconnect();
        BridgeReconnectResult result = reconnect.CompleteBridgeReconnect(Session("transport-2", UiTransportState.Ready), 0);
        result.Batches[0].Mutations[0].Value = "mutated result";
        result.ApplicationState[0].Value.StringValue = "mutated result";
        result.Focus.FocusedNodeId = "mutated-result";
        BridgeReconnectResult repeated = reconnect.CreateBridgeResync("transport-2", 0);

        Assert.Multiple(() =>
        {
            Assert.That(captured.Success, Is.True);
            Assert.That(started.Success, Is.True);
            Assert.That(started.State, Is.EqualTo(UiTransportState.Reconnecting));
            Assert.That(original.State, Is.EqualTo(UiTransportState.Reconnecting));
            Assert.That(result.Success, Is.True);
            Assert.That(result.Applied, Is.True);
            Assert.That(result.Code, Is.EqualTo("bridge.reconnect-incremental"));
            Assert.That(result.PreviousSession, Is.EqualTo("transport-1"));
            Assert.That(result.Session, Is.EqualTo("transport-2"));
            Assert.That(result.BaseRevision, Is.Zero);
            Assert.That(result.Revision, Is.EqualTo(2));
            Assert.That(result.RequiresFullSnapshot, Is.False);
            Assert.That(result.Snapshot, Is.Null);
            Assert.That(result.Batches.Select(item => item.BaseRevision), Is.EqualTo(new int[] { 0, 1 }));
            Assert.That(result.Batches.Select(item => item.Revision), Is.EqualTo(new int[] { 1, 2 }));
            Assert.That(repeated.Batches[0].Mutations[0].Value, Is.EqualTo("Value 1"));
            Assert.That(repeated.ApplicationState.Single().Value.StringValue, Is.EqualTo("saved-state"));
            Assert.That(repeated.RouteUrl, Is.EqualTo("/products/7"));
            Assert.That(repeated.RouteData!.RouteId, Is.EqualTo("product"));
            Assert.That(repeated.RouteData.Parameters.Single().Value.StringValue, Is.EqualTo("7"));
            Assert.That(repeated.HistoryState!.StringValue, Is.EqualTo("history-state"));
            Assert.That(repeated.HistoryEntryId, Is.EqualTo("history-7"));
            Assert.That(repeated.NavigationGeneration, Is.EqualTo(8));
            Assert.That(repeated.Focus.FocusedNodeId, Is.EqualTo("field"));
            Assert.That(repeated.Focus.SelectionStart, Is.EqualTo(2));
            Assert.That(repeated.Focus.ScrollOffsets.Single().Key, Is.EqualTo("scroll"));
        });
    }

    [Test]
    public void FallsBackToDetachedFullSnapshotWhenAcknowledgedHistoryWasEvicted()
    {
        UpdateBatchCore updates = UpdateHistory(2, 0, 3);
        BridgeReconnectCore reconnect = new BridgeReconnectCore(Session("transport-1", UiTransportState.Faulted), updates);
        reconnect.CaptureBridgeHandoff(Handoff(), FocusState());
        reconnect.BeginBridgeReconnect();

        BridgeReconnectResult fallback = reconnect.CompleteBridgeReconnect(Session("transport-2", UiTransportState.Ready), 0);
        fallback.Snapshot!.Roots[0].Children[0].Text = "mutated recovery";
        BridgeReconnectResult repeated = reconnect.CreateBridgeResync("transport-2", 0);

        Assert.Multiple(() =>
        {
            Assert.That(fallback.Success, Is.True);
            Assert.That(fallback.Applied, Is.True);
            Assert.That(fallback.Code, Is.EqualTo("bridge.reconnect-full-snapshot"));
            Assert.That(fallback.RequiresFullSnapshot, Is.True);
            Assert.That(fallback.Batches, Is.Empty);
            Assert.That(fallback.Snapshot.Revision, Is.EqualTo(3));
            Assert.That(repeated.Snapshot!.Roots[0].Children[0].Text, Is.EqualTo("Value 3"));
            Assert.That(updates.History, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void TreatsCurrentRevisionAsIdempotentHandoffOnlyResync()
    {
        UpdateBatchCore updates = UpdateHistory(4, 4, 5);
        BridgeReconnectCore reconnect = new BridgeReconnectCore(Session("transport-1", UiTransportState.Ready), updates);
        reconnect.CaptureBridgeHandoff(Handoff(), FocusState());
        reconnect.BeginBridgeReconnect();

        BridgeReconnectResult result = reconnect.CompleteBridgeReconnect(Session("transport-2", UiTransportState.Ready), 5);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Applied, Is.False);
            Assert.That(result.Code, Is.EqualTo("bridge.reconnect-current"));
            Assert.That(result.BaseRevision, Is.EqualTo(5));
            Assert.That(result.Revision, Is.EqualTo(5));
            Assert.That(result.Batches, Is.Empty);
            Assert.That(result.Snapshot, Is.Null);
            Assert.That(result.ApplicationState.Single().Value.StringValue, Is.EqualTo("saved-state"));
            Assert.That(result.Focus.FocusedNodeId, Is.EqualTo("field"));
        });
    }

    [Test]
    public void IsolatesRetiredSessionsAndRejectsUnknownOrReusedSessionIdentities()
    {
        UpdateBatchCore updates = UpdateHistory(4, 0, 1);
        BridgeReconnectCore reconnect = new BridgeReconnectCore(Session("transport-1", UiTransportState.Ready), updates);
        BridgeReconnectResult firstStart = reconnect.BeginBridgeReconnect();
        BridgeReconnectResult repeatedStart = reconnect.BeginBridgeReconnect();
        BridgeReconnectResult completed = reconnect.CompleteBridgeReconnect(Session("transport-2", UiTransportState.Ready), 0);

        BridgeReconnectResult stale = reconnect.CreateBridgeResync("transport-1", 0);
        BridgeReconnectResult unknown = reconnect.CreateBridgeResync("transport-unknown", 0);
        reconnect.BeginBridgeReconnect();
        BridgeReconnectResult reused = reconnect.CompleteBridgeReconnect(Session("transport-1", UiTransportState.Ready), 0);

        Assert.Multiple(() =>
        {
            Assert.That(firstStart.Applied, Is.True);
            Assert.That(repeatedStart.Success, Is.True);
            Assert.That(repeatedStart.Applied, Is.False);
            Assert.That(repeatedStart.Code, Is.EqualTo("bridge.reconnect-in-progress"));
            Assert.That(completed.Success, Is.True);
            Assert.That(stale.Success, Is.True);
            Assert.That(stale.Applied, Is.False);
            Assert.That(stale.IsStale, Is.True);
            Assert.That(stale.Code, Is.EqualTo("bridge.reconnect-stale-session"));
            Assert.That(stale.Batches, Is.Empty);
            Assert.That(stale.Snapshot, Is.Null);
            Assert.That(unknown.Success, Is.False);
            Assert.That(unknown.Code, Is.EqualTo("bridge.reconnect-session-mismatch"));
            Assert.That(reused.Success, Is.False);
            Assert.That(reused.Code, Is.EqualTo("bridge.reconnect-session-reused"));
            Assert.That(reconnect.Session!.Session, Is.EqualTo("transport-2"));
            Assert.That(reconnect.Session.State, Is.EqualTo(UiTransportState.Reconnecting));
        });
    }

    [Test]
    public void ValidatesReconnectStateRevisionSnapshotAndHandoffGeneration()
    {
        BridgeReconnectCore missing = new BridgeReconnectCore(null, null);
        BridgeReconnectResult missingSession = missing.BeginBridgeReconnect();
        BridgeReconnectCore reconnect = new BridgeReconnectCore(Session("transport-1", UiTransportState.Stopped), null);
        BridgeReconnectResult invalidState = reconnect.BeginBridgeReconnect();
        reconnect.Session!.State = UiTransportState.Ready;
        BridgeReconnectResult invalidHandoff = reconnect.CaptureBridgeHandoff(new UiHydrationManifest { NavigationGeneration = -1 }, null);
        reconnect.BeginBridgeReconnect();
        BridgeReconnectResult replacementNotReady = reconnect.CompleteBridgeReconnect(Session("transport-2", UiTransportState.Handshaking), 0);
        BridgeReconnectResult invalidRevision = reconnect.CompleteBridgeReconnect(Session("transport-2", UiTransportState.Ready), -1);
        BridgeReconnectResult missingSnapshot = reconnect.CompleteBridgeReconnect(Session("transport-2", UiTransportState.Ready), 0);
        reconnect.Updates = UpdateHistory(4, 0, 1);
        BridgeReconnectResult future = reconnect.CreateBridgeResync("transport-2", 2);

        Assert.Multiple(() =>
        {
            Assert.That(missingSession.Code, Is.EqualTo("bridge.reconnect-missing-session"));
            Assert.That(invalidState.Code, Is.EqualTo("bridge.reconnect-invalid-state"));
            Assert.That(invalidHandoff.Code, Is.EqualTo("bridge.reconnect-invalid-navigation-generation"));
            Assert.That(replacementNotReady.Code, Is.EqualTo("bridge.reconnect-replacement-not-ready"));
            Assert.That(invalidRevision.Code, Is.EqualTo("bridge.reconnect-invalid-revision"));
            Assert.That(missingSnapshot.Code, Is.EqualTo("bridge.reconnect-missing-snapshot"));
            Assert.That(future.Code, Is.EqualTo("bridge.reconnect-future-revision"));
        });
    }

    private static BridgeSession Session(string identifier, UiTransportState state)
    {
        BridgeSession session = new BridgeSession(null, null);
        session.Session = identifier;
        session.State = state;
        return session;
    }

    private static UpdateBatchCore UpdateHistory(int maximumHistoryCount, int initialRevision, int currentRevision)
    {
        UpdateBatchCore updates = new UpdateBatchCore();
        updates.InitializeUpdateHistory(Snapshot(initialRevision, "Initial"), maximumHistoryCount);
        int revision = initialRevision + 1;
        while (revision <= currentRevision)
        {
            UpdateBatchOperationResult created = updates.BeginUpdateTransaction(Snapshot(revision, "Value " + revision), UiUpdatePriority.Normal, null);
            updates.AcknowledgeUpdateTransaction(created.Batch!.Id, revision);
            revision++;
        }

        return updates;
    }

    private static DomSnapshot Snapshot(int revision, string text)
    {
        string hash = "hash-" + revision;
        DomNode child = new DomNode
        {
            Kind = DomNodeKind.Text,
            Key = "text",
            Id = "text",
            Text = text,
            SubtreeHash = hash + "-text"
        };
        DomNode root = new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "root",
            Id = "root",
            Name = HtmlElements.Division,
            Children = new DomNode[] { child },
            SubtreeHash = hash + "-root"
        };
        return new DomSnapshot
        {
            SessionId = "render-session",
            RootSeed = "seed",
            Revision = revision,
            Roots = new DomNode[] { root },
            ContentHash = hash
        };
    }

    private static UiHydrationManifest Handoff()
    {
        return new UiHydrationManifest
        {
            State = new UiValueEntry[] { Entry("application", "saved-state") },
            RouteUrl = "/products/7",
            RouteData = new UiRouteData
            {
                RouteId = "product",
                Parameters = new UiValueEntry[] { Entry("id", "7") }
            },
            HistoryState = Value("history-state"),
            HistoryEntryId = "history-7",
            NavigationGeneration = 8
        };
    }

    private static UiPreservationState FocusState()
    {
        return new UiPreservationState
        {
            PreserveFocus = true,
            FocusedNodeId = "field",
            PreserveSelection = true,
            HasSelection = true,
            SelectionStart = 2,
            SelectionEnd = 5,
            PreserveScroll = true,
            ScrollOffsets = new UiValueEntry[] { Entry("scroll", "20,40") }
        };
    }

    private static UiValueEntry Entry(string key, string value)
    {
        return new UiValueEntry { Key = key, Value = Value(value) };
    }

    private static UiValue Value(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }
}
