using System.Collections.Generic;

namespace dns;

internal class DnsIterativeReachabilityState
{
    internal DnsName Zone = new DnsName();
    internal DnsEndpoint Endpoint = new DnsEndpoint();
    internal int ConsecutiveFailures;
    internal bool Lame;
    internal bool HasSuppressedUntil;
    internal DnsClockValue SuppressedUntil = new DnsClockValue();
}

internal static class IterativeServerStateOperations
{
    const int SuppressionMilliseconds = 30000;

    internal static bool ShouldUseEdns(
        DnsIterativeResolverState state,
        DnsEndpoint endpoint)
    {
        if (!state.Options.UseEdns)
        {
            return false;
        }

        DnsServerState? known = FindServerState(
            state.ServerStates,
            endpoint);
        return known == null || !known.HasEdnsCapability ||
            known.SupportsEdns;
    }

    internal static int SelectUdpPayloadSize(
        DnsIterativeResolverState state,
        DnsEndpoint endpoint)
    {
        int result = state.Options.EdnsUdpPayloadSize;
        DnsServerState? known = FindServerState(
            state.ServerStates,
            endpoint);
        if (known != null && known.HasEdnsCapability &&
            known.SupportsEdns &&
            known.EdnsUdpPayloadSize >=
                DnsProtocolLimits.ClassicUdpPayloadSize &&
            known.EdnsUdpPayloadSize < result)
        {
            result = known.EdnsUdpPayloadSize;
        }

        return result;
    }

    internal static bool IsPreferred(
        DnsIterativeResolverState state,
        DnsIterativeAuthorityCandidate candidate)
    {
        DnsIterativeReachabilityState? known = FindReachabilityState(
            state.ReachabilityStates,
            candidate.Zone,
            candidate.Endpoint);
        if (known == null)
        {
            return true;
        }

        if (!known.HasSuppressedUntil)
        {
            return !known.Lame;
        }

        DnsClockValue now = TransportTime.ReadClock();
        if (now.MonotonicMilliseconds >=
            known.SuppressedUntil.MonotonicMilliseconds)
        {
            known.HasSuppressedUntil = false;
            known.Lame = false;
            known.ConsecutiveFailures = 0;
            return true;
        }

        return false;
    }

    internal static void ObserveResult(
        DnsIterativeResolverState state,
        DnsIterativeAuthorityCandidate candidate,
        DnsQueryResult result)
    {
        DnsServerState server = EnsureServerState(
            state.ServerStates,
            candidate.Endpoint);
        DnsServerState? observed = FindServerState(
            result.ServerStates,
            candidate.Endpoint);
        if (observed != null)
        {
            MergeServerState(server, observed);
        }
        else
        {
            ObserveBasicResult(server, result);
        }

        if (result.Success)
        {
            MarkReachable(state, candidate);
        }
        else
        {
            MarkFailure(state, candidate);
        }
    }

    internal static void MarkFailure(
        DnsIterativeResolverState state,
        DnsIterativeAuthorityCandidate candidate)
    {
        DnsIterativeReachabilityState known = EnsureReachabilityState(
            state.ReachabilityStates,
            candidate.Zone,
            candidate.Endpoint);
        known.ConsecutiveFailures = known.ConsecutiveFailures + 1;
        Suppress(known);
    }

    internal static void MarkProtocolFailure(
        DnsIterativeResolverState state,
        DnsIterativeAuthorityCandidate candidate)
    {
        DnsServerState server = EnsureServerState(
            state.ServerStates,
            candidate.Endpoint);
        server.ConsecutiveFailures = server.ConsecutiveFailures + 1;
        server.HasSuppressedUntil = true;
        server.SuppressedUntil = CreateSuppressionDeadline();
        MarkFailure(state, candidate);
    }

    internal static void MarkLame(
        DnsIterativeResolverState state,
        DnsIterativeAuthorityCandidate candidate)
    {
        DnsIterativeReachabilityState known = EnsureReachabilityState(
            state.ReachabilityStates,
            candidate.Zone,
            candidate.Endpoint);
        known.Lame = true;
        Suppress(known);
    }

    static void MergeServerState(
        DnsServerState destination,
        DnsServerState source)
    {
        destination.AttemptCount = destination.AttemptCount +
            source.AttemptCount;
        destination.LastAttempt = CopyClock(source.LastAttempt);
        if (source.HasSuccessfulExchange)
        {
            destination.HasSuccessfulExchange = true;
            destination.ConsecutiveFailures = 0;
            destination.LastSuccess = CopyClock(source.LastSuccess);
            destination.PreferredTransport = source.PreferredTransport;
            destination.HasSuppressedUntil = false;
        }
        else
        {
            int failures = source.ConsecutiveFailures;
            if (failures < 1)
            {
                failures = 1;
            }

            destination.ConsecutiveFailures =
                destination.ConsecutiveFailures + failures;
            destination.HasSuppressedUntil = true;
            destination.SuppressedUntil = CreateSuppressionDeadline();
        }

        if (source.HasEdnsCapability)
        {
            MergeEdnsState(destination, source);
        }
    }

    static void ObserveBasicResult(
        DnsServerState destination,
        DnsQueryResult result)
    {
        int attempts = result.Attempts.Count;
        if (attempts < 1)
        {
            attempts = 1;
        }

        destination.AttemptCount = destination.AttemptCount + attempts;
        destination.LastAttempt = TransportTime.ReadClock();
        if (result.Success)
        {
            destination.HasSuccessfulExchange = true;
            destination.ConsecutiveFailures = 0;
            destination.LastSuccess = CopyClock(destination.LastAttempt);
            destination.HasSuppressedUntil = false;
        }
        else
        {
            destination.ConsecutiveFailures =
                destination.ConsecutiveFailures + 1;
            destination.HasSuppressedUntil = true;
            destination.SuppressedUntil = CreateSuppressionDeadline();
        }
    }

    static void MergeEdnsState(
        DnsServerState destination,
        DnsServerState source)
    {
        if (!source.SupportsEdns)
        {
            destination.HasEdnsCapability = true;
            destination.SupportsEdns = false;
            destination.EdnsUdpPayloadSize =
                DnsProtocolLimits.ClassicUdpPayloadSize;
            return;
        }

        int payload = EdnsOperations.ClampPayloadSize(
            source.EdnsUdpPayloadSize);
        if (!destination.HasEdnsCapability ||
            !destination.SupportsEdns ||
            destination.EdnsUdpPayloadSize == 0 ||
            payload < destination.EdnsUdpPayloadSize)
        {
            destination.EdnsUdpPayloadSize = payload;
        }

        destination.HasEdnsCapability = true;
        destination.SupportsEdns = true;
    }

    static void MarkReachable(
        DnsIterativeResolverState state,
        DnsIterativeAuthorityCandidate candidate)
    {
        DnsIterativeReachabilityState known = EnsureReachabilityState(
            state.ReachabilityStates,
            candidate.Zone,
            candidate.Endpoint);
        known.ConsecutiveFailures = 0;
        known.Lame = false;
        known.HasSuppressedUntil = false;
    }

    static void Suppress(DnsIterativeReachabilityState state)
    {
        state.HasSuppressedUntil = true;
        state.SuppressedUntil = CreateSuppressionDeadline();
    }

    static DnsClockValue CreateSuppressionDeadline()
    {
        return TransportTime.CreateDeadlineAt(
            TransportTime.ReadClock(),
            SuppressionMilliseconds,
            false,
            new DnsClockValue());
    }

    static DnsServerState EnsureServerState(
        List<DnsServerState> states,
        DnsEndpoint endpoint)
    {
        DnsServerState? known = FindServerState(states, endpoint);
        if (known != null)
        {
            return known;
        }

        DnsServerState result = new DnsServerState();
        result.Endpoint = CopyEndpoint(endpoint);
        result.PreferredTransport = DnsTransportKind.Udp;
        states.Add(result);
        return result;
    }

    static DnsServerState? FindServerState(
        List<DnsServerState> states,
        DnsEndpoint endpoint)
    {
        int index = 0;
        while (index < states.Count)
        {
            if (EndpointsEqual(states[index].Endpoint, endpoint))
            {
                return states[index];
            }

            index = index + 1;
        }

        return null;
    }

    static DnsIterativeReachabilityState EnsureReachabilityState(
        List<DnsIterativeReachabilityState> states,
        DnsName zone,
        DnsEndpoint endpoint)
    {
        DnsIterativeReachabilityState? known = FindReachabilityState(
            states,
            zone,
            endpoint);
        if (known != null)
        {
            return known;
        }

        DnsIterativeReachabilityState result =
            new DnsIterativeReachabilityState();
        result.Zone = RecordSetOperations.CopyName(zone, true);
        result.Endpoint = CopyEndpoint(endpoint);
        states.Add(result);
        return result;
    }

    static DnsIterativeReachabilityState? FindReachabilityState(
        List<DnsIterativeReachabilityState> states,
        DnsName zone,
        DnsEndpoint endpoint)
    {
        int index = 0;
        while (index < states.Count)
        {
            if (RecordSetOperations.NamesEqual(states[index].Zone, zone) &&
                EndpointsEqual(states[index].Endpoint, endpoint))
            {
                return states[index];
            }

            index = index + 1;
        }

        return null;
    }

    static bool EndpointsEqual(DnsEndpoint left, DnsEndpoint right)
    {
        if (left.Port != right.Port ||
            left.HasAddress != right.HasAddress)
        {
            return false;
        }

        if (left.HasAddress)
        {
            return left.AddressFamily == right.AddressFamily &&
                BytesEqual(left.Address, right.Address);
        }

        return string.Equals(
            left.Host,
            right.Host,
            System.StringComparison.OrdinalIgnoreCase);
    }

    static bool BytesEqual(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static DnsEndpoint CopyEndpoint(DnsEndpoint source)
    {
        DnsEndpoint result = new DnsEndpoint();
        result.Host = source.Host;
        result.HasAddress = source.HasAddress;
        result.AddressFamily = source.AddressFamily;
        result.Port = source.Port;
        result.Address = RecordSetOperations.CopyBytes(source.Address);
        return result;
    }

    static DnsClockValue CopyClock(DnsClockValue source)
    {
        DnsClockValue result = new DnsClockValue();
        result.UnixTimeMilliseconds = source.UnixTimeMilliseconds;
        result.MonotonicMilliseconds = source.MonotonicMilliseconds;
        return result;
    }
}
