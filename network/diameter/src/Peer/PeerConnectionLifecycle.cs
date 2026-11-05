using System;
using System.Collections.Generic;

namespace diameter;

internal class PeerConnectionLifecycleResult
{
    internal bool Accepted;
    internal DiameterPeerSnapshot Peer = new DiameterPeerSnapshot();
    internal List<PeerStateMachineAction> Actions =
        new List<PeerStateMachineAction>();
    internal DiameterDisconnectRequest? DisconnectRequest;
    internal string Code = string.Empty;
}

internal class PeerConnectionLifecycle
{
    internal PeerTable Table;

    internal PeerConnectionLifecycle(PeerTable table)
    {
        if (table == null)
        {
            throw new ArgumentException("A peer table is required.");
        }

        Table = table;
    }

    internal PeerConnectionLifecycleResult ClaimDuePeerConnectionAttempt(
        string identity)
    {
        string key = Table.ReadPeerTableKey(identity);
        lock (Table.TableLock)
        {
            DiameterPeerSnapshot snapshot =
                Table.ReadRequiredPeerTableEntry(key).Snapshot;
            DateTimeOffset now = Table.Clock.ReadUtcTime();
            if (!Table.IsPeerTableRetryEligible(snapshot, now))
            {
                return CreatePeerLifecycleResult(
                    snapshot,
                    ReadPeerLifecycleIneligibleCode(snapshot, now));
            }

            if (snapshot.NextConnectionAttemptAt == default(DateTimeOffset) ||
                snapshot.NextConnectionAttemptAt > now)
            {
                return CreatePeerLifecycleResult(snapshot, "peer-retry-not-due");
            }

            PeerConnectionLifecycleResult result = BeginPeerLifecycleConnection(
                snapshot,
                now,
                "peer-retry-claimed");
            return result;
        }
    }

    internal PeerConnectionLifecycleResult RequestManualPeerConnection(
        string identity)
    {
        string key = Table.ReadPeerTableKey(identity);
        lock (Table.TableLock)
        {
            DiameterPeerSnapshot snapshot =
                Table.ReadRequiredPeerTableEntry(key).Snapshot;
            DateTimeOffset now = Table.Clock.ReadUtcTime();
            if (!snapshot.Configuration.Enabled)
            {
                return CreatePeerLifecycleResult(snapshot, "peer-disabled");
            }

            if (Table.IsPeerTableDynamicExpired(snapshot, now))
            {
                return CreatePeerLifecycleResult(snapshot, "dynamic-peer-expired");
            }

            if (snapshot.State != DiameterPeerState.Closed ||
                snapshot.Connection != null)
            {
                return CreatePeerLifecycleResult(
                    snapshot,
                    "peer-connection-already-active");
            }

            snapshot.ReconnectSuppressed = false;
            snapshot.ReconnectSuppressionCause = DiameterDisconnectCause.Rebooting;
            snapshot.ReconnectSuppressedAt = default(DateTimeOffset);
            return BeginPeerLifecycleConnection(
                snapshot,
                now,
                "manual-peer-connect-started");
        }
    }

    internal PeerConnectionLifecycleResult RequestManualPeerDisconnection(
        DiameterDisconnectRequest request)
    {
        if (request == null)
        {
            throw new ArgumentException("A peer disconnect request is required.");
        }

        request.ValidateDisconnectRequest();
        string key = Table.ReadPeerTableKey(request.PeerIdentity.Value);
        lock (Table.TableLock)
        {
            DiameterPeerSnapshot snapshot =
                Table.ReadRequiredPeerTableEntry(key).Snapshot;
            DateTimeOffset now = Table.Clock.ReadUtcTime();
            if (request.SuppressReconnect)
            {
                SuppressPeerLifecycleReconnect(snapshot, request.Cause, now);
            }

            PeerConnectionLifecycleResult result = CreatePeerLifecycleResult(
                snapshot,
                "manual-peer-disconnect-started");
            result.Accepted = true;
            result.DisconnectRequest = ClonePeerLifecycleDisconnectRequest(request);
            if (snapshot.State == DiameterPeerState.Closed &&
                snapshot.Connection == null)
            {
                result.Code = "peer-already-disconnected";
                result.Peer = PeerTableCloner.ClonePeerTableSnapshot(snapshot);
                return result;
            }

            if (snapshot.State == DiameterPeerState.Closing)
            {
                result.Code = "peer-disconnect-already-in-progress";
                result.Peer = PeerTableCloner.ClonePeerTableSnapshot(snapshot);
                return result;
            }

            PeerStateMachine machine = new PeerStateMachine(snapshot.State);
            PeerStateTransition transition = machine.HandlePeerStateEvent(
                PeerStateMachineEvent.Stop);
            if (transition.Defined)
            {
                result.Actions = transition.Actions;
                ApplyPeerLifecycleState(snapshot, transition.NextState, now);
                if (!request.WaitForAnswer)
                {
                    AddPeerLifecycleDisconnectAction(result.Actions, snapshot.Direction);
                    ClosePeerLifecycleConnection(snapshot, now);
                    SchedulePeerLifecycleRetry(snapshot, now);
                    result.Code = "manual-peer-disconnect-completed";
                }
            }
            else
            {
                result.Actions.Add(PeerStateMachineAction.Cleanup);
                ClosePeerLifecycleConnection(snapshot, now);
                SchedulePeerLifecycleRetry(snapshot, now);
                result.Code = "manual-peer-connect-cancelled";
            }

            result.Peer = PeerTableCloner.ClonePeerTableSnapshot(snapshot);
            return result;
        }
    }

    internal PeerConnectionLifecycleResult RecordRemotePeerDisconnect(
        string identity,
        DiameterDisconnectCause cause)
    {
        ValidatePeerLifecycleDisconnectCause(cause);
        string key = Table.ReadPeerTableKey(identity);
        lock (Table.TableLock)
        {
            DiameterPeerSnapshot snapshot =
                Table.ReadRequiredPeerTableEntry(key).Snapshot;
            DateTimeOffset now = Table.Clock.ReadUtcTime();
            PeerConnectionLifecycleResult result = CreatePeerLifecycleResult(
                snapshot,
                "peer-disconnected");
            result.Accepted = true;
            if (snapshot.Connection != null ||
                snapshot.State != DiameterPeerState.Closed)
            {
                AddPeerLifecycleDisconnectAction(result.Actions, snapshot.Direction);
            }

            ClosePeerLifecycleConnection(snapshot, now);
            if (cause == DiameterDisconnectCause.Rebooting)
            {
                snapshot.ReconnectSuppressed = false;
                snapshot.ReconnectSuppressionCause = cause;
                snapshot.ReconnectSuppressedAt = default(DateTimeOffset);
                SchedulePeerLifecycleRetry(snapshot, now);
                result.Code = "peer-rebooting-retry-scheduled";
            }
            else
            {
                SuppressPeerLifecycleReconnect(snapshot, cause, now);
                if (cause == DiameterDisconnectCause.Busy)
                {
                    result.Code = "peer-busy-reconnect-suppressed";
                }
                else
                {
                    result.Code = "peer-communication-reconnect-suppressed";
                }
            }

            result.Peer = PeerTableCloner.ClonePeerTableSnapshot(snapshot);
            return result;
        }
    }

    internal List<DiameterPeerSnapshot> SweepExpiredDynamicPeers()
    {
        return Table.ExpireDynamicPeerEntries();
    }

    internal PeerConnectionLifecycleResult BeginPeerLifecycleConnection(
        DiameterPeerSnapshot snapshot,
        DateTimeOffset now,
        string code)
    {
        PeerStateMachine machine = new PeerStateMachine(snapshot.State);
        PeerStateTransition transition = machine.HandlePeerStateEvent(
            PeerStateMachineEvent.Start);
        if (!transition.Defined)
        {
            return CreatePeerLifecycleResult(
                snapshot,
                "peer-connection-already-active");
        }

        ApplyPeerLifecycleState(snapshot, transition.NextState, now);
        snapshot.Direction = DiameterConnectionDirection.Outbound;
        snapshot.NextConnectionAttemptAt = default(DateTimeOffset);
        if (snapshot.ReconnectCount < long.MaxValue)
        {
            snapshot.ReconnectCount = snapshot.ReconnectCount + 1;
        }

        PeerConnectionLifecycleResult result = CreatePeerLifecycleResult(
            snapshot,
            code);
        result.Accepted = true;
        result.Actions = transition.Actions;
        return result;
    }

    internal void ApplyPeerLifecycleState(
        DiameterPeerSnapshot snapshot,
        DiameterPeerState state,
        DateTimeOffset now)
    {
        snapshot.State = state;
        snapshot.StateChangedAt = now;
        snapshot.Available = false;
    }

    internal void ClosePeerLifecycleConnection(
        DiameterPeerSnapshot snapshot,
        DateTimeOffset now)
    {
        ApplyPeerLifecycleState(snapshot, DiameterPeerState.Closed, now);
        snapshot.Connection = null;
        snapshot.ConnectedAt = default(DateTimeOffset);
        snapshot.WatchdogPending = false;
        snapshot.WatchdogDeadlineAt = default(DateTimeOffset);
        snapshot.PendingWatchdogHopByHopIdentifier = 0;
        snapshot.PendingWatchdogEndToEndIdentifier = 0;
        snapshot.DisconnectPending = false;
        snapshot.DisconnectInitiatedLocally = false;
        snapshot.DisconnectDrainPending = false;
        snapshot.DisconnectAnswerPending = false;
        snapshot.DisconnectTransportClosurePending = false;
        snapshot.DisconnectDeadlineAt = default(DateTimeOffset);
        snapshot.PendingDisconnectHopByHopIdentifier = 0;
        snapshot.PendingDisconnectEndToEndIdentifier = 0;
        if (snapshot.WatchdogState != DiameterPeerWatchdogState.Initial)
        {
            snapshot.WatchdogState = DiameterPeerWatchdogState.Down;
        }
    }

    internal void SchedulePeerLifecycleRetry(
        DiameterPeerSnapshot snapshot,
        DateTimeOffset now)
    {
        if (Table.IsPeerTableRetryEligible(snapshot, now))
        {
            snapshot.NextConnectionAttemptAt = now.AddMilliseconds(
                Table.ReadPeerTableRetryDelayMilliseconds());
        }
        else
        {
            snapshot.NextConnectionAttemptAt = default(DateTimeOffset);
        }
    }

    internal void SuppressPeerLifecycleReconnect(
        DiameterPeerSnapshot snapshot,
        DiameterDisconnectCause cause,
        DateTimeOffset now)
    {
        snapshot.ReconnectSuppressed = true;
        snapshot.ReconnectSuppressionCause = cause;
        snapshot.ReconnectSuppressedAt = now;
        snapshot.NextConnectionAttemptAt = default(DateTimeOffset);
    }

    internal void AddPeerLifecycleDisconnectAction(
        List<PeerStateMachineAction> actions,
        DiameterConnectionDirection direction)
    {
        if (direction == DiameterConnectionDirection.Inbound)
        {
            actions.Add(PeerStateMachineAction.ResponderDisconnect);
        }
        else
        {
            actions.Add(PeerStateMachineAction.InitiatorDisconnect);
        }
    }

    internal string ReadPeerLifecycleIneligibleCode(
        DiameterPeerSnapshot snapshot,
        DateTimeOffset now)
    {
        if (!snapshot.Configuration.Enabled)
        {
            return "peer-disabled";
        }

        if (!snapshot.Configuration.ConnectAutomatically)
        {
            return "automatic-peer-connect-disabled";
        }

        if (snapshot.ReconnectSuppressed)
        {
            return "peer-reconnect-suppressed";
        }

        if (Table.IsPeerTableDynamicExpired(snapshot, now))
        {
            return "dynamic-peer-expired";
        }

        return "peer-connection-already-active";
    }

    internal PeerConnectionLifecycleResult CreatePeerLifecycleResult(
        DiameterPeerSnapshot snapshot,
        string code)
    {
        PeerConnectionLifecycleResult result =
            new PeerConnectionLifecycleResult();
        result.Peer = PeerTableCloner.ClonePeerTableSnapshot(snapshot);
        result.Code = code;
        return result;
    }

    internal DiameterDisconnectRequest ClonePeerLifecycleDisconnectRequest(
        DiameterDisconnectRequest source)
    {
        DiameterDisconnectRequest result = new DiameterDisconnectRequest();
        result.PeerIdentity.Value = source.PeerIdentity.Value;
        result.Cause = source.Cause;
        result.TimeoutMilliseconds = source.TimeoutMilliseconds;
        result.WaitForAnswer = source.WaitForAnswer;
        result.SuppressReconnect = source.SuppressReconnect;
        return result;
    }

    internal void ValidatePeerLifecycleDisconnectCause(
        DiameterDisconnectCause cause)
    {
        if (cause != DiameterDisconnectCause.Rebooting &&
            cause != DiameterDisconnectCause.Busy &&
            cause != DiameterDisconnectCause.DoNotWantToTalkToYou)
        {
            throw new ArgumentOutOfRangeException("cause");
        }
    }
}
