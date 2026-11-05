using System;

namespace diameter;

internal interface IPeerRestartNotificationSink
{
    void HandlePeerRestart(DiameterPeerEvent peerEvent);
}

internal class PeerRestartObservation
{
    internal bool Observed;
    internal bool RestartDetected;
    internal int NotifiedSubsystems;
    internal uint PreviousOriginStateId;
    internal uint CurrentOriginStateId;
    internal DiameterPeerEvent? PeerEvent;
    internal DiameterPeerSnapshot Peer = new DiameterPeerSnapshot();
    internal string Code = string.Empty;
}

internal class PeerOriginStateMessage
{
    internal bool Valid;
    internal bool Present;
    internal uint OriginStateId;
    internal string Code = string.Empty;
}

internal class PeerRestartDetector
{
    internal PeerTable Table;
    internal IPeerRestartNotificationSink RequestSubsystem;
    internal IPeerRestartNotificationSink SessionSubsystem;
    internal IPeerRestartNotificationSink ApplicationSubsystem;

    internal PeerRestartDetector(
        PeerTable table,
        IPeerRestartNotificationSink requestSubsystem,
        IPeerRestartNotificationSink sessionSubsystem,
        IPeerRestartNotificationSink applicationSubsystem)
    {
        if (table == null || requestSubsystem == null ||
            sessionSubsystem == null || applicationSubsystem == null)
        {
            throw new ArgumentException(
                "A peer table and all restart notification sinks are required.");
        }

        Table = table;
        RequestSubsystem = requestSubsystem;
        SessionSubsystem = sessionSubsystem;
        ApplicationSubsystem = applicationSubsystem;
    }

    internal PeerRestartObservation ObservePeerCapabilities(
        string identity,
        DiameterPeerCapabilities capabilities)
    {
        if (capabilities == null)
        {
            throw new ArgumentException("Peer capabilities are required.");
        }

        string key = Table.ReadPeerTableKey(identity);
        PeerRestartObservation result;
        lock (Table.TableLock)
        {
            DiameterPeerSnapshot snapshot =
                Table.ReadRequiredPeerTableEntry(key).Snapshot;
            uint previous = snapshot.Capabilities.OriginStateId;
            DiameterPeerSnapshot updated = Table.UpdatePeerTableCapabilities(
                key,
                capabilities);
            result = CreatePeerRestartObservation(
                updated,
                previous,
                capabilities.OriginStateId);
        }

        NotifyPeerRestartSubsystems(result);
        return result;
    }

    internal PeerRestartObservation ObservePeerMessage(
        string identity,
        DiameterMessage message)
    {
        string key = Table.ReadPeerTableKey(identity);
        PeerOriginStateMessage parsed = ReadPeerOriginStateMessage(key, message);
        if (!parsed.Valid || !parsed.Present)
        {
            PeerRestartObservation ignored = new PeerRestartObservation();
            lock (Table.TableLock)
            {
                ignored.Peer = PeerTableCloner.ClonePeerTableSnapshot(
                    Table.ReadRequiredPeerTableEntry(key).Snapshot);
            }

            ignored.Code = parsed.Code;
            return ignored;
        }

        PeerRestartObservation result;
        lock (Table.TableLock)
        {
            DiameterPeerSnapshot snapshot =
                Table.ReadRequiredPeerTableEntry(key).Snapshot;
            uint previous = snapshot.Capabilities.OriginStateId;
            snapshot.Capabilities.OriginStateId = parsed.OriginStateId;
            result = CreatePeerRestartObservation(
                PeerTableCloner.ClonePeerTableSnapshot(snapshot),
                previous,
                parsed.OriginStateId);
        }

        NotifyPeerRestartSubsystems(result);
        return result;
    }

    internal PeerRestartObservation CreatePeerRestartObservation(
        DiameterPeerSnapshot peer,
        uint previous,
        uint current)
    {
        PeerRestartObservation result = new PeerRestartObservation();
        result.Peer = PeerTableCloner.ClonePeerTableSnapshot(peer);
        result.PreviousOriginStateId = previous;
        result.CurrentOriginStateId = current;
        if (current == 0)
        {
            result.Code = "origin-state-inference-disabled";
            return result;
        }

        result.Observed = true;
        if (previous == 0)
        {
            result.Code = "origin-state-baseline-recorded";
            return result;
        }

        if (current <= previous)
        {
            result.Code = "origin-state-not-advanced";
            return result;
        }

        DiameterPeerEvent peerEvent = new DiameterPeerEvent();
        peerEvent.Kind = DiameterPeerEventKind.RestartDetected;
        peerEvent.Peer = PeerTableCloner.ClonePeerTableSnapshot(peer);
        peerEvent.PreviousState = peer.State;
        peerEvent.CurrentState = peer.State;
        peerEvent.PreviousOriginStateId = previous;
        peerEvent.CurrentOriginStateId = current;
        peerEvent.OccurredAt = Table.Clock.ReadUtcTime();
        peerEvent.Code = "peer.restart.origin-state-id";
        peerEvent.Message =
            "Peer restart detected from an increased Origin-State-Id.";
        result.RestartDetected = true;
        result.PeerEvent = peerEvent;
        result.Code = "peer-restart-detected";
        return result;
    }

    internal PeerOriginStateMessage ReadPeerOriginStateMessage(
        string peerKey,
        DiameterMessage message)
    {
        PeerOriginStateMessage result = new PeerOriginStateMessage();
        if (message == null || message.Avps == null)
        {
            result.Code = "origin-state-message-missing";
            return result;
        }

        int originHostCount = 0;
        int originStateCount = 0;
        string originHost = string.Empty;
        uint originStateId = 0;
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp != null && avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.OriginHost)
            {
                DiameterValueResult value =
                    ValueCodecs.DecodeDiameterIdentity(avp.Data);
                if (value.Status != DiameterCodecStatus.Success)
                {
                    result.Code = "invalid-origin-host";
                    return result;
                }

                originHost = value.Text;
                originHostCount = originHostCount + 1;
            }
            else if (avp != null && avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.OriginStateId)
            {
                if (avp.Data == null || avp.Data.Length != 4)
                {
                    result.Code = "invalid-origin-state-id";
                    return result;
                }

                originStateId = NetworkByteOrder.ReadUInt32(avp.Data);
                originStateCount = originStateCount + 1;
            }

            index = index + 1;
        }

        if (originStateCount == 0)
        {
            result.Valid = true;
            result.Code = "origin-state-id-absent";
            return result;
        }

        if (originStateCount != 1 || originHostCount != 1)
        {
            result.Code = "ambiguous-origin-state-id";
            return result;
        }

        if (Table.ReadPeerTableKey(originHost) != peerKey)
        {
            result.Code = "origin-state-peer-mismatch";
            return result;
        }

        result.Valid = true;
        result.Present = true;
        result.OriginStateId = originStateId;
        result.Code = "origin-state-id-observed";
        return result;
    }

    internal void NotifyPeerRestartSubsystems(PeerRestartObservation observation)
    {
        if (!observation.RestartDetected || observation.PeerEvent == null)
        {
            return;
        }

        RequestSubsystem.HandlePeerRestart(
            PeerTableCloner.ClonePeerTableEvent(observation.PeerEvent));
        observation.NotifiedSubsystems = observation.NotifiedSubsystems + 1;
        SessionSubsystem.HandlePeerRestart(
            PeerTableCloner.ClonePeerTableEvent(observation.PeerEvent));
        observation.NotifiedSubsystems = observation.NotifiedSubsystems + 1;
        ApplicationSubsystem.HandlePeerRestart(
            PeerTableCloner.ClonePeerTableEvent(observation.PeerEvent));
        observation.NotifiedSubsystems = observation.NotifiedSubsystems + 1;
    }
}
