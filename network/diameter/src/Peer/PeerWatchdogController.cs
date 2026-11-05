using System;

namespace diameter;

internal class PeerWatchdogResult
{
    internal bool Handled;
    internal bool FailoverRequired;
    internal bool FailbackReady;
    internal bool CloseConnection;
    internal bool MessageDiscarded;
    internal DiameterMessage? Message;
    internal DiameterPeerSnapshot Peer = new DiameterPeerSnapshot();
    internal string Code = string.Empty;
}

internal class PeerWatchdogMessageValidation
{
    internal bool Valid;
    internal uint ResultCode;
    internal string Code = string.Empty;
}

internal class PeerWatchdogController
{
    internal PeerTable Table;
    internal IDiameterBaseCommandFactory CommandFactory;
    internal DiameterBaseCommandOptions OriginOptions;

    internal PeerWatchdogController(
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
        CommandFactory = commandFactory;
        OriginOptions = ClonePeerWatchdogOriginOptions(originOptions);
    }

    internal PeerWatchdogResult HandlePeerWatchdogConnectionOpened(
        string identity)
    {
        string key = Table.ReadPeerTableKey(identity);
        lock (Table.TableLock)
        {
            DiameterPeerSnapshot snapshot =
                Table.ReadRequiredPeerTableEntry(key).Snapshot;
            if ((snapshot.State != DiameterPeerState.InitiatorOpen &&
                snapshot.State != DiameterPeerState.ResponderOpen) ||
                snapshot.Connection == null || !snapshot.Connection.Open)
            {
                return CreatePeerWatchdogResult(
                    snapshot,
                    "watchdog-connection-not-open");
            }

            DateTimeOffset now = Table.Clock.ReadUtcTime();
            PeerWatchdogResult result;
            if (snapshot.WatchdogState == DiameterPeerWatchdogState.Initial)
            {
                snapshot.WatchdogState = DiameterPeerWatchdogState.Okay;
                snapshot.WatchdogPending = false;
                snapshot.ConsecutiveWatchdogSuccesses = 0;
                snapshot.ConsecutiveWatchdogFailures = 0;
                snapshot.ReopenWatchdogMisses = 0;
                snapshot.Available = snapshot.Configuration.Enabled;
                SetPeerWatchdogDeadline(snapshot, now);
                result = CreatePeerWatchdogResult(
                    snapshot,
                    "initial-watchdog-connection-ready");
                result.Handled = true;
                return result;
            }

            if (snapshot.WatchdogState == DiameterPeerWatchdogState.Down ||
                snapshot.WatchdogState == DiameterPeerWatchdogState.Suspect)
            {
                snapshot.WatchdogState = DiameterPeerWatchdogState.Reopen;
                snapshot.Available = false;
                snapshot.WatchdogPending = false;
                snapshot.ConsecutiveWatchdogSuccesses = 0;
                snapshot.ConsecutiveWatchdogFailures = 0;
                snapshot.ReopenWatchdogMisses = 0;
                result = CreatePeerWatchdogRequest(
                    snapshot,
                    now,
                    "reopen-watchdog-sent");
                return result;
            }

            return CreatePeerWatchdogResult(
                snapshot,
                "watchdog-connection-already-active");
        }
    }

    internal PeerWatchdogResult HandlePeerWatchdogTimer(string identity)
    {
        string key = Table.ReadPeerTableKey(identity);
        lock (Table.TableLock)
        {
            DiameterPeerSnapshot snapshot =
                Table.ReadRequiredPeerTableEntry(key).Snapshot;
            DateTimeOffset now = Table.Clock.ReadUtcTime();
            if ((snapshot.State != DiameterPeerState.InitiatorOpen &&
                snapshot.State != DiameterPeerState.ResponderOpen) ||
                snapshot.Connection == null || !snapshot.Connection.Open ||
                snapshot.WatchdogState == DiameterPeerWatchdogState.Down)
            {
                return CreatePeerWatchdogResult(snapshot, "watchdog-peer-not-open");
            }

            DateTimeOffset deadline = snapshot.WatchdogDeadlineAt;
            if (deadline == default(DateTimeOffset))
            {
                deadline = Table.ReadPeerWatchdogReferenceTime(snapshot).AddMilliseconds(
                    Table.Timers.WatchdogIntervalMilliseconds);
                snapshot.WatchdogDeadlineAt = deadline;
            }

            if (deadline > now)
            {
                return CreatePeerWatchdogResult(snapshot, "watchdog-timer-not-due");
            }

            if (snapshot.WatchdogState == DiameterPeerWatchdogState.Initial)
            {
                snapshot.WatchdogState = DiameterPeerWatchdogState.Okay;
                snapshot.Available = snapshot.Configuration.Enabled;
            }

            if (snapshot.WatchdogState == DiameterPeerWatchdogState.Okay)
            {
                if (!snapshot.WatchdogPending)
                {
                    return CreatePeerWatchdogRequest(
                        snapshot,
                        now,
                        "idle-watchdog-sent");
                }

                snapshot.WatchdogState = DiameterPeerWatchdogState.Suspect;
                snapshot.Available = false;
                snapshot.ConsecutiveWatchdogSuccesses = 0;
                snapshot.ConsecutiveWatchdogFailures =
                    Table.IncrementPeerTableCounter(
                        snapshot.ConsecutiveWatchdogFailures);
                SetPeerWatchdogDeadline(snapshot, now);
                PeerWatchdogResult suspect = CreatePeerWatchdogResult(
                    snapshot,
                    "watchdog-peer-suspect");
                suspect.Handled = true;
                suspect.FailoverRequired = true;
                return suspect;
            }

            if (snapshot.WatchdogState == DiameterPeerWatchdogState.Suspect)
            {
                snapshot.ConsecutiveWatchdogFailures =
                    Table.IncrementPeerTableCounter(
                        snapshot.ConsecutiveWatchdogFailures);
                PeerWatchdogResult down = CreatePeerWatchdogResult(
                    snapshot,
                    "watchdog-peer-down");
                down.Handled = true;
                down.CloseConnection = true;
                ClosePeerWatchdogConnection(snapshot, now);
                down.Peer = PeerTableCloner.ClonePeerTableSnapshot(snapshot);
                return down;
            }

            if (snapshot.WatchdogState == DiameterPeerWatchdogState.Reopen)
            {
                if (!snapshot.WatchdogPending)
                {
                    return CreatePeerWatchdogRequest(
                        snapshot,
                        now,
                        "reopen-watchdog-sent");
                }

                snapshot.ConsecutiveWatchdogSuccesses = 0;
                snapshot.ConsecutiveWatchdogFailures =
                    Table.IncrementPeerTableCounter(
                        snapshot.ConsecutiveWatchdogFailures);
                snapshot.ReopenWatchdogMisses =
                    Table.IncrementPeerTableCounter(snapshot.ReopenWatchdogMisses);
                if (snapshot.ReopenWatchdogMisses >= 2)
                {
                    PeerWatchdogResult failed = CreatePeerWatchdogResult(
                        snapshot,
                        "reopen-watchdog-failed");
                    failed.Handled = true;
                    failed.CloseConnection = true;
                    ClosePeerWatchdogConnection(snapshot, now);
                    failed.Peer = PeerTableCloner.ClonePeerTableSnapshot(snapshot);
                    return failed;
                }

                SetPeerWatchdogDeadline(snapshot, now);
                PeerWatchdogResult missed = CreatePeerWatchdogResult(
                    snapshot,
                    "reopen-watchdog-pending");
                missed.Handled = true;
                return missed;
            }

            return CreatePeerWatchdogResult(snapshot, "watchdog-state-invalid");
        }
    }

    internal PeerWatchdogResult HandlePeerWatchdogRequest(
        string identity,
        DiameterMessage request)
    {
        PeerWatchdogMessageValidation validation = ValidatePeerWatchdogMessage(
            request,
            true);
        string key = Table.ReadPeerTableKey(identity);
        lock (Table.TableLock)
        {
            DiameterPeerSnapshot snapshot =
                Table.ReadRequiredPeerTableEntry(key).Snapshot;
            if (!validation.Valid)
            {
                return CreatePeerWatchdogResult(snapshot, validation.Code);
            }

            if (snapshot.WatchdogState == DiameterPeerWatchdogState.Reopen ||
                snapshot.WatchdogState == DiameterPeerWatchdogState.Down ||
                snapshot.WatchdogState == DiameterPeerWatchdogState.Initial)
            {
                PeerWatchdogResult discarded = CreatePeerWatchdogResult(
                    snapshot,
                    "watchdog-request-discarded");
                discarded.MessageDiscarded = true;
                return discarded;
            }

            DateTimeOffset now = Table.Clock.ReadUtcTime();
            PeerWatchdogResult result = RecordPeerWatchdogMessageReceived(
                snapshot,
                now,
                "watchdog-request-answered");
            DiameterBaseCommandOptions options =
                ClonePeerWatchdogOriginOptions(OriginOptions);
            options.Request = request;
            options.ResultCode = DiameterResultCodes.Success;
            result.Message = CommandFactory.CreateDeviceWatchdogAnswer(options);
            result.Handled = true;
            result.Peer = PeerTableCloner.ClonePeerTableSnapshot(snapshot);
            return result;
        }
    }

    internal PeerWatchdogResult HandlePeerWatchdogAnswer(
        string identity,
        DiameterMessage answer)
    {
        PeerWatchdogMessageValidation validation = ValidatePeerWatchdogMessage(
            answer,
            false);
        string key = Table.ReadPeerTableKey(identity);
        lock (Table.TableLock)
        {
            DiameterPeerSnapshot snapshot =
                Table.ReadRequiredPeerTableEntry(key).Snapshot;
            if (!validation.Valid)
            {
                return CreatePeerWatchdogResult(snapshot, validation.Code);
            }

            if (!snapshot.WatchdogPending ||
                answer.HopByHopIdentifier !=
                    snapshot.PendingWatchdogHopByHopIdentifier ||
                answer.EndToEndIdentifier !=
                    snapshot.PendingWatchdogEndToEndIdentifier)
            {
                return CreatePeerWatchdogResult(
                    snapshot,
                    "watchdog-answer-not-correlated");
            }

            DateTimeOffset now = Table.Clock.ReadUtcTime();
            snapshot.WatchdogPending = false;
            snapshot.PendingWatchdogHopByHopIdentifier = 0;
            snapshot.PendingWatchdogEndToEndIdentifier = 0;
            snapshot.LastActivityAt = now;
            snapshot.LastWatchdogAt = now;
            snapshot.ConsecutiveWatchdogFailures = 0;
            snapshot.ReopenWatchdogMisses = 0;
            snapshot.ConsecutiveWatchdogSuccesses =
                Table.IncrementPeerTableCounter(
                    snapshot.ConsecutiveWatchdogSuccesses);
            SetPeerWatchdogDeadline(snapshot, now);

            PeerWatchdogResult result = CreatePeerWatchdogResult(
                snapshot,
                "watchdog-answer-accepted");
            result.Handled = true;
            if (snapshot.WatchdogState == DiameterPeerWatchdogState.Suspect)
            {
                RestorePeerWatchdogAvailability(snapshot);
                result.FailbackReady = true;
                result.Code = "suspect-peer-restored";
            }
            else if (snapshot.WatchdogState == DiameterPeerWatchdogState.Reopen)
            {
                if (snapshot.ConsecutiveWatchdogSuccesses >= 3)
                {
                    RestorePeerWatchdogAvailability(snapshot);
                    result.FailbackReady = true;
                    result.Code = "reopen-peer-stabilized";
                }
                else
                {
                    snapshot.Available = false;
                    result.Code = "reopen-watchdog-accepted";
                }
            }
            else if (snapshot.WatchdogState == DiameterPeerWatchdogState.Okay)
            {
                snapshot.Available = snapshot.Configuration.Enabled;
            }
            else
            {
                result.Handled = false;
                result.MessageDiscarded = true;
                result.Code = "watchdog-answer-discarded";
            }

            result.Peer = PeerTableCloner.ClonePeerTableSnapshot(snapshot);
            return result;
        }
    }

    internal PeerWatchdogResult HandlePeerMessageReceived(
        string identity,
        DiameterMessage message)
    {
        if (message == null)
        {
            throw new ArgumentException("A received Diameter message is required.");
        }

        string key = Table.ReadPeerTableKey(identity);
        lock (Table.TableLock)
        {
            DiameterPeerSnapshot snapshot =
                Table.ReadRequiredPeerTableEntry(key).Snapshot;
            if (message.CommandCode == DiameterCommandCodes.DeviceWatchdog)
            {
                return CreatePeerWatchdogResult(
                    snapshot,
                    "watchdog-message-requires-specific-handler");
            }

            if (snapshot.WatchdogState == DiameterPeerWatchdogState.Reopen ||
                snapshot.WatchdogState == DiameterPeerWatchdogState.Down ||
                snapshot.WatchdogState == DiameterPeerWatchdogState.Initial)
            {
                PeerWatchdogResult discarded = CreatePeerWatchdogResult(
                    snapshot,
                    "peer-message-discarded");
                discarded.MessageDiscarded = true;
                return discarded;
            }

            return RecordPeerWatchdogMessageReceived(
                snapshot,
                Table.Clock.ReadUtcTime(),
                "peer-activity-recorded");
        }
    }

    internal PeerWatchdogResult HandlePeerTransportFailure(string identity)
    {
        string key = Table.ReadPeerTableKey(identity);
        lock (Table.TableLock)
        {
            DiameterPeerSnapshot snapshot =
                Table.ReadRequiredPeerTableEntry(key).Snapshot;
            if (snapshot.WatchdogState == DiameterPeerWatchdogState.Down &&
                snapshot.State == DiameterPeerState.Closed &&
                snapshot.Connection == null)
            {
                return CreatePeerWatchdogResult(
                    snapshot,
                    "peer-transport-already-down");
            }

            PeerWatchdogResult result = CreatePeerWatchdogResult(
                snapshot,
                "peer-transport-failed");
            result.Handled = true;
            result.CloseConnection = true;
            result.FailoverRequired =
                snapshot.WatchdogState == DiameterPeerWatchdogState.Okay;
            ClosePeerWatchdogConnection(snapshot, Table.Clock.ReadUtcTime());
            result.Peer = PeerTableCloner.ClonePeerTableSnapshot(snapshot);
            return result;
        }
    }

    internal PeerWatchdogResult CreatePeerWatchdogRequest(
        DiameterPeerSnapshot snapshot,
        DateTimeOffset now,
        string code)
    {
        DiameterBaseCommandOptions options =
            ClonePeerWatchdogOriginOptions(OriginOptions);
        DiameterMessage message = CommandFactory.CreateDeviceWatchdogRequest(options);
        snapshot.WatchdogPending = true;
        snapshot.PendingWatchdogHopByHopIdentifier =
            message.HopByHopIdentifier;
        snapshot.PendingWatchdogEndToEndIdentifier =
            message.EndToEndIdentifier;
        snapshot.LastWatchdogAt = now;
        SetPeerWatchdogDeadline(snapshot, now);
        PeerWatchdogResult result = CreatePeerWatchdogResult(snapshot, code);
        result.Handled = true;
        result.Message = message;
        return result;
    }

    internal PeerWatchdogResult RecordPeerWatchdogMessageReceived(
        DiameterPeerSnapshot snapshot,
        DateTimeOffset now,
        string code)
    {
        snapshot.LastActivityAt = now;
        SetPeerWatchdogDeadline(snapshot, now);
        PeerWatchdogResult result = CreatePeerWatchdogResult(snapshot, code);
        result.Handled = true;
        if (snapshot.WatchdogState == DiameterPeerWatchdogState.Suspect)
        {
            RestorePeerWatchdogAvailability(snapshot);
            result.FailbackReady = true;
            result.Code = "suspect-peer-restored";
        }

        result.Peer = PeerTableCloner.ClonePeerTableSnapshot(snapshot);
        return result;
    }

    internal void RestorePeerWatchdogAvailability(
        DiameterPeerSnapshot snapshot)
    {
        snapshot.WatchdogState = DiameterPeerWatchdogState.Okay;
        snapshot.Available = snapshot.Configuration.Enabled &&
            snapshot.Connection != null && snapshot.Connection.Open;
        snapshot.ConsecutiveWatchdogFailures = 0;
        snapshot.ReopenWatchdogMisses = 0;
    }

    internal void ClosePeerWatchdogConnection(
        DiameterPeerSnapshot snapshot,
        DateTimeOffset now)
    {
        snapshot.State = DiameterPeerState.Closed;
        snapshot.StateChangedAt = now;
        snapshot.Connection = null;
        snapshot.ConnectedAt = default(DateTimeOffset);
        snapshot.Available = false;
        snapshot.WatchdogState = DiameterPeerWatchdogState.Down;
        snapshot.WatchdogPending = false;
        snapshot.WatchdogDeadlineAt = default(DateTimeOffset);
        snapshot.PendingWatchdogHopByHopIdentifier = 0;
        snapshot.PendingWatchdogEndToEndIdentifier = 0;
        snapshot.ConsecutiveWatchdogSuccesses = 0;
        snapshot.ReopenWatchdogMisses = 0;
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

    internal void SetPeerWatchdogDeadline(
        DiameterPeerSnapshot snapshot,
        DateTimeOffset now)
    {
        snapshot.WatchdogDeadlineAt = now.AddMilliseconds(
            Table.ReadPeerWatchdogDelayMilliseconds());
    }

    internal PeerWatchdogMessageValidation ValidatePeerWatchdogMessage(
        DiameterMessage message,
        bool requestExpected)
    {
        PeerWatchdogMessageValidation result =
            new PeerWatchdogMessageValidation();
        if (message == null)
        {
            result.Code = "watchdog-message-missing";
            return result;
        }

        bool request = (message.Flags & DiameterMessageFlags.Request) !=
            DiameterMessageFlags.None;
        if (message.Version != DiameterProtocolConstants.Version ||
            message.ApplicationId != DiameterApplicationIds.Common ||
            message.CommandCode != DiameterCommandCodes.DeviceWatchdog ||
            request != requestExpected)
        {
            result.Code = "invalid-watchdog-message";
            return result;
        }

        if (!requestExpected)
        {
            int resultCount = 0;
            int index = 0;
            while (index < message.Avps.Count)
            {
                DiameterAvp avp = message.Avps[index];
                if (avp.Code == DiameterAvpCodes.ResultCode &&
                    avp.VendorId == DiameterVendorIds.Ietf)
                {
                    if (avp.Data == null || avp.Data.Length != 4)
                    {
                        result.Code = "invalid-watchdog-result-code";
                        return result;
                    }

                    result.ResultCode = NetworkByteOrder.ReadUInt32(avp.Data);
                    resultCount = resultCount + 1;
                }

                index = index + 1;
            }

            if (resultCount != 1)
            {
                result.Code = "invalid-watchdog-result-code";
                return result;
            }
        }

        result.Valid = true;
        result.Code = "valid-watchdog-message";
        return result;
    }

    internal DiameterBaseCommandOptions ClonePeerWatchdogOriginOptions(
        DiameterBaseCommandOptions source)
    {
        DiameterBaseCommandOptions result = new DiameterBaseCommandOptions();
        result.OriginHost.Value = source.OriginHost.Value;
        result.OriginRealm.Value = source.OriginRealm.Value;
        result.ResultCode = source.ResultCode;
        result.Request = source.Request;
        return result;
    }

    internal PeerWatchdogResult CreatePeerWatchdogResult(
        DiameterPeerSnapshot snapshot,
        string code)
    {
        PeerWatchdogResult result = new PeerWatchdogResult();
        result.Peer = PeerTableCloner.ClonePeerTableSnapshot(snapshot);
        result.Code = code;
        return result;
    }
}
