namespace sip;

internal class SipNonceReplayTracker
{
    ISipMonotonicClock clock;
    System.Collections.Generic.List<SipNonceReplayEntry> entries;
    object gate;
    int maximumEntryCount;
    long retentionMilliseconds;
    long lastClockMilliseconds = -1;

    internal SipNonceReplayTracker(ISipMonotonicClock clock,
        SipNonceOptions? options)
    {
        if (clock == null)
        {
            throw new System.ArgumentNullException("clock");
        }
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }
        if (options.MaxReplayEntries <= 0)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }
        if (options.LifetimeSeconds <= 0)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }
        if (options.ClockSkewSeconds < 0)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }

        this.clock = clock;
        entries = new System.Collections.Generic.List<SipNonceReplayEntry>();
        gate = new object();
        maximumEntryCount = options.MaxReplayEntries;
        long retentionSeconds = (long)options.LifetimeSeconds
            + (long)options.ClockSkewSeconds * 2;
        retentionMilliseconds = retentionSeconds * 1000;
    }

    internal SipAuthenticationStatus CheckAndRecord(string? realm,
        string? identity, string? nonce, string? clientNonce, long nonceCount)
    {
        ValidateText(realm, "realm");
        ValidateText(identity, "identity");
        ValidateText(nonce, "nonce");
        ValidateText(clientNonce, "clientNonce");
        if (nonceCount < 1 || nonceCount > 4294967295L)
        {
            throw new System.ArgumentOutOfRangeException("nonceCount");
        }

        lock (gate)
        {
            long now = ReadClock();
            RemoveExpired(now);
            int existingIndex = FindEntry(realm, identity, nonce, clientNonce);
            if (existingIndex >= 0)
            {
                SipNonceReplayEntry existing = entries[existingIndex];
                if (nonceCount <= existing.HighestNonceCount)
                {
                    return SipAuthenticationStatus.ReplayDetected;
                }

                existing.HighestNonceCount = nonceCount;
                return SipAuthenticationStatus.Success;
            }

            if (entries.Count >= maximumEntryCount)
            {
                return SipAuthenticationStatus.Failed;
            }

            SipNonceReplayEntry added = new SipNonceReplayEntry();
            added.Realm = realm == null ? "" : realm;
            added.Identity = identity == null ? "" : identity;
            added.Nonce = nonce == null ? "" : nonce;
            added.ClientNonce = clientNonce == null ? "" : clientNonce;
            added.HighestNonceCount = nonceCount;
            added.ExpiresAtMilliseconds = CreateExpiration(now);
            entries.Add(added);
            return SipAuthenticationStatus.Success;
        }
    }

    internal int GetEntryCount()
    {
        lock (gate)
        {
            RemoveExpired(ReadClock());
            return entries.Count;
        }
    }

    int FindEntry(string? realm, string? identity, string? nonce,
        string? clientNonce)
    {
        int index = 0;
        while (index < entries.Count)
        {
            SipNonceReplayEntry entry = entries[index];
            if (System.String.Equals(entry.Realm, realm,
                    System.StringComparison.Ordinal)
                && System.String.Equals(entry.Identity, identity,
                    System.StringComparison.Ordinal)
                && System.String.Equals(entry.Nonce, nonce,
                    System.StringComparison.Ordinal)
                && System.String.Equals(entry.ClientNonce, clientNonce,
                    System.StringComparison.Ordinal))
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    void RemoveExpired(long now)
    {
        int index = entries.Count - 1;
        while (index >= 0)
        {
            SipNonceReplayEntry entry = entries[index];
            if (now > entry.ExpiresAtMilliseconds)
            {
                entry.Realm = "";
                entry.Identity = "";
                entry.Nonce = "";
                entry.ClientNonce = "";
                entry.HighestNonceCount = 0;
                entry.ExpiresAtMilliseconds = 0;
                entries.RemoveAt(index);
            }
            index = index - 1;
        }
    }

    long ReadClock()
    {
        long current = clock.GetMilliseconds();
        if (current < 0)
        {
            throw new System.InvalidOperationException(
                "The replay clock returned a negative value.");
        }
        if (lastClockMilliseconds >= 0 && current < lastClockMilliseconds)
        {
            return lastClockMilliseconds;
        }

        lastClockMilliseconds = current;
        return current;
    }

    long CreateExpiration(long now)
    {
        return retentionMilliseconds > long.MaxValue - now
            ? long.MaxValue : now + retentionMilliseconds;
    }

    static void ValidateText(string? value, string parameterName)
    {
        if (value == null || value.Length == 0)
        {
            throw new System.ArgumentException(
                "Replay tracking values cannot be null or empty.", parameterName);
        }
    }
}
