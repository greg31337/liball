using System;
using System.Collections.Generic;

namespace htmlgui;

internal class BridgeReconnectResult
{
    internal bool Success;
    internal bool Applied;
    internal bool IsStale;
    internal bool RequiresFullSnapshot;
    internal string Code = string.Empty;
    internal string Session = string.Empty;
    internal string PreviousSession = string.Empty;
    internal UiTransportState State;
    internal int BaseRevision;
    internal int Revision;
    internal UiUpdateBatch[] Batches = Array.Empty<UiUpdateBatch>();
    internal DomSnapshot? Snapshot;
    internal UiValueEntry[] ApplicationState = Array.Empty<UiValueEntry>();
    internal string? RouteUrl;
    internal UiRouteData? RouteData;
    internal UiValue? HistoryState;
    internal string? HistoryEntryId;
    internal int NavigationGeneration;
    internal UiPreservationState Focus = new UiPreservationState();
}

internal class BridgeReconnectCore
{
    internal const int MaximumRetiredSessionCount = 32;

    internal BridgeSession? Session;
    internal UpdateBatchCore? Updates;
    internal UiHydrationManifest Handoff;
    internal UiPreservationState Focus;
    internal List<string> RetiredSessions;
    internal string ReconnectingSession = string.Empty;

    internal BridgeReconnectCore(BridgeSession? session, UpdateBatchCore? updates)
    {
        Session = session;
        Updates = updates;
        Handoff = new UiHydrationManifest();
        Focus = new UiPreservationState();
        RetiredSessions = new List<string>();
    }

    internal BridgeReconnectResult CaptureBridgeHandoff(UiHydrationManifest? handoff, UiPreservationState? focus)
    {
        UiHydrationManifest checkedHandoff = handoff ?? new UiHydrationManifest();
        if (checkedHandoff.NavigationGeneration < 0)
        {
            return CreateReconnectFailure("bridge.reconnect-invalid-navigation-generation");
        }

        Handoff = CopyReconnectHandoff(checkedHandoff);
        Focus = CopyReconnectFocus(focus ?? new UiPreservationState());
        BridgeReconnectResult result = CreateReconnectSuccess(true, "bridge.reconnect-handoff-captured");
        AddReconnectHandoff(result);
        return result;
    }

    internal BridgeReconnectResult BeginBridgeReconnect()
    {
        if (Session == null)
        {
            return CreateReconnectFailure("bridge.reconnect-missing-session");
        }

        UiTransportState state = Session.State;
        if (state == UiTransportState.Reconnecting)
        {
            BridgeReconnectResult repeated = CreateReconnectSuccess(false, "bridge.reconnect-in-progress");
            repeated.PreviousSession = ReconnectingSession;
            return repeated;
        }

        if (state != UiTransportState.Ready && state != UiTransportState.Faulted)
        {
            return CreateReconnectFailure("bridge.reconnect-invalid-state");
        }

        if (!Session.IsIdentifierValid(Session.Session))
        {
            return CreateReconnectFailure("bridge.reconnect-invalid-session");
        }

        ReconnectingSession = Session.Session;
        Session.State = UiTransportState.Reconnecting;
        BridgeReconnectResult result = CreateReconnectSuccess(true, "bridge.reconnect-started");
        result.PreviousSession = ReconnectingSession;
        return result;
    }

    internal BridgeReconnectResult CompleteBridgeReconnect(BridgeSession? replacement, int lastAcknowledgedRevision)
    {
        if (Session == null || Session.State != UiTransportState.Reconnecting)
        {
            return CreateReconnectFailure("bridge.reconnect-invalid-state");
        }

        if (replacement == null || replacement.State != UiTransportState.Ready)
        {
            return CreateReconnectFailure("bridge.reconnect-replacement-not-ready");
        }

        if (!replacement.IsIdentifierValid(replacement.Session))
        {
            return CreateReconnectFailure("bridge.reconnect-invalid-session");
        }

        if (ValueComparer.TextEquals(replacement.Session, ReconnectingSession) || IsRetiredBridgeSession(replacement.Session))
        {
            return CreateReconnectFailure("bridge.reconnect-session-reused");
        }

        if (lastAcknowledgedRevision < 0)
        {
            return CreateReconnectFailure("bridge.reconnect-invalid-revision");
        }

        string previousSession = ReconnectingSession;
        AddRetiredBridgeSession(previousSession);
        Session = replacement;
        ReconnectingSession = string.Empty;
        BridgeReconnectResult result = CreateBridgeResync(replacement.Session, lastAcknowledgedRevision);
        result.PreviousSession = previousSession;
        return result;
    }

    internal BridgeReconnectResult CreateBridgeResync(string? session, int lastAcknowledgedRevision)
    {
        if (Session == null)
        {
            return CreateReconnectFailure("bridge.reconnect-missing-session");
        }

        if (string.IsNullOrWhiteSpace(session))
        {
            return CreateReconnectFailure("bridge.reconnect-invalid-session");
        }

        if (!ValueComparer.TextEquals(session, Session.Session))
        {
            if (IsRetiredBridgeSession(session))
            {
                BridgeReconnectResult stale = CreateReconnectSuccess(false, "bridge.reconnect-stale-session");
                stale.IsStale = true;
                stale.PreviousSession = session ?? string.Empty;
                return stale;
            }

            return CreateReconnectFailure("bridge.reconnect-session-mismatch");
        }

        if (Session.State != UiTransportState.Ready)
        {
            return CreateReconnectFailure("bridge.reconnect-invalid-state");
        }

        if (lastAcknowledgedRevision < 0)
        {
            return CreateReconnectFailure("bridge.reconnect-invalid-revision");
        }

        UpdateBatchCore? updateCore = Updates;
        if (updateCore == null || updateCore.CurrentSnapshot == null)
        {
            return CreateReconnectFailure("bridge.reconnect-missing-snapshot");
        }

        DomSnapshot current = updateCore.CurrentSnapshot;
        if (lastAcknowledgedRevision > current.Revision)
        {
            return CreateReconnectFailure("bridge.reconnect-future-revision");
        }

        if (lastAcknowledgedRevision == current.Revision)
        {
            BridgeReconnectResult currentResult = CreateReconnectSuccess(false, "bridge.reconnect-current");
            currentResult.BaseRevision = lastAcknowledgedRevision;
            currentResult.Revision = current.Revision;
            AddReconnectHandoff(currentResult);
            return currentResult;
        }

        UiUpdateBatch[] batches = FindReconnectBatches(lastAcknowledgedRevision, current.Revision);
        if (batches.Length == 0)
        {
            BridgeReconnectResult snapshotResult = CreateReconnectSuccess(true, "bridge.reconnect-full-snapshot");
            snapshotResult.RequiresFullSnapshot = true;
            snapshotResult.BaseRevision = lastAcknowledgedRevision;
            snapshotResult.Revision = current.Revision;
            snapshotResult.Snapshot = CopyReconnectSnapshot(current);
            AddReconnectHandoff(snapshotResult);
            return snapshotResult;
        }

        BridgeReconnectResult incremental = CreateReconnectSuccess(true, "bridge.reconnect-incremental");
        incremental.BaseRevision = lastAcknowledgedRevision;
        incremental.Revision = current.Revision;
        incremental.Batches = batches;
        AddReconnectHandoff(incremental);
        return incremental;
    }

    internal UiUpdateBatch[] FindReconnectBatches(int baseRevision, int currentRevision)
    {
        UpdateBatchCore? updateCore = Updates;
        if (updateCore == null)
        {
            return Array.Empty<UiUpdateBatch>();
        }

        List<UiUpdateBatch> result = new List<UiUpdateBatch>();
        int expectedRevision = baseRevision;
        while (expectedRevision < currentRevision)
        {
            UpdateBatchHistoryEntry? found = null;
            int index = 0;
            while (index < updateCore.History.Count)
            {
                UpdateBatchHistoryEntry candidate = updateCore.History[index];
                if (candidate.Acknowledged && !candidate.Rejected && candidate.Batch.BaseRevision == expectedRevision)
                {
                    found = candidate;
                    break;
                }

                index++;
            }

            if (found == null || found.Batch.Revision <= expectedRevision || found.Batch.Revision > currentRevision)
            {
                return Array.Empty<UiUpdateBatch>();
            }

            result.Add(updateCore.CopyUpdateBatch(found.Batch));
            expectedRevision = found.Batch.Revision;
        }

        if (expectedRevision != currentRevision)
        {
            return Array.Empty<UiUpdateBatch>();
        }

        return result.ToArray();
    }

    internal bool IsRetiredBridgeSession(string? session)
    {
        int index = 0;
        while (session != null && index < RetiredSessions.Count)
        {
            if (ValueComparer.TextEquals(RetiredSessions[index], session))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal void AddRetiredBridgeSession(string session)
    {
        if (!IsRetiredBridgeSession(session))
        {
            RetiredSessions.Add(session);
        }

        while (RetiredSessions.Count > MaximumRetiredSessionCount)
        {
            RetiredSessions.RemoveAt(0);
        }
    }

    internal void AddReconnectHandoff(BridgeReconnectResult result)
    {
        UiHydrationManifest handoff = CopyReconnectHandoff(Handoff);
        result.ApplicationState = handoff.State;
        result.RouteUrl = handoff.RouteUrl;
        result.RouteData = handoff.RouteData;
        result.HistoryState = handoff.HistoryState;
        result.HistoryEntryId = handoff.HistoryEntryId;
        result.NavigationGeneration = handoff.NavigationGeneration;
        result.Focus = CopyReconnectFocus(Focus);
    }

    internal BridgeReconnectResult CreateReconnectSuccess(bool applied, string code)
    {
        BridgeReconnectResult result = new BridgeReconnectResult();
        result.Success = true;
        result.Applied = applied;
        result.Code = code;
        if (Session != null)
        {
            result.Session = Session.Session;
            result.State = Session.State;
        }

        return result;
    }

    internal BridgeReconnectResult CreateReconnectFailure(string code)
    {
        BridgeReconnectResult result = new BridgeReconnectResult();
        result.Code = code;
        if (Session != null)
        {
            result.Session = Session.Session;
            result.State = Session.State;
        }

        return result;
    }

    internal UiHydrationManifest CopyReconnectHandoff(UiHydrationManifest source)
    {
        object? copied = new ModelNormalizer().CopyValue(source);
        if (copied == null)
        {
            throw new InvalidOperationException("The reconnect handoff copy could not be created.");
        }

        return (UiHydrationManifest)copied;
    }

    internal UiPreservationState CopyReconnectFocus(UiPreservationState source)
    {
        object? copied = new ModelNormalizer().CopyValue(source);
        if (copied == null)
        {
            throw new InvalidOperationException("The reconnect focus copy could not be created.");
        }

        return (UiPreservationState)copied;
    }

    internal DomSnapshot CopyReconnectSnapshot(DomSnapshot source)
    {
        object? copied = new ModelNormalizer().CopyValue(source);
        if (copied == null)
        {
            throw new InvalidOperationException("The reconnect snapshot copy could not be created.");
        }

        return (DomSnapshot)copied;
    }
}
