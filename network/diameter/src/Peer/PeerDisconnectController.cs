using System;

namespace diameter;

internal class PeerDisconnectResult
{
    internal bool Handled;
    internal bool DrainRequired;
    internal bool WaitingForAnswer;
    internal bool WaitingForTransportClose;
    internal bool CloseConnection;
    internal DiameterDisconnectCause Cause;
    internal DiameterMessage? Message;
    internal DiameterPeerSnapshot Peer = new DiameterPeerSnapshot();
    internal string Code = string.Empty;
}

internal class PeerDisconnectMessageValidation
{
    internal bool Valid;
    internal DiameterDisconnectCause Cause;
    internal uint ResultCode;
    internal string Code = string.Empty;
}

internal class PeerDisconnectController
{
    internal PeerTable Table;
    internal PeerConnectionLifecycle Lifecycle;
    internal IDiameterBaseCommandFactory CommandFactory;
    internal DiameterBaseCommandOptions OriginOptions;

    internal PeerDisconnectController(
        PeerTable table,
        IDiameterBaseCommandFactory commandFactory,
        DiameterBaseCommandOptions originOptions)
    {
        if (table == null || commandFactory == null || originOptions == null ||
            originOptions.OriginHost == null || originOptions.OriginRealm == null ||
            ValueCodecs.NormalizeDiameterIdentity(originOptions.OriginHost).Status !=
                DiameterCodecStatus.Success ||
            ValueCodecs.NormalizeDiameterRealm(originOptions.OriginRealm).Status !=
                DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "A peer table, command factory, and valid local origin are required.");
        }

        Table = table;
        Lifecycle = new PeerConnectionLifecycle(table);
        CommandFactory = commandFactory;
        OriginOptions = ClonePeerDisconnectOriginOptions(originOptions);
    }

    internal PeerDisconnectResult BeginPeerDisconnect(
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
            PeerDisconnectResult result = CreatePeerDisconnectResult(
                snapshot,
                "peer-disconnect-not-started");
            result.Cause = request.Cause;
            if (snapshot.DisconnectPending)
            {
                result.Handled = true;
                result.DrainRequired = snapshot.DisconnectDrainPending;
                result.WaitingForAnswer = snapshot.DisconnectAnswerPending;
                result.WaitingForTransportClose =
                    snapshot.DisconnectTransportClosurePending;
                result.Code = "peer-disconnect-already-in-progress";
                return result;
            }

            if (snapshot.State == DiameterPeerState.Closed &&
                snapshot.Connection == null)
            {
                result.Handled = true;
                result.Code = "peer-already-disconnected";
                return result;
            }

            if (request.SuppressReconnect)
            {
                Lifecycle.SuppressPeerLifecycleReconnect(
                    snapshot,
                    request.Cause,
                    now);
            }

            if ((snapshot.State != DiameterPeerState.InitiatorOpen &&
                snapshot.State != DiameterPeerState.ResponderOpen) ||
                snapshot.Connection == null || !snapshot.Connection.Open)
            {
                Lifecycle.ClosePeerLifecycleConnection(snapshot, now);
                Lifecycle.SchedulePeerLifecycleRetry(snapshot, now);
                snapshot.LastDisconnectCause = request.Cause;
                snapshot.LastDisconnectedAt = now;
                result.Handled = true;
                result.CloseConnection = true;
                result.Code = "peer-connection-attempt-cancelled";
                result.Peer = PeerTableCloner.ClonePeerTableSnapshot(snapshot);
                return result;
            }

            DiameterBaseCommandOptions options =
                ClonePeerDisconnectOriginOptions(OriginOptions);
            options.DisconnectCause = request.Cause;
            DiameterMessage message =
                CommandFactory.CreateDisconnectPeerRequest(options);
            snapshot.State = DiameterPeerState.Closing;
            snapshot.StateChangedAt = now;
            snapshot.Available = false;
            snapshot.DisconnectPending = true;
            snapshot.DisconnectInitiatedLocally = true;
            snapshot.DisconnectDrainPending = true;
            snapshot.DisconnectAnswerPending = request.WaitForAnswer;
            snapshot.DisconnectTransportClosurePending = false;
            snapshot.DisconnectDeadlineAt = now.AddMilliseconds(
                ReadPeerDisconnectTimeout(request.TimeoutMilliseconds));
            snapshot.PendingDisconnectHopByHopIdentifier =
                message.HopByHopIdentifier;
            snapshot.PendingDisconnectEndToEndIdentifier =
                message.EndToEndIdentifier;
            snapshot.LastDisconnectCause = request.Cause;

            result.Handled = true;
            result.DrainRequired = true;
            result.WaitingForAnswer = request.WaitForAnswer;
            result.Message = message;
            result.Code = "disconnect-request-pending-drain";
            result.Peer = PeerTableCloner.ClonePeerTableSnapshot(snapshot);
            return result;
        }
    }

    internal PeerDisconnectResult HandlePeerDisconnectRequest(
        string identity,
        DiameterMessage request)
    {
        PeerDisconnectMessageValidation validation =
            ValidatePeerDisconnectMessage(request, true);
        string key = Table.ReadPeerTableKey(identity);
        lock (Table.TableLock)
        {
            DiameterPeerSnapshot snapshot =
                Table.ReadRequiredPeerTableEntry(key).Snapshot;
            if (!validation.Valid)
            {
                return CreatePeerDisconnectResult(snapshot, validation.Code);
            }

            if ((snapshot.State != DiameterPeerState.InitiatorOpen &&
                snapshot.State != DiameterPeerState.ResponderOpen &&
                snapshot.State != DiameterPeerState.Closing) ||
                snapshot.Connection == null || !snapshot.Connection.Open)
            {
                return CreatePeerDisconnectResult(
                    snapshot,
                    "disconnect-request-peer-not-open");
            }

            DateTimeOffset now = Table.Clock.ReadUtcTime();
            ApplyPeerDisconnectReconnectPolicy(
                snapshot,
                validation.Cause,
                now);
            DiameterBaseCommandOptions options =
                ClonePeerDisconnectOriginOptions(OriginOptions);
            options.Request = request;
            options.ResultCode = DiameterResultCodes.Success;
            DiameterMessage answer =
                CommandFactory.CreateDisconnectPeerAnswer(options);
            snapshot.State = DiameterPeerState.Closing;
            snapshot.StateChangedAt = now;
            snapshot.Available = false;
            snapshot.DisconnectPending = true;
            snapshot.DisconnectInitiatedLocally = false;
            snapshot.DisconnectDrainPending = true;
            snapshot.DisconnectAnswerPending = false;
            snapshot.DisconnectTransportClosurePending = false;
            snapshot.DisconnectDeadlineAt = now.AddMilliseconds(
                Table.Timers.DrainTimeoutMilliseconds);
            snapshot.PendingDisconnectHopByHopIdentifier = 0;
            snapshot.PendingDisconnectEndToEndIdentifier = 0;
            snapshot.LastDisconnectCause = validation.Cause;

            PeerDisconnectResult result = CreatePeerDisconnectResult(
                snapshot,
                "disconnect-answer-pending-drain");
            result.Handled = true;
            result.DrainRequired = true;
            result.Cause = validation.Cause;
            result.Message = answer;
            return result;
        }
    }

    internal PeerDisconnectResult HandlePeerDisconnectAnswer(
        string identity,
        DiameterMessage answer)
    {
        PeerDisconnectMessageValidation validation =
            ValidatePeerDisconnectMessage(answer, false);
        string key = Table.ReadPeerTableKey(identity);
        lock (Table.TableLock)
        {
            DiameterPeerSnapshot snapshot =
                Table.ReadRequiredPeerTableEntry(key).Snapshot;
            if (!validation.Valid)
            {
                return CreatePeerDisconnectResult(snapshot, validation.Code);
            }

            if (!snapshot.DisconnectPending ||
                !snapshot.DisconnectInitiatedLocally ||
                !snapshot.DisconnectAnswerPending ||
                answer.HopByHopIdentifier !=
                    snapshot.PendingDisconnectHopByHopIdentifier ||
                answer.EndToEndIdentifier !=
                    snapshot.PendingDisconnectEndToEndIdentifier)
            {
                return CreatePeerDisconnectResult(
                    snapshot,
                    "disconnect-answer-not-correlated");
            }

            DateTimeOffset now = Table.Clock.ReadUtcTime();
            DiameterDisconnectCause cause = snapshot.LastDisconnectCause;
            FinalizePeerDisconnect(snapshot, now);
            PeerDisconnectResult result = CreatePeerDisconnectResult(
                snapshot,
                "disconnect-answer-accepted");
            result.Handled = true;
            result.CloseConnection = true;
            result.Cause = cause;
            return result;
        }
    }

    internal PeerDisconnectResult CompletePeerDisconnectDrain(string identity)
    {
        string key = Table.ReadPeerTableKey(identity);
        lock (Table.TableLock)
        {
            DiameterPeerSnapshot snapshot =
                Table.ReadRequiredPeerTableEntry(key).Snapshot;
            if (!snapshot.DisconnectPending ||
                !snapshot.DisconnectDrainPending)
            {
                return CreatePeerDisconnectResult(
                    snapshot,
                    "disconnect-drain-not-pending");
            }

            snapshot.DisconnectDrainPending = false;
            PeerDisconnectResult result = CreatePeerDisconnectResult(
                snapshot,
                "disconnect-drain-completed");
            result.Handled = true;
            result.Cause = snapshot.LastDisconnectCause;
            if (snapshot.DisconnectInitiatedLocally)
            {
                if (snapshot.DisconnectAnswerPending)
                {
                    result.WaitingForAnswer = true;
                    result.Code = "disconnect-answer-pending";
                    result.Peer = PeerTableCloner.ClonePeerTableSnapshot(snapshot);
                    return result;
                }

                FinalizePeerDisconnect(snapshot, Table.Clock.ReadUtcTime());
                result.CloseConnection = true;
                result.Code = "disconnect-without-answer-completed";
                result.Peer = PeerTableCloner.ClonePeerTableSnapshot(snapshot);
                return result;
            }

            snapshot.DisconnectTransportClosurePending = true;
            result.WaitingForTransportClose = true;
            result.Code = "peer-transport-close-pending";
            result.Peer = PeerTableCloner.ClonePeerTableSnapshot(snapshot);
            return result;
        }
    }

    internal PeerDisconnectResult HandlePeerDisconnectTimer(string identity)
    {
        string key = Table.ReadPeerTableKey(identity);
        lock (Table.TableLock)
        {
            DiameterPeerSnapshot snapshot =
                Table.ReadRequiredPeerTableEntry(key).Snapshot;
            if (!snapshot.DisconnectPending)
            {
                return CreatePeerDisconnectResult(
                    snapshot,
                    "disconnect-timer-not-active");
            }

            DateTimeOffset now = Table.Clock.ReadUtcTime();
            if (snapshot.DisconnectDeadlineAt > now)
            {
                return CreatePeerDisconnectResult(
                    snapshot,
                    "disconnect-timer-not-due");
            }

            bool drainPending = snapshot.DisconnectDrainPending;
            bool answerPending = snapshot.DisconnectAnswerPending;
            bool closePending = snapshot.DisconnectTransportClosurePending;
            DiameterDisconnectCause cause = snapshot.LastDisconnectCause;
            FinalizePeerDisconnect(snapshot, now);
            PeerDisconnectResult result = CreatePeerDisconnectResult(
                snapshot,
                "disconnect-timeout");
            result.Handled = true;
            result.CloseConnection = true;
            result.Cause = cause;
            if (drainPending)
            {
                result.Code = "disconnect-drain-timeout";
            }
            else if (answerPending)
            {
                result.Code = "disconnect-answer-timeout";
            }
            else if (closePending)
            {
                result.Code = "peer-transport-close-timeout";
            }

            return result;
        }
    }

    internal PeerDisconnectResult HandlePeerDisconnectTransportClosed(
        string identity)
    {
        string key = Table.ReadPeerTableKey(identity);
        lock (Table.TableLock)
        {
            DiameterPeerSnapshot snapshot =
                Table.ReadRequiredPeerTableEntry(key).Snapshot;
            if (!snapshot.DisconnectPending &&
                snapshot.State == DiameterPeerState.Closed &&
                snapshot.Connection == null)
            {
                return CreatePeerDisconnectResult(
                    snapshot,
                    "peer-transport-already-closed");
            }

            DiameterDisconnectCause cause = snapshot.LastDisconnectCause;
            FinalizePeerDisconnect(snapshot, Table.Clock.ReadUtcTime());
            PeerDisconnectResult result = CreatePeerDisconnectResult(
                snapshot,
                "peer-transport-closed");
            result.Handled = true;
            result.Cause = cause;
            return result;
        }
    }

    internal void ApplyPeerDisconnectReconnectPolicy(
        DiameterPeerSnapshot snapshot,
        DiameterDisconnectCause cause,
        DateTimeOffset now)
    {
        if (cause == DiameterDisconnectCause.Rebooting)
        {
            snapshot.ReconnectSuppressed = false;
            snapshot.ReconnectSuppressionCause = cause;
            snapshot.ReconnectSuppressedAt = default(DateTimeOffset);
            return;
        }

        Lifecycle.SuppressPeerLifecycleReconnect(snapshot, cause, now);
    }

    internal void FinalizePeerDisconnect(
        DiameterPeerSnapshot snapshot,
        DateTimeOffset now)
    {
        Lifecycle.ClosePeerLifecycleConnection(snapshot, now);
        snapshot.DisconnectPending = false;
        snapshot.DisconnectInitiatedLocally = false;
        snapshot.DisconnectDrainPending = false;
        snapshot.DisconnectAnswerPending = false;
        snapshot.DisconnectTransportClosurePending = false;
        snapshot.DisconnectDeadlineAt = default(DateTimeOffset);
        snapshot.PendingDisconnectHopByHopIdentifier = 0;
        snapshot.PendingDisconnectEndToEndIdentifier = 0;
        snapshot.LastDisconnectedAt = now;
        Lifecycle.SchedulePeerLifecycleRetry(snapshot, now);
    }

    internal int ReadPeerDisconnectTimeout(int requestedTimeoutMilliseconds)
    {
        if (requestedTimeoutMilliseconds < Table.Timers.DrainTimeoutMilliseconds)
        {
            return requestedTimeoutMilliseconds;
        }

        return Table.Timers.DrainTimeoutMilliseconds;
    }

    internal PeerDisconnectMessageValidation ValidatePeerDisconnectMessage(
        DiameterMessage message,
        bool requestExpected)
    {
        PeerDisconnectMessageValidation result =
            new PeerDisconnectMessageValidation();
        if (message == null)
        {
            result.Code = "disconnect-message-missing";
            return result;
        }

        bool request = (message.Flags & DiameterMessageFlags.Request) !=
            DiameterMessageFlags.None;
        if (message.Version != DiameterProtocolConstants.Version ||
            message.ApplicationId != DiameterApplicationIds.Common ||
            message.CommandCode != DiameterCommandCodes.DisconnectPeer ||
            request != requestExpected)
        {
            result.Code = "invalid-disconnect-message";
            return result;
        }

        uint requiredCode = DiameterAvpCodes.ResultCode;
        if (requestExpected)
        {
            requiredCode = DiameterAvpCodes.DisconnectCause;
        }

        int requiredCount = 0;
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp.Code == requiredCode &&
                avp.VendorId == DiameterVendorIds.Ietf)
            {
                if (avp.Data == null || avp.Data.Length != 4)
                {
                    result.Code = "invalid-disconnect-avp";
                    return result;
                }

                uint value = NetworkByteOrder.ReadUInt32(avp.Data);
                if (requestExpected)
                {
                    if (value > 2)
                    {
                        result.Code = "invalid-disconnect-cause";
                        return result;
                    }

                    result.Cause = (DiameterDisconnectCause)value;
                }
                else
                {
                    result.ResultCode = value;
                }

                requiredCount = requiredCount + 1;
            }

            index = index + 1;
        }

        if (requiredCount != 1)
        {
            result.Code = "invalid-disconnect-avp";
            return result;
        }

        result.Valid = true;
        result.Code = "valid-disconnect-message";
        return result;
    }

    internal DiameterBaseCommandOptions ClonePeerDisconnectOriginOptions(
        DiameterBaseCommandOptions source)
    {
        DiameterBaseCommandOptions result = new DiameterBaseCommandOptions();
        result.OriginHost.Value = source.OriginHost.Value;
        result.OriginRealm.Value = source.OriginRealm.Value;
        result.ResultCode = source.ResultCode;
        result.Request = source.Request;
        result.DisconnectCause = source.DisconnectCause;
        return result;
    }

    internal PeerDisconnectResult CreatePeerDisconnectResult(
        DiameterPeerSnapshot snapshot,
        string code)
    {
        PeerDisconnectResult result = new PeerDisconnectResult();
        result.Peer = PeerTableCloner.ClonePeerTableSnapshot(snapshot);
        result.Cause = snapshot.LastDisconnectCause;
        result.Code = code;
        return result;
    }
}
