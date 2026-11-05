using System;
using System.Collections.Generic;
using System.Globalization;

namespace diameter;

internal class PendingRequestTableEntry
{
    internal string Key = string.Empty;
    internal DiameterPendingRequestSnapshot Snapshot =
        new DiameterPendingRequestSnapshot();
    internal List<PendingRequestTableAttempt> Attempts =
        new List<PendingRequestTableAttempt>();
    internal IDiameterAnswerHandler? Handler;
    internal long DeadlineMonotonicMilliseconds;
}

internal class PendingRequestTableAttempt
{
    internal string Key = string.Empty;
    internal DiameterPendingRequestSnapshot Snapshot =
        new DiameterPendingRequestSnapshot();
}

internal class PendingRequestTableTombstone
{
    internal string Key = string.Empty;
    internal DiameterPendingRequestSnapshot Snapshot =
        new DiameterPendingRequestSnapshot();
    internal IDiameterAnswerHandler? Handler;
    internal long ExpiresMonotonicMilliseconds;
}

internal class PendingRequestTableResult
{
    internal bool Accepted;
    internal bool Found;
    internal bool Late;
    internal bool CapacityReached;
    internal DiameterPendingRequestSnapshot? PendingRequest;
    internal DiameterAnswerResult? AnswerResult;
    internal IDiameterAnswerHandler? Handler;
    internal string Code = string.Empty;
}

internal class PendingRequestTable
{
    internal Dictionary<string, PendingRequestTableEntry> EntriesByKey =
        new Dictionary<string, PendingRequestTableEntry>();
    internal Dictionary<long, PendingRequestTableEntry> EntriesByIdentifier =
        new Dictionary<long, PendingRequestTableEntry>();
    internal List<long> RequestOrder = new List<long>();
    internal Dictionary<string, PendingRequestTableTombstone> Tombstones =
        new Dictionary<string, PendingRequestTableTombstone>();
    internal List<string> TombstoneOrder = new List<string>();
    internal object TableLock = new object();
    internal IDiameterClock Clock;
    internal int MaximumPendingRequests;
    internal int LateAnswerLifetimeMilliseconds;
    internal long NextRequestIdentifier = 1;

    internal PendingRequestTable(
        IDiameterClock clock,
        int maximumPendingRequests,
        int lateAnswerLifetimeMilliseconds)
    {
        if (clock == null)
        {
            throw new ArgumentException("A pending-request clock is required.");
        }

        if (maximumPendingRequests <= 0)
        {
            throw new ArgumentOutOfRangeException("maximumPendingRequests");
        }

        if (lateAnswerLifetimeMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                "lateAnswerLifetimeMilliseconds");
        }

        Clock = clock;
        MaximumPendingRequests = maximumPendingRequests;
        LateAnswerLifetimeMilliseconds = lateAnswerLifetimeMilliseconds;
    }

    internal PendingRequestTableResult RegisterPendingRequest(
        DiameterSendRequest sendRequest,
        DiameterIdentity peerIdentity,
        DiameterMessage outboundMessage,
        IDiameterAnswerHandler? handler,
        bool delivered)
    {
        ValidatePendingRequestRegistration(
            sendRequest,
            peerIdentity,
            outboundMessage);
        DiameterValueResult normalized =
            ValueCodecs.NormalizeDiameterIdentity(peerIdentity);
        string key = CreatePendingRequestKey(
            normalized.Text,
            outboundMessage.HopByHopIdentifier);
        lock (TableLock)
        {
            long monotonicNow = Clock.ReadMonotonicMilliseconds();
            PurgeExpiredPendingRequestTombstones(monotonicNow);
            if (EntriesByKey.ContainsKey(key) || Tombstones.ContainsKey(key))
            {
                return CreatePendingRequestTableResult(
                    "pending-request-key-collision");
            }

            if (EntriesByIdentifier.Count >= MaximumPendingRequests)
            {
                PendingRequestTableResult full = CreatePendingRequestTableResult(
                    "pending-request-limit-reached");
                full.CapacityReached = true;
                return full;
            }

            DateTimeOffset utcNow = Clock.ReadUtcTime();
            PendingRequestTableEntry entry = new PendingRequestTableEntry();
            entry.Key = key;
            entry.Handler = handler;
            entry.DeadlineMonotonicMilliseconds = AddPendingRequestDuration(
                monotonicNow,
                sendRequest.TimeoutMilliseconds);
            entry.Snapshot.RequestIdentifier =
                ReadNextPendingRequestIdentifier();
            entry.Snapshot.SendRequest = ClonePendingSendRequest(sendRequest);
            entry.Snapshot.OutboundMessage =
                ClonePendingMessage(outboundMessage);
            entry.Snapshot.PeerIdentity = ClonePendingIdentity(peerIdentity);
            entry.Snapshot.OriginalHopByHopIdentifier =
                outboundMessage.HopByHopIdentifier;
            entry.Snapshot.HopByHopIdentifier =
                outboundMessage.HopByHopIdentifier;
            entry.Snapshot.EndToEndIdentifier =
                outboundMessage.EndToEndIdentifier;
            entry.Snapshot.CreatedAt = utcNow;
            entry.Snapshot.LastSentAt = utcNow;
            entry.Snapshot.DeadlineAt = utcNow.AddMilliseconds(
                sendRequest.TimeoutMilliseconds);
            entry.Snapshot.AttemptCount = 1;
            entry.Snapshot.Delivered = delivered;
            entry.Snapshot.FailoverEligible = sendRequest.AllowFailover;
            entry.Snapshot.RetransmissionEligible =
                sendRequest.AllowRetransmission;
            AddPendingRequestAttempt(entry, key);
            EntriesByIdentifier[entry.Snapshot.RequestIdentifier] = entry;
            RequestOrder.Add(entry.Snapshot.RequestIdentifier);

            PendingRequestTableResult result = CreatePendingRequestTableResult(
                "pending-request-registered");
            result.Accepted = true;
            result.Found = true;
            result.PendingRequest = ClonePendingRequestSnapshot(entry.Snapshot);
            return result;
        }
    }

    internal PendingRequestTableResult ReadPendingRequest(
        string peerIdentity,
        uint hopByHopIdentifier)
    {
        string key = CreatePendingRequestKey(
            NormalizePendingPeerIdentity(peerIdentity),
            hopByHopIdentifier);
        lock (TableLock)
        {
            PurgeExpiredPendingRequestTombstones(
                Clock.ReadMonotonicMilliseconds());
            if (EntriesByKey.ContainsKey(key))
            {
                PendingRequestTableEntry entry = EntriesByKey[key];
                PendingRequestTableAttempt attempt =
                    ReadPendingRequestAttempt(entry, key);
                PendingRequestTableResult found = CreatePendingRequestTableResult(
                    "pending-request-found");
                found.Found = true;
                found.PendingRequest = ClonePendingRequestSnapshot(
                    attempt.Snapshot);
                return found;
            }

            if (Tombstones.ContainsKey(key))
            {
                PendingRequestTableResult late = CreatePendingRequestTableResult(
                    "pending-request-already-completed");
                late.Found = true;
                late.Late = true;
                late.PendingRequest = ClonePendingRequestSnapshot(
                    Tombstones[key].Snapshot);
                return late;
            }

            return CreatePendingRequestTableResult("pending-request-not-found");
        }
    }

    internal PendingRequestTableResult ReadPendingRequestByIdentifier(
        long requestIdentifier)
    {
        if (requestIdentifier <= 0)
        {
            return CreatePendingRequestTableResult(
                "pending-request-not-found");
        }

        lock (TableLock)
        {
            PurgeExpiredPendingRequestTombstones(
                Clock.ReadMonotonicMilliseconds());
            if (!EntriesByIdentifier.ContainsKey(requestIdentifier))
            {
                return CreatePendingRequestTableResult(
                    "pending-request-not-found");
            }

            PendingRequestTableResult result =
                CreatePendingRequestTableResult("pending-request-found");
            result.Found = true;
            result.PendingRequest = ClonePendingRequestSnapshot(
                EntriesByIdentifier[requestIdentifier].Snapshot);
            return result;
        }
    }

    internal PendingRequestTableResult RetryPendingRequest(
        long requestIdentifier,
        uint expectedHopByHopIdentifier,
        DiameterIdentity alternatePeer,
        DiameterMessage retransmittedMessage,
        bool delivered)
    {
        if (requestIdentifier <= 0 || alternatePeer == null ||
            retransmittedMessage == null)
        {
            throw new ArgumentException(
                "A pending request, alternate peer, and retransmitted message are required.");
        }

        ValidatePendingRequestMessageStorage(
            retransmittedMessage,
            "retransmitted message");
        DiameterValueResult normalizedAlternate =
            ValueCodecs.NormalizeDiameterIdentity(alternatePeer);
        if (normalizedAlternate.Status != DiameterCodecStatus.Success)
        {
            throw new ArgumentException("A valid alternate peer is required.");
        }

        lock (TableLock)
        {
            long monotonicNow = Clock.ReadMonotonicMilliseconds();
            PurgeExpiredPendingRequestTombstones(monotonicNow);
            if (!EntriesByIdentifier.ContainsKey(requestIdentifier))
            {
                return CreatePendingRequestTableResult(
                    "pending-request-not-found");
            }

            PendingRequestTableEntry entry =
                EntriesByIdentifier[requestIdentifier];
            DiameterPendingRequestSnapshot current = entry.Snapshot;
            if (entry.DeadlineMonotonicMilliseconds <= monotonicNow)
            {
                return CreatePendingRequestTableResult(
                    "pending-request-deadline-expired");
            }

            if (current.HopByHopIdentifier != expectedHopByHopIdentifier)
            {
                return CreatePendingRequestTableResult(
                    "pending-request-attempt-changed");
            }

            string currentPeer = NormalizePendingPeerIdentity(
                current.PeerIdentity.Value);
            if (currentPeer == normalizedAlternate.Text)
            {
                return CreatePendingRequestTableResult(
                    "alternate-peer-not-distinct");
            }

            string validationCode = ValidatePendingRequestRetryMessage(
                current,
                retransmittedMessage);
            if (validationCode.Length > 0)
            {
                return CreatePendingRequestTableResult(validationCode);
            }

            string key = CreatePendingRequestKey(
                normalizedAlternate.Text,
                retransmittedMessage.HopByHopIdentifier);
            if (EntriesByKey.ContainsKey(key) || Tombstones.ContainsKey(key))
            {
                return CreatePendingRequestTableResult(
                    "pending-request-key-collision");
            }

            DateTimeOffset utcNow = Clock.ReadUtcTime();
            current.OutboundMessage = ClonePendingMessage(
                retransmittedMessage);
            current.PeerIdentity = ClonePendingIdentity(alternatePeer);
            current.HopByHopIdentifier =
                retransmittedMessage.HopByHopIdentifier;
            current.LastSentAt = utcNow;
            if (current.AttemptCount < int.MaxValue)
            {
                current.AttemptCount = current.AttemptCount + 1;
            }

            current.Delivered = current.Delivered || delivered;
            entry.Key = key;
            AddPendingRequestAttempt(entry, key);

            PendingRequestTableResult result =
                CreatePendingRequestTableResult(
                    "pending-request-retransmitted");
            result.Accepted = true;
            result.Found = true;
            result.PendingRequest = ClonePendingRequestSnapshot(current);
            return result;
        }
    }

    internal PendingRequestTableResult RecordPendingRequestDelivery(
        long requestIdentifier,
        uint expectedHopByHopIdentifier,
        bool delivered)
    {
        if (requestIdentifier <= 0)
        {
            throw new ArgumentOutOfRangeException("requestIdentifier");
        }

        lock (TableLock)
        {
            if (!EntriesByIdentifier.ContainsKey(requestIdentifier))
            {
                return CreatePendingRequestTableResult(
                    "pending-request-not-found");
            }

            PendingRequestTableEntry entry =
                EntriesByIdentifier[requestIdentifier];
            if (entry.Snapshot.HopByHopIdentifier !=
                expectedHopByHopIdentifier)
            {
                return CreatePendingRequestTableResult(
                    "pending-request-attempt-changed");
            }

            entry.Snapshot.Delivered =
                entry.Snapshot.Delivered || delivered;
            PendingRequestTableResult result =
                CreatePendingRequestTableResult(
                    delivered ?
                        "pending-request-delivered" :
                        "pending-request-not-delivered");
            result.Accepted = true;
            result.Found = true;
            result.PendingRequest = ClonePendingRequestSnapshot(
                entry.Snapshot);
            return result;
        }
    }

    internal PendingRequestTableResult CompletePendingRequest(
        string peerIdentity,
        uint hopByHopIdentifier,
        DiameterMessage answer)
    {
        if (answer == null)
        {
            throw new ArgumentException("A Diameter answer is required.");
        }

        ValidatePendingRequestMessageStorage(answer, "answer");

        string key = CreatePendingRequestKey(
            NormalizePendingPeerIdentity(peerIdentity),
            hopByHopIdentifier);
        lock (TableLock)
        {
            long monotonicNow = Clock.ReadMonotonicMilliseconds();
            PurgeExpiredPendingRequestTombstones(monotonicNow);
            if (EntriesByKey.ContainsKey(key))
            {
                PendingRequestTableEntry entry = EntriesByKey[key];
                PendingRequestTableAttempt attempt =
                    ReadPendingRequestAttempt(entry, key);
                RemovePendingRequestEntry(entry);
                AddPendingRequestTombstones(entry, monotonicNow);
                PendingRequestTableResult completed =
                    CreatePendingRequestTableResult(
                        "pending-request-completed");
                completed.Found = true;
                completed.Handler = entry.Handler;
                completed.PendingRequest = ClonePendingRequestSnapshot(
                    entry.Snapshot);
                completed.AnswerResult = CreatePendingAnswerResult(
                    entry.Snapshot,
                    attempt.Snapshot.PeerIdentity,
                    answer,
                    false,
                    "answer-received");
                return completed;
            }

            if (Tombstones.ContainsKey(key))
            {
                PendingRequestTableTombstone tombstone = Tombstones[key];
                PendingRequestTableResult late = CreatePendingRequestTableResult(
                    "late-answer-received");
                late.Found = true;
                late.Late = true;
                late.PendingRequest = ClonePendingRequestSnapshot(
                    tombstone.Snapshot);
                if (tombstone.Snapshot.SendRequest.ReportLateAnswers)
                {
                    late.Handler = tombstone.Handler;
                    late.AnswerResult = CreatePendingAnswerResult(
                        tombstone.Snapshot,
                        tombstone.Snapshot.PeerIdentity,
                        answer,
                        true,
                        "late-answer-received");
                }

                return late;
            }

            return CreatePendingRequestTableResult("pending-request-not-found");
        }
    }

    internal PendingRequestTableResult CancelPendingRequest(
        long requestIdentifier)
    {
        if (requestIdentifier <= 0)
        {
            throw new ArgumentOutOfRangeException("requestIdentifier");
        }

        lock (TableLock)
        {
            long monotonicNow = Clock.ReadMonotonicMilliseconds();
            PurgeExpiredPendingRequestTombstones(monotonicNow);
            if (!EntriesByIdentifier.ContainsKey(requestIdentifier))
            {
                return CreatePendingRequestTableResult(
                    "pending-request-not-found");
            }

            PendingRequestTableEntry entry =
                EntriesByIdentifier[requestIdentifier];
            RemovePendingRequestEntry(entry);
            AddPendingRequestTombstones(entry, monotonicNow);
            PendingRequestTableResult result = CreatePendingRequestTableResult(
                "pending-request-cancelled");
            result.Found = true;
            result.Handler = entry.Handler;
            result.PendingRequest = ClonePendingRequestSnapshot(entry.Snapshot);
            result.AnswerResult = CreatePendingTerminalResult(
                entry.Snapshot,
                true,
                false,
                "request-cancelled");
            return result;
        }
    }

    internal PendingRequestTableResult FailPendingRequest(
        long requestIdentifier,
        bool peerFailed,
        string code)
    {
        if (requestIdentifier <= 0)
        {
            throw new ArgumentOutOfRangeException("requestIdentifier");
        }

        if (code == null || code.Length == 0 ||
            ValueCodecs.FindInvalidUtf16Index(code) >= 0)
        {
            throw new ArgumentException(
                "A valid request failure code is required.");
        }

        lock (TableLock)
        {
            long monotonicNow = Clock.ReadMonotonicMilliseconds();
            PurgeExpiredPendingRequestTombstones(monotonicNow);
            if (!EntriesByIdentifier.ContainsKey(requestIdentifier))
            {
                return CreatePendingRequestTableResult(
                    "pending-request-not-found");
            }

            PendingRequestTableEntry entry =
                EntriesByIdentifier[requestIdentifier];
            RemovePendingRequestEntry(entry);
            AddPendingRequestTombstones(entry, monotonicNow);
            PendingRequestTableResult result =
                CreatePendingRequestTableResult(
                    "pending-request-failed");
            result.Found = true;
            result.Handler = entry.Handler;
            result.PendingRequest = ClonePendingRequestSnapshot(
                entry.Snapshot);
            result.AnswerResult = CreatePendingTerminalResult(
                entry.Snapshot,
                false,
                false,
                code);
            result.AnswerResult.PeerFailed = peerFailed;
            return result;
        }
    }

    internal List<PendingRequestTableResult> ExpirePendingRequests()
    {
        List<PendingRequestTableResult> results =
            new List<PendingRequestTableResult>();
        lock (TableLock)
        {
            long monotonicNow = Clock.ReadMonotonicMilliseconds();
            PurgeExpiredPendingRequestTombstones(monotonicNow);
            List<long> expired = new List<long>();
            int index = 0;
            while (index < RequestOrder.Count)
            {
                long identifier = RequestOrder[index];
                if (EntriesByIdentifier.ContainsKey(identifier) &&
                    EntriesByIdentifier[identifier].DeadlineMonotonicMilliseconds <=
                        monotonicNow)
                {
                    expired.Add(identifier);
                }

                index = index + 1;
            }

            index = 0;
            while (index < expired.Count)
            {
                PendingRequestTableEntry entry =
                    EntriesByIdentifier[expired[index]];
                RemovePendingRequestEntry(entry);
                AddPendingRequestTombstones(entry, monotonicNow);
                PendingRequestTableResult result =
                    CreatePendingRequestTableResult("pending-request-timed-out");
                result.Found = true;
                result.Handler = entry.Handler;
                result.PendingRequest = ClonePendingRequestSnapshot(entry.Snapshot);
                result.AnswerResult = CreatePendingTerminalResult(
                    entry.Snapshot,
                    false,
                    true,
                    "request-timed-out");
                results.Add(result);
                index = index + 1;
            }
        }

        return results;
    }

    internal List<DiameterPendingRequestSnapshot> ReadPendingRequestSnapshots()
    {
        List<DiameterPendingRequestSnapshot> result =
            new List<DiameterPendingRequestSnapshot>();
        lock (TableLock)
        {
            int index = 0;
            while (index < RequestOrder.Count)
            {
                long identifier = RequestOrder[index];
                if (EntriesByIdentifier.ContainsKey(identifier))
                {
                    result.Add(ClonePendingRequestSnapshot(
                        EntriesByIdentifier[identifier].Snapshot));
                }

                index = index + 1;
            }
        }

        return result;
    }

    internal int ReadPendingRequestCount()
    {
        lock (TableLock)
        {
            return EntriesByIdentifier.Count;
        }
    }

    internal int ReadPendingRequestTombstoneCount()
    {
        lock (TableLock)
        {
            PurgeExpiredPendingRequestTombstones(
                Clock.ReadMonotonicMilliseconds());
            return Tombstones.Count;
        }
    }

    internal void ValidatePendingRequestRegistration(
        DiameterSendRequest sendRequest,
        DiameterIdentity peerIdentity,
        DiameterMessage outboundMessage)
    {
        if (sendRequest == null || peerIdentity == null ||
            outboundMessage == null)
        {
            throw new ArgumentException(
                "A send request, peer identity, and outbound message are required.");
        }

        sendRequest.ValidateSendRequest();
        if (ValueCodecs.NormalizeDiameterIdentity(peerIdentity).Status !=
                DiameterCodecStatus.Success)
        {
            throw new ArgumentException("A valid peer identity is required.");
        }

        if (outboundMessage.Avps == null ||
            outboundMessage.Version != DiameterProtocolConstants.Version ||
            outboundMessage.CommandCode == 0 ||
            outboundMessage.CommandCode >
                DiameterProtocolConstants.MaximumCommandCode ||
            (outboundMessage.Flags & DiameterMessageFlags.Request) ==
                DiameterMessageFlags.None ||
            (outboundMessage.Flags & DiameterMessageFlags.Error) !=
                DiameterMessageFlags.None ||
            outboundMessage.CommandCode != sendRequest.Request.CommandCode ||
            outboundMessage.ApplicationId != sendRequest.Request.ApplicationId)
        {
            throw new ArgumentException(
                "The current outbound message must match the send request.");
        }

        ValidatePendingRequestMessageStorage(
            sendRequest.Request,
            "send request");
        ValidatePendingRequestMessageStorage(
            outboundMessage,
            "outbound message");
    }

    internal void ValidatePendingRequestMessageStorage(
        DiameterMessage message,
        string description)
    {
        if (message.Avps == null)
        {
            throw new ArgumentException(
                "The " + description + " AVP collection is required.");
        }

        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp == null || avp.Data == null)
            {
                throw new ArgumentException(
                    "The " + description +
                    " must contain owned AVPs and payloads.");
            }

            index = index + 1;
        }
    }

    internal string ValidatePendingRequestRetryMessage(
        DiameterPendingRequestSnapshot current,
        DiameterMessage retransmittedMessage)
    {
        if (!current.FailoverEligible)
        {
            return "pending-request-failover-disabled";
        }

        if (!current.RetransmissionEligible)
        {
            return "pending-request-retransmission-disabled";
        }

        if (retransmittedMessage.Version != DiameterProtocolConstants.Version ||
            (retransmittedMessage.Flags & DiameterMessageFlags.Request) ==
                DiameterMessageFlags.None ||
            (retransmittedMessage.Flags & DiameterMessageFlags.Error) !=
                DiameterMessageFlags.None ||
            (retransmittedMessage.Flags &
                DiameterMessageFlags.PotentiallyRetransmitted) ==
                DiameterMessageFlags.None)
        {
            return "invalid-retransmitted-request-header";
        }

        if (retransmittedMessage.CommandCode !=
                current.OutboundMessage.CommandCode ||
            retransmittedMessage.ApplicationId !=
                current.OutboundMessage.ApplicationId ||
            retransmittedMessage.EndToEndIdentifier !=
                current.EndToEndIdentifier ||
            retransmittedMessage.HopByHopIdentifier ==
                current.HopByHopIdentifier)
        {
            return "invalid-retransmitted-request-correlation";
        }

        DiameterMessageFlags currentProxyable =
            current.OutboundMessage.Flags & DiameterMessageFlags.Proxyable;
        DiameterMessageFlags retryProxyable =
            retransmittedMessage.Flags & DiameterMessageFlags.Proxyable;
        if (currentProxyable != retryProxyable)
        {
            return "invalid-retransmitted-request-proxyable-bit";
        }

        return string.Empty;
    }

    internal long ReadNextPendingRequestIdentifier()
    {
        long candidate = NextRequestIdentifier;
        while (EntriesByIdentifier.ContainsKey(candidate) || candidate <= 0)
        {
            if (candidate == long.MaxValue)
            {
                candidate = 1;
            }
            else
            {
                candidate = candidate + 1;
            }
        }

        if (candidate == long.MaxValue)
        {
            NextRequestIdentifier = 1;
        }
        else
        {
            NextRequestIdentifier = candidate + 1;
        }

        return candidate;
    }

    internal void RemovePendingRequestEntry(PendingRequestTableEntry entry)
    {
        int index = 0;
        while (index < entry.Attempts.Count)
        {
            EntriesByKey.Remove(entry.Attempts[index].Key);
            index = index + 1;
        }

        EntriesByIdentifier.Remove(entry.Snapshot.RequestIdentifier);
        RequestOrder.Remove(entry.Snapshot.RequestIdentifier);
    }

    internal void AddPendingRequestTombstones(
        PendingRequestTableEntry entry,
        long monotonicNow)
    {
        int index = 0;
        while (index < entry.Attempts.Count)
        {
            while (TombstoneOrder.Count >= MaximumPendingRequests)
            {
                string oldest = TombstoneOrder[0];
                TombstoneOrder.RemoveAt(0);
                Tombstones.Remove(oldest);
            }

            PendingRequestTableAttempt attempt = entry.Attempts[index];
            PendingRequestTableTombstone tombstone =
                new PendingRequestTableTombstone();
            tombstone.Key = attempt.Key;
            tombstone.Snapshot = ClonePendingRequestSnapshot(
                attempt.Snapshot);
            if (entry.Snapshot.SendRequest.ReportLateAnswers)
            {
                tombstone.Handler = entry.Handler;
            }

            tombstone.ExpiresMonotonicMilliseconds = AddPendingRequestDuration(
                monotonicNow,
                LateAnswerLifetimeMilliseconds);
            Tombstones[attempt.Key] = tombstone;
            TombstoneOrder.Add(attempt.Key);
            index = index + 1;
        }
    }

    internal void AddPendingRequestAttempt(
        PendingRequestTableEntry entry,
        string key)
    {
        PendingRequestTableAttempt attempt =
            new PendingRequestTableAttempt();
        attempt.Key = key;
        attempt.Snapshot = ClonePendingRequestSnapshot(entry.Snapshot);
        entry.Attempts.Add(attempt);
        EntriesByKey[key] = entry;
    }

    internal PendingRequestTableAttempt ReadPendingRequestAttempt(
        PendingRequestTableEntry entry,
        string key)
    {
        int index = 0;
        while (index < entry.Attempts.Count)
        {
            if (entry.Attempts[index].Key == key)
            {
                return entry.Attempts[index];
            }

            index = index + 1;
        }

        throw new InvalidOperationException(
            "The pending-request attempt index is inconsistent.");
    }

    internal void PurgeExpiredPendingRequestTombstones(long monotonicNow)
    {
        int index = 0;
        while (index < TombstoneOrder.Count)
        {
            string key = TombstoneOrder[index];
            if (!Tombstones.ContainsKey(key) ||
                Tombstones[key].ExpiresMonotonicMilliseconds <= monotonicNow)
            {
                Tombstones.Remove(key);
                TombstoneOrder.RemoveAt(index);
            }
            else
            {
                index = index + 1;
            }
        }
    }

    internal string NormalizePendingPeerIdentity(string peerIdentity)
    {
        DiameterIdentity identity = new DiameterIdentity();
        identity.Value = peerIdentity;
        DiameterValueResult normalized =
            ValueCodecs.NormalizeDiameterIdentity(identity);
        if (normalized.Status != DiameterCodecStatus.Success)
        {
            throw new ArgumentException("A valid peer identity is required.");
        }

        return normalized.Text;
    }

    internal string CreatePendingRequestKey(
        string normalizedPeerIdentity,
        uint hopByHopIdentifier)
    {
        return normalizedPeerIdentity + "|" +
            hopByHopIdentifier.ToString(CultureInfo.InvariantCulture);
    }

    internal long AddPendingRequestDuration(long value, int durationMilliseconds)
    {
        if (value > long.MaxValue - durationMilliseconds)
        {
            return long.MaxValue;
        }

        return value + durationMilliseconds;
    }

    internal PendingRequestTableResult CreatePendingRequestTableResult(
        string code)
    {
        PendingRequestTableResult result = new PendingRequestTableResult();
        result.Code = code;
        return result;
    }

    internal DiameterAnswerResult CreatePendingAnswerResult(
        DiameterPendingRequestSnapshot snapshot,
        DiameterIdentity answerPeerIdentity,
        DiameterMessage answer,
        bool late,
        string code)
    {
        DiameterAnswerResult result = new DiameterAnswerResult();
        result.RequestIdentifier = snapshot.RequestIdentifier;
        result.PendingRequest = ClonePendingRequestSnapshot(snapshot);
        result.Answer = ClonePendingMessage(answer);
        result.PeerIdentity = ClonePendingIdentity(answerPeerIdentity);
        result.AnswerReceived = true;
        result.Correlated = true;
        result.ErrorAnswer =
            (answer.Flags & DiameterMessageFlags.Error) !=
                DiameterMessageFlags.None;
        result.Late = late;
        result.Code = code;
        return result;
    }

    internal DiameterAnswerResult CreatePendingTerminalResult(
        DiameterPendingRequestSnapshot snapshot,
        bool cancelled,
        bool timedOut,
        string code)
    {
        DiameterAnswerResult result = new DiameterAnswerResult();
        result.RequestIdentifier = snapshot.RequestIdentifier;
        result.PendingRequest = ClonePendingRequestSnapshot(snapshot);
        result.PeerIdentity = ClonePendingIdentity(snapshot.PeerIdentity);
        result.Cancelled = cancelled;
        result.TimedOut = timedOut;
        result.Code = code;
        return result;
    }

    internal DiameterPendingRequestSnapshot ClonePendingRequestSnapshot(
        DiameterPendingRequestSnapshot source)
    {
        DiameterPendingRequestSnapshot result =
            new DiameterPendingRequestSnapshot();
        result.RequestIdentifier = source.RequestIdentifier;
        result.SendRequest = ClonePendingSendRequest(source.SendRequest);
        result.OutboundMessage = ClonePendingMessage(source.OutboundMessage);
        result.PeerIdentity = ClonePendingIdentity(source.PeerIdentity);
        result.OriginalHopByHopIdentifier =
            source.OriginalHopByHopIdentifier;
        result.HopByHopIdentifier = source.HopByHopIdentifier;
        result.EndToEndIdentifier = source.EndToEndIdentifier;
        result.CreatedAt = source.CreatedAt;
        result.LastSentAt = source.LastSentAt;
        result.DeadlineAt = source.DeadlineAt;
        result.AttemptCount = source.AttemptCount;
        result.Delivered = source.Delivered;
        result.FailoverEligible = source.FailoverEligible;
        result.RetransmissionEligible = source.RetransmissionEligible;
        return result;
    }

    internal DiameterSendRequest ClonePendingSendRequest(
        DiameterSendRequest source)
    {
        DiameterSendRequest result = new DiameterSendRequest();
        result.Request = ClonePendingMessage(source.Request);
        if (source.PreferredPeer != null)
        {
            result.PreferredPeer = ClonePendingIdentity(source.PreferredPeer);
        }

        result.TimeoutMilliseconds = source.TimeoutMilliseconds;
        result.AllowFailover = source.AllowFailover;
        result.AllowRetransmission = source.AllowRetransmission;
        result.ReportLateAnswers = source.ReportLateAnswers;
        result.CorrelationText = source.CorrelationText;
        return result;
    }

    internal DiameterIdentity ClonePendingIdentity(DiameterIdentity source)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = source.Value;
        return result;
    }

    internal DiameterMessage ClonePendingMessage(DiameterMessage source)
    {
        DiameterMessage result = new DiameterMessage();
        result.Version = source.Version;
        result.Flags = source.Flags;
        result.CommandCode = source.CommandCode;
        result.ApplicationId = source.ApplicationId;
        result.HopByHopIdentifier = source.HopByHopIdentifier;
        result.EndToEndIdentifier = source.EndToEndIdentifier;
        int index = 0;
        while (index < source.Avps.Count)
        {
            DiameterAvp sourceAvp = source.Avps[index];
            DiameterAvp avp = new DiameterAvp();
            avp.Code = sourceAvp.Code;
            avp.Flags = sourceAvp.Flags;
            avp.VendorId = sourceAvp.VendorId;
            avp.Data = new byte[sourceAvp.Data.Length];
            sourceAvp.Data.CopyTo(avp.Data, 0);
            result.Avps.Add(avp);
            index = index + 1;
        }

        return result;
    }
}
