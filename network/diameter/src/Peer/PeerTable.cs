using System;
using System.Collections.Generic;

namespace diameter;

internal class PeerTable
{
    internal Dictionary<string, PeerTableEntry> Entries =
        new Dictionary<string, PeerTableEntry>();
    internal List<string> EntryOrder = new List<string>();
    internal object TableLock = new object();
    internal IDiameterClock Clock;
    internal DiameterTimerConfiguration Timers;
    internal IPeerRetryJitterSource RetryJitterSource =
        new SecurePeerRetryJitterSource();
    internal IPeerWatchdogJitterSource WatchdogJitterSource =
        new SecurePeerWatchdogJitterSource();
    internal int MaximumPeerCount;

    internal PeerTable(
        IDiameterClock clock,
        DiameterTimerConfiguration timers,
        int maximumPeerCount)
    {
        if (clock == null || timers == null)
        {
            throw new ArgumentException("A peer table requires a clock and timer settings.");
        }

        timers.ValidateConfiguration();
        if (maximumPeerCount <= 0)
        {
            throw new ArgumentOutOfRangeException("maximumPeerCount");
        }

        Clock = clock;
        Timers = ClonePeerTimerConfiguration(timers);
        MaximumPeerCount = maximumPeerCount;
    }

    internal DiameterPeerSnapshot AddOrUpdatePeerConfiguration(
        DiameterPeerConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentException("A peer configuration is required.");
        }

        configuration.ValidatePeerConfiguration();
        DateTimeOffset now = Clock.ReadUtcTime();
        if (configuration.Source == DiameterPeerSource.Dynamic &&
            configuration.ExpiresAt <= now)
        {
            throw new ArgumentException("A dynamic peer configuration must not be expired.");
        }

        DiameterPeerConfiguration owned =
            PeerTableCloner.ClonePeerTableConfiguration(configuration);
        string key = ReadPeerTableKey(configuration.Identity.Value);
        lock (TableLock)
        {
            if (Entries.ContainsKey(key))
            {
                PeerTableEntry existing = Entries[key];
                if (existing.Snapshot.Configuration.Source == DiameterPeerSource.Static &&
                    owned.Source == DiameterPeerSource.Dynamic)
                {
                    return PeerTableCloner.ClonePeerTableSnapshot(existing.Snapshot);
                }

                if (!ValueCodecs.AreDiameterRealmsEqual(
                    existing.Snapshot.Configuration.Realm,
                    owned.Realm))
                {
                    throw new ArgumentException(
                        "A peer identity cannot move to a different realm.");
                }

                existing.Snapshot.Configuration = owned;
                if (existing.Snapshot.CurrentEndpoint == null ||
                    !ContainsPeerTableEndpoint(
                        owned.Endpoints,
                        existing.Snapshot.CurrentEndpoint))
                {
                    existing.Snapshot.CurrentEndpoint =
                        PeerTableCloner.ClonePeerTableEndpoint(owned.Endpoints[0]);
                }

                if (!owned.Enabled)
                {
                    existing.Snapshot.Available = false;
                    existing.Snapshot.NextConnectionAttemptAt =
                        default(DateTimeOffset);
                }
                else if (owned.ConnectAutomatically &&
                    existing.Snapshot.State == DiameterPeerState.Closed &&
                    existing.Snapshot.NextConnectionAttemptAt ==
                        default(DateTimeOffset))
                {
                    existing.Snapshot.NextConnectionAttemptAt = now;
                }

                return PeerTableCloner.ClonePeerTableSnapshot(existing.Snapshot);
            }

            if (Entries.Count >= MaximumPeerCount)
            {
                throw new DiameterPeerException("The peer table capacity has been reached.");
            }

            PeerTableEntry entry = new PeerTableEntry();
            entry.Snapshot.Configuration = owned;
            entry.Snapshot.CurrentEndpoint =
                PeerTableCloner.ClonePeerTableEndpoint(owned.Endpoints[0]);
            entry.Snapshot.State = DiameterPeerState.Closed;
            entry.Snapshot.StateChangedAt = now;
            if (owned.Enabled && owned.ConnectAutomatically)
            {
                entry.Snapshot.NextConnectionAttemptAt = now;
            }

            Entries[key] = entry;
            EntryOrder.Add(key);
            return PeerTableCloner.ClonePeerTableSnapshot(entry.Snapshot);
        }
    }

    internal bool RemovePeerTableEntry(string identity)
    {
        string key = ReadPeerTableKey(identity);
        lock (TableLock)
        {
            if (!Entries.ContainsKey(key))
            {
                return false;
            }

            Entries.Remove(key);
            EntryOrder.Remove(key);
            return true;
        }
    }

    internal DiameterPeerSnapshot? ReadPeerTableSnapshot(string identity)
    {
        string key = ReadPeerTableKey(identity);
        lock (TableLock)
        {
            if (!Entries.ContainsKey(key))
            {
                return null;
            }

            return PeerTableCloner.ClonePeerTableSnapshot(Entries[key].Snapshot);
        }
    }

    internal List<DiameterPeerSnapshot> ReadAllPeerTableSnapshots()
    {
        List<DiameterPeerSnapshot> result = new List<DiameterPeerSnapshot>();
        lock (TableLock)
        {
            int index = 0;
            while (index < EntryOrder.Count)
            {
                result.Add(PeerTableCloner.ClonePeerTableSnapshot(
                    Entries[EntryOrder[index]].Snapshot));
                index = index + 1;
            }
        }

        return result;
    }

    internal List<DiameterPeerSnapshot> ExpireDynamicPeerEntries()
    {
        DateTimeOffset now = Clock.ReadUtcTime();
        List<DiameterPeerSnapshot> result = new List<DiameterPeerSnapshot>();
        lock (TableLock)
        {
            List<string> removals = new List<string>();
            int index = 0;
            while (index < EntryOrder.Count)
            {
                string key = EntryOrder[index];
                DiameterPeerSnapshot snapshot = Entries[key].Snapshot;
                if (snapshot.Configuration.Source == DiameterPeerSource.Dynamic &&
                    snapshot.Configuration.ExpiresAt <= now &&
                    snapshot.State == DiameterPeerState.Closed &&
                    snapshot.Connection == null)
                {
                    result.Add(PeerTableCloner.ClonePeerTableSnapshot(snapshot));
                    removals.Add(key);
                }

                index = index + 1;
            }

            index = 0;
            while (index < removals.Count)
            {
                Entries.Remove(removals[index]);
                EntryOrder.Remove(removals[index]);
                index = index + 1;
            }
        }

        return result;
    }

    internal DiameterPeerSnapshot SelectPeerTableEndpoint(
        string identity,
        DiameterEndpoint endpoint)
    {
        if (endpoint == null)
        {
            throw new ArgumentException("A peer endpoint is required.");
        }

        string key = ReadPeerTableKey(identity);
        lock (TableLock)
        {
            DiameterPeerSnapshot snapshot = ReadRequiredPeerTableEntry(key).Snapshot;
            int index = FindPeerTableEndpointIndex(
                snapshot.Configuration.Endpoints,
                endpoint);
            if (index < 0)
            {
                throw new ArgumentException("The endpoint is not configured for the peer.");
            }

            snapshot.CurrentEndpoint = PeerTableCloner.ClonePeerTableEndpoint(
                snapshot.Configuration.Endpoints[index]);
            return PeerTableCloner.ClonePeerTableSnapshot(snapshot);
        }
    }

    internal DiameterPeerSnapshot AdvancePeerTableEndpoint(string identity)
    {
        string key = ReadPeerTableKey(identity);
        lock (TableLock)
        {
            DiameterPeerSnapshot snapshot = ReadRequiredPeerTableEntry(key).Snapshot;
            int index = -1;
            if (snapshot.CurrentEndpoint != null)
            {
                index = FindPeerTableEndpointIndex(
                    snapshot.Configuration.Endpoints,
                    snapshot.CurrentEndpoint);
            }

            index = index + 1;
            if (index < 0 || index >= snapshot.Configuration.Endpoints.Count)
            {
                index = 0;
            }

            snapshot.CurrentEndpoint = PeerTableCloner.ClonePeerTableEndpoint(
                snapshot.Configuration.Endpoints[index]);
            return PeerTableCloner.ClonePeerTableSnapshot(snapshot);
        }
    }

    internal DiameterPeerSnapshot AttachPeerTableConnection(
        string identity,
        DiameterEndpoint endpoint,
        DiameterConnectionMetadata connection,
        DiameterConnectionDirection direction)
    {
        if (endpoint == null || connection == null ||
            connection.LocalEndpoint == null || connection.RemoteEndpoint == null ||
            connection.Transport == null || connection.Security == null ||
            !connection.Open)
        {
            throw new ArgumentException("An open peer connection and endpoint are required.");
        }

        ValidatePeerTableDirection(direction);
        string key = ReadPeerTableKey(identity);
        lock (TableLock)
        {
            DiameterPeerSnapshot snapshot = ReadRequiredPeerTableEntry(key).Snapshot;
            int endpointIndex = FindPeerTableEndpointIndex(
                snapshot.Configuration.Endpoints,
                endpoint);
            if (endpointIndex < 0)
            {
                throw new ArgumentException("The connected endpoint is not configured.");
            }

            snapshot.CurrentEndpoint = PeerTableCloner.ClonePeerTableEndpoint(
                snapshot.Configuration.Endpoints[endpointIndex]);
            snapshot.Connection = QueuedDiameterConnection.CloneConnectionMetadata(
                connection);
            snapshot.Connection.Direction = direction;
            snapshot.Direction = direction;
            snapshot.ConnectedAt = connection.EstablishedAt;
            if (snapshot.ConnectedAt == default(DateTimeOffset))
            {
                snapshot.ConnectedAt = Clock.ReadUtcTime();
            }

            snapshot.LastActivityAt = snapshot.ConnectedAt;
            snapshot.NextConnectionAttemptAt = default(DateTimeOffset);
            return PeerTableCloner.ClonePeerTableSnapshot(snapshot);
        }
    }

    internal DiameterPeerSnapshot DetachPeerTableConnection(string identity)
    {
        string key = ReadPeerTableKey(identity);
        lock (TableLock)
        {
            DiameterPeerSnapshot snapshot = ReadRequiredPeerTableEntry(key).Snapshot;
            snapshot.Connection = null;
            snapshot.ConnectedAt = default(DateTimeOffset);
            snapshot.WatchdogPending = false;
            snapshot.Available = false;
            return PeerTableCloner.ClonePeerTableSnapshot(snapshot);
        }
    }

    internal DiameterPeerSnapshot UpdatePeerTableCapabilities(
        string identity,
        DiameterPeerCapabilities capabilities)
    {
        string key = ReadPeerTableKey(identity);
        lock (TableLock)
        {
            DiameterPeerSnapshot snapshot = ReadRequiredPeerTableEntry(key).Snapshot;
            ValidatePeerTableCapabilities(snapshot.Configuration, capabilities);
            DiameterPeerCapabilities owned =
                PeerTableCloner.ClonePeerTableCapabilities(capabilities);
            if (owned.UpdatedAt == default(DateTimeOffset))
            {
                owned.UpdatedAt = Clock.ReadUtcTime();
            }

            snapshot.Capabilities = owned;
            return PeerTableCloner.ClonePeerTableSnapshot(snapshot);
        }
    }

    internal DiameterPeerSnapshot TransitionPeerTableState(
        string identity,
        DiameterPeerState state)
    {
        ValidatePeerTableState(state);
        string key = ReadPeerTableKey(identity);
        lock (TableLock)
        {
            DiameterPeerSnapshot snapshot = ReadRequiredPeerTableEntry(key).Snapshot;
            bool openState = state == DiameterPeerState.InitiatorOpen ||
                state == DiameterPeerState.ResponderOpen;
            if (openState && (snapshot.Connection == null || !snapshot.Connection.Open))
            {
                throw new DiameterPeerException(
                    "An open peer state requires an attached open connection.");
            }

            snapshot.State = state;
            snapshot.StateChangedAt = Clock.ReadUtcTime();
            if (state == DiameterPeerState.InitiatorOpen)
            {
                snapshot.Direction = DiameterConnectionDirection.Outbound;
                if (snapshot.Connection != null)
                {
                    snapshot.Connection.Direction = snapshot.Direction;
                }
            }
            else if (state == DiameterPeerState.ResponderOpen)
            {
                snapshot.Direction = DiameterConnectionDirection.Inbound;
                if (snapshot.Connection != null)
                {
                    snapshot.Connection.Direction = snapshot.Direction;
                }
            }

            if (openState)
            {
                if (snapshot.ConnectedAt == default(DateTimeOffset))
                {
                    snapshot.ConnectedAt = snapshot.StateChangedAt;
                }

                snapshot.Available = snapshot.Configuration.Enabled &&
                    snapshot.WatchdogState != DiameterPeerWatchdogState.Suspect &&
                    snapshot.WatchdogState != DiameterPeerWatchdogState.Down &&
                    snapshot.WatchdogState != DiameterPeerWatchdogState.Reopen;
                snapshot.NextConnectionAttemptAt = default(DateTimeOffset);
            }
            else
            {
                snapshot.Available = false;
            }

            if (state == DiameterPeerState.Closed)
            {
                snapshot.Connection = null;
                snapshot.ConnectedAt = default(DateTimeOffset);
                snapshot.WatchdogPending = false;
            }

            return PeerTableCloner.ClonePeerTableSnapshot(snapshot);
        }
    }

    internal DiameterPeerSnapshot SchedulePeerTableConnectionRetry(string identity)
    {
        string key = ReadPeerTableKey(identity);
        lock (TableLock)
        {
            DiameterPeerSnapshot snapshot = ReadRequiredPeerTableEntry(key).Snapshot;
            DateTimeOffset now = Clock.ReadUtcTime();
            if (!IsPeerTableRetryEligible(snapshot, now))
            {
                snapshot.NextConnectionAttemptAt = default(DateTimeOffset);
                return PeerTableCloner.ClonePeerTableSnapshot(snapshot);
            }

            snapshot.NextConnectionAttemptAt = now.AddMilliseconds(
                ReadPeerTableRetryDelayMilliseconds());
            return PeerTableCloner.ClonePeerTableSnapshot(snapshot);
        }
    }

    internal List<DiameterPeerSnapshot> ReadDuePeerConnectionRetries()
    {
        DateTimeOffset now = Clock.ReadUtcTime();
        List<DiameterPeerSnapshot> result = new List<DiameterPeerSnapshot>();
        lock (TableLock)
        {
            int index = 0;
            while (index < EntryOrder.Count)
            {
                DiameterPeerSnapshot snapshot = Entries[EntryOrder[index]].Snapshot;
                if (IsPeerTableRetryEligible(snapshot, now) &&
                    snapshot.NextConnectionAttemptAt != default(DateTimeOffset) &&
                    snapshot.NextConnectionAttemptAt <= now)
                {
                    result.Add(PeerTableCloner.ClonePeerTableSnapshot(snapshot));
                }

                index = index + 1;
            }
        }

        return result;
    }

    internal DiameterPeerSnapshot RecordPeerTableActivity(string identity)
    {
        string key = ReadPeerTableKey(identity);
        lock (TableLock)
        {
            DiameterPeerSnapshot snapshot = ReadRequiredPeerTableEntry(key).Snapshot;
            snapshot.LastActivityAt = Clock.ReadUtcTime();
            snapshot.WatchdogDeadlineAt = snapshot.LastActivityAt.AddMilliseconds(
                ReadPeerWatchdogDelayMilliseconds());
            return PeerTableCloner.ClonePeerTableSnapshot(snapshot);
        }
    }

    internal DiameterPeerSnapshot RecordPeerTableWatchdogSent(string identity)
    {
        string key = ReadPeerTableKey(identity);
        lock (TableLock)
        {
            DiameterPeerSnapshot snapshot = ReadRequiredPeerTableEntry(key).Snapshot;
            snapshot.LastWatchdogAt = Clock.ReadUtcTime();
            snapshot.WatchdogPending = true;
            snapshot.WatchdogDeadlineAt = snapshot.LastWatchdogAt.AddMilliseconds(
                ReadPeerWatchdogDelayMilliseconds());
            return PeerTableCloner.ClonePeerTableSnapshot(snapshot);
        }
    }

    internal DiameterPeerSnapshot RecordPeerTableWatchdogOutcome(
        string identity,
        bool successful)
    {
        string key = ReadPeerTableKey(identity);
        lock (TableLock)
        {
            DiameterPeerSnapshot snapshot = ReadRequiredPeerTableEntry(key).Snapshot;
            snapshot.LastWatchdogAt = Clock.ReadUtcTime();
            snapshot.WatchdogPending = false;
            snapshot.WatchdogDeadlineAt = snapshot.LastWatchdogAt.AddMilliseconds(
                ReadPeerWatchdogDelayMilliseconds());
            if (successful)
            {
                snapshot.ConsecutiveWatchdogSuccesses = IncrementPeerTableCounter(
                    snapshot.ConsecutiveWatchdogSuccesses);
                snapshot.ConsecutiveWatchdogFailures = 0;
            }
            else
            {
                snapshot.ConsecutiveWatchdogFailures = IncrementPeerTableCounter(
                    snapshot.ConsecutiveWatchdogFailures);
                snapshot.ConsecutiveWatchdogSuccesses = 0;
            }

            return PeerTableCloner.ClonePeerTableSnapshot(snapshot);
        }
    }

    internal DiameterPeerSnapshot IncrementPeerTableReconnectCount(string identity)
    {
        string key = ReadPeerTableKey(identity);
        lock (TableLock)
        {
            DiameterPeerSnapshot snapshot = ReadRequiredPeerTableEntry(key).Snapshot;
            if (snapshot.ReconnectCount < long.MaxValue)
            {
                snapshot.ReconnectCount = snapshot.ReconnectCount + 1;
            }

            return PeerTableCloner.ClonePeerTableSnapshot(snapshot);
        }
    }

    internal List<DiameterPeerSnapshot> ReadDuePeerWatchdogs()
    {
        DateTimeOffset now = Clock.ReadUtcTime();
        List<DiameterPeerSnapshot> result = new List<DiameterPeerSnapshot>();
        lock (TableLock)
        {
            int index = 0;
            while (index < EntryOrder.Count)
            {
                DiameterPeerSnapshot snapshot = Entries[EntryOrder[index]].Snapshot;
                DateTimeOffset deadline = snapshot.WatchdogDeadlineAt;
                if (deadline == default(DateTimeOffset))
                {
                    deadline = ReadPeerWatchdogReferenceTime(snapshot).AddMilliseconds(
                        Timers.WatchdogIntervalMilliseconds);
                }

                if ((snapshot.State == DiameterPeerState.InitiatorOpen ||
                    snapshot.State == DiameterPeerState.ResponderOpen) &&
                    snapshot.Connection != null && snapshot.Connection.Open &&
                    deadline <= now)
                {
                    result.Add(PeerTableCloner.ClonePeerTableSnapshot(snapshot));
                }

                index = index + 1;
            }
        }

        return result;
    }

    internal DateTimeOffset ReadPeerWatchdogReferenceTime(
        DiameterPeerSnapshot snapshot)
    {
        DateTimeOffset reference = snapshot.ConnectedAt;
        if (snapshot.StateChangedAt > reference)
        {
            reference = snapshot.StateChangedAt;
        }

        if (snapshot.LastActivityAt > reference)
        {
            reference = snapshot.LastActivityAt;
        }

        if (snapshot.LastWatchdogAt > reference)
        {
            reference = snapshot.LastWatchdogAt;
        }

        return reference;
    }

    internal PeerTableEntry ReadRequiredPeerTableEntry(string key)
    {
        if (!Entries.ContainsKey(key))
        {
            throw new DiameterPeerException("The requested peer is not in the peer table.");
        }

        return Entries[key];
    }

    internal string ReadPeerTableKey(string identity)
    {
        DiameterIdentity value = new DiameterIdentity();
        value.Value = identity;
        DiameterValueResult normalized = ValueCodecs.NormalizeDiameterIdentity(value);
        if (normalized.Status != DiameterCodecStatus.Success)
        {
            throw new ArgumentException("A valid Diameter peer identity is required.");
        }

        return normalized.Text;
    }

    internal bool ContainsPeerTableEndpoint(
        List<DiameterEndpoint> endpoints,
        DiameterEndpoint endpoint)
    {
        return FindPeerTableEndpointIndex(endpoints, endpoint) >= 0;
    }

    internal int FindPeerTableEndpointIndex(
        List<DiameterEndpoint> endpoints,
        DiameterEndpoint endpoint)
    {
        int index = 0;
        while (index < endpoints.Count)
        {
            DiameterEndpoint candidate = endpoints[index];
            bool sameAddress = candidate.Address == null && endpoint.Address == null;
            if (candidate.Address != null && endpoint.Address != null)
            {
                sameAddress = candidate.Address.Equals(endpoint.Address);
            }

            if (sameAddress && string.Equals(
                candidate.Host,
                endpoint.Host,
                StringComparison.OrdinalIgnoreCase) &&
                candidate.Port == endpoint.Port &&
                candidate.Transport == endpoint.Transport &&
                candidate.Security == endpoint.Security)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal void ValidatePeerTableCapabilities(
        DiameterPeerConfiguration configuration,
        DiameterPeerCapabilities capabilities)
    {
        if (capabilities == null || capabilities.OriginHost == null ||
            capabilities.OriginRealm == null || capabilities.HostAddresses == null ||
            capabilities.SupportedVendorIds == null ||
            capabilities.AuthApplicationIds == null ||
            capabilities.AcctApplicationIds == null ||
            capabilities.VendorSpecificApplications == null ||
            capabilities.InbandSecurityIds == null ||
            capabilities.CommonApplications == null)
        {
            throw new ArgumentException("Peer capability values must not be null.");
        }

        if ((!string.IsNullOrEmpty(capabilities.OriginHost.Value) &&
            !ValueCodecs.AreDiameterIdentitiesEqual(
                configuration.Identity,
                capabilities.OriginHost)) ||
            (!string.IsNullOrEmpty(capabilities.OriginRealm.Value) &&
            !ValueCodecs.AreDiameterRealmsEqual(
                configuration.Realm,
                capabilities.OriginRealm)))
        {
            throw new ArgumentException(
                "Peer capabilities must identify the configured peer and realm.");
        }

        int addressIndex = 0;
        while (addressIndex < capabilities.HostAddresses.Count)
        {
            if (capabilities.HostAddresses[addressIndex] == null)
            {
                throw new ArgumentException(
                    "Peer capabilities must not contain null addresses.");
            }

            addressIndex = addressIndex + 1;
        }

        ValidatePeerTableApplicationList(capabilities.VendorSpecificApplications);
        ValidatePeerTableApplicationList(capabilities.CommonApplications);
    }

    internal void ValidatePeerTableApplicationList(
        List<DiameterApplicationId> applications)
    {
        int index = 0;
        while (index < applications.Count)
        {
            if (applications[index] == null)
            {
                throw new ArgumentException(
                    "Peer capabilities must not contain null applications.");
            }

            index = index + 1;
        }
    }

    internal void ValidatePeerTableState(DiameterPeerState state)
    {
        if (state != DiameterPeerState.Closed &&
            state != DiameterPeerState.WaitConnectionAcknowledgement &&
            state != DiameterPeerState.WaitInitiatorCapabilitiesAnswer &&
            state != DiameterPeerState.WaitConnectionAcknowledgementElection &&
            state != DiameterPeerState.WaitReturns &&
            state != DiameterPeerState.ResponderOpen &&
            state != DiameterPeerState.InitiatorOpen &&
            state != DiameterPeerState.Closing)
        {
            throw new ArgumentOutOfRangeException("state");
        }
    }

    internal void ValidatePeerTableDirection(DiameterConnectionDirection direction)
    {
        if (direction != DiameterConnectionDirection.Inbound &&
            direction != DiameterConnectionDirection.Outbound)
        {
            throw new ArgumentOutOfRangeException("direction");
        }
    }

    internal int IncrementPeerTableCounter(int value)
    {
        if (value == int.MaxValue)
        {
            return value;
        }

        return value + 1;
    }

    internal DiameterTimerConfiguration ClonePeerTimerConfiguration(
        DiameterTimerConfiguration source)
    {
        DiameterTimerConfiguration result = new DiameterTimerConfiguration();
        result.ConnectionRetryMilliseconds = source.ConnectionRetryMilliseconds;
        result.ConnectionRetryJitterPercent =
            source.ConnectionRetryJitterPercent;
        result.WatchdogIntervalMilliseconds = source.WatchdogIntervalMilliseconds;
        result.WatchdogJitterMilliseconds = source.WatchdogJitterMilliseconds;
        result.RequestTimeoutMilliseconds = source.RequestTimeoutMilliseconds;
        result.ConnectTimeoutMilliseconds = source.ConnectTimeoutMilliseconds;
        result.DrainTimeoutMilliseconds = source.DrainTimeoutMilliseconds;
        result.DuplicateCacheLifetimeMilliseconds =
            source.DuplicateCacheLifetimeMilliseconds;
        result.RedirectCacheMaximumLifetimeMilliseconds =
            source.RedirectCacheMaximumLifetimeMilliseconds;
        result.SessionSweepMilliseconds = source.SessionSweepMilliseconds;
        return result;
    }

    internal bool IsPeerTableRetryEligible(
        DiameterPeerSnapshot snapshot,
        DateTimeOffset now)
    {
        return snapshot.Configuration.Enabled &&
            snapshot.Configuration.ConnectAutomatically &&
            !snapshot.ReconnectSuppressed &&
            snapshot.State == DiameterPeerState.Closed &&
            snapshot.Connection == null &&
            !IsPeerTableDynamicExpired(snapshot, now);
    }

    internal bool IsPeerTableDynamicExpired(
        DiameterPeerSnapshot snapshot,
        DateTimeOffset now)
    {
        return snapshot.Configuration.Source == DiameterPeerSource.Dynamic &&
            snapshot.Configuration.ExpiresAt <= now;
    }

    internal long ReadPeerTableRetryDelayMilliseconds()
    {
        int jitter = Timers.ConnectionRetryJitterPercent;
        if (jitter == 0)
        {
            return Timers.ConnectionRetryMilliseconds;
        }

        int maximumExclusive = jitter * 2 + 1;
        int selection = RetryJitterSource.ReadPeerRetryJitterValue(
            maximumExclusive);
        if (selection < 0 || selection >= maximumExclusive)
        {
            throw new InvalidOperationException(
                "The peer retry jitter source returned an out-of-range value.");
        }

        int percentage = 100 - jitter + selection;
        long delay = (long)Timers.ConnectionRetryMilliseconds * percentage / 100L;
        if (delay < 1)
        {
            return 1;
        }

        return delay;
    }

    internal long ReadPeerWatchdogDelayMilliseconds()
    {
        int jitter = Timers.WatchdogJitterMilliseconds;
        if (jitter == 0)
        {
            return Timers.WatchdogIntervalMilliseconds;
        }

        int maximumExclusive = jitter * 2 + 1;
        int selection = WatchdogJitterSource.ReadPeerWatchdogJitterValue(
            maximumExclusive);
        if (selection < 0 || selection >= maximumExclusive)
        {
            throw new InvalidOperationException(
                "The peer watchdog jitter source returned an out-of-range value.");
        }

        long delay = (long)Timers.WatchdogIntervalMilliseconds - jitter +
            selection;
        if (delay < 1)
        {
            return 1;
        }

        return delay;
    }
}

internal class PeerTableEntry
{
    internal DiameterPeerSnapshot Snapshot = new DiameterPeerSnapshot();
}
