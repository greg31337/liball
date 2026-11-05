using System;
using System.Threading;

namespace dns;

internal static class TransportTime
{
    internal static DnsClockValue ReadClock()
    {
        DnsClockValue result = new DnsClockValue();
        result.UnixTimeMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        result.MonotonicMilliseconds = Environment.TickCount64;
        return result;
    }

    internal static DnsClockValue CreateDeadline(
        int timeoutMilliseconds,
        bool hasOuterDeadline,
        DnsClockValue outerDeadline)
    {
        return CreateDeadlineAt(
            ReadClock(),
            timeoutMilliseconds,
            hasOuterDeadline,
            outerDeadline);
    }

    internal static DnsClockValue CreateDeadlineAt(
        DnsClockValue now,
        int timeoutMilliseconds,
        bool hasOuterDeadline,
        DnsClockValue outerDeadline)
    {
        if (now == null || outerDeadline == null)
        {
            throw new ArgumentNullException("clock");
        }

        int boundedTimeout = timeoutMilliseconds;
        if (boundedTimeout < 0)
        {
            boundedTimeout = 0;
        }

        DnsClockValue result = new DnsClockValue();
        result.UnixTimeMilliseconds = AddSaturated(
            now.UnixTimeMilliseconds, boundedTimeout);
        result.MonotonicMilliseconds = AddSaturated(
            now.MonotonicMilliseconds, boundedTimeout);
        if (hasOuterDeadline &&
            outerDeadline.MonotonicMilliseconds < result.MonotonicMilliseconds)
        {
            result.UnixTimeMilliseconds = outerDeadline.UnixTimeMilliseconds;
            result.MonotonicMilliseconds = outerDeadline.MonotonicMilliseconds;
        }

        return result;
    }

    internal static int RemainingMilliseconds(DnsClockValue deadline)
    {
        if (deadline == null)
        {
            throw new ArgumentNullException("deadline");
        }

        return RemainingMillisecondsAt(
            deadline, Environment.TickCount64);
    }

    internal static int RemainingMillisecondsAt(
        DnsClockValue deadline,
        long monotonicMilliseconds)
    {
        if (deadline == null)
        {
            throw new ArgumentNullException("deadline");
        }

        long remaining = deadline.MonotonicMilliseconds -
            monotonicMilliseconds;
        if (remaining <= 0)
        {
            return 0;
        }

        if (remaining > int.MaxValue)
        {
            return int.MaxValue;
        }

        return (int)remaining;
    }

    internal static long ElapsedMilliseconds(
        DnsClockValue startedAt,
        DnsClockValue completedAt)
    {
        if (startedAt == null || completedAt == null)
        {
            throw new ArgumentNullException("clock");
        }

        long elapsed = completedAt.MonotonicMilliseconds -
            startedAt.MonotonicMilliseconds;
        if (elapsed < 0)
        {
            return 0;
        }

        return elapsed;
    }

    static long AddSaturated(long value, int addition)
    {
        if (value > long.MaxValue - addition)
        {
            return long.MaxValue;
        }

        return value + addition;
    }
}

internal static class TransportCancellation
{
    internal static bool IsRequested(
        bool cancellationRequested,
        CancellationToken cancellationToken)
    {
        return cancellationRequested || cancellationToken.IsCancellationRequested;
    }

    internal static bool IsStateRequested(TransportCancellationState state)
    {
        if (state == null)
        {
            throw new ArgumentNullException("state");
        }

        bool requested = state.CancellationRequested;
        if (state.Request != null)
        {
            requested = requested || state.Request.CancellationRequested;
        }

        return requested || state.CancellationToken.IsCancellationRequested;
    }
}

internal class TransportCancellationState
{
    internal bool CancellationRequested;
    internal DnsTransportRequest? Request;
    internal CancellationToken CancellationToken;
}
