using System;
using System.Collections.Generic;
using System.Globalization;

namespace diameter;

internal class DuplicateRequestStoreEntry
{
    internal string Key = string.Empty;
    internal DiameterIdentity OriginHost = new DiameterIdentity();
    internal uint EndToEndIdentifier;
    internal uint CommandCode;
    internal uint ApplicationId;
    internal DiameterMessage OriginalRequest = new DiameterMessage();
    internal DiameterMessage? CachedAnswer;
    internal bool ReplaySafe;
    internal int ObservationCount;
    internal DateTimeOffset FirstSeenAt;
    internal DateTimeOffset LastSeenAt;
    internal DateTimeOffset ExpiresAt;
    internal long ExpiresMonotonicMilliseconds;
}

internal class DuplicateRequestStore : IDuplicateRequestStore
{
    internal Dictionary<string, DuplicateRequestStoreEntry> Entries =
        new Dictionary<string, DuplicateRequestStoreEntry>();
    internal List<string> EntryOrder = new List<string>();
    internal object StoreLock = new object();
    internal IDiameterClock Clock;
    internal int MaximumEntries;
    internal int LifetimeMilliseconds;

    internal DuplicateRequestStore(
        IDiameterClock clock,
        int maximumEntries,
        int lifetimeMilliseconds)
    {
        if (clock == null)
        {
            throw new ArgumentException(
                "A duplicate-request clock is required.");
        }

        if (maximumEntries <= 0)
        {
            throw new ArgumentOutOfRangeException("maximumEntries");
        }

        if (lifetimeMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException("lifetimeMilliseconds");
        }

        Clock = clock;
        MaximumEntries = maximumEntries;
        LifetimeMilliseconds = lifetimeMilliseconds;
    }

    /// <summary>Observes an inbound request for duplicate suppression.</summary>
    /// <param name="request">The inbound request.</param>
    /// <param name="originHost">The request Origin-Host.</param>
    /// <param name="receivedAt">The request receive time.</param>
    /// <returns>The owned duplicate observation.</returns>
    public DiameterDuplicateResult ObserveDiameterDuplicateRequest(
        DiameterMessage? request,
        DiameterIdentity? originHost,
        DateTimeOffset receivedAt)
    {
        if (request == null)
        {
            throw new ArgumentException("A Diameter request is required.");
        }

        DiameterMessage input = request;
        ValidateDuplicateRequestMessage(input);
        DiameterValueResult normalizedOrigin =
            ValueCodecs.NormalizeDiameterIdentity(originHost);
        if (normalizedOrigin.Status != DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "A valid Origin-Host identity is required.");
        }

        ValidateDuplicateRequestOrigin(input, normalizedOrigin.Text);
        string key = CreateDuplicateRequestKey(
            normalizedOrigin.Text,
            input.EndToEndIdentifier);
        lock (StoreLock)
        {
            long monotonicNow = Clock.ReadMonotonicMilliseconds();
            PurgeExpiredDuplicateRequests(monotonicNow);
            if (!Entries.ContainsKey(key))
            {
                EvictDuplicateRequestForCapacity();
                DuplicateRequestStoreEntry entry =
                    new DuplicateRequestStoreEntry();
                entry.Key = key;
                entry.OriginHost.Value = normalizedOrigin.Text;
                entry.EndToEndIdentifier = input.EndToEndIdentifier;
                entry.CommandCode = input.CommandCode;
                entry.ApplicationId = input.ApplicationId;
                entry.OriginalRequest = CloneDuplicateMessage(input);
                entry.ObservationCount = 1;
                entry.FirstSeenAt = receivedAt;
                entry.LastSeenAt = receivedAt;
                SetDuplicateRequestExpiry(
                    entry,
                    receivedAt,
                    monotonicNow);
                Entries[key] = entry;
                EntryOrder.Add(key);
                return CreateDuplicateRequestResult(
                    entry,
                    input,
                    false,
                    false,
                    "duplicate-request-first-observation");
            }

            DuplicateRequestStoreEntry existing = Entries[key];
            if (existing.ObservationCount < int.MaxValue)
            {
                existing.ObservationCount =
                    existing.ObservationCount + 1;
            }

            if (receivedAt > existing.LastSeenAt)
            {
                existing.LastSeenAt = receivedAt;
            }

            SetDuplicateRequestExpiry(
                existing,
                existing.LastSeenAt,
                monotonicNow);
            TouchDuplicateRequestEntry(key);
            bool equivalent = DuplicateRequestsEquivalent(
                existing.OriginalRequest,
                input);
            if (!equivalent)
            {
                return CreateDuplicateRequestResult(
                    existing,
                    input,
                    true,
                    false,
                    "duplicate-request-key-collision");
            }

            bool replay = existing.ReplaySafe &&
                existing.CachedAnswer != null;
            if (replay)
            {
                return CreateDuplicateRequestResult(
                    existing,
                    input,
                    true,
                    true,
                    "duplicate-answer-replay-available");
            }

            return CreateDuplicateRequestResult(
                existing,
                input,
                true,
                false,
                "duplicate-request-detected");
        }
    }

    /// <summary>Caches an answer according to its explicit replay policy.</summary>
    /// <param name="result">The observation and answer to cache.</param>
    public void CacheDiameterDuplicateAnswer(DiameterDuplicateResult? result)
    {
        if (result == null || result.Request == null)
        {
            throw new ArgumentException(
                "Observed duplicate metadata and its request are required.");
        }

        DiameterValueResult normalizedOrigin =
            ValueCodecs.NormalizeDiameterIdentity(result.OriginHost);
        if (normalizedOrigin.Status != DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "The observed Origin-Host identity is invalid.");
        }

        DiameterMessage request = result.Request;
        ValidateDuplicateRequestMessage(request);
        ValidateDuplicateRequestOrigin(request, normalizedOrigin.Text);
        string key = CreateDuplicateRequestKey(
            normalizedOrigin.Text,
            request.EndToEndIdentifier);
        lock (StoreLock)
        {
            PurgeExpiredDuplicateRequests(
                Clock.ReadMonotonicMilliseconds());
            if (!Entries.ContainsKey(key))
            {
                throw new ArgumentException(
                    "The duplicate observation is no longer stored.");
            }

            DuplicateRequestStoreEntry entry = Entries[key];
            if (result.EndToEndIdentifier != request.EndToEndIdentifier ||
                result.CommandCode != request.CommandCode ||
                result.ApplicationId != request.ApplicationId ||
                !DuplicateRequestsEquivalent(
                    entry.OriginalRequest,
                    request))
            {
                throw new ArgumentException(
                    "The duplicate observation does not match the stored request.");
            }

            if (!result.ReplaySafe)
            {
                result.CachedAnswerAvailable = false;
                return;
            }

            if (result.CachedAnswer == null)
            {
                throw new ArgumentException(
                    "A replay-safe observation requires a cached answer.");
            }

            string answerFailure = ValidateDuplicateCachedAnswer(
                request,
                result.CachedAnswer);
            if (answerFailure.Length > 0)
            {
                throw new ArgumentException(answerFailure);
            }

            entry.CachedAnswer = CloneDuplicateMessage(
                result.CachedAnswer);
            entry.ReplaySafe = true;
            result.CachedAnswerAvailable = true;
        }
    }

    internal void ValidateDuplicateRequestMessage(DiameterMessage request)
    {
        if (request.Avps == null)
        {
            throw new ArgumentException(
                "The request AVP collection is required.");
        }

        int flagValue = (int)request.Flags;
        if (request.Version != DiameterProtocolConstants.Version ||
            request.CommandCode == 0U ||
            request.CommandCode > DiameterProtocolConstants.MaximumCommandCode ||
            flagValue < 0 || flagValue > 255 ||
            (flagValue & DiameterProtocolConstants.MessageReservedFlagMask) != 0 ||
            (request.Flags & DiameterMessageFlags.Request) ==
                DiameterMessageFlags.None ||
            (request.Flags & DiameterMessageFlags.Error) !=
                DiameterMessageFlags.None)
        {
            throw new ArgumentException(
                "A valid inbound Diameter request is required.");
        }

        int index = 0;
        while (index < request.Avps.Count)
        {
            DiameterAvp avp = request.Avps[index];
            if (avp == null || avp.Data == null)
            {
                throw new ArgumentException(
                    "The request must contain owned AVPs and payloads.");
            }

            index = index + 1;
        }
    }

    internal void ValidateDuplicateRequestOrigin(
        DiameterMessage request,
        string normalizedOrigin)
    {
        int count = 0;
        string messageOrigin = string.Empty;
        int index = 0;
        while (index < request.Avps.Count)
        {
            DiameterAvp avp = request.Avps[index];
            if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.OriginHost)
            {
                DiameterValueResult decoded =
                    ValueCodecs.DecodeDiameterIdentity(avp.Data);
                if (decoded.Status != DiameterCodecStatus.Success)
                {
                    throw new ArgumentException(
                        "The request Origin-Host AVP is invalid.");
                }

                count = count + 1;
                messageOrigin = decoded.Text;
            }

            index = index + 1;
        }

        if (count != 1 || messageOrigin != normalizedOrigin)
        {
            throw new ArgumentException(
                "The request must contain the supplied Origin-Host exactly once.");
        }
    }

    internal string ValidateDuplicateCachedAnswer(
        DiameterMessage request,
        DiameterMessage answer)
    {
        if (answer.Avps == null)
        {
            return "The cached answer AVP collection is required.";
        }

        int index = 0;
        while (index < answer.Avps.Count)
        {
            DiameterAvp avp = answer.Avps[index];
            if (avp == null || avp.Data == null)
            {
                return "The cached answer must contain owned AVPs and payloads.";
            }

            index = index + 1;
        }

        int flagValue = (int)answer.Flags;
        DiameterMessageFlags requestProxyable =
            request.Flags & DiameterMessageFlags.Proxyable;
        DiameterMessageFlags answerProxyable =
            answer.Flags & DiameterMessageFlags.Proxyable;
        if (answer.Version != request.Version ||
            answer.CommandCode != request.CommandCode ||
            answer.ApplicationId != request.ApplicationId ||
            answer.HopByHopIdentifier != request.HopByHopIdentifier ||
            answer.EndToEndIdentifier != request.EndToEndIdentifier ||
            requestProxyable != answerProxyable ||
            flagValue < 0 || flagValue > 255 ||
            (flagValue & DiameterProtocolConstants.MessageReservedFlagMask) != 0 ||
            (answer.Flags & DiameterMessageFlags.Request) !=
                DiameterMessageFlags.None ||
            (answer.Flags & DiameterMessageFlags.PotentiallyRetransmitted) !=
                DiameterMessageFlags.None)
        {
            return "The cached answer does not correlate with the observed request.";
        }

        return string.Empty;
    }

    internal bool DuplicateRequestsEquivalent(
        DiameterMessage left,
        DiameterMessage right)
    {
        int leftFlags = (int)left.Flags &
            ~(int)DiameterMessageFlags.PotentiallyRetransmitted;
        int rightFlags = (int)right.Flags &
            ~(int)DiameterMessageFlags.PotentiallyRetransmitted;
        if (left.Version != right.Version ||
            leftFlags != rightFlags ||
            left.CommandCode != right.CommandCode ||
            left.ApplicationId != right.ApplicationId ||
            left.EndToEndIdentifier != right.EndToEndIdentifier ||
            left.Avps.Count != right.Avps.Count)
        {
            return false;
        }

        int index = 0;
        while (index < left.Avps.Count)
        {
            DiameterAvp leftAvp = left.Avps[index];
            DiameterAvp rightAvp = right.Avps[index];
            if (leftAvp.Code != rightAvp.Code ||
                leftAvp.Flags != rightAvp.Flags ||
                leftAvp.VendorId != rightAvp.VendorId)
            {
                return false;
            }

            if (leftAvp.VendorId == DiameterVendorIds.Ietf &&
                leftAvp.Code == DiameterAvpCodes.OriginHost)
            {
                DiameterValueResult leftOrigin =
                    ValueCodecs.DecodeDiameterIdentity(leftAvp.Data);
                DiameterValueResult rightOrigin =
                    ValueCodecs.DecodeDiameterIdentity(rightAvp.Data);
                if (leftOrigin.Status != DiameterCodecStatus.Success ||
                    rightOrigin.Status != DiameterCodecStatus.Success ||
                    leftOrigin.Text != rightOrigin.Text)
                {
                    return false;
                }
            }
            else
            {
                if (leftAvp.Data.Length != rightAvp.Data.Length)
                {
                    return false;
                }

                int dataIndex = 0;
                while (dataIndex < leftAvp.Data.Length)
                {
                    if (leftAvp.Data[dataIndex] != rightAvp.Data[dataIndex])
                    {
                        return false;
                    }

                    dataIndex = dataIndex + 1;
                }
            }

            index = index + 1;
        }

        return true;
    }

    internal DiameterDuplicateResult CreateDuplicateRequestResult(
        DuplicateRequestStoreEntry entry,
        DiameterMessage request,
        bool duplicate,
        bool replay,
        string code)
    {
        DiameterDuplicateResult result = new DiameterDuplicateResult();
        result.OriginHost.Value = entry.OriginHost.Value;
        result.EndToEndIdentifier = entry.EndToEndIdentifier;
        result.CommandCode = request.CommandCode;
        result.ApplicationId = request.ApplicationId;
        result.Request = CloneDuplicateMessage(request);
        result.Duplicate = duplicate;
        result.Retransmission =
            (request.Flags & DiameterMessageFlags.PotentiallyRetransmitted) !=
                DiameterMessageFlags.None;
        result.ObservationCount = entry.ObservationCount;
        result.FirstSeenAt = entry.FirstSeenAt;
        result.LastSeenAt = entry.LastSeenAt;
        result.ExpiresAt = entry.ExpiresAt;
        result.Code = code;
        if (code == "duplicate-request-first-observation")
        {
            result.Message = "The request key was observed for the first time.";
        }
        else if (code == "duplicate-request-key-collision")
        {
            result.Message =
                "The duplicate key was reused by a different request.";
        }
        else if (replay && entry.CachedAnswer != null)
        {
            result.CachedAnswer = CloneDuplicateMessage(entry.CachedAnswer);
            result.CachedAnswer.HopByHopIdentifier =
                request.HopByHopIdentifier;
            result.CachedAnswer.EndToEndIdentifier =
                request.EndToEndIdentifier;
            result.CachedAnswerAvailable = true;
            result.ReplaySafe = true;
            result.Message = "A replay-safe cached answer is available.";
        }
        else
        {
            result.Message = "The request is a duplicate without a replayable answer.";
        }

        return result;
    }

    internal void SetDuplicateRequestExpiry(
        DuplicateRequestStoreEntry entry,
        DateTimeOffset referenceTime,
        long monotonicNow)
    {
        entry.ExpiresAt = AddDuplicateRequestUtcDuration(
            referenceTime,
            LifetimeMilliseconds);
        entry.ExpiresMonotonicMilliseconds =
            AddDuplicateRequestMonotonicDuration(
                monotonicNow,
                LifetimeMilliseconds);
    }

    internal DateTimeOffset AddDuplicateRequestUtcDuration(
        DateTimeOffset value,
        int durationMilliseconds)
    {
        long durationTicks =
            (long)durationMilliseconds * TimeSpan.TicksPerMillisecond;
        if (value.UtcTicks > DateTimeOffset.MaxValue.UtcTicks - durationTicks)
        {
            return DateTimeOffset.MaxValue;
        }

        return value.AddTicks(durationTicks);
    }

    internal long AddDuplicateRequestMonotonicDuration(
        long value,
        int durationMilliseconds)
    {
        if (value > long.MaxValue - durationMilliseconds)
        {
            return long.MaxValue;
        }

        return value + durationMilliseconds;
    }

    internal void EvictDuplicateRequestForCapacity()
    {
        while (Entries.Count >= MaximumEntries && EntryOrder.Count > 0)
        {
            string oldest = EntryOrder[0];
            EntryOrder.RemoveAt(0);
            Entries.Remove(oldest);
        }
    }

    internal void TouchDuplicateRequestEntry(string key)
    {
        EntryOrder.Remove(key);
        EntryOrder.Add(key);
    }

    internal void PurgeExpiredDuplicateRequests(long monotonicNow)
    {
        int index = 0;
        while (index < EntryOrder.Count)
        {
            string key = EntryOrder[index];
            if (!Entries.ContainsKey(key) ||
                Entries[key].ExpiresMonotonicMilliseconds <= monotonicNow)
            {
                Entries.Remove(key);
                EntryOrder.RemoveAt(index);
            }
            else
            {
                index = index + 1;
            }
        }
    }

    internal int ReadDuplicateRequestCount()
    {
        lock (StoreLock)
        {
            PurgeExpiredDuplicateRequests(
                Clock.ReadMonotonicMilliseconds());
            return Entries.Count;
        }
    }

    internal string CreateDuplicateRequestKey(
        string normalizedOrigin,
        uint endToEndIdentifier)
    {
        return normalizedOrigin + "|" +
            endToEndIdentifier.ToString(CultureInfo.InvariantCulture);
    }

    internal DiameterMessage CloneDuplicateMessage(DiameterMessage source)
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
